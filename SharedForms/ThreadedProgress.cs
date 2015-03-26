using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;


namespace SharedForms
{
	//use this like the following:
	//Thread	uiThread	=new Thread(() =>
	//	{
	//		tProg	=new ThreadedProgress("Doing Stuff...");
	//		Application.Run(tProg);
	//	});
	//uiThread.SetApartmentState(ApartmentState.STA);
	//uiThread.Start();
	//while(tProg == null)
	//{
	//	Thread.Sleep(0);
	//}
	//tProg.Whatever()
	public partial class ThreadedProgress : Form
	{
		int	mMax;


		public ThreadedProgress(string title)
		{
			InitializeComponent();

			Text	=title;
		}


		public void Nuke()
		{
			Action<Form>	close	=frm => frm.Close();

			FormExtensions.Invoke(this, close);
		}


		public void SetSizeInfo(int min, int max)
		{
			mMax	=max;

			Action<ProgressBar>	pmin	=prog => prog.Minimum	=min;
			Action<ProgressBar>	pmax	=prog => prog.Maximum	=max;
			Action<ProgressBar>	pcur	=prog => prog.Value		=0;
			Action<ProgressBar>	pvis	=prog => prog.Visible	=true;

			FormExtensions.Invoke(Prog, pmin);
			FormExtensions.Invoke(Prog, pmax);
			FormExtensions.Invoke(Prog, pcur);
			FormExtensions.Invoke(Prog, pvis);
		}


		public int GetMax()
		{
			return	mMax;
		}


		public void SetCurrent(int val)
		{
			Action<ProgressBar>	pcur	=prog => prog.Value		=val;
			FormExtensions.Invoke(Prog, pcur);
		}
	}
}
