using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PathLib
{
	public class PathConnection
	{
		public PathNode			mConnectedTo;
		public float			mDistanceToCenter;
		public ConvexPoly.Edge	mEdgeBetween;
	}
}
