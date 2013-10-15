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
		internal ConvexPoly	mPoly;	//ref, multiple nodes might point to it
		internal Vector3	mPoint;
		internal float		mHScorePenalty;	//use to discourage use

		internal List<PathConnection>	mConnections	=new List<PathConnection>();


		internal PathNode(ConvexPoly cp, Vector3 point)
		{
			mPoly	=cp;
			mPoint	=point;
		}


		internal float DistanceBetweenNodes(PathNode pn2)
		{
			return	Vector3.Distance(mPoint, pn2.mPoint);
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
