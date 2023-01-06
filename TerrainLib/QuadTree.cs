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

		internal static int	LeafPoints	=4;


		internal QuadTree()
		{
			mRoot	=null;
		}


		internal void Build(float [,]data, BoundingBox bound)
		{
			mRoot	=new QuadNode();

			mRoot.Build(data, bound);
		}


		//returns all node boxes, for debug draw
		internal List<BoundingBox> GetAllBoxes()
		{
			List<BoundingBox>	ret	=new List<BoundingBox>();

			mRoot.GetAllBoxes(ret);

			return	ret;
		}


		internal void FixBoxHeights(float[,] heightGrid)
		{
			mRoot.FixBoxHeights(heightGrid);
		}


		internal bool Trace(Vector3 start, Vector3 end, out Vector3 hit)
		{
			return	mRoot.Trace(start, end, out hit);
		}
	}
}
