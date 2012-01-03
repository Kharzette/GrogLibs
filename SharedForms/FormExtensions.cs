using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
#endif
	}
}
