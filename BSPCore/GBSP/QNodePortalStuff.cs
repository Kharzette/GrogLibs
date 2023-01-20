using System;
using System.IO;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using Vortice.Mathematics;
using UtilityLib;


namespace BSPCore;

internal partial class GBSPNode
{
	void	SplitNodePortals(PlanePool pool, ClipPools cp)
	{
		GBSPPlane	plane	=pool.mPlanes[mPlaneNum];

		GBSPNode	other	=null;
		GBSPPortal	next	=null;
		for(GBSPPortal p=mPortals;p != null;p=next)
		{
			bool	bSide;
			if(p.mFrontNode == this)
			{
				bSide	=false;
				next	=p.mNextFront;
				other	=p.mBackNode;
			}
			else if(p.mBackNode == this)
			{
				bSide	=true;
				next	=p.mNextBack;
				other	=p.mFrontNode;
			}
			else
			{
				CoreEvents.Print("SplitNodePortals:  Mislinked portal\n");
				return;
			}

			RemovePortalFromNode(p, p.mFrontNode);
			RemovePortalFromNode(p, p.mBackNode);

			GBSPPoly	frontPoly, backPoly;

			//cut portal in two
			if(!p.mPoly.SplitEpsilon(0.001f, plane, out frontPoly, out backPoly, false))
			{
				CoreEvents.Print("SplitNodePortals:  Error splitting portal\n");
				return;
			}

			if(frontPoly != null && frontPoly.IsTiny())
			{
				frontPoly.Free();
				frontPoly	=null;
			}
			if(backPoly != null && backPoly.IsTiny())
			{
				backPoly.Free();
				backPoly	=null;
			}

			if(frontPoly == null && backPoly == null)
			{
				continue;
			}

			if(frontPoly != null)
			{
				backPoly.Free();
				if(bSide)
				{
					AddPortalToNodes(p, other, mBack);
				}
				else
				{
					AddPortalToNodes(p, mBack, other);
				}
				continue;
			}

			if(backPoly != null)
			{
				frontPoly.Free();
				if(bSide)
				{
					AddPortalToNodes(p, other, mFront);
				}
				else
				{
					AddPortalToNodes(p, mFront, other);
				}
				continue;
			}

			//split, copy portal
			p.mPoly.Free();
			GBSPPortal	newPortal	=new GBSPPortal(p);

			newPortal.mPoly	=backPoly;
			p.mPoly			=frontPoly;

			if(bSide)
			{
				AddPortalToNodes(p, other, mFront);
				AddPortalToNodes(newPortal, other, mBack);
			}
			else
			{
				AddPortalToNodes(p, mFront, other);
				AddPortalToNodes(newPortal, mBack, other);
			}
		}

		mPortals	=null;
	}


	void	MakeTreePortals_r(PlanePool pool, ClipPools cp)
	{
		CalcBoundsFromPortals();
		if(mBounds.mMins.X >= mBounds.mMaxs.X)
		{
			CoreEvents.Print("MakeTreePortals_r:  Node with no volume\n");
		}

		for(int i=0;i < 3;i++)
		{
			if(mBounds.mMins.ArrayAccess(i) < -8000 || mBounds.mMaxs.ArrayAccess(i) > 8000)
			{
				CoreEvents.Print("MakeTreePortals_r:  Node with unbounded volume\n");
				break;
			}
		}
		if(mPlaneNum == PlanePool.PLANENUM_LEAF)
		{
			return;
		}

		MakeNodePortal(pool, cp);
		SplitNodePortals(pool, cp);

		mFront.MakeTreePortals_r(pool, cp);
		mBack.MakeTreePortals_r(pool, cp);
	}

    
	static bool	RemovePortalFromNode(GBSPPortal port, GBSPNode node)
	{
		Debug.Assert(node.mPortals != null);
		Debug.Assert(!(port.mFrontNode == node && port.mBackNode == node));
		Debug.Assert(port.mFrontNode == node || port.mBackNode == node);

		//find the portal on this node
		GBSPPortal	t			=null;
		GBSPPortal	prevPort	=null;
		bool		prevSide	=false;
		for(t=node.mPortals;;)
		{
			if(t == null)
			{
				CoreEvents.Print("RemovePortalFromNode:  portal not in leaf\n");
				return false;
			}

			if(t == port)
			{
				break;
			}

			if(t.mFrontNode == node)
			{
				prevPort	=t;
				prevSide	=false;
				t			=t.mNextFront;
			}
			else
			{
				prevPort	=t;
				prevSide	=true;
				t			=t.mNextBack;
			}
//			else
//			{
//				CoreEvents.Print("RemovePortalFromNode:  portal not bounding leaf\n");
//				return false;
//			}
		}

		//this is very confusing, but it is basically
		//pointing the previous portal to port->next
		//wayway easier with pointers
		if(port.mFrontNode == node)
		{
			if(prevPort == null)
			{
				//very first portal in the list
				node.mPortals	=port.mNextFront;
				port.mFrontNode	=null;
			}
			else if(prevSide)
			{
				prevPort.mNextBack	=port.mNextFront;
				port.mNextFront		=null;
			}
			else
			{
				prevPort.mNextFront	=port.mNextFront;
				port.mNextFront		=null;
			}
		}
		else if(port.mBackNode == node)
		{
			if(prevPort == null)
			{
				//very first portal in the list
				node.mPortals	=port.mNextBack;
				port.mBackNode	=null;
			}
			else if(prevSide)
			{
				prevPort.mNextBack	=port.mNextBack;
				port.mNextBack		=null;
			}
			else
			{
				prevPort.mNextFront	=port.mNextBack;
				port.mNextBack		=null;
			}
		}
		return	true;
	}


	static bool	AddPortalToNodes(GBSPPortal port, GBSPNode front, GBSPNode back)
	{
		if(port.mFrontNode != null || port.mBackNode != null)
		{
			CoreEvents.Print("AddPortalToNodes:  Portal already looks at one of the nodes.\n");
			return	false;
		}

		port.mFrontNode	=front;
		port.mNextFront	=front.mPortals;
		front.mPortals	=port;

		port.mBackNode	=back;
		port.mNextBack	=back.mPortals;
		back.mPortals	=port;

		return	true;
	}
}