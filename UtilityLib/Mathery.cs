using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace UtilityLib
{
	public class Mathery
	{
		public const float VCompareEpsilon	=0.001f;


		public static Color RandomColor(Random rnd)
		{
			Microsoft.Xna.Framework.Graphics.Color	randColor
				=new Microsoft.Xna.Framework.Graphics.Color(
						Convert.ToByte(rnd.Next(255)),
						Convert.ToByte(rnd.Next(255)),
						Convert.ToByte(rnd.Next(255)));
			return	randColor;
		}


		public static float VecIdx(Vector3 v, int idx)
		{
			if(idx == 0)
			{
				return	v.X;
			}
			else if(idx == 1)
			{
				return	v.Y;
			}
			return	v.Z;
		}


		public static bool CompareVectorEpsilon(Vector3 v1, Vector3 v2, float epsilon)
		{
			if((v1.X - v2.X) < -epsilon || (v1.X - v2.X) > epsilon)
			{
				return	false;
			}
			if((v1.Y - v2.Y) < -epsilon || (v1.Y - v2.Y) > epsilon)
			{
				return	false;
			}
			if((v1.Z - v2.Z) < -epsilon || (v1.Z - v2.Z) > epsilon)
			{
				return	false;
			}
			return	true;
		}


		public static void VecIdxAssign(ref Vector3 v, int idx, float val)
		{
			if(idx == 0)
			{
				v.X	=val;
			}
			else if(idx == 1)
			{
				v.Y	=val;
			}
			else
			{
				v.Z	=val;
			}
		}
	}
}
