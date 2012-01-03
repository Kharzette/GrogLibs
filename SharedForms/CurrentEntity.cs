using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.Diagnostics;
using EntityLib;


namespace SharedForms
{
	public partial class CurrentEntity : Form
	{
		//entity and system
		EntitySystem	mESystem;
		Entity			mCurEntity;

		//data the controls bind to
		BindingList<string>	mCompNames	=new BindingList<string>();

		//list of types returned from system
		List<Type>	mCompTypes;

		//events
		public event EventHandler	eEntChanged;


		public CurrentEntity(EntitySystem es)
		{
			InitializeComponent();

			mESystem	=es;

			mCompTypes	=mESystem.GetComponentChoices();

			foreach(Type t in mCompTypes)
			{
				mCompNames.Add(t.ToString());
			}

			ComponentChoices.DataSource	=mCompNames;
			this.Enabled	=false;

			//set column width
			OnSizeChanged(null, null);
		}


		void OnAddComponent(object sender, EventArgs e)
		{
			string	compName	=ComponentChoices.SelectedItem as string;

			//find in type list
			foreach(Type t in mCompTypes)
			{
				if(t.ToString() == compName)
				{
					Assembly	ass		=Assembly.GetAssembly(t);
					object		comp	=ass.CreateInstance(compName);

					mESystem.AddComponent(mCurEntity, comp as EntityLib.Component);

					UpdateList();

					return;
				}
			}
		}


		void UpdateList()
		{
			//clear listview
			ComponentList.Items.Clear();

			//get all components for current entity
			List<EntityLib.Component>	comps	=mESystem.GetComponents(mCurEntity, null);

			foreach(EntityLib.Component comp in comps)
			{
				ListViewItem	lvi	=new ListViewItem();

				lvi.Text	=comp.GetType().ToString();
				lvi.Tag		=comp;

				ComponentList.Items.Add(lvi);
			}
		}


		public void SetCurrentEntity(Entity ent)
		{
			if(ent == null)
			{
				ComponentProperties.SelectedObject	=null;
				this.Enabled	=false;
				return;
			}

			mCurEntity		=ent;
			EntityID.Text	="" + ent.ID;

			UpdateList();

			ComponentList.SelectedItems.Clear();
			if(ComponentList.Items.Count > 0)
			{
				ComponentList.Items[0].Selected	=true;
			}

			this.Enabled	=true;
		}


		private void OnComponentListSelectedIndexChanged(object sender, EventArgs e)
		{
			if(ComponentList.SelectedIndices.Count == 0)
			{
				ComponentProperties.SelectedObject	=null;
				return;
			}

			Debug.Assert(ComponentList.SelectedIndices.Count == 1);

			int	idx	=ComponentList.SelectedIndices[0];

			ListViewItem	selItem	=ComponentList.Items[idx];

			ComponentProperties.SelectedObject	=selItem.Tag;
		}


		void OnSizeChanged(object sender, EventArgs e)
		{
			//make column width cover the entire thing
			//minus room for a vertical scrollbar
			int	width	=ComponentList.Width;

			//adjust for scrollbar
			width	-=SystemInformation.VerticalScrollBarWidth;

			//adjust for frame
			if(ComponentList.BorderStyle == BorderStyle.Fixed3D)
			{
				width	-=4;
			}
			else if(ComponentList.BorderStyle == BorderStyle.FixedSingle)
			{
				width	-=2;
			}

			ComponentList.Columns[0].Width	=width;
		}


		void OnCListKeyUp(object sender, KeyEventArgs e)
		{
			if(e.KeyCode == Keys.Delete)
			{
				if(ComponentList.SelectedItems.Count == 1)
				{
					ListViewItem	itm	=ComponentList.SelectedItems[0];

					//blast from listview
					ComponentList.Items.Remove(itm);

					//nuke from system
					mESystem.RemoveComponent(mCurEntity, itm.Tag as EntityLib.Component);

					UtilityLib.Misc.SafeInvoke(eEntChanged, null);
				}
			}
		}


		void OnPropertyValueChanged(object s, PropertyValueChangedEventArgs e)
		{
			UtilityLib.Misc.SafeInvoke(eEntChanged, null);
		}
	}
}
