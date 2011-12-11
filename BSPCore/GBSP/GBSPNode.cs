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
		Int32			mPlaneNum;						//-1 if a leaf
		UInt32			mContents;						//Contents node/leaf
		List<GBSPFace>	mFaces	=new List<GBSPFace>();	//Faces on this node
		GBSPNode		mFront, mBack;					//Front and back child
		GBSPNode		mParent;						//Parent of this node
		Bounds			mBounds	=new Bounds();			//Current BBox of node
		GBSPBrush		mVolume;

		//Info for this node as a leaf
		List<GBSPPortal>	mPortals	=new List<GBSPPortal>();//Portals on this leaf
		Int32				mNumLeafFaces;						//Number of faces touching this leaf
		List<GBSPFace>		mLeafFaces	=new List<GBSPFace>();	//Pointer to Faces touching this leaf
		Int32				mCurrentFill;						//For the outside filling stage
		Int32				mEntity;							//1 if entity touching leaf
		Int32				mOccupied;							//FIXME:  Can use Entity!!!
		Int32				mPortalLeafNum;						//For portal saving

		bool	mbDetail;
		Int32	mCluster;
		Int32	mArea;		//Area number, 0 == invalid area

		GBSPSide		mSide;
		List<GBSPBrush>	mBrushList	=new List<GBSPBrush>();
		internal Int32	mFirstSide;			//For bevel bbox clipping
		internal Int32	mNumSides;

		//For GFX file saving
		internal Int32	mFrontID, mBackID;
		internal Int32	mFirstFace;
		internal Int32	mNumFaces;
		internal Int32	mFirstPortal;
		internal Int32	mNumPortals;

		internal const int	MAX_AREAS			=256;
		internal const int	MAX_AREA_PORTALS	=1024;


		internal void BuildBSP(List<GBSPBrush> brushList, PlanePool pool, bool bVerbose)
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

			mVolume	=new GBSPBrush(new MapBrush(bounds, pool));

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

		static internal GBSPNode BlockTree(Array blockNodes, PlanePool pp, int xMin, int zMin, int xl, int zl, int xh, int zh)
		{
			if(xl == xh && zl == zh)
			{
				GBSPNode	ret	=(GBSPNode)blockNodes.GetValue(xl - xMin, zl - zMin);
				if(ret == null)
				{
					ret	=new GBSPNode();
					ret.LeafNode(null);
				}
				return	ret;
			}

			//create a seperator along the largest axis
			GBSPNode	n	=new GBSPNode();
			GBSPPlane	p	=new GBSPPlane();
			
			if(xh - xl > zh - zl)
			{
				//split x axis
				int	mid		=xl + (xh - xl) / 2 + 1;
				p.mNormal	=Vector3.UnitX;
				p.mDist		=mid * 1024;

				sbyte	side;
				n.mPlaneNum		=pp.FindPlane(p, out side);
				n.mFront	=BlockTree(blockNodes, pp, xMin, zMin, mid, zl, xh, zh);
				n.mBack		=BlockTree(blockNodes, pp, xMin, zMin, xl, zl, mid - 1, zh);
			}
			else
			{
				int	mid		=zl + (zh - zl) / 2 + 1;
				p.mNormal	=Vector3.UnitZ;
				p.mDist		=mid * 1024;

				sbyte	side;
				n.mPlaneNum		=pp.FindPlane(p, out side);
				n.mFront	=BlockTree(blockNodes, pp, xMin, zMin, xl, mid, xh, zh);
				n.mBack		=BlockTree(blockNodes, pp, xMin, zMin, xl, zl, xh, mid - 1);
			}
			return	n;
		}

		void LeafNode(List<GBSPBrush> list)
		{
			mPlaneNum	=PlanePool.PLANENUM_LEAF;
			mContents	=GBSPBrush.GetLeafContents(list);

			//Once brushes get down to the leafs, we don't need
			//to keep the polys on them anymore...
			//We can free them now...
			GBSPBrush.FreeSidePolys(list);

			mBounds.mMins	=Vector3.Zero;
			mBounds.mMaxs	=Vector3.Zero;

			if(list != null)
			{
				mBrushList	=list;
			}
		}


		internal bool CheckPlaneAgainstVolume(Int32 planeNum, PlanePool pp)
		{
			GBSPBrush	front, back;

			mVolume.Split(planeNum, 0, 0, true, pp, out front, out back, false);

			return	(front != null && back != null);
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


		void BuildTree_r(BuildStats bs, List<GBSPBrush> brushes, PlanePool pool)
		{
			GBSPSide	BestSide;

			List<GBSPBrush>	childrenFront;
			List<GBSPBrush>	childrenBack;

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

			brushes.Clear();

			//Allocate children before recursing
			mFront	=new GBSPNode();
			mBack	=new GBSPNode();
			mFront.mParent	=mBack.mParent	=this;

			mVolume.Split(mPlaneNum, 0, 0, true, pool, out mFront.mVolume, out mBack.mVolume, false);

			//Recursively process children
			mFront.BuildTree_r(bs, childrenFront, pool);
			mBack.BuildTree_r(bs, childrenBack, pool);
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

			mFront.GetLeafTriangles(verts, indexes, bCheckFlags);
			mBack.GetLeafTriangles(verts, indexes, bCheckFlags);
		}


		bool CreatePortalOnNode(PlanePool pool)
		{
			GBSPPoly	poly	=new GBSPPoly(pool.mPlanes[mPlaneNum]);
			if(poly == null)
			{
				CoreEvents.Print("CreatePolyOnNode:  Could not create poly.\n");
				return	false;
			}

			//clip by parents
			GBSPNode	node	=this;
			for(GBSPNode parent = mParent;parent != null && !poly.IsTiny();)
			{
				bool	bSide;

				GBSPPlane	plane	=pool.mPlanes[parent.mPlaneNum];

				bSide	=(parent.mFront == node)? false : true;

				if(!poly.ClipPolyEpsilon(0.001f, plane, bSide))
				{
					return	false;
				}

				node	=parent;
				parent	=parent.mParent;
			}

			//clip by portals on the node
			foreach(GBSPPortal port in mPortals)
			{
				bool	bFlipSide	=(port.mFrontNode != this);

				poly.ClipPolyEpsilon(UtilityLib.Mathery.ON_EPSILON, port.mPlane, bFlipSide);
			}

			if(poly.IsTiny())
			{
				return	false;
			}

			GBSPPortal	newPortal	=new GBSPPortal();
			newPortal.mPlane		=pool.mPlanes[mPlaneNum];
			newPortal.mOnNode		=this;
			newPortal.mPoly			=poly;

			newPortal.mFrontNode	=mFront;
			newPortal.mBackNode		=mBack;
			mFront.mPortals.Add(newPortal);
			mBack.mPortals.Add(newPortal);

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
					node	=node.mFront;
				}
				else
				{
					node	=node.mBack;
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
				mLeafFaces.Clear();
				mFaces.Clear();
				mBrushList.Clear();
				return;
			}

			mFront.FreeBSP_r();
			mBack.FreeBSP_r();

			mFront	=null;
			mBack	=null;
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
				mFront.MakeFaces_r(pool, tip, ref numMerged, ref numMake);
				mBack.MakeFaces_r(pool, tip, ref numMerged, ref numMake);
				
				//Marge list (keepin that typo, funny)
				GBSPFace.MergeFaceList(mFaces, pool, ref numMerged);

				//Subdivide them for lightmaps
				//using big atlas'd lightmaps now, no need to subdiv
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
			foreach(GBSPPortal p in mPortals)
			{
				side	=(p.mBackNode == this)? 1 : 0;

				GBSPFace	newFace;
				if(side == 0)
				{
					p.mFrontFace	=p.FaceFromPortal(side);
					newFace			=p.mFrontFace;
				}
				else
				{
					p.mBackFace		=p.FaceFromPortal(side);
					newFace			=p.mBackFace;
				}

				if(newFace != null)
				{
					//Record the contents on each side of the face
					newFace.SetContents(0, mContents);

					//Back side contents is the leaf on the other side of this portal
					if(side == 0)
					{
						newFace.SetContents(1, p.mBackNode.mContents);
					}
					else
					{
						newFace.SetContents(1, p.mFrontNode.mContents);
					}					

					//Add the face to the list of faces on the node
					//that originaly created the portal
					p.mOnNode.mFaces.Add(newFace);

					numMake++;
				}
			}
		}


		internal void MakeLeafFaces()
		{
			//Recurse down to leafs
			if(mPlaneNum != PlanePool.PLANENUM_LEAF)
			{
				mFront.MakeLeafFaces();
				mBack.MakeLeafFaces();
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
			foreach(GBSPPortal p in mPortals)
			{
				side	=(p.mBackNode == this)? 1 : 0;

				if(side == 0)
				{
					if(p.mFrontFace == null)
					{
						continue;
					}

					CountLeafFaces_r(p.mFrontFace);
				}
				else
				{
					if(p.mBackFace == null)
					{
						continue;
					}

					CountLeafFaces_r(p.mBackFace);
				}
			}

			//Reset counter
			mNumLeafFaces	=0;
			
			//See which portals are valid
			foreach(GBSPPortal p in mPortals)
			{
				side	=(p.mBackNode == this)? 1 : 0;

				if(side == 0)
				{
					if(p.mFrontFace == null)
					{
						continue;
					}

					GetLeafFaces_r(p.mFrontFace);
				}
				else
				{
					if(p.mBackFace == null)
					{
						continue;
					}

					GetLeafFaces_r(p.mBackFace);
				}
			}
		}


		void GetLeafFaces_r(GBSPFace f)
		{
			while(f.mMerged != null)
			{
				f	=f.mMerged;
			}

			if(f.mSplit0 != null)
			{
				GetLeafFaces_r(f.mSplit0);
				GetLeafFaces_r(f.mSplit1);
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

			if(f.mSplit0 != null)
			{
				CountLeafFaces_r(f.mSplit0);
				CountLeafFaces_r(f.mSplit1);
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

			mFront.MergeNodes_r(ref mergedNodes);
			mBack.MergeNodes_r(ref mergedNodes);

			if(mFront.mPlaneNum == PlanePool.PLANENUM_LEAF
				&& mBack.mPlaneNum == PlanePool.PLANENUM_LEAF)
			{
				if(((mFront.mContents & Contents.BSP_CONTENTS_SOLID2) != 0)
					&& ((mBack.mContents & Contents.BSP_CONTENTS_SOLID2) != 0))
				{
					if((mFront.mContents & 0xffff0000)
						== (mBack.mContents & 0xffff0000))
					{
						if(mFaces.Count > 0)
						{
							CoreEvents.Print("Node.mFaces seperating BSP_CONTENTS_SOLID!\n");
						}

						if(mFront.mFaces.Count > 0 || mBack.mFaces.Count > 0)
						{
							CoreEvents.Print("!Node.mFaces with children\n");
						}

						// FIXME: free stuff
						mPlaneNum	=PlanePool.PLANENUM_LEAF;
						mContents	=mFront.mContents;
						mContents	|=mBack.mContents;

						mbDetail	=false;

						if(mBrushList.Count > 0)
						{
							CoreEvents.Print("MergeNodes: node.mBrushList\n");
						}

						//combine brush lists
						mBrushList	=mBack.mBrushList;
						mBrushList.AddRange(mFront.mBrushList);
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
				mFront.CreateLeafClusters_r(ref numLeafClusters);
				mBack.CreateLeafClusters_r(ref numLeafClusters);
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

			mFront.FillLeafClusters_r(cluster);
			mBack.FillLeafClusters_r(cluster);
		}


		internal UInt32 ClusterContents()
		{
			UInt32	c1, c2, con;

			// Stop at leafs, and start returning contents
			if(mPlaneNum == PlanePool.PLANENUM_LEAF)
			{
				return	mContents;
			}

			c1	=mFront.ClusterContents();
			c2	=mBack.ClusterContents();

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

			if(!mFront.GetFaceVertIndexNumbers_r(ff))
			{
				return	false;
			}
			if(!mBack.GetFaceVertIndexNumbers_r(ff))
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
			
			GNode.mChildren[0]	=mFrontID;
			GNode.mChildren[1]	=mBackID;
			GNode.mNumFaces		=mNumFaces;
			GNode.mFirstFace	=mFirstFace;
			GNode.mPlaneNum		=mPlaneNum;
			GNode.mMins			=mBounds.mMins;
			GNode.mMaxs			=mBounds.mMaxs;

			GNode.Write(bw);

			if(!mFront.SaveGFXNodes_r(bw))
			{
				return	false;
			}
			if(!mBack.SaveGFXNodes_r(bw))
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
			
			if(!mFront.FixTJunctions_r(ff, tip))
			{
				return	false;
			}
			if(!mBack.FixTJunctions_r(ff, tip))
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

			mFrontID	=mFront.PrepGFXNodes_r(mFrontID, nc);
			mBackID		=mBack.PrepGFXNodes_r(mBackID, nc);

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

			if(!mFront.SaveGFXLeafs_r(bw, gfxLeafFaces, ref totalLeafSize))
			{
				return	false;
			}
			if(!mBack.SaveGFXLeafs_r(bw, gfxLeafFaces, ref totalLeafSize))
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
			
			if(!mFront.SaveGFXFaces_r(bw))
			{
				return	false;
			}
			if(!mBack.SaveGFXFaces_r(bw))
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
