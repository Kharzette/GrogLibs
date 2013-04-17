using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;


namespace UtilityLib
{
	public static partial class Misc
	{
		public static Vector4 ARGBToVector4(int argb)
		{
			Vector4	ret	=Vector4.Zero;

			ret.X	=((float)((argb & 0x00ff0000) >> 16) / 255f);
			ret.Y	=((float)((argb & 0x0000ff00) >> 8) / 255f);
			ret.Z	=((float)(argb & 0x000000ff) / 255f);
			ret.W	=((float)((argb & 0xff000000) >> 24) / 255f);

			return	ret;
		}


		public static int Vector4ToARGB(Vector4 vecColor)
		{
			int	argb	=(int)(vecColor.W * 255f) << 24;
			argb		|=(int)(vecColor.X * 255f) << 16;
			argb		|=(int)(vecColor.Y * 255f) << 8;
			argb		|=(int)(vecColor.Z * 255f);

			return	argb;
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


		public static Vector2 StringToVector2(string vstring)
		{
			Vector2	ret	=Vector2.Zero;

			string	[]elements	=vstring.Split(' ');
			
			Mathery.TryParse(elements[0], out ret.X);
			Mathery.TryParse(elements[1], out ret.Y);

			return	ret;
		}


		public static Vector3 StringToVector3(string vstring)
		{
			Vector3	ret	=Vector3.Zero;

			string	[]elements	=vstring.Split(' ');
			
			Mathery.TryParse(elements[0], out ret.X);
			Mathery.TryParse(elements[1], out ret.Y);
			Mathery.TryParse(elements[2], out ret.Z);

			return	ret;
		}


		public static Vector4 StringToVector4(string vstring)
		{
			Vector4	ret	=Vector4.Zero;

			string	[]elements	=vstring.Split(' ');
			
			Mathery.TryParse(elements[0], out ret.X);
			Mathery.TryParse(elements[1], out ret.Y);
			Mathery.TryParse(elements[2], out ret.Z);
			Mathery.TryParse(elements[3], out ret.W);

			return	ret;
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


		//returns a centered box
		public static BoundingBox MakeBox(float width, float height)
		{
			return	MakeBox(width, height, width);
		}


		//returns a centered box
		public static BoundingBox MakeBox(float width, float height, float depth)
		{
			BoundingBox	ret;

			float	halfWidth	=width * 0.5f;
			float	halfHeight	=height * 0.5f;
			float	halfDepth	=depth * 0.5f;

			ret.Min.X	=-halfWidth;
			ret.Max.X	=halfWidth;
			
			ret.Min.Y	=-halfHeight;
			ret.Max.Y	=halfHeight;

			ret.Min.Z	=-halfDepth;
			ret.Max.Z	=halfDepth;

			return	ret;
		}
	}
}
