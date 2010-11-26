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


		public Bounds()
		{
			ClearBounds();
		}


		public Bounds(Bounds bnd)
		{
			mMins	=bnd.mMins;
			mMaxs	=bnd.mMaxs;
		}


		public void ClearBounds()
		{
			mMins.X	=mMins.Y =mMins.Z	=Brush.MIN_MAX_BOUNDS;
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
				if(UtilityLib.Mathery.VecIdx(mMins, i) <= -Brush.MIN_MAX_BOUNDS
					|| UtilityLib.Mathery.VecIdx(mMaxs, i) >= Brush.MIN_MAX_BOUNDS)
				{
					return	true;
				}
			}
			return	false;
		}
	}
}
