using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PathLib
{
	class AStarNode
	{
		public PathNode		mNode;
		public AStarNode	mParent;
		public float		mGScore, mHScore;
	}
}
