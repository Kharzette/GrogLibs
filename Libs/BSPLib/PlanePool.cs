using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;


namespace BSPLib
{
	internal class PlanePool
	{
		internal List<GBSPPlane>	mPlanes	=new List<GBSPPlane>();

		internal const Int32	MAX_BSP_PLANES		=32000;
		internal const Int32	PLANENUM_LEAF		=-1;
		


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


		internal Int32 FindPlane(GFXPlane plane, out sbyte side)
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


		internal void Write(BinaryWriter bw)
		{
			bw.Write(mPlanes.Count);

			foreach(GBSPPlane p in mPlanes)
			{
				p.Write(bw);
			}
		}


		internal void Read(BinaryReader br)
		{
			int	cnt	=br.ReadInt32();

			mPlanes.Clear();

			for(int i=0;i < cnt;i++)
			{
				GBSPPlane	p	=new GBSPPlane();

				p.Read(br);

				mPlanes.Add(p);
			}
		}
	}
}
