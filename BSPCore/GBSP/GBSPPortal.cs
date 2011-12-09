using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;


namespace BSPCore
{
	internal class GBSPPortal
	{
		internal GBSPPoly	mPoly;					//Convex poly that holds the shape of the portal
		internal GBSPNode	mFrontNode, mBackNode;	//Node on each side of the portal
		internal GBSPPortal	mFrontPort, mBackPort;	//Next portal for each node
		internal GBSPPlane	mPlane;

		internal GBSPNode	mOnNode;
		internal GBSPFace	mFrontFace, mBackFace;
		internal GBSPSide	mSide;
		internal byte		mSideFound;


		internal GBSPPortal() { }
		internal GBSPPortal(GBSPPortal copyMe)
		{
			mPoly		=new GBSPPoly(copyMe.mPoly);
			mFrontNode	=copyMe.mFrontNode;
			mBackNode	=copyMe.mBackNode;
			mFrontPort	=copyMe.mFrontPort;
			mBackPort	=copyMe.mBackPort;
			mPlane		=copyMe.mPlane;
			mOnNode		=copyMe.mOnNode;
			mFrontFace	=copyMe.mFrontFace;
			mBackFace	=copyMe.mBackFace;
			mSide		=copyMe.mSide;
			mSideFound	=copyMe.mSideFound;
		}


		internal GBSPFace FaceFromPortal(Int32 planeSide)
		{
			if(mSide == null)
			{
				return	null;	//Portal does not bridge different visible contents
			}

			if(planeSide == 0)
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
			return	new GBSPFace(this, planeSide);
		}


		internal void FindPortalSide(PlanePool pool)
		{
			GBSPSide	bestSide	=mOnNode.GetBestPortalSide(mFrontNode, mBackNode, pool);
			if(bestSide == null)
			{
				return;
			}

			mSideFound	=1;
			mSide		=bestSide;
		}


		internal bool CanSeeThroughPortal()
		{
			UInt32	c1, c2;

			//Can't see into or from solid
			if(mFrontNode.IsContentsSolid() || mBackNode.IsContentsSolid())
			{
				return	false;
			}

			if(mOnNode == null)
			{
				return	false;
			}

			//'Or' together all cluster contents under portals nodes
			c1	=mFrontNode.ClusterContents();
			c2	=mBackNode.ClusterContents();

			//Can only see through portal if contents on both sides are translucent...
			//if ((c1 & BSP_CONTENTS_TRANSLUCENT) && (c2 & BSP_CONTENTS_TRANSLUCENT))
			//	return GE_TRUE;

			if(MajorVisibleContents(c1^c2) == 0)
			{
				return	true;
			}

			//Cancel solid if it's detail, or translucent on the leafs/clusters
			if((c1 & (Contents.BSP_CONTENTS_TRANSLUCENT2
				| Contents.BSP_CONTENTS_DETAIL2)) != 0)
			{
				c1 = 0;
			}
			if((c2 & (Contents.BSP_CONTENTS_TRANSLUCENT2
				| Contents.BSP_CONTENTS_DETAIL2)) != 0)
			{
				c2 = 0;
			}

			if(((c1 | c2) & Contents.BSP_CONTENTS_SOLID2) != 0)
			{
				return	false;	//If it's solid on either side, return GE_FALSE
			}

			if((c1 ^ c2) == 0)
			{
				return	true;	//If it's the same on both sides then we can definitly see through it...
			}

			if(MajorVisibleContents(c1^c2) == 0)
			{
				return	true;
			}
			return	false;
		}


		UInt32 MajorVisibleContents(UInt32 con)
		{
			Int32	j;
			UInt32	MajorContents;

			if(con == 0)
			{
				return	0;
			}

			// Only check visible contents
			con	&=Contents.BSP_VISIBLE_CONTENTS;
			
			//Return the strongest one, return the first lsb
			for(j=0;j < 32;j++)
			{
				MajorContents	=(con & (1U << j));
				if(MajorContents != 0)
				{
					return MajorContents;
				}
			}

			return 0;
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
