using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;


namespace UtilityLib
{
	public static class Misc
	{
		public static void SafeInvoke(this EventHandler eh, object sender)
		{
			if(eh != null)
			{
				eh(sender, EventArgs.Empty);
			}
		}


		public static void SafeInvoke(this EventHandler eh, object sender, EventArgs ea)
		{
			if(eh != null)
			{
				eh(sender, ea);
			}
		}


		public static void SafeInvole<T>(this EventHandler<T> eh, object sender, T ea) where T : EventArgs
		{
			if(eh != null)
			{
				eh(sender, ea);
			}
		}


		public static Vector3 ColorNormalize(Vector3 inVec)
		{
			float	mag	=-696969;

			if(inVec.X > mag)
			{
				mag	=inVec.X;
			}
			if(inVec.Y > mag)
			{
				mag	=inVec.Y;
			}
			if(inVec.Z > mag)
			{
				mag	=inVec.Z;
			}
			return	inVec / mag;
		}


		public static string VectorToString(Vector3 vec)
		{
			return	vec.X.ToString(System.Globalization.CultureInfo.InvariantCulture)
				+ " " + vec.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)
				+ " " + vec.Z.ToString(System.Globalization.CultureInfo.InvariantCulture);
		}


		public static string VectorToString(Vector2 vec)
		{
			return	vec.X.ToString(System.Globalization.CultureInfo.InvariantCulture)
				+ " " + vec.Y.ToString(System.Globalization.CultureInfo.InvariantCulture);
		}


		public static string VectorToString(Vector4 vec)
		{
			return	vec.X.ToString(System.Globalization.CultureInfo.InvariantCulture)
				+ " " + vec.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)
				+ " " + vec.Z.ToString(System.Globalization.CultureInfo.InvariantCulture)
				+ " " + vec.W.ToString(System.Globalization.CultureInfo.InvariantCulture);
		}


		public static string AssignValue(string val)
		{
			if(val == null)
			{
				return	"";
			}
			return	val;
		}


#if !X64
		public static Color ModulateColour(Color a, Color b)
		{
			int	A	=a.A * b.A;
			int	R	=a.R * b.R;
			int	G	=a.G * b.G;
			int	B	=a.B * b.B;

			Color	ret	=Color.White;

			ret.A	=(byte)(A >> 8);
			ret.R	=(byte)(R >> 8);
			ret.G	=(byte)(G >> 8);
			ret.B	=(byte)(B >> 8);

			return	ret;
		}
#endif


		public static bool bFlagSet(UInt32 val, UInt32 flag)
		{
			return	((val & flag) != 0);
		}


		public static bool bFlagSet(Int32 val, Int32 flag)
		{
			return	((val & flag) != 0);
		}
	}
}
