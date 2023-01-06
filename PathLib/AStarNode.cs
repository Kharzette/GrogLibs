using System;

namespace PathLib
{
	class AStarNode
	{
		internal PathNode	mNode;
		internal AStarNode	mParent;
		internal float		mGScore, mHScore;
	}
}
