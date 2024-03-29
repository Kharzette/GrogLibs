﻿using System;
using System.IO;
using System.Numerics;
using Vortice.Mathematics;
using UtilityLib;

namespace MeshLib;


//this is a cylinder shape with rounded ends
//Intended to work as a bone space capsule with the bottom
//sphere center at the joint, no translation and extend along Z
public struct BoundingCapsule
{
	internal float	mRadius;	//Same for cylinder and both ends
	internal float	mLength;	//distance between cap sphere centers
								//not the actual full length of the capsule	

	const float	MinSize	=0.001f;


	internal BoundingCapsule(float rad, float len)
	{
		mRadius	=rad;
		mLength	=len;
	}


	internal BoundingCapsule(BinaryReader br)
	{
		mRadius	=br.ReadSingle();
		mLength	=br.ReadSingle();
	}


	internal void Write(BinaryWriter bw)
	{
		bw.Write(mRadius);
		bw.Write(mLength);
	}


	internal void IncLength(float delta)
	{
		//don't allow negative
		mLength	=MathHelper.Max(mLength + delta, MinSize);
	}

	internal void IncRadius(float delta)
	{
		//don't allow negative
		mRadius	=MathHelper.Max(mRadius + delta, MinSize);
	}


	internal void Scale(float scalar)
	{
		mRadius	*=scalar;
		mLength	*=scalar;
	}


	static internal BoundingBox BoxFromCapsule(BoundingCapsule bc)
	{
		Vector3	min, max;

		Vector3	zEnd	=Vector3.UnitZ * bc.mRadius;

		min	=new Vector3(-bc.mRadius, -bc.mRadius, -bc.mRadius);
		max	=new Vector3(zEnd.X + bc.mRadius, zEnd.Y + bc.mRadius, zEnd.Z + bc.mRadius);

		return	new BoundingBox(min, max);
	}


	static internal BoundingCapsule CreateFromBoundingBox(BoundingBox box)
	{
		//find radius
		float	radius	=0f;
		float	length	=0f;
		Vector3	radVec	=Vector3.Zero;

		//measure from center to most distant side
		if(box.Min.Z < box.Min.X && box.Min.Z < box.Min.Y)
		{
			//use Z for length
			length	=box.Max.Z - box.Min.Z;
			radVec	=Vector3.UnitX + Vector3.UnitY;	//xy for radius
		}
		else if(box.Min.Y < box.Min.X && box.Min.Y < box.Min.Z)
		{
			length	=box.Max.Y - box.Min.Y;
			radVec	=Vector3.UnitX + Vector3.UnitZ;	//xz for radius
		}
		else
		{
			length	=box.Max.X - box.Min.X;
			radVec	=Vector3.UnitZ + Vector3.UnitY;	//zy for radius
		}

		radVec	=Vector3.Normalize(radVec);
		radius	=Vector3.Dot(radVec, box.Max - box.Center);

		return	new BoundingCapsule(radius, length);
	}


	internal bool	IntersectRay(Vector3 pos, Vector3 orientation,
		Ray ray, float rayRadius,
		out Vector3 impact1, out Vector3 impact2,
		out Vector3 norm1, out Vector3 norm2)
	{
		Vector3	A	=pos;
		Vector3	B	=pos + (orientation * mLength);
		
		return	Mathery.IntersectRayCapsule(A, B, mRadius, ray, rayRadius,
			out impact1, out impact2, out norm1, out norm2);
	}
}