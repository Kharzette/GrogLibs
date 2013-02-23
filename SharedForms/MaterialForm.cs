using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MeshLib;


namespace SharedForms
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

		//showing mesh part grid?
		bool	mbMeshPartGridVisible;

		//events
		public event EventHandler	eMaterialNuked;
		public event EventHandler	eLibraryCleared;
		public event EventHandler	eNukedMeshPart;
		public event EventHandler	eLibrarySaved;

		//column indexes for special behavior
		int	mShaderColumn;
		int	mTechniqueColumn;
		int	mEmissiveColumn;
		int	mBlendFactorColumn;


		void MakeEnumColumn(string colName, Type colType)
		{
			MaterialGrid.Columns.Remove(colName);

			DataGridViewComboBoxColumn	slotColumn
				=new DataGridViewComboBoxColumn();

			foreach(object b in Enum.GetValues(colType))
			{
				slotColumn.Items.Add(b);
			}

			slotColumn.DisplayIndex		=0;
			slotColumn.HeaderText		=colName;
			slotColumn.DataPropertyName	=colName;

			MaterialGrid.Columns.Add(slotColumn);
		}


		void MakeBoolColumn(string colName)
		{
			MaterialGrid.Columns.Remove(colName);

			DataGridViewComboBoxColumn	slotColumn
				=new DataGridViewComboBoxColumn();

			slotColumn.Items.Add(false);
			slotColumn.Items.Add(true);
			slotColumn.DisplayIndex		=0;
			slotColumn.HeaderText		=colName;
			slotColumn.DataPropertyName	=colName;

			MaterialGrid.Columns.Add(slotColumn);
		}


		void MakeColorColumn(string colName)
		{
			DataGridViewColorColumn	colorColumn	=new DataGridViewColorColumn();
			colorColumn.HeaderText			=colName;
			colorColumn.DataPropertyName	=colName;

			MaterialGrid.Columns.Remove(colName);

			MaterialGrid.Columns.Add(colorColumn);
		}


		public MaterialForm(GraphicsDevice gd,
			MaterialLib.MaterialLib matlib,
			bool bMeshPartsVisible) : base()
		{
			InitializeComponent();

			mMatLib					=matlib;
			mGD						=gd;
			mbMeshPartGridVisible	=bMeshPartsVisible;

			if(!mbMeshPartGridVisible)
			{
				MeshPartGrid.Visible	=false;
			}

			mMatModel	=new MaterialGridModel(mMatLib.GetMaterials());

			NewMaterial.Enabled		=true;
			MaterialGrid.DataSource	=mMatModel;

			MakeColorColumn("Emissive");
			MakeEnumColumn("AlphaBlendFunc", typeof(BlendFunction));
			MakeEnumColumn("AlphaDestBlend", typeof(Blend));
			MakeEnumColumn("AlphaSrcBlend", typeof(Blend));
			MakeColorColumn("BlendFactor");
			MakeEnumColumn("ColorBlendFunc", typeof(BlendFunction));
			MakeEnumColumn("ColorDestBlend", typeof(Blend));
			MakeEnumColumn("ColorSrcBlend", typeof(Blend));
			MakeBoolColumn("DepthEnable");
			MakeEnumColumn("DepthFunc", typeof(CompareFunction));
			MakeBoolColumn("DepthWriteEnable");
			MakeEnumColumn("CullMode", typeof(CullMode));

			foreach(DataGridViewColumn col in MaterialGrid.Columns)
			{
				if(col.HeaderText == "ShaderName")
				{
					mShaderColumn	=MaterialGrid.Columns.IndexOf(col);
				}
				else if(col.HeaderText == "Technique")
				{
					mTechniqueColumn	=MaterialGrid.Columns.IndexOf(col);
				}
				else if(col.HeaderText == "Emissive")
				{
					mEmissiveColumn	=MaterialGrid.Columns.IndexOf(col);
				}
				else if(col.HeaderText == "BlendFactor")
				{
					mBlendFactorColumn	=MaterialGrid.Columns.IndexOf(col);
				}
			}
			

			//set custom columns to
			//read only so text can't be entered in cell
			MaterialGrid.Columns[mShaderColumn].ReadOnly		=true;
			MaterialGrid.Columns[mTechniqueColumn].ReadOnly		=true;
			MaterialGrid.Columns[mEmissiveColumn].ReadOnly		=true;
			MaterialGrid.Columns[mBlendFactorColumn].ReadOnly	=true;

			//adjust size
			if(!mbMeshPartGridVisible)
			{
				MeshPartGroup.Visible	=false;

				//split the height between prop and mat
				MaterialProperties.SetBounds(MaterialProperties.Left,
					MaterialProperties.Top,
					MaterialGrid.Width,
					MaterialGrid.Height + (MeshPartGrid.Height));

				MaterialGrid.SetBounds(MaterialGrid.Left,
					MaterialGrid.Top - (MeshPartGrid.Height / 2),
					MaterialGrid.Width,
					MaterialGrid.Height + (MeshPartGrid.Height / 2));
			}
		}


		public void ApplyMat()
		{
			OnApplyMaterial(null, null);
		}


		void OnTextureListOk(object sender, EventArgs ea)
		{
			DataGridViewSelectedRowCollection	matSel	=MaterialGrid.SelectedRows;

			mEditingCell.Value	=sender;
			mEditingCell		=null;

			mTF.eOk		-=OnTextureListOk;
			mTF.eCancel	-=OnTextureListCancel;

			//hack to autoset width / height
			Texture2D	tex	=mMatLib.GetTexture(sender as string);
			if(tex != null)
			{
				//add / update the tex size parameter
				//but only if it is already there
				MaterialLib.GUIStates	gs	=(MaterialLib.GUIStates)matSel[0].DataBoundItem;
				MaterialLib.Material	mat	=gs.GetParentMaterial();
				mat.SetParameter("mTexSize",
					"" + (tex.Width * 2) + " " + (tex.Height * 2));

				//set texture enabled
				mat.SetParameter("mbTextureEnabled", "true");
			}
			else
			{
				//check for cube
				TextureCube	texCube	=mMatLib.GetTextureCube(sender as string);
				if(texCube != null)
				{
					//change texture param to texcube
					MaterialLib.GUIStates	gs	=(MaterialLib.GUIStates)matSel[0].DataBoundItem;
					MaterialLib.Material	mat	=gs.GetParentMaterial();

					mat.SetTextureParameterToCube("mTexture");

					//set texture enabled
					mat.SetParameter("mbTextureEnabled", "true");
				}
			}
			MaterialGrid.Enabled		=true;
			MaterialProperties.Enabled	=true;
		}


		void OnTextureListCancel(object sender, EventArgs ea)
		{
			mTF.eOk		-=OnTextureListOk;
			mTF.eCancel	-=OnTextureListCancel;

			MaterialGrid.Enabled		=true;
			MaterialProperties.Enabled	=true;
		}


		void OnTechniqueListOk(object sender, EventArgs ea)
		{
			DataGridViewSelectedRowCollection	matSel	=MaterialGrid.SelectedRows;

			matSel[0].Cells[mTechniqueColumn].Value	=sender;

			mTL.eOk		-=OnTechniqueListOk;
			mTL.eCancel	-=OnTechniqueListCancel;

			MaterialGrid.Enabled	=true;

			//update shader parameters
			MaterialLib.GUIStates	gs	=(MaterialLib.GUIStates)matSel[0].DataBoundItem;
			MaterialLib.Material	mat	=gs.GetParentMaterial();

			Effect	fx	=mMatLib.GetMaterialShader(mat.Name);
			mat.UpdateShaderParameters(fx);

			MaterialProperties.DataSource			=mat.Parameters;
			MaterialProperties.Columns[0].ReadOnly	=true;
			MaterialProperties.Columns[1].ReadOnly	=true;
			MaterialProperties.Columns[2].ReadOnly	=true;
			MaterialProperties.Update();
		}


		void OnTechniqueListCancel(object sender, EventArgs ea)
		{
			mTL.eOk		-=OnTechniqueListOk;
			mTL.eCancel	-=OnTechniqueListCancel;

			MaterialGrid.Enabled	=true;
		}


		void OnShaderListOk(object sender, EventArgs ea)
		{
			DataGridViewSelectedRowCollection	matSel	=MaterialGrid.SelectedRows;

			matSel[0].Cells[mShaderColumn].Value	=sender;

			mSL.eOk		-=OnShaderListOk;
			mSL.eCancel	-=OnShaderListCancel;

			MaterialGrid.Enabled	=true;

			//update shader parameters
			MaterialLib.GUIStates	gs	=(MaterialLib.GUIStates)matSel[0].DataBoundItem;
			MaterialLib.Material	mat	=gs.GetParentMaterial();

			Effect	fx	=mMatLib.GetMaterialShader(mat.Name);
			mat.Technique	="";	//reset this
			mat.UpdateShaderParameters(fx);

			MaterialProperties.DataSource			=mat.Parameters;
			MaterialProperties.Columns[0].ReadOnly	=true;
			MaterialProperties.Columns[1].ReadOnly	=true;
			MaterialProperties.Columns[2].ReadOnly	=true;
		}


		void OnShaderListCancel(object sender, EventArgs ea)
		{
			mSL.eOk		-=OnShaderListOk;
			mSL.eCancel	-=OnShaderListCancel;

			MaterialGrid.Enabled	=true;
		}


		void GuessMaterials()
		{
			Dictionary<string, MaterialLib.Material>	mats	=mMatLib.GetMaterials();

			for(int i=0;i < MeshPartGrid.Rows.Count;i++)
			{
				string	meshName	=MeshPartGrid.Rows[i].Cells[0].Value as string;

				if(meshName.EndsWith("Mesh"))
				{
					meshName	=meshName.Substring(0, meshName.Length - 4);

					if(mats.ContainsKey(meshName))
					{
						MeshPartGrid.Rows[i].Cells[1].Value	=meshName;
					}
				}
			}
		}


		public void UpdateMaterials()
		{
			mMatModel	=new MaterialGridModel(mMatLib.GetMaterials());

			Action<DataGridView>	setSrc	=src => src.DataSource = mMatModel;

			SharedForms.FormExtensions.Invoke(MaterialGrid, setSrc);
		}


		void OnNewMaterial(object sender, EventArgs e)
		{
			MaterialLib.Material	m	=mMatLib.CreateMaterial();

			m.Name			="default";
			m.ShaderName	="";
			m.Technique		="";

			bool	bFirst	=true;
			int		cnt		=1;
			while(mMatLib.GetMaterial(m.Name) != null)
			{
				if(bFirst)
				{
					m.Name	+="000";
					bFirst	=false;
				}
				else
				{
					m.Name	="default" + String.Format("{0:000}", cnt);
					cnt++;
				}
			}

			//set some defaults
			m.BlendState	=BlendState.Opaque;
			m.DepthState	=DepthStencilState.Default;
			m.RasterState	=RasterizerState.CullCounterClockwise;

			mMatLib.AddMaterial(m);
			mMatModel.Add(m.GetGUIStates());

			MaterialProperties.DataSource			=m.Parameters;
			MaterialProperties.Columns[0].ReadOnly	=true;
			MaterialProperties.Columns[1].ReadOnly	=true;
			MaterialProperties.Columns[2].ReadOnly	=true;
		}


		void OnApplyMaterial(object sender, EventArgs e)
		{
			DataGridViewSelectedRowCollection	matSel	=MaterialGrid.SelectedRows;
			DataGridViewSelectedRowCollection	mpSel	=MeshPartGrid.SelectedRows;

			foreach(DataGridViewRow dgvr in mpSel)
			{
				dgvr.Cells[1].Value	=matSel[0].Cells[0].Value;
			}
		}


		void OnSelectionChanged(object sender, EventArgs e)
		{
			DataGridViewSelectedRowCollection	matSel	=MaterialGrid.SelectedRows;

			if(matSel.Count > 0)
			{
				MaterialLib.GUIStates	gs	=(MaterialLib.GUIStates)matSel[0].DataBoundItem;
				MaterialProperties.DataSource			=gs.Parameters;
				MaterialProperties.Columns[0].ReadOnly	=true;
				MaterialProperties.Columns[1].ReadOnly	=true;
				MaterialProperties.Columns[2].ReadOnly	=true;
			}
		}


		void OnCellValidated(object sender, DataGridViewCellEventArgs e)
		{
			//update name?
			if(e.ColumnIndex == 0)
			{
				mMatLib.UpdateDictionaries();
			}
		}


		void OnCellClick(object sender, DataGridViewCellMouseEventArgs e)
		{
			if(e.RowIndex == -1)
			{
				return;	//header click?
			}

			//if this is the shader name
			if(e.ColumnIndex == mShaderColumn)
			{
				mSL	=new ShaderList(mMatLib);
				mSL.eOk					+=OnShaderListOk;
				mSL.eCancel				+=OnShaderListCancel;
				mSL.Visible				=true;
				MaterialGrid.Enabled	=false;
			}
			else if(e.ColumnIndex == mTechniqueColumn)
			{
				MaterialLib.GUIStates	gs	=(MaterialLib.GUIStates)MaterialGrid.Rows[e.RowIndex].DataBoundItem;
				MaterialLib.Material	m	=gs.GetParentMaterial();

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
			else if(e.ColumnIndex == mEmissiveColumn)
			{
				ColorDialog	cd	=new ColorDialog();

				cd.AllowFullOpen	=true;
				cd.SolidColorOnly	=true;

				DialogResult	dr	=cd.ShowDialog();

				if(dr == DialogResult.OK)
				{
					MaterialLib.GUIStates	gs	=(MaterialLib.GUIStates)MaterialGrid.Rows[e.RowIndex].DataBoundItem;
					MaterialLib.Material	m	=gs.GetParentMaterial();

					m.Emissive	=DataGridViewColorCell.ConvertColor(cd.Color);
				}
			}
			else if(e.ColumnIndex == mBlendFactorColumn)
			{
				ColorDialog	cd	=new ColorDialog();

				cd.AllowFullOpen	=true;
				cd.SolidColorOnly	=true;

				DialogResult	dr	=cd.ShowDialog();

				if(dr == DialogResult.OK)
				{
					MaterialLib.GUIStates	gs	=(MaterialLib.GUIStates)MaterialGrid.Rows[e.RowIndex].DataBoundItem;
					MaterialLib.Material	m	=gs.GetParentMaterial();

					gs.BlendFactor	=DataGridViewColorCell.ConvertColor(cd.Color);
				}
			}
		}


		void OnPropCellClick(object sender, DataGridViewCellEventArgs e)
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

				if(ept == EffectParameterType.Texture
					|| ept == EffectParameterType.Texture1D
					|| ept == EffectParameterType.Texture2D
					|| ept == EffectParameterType.Texture3D
					|| ept == EffectParameterType.TextureCube)
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


		void OnSizeChanged(object sender, EventArgs e)
		{
			if(!mbMeshPartGridVisible)
			{
				return;
			}
			//get the mesh part grid out of the material
			//grid's junk
			int	adjust	=MaterialGrid.Top - 6;

			adjust	-=(MeshPartGrid.Top + MeshPartGrid.Size.Height);

			MeshPartGrid.SetBounds(MeshPartGrid.Left,
				MeshPartGrid.Top + adjust,
				MeshPartGrid.Width,
				MeshPartGrid.Height);
		}


		void OnPropValueValidated(object sender, DataGridViewCellEventArgs e)
		{
			if(e.ColumnIndex != 3)
			{
				return;
			}
			MaterialLib.GUIStates	gs	=(MaterialLib.GUIStates)MaterialGrid.SelectedRows[0].DataBoundItem;
			MaterialLib.Material	m	=gs.GetParentMaterial();

			mMatLib.ApplyParameters(m.Name);
		}


		void OnSave(object sender, EventArgs e)
		{
			mSFD.DefaultExt	="*.MatLib";
			mSFD.Filter		="Material lib files (*.MatLib)|*.MatLib|All files (*.*)|*.*";

			DialogResult	dr	=mSFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			mMatLib.SaveToFile(mSFD.FileName);

			UtilityLib.Misc.SafeInvoke(eLibrarySaved, mSFD.FileName);
		}


		void OnLoad(object sender, EventArgs e)
		{
			mOFD.DefaultExt	="*.MatLib";
			mOFD.Filter		="Material lib files (*.MatLib)|*.MatLib|All files (*.*)|*.*";

			DialogResult	dr	=mOFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			mMatLib.ReadFromFile(mOFD.FileName, true, mGD);

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


		void OnNukeMaterial(object sender, DataGridViewRowCancelEventArgs e)
		{
			if(!MeshPartGrid.Visible)
			{
				//if working with indoor meshes, delete can screw up
				//material vis indexing
				e.Cancel	=true;
				return;
			}
			MaterialLib.GUIStates	gs	=(MaterialLib.GUIStates)e.Row.DataBoundItem;
			MaterialLib.Material	mat	=gs.GetParentMaterial();

			MaterialProperties.DataSource	=null;

			mMatLib.NukeMaterial(mat.Name);

			if(eMaterialNuked != null)
			{
				eMaterialNuked(mat.Name, null);
			}
		}


		void OnRefreshShaders(object sender, EventArgs e)
		{
			mMatLib.RefreshShaderParameters();
		}


		void OnGuessTextures(object sender, EventArgs e)
		{
			mMatLib.GuessTextures();
			UpdateMaterials();
		}


		void OnTexSizeDown(object sender, EventArgs e)
		{
			if(Microsoft.Xna.Framework.Input.Keyboard.GetState()
				.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift))
			{
				mMatLib.BoostTexSizes(true);
			}
			else
			{
				if(MaterialGrid.SelectedRows.Count <= 0)
				{
					return;
				}
				MaterialLib.GUIStates	gs	=(MaterialLib.GUIStates)MaterialGrid.SelectedRows[0].DataBoundItem;
				mMatLib.BoostTexSize(gs.Name, true);
			}
		}


		void OnTexSizeUp(object sender, EventArgs e)
		{
			if(Microsoft.Xna.Framework.Input.Keyboard.GetState()
				.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift))
			{
				mMatLib.BoostTexSizes(false);
			}
			else
			{
				MaterialLib.GUIStates	gs	=(MaterialLib.GUIStates)MaterialGrid.SelectedRows[0].DataBoundItem;
				mMatLib.BoostTexSize(gs.Name, false);
			}
		}


		void OnGetEmissive(object sender, EventArgs e)
		{
			mMatLib.AssignEmissives();

			MaterialGrid.Refresh();
		}


		void OnMergeMaterialLib(object sender, EventArgs e)
		{
			mOFD.DefaultExt	="*.MatLib";
			mOFD.Filter		="Material lib files (*.MatLib)|*.MatLib|All files (*.*)|*.*";

			DialogResult	dr	=mOFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			mMatLib.MergeFromFile(mOFD.FileName);

			UpdateMaterials();

			MaterialProperties.DataSource			=mMatModel[0].Parameters;
			MaterialProperties.Columns[0].ReadOnly	=true;
			MaterialProperties.Columns[1].ReadOnly	=true;
			MaterialProperties.Columns[2].ReadOnly	=true;
		}


		void OnKeyUp(object sender, KeyEventArgs e)
		{
			if(e.Control)
			{
				if(e.KeyCode == Keys.G)
				{
					OnGenBiNormalTangent(null, null);
				}
				else if(e.KeyCode == Keys.D)
				{
					GuessMaterials();
				}
			}
		}
	}
}
