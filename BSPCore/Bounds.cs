using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using Microsoft.Xna.Framework;


namespace BSPCore
{
	public class Bounds
	{
		public Vector3	mMins, mMaxs;

		public const float	MIN_MAX_BOUNDS	=15192.0f;


		public Bounds()
		{
			Clear();
		}


		public Bounds(Bounds bnd)
		{
			mMins	=bnd.mMins;
			mMaxs	=bnd.mMaxs;
		}


		public void Clear()
		{
			mMins.X	=mMins.Y =mMins.Z	=MIN_MAX_BOUNDS;
			mMaxs	=-mMins;
		}


		public void AddPointToBounds(Vector3 pnt)
		{
			if(pnt.X < mMins.X)
			{
				mMins.X	=pnt.X;
			}
			if(pnt.X > mMaxs.X)
			{
				mMaxs.X	=pnt.X;
			}
			if(pnt.Y < mMins.Y)
			{
				mMins.Y	=pnt.Y;
			}
			if(pnt.Y > mMaxs.Y)
			{
				mMaxs.Y	=pnt.Y;
			}
			if(pnt.Z < mMins.Z)
			{
				mMins.Z	=pnt.Z;
			}
			if(pnt.Z > mMaxs.Z)
			{
				mMaxs.Z	=pnt.Z;
			}
		}


		internal bool Overlaps(Bounds b2)
		{
			for(int i=0;i < 3;i++)
			{
				if(UtilityLib.Mathery.VecIdx(mMins, i) >= UtilityLib.Mathery.VecIdx(b2.mMaxs, i) ||
					UtilityLib.Mathery.VecIdx(mMaxs, i) <= UtilityLib.Mathery.VecIdx(b2.mMins, i))
				{
					return	false;
				}
			}
			return	true;
		}


		public void Merge(Bounds b1, Bounds b2)
		{
			if(b1 != null)
			{
				AddPointToBounds(b1.mMins);
				AddPointToBounds(b1.mMaxs);
			}
			if(b2 != null)
			{
				AddPointToBounds(b2.mMins);
				AddPointToBounds(b2.mMaxs);
			}
		}


		internal bool IsMaxExtents()
		{
			for(int i=0;i < 3;i++)
			{
				if(UtilityLib.Mathery.VecIdx(mMins, i) <= -MIN_MAX_BOUNDS
					|| UtilityLib.Mathery.VecIdx(mMaxs, i) >= MIN_MAX_BOUNDS)
				{
					return	true;
				}
			}
			return	false;
		}


		internal void GetPlanes(PlanePool pp, out int []planes)
		{
			planes	=new int[6];

			GBSPPlane	p	=new GBSPPlane();
			bool		bFlip;

			//max x
			p.mNormal	=Vector3.UnitX;
			p.mDist		=mMaxs.X;
			planes[0]	=pp.FindPlane(p, out bFlip);

			//max y
			p.mNormal	=Vector3.UnitY;
			p.mDist		=mMaxs.Y;
			planes[1]	=pp.FindPlane(p, out bFlip);

			//max z
			p.mNormal	=Vector3.UnitZ;
			p.mDist		=mMaxs.Z;
			planes[2]	=pp.FindPlane(p, out bFlip);

			//min x
			p.mNormal	=-Vector3.UnitX;
			p.mDist		=-mMins.X;
			planes[3]	=pp.FindPlane(p, out bFlip);

			//min y
			p.mNormal	=-Vector3.UnitY;
			p.mDist		=-mMins.Y;
			planes[4]	=pp.FindPlane(p, out bFlip);

			//min z
			p.mNormal	=-Vector3.UnitZ;
			p.mDist		=-mMins.Z;
			planes[5]	=pp.FindPlane(p, out bFlip);
		}


		internal UInt32 BoxOnPlaneSide(GBSPPlane Plane)
		{
			UInt32	Side;
			Vector3	Corner1, Corner2;
			float	Dist1, Dist2;

			Corner1	=Vector3.Zero;
			Corner2	=Vector3.Zero;
			
			if(Plane.mType < 3)
			{
				Side	=0;

				if(UtilityLib.Mathery.VecIdx(mMaxs, Plane.mType)
					> Plane.mDist + GBSPPlane.PLANESIDE_EPSILON)
				{
					Side	|=GBSPPlane.PSIDE_FRONT;
				}

				if(UtilityLib.Mathery.VecIdx(mMins, Plane.mType)
					< Plane.mDist - GBSPPlane.PLANESIDE_EPSILON)
				{
					Side	|=GBSPPlane.PSIDE_BACK;
				}
				return	Side;
			}
			
			for(int i=0;i < 3;i++)
			{
				if(UtilityLib.Mathery.VecIdx(Plane.mNormal, i) < 0)
				{
					UtilityLib.Mathery.VecIdxAssign(ref Corner1, i,
						UtilityLib.Mathery.VecIdx(mMins, i));
					UtilityLib.Mathery.VecIdxAssign(ref Corner2, i,
						UtilityLib.Mathery.VecIdx(mMaxs, i));
				}
				else
				{
					UtilityLib.Mathery.VecIdxAssign(ref Corner2, i,
						UtilityLib.Mathery.VecIdx(mMins, i));
					UtilityLib.Mathery.VecIdxAssign(ref Corner1, i,
						UtilityLib.Mathery.VecIdx(mMaxs, i));
				}
			}

			Dist1	=Vector3.Dot(Plane.mNormal, Corner1) - Plane.mDist;
			Dist2	=Vector3.Dot(Plane.mNormal, Corner2) - Plane.mDist;
			Side	=0;
			if(Dist1 >= GBSPPlane.PLANESIDE_EPSILON)
			{
				Side	=GBSPPlane.PSIDE_FRONT;
			}
			if(Dist2 < GBSPPlane.PLANESIDE_EPSILON)
			{
				Side	|=GBSPPlane.PSIDE_BACK;
			}

			return	Side;
		}


		internal bool IsPointInbounds(Vector3 pnt)
		{
			return	IsPointInbounds(pnt, 0.0f);
		}


		internal bool IsPointInbounds(Vector3 pnt, float epsilon)
		{
			if(pnt.X < (mMins.X - epsilon))
			{
				return	false;
			}
			if(pnt.Y < (mMins.Y - epsilon))
			{
				return	false;
			}
			if(pnt.Z < (mMins.Z - epsilon))
			{
				return	false;
			}
			if(pnt.X > (mMaxs.X + epsilon))
			{
				return	false;
			}
			if(pnt.Y > (mMaxs.Y + epsilon))
			{
				return	false;
			}
			if(pnt.Z > (mMaxs.Z + epsilon))
			{
				return	false;
			}
			return	true;
		}


		internal Vector3 GetCenter()
		{
			return	(mMins + mMaxs) / 2.0f;
		}

		internal void AddPointToBounds(Vector2 vec2)
		{
			Vector3	vec3	=Vector3.Zero;
			vec3.X	=vec2.X;
			vec3.Y	=vec2.Y;

			AddPointToBounds(vec3);
		}
	}
}
