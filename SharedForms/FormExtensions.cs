using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;


namespace SharedForms
{
	public static class FormExtensions
	{
#if !XBOX
	    public delegate void Func();


		public static void Invoke<T>(this T c, Action<T> doStuff)
			where T:System.Windows.Forms.Control
		{
			if(c.InvokeRequired)
			{
				c.Invoke((EventHandler) delegate { doStuff(c); } );
			}
			else
			{
				doStuff(c);
			}
		}


		public static void SizeColumns(ListView lv)
		{
			//set to header size first
			Action<ListView>	autoResize	=lvar => lvar.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
			FormExtensions.Invoke(lv, autoResize);

			List<int>	sizes	=new List<int>();
			for(int i=0;i < lv.Columns.Count;i++)
			{
				Action<ListView>	addWidth	=lvar => sizes.Add(lvar.Columns[i].Width);
				FormExtensions.Invoke(lv, addWidth);
			}

			for(int i=0;i < lv.Columns.Count;i++)
			{
				Action<ListView>	arHeader	=lvar => {
					lvar.Columns[i].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);

					if(lvar.Columns[i].Width < sizes[i])
					{
						lvar.Columns[i].AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
					}
				};

				FormExtensions.Invoke(lv, arHeader);
			}
		}
	}
#endif
}
