using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows.Forms;
using SharpDX.RawInput;
using SharpDX.Multimedia;


namespace InputLib
{
	public class Input
	{
		//useful for applying force such as game movement
		public class HeldKeyInfo
		{
			public long		mTimeHeld;
			public Keys		mKey;
			public int		mCode;


			internal HeldKeyInfo(KeyHeldInfo copyMe)
			{
				mTimeHeld	=copyMe.mTimeHeld / (Stopwatch.Frequency / 1000);
				mKey		=copyMe.mKey;
				mCode		=copyMe.mCode;
			}
		}

		internal class KeyHeldInfo
		{
			internal IntPtr	mDevice;
			internal long	mInitialPressTime;
			internal long	mTimeHeld;
			internal Keys	mKey;
			internal int	mCode;


			internal KeyHeldInfo(){}

			internal KeyHeldInfo(KeyHeldInfo copyMe)
			{
				mDevice				=copyMe.mDevice;
				mInitialPressTime	=copyMe.mInitialPressTime;
				mTimeHeld			=copyMe.mTimeHeld;
				mKey				=copyMe.mKey;
				mCode				=copyMe.mCode;
			}
		};

		//this stuff piles up between updates
		//via the handlers
		Dictionary<int, KeyHeldInfo>	mKeysHeld	=new Dictionary<int, KeyHeldInfo>();
		Dictionary<int, KeyHeldInfo>	mKeysUp		=new Dictionary<int, KeyHeldInfo>();

		//this is the data computed every update
		Dictionary<int, HeldKeyInfo>	mKeysHeldSince	=new Dictionary<int, HeldKeyInfo>();


		public Input()
		{
			List<DeviceInfo>	devs	=Device.GetDevices();

			Device.RegisterDevice(UsagePage.Generic, UsageId.GenericKeyboard, DeviceFlags.None);
			Device.RegisterDevice(UsagePage.Generic, UsageId.GenericMouse, DeviceFlags.None);
			Device.RegisterDevice(UsagePage.Generic, UsageId.GenericGamepad, DeviceFlags.None);

			Device.KeyboardInput	+=OnKeyInput;
		}


		void OnKeyInput(object sender, EventArgs ea)
		{
			KeyboardInputEventArgs	kiea	=ea as KeyboardInputEventArgs;
			if(kiea == null)
			{
				return;
			}

			long	ts	=Stopwatch.GetTimestamp();

			if(mKeysHeld.ContainsKey(kiea.MakeCode))
			{
				KeyHeldInfo	kh	=mKeysHeld[kiea.MakeCode];
				kh.mTimeHeld	=ts - kh.mInitialPressTime;
				if(kiea.State == KeyState.KeyUp)
				{
					if(mKeysUp.ContainsKey(kiea.MakeCode))
					{
						mKeysUp[kiea.MakeCode].mTimeHeld	=kh.mTimeHeld;
					}
					else
					{
						mKeysUp.Add(kiea.MakeCode, new KeyHeldInfo(kh));
					}
				}
			}
			else
			{
				KeyHeldInfo	kh			=new KeyHeldInfo();
				kh.mDevice				=kiea.Device;
				kh.mInitialPressTime	=ts;
				kh.mTimeHeld			=0;
				kh.mKey					=kiea.Key;
				kh.mCode				=kiea.MakeCode;

				mKeysHeld.Add(kiea.MakeCode, kh);
			}

			Debug.WriteLine(ts + "," + kiea.Key + "," + kiea.MakeCode
				+ "," + kiea.ScanCodeFlags + "," + kiea.State);
		}


		public void Update()
		{
			mKeysHeldSince.Clear();

			long	ts	=Stopwatch.GetTimestamp();

			foreach(KeyValuePair<int, KeyHeldInfo> keys in mKeysHeld)
			{
				if(!mKeysUp.ContainsKey(keys.Key))
				{
					//still held
					keys.Value.mTimeHeld	=ts - keys.Value.mInitialPressTime;
				}
				mKeysHeldSince.Add(keys.Key, new HeldKeyInfo(keys.Value));
			}

			mKeysUp.Clear();
			mKeysHeld.Clear();
		}


		public Dictionary<int, HeldKeyInfo> GetHeldKeyInfo()
		{
			return	mKeysHeldSince;
		}
	}
}
