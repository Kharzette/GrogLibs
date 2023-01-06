namespace SharpDX.Windows;

public class MessageEventArgs : EventArgs
{
	public IntPtr	mWParam, mLParam;

	public MessageEventArgs(IntPtr wParam, IntPtr lParam)
	{
		mWParam	=wParam;
		mLParam	=lParam;
	}
}