using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;


namespace PathLib
{
	internal class PathNode
	{
		//raw stuff for the grid
		internal ConvexPoly	mPoly;
		internal float		mHScorePenalty;	//use to discourage use

		internal List<PathConnection>	mConnections	=new List<PathConnection>();


		internal PathNode(ConvexPoly cp)
		{
			mPoly	=cp;
		}


		internal float CenterToCenterDistance(PathNode pn2)
		{
			Vector3	myPos		=mPoly.GetCenter();
			Vector3	otherPos	=pn2.mPoly.GetCenter();

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
