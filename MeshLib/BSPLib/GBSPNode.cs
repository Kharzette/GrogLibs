using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class BuildStats
	{
		public Int32	NumVisFaces;
		public Int32	NumNonVisFaces;
		public Int32	NumVisBrushes;
		public Int32	NumVisNodes;
		public Int32	NumNonVisNodes;
	}


	public class GBSPNode
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

		GBSPBrush	mVolume;
		GBSPSide	mSide;
		GBSPBrush	mBrushList;

		// For GFX file saving
		public Int32	[]mChildrenID	=new int[2];
		public Int32	mFirstFace;
		public Int32	mNumFaces;
		public Int32	mFirstPortal;
		public Int32	mNumPortals;

		public Int32	mFirstSide;			// For bevel bbox clipping
		public Int32	mNumSides;

		public const float	SUBDIVIDESIZE		=235.0f;
		public const int	MAX_TEMP_LEAF_SIDES	=100;
		public const int	MAX_LEAF_SIDES		=64000 * 2;
		public const int	MAX_AREAS			=256;
		public const int	MAX_AREA_PORTALS	=1024;


		internal void BuildBSP(GBSPBrush brushList, PlanePool pool, bool bVerbose)
		{
			if(bVerbose)
			{
				Map.Print("--- Build BSP Tree ---\n");
			}

			BuildStats	bs		=new BuildStats();
			Bounds		bounds	=new Bounds();
			GBSPBrush.BrushListStats(brushList, bs, bounds, pool);

			if(bVerbose)
			{
				Map.Print("Total Brushes          : " + bs.NumVisBrushes + "\n");
				Map.Print("Total Faces            : " + bs.NumVisFaces + "\n");
				Map.Print("Faces Removed          : " + bs.NumNonVisFaces + "\n");
			}
			bs.NumVisNodes		=0;
			bs.NumNonVisNodes	=0;

			BuildTree_r(bs, brushList, pool);


			//Top node is always valid, this way portals can use top node to get box of entire bsp...
			mBounds	=new Bounds(bounds);

			if(bVerbose)
			{
				Map.Print("Total Nodes            : " + (bs.NumVisNodes/2 - bs.NumNonVisNodes) + "\n");
				Map.Print("Nodes Removed          : " + bs.NumNonVisNodes + "\n");
				Map.Print("Total Leafs            : " + (bs.NumVisNodes+1)/2 + "\n");
			}
		}


		void LeafNode(GBSPBrush Brushes)
		{
			mPlaneNum	=PlanePool.PLANENUM_LEAF;
			mContents	=GBSPBrush.GetLeafContents(Brushes);

			//Once brushes get down to the leafs, we don't need
			//to keep the polys on them anymore...
			//We can free them now...
			GBSPBrush.FreeSidePolys(Brushes);

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


		internal bool CreatePortals(GBSPNode outNode, bool bVis, bool bVerbose,
			PlanePool pool, Vector3 nodeMins, Vector3 nodeMaxs)
		{
			if(bVerbose)
			{
				Map.Print(" --- Create Portals --- \n");
			}

			if(!CreateAllOutsidePortals(pool, ref outNode, nodeMins, nodeMaxs))
			{
				Map.Print("CreatePortals:  Could not create bbox portals.\n");
				return	false;
			}

			if(!PartitionPortals_r(pool, bVis))
			{
				Map.Print("CreatePortals:  Could not partition portals.\n");
				return	false;
			}
			return	true;
		}


		bool CreateAllOutsidePortals(PlanePool pool, ref GBSPNode outsideNode,
			Vector3 nodeMins, Vector3 nodeMaxs)
		{
			GBSPPlane	[]PPlanes	=new GBSPPlane[6];
			GBSPPortal	[]Portals	=new GBSPPortal[6];

			//clear outside node
			outsideNode.mArea			=0;
			outsideNode.mBounds.mMins	=Vector3.Zero;
			outsideNode.mBounds.mMaxs	=Vector3.Zero;
			outsideNode.mBrushList		=null;
			outsideNode.mChildren[0]	=null;
			outsideNode.mChildren[1]	=null;
			outsideNode.mChildrenID[0]	=0;
			outsideNode.mChildrenID[1]	=0;
			outsideNode.mCluster		=0;
			outsideNode.mCurrentFill	=0;
			outsideNode.mbDetail			=false;
			outsideNode.mEntity			=0;
			outsideNode.mFaces			=null;
			outsideNode.mFirstFace		=0;
			outsideNode.mFirstPortal	=0;
			outsideNode.mFirstSide		=0;
			outsideNode.mLeafFaces		=null;
			outsideNode.mNumFaces		=0;
			outsideNode.mNumLeafFaces	=0;
			outsideNode.mNumPortals		=0;
			outsideNode.mNumSides		=0;
			outsideNode.mOccupied		=0;
			outsideNode.mParent			=null;
			outsideNode.mPortalLeafNum	=0;
			outsideNode.mPortals		=null;
			outsideNode.mSide			=null;
			outsideNode.mVolume			=null;

			outsideNode.mPlaneNum	=PlanePool.PLANENUM_LEAF;
			outsideNode.mContents	=Contents.BSP_CONTENTS_SOLID2;

			//So there won't be NULL volume leafs when we create the outside portals
			for(int k=0;k < 3;k++)
			{
				if(UtilityLib.Mathery.VecIdx(nodeMins, k) - 128.0f
					<= -Bounds.MIN_MAX_BOUNDS ||
					UtilityLib.Mathery.VecIdx(nodeMaxs, k) + 128.0f
					>= Bounds.MIN_MAX_BOUNDS)
				{
					Map.Print("CreateAllOutsidePortals:  World BOX out of range...\n");
					return	false;
				}
			}

			nodeMins	-=(Vector3.One * 128.0f);
			nodeMaxs	+=(Vector3.One * 128.0f);

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
						PPlanes[Index].mDist	=UtilityLib.Mathery.VecIdx(nodeMins, i);
					}
					else
					{
						UtilityLib.Mathery.VecIdxAssign(ref PPlanes[Index].mNormal, i, -1.0f);
						PPlanes[Index].mDist	=-UtilityLib.Mathery.VecIdx(nodeMaxs, i);
					}
					
					Portals[Index]	=GBSPPortal.CreateOutsidePortal(PPlanes[Index], this, pool, ref outsideNode);

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

					if(Portals[i].mPoly.VertCount() < 3)
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
				if((mContents & Contents.BSP_CONTENTS_SOLID2) != 0)
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

				p.mPoly.AddToBounds(mBounds);
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


		bool PartitionPortals_r(PlanePool pool, bool bVisPortals)
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
			if(bVisPortals && mbDetail)
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
			for(Portal = mPortals;Portal != null && NewPoly.VertCount() > 2;Portal =Portal.mNext[Side])
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

				if(NewPoly.VertCount() < 3)
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
			
			if(!Front.PartitionPortals_r(pool, bVisPortals))
			{
				return	false;
			}
			if(!Back.PartitionPortals_r(pool, bVisPortals))
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

				if((Node.mContents & Contents.BSP_CONTENTS_SOLID2) == 0)
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


		bool FillLeafs_r(bool Fill, Int32 Dist, int curFill,
			ref bool hitEnt, ref GBSPNode hitNode)
		{
			GBSPPortal		Portal;
			Int32			Side;
			
			//if (HitEntity)
			//	return GE_TRUE;
			
			if((mContents & Contents.BSP_CONTENTS_SOLID2) != 0)
			{
				return	true;
			}

			if(mCurrentFill == curFill)
			{
				return	true;
			}

			mCurrentFill	=curFill;
			mOccupied		=Dist;

			if(Fill)
			{
				//Preserve user contents
				mContents	&=0xffff0000;
				mContents	|=Contents.BSP_CONTENTS_SOLID2;
			}
			else 
			{
				if(mEntity != 0)
				{
					hitEnt		=true;
//					entHit		=mEntity;
					hitNode		=this;
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

				if(!Portal.mNodes[(Side == 0)? 1 : 0].FillLeafs_r(Fill,
					Dist + 1, curFill, ref hitEnt, ref hitNode))
				{
					return	false;
				}
			}

			return	true;
		}


		bool FillLeafs2_r(int curFill)
		{
			GBSPPortal	Portal;
			Int32		Side;
			
			if((mContents & Contents.BSP_CONTENTS_SOLID2) != 0)
			{
				return	true;
			}

			if(mCurrentFill == curFill)
			{
				return	true;
			}

			mCurrentFill	=curFill;

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
				if(!Portal.mNodes[(Side==0)? 1 : 0].FillLeafs2_r(curFill))
				{
					return	false;
				}
			}

			return	true;
		}


		bool FillFromEntities(Int32 curFill, List<MapEntity> ents, PlanePool pool)
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

				if((Node.mContents & Contents.BSP_CONTENTS_SOLID2) != 0)
				{
					continue;
				}
				
				//There is at least one entity in empty space...
				Empty	=true;
				
				if(!Node.FillLeafs2_r(curFill))
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


		void FillUnTouchedLeafs_r(Int32 curFill, ref int numRemovedLeafs)
		{
			if(mPlaneNum != PlanePool.PLANENUM_LEAF)
			{
				mChildren[0].FillUnTouchedLeafs_r(curFill, ref numRemovedLeafs);
				mChildren[1].FillUnTouchedLeafs_r(curFill, ref numRemovedLeafs);
				return;
			}

			if((mContents & Contents.BSP_CONTENTS_SOLID2) != 0)
			{
				return;		//allready solid or removed...
			}

			if(mCurrentFill != curFill)
			{
				//Fill er in with solid so it does not show up...(Preserve user contents)
				mContents	&=(0xffff0000);
				mContents	|=Contents.BSP_CONTENTS_SOLID2;
				numRemovedLeafs++;
			}
		}


		internal int RemoveHiddenLeafs(GBSPNode oNode,
			List<MapEntity> ents,
			PlanePool pool, bool bVerbose)
		{
			Int32	Side;

			Map.Print(" --- Remove Hidden Leafs --- \n");

			GBSPNode	outsideNode	=oNode;

			Side	=(outsideNode.mPortals.mNodes[0] == outsideNode)? 1 : 0;

			int	NumRemovedLeafs	=0;

			if(!PlaceEntities(ents, pool))
			{
				return	-1;
			}

			bool		HitEntity	=false;
			GBSPNode	HitNode		=null;
			int			CurrentFill	=1;

			if(!outsideNode.mPortals.mNodes[Side].FillLeafs_r(false, 1, CurrentFill, ref HitEntity, ref HitNode))
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
			
			if(!FillFromEntities(CurrentFill, ents, pool))
			{
				return	-1;
			}
			
			FillUnTouchedLeafs_r(CurrentFill, ref NumRemovedLeafs);

			if(bVerbose)
			{
				Map.Print("Removed Leafs          : " + NumRemovedLeafs + "\n");
			}

			return	NumRemovedLeafs;
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
					if(((p.mNodes[sOpposite].mContents & Contents.BSP_CONTENTS_SOLID2) == 0)
						&& ((p.mNodes[s].mContents & Contents.BSP_CONTENTS_SHEET) != 0)
						&& ((p.mSide.mFlags & GBSPSide.SIDE_SHEET) == 0))
					{ 
						p.mSide.mFlags	&=~GBSPSide.SIDE_VISIBLE;
						p.mSide			=null;
						p.mSideFound	=1;		// Don't look for this side again!!!
					}
				}
			}

		}


		internal void MarkVisibleSides(List<MapBrush> list, PlanePool pool, bool bVerbose)
		{
			if(bVerbose)
			{
				Map.Print("--- Map Portals to Brushes ---\n");
			}

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
				Map.Print("--- Finalize Faces ---\n");
			}
			
			int	NumMerged		=0;
			int	NumMakeFaces	=0;

			MakeFaces_r(pool, tip, ref NumMerged, ref NumMakeFaces);

			if(bVerbose)
			{
				Map.Print("TotalFaces             : " + NumMakeFaces + "\n");
				Map.Print("Merged Faces           : " + NumMerged + "\n");
				Map.Print("FinalFaces             : " + ((NumMakeFaces - NumMerged)) + "\n");
			}
		}


		void MakeFaces_r(PlanePool pool, TexInfoPool tip, ref int NumMerged, ref int NumMake)
		{
			GBSPPortal	p;
			Int32		s;

			//Recurse down to leafs
			if(mPlaneNum != PlanePool.PLANENUM_LEAF)
			{
				mChildren[0].MakeFaces_r(pool, tip, ref NumMerged, ref NumMake);
				mChildren[1].MakeFaces_r(pool, tip, ref NumMerged, ref NumMake);
				
				//Marge list (keepin that typo, funny)
				GBSPFace.MergeFaceList2(mFaces, pool, ref NumMerged);

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
			for(p=mPortals;p != null;p=p.mNext[s])
			{
				s	=(p.mNodes[1] == this)? 1 : 0;

				p.mFace[s]	=p.FaceFromPortal(s);
				if(p.mFace[s] != null)
				{
					//Record the contents on each side of the face
					p.mFace[s].SetContents(0, mContents);
					p.mFace[s].SetContents(1, p.mNodes[(s == 0)? 1 : 0].mContents);	// Back side contents is the leaf on the other side of this portal

					//Add the face to the list of faces on the node
					//that originaly created the portal
					GBSPFace.AddToListStart(ref p.mOnNode.mFaces, p.mFace[s]);

					NumMake++;
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
			if((mContents & Contents.BSP_CONTENTS_SOLID2) != 0)
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
							Map.Print("Node.mFaces seperating BSP_CONTENTS_SOLID!");
						}

						if(mChildren[0].mFaces != null || mChildren[1].mFaces != null)
						{
							Map.Print("!Node.mFaces with children");
						}

						// FIXME: free stuff
						mPlaneNum	=PlanePool.PLANENUM_LEAF;
						mContents	=mChildren[0].mContents;
						mContents	|=mChildren[1].mContents;

						mbDetail	=false;

						if(mBrushList != null)
						{
							Map.Print("MergeNodes: node.mBrushList");
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
				Map.Print("--- Merge Nodes ---\n");
			}

			int	MergedNodes	=0;
			
			MergeNodes_r(ref MergedNodes);

			if(bVerbose)
			{
				Map.Print("Num Merged             : " + MergedNodes + "\n");
			}
		}


		internal bool CreateLeafClusters(bool bVerbose, ref int numLeafClusters)
		{
			Map.Print(" --- CreateLeafClusters --- \n");

			if(!CreateLeafClusters_r(ref numLeafClusters))
			{
				Map.Print("CreateLeafClusters:  Failed to find leaf clusters.\n");
				return	false;
			}

			if(bVerbose)
			{
				Map.Print("Num Clusters       : " + numLeafClusters + "\n");
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


		internal bool CreateLeafSides(PlanePool pool,
			List<GFXLeafSide> leafSides, bool bVerbose)
		{
			if(bVerbose)
			{
				Map.Print(" --- Create Leaf Sides --- \n");
			}

			int	numLeafBevels	=0;
			
			if(!CreateLeafSides_r(pool, ref numLeafBevels, leafSides))
			{
				return	false;
			}

			if(bVerbose)
			{
				Map.Print("Num Leaf Sides       : " + leafSides.Count);
				Map.Print("Num Leaf Bevels      : " + numLeafBevels);
			}
			return	true;
		}


		bool CreateLeafSides_r(PlanePool pool, ref int numLeafBevels,
			List<GFXLeafSide> leafSides)
		{
			GBSPPortal	Portal, Next;
			Int32		Side, i;

			mFirstSide	=-1;
			mNumSides	=0;

			//At a leaf, convert portals to leaf sides...
			if(mPlaneNum == PlanePool.PLANENUM_LEAF)
			{
				//Don't convert empty leafs
				if((mContents & Contents.BSP_CONTENTS_SOLID_CLIP) == 0)
				{
					return	true;
				}

				if(mPortals == null)
				{
					Map.Print("*WARNING* CreateLeafSides:  Contents leaf with no portals!\n");
					return	true;
				}

				//Reset number of sides for this solid leaf (should we save out other contents?)
				//	(this is just for a collision hull for now...)
				int	CNumLeafSides	=0;

				List<Int32>	LPlaneNumbers	=new List<int>();
				List<Int32>	LPlaneSides		=new List<int>();

				for(Portal=mPortals;Portal != null;Portal=Next)
				{
					Side	=(Portal.mNodes[0] == this)? 1 : 0;
					Next	=Portal.mNext[(Side == 0)? 1 : 0];

					for(i=0;i < CNumLeafSides;i++)
					{
						if(LPlaneNumbers[i] == Portal.mPlaneNum
							&& LPlaneSides[i] == Side)
						{
							break;
						}
					}

					//Make sure we don't duplicate planes (this happens with portals)
					if(i >= MAX_TEMP_LEAF_SIDES)
					{
						Map.Print("CreateLeafSides_r:  Max portal leaf sides.\n");
						return	false;
					}

					if(i >= CNumLeafSides)
					{
						LPlaneNumbers.Add(Portal.mPlaneNum);
						LPlaneSides.Add(Side);
						CNumLeafSides++;
					}					
				}
				
				if(!FinishLeafSides(pool, ref CNumLeafSides,
					LPlaneSides, LPlaneNumbers,
					ref numLeafBevels, leafSides))
				{
					return	false;
				}

				return	true;
			}

			if(!mChildren[0].CreateLeafSides_r(pool, ref numLeafBevels, leafSides))
			{
				return	false;
			}
			if(!mChildren[1].CreateLeafSides_r(pool, ref numLeafBevels, leafSides))
			{
				return	false;
			}

			return	true;
		}


		bool FinishLeafSides(PlanePool pool, ref int cNumLeafSides,
			List<int> LPlaneSides, List<int> LPlaneNumbers,
			ref int numLeafBevels, List<GFXLeafSide> leafSides)
		{
			GBSPPlane	Plane	=new GBSPPlane();
			Int32		Axis, i, Dir;
			Bounds		bnd;

			if(!GetLeafBBoxFromPortals(out bnd))
			{
				Map.Print("FinishLeafSides:  Could not get leaf portal BBox.\n");
				return	false;
			}
			
			if(cNumLeafSides < 4)
			{
				Map.Print("*WARNING*  FinishLeafSides:  Incomplete leaf volume.\n");
			}
			else
			{
				//Add any bevel planes to the sides so we can expand them for axial box collisions
				for(Axis=0;Axis < 3;Axis++)
				{
					for(Dir=-1;Dir <= 1;Dir += 2)
					{
						//See if the plane is allready in the sides
						for(i=0;i < cNumLeafSides;i++)
						{
							Plane	=pool.mPlanes[LPlaneNumbers[i]];
								
							if(LPlaneSides[i] != 0)
							{
								Plane.Inverse();
							}
							if(UtilityLib.Mathery.VecIdx(Plane.mNormal, Axis) == Dir)
							{
								break;
							}
						}
						if(i >= cNumLeafSides)
						{
							//Add a new axial aligned side
							Plane.mNormal	=Vector3.Zero;

							UtilityLib.Mathery.VecIdxAssign(ref Plane.mNormal, Axis, Dir);

							//get the mins/maxs from the gbsp brush
							if(Dir == 1)
							{
								Plane.mDist	=UtilityLib.Mathery.VecIdx(bnd.mMaxs, Axis);
							}
							else
							{
								Plane.mDist	=-UtilityLib.Mathery.VecIdx(bnd.mMins, Axis);
							}

							sbyte	side;
							LPlaneNumbers.Add(pool.FindPlane(Plane, out side));
							LPlaneSides.Add(side);
							
							if(LPlaneNumbers[i] == -1)
							{
								Map.Print("FinishLeafSides:  Could not create the plane.\n");
								return	false;
							}

							cNumLeafSides++;							
							numLeafBevels++;
						}
					}
				}
			}
			mFirstSide	=leafSides.Count;
			mNumSides	=cNumLeafSides;
			
			for(i=0;i < cNumLeafSides;i++)
			{
				if(cNumLeafSides >= MAX_LEAF_SIDES)
				{
					Map.Print("FinishLeafSides:  Max Leaf Sides.\n");
					return	false;
				}
				GFXLeafSide	ls	=new GFXLeafSide();
				ls.mPlaneNum	=LPlaneNumbers[i];
				ls.mPlaneSide	=LPlaneSides[i];
				leafSides.Add(ls);
			}
			return	true;
		}


		bool GetLeafBBoxFromPortals(out Bounds bnd)
		{
			GBSPPortal	port;
			Int32		side;

			bnd	=new Bounds();
			bnd.Clear();

			if(mPlaneNum != PlanePool.PLANENUM_LEAF)
			{
				Map.Print("GetLeafBBoxFromPortals:  Not a leaf.\n");
				return	false;
			}

			for(port=mPortals;port != null;port=port.mNext[side])
			{
				side	=(port.mNodes[1] == this)? 1 : 0;

				port.mPoly.AddToBounds(bnd);
			}
			return	true;
		}


		internal delegate GBSPModel ModelForLeafNode(GBSPNode n);

		bool FillAreas_r(Int32 Area, ModelForLeafNode modForLeaf)
		{
			GBSPPortal	Portal;
			Int32		Side;

			if ((mContents & Contents.BSP_CONTENTS_SOLID2) != 0)
			{
				return	true;	//Stop at solid leafs
			}

			if((mContents & Contents.BSP_CONTENTS_AREA2) != 0)
			{
				GBSPModel	Model;

				Model	=modForLeaf(this);
				if(Model == null)
				{
					Map.Print("FillAreas_r:  No model for leaf.\n");
					return	false;
				}

				if(Model.mAreas[0] == Area || Model.mAreas[1] == Area)
				{
					return	true;	//Already flooded into this portal from this area
				}

				if(Model.mAreas[0] == 0)
				{
					Model.mAreas[0]	=Area;
				}
				else if(Model.mAreas[1] == 0)
				{
					Model.mAreas[1]	=Area;
				}
				else
				{
					Map.Print("*WARNING* FillAreas_r:  Area Portal touched more than 2 areas.\n");
				}
				return	true;
			}

			if(mArea != 0)		// Already set
			{
				return	true;
			}

			//Mark it
			mArea	=Area;

			//Flood through all of this leafs portals
			for(Portal=mPortals;Portal != null;Portal=Portal.mNext[Side])
			{
				Side	=(Portal.mNodes[1] == this)? 1 : 0;
				
				if(!Portal.mNodes[(Side == 0)? 1 : 0].FillAreas_r(Area, modForLeaf))
				{
					return	false;
				}
			}
			return	true;
		}


		internal bool CreateAreas_r(ref int numAreas, ModelForLeafNode modForLeaf)
		{
			if(mPlaneNum == PlanePool.PLANENUM_LEAF)
			{
				//Stop at solid
				if((mContents & Contents.BSP_CONTENTS_SOLID2) != 0)
				{
					return	true;
				}

				//Don't start at area portals
				if((mContents & Contents.BSP_CONTENTS_AREA2) != 0)
				{
					return	true;
				}

				//Already set
				if(mArea != 0)
				{
					return	true;
				}

				//Once we find a normal leaf, flood out marking the current area
				//stopping at other areas leafs, and solid leafs (unpassable leafs)
				if(!FillAreas_r(numAreas, modForLeaf))
				{
					return	false;
				}

				return	true;
			}

			if(!mChildren[0].CreateAreas_r(ref numAreas, modForLeaf))
			{
				return	false;
			}
			if(!mChildren[1].CreateAreas_r(ref numAreas, modForLeaf))
			{
				return	false;
			}
			return	true;
		}


		internal bool FinishAreaPortals_r(ModelForLeafNode modForLeaf)
		{
			GBSPModel	Model;

			if(mPlaneNum != PlanePool.PLANENUM_LEAF)
			{
				if(!mChildren[0].FinishAreaPortals_r(modForLeaf))
				{
					return	false;
				}
				if(!mChildren[1].FinishAreaPortals_r(modForLeaf))
				{
					return	false;
				}
			}

			if((mContents & Contents.BSP_CONTENTS_AREA2) == 0)
			{
				return	true;	//Only interested in area portals
			}

			if(mArea != 0)
			{
				return	true;		// Already set...
			}

			Model	=modForLeaf(this);

			if(Model == null)
			{
				Map.Print("FinishAreaPortals_r:  No model for leaf.\n");
				return	false;
			}

			//Set to first area that flooded into portal
			mArea	=Model.mAreas[0];
			Model.mbAreaPortal	=true;
			
			return	true;
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


		internal bool GetVisInfo(List<VISPortal> visPortals,
			Dictionary<Int32, VISLeaf> visLeafs,
			PlanePool pool, int numLeafClusters)
		{
			if(mPlaneNum == PlanePool.PLANENUM_LEAF || mbDetail)
			{
				if((mContents & Contents.BSP_CONTENTS_SOLID2) != 0)
				{
					return	true;
				}

				if(mPortals == null)
				{
					return	true;
				}

				Int32		Side;
				GBSPNode	[]Nodes	=new GBSPNode[2];
				for(GBSPPortal Portal=mPortals;Portal != null;Portal=Portal.mNext[Side])
				{					
					Nodes[0]	=Portal.mNodes[0];
					Nodes[1]	=Portal.mNodes[1];
			
					Side	=(Nodes[1] == this)? 1 : 0;

					if(Portal.mPoly == null)
					{
						continue;
					}

					if(!Portal.CanSeeThroughPortal())
					{
						continue;
					}
					
					if(Nodes[0].mCluster == Nodes[1].mCluster)	
					{
						Map.Print("GetVisInfo:  Portal seperating the same cluster.\n");
						return	false;
					}
					VISPortal	port	=new VISPortal();

					port.mPoly	=new GBSPPoly(Portal.mPoly);
					if(Side == 0)
					{
						port.mPoly.Reverse();
					}

					Int32	Side2	=Side;

					port.mPlane	=new GBSPPlane(port.mPoly);

					if(Vector3.Dot(pool.mPlanes[Portal.mPlaneNum].mNormal, port.mPlane.mNormal) < 0.99f)
					{
						if(Side != 0)
						{
							Side2	=0;
						}
						else
						{
							Side2	=1;
						}
					}

					if(Nodes[Side2].mCluster < 0 || Nodes[Side2].mCluster > numLeafClusters)
					{
						Map.Print("GetVisInfo: Bad leaf cluster number.\n");
						return	false;
					}

					Int32	leafFrom	=Nodes[Side2].mCluster;
					Int32	oppSide2	=(Side2 == 0)? 1 : 0;
					if(Nodes[oppSide2].mCluster < 0 || Nodes[oppSide2].mCluster > numLeafClusters)
					{
						Map.Print("GetVisInfo: Bad leaf cluster number 2.\n");
						return	false;
					}

					Int32	leafTo	=Nodes[oppSide2].mCluster;

					if(leafFrom == 0)
					{
						int	gack	=0;
						gack++;
					}

					VISLeaf		leaf	=null;
					if(visLeafs.ContainsKey(leafFrom))
					{
						leaf	=visLeafs[leafFrom];
					}
					else
					{
						leaf	=new VISLeaf();
						visLeafs.Add(leafFrom, leaf);
					}
					port.mLeaf		=leafTo;
					port.mNext		=leaf.mPortals;
					leaf.mPortals	=port;

					port.CalcPortalInfo();

					visPortals.Add(port);
				}

				return	true;
			}

			if(mPortals != null)
			{
				Map.Print("*WARNING* SavePortalFile_r:  Node with portal.\n");
			}

			if(!mChildren[0].GetVisInfo(visPortals, visLeafs, pool, numLeafClusters))
			{
				return	false;
			}
			if(!mChildren[1].GetVisInfo(visPortals, visLeafs, pool, numLeafClusters))
			{
				return	false;
			}
			return	true;
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


		internal bool PrepPortalFile_r(ref int numPortalLeafs, ref int numPortals)
		{
			GBSPNode	[]Nodes	=new GBSPNode[2];
			GBSPPortal	Portal;
			Int32		Side;

			//Stop at leafs, and detail nodes (stop at beginning of clusters)
			if(mPlaneNum == PlanePool.PLANENUM_LEAF || mbDetail)
			{
				if((mContents & Contents.BSP_CONTENTS_SOLID2) != 0)
				{
					return	true;
				}

				//Give this portal it's leaf number...
				mPortalLeafNum	=numPortalLeafs;
				numPortalLeafs++;

				if(mPortals == null)
				{
					Map.Print("*WARNING* PrepPortalFile_r:  Leaf without any portals.\n");
					return	true;
				}

				//Save out all the portals that belong to this leaf...
				for(Portal=mPortals;Portal != null;Portal=Portal.mNext[Side])
				{
					Nodes[0]	=Portal.mNodes[0];
					Nodes[1]	=Portal.mNodes[1];
			
					Side	=(Nodes[1] == this)? 1 : 0;

					if(Portal.mPoly == null)
					{
						Map.Print("*WARNING*  SavePortalFile_r:  Portal with NULL poly.\n");
						continue;
					}

					if(!Portal.CanSeeThroughPortal())
					{
						continue;
					}
					
					if(Nodes[0].mCluster == Nodes[1].mCluster)	
					{
						Map.Print("PrepPortalFile_r:  Portal seperating the same cluster.\n");
						return	false;
					}
					
					//This portal is good...
					numPortals++;
				}

				return	true;
			}

			mPortalLeafNum = -1;

			if(mPortals != null)
			{
				Map.Print("*WARNING* PrepPortalFile_r:  Node with portal.\n");
			}

			if(!mChildren[0].PrepPortalFile_r(ref numPortalLeafs, ref numPortals))
			{
				return	false;
			}
			if(!mChildren[1].PrepPortalFile_r(ref numPortalLeafs, ref numPortals))
			{
				return	false;
			}
			return	true;
		}


		internal bool SavePortalFile_r(BinaryWriter bw, PlanePool pool, int numLeafClusters)
		{
			GBSPNode	[]Nodes	=new GBSPNode[2];
			GBSPPortal	Portal;
			Int32		Side, Side2, Cluster;
			GBSPPlane	Plane;
			GBSPPoly	Poly;

			if(mPlaneNum == PlanePool.PLANENUM_LEAF || mbDetail)
			{
				//Can't see from solid
				if((mContents & Contents.BSP_CONTENTS_SOLID2) != 0)
				{
					return	true;
				}
				
				if(mPortals == null)
				{
					return	true;
				}

				for(Portal=mPortals;Portal!=null;Portal=Portal.mNext[Side])
				{
					Nodes[0]	=Portal.mNodes[0];
					Nodes[1]	=Portal.mNodes[1];

					Side	=(Nodes[1] == this)? 1 : 0;

					if(Portal.mPoly == null)
					{
						continue;
					}

					if(!Portal.CanSeeThroughPortal())
					{
						continue;
					}

					if(Nodes[0].mCluster == Nodes[1].mCluster)	
					{
						Map.Print("PrepPortalFile_r:  Portal seperating the same cluster.\n");
						return	false;
					}

					Poly	=Portal.mPoly;

					if(Poly.VertCount() < 3)
					{
						Map.Print("SavePortalFile_r:  Portal poly verts < 3.\n");
						return	false;
					}

					bw.Write(Poly.VertCount());

					if(Side == 0)
					{
						//If on front side, reverse so it points to the other leaf
						Poly.WriteReverse(bw);
					}					
					else
					{
						//It's allready pointing to the other leaf
						Poly.Write(bw);
					}

					Side2	=Side;

					Plane	=new GBSPPlane(Poly);
					if(Vector3.Dot(pool.mPlanes[Portal.mPlaneNum].mNormal, Plane.mNormal) < 0.99f)
					{
						Side2	=(Side2 == 0)? 1 : 0;
					}

					if(Nodes[Side2].mCluster < 0
						|| Nodes[Side2].mCluster > numLeafClusters)
					{
						Map.Print("SavePortalFile_r:  Bad Leaf Cluster Number.\n");
						return	false;
					}

					Cluster	=Nodes[Side2].mCluster;
					bw.Write(Cluster);

					int	Side2Opposite	=(Side2 == 0)? 1 : 0;
						
					if (Nodes[Side2Opposite].mCluster < 0
						|| Nodes[Side2Opposite].mCluster > numLeafClusters)
					{
						Map.Print("SavePortalFile_r:  Bad Leaf Cluster Number.\n");
						return	false;
					}

					Cluster	=Nodes[Side2Opposite].mCluster;
					bw.Write(Cluster);
				}
				return	true;
			}

			if(mPortals != null)
			{
				Map.Print("*WARNING* SavePortalFile_r:  Node with portal.\n");
			}

			if(!mChildren[0].SavePortalFile_r(bw, pool, numLeafClusters))
			{
				return	false;
			}
			if(!mChildren[1].SavePortalFile_r(bw, pool, numLeafClusters))
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
			Int32	CurrentNode;

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
				
			CurrentNode	=nc.mNumGFXNodes;

			PrepGFXNode(nc);

			nc.mNumGFXNodes++;

			mChildrenID[0]	=mChildren[0].PrepGFXNodes_r(mChildrenID[0], nc);
			mChildrenID[1]	=mChildren[1].PrepGFXNodes_r(mChildrenID[1], nc);

			return CurrentNode;
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
			Int32	j;
			UInt32	MajorContents;

			if(con == 0)
			{
				return 0;
			}

			//Only check visible contents
			con	&=Contents.BSP_VISIBLE_CONTENTS;
			
			//Return the strongest one, return the first lsb
			for(j=0;j < 32;j++)
			{
				MajorContents	=(con & (UInt32)(1<<j));

				if(MajorContents != 0)
				{
					return	MajorContents;
				}
			}

			return 0;
		}


		static internal bool WindowCheck(GBSPNode front, GBSPNode back)
		{
			return	(((front.mContents & Contents.BSP_CONTENTS_WINDOW2) != 0)
						&& VisibleContents(back.mContents
						^ front.mContents) == Contents.BSP_CONTENTS_WINDOW2);
		}


		internal GBSPSide GetBestPortalSide(GBSPNode front, GBSPNode back, PlanePool pool)
		{
			UInt32	visContents, majorContents;

			//First, check to see if the contents are intersecting sheets (special case)
			if(((front.mContents & Contents.BSP_CONTENTS_SHEET) != 0)
				&& ((back.mContents & Contents.BSP_CONTENTS_SHEET) != 0))
			{
				//The contents are intersecting sheets, so or them together
				visContents	=front.mContents | back.mContents;
			}
			else
			{
				//Make sure the contents on both sides are not the same
				visContents	=front.mContents ^ back.mContents;
			}

			//There must be a visible contents on at least one side of the portal...
			majorContents	=VisibleContents(visContents);

			if(majorContents == 0)
			{
				return	null;
			}

			GBSPSide	bestSide	=null;
			float		bestDot		=0.0f;
			GBSPPlane	p1			=pool.mPlanes[mPlaneNum];

			List<GBSPSide>	ogSides	=new List<GBSPSide>();

			GBSPBrush.GetOriginalSidesByContents(front.mBrushList, majorContents, ogSides);
			GBSPBrush.GetOriginalSidesByContents(back.mBrushList, majorContents, ogSides);

			foreach(GBSPSide side in ogSides)
			{
				if((side.mFlags & GBSPSide.SIDE_NODE) != 0)
				{
					continue;		// Side not visible (result of a csg'd topbrush)
				}

				//First, Try an exact match
				if(side.mPlaneNum == mPlaneNum)
				{
					return	side;
				}

				//In the mean time, try for the closest match
				GBSPPlane	p2	=pool.mPlanes[side.mPlaneNum];
				float	dot	=Vector3.Dot(p1.mNormal, p2.mNormal);
				if(dot > bestDot)
				{
					bestDot		=dot;
					bestSide	=side;
				}
			}
			if(bestSide == null)
			{
				Map.Print("WARNING: Could not map portal to original brush...\n");
			}
			return	bestSide;
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
