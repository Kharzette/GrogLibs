using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;


namespace PathLib
{
	internal class PathConnection
	{
		internal PathNode	mConnectedTo;
		internal float		mDistanceBetween;
		internal bool		mbUseEdge;
		internal Vector3	mEdgeBetween;	//on edge point between connected
		internal bool		mbPassable;		//might be blocked by a locked door or obstacle
	}
}
