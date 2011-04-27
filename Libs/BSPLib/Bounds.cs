using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using Microsoft.Xna.Framework;


namespace BSPLib
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
