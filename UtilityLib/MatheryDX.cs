﻿using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using Vortice.Direct3D11;
using Vortice.Mathematics;
using System.Numerics;


namespace UtilityLib;

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
	public static float dot(this Vector3 aVec, Vector3 bVec)
	{
		return	Vector3.Dot(aVec, bVec);
	}


	//returns a + (b * scalar)
	public static Vector3 MAdd(Vector3 aVec, Vector3 bVec, float scalar)
	{
		Vector3	ret	=bVec * scalar;

		ret	+=aVec;

		return	ret;
	}


	public static Vector3 XYZ(this in Quaternion q)
	{
		return	new Vector3(q.X, q.Y, q.Z);
	}


	public static Vector3 Forward(this in Matrix4x4 mat)
	{
		return	new Vector3(mat.M31, mat.M32, mat.M33);
	}


	//this was in sharpdx, and I used it alot for boundy stuff
	//keeps things homogeneous
	public static void TransformCoordinate(Vector3 coord, ref Matrix4x4 mat, out Vector3 result)
	{
		Vector3	vec	=Vector3.Zero;

		vec.X	=(coord.X * mat.M11) + (coord.Y * mat.M21) + (coord.Z * mat.M31) + mat.M41;
		vec.Y	=(coord.X * mat.M12) + (coord.Y * mat.M22) + (coord.Z * mat.M32) + mat.M42;
		vec.Z	=(coord.X * mat.M13) + (coord.Y * mat.M23) + (coord.Z * mat.M33) + mat.M43;

		float	w	=1f / ((coord.X * mat.M14) + (coord.Y * mat.M24) + (coord.Z * mat.M34) + mat.M44);

		result	=Vector3.Multiply(vec, w);
	}


	//non ref matrix ver
	public static void TransformCoordinate(Vector3 coord, Matrix4x4 mat, out Vector3 result)
	{
		Vector3	vec	=Vector3.Zero;

		vec.X	=(coord.X * mat.M11) + (coord.Y * mat.M21) + (coord.Z * mat.M31) + mat.M41;
		vec.Y	=(coord.X * mat.M12) + (coord.Y * mat.M22) + (coord.Z * mat.M32) + mat.M42;
		vec.Z	=(coord.X * mat.M13) + (coord.Y * mat.M23) + (coord.Z * mat.M33) + mat.M43;

		float	w	=1f / ((coord.X * mat.M14) + (coord.Y * mat.M24) + (coord.Z * mat.M34) + mat.M44);

		result	=Vector3.Multiply(vec, w);
	}


	public static Vector3 TransformCoordinate(Vector3 coord, ref Matrix4x4 mat)
	{
		Vector3	ret;

		TransformCoordinate(coord, ref mat, out ret);

		return	ret;
	}


	//array ver
	public static void TransformCoordinate(Vector3 []coords, Matrix4x4 mat, Vector3 []results)
	{
		Debug.Assert(results.Length >= coords.Length);

		Parallel.For(0, coords.Length, (k) =>
		{
			Vector3	vec		=Vector3.Zero;
			Vector3	coord	=coords[k];

			vec.X	=(coord.X * mat.M11) + (coord.Y * mat.M21) + (coord.Z * mat.M31) + mat.M41;
			vec.Y	=(coord.X * mat.M12) + (coord.Y * mat.M22) + (coord.Z * mat.M32) + mat.M42;
			vec.Z	=(coord.X * mat.M13) + (coord.Y * mat.M23) + (coord.Z * mat.M33) + mat.M43;

			float	w	=1f / ((coord.X * mat.M14) + (coord.Y * mat.M24) + (coord.Z * mat.M34) + mat.M44);

			results[k]	=Vector3.Multiply(vec, w);
		});
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


	public static float ArrayAccess(this Vector3 v3, int sub)
	{
		if(sub == 0)
		{
			return	v3.X;
		}
		else if(sub == 1)
		{
			return	v3.Y;
		}
		return	v3.Z;
	}


	public static void ArraySet(this Vector3 v3, int sub, float value)
	{
		if(sub == 0)
		{
			v3.X	=value;
		}
		else if(sub == 1)
		{
			v3.Y	=value;
		}
		else
		{
			v3.Z	=value;
		}
	}


	//handy for printing
	public static string IntStr(this Vector3 vec)
	{
		return	"" + (int)vec.X + ", " + (int)vec.Y + ", " + (int)vec.Z;
	}


	//extension methods don't work on value types :(
	public static void ClearBoundingBox(ref BoundingBox bb)
	{
		bb	=new BoundingBox(
			Vector3.One * MIN_MAX_BOUNDS,
			-Vector3.One * MIN_MAX_BOUNDS);
	}


	public static float BoxPlaneDistance(this BoundingBox bb, Vector3 planeNormal)
	{
		Vector3	posNorm	=planeNormal.Positive();

		return	Vector3.Dot(posNorm, bb.Max);
	}


	//distance from point to infinite line
	//from Paul Bourke's site
	public static float	PointLineDistance(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
	{
		Vector3	lineVec	=lineEnd - lineStart;

		float	lenSquared	=lineVec.LengthSquared();

		float	u	=(((point.X - lineStart.X) * (lineEnd.X - lineStart.X)) +
			((point.Y - lineStart.Y) * (lineEnd.Y - lineStart.Y)) +
			((point.Z - lineStart.Z) * (lineEnd.Z - lineStart.Z))) / lenSquared;

		Vector3	intersection	=MAdd(lineStart, lineVec, u);

		return	Vector3.Distance(point, intersection);
	}


	//distance from point to line segment
	//from Paul Bourke's site
	public static bool	PointLineSegDistance(Vector3 point, Vector3 lineStart, Vector3 lineEnd, out float dist)
	{
		Vector3	lineVec	=lineEnd - lineStart;

		float	lenSquared	=lineVec.LengthSquared();

		float	u	=(((point.X - lineStart.X) * (lineEnd.X - lineStart.X)) +
			((point.Y - lineStart.Y) * (lineEnd.Y - lineStart.Y)) +
			((point.Z - lineStart.Z) * (lineEnd.Z - lineStart.Z))) / lenSquared;

		if(u < 0f || u > 1f)
		{
			//closest point lies outside the line segment
			dist	=0f;
			return	false;
		}

		Vector3	intersection	=MAdd(lineStart, lineVec, u);

		dist	=Vector3.Distance(point, intersection);

		return	true;
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


	public static Matrix4x4 MatrixFromDirection(Vector3 dir)
	{
		//get a good side vec
		Vector3	side	=Vector3.Cross(Vector3.UnitY, dir);

		float	len	=side.LengthSquared();
		if(len <= 0f)
		{
			//dir must be pointing straight up or down
			side	=Vector3.Cross(Vector3.UnitZ, dir);
		}

		side	=Vector3.Normalize(side);

		//get up vec
		Vector3	up	=Vector3.Cross(dir, side);

		//recross for better side
		side	=Vector3.Cross(up, dir);

		Matrix4x4	ret	=new Matrix4x4(side.X, side.Y, side.Z, 0f,
			up.X, up.Y, up.Z, 0f,
			dir.X, dir.Y, dir.Z, 0f,
			0f, 0f, 0f, 1f);

		return	ret;
	}


	//direction points at a cube face
	public static void CreateCubeMapViewProjMatrix(TextureCubeFace face,
		Vector3 cubeCenter, float farPlane,
		out Matrix4x4 cubeView, out Matrix4x4 cubeProj)
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
		cubeView	=Matrix4x4.CreateLookAt(cubeCenter,
			cubeCenter + VectorForCubeFace(face), upVec);

		//Values are to project on the cube face
		cubeProj	=Matrix4x4.CreatePerspectiveFieldOfView(
			MathHelper.PiOver2, 1f, 1f, farPlane);
	}


	//useful for directional shadow mapping, making item icons
	//from a rendertarget, or even mini maps
	public static void CreateBoundedDirectionalOrthoViewProj(
		BoundingBox bounds, Vector3 direction,
		out Matrix4x4 lightView, out Matrix4x4 lightProj, out Vector3 fakeOrigin)
	{
		//create a matrix aimed in the direction
		Matrix4x4	dirAim	=Matrix4x4.CreateLookAt(Vector3.Zero, -direction, Vector3.UnitY);

		//Get the corners
		Vector3[]	boxCorners	=bounds.GetCorners();

		//Transform the positions of the corners into the direction
		for(int i=0;i < boxCorners.Length;i++)
		{
			boxCorners[i]	=
			Vector3.Transform(boxCorners[i], dirAim);
		}
		
		//Find the smallest box around the points
		//in the directional frame of reference
		BoundingBox	dirSpaceBox	=BoundingBox.CreateFromPoints(boxCorners);

		Vector3	boxSize		=dirSpaceBox.Max - dirSpaceBox.Min;
		Vector3	halfBoxSize	=boxSize * 0.5f;
		
		//The viewpoint should be in the center of the back
		//panel of the box.
		Vector3	viewPos	=dirSpaceBox.Min + halfBoxSize;
		
		viewPos.Z	=dirSpaceBox.Min.Z;

		Matrix4x4	viewInv;
		if(!Matrix4x4.Invert(dirAim, out viewInv))
		{
			viewInv	=Matrix4x4.Identity;
		}
		
		//transform the view position back into worldspace
		viewPos	=Mathery.TransformCoordinate(viewPos, ref viewInv);
		
		//Create the view matrix
		lightView	=Matrix4x4.CreateLookAt(viewPos,
			viewPos - direction, Vector3.UnitY);

		//Create the ortho projection matrix
		lightProj	=Matrix4x4.CreateOrthographic(
				boxSize.X, boxSize.Y, -boxSize.Z, boxSize.Z);

		//get a good fake position for use in shader distance calcs
		fakeOrigin	=viewPos;
	}


	public static bool InFrustumLightAdjust(BoundingFrustum frust, Vector3 lightDir, BoundingSphere bs)
	{
		//extend down light ray 5 units
		//expand radius by 10
		BoundingSphere	bsAdjusted	=new BoundingSphere(bs.Center + (lightDir * 5f), bs.Radius + 10f);

		return	frust.Intersects(bsAdjusted);
	}


	public static BoundingSphere SphereFromPoints(IEnumerable<Vector3> points)
	{
		Vector3	center	=Vector3.Zero;
		float	radius	=0f;

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

		center	=min + (max - min) / 2.0f;

		foreach(Vector3 pnt in points)
		{
			float	dist	=Vector3.Distance(pnt, center);
			if(dist > radius)
			{
				radius	=dist;
			}
		}

		return	new BoundingSphere(center, radius);
	}


	public static void AddPointToBoundingBox(ref Vector3 min, ref Vector3 max, Vector3 pnt)
	{
		if(pnt.X < min.X)
		{
			min.X	=pnt.X;
		}
		if(pnt.X > max.X)
		{
			max.X	=pnt.X;
		}
		if(pnt.Y < min.Y)
		{
			min.Y	=pnt.Y;
		}
		if(pnt.Y > max.Y)
		{
			max.Y	=pnt.Y;
		}
		if(pnt.Z < min.Z)
		{
			min.Z	=pnt.Z;
		}
		if(pnt.Z > max.Z)
		{
			max.Z	=pnt.Z;
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
		ret.X	=rnd.NextSingle();
		ret.Y	=rnd.NextSingle();
		ret.Z	=rnd.NextSingle();

		//scale to -1 to 1 range
		ret	*=2f;
		ret	-=Vector3.One;

		//scale to range
		ret	*=range;

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
					Convert.ToByte(rnd.Next(55)));	//avoid background color
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

			ret	=Vector3.Normalize(ret);

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

			ret	=Vector3.Normalize(ret);

			if(!float.IsNaN(ret.X))
			{
				break;
			}
		}
		return	ret;
	}


	public static string RandomString(int length)
	{
		string	ret	=System.IO.Path.GetRandomFileName();

		ret	=ret.Substring(length);

		return	ret.Replace(".", "");
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
	public static void WrapAngleDegrees(ref Int16 angle)
	{
		angle	%=360;
	}


	//normalize degree angles into 0 to 360
	public static void WrapAngleDegreesPositive(ref Half angle)
	{
		float	fAng	=(float)angle;

		float	modAng	=fAng % 360f;

		if(modAng < 0f)
		{
			modAng	+=360f;
		}

		angle	=(Half)modAng;
	}


	public static void WrapAngleDegreesPositive(ref float angle)
	{
		float	modAng	=angle % 360f;

		if(modAng < 0f)
		{
			modAng	+=360f;
		}

		angle	=modAng;
	}


	//normalize degree angles into -360 to 360
	public static void WrapAngleDegrees(ref Half angle)
	{
		float	fAng	=(float)angle;

		float	modAng	=fAng % 360f;

		angle	=(Half)modAng;
	}


	public static void WrapAngleDegrees(ref float angle)
	{
		angle	%=360f;
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


	//snaps near axial normals to perfectly axial
	public static void SnapNormal(ref Vector3 vec)
	{
		if(CompareFloatEpsilon(vec.X, 1f, ANGLE_EPSILON))
		{
			vec		=Vector3.Zero;
			vec.X	=1f;
		}
		else if(CompareFloatEpsilon(vec.X, -1f, ANGLE_EPSILON))
		{
			vec		=Vector3.Zero;
			vec.X	=-1f;
		}
		else if(CompareFloatEpsilon(vec.Y, 1f, ANGLE_EPSILON))
		{
			vec		=Vector3.Zero;
			vec.Y	=1f;
		}
		else if(CompareFloatEpsilon(vec.Y, -1f, ANGLE_EPSILON))
		{
			vec		=Vector3.Zero;
			vec.Y	=-1f;
		}
		else if(CompareFloatEpsilon(vec.Z, 1f, ANGLE_EPSILON))
		{
			vec		=Vector3.Zero;
			vec.Z	=1f;
		}
		else if(CompareFloatEpsilon(vec.Z, -1f, ANGLE_EPSILON))
		{
			vec		=Vector3.Zero;
			vec.Z	=-1f;
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

		sAxis	=Vector3.Normalize(sAxis);
		tAxis	=Vector3.Normalize(tAxis);

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

		norm	=Vector3.Normalize(norm);
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

		norm	=Vector3.Normalize(norm);
		dist	=Vector3.Dot(verts[1], norm);
	}


	public static float? RayIntersectBox(Vector3 start, Vector3 end, BoundingBox box)
	{
		Vector3	dir	=end - start;

		//keep the length
		float	len	=dir.Length();

		//normalize
		dir	/=len;

		Ray	ray	=new Ray(start, dir);

		float	?dist	=box.Intersects(ray);
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


	public static Vector3	BoxNormalAtPoint(BoundingBox bb, Vector3 pos)
	{
		Vector3	pbVec	=(pos - bb.Center);
		Vector3	ret		=Vector3.Zero;

		float	bestDist	=float.MinValue;

		foreach(Vector3 ax in AxialNormals)
		{
			float	dist	=Vector3.Dot(ax, pbVec);
			if(dist > bestDist)
			{
				ret			=ax;
				bestDist	=dist;
			}
		}
		return	ret;
	}


	public static BoundingSphere TransformSphere(Matrix4x4 trans, BoundingSphere bs)
	{
		Vector3	pos	=bs.Center;
		float	rad	=bs.Radius;

		pos	=Mathery.TransformCoordinate(pos, ref trans);

		Vector3	scaleVec	=new Vector3(trans.M11, trans.M22, trans.M33);
			
		//use the greatest dimension
		rad	*=GreatestSphereDimension(scaleVec);

		return	new BoundingSphere(pos, rad);
	}


	//from Jim Drygiannakis on github
	//This one is nice because it gives 2 intersections
	//which is handy for capsules as the "inner" side of
	//the endpoint spheres can be excluded
	public static bool	IntersectRaySphere(Ray ray, BoundingSphere sphere, out float tmin, out float tmax)
	{
		Vector3	CO	=ray.Position - sphere.Center;

		float	a	=Vector3.Dot(ray.Direction, ray.Direction);
		float	b	=2.0f * Vector3.Dot(CO, ray.Direction);
		float	c	=Vector3.Dot(CO, CO) - (sphere.Radius * sphere.Radius);

		float	discriminant	=b * b - 4.0f * a * c;
		if(discriminant < 0.0f)
		{
			tmin	=tmax	=0f;
			return	false;
		}

		tmin	=(-b - MathHelper.Sqrt(discriminant)) / (2.0f * a);
		tmax	=(-b + MathHelper.Sqrt(discriminant)) / (2.0f * a);
		if(tmin > tmax)
		{
			float	temp	=tmin;

			tmin	=tmax;
			tmax	=temp;
		}
		return	true;
	}


	//from Jim Drygiannakis on github
	//This is the best intersection I've found, way way better
	//than my own efforts, which didn't cover all the corner cases.
	public static bool IntersectRayCapsule(
		Vector3	A, Vector3 B, float capRadius,	//capsule dimensions
		Ray ray, float rayRadius,				//ray
		out Vector3 p1, out Vector3 p2,			//impact points
		out Vector3 n1, out Vector3 n2)			//impact normals
	{
		// http://pastebin.com/2XrrNcxb
		// Substituting equ. (1) - (6) to equ. (I) and solving for t' gives:
		//
		// t' = (t * dot(AB, d) + dot(AB, AO)) / dot(AB, AB); (7) or
		// t' = t * m + n where 
		// m = dot(AB, d) / dot(AB, AB) and 
		// n = dot(AB, AO) / dot(AB, AB)
		//
		Vector3	AB	=B - A;
		Vector3	AO	=ray.Position - A;

		float	bothRads	=capRadius + rayRadius;

		float	AB_dot_d	=Vector3.Dot(AB, ray.Direction);
		float	AB_dot_AO	=Vector3.Dot(AB, AO);
		float	AB_dot_AB	=Vector3.Dot(AB, AB);
		
		float	m	=AB_dot_d / AB_dot_AB;
		float	n	=AB_dot_AO / AB_dot_AB;

		// Substituting (7) into (II) and solving for t gives:
		//
		// dot(Q, Q)*t^2 + 2*dot(Q, R)*t + (dot(R, R) - r^2) = 0
		// where
		// Q = d - AB * m
		// R = AO - AB * n
		Vector3	Q	=ray.Direction - (AB * m);
		Vector3	R	=AO - (AB * n);

		float	a	=Vector3.Dot(Q, Q);
		float	b	=2.0f * Vector3.Dot(Q, R);
		float	c	=Vector3.Dot(R, R) - (bothRads * bothRads);

		if(a == 0f)
		{
			// Special case: AB and ray direction are parallel. If there is an intersection it will be on the end spheres...
			// NOTE: Why is that?
			// Q = d - AB * m =>
			// Q = d - AB * (|AB|*|d|*cos(AB,d) / |AB|^2) => |d| == 1.0
			// Q = d - AB * (|AB|*cos(AB,d)/|AB|^2) =>
			// Q = d - AB * cos(AB, d) / |AB| =>
			// Q = d - unit(AB) * cos(AB, d)
			//
			// |Q| == 0 means Q = (0, 0, 0) or d = unit(AB) * cos(AB,d)
			// both d and unit(AB) are unit vectors, so cos(AB, d) = 1 => AB and d are parallel.
			// 
			BoundingSphere	sphereA	=new BoundingSphere(A, bothRads);
			BoundingSphere	sphereB	=new BoundingSphere(B, bothRads);

			float	atmin, atmax, btmin, btmax;
			
			if(!IntersectRaySphere(ray, sphereA, out atmin, out atmax) ||
				!IntersectRaySphere(ray, sphereB, out btmin, out btmax))
			{
				// No intersection with one of the spheres means no intersection at all...
				p1	=p2	=n1	=n2	=Vector3.Zero;
				return	false;
			}

			if(atmin < btmin)
			{
				p1	=ray.Position + (ray.Direction * atmin);
				n1	=p1 - A;
				n1	=Vector3.Normalize(n1);
			}
			else
			{
				p1	=ray.Position + (ray.Direction * btmin);
				n1	=p1 - B;
				n1	=Vector3.Normalize(n1);
			}

			if(atmax > btmax)
			{
				p2	=ray.Position + (ray.Direction * atmax);
				n2	=p2 - A;
				n2	=Vector3.Normalize(n2);
			}
			else
			{
				p2	=ray.Position + (ray.Direction * btmax);
				n2	=p2 - B;
				n2	=Vector3.Normalize(n2);
			}

			return true;
		}

		float	discriminant	=b * b - 4.0f * a * c;
		if(discriminant < 0.0f)
		{
			// The ray doesn't hit the infinite cylinder defined by (A, B).
			// No intersection.
			p1	=p2	=n1	=n2	=Vector3.Zero;
			return	false;
		}

		float	tmin	=(-b - MathHelper.Sqrt(discriminant)) / (2.0f * a);
		float	tmax	=(-b + MathHelper.Sqrt(discriminant)) / (2.0f * a);
		if(tmin > tmax)
		{
			float	temp	=tmin;

			tmin	=tmax;
			tmax	=temp;
		}

		// Now check to see if K1 and K2 are inside the line segment defined by A,B
		float	t_k1	=tmin * m + n;
		if(t_k1 < 0f)
		{
			// On sphere (A, r)...
			BoundingSphere	s	=new BoundingSphere(A, bothRads);

			float	stmin, stmax;
			if(IntersectRaySphere(ray, s, out stmin, out stmax))
			{
				p1	=ray.Position + (ray.Direction * stmin);
				n1	=p1 - A;
				n1	=Vector3.Normalize(n1);
			}
			else
			{
				p1	=p2	=n1	=n2	=Vector3.Zero;
				return	false;
			}
		}
		else if(t_k1 > 1f)
		{
			// On sphere (B, r)...
			BoundingSphere	s	=new BoundingSphere(B, bothRads);

			float	stmin, stmax;
			if(IntersectRaySphere(ray, s, out stmin, out stmax))
			{
				p1	=ray.Position + (ray.Direction * stmin);
				n1	=p1 - B;
				n1	=Vector3.Normalize(n1);
			}
			else
			{
				p1	=p2	=n1	=n2	=Vector3.Zero;
				return	false;
			}
		}
		else
		{
			// On the cylinder...
			p1	=ray.Position + (ray.Direction * tmin);

			Vector3	k1	=A + AB * t_k1;
			n1	=p1 - k1;
			n1	=Vector3.Normalize(n1);
		}

		float	t_k2	=tmax * m + n;
		if(t_k2 < 0f)
		{
			// On sphere (A, r)...
			BoundingSphere	s	=new BoundingSphere(A, bothRads);

			float	stmin, stmax;
			if(IntersectRaySphere(ray, s, out stmin, out stmax))
			{
				p2	=ray.Position + (ray.Direction * stmax);
				n2	=p2 - A;
				n2	=Vector3.Normalize(n2);
			}
			else
			{
				p1	=p2	=n1	=n2	=Vector3.Zero;
				return	false;
			}
		}
		else if(t_k2 > 1f)
		{
			// On sphere (B, r)...
			BoundingSphere	s	=new BoundingSphere(B, bothRads);

			float	stmin, stmax;
			if(IntersectRaySphere(ray, s, out stmin, out stmax))
			{
				p2	=ray.Position + (ray.Direction * stmax);
				n2	=p2 - B;
				n2	=Vector3.Normalize(n2);
			}
			else
			{
				p1	=p2	=n1	=n2	=Vector3.Zero;
				return	false;
			}
		}
		else
		{
			p2	=ray.Position + (ray.Direction * tmax);

			Vector3	k2	=A + AB * t_k2;
			n2	=p2 - k2;
			n2	=Vector3.Normalize(n2);
		}
		
		return	true;
	}


	public static float? RayIntersectSphere(Vector3 start, Vector3 end, BoundingSphere sphere)
	{
		Vector3	dir	=end - start;

		//keep the length
		float	len	=dir.Length();

		//normalize
		dir	/=len;

		Ray	ray	=new Ray(start, dir);

		float	?dist	=sphere.Intersects(ray);
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


	public static bool IsBoundingBoxCentered(BoundingBox box)
	{
		Vector3	delta	=CenterBoundingBoxAtOrigin(ref box);

		return	(delta == Vector3.Zero);
	}


	public static Vector3 CenterBoundingBoxAtOrigin(ref BoundingBox box)
	{
		BoundingBox	start	=box;

		float	xsize	=box.Max.X - box.Min.X;
		float	zsize	=box.Max.Z - box.Min.Z;
		float	height	=box.Max.Y - box.Min.Y;

		xsize	*=0.5f;
		zsize	*=0.5f;
		height	*=0.5f;

		box	=new BoundingBox(new Vector3(-xsize, -height, -zsize), new Vector3(xsize, height, zsize));

		return	start.Min - box.Min;
	}


	public static Vector4 ClampVector(Vector4 val, Vector4 min, Vector4 max)
	{
		Vector4	ret	=Vector4.Zero;

		ret.X	=Math.Clamp(val.X, min.X, max.X);
		ret.Y	=Math.Clamp(val.Y, min.Y, max.Y);
		ret.Z	=Math.Clamp(val.Z, min.Z, max.Z);
		ret.W	=Math.Clamp(val.W, min.W, max.W);

		return	ret;
	}


	public static void ClipRayToBox(BoundingBox box,
		ref Vector3 rayStart, ref Vector3 rayEnd)
	{
		if(rayStart.X < box.Min.X)
		{
			rayStart.X	=box.Min.X;
		}
		if(rayStart.Y < box.Min.Y)
		{
			rayStart.Y	=box.Min.Y;
		}
		if(rayStart.Z < box.Min.Z)
		{
			rayStart.Z	=box.Min.Z;
		}

		if(rayStart.X > box.Max.X)
		{
			rayStart.X	=box.Max.X;
		}
		if(rayStart.Y > box.Max.Y)
		{
			rayStart.Y	=box.Max.Y;
		}
		if(rayStart.Z > box.Max.Z)
		{
			rayStart.Z	=box.Max.Z;
		}

		if(rayEnd.X < box.Min.X)
		{
			rayEnd.X	=box.Min.X;
		}
		if(rayEnd.Y < box.Min.Y)
		{
			rayEnd.Y	=box.Min.Y;
		}
		if(rayEnd.Z < box.Min.Z)
		{
			rayEnd.Z	=box.Min.Z;
		}

		if(rayEnd.X > box.Max.X)
		{
			rayEnd.X	=box.Max.X;
		}
		if(rayEnd.Y > box.Max.Y)
		{
			rayEnd.Y	=box.Max.Y;
		}
		if(rayEnd.Z > box.Max.Z)
		{
			rayEnd.Z	=box.Max.Z;
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
				return	MathHelper.TwoPi;
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