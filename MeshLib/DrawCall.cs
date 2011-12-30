using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;


namespace MeshLib
{
	public class DrawCall
	{
		public int		mNumVerts;
		public int		mStartIndex;	//offsets
		public int		mPrimCount;		//num prims per call
		public Vector3	mSortPoint;
	}
}
