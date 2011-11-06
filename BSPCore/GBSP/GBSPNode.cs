using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;


namespace BSPCore
{
	internal class BuildStats
	{
		internal Int32	NumVisFaces;
		internal Int32	NumNonVisFaces;
		internal Int32	NumVisBrushes;
		internal Int32	NumVisNodes;
		internal Int32	NumNonVisNodes;
	}


	internal partial class GBSPNode
	{
		//Info for this node as a node or leaf
		Int32		mPlaneNum;						//-1 if a leaf
		UInt32		mContents;						//Contents node/leaf
		GBSPFace	mFaces;							//Faces on this node
		GBSPNode	[]mChildren	=new GBSPNode[2];	//Front and back child
		GBSPNode	mParent;						//Parent of this node
		Bounds		mBounds	=new Bounds();			//Current BBox of node

		//Info for this node as a leaf
		GBSPPortal		mPortals;							//Portals on this leaf
		Int32			mNumLeafFaces;						//Number of faces touching this leaf
		List<GBSPFace>	mLeafFaces	=new List<GBSPFace>();	//Pointer to Faces touching this leaf
		Int32			mCurrentFill;						//For the outside filling stage
		Int32			mEntity;							//1 if entity touching leaf
		Int32			mOccupied;							//FIXME:  Can use Entity!!!
		Int32			mPortalLeafNum;						//For portal saving

		bool	mbDetail;
		Int32	mCluster;
		Int32	mArea;		//Area number, 0 == invalid area

		GBSPSide	mSide;
		GBSPBrush	mBrushList;

		//For GFX file saving
		internal Int32	[]mChildrenID	=new int[2];
		internal Int32	mFirstFace;
		internal Int32	mNumFaces;
		internal Int32	mFirstPortal;
		internal Int32	mNumPortals;

		internal Int32	mFirstSide;			//For bevel bbox clipping
		internal Int32	mNumSides;

		internal const int	MAX_TEMP_LEAF_SIDES	=100;
		internal const int	MAX_LEAF_SIDES		=64000 * 2;
		internal const int	MAX_AREAS			=256;
		internal const int	MAX_AREA_PORTALS	=1024;


		internal void BuildBSP(GBSPBrush brushList, PlanePool pool, bool bVerbose)
		{
			if(bVerbose)
			{
				CoreEvents.Print("--- Build BSP Tree ---\n");
			}

			BuildStats	bs		=new BuildStats();
			Bounds		bounds	=new Bounds();
			GBSPBrush.BrushListStats(brushList, bs, bounds, pool);

			if(bVerbose)
			{
				CoreEvents.Print("Total Brushes          : " + bs.NumVisBrushes + "\n");
				CoreEvents.Print("Total Faces            : " + bs.NumVisFaces + "\n");
				CoreEvents.Print("Faces Removed          : " + bs.NumNonVisFaces + "\n");
			}
			bs.NumVisNodes		=0;
			bs.NumNonVisNodes	=0;

			BuildTree_r(bs, brushList, pool);


			//Top node is always valid, this way portals can use top node to get box of entire bsp...
			mBounds	=new Bounds(bounds);

			if(bVerbose)
			{
				CoreEvents.Print("Total Nodes            : " + (bs.NumVisNodes/2 - bs.NumNonVisNodes) + "\n");
				CoreEvents.Print("Nodes Removed          : " + bs.NumNonVisNodes + "\n");
				CoreEvents.Print("Total Leafs            : " + (bs.NumVisNodes+1)/2 + "\n");
			}
		}


		void LeafNode(GBSPBrush listHead)
		{
			mPlaneNum	=PlanePool.PLANENUM_LEAF;
			mContents	=GBSPBrush.GetLeafContents(listHead);

			//Once brushes get down to the leafs, we don't need
			//to keep the polys on them anymore...
			//We can free them now...
			GBSPBrush.FreeSidePolys(listHead);

			mBounds.mMins	=Vector3.Zero;
			mBounds.mMaxs	=Vector3.Zero;

			mBrushList	=listHead;
		}


		internal bool CheckPlaneAgainstParents(Int32 planeNum)
		{
			for(GBSPNode p=mParent;p != null;p = p.mParent)
			{
				if(p.mPlaneNum == planeNum)
				{
					CoreEvents.Print("Tried parent");
					return	false;
				}
			}
			return	true;
		}


		void BuildTree_r(BuildStats bs, GBSPBrush brushes, PlanePool pool)
		{
			GBSPSide	BestSide;

			GBSPBrush	childrenFront;
			GBSPBrush	childrenBack;

			bs.NumVisNodes++;

			//find the best plane to use as a splitter
			BestSide	=GBSPBrush.SelectSplitSide(bs, brushes, this, pool);
			
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

			GBSPBrush.SplitBrushList(brushes, mPlaneNum, pool, out childrenFront, out childrenBack);

			GBSPBrush.FreeBrushList(brushes);
			
			//Allocate children before recursing
			for(int i=0;i < 2;i++)
			{
				mChildren[i]			=new GBSPNode();
				mChildren[i].mParent	=this;
			}

			//Recursively process children
			mChildren[0].BuildTree_r(bs, childrenFront, pool);
			mChildren[1].BuildTree_r(bs, childrenBack, pool);
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

			mChildren[0].GetTriangles(verts, indexes, bCheckFlags);
			mChildren[1].GetTriangles(verts, indexes, bCheckFlags);
		}


		internal void GetLeafTriangles(List<Vector3> verts, List<UInt32> indexes, bool bCheckFlags)
		{
			if(mLeafFaces.Count > 0)
			{
				foreach(GBSPFace face in mLeafFaces)
				{
					face.GetTriangles(verts, indexes, bCheckFlags);
				}
			}
			if(mPlaneNum == PlanePool.PLANENUM_LEAF)
			{
				return;
			}

			mChildren[0].GetLeafTriangles(verts, indexes, bCheckFlags);
			mChildren[1].GetLeafTriangles(verts, indexes, bCheckFlags);
		}


		bool CreatePolyOnNode(out GBSPPoly outPoly, PlanePool pool)
		{
			outPoly	=null;

			GBSPPoly	poly	=new GBSPPoly(pool.mPlanes[mPlaneNum]);
			if(poly == null)
			{
				CoreEvents.Print("CreatePolyOnNode:  Could not create poly.\n");
				return	false;
			}

			//Clip this portal by all the parents of this node
			GBSPNode	node	=this;
			for(GBSPNode parent = mParent;parent != null && !poly.IsTiny();)
			{
				bool	bSide;

				GBSPPlane	plane	=pool.mPlanes[parent.mPlaneNum];

				bSide	=(parent.mChildren[0] == node)? false : true;

				if(!poly.ClipPolyEpsilon(0.001f, plane, bSide))
				{
					return	false;
				}

				node	=parent;
				parent	=parent.mParent;
			}

			outPoly	=poly;

			return	true;
		}


		GBSPNode FindLeaf(Vector3 pos, PlanePool pool)
		{
			GBSPNode	node	=this;

			while(node != null && node.mPlaneNum != PlanePool.PLANENUM_LEAF)
			{
				GBSPPlane	plane	=pool.mPlanes[node.mPlaneNum];
				float		dist	=Vector3.Dot(pos, plane.mNormal) - plane.mDist;

				if(dist > 0)
				{
					node	=node.mChildren[0];
				}
				else
				{
					node	=node.mChildren[1];
				}
			}

			if(node == null)
			{
				CoreEvents.Print("FindLeaf:  NULL Node/Leaf.\n");
			}
			return	node;
		}


		bool PlaceEntities(List<MapEntity> ents, PlanePool pool)
		{				
			bool	bEmpty	=false;

			for(int i=1;i < ents.Count;i++)
			{
				MapEntity	e	=ents[i];
				Vector3		org	=Vector3.Zero;

				if(!e.GetOrigin(out org))
				{
					continue;
				}

				GBSPNode	node	=FindLeaf(org, pool);
				if(node == null)
				{
					return	false;
				}

				if((node.mContents & Contents.BSP_CONTENTS_SOLID2) == 0)
				{
					node.mEntity	=i;
					bEmpty			=true;
				}
			}
			
			if(!bEmpty)
			{
				CoreEvents.Print("PlaceEntities:  No valid entities for operation");
				return	false;
			}			
			return	true;
		}


		internal void FreeBSP_r()
		{
			if(mPlaneNum == PlanePool.PLANENUM_LEAF)
			{
				mLeafFaces	=null;

				GBSPFace.FreeFaceList(mFaces);
				GBSPBrush.FreeBrushList(mBrushList);
				return;
			}

			mChildren[0].FreeBSP_r();
			mChildren[1].FreeBSP_r();

			mChildren[0]	=null;
			mChildren[1]	=null;
		}


		internal void MakeFaces(PlanePool pool, TexInfoPool tip, bool bVerbose)
		{
			if(bVerbose)
			{
				CoreEvents.Print("--- Finalize Faces ---\n");
			}
			
			int	numMerged		=0;
			int	numMakeFaces	=0;

			MakeFaces_r(pool, tip, ref numMerged, ref numMakeFaces);

			if(bVerbose)
			{
				CoreEvents.Print("TotalFaces             : " + numMakeFaces + "\n");
				CoreEvents.Print("Merged Faces           : " + numMerged + "\n");
				CoreEvents.Print("FinalFaces             : " + ((numMakeFaces - numMerged)) + "\n");
			}
		}


		void MakeFaces_r(PlanePool pool, TexInfoPool tip, ref int numMerged, ref int numMake)
		{
			//Recurse down to leafs
			if(mPlaneNum != PlanePool.PLANENUM_LEAF)
			{
				mChildren[0].MakeFaces_r(pool, tip, ref numMerged, ref numMake);
				mChildren[1].MakeFaces_r(pool, tip, ref numMerged, ref numMake);
				
				//Marge list (keepin that typo, funny)
				GBSPFace.MergeFaceList(mFaces, pool, ref numMerged);

				//Subdivide them for lightmaps
				//SubdivideNodeFaces(tip, ref NumSubdivided);
				return;
			}

			//Solid leafs never have visible faces
			if((mContents & Contents.BSP_CONTENTS_SOLID2) != 0)
			{
				return;
			}

			//See which portals are valid
			Int32	side;
			for(GBSPPortal p=mPortals;p != null;p=p.mNext[side])
			{
				side	=(p.mNodes[1] == this)? 1 : 0;

				p.mFace[side]	=p.FaceFromPortal(side);
				if(p.mFace[side] != null)
				{
					//Record the contents on each side of the face
					p.mFace[side].SetContents(0, mContents);
					p.mFace[side].SetContents(1, p.mNodes[(side == 0)? 1 : 0].mContents);	// Back side contents is the leaf on the other side of this portal

					//Add the face to the list of faces on the node
					//that originaly created the portal
					GBSPFace.AddToListStart(ref p.mOnNode.mFaces, p.mFace[side]);

					numMake++;
				}
			}
		}


		internal void MakeLeafFaces()
		{
			//Recurse down to leafs
			if(mPlaneNum != PlanePool.PLANENUM_LEAF)
			{
				mChildren[0].MakeLeafFaces();
				mChildren[1].MakeLeafFaces();
				return;
			}

			//Solid leafs never have visible faces
			if((mContents & Contents.BSP_CONTENTS_SOLID2) != 0)
			{
				return;
			}

			//Reset counter
			mNumLeafFaces	=0;

			//See which portals are valid
			Int32	side;
			for(GBSPPortal p=mPortals;p != null;p=p.mNext[side])
			{
				side	=(p.mNodes[1] == this)? 1 : 0;

				if(p.mFace[side] == null)
				{
					continue;
				}

				CountLeafFaces_r(p.mFace[side]);
			}

			//Reset counter
			mNumLeafFaces	=0;
			
			//See which portals are valid
			for(GBSPPortal p=mPortals;p != null;p=p.mNext[side])
			{
				side	=(p.mNodes[1] == this)? 1 : 0;

				if(p.mFace[side] == null)
				{
					continue;
				}

				GetLeafFaces_r(p.mFace[side]);
			}
		}


		void GetLeafFaces_r(GBSPFace f)
		{
			while(f.mMerged != null)
			{
				f	=f.mMerged;
			}

			if(f.mSplit[0] != null)
			{
				GetLeafFaces_r(f.mSplit[0]);
				GetLeafFaces_r(f.mSplit[1]);
				return;
			}
			mLeafFaces.Add(f);
			mNumLeafFaces++;
		}


		void CountLeafFaces_r(GBSPFace f)
		{
			while(f.mMerged != null)
			{
				f	=f.mMerged;
			}

			if(f.mSplit[0] != null)
			{
				CountLeafFaces_r(f.mSplit[0]);
				CountLeafFaces_r(f.mSplit[1]);
				return;
			}

			mNumLeafFaces++;
		}


		void MergeNodes_r(ref int mergedNodes)
		{
			if(mPlaneNum == PlanePool.PLANENUM_LEAF)
			{
				return;
			}

			mChildren[0].MergeNodes_r(ref mergedNodes);
			mChildren[1].MergeNodes_r(ref mergedNodes);

			if(mChildren[0].mPlaneNum == PlanePool.PLANENUM_LEAF
				&& mChildren[1].mPlaneNum == PlanePool.PLANENUM_LEAF)
			{
				if(((mChildren[0].mContents & Contents.BSP_CONTENTS_SOLID2) != 0)
					&& ((mChildren[1].mContents & Contents.BSP_CONTENTS_SOLID2) != 0))
				{
					if((mChildren[0].mContents & 0xffff0000)
						== (mChildren[1].mContents & 0xffff0000))
					{
						if(mFaces != null)
						{
							CoreEvents.Print("Node.mFaces seperating BSP_CONTENTS_SOLID!");
						}

						if(mChildren[0].mFaces != null || mChildren[1].mFaces != null)
						{
							CoreEvents.Print("!Node.mFaces with children");
						}

						// FIXME: free stuff
						mPlaneNum	=PlanePool.PLANENUM_LEAF;
						mContents	=mChildren[0].mContents;
						mContents	|=mChildren[1].mContents;

						mbDetail	=false;

						if(mBrushList != null)
						{
							CoreEvents.Print("MergeNodes: node.mBrushList");
						}

						//combine brush lists
						mBrushList	=mChildren[1].mBrushList;
						GBSPBrush.MergeLists(mChildren[0].mBrushList, mBrushList);
						mergedNodes++;
					}
				}
			}
		}


		internal void MergeNodes(bool bVerbose)
		{
			if(bVerbose)
			{
				CoreEvents.Print("--- Merge Nodes ---\n");
			}

			int	mergedNodes	=0;
			
			MergeNodes_r(ref mergedNodes);

			if(bVerbose)
			{
				CoreEvents.Print("Num Merged             : " + mergedNodes + "\n");
			}
		}


		internal bool CreateLeafClusters(bool bVerbose, ref int numLeafClusters)
		{
			CoreEvents.Print(" --- CreateLeafClusters --- \n");

			if(!CreateLeafClusters_r(ref numLeafClusters))
			{
				CoreEvents.Print("CreateLeafClusters:  Failed to find leaf clusters.\n");
				return	false;
			}

			if(bVerbose)
			{
				CoreEvents.Print("Num Clusters       : " + numLeafClusters + "\n");
			}
			return	true;
		}


		bool CreateLeafClusters_r(ref int numLeafClusters)
		{
			if(mPlaneNum != PlanePool.PLANENUM_LEAF && !mbDetail)
			{
				mChildren[0].CreateLeafClusters_r(ref numLeafClusters);
				mChildren[1].CreateLeafClusters_r(ref numLeafClusters);
				return	true;
			}
			
			//Either a leaf or detail node
			if((mContents & Contents.BSP_CONTENTS_SOLID2) != 0)
			{
				mCluster	=-1;
				return	true;
			}
			
			FillLeafClusters_r(numLeafClusters);

			numLeafClusters++;

			return	true;
		}


		void FillLeafClusters_r(Int32 cluster)
		{
			if(mPlaneNum == PlanePool.PLANENUM_LEAF)
			{
				if((mContents & Contents.BSP_CONTENTS_SOLID2) != 0)
				{
					mCluster	=-1;
				}
				else
				{
					mCluster	=cluster;
				}
				return;
			}
		
			mCluster	=cluster;

			mChildren[0].FillLeafClusters_r(cluster);
			mChildren[1].FillLeafClusters_r(cluster);
		}


		internal UInt32 ClusterContents()
		{
			UInt32	c1, c2, con;

			// Stop at leafs, and start returning contents
			if(mPlaneNum == PlanePool.PLANENUM_LEAF)
			{
				return	mContents;
			}

			c1	=mChildren[0].ClusterContents();
			c2	=mChildren[1].ClusterContents();

			con	=(c1 | c2);	//Or together children, and return

			if(((c1 & Contents.BSP_CONTENTS_SOLID2) == 0)
				|| ((c2 & Contents.BSP_CONTENTS_SOLID2) == 0))
			{
				con	&=~Contents.BSP_CONTENTS_SOLID2;
			}
			return	(con);
		}


		internal bool GetFaceVertIndexNumbers_r(FaceFixer ff)
		{
			if(mPlaneNum == PlanePool.PLANENUM_LEAF)
			{
				return	true;
			}

			if(!GBSPFace.GetFaceListVertIndexNumbers(mFaces, ff))
			{
				return	false;
			}

			if(!mChildren[0].GetFaceVertIndexNumbers_r(ff))
			{
				return	false;
			}
			if(!mChildren[1].GetFaceVertIndexNumbers_r(ff))
			{
				return	false;
			}
			return	true;
		}


		internal bool SaveGFXNodes_r(BinaryWriter bw)
		{
			GFXNode	GNode	=new GFXNode();

			if(mPlaneNum == PlanePool.PLANENUM_LEAF)
			{
				return	true;
			}
			
			GNode.mChildren[0]	=mChildrenID[0];
			GNode.mChildren[1]	=mChildrenID[1];
			GNode.mNumFaces		=mNumFaces;
			GNode.mFirstFace	=mFirstFace;
			GNode.mPlaneNum		=mPlaneNum;
			GNode.mMins			=mBounds.mMins;
			GNode.mMaxs			=mBounds.mMaxs;

			GNode.Write(bw);

			if(!mChildren[0].SaveGFXNodes_r(bw))
			{
				return	false;
			}
			if(!mChildren[1].SaveGFXNodes_r(bw))
			{
				return	false;
			}
			return	true;
		}


		internal bool FixTJunctions_r(FaceFixer ff, TexInfoPool tip)
		{
			if(mPlaneNum == PlanePool.PLANENUM_LEAF)
			{
				return	true;
			}

			GBSPFace.FixFaceListTJunctions(mFaces, ff, tip);
			
			if(!mChildren[0].FixTJunctions_r(ff, tip))
			{
				return	false;
			}
			if(!mChildren[1].FixTJunctions_r(ff, tip))
			{
				return	false;
			}
			return	true;
		}


		internal int PrepGFXNodes_r(Int32 Original, NodeCounter nc)
		{
			Int32	currentNode;

			//Prep the leaf and it's portals
			if(mPlaneNum == PlanePool.PLANENUM_LEAF)
			{
				if((mContents & Contents.BSP_CONTENTS_SOLID2) != 0)
				{
					nc.mNumSolidLeafs++;	// Remember how many solid leafs there are
				}

				mNumPortals		=0;
				mFirstPortal	=-1;

				// To be able to save out portal LeafTo's
				mPortalLeafNum	=nc.mNumGFXLeafs;

				//Count num gfx leaf faces here, so we know how big to make the array
				//later, when they are saved out...
				nc.mNumGFXLeafFaces	+=mNumLeafFaces;

				//Increase the number of leafs
				nc.mNumGFXLeafs++;

				return -(nc.mNumGFXLeafs);
			}
				
			currentNode	=nc.mNumGFXNodes;

			PrepGFXNode(nc);

			nc.mNumGFXNodes++;

			mChildrenID[0]	=mChildren[0].PrepGFXNodes_r(mChildrenID[0], nc);
			mChildrenID[1]	=mChildren[1].PrepGFXNodes_r(mChildrenID[1], nc);

			return currentNode;
		}


		void PrepGFXNode(NodeCounter nc)
		{
			mFirstFace	=nc.mNumGFXFaces;

			mNumFaces	=GBSPFace.PrepFaceList(mFaces, nc);
		}


		internal bool SaveGFXLeafs_r(BinaryWriter bw, List<Int32> gfxLeafFaces, ref int totalLeafSize)
		{
			GFXLeaf	GLeaf	=new GFXLeaf();
			Int32	i;

			if(mPlaneNum == PlanePool.PLANENUM_LEAF)
			{
				GLeaf.mContents	=mContents;

				GLeaf.mMins	=mBounds.mMins;
				GLeaf.mMaxs =mBounds.mMaxs;

				GLeaf.mFirstFace	=gfxLeafFaces.Count;
				GLeaf.mFirstPortal	=mFirstPortal;
				GLeaf.mNumPortals	=mNumPortals;

				GLeaf.mCluster	=mCluster;	//CHANGE: CLUSTER
				GLeaf.mArea		=mArea;

				GLeaf.mFirstSide	=mFirstSide;
				GLeaf.mNumSides		=mNumSides;

				GLeaf.mNumFaces	=0;

				for(i=0;i < mNumLeafFaces;i++)
				{
					if(!mLeafFaces[i].IsVisible())
					{
						continue;
					}
					
					//Don't output mark face if it was skipped in the face output stage
					//(or it will reference an invalid face...)
					if(mLeafFaces[i].mIndexVerts.Length <= 0)
					{
						continue;
					}
					gfxLeafFaces.Add(mLeafFaces[i].mOutputNum);
					GLeaf.mNumFaces++;
				}

				totalLeafSize++;

				GLeaf.Write(bw);

				return	true;
			}

			if(!mChildren[0].SaveGFXLeafs_r(bw, gfxLeafFaces, ref totalLeafSize))
			{
				return	false;
			}
			if(!mChildren[1].SaveGFXLeafs_r(bw, gfxLeafFaces, ref totalLeafSize))
			{
				return	false;
			}
			return	true;
		}


		internal bool SaveGFXFaces_r(BinaryWriter bw)
		{
			if(mPlaneNum == PlanePool.PLANENUM_LEAF)
			{
				return	true;
			}

			GBSPFace.ConvertListToGFXAndSave(mFaces, bw);
			
			if(!mChildren[0].SaveGFXFaces_r(bw))
			{
				return	false;
			}
			if(!mChildren[1].SaveGFXFaces_r(bw))
			{
				return	false;
			}
			return	true;
		}


		static UInt32 VisibleContents(UInt32 con)
		{
			UInt32	majorContents;

			if(con == 0)
			{
				return	0;
			}

			//Only check visible contents
			con	&=Contents.BSP_VISIBLE_CONTENTS;
			
			//Return the strongest one, return the first lsb
			for(int j=0;j < 32;j++)
			{
				majorContents	=(con & (UInt32)(1 << j));

				if(majorContents != 0)
				{
					return	majorContents;
				}
			}
			return	0;
		}


		static internal bool WindowCheck(GBSPNode front, GBSPNode back)
		{
			return	(((front.mContents & Contents.BSP_CONTENTS_WINDOW2) != 0)
						&& VisibleContents(back.mContents
						^ front.mContents) == Contents.BSP_CONTENTS_WINDOW2);
		}


		internal bool IsContentsSolid()
		{
			return	((mContents & Contents.BSP_CONTENTS_SOLID2) != 0);
		}


		internal bool IsLeaf()
		{
			return	(mPlaneNum != PlanePool.PLANENUM_LEAF);
		}


		internal Bounds GetBounds()
		{
			return	mBounds;
		}


		internal void SetDetail(bool bDetail)
		{
			mbDetail	=bDetail;
		}


		internal int GetOriginalEntityNum()
		{
			return	GBSPBrush.GetOriginalEntityNum(mBrushList);
		}
	}
}
