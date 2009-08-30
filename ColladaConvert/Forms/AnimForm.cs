using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ColladaConvert
{
	public partial class AnimForm : Form
	{
		//data
		AnimGridModel		mAnimGrid;
		Character.AnimLib	mAnimLib;

		//file dialog
		OpenFileDialog	mOFD	=new OpenFileDialog();
		SaveFileDialog	mSFD	=new SaveFileDialog();

		//events
		public event	EventHandler	eLoadAnim;
		public event	EventHandler	eLoadModel;
		public event	EventHandler	eAnimSelectionChanged;
		public event	EventHandler	eTimeScaleChanged;
		public event	EventHandler	eSaveLibrary;
		public event	EventHandler	eSaveCharacter;
		public event	EventHandler	eLoadCharacter;

		public AnimForm(Character.AnimLib anlib)
		{
			InitializeComponent();

			mAnimLib	=anlib;

			ColladaConvert.eAnimsUpdated	+=OnAnimsUpdated;
		}

		private void LoadAnim_Click(object sender, EventArgs e)
		{
			DialogResult	dr	=mOFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			eLoadAnim(mOFD.FileName, null);
		}

		private void LoadModel_Click(object sender, EventArgs e)
		{
			DialogResult	dr	=mOFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			eLoadModel(mOFD.FileName, null);
		}


		private void OnAnimsUpdated(object sender, EventArgs e)
		{
			List<Character.Anim>	anms	=(List<Character.Anim>)sender;

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
			DialogResult	dr	=mOFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			eLoadModel(mOFD.FileName, null);
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
	}
}
