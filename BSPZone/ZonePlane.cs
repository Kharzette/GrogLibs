﻿using System;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using UtilityLib;


namespace BSPZone;

public struct ZonePlane
{
	public Vector3	mNormal;
	public float	mDist;

	//default blank planes
	static ZonePlane	mBlank	=new ZonePlane(Vector3.Zero, 0.0f);
	static ZonePlane	mBlankX	=new ZonePlane(Vector3.UnitX, 0.0f);


	public ZonePlane(Vector3 norm, float dist)
	{
		mNormal	=norm;
		mDist	=dist;
	}


	public static ZonePlane Blank
	{
		get { return mBlank; }
	}

	public static ZonePlane BlankX
	{
		get { return mBlankX; }
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public override bool Equals(object obj)
	{
		return	base.Equals(obj);
	}

	public static bool operator ==(ZonePlane p1, ZonePlane p2)
	{
		return p1.mNormal.X == p2.mNormal.X
			&& p1.mNormal.Y == p2.mNormal.Y
			&& p1.mNormal.Z == p2.mNormal.Z
			&& p1.mDist == p2.mDist;
	}

	public static bool operator !=(ZonePlane p1, ZonePlane p2)
	{
		return !(p1 == p2);
	}

	public void Write(BinaryWriter bw)
	{
		bw.Write(mNormal.X);
		bw.Write(mNormal.Y);
		bw.Write(mNormal.Z);
		bw.Write(mDist);
	}


	public void Read(BinaryReader br, out ZonePlane read)
	{
		read.mNormal.X	=br.ReadSingle();
		read.mNormal.Y	=br.ReadSingle();
		read.mNormal.Z	=br.ReadSingle();
		read.mDist		=br.ReadSingle();
	}


	public float Distance(Vector3 pos)
	{
		return	Vector3.Dot(pos, mNormal) - mDist;
	}


	public bool IsGround()
	{
		return	(Vector3.Dot(mNormal, Vector3.UnitY) > Zone.RampAngle);
	}


	//push slightly to the front side
	internal Vector3 ReflectPosition(Vector3 start, Vector3 end)
	{
		float	startDist	=Distance(start);
		float	dist		=Distance(end);

		//is the direction vector valid to find a collision response?
		if(startDist <= 0f || dist >= Mathery.DIST_EPSILON)
		{
			//place end directly on the plane
			end	-=(mNormal * dist);

			//adjust it to the front side
			end	+=(mNormal * Mathery.DIST_EPSILON);
		}
		else
		{
			end	-=(mNormal * (dist - Mathery.DIST_EPSILON));
		}
		return	end;
	}


	//adjust a position just off the front side
	internal void ReflectPosition(ref Vector3 pos)
	{
		float	dist	=Distance(pos);

		//directly on or off a bit?
		if(dist >= Mathery.DIST_EPSILON)
		{
			//place end directly on the plane
			pos	-=(mNormal * dist);

			//adjust it to the front side
			pos	+=(mNormal * Mathery.DIST_EPSILON);
		}
		else
		{
			pos	-=(mNormal * (dist - Mathery.DIST_EPSILON));
		}
	}


	//adjust a movement vec along the plane
	internal bool MoveAlong(ref Vector3 moveVec)
	{
		Debug.Assert(IsGround());

		if(moveVec == Vector3.Zero)
		{
			return	false;
		}

		//save length
		float	len	=moveVec.Length();

		Vector3	moveNorm	=moveVec / len;

		float	dot	=moveNorm.dot(mNormal);

		if(dot < -(1f -Zone.RampAngle) || dot > (1f - Zone.RampAngle))
		{
			return	false;	//movement is too perp for alignment
		}

		Vector3	sideVec	=Vector3.Cross(moveVec, mNormal);

		Vector3	newVec	=Vector3.Cross(mNormal, sideVec);

		newVec	=Vector3.Normalize(newVec);

		moveVec	=newVec * len;

		return	true;
	}


	internal void Inverse()
	{
		mNormal	=-mNormal;
		mDist	=-mDist;
	}


	internal static ZonePlane Transform(ZonePlane plane, Matrix4x4 mat)
	{
		Vector3	p0, p1, p2;

		if(plane == ZonePlane.Blank)
		{
			return	plane;
		}

		Mathery.PointsFromPlane(plane.mNormal, plane.mDist, out p0, out p1, out p2);

		Mathery.TransformCoordinate(p0, ref mat, out p0);
		Mathery.TransformCoordinate(p1, ref mat, out p1);
		Mathery.TransformCoordinate(p2, ref mat, out p2);

		ZonePlane	ret	=ZonePlane.Blank;

		UtilityLib.Mathery.PlaneFromVerts(p0, p1, p2, out ret.mNormal, out ret.mDist);

		return	ret;
	}


	//the xna transform expects an inverted matrix
	//which is quite odd
	//note this method doesn't work very well at all
	//getting very off the wall results with rotations
	internal static ZonePlane XNATransform(ZonePlane zonePlane, Matrix4x4 matrix)
	{
		Plane	XNAPlane;
		XNAPlane.D		=zonePlane.mDist;
		XNAPlane.Normal	=zonePlane.mNormal;

		XNAPlane	=Plane.Transform(XNAPlane, matrix);

		ZonePlane	ret	=ZonePlane.Blank;

		ret.mNormal	=XNAPlane.Normal;
		ret.mDist	=XNAPlane.D;

		return	ret;
	}
}