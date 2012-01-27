using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using BSPCore;
using Microsoft.Xna.Framework;	//xnamathery


namespace SharedForms
{
	public partial class BSPForm : Form
	{
		OpenFileDialog	mOFD	=new OpenFileDialog();
		SaveFileDialog	mSFD	=new SaveFileDialog();

		//build params
		BSPBuildParams	mBSPParams		=new BSPBuildParams();
		LightParams		mLightParams	=new LightParams();

		public event EventHandler	eOpenMap;
		public event EventHandler	eBuild;
		public event EventHandler	eSave;
		public event EventHandler	eLight;


		public string NumberOfPlanes
		{
			get { return NumPlanes.Text; }
			set
			{
				Action<TextBox>	ta	=tb => tb.Text = value;
				SharedForms.FormExtensions.Invoke(NumPlanes, ta);
			}
		}

		public string NumberOfPortals
		{
			get { return NumPortals.Text; }
			set
			{
				Action<TextBox>	ta	=tb => tb.Text = value;
				SharedForms.FormExtensions.Invoke(NumPortals, ta);
			}
		}

		public string NumberOfVerts
		{
			get { return NumVerts.Text; }
			set
			{
				Action<TextBox>	ta	=tb => tb.Text = value;
				SharedForms.FormExtensions.Invoke(NumVerts, ta);
			}
		}

		public string NumberOfClusters
		{
			get { return NumClusters.Text; }
			set
			{
				Action<TextBox>	ta	=tb => tb.Text = value;
				SharedForms.FormExtensions.Invoke(NumClusters, ta);
			}
		}

		public BSPBuildParams BSPParameters
		{
			get
			{
				mBSPParams.mMaxThreads			=(int)MaxThreads.Value;
				mBSPParams.mbVerbose			=VerboseBSP.Checked;
				mBSPParams.mbEntityVerbose		=VerboseEntity.Checked;
				mBSPParams.mbFixTJunctions		=FixTJunctions.Checked;
				mBSPParams.mbSlickAsGouraud		=SlickAsGouraud.Checked;
				mBSPParams.mbWarpAsMirror		=WarpAsMirror.Checked;
				mBSPParams.mbSkyEmitLight		=SkyEmitLight.Checked;
				mBSPParams.mbLavaEmitLight		=LavaEmitLight.Checked;
				mBSPParams.mbWindowEmitLight	=WindowEmitLight.Checked;
				mBSPParams.mbWindowTransparent	=WindowTransparent.Checked;
				mBSPParams.mbTransparentDetail	=TransDetail.Checked;

				return	mBSPParams;
			}
			set { }	//donut allow settery
		}

		public LightParams LightParameters
		{
			get
			{
				mLightParams.mbSeamCorrection		=SeamCorrection.Checked;
				mLightParams.mbSurfaceLighting		=SurfaceLighting.Checked;
				mLightParams.mSurfLightFrequency	=(int)SurfaceLightFrequency.Value;
				mLightParams.mSurfLightStrength		=(int)SurfaceLightStrength.Value;
				mLightParams.mMinLight.X			=(float)MinLightX.Value;
				mLightParams.mMinLight.Y			=(float)MinLightY.Value;
				mLightParams.mMinLight.Z			=(float)MinLightZ.Value;
				mLightParams.mMaxIntensity			=(int)MaxIntensity.Value;
				mLightParams.mLightGridSize			=(int)LightGridSize.Value;
				mLightParams.mNumSamples			=(int)NumSamples.Value;

				return	mLightParams;
			}
			set { }	//donut allow settery
		}


		public BSPForm()
		{
			InitializeComponent();
		}


		void OnOpenMap(object sender, EventArgs e)
		{
			mOFD.DefaultExt	="*.map";
			mOFD.Filter		="Quake map files (*.map)|*.map|Valve map files (*.vmf)|*.vmf|All files (*.*)|*.*";

			DialogResult	dr	=mOFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			CoreEvents.Print("Opening map " + mOFD.FileName + "\n");

			UtilityLib.Misc.SafeInvoke(eOpenMap, mOFD.FileName);
		}


		public void SetBuildEnabled(bool bOn)
		{
			Action<Button>	enable	=but => but.Enabled = bOn;
			SharedForms.FormExtensions.Invoke(BuildGBSP, enable);
		}


		public void SetSaveEnabled(bool bOn)
		{
			Action<Button>	enable	=but => but.Enabled = bOn;
			SharedForms.FormExtensions.Invoke(SaveGBSP, enable);
		}


		public void EnableFileIO(bool bOn)
		{
			Action<GroupBox>	enable	=but => but.Enabled = bOn;
			SharedForms.FormExtensions.Invoke(GroupFileIO, enable);
		}


		void OnLightGBSP(object sender, EventArgs e)
		{
			mOFD.DefaultExt	="*.gbsp";
			mOFD.Filter		="GBSP files (*.gbsp)|*.gbsp|All files (*.*)|*.*";

			DialogResult	dr	=mOFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			CoreEvents.Print("Lighting gbsp " + mOFD.FileName + "\n");

			UtilityLib.Misc.SafeInvoke(eLight, mOFD.FileName);
		}


		void OnBuildGBSP(object sender, EventArgs e)
		{
			UtilityLib.Misc.SafeInvoke(eBuild, null);
		}


		void OnSaveGBSP(object sender, EventArgs e)
		{
			mSFD.DefaultExt	="*.gbsp";
			mSFD.Filter		="GBSP files (*.gbsp)|*.gbsp|All files (*.*)|*.*";

			DialogResult	dr	=mSFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			CoreEvents.Print("Saving gbsp " + mSFD.FileName + "\n");

			UtilityLib.Misc.SafeInvoke(eSave, mSFD.FileName);
		}


		void OnVerbose(object sender, EventArgs e)
		{
			if(VerboseBSP.Checked)
			{
				CoreEvents.Print("Note that verbosity can adversely affect performance, especially in vis.\n");
			}
		}


		void OnSurfaceLighting(object sender, EventArgs e)
		{
			SurfaceLightFrequency.Enabled	=SurfaceLighting.Checked;
			SurfaceLightStrength.Enabled	=SurfaceLighting.Checked;
		}
	}
}
