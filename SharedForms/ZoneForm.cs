using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SharedForms
{
	public partial class ZoneForm : Form
	{
		OpenFileDialog	mOFD	=new OpenFileDialog();
		SaveFileDialog	mSFD	=new SaveFileDialog();

		public event EventHandler	eGenerateMaterials;
		public event EventHandler	eZoneGBSP;
		public event EventHandler	eSaveZone;
		public event EventHandler	eSaveEmissives;
		public event EventHandler	eMaterialVis;
		public event EventHandler	eLoadDebug;


		public ZoneForm()
		{
			InitializeComponent();
		}


		public bool SaveDebugInfo
		{
			get { return SaveDebug.Checked; }
		}


		public void EnableFileIO(bool bOn)
		{
			Action<GroupBox>	enable	=but => but.Enabled = bOn;
			SharedForms.FormExtensions.Invoke(GroupFileIO, enable);
		}


		public void SetZoneSaveEnabled(bool bOn)
		{
			Action<Button>	enable	=but => but.Enabled = bOn;
			SharedForms.FormExtensions.Invoke(SaveZone, enable);
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


		void OnMaterialVis(object sender, EventArgs e)
		{
			mOFD.DefaultExt	="*.gbsp";
			DialogResult	dr	=mOFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			BSPCore.CoreEvents.Print("Material vising " + mOFD.FileName + "\n");

			UtilityLib.Misc.SafeInvoke(eMaterialVis, mOFD.FileName);
		}


		void OnZone(object sender, EventArgs e)
		{
			mOFD.DefaultExt	="*.gbsp";
			DialogResult	dr	=mOFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			BSPCore.CoreEvents.Print("Zoning " + mOFD.FileName + "\n");

			UtilityLib.Misc.SafeInvoke(eZoneGBSP, mOFD.FileName);
		}


		void OnSaveZone(object sender, EventArgs e)
		{
			//google was useless for the "file is not valid" problem
			mSFD.CheckFileExists	=false;
			mSFD.CreatePrompt		=false;
			mSFD.ValidateNames		=false;
			mSFD.DefaultExt			="*.Zone";
			mSFD.Filter				="Zone files (*.Zone)|*.Zone|All files (*.*)|*.*";

			DialogResult	dr	=mSFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			BSPCore.CoreEvents.Print("Saving Zone " + mSFD.FileName + "\n");

			UtilityLib.Misc.SafeInvoke(eSaveZone, mSFD.FileName);
		}


		public int GetLightAtlasSize()
		{
			return	(int)AtlasSize.Value;
		}


		void OnSaveEmissives(object sender, EventArgs e)
		{
			DialogResult	dr	=mSFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			BSPCore.CoreEvents.Print("Saving Emissives " + mSFD.FileName + "\n");

			UtilityLib.Misc.SafeInvoke(eSaveEmissives, mSFD.FileName);
		}


		void OnLoadDebug(object sender, EventArgs e)
		{
			mOFD.DefaultExt	="*.Portals";
			DialogResult	dr	=mOFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			BSPCore.CoreEvents.Print("Loading debug file " + mSFD.FileName + "\n");

			UtilityLib.Misc.SafeInvoke(eLoadDebug, mOFD.FileName);
		}
	}
}
