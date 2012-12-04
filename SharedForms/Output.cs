using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using BSPCore;


namespace SharedForms
{
	public partial class Output : Form
	{
		public Output() : base()
		{
			InitializeComponent();

			CoreEvents.ePrint					+=OnPrint;
			ProgressWatcher.eProgressUpdated	+=OnProgress;
		}


		void OnPrint(object sender, EventArgs ea)
		{
			string	toPrint	=sender as string;
			if(toPrint == null)
			{
				return;
			}

			Action<TextBox>	ta	=con => con.AppendText(toPrint);
			SharedForms.FormExtensions.Invoke(ConsoleOut, ta);
		}


		void OnProgress(object sender, EventArgs ea)
		{
			ProgressEventArgs	pea	=ea as ProgressEventArgs;

			UpdateProgress(pea.mMin, pea.mMax, pea.mCurrent);
		}


		public void Print(string toPrint)
		{
			OnPrint(toPrint, null);
		}


		public void UpdateProgress(int min, int max, int cur)
		{
			if(cur < min)
			{
				cur	=min;
			}
			else if(cur > max)
			{
				cur	=max;
			}

			Action<ProgressBar>	pmin	=prog => prog.Minimum = min;
			Action<ProgressBar>	pmax	=prog => prog.Maximum = max;
			Action<ProgressBar>	pcur	=prog => prog.Value = cur;

			SharedForms.FormExtensions.Invoke(BuildProgress, pmin);
			SharedForms.FormExtensions.Invoke(BuildProgress, pmax);
			SharedForms.FormExtensions.Invoke(BuildProgress, pcur);
		}
	}
}
