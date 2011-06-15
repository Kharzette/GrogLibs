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
			return	new MapBrush(bnd, pp);
		}
	}
}
