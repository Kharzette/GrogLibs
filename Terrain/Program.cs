using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Surface.Core;
using Microsoft.Xna.Framework;

namespace Terrain
{
	static class Program
	{
		// Hold on to the game window.
		static GameWindow Window;

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
			using(Terrain game = new Terrain(args))
			{
				game.Run();
			}
		}
        /// <summary>
        /// Sets the window style for the specified HWND to None.
        /// </summary>
        /// <param name="hWnd">the handle of the window</param>
        internal static void RemoveBorder(IntPtr hWnd)
        {
            Form form = (Form)Form.FromHandle(hWnd);
            form.FormBorderStyle = FormBorderStyle.None;
        }

		/// <summary>
		/// Moves the upper left corner of the specified HWND to the
		/// specified location.
		/// </summary>
		/// <param name="hWnd">the game window</param>
		/// <param name="left">the desired location for the left edge of the window</param>
		/// <param name="top">the desired location for the top edge of the window</param>
		internal static void PositionWindow(GameWindow window, int left, int top)
		{
			if(window == null)
				throw new ArgumentNullException("window");

			if(Window != null)
			{
				Window.ClientSizeChanged -= new EventHandler(OnSetWindowPosition);
				Window.ScreenDeviceNameChanged -= new EventHandler(OnSetWindowPosition);
			}

			Window = window;

			Window.ClientSizeChanged += new EventHandler(OnSetWindowPosition);
			Window.ScreenDeviceNameChanged += new EventHandler(OnSetWindowPosition);

			UpdateWindowPosition();
		}

		/// <summary>
		/// When the ScreenDeviceChanges or the ClientSizeChanges update the Windows Position.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void OnSetWindowPosition(object sender, EventArgs e)
		{
			UpdateWindowPosition();
		}

		/// <summary>
		/// Use the Desktop bounds to update the the position of the Window correctly.
		/// </summary>
		private static void UpdateWindowPosition()
		{
			IntPtr hWnd = Window.Handle;
			Form form = (Form)Form.FromHandle(hWnd);
			form.SetDesktopLocation(InteractiveSurface.DefaultInteractiveSurface.Left - (Window.ClientBounds.Left - form.DesktopBounds.Left),
									InteractiveSurface.DefaultInteractiveSurface.Top - (Window.ClientBounds.Top - form.DesktopBounds.Top));
		}
	}
}

