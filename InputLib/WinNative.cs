using System;
using System.IO;
using System.Text;
using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace	InputLib;

internal static unsafe class WinNative
{
	internal const UInt32 	RIDI_PREPARSEDDATA	=0x20000005;
	internal const UInt32 	RIDI_DEVICENAME		=0x20000007;
	internal const UInt32 	RIDI_DEVICEINFO		=0x2000000b;
	internal const UInt32 	RID_HEADER			=0x10000005;
	internal const UInt32 	RID_INPUT			=0x10000003;

	//HID usage pages
	internal const UInt16	HID_USAGE_PAGE_GENERIC	=0x01;
	internal const UInt16	HID_USAGE_PAGE_GAME		=0x05;
	internal const UInt16	HID_USAGE_PAGE_LED		=0x08;
	internal const UInt16	HID_USAGE_PAGE_BUTTON	=0x09;

	//HID usages
	internal const UInt16	HID_USAGE_POINTER	=0x01;
	internal const UInt16	HID_USAGE_MOUSE		=0x02;
	internal const UInt16	HID_USAGE_JOYSTICK	=0x04;
	internal const UInt16	HID_USAGE_GAMEPAD	=0x05;
	internal const UInt16	HID_USAGE_KEYBOARD	=0x06;
	internal const UInt16	HID_USAGE_KEYPAD	=0x07;
	internal const UInt16	HID_USAGE_MULTIAXIS	=0x08;

	//for register flags
	internal const UInt32 	RIDEV_NOLEGACY		=0x00000030;
	internal const UInt32 	RIDEV_INPUTSINK		=0x00000100;	//background?
	internal const UInt32 	RIDEV_CAPTUREMOUSE	=0x00000200;
	internal const UInt32 	RIDEV_DEVNOTIFY		=0x00002000;	//connect msgs

	//mouse buttons down
	internal const UInt16	RI_MOUSE_BUTTON_1_DOWN	=0x0001;
	internal const UInt16	RI_MOUSE_BUTTON_2_DOWN	=0x0004;
	internal const UInt16	RI_MOUSE_BUTTON_3_DOWN	=0x0010;
	internal const UInt16	RI_MOUSE_BUTTON_4_DOWN	=0x0040;
	internal const UInt16	RI_MOUSE_BUTTON_5_DOWN	=0x0100;

	//mouse buttons up
	internal const UInt16	RI_MOUSE_BUTTON_1_UP	=0x0002;
	internal const UInt16	RI_MOUSE_BUTTON_2_UP	=0x0008;
	internal const UInt16	RI_MOUSE_BUTTON_3_UP	=0x0020;
	internal const UInt16	RI_MOUSE_BUTTON_4_UP	=0x0080;
	internal const UInt16	RI_MOUSE_BUTTON_5_UP	=0x0200;

	//other mouse junx
	internal const UInt16	RI_MOUSE_WHEEL	=0x0400;
	internal const UInt16	RI_MOUSE_HWHEEL	=0x0800;


	[StructLayout(LayoutKind.Explicit, Pack = 0)]
	internal struct RawInputDeviceList
	{
		[FieldOffset(0)]
		internal IntPtr hDevice;

		[FieldOffset(8)]
		internal UInt32 dwType;
	}

	[StructLayout(LayoutKind.Explicit)]
	internal struct DeviceInfo
	{
		[FieldOffset(0)]
		internal UInt32	cbSize;

		[FieldOffset(4)]
		internal UInt32	dwType;

		//these are unionized
		[FieldOffset(8)]
		internal DeviceInfoMouse	mouse;
		[FieldOffset(8)]
		internal DeviceInfoKeyboard	keyboard;
		[FieldOffset(8)]
		internal DeviceInfoHid		hid;
	}
	
	//used by register
	[StructLayout(LayoutKind.Explicit)]
	internal struct RegRawInputDevice
	{
		[FieldOffset(0)]
		internal UInt16	usUsagePage;
		[FieldOffset(2)]
		internal UInt16	usUsage;
		[FieldOffset(4)]
		internal UInt32	dwFlags;
		[FieldOffset(8)]
		internal IntPtr	hwndTarget;
	}

	[StructLayout(LayoutKind.Explicit)]
	internal struct RawInputHeader
	{
		[FieldOffset(0)]
		internal UInt32	dwType;
		[FieldOffset(4)]
		internal UInt32	dwSize;
		[FieldOffset(8)]
		internal IntPtr	hDevice;
		[FieldOffset(16)]
		internal IntPtr	wParam;
	}

	[StructLayout(LayoutKind.Explicit)]
	internal struct RawInputData
	{
		[FieldOffset(0)]
		internal RawInputHeader	header;

		//these are unioned
		[FieldOffset(24)]
		internal RawInputMouseData		mouse;
		[FieldOffset(24)]
		internal RawInputKeyboardData	keyboard;
		[FieldOffset(24)]
		internal RawInputHIDData		hid;
	}

	[StructLayout(LayoutKind.Explicit)]
	internal struct RawInputMouseData
	{
		[FieldOffset(0)]
		internal UInt16	usFlags;

		//these are unioned
		[FieldOffset(2)]
		internal UInt32	ulButtons;
		[FieldOffset(2)]
		internal UInt16	usButtonFlags;
		[FieldOffset(4)]
		internal UInt16	usButtonData;

		//aligns 4 for some reason
		[FieldOffset(8)]
		internal UInt32	ulRawButtons;

		[FieldOffset(12)]
		internal Int32	lLastX;
		[FieldOffset(16)]
		internal Int32	lLastY;

		[FieldOffset(20)]
		internal UInt32	ulExtraInformation;
	}

	[StructLayout(LayoutKind.Explicit)]
	internal struct RawInputKeyboardData
	{
		[FieldOffset(0)]
		internal UInt16	MakeCode;

		[FieldOffset(2)]
		internal UInt16	Flags;

		[FieldOffset(4)]
		internal UInt16	Reserved;

		[FieldOffset(6)]
		internal UInt16	VKey;

		[FieldOffset(8)]
		internal UInt32	Message;

		[FieldOffset(12)]
		internal UInt32	ExtraInformation;
	}

	[StructLayout(LayoutKind.Explicit)]
	internal struct RawInputHIDData
	{
		[FieldOffset(0)]
		internal UInt32	dwSizeHid;
		[FieldOffset(4)]
		internal UInt32	dwCount;
		[FieldOffset(8)]
		internal IntPtr	RawData;
	}

	internal struct DeviceInfoMouse
	{
		internal UInt32	dwId;
		internal UInt32	dwNumberOfButtons;
		internal UInt32	dwSampleRate;
		internal bool	fHasHorizontalWheel;
	}

	internal struct DeviceInfoKeyboard
	{
		internal UInt32	dwType;
		internal UInt32	dwSubType;
		internal UInt32	dwKeyboardMode;
		internal UInt32	dwNumberOfFunctionKeys;
		internal UInt32	dwNumberOfIndicators;
		internal UInt32	dwNumberOfKeysTotal;
	}
	
	internal struct DeviceInfoHid
	{
		internal uint	dwVendorID;
		internal uint	dwProductID;
		internal uint	dwVersionNumber;
		internal ushort	usUsagePage;
		internal ushort	usUsage;
	}

	//CreateFile Flags
	[Flags]
	public enum DesiredAccess : uint
	{
		None,
		Write = 0x40000000,
		Read = 0x80000000
	}
	
	[Flags]
	public enum ShareMode : uint
	{
		None,
		Read = 0x00000001,
		Write = 0x00000002,
		Delete = 0x00000004
	}
	
	public enum CreateDisposition : uint
	{
		CreateNew = 1,
		CreateAlways,
		OpenExisting,
		OpenAlways,
		TruncateExisting
	}

	internal static string GetLastError()
	{
		int	err	=Marshal.GetLastWin32Error();

		//alloc some ram for the message
		IntPtr	errMem	=Marshal.AllocHGlobal(512);

		FormatMessage(0x00001000,	//FROM_SYSTEM
			IntPtr.Zero, (uint)err, 0, errMem, 512, IntPtr.Zero);

		string	szErr	=Marshal.PtrToStringAnsi(errMem);

		Marshal.FreeHGlobal(errMem);

		return	szErr;
	}

	internal static string GetProductName(string longAssUSBName)
	{
		IntPtr	handy	=TryToOpenDevice(longAssUSBName);
		if(handy == null)
		{
			return	null;
		}

		//alloc some ram for the name
		IntPtr	nameMem	=Marshal.AllocHGlobal(4093);

		if(!HidD_GetProductString(handy, nameMem, 4093))
		{
			return	null;
		}

		CloseHandle(handy);

		string	productName	=Marshal.PtrToStringAuto(nameMem);

		Marshal.FreeHGlobal(nameMem);

		return	productName;
	}

	internal static DeviceInfo? GetDeviceInfo(IntPtr handle)
	{
		int		DILen	=sizeof(DeviceInfo);
		IntPtr	DIRam	=Marshal.AllocHGlobal((int)DILen);

		UInt32	dwDILen	=(UInt32)DILen;

		//GRIDI needs the size set, so gotta marshal twice
		DeviceInfo	di	=new DeviceInfo();

		//set size
		di.cbSize	=dwDILen;

		Marshal.StructureToPtr<DeviceInfo>(di, DIRam, false);

		UInt32	result	=GetRawInputDeviceInfo(
			handle,
			RIDI_DEVICEINFO,
			DIRam, ref dwDILen);

		if(result == 0xFFFFFFFF || result == 0)
		{
			return	null;
		}

		DILen	=(int)dwDILen;

		di	=Marshal.PtrToStructure<DeviceInfo>(DIRam);

		return	di;
	}

	internal static IntPtr TryToOpenDevice(string longAssUSBName)
	{
		IntPtr	handy	=CreateFile(longAssUSBName,
			DesiredAccess.None,		//The KEY!  This took DAYS to find
			ShareMode.Read | ShareMode.Write,
			IntPtr.Zero,
			CreateDisposition.OpenExisting,
			0, IntPtr.Zero);

		if(handy.ToInt64() == -1)
		{
			return	IntPtr.Zero;
		}
		return	handy;
	}

	internal static void GetDeviceList(out List<RawInputDeviceList> devList)
	{
		devList	=new List<RawInputDeviceList>();

		uint	numDevices	=0;
		int		idSize		=sizeof(RawInputDeviceList);

		//call with no buffer first to get num devices
		GetRawInputDeviceList(IntPtr.Zero, ref numDevices, (uint)idSize);

		//alloc some ram for the list
		IntPtr	devRam	=Marshal.AllocHGlobal((int)numDevices * idSize);

		GetRawInputDeviceList(devRam, ref numDevices, (uint)idSize);

		for(int i=0;i < numDevices;i++)
		{
			RawInputDeviceList	ridl	=Marshal.PtrToStructure<RawInputDeviceList>(
				devRam + (i * idSize));

			devList.Add(ridl);
		}

		Marshal.FreeHGlobal(devRam);
	}

	internal static RawInputHeader? GetHeaderFromRawInputMessage(IntPtr wParam, IntPtr lParam)
	{
		IntPtr	header	=Marshal.AllocHGlobal(sizeof(RawInputHeader));

		int	dataSize	=0;
		int	ret	=WinNative.GetRawInputData(lParam, RID_HEADER,
			header, ref dataSize, sizeof(RawInputHeader));
		if(ret == -1)
		{
			return	null;
		}

		RawInputHeader	rih	=Marshal.PtrToStructure<RawInputHeader>(header);

		Marshal.FreeHGlobal(header);

		return	rih;
	}

	//this has allocations but should be threadsafe maybe?
	internal static RawInputData? GetDataFromRawInputMessage(
		IntPtr wParam, IntPtr lParam)
	{
		//the hid message can be a strange size
		//pass null data to get dataSize
		int	dataSize	=0;
		int	ret	=GetRawInputData(lParam, RID_INPUT,
			IntPtr.Zero, ref dataSize, sizeof(RawInputHeader));
		if(dataSize <= 0 || ret == -1)
		{
			return	null;
		}

		IntPtr	data	=Marshal.AllocHGlobal(dataSize);

		ret	=WinNative.GetRawInputData(lParam, RID_INPUT,
			data, ref dataSize, sizeof(RawInputHeader));
		if(ret == -1)
		{
			Marshal.FreeHGlobal(data);
			return	null;
		}

		RawInputData	rid	=Marshal.PtrToStructure<RawInputData>(data);

		Marshal.FreeHGlobal(data);

		return	rid;
	}

	//no allocations
	internal static RawInputData? GetDataFromRawInputMessage(
		IntPtr wParam, IntPtr lParam, IntPtr msgBuf, int msgBufSize)
	{
		int	dataSize	=msgBufSize;
		int	ret	=WinNative.GetRawInputData(lParam, RID_INPUT,
			msgBuf, ref dataSize, sizeof(RawInputHeader));
		if(ret == -1)
		{
			return	null;
		}

		RawInputData	rid	=Marshal.PtrToStructure<RawInputData>(msgBuf);

		return	rid;
	}

	internal static void GetDeviceNames(List<RawInputDeviceList> devList,
		out Dictionary<IntPtr, string> devNames)
	{
		devNames	=new Dictionary<IntPtr, string>();

		//not really sure what max size is
		UInt32	nameSize	=512;

		//alloc some space for names, they are quite big
		IntPtr	nameMem	=Marshal.AllocHGlobal((int)nameSize);

		foreach(RawInputDeviceList ridl in devList)
		{
			UInt32	result	=GetRawInputDeviceInfo(ridl.hDevice,
				RIDI_DEVICENAME, nameMem, ref nameSize);
			
			if((int)result < 1)
			{
				//something went wrong
				continue;
			}

			string	name	=Marshal.PtrToStringAnsi(nameMem);

			devNames.Add(ridl.hDevice, name);
		}

		Marshal.FreeHGlobal(nameMem);
	}
	
	[DllImport("kernel32", SetLastError = true)]
	public static extern bool CloseHandle(IntPtr hObject);

	[DllImport("kernel32", EntryPoint = "CreateFile", SetLastError = true, CharSet = CharSet.Unicode)]
	static extern IntPtr CreateFile(string lpFileName,
		DesiredAccess dwDesiredAccess, ShareMode dwShareMode,
		IntPtr lpSecurityAttributes,
		CreateDisposition dwCreationDisposition,
		uint dwFlagsAndAttributes, IntPtr hTemplateFile);

	[DllImport("kernel32.dll")]
	static extern uint FormatMessage(uint dwFlags, IntPtr lpSource,
		uint dwMessageId, uint dwLanguageId, IntPtr lpBuffer,
		uint nSize, IntPtr Arguments);


	[DllImport("hid.dll", SetLastError=true, CharSet=CharSet.Unicode)]
	internal static extern bool	HidD_GetProductString(
		IntPtr	HidDeviceObject,
		IntPtr	Buffer,				//outs this
		UInt64	BufferLength);


	//need these for translating keys
	[DllImport("user32.dll")]
	internal static extern int MapVirtualKey(uint uCode, uint uMapType);

	[DllImport("user32", SetLastError=true, CharSet=CharSet.Unicode)]
	internal static extern int GetKeyNameTextW(uint lParam, StringBuilder lpString, int nSize);

	//rawinput stuff
	[DllImport("User32.dll", SetLastError = true)]
	public static extern int GetRawInputData(IntPtr hRawInput,
		UInt32 uiCommand,	//RID_HEADER or RID_INPUT
		IntPtr pData,
		ref int pcbSize,
		int cbSizeHeader);

	[DllImport("User32.dll", SetLastError = true)]
	internal static extern uint GetRawInputDeviceList(
		IntPtr pRawInputDeviceList, ref uint uiNumDevices, uint cbSize);
	
	[DllImport("user32.dll", SetLastError = true)]
	internal static extern uint GetRawInputDeviceInfo(
		IntPtr hDevice, uint uiCommand,
		IntPtr data, ref uint size);

	[DllImport("User32.dll", SetLastError = true)]
	internal static extern bool RegisterRawInputDevices(
		[MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0)]
		RegRawInputDevice[] pRawInputDevices,
		int uiNumDevices, int cbSize);
}