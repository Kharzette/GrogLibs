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
using Character;

namespace ColladaConvert
{
	public partial class MaterialForm : Form
	{
		MaterialGridModel	mMatModel;		
		MaterialLib			mMatLib;		
		OpenFileDialog		mOFD	=new OpenFileDialog();
		SaveFileDialog		mSFD	=new SaveFileDialog();
		ShaderList			mSL;
		TextureForm			mTF;
		TechniqueList		mTL;

		//temporary reference to a cell being modified
		DataGridViewCell	mEditingCell;


		public MaterialForm(MaterialLib matlib)
		{
			InitializeComponent();

			mMatLib	=matlib;

			mMatModel	=new MaterialGridModel(mMatLib.GetMaterials());

			NewMaterial.Enabled		=true;
			ApplyMaterial.Enabled	=false;

			MaterialGrid.DataSource	=mMatModel;

			ColladaConvert.eMeshPartListUpdated	+=OnMeshPartListUpdated;

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

			mSL.eOk		-=OnTechniqueListOk;
			mSL.eCancel	-=OnTechniqueListCancel;

			MaterialGrid.Enabled	=true;

			//update shader parameters
			Character.Material	mat	=(Character.Material)matSel[0].DataBoundItem;
			UpdateShaderParameters(mat);

			MaterialProperties.DataSource			=mat.Parameters;
			MaterialProperties.Columns[0].ReadOnly	=true;
			MaterialProperties.Columns[1].ReadOnly	=true;
			MaterialProperties.Columns[2].ReadOnly	=true;
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
			Character.Material	mat	=(Character.Material)matSel[0].DataBoundItem;
			UpdateShaderParameters(mat);

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
			OnSelectionChanged(sender, e);
		}


		private void OnMeshPartListUpdated(object sender, EventArgs ea)
		{
			List<Character.Mesh>	lm	=(List<Character.Mesh>)sender;

			BindingList<Character.Mesh>	blm	=new BindingList<Character.Mesh>();

			foreach(Character.Mesh m in lm)
			{
				blm.Add(m);
			}

			MeshPartGrid.DataSource	=blm;
		}


		private void UpdateMaterials()
		{
			mMatModel	=new MaterialGridModel(mMatLib.GetMaterials());
			MaterialGrid.DataSource	=mMatModel;
		}


		private void OnNewMaterial(object sender, EventArgs e)
		{
			Character.Material	m	=new Character.Material();

			m.Name			="default";
			m.ShaderName	="";
			m.Technique		="";

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
				Character.Material	mat	=(Character.Material)matSel[0].DataBoundItem;
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

			mpSel[0].Cells[1].Value	=matSel[0].Cells[0].Value;
		}


		private void OnCellClick(object sender, DataGridViewCellMouseEventArgs e)
		{
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
				Character.Material	m	=(Character.Material)
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


		private void UpdateShaderParameters(Character.Material m)
		{
			//grab fx
			Effect	fx	=mMatLib.GetMaterialShader(m.Name);

			List<ShaderParameters>	parms	=new List<ShaderParameters>();

			foreach(EffectParameter ep in fx.Parameters)
			{
				Debug.WriteLine("Parm: " + ep.Semantic);

				//skip matrices
				if(ep.ParameterClass == EffectParameterClass.MatrixColumns
					|| ep.ParameterClass == EffectParameterClass.MatrixRows)
				{
					continue;
				}

				//skip samplers
				if(ep.ParameterType == EffectParameterType.Sampler)
				{
					continue;
				}

				//skip stuff with lots of elements
				//such as lists of bones
				if(ep.Elements.Count > 0)
				{
					continue;
				}

				ShaderParameters	sp	=new ShaderParameters();

				sp.Name		=ep.Name;
				sp.Class	=ep.ParameterClass;
				sp.Type		=ep.ParameterType;

				switch(sp.Class)
				{
					case EffectParameterClass.MatrixColumns:
						sp.Value	=Convert.ToString(ep.GetValueMatrix());
						break;

					case EffectParameterClass.MatrixRows:
						sp.Value	=Convert.ToString(ep.GetValueMatrix());
						break;

					case EffectParameterClass.Vector:
						if(ep.ColumnCount == 2)
						{
							Vector2	vec	=ep.GetValueVector2();
							sp.Value	=Convert.ToString(vec.X)
								+ " " + Convert.ToString(vec.Y);
						}
						else if(ep.ColumnCount == 3)
						{
							Vector3	vec	=ep.GetValueVector3();
							sp.Value	=Convert.ToString(vec.X)
								+ " " + Convert.ToString(vec.Y)
								+ " " + Convert.ToString(vec.Z);
						}
						else
						{
							Vector4	vec	=ep.GetValueVector4();
							sp.Value	=Convert.ToString(vec.X)
								+ " " + Convert.ToString(vec.Y)
								+ " " + Convert.ToString(vec.Z)
								+ " " + Convert.ToString(vec.W);
						}
						break;
				}					

				parms.Add(sp);
			}
			m.Parameters	=parms;
		}


		private void OnPropCellClick(object sender, DataGridViewCellEventArgs e)
		{
			if(e.ColumnIndex != 3)
			{
				return;	//only interested in param value
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
			Character.Material	m	=(Character.Material)
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

			mMatLib.ReadFromFile(mOFD.FileName);

			UpdateMaterials();

			MaterialProperties.DataSource			=mMatModel[0].Parameters;
			MaterialProperties.Columns[0].ReadOnly	=true;
			MaterialProperties.Columns[1].ReadOnly	=true;
			MaterialProperties.Columns[2].ReadOnly	=true;
		}
	}
}
