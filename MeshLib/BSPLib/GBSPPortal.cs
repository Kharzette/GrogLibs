using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class GBSPPortal
	{
		public GBSPPoly		mPoly;							//Convex poly that holds the shape of the portal
		public GBSPNode		[]mNodes	=new GBSPNode[2];	//Node on each side of the portal
		public GBSPPortal	[]mNext		=new GBSPPortal[2];	//Next portal for each node
		public Int32		mPlaneNum;

		public GBSPNode	mOnNode;
		public GBSPFace	[]mFace	=new GBSPFace[2];
		public GBSPSide	mSide;
		public byte		mSideFound;


		internal GBSPPortal() { }
		internal GBSPPortal(GBSPPortal copyMe)
		{
			mPoly		=new GBSPPoly(copyMe.mPoly);
			mNodes[0]	=copyMe.mNodes[0];
			mNodes[1]	=copyMe.mNodes[1];
			mNext[0]	=copyMe.mNext[0];
			mNext[1]	=copyMe.mNext[1];
			mPlaneNum	=copyMe.mPlaneNum;
			mOnNode		=copyMe.mOnNode;
			mFace[0]	=copyMe.mFace[0];
			mFace[1]	=copyMe.mFace[1];
			mSide		=copyMe.mSide;
			mSideFound	=copyMe.mSideFound;
		}


		internal static GBSPPortal CreateOutsidePortal(GBSPPlane Plane, GBSPNode Node, GBSPNode outNode, PlanePool pool)
		{
			GBSPPortal	NewPortal;
			sbyte		Side;

			NewPortal	=new GBSPPortal();
			if(NewPortal == null)
			{
				return	null;
			}

			NewPortal.mPoly	=new GBSPPoly(Plane);
			if(NewPortal.mPoly == null || NewPortal.mPoly.IsTiny())
			{
				return	null;
			}
			NewPortal.mPlaneNum	=pool.FindPlane(Plane, out Side);

			if(NewPortal.mPlaneNum == -1)
			{
				Map.Print("CreateOutsidePortal:  Could not create plane.\n");
				return	null;
			}

			if(Side == 0)
			{
				if(!GBSPNode.AddPortalToNodes(NewPortal, Node, outNode))
				{
					return	null;
				}
			}
			else
			{
				if(!GBSPNode.AddPortalToNodes(NewPortal, outNode, Node))
				{
					return	null;
				}
			}
			return	NewPortal;
		}


		internal bool Check()
		{
			if(mPoly.mVerts.Count < 3)
			{
				Map.Print("CheckPortal:  NumVerts < 3.\n");
				return	false;
			}

			for(int i=0;i < mPoly.mVerts.Count;i++)
			{
				for(int k=0;k < 3;k++)
				{
					float	val	=UtilityLib.Mathery.VecIdx(mPoly.mVerts[i], k);

					if(val == Brush.MIN_MAX_BOUNDS)
					{
						Map.Print("CheckPortal:  Portal was not clipped on all sides!!!\n");
						return	false;
					}
					if(val == -Brush.MIN_MAX_BOUNDS)
					{
						Map.Print("CheckPortal:  Portal was not clipped on all sides!!!\n");
						return	false;
					}
				}
			}
			return	true;
		}
	}
}
