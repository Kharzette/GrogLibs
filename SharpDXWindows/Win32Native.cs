// Copyright (c) 2010-2014 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using Vortice.Win32;

namespace SharpDX;

/// <summary>
/// Internal class to interact with Native Message
/// </summary>
partial class Win32Native
{
    /// <summary>
    /// Interop type for a Rectangle (4 ints).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    [DebuggerDisplay("Left: {Left}, Top: {Top}, Right: {Right}, Bottom: {Bottom}")]
    public struct RawRectangle
    {
        public RawRectangle(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        /// <summary>
        /// The left position.
        /// </summary>
        public int Left;

        /// <summary>
        /// The top position.
        /// </summary>
        public int Top;

        /// <summary>
        /// The right position
        /// </summary>
        public int Right;

        /// <summary>
        /// The bottom position.
        /// </summary>
        public int Bottom;

        /// <summary>
        /// Gets a value indicating whether this instance is empty.
        /// </summary>
        /// <value><c>true</c> if this instance is empty; otherwise, <c>false</c>.</value>
        public bool IsEmpty
        {
            get { return Left == 0 && Top == 0 && Right == 0 && Bottom == 0; }
        }
    }

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct TextMetric
	{
		public int tmHeight;
		public int tmAscent;
		public int tmDescent;
		public int tmInternalLeading;
		public int tmExternalLeading;
		public int tmAveCharWidth;
		public int tmMaxCharWidth;
		public int tmWeight;
		public int tmOverhang;
		public int tmDigitizedAspectX;
		public int tmDigitizedAspectY;
		public char tmFirstChar;
		public char tmLastChar;
		public char tmDefaultChar;
		public char tmBreakChar;
		public byte tmItalic;
		public byte tmUnderlined;
		public byte tmStruckOut;
		public byte tmPitchAndFamily;
		public byte tmCharSet;
	}

	[DllImport("user32.dll", EntryPoint = "PeekMessage")]
	public static extern int PeekMessage(out NativeMessage lpMsg, IntPtr hWnd, int wMsgFilterMin,
											int wMsgFilterMax, int wRemoveMsg);

	[DllImport("user32.dll", EntryPoint = "GetMessage")]
	public static extern int GetMessage(out NativeMessage lpMsg, IntPtr hWnd, int wMsgFilterMin,
											int wMsgFilterMax);

	[DllImport("user32.dll", EntryPoint = "TranslateMessage")]
	public static extern int TranslateMessage(ref NativeMessage lpMsg);

	[DllImport("user32.dll", EntryPoint = "DispatchMessage")]
	public static extern int DispatchMessage(ref NativeMessage lpMsg);

	public enum WindowLongType : int
	{
		WndProc = (-4),
		HInstance = (-6),
		HwndParent = (-8),
		Style = (-16),
		ExtendedStyle = (-20),
		UserData = (-21),
		Id = (-12)
	}

	public delegate IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

	public static IntPtr GetWindowLong(IntPtr hWnd, WindowLongType index)
	{
		if (IntPtr.Size == 4)
		{
			return GetWindowLong32(hWnd, index);
		}
		return GetWindowLong64(hWnd, index);
	}

	[DllImport("user32.dll", EntryPoint = "GetFocus", CharSet = CharSet.Unicode)]
	public static extern IntPtr GetFocus();

	[DllImport("user32.dll", EntryPoint = "GetWindowLong", CharSet = CharSet.Unicode)]
	private static extern IntPtr GetWindowLong32(IntPtr hwnd, WindowLongType index);

	[DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", CharSet = CharSet.Unicode)]
	private static extern IntPtr GetWindowLong64(IntPtr hwnd, WindowLongType index);

	public static IntPtr SetWindowLong(IntPtr hwnd, WindowLongType index, IntPtr wndProcPtr)
	{
		if (IntPtr.Size == 4)
		{
			return SetWindowLong32(hwnd, index, wndProcPtr);
		}
		return SetWindowLongPtr64(hwnd, index, wndProcPtr);
	}

	[DllImport("user32.dll", EntryPoint = "SetParent", CharSet = CharSet.Unicode)]
	public static extern IntPtr SetParent(IntPtr hWnd, IntPtr hWndParent);

	[DllImport("user32.dll", EntryPoint = "SetWindowLong", CharSet = CharSet.Unicode)]
	private static extern IntPtr SetWindowLong32(IntPtr hwnd, WindowLongType index, IntPtr wndProc);


	public static bool ShowWindow(IntPtr hWnd, bool windowVisible)
	{
		return ShowWindow(hWnd, windowVisible ? 1 : 0);
	}

	[DllImport("user32.dll", EntryPoint = "ShowWindow", CharSet = CharSet.Unicode)]
	private static extern bool ShowWindow(IntPtr hWnd, int mCmdShow);

	[DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", CharSet = CharSet.Unicode)]
	private static extern IntPtr SetWindowLongPtr64(IntPtr hwnd, WindowLongType index, IntPtr wndProc);

	[DllImport("user32.dll", EntryPoint = "CallWindowProc", CharSet = CharSet.Unicode)]
	public static extern IntPtr CallWindowProc(IntPtr wndProc, IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);

	[DllImport("user32.dll", EntryPoint = "GetClientRect")]
	public static extern bool GetClientRect(IntPtr hWnd, out RawRectangle lpRect);

	[DllImport("kernel32.dll", EntryPoint = "GetModuleHandle", CharSet = CharSet.Unicode)]
	public static extern IntPtr GetModuleHandle(string lpModuleName);
}