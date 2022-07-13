using System.IO;
using System.Numerics;
using Vortice.Mathematics;
using UtilityLib;

namespace MeshLib;


//this is a cylinder shape with rounded ends
public class BoundingCapsule
{
	internal float	mRadius;
	internal float	mLength;
	

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


	static internal BoundingCapsule CreateFromBoundingBox(BoundingBox box)
	{
		//find radius
		float	radius	=0f;

		//measure from center to most distant side
		if(box.Min.Z < box.Min.X)
		{
			radius	=box.Center.Z - box.Min.Z;
		}
		else
		{
			radius	=box.Center.X - box.Min.X;
		}

		float	length =box.Height - (2 * radius);
		if(length < 0)
		{
			//this shape is goofy and probably won't work
			//boost length to at least 1
			length	=1;
		}

		return	new BoundingCapsule(radius, length);
	}


	internal bool RayCollide(Vector3 position, Quaternion orientation,
		Vector3	rayStart, Vector3 unitRayDirection, float len, out Vector3 impacto)
	{
		//position is the base of the capsule in world space
		//get the centerpoint of the base sphere == point A
		Vector3	A	=position + orientation.XYZ() * mRadius;

		//find B which is the endpoint sphere center
		Vector3	B	=position + orientation.XYZ() * (mLength - mRadius);

		//rayStart is C, the start point of the ray
		//D is the end point of the ray
		Vector3	D	=rayStart + (unitRayDirection * len);

		Vector3	segStart, segEnd;
		bool	bSolved	=Mathery.ShortestLineBetweenTwoLines(A, B, rayStart, D,
			out segStart, out segEnd);

		if(!bSolved)
		{
			//not sure this would ever really happen
			impacto	=position;
			return	true;
		}

		Vector3	segVector	=segEnd - segStart;

		float	dist	=segVector.Length();
		if(dist > mRadius)
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

		//normalize
		segVector	/=dist;

		impacto	=segVector * mRadius;

		return	true;
	}



}
