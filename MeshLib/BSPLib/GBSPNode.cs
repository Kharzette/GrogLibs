using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class GBSPNode
	{
		// Info for this node as a node or leaf
		public Int32			mPlaneNum;						// -1 if a leaf
		public Int32			mPlaneSide;						// CHANGE1!!!
		UInt32			mContents;						// Contents node/leaf
		List<GBSPFace>	mFaces	=new List<GBSPFace>();	// Faces on this node
		GBSPNode		mFront, mBack;					// Front and back child
		GBSPNode		mParent;						// Parent of this node
		Bounds			mBounds	=new Bounds();			// Current BBox of node

		// Info for this node as a leaf
		List<GBSPPortal>	mPortals	=new List<GBSPPortal>();	// Portals on this leaf
		Int32				mNumLeafFaces;							// Number of faces touching this leaf
		List<GBSPFace>		mLeafFaces	=new List<GBSPFace>();		// Pointer to Faces touching this leaf
		Int32				mCurrentFill;							// For the outside filling stage
		Int32				mEntity;								// 1 if entity touching leaf
		Int32				mOccupied;								// FIXME:  Can use Entity!!!
		Int32				mPortalLeafNum;							// For portal saving

		public bool	mDetail;
		Int32	mCluster;
		Int32	mArea;				// Area number, 0 == invalid area

		GBSPBrush			mVolume;
		GBSPSide			mSide;
		List<GBSPBrush>		mBrushList	=new List<GBSPBrush>();

		// For GFX file saving
		Int32	mChildIDFront, mChildIDBack;
		Int32	mFirstFace;
		Int32	mNumFaces;
		Int32	mFirstPortal;
		Int32	mNumPortals;

		Int32	mFirstSide;			// For bevel bbox clipping
		Int32	mNumSides;


		internal void BuildBSP(List<GBSPBrush> brushList, PlanePool pool)
		{
			Int32		NumVisFaces, NumNonVisFaces;
			Int32		NumVisBrushes;
			float		Volume;
			Bounds		bounds	=new Bounds();

			Map.Print("--- Build BSP Tree ---\n");

			float	MicroVolume	=0.1f;

			NumVisFaces		=0;
			NumNonVisFaces	=0;
			NumVisBrushes	=0;

			foreach(GBSPBrush b in brushList)
			{
				NumVisBrushes++;
				
				Volume	=b.Volume(pool);
				if(Volume < MicroVolume)
				{
					Map.Print("**WARNING** BuildBSP: Brush with NULL volume\n");
				}
				
				for(int i=0;i < b.mSides.Count;i++)
				{
					if(b.mSides[i].mPoly.mVerts.Count < 3)
					{
						continue;
					}
					if((b.mSides[i].mFlags & GBSPSide.SIDE_NODE) != 0)
					{
						continue;
					}
					if((b.mSides[i].mFlags & GBSPSide.SIDE_VISIBLE) != 0)
					{
						NumVisFaces++;
					}
					else
					{
						NumNonVisFaces++;
					}
				}

				bounds.Merge(b.mBounds, null);
			}
			Map.Print("Total Brushes          : " + NumVisBrushes + "\n");
			Map.Print("Total Faces            : " + NumVisFaces + "\n");
			Map.Print("Faces Removed          : " + NumNonVisFaces + "\n");
						
			int	NumVisNodes		=0;
			int	NumNonVisNodes	=0;

			BuildTree_r(brushList, pool, ref NumVisNodes, ref NumNonVisNodes);
			
			//Top node is always valid, this way portals can use top node to get box of entire bsp...
			mBounds	=bounds;

//			TreeMins = Mins;
//			TreeMaxs = Maxs;

			Map.Print("Total Nodes            : " + (NumVisNodes/2 - NumNonVisNodes) + "\n");
			Map.Print("Nodes Removed          : " + NumNonVisNodes + "\n");
			Map.Print("Total Leafs            : " + (NumVisNodes+1)/2 + "\n");
		}


		void LeafNode(List<GBSPBrush> Brushes)
		{
			mPlaneNum	=PlanePool.PLANENUM_LEAF;
			mContents	=0;

			//Get the contents of this leaf, by examining all the brushes that made this leaf
			foreach(GBSPBrush b in Brushes)
			{
				if((b.mOriginal.mContents & GBSPBrush.BSP_CONTENTS_SOLID2) != 0)
				{
					int	i=0;
					for(i=0;i < b.mSides.Count;i++)
					{
						if((b.mSides[i].mFlags & GBSPSide.SIDE_NODE) == 0)
						{
							break;
						}
					}
				
					//If all the planes in this leaf where caused by splits, then
					//we can force this leaf to be solid...
					if(i == b.mSides.Count)
					{
						//Node->Contents &= 0xffff0000;
						mContents	|=GBSPBrush.BSP_CONTENTS_SOLID2;
						//break;
					}
					
				}
				
				mContents	|=b.mOriginal.mContents;
			}

			//Once brushes get down to the leafs, we don't need to keep the polys on them anymore...
			//We can free them now...
			foreach(GBSPBrush b in Brushes)
			{
				foreach(GBSPSide side in b.mSides)
				{
					side.mPoly.mVerts.Clear();
				}
			}

			mBrushList	=Brushes;
		}


		internal bool CheckPlaneAgainstParents(Int32 PNum)
		{
			for(GBSPNode p=mParent;p != null;p = p.mParent)
			{
				if(p.mPlaneNum == PNum)
				{
					Map.Print("Tried parent");
					return	false;
				}
			}
			return	true;
		}


		void BuildTree_r(List<GBSPBrush> brushes, PlanePool pool, ref int NumVisNodes, ref int NumNonVisNodes)
		{
			GBSPSide	BestSide;

			List<GBSPBrush>	childrenFront	=new List<GBSPBrush>();
			List<GBSPBrush>	childrenBack	=new List<GBSPBrush>();

			NumVisNodes++;

			//find the best plane to use as a splitter
			BestSide	=GBSPBrush.SelectSplitSide(brushes, this, pool, ref NumNonVisNodes);
			
			if(BestSide == null)
			{
				//leaf node
				mSide		=null;
				mPlaneNum	=PlanePool.PLANENUM_LEAF;
				LeafNode(brushes);
				return;
			}

			//This is a splitplane node
			mSide		=BestSide;
			mPlaneNum	=BestSide.mPlaneNum;

			GBSPBrush.SplitBrushList(brushes, this, pool, childrenFront, childrenBack);

			brushes.Clear();
			
			//Allocate children before recursing
			mFront			=new GBSPNode();
			mFront.mParent	=this;
			mBack			=new GBSPNode();
			mBack.mParent	=this;

			//Recursively process children
			mFront.BuildTree_r(childrenFront, pool, ref NumVisNodes, ref NumNonVisNodes);
			mBack.BuildTree_r(childrenBack, pool, ref NumVisNodes, ref NumNonVisNodes);
		}


		internal void GetTriangles(List<Vector3> verts, List<UInt32> indexes, bool bCheckFlags)
		{
			if(mSide != null)
			{
				mSide.GetTriangles(verts, indexes, bCheckFlags);
			}
			if(mPlaneNum == PlanePool.PLANENUM_LEAF)
			{
				return;
			}

			mFront.GetTriangles(verts, indexes, bCheckFlags);
			mBack.GetTriangles(verts, indexes, bCheckFlags);
		}
	}
}
