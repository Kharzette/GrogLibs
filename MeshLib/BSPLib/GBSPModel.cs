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


		internal bool ProcessWorldModel(List<MapBrush> list, List<MapEntity> ents, PlanePool pool, TexInfoPool tip)
		{
			GBSPBrush	prev	=null;

			list.Reverse();
			foreach(MapBrush b in list)
			{
				GBSPBrush	gb	=new GBSPBrush(b);

				//if brush is being dropped, the mOriginal
				//reference will be null
				if(gb.mOriginal == null)
				{
					continue;
				}

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

			for(prev = mGBSPBrushes;prev != null;prev=prev.mNext)
			{
				if(prev.mOriginal == null)
				{
					int	gack	=0;
					gack++;
				}
			}

			mGBSPBrushes	=GBSPBrush.CSGBrushes(mGBSPBrushes, pool);


			GBSPNode	root	=new GBSPNode();
			root.BuildBSP(mGBSPBrushes, pool, ref mBounds);

			mGBSPBrushes	=null;
			prev			=null;

			if(!root.CreatePortals(this, false, pool))
			{
				Map.Print("Could not create the portals.\n");
				return	false;
			}

			int	numRemovedLeafs	=0;
			if(root.RemoveHiddenLeafs(mOutsideNode, ents, ref numRemovedLeafs, pool) == -1)
			{
				Map.Print("Failed to remove hidden leafs.\n");
			}

			root.MarkVisibleSides(list, pool);

			if(!root.FreePortals())
			{
				Map.Print("BuildBSP:  Could not free portals.\n");
				return	false;
			}

			root.FreeBSP_r();

			foreach(MapBrush b in list)
			{
				GBSPBrush	gb	=new GBSPBrush(b);

				//if brush is being dropped, the mOriginal
				//reference will be null
				if(gb.mOriginal == null)
				{
					continue;
				}

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

			root.BuildBSP(mGBSPBrushes, pool, ref mBounds);

			if(!root.CreatePortals(this, false, pool))
			{
				Map.Print("Could not create the portals.\n");
				return	false;
			}

			numRemovedLeafs	=0;
			if(root.RemoveHiddenLeafs(mOutsideNode, ents, ref numRemovedLeafs, pool) == -1)
			{
				Map.Print("Failed to remove hidden leafs.\n");
			}

			root.MarkVisibleSides(list, pool);

			root.MakeFaces(pool, tip);

			root.MakeLeafFaces();

			if(!root.FreePortals())
			{
				Map.Print("BuildBSP:  Could not free portals.\n");
				return	false;
			}

			root.MergeNodes();

			mRootNode[0]	=root;

			return	true;
		}


		internal bool ProcessSubModel(List<MapBrush> list, PlanePool pool, TexInfoPool tip)
		{
			GBSPBrush	prev	=null;

			list.Reverse();
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
			root.BuildBSP(mGBSPBrushes, pool, ref mBounds);

			mGBSPBrushes	=null;
			prev			=null;

			if(!root.CreatePortals(this, false, pool))
			{
				Map.Print("Could not create the portals.\n");
				return	false;
			}

			root.MarkVisibleSides(list, pool);

			root.MakeFaces(pool, tip);

			if(!root.FreePortals())
			{
				Map.Print("BuildBSP:  Could not free portals.\n");
				return	false;
			}

			root.MergeNodes();

			mRootNode[0]	=root;

			return	true;
		}


		internal void GetTriangles(List<Vector3> verts, List<uint> indexes, bool bCheck)
		{
//			for(GBSPBrush b = mGBSPBrushes;b != null;b=b.mNext)
//			{
//				b.GetTriangles(verts, indexes, bCheck);
//			}
			mRootNode[0].GetLeafTriangles(verts, indexes, bCheck);
		}
	}
}
