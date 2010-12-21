using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace BSPBuilder
{
	public partial class MaterialForm : Form
	{
		MaterialGridModel		mMatModel;		
		MaterialLib.MaterialLib	mMatLib;
		OpenFileDialog			mOFD	=new OpenFileDialog();
		SaveFileDialog			mSFD	=new SaveFileDialog();
		ShaderList				mSL;
		TextureForm				mTF;
		TechniqueList			mTL;
		GraphicsDevice			mGD;

		//temporary reference to a cell being modified
		DataGridViewCell	mEditingCell;

		//events
		public event EventHandler	eBoundsUpdated;
		public event EventHandler	eMaterialNuked;
		public event EventHandler	eLibraryCleared;


		public MaterialForm(GraphicsDevice gd, MaterialLib.MaterialLib matlib)
		{
			InitializeComponent();

			mMatLib		=matlib;
			mGD			=gd;

			mMatModel	=new MaterialGridModel(mMatLib.GetMaterials());

			NewMaterial.Enabled		=true;
			ApplyMaterial.Enabled	=false;

			MaterialGrid.DataSource	=mMatModel;

			MaterialGrid.Columns.Remove("SourceBlend");

			DataGridViewComboBoxColumn	slotColumn
				=new DataGridViewComboBoxColumn();

			foreach(Blend b in Enum.GetValues(typeof(Blend)))
			{
				slotColumn.Items.Add(b);
			}

			slotColumn.DisplayIndex		=0;
			slotColumn.HeaderText		="SourceBlend";
			slotColumn.DataPropertyName	="SourceBlend";

			MaterialGrid.Columns.Add(slotColumn);

			MaterialGrid.Columns.Remove("DestBlend");

			DataGridViewComboBoxColumn	slotColumn2
				=new DataGridViewComboBoxColumn();

			foreach(Blend b in Enum.GetValues(typeof(Blend)))
			{
				slotColumn2.Items.Add(b);
			}

			slotColumn2.DisplayIndex		=0;
			slotColumn2.HeaderText			="DestBlend";
			slotColumn2.DataPropertyName	="DestBlend";

			MaterialGrid.Columns.Add(slotColumn2);

			MaterialGrid.Columns.Remove("CullMode");

			DataGridViewComboBoxColumn	slotColumn3
				=new DataGridViewComboBoxColumn();

			foreach(CullMode cm in Enum.GetValues(typeof(CullMode)))
			{
				slotColumn3.Items.Add(cm);
			}

			slotColumn3.DisplayIndex		=0;
			slotColumn3.HeaderText			="CullMode";
			slotColumn3.DataPropertyName	="CullMode";

			MaterialGrid.Columns.Add(slotColumn3);

			MaterialGrid.Columns.Remove("ZFunction");

			DataGridViewComboBoxColumn	slotColumn4
				=new DataGridViewComboBoxColumn();

			foreach(CompareFunction cm in Enum.GetValues(typeof(CompareFunction)))
			{
				slotColumn4.Items.Add(cm);
			}

			slotColumn4.DisplayIndex		=0;
			slotColumn4.HeaderText			="ZFunction";
			slotColumn4.DataPropertyName	="ZFunction";

			MaterialGrid.Columns.Add(slotColumn4);

			MaterialGrid.Columns.Remove("BlendFunction");

			DataGridViewComboBoxColumn	slotColumn5
				=new DataGridViewComboBoxColumn();

			foreach(BlendFunction cm in Enum.GetValues(typeof(BlendFunction)))
			{
				slotColumn5.Items.Add(cm);
			}

			slotColumn5.DisplayIndex		=0;
			slotColumn5.HeaderText			="BlendFunction";
			slotColumn5.DataPropertyName	="BlendFunction";

			MaterialGrid.Columns.Add(slotColumn5);

			//set shadername and technique columns to
			//read only so text can't be entered in cell
			MaterialGrid.Columns[1].ReadOnly	=true;
			MaterialGrid.Columns[2].ReadOnly	=true;
		}


		private void OnTextureListOk(object sender, EventArgs ea)
		{
			DataGridViewSelectedRowCollection	matSel	=MaterialProperties.SelectedRows;

			mEditingCell.Value	=sender;
			mEditingCell		=null;

			mTF.eOk		-=OnTextureListOk;
			mTF.eCancel	-=OnTextureListCancel;

			MaterialGrid.Enabled		=true;
			MaterialProperties.Enabled	=true;
		}


		private void OnTextureListCancel(object sender, EventArgs ea)
		{
			mTF.eOk		-=OnTextureListOk;
			mTF.eCancel	-=OnTextureListCancel;

			MaterialGrid.Enabled		=true;
			MaterialProperties.Enabled	=true;
		}


		private void OnTechniqueListOk(object sender, EventArgs ea)
		{
			DataGridViewSelectedRowCollection	matSel	=MaterialGrid.SelectedRows;

			matSel[0].Cells[2].Value	=sender;

			mTL.eOk		-=OnTechniqueListOk;
			mTL.eCancel	-=OnTechniqueListCancel;

			MaterialGrid.Enabled	=true;

			//update shader parameters
			MaterialLib.Material	mat	=(MaterialLib.Material)matSel[0].DataBoundItem;

			Effect	fx	=mMatLib.GetMaterialShader(mat.Name);
			mat.UpdateShaderParameters(fx);

			MaterialProperties.DataSource			=mat.Parameters;
			MaterialProperties.Columns[0].ReadOnly	=true;
			MaterialProperties.Columns[1].ReadOnly	=true;
			MaterialProperties.Columns[2].ReadOnly	=true;
			MaterialProperties.Update();
		}


		private void OnTechniqueListCancel(object sender, EventArgs ea)
		{
			mSL.eOk		-=OnTechniqueListOk;
			mSL.eCancel	-=OnTechniqueListCancel;

			MaterialGrid.Enabled	=true;
		}


		private void OnShaderListOk(object sender, EventArgs ea)
		{
			DataGridViewSelectedRowCollection	matSel	=MaterialGrid.SelectedRows;

			matSel[0].Cells[1].Value	=sender;

			mSL.eOk		-=OnShaderListOk;
			mSL.eCancel	-=OnShaderListCancel;

			MaterialGrid.Enabled	=true;

			//update shader parameters
			MaterialLib.Material	mat	=(MaterialLib.Material)matSel[0].DataBoundItem;

			Effect	fx	=mMatLib.GetMaterialShader(mat.Name);
			mat.UpdateShaderParameters(fx);

			MaterialProperties.DataSource			=mat.Parameters;
			MaterialProperties.Columns[0].ReadOnly	=true;
			MaterialProperties.Columns[1].ReadOnly	=true;
			MaterialProperties.Columns[2].ReadOnly	=true;
		}


		private void OnShaderListCancel(object sender, EventArgs ea)
		{
			mSL.eOk		-=OnShaderListOk;
			mSL.eCancel	-=OnShaderListCancel;

			MaterialGrid.Enabled	=true;
		}


		private void MeshPartGrid_SelectionChanged(object sender, EventArgs e)
		{
		}


		internal void UpdateMaterials()
		{
			mMatModel	=new MaterialGridModel(mMatLib.GetMaterials());
			MaterialGrid.DataSource	=mMatModel;
		}


		private void OnNewMaterial(object sender, EventArgs e)
		{
			MaterialLib.Material	m	=new MaterialLib.Material();

			m.Name			="default";
			m.ShaderName	="";
			m.Technique		="";
			m.Alpha			=false;
			m.BlendFunction	=BlendFunction.Add;
			m.SourceBlend	=Blend.SourceAlpha;
			m.DestBlend		=Blend.InverseSourceAlpha;
			m.DepthWrite	=true;
			m.CullMode		=CullMode.CullCounterClockwiseFace;
			m.ZFunction		=CompareFunction.Less;

			mMatLib.AddMaterial(m);
			mMatModel.Add(m);

			MaterialProperties.DataSource			=m.Parameters;
			MaterialProperties.Columns[0].ReadOnly	=true;
			MaterialProperties.Columns[1].ReadOnly	=true;
			MaterialProperties.Columns[2].ReadOnly	=true;
		}


		private void OnSelectionChanged(object sender, EventArgs e)
		{
			DataGridViewSelectedRowCollection	matSel	=MaterialGrid.SelectedRows;
			DataGridViewSelectedRowCollection	mpSel	=MeshPartGrid.SelectedRows;

			if(matSel.Count > 0)
			{
				if(mpSel.Count > 0)
				{
					ApplyMaterial.Enabled	=true;
				}
				MaterialLib.Material	mat	=(MaterialLib.Material)matSel[0].DataBoundItem;
				MaterialProperties.DataSource			=mat.Parameters;
				MaterialProperties.Columns[0].ReadOnly	=true;
				MaterialProperties.Columns[1].ReadOnly	=true;
				MaterialProperties.Columns[2].ReadOnly	=true;
			}
			else
			{
				ApplyMaterial.Enabled	=false;
			}
		}


		private void OnCellValidated(object sender, DataGridViewCellEventArgs e)
		{
			//update name?
			if(e.ColumnIndex == 0)
			{
				mMatLib.UpdateDictionaries();
			}
		}


		private void OnApplyMaterial(object sender, EventArgs e)
		{
			DataGridViewSelectedRowCollection	matSel	=MaterialGrid.SelectedRows;
			DataGridViewSelectedRowCollection	mpSel	=MeshPartGrid.SelectedRows;

			foreach(DataGridViewRow dgvr in mpSel)
			{
				dgvr.Cells[1].Value	=matSel[0].Cells[0].Value;
			}
		}


		private void OnCellClick(object sender, DataGridViewCellMouseEventArgs e)
		{
			if(e.RowIndex == -1)
			{
				return;	//header click?
			}

			//if this is the shader name
			if(e.ColumnIndex == 1)
			{
				mSL	=new ShaderList(mMatLib);
				mSL.eOk					+=OnShaderListOk;
				mSL.eCancel				+=OnShaderListCancel;
				mSL.Visible				=true;
				MaterialGrid.Enabled	=false;
			}
			else if(e.ColumnIndex == 2)
			{
				MaterialLib.Material	m	=(MaterialLib.Material)
					MaterialGrid.Rows[e.RowIndex].DataBoundItem;

				if(mMatLib.GetMaterialShader(m.Name) != null)
				{
					//techniques
					mTL	=new TechniqueList(mMatLib, m.Name);
					mTL.eOk					+=OnTechniqueListOk;
					mTL.eCancel				+=OnTechniqueListCancel;
					mTL.Visible				=true;
					MaterialGrid.Enabled	=false;
				}
			}
		}


		private void OnPropCellClick(object sender, DataGridViewCellEventArgs e)
		{
			if(e.ColumnIndex != 3)
			{
				return;	//only interested in param value
			}
			if(e.RowIndex == -1)
			{
				return;
			}

			DataGridViewCell	cell	=
				MaterialProperties.Rows[e.RowIndex].Cells[3];

			//figure out what type this is
			EffectParameterClass	epc	=(EffectParameterClass)
				MaterialProperties.Rows[e.RowIndex].Cells[1].Value;

			if(epc == EffectParameterClass.Object)
			{
				EffectParameterType	ept	=(EffectParameterType)
					MaterialProperties.Rows[e.RowIndex].Cells[2].Value;

				if(ept == EffectParameterType.Texture)
				{
					//keep a reference to this cell while
					//the tex gump comes up
					mEditingCell	=cell;

					mTF	=new TextureForm(mMatLib);
					mTF.eOk						+=OnTextureListOk;
					mTF.eCancel					+=OnTextureListCancel;
					mTF.Visible					=true;
					MaterialGrid.Enabled		=false;
					MaterialProperties.Enabled	=false;
				}
			}
		}


		private void OnPropValueValidated(object sender, DataGridViewCellEventArgs e)
		{
			if(e.ColumnIndex != 3)
			{
				return;
			}
			MaterialLib.Material	m	=(MaterialLib.Material)
				MaterialGrid.SelectedRows[0].DataBoundItem;

			mMatLib.ApplyParameters(m.Name);
		}


		private void OnSizeChanged(object sender, EventArgs e)
		{
			//get the mesh part grid out of the material
			//grid's junk
			int	adjust	=MaterialGrid.Top - 6;

			adjust	-=(MeshPartGrid.Top + MeshPartGrid.Size.Height);

			MeshPartGrid.SetBounds(MeshPartGrid.Left,
				MeshPartGrid.Top + adjust,
				MeshPartGrid.Width,
				MeshPartGrid.Height);
		}


		private void OnSave(object sender, EventArgs e)
		{
			DialogResult	dr	=mSFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			mMatLib.SaveToFile(mSFD.FileName);
		}


		private void OnLoad(object sender, EventArgs e)
		{
			DialogResult	dr	=mOFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			mMatLib.ReadFromFile(mOFD.FileName, true);

			mMatLib.LoadToolTextures(mGD);

			//notify anyone interested
			if(eLibraryCleared != null)
			{
				eLibraryCleared(null, null);
			}

			UpdateMaterials();

			MaterialProperties.DataSource			=mMatModel[0].Parameters;
			MaterialProperties.Columns[0].ReadOnly	=true;
			MaterialProperties.Columns[1].ReadOnly	=true;
			MaterialProperties.Columns[2].ReadOnly	=true;
		}


		private void OnMeshPartNuking(object sender, DataGridViewRowCancelEventArgs e)
		{
		}


		private void OnNukeMaterial(object sender, DataGridViewRowCancelEventArgs e)
		{
			MaterialLib.Material	mat	=(MaterialLib.Material)e.Row.DataBoundItem;

			mMatLib.NukeMaterial(mat.Name);

			if(eMaterialNuked != null)
			{
				eMaterialNuked(mat.Name, null);
			}
		}


		private void OnBoundMesh(object sender, EventArgs e)
		{
		}


		void BoundsChanged()
		{
		}


		void OnRefreshShaders(object sender, EventArgs e)
		{
			mMatLib.RefreshShaderParameters();
		}


		internal bool bDrawBounds()
		{
			return	DrawBounds.Checked;
		}
	}
}
