using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using EntityLib;


namespace SharedForms
{
	public partial class EntityList : Form
	{
		EntitySystem	mESystem;

		//file dialogs
		OpenFileDialog	mOFD	=new OpenFileDialog();
		SaveFileDialog	mSFD	=new SaveFileDialog();

		public event EventHandler	eEntitySelected;


		public EntityList(EntitySystem es)
		{
			InitializeComponent();

			mESystem	=es;

			OnSizeChanged(null, null);
		}


		void OnSizeChanged(object sender, EventArgs e)
		{
			//make column width cover the entire thing
			//minus room for a vertical scrollbar
			int	width	=Entities.Width;

			//adjust for scrollbar
			width	-=SystemInformation.VerticalScrollBarWidth;

			//adjust for ID column
			width	-=80;

			//adjust for frame
			if(Entities.BorderStyle == BorderStyle.Fixed3D)
			{
				width	-=4;
			}
			else if(Entities.BorderStyle == BorderStyle.FixedSingle)
			{
				width	-=2;
			}

			Entities.Columns[0].Width	=80;
			Entities.Columns[1].Width	=width;
		}


		void OnKeyUp(object sender, KeyEventArgs e)
		{
			if(e.KeyCode == Keys.Delete)
			{
				foreach(ListViewItem lvi in Entities.SelectedItems)
				{
					//blast from listview
					Entities.Items.Remove(lvi);

					//nuke from entity system as well
					mESystem.KillEntity(lvi.Tag as Entity);
				}
			}
		}


		void OnSelectionChanged(object sender, EventArgs e)
		{
			if(Entities.SelectedIndices.Count == 0
				|| Entities.SelectedIndices.Count > 1)
			{
				UtilityLib.Misc.SafeInvoke(eEntitySelected, null);
				return;
			}

			ListViewItem	seld	=Entities.SelectedItems[0];

			UtilityLib.Misc.SafeInvoke(eEntitySelected, seld.Tag);
		}


		public void RefreshSelected()
		{
			foreach(ListViewItem lvi in Entities.SelectedItems)
			{
				//nuke existing
				lvi.SubItems.Clear();

				Entity	ent	=lvi.Tag as Entity;

				lvi.Text	="" + ent.ID;

				List<EntityLib.Component>	comps	=
					mESystem.GetComponents(ent, typeof(EntityLib.Components.NonUniqueName));

				if(comps.Count != 0)
				{
					EntityLib.Components.NonUniqueName	nun	=
						comps[0] as EntityLib.Components.NonUniqueName;

					lvi.SubItems.Add(nun.Name);
				}
			}
		}


		public List<Entity> GetEntities()
		{
			List<Entity>	ret	=new List<Entity>();

			foreach(ListViewItem lvi in Entities.Items)
			{
				ret.Add(lvi.Tag as Entity);
			}
			return	ret;
		}


		void OnCreateNew(object sender, EventArgs e)
		{
			ListViewItem	lvi	=new ListViewItem();
			Entity			ent	=new Entity();
			lvi.Tag	=ent;

			EntityLib.Components.NonUniqueName	name
				=new EntityLib.Components.NonUniqueName();
			name.Name	="Unnamed";

			mESystem.AddComponent(ent, name);

			lvi.Text	="" + ent.ID;
			lvi.SubItems.Add(name.Name);

			Entities.Items.Add(lvi);
		}


		void OnLoad(object sender, EventArgs ea)
		{
			mOFD.Multiselect	=false;
			DialogResult	dr	=mOFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			FileStream		fs	=new FileStream(mOFD.FileName, FileMode.Open, FileAccess.Read);
			BinaryReader	br	=new BinaryReader(fs);

			mESystem.Load(br);

			br.Close();
			fs.Close();

			//populate list
			List<Entity>	ents	=mESystem.GetAllEntitiesWithComponent(null);
			foreach(Entity e in ents)
			{
				ListViewItem	lvi	=new ListViewItem();
				lvi.Tag	=e;

				List<EntityLib.Component>	comps	=
					mESystem.GetComponents(e, typeof(EntityLib.Components.NonUniqueName));

				lvi.Text	="" + e.ID;

				if(comps != null && comps.Count > 0)
				{
					EntityLib.Components.NonUniqueName	name
						=comps[0] as EntityLib.Components.NonUniqueName;

					lvi.SubItems.Add(name.Name);
				}

				Entities.Items.Add(lvi);
			}
		}


		void OnSave(object sender, EventArgs e)
		{
			DialogResult	dr	=mSFD.ShowDialog();

			if(dr == DialogResult.Cancel)
			{
				return;
			}

			FileStream		fs	=new FileStream(mSFD.FileName, FileMode.Create, FileAccess.Write);
			BinaryWriter	bw	=new BinaryWriter(fs);

			mESystem.Save(bw);

			bw.Close();
			fs.Close();
		}
	}
}
