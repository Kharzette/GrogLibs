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
		AnimGrid	mAnimGrid;

		//file dialog
		OpenFileDialog	mOFD	=new OpenFileDialog();

		//events
		public event	EventHandler	eOpenAnim;
		public event	EventHandler	eOpenModel;
		public event	EventHandler	eAnimSelectionChanged;
		public event	EventHandler	eTimeScaleChanged;

		public AnimForm()
		{
			InitializeComponent();

			Collada.eAnimsUpdated	+=OnAnimsUpdated;
		}

		private void LoadAnim_Click(object sender, EventArgs e)
		{
			DialogResult	dr	=mOFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			eOpenAnim(mOFD.FileName, null);
		}

		private void LoadModel_Click(object sender, EventArgs e)
		{
			DialogResult	dr	=mOFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			eOpenModel(mOFD.FileName, null);
		}


		private void OnAnimsUpdated(object sender, EventArgs e)
		{
			List<Character.Anim>	anms	=(List<Character.Anim>)sender;

			mAnimGrid	=new AnimGrid(anms);

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
	}
}
