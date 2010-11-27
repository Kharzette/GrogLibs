using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class GBSPModel
	{
		public GBSPNode	[]mRootNode	=new GBSPNode[2];
		public Vector3	mOrigin;
		public GBSPNode	mOutsideNode	=new GBSPNode();
		public Bounds	mBounds;

		//for saving, might delete
		public int		[]mRootNodeID	=new int[2];
		public int		mFirstFace, mNumFaces;
		public int		mFirstLeaf, mNumLeafs;
		public int		mFirstCluster, mNumClusters;
		public int		mNumSolidLeafs;

		//area portal stuff, probably won't use
		public bool		mbAreaPortal;
		public int		[]Areas	=new int[2];

		//temporary
		GBSPBrush	mGBSPBrushes;


		internal bool ProcessWorldModel(List<MapBrush> list, PlanePool pool)
		{
			GBSPBrush	prev	=null;
			foreach(MapBrush b in list)
			{
				GBSPBrush	gb	=new GBSPBrush(b);

				if(prev != null)
				{
					prev.mNext	=gb;
				}

				if(mGBSPBrushes == null)
				{
					mGBSPBrushes	=gb;
				}
				prev	=gb;
			}

			mGBSPBrushes	=GBSPBrush.CSGBrushes(mGBSPBrushes, pool);

			GBSPNode	root	=new GBSPNode();
//			root.BuildBSP(mGBSPBrushes, pool, ref mBounds);

			//remove
//			mRootNode[0]	=root;

/*			if(!root.CreatePortals(this, false, pool))
			{
				Map.Print("Could not create the portals.\n");
				return	false;
			}*/

			mRootNode[0]	=root;

			return	true;
		}


		internal void ProcessSubModel(List<MapBrush> list, PlanePool pool)
		{
			GBSPBrush	prev	=null;
			foreach(MapBrush b in list)
			{
				GBSPBrush	gb	=new GBSPBrush(b);

				if(prev != null)
				{
					prev.mNext	=gb;
				}

				if(mGBSPBrushes == null)
				{
					mGBSPBrushes	=gb;
				}
				prev	=gb;
			}

			GBSPBrush.CSGBrushes(mGBSPBrushes, pool);

			GBSPNode	root	=new GBSPNode();
			root.BuildBSP(mGBSPBrushes, pool, ref mBounds);

			mRootNode[0]	=root;
		}


		internal void GetTriangles(List<Vector3> verts, List<uint> indexes, bool bCheck)
		{
			for(GBSPBrush b = mGBSPBrushes;b != null;b=b.mNext)
			{
				b.GetTriangles(verts, indexes, bCheck);
			}
//			mRootNode[0].GetTriangles(verts, indexes, bCheck);

			/*int	Side	=0;
			for(GBSPPortal p = mRootNode[0].mPortals;p != null;p=p.mNext[Side])
			{
				Side	=(p.mOnNode == p.mNodes[0])? 0 : 1;

				p.mSide.GetTriangles(verts, indexes, bCheck);
			}*/
		}
	}
}
