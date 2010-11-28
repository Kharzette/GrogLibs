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
		public List<GBSPFace>	mLeafFaces	=new List<GBSPFace>();		// Pointer to Faces touching this leaf
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

		public const float	SUBDIVIDESIZE	=235.0f;


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
						//Node.mContents &= 0xffff0000;
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

			mBounds.mMins	=Vector3.Zero;
			mBounds.mMaxs	=Vector3.Zero;

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


		internal void GetPortalTriangles(List<Vector3> verts, List<UInt32> indexes, bool bCheckFlags)
		{
			if(mPlaneNum == PlanePool.PLANENUM_LEAF)
			{
				if((mContents & GBSPBrush.BSP_CONTENTS_SOLID2) != 0)
				{
					return;
				}
				if(mPortals == null)
				{
					return;
				}
				int	Side	=0;
				for(GBSPPortal port=mPortals;port != null;port=port.mNext[Side])
				{
					Side	=(port.mOnNode == port.mNodes[0])? 0 : 1;

					port.mPoly.GetTriangles(verts, indexes, bCheckFlags);
				}
				return;
			}

			mChildren[0].GetPortalTriangles(verts, indexes, bCheckFlags);
			mChildren[1].GetPortalTriangles(verts, indexes, bCheckFlags);
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

				Side	=(Parent.mChildren[0] == Node)? false : true;

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


		GBSPNode FindLeaf(Vector3 Origin, PlanePool pool)
		{
			GBSPPlane	Plane;
			float		Dist;

			GBSPNode	Node	=this;

			while(Node != null && Node.mPlaneNum != PlanePool.PLANENUM_LEAF)
			{
				Plane	=pool.mPlanes[Node.mPlaneNum];
				Dist	=Vector3.Dot(Origin, Plane.mNormal) - Plane.mDist;

				if(Dist > 0)
				{
					Node	=Node.mChildren[0];
				}
				else
				{
					Node	=Node.mChildren[1];
				}
			}

			if(Node == null)
			{
				Map.Print("FindLeaf:  NULL Node/Leaf.\n");
			}

			return Node;
		}


		bool PlaceEntities(List<MapEntity> ents, PlanePool pool)
		{				
			Int32		i;
			GBSPNode	Node;
			bool		Empty	=false;

			for(i=1;i < ents.Count;i++)
			{
				MapEntity	e	=ents[i];
				Vector3		org	=Vector3.Zero;

				if(!e.GetOrigin(out org))
				{
					continue;
				}

				Node	=FindLeaf(org, pool);
				if(Node == null)
				{
					return	false;
				}

				if((Node.mContents & GBSPBrush.BSP_CONTENTS_SOLID2) == 0)
				{
					Node.mEntity	=i;
					Empty			=true;
				}
			}
			
			if(!Empty)
			{
				Map.Print("PlaceEntities:  No valid entities for operation");
				return	false;
			}			
			return	true;
		}


		bool FillLeafs_r(bool Fill, Int32 Dist, ref int CurrentFill,
			ref bool HitEntity, ref int EntityHit, ref GBSPNode HitNode)
		{
			GBSPPortal		Portal;
			Int32			Side;
			
			//if (HitEntity)
			//	return GE_TRUE;
			
			if((mContents & GBSPBrush.BSP_CONTENTS_SOLID2) != 0)
			{
				return	true;
			}

			if(mCurrentFill == CurrentFill)
			{
				return	true;
			}

			mCurrentFill	=CurrentFill;
			mOccupied		=Dist;

			if(Fill)
			{
				//Preserve user contents
				mContents	&=0xffff0000;
				mContents	|=GBSPBrush.BSP_CONTENTS_SOLID2;
			}
			else 
			{
				if(mEntity != 0)
				{
					HitEntity	=true;
					EntityHit	=mEntity;
					HitNode		=this;
					return	true;
				}
			}

			for(Portal=mPortals;Portal != null;Portal = Portal.mNext[Side])
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
					Map.Print("FillLeafs_r:  Portal does not look at either node.\n");
					return	false;
				}
				
				//if (!CanPassPortal(Portal))
				//	continue;

				if(!Portal.mNodes[(Side == 0)? 1 : 0].FillLeafs_r(
					Fill, Dist+1, ref CurrentFill,
					ref HitEntity, ref EntityHit, ref HitNode))
				{
					return	false;
				}
			}

			return	true;
		}


		bool FillLeafs2_r(int CurrentFill)
		{
			GBSPPortal	Portal;
			Int32		Side;
			
			if((mContents & GBSPBrush.BSP_CONTENTS_SOLID2) != 0)
			{
				return	true;
			}

			if(mCurrentFill == CurrentFill)
			{
				return	true;
			}

			mCurrentFill	=CurrentFill;

			for(Portal=mPortals;Portal != null;Portal = Portal.mNext[Side])
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
					Map.Print("RemoveOutside2_r:  Portal does not look at either node.\n");
					return	false;
				}

				//Go though the portal to the node on the other side (!side)
				if(!Portal.mNodes[(Side==0)? 1 : 0].FillLeafs2_r(CurrentFill))
				{
					return	false;
				}
			}

			return	true;
		}


		bool FillFromEntities(List<MapEntity> ents, PlanePool pool, int currentFill)
		{
			Int32		i;
			GBSPNode	Node;
			bool		Empty;

			Empty	=false;
			
			for(i=1;i < ents.Count;i++)	//Don't use the world as an entity (skip 0)!!
			{
				MapEntity	e	=ents[i];
				Vector3		org	=Vector3.Zero;

				if(!e.GetOrigin(out org))
				{
					continue;
				}

				Node	=FindLeaf(org, pool);

				if((Node.mContents & GBSPBrush.BSP_CONTENTS_SOLID2) != 0)
				{
					continue;
				}
				
				//There is at least one entity in empty space...
				Empty	=true;
				
				if(!Node.FillLeafs2_r(currentFill))
				{
					return	false;
				}
			}

			if(!Empty)
			{
				Map.Print("FillFromEntities:  No valid entities for operation.\n");
				return	false;
			}

			return	true;
		}


		void FillUnTouchedLeafs_r(int currentFill, ref int NumRemovedLeafs)
		{
			if(mPlaneNum != PlanePool.PLANENUM_LEAF)
			{
				mChildren[0].FillUnTouchedLeafs_r(currentFill, ref NumRemovedLeafs);
				mChildren[1].FillUnTouchedLeafs_r(currentFill, ref NumRemovedLeafs);
				return;
			}

			if((mContents & GBSPBrush.BSP_CONTENTS_SOLID2) != 0)
			{
				return;		//allready solid or removed...
			}

			if(mCurrentFill != currentFill)
			{
				//Fill er in with solid so it does not show up...(Preserve user contents)
				mContents	&=(0xffff0000);
				mContents	|=GBSPBrush.BSP_CONTENTS_SOLID2;
				NumRemovedLeafs++;
			}
		}


		internal int RemoveHiddenLeafs(GBSPNode outsideNode, List<MapEntity> ents,
										ref int numRemovedLeafs, PlanePool pool)
		{
			Int32	Side;

			Map.Print(" --- Remove Hidden Leafs --- \n");

			Side	=(outsideNode.mPortals.mNodes[0] == outsideNode)? 1 : 0;

			numRemovedLeafs	=0;

			if(!PlaceEntities(ents, pool))
			{
				return	-1;
			}

			bool		HitEntity	=false;
			GBSPNode	HitNode		=null;
			int			CurrentFill	=1;
			int			EntityHit	=0;

			if(!outsideNode.mPortals.mNodes[Side].FillLeafs_r(false, 1,
				ref CurrentFill, ref HitEntity, ref EntityHit, ref HitNode))
			{
				return -1;
			}

			if(HitEntity)
			{
				Map.Print("*****************************************\n");
				Map.Print("*           *** LEAK ***                *\n");
				Map.Print("* Level is NOT sealed.                  *\n");
				Map.Print("* Optimal removal will not be performed.*\n");
				Map.Print("*****************************************\n");

//				WriteLeakFile("Test", HitNode, ONode);
				return	-1;
			}

			CurrentFill	=2;
			
			if(!FillFromEntities(ents, pool, CurrentFill))
			{
				return	-1;
			}
			
			FillUnTouchedLeafs_r(CurrentFill, ref numRemovedLeafs);

			Map.Print("Removed Leafs          : " + numRemovedLeafs + "\n");

			return	numRemovedLeafs;
		}


		void MarkVisibleSides_r(PlanePool pool)
		{
			GBSPPortal	p;
			Int32		s;

			//Recurse to leafs 
			if(mPlaneNum != PlanePool.PLANENUM_LEAF)
			{
				mChildren[0].MarkVisibleSides_r(pool);
				mChildren[1].MarkVisibleSides_r(pool);
				return;
			}

			// Empty (air) leafs don't have faces
			if(mContents == 0)
			{
				return;
			}

			for(p=mPortals;p != null;p=p.mNext[s])
			{
				s	=(p.mNodes[1] == this)? 1 : 0;

				if(p.mOnNode == null)
				{
					continue;		// Outside node (assert for it here!!!)
				}

				if(p.mSideFound == 0)
				{
					p.FindPortalSide(s, pool);
				}

				if(p.mSide != null)
				{
					p.mSide.mFlags	|=GBSPSide.SIDE_VISIBLE;
				}

				if(p.mSide != null)
				{
					int	sOpposite	=(s == 0)? 1 : 0;
					if(((p.mNodes[sOpposite].mContents & GBSPBrush.BSP_CONTENTS_SOLID2) == 0)
						&& ((p.mNodes[s].mContents & GBSPBrush.BSP_CONTENTS_SHEET) != 0)
						&& ((p.mSide.mFlags & GBSPSide.SIDE_SHEET) == 0))
					{ 
						p.mSide.mFlags	&=~GBSPSide.SIDE_VISIBLE;
						p.mSide			=null;
						p.mSideFound	=1;		// Don't look for this side again!!!
					}
				}
			}

		}


		internal void MarkVisibleSides(List<MapBrush> list, PlanePool pool)
		{
			Map.Print("--- Map Portals to Brushes ---\n");

			//Clear all the visible flags
			foreach(MapBrush b in list)
			{
				foreach(GBSPSide s in b.mOriginalSides)
				{
					s.mFlags	&=~GBSPSide.SIDE_VISIBLE;
				}
			}
			
			//Set visible flags on the sides that are used by portals
			MarkVisibleSides_r(pool);
		}


		static bool FreePortals_r(GBSPNode Node)
		{
			GBSPPortal	Portal, Next;
			Int32		Side;

			if(Node == null)
			{
				return	true;
			}
			
			for(Portal=Node.mPortals;Portal != null;Portal = Next)
			{
				if(Portal.mNodes[0] == Node)
				{
					Side	=0;
				}
				else if(Portal.mNodes[1] == Node)
				{
					Side	=1;
				}
				else
				{
					Map.Print("FreePortals_r:  Portal does not look at either node.\n");
					return	false;
				}

				Next	=Portal.mNext[Side];

				Portal.mNodes[0].RemovePortal(Portal);
				Portal.mNodes[1].RemovePortal(Portal);

				//free portal here
			}

			Node.mPortals	=null;

			if(Node.mPlaneNum == PlanePool.PLANENUM_LEAF)
			{
				return	true;
			}

			if(!FreePortals_r(Node.mChildren[0]))
			{
				return	false;
			}

			if(!FreePortals_r(Node.mChildren[1]))
			{
				return	false;
			}
			return	true;
		}


		internal bool FreePortals()
		{
			return	FreePortals_r(this);
		}


		internal void FreeBSP_r()
		{
			if(mPlaneNum == PlanePool.PLANENUM_LEAF)
			{
				mLeafFaces	=null;

				GBSPFace	Next	=null;
				for(GBSPFace f=mFaces;f != null;f=Next)
				{
					Next	=f.mNext;
					f		=null;
				}
				GBSPBrush	NextB	=null;
				for(GBSPBrush b=mBrushList;b != null;b=NextB)
				{
					NextB	=b.mNext;
					b.mSides.Clear();
				}
				return;
			}

			mChildren[0].FreeBSP_r();
			mChildren[1].FreeBSP_r();
		}

		internal void MakeFaces(PlanePool pool, TexInfoPool tip)
		{
			Map.Print("--- Finalize Faces ---\n");
			
			int	NumMerged		=0;
			int	NumSubdivided	=0;
			int	NumMakeFaces	=0;

			MakeFaces_r(pool, tip, ref NumMerged, ref NumSubdivided, ref NumMakeFaces);

			Map.Print("TotalFaces             : " + NumMakeFaces + "\n");
			Map.Print("Merged Faces           : " + NumMerged + "\n");
			Map.Print("Subdivided Faces       : " + NumSubdivided + "\n");
			Map.Print("FinalFaces             : " + ((NumMakeFaces-NumMerged)+NumSubdivided) + "\n");
		}


		void SubdivideFace(GBSPFace Face, TexInfoPool tip, ref int NumSubdivided)
		{
			float		Mins, Maxs;
			float		v;
			Int32		Axis, i;
			TexInfo		Tex;
			Vector3		Temp;
			float		Dist;
			GBSPPoly	p, Frontp, Backp;
			GBSPPlane	Plane;

			if(Face.mMerged != null)
			{
				return;
			}

			//Special (non-surface cached) faces don't need subdivision
			Tex	=tip.mTexInfos[Face.mTexInfo];

			//if ( Tex->Flags & TEXINFO_GOURAUD)
			//	FanFace_r(Node, Face);

			if((Tex.mFlags & TexInfo.TEXINFO_NO_LIGHTMAP) != 0)
			{
				return;
			}

			for(Axis=0;Axis < 2;Axis++)
			{
				while(true)
				{
					float	TexSize, SplitDist;
					float	Mins16, Maxs16;

					Mins	=999999.0f;
					Maxs	=-999999.0f;

					if(Axis == 0)
					{
						Temp	=Tex.mUVec;
					}
					else
					{
						Temp	=Tex.mVVec;
					}					

					for(i=0;i < Face.mPoly.mVerts.Count;i++)
					{
						v	=Vector3.Dot(Face.mPoly.mVerts[i], Temp);
						if(v < Mins)
						{
							Mins	=v;
						}
						if(v > Maxs)
						{
							Maxs	=v;
						}
					}
					
					Mins16	=(float)Math.Floor(Mins / 16.0f) * 16.0f;
					Maxs16	=(float)Math.Ceiling(Maxs / 16.0f) * 16.0f;

					TexSize	=Maxs - Mins;
					//TexSize = Maxs16 - Mins16;

					// Default to the Subdivide size
					SplitDist	=SUBDIVIDESIZE;

					if(TexSize <= SUBDIVIDESIZE)
					{
						break;
					}
					
					//Make a copy
					p	=new GBSPPoly(Face.mPoly);

					//Split it
					NumSubdivided++;

					v	=Temp.Length();
					Temp.Normalize();				

					Dist	=(Mins + SplitDist - 16) / v;
					//Dist = (Mins16 + SplitDist)/v;

					Plane.mNormal	=Temp;
					Plane.mDist		=Dist;
					Plane.mType		=GBSPPlane.PLANE_ANY;

					p.Split(Plane, out Frontp, out Backp, false);
					
					if(Frontp == null || Backp == null)
					{
						Map.Print("*WARNING* SubdivideFace: didn't split the polygon: ");
						Map.Print("" + Mins + ", " + Maxs + ", " + SplitDist + "\n");
						break;
					}

					Face.mSplit[0]			=new GBSPFace(Face);
					Face.mSplit[0].mPoly	=Frontp;
					Face.mSplit[0].mNext	=mFaces;

					mFaces	=Face.mSplit[0];

					Face.mSplit[1]			=new GBSPFace(Face);
					Face.mSplit[1].mPoly	=Backp;
					Face.mSplit[1].mNext	=mFaces;

					mFaces	=Face.mSplit[1];

					SubdivideFace(Face.mSplit[0], tip, ref NumSubdivided);
					SubdivideFace(Face.mSplit[1], tip, ref NumSubdivided);
					return;
				}
			}
		}


		void SubdivideNodeFaces(TexInfoPool tip, ref int numSubDivd)
		{
			GBSPFace	f;

			for(f = mFaces;f != null;f=f.mNext)
			{
				SubdivideFace(f, tip, ref numSubDivd);
			}
		}


		void MakeFaces_r(PlanePool pool, TexInfoPool tip, ref int NumMerged, ref int NumSubdivided, ref int NumMakeFaces)
		{
			GBSPPortal	p;
			Int32		s;

			//Recurse down to leafs
			if(mPlaneNum != PlanePool.PLANENUM_LEAF)
			{
				mChildren[0].MakeFaces_r(pool, tip, ref NumMerged, ref NumSubdivided, ref NumMakeFaces);
				mChildren[1].MakeFaces_r(pool, tip, ref NumMerged, ref NumSubdivided, ref NumMakeFaces);
				
				//Marge list (keepin that typo, funny)
				GBSPFace.MergeFaceList2(mFaces, pool, ref NumMerged);

				//Subdivide them for lightmaps
				//SubdivideNodeFaces(tip, ref NumSubdivided);
				return;
			}

			//Solid leafs never have visible faces
			if((mContents & GBSPBrush.BSP_CONTENTS_SOLID2) != 0)
			{
				return;
			}

			//See which portals are valid
			for(p=mPortals;p != null;p=p.mNext[s])
			{
				s	=(p.mNodes[1] == this)? 1 : 0;

				p.mFace[s]	=p.FaceFromPortal(s);
				if(p.mFace[s] != null)
				{
					//Record the contents on each side of the face
					p.mFace[s].mContents[0]	=mContents;			// Front side contents is this leaf
					p.mFace[s].mContents[1]	=p.mNodes[(s == 0)? 1 : 0].mContents;	// Back side contents is the leaf on the other side of this portal

					//Add the face to the list of faces on the node that originaly created the portal
					p.mFace[s].mNext	=p.mOnNode.mFaces;
					p.mOnNode.mFaces	=p.mFace[s];

					NumMakeFaces++;
				}
			}
		}


		internal void MakeLeafFaces()
		{
			GBSPPortal	p;
			Int32		s;

			//Recurse down to leafs
			if(mPlaneNum != PlanePool.PLANENUM_LEAF)
			{
				mChildren[0].MakeLeafFaces();
				mChildren[1].MakeLeafFaces();
				return;
			}

			//Solid leafs never have visible faces
			if((mContents & GBSPBrush.BSP_CONTENTS_SOLID2) != 0)
			{
				return;
			}

			//Reset counter
			mNumLeafFaces	=0;

			//See which portals are valid
			for(p=mPortals;p != null;p=p.mNext[s])
			{
				s	=(p.mNodes[1] == this)? 1 : 0;

				if(p.mFace[s] == null)
				{
					continue;
				}

				CountLeafFaces_r(p.mFace[s]);
			}

			//Reset counter
			mNumLeafFaces	=0;
			
			//See which portals are valid
			for(p=mPortals;p != null;p=p.mNext[s])
			{
				s	=(p.mNodes[1] == this)? 1 : 0;

				if(p.mFace[s] == null)
				{
					continue;
				}

				GetLeafFaces_r(p.mFace[s]);
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


		void CountLeafFaces_r(GBSPFace Face)
		{
			while(Face.mMerged != null)
			{
				Face	=Face.mMerged;
			}

			if(Face.mSplit[0] != null)
			{
				CountLeafFaces_r(Face.mSplit[0]);
				CountLeafFaces_r(Face.mSplit[1]);
				return;
			}

			mNumLeafFaces++;
		}


		void MergeNodes_r(ref int mergedNodes)
		{
			GBSPBrush	b, Next;

			if(mPlaneNum == PlanePool.PLANENUM_LEAF)
			{
				return;
			}

			mChildren[0].MergeNodes_r(ref mergedNodes);
			mChildren[1].MergeNodes_r(ref mergedNodes);

			if(mChildren[0].mPlaneNum == PlanePool.PLANENUM_LEAF
				&& mChildren[1].mPlaneNum == PlanePool.PLANENUM_LEAF)
			{
				if(((mChildren[0].mContents & GBSPBrush.BSP_CONTENTS_SOLID2) != 0)
					&& ((mChildren[1].mContents & GBSPBrush.BSP_CONTENTS_SOLID2) != 0))
				{
					if((mChildren[0].mContents & 0xffff0000)
						== (mChildren[1].mContents & 0xffff0000))
					{
						if(mFaces != null)
						{
							Map.Print("Node.mFaces seperating BSP_CONTENTS_SOLID!");
						}

						if(mChildren[0].mFaces != null || mChildren[1].mFaces != null)
						{
							Map.Print("!Node.mFaces with children");
						}

						// FIXME: free stuff
						mPlaneNum	=PlanePool.PLANENUM_LEAF;
						//mContents = GBSPBrush.BSP_CONTENTS_SOLID2;
						mContents	=mChildren[0].mContents;
						mContents	|=mChildren[1].mContents;

						mDetail	=false;

						if(mBrushList != null)
						{
							Map.Print("MergeNodes: node.mBrushList");
						}

						//combine brush lists
						mBrushList	=mChildren[1].mBrushList;

						for(b = mChildren[0].mBrushList;b != null;b=Next)
						{
							Next		=b.mNext;
							b.mNext		=mBrushList;
							mBrushList	=b;
						}
						mergedNodes++;
					}
				}
			}
		}


		internal void MergeNodes()
		{
			Map.Print("--- Merge Nodes ---\n");

			int	MergedNodes	=0;
			
			MergeNodes_r(ref MergedNodes);

			Map.Print("Num Merged             : " + MergedNodes + "\n");
		}
	}
}
