using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;


namespace BSPBuilder
{
	public partial class CollisionForm : Form
	{
		public event EventHandler	eStartRay;
		public event EventHandler	eEndRay;
		public event EventHandler	eRepeatRay;


		public CollisionForm()
		{
			InitializeComponent();
		}


		void OnStartRay(object sender, EventArgs e)
		{
			if(eStartRay != null)
			{
				eStartRay(null, null);
			}
		}


		void OnEndRay(object sender, EventArgs e)
		{
			if(eEndRay != null)
			{
				eEndRay(null, null);
			}
		}


		void OnRepeatRay(object sender, EventArgs e)
		{
			if(eRepeatRay != null)
			{
				eRepeatRay(null, null);
			}
		}


		internal void PrintToConsole(string text)
		{
			AppendTextBox(ConsoleOut, text);
		}

		delegate void AppendTextDel(TextBox tb, string txt);

		void AppendTextBox(TextBox tbox, string str)
		{
			if(tbox.InvokeRequired)
			{
				AppendTextDel	appText	=delegate(TextBox tb, string txt) { tb.AppendText(txt); };

				object	[]pms	=new object[2];

				pms[0]	=tbox;
				pms[1]	=str;

				tbox.Invoke(appText, pms);
			}
			else
			{
				tbox.AppendText(str);
			}
		}
	}
}
