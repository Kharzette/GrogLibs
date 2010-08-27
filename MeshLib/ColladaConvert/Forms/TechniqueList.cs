using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Xna.Framework.Graphics;
using MeshLib;

namespace ColladaConvert
{
	public partial class TechniqueList : Form
	{
		MaterialLib.MaterialLib	mMatLib;

		BindingList<ShaderGridModel>	mTechniques	=new BindingList<ShaderGridModel>();

		public event EventHandler	eOk;
		public event EventHandler	eCancel;


		public TechniqueList(MaterialLib.MaterialLib matLib, string matName)
		{
			InitializeComponent();

			mMatLib	=matLib;

			Effect	fx	=matLib.GetMaterialShader(matName);

			foreach(EffectTechnique et in fx.Techniques)
			{
				ShaderGridModel	sgm	=new ShaderGridModel();
				sgm.Name	=et.Name;
				mTechniques.Add(sgm);
			}
			TechniqueGrid.DataSource	=mTechniques;
		}

		private void OnSelectionChanged(object sender, EventArgs e)
		{
			DataGridViewSelectedRowCollection
				sel	=TechniqueGrid.SelectedRows;

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
			Debug.Assert(TechniqueGrid.CurrentRow != null);

			eOk((string)TechniqueGrid.CurrentRow.Cells[0].FormattedValue, null);

			Close();
		}

		private void OnCancel(object sender, EventArgs e)
		{
			eCancel(null, null);
			Close();
		}
	}
}
