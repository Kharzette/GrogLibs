using System;
using System.IO;


namespace BSPCore;

internal class GBSPPortal
{
	internal GBSPPlane	mPlane;
	internal GBSPNode	mOnNode;
	internal GBSPNode	mFrontNode, mBackNode;	//Node on each side of the portal
	internal GBSPPortal	mNextFront, mNextBack;	//Next portal for each node
	internal GBSPPoly	mPoly;					//Convex poly that holds the shape of the portal

	internal bool		mbSideFound;
	internal GBSPSide	mSide;
	internal GBSPFace	mFrontFace, mBackFace;


	internal GBSPPortal() { }

	internal GBSPPortal(GBSPPortal copyMe)
	{
		mPlane		=copyMe.mPlane;
		mOnNode		=copyMe.mOnNode;
		mFrontNode	=copyMe.mFrontNode;
		mBackNode	=copyMe.mBackNode;
		mNextFront	=copyMe.mNextFront;
		mNextBack	=copyMe.mNextBack;
		mPoly		=new GBSPPoly(copyMe.mPoly);

		mFrontFace	=copyMe.mFrontFace;
		mBackFace	=copyMe.mBackFace;
		mSide		=copyMe.mSide;
		mbSideFound	=copyMe.mbSideFound;
	}


	internal void Free()
	{
		if(mPoly != null)
		{
			mPoly.Free();
		}
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
		GBSPSide	bestSide	=GBSPNode.FindPortalSide(this, pool);
		if(bestSide == null)
		{
			return;
		}

		mbSideFound	=true;
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