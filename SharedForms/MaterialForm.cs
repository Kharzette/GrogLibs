using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MeshLib;
using UtilityLib;


namespace SharedForms;

public partial class MaterialForm : Form
{
	OpenFileDialog		mOFD	=new OpenFileDialog();
	SaveFileDialog		mSFD	=new SaveFileDialog();
	ColorDialog			mCD		=new ColorDialog();


	MaterialLib.MaterialLib	mMatLib;
	MaterialLib.StuffKeeper	mSKeeper;

	public event EventHandler	eNukedMeshPart;
	public event EventHandler	eStripElements;
	public event EventHandler	eGenTangents;
	public event EventHandler	eFoundSeams;
	public event EventHandler	eMatLibNotReadyToSave;


	public MaterialForm(MaterialLib.MaterialLib matLib,
		MaterialLib.StuffKeeper sk)
	{
		InitializeComponent();

		mMatLib		=matLib;
		mSKeeper	=sk;

		MaterialList.Columns.Add("Name");
		MaterialList.Columns.Add("VShader");
		MaterialList.Columns.Add("PShader");

		RefreshMaterials();

		MeshPartList.Columns.Add("Name");
		MeshPartList.Columns.Add("Material Name");
		MeshPartList.Columns.Add("Vertex Format");
		MeshPartList.Columns.Add("Visible");

		//This needs to happen to get the middle stuff in the
		//right spot.  It happens automagically but to no avail...
		//I think everything isn't quite set up when it is called.
		//So call it manually.
		OnFormSizeChanged(null, null);
	}


	public void RefreshMaterials()
	{
		Action<ListView>	clear	=lv => lv.Items.Clear();

		FormExtensions.Invoke(MaterialList, clear);

		List<string>	names	=mMatLib.GetMaterialNames();
		List<string>	vsEnts	=mSKeeper.GetVSEntryList();
		List<string>	psEnts	=mSKeeper.GetVSEntryList();

		foreach(string name in names)
		{
			Action<ListView>	addItem	=lv => lv.Items.Add(name);

			FormExtensions.Invoke(MaterialList, addItem);
		}

		for(int i=0;i < MaterialList.Items.Count;i++)
		{
			Action<ListView>	tagAndSub	=lv =>
			{
				lv.Items[i].Tag = "MaterialName";
				lv.Items[i].SubItems.Add(MaterialList.Items[i].Text);
				lv.Items[i].SubItems.Add(MaterialList.Items[i].Text);
				lv.Items[i].SubItems[1].Tag	="MaterialVS";
				lv.Items[i].SubItems[2].Tag	="MaterialPS";
			};

			FormExtensions.Invoke(MaterialList, tagAndSub);
		}

		FormExtensions.SizeColumns(MaterialList);
	}


	public void RefreshMeshPartList()
	{
		MeshPartList.Items.Clear();

		Mesh.MeshAndArch	maa	=MeshPartList.Tag as Mesh.MeshAndArch;
		if(maa == null)
		{
			return;
		}

		int	count	=maa.mArch.GetPartCount();

		for(int i=0;i < count;i++)
		{
			string	partName	=maa.mArch.GetPartName(i);
			Type	partType	=maa.mArch.GetPartVertexType(i);

			string	matName	="";
			if(maa.mMesh is StaticMesh)
			{
				matName	=(maa.mMesh as StaticMesh).GetPartMaterialName(i);
			}
			else
			{
				matName	=(maa.mMesh as Character).GetPartMaterialName(i);
			}

			ListViewItem	lvi	=MeshPartList.Items.Add(partName);

			lvi.Tag	=i;

			lvi.SubItems.Add(matName);
			lvi.SubItems.Add(partType.ToString());

			//set the tag on this one for click detection help
			ListViewItem.ListViewSubItem	vis	=lvi.SubItems.Add("true");
			vis.Tag	=69;
		}

		FormExtensions.SizeColumns(MeshPartList);
	}


	public int GetTexCoordSet()
	{
		return	(int)TexCoordSet.Value;
	}


	//from a stacko question
	int DropDownWidth(ListBox myBox)
	{
		int maxWidth = 0, temp = 0;
		foreach(var obj in myBox.Items)
		{
			temp	=TextRenderer.MeasureText(obj.ToString(), myBox.Font).Width;
			if(temp > maxWidth)
			{
				maxWidth = temp;
			}
		}
		return maxWidth;
	}
	
	
	void SpawnVSComboBox(string matName, ListViewItem.ListViewSubItem sub)
	{	
		List<string>	vsEntries	=mSKeeper.GetVSEntryList();
		if(vsEntries.Count <= 0)
		{
			return;
		}

		ListBox				lbox	=new ListBox();
		ListBoxContainer	lbc		=new ListBoxContainer();

		Point	loc	=sub.Bounds.Location;

		lbc.Location	=MaterialList.PointToScreen(loc);

		lbox.Parent		=lbc;
		lbox.Location	=new Point(0, 0);
		lbox.Tag		=matName;

		string	current	=mMatLib.GetMaterialVShader(matName);

		foreach(string fx in vsEntries)
		{
			lbox.Items.Add(fx);
		}

		if(current != null)
		{
			lbox.SelectedItem	=current;
		}

		int	width	=DropDownWidth(lbox);

		width	+=SystemInformation.VerticalScrollBarWidth;

		Size	fit	=new System.Drawing.Size(width, lbox.Size.Height);

		lbox.Size	=fit;
		lbc.Size	=fit;

		lbc.Visible		=true;
		lbox.Visible	=true;

		lbox.MouseClick	+=OnVSListBoxClick;
		lbox.Leave		+=OnVSListBoxEscaped;
		lbox.KeyPress	+=OnVSListBoxKey;
		lbox.LostFocus	+=OnVSLostFocus;
		lbox.Focus();
	}


	void SpawnPSComboBox(string matName, ListViewItem.ListViewSubItem sub)
	{
		List<string>	psEntries	=mSKeeper.GetPSEntryList();
		if(psEntries.Count <= 0)
		{
			return;
		}

		ListBox				lbox	=new ListBox();
		ListBoxContainer	lbc		=new ListBoxContainer();

		Point	loc	=sub.Bounds.Location;

		lbc.Location	=MaterialList.PointToScreen(loc);
		lbox.Parent		=lbc;
		lbox.Location	=new Point(0, 0);
		lbox.Tag		=matName;

		foreach(string pe in psEntries)
		{
			lbox.Items.Add(pe);
		}

		string	current	=mMatLib.GetMaterialPShader(matName);
		if(current != null)
		{
			lbox.SelectedItem	=current;
		}

		int	width	=DropDownWidth(lbox);

		width	+=SystemInformation.VerticalScrollBarWidth;

		Size	fit	=new System.Drawing.Size(width, lbox.Size.Height);

		lbox.Size	=fit;
		lbc.Size	=fit;

		lbox.Visible	=true;
		lbc.Visible	=true;

		lbox.Leave		+=OnPSListBoxEscaped;
		lbox.KeyPress	+=OnPSListBoxKey;
		lbox.MouseClick	+=OnPSListBoxClick;
		lbox.LostFocus	+=OnPSLostFocus;
		lbox.Focus();
	}


	void SpawnTextureComboBox(PictureBox pb, string matName)
	{
		List<string>	texs	=mSKeeper.GetTexture2DList();
		if(texs.Count <= 0)
		{
			return;
		}

		ListBox				lbox	=new ListBox();
		ListBoxContainer	lbc		=new ListBoxContainer();

		Point	loc	=pb.Bounds.Location;

		lbc.Location	=MaterialList.PointToScreen(loc);
		lbox.Parent		=lbc;
		lbox.Location	=new Point(0, 0);
		lbox.Tag		=pb.Name;

		foreach(string pe in texs)
		{
			lbox.Items.Add(pe);
		}

		string	current	=mMatLib.GetMaterialTexture0(matName);
		if(current != null)
		{
			lbox.SelectedItem	=current;
		}

		int	width	=DropDownWidth(lbox);

		width	+=SystemInformation.VerticalScrollBarWidth;

		Size	fit	=new System.Drawing.Size(width, lbox.Size.Height);

		lbox.Size	=fit;
		lbc.Size	=fit;

		lbox.Visible	=true;
		lbc.Visible	=true;

		lbox.Leave		+=OnTexListBoxEscaped;
		lbox.KeyPress	+=OnTexListBoxKey;
		lbox.MouseClick	+=OnTexListBoxClick;
		lbox.LostFocus	+=OnTexLostFocus;
		lbox.Focus();
	}


	//not a real type, just is it a bsp, basic mesh, character etc
	void CheckMaterialType(string mat)
	{
		mMatLib.CheckMaterialType(mat);
	}


	void SetListVShader(string mat, string shd)
	{
		foreach(ListViewItem lvi in MaterialList.Items)
		{
			if(lvi.Text == mat)
			{
				lvi.SubItems[1].Text	=shd;
				CheckMaterialType(mat);
				return;
			}
		}
	}


	void SetListPShader(string mat, string shd)
	{
		foreach(ListViewItem lvi in MaterialList.Items)
		{
			if(lvi.Text == mat)
			{
				lvi.SubItems[2].Text	=shd;
				return;
			}
		}
	}


	void SetListTexture(string mat, string shd)
	{
		foreach(ListViewItem lvi in MaterialList.Items)
		{
			if(lvi.Text == mat)
			{
				lvi.SubItems[2].Text	=shd;
				return;
			}
		}
	}


	void OnPSListBoxKey(object sender, KeyPressEventArgs kpea)
	{
		ListBox	lb	=sender as ListBox;

		if(kpea.KeyChar == 27)	//escape
		{
			DisposePBox(lb);
		}
		else if(kpea.KeyChar == '\r')
		{
			if(lb.SelectedIndex != -1)
			{
				mMatLib.SetMaterialPShader(lb.Tag as string, lb.SelectedItem as string);
				SetListPShader(lb.Tag as string, lb.SelectedItem as string);
				OnMaterialSelectionChanged(null, null);
			}
			DisposePBox(lb);
		}
	}


	void OnTexListBoxKey(object sender, KeyPressEventArgs kpea)
	{
		ListBox	lb	=sender as ListBox;

		if(kpea.KeyChar == 27)	//escape
		{
			DisposeTexBox(lb);
		}
		else if(kpea.KeyChar == '\r')
		{
			if(lb.SelectedIndex != -1)
			{
				mMatLib.SetMaterialTexture0(lb.Tag as string, lb.SelectedItem as string);
				SetListTexture(lb.Tag as string, lb.SelectedItem as string);
				OnMaterialSelectionChanged(null, null);
			}
			DisposePBox(lb);
		}
	}


	void OnVSLostFocus(object sender, EventArgs ea)
	{
		DisposeVBox(sender as ListBox);
	}


	void OnPSLostFocus(object sender, EventArgs ea)
	{
		DisposeVBox(sender as ListBox);
	}


	void OnTexLostFocus(object sender, EventArgs ea)
	{
		DisposeTexBox(sender as ListBox);
	}


	void OnVSListBoxKey(object sender, KeyPressEventArgs kpea)
	{
		ListBox	lb	=sender as ListBox;

		if(kpea.KeyChar == 27)	//escape
		{
			DisposeVBox(lb);
		}
		else if(kpea.KeyChar == '\r')
		{
			if(lb.SelectedIndex != -1)
			{
				mMatLib.SetMaterialVShader(lb.Tag as string, lb.SelectedItem as string);
				SetListVShader(lb.Tag as string, lb.SelectedItem as string);
				OnMaterialSelectionChanged(null, null);
			}
			DisposeVBox(lb);
		}
	}


	void OnPSListBoxClick(object sender, MouseEventArgs mea)
	{
		ListBox	lb	=sender as ListBox;

		if(lb.SelectedIndex != -1)
		{
			mMatLib.SetMaterialPShader(lb.Tag as string, lb.SelectedItem as string);
			SetListPShader(lb.Tag as string, lb.SelectedItem as string);
			OnMaterialSelectionChanged(null, null);
		}
		DisposePBox(lb);
	}


	void OnTexListBoxClick(object sender, MouseEventArgs mea)
	{
		ListBox	lb	=sender as ListBox;

		if(lb.SelectedIndex != -1)
		{
			mMatLib.SetMaterialTexture0(lb.Tag as string, lb.SelectedItem as string);
			SetListTexture(lb.Tag as string, lb.SelectedItem as string);
			OnMaterialSelectionChanged(null, null);
		}
		DisposePBox(lb);
	}


	void OnVSListBoxClick(object sender, MouseEventArgs mea)
	{
		ListBox	lb	=sender as ListBox;

		if(lb.SelectedIndex != -1)
		{
			mMatLib.SetMaterialVShader(lb.Tag as string, lb.SelectedItem as string);
			SetListVShader(lb.Tag as string, lb.SelectedItem as string);
			OnMaterialSelectionChanged(null, null);
		}
		DisposeVBox(lb);
	}


	void OnPSListBoxEscaped(object sender, EventArgs ea)
	{		
		ListBox	lb	=sender as ListBox;

		DisposePBox(lb);
	}


	void OnVSListBoxEscaped(object sender, EventArgs ea)
	{
		ListBox	lb	=sender as ListBox;

		DisposeVBox(lb);
	}


	void OnTexListBoxEscaped(object sender, EventArgs ea)
	{
		ListBox	lb	=sender as ListBox;

		DisposeVBox(lb);
	}


	void OnMaterialSelectionChanged(object sender, EventArgs e)
	{
	}

	
	void OnMaterialRename(object sender, LabelEditEventArgs e)
	{
		if(!mMatLib.RenameMaterial(MaterialList.Items[e.Item].Text, e.Label))
		{
			e.CancelEdit	=true;
		}
		else
		{
			FormExtensions.SizeColumns(MaterialList);	//this doesn't work, still has the old value
		}
	}


	void OnMeshPartMouseUp(object sender, MouseEventArgs mea)
	{
		Mesh.MeshAndArch	maa	=MeshPartList.Tag as Mesh.MeshAndArch;
		if(maa == null)
		{
			return;
		}

		StaticMesh	sm	=maa.mMesh as StaticMesh;
		Character	chr	=maa.mMesh as Character;
		if(sm == null && chr == null)
		{
			return;
		}

		foreach(ListViewItem lvi in MeshPartList.Items)
		{
			if(lvi.Bounds.Contains(mea.Location))
			{
				foreach(ListViewItem.ListViewSubItem sub in lvi.SubItems)
				{
					if(sub.Bounds.Contains(mea.Location))
					{
						if(sub.Tag != null && (int)sub.Tag == 69)
						{
							int	index	=(int)lvi.Tag;

							if((string)sub.Text == "True")
							{
								sub.Text	="False";

								if(sm != null)
								{
									sm.SetPartVisible(index, false);
								}
								else
								{
									chr.SetPartVisible(index, false);
								}
							}
							else
							{
								sub.Text	="True";
								if(sm != null)
								{
									sm.SetPartVisible(index, true);
								}
								else
								{
									chr.SetPartVisible(index, true);
								}
							}
						}
					}
				}
			}
		}
	}


	void OnMatListClick(object sender, MouseEventArgs e)
	{
		foreach(ListViewItem lvi in MaterialList.Items)
		{
			if(lvi.Bounds.Contains(e.Location))
			{
				foreach(ListViewItem.ListViewSubItem sub in lvi.SubItems)
				{
					if(sub.Bounds.Contains(e.Location))
					{
						if((string)sub.Tag == "MaterialVS")
						{
							SpawnVSComboBox(lvi.Text, sub);
						}
						else if((string)sub.Tag == "MaterialPS")
						{
							SpawnPSComboBox(lvi.Text, sub);
						}
					}
				}
			}
		}
	}


	//the new button becomes a clone button with a mat selected
	void OnNewMaterial(object sender, EventArgs e)
	{
		string	baseName	="default";
		bool	bClone		=false;
		if(MaterialList.SelectedIndices.Count == 1)
		{
			baseName	=MaterialList.Items[MaterialList.SelectedIndices[0]].Text;
			bClone		=true;
		}

		List<string>	names	=mMatLib.GetMaterialNames();

		string	tryName	=baseName;
		bool	bFirst	=true;
		int		cnt		=1;
		while(names.Contains(tryName))
		{
			if(bFirst)
			{
				tryName	+="000";
				bFirst	=false;
			}
			else
			{
				tryName	=baseName + String.Format("{0:000}", cnt);
				cnt++;
			}
		}

		if(bClone)
		{
//			mMatLib.CloneMaterial(baseName, tryName);
		}
		else
		{
			mMatLib.CreateMaterial(tryName, false, false);
		}

		RefreshMaterials();
	}


	public void SetMesh(object sender)
	{
		Mesh.MeshAndArch	maa	=sender as Mesh.MeshAndArch;
		if(maa == null)
		{
			return;
		}

		MeshPartList.Tag	=maa;

		RefreshMeshPartList();
	}


	void OnFormSizeChanged(object sender, EventArgs e)
	{
		//get the mesh part grid out of the material
		//grid's junk
		int	adjust	=MeshPartGroup.Top - 6;

		adjust	-=(MeshPartList.Top + MeshPartList.Size.Height);

		MeshPartList.SetBounds(MeshPartList.Left,
			MeshPartList.Top + adjust,
			MeshPartList.Width,
			MeshPartList.Height);
	}


	void OnMatListKeyUp(object sender, KeyEventArgs e)
	{
		if(e.KeyValue == 46)	//delete
		{
			if(MaterialList.SelectedItems.Count < 1)
			{
				return;	//nothing to do
			}

			foreach(ListViewItem lvi in MaterialList.SelectedItems)
			{
				mMatLib.NukeMaterial(lvi.Text);
			}

			RefreshMaterials();
			NewMaterial.Text	="New Mat";
		}
	}


	void OnMeshPartListKeyUp(object sender, KeyEventArgs e)
	{
		if(e.KeyValue == 46)	//delete
		{
			if(MeshPartList.SelectedItems.Count < 1)
			{
				return;	//nothing to do
			}

			List<int>	toNuke	=new List<int>();

			foreach(ListViewItem lvi in MeshPartList.SelectedItems)
			{
				toNuke.Add((int)lvi.Tag);
			}

			Misc.SafeInvoke(eNukedMeshPart, toNuke);

			RefreshMeshPartList();
		}
		else if(e.KeyValue == 113)	//F2
		{
			if(MeshPartList.SelectedItems.Count != 1)
			{
				return;	//nothing to do
			}

			MeshPartList.SelectedItems[0].BeginEdit();
		}
	}


	void OnApplyMaterial(object sender, EventArgs e)
	{
		if(MaterialList.SelectedItems.Count != 1)
		{
			return;	//nothing to do
		}

		Mesh.MeshAndArch	maa	=MeshPartList.Tag as Mesh.MeshAndArch;
		if(maa == null)
		{
			return;
		}

		StaticMesh	sm	=maa.mMesh as StaticMesh;
		Character	chr	=maa.mMesh as Character;
		if(sm == null && chr == null)
		{
			return;
		}

		string	matName	=MaterialList.SelectedItems[0].Text;

		foreach(ListViewItem lvi in MeshPartList.SelectedItems)
		{
			int	meshIndex	=(int)lvi.Tag;

			if(chr == null)
			{
				sm.SetPartMaterialName(meshIndex, matName, mSKeeper);
			}
			else
			{
				chr.SetPartMaterialName(meshIndex, matName, mSKeeper);
			}

			lvi.SubItems[1].Text	=matName;
		}
	}


	void OnSaveMaterialLib(object sender, EventArgs e)
	{
		mSFD.DefaultExt	="*.MatLib";
		mSFD.Filter		="Material lib files (*.MatLib)|*.MatLib|All files (*.*)|*.*";

		DialogResult	dr	=mSFD.ShowDialog();

		if(dr == DialogResult.Cancel)
		{
			return;
		}

//		bool	bRet	=mMatLib.SaveToFile(mSFD.FileName);
//		if(!bRet)
		{
			Misc.SafeInvoke(eMatLibNotReadyToSave, null);
		}
	}

	
	void OnLoadMaterialLib(object sender, EventArgs e)
	{
		mOFD.DefaultExt	="*.MatLib";
		mOFD.Filter		="Material lib files (*.MatLib)|*.MatLib|All files (*.*)|*.*";

		DialogResult	dr	=mOFD.ShowDialog();

		if(dr == DialogResult.Cancel)
		{
			return;
		}

//		mMatLib.ReadFromFile(mOFD.FileName);
		RefreshMaterials();
	}


	void OnMatchAndVisible(object sender, EventArgs e)
	{
		Mesh.MeshAndArch	maa	=MeshPartList.Tag as Mesh.MeshAndArch;
		if(maa == null)
		{
			return;
		}

		StaticMesh	sm	=maa.mMesh as StaticMesh;
		Character	chr	=maa.mMesh as Character;
		if(sm == null && chr == null)
		{
			return;
		}

		foreach(ListViewItem lvi in MaterialList.Items)
		{
			string	matName	=lvi.Text;

			foreach(ListViewItem lviMesh in MeshPartList.Items)
			{
				string	meshName	=lviMesh.Text;

				if(meshName.Contains(matName))
				{
					int	meshIndex	=(int)lviMesh.Tag;

					if(chr == null)
					{
						sm.SetPartMaterialName(meshIndex, matName, mSKeeper);
					}
					else
					{
						chr.SetPartMaterialName(meshIndex, matName, mSKeeper);
					}

					lviMesh.SubItems[1].Text	=matName;
				}
			}
		}
	}


	void OnStripElements(object sender, EventArgs e)
	{
		if(MeshPartList.SelectedItems.Count < 1)
		{
			return;
		}

		Mesh.MeshAndArch	maa	=MeshPartList.Tag as Mesh.MeshAndArch;
		if(maa == null)
		{
			return;
		}

		List<int>	parts	=new List<int>();
		foreach(ListViewItem lviMesh in MeshPartList.SelectedItems)
		{
			parts.Add((int)lviMesh.Tag);
		}

		ArchEventArgs	aea	=new ArchEventArgs();

		aea.mArch		=maa.mArch;
		aea.mIndexes	=parts;

		Misc.SafeInvoke(eStripElements, null, aea);
	}


	void DisposeVBox(ListBox lb)
	{
		lb.Leave		-=OnVSListBoxEscaped;
		lb.KeyPress		-=OnVSListBoxKey;
		lb.MouseClick	-=OnVSListBoxClick;
		lb.LostFocus	-=OnVSLostFocus;
		lb.Parent.Dispose();
	}


	void DisposePBox(ListBox lb)
	{
		lb.Leave		-=OnPSListBoxEscaped;
		lb.KeyPress		-=OnPSListBoxKey;
		lb.MouseClick	-=OnPSListBoxClick;
		lb.LostFocus	-=OnPSLostFocus;
		lb.Parent.Dispose();
	}


	void DisposeTexBox(ListBox lb)
	{
		lb.Leave		-=OnTexListBoxEscaped;
		lb.KeyPress		-=OnTexListBoxKey;
		lb.MouseClick	-=OnTexListBoxClick;
		lb.LostFocus	-=OnTexLostFocus;
		lb.Parent.Dispose();
	}


	void OnMergeMatLib(object sender, EventArgs e)
	{
		mOFD.DefaultExt	="*.MatLib";
		mOFD.Filter		="Material lib files (*.MatLib)|*.MatLib|All files (*.*)|*.*";

		DialogResult	dr	=mOFD.ShowDialog();

		if(dr == DialogResult.Cancel)
		{
			return;
		}

//		mMatLib.MergeFromFile(mOFD.FileName);

		RefreshMaterials();
	}


	void OnGuessTextures(object sender, EventArgs e)
	{
		mMatLib.GuessTextures();
	}


	void OnVariableValueChanged(object sender, DataGridViewCellEventArgs e)
	{
		if(MaterialList.SelectedItems.Count < 1
			|| MaterialList.SelectedItems.Count > 1)
		{
			return;	//nothing to do
		}

//		mMatLib.FixTextureVariables(MaterialList.SelectedItems[0].Text);
	}


	void OnFrankenstein(object sender, EventArgs e)
	{
		Mesh.MeshAndArch	maa	=MeshPartList.Tag as Mesh.MeshAndArch;
		if(maa == null)
		{
			return;
		}

		Misc.SafeInvoke(eFoundSeams, maa.mArch.Frankenstein());
	}


	void OnMeshPartRename(object sender, LabelEditEventArgs e)
	{
		Mesh.MeshAndArch	maa	=MeshPartList.Tag as Mesh.MeshAndArch;
		if(maa == null)
		{
			return;
		}

		int	meshIndex	=(int)MeshPartList.Items[e.Item].Tag;

		if(!maa.mArch.RenamePart(meshIndex, e.Label))
		{
			e.CancelEdit	=true;
		}
		else
		{
			FormExtensions.SizeColumns(MeshPartList);	//this doesn't work, still has the old value
		}
	}


	void OnGenTangents(object sender, EventArgs e)
	{
		if(MeshPartList.SelectedItems.Count <= 0)
		{
			return;
		}

		Mesh.MeshAndArch	maa	=MeshPartList.Tag as Mesh.MeshAndArch;
		if(maa == null)
		{
			return;
		}

		List<int>	parts	=new List<int>();
		foreach(ListViewItem lviMesh in MeshPartList.SelectedItems)
		{
			parts.Add((int)lviMesh.Tag);
		}

		ArchEventArgs	aea	=new ArchEventArgs();

		aea.mArch		=maa.mArch;
		aea.mIndexes	=parts;

		Misc.SafeInvoke(eGenTangents, null, aea);

		RefreshMeshPartList();
	}
	
	
	void OnSolidColor(object sender, EventArgs e)
	{
		DialogResult	dr	=mCD.ShowDialog();

		if(dr == DialogResult.Cancel)
		{
			return;
		}

		SolidColor.BackColor	=mCD.Color;
	}
	
	
	void OnTexture0Click(object sender, EventArgs e)
	{
	}
}