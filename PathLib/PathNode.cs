using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;


namespace PathLib
{
	internal class PathNode
	{
		//raw stuff for grid or navmesh
		internal ConvexPoly	mPoly;
		internal Vector3	mPoint;
		internal float		mHScorePenalty;	//use to discourage use

		internal List<PathConnection>	mConnections	=new List<PathConnection>();


		internal PathNode(ConvexPoly cp)
		{
			mPoly	=cp;
		}

		internal PathNode(Vector3 point)
		{
			mPoint	=point;
		}


		internal float CenterToCenterDistance(PathNode pn2)
		{
			Vector3	myPos		=Vector3.Zero;
			Vector3	otherPos	=Vector3.Zero;

			if(mPoly != null)
			{
				myPos		=mPoly.GetCenter();
			}
			else
			{
				myPos	=mPoint;
			}

			if(pn2.mPoly != null)
			{
				otherPos	=pn2.mPoly.GetCenter();
			}
			else
			{
				otherPos	=pn2.mPoint;
			}

			return	Vector3.Distance(myPos, otherPos);
		}


		internal Edge FindEdgeBetween(PathNode pathNode)
		{
			foreach(PathConnection pc in mConnections)
			{
				if(pc.mConnectedTo == pathNode)
				{
					return	pc.mEdgeBetween;
				}
			}
			return	null;
		}
	}
}
