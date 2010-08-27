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
		public event EventHandler	eEntityIndChanged;


		public MainForm()
		{
			InitializeComponent();
		}


		void OnOpenVMF(object sender, EventArgs e)
		{			
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


		public bool bBevels
		{
			get { return BevelCorners.Checked; }
			set { BevelCorners.Checked = value; }
		}

		public decimal BevelHullSize
		{
			get { return HullSize.Value; }
			set { HullSize.Value = value; }
		}

		public string NumberOfPortals
		{
			get { return NumPortals.Text; }
			set { NumPortals.Text = value; }
		}

		public int EntityInd
		{
			get { return (int)EntityIndex.Value; }
			set { EntityIndex.Value = (decimal)value; }
		}


		private void OnEntityIndexChanged(object sender, EventArgs e)
		{
			if(eEntityIndChanged != null)
			{
				eEntityIndChanged(EntityInd, null);
			}
		}
	}
}
