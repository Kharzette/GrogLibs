using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;


namespace SharedForms
{
	class DataGridViewColorCell : DataGridViewCell
	{
		protected override void Paint(Graphics graphics, Rectangle clipBounds,
			Rectangle cellBounds, int rowIndex, DataGridViewElementStates cellState,
			object value, object formattedValue, string errorText,
			DataGridViewCellStyle cellStyle,
			DataGridViewAdvancedBorderStyle advancedBorderStyle,
			DataGridViewPaintParts paintParts)
		{
			base.Paint(graphics, clipBounds, cellBounds, rowIndex,
				cellState, value, formattedValue, errorText, cellStyle,
				advancedBorderStyle, paintParts);

			Microsoft.Xna.Framework.Color	col
				=(Microsoft.Xna.Framework.Color)value;

			Color		fcol	=ConvertColor(col);
			SolidBrush	b		=new SolidBrush(fcol);

			cellBounds.Width--;
			cellBounds.Height--;

			graphics.FillRectangle(b, cellBounds);
		}


		internal static Microsoft.Xna.Framework.Color ConvertColor(Color val)
		{
			return	new	Microsoft.Xna.Framework.Color(val.R, val.G, val.B, val.A);
		}


		internal static Color ConvertColor(Microsoft.Xna.Framework.Color val)
		{
			return	Color.FromArgb(val.A, val.R, val.G, val.B);
		}
	}
	
	
	class DataGridViewColorColumn : DataGridViewColumn
	{
		public DataGridViewColorColumn()
		{
			this.CellTemplate	=new DataGridViewColorCell();
			this.ReadOnly		=true;
		}
	}
}
