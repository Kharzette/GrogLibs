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

		//build params
		BSPBuildParams	mBSPParams		=new BSPBuildParams();
		LightParams		mLightParams	=new LightParams();
		VisParams		mVisParams		=new VisParams();

		public event EventHandler	eOpenBrushFile;
		public event EventHandler	eBuildGBSP;
		public event EventHandler	eLightGBSP;
		public event EventHandler	eLoadGBSP;
		public event EventHandler	eVisGBSP;
		public event EventHandler	eGenerateMaterials;
		public event EventHandler	eSaveGBSP;
		public event EventHandler	eSaveZone;
		public event EventHandler	eDrawChoiceChanged;
		public event EventHandler	eQueryBuildFarm;


		public MainForm()
		{
			InitializeComponent();

			int	coreCount	=0;
			foreach(var item in new System.Management.ManagementObjectSearcher("Select * from Win32_Processor").Get())
			{
				coreCount	+=int.Parse(item["NumberOfCores"].ToString());
			}
			MaxCPUCores.Maximum	=coreCount;
			MaxCPUCores.Minimum	=1;
			MaxCPUCores.Value	=coreCount;
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


		internal string NumberOfPlanes
		{
			get { return NumPlanes.Text; }
			set { SetTextBoxValue(NumPlanes, value); }
		}

		internal string NumberOfPortals
		{
			get { return NumPortals.Text; }
			set { SetTextBoxValue(NumPortals, value); }
		}

		internal string NumberOfVerts
		{
			get { return NumVerts.Text; }
			set { SetTextBoxValue(NumVerts, value); }
		}

		internal string NumberOfClusters
		{
			get { return NumClusters.Text; }
			set { SetTextBoxValue(NumClusters, value); }
		}

		internal bool DrawNWork
		{
			get { return DrawAndWork.Checked; }
			set { DrawAndWork.Checked = value; }
		}

		internal BSPBuildParams BSPParameters
		{
			get
			{
				mBSPParams.mbVerbose		=VerboseBSP.Checked;
				mBSPParams.mbEntityVerbose	=VerboseEntity.Checked;
				mBSPParams.mMaxCores		=(int)MaxCPUCores.Value;
				mBSPParams.mbFixTJunctions	=FixTJunctions.Checked;

				return	mBSPParams;
			}
			set { }	//donut allow settery
		}

		internal LightParams LightParameters
		{
			get
			{
				mLightParams.mbSeamCorrection	=SeamCorrection.Checked;
				mLightParams.mbRadiosity		=Radiosity.Checked;
				mLightParams.mbFastPatch		=FastPatch.Checked;
				mLightParams.mPatchSize			=(int)PatchSize.Value;
				mLightParams.mNumBounces		=(int)NumBounce.Value;
				mLightParams.mLightScale		=(float)LightScale.Value;
				mLightParams.mMinLight.X		=(float)MinLightX.Value;
				mLightParams.mMinLight.Y		=(float)MinLightY.Value;
				mLightParams.mMinLight.Z		=(float)MinLightZ.Value;
				mLightParams.mSurfaceReflect	=(float)ReflectiveScale.Value;
				mLightParams.mMaxIntensity		=(int)MaxIntensity.Value;
				mLightParams.mLightGridSize		=(int)LightGridSize.Value;
				mLightParams.mAtlasSize			=(int)AtlasSize.Value;

				return	mLightParams;
			}
			set { }	//donut allow settery
		}

		internal VisParams VisParameters
		{
			get
			{
				mVisParams.mbFullVis		=FullVis.Checked;
				mVisParams.mbSortPortals	=SortPortals.Checked;
				mVisParams.mbDistribute		=DistributeVis.Checked;
				mVisParams.mNumRetries		=(int)NumRetries.Value;
				mVisParams.mGranularity		=(int)VisGranularity.Value;

				return	mVisParams;
			}
			set { }	//donut allow settery
		}


		internal void PrintToConsole(string text)
		{
			AppendTextBox(ConsoleOut, text);
		}


		void OnOpenBrushFile(object sender, EventArgs e)
		{			
			mOFD.DefaultExt	="*.vmf";
			DialogResult	dr	=mOFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			UtilityLib.Misc.SafeInvoke(eOpenBrushFile, mOFD.FileName);
		}


		void OnLightGBSP(object sender, EventArgs e)
		{
			mOFD.DefaultExt	="*.gbsp";
			DialogResult	dr	=mOFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			UtilityLib.Misc.SafeInvoke(eLightGBSP, mOFD.FileName);
		}


		void OnDrawChoiceChanged(object sender, EventArgs e)
		{
			UtilityLib.Misc.SafeInvoke(eDrawChoiceChanged, DrawChoice.SelectedItem);
		}


		void OnBuildGBSP(object sender, EventArgs e)
		{
			UtilityLib.Misc.SafeInvoke(eBuildGBSP, null);
		}


		void OnSaveGBSP(object sender, EventArgs e)
		{
			DialogResult	dr	=mSFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			UtilityLib.Misc.SafeInvoke(eSaveGBSP, mSFD.FileName);
		}


		void OnGenerateMaterials(object sender, EventArgs e)
		{
			mOFD.DefaultExt	="*.gbsp";
			DialogResult	dr	=mOFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			UtilityLib.Misc.SafeInvoke(eGenerateMaterials, mOFD.FileName);
		}


		void OnVisGBSP(object sender, EventArgs e)
		{
			mOFD.DefaultExt	="*.gbsp";
			DialogResult	dr	=mOFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			UtilityLib.Misc.SafeInvoke(eVisGBSP, mOFD.FileName);
		}


		internal void SetBuildEnabled(bool bOn)
		{
			EnableControl(BuildGBSP, bOn);
		}


		internal void SetSaveEnabled(bool bOn)
		{
			EnableControl(SaveGBSP, bOn);
		}


		void OnLoadGBSP(object sender, EventArgs e)
		{
			mOFD.DefaultExt	="*.gbsp";
			DialogResult	dr	=mOFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			UtilityLib.Misc.SafeInvoke(eLoadGBSP, mOFD.FileName);
		}


		void OnFullVisChanged(object sender, EventArgs e)
		{
			DistributeVis.Enabled	=FullVis.Checked;

			if(FullVis.Checked)
			{
				SortPortals.Checked	=true;
				SortPortals.Enabled	=false;

				NumRetries.Enabled		=DistributeVis.Checked;
				VisGranularity.Enabled	=DistributeVis.Checked;
			}
			else
			{
				SortPortals.Enabled	=true;
			}
		}


		void OnRadiosityChanged(object sender, EventArgs e)
		{
			FastPatch.Enabled		=Radiosity.Checked;
			PatchSize.Enabled		=Radiosity.Checked;
			ReflectiveScale.Enabled	=Radiosity.Checked;
//			NumBounce.Enabled		=Radiosity.Checked;
		}


		void OnSaveZone(object sender, EventArgs e)
		{
			DialogResult	dr	=mSFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			UtilityLib.Misc.SafeInvoke(eSaveZone, mSFD.FileName);
		}


		internal void SetZoneSaveEnabled(bool bOn)
		{
			EnableControl(SaveZone, bOn);
		}


		delegate void EnableControlCB(Control c, bool bOn);

		void EnableControl(Control control, bool bOn)
		{
			if(control.InvokeRequired)
			{
				EnableControlCB	enable	=delegate(Control c, bool bEn) { c.Enabled = bEn; };

				object	[]pms	=new object[2];

				pms[0]	=control;
				pms[1]	=bOn;

				control.Invoke(enable, pms);
			}
			else
			{
				control.Enabled	=bOn;
			}
		}

		internal void EnableFileIO(bool bOn)
		{
			EnableControl(GroupFileIO, bOn);
		}


		internal void UpdateProgress(ProgressEventArgs pea)
		{
			UpdateProgressBar(Progress1, pea.mMin, pea.mMax, pea.mCurrent);
		}


		internal void ClearProgress()
		{
			UpdateProgressBar(Progress1, 0, 0, 0);
		}


		void OnQueryBuildFarm(object sender, EventArgs e)
		{
			UtilityLib.Misc.SafeInvoke(eQueryBuildFarm, null);
		}


		void OnDistributeChanged(object sender, EventArgs e)
		{
			VisGranularity.Enabled	=DistributeVis.Checked;
			NumRetries.Enabled		=DistributeVis.Checked;
		}


		internal void EnableVisGroup(bool bOn)
		{
			EnableControl(VisGroup, bOn);
		}
	}
}
