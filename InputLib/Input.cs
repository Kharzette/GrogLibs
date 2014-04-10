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
		internal class HeldKeyInfo
		{
			internal long	mTimeHeld;
			internal Keys	mKey;
			internal int	mCode;

			internal HeldKeyInfo(KeyHeldInfo copyMe)
			{
				mTimeHeld	=copyMe.mTimeHeld;
				mKey		=copyMe.mKey;
				mCode		=copyMe.mCode;
			}
		}

		public class InputAction
		{
			public float	mTimeHeld;
			public Enum		mAction;	//user specified thing?

			internal InputAction(long timeHeld, Enum act)
			{
				mTimeHeld	=(float)timeHeld / ((float)Stopwatch.Frequency / 1000f);
				mAction		=act;
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

		//mappings to controllers / keys / mice / whatever
		Dictionary<int, ActionMapping>	mActionMap	=new Dictionary<int, ActionMapping>();


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
					mKeysHeld.Remove(kiea.MakeCode);
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
		}


		void Update()
		{
			long	ts	=Stopwatch.GetTimestamp();

			foreach(KeyValuePair<int, KeyHeldInfo> keys in mKeysHeld)
			{
				keys.Value.mTimeHeld	=ts - keys.Value.mInitialPressTime;
			}
		}


		public List<InputAction> GetAction()
		{
			Update();
			return	ComputeActions();
		}


		public void MapAction(Enum action, int keyCode)
		{
			if(mActionMap.ContainsKey(keyCode))
			{
				//overwrite existing?
				mActionMap[keyCode].mAction	=action;
				mActionMap[keyCode].mActionType	=ActionMapping.ActionTypes.ContinuousHold;
			}
			else
			{
				ActionMapping	amap	=new ActionMapping();

				amap.mAction		=action;
				amap.mActionType	=ActionMapping.ActionTypes.ContinuousHold;
				amap.mKeyCode		=keyCode;

				mActionMap.Add(keyCode, amap);
			}
		}


		List<InputAction> ComputeActions()
		{
			List<InputAction>	acts	=new List<InputAction>();

			long	ts	=Stopwatch.GetTimestamp();

			foreach(KeyValuePair<int, KeyHeldInfo> heldKey in mKeysHeld)
			{
				if(mActionMap.ContainsKey(heldKey.Key))
				{
					KeyHeldInfo	khi	=heldKey.Value;

					//make sure at least some time has passed
					if(khi.mInitialPressTime == ts || khi.mTimeHeld == 0)
					{
						continue;
					}

					ActionMapping	map	=mActionMap[heldKey.Key];

					InputAction	act	=new InputAction(heldKey.Value.mTimeHeld, map.mAction);

					acts.Add(act);

					//reset time
					heldKey.Value.mInitialPressTime	=ts;
					heldKey.Value.mTimeHeld			=0;
				}
			}

			mKeysUp.Clear();

			return	acts;
		}
	}
}
