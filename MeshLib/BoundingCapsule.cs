using System;
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
	internal float	mRadius;
	internal float	mLength;	//distance between cap sphere centers
								//not the actual full length of the capsule	

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


	internal void Scale(float scalar)
	{
		mRadius	*=scalar;
		mLength	*=scalar;
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


	//returned impact should be along the ray
	internal bool RayCollide(Vector3 position, Vector3 orientation,
		Vector3	rayStart, Vector3 rayEnd, float rayRadius, out Vector3 impacto)
	{
		//position is the bottom sphere center
		Vector3	A	=position;

		//find B which is the endpoint sphere center
		Vector3	B	=position + orientation * mLength;

		//rayStart is C, the start point of the ray
		//D is the end point of the ray
		Vector3	D	=rayEnd;

		Vector3	segStart, segEnd;
		bool	bSolved	=Mathery.ShortestLineBetweenTwoLines(A, B, rayStart, D,
			out segStart, out segEnd);

		if(!bSolved)
		{
			//happens with bad data I think
			impacto	=Vector3.Zero;
			return	false;
		}

		Vector3	segVector	=segEnd - segStart;

		float	dist	=segVector.Length();
		if(dist > (mRadius + rayRadius))
		{
			//no hit
			impacto	=Vector3.Zero;
			return	false;
		}

		if(dist == 0f)
		{
			//right atop
			impacto	=segStart;
			return	true;
		}

		//A line segment has been found that is shorter than
		//the combined radiusesesesees of the capsule and the line...
		//
		//Now check if the segment lies between the two endpoints of the capsule.
		//this is checking the cylinder portion of the capsule

		//plane at the base of the capsule...
		Vector3	baseNormal	=orientation;
		float	baseDist	=Vector3.Dot(baseNormal, A);

		//plane at the end of the capsule
		Vector3	endNormal	=-orientation;
		float	endDist		=Vector3.Dot(endNormal, B);

		//segstart / end will always have the same distance to these planes
		float	segBaseDist	=Vector3.Dot(baseNormal, segStart) - baseDist;
		float	segEndDist	=Vector3.Dot(endNormal, segStart) - endDist;

		if(segBaseDist >= 0 && segEndDist >= 0)
		{
			//segment is within the cylinder
			//find impact

			//segStart is along the capsule line between A and B
			//segEnd is on the ray
			//
			//impacto should be the point along the ray where the
			//distance to capsule line AB is mRadius + rayRadius
			//and make sure impacto is still within the ray segment
			//
			//Finding a ratio between the distance from rayStart to
			//segStart, and length of segVector might work

			float	bothRadii	=(mRadius + rayRadius);

			float	rsDist	=Vector3.Distance(rayStart, segStart);

			float	ratio	=(dist - bothRadii) / (rsDist - bothRadii);

			//scale radii by ratio
//			ratio	*=(mRadius + rayRadius);

			Vector3	raySegment	=segEnd - rayStart;

			//this will be the ray where dist == radii
			raySegment	*=ratio;

			impacto	=segEnd + raySegment;

//			float	raySegLen	=raySegment.Length();

//			raySegment	/=raySegLen;

//			raySegment	*=(mRadius + rayRadius);

			//a perfectly perpendicular collision would get a
			//zero ratio.  The dot will show how much additional
			//vector length is needed to put the collision point
			//at the radiuseseesses boundary
//			float	ratio	=Vector3.Dot(raySegment, orientation);

//			impacto	=segEnd - (raySegment * ratio);

//			impacto	=segEnd;

			return	true;
		}

		//Shortest line segment is not within the cylinder section...
		//Check the end spheres
		segBaseDist	=Vector3.Distance(segEnd, A);
		segEndDist	=Vector3.Distance(segEnd, B);

		if(segBaseDist < (mRadius + rayRadius))
		{
			//hit on base sphere
			//normalize
			segVector	/=dist;

			//vector between ray and sphere center
			Vector3	capDir	=segEnd - A;

			//normalize
			capDir	/=segBaseDist;

			//this impact point could project outside the original ray
			//might need to rethink this
			impacto	=A + (capDir * (mRadius + rayRadius));

			return	true;
		}
		else if(segEndDist < (mRadius + rayRadius))
		{
			//hit on end sphere
			//normalize
			segVector	/=dist;

			//vector between ray and sphere center
			Vector3	capDir	=segEnd - B;

			//normalize
			capDir	/=segEndDist;

			//this impact point could project outside the original ray
			//might need to rethink this
			impacto	=B + (capDir * (mRadius + rayRadius));

			return	true;
		}

		impacto	=Vector3.Zero;
		return	false;
	}



}
