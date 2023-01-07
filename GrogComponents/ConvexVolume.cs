using System;
using System.Collections.Generic;
using SharpDX;
using BSPZone;


namespace EntityLib;

public class ConvexVolume : Component
{
	public enum State
	{
		Active
	}

	List<ZonePlane>	mPlanes	=new List<ZonePlane>();

	bool	mbActive;
	float	mSizeY;

	public bool Active
	{
		get { return	mbActive; }
	}

	public float SizeY
	{
		get { return	mSizeY; }
	}



	public ConvexVolume(BoundingBox box, Vector3 pos, Entity owner) : base(owner)
	{
		mSizeY	=box.Height;

		ZonePlane	zp	=ZonePlane.Blank;

		box.Minimum	+=pos;
		box.Maximum	+=pos;

		//max x
		zp.mNormal	=Vector3.UnitX;
		zp.mDist	=box.Maximum.X;
		mPlanes.Add(zp);

		//max y
		zp.mNormal	=Vector3.UnitY;
		zp.mDist	=box.Maximum.Y;
		mPlanes.Add(zp);

		//max z
		zp.mNormal	=Vector3.UnitZ;
		zp.mDist	=box.Maximum.Z;
		mPlanes.Add(zp);

		//min x
		zp.mNormal	=-Vector3.UnitX;
		zp.mDist	=-box.Minimum.X;
		mPlanes.Add(zp);

		//min y
		zp.mNormal	=-Vector3.UnitY;
		zp.mDist	=-box.Minimum.Y;
		mPlanes.Add(zp);

		//min z
		zp.mNormal	=-Vector3.UnitZ;
		zp.mDist	=-box.Minimum.Z;
		mPlanes.Add(zp);

		mbActive	=true;
	}


	public bool SphereMotionIntersects(float radius, Vector3 start, Vector3 end)
	{
		foreach(ZonePlane zp in mPlanes)
		{
			float	sDist	=zp.Distance(start);
			float	eDist	=zp.Distance(end);

			if(sDist > radius && eDist > radius)
			{
				return	false;
			}
		}
		return	true;
	}


	public override void StateChange(Enum state, UInt32 value)
	{
		if(state.Equals(State.Active))
		{
			mbActive	=value != 0;
		}
	}
}