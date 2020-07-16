using System.Collections.Generic;
using SharpDX;
using BSPZone;


namespace EntityLib
{
	public class ConvexVolume : Component
	{
		List<ZonePlane>	mPlanes	=new List<ZonePlane>();


		public ConvexVolume(BoundingBox box, Entity owner) : base(owner)
		{
			ZonePlane	zp	=ZonePlane.Blank;

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
	}
}