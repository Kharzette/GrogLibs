using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;


namespace PathLib
{
	public class PathNode
	{
		//raw stuff for the grid
		public ConvexPoly	mPoly;

		public List<PathConnection>	mConnections	=new List<PathConnection>();


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
	}
}
