using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;
using SharpDX.Direct3D11;


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

			if(elements.Length != 3)
			{
				return	ret;
			}
			
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


		public static Matrix StringToMatrix(string vstring)
		{
			Matrix	ret	=Matrix.Identity;

			string	[]elements	=vstring.Split(' ');
			
			Mathery.TryParse(elements[0], out ret.M11);
			Mathery.TryParse(elements[1], out ret.M12);
			Mathery.TryParse(elements[2], out ret.M13);
			Mathery.TryParse(elements[3], out ret.M14);
			Mathery.TryParse(elements[4], out ret.M21);
			Mathery.TryParse(elements[5], out ret.M22);
			Mathery.TryParse(elements[6], out ret.M23);
			Mathery.TryParse(elements[7], out ret.M24);
			Mathery.TryParse(elements[8], out ret.M31);
			Mathery.TryParse(elements[9], out ret.M32);
			Mathery.TryParse(elements[10], out ret.M33);
			Mathery.TryParse(elements[11], out ret.M34);
			Mathery.TryParse(elements[12], out ret.M41);
			Mathery.TryParse(elements[13], out ret.M42);
			Mathery.TryParse(elements[14], out ret.M43);
			Mathery.TryParse(elements[15], out ret.M44);

			return	ret;
		}


		public static string FloatToString(float val)
		{
			return	val.ToString(System.Globalization.CultureInfo.InvariantCulture);
		}


		public static string FloatArrayToString(float []val)
		{
			string	ret	="";
			for(int i=0;i < val.Length;i++)
			{
				ret	+=val[i].ToString(System.Globalization.CultureInfo.InvariantCulture) + " ";
			}

			ret	=ret.TrimEnd(' ');

			return	ret;
		}


		public static string VectorToString(Vector3 vec, int numDecimalPlaces)
		{
			return	FloatToString(vec.X, numDecimalPlaces)
				+ " " + FloatToString(vec.Y, numDecimalPlaces)
				+ " " + FloatToString(vec.Z, numDecimalPlaces);
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


		public static string MatrixToString(Matrix mat)
		{
			return	mat.M11.ToString(System.Globalization.CultureInfo.InvariantCulture)
				+ " " + mat.M12.ToString(System.Globalization.CultureInfo.InvariantCulture)
				+ " " + mat.M13.ToString(System.Globalization.CultureInfo.InvariantCulture)
				+ " " + mat.M14.ToString(System.Globalization.CultureInfo.InvariantCulture)
				+ " " + mat.M21.ToString(System.Globalization.CultureInfo.InvariantCulture)
				+ " " + mat.M22.ToString(System.Globalization.CultureInfo.InvariantCulture)
				+ " " + mat.M23.ToString(System.Globalization.CultureInfo.InvariantCulture)
				+ " " + mat.M24.ToString(System.Globalization.CultureInfo.InvariantCulture)
				+ " " + mat.M31.ToString(System.Globalization.CultureInfo.InvariantCulture)
				+ " " + mat.M32.ToString(System.Globalization.CultureInfo.InvariantCulture)
				+ " " + mat.M33.ToString(System.Globalization.CultureInfo.InvariantCulture)
				+ " " + mat.M34.ToString(System.Globalization.CultureInfo.InvariantCulture)
				+ " " + mat.M41.ToString(System.Globalization.CultureInfo.InvariantCulture)
				+ " " + mat.M42.ToString(System.Globalization.CultureInfo.InvariantCulture)
				+ " " + mat.M43.ToString(System.Globalization.CultureInfo.InvariantCulture)
				+ " " + mat.M44.ToString(System.Globalization.CultureInfo.InvariantCulture);
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

			ret.Minimum.X	=-halfWidth;
			ret.Maximum.X	=halfWidth;
			
			ret.Minimum.Y	=-halfHeight;
			ret.Maximum.Y	=halfHeight;

			ret.Minimum.Z	=-halfDepth;
			ret.Maximum.Z	=halfDepth;

			return	ret;
		}


		public static Color SystemColorToDXColor(System.Drawing.Color col)
		{
			Color	ret;

			ret.R	=col.R;
			ret.G	=col.G;
			ret.B	=col.B;
			ret.A	=col.A;

			return	ret;
		}
	}
}
