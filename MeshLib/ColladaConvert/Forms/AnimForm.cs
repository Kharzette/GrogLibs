using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

namespace ColladaConvert
{
	public partial class AnimForm : Form
	{
		//data
		AnimGridModel	mAnimGrid;
		MeshLib.AnimLib	mAnimLib;

		//file dialog
		OpenFileDialog	mOFD	=new OpenFileDialog();
		SaveFileDialog	mSFD	=new SaveFileDialog();

		//events
		public event	EventHandler	eLoadAnim;
		public event	EventHandler	eLoadModel;
		public event	EventHandler	eLoadStaticModel;
		public event	EventHandler	eAnimSelectionChanged;
		public event	EventHandler	eTimeScaleChanged;
		public event	EventHandler	eSaveLibrary;
		public event	EventHandler	eSaveCharacter;
		public event	EventHandler	eLoadCharacter;
		public event	EventHandler	eLoadLibrary;
		public event	EventHandler	eSaveStatic;
		public event	EventHandler	eLoadStatic;


		public AnimForm(MeshLib.AnimLib anlib)
		{
			InitializeComponent();

			mAnimLib	=anlib;

			ColladaConvert.eAnimsUpdated	+=OnAnimsUpdated;
		}


		private void OnLoadModel(object sender, EventArgs e)
		{
			mOFD.Multiselect	=false;
			DialogResult	dr	=mOFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			eLoadModel(mOFD.FileName, null);
		}


		private void OnAnimsUpdated(object sender, EventArgs e)
		{
			List<MeshLib.Anim>	anms	=mAnimLib.GetAnims();

			mAnimGrid	=new AnimGridModel(anms);

			AnimGrid.DataSource	=mAnimGrid;
		}


		private void AnimGrid_SelectionChanged(object sender, EventArgs e)
		{
			DataGridViewSelectedRowCollection	row	=AnimGrid.SelectedRows;

			if(eAnimSelectionChanged != null)
			{
				eAnimSelectionChanged(row, null);
			}
		}

		private void TimeScale_ValueChanged(object sender, EventArgs e)
		{
			eTimeScaleChanged(TimeScale.Value, null);
		}


		private void OnSaveLibrary(object sender, EventArgs e)
		{
			DialogResult	dr	=mSFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			eSaveLibrary(mSFD.FileName, null);
		}


		private void OnLoadLibrary(object sender, EventArgs e)
		{
			mOFD.Multiselect	=false;
			DialogResult	dr	=mOFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			eLoadLibrary(mOFD.FileName, null);
		}


		private void OnSaveCharacter(object sender, EventArgs e)
		{
			DialogResult	dr	=mSFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			eSaveCharacter(mSFD.FileName, null);
		}


		private void OnLoadCharacter(object sender, EventArgs e)
		{
			mOFD.Multiselect	=false;
			DialogResult	dr	=mOFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			eLoadCharacter(mOFD.FileName, null);
		}

		private void OnCellValidated(object sender, DataGridViewCellEventArgs e)
		{
			//update name?
			if(e.ColumnIndex == 0)
			{
				mAnimLib.UpdateDictionaries();
			}
		}


		private void OnRowNuking(object sender, DataGridViewRowCancelEventArgs e)
		{
			MeshLib.Anim	nukeMe	=(MeshLib.Anim)e.Row.DataBoundItem;
			mAnimLib.NukeAnim(nukeMe.Name);
		}


		private void OnClearAll(object sender, EventArgs e)
		{
			mAnimLib.NukeAll();

			List<MeshLib.Anim>	anms	=mAnimLib.GetAnims();

			mAnimGrid	=new AnimGridModel(anms);

			AnimGrid.DataSource	=mAnimGrid;
		}


		private void OnCompress(object sender, EventArgs e)
		{
			if(AnimGrid.SelectedRows.Count <= 0)
			{
				return;
			}
			mAnimLib.Reduce(
				Convert.ToString(AnimGrid.SelectedRows[0].Cells[0].Value),
				Convert.ToSingle(MaxError.Value));
		}


		private void OnLoadStaticModel(object sender, EventArgs e)
		{
			mOFD.Multiselect	=false;
			DialogResult	dr	=mOFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			eLoadStaticModel(mOFD.FileName, null);
		}


		private void OnLoadAnim(object sender, EventArgs e)
		{
			mOFD.Multiselect	=true;
			DialogResult	dr	=mOFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			string	[]fnames	=mOFD.FileNames;

			foreach(string fname in fnames)
			{
				eLoadAnim(fname, null);
			}
		}


		private void OnSaveStatic(object sender, EventArgs e)
		{
			DialogResult	dr	=mSFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			eSaveStatic(mSFD.FileName, null);
		}


		private void OnLoadStatic(object sender, EventArgs e)
		{
			mOFD.Multiselect	=true;
			DialogResult	dr	=mOFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			string	[]fnames	=mOFD.FileNames;

			foreach(string fname in fnames)
			{
				eLoadStatic(fname, null);
			}
		}
	}
}
