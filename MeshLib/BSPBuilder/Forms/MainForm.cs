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


		public MainForm()
		{
			InitializeComponent();

			MaxCPUCores.Maximum	=Environment.ProcessorCount * 2;
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

			if(eOpenBrushFile != null)
			{
				eOpenBrushFile(mOFD.FileName, null);
			}
		}


		void OnLightGBSP(object sender, EventArgs e)
		{
			mOFD.DefaultExt	="*.gbsp";
			DialogResult	dr	=mOFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			if(eLightGBSP != null)
			{
				eLightGBSP(mOFD.FileName, null);
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


		void OnBuildGBSP(object sender, EventArgs e)
		{
			if(eBuildGBSP != null)
			{
				eBuildGBSP(null, null);
			}
		}


		void OnSaveGBSP(object sender, EventArgs e)
		{
			DialogResult	dr	=mSFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			if(eSaveGBSP != null)
			{
				eSaveGBSP(mSFD.FileName, null);
			}
		}


		private void OnGenerateMaterials(object sender, EventArgs e)
		{
			mOFD.DefaultExt	="*.gbsp";
			DialogResult	dr	=mOFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			if(eGenerateMaterials != null)
			{
				eGenerateMaterials(mOFD.FileName, null);
			}
		}


		void OnVisGBSP(object sender, EventArgs e)
		{
			mOFD.DefaultExt	="*.gbsp";
			DialogResult	dr	=mOFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			if(eVisGBSP != null)
			{
				eVisGBSP(mOFD.FileName, null);
			}
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

			if(eLoadGBSP != null)
			{
				eLoadGBSP(mOFD.FileName, null);
			}
		}


		void OnFullVisChanged(object sender, EventArgs e)
		{
			if(FullVis.Checked)
			{
				SortPortals.Checked	=true;
				SortPortals.Enabled	=false;
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

			if(eSaveZone != null)
			{
				eSaveZone(mSFD.FileName, null);
			}
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
			if(pea.mIndex == 0)
			{
				UpdateProgressBar(Progress1, pea.mMin, pea.mMax, pea.mCurrent);
			}
			else if(pea.mIndex == 1)
			{
				UpdateProgressBar(Progress2, pea.mMin, pea.mMax, pea.mCurrent);
			}
			else if(pea.mIndex == 2)
			{
				UpdateProgressBar(Progress3, pea.mMin, pea.mMax, pea.mCurrent);
			}
			else if(pea.mIndex == 3)
			{
				UpdateProgressBar(Progress4, pea.mMin, pea.mMax, pea.mCurrent);
			}
		}


		internal void ClearProgress()
		{
			UpdateProgressBar(Progress1, 0, 0, 0);
			UpdateProgressBar(Progress2, 0, 0, 0);
			UpdateProgressBar(Progress3, 0, 0, 0);
			UpdateProgressBar(Progress4, 0, 0, 0);
		}
	}
}
