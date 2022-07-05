using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows.Forms;
using SharpDX.Windows;
using Vortice.Multimedia;
using Vortice.DirectInput;
using Vortice.XInput;

using System.Runtime.InteropServices;

using FormsKeys	=System.Windows.Forms.Keys;


namespace InputLib;

public class Input
{
	//convert from cpu tics to whatever is desired
	static internal float	TimeScalar;

	public class InputAction
	{
		public enum DeviceType
		{
			KEYS, ANALOG, MOUSE
		}

		public float		mMultiplier;	//time or analog amount
		public Enum			mAction;		//user specified thing?
		public bool			mbTime;			//multiplier is time
		public DeviceType	mDevice;		//so can scale to user preferences

		internal InputAction(DeviceType dev, long timeHeld, Enum act)
		{
			//scale time to program preference
			mMultiplier	=(float)timeHeld * Input.TimeScalar;
			mAction		=act;
			mbTime		=true;
			mDevice		=dev;
		}

		internal InputAction(DeviceType dev, float amount, Enum act)
		{
			mMultiplier	=amount;
			mAction		=act;
			mbTime		=false;
			mDevice		=dev;
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
	}

	internal class MouseMovementInfo
	{
		internal IntPtr	mDevice;
		internal int	mXMove, mYMove;
	}

	public enum MoveAxis
	{
		MouseXAxis			=1000,
		MouseYAxis			=1002,
		GamePadLeftXAxis	=1003,
		GamePadLeftYAxis	=1004,
		GamePadRightXAxis	=1005,
		GamePadRightYAxis	=1006,
		GamePadLeftTrigger	=1007,
		GamePadRightTrigger	=1008
	};

	public enum VariousButtons
	{
		LeftMouseButton			=2000,
		RightMouseButton		=2001,
		MiddleMouseButton		=2002,
		MouseButton4			=2003,
		MouseButton5			=2004,
		MouseButton6			=2005,
		MouseButton7			=2006,
		MouseButton8			=2007,
		MouseButton9			=2008,
		MouseButton10			=2009,
		GamePadA				=2100,
		GamePadB				=2101,
		GamePadX				=2102,
		GamePadY				=2103,
		GamePadLeftAnalog		=2104,
		GamePadRightAnalog		=2105,
		GamePadLeftShoulder		=2106,
		GamePadRightShoulder	=2107,
		GamePadStart			=2108,
		GamePadBack				=2109,
		GamePadDPadUp			=2110,
		GamePadDPadDown			=2111,
		GamePadDPadLeft			=2112,
		GamePadDPadRight		=2113
	}

	//chunk of memory for processing rawinput messages
	IntPtr	mMsgMem;

	//list of device handles and types
	List<WinNative.RawInputDeviceList>	mDeviceList;

	//list of device_info
	Dictionary<IntPtr, WinNative.DeviceInfo>	mDeviceInfos	=new Dictionary<IntPtr, WinNative.DeviceInfo>();

	//list of device names from rawinput
	//these are superlong addresses
	Dictionary<IntPtr, string>	mDeviceNames;

	//actual name of the device such as
	//MegaCool over 9000 RGB 69 button mmo mouse hyper champion edition
	Dictionary<IntPtr, string>	mProductNames	=new Dictionary<IntPtr, string>();

	//this stuff piles up between updates
	//via the handlers
	Dictionary<int, KeyHeldInfo>	mKeysHeld	=new Dictionary<int, KeyHeldInfo>();
	Dictionary<int, KeyHeldInfo>	mKeysUp		=new Dictionary<int, KeyHeldInfo>();

	//all moves accumulated for the update
	MouseMovementInfo	mMouseMoves;

	//mappings to controllers / keys / mice / whatever
	Dictionary<UInt32, List<ActionMapping>>	mActionMap	=new Dictionary<uint, List<ActionMapping>>();

	//active toggles
	List<int>	mActiveToggles	=new List<int>();

	//fired actions (for once per press activations)
	List<int>	mOnceActives	=new List<int>();

	//for a press & release action, to ensure the combo was down
	List<UInt32>	mWasHeld	=new List<UInt32>();

	//buffer for winapi key name
	StringBuilder	mNameBuf	=new StringBuilder(260);

	//list of all possible modifiers for checking held key combos
	List<UInt32>	mModCombos	=new List<UInt32>();

	//sticks/pads
	//Controller	[]mXControllers;

	//keep track of buttons held on controllers
	List<int>	[]mXButtonsHeld	=new List<int>[4];

	System.Windows.Forms.KeysConverter	mKeyConverter	=new KeysConverter();

	//mouse pos at last update
	int		mLastMouseX, mLastMouseY;
	bool	mbResetPos;
	long	mLastUpdateTime;

	const float	DeadZone		=5000f;
	const int	MSG_RAM_SIZE	=256;	//enough maybe?


	public void OnInput(object sender, MessageEventArgs ea)
	{
		WinNative.RawInputData	?rid
			=WinNative.GetDataFromRawInputMessage(
				ea.mWParam, ea.mLParam,
				mMsgMem, MSG_RAM_SIZE);

		if(rid == null)
		{
			return;
		}

		WinNative.RawInputData	dat	=rid.Value;

		if(rid.Value.header.dwType == 0)
		{
			DoMouseInput(rid.Value.header.hDevice, ref dat.mouse);
			/*
			Console.WriteLine("Got input mouse message in the " + ground
//				+ " from device " + mDeviceNames[rid.Value.header.hDevice]);
				+ "X: " + rid.Value.mouse.lLastX
				+ " Y: " + rid.Value.mouse.lLastY
				+ ", usFlags: " + rid.Value.mouse.usFlags
				+ ", usButtonFlags: " + rid.Value.mouse.usButtonFlags
				+ ", usButtonData: " + rid.Value.mouse.usButtonData
				+ ", ulRawButtons: " + rid.Value.mouse.ulRawButtons
				+ ", ulButtons: " + rid.Value.mouse.ulButtons
				);*/
		}
		else if(rid.Value.header.dwType == 1)
		{
			string	ground	=(ea.mWParam.ToInt64()==0)? "foreground": "background";
			DoKeyInput(rid.Value.header.hDevice, ref dat.keyboard);
//			Console.WriteLine("Got input key message in the " + ground
//				+ " from device " + mDeviceNames[rid.Value.header.hDevice]);
//				+ "MakeCode: " + rid.Value.keyboard.MakeCode
//				+ " VKey: " + rid.Value.keyboard.VKey);
		}
		else
		{
			string	ground	=(ea.mWParam.ToInt64()==0)? "foreground": "background";
			Console.WriteLine("Got input hid message in the " + ground
//				+ " from device " + mDeviceNames[rid.Value.header.hDevice]);
				+ "count: " + rid.Value.hid.dwCount
				+ " size: " + rid.Value.hid.dwSizeHid);
		}
	}


	public unsafe Input(float timeScalar, RenderForm hwnd)
	{
		TimeScalar	=timeScalar;

		mMsgMem	=Marshal.AllocHGlobal(MSG_RAM_SIZE);

		EnumDevices();

		//listen for rawinput messages
		//this ties to sharpjunx, probably bad
		hwnd.Input	+=OnInput;

		//interested in keyboards, and mice?
		//I'll do sticks and pads later
		WinNative.RegRawInputDevice	[]regs
			=new WinNative.RegRawInputDevice[2];

		//keys
		regs[0].usUsage		=WinNative.HID_USAGE_KEYBOARD;
		regs[0].usUsagePage	=WinNative.HID_USAGE_PAGE_GENERIC;
		regs[0].dwFlags		=0;	//nothing
		regs[0].hwndTarget	=hwnd.Handle;

		//meeces, set noleg so setcapture works
		regs[1].usUsage		=WinNative.HID_USAGE_MOUSE;
		regs[1].usUsagePage	=WinNative.HID_USAGE_PAGE_GENERIC;
		regs[1].dwFlags		=0;
//		regs[1].dwFlags		=WinNative.RIDEV_CAPTUREMOUSE
//							| WinNative.RIDEV_NOLEGACY;
		regs[1].hwndTarget	=hwnd.Handle;


		WinNative.RegisterRawInputDevices(regs, 2, sizeof(WinNative.RegRawInputDevice));

		/*
		mbResetPos		=true;

		for(int i=0;i < 4 ;i++)
		{
			mXButtonsHeld[i]	=new List<int>();
		}*/

		mLastUpdateTime	=Stopwatch.GetTimestamp();

		//modifier combos pre shifted
		mModCombos.Add((UInt32)Modifiers.None << 29);
		mModCombos.Add((UInt32)Modifiers.ShiftHeld << 29);
		mModCombos.Add((UInt32)(Modifiers.ControlHeld) << 29);
		mModCombos.Add((UInt32)(Modifiers.ShiftHeld | Modifiers.ControlHeld) << 29);
		mModCombos.Add((UInt32)(Modifiers.AltHeld) << 29);
		mModCombos.Add((UInt32)(Modifiers.ShiftHeld | Modifiers.AltHeld) << 29);
		mModCombos.Add((UInt32)(Modifiers.ControlHeld | Modifiers.AltHeld) << 29);
		mModCombos.Add((UInt32)(Modifiers.ShiftHeld | Modifiers.ControlHeld | Modifiers.AltHeld) << 29);
	}


	unsafe void EnumDevices()
	{
		WinNative.GetDeviceList(out mDeviceList);

		WinNative.GetDeviceNames(mDeviceList, out mDeviceNames);

		//grab infos and product names
		foreach(KeyValuePair<IntPtr, string> dn in mDeviceNames)
		{
			string	prodName	=WinNative.GetProductName(dn.Value);
			if(prodName != null)
			{
				mProductNames.Add(dn.Key, prodName);
			}

			WinNative.DeviceInfo	?di	=WinNative.GetDeviceInfo(dn.Key);
			if(di != null)
			{
				mDeviceInfos.Add(dn.Key, di.Value);
			}
		}
	}


	public void FreeAll(RenderForm hwnd)
	{
		hwnd.Input	-=OnInput;
		Marshal.FreeHGlobal(mMsgMem);
	}


	//in case of big delta times in a game, like sitting at a breakpoint
	//or a big drop in framerate
	//this is only needed for ActionTypes.ContinuousHold
	public void ClampInputTimes(float amount)
	{
		long	clampTo	=(long)(amount / TimeScalar);

		foreach(KeyValuePair<int, KeyHeldInfo> key in mKeysHeld)
		{
			key.Value.mTimeHeld =Math.Min(key.Value.mTimeHeld, clampTo);
//				Debug.WriteLine("" + key.Value.mTimeHeld);
		}
	}

/*
	public bool IsXControllerConnected(int index)
	{
		return	mXControllers[index].IsConnected;
	}*/


	//C# really needs const
	unsafe void DoMouseInput(IntPtr device, ref WinNative.RawInputMouseData rimd)
	{
		if(mbResetPos)
		{
			mLastMouseX	=rimd.lLastX;
			mLastMouseY	=rimd.lLastY;
			mbResetPos	=false;
		}

		if(rimd.lLastX != 0 || rimd.lLastY != 0)
		{
			if(mMouseMoves == null)
			{
				mMouseMoves			=new MouseMovementInfo();
				mMouseMoves.mDevice	=device;
			}
			mMouseMoves.mXMove	+=rimd.lLastX;
			mMouseMoves.mYMove	+=rimd.lLastY;
		}

		List<int>	buttonsDown	=new List<int>();
		List<int>	buttonsUp	=new List<int>();
		if(rimd.usButtonFlags != 0)
		{
			if((rimd.usButtonFlags & WinNative.RI_MOUSE_BUTTON_1_DOWN) != 0)
			{
				buttonsDown.Add((int)VariousButtons.LeftMouseButton);
			}
			if((rimd.usButtonFlags & WinNative.RI_MOUSE_BUTTON_2_DOWN) != 0)
			{
				buttonsDown.Add((int)VariousButtons.RightMouseButton);
			}
			if((rimd.usButtonFlags & WinNative.RI_MOUSE_BUTTON_3_DOWN) != 0)
			{
				buttonsDown.Add((int)VariousButtons.MiddleMouseButton);
			}
			if((rimd.usButtonFlags & WinNative.RI_MOUSE_BUTTON_4_DOWN) != 0)
			{
				buttonsDown.Add((int)VariousButtons.MouseButton4);
			}
			if((rimd.usButtonFlags & WinNative.RI_MOUSE_BUTTON_5_DOWN) != 0)
			{
				buttonsDown.Add((int)VariousButtons.MouseButton5);
			}

			if((rimd.usButtonFlags & WinNative.RI_MOUSE_BUTTON_1_UP) != 0)
			{
				buttonsUp.Add((int)VariousButtons.LeftMouseButton);
			}
			if((rimd.usButtonFlags & WinNative.RI_MOUSE_BUTTON_2_UP) != 0)
			{
				buttonsUp.Add((int)VariousButtons.RightMouseButton);
			}
			if((rimd.usButtonFlags & WinNative.RI_MOUSE_BUTTON_3_UP) != 0)
			{
				buttonsUp.Add((int)VariousButtons.MiddleMouseButton);
			}
			if((rimd.usButtonFlags & WinNative.RI_MOUSE_BUTTON_4_UP) != 0)
			{
				buttonsUp.Add((int)VariousButtons.MouseButton4);
			}
			if((rimd.usButtonFlags & WinNative.RI_MOUSE_BUTTON_5_UP) != 0)
			{
				buttonsUp.Add((int)VariousButtons.MouseButton5);
			}
		}

		long	ts	=Stopwatch.GetTimestamp();
		foreach(int down in buttonsDown)
		{
			AddHeldKey(down, Keys.NoName, ts, false, device);
		}
		foreach(int up in buttonsUp)
		{
			AddHeldKey(up, Keys.NoName, ts, true, device);
		}

		if(!mbResetPos)
		{
			mLastMouseX	=rimd.lLastX;
			mLastMouseY	=rimd.lLastY;
		}
	}


	void AddHeldKey(int code, Keys key, long ts, bool bUp, IntPtr dev)
	{
		if(mKeysHeld.ContainsKey(code))
		{
			KeyHeldInfo	kh	=mKeysHeld[code];
			kh.mTimeHeld	=ts - kh.mInitialPressTime;
			if(bUp)
			{
				if(mKeysUp.ContainsKey(code))
				{
					mKeysUp[code].mTimeHeld	=kh.mTimeHeld;
				}
				else
				{
					mKeysUp.Add(code, new KeyHeldInfo(kh));
				}
				mKeysHeld.Remove(code);
			}
		}
		else
		{
			if(bUp)
			{
				//Land here after a ctrl-alt-del with code 1
				return;
			}
			KeyHeldInfo	kh			=new KeyHeldInfo();
			kh.mDevice				=dev;
			kh.mInitialPressTime	=ts;
			kh.mTimeHeld			=0;
			kh.mKey					=key;
			kh.mCode				=code;

			mKeysHeld.Add(code, kh);
		}
	}

/*
	public string GetKeyName(int makeCode, ScanCodeFlags scf)
	{
		uint	e0Thing	=0;
		
		if(((int)scf & (int)ScanCodeFlags.E0) != 0)
		{
			e0Thing	=(1 << 24);
		}

		uint	bigKey	=((uint)makeCode << 16) | e0Thing;

		int ret	=GetKeyNameTextW(bigKey, mNameBuf, 260);

		if(ret != 0)
		{
			return	mNameBuf.ToString();
		}
		return	"Unknown";
	}
*/


	public void ClearInputs()
	{
		mKeysHeld.Clear();
		mKeysUp.Clear();
		mActiveToggles.Clear();
		mOnceActives.Clear();
		mWasHeld.Clear();
//		foreach(List<int> xb in mXButtonsHeld)
//		{
//			xb.Clear();
//		}
		mMouseMoves	=null;
	}


	void DoKeyInput(IntPtr device, ref WinNative.RawInputKeyboardData keyData)
	{
		if((int)keyData.VKey == 255)
		{
			return;	//fake key
		}

		long	ts	=Stopwatch.GetTimestamp();

		AddHeldKey(keyData.MakeCode, (Keys)keyData.VKey, ts,
			(keyData.Flags & WinNative.RI_KEY_BREAK) != 0,
			device);
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

		List<InputAction>	ret	=ComputeActions();

		mLastUpdateTime	=Stopwatch.GetTimestamp();

		return	ret;
	}


	UInt32 KeyPlusMod(int keyCode, Modifiers mod)
	{
		return	(UInt32)(keyCode & 0x1FFFFFFF) | ((UInt32)mod << 29);
	}


	public void MapAction(Enum action, ActionTypes mode,
		Modifiers mod, int keyCode)
	{
		Debug.Assert(mode != ActionTypes.Toggle);

		UInt32	code	=KeyPlusMod(keyCode, mod);

		ActionMapping	amap	=new ActionMapping();

		amap.mAction		=action;
		amap.mActionType	=mode;
		amap.mKeyCode		=keyCode;
		amap.mModifier		=mod;

		if(!mActionMap.ContainsKey(code))			
		{
			mActionMap.Add(code, new List<ActionMapping>());
		}
		mActionMap[code].Add(amap);
	}


	public void MapAction(Enum action, ActionTypes mode,
		Modifiers mod, VariousButtons button)
	{
		MapAction(action, mode, mod, (int)button);
	}


	public void MapAction(Enum Action, ActionTypes mode,
		Modifiers mod, FormsKeys key)
	{
		int	keyCode	=WinNative.MapVirtualKey((uint)key, 0);

		MapAction(Action, mode, mod, keyCode);
	}


	public void MapToggleAction(Enum action, Enum actionOff, Modifiers mod, int keyCode)
	{
		UInt32	code	=KeyPlusMod(keyCode, mod);

		ActionMapping	amap	=new ActionMapping();

		amap.mAction		=action;
		amap.mModifier		=mod;
		amap.mActionOff		=actionOff;
		amap.mActionType	=ActionTypes.Toggle;
		amap.mKeyCode		=keyCode;

		if(!mActionMap.ContainsKey(code))
		{
			mActionMap.Add(code, new List<ActionMapping>());
		}

		mActionMap[code].Add(amap);
	}


	public void MapToggleAction(Enum action, Enum actionOff,
		Modifiers mod, VariousButtons button)
	{
		MapToggleAction(action, actionOff, mod, (int)button);
	}


	public void MapToggleAction(Enum action, Enum actionOff,
		Modifiers mod, FormsKeys key)
	{
		int	keyCode	=WinNative.MapVirtualKey((uint)key, 0);

		MapToggleAction(action, actionOff, mod, keyCode);
	}


	public void MapAxisAction(Enum action, MoveAxis ma)
	{
		UInt32	moveCode	=(UInt32)ma;

		if(!mActionMap.ContainsKey(moveCode))
		{
			mActionMap.Add(moveCode, new List<ActionMapping>());
		}

		ActionMapping	amap	=new ActionMapping();

		amap.mAction		=action;
		amap.mActionType	=ActionTypes.AnalogAmount;
		amap.mKeyCode		=(int)moveCode;

		mActionMap[moveCode].Add(amap);
	}


	public void UnMapAction(int keyCode, Modifiers mod)
	{
		UInt32	code	=KeyPlusMod(keyCode, mod);

		if(mActionMap.ContainsKey(code))
		{
			mActionMap.Remove(code);
		}
	}


	public void UnMapAction(FormsKeys key, Modifiers mod)
	{
		int	keyCode	=WinNative.MapVirtualKey((uint)key, 0);

		UnMapAction(keyCode, mod);
	}


	public void UnMapAxisAction(MoveAxis ma)
	{
		UInt32	moveCode	=(UInt32)ma;

		if(mActionMap.ContainsKey(moveCode))
		{
			mActionMap.Remove(moveCode);
		}
	}


	bool IsModifierHeld(UInt32 mod)
	{
		//test shift/ctrl/alt
		bool	bShiftHeld	=false;
		bool	bCtrlHeld	=false;
		bool	bAltHeld	=false;

		foreach(KeyValuePair<int, KeyHeldInfo> held in mKeysHeld)
		{
			FormsKeys	key	=(FormsKeys)WinNative.MapVirtualKey((uint)held.Key, 1);
			if(key == FormsKeys.ShiftKey)
			{
				bShiftHeld	=true;
			}
			else if(key == FormsKeys.ControlKey)
			{
				bCtrlHeld	=true;
			}
			else if(key == FormsKeys.Menu)
			{
				bAltHeld	=true;
			}
		}

		if(mod == (UInt32)Modifiers.None)
		{
			//none wants no modifier keys held
			return	(!bShiftHeld && !bCtrlHeld && !bAltHeld);
		}

		bool	bRet	=true;

		if((mod & ((UInt32)Modifiers.ShiftHeld << 29)) != 0)
		{
			bRet	&=bShiftHeld;
		}

		if((mod & ((UInt32)Modifiers.ControlHeld << 29)) != 0)
		{
			bRet	&=bCtrlHeld;
		}

		if((mod & ((UInt32)Modifiers.AltHeld << 29)) != 0)
		{
			bRet	&=bAltHeld;
		}

		return	bRet;
	}


	List<InputAction> ComputeActions()
	{
		List<InputAction>	acts	=new List<InputAction>();

		long	ts	=Stopwatch.GetTimestamp();

/*
		for(int i=0;i < 4;i++)
		{
			if(!mXControllers[i].IsConnected)
			{
				continue;
			}

			State	state	=mXControllers[i].GetState();

			if(mActionMap.ContainsKey((int)MoveAxis.GamePadLeftXAxis))
			{
				if(state.Gamepad.LeftThumbX < -Gamepad.LeftThumbDeadZone
					|| state.Gamepad.LeftThumbX > Gamepad.LeftThumbDeadZone)
				{
					foreach(ActionMapping am in mActionMap[(int)MoveAxis.GamePadLeftXAxis])
					{
						float	ax		=state.Gamepad.LeftThumbX / 32768f;
						InputAction	ma	=new InputAction(
							InputAction.DeviceType.ANALOG, ax, am.mAction);
						acts.Add(ma);
					}
				}
			}

			if(mActionMap.ContainsKey((int)MoveAxis.GamePadLeftYAxis))
			{
				if(state.Gamepad.LeftThumbY < -Gamepad.LeftThumbDeadZone
					|| state.Gamepad.LeftThumbY > Gamepad.LeftThumbDeadZone)
				{
					foreach(ActionMapping am in mActionMap[(int)MoveAxis.GamePadLeftYAxis])
					{
						float	ax		=state.Gamepad.LeftThumbY / 32768f;
						InputAction	ma	=new InputAction(
							InputAction.DeviceType.ANALOG, ax, am.mAction);
						acts.Add(ma);
					}
				}
			}

			if(mActionMap.ContainsKey((int)MoveAxis.GamePadRightXAxis))
			{
				if(state.Gamepad.RightThumbX < -Gamepad.RightThumbDeadZone
					|| state.Gamepad.RightThumbX > Gamepad.RightThumbDeadZone)
				{
					foreach(ActionMapping am in mActionMap[(int)MoveAxis.GamePadRightXAxis])
					{
						float	ax		=state.Gamepad.RightThumbX / 32768f;
						InputAction	ma	=new InputAction(
							InputAction.DeviceType.ANALOG, ax, am.mAction);
						acts.Add(ma);
					}
				}
			}

			if(mActionMap.ContainsKey((int)MoveAxis.GamePadRightYAxis))
			{
				if(state.Gamepad.RightThumbY < -Gamepad.RightThumbDeadZone
					|| state.Gamepad.RightThumbY > Gamepad.RightThumbDeadZone)
				{
					foreach(ActionMapping am in mActionMap[(int)MoveAxis.GamePadRightYAxis])
					{
						float	ax		=state.Gamepad.RightThumbY / 32768f;
						InputAction	ma	=new InputAction(
							InputAction.DeviceType.ANALOG, ax, am.mAction);
						acts.Add(ma);
					}
				}
			}

			if(mActionMap.ContainsKey((int)MoveAxis.GamePadLeftTrigger))
			{
				if(state.Gamepad.LeftTrigger > Gamepad.TriggerThreshold)
				{
					foreach(ActionMapping am in mActionMap[(int)MoveAxis.GamePadLeftTrigger])
					{
						InputAction	ma	=new InputAction(
							InputAction.DeviceType.ANALOG,
							state.Gamepad.LeftTrigger / 255f, am.mAction);
						acts.Add(ma);
					}
				}
			}

			if(mActionMap.ContainsKey((int)MoveAxis.GamePadRightTrigger))
			{
				if(state.Gamepad.RightTrigger > Gamepad.TriggerThreshold)
				{
					foreach(ActionMapping am in mActionMap[(int)MoveAxis.GamePadRightTrigger])
					{
						InputAction	ma	=new InputAction(
							InputAction.DeviceType.ANALOG,
							(float)state.Gamepad.RightTrigger / 255f,
							am.mAction);
						acts.Add(ma);
					}
				}
			}

			List<int>	toNuke	=new List<int>();

			//check for up events
			foreach(int vb in mXButtonsHeld[i])
			{
				if(CheckButtonUp(state, ts, i, (VariousButtons)vb))
				{
					toNuke.Add(vb);
				}
			}

			foreach(int nuke in toNuke)
			{
				mXButtonsHeld[i].Remove(nuke);
			}

			ProcessXButton(state, ts, i, VariousButtons.GamePadA);
			ProcessXButton(state, ts, i, VariousButtons.GamePadB);
			ProcessXButton(state, ts, i, VariousButtons.GamePadX);
			ProcessXButton(state, ts, i, VariousButtons.GamePadY);
			ProcessXButton(state, ts, i, VariousButtons.GamePadBack);
			ProcessXButton(state, ts, i, VariousButtons.GamePadDPadDown);
			ProcessXButton(state, ts, i, VariousButtons.GamePadDPadLeft);
			ProcessXButton(state, ts, i, VariousButtons.GamePadDPadRight);
			ProcessXButton(state, ts, i, VariousButtons.GamePadDPadUp);
			ProcessXButton(state, ts, i, VariousButtons.GamePadLeftShoulder);
			ProcessXButton(state, ts, i, VariousButtons.GamePadRightShoulder);
			ProcessXButton(state, ts, i, VariousButtons.GamePadLeftAnalog);
			ProcessXButton(state, ts, i, VariousButtons.GamePadRightAnalog);
			ProcessXButton(state, ts, i, VariousButtons.GamePadStart);
		}*/

		foreach(KeyValuePair<int, KeyHeldInfo> heldKey in mKeysHeld)
		{
			foreach(UInt32 combo in mModCombos)
			{
				UInt32	modKey	=combo | (UInt32)(heldKey.Key & 0x1FFFFFFF);

				if(mActionMap.ContainsKey(modKey) && IsModifierHeld(combo))
				{
					KeyHeldInfo	khi	=heldKey.Value;

					//make sure at least some time has passed
					if(khi.mInitialPressTime == ts || khi.mTimeHeld == 0)
					{
						continue;
					}

					foreach(ActionMapping am in mActionMap[modKey])
					{
						if(am.mActionType == ActionTypes.ContinuousHold)
						{
							InputAction	act	=new InputAction(
								InputAction.DeviceType.KEYS, heldKey.Value.mTimeHeld, am.mAction);
							acts.Add(act);

							//reset time
							heldKey.Value.mInitialPressTime	=ts;
							heldKey.Value.mTimeHeld			=0;
						}
						else if(am.mActionType == ActionTypes.PressAndRelease)
						{
							if(!mWasHeld.Contains(modKey))
							{
								mWasHeld.Add(modKey);
							}
						}
						else if(am.mActionType == ActionTypes.Toggle)
						{
							if(!mActiveToggles.Contains(heldKey.Key))
							{
								//toggle on
								InputAction	act	=new InputAction(
									InputAction.DeviceType.KEYS, heldKey.Value.mTimeHeld, am.mAction);
								acts.Add(act);

								mActiveToggles.Add(heldKey.Key);
							}
						}
						else if(am.mActionType == ActionTypes.ActivateOnce)
						{
							if(!mOnceActives.Contains(heldKey.Key))
							{
								//not yet fired
								InputAction	act	=new InputAction(
									InputAction.DeviceType.KEYS, heldKey.Value.mTimeHeld, am.mAction);
								acts.Add(act);

								mOnceActives.Add(heldKey.Key);
							}
						}
						else
						{
							Debug.Assert(false);
						}
					}
				}
			}
		}

		foreach(KeyValuePair<int, KeyHeldInfo> heldKey in mKeysUp)
		{
			foreach(UInt32 combo in mModCombos)
			{
				UInt32	modKey	=combo | (UInt32)(heldKey.Key & 0x1FFFFFFF);

				if(mActionMap.ContainsKey(modKey))
				{
					KeyHeldInfo	khi	=heldKey.Value;

					//make sure at least some time has passed
					if(khi.mInitialPressTime == ts || khi.mTimeHeld == 0)
					{
						continue;
					}

					foreach(ActionMapping am in mActionMap[modKey])
					{
						if(am.mActionType == ActionTypes.ContinuousHold)
						{
							//nothing to do here
						}
						else if(am.mActionType == ActionTypes.PressAndRelease)
						{
							if(mWasHeld.Contains(modKey))
							{
								//release action
								InputAction	act	=new InputAction(
									InputAction.DeviceType.KEYS, heldKey.Value.mTimeHeld, am.mAction);
								acts.Add(act);
								mWasHeld.Remove(modKey);
							}
						}
						else if(am.mActionType == ActionTypes.Toggle)
						{
							if(mActiveToggles.Contains(heldKey.Key))
							{
								//toggle off
								InputAction	act	=new InputAction(
									InputAction.DeviceType.KEYS, heldKey.Value.mTimeHeld, am.mActionOff);
								acts.Add(act);

								mActiveToggles.Remove(heldKey.Key);
							}
						}
						else if(am.mActionType == ActionTypes.ActivateOnce)
						{
							if(mOnceActives.Contains(heldKey.Key))
							{
								mOnceActives.Remove(heldKey.Key);
							}
						}
						else
						{
							Debug.Assert(false);
						}
					}
				}
			}
		}

		if(mMouseMoves != null)
		{
			if(mMouseMoves.mXMove != 0)
			{
				if(mActionMap.ContainsKey((int)MoveAxis.MouseXAxis))
				{
					foreach(ActionMapping am in mActionMap[(int)MoveAxis.MouseXAxis])
					{
						InputAction	ma	=new InputAction(
							InputAction.DeviceType.MOUSE, (float)mMouseMoves.mXMove, am.mAction);

						acts.Add(ma);
					}
				}
			}

			if(mMouseMoves.mYMove != 0)
			{
				if(mActionMap.ContainsKey((int)MoveAxis.MouseYAxis))
				{
					foreach(ActionMapping am in mActionMap[(int)MoveAxis.MouseYAxis])
					{
						InputAction	ma	=new InputAction(
							InputAction.DeviceType.MOUSE, (float)mMouseMoves.mYMove, am.mAction);

						acts.Add(ma);
					}
				}
			}
		}

		mKeysUp.Clear();
		mMouseMoves	=null;

		return	acts;
	}

/*
	GamepadButtonFlags	VariousToGPBF(VariousButtons vb)
	{
		switch(vb)
		{
			case	VariousButtons.GamePadA:
				return	GamepadButtonFlags.A;
			case	VariousButtons.GamePadB:
				return	GamepadButtonFlags.B;
			case	VariousButtons.GamePadX:
				return	GamepadButtonFlags.X;
			case	VariousButtons.GamePadY:
				return	GamepadButtonFlags.Y;
			case	VariousButtons.GamePadBack:
				return	GamepadButtonFlags.Back;
			case	VariousButtons.GamePadDPadDown:
				return	GamepadButtonFlags.DPadDown;
			case	VariousButtons.GamePadDPadLeft:
				return	GamepadButtonFlags.DPadLeft;
			case	VariousButtons.GamePadDPadRight:
				return	GamepadButtonFlags.DPadRight;
			case	VariousButtons.GamePadDPadUp:
				return	GamepadButtonFlags.DPadUp;
			case	VariousButtons.GamePadLeftAnalog:
				return	GamepadButtonFlags.LeftThumb;
			case	VariousButtons.GamePadLeftShoulder:
				return	GamepadButtonFlags.LeftShoulder;
			case	VariousButtons.GamePadRightAnalog:
				return	GamepadButtonFlags.RightThumb;
			case	VariousButtons.GamePadRightShoulder:
				return	GamepadButtonFlags.RightShoulder;
			case	VariousButtons.GamePadStart:
				return	GamepadButtonFlags.Start;
		}
		return	GamepadButtonFlags.None;
	}


	bool CheckButtonUp(State state, long ts, int index, VariousButtons vb)
	{
		GamepadButtonFlags	gpbf	=VariousToGPBF(vb);

		if(!state.Gamepad.Buttons.HasFlag(gpbf))
		{
			AddHeldKey((int)vb, Keys.NoName, ts, true, IntPtr.Zero);
			return	true;
		}
		return	false;
	}


	void ProcessXButton(State state, long ts, int index, VariousButtons button)
	{
		if(state.Gamepad.Buttons.HasFlag(VariousToGPBF(button)))
		{
			AddHeldKey((int)button, Keys.NoName, ts, false, IntPtr.Zero);
			if(!mXButtonsHeld[index].Contains((int)button))
			{
				mXButtonsHeld[index].Add((int)button);
			}
		}
	}*/
}
