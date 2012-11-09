using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics.PackedVector;


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


		public static bool InFrustumLightAdjust(BoundingFrustum frust, Vector3 lightDir, BoundingSphere bs)
		{
			//extend down light ray 5 units
			bs.Center	+=(lightDir * 5.0f);

			//expand radius by 10
			bs.Radius	+=10;			

			ContainmentType	ct	=frust.Contains(bs);

			if(ct == ContainmentType.Disjoint)
			{
				return	false;
			}
			return	true;
		}


		public static BoundingSphere SphereFromPoints(IEnumerable<Vector3> points)
		{
			BoundingSphere	ret;

			ret.Center	=Vector3.Zero;
			ret.Radius	=0.0f;

			//find min and max
			Vector3	min	=Vector3.One * float.MaxValue;
			Vector3	max	=Vector3.One * float.MinValue;
			foreach(Vector3 pnt in points)
			{
				if(pnt.X < min.X)
				{
					min.X	=pnt.X;
				}
				if(pnt.Y < min.Y)
				{
					min.Y	=pnt.Y;
				}
				if(pnt.Z < min.Z)
				{
					min.Z	=pnt.Z;
				}

				if(pnt.X > max.X)
				{
					max.X	=pnt.X;
				}
				if(pnt.Y > max.Y)
				{
					max.Y	=pnt.Y;
				}
				if(pnt.Z > max.Z)
				{
					max.Z	=pnt.Z;
				}
			}

			ret.Center	=min + (max - min) / 2.0f;

			foreach(Vector3 pnt in points)
			{
				float	dist	=Vector3.Distance(pnt, ret.Center);
				if(dist > ret.Radius)
				{
					ret.Radius	=dist;
				}
			}

			return	ret;
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


		public static void TinyToZero(ref Vector3 val)
		{
			if(val.X < VCompareEpsilon && val.X > -VCompareEpsilon)
			{
				val.X	=0.0f;
			}
			if(val.Y < VCompareEpsilon && val.Y > -VCompareEpsilon)
			{
				val.Y	=0.0f;
			}
			if(val.Z < VCompareEpsilon && val.Z > -VCompareEpsilon)
			{
				val.Z	=0.0f;
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


		public static Vector3 RandomDirection(Random rnd)
		{
			Vector3	ret	=Vector3.Zero;

			for(;;)
			{
				ret.X	=(float)rnd.NextDouble();
				ret.Y	=(float)rnd.NextDouble();
				ret.Z	=(float)rnd.NextDouble();

				ret	-=(Vector3.One * 0.5f);

				ret.Normalize();

				if(!float.IsNaN(ret.X))
				{
					break;
				}
			}
			return	ret;
		}


		public static Vector3 RandomColorVector(Random rnd)
		{
			Vector3	ret	=Vector3.Zero;

			ret.X	=(float)rnd.NextDouble();
			ret.Y	=(float)rnd.NextDouble();
			ret.Z	=(float)rnd.NextDouble();

			return	ret;
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


		public static void WrapAngleDegrees(ref Int16 angle)
		{
			while(angle < 0)
			{
				angle	+=360;
			}
			while(angle > 360)
			{
				angle	-=360;
			}
		}


		public static void WrapAngleDegrees(ref HalfSingle angle)
		{
			float	ang	=angle.ToSingle();
			while(ang < 0.0f)
			{
				ang	+=360.0f;
			}
			while(ang > 360.0f)
			{
				ang	-=360.0f;
			}
			angle	=new HalfSingle(ang);
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


		public static void PointsFromPlane(Vector3 norm, float dist, out Vector3 p0, out Vector3 p1, out Vector3 p2)
		{
			//generate some axis vecs
			Vector3	sAxis	=Vector3.Cross(norm, Vector3.UnitY);
			if(sAxis.LengthSquared() <= 0.0f)
			{
				sAxis	=-Vector3.Cross(norm, Vector3.UnitZ);
			}

			Vector3	tAxis	=Vector3.Cross(norm, sAxis);

			p0	=p1	=p2	=norm * dist;

			p2	+=tAxis * 30f;
			p1	-=tAxis * 15f;
			p1	-=sAxis * 15f;
			p0	-=tAxis * 15f;
			p0	+=sAxis * 15f;
		}


		public static void PlaneFromVerts(Vector3 v0, Vector3 v1, Vector3 v2, out Vector3 norm, out float dist)
		{
			norm	=Vector3.Zero;

			//gen a plane normal from the cross of edge vectors
			Vector3	e1  =v0 - v1;
			Vector3	e2  =v2 - v1;

			norm   =Vector3.Cross(e1, e2);

			if(norm.LengthSquared() <= 0f)
			{
				norm	=Vector3.UnitX;
				dist	=0.0f;
				return;
			}

			norm.Normalize();
			dist	=Vector3.Dot(v1, norm);
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


		public static float? RayIntersectBox(Vector3 start, Vector3 end, BoundingBox box)
		{
			Ray	ray;

			ray.Position	=start;
			ray.Direction	=(end - start);

			//keep the length
			float	len	=ray.Direction.Length();

			//normalize
			ray.Direction	/=len;

			Nullable<float>	dist	=box.Intersects(ray);

			if(dist == null)
			{
				return	null;
			}
			if(dist <= len)
			{
				return	dist;
			}
			return	null;
		}


		public static float? RayIntersectSphere(Vector3 start, Vector3 end, BoundingSphere sphere)
		{
			Ray	ray;

			ray.Position	=start;
			ray.Direction	=(end - start);

			//keep the length
			float	len	=ray.Direction.Length();

			//normalize
			ray.Direction	/=len;

			Nullable<float>	dist	=sphere.Intersects(ray);

			if(dist == null)
			{
				return	null;
			}
			if(dist <= len)
			{
				return	dist;
			}
			return	null;
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
//			return	float.TryParse(str, out val);
			return	float.TryParse(str,
				System.Globalization.NumberStyles.Float,
				System.Globalization.CultureInfo.InvariantCulture, out val);
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