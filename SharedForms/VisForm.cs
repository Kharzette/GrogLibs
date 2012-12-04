using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;


namespace SharedForms
{
	public partial class VisForm : Form
	{
		OpenFileDialog			mOFD	=new OpenFileDialog();
		SaveFileDialog			mSFD	=new SaveFileDialog();


		public event EventHandler	eVis;
		public event EventHandler	eResumeVis;
		public event EventHandler	eStopVis;
		public event EventHandler	eQueryVisFarm;
		public event EventHandler	eReloadVisFarm;


		public VisForm() : base()
		{
			InitializeComponent();
		}

		public bool bDistributed
		{
			get { return Distributed.Checked; }
			set
			{
				Action<CheckBox>	ac	=a => a.Checked = value;
				SharedForms.FormExtensions.Invoke(Distributed, ac);
			}
		}

		public bool bRough
		{
			get { return RoughVis.Checked; }
			set
			{
				Action<CheckBox>	ac	=a => a.Checked = value;
				SharedForms.FormExtensions.Invoke(RoughVis, ac);
			}
		}

		public bool bSortPortals
		{
			get { return SortPortals.Checked; }
			set
			{
				Action<CheckBox>	ac	=a => a.Checked = value;
				SharedForms.FormExtensions.Invoke(SortPortals, ac);
			}
		}


		public void EnableFileIO(bool bOn)
		{
			Action<GroupBox>	enable	=but => but.Enabled = bOn;
			SharedForms.FormExtensions.Invoke(GroupFileIO, enable);
		}


		void OnVisGBSP(object sender, EventArgs e)
		{
			mOFD.DefaultExt	="*.gbsp";
			mOFD.Filter		="GBSP files (*.gbsp)|*.gbsp|All files (*.*)|*.*";

			DialogResult	dr	=mOFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			BSPCore.CoreEvents.Print("Vising gbsp " + mOFD.FileName + "\n");

			UtilityLib.Misc.SafeInvoke(eVis, mOFD.FileName);
		}


		void OnResumeVis(object sender, EventArgs e)
		{
			mOFD.DefaultExt	="*.gbsp";
			mOFD.Filter		="GBSP files (*.gbsp)|*.gbsp|All files (*.*)|*.*";

			DialogResult	dr	=mOFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			BSPCore.CoreEvents.Print("Resuming gbsp " + mOFD.FileName + "\n");

			UtilityLib.Misc.SafeInvoke(eResumeVis, mOFD.FileName);
		}


		void OnStopVis(object sender, EventArgs e)
		{
			UtilityLib.Misc.SafeInvoke(eStopVis, null);
		}


		void OnQueryVisFarm(object sender, EventArgs e)
		{
			UtilityLib.Misc.SafeInvoke(eQueryVisFarm, null);
		}


		void OnReLoadVisFarm(object sender, EventArgs e)
		{
			UtilityLib.Misc.SafeInvoke(eReloadVisFarm, null);
		}


		void OnDistChecked(object sender, EventArgs e)
		{
			//some of these donut work with each other
			if(Distributed.Checked)
			{
				RoughVis.Checked	=false;
				SortPortals.Checked	=false;
				RoughVis.Enabled	=false;
				SortPortals.Enabled	=false;
				VisGBSP.Enabled		=false;
				ResumeVis.Enabled	=true;
			}
			else
			{
				RoughVis.Enabled	=true;
				SortPortals.Enabled	=true;
				VisGBSP.Enabled		=true;
				ResumeVis.Enabled	=false;
			}
		}
	}
}
