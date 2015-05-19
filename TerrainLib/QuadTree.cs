using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using SharpDX;


namespace TerrainLib
{
	internal class QuadTree
	{
		QuadNode	mRoot;

		internal static int	LeafPoints	=16;


		internal QuadTree()
		{
			mRoot	=null;
		}


		internal void Build(float [,]data, BoundingBox bound)
		{
			mRoot	=new QuadNode();

			mRoot.Build(data, bound);
		}
	}
}
