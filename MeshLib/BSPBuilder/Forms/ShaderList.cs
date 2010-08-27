using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Xna.Framework.Graphics;


namespace BSPBuilder
{
	public partial class ShaderList : Form
	{
		MaterialLib.MaterialLib	mMatLib;

		BindingList<ShaderGridModel>	mShaders	=new BindingList<ShaderGridModel>();

		public event EventHandler	eOk;
		public event EventHandler	eCancel;


		public ShaderList(MaterialLib.MaterialLib matlib)
		{
			InitializeComponent();

			mMatLib	=matlib;

			Dictionary<string, Effect>	shaders
				=mMatLib.GetShaders();

			foreach(KeyValuePair<string, Effect> fx in shaders)
			{
				ShaderGridModel	sgm	=new ShaderGridModel();
				sgm.Name	=fx.Key;
				mShaders.Add(sgm);
			}
			ShaderGrid.DataSource	=mShaders;
		}

		private void OnSelectionChanged(object sender, EventArgs e)
		{
			DataGridViewSelectedRowCollection
				sel	=ShaderGrid.SelectedRows;

			if(sel.Count > 0)
			{
				Ok.Enabled	=true;
			}
			else
			{
				Ok.Enabled	=false;
			}
		}

		private void OnOk(object sender, EventArgs e)
		{
			Debug.Assert(ShaderGrid.CurrentRow != null);

			eOk((string)ShaderGrid.CurrentRow.Cells[0].FormattedValue, null);

			Close();
		}

		private void OnCancel(object sender, EventArgs e)
		{
			eCancel(null, null);
			Close();
		}
	}
}
