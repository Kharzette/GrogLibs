using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace UtilityLib
{
	public static class Mathery
	{
		public const float		ON_EPSILON		=0.1f;
		public const float		NORMAL_EPSILON	=0.00001f;
		public const float		DIST_EPSILON	=0.01f;
		public const float		ANGLE_EPSILON	=0.00001f;
		public const float		VCompareEpsilon	=0.001f;
		public const float		MIN_MAX_BOUNDS	=15192.0f;
		public static Vector3	[]AxialNormals	=new Vector3[6];


		static Mathery()
		{
			AxialNormals[0]	=Vector3.UnitX;
			AxialNormals[1]	=Vector3.UnitY;
			AxialNormals[2]	=Vector3.UnitZ;
			AxialNormals[3]	=-Vector3.UnitX;
			AxialNormals[4]	=-Vector3.UnitY;
			AxialNormals[5]	=-Vector3.UnitZ;
		}


		//extension methods don't work on value types :(
		public static void ClearBoundingBox(ref BoundingBox bb)
		{
			bb.Min	=Vector3.One * MIN_MAX_BOUNDS;
			bb.Max	=-bb.Min;
		}


		public static void AddPointToBoundingBox(ref BoundingBox bb, Vector3 pnt)
		{
			if(pnt.X < bb.Min.X)
			{
				bb.Min.X	=pnt.X;
			}
			if(pnt.X > bb.Max.X)
			{
				bb.Max.X	=pnt.X;
			}
			if(pnt.Y < bb.Min.Y)
			{
				bb.Min.Y	=pnt.Y;
			}
			if(pnt.Y > bb.Max.Y)
			{
				bb.Max.Y	=pnt.Y;
			}
			if(pnt.Z < bb.Min.Z)
			{
				bb.Min.Z	=pnt.Z;
			}
			if(pnt.Z > bb.Max.Z)
			{
				bb.Max.Z	=pnt.Z;
			}
		}


		public static Color RandomColor(Random rnd)
		{
			Microsoft.Xna.Framework.Color	randColor
				=new Microsoft.Xna.Framework.Color(
						Convert.ToByte(rnd.Next(255)),
						Convert.ToByte(rnd.Next(255)),
						Convert.ToByte(rnd.Next(255)));
			return	randColor;
		}


		//for radians use mathhelper
		public static void WrapAngleDegrees(ref float angle)
		{
			while(angle < 0.0f)
			{
				angle	+=360.0f;
			}
			while(angle > 360.0f)
			{
				angle	-=360.0f;
			}
		}


		public static bool IsAxial(Vector3 v)
		{
			foreach(Vector3 ax in AxialNormals)
			{
				if(CompareVectorEpsilon(ax, v, 0.001f))
				{
					return	true;
				}
			}
			return	false;
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


		public static float VecIdx(Vector3 v, UInt32 idx)
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


		public static bool CompareVector(Vector3 v1, Vector3 v2)
		{
			return	CompareVectorEpsilon(v1, v2, VCompareEpsilon);
		}


		public static bool CompareVectorABS(Vector3 v1, Vector3 v2, float epsilon)
		{
			v1.X	=Math.Abs(v1.X);
			v1.Y	=Math.Abs(v1.Y);
			v1.Z	=Math.Abs(v1.Z);
			v2.X	=Math.Abs(v2.X);
			v2.Y	=Math.Abs(v2.Y);
			v2.Z	=Math.Abs(v2.Z);

			return	CompareVectorEpsilon(v1, v2, epsilon);
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


		public static void SnapVector(ref Vector3 vec)
		{
			for(int i=0;i < 3;i++)
			{
				float	vecElement	=VecIdx(vec, i);
				vecElement	=Math.Abs(vecElement - 1.0f);
				if(vecElement < ANGLE_EPSILON)
				{
					vec	=Vector3.Zero;
					VecIdxAssign(ref vec, i, 1.0f);
					break;
				}

				vecElement	=VecIdx(vec, i);
				vecElement	=Math.Abs(vecElement - -1.0f);
				if(vecElement < ANGLE_EPSILON)
				{
					vec	=Vector3.Zero;
					VecIdxAssign(ref vec, i, -1.0f);
					break;
				}
			}
		}


		public static void PlaneFromVerts(List<Vector3> verts, out Vector3 norm, out float dist)
		{
			int	i;

			norm	=Vector3.Zero;

			for(i=0;i < verts.Count;i++)
			{
				//gen a plane normal from the cross of edge vectors
				Vector3	v1  =verts[i] - verts[(i + 1) % verts.Count];
				Vector3	v2  =verts[(i + 2) % verts.Count] - verts[(i + 1) % verts.Count];

				norm   =Vector3.Cross(v1, v2);

				if(!norm.Equals(Vector3.Zero))
				{
					break;
				}
				//try the next three if there are three
			}
			if(i >= verts.Count)
			{
				norm	=Vector3.UnitX;
				dist	=0.0f;
				return;
			}

			norm.Normalize();
			dist	=Vector3.Dot(verts[1], norm);
		}


		public static bool TryParse(string str, out float val)
		{
#if XBOX
			try
			{
				val	=float.Parse(str);
				return	true;
			}
			catch
			{
				val	=float.NaN;
				return	false;
			}
#else
			return	float.TryParse(str, out val);
#endif
		}


		public static bool TryParse(string str, out int val)
		{
#if XBOX
			try
			{
				val	=int.Parse(str);
				return	true;
			}
			catch
			{
				val	=0;
				return	false;
			}
#else
			return	int.TryParse(str, out val);
#endif
		}


		public static bool TryParse(string str, out UInt32 val)
		{
#if XBOX
			try
			{
				val	=UInt32.Parse(str);
				return	true;
			}
			catch
			{
				val	=0;
				return	false;
			}
#else
			return	UInt32.TryParse(str, out val);
#endif
		}


		public static bool TryParse(string str, out bool val)
		{
#if XBOX
			try
			{
				val	=bool.Parse(str);
				return	true;
			}
			catch
			{
				val	=false;
				return	false;
			}
#else
			return	bool.TryParse(str, out val);
#endif
		}
	}
}