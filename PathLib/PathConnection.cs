using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PathLib
{
	internal class PathConnection
	{
		internal PathNode	mConnectedTo;
		internal float		mDistanceToCenter;
		internal Edge		mEdgeBetween;
		internal bool		mbPassable;	//might be blocked by a locked door or obstacle
	}
}
