using System;
using System.Numerics;


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
