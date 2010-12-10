using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSPLib
{
	public class PlanePool
	{
		public List<GBSPPlane>	mPlanes	=new List<GBSPPlane>();

		public const Int32	MAX_BSP_PLANES		=32000;
		public const Int32	PLANENUM_LEAF		=-1;
		


		internal Int32 FindPlane(GBSPPlane plane, out sbyte side)
		{
			GBSPPlane	plane1	=new GBSPPlane(plane);

			plane1.Snap();
			plane1.Side(out side);

			for(int i=0;i < mPlanes.Count;i++)
			{
				if(plane1.Compare(mPlanes[i]))
				{
					return	i;
				}
			}

			if(mPlanes.Count > MAX_BSP_PLANES)
			{
				return	-1;
			}

			mPlanes.Add(plane1);

			return	mPlanes.Count - 1;
		}


		internal GFXPlane[] GetGFXArray()
		{
			GFXPlane	[]ret	=new GFXPlane[mPlanes.Count];

			for(int i=0;i < mPlanes.Count;i++)
			{
				ret[i]			=new GFXPlane();
				ret[i].mNormal	=mPlanes[i].mNormal;
				ret[i].mDist	=mPlanes[i].mDist;
				ret[i].mType	=mPlanes[i].mType;
			}
			return	ret;
		}
	}
}
