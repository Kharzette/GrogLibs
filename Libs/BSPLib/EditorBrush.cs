using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class EditorBrush
	{
		MapBrush	mBrush;


		public EditorBrush(Bounds bnd)
		{
			PlanePool	pp	=new PlanePool();
			mBrush			=BrushFromBounds(bnd, pp);
		}


		public void GetTriangles(List<Vector3> tris, List<UInt32> ind, bool bCheckFlags)
		{
			mBrush.GetTriangles(tris, ind, bCheckFlags);
		}


		public void GetLines(List<Vector3> tris, List<UInt32> ind, bool bCheckFlags)
		{
			mBrush.GetLines(tris, ind, bCheckFlags);
		}


		MapBrush BrushFromBounds(Bounds bnd, PlanePool pp)
		{
			MapBrush	ret	=new MapBrush();

			for(int i=0;i < 3;i++)
			{
				GBSPPlane	p	=new GBSPPlane();

				p.mNormal	=Vector3.Zero;

				UtilityLib.Mathery.VecIdxAssign(ref p.mNormal, i, 1.0f);
				p.mDist	=UtilityLib.Mathery.VecIdx(bnd.mMaxs, i) + 1.0f;

				GBSPSide	side	=new GBSPSide();
				side.mPlaneNum		=pp.FindPlane(p, out side.mPlaneSide);

				UtilityLib.Mathery.VecIdxAssign(ref p.mNormal, i, -1.0f);
				p.mDist	=-(UtilityLib.Mathery.VecIdx(bnd.mMins, i) - 1.0f);

				GBSPSide	side2	=new GBSPSide();
				side2.mPlaneNum		=pp.FindPlane(p, out side2.mPlaneSide);

				side.FixFlags();
				side2.FixFlags();

				ret.mOriginalSides.Add(side);
				ret.mOriginalSides.Add(side2);
			}

			ret.MakePolys(pp);
			ret.FixContents();

			return	ret;
		}
	}
}
