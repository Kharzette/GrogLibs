using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BSPLib;


namespace BSPBuilder
{
	public partial class MainForm : Form
	{
		OpenFileDialog			mOFD	=new OpenFileDialog();
		SaveFileDialog			mSFD	=new SaveFileDialog();

		public event EventHandler	eOpenVMF;
		public event EventHandler	eOpenMap;
		public event EventHandler	eOpenZone;
		public event EventHandler	eSaveZone;
		public event EventHandler	eDrawChoiceChanged;


		public MainForm()
		{
			InitializeComponent();

			MaxCPUCores.Maximum	=Environment.ProcessorCount;
			MaxCPUCores.Minimum	=1;
			MaxCPUCores.Value	=Environment.ProcessorCount;

			ProgressWatcher.eProgressUpdated	+=OnProgressUpdated;
		}


		delegate void SetTextDel(TextBox tb, string txt);

		void SetTextBoxValue(TextBox tbox, string str)
		{
			if(tbox.InvokeRequired)
			{
				SetTextDel	setText	=delegate(TextBox tb, string txt) {	tb.Text = txt; };

				object	[]pms	=new object[2];

				pms[0]	=tbox;
				pms[1]	=str;

				tbox.Invoke(setText, pms);
			}
			else
			{
				tbox.Text	=str;
			}
		}


		delegate void AppendTextDel(TextBox tb, string txt);

		void AppendTextBox(TextBox tbox, string str)
		{
			if(tbox.InvokeRequired)
			{
				AppendTextDel	appText	=delegate(TextBox tb, string txt) { tb.AppendText(txt); };

				object	[]pms	=new object[2];

				pms[0]	=tbox;
				pms[1]	=str;

				tbox.Invoke(appText, pms);
			}
			else
			{
				tbox.AppendText(str);
			}
		}


		delegate void UpdateProgressBarDel(ProgressBar pb, int min, int max, int cur);

		void UpdateProgressBar(ProgressBar pb, int min, int max, int cur)
		{
			if(pb.InvokeRequired)
			{
				UpdateProgressBarDel	updel	=delegate(ProgressBar prb, int mn, int mx, int cr)
							{ prb.Minimum	=mn; prb.Maximum =mx; prb.Value = cr; };

				object	[]pms	=new object[4];

				pms[0]	=pb;
				pms[1]	=min;
				pms[2]	=max;
				pms[3]	=cur;

				pb.Invoke(updel, pms);
			}
			else
			{
				pb.Minimum	=min;
				pb.Maximum	=max;
				pb.Value	=cur;
			}
		}


		internal bool bBevels
		{
			get { return BevelBrushes.Checked; }
			set { BevelBrushes.Checked = value; }
		}

		internal string NumberOfMapFaces
		{
			get { return NumMapFaces.Text; }
			set { SetTextBoxValue(NumMapFaces, value); }
		}

		internal string NumberOfPortals
		{
			get { return NumPortals.Text; }
			set { SetTextBoxValue(NumPortals, value); }
		}

		internal string NumberOfDrawFaces
		{
			get { return NumDrawFaces.Text; }
			set { SetTextBoxValue(NumDrawFaces, value); }
		}

		internal string NumberOfCollisionFaces
		{
			get { return NumCollisionFaces.Text; }
			set { SetTextBoxValue(NumCollisionFaces, value); }
		}

		internal int MaxNumberOfCPUCores
		{
			get { return (int)MaxCPUCores.Value; }
			set { MaxCPUCores.Value = value; }
		}


		internal void PrintToConsole(string text)
		{
			AppendTextBox(ConsoleOut, text);
		}


		void OnOpenVMF(object sender, EventArgs e)
		{			
			mOFD.DefaultExt	="*.vmf";
			DialogResult	dr	=mOFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			if(eOpenVMF != null)
			{
				eOpenVMF(mOFD.FileName, null);
			}
		}


		void OnOpenZone(object sender, EventArgs e)
		{
			mOFD.DefaultExt	="*.Zone";
			DialogResult	dr	=mOFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			if(eOpenZone != null)
			{
				eOpenZone(mOFD.FileName, null);
			}
		}


		void OnOpenMap(object sender, EventArgs e)
		{
			mOFD.DefaultExt	="*.map";
			DialogResult	dr	=mOFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			if(eOpenMap != null)
			{
				eOpenMap(mOFD.FileName, null);
			}
		}


		void OnSaveZone(object sender, EventArgs e)
		{
			DialogResult	dr	=mSFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			if(eSaveZone != null)
			{
				eSaveZone(mSFD.FileName, null);
			}
		}


		void OnDrawChoiceChanged(object sender, EventArgs e)
		{
			if(eDrawChoiceChanged != null)
			{
				eDrawChoiceChanged(DrawChoice.SelectedItem, null);
			}
		}


		void OnProgressUpdated(object sender, EventArgs ea)
		{
			ProgressEventArgs	pea	=ea as ProgressEventArgs;

			Console.WriteLine("Updating " + pea.mIndex + " with " + pea.mCurrent);

			ProgressBar	pb	=null;
			switch(pea.mIndex)
			{
				case	0:
					pb	=Progress1;
					break;
				case	1:
					pb	=Progress2;
					break;
				case	2:
					pb	=Progress3;
					break;
				case	3:
					pb	=Progress4;
					break;
			}
			if(pb != null)
			{
				UpdateProgressBar(pb, pea.mMin, pea.mMax, pea.mCurrent);
			}
		}
	}
}
