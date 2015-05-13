using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;


namespace SharedForms
{
	public class PowerOfTwoUpDown : NumericUpDown
	{
		public override void UpButton()
		{
			int	intVal	=(int)Value;

			intVal	=Math.Min(intVal << 1, (int)Maximum);

			Text	="" + intVal;
		}

		public override void DownButton()
		{
			int	intVal	=(int)Value;

			intVal	=Math.Max(intVal >> 1, (int)Minimum);

			Text	="" + intVal;
		}

		protected override void UpdateEditText()
		{
			int	intVal	=(int)Value;
			int	pow	=0;
			while(intVal > 0)
			{
				intVal	>>=1;
				pow++;
			}
			intVal	=1 << (pow - 1);

			intVal	=Math.Min(intVal, (int)Maximum);
			intVal	=Math.Max(intVal, (int)Minimum);

			Text	="" + intVal;
			Value	=intVal;
		}
	}
}
