using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MeshLib;

namespace SharedForms
{
	public partial class MaterialForm : Form
	{
#if !NoMesh
		public void UpdateMeshPartList(List<SkinnedMesh> skm, List<StaticMesh> stm)
		{
			BindingList<Mesh>	blm	=new BindingList<Mesh>();

			if(skm != null && skm.Count != 0)
			{
				foreach(SkinnedMesh m in skm)
				{
					blm.Add(m);
				}
			}

			if(stm != null && stm.Count != 0)
			{
				foreach(StaticMesh m in stm)
				{
					blm.Add(m);
				}
			}
			MeshPartGrid.DataSource	=blm;

//			BoundsChanged();
		}


		void OnMeshPartNuking(object sender, DataGridViewRowCancelEventArgs e)
		{
			if(e.Row.DataBoundItem.GetType().BaseType == typeof(Mesh))
			{
				Mesh	nukeMe	=(Mesh)e.Row.DataBoundItem;
				eNukedMeshPart(nukeMe, null);
			}
		}


		void OnGenBiNormalTangent(object sender, EventArgs e)
		{
			DataGridViewSelectedRowCollection	mpSel	=MeshPartGrid.SelectedRows;

			if(mpSel == null)
			{
				return;
			}

			foreach(DataGridViewRow row in mpSel)
			{
				Mesh	msh	=row.DataBoundItem as Mesh;
				if(msh == null)
				{
					continue;
				}
				msh.GenTangents(mGD, (int)TexCoordSet.Value);
			}

			//vertex type is longer now, make columns fit
			MeshPartGrid.AutoResizeColumns();

			MeshPartGrid.Refresh();
		}


		void OnMeshPartListUpdated(object sender, EventArgs ea)
		{
			List<SkinnedMesh>	skm	=sender as List<SkinnedMesh>;
			List<StaticMesh>	stm	=sender as List<StaticMesh>;

			if(skm != null && skm.Count != 0)
			{
				BindingList<Mesh>	blm	=new BindingList<Mesh>();

				foreach(SkinnedMesh m in skm)
				{
					blm.Add(m);
				}

				MeshPartGrid.DataSource	=blm;
			}
			else if(stm != null && stm.Count != 0)
			{
				BindingList<Mesh>	blm	=new BindingList<Mesh>();

				foreach(StaticMesh m in stm)
				{
					blm.Add(m);
				}

				MeshPartGrid.DataSource	=blm;
			}
		}
#else
		void OnMeshPartNuking(object sender, DataGridViewRowCancelEventArgs e)
		{
		}

		void OnGenBiNormalTangent(object sender, EventArgs e)
		{
		}

		void OnMeshPartListUpdated(object sender, EventArgs ea)
		{
		}
#endif
	}
}
