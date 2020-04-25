using System;
using System.IO;


namespace BSPCore
{
	internal class GBSPPortal
	{
		internal GBSPPoly	mPoly;					//Convex poly that holds the shape of the portal
		internal GBSPNode	mFrontNode, mBackNode;	//Node on each side of the portal
		internal GBSPPlane	mPlane;

		internal GBSPNode	mOnNode;
		internal GBSPFace	mFrontFace, mBackFace;
		internal GBSPSide	mSide;
		internal bool		mSideFound;


		internal GBSPPortal() { }
		internal GBSPPortal(GBSPPortal copyMe)
		{
			mPoly		=new GBSPPoly(copyMe.mPoly);
			mFrontNode	=copyMe.mFrontNode;
			mBackNode	=copyMe.mBackNode;
			mPlane		=copyMe.mPlane;
			mOnNode		=copyMe.mOnNode;
			mFrontFace	=copyMe.mFrontFace;
			mBackFace	=copyMe.mBackFace;
			mSide		=copyMe.mSide;
			mSideFound	=copyMe.mSideFound;
		}


		internal GBSPFace FaceFromPortal(bool bFlip)
		{
			if(mSide == null)
			{
				return	null;	//Portal does not bridge different visible contents
			}
			if((mSide.mFlags & GBSPSide.SIDE_VISIBLE) == 0)
			{
				return	null;	//invisible side
			}

			if(!bFlip)
			{
				if(GBSPNode.WindowCheck(mFrontNode, mBackNode))
				{
					return	null;
				}
			}
			else
			{
				if(GBSPNode.WindowCheck(mBackNode, mFrontNode))
				{
					return	null;
				}
			}
			return	new GBSPFace(this, bFlip);
		}


		internal void FindPortalSide(PlanePool pool)
		{
			GBSPSide	bestSide	=mOnNode.GetBestPortalSide(mFrontNode, mBackNode, pool);
			if(bestSide == null)
			{
				return;
			}

			mSideFound	=true;
			mSide		=bestSide;
		}


		internal bool CanSeeThroughPortal()
		{
			UInt32	c1, c2;

			if(mOnNode == null)
			{
				return	false;
			}

			//'Or' together all cluster contents under portals nodes
			c1	=mFrontNode.ClusterContents();
			c2	=mBackNode.ClusterContents();

			if(GrogContents.VisSeeThru(c1) && GrogContents.VisSeeThru(c2))
			{
				return	true;
			}
			return	false;
		}


		internal bool Check()
		{
			if(mPoly.VertCount() < 3)
			{
				CoreEvents.Print("CheckPortal:  NumVerts < 3.\n");
				return	false;
			}

			if(mPoly.IsMaxExtents())
			{
				CoreEvents.Print("CheckPortal:  Portal was not clipped on all sides!!!\n");
				return	false;
			}
			return	true;
		}


		internal void Write(BinaryWriter bw)
		{
			mPoly.Write(bw);
		}
	}
}
