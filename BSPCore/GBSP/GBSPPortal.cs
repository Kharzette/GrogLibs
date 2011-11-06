using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPCore
{
	internal class GBSPPortal
	{
		internal GBSPPoly	mPoly;							//Convex poly that holds the shape of the portal
		internal GBSPNode	[]mNodes	=new GBSPNode[2];	//Node on each side of the portal
		internal GBSPPortal	[]mNext		=new GBSPPortal[2];	//Next portal for each node
		internal Int32		mPlaneNum;

		internal GBSPNode	mOnNode;
		internal GBSPFace	[]mFace	=new GBSPFace[2];
		internal GBSPSide	mSide;
		internal byte		mSideFound;


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


		internal static GBSPPortal CreateOutsidePortal(GBSPPlane plane,
			GBSPNode node, PlanePool pool, ref GBSPNode outsideNode)
		{
			GBSPPortal	newPortal;
			sbyte		side;

			newPortal	=new GBSPPortal();
			if(newPortal == null)
			{
				return	null;
			}

			newPortal.mPoly	=new GBSPPoly(plane);
			if(newPortal.mPoly == null || newPortal.mPoly.IsTiny())
			{
				return	null;
			}
			newPortal.mPlaneNum	=pool.FindPlane(plane, out side);

			if(newPortal.mPlaneNum == -1)
			{
				CoreEvents.Print("CreateOutsidePortal:  Could not create plane.\n");
				return	null;
			}

			if(side == 0)
			{
				if(!GBSPNode.AddPortalToNodes(newPortal, node, outsideNode))
				{
					return	null;
				}
			}
			else
			{
				if(!GBSPNode.AddPortalToNodes(newPortal, outsideNode, node))
				{
					return	null;
				}
			}
			return	newPortal;
		}


		internal GBSPFace FaceFromPortal(Int32 planeSide)
		{
			Int32	notPlaneSide	=(planeSide == 0)? 1 : 0;

			GBSPSide	Side	=mSide;			
			if(Side == null)
			{
				return	null;	//Portal does not bridge different visible contents
			}

			if(GBSPNode.WindowCheck(mNodes[planeSide], mNodes[notPlaneSide]))
			{
				return	null;
			}

			return	new GBSPFace(this, planeSide);
		}


		internal void FindPortalSide(PlanePool pool)
		{
			GBSPSide	bestSide	=mOnNode.GetBestPortalSide(mNodes[0], mNodes[1], pool);
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
			if(mNodes[0].IsContentsSolid() || mNodes[1].IsContentsSolid())
			{
				return	false;
			}

			if(mOnNode == null)
			{
				return	false;
			}

			//'Or' together all cluster contents under portals nodes
			c1	=mNodes[0].ClusterContents();
			c2	=mNodes[1].ClusterContents();

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


		internal void Merge(GBSPNode n, PlanePool pool)
		{
			//make a list of possible merge candidates
			List<GBSPPortal>	nextPorts	=new List<GBSPPortal>();

			Vector3	norm	=pool.mPlanes[mPlaneNum].mNormal;

			for(GBSPPortal port=this;port != null;)
			{
				if(port == this)
				{
					port	=port.GetNextPortal(n);
					continue;
				}
				if(port.mPoly == null || port.mPoly.VertCount() < 3)
				{
					port	=port.GetNextPortal(n);
					continue;
				}
				if(!port.CanSeeThroughPortal())
				{
					port	=port.GetNextPortal(n);
					continue;
				}

				nextPorts.Add(port);

				port	=port.GetNextPortal(n);
			}

			GBSPPoly	merged	=new GBSPPoly(mPoly);

			List<GBSPPortal>	removed	=new List<GBSPPortal>();

			foreach(GBSPPortal port in nextPorts)
			{
				GBSPPoly	reMerged	=GBSPPoly.Merge(merged, port.mPoly, norm, pool);

				if(reMerged == null || reMerged.VertCount() < 3)
				{
					continue;
				}

				merged	=new GBSPPoly(reMerged);

				removed.Add(port);
			}

			mPoly	=merged;

			for(GBSPPortal port=this;port != null;)
			{
				GBSPPortal	next	=port.GetNextPortal(n);
				while(next != null && removed.Contains(next))
				{
					next	=next.GetNextPortal(n);
				}

				if(mNodes[0] == n)
				{
					port.mNext[0]	=next;
				}
				else
				{
					port.mNext[1]	=next;
				}

				port	=next;
			}
		}


		GBSPPortal GetNextPortal(GBSPNode n)
		{
			if(mNodes[0] == n)
			{
				return	mNext[0];
			}
			else
			{
				return	mNext[1];
			}
		}
	}
}
