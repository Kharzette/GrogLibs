using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class GBSPNode
	{
		// Info for this node as a node or leaf
		public Int32			mPlaneNum;						// -1 if a leaf
		public Int32			mPlaneSide;						// CHANGE1!!!
		public UInt32			mContents;						// Contents node/leaf
		public GBSPFace			mFaces;							// Faces on this node
		public GBSPNode			[]mChildren	=new GBSPNode[2];	// Front and back child
		public GBSPNode			mParent;						// Parent of this node
		public Bounds			mBounds	=new Bounds();			// Current BBox of node

		// Info for this node as a leaf
		public GBSPPortal		mPortals;		// Portals on this leaf
		public Int32			mNumLeafFaces;	// Number of faces touching this leaf
		public GBSPFace			mLeafFaces;		// Pointer to Faces touching this leaf
		public Int32			mCurrentFill;	// For the outside filling stage
		public Int32			mEntity;		// 1 if entity touching leaf
		public Int32			mOccupied;		// FIXME:  Can use Entity!!!
		public Int32			mPortalLeafNum;	// For portal saving

		public bool		mDetail;
		public Int32	mCluster;
		public Int32	mArea;				// Area number, 0 == invalid area

		public GBSPBrush	mVolume;
		public GBSPSide		mSide;
		public GBSPBrush	mBrushList;

		// For GFX file saving
		public Int32	[]mChildrenID	=new int[2];
		public Int32	mFirstFace;
		public Int32	mNumFaces;
		public Int32	mFirstPortal;
		public Int32	mNumPortals;

		public Int32	mFirstSide;			// For bevel bbox clipping
		public Int32	mNumSides;


		internal void BuildBSP(GBSPBrush brushList, PlanePool pool, ref Bounds treeBounds)
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

			for(GBSPBrush b = brushList;b != null;b=b.mNext)
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

			treeBounds	=bounds;

			Map.Print("Total Nodes            : " + (NumVisNodes/2 - NumNonVisNodes) + "\n");
			Map.Print("Nodes Removed          : " + NumNonVisNodes + "\n");
			Map.Print("Total Leafs            : " + (NumVisNodes+1)/2 + "\n");
		}


		void LeafNode(GBSPBrush Brushes)
		{
			mPlaneNum	=PlanePool.PLANENUM_LEAF;
			mContents	=0;

			//Get the contents of this leaf, by examining all the brushes that made this leaf
			for(GBSPBrush b=Brushes;b != null;b=b.mNext)
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
			for(GBSPBrush b=Brushes;b != null;b=b.mNext)
			{
				foreach(GBSPSide side in b.mSides)
				{
					if(side.mPoly != null)
					{
						side.mPoly.mVerts.Clear();
						side.mPoly	=null;
					}
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


		void BuildTree_r(GBSPBrush brushes, PlanePool pool, ref int NumVisNodes, ref int NumNonVisNodes)
		{
			GBSPSide	BestSide;

			GBSPBrush	childrenFront;
			GBSPBrush	childrenBack;

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

			GBSPBrush.SplitBrushList(brushes, this, pool, out childrenFront, out childrenBack);

			GBSPBrush.FreeBrushList(brushes);
			
			//Allocate children before recursing
			for(int i=0;i < 2;i++)
			{
				mChildren[i]			=new GBSPNode();
				mChildren[i].mParent	=this;
			}

			//Recursively process children
			mChildren[0].BuildTree_r(childrenFront, pool, ref NumVisNodes, ref NumNonVisNodes);
			mChildren[1].BuildTree_r(childrenBack, pool, ref NumVisNodes, ref NumNonVisNodes);
		}


		internal bool CreatePortals(GBSPModel model, bool bVis, PlanePool pool)
		{
			Map.Print(" --- Create Portals --- \n");

			Bounds	nodeBounds	=model.mBounds;

			if(!CreateAllOutsidePortals(model.mOutsideNode, nodeBounds, pool))
			{
				Map.Print("CreatePortals:  Could not create bbox portals.\n");
				return	false;
			}

			if(!PartitionPortals_r(bVis, pool))
			{
				Map.Print("CreatePortals:  Could not partition portals.\n");
				return	false;
			}
			return	true;
		}


		bool CreateAllOutsidePortals(GBSPNode outNode, Bounds nodeBounds, PlanePool pool)
		{
			GBSPPlane	[]PPlanes	=new GBSPPlane[6];
			GBSPPortal	[]Portals	=new GBSPPortal[6];

			outNode.mPlaneNum	=PlanePool.PLANENUM_LEAF;
			outNode.mContents	=GBSPBrush.BSP_CONTENTS_SOLID2;

			//So there won't be NULL volume leafs when we create the outside portals
			for(int k=0;k < 3;k++)
			{
				if(UtilityLib.Mathery.VecIdx(nodeBounds.mMins, k) - 128.0f
					<= -Brush.MIN_MAX_BOUNDS ||
					UtilityLib.Mathery.VecIdx(nodeBounds.mMaxs, k) + 128.0f
					>= Brush.MIN_MAX_BOUNDS)
				{
					Map.Print("CreateAllOutsidePortals:  World BOX out of range...\n");
					return	false;
				}
			}

			nodeBounds.mMins	-=(Vector3.One * 128.0f);
			nodeBounds.mMaxs	+=(Vector3.One * 128.0f);

			// Create 6 portals, and point to the outside and the RootNode
			for(int i=0;i < 3;i++)
			{
				for(int k=0;k < 2;k++)
				{
					int	Index	=k * 3 + i;

					PPlanes[Index].mNormal	=Vector3.Zero;

					if(k == 0)
					{
						UtilityLib.Mathery.VecIdxAssign(ref PPlanes[Index].mNormal, i, 1.0f);
						PPlanes[Index].mDist	=UtilityLib.Mathery.VecIdx(nodeBounds.mMins, i);
					}
					else
					{
						UtilityLib.Mathery.VecIdxAssign(ref PPlanes[Index].mNormal, i, -1.0f);
						PPlanes[Index].mDist	=-UtilityLib.Mathery.VecIdx(nodeBounds.mMaxs, i);
					}
					
					Portals[Index]	=GBSPPortal.CreateOutsidePortal(PPlanes[Index], this, outNode, pool);

					if(Portals[Index] == null)
					{
						return	false;
					}
				}
			}
									  
			for(int i=0;i < 6;i++)
			{
				for(int k=0;k < 6;k++)
				{
					if(k == i)
					{
						continue;
					}

					if(!Portals[i].mPoly.ClipPoly(PPlanes[k], false))
					{
						Map.Print("CreateAllOutsidePortals:  There was an error clipping the portal.\n");
						return	false;
					}

					if(Portals[i].mPoly.mVerts.Count < 3)
					{
						Map.Print("CreateAllOutsidePortals:  Portal was clipped away.\n");
						return	false;
					}
				}
			}

			return	true;
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


		void CalcBoundsFromPortals()
		{
			mBounds.Clear();

			int	s	=0;
			for(GBSPPortal p = mPortals;p != null;p=p.mNext[s])
			{
				s	=(p.mNodes[1] == this)? 1 : 0;

				foreach(Vector3 pnt in p.mPoly.mVerts)
				{
					mBounds.AddPointToBounds(pnt);
				}
			}
		}


		bool CreatePolyOnNode(out GBSPPoly Out, PlanePool pool)
		{
			GBSPPoly	Poly;
			GBSPPlane	Plane;
			GBSPNode	Parent, Node	=this;

			Out	=null;

			Poly	=new GBSPPoly(pool.mPlanes[mPlaneNum]);
			if(Poly == null)
			{
				Map.Print("CreatePolyOnNode:  Could not create poly.\n");
				return	false;
			}

			//Clip this portal by all the parents of this node
			for(Parent = mParent;Parent != null && !Poly.IsTiny();)
			{
				bool	Side;

				Plane	=pool.mPlanes[Parent.mPlaneNum];

				Side	=(Parent.mChildren[0] == this)? false : true;

				if(!Poly.ClipPolyEpsilon(0.001f, Plane, Side))
				{
					return	false;
				}

				Node	=Parent;
				Parent	=Parent.mParent;
			}

			Out	=Poly;

			return	true;
		}


		bool PartitionPortals_r(bool bVisPortals, PlanePool pool)
		{
			GBSPPoly		NewPoly, FPoly, BPoly;
			GBSPPlane		pPlane, pPlane2;
			GBSPPortal		Portal, NewPortal, Next;
			GBSPNode		Front, Back, OtherNode;
			Int32			Side;

			CalcBoundsFromPortals();

			if(mPlaneNum == PlanePool.PLANENUM_LEAF)
			{
				return	true;
			}

			//We can stop at detail seperators for the vis tree
			if(bVisPortals && mDetail)
			{
				return	true;
			}

			Front	=mChildren[0];
			Back	=mChildren[1];

			pPlane	=pool.mPlanes[mPlaneNum];

			//Create a new portal
			if(!CreatePolyOnNode(out NewPoly, pool))
			{
				Map.Print("PartitionPortals_r:  CreatePolyOnNode failed.\n");
				return false;
			}

			//Clip it against all other portals attached to this node
			for(Portal = mPortals;Portal != null && NewPoly.mVerts.Count > 2;Portal =Portal.mNext[Side])
			{
				if(Portal.mNodes[0] == this)
				{
					Side	=0;
				}
				else if(Portal.mNodes[1] == this)
				{
					Side	=1;
				}
				else
				{
					Map.Print("PartitionPortals_r:  Portal does not look at either node.\n");
					return false;
				}

				pPlane2	=pool.mPlanes[Portal.mPlaneNum];

				if(!NewPoly.ClipPolyEpsilon(0.001f, pPlane2, Side != 0))
				{
					Map.Print("PartitionPortals_r:  There was an error clipping the poly.\n");
					return false;
				}

				if(NewPoly.mVerts.Count < 3)
				{
					Map.Print("PartitionPortals_r:  Portal was cut away.\n");
					break;
				}
			}
			
			if(NewPoly.IsTiny())
			{
				NewPoly	=null;
			}

			if(NewPoly != null)
			{
				NewPortal	=new GBSPPortal();
				if(NewPortal == null)
				{
					Map.Print("PartitionPortals_r:  Out of memory for portal.\n");
					return	false;
				}
				NewPortal.mPoly		=NewPoly;
				NewPortal.mPlaneNum	=mPlaneNum;
				NewPortal.mOnNode	=this;

				if(!NewPortal.Check())
				{
					Map.Print("PartiionPortals_r:  Check Portal failed.\n");
					return	false;
				}
				else
				{
					AddPortalToNodes(NewPortal, Front, Back);
				}
			}
			
			//Partition all portals by this node
			for(Portal = mPortals;Portal != null;Portal = Next)
			{
				if(Portal.mNodes[0] == this)
				{
					Side	=0;
				}
				else if(Portal.mNodes[1] == this)
				{
					Side	=1;
				}
				else
				{
					Map.Print("PartitionPortals_r:  Portal does not look at either node.\n");
					return	false;
				}

				Next	=Portal.mNext[Side];

				//Remember the node on the back side
				OtherNode	=(Side == 0)? Portal.mNodes[1] : Portal.mNodes[0];
				Portal.mNodes[0].RemovePortal(Portal);
				Portal.mNodes[1].RemovePortal(Portal);

				if(!Portal.mPoly.SplitEpsilon(0.001f, pPlane, out FPoly, out BPoly, false))
				{
					Map.Print("PartitionPortals_r:  Could not split portal.\n");
					return false;
				}

				if(FPoly != null && FPoly.IsTiny())
				{
					FPoly	=null;
				}

				if(BPoly != null && BPoly.IsTiny())
				{
					BPoly	=null;
				}
				
				if(FPoly == null && BPoly == null)
				{
					continue;
				}
				
				if(FPoly == null)
				{
					Portal.mPoly	=BPoly;
					if(Side != 0)
					{
						AddPortalToNodes(Portal, OtherNode, Back);
					}
					else
					{
						AddPortalToNodes(Portal, Back, OtherNode);
					}
					continue;
				}

				if(BPoly == null)
				{
					Portal.mPoly	=FPoly;
					if(Side != 0)
					{
						AddPortalToNodes(Portal, OtherNode, Front);
					}
					else
					{
						AddPortalToNodes(Portal, Front, OtherNode);
					}
					continue;
				}

				//Portal was split
				Portal.mPoly	=FPoly;
				NewPortal		=new GBSPPortal(Portal);
				if(NewPortal == null)
				{
					Map.Print("PartitionPortals_r:  Out of memory for portal.\n");
					return	false;
				}
				NewPortal.mPoly	=BPoly;
				
				if(Side != 0)
				{
					AddPortalToNodes(Portal, OtherNode, Front);
					AddPortalToNodes(NewPortal, OtherNode, Back);
				}
				else
				{
					AddPortalToNodes(Portal, Front, OtherNode);
					AddPortalToNodes(NewPortal, Back, OtherNode);
				}
			}

			if(mPortals != null)
			{
				Map.Print("*WARNING* PartitionPortals_r:  Portals still on node after distribution...\n");
			}
			
			if(!Front.PartitionPortals_r(bVisPortals, pool))
			{
				return	false;
			}
			if(!Back.PartitionPortals_r(bVisPortals, pool))
			{
				return	false;
			}
			return	true;
		}


		void RemovePortal(GBSPPortal port)
		{
			Debug.Assert(mPortals != null);
			Debug.Assert(!(mPortals.mNodes[0] == this && mPortals.mNodes[1] == this));
			Debug.Assert(mPortals.mNodes[0] == this || mPortals.mNodes[1] == this);

			//Find the portal on this node
			int	Side	=0;
			for(GBSPPortal p = mPortals;p != null;p=p.mNext[Side])
			{
				Debug.Assert(!(p.mNodes[0] == this && p.mNodes[1] == this));
				Debug.Assert(p.mNodes[0] == this || p.mNodes[1] == this);

				Side	=(p.mNodes[1] == this)? 1 : 0;

				if(p == port)
				{
					int	Side2			=(port.mNodes[1] == this)? 1 : 0;
					mPortals			=port.mNext[Side2];
					port.mNodes[Side2]	=null;
					return;
				}
				else if(p.mNext[Side] == port)
				{
					int Side2			=(port.mNodes[1] == this)? 1 : 0;
					p.mNext[Side]		=port.mNext[Side2];
					port.mNodes[Side2]	=null;
					return;
				}
			}
		}


		internal static bool AddPortalToNodes(GBSPPortal port, GBSPNode Front, GBSPNode Back)
		{
			if(port.mNodes[0] != null || port.mNodes[1] != null)
			{
				Map.Print("LinkPortal:  Portal allready looks at one of the nodes.\n");
				return	false;
			}

			port.mNodes[0]	=Front;
			port.mNext[0]	=Front.mPortals;
			Front.mPortals	=port;

			port.mNodes[1]	=Back;
			port.mNext[1]	=Back.mPortals;
			Back.mPortals	=port;

			return	true;
		}
	}
}
