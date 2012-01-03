using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;


namespace SharedForms
{
	//adapted from Wout de Zeeuw on codeproject
	public class TabbyPropertyGrid : PropertyGrid
	{
		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if(keyData == Keys.Tab || keyData == (Keys.Tab | Keys.Shift))
			{
				GridItem	selItem	=SelectedGridItem;
				GridItem	root	=selItem;

				//find root
				while(root.Parent != null)
				{
					root	=root.Parent;
				}

				List<GridItem>	items	=new List<GridItem>();

				AddExpandedItems(root, items);

				if(selItem != null)
				{
					int	idx	=items.IndexOf(selItem);

					if((keyData & Keys.Shift) == Keys.Shift)
					{
						idx--;

						if(idx < 0)
						{
							idx	=items.Count - 1;
						}

						SelectedGridItem	=items[idx];
						if(SelectedGridItem.GridItems != null
							&& SelectedGridItem.GridItems.Count > 0)
						{
							SelectedGridItem.Expanded	=false;
						}
					}
					else
					{
						idx++;

						if(idx >= items.Count)
						{
							idx	=0;
						}

						SelectedGridItem	=items[idx];
						if(SelectedGridItem.GridItems.Count > 0)
						{
							SelectedGridItem.Expanded	=true;
						}
					}
					return	true;
				}
			}

			return base.ProcessCmdKey(ref msg, keyData);
		}


		void AddExpandedItems(GridItem parent, List<GridItem> items)
		{
			if(parent.PropertyDescriptor != null)
			{
				items.Add(parent);
			}

			if(parent.Expanded)
			{
				foreach(GridItem child in parent.GridItems)
				{
					AddExpandedItems(child, items);
				}
			}
		}
	}
}
