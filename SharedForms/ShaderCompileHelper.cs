using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;


namespace SharedForms;

public static class ShaderCompileHelper
{
	public static ThreadedProgress	mTProg;
	public static string			mTitle;

	public static EventHandler	CompileNeededHandler	=new EventHandler(
		delegate(object sender, EventArgs ea)
		{
			Thread	uiThread	=new Thread(() =>
				{
					mTProg	=new ThreadedProgress(mTitle);
					Application.Run(mTProg);
				});

			uiThread.SetApartmentState(ApartmentState.STA);
			uiThread.Start();

			while(mTProg == null)
			{
				Thread.Sleep(0);
			}

			mTProg.SetSizeInfo(0, (int)sender);
		});

	public static EventHandler	CompileDoneHandler	=new EventHandler(
		delegate(object sender, EventArgs ea)
		{
			mTProg.SetCurrent((int)sender);
			if((int)sender == mTProg.GetMax())
			{
				mTProg.Nuke();
			}
		});
}