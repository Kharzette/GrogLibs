using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Microsoft.Xna.Framework.Graphics;
using Character;

namespace ColladaConvert
{
	public partial class MaterialForm : Form
	{
		MaterialGridModel	mMatModel;		
		MaterialLib			mMatLib;		
		OpenFileDialog		mOFD	=new OpenFileDialog();

		//data bindings for tex & shader
		BindingList<TextureGridModel>	mTextures	=new BindingList<TextureGridModel>();
		BindingList<ShaderGridModel>	mShaders	=new BindingList<ShaderGridModel>();

		
		public MaterialForm(MaterialLib matlib)
		{
			InitializeComponent();

			mMatLib	=matlib;

			mMatModel	=new MaterialGridModel(mMatLib.GetMaterials());

			//disable buttons
			ApplyTexture.Enabled	=false;
			ApplyMaterial.Enabled	=false;

			Dictionary<string, Texture2D>	textures	=mMatLib.GetTextures();
			Dictionary<string, Effect>		shaders		=mMatLib.GetShaders();

			foreach(KeyValuePair<string, Texture2D> tex in textures)
			{
				TextureGridModel	tgm	=new TextureGridModel();
				tgm.Name	=tex.Key;
				mTextures.Add(tgm);
			}

			foreach(KeyValuePair<string, Effect> fx in shaders)
			{
				ShaderGridModel	sgm	=new ShaderGridModel();
				sgm.Name	=fx.Key;
				mShaders.Add(sgm);
			}

			TextureGrid.DataSource	=mTextures;
			MaterialGrid.DataSource	=mMatModel;
			ShaderGrid.DataSource	=mShaders;

			Collada.eMeshPartListUpdated	+=OnMeshPartListUpdated;
		}


		private void ApplyTexture_Click(object sender, EventArgs e)
		{
			Character.Material	mat	=new Character.Material();

			if(TextureGrid.CurrentRow != null)
			{
				mat.Map			=(string)TextureGrid.CurrentRow.Cells[0].FormattedValue;
				mat.Name		=(string)TextureGrid.CurrentRow.Cells[0].FormattedValue;
				mat.ShaderName	=(string)ShaderGrid.CurrentRow.Cells[0].FormattedValue;

				//chop off the file extension
				mat.Name	=mat.Name.Substring(0, mat.Name.Length - 4);

				mMatLib.AddMaterial(mat);
				UpdateMaterials();
			}
		}


		private void ApplyMaterial_Click(object sender, EventArgs e)
		{
			DataGridViewSelectedRowCollection	matSel	=MaterialGrid.SelectedRows;
			DataGridViewSelectedRowCollection	mpSel	=MeshPartGrid.SelectedRows;

			mpSel[0].Cells[1].Value	=matSel[0].Cells[0].Value;
		}


		private void TextureGrid_SelectionChanged(object sender, EventArgs e)
		{
			DataGridViewSelectedRowCollection	texSel	=TextureGrid.SelectedRows;
			DataGridViewSelectedRowCollection	shdSel	=ShaderGrid.SelectedRows;

			if(texSel.Count > 0 && shdSel.Count > 0)
			{
				ApplyTexture.Enabled	=true;
			}
			else
			{
				ApplyTexture.Enabled	=false;
			}
		}


		private void ShaderGrid_SelectionChanged(object sender, EventArgs e)
		{
			TextureGrid_SelectionChanged(sender, e);
		}


		private void MaterialGrid_SelectionChanged(object sender, EventArgs e)
		{
			DataGridViewSelectedRowCollection	matSel	=MaterialGrid.SelectedRows;
			DataGridViewSelectedRowCollection	mpSel	=MeshPartGrid.SelectedRows;

			if(matSel.Count > 0 && mpSel.Count > 0)
			{
				ApplyMaterial.Enabled	=true;
			}
			else
			{
				ApplyMaterial.Enabled	=false;
			}
		}


		private void MeshPartGrid_SelectionChanged(object sender, EventArgs e)
		{
			MaterialGrid_SelectionChanged(sender, e);
		}


		private void OnMeshPartListUpdated(object sender, EventArgs ea)
		{
			BindingList<Character.Mesh>	blm	=(BindingList<Character.Mesh>)sender;

			MeshPartGrid.DataSource	=blm;
		}


		private void UpdateMaterials()
		{
			mMatModel	=new MaterialGridModel(mMatLib.GetMaterials());
			MaterialGrid.DataSource	=mMatModel;
		}

		private void MaterialGrid_CellValidated(object sender, DataGridViewCellEventArgs e)
		{
			mMatLib.UpdateDictionaries();
		}
	}
}
