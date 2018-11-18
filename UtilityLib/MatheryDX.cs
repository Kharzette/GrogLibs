using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX;
using SharpDX.Direct3D11;


namespace UtilityLib
{
	public static partial class Mathery
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


		//Vector extension methods
		public static float dot(this Half4 aVec, Vector3 bVec)
		{
			Vector3	v3a	=Vector3.Zero;

			v3a.X	=aVec.X;
			v3a.Y	=aVec.Y;
			v3a.Z	=aVec.Z;

			return	v3a.dot(bVec);
		}


		public static float dot(this Vector3 aVec, Vector3 bVec)
		{
			return	Vector3.Dot(aVec, bVec);
		}


		public static float Distance(this Vector3 aVec, Vector3 bVec)
		{
			return	Vector3.Distance(aVec, bVec);
		}


		//flip negatives
		public static Vector3 Positive(this Vector3 vec)
		{
			Vector3	ret	=vec;

			if(ret.X < 0)
			{
				ret.X	=-ret.X;
			}
			if(ret.Y < 0)
			{
				ret.Y	=-ret.Y;
			}
			if(ret.Z < 0)
			{
				ret.Z	=-ret.Z;
			}
			return	ret;
		}


		public static Vector3 XYZ(this Vector4 v4)
		{
			Vector3	ret	=Vector3.Zero;

			ret.X	=v4.X;
			ret.Y	=v4.Y;
			ret.Z	=v4.Z;

			return	ret;
		}


		public static Vector4 MulXYZ(this Vector4 v4, float scalar)
		{
			Vector4	ret	=v4;

			ret.X	*=scalar;
			ret.Y	*=scalar;
			ret.Z	*=scalar;

			return	ret;
		}


		public static Vector4 ToV4(this Vector3 v3, float wVal)
		{
			Vector4	ret	=Vector4.Zero;

			ret.X	=v3.X;
			ret.Y	=v3.Y;
			ret.Z	=v3.Z;
			ret.W	=wVal;

			return	ret;
		}


		//handy for printing
		public static string IntStr(this Vector3 vec)
		{
			return	"" + (int)vec.X + ", " + (int)vec.Y + ", " + (int)vec.Z;
		}


		//extension methods don't work on value types :(
		public static void ClearBoundingBox(ref BoundingBox bb)
		{
			bb.Minimum	=Vector3.One * MIN_MAX_BOUNDS;
			bb.Maximum	=-bb.Minimum;
		}


		public static float BoxPlaneDistance(this BoundingBox bb, Vector3 planeNormal)
		{
			Vector3	posNorm	=planeNormal.Positive();

			return	Vector3.Dot(posNorm, bb.Maximum);
		}


		//find segment between two lines
		//from Paul Bourke's site
		public static bool ShortestLineBetweenTwoLines(Vector3 A, Vector3 B, Vector3 C, Vector3 D,
			out Vector3 shortA, out Vector3 shortB)
		{
			Vector3	p13, p43, p21;
			float	d1343, d4321, d1321, d4343, d2121;
			float	numer, denom;

			shortA	=Vector3.Zero;
			shortB	=Vector3.Zero;

			p13	=A - C;
			p43	=D - C;
			
			if(Math.Abs(p43.X) < 0.001f
				&& Math.Abs(p43.Y) < 0.001f
				&& Math.Abs(p43.Z) < 0.001f)
			{
				return	false;
			}

			p21	=B - A;
			
			if(Math.Abs(p21.X) < 0.001f
				&& Math.Abs(p21.Y) < 0.001f
				&& Math.Abs(p21.Z) < 0.001f)
			{
				return	false;
			}
			
			d1343	=Vector3.Dot(p13, p43);
			d4321	=Vector3.Dot(p43, p21);
			d1321	=Vector3.Dot(p13, p21);
			d4343	=Vector3.Dot(p43, p43);
			d2121	=Vector3.Dot(p21, p21);
			
			denom	=d2121 * d4343 - d4321 * d4321;

			if(Math.Abs(denom) < 0.001f)
			{
				return	false;
			}
			
			numer	=d1343 * d4321 - d1321 * d4343;
			
			float	mua	=numer / denom;
			float	mub	=(d1343 + d4321 * (mua)) / d4343;

			shortA	=A + (p21 * mua);
			shortB	=C + (p43 * mub);

			return	true;
		}


		static Vector3 VectorForCubeFace(TextureCubeFace face)
		{
			switch(face)
			{
				case	TextureCubeFace.NegativeX:
					return	-Vector3.UnitX;
				case	TextureCubeFace.NegativeY:
					return	-Vector3.UnitY;
				case	TextureCubeFace.NegativeZ:
					return	-Vector3.UnitZ;
				case	TextureCubeFace.PositiveX:
					return	Vector3.UnitX;
				case	TextureCubeFace.PositiveY:
					return	Vector3.UnitY;
				case	TextureCubeFace.PositiveZ:
					return	Vector3.UnitZ;
			}
			Debug.Assert(false);
			return	Vector3.One;
		}


		//direction points at a cube face
		public static void CreateCubeMapViewProjMatrix(TextureCubeFace face,
			Vector3 cubeCenter, float farPlane,
			out Matrix cubeView, out Matrix cubeProj)
		{
			//find a good up vector
			Vector3	upVec	=Vector3.UnitY;
			if(face == TextureCubeFace.NegativeY)
			{
				upVec	=Vector3.UnitZ;
			}
			else if(face == TextureCubeFace.PositiveY)
			{
				upVec	=-Vector3.UnitZ;
			}

			//Create the view matrix aimed at the cube face
			cubeView	=Matrix.LookAtLH(cubeCenter,
				cubeCenter + VectorForCubeFace(face), upVec);

			//Values are to project on the cube face
			cubeProj	=Matrix.PerspectiveFovLH(
				MathUtil.PiOverTwo, 1f, 1f, farPlane);
        }


		//useful for directional shadow mapping, making item icons
		//from a rendertarget, or even mini maps
		public static void CreateBoundedDirectionalOrthoViewProj(
			BoundingBox bounds, Vector3 direction,
			out Matrix lightView, out Matrix lightProj, out Vector3 fakeOrigin)
		{
			//create a matrix aimed in the direction
			Matrix	dirAim	=Matrix.LookAtLH(Vector3.Zero, -direction, Vector3.Up);

			//Get the corners
			Vector3[]	boxCorners	=bounds.GetCorners();

			//Transform the positions of the corners into the direction
			for(int i=0;i < boxCorners.Length;i++)
			{
				boxCorners[i]	=Vector3.TransformCoordinate(boxCorners[i], dirAim);
			}
			
			//Find the smallest box around the points
			//in the directional frame of reference
			BoundingBox	dirSpaceBox	=BoundingBox.FromPoints(boxCorners);

			Vector3	boxSize		=dirSpaceBox.Maximum - dirSpaceBox.Minimum;
			Vector3	halfBoxSize	=boxSize * 0.5f;
			
			//The viewpoint should be in the center of the back
			//panel of the box.
			Vector3	viewPos	=dirSpaceBox.Minimum + halfBoxSize;
			
			viewPos.Z	=dirSpaceBox.Minimum.Z;
			
			//transform the view position back into worldspace
			viewPos	=Vector3.TransformCoordinate(viewPos, Matrix.Invert(dirAim));
			
			//Create the view matrix
			lightView	=Matrix.LookAtLH(viewPos,
				viewPos - direction, Vector3.Up);

			//Create the ortho projection matrix
			lightProj	=Matrix.OrthoLH(
					boxSize.X, boxSize.Y, -boxSize.Z, boxSize.Z);

			//get a good fake position for use in shader distance calcs
			fakeOrigin	=viewPos;
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
			if(pnt.X < bb.Minimum.X)
			{
				bb.Minimum.X	=pnt.X;
			}
			if(pnt.X > bb.Maximum.X)
			{
				bb.Maximum.X	=pnt.X;
			}
			if(pnt.Y < bb.Minimum.Y)
			{
				bb.Minimum.Y	=pnt.Y;
			}
			if(pnt.Y > bb.Maximum.Y)
			{
				bb.Maximum.Y	=pnt.Y;
			}
			if(pnt.Z < bb.Minimum.Z)
			{
				bb.Minimum.Z	=pnt.Z;
			}
			if(pnt.Z > bb.Maximum.Z)
			{
				bb.Maximum.Z	=pnt.Z;
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


		public static void TinyToZero(ref Vector3 val, float amount)
		{
			if(val.X < amount && val.X > -amount)
			{
				val.X	=0.0f;
			}
			if(val.Y < amount && val.Y > -amount)
			{
				val.Y	=0.0f;
			}
			if(val.Z < amount && val.Z > -amount)
			{
				val.Z	=0.0f;
			}
		}


		//range should be the value to keep the position within
		//positive or negative.  Like 5 10 15 would return a
		//vector within plus or minus 5 x, plus or minus 10 y,
		//and plus or minus 15 z
		public static Vector3 RandomPosition(Random rnd, Vector3 range)
		{
			Vector3	ret	=Vector3.Zero;

			ret.X	=(float)rnd.NextDouble(-range.X, range.X);
			ret.Y	=(float)rnd.NextDouble(-range.Y, range.Y);
			ret.Z	=(float)rnd.NextDouble(-range.Z, range.Z);

			return	ret;
		}


		public static float RandomFloatNext(Random rnd, float min, float max)
		{
			double	d	=rnd.NextDouble();

			d	*=(max - min);
			d	+=min;

			return	(float)d;
		}


		public static Color RandomColor(Random rnd)
		{
			Color	randColor
				=new Color(Convert.ToByte(rnd.Next(255)),
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


		//flat, good for mobiles
		public static Vector3 RandomDirectionXZ(Random rnd)
		{
			Vector3	ret	=Vector3.Zero;

			for(;;)
			{
				ret.X	=(float)rnd.NextDouble();
				ret.Y	=0.5f;	//will go to zero
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


		public static Vector4 RandomColorVector4(Random rnd)
		{
			Vector4	ret	=Vector4.Zero;

			ret.X	=(float)rnd.NextDouble();
			ret.Y	=(float)rnd.NextDouble();
			ret.Z	=(float)rnd.NextDouble();
			ret.W	=(float)rnd.NextDouble();

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


		public static void WrapAngleDegrees(ref Half angle)
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


		public static bool CompareMatrix(Matrix mat1, Matrix mat2, float epsilon)
		{
			if(!CompareVectorEpsilon(mat1.Forward, mat2.Forward, epsilon))
			{
				return	false;
			}

			if(!CompareVectorEpsilon(mat1.Left, mat2.Left, epsilon))
			{
				return	false;
			}

			if(!CompareVectorEpsilon(mat1.Up, mat2.Up, epsilon))
			{
				return	false;
			}

			if(!CompareVectorEpsilon(mat1.TranslationVector, mat2.TranslationVector, epsilon))
			{
				return	false;
			}
			return	true;
		}


		public static bool IsIdentity(Matrix mat, float epsilon)
		{
			return	CompareMatrix(mat, Matrix.Identity, epsilon);
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


		public static bool CompareVectorEpsilon(Vector2 v1, Vector2 v2, float epsilon)
		{
			if((v1.X - v2.X) < -epsilon || (v1.X - v2.X) > epsilon)
			{
				return	false;
			}
			if((v1.Y - v2.Y) < -epsilon || (v1.Y - v2.Y) > epsilon)
			{
				return	false;
			}
			return	true;
		}


		public static bool CompareFloatEpsilon(float f1, float f2, float epsilon)
		{
			if((f1 - f2) < -epsilon || (f1 - f2) > epsilon)
			{
				return	false;
			}
			return	true;
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


		public static bool CompareFloat(float f1, float f2)
		{
			return	CompareFloatEpsilon(f1, f2, VCompareEpsilon);
		}


		public static bool CompareVector(Vector3 v1, Vector3 v2)
		{
			return	CompareVectorEpsilon(v1, v2, VCompareEpsilon);
		}


		public static bool CompareVector(Vector2 v1, Vector2 v2)
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


		public static void SnapVector(ref Vector3 vec)
		{
			for(int i=0;i < 3;i++)
			{
				float	vecElement	=vec[i];
				vecElement	=Math.Abs(vecElement - 1.0f);
				if(vecElement < ANGLE_EPSILON)
				{
					vec		=Vector3.Zero;
					vec[i]	=1f;
					break;
				}

				vecElement	=vec[i];
				vecElement	=Math.Abs(vecElement - -1.0f);
				if(vecElement < ANGLE_EPSILON)
				{
					vec	=Vector3.Zero;
					vec[i]	=-1.0f;
					break;
				}
			}
		}


		public static void PointsFromPlane(Vector3 norm, float dist,
			out Vector3 p0, out Vector3 p1, out Vector3 p2, out Vector3 p3)
		{
			//generate some axis vecs
			Vector3	sAxis	=Vector3.Cross(norm, Vector3.UnitY);
			if(sAxis.LengthSquared() <= 0.0f)
			{
				sAxis	=-Vector3.Cross(norm, Vector3.UnitZ);
			}

			Vector3	tAxis	=Vector3.Cross(norm, sAxis);

			sAxis.Normalize();
			tAxis.Normalize();

			p0	=p1	=p2	=p3 =norm * dist;

			p0	-=sAxis * MIN_MAX_BOUNDS;
			p0	-=tAxis * MIN_MAX_BOUNDS;

			p1	+=sAxis * MIN_MAX_BOUNDS;
			p1	-=tAxis * MIN_MAX_BOUNDS;

			p2	+=sAxis * MIN_MAX_BOUNDS;
			p2	+=tAxis * MIN_MAX_BOUNDS;

			p3	-=sAxis * MIN_MAX_BOUNDS;
			p3	+=tAxis * MIN_MAX_BOUNDS;
		}


		public static void PointsFromPlane(Vector3 norm, float dist,
			out Vector3 p0, out Vector3 p1, out Vector3 p2)
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

			float	dist;

			bool	bIntersects	=box.Intersects(ref ray, out dist);

			if(!bIntersects)
			{
				return	null;
			}

			if(dist <= len)
			{
				return	dist;
			}
			return	null;
		}


		//returns the largest axis value
		public static float GreatestSphereDimension(Vector3 vec)
		{
			vec.X	=Math.Abs(vec.X);
			vec.Y	=Math.Abs(vec.Y);
			vec.Z	=Math.Abs(vec.Z);

			if(vec.X > vec.Y)
			{
				if(vec.X > vec.Z)
				{
					return	vec.X;
				}
				else
				{
					return	vec.Z;
				}
			}
			else
			{
				if(vec.Y > vec.Z)
				{
					return	vec.Y;
				}
				else
				{
					return	vec.Z;
				}
			}
		}


		public static BoundingSphere TransformSphere(Matrix trans, BoundingSphere bs)
		{
			Vector3	pos		=bs.Center;

			bs.Center	=Vector3.TransformCoordinate(pos, trans);
				
			//use the greatest dimension
			bs.Radius	*=GreatestSphereDimension(trans.ScaleVector);

			return	bs;
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

			float	dist;

			bool	bIntersects	=sphere.Intersects(ref ray, out dist);

			if(!bIntersects)
			{
				return	null;
			}

			if(dist <= len)
			{
				return	dist;
			}
			return	null;
		}


		public static bool IsBoundingBoxCentered(BoundingBox box)
		{
			Vector3	delta	=CenterBoundingBoxAtOrigin(ref box);

			return	(delta == Vector3.Zero);
		}


		public static Vector3 CenterBoundingBoxAtOrigin(ref BoundingBox box)
		{
			BoundingBox	start	=box;

			float	xsize	=box.Maximum.X - box.Minimum.X;
			float	zsize	=box.Maximum.Z - box.Minimum.Z;
			float	height	=box.Maximum.Y - box.Minimum.Y;

			xsize	*=0.5f;
			zsize	*=0.5f;
			height	*=0.5f;

			box.Minimum.X	=-xsize;
			box.Maximum.X	=xsize;
			box.Minimum.Z	=-zsize;
			box.Maximum.Z	=zsize;
			box.Minimum.Y	=-height;
			box.Maximum.Y	=height;

			return	start.Minimum - box.Minimum;
		}


		public static Vector4 ClampVector(Vector4 val, Vector4 min, Vector4 max)
		{
			Vector4	ret	=Vector4.Zero;

			ret.X	=MathUtil.Clamp(val.X, min.X, max.X);
			ret.Y	=MathUtil.Clamp(val.Y, min.Y, max.Y);
			ret.Z	=MathUtil.Clamp(val.Z, min.Z, max.Z);
			ret.W	=MathUtil.Clamp(val.W, min.W, max.W);

			return	ret;
		}


		public static void ClipRayToBox(BoundingBox box,
			ref Vector3 rayStart, ref Vector3 rayEnd)
		{
			if(rayStart.X < box.Minimum.X)
			{
				rayStart.X	=box.Minimum.X;
			}
			if(rayStart.Y < box.Minimum.Y)
			{
				rayStart.Y	=box.Minimum.Y;
			}
			if(rayStart.Z < box.Minimum.Z)
			{
				rayStart.Z	=box.Minimum.Z;
			}

			if(rayStart.X > box.Maximum.X)
			{
				rayStart.X	=box.Maximum.X;
			}
			if(rayStart.Y > box.Maximum.Y)
			{
				rayStart.Y	=box.Maximum.Y;
			}
			if(rayStart.Z > box.Maximum.Z)
			{
				rayStart.Z	=box.Maximum.Z;
			}

			if(rayEnd.X < box.Minimum.X)
			{
				rayEnd.X	=box.Minimum.X;
			}
			if(rayEnd.Y < box.Minimum.Y)
			{
				rayEnd.Y	=box.Minimum.Y;
			}
			if(rayEnd.Z < box.Minimum.Z)
			{
				rayEnd.Z	=box.Minimum.Z;
			}

			if(rayEnd.X > box.Maximum.X)
			{
				rayEnd.X	=box.Maximum.X;
			}
			if(rayEnd.Y > box.Maximum.Y)
			{
				rayEnd.Y	=box.Maximum.Y;
			}
			if(rayEnd.Z > box.Maximum.Z)
			{
				rayEnd.Z	=box.Maximum.Z;
			}
		}


		//uses the add up the angles trick to determine point in poly
		public static float ComputeAngleSum(Vector3 point, List<Vector3> verts)
		{
			float	dotSum	=0f;
			for(int i=0;i < verts.Count;i++)
			{
				int	vIdx0	=i;
				int	vIdx1	=((i + 1) % verts.Count);

				Vector3	v1	=verts[vIdx0] - point;
				Vector3	v2	=verts[vIdx1] - point;

				float	len1	=v1.Length();
				float	len2	=v2.Length();

				if((len1 * len2) < 0.0001f)
				{
					return	MathUtil.TwoPi;
				}

				v1	/=len1;
				v2	/=len2;

				float	dot	=Vector3.Dot(v1, v2);

				if(dot > 1f)
				{
					dot	=1f;
				}

				dotSum	+=(float)Math.Acos(dot);
			}
			return	dotSum;
		}
	}
}