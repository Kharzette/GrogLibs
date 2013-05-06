using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PathLib
{
	class AStarNode
	{
		internal PathNode	mNode;
		internal AStarNode	mParent;
		internal float		mGScore, mHScore;
	}
}
