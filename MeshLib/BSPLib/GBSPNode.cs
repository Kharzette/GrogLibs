using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
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

		public const float	SUBDIVIDESIZE		=235.0f;
		public const int	MAX_TEMP_LEAF_SIDES	=100;
		public const int	MAX_LEAF_SIDES		=64000 * 2;
		public const int	MAX_AREAS			=256;
		public const int	MAX_AREA_PORTALS	=1024;


		internal void BuildBSP(GBSPGlobals gbs, GBSPBrush brushList, PlanePool pool)
		{
			Int32		NumVisFaces, NumNonVisFaces;
			Int32		NumVisBrushes;
			float		Volume;
			Bounds		bounds	=new Bounds();

			if(gbs.Verbose)
			{
				Map.Print("--- Build BSP Tree ---\n");
			}

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
						
			gbs.NumVisNodes		=0;
			gbs.NumNonVisNodes	=0;

//			FileStream		fs	=new FileStream("PlaneChoice.txt", FileMode.CreateNew);
//			StreamWriter	sw	=new StreamWriter(fs);

//			int	cnt	=0;

			BuildTree_r(gbs, brushList, pool);

//			sw.Close();
//			fs.Close();
			
			//Top node is always valid, this way portals can use top node to get box of entire bsp...
			mBounds	=new Bounds(bounds);

			gbs.TreeMins	=bounds.mMins;
			gbs.TreeMaxs	=bounds.mMaxs;

			if(gbs.Verbose)
			{
				Map.Print("Total Nodes            : " + (gbs.NumVisNodes/2 - gbs.NumNonVisNodes) + "\n");
				Map.Print("Nodes Removed          : " + gbs.NumNonVisNodes + "\n");
				Map.Print("Total Leafs            : " + (gbs.NumVisNodes+1)/2 + "\n");
			}
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


		void BuildTree_r(GBSPGlobals gbs, GBSPBrush brushes, PlanePool pool)
		{
			GBSPSide	BestSide;

			GBSPBrush	childrenFront;
			GBSPBrush	childrenBack;

			gbs.NumVisNodes++;

			//find the best plane to use as a splitter
			BestSide	=GBSPBrush.SelectSplitSide(gbs, brushes, this, pool);
			
			if(BestSide == null)
			{
				//leaf node
				mSide		=null;
				mPlaneNum	=PlanePool.PLANENUM_LEAF;
				LeafNode(brushes);
				return;
			}
/*			else
			{
				if(cnt >= 20)
				{
					sw.Write("" + BestSide.mPlaneNum + ",\n");
					cnt	=0;
				}
				else
				{
					sw.Write("" + BestSide.mPlaneNum + ", ");
				}
				cnt++;
			}*/

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
			mChildren[0].BuildTree_r(gbs, childrenFront, pool);
			mChildren[1].BuildTree_r(gbs, childrenBack, pool);
		}


		internal bool CreatePortals(GBSPGlobals gbs, GBSPModel model, bool bVis, PlanePool pool)
		{
			if(gbs.Verbose)
			{
				Map.Print(" --- Create Portals --- \n");
			}

			gbs.bVisPortals	=bVis;
			gbs.OutsideNode	=model.mOutsideNode;

			gbs.NodeMins	=model.mBounds.mMins;
			gbs.NodeMaxs	=model.mBounds.mMaxs;

			if(!CreateAllOutsidePortals(gbs, pool))
			{
				Map.Print("CreatePortals:  Could not create bbox portals.\n");
				return	false;
			}

			if(!PartitionPortals_r(gbs, pool))
			{
				Map.Print("CreatePortals:  Could not partition portals.\n");
				return	false;
			}
			return	true;
		}


		bool CreateAllOutsidePortals(GBSPGlobals gbs, PlanePool pool)
		{
			GBSPPlane	[]PPlanes	=new GBSPPlane[6];
			GBSPPortal	[]Portals	=new GBSPPortal[6];

			//clear outside node
			gbs.OutsideNode.mArea			=0;
			gbs.OutsideNode.mBounds.mMins	=Vector3.Zero;
			gbs.OutsideNode.mBounds.mMaxs	=Vector3.Zero;
			gbs.OutsideNode.mBrushList		=null;
			gbs.OutsideNode.mChildren[0]	=null;
			gbs.OutsideNode.mChildren[1]	=null;
			gbs.OutsideNode.mChildrenID[0]	=0;
			gbs.OutsideNode.mChildrenID[1]	=0;
			gbs.OutsideNode.mCluster		=0;
			gbs.OutsideNode.mCurrentFill	=0;
			gbs.OutsideNode.mDetail			=false;
			gbs.OutsideNode.mEntity			=0;
			gbs.OutsideNode.mFaces			=null;
			gbs.OutsideNode.mFirstFace		=0;
			gbs.OutsideNode.mFirstPortal	=0;
			gbs.OutsideNode.mFirstSide		=0;
			gbs.OutsideNode.mLeafFaces		=null;
			gbs.OutsideNode.mNumFaces		=0;
			gbs.OutsideNode.mNumLeafFaces	=0;
			gbs.OutsideNode.mNumPortals		=0;
			gbs.OutsideNode.mNumSides		=0;
			gbs.OutsideNode.mOccupied		=0;
			gbs.OutsideNode.mParent			=null;
			gbs.OutsideNode.mPlaneSide		=0;
			gbs.OutsideNode.mPortalLeafNum	=0;
			gbs.OutsideNode.mPortals		=null;
			gbs.OutsideNode.mSide			=null;
			gbs.OutsideNode.mVolume			=null;

			gbs.OutsideNode.mPlaneNum	=PlanePool.PLANENUM_LEAF;
			gbs.OutsideNode.mContents	=GBSPBrush.BSP_CONTENTS_SOLID2;

			//So there won't be NULL volume leafs when we create the outside portals
			for(int k=0;k < 3;k++)
			{
				if(UtilityLib.Mathery.VecIdx(gbs.NodeMins, k) - 128.0f
					<= -Brush.MIN_MAX_BOUNDS ||
					UtilityLib.Mathery.VecIdx(gbs.NodeMaxs, k) + 128.0f
					>= Brush.MIN_MAX_BOUNDS)
				{
					Map.Print("CreateAllOutsidePortals:  World BOX out of range...\n");
					return	false;
				}
			}

			gbs.NodeMins	-=(Vector3.One * 128.0f);
			gbs.NodeMaxs	+=(Vector3.One * 128.0f);

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
						PPlanes[Index].mDist	=UtilityLib.Mathery.VecIdx(gbs.NodeMins, i);
					}
					else
					{
						UtilityLib.Mathery.VecIdxAssign(ref PPlanes[Index].mNormal, i, -1.0f);
						PPlanes[Index].mDist	=-UtilityLib.Mathery.VecIdx(gbs.NodeMaxs, i);
					}
					
					Portals[Index]	=GBSPPortal.CreateOutsidePortal(gbs, PPlanes[Index], this, pool);

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


		bool PartitionPortals_r(GBSPGlobals gbs, PlanePool pool)
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
			if(gbs.bVisPortals && mDetail)
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
			
			if(!Front.PartitionPortals_r(gbs, pool))
			{
				return	false;
			}
			if(!Back.PartitionPortals_r(gbs, pool))
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


		bool FillLeafs_r(GBSPGlobals gbs, bool Fill, Int32 Dist)
		{
			GBSPPortal		Portal;
			Int32			Side;
			
			//if (HitEntity)
			//	return GE_TRUE;
			
			if((mContents & GBSPBrush.BSP_CONTENTS_SOLID2) != 0)
			{
				return	true;
			}

			if(mCurrentFill == gbs.CurrentFill)
			{
				return	true;
			}

			mCurrentFill	=gbs.CurrentFill;
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
					gbs.HitEntity	=true;
					gbs.EntityHit	=mEntity;
					gbs.HitNode		=this;
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

				if(!Portal.mNodes[(Side == 0)? 1 : 0].FillLeafs_r(gbs, Fill, Dist+1))
				{
					return	false;
				}
			}

			return	true;
		}


		bool FillLeafs2_r(GBSPGlobals gbs)
		{
			GBSPPortal	Portal;
			Int32		Side;
			
			if((mContents & GBSPBrush.BSP_CONTENTS_SOLID2) != 0)
			{
				return	true;
			}

			if(mCurrentFill == gbs.CurrentFill)
			{
				return	true;
			}

			mCurrentFill	=gbs.CurrentFill;

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
				if(!Portal.mNodes[(Side==0)? 1 : 0].FillLeafs2_r(gbs))
				{
					return	false;
				}
			}

			return	true;
		}


		bool FillFromEntities(GBSPGlobals gbs, List<MapEntity> ents, PlanePool pool)
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
				
				if(!Node.FillLeafs2_r(gbs))
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


		void FillUnTouchedLeafs_r(GBSPGlobals gg)
		{
			if(mPlaneNum != PlanePool.PLANENUM_LEAF)
			{
				mChildren[0].FillUnTouchedLeafs_r(gg);
				mChildren[1].FillUnTouchedLeafs_r(gg);
				return;
			}

			if((mContents & GBSPBrush.BSP_CONTENTS_SOLID2) != 0)
			{
				return;		//allready solid or removed...
			}

			if(mCurrentFill != gg.CurrentFill)
			{
				//Fill er in with solid so it does not show up...(Preserve user contents)
				mContents	&=(0xffff0000);
				mContents	|=GBSPBrush.BSP_CONTENTS_SOLID2;
				gg.NumRemovedLeafs++;
			}
		}


		internal int RemoveHiddenLeafs(GBSPGlobals gbs, GBSPNode oNode,
			List<MapEntity> ents, PlanePool pool)
		{
			Int32	Side;

			Map.Print(" --- Remove Hidden Leafs --- \n");

			gbs.OutsideNode	=oNode;

			Side	=(gbs.OutsideNode.mPortals.mNodes[0] == gbs.OutsideNode)? 1 : 0;

			gbs.NumRemovedLeafs	=0;

			if(!PlaceEntities(ents, pool))
			{
				return	-1;
			}

			gbs.HitEntity	=false;
			gbs.HitNode		=null;
			gbs.CurrentFill	=1;

			if(!gbs.OutsideNode.mPortals.mNodes[Side].FillLeafs_r(gbs, false, 1))
			{
				return -1;
			}

			if(gbs.HitEntity)
			{
				Map.Print("*****************************************\n");
				Map.Print("*           *** LEAK ***                *\n");
				Map.Print("* Level is NOT sealed.                  *\n");
				Map.Print("* Optimal removal will not be performed.*\n");
				Map.Print("*****************************************\n");

//				WriteLeakFile("Test", HitNode, ONode);
				return	-1;
			}

			gbs.CurrentFill	=2;
			
			if(!FillFromEntities(gbs, ents, pool))
			{
				return	-1;
			}
			
			FillUnTouchedLeafs_r(gbs);

			if(gbs.Verbose)
			{
				Map.Print("Removed Leafs          : " + gbs.NumRemovedLeafs + "\n");
			}

			return	gbs.NumRemovedLeafs;
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


		internal void MarkVisibleSides(GBSPGlobals gg, List<MapBrush> list, PlanePool pool)
		{
			if(gg.Verbose)
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

			mChildren[0]	=null;
			mChildren[1]	=null;
		}

		internal void MakeFaces(GBSPGlobals gg, PlanePool pool, TexInfoPool tip)
		{
			if(gg.Verbose)
			{
				Map.Print("--- Finalize Faces ---\n");
			}
			
			gg.NumMerged		=0;
			gg.NumSubdivided	=0;
			gg.NumMakeFaces		=0;

			MakeFaces_r(gg, pool, tip);

			Map.Print("TotalFaces             : " + gg.NumMakeFaces + "\n");
			Map.Print("Merged Faces           : " + gg.NumMerged + "\n");
			Map.Print("Subdivided Faces       : " + gg.NumSubdivided + "\n");
			Map.Print("FinalFaces             : " + ((gg.NumMakeFaces - gg.NumMerged) + gg.NumSubdivided) + "\n");
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


		void MakeFaces_r(GBSPGlobals gg, PlanePool pool, TexInfoPool tip)
		{
			GBSPPortal	p;
			Int32		s;

			//Recurse down to leafs
			if(mPlaneNum != PlanePool.PLANENUM_LEAF)
			{
				mChildren[0].MakeFaces_r(gg, pool, tip);
				mChildren[1].MakeFaces_r(gg, pool, tip);
				
				//Marge list (keepin that typo, funny)
				GBSPFace.MergeFaceList2(gg, mFaces, pool);

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

					gg.NumMakeFaces++;
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


		void MergeNodes_r(GBSPGlobals gg)
		{
			GBSPBrush	b, Next;

			if(mPlaneNum == PlanePool.PLANENUM_LEAF)
			{
				return;
			}

			mChildren[0].MergeNodes_r(gg);
			mChildren[1].MergeNodes_r(gg);

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
						gg.MergedNodes++;
					}
				}
			}
		}


		internal void MergeNodes(GBSPGlobals gg)
		{
			if(gg.Verbose)
			{
				Map.Print("--- Merge Nodes ---\n");
			}

			gg.MergedNodes	=0;
			
			MergeNodes_r(gg);

			if(gg.Verbose)
			{
				Map.Print("Num Merged             : " + gg.MergedNodes + "\n");
			}
		}


		internal bool CreateLeafClusters(GBSPGlobals gg)
		{
			Map.Print(" --- CreateLeafClusters --- \n");

			if(!CreateLeafClusters_r(gg))
			{
				Map.Print("CreateLeafClusters:  Failed to find leaf clusters.\n");
				return	false;
			}

			if(gg.Verbose)
			{
				Map.Print("Num Clusters       : " + gg.NumLeafClusters + "\n");
			}
			return	true;
		}


		bool CreateLeafClusters_r(GBSPGlobals gg)
		{
			if(mPlaneNum != PlanePool.PLANENUM_LEAF && !mDetail)
			{
				mChildren[0].CreateLeafClusters_r(gg);
				mChildren[1].CreateLeafClusters_r(gg);
				return	true;
			}
			
			//Either a leaf or detail node
			if((mContents & GBSPBrush.BSP_CONTENTS_SOLID2) != 0)
			{
				mCluster	=-1;
				return	true;
			}
			
			FillLeafClusters_r(gg.NumLeafClusters);

			gg.NumLeafClusters++;

			return	true;
		}


		void FillLeafClusters_r(Int32 cluster)
		{
			if(mPlaneNum == PlanePool.PLANENUM_LEAF)
			{
				if((mContents & GBSPBrush.BSP_CONTENTS_SOLID2) != 0)
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


		internal bool CreateLeafSides(GBSPGlobals gg, PlanePool pool)
		{
			if(gg.Verbose)
			{
				Map.Print(" --- Create Leaf Sides --- \n");
			}
			
			if(!CreateLeafSides_r(gg, pool))
			{
				return	false;
			}

			if(gg.Verbose)
			{
				Map.Print("Num Leaf Sides       : " + gg.NumLeafSides);
				Map.Print("Num Leaf Bevels      : " + gg.NumLeafBevels);
			}
			return	true;
		}


		bool CreateLeafSides_r(GBSPGlobals gg, PlanePool pool)
		{
			GBSPPortal	Portal, Next;
			Int32		Side, i;

			mFirstSide	=-1;
			mNumSides	=0;

			//At a leaf, convert portals to leaf sides...
			if(mPlaneNum == PlanePool.PLANENUM_LEAF)
			{
				//Don't convert empty leafs
				if((mContents & GBSPBrush.BSP_CONTENTS_SOLID_CLIP) == 0)
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
				gg.CNumLeafSides	=0;

				for(Portal=mPortals;Portal != null;Portal=Next)
				{
					Side	=(Portal.mNodes[0] == this)? 1 : 0;
					Next	=Portal.mNext[(Side == 0)? 1 : 0];

					for(i=0;i < gg.CNumLeafSides;i++)
					{
						if(gg.LPlaneNumbers[i] == Portal.mPlaneNum
							&& gg.LPlaneSides[i] == Side)
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

					if(i >= gg.CNumLeafSides)
					{
						gg.LPlaneNumbers[i]	=Portal.mPlaneNum;
						gg.LPlaneSides[i]	=Side;
						gg.CNumLeafSides++;
					}
					
				}
				
				if(!FinishLeafSides(gg, pool))
				{
					return	false;
				}

				return	true;
			}

			if(!mChildren[0].CreateLeafSides(gg, pool))
			{
				return	false;
			}
			if(!mChildren[1].CreateLeafSides(gg, pool))
			{
				return	false;
			}

			return	true;
		}


		bool FinishLeafSides(GBSPGlobals gg, PlanePool pool)
		{
			GBSPPlane	Plane	=new GBSPPlane();
			Int32		Axis, i, Dir;
			Bounds		bnd;

			if(!GetLeafBBoxFromPortals(out bnd))
			{
				Map.Print("FinishLeafSides:  Could not get leaf portal BBox.\n");
				return	false;
			}
			
			if(gg.CNumLeafSides < 4)
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
						for(i=0;i < gg.CNumLeafSides;i++)
						{
							Plane	=pool.mPlanes[gg.LPlaneNumbers[i]];
								
							if(gg.LPlaneSides[i] != 0)
							{
								Plane.Inverse();
							}
							if(UtilityLib.Mathery.VecIdx(Plane.mNormal, Axis) == Dir)
							{
								break;
							}
						}
						if(i >= gg.CNumLeafSides)
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
							gg.LPlaneNumbers[i]	=pool.FindPlane(Plane, out side);
							gg.LPlaneSides[i]	=side;
							
							if(gg.LPlaneNumbers[i] == -1)
							{
								Map.Print("FinishLeafSides:  Could not create the plane.\n");
								return	false;
							}

							gg.CNumLeafSides++;
							
							gg.NumLeafBevels++;
						}
					}
				}
			}
			mFirstSide	=gg.NumLeafSides;
			mNumSides	=gg.CNumLeafSides;
			
			for(i=0;i < gg.CNumLeafSides;i++)
			{
				if(gg.NumLeafSides >= MAX_LEAF_SIDES)
				{
					Map.Print("FinishLeafSides:  Max Leaf Sides.\n");
					return	false;
				}
				gg.LeafSides[gg.NumLeafSides].mPlaneNum		=gg.LPlaneNumbers[i];
				gg.LeafSides[gg.NumLeafSides].mPlaneSide	=gg.LPlaneSides[i];
				gg.NumLeafSides++;
			}
			return	true;
		}


		bool GetLeafBBoxFromPortals(out Bounds bnd)
		{
			GBSPPoly	poly;
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

				poly	=port.mPoly;

				foreach(Vector3 vert in poly.mVerts)
				{
					for(int k=0;k < 3;k++)
					{
						if(UtilityLib.Mathery.VecIdx(vert, k)
							< UtilityLib.Mathery.VecIdx(bnd.mMins, k))
						{
							UtilityLib.Mathery.VecIdxAssign(ref bnd.mMins, k,
								UtilityLib.Mathery.VecIdx(vert, k));
						}
						if(UtilityLib.Mathery.VecIdx(vert, k)
							> UtilityLib.Mathery.VecIdx(bnd.mMaxs, k))
						{
							UtilityLib.Mathery.VecIdxAssign(ref bnd.mMaxs, k,
								UtilityLib.Mathery.VecIdx(vert, k));
						}
					}
				}
			}
			return	true;
		}


		internal bool CreateAreas(Map map)
		{
			Map.Print(" --- Create Area Leafs --- \n");

			//Clear all model area info
			foreach(GBSPModel mod in map.mModels)
			{
				mod.mAreas[0]		=mod.mAreas[1]	=0;
				mod.mbAreaPortal	=false;
			}

			map.mGlobals.GFXAreas		=new GFXArea[MAX_AREAS];
			map.mGlobals.GFXAreaPortals	=new GFXAreaPortal[MAX_AREA_PORTALS];

			//create actual objects
			for(int i=0;i < MAX_AREAS;i++)
			{
				map.mGlobals.GFXAreas[i]	=new GFXArea();
			}
			for(int i=0;i < MAX_AREA_PORTALS;i++)
			{
				map.mGlobals.GFXAreaPortals[i]	=new GFXAreaPortal();
			}

			map.mGlobals.NumGFXAreas		=1;	//0 invalid
			map.mGlobals.NumGFXAreaPortals	=0;

			if(!CreateAreas_r(map))
			{
				Map.Print("Could not create model areas.\n");
				return	false;
			}

			if(!FinishAreaPortals_r(map))
			{
				Map.Print("CreateAreas: FinishAreaPortals_r failed.\n");
				return	false;
			}

			if(!map.FinishAreas())
			{
				Map.Print("Could not finalize model areas.\n");
				return	false;
			}

			return	true;
		}


		bool FillAreas_r(Int32 Area, Map map)
		{
			GBSPPortal	Portal;
			Int32		Side;

			if ((mContents & GBSPBrush.BSP_CONTENTS_SOLID2) != 0)
			{
				return	true;	//Stop at solid leafs
			}

			if((mContents & GBSPBrush.BSP_CONTENTS_AREA2) != 0)
			{
				GBSPModel	Model;

				Model	=map.ModelForLeafNode(this);
				
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
				
				if(!Portal.mNodes[(Side == 0)? 1 : 0].FillAreas_r(Area, map))
				{
					return	false;
				}
			}
			return	true;
		}


		bool CreateAreas_r(Map map)
		{
			if(mPlaneNum == PlanePool.PLANENUM_LEAF)
			{
				//Stop at solid
				if((mContents & GBSPBrush.BSP_CONTENTS_SOLID2) != 0)
				{
					return	true;
				}

				//Don't start at area portals
				if((mContents & GBSPBrush.BSP_CONTENTS_AREA2) != 0)
				{
					return	true;
				}

				//Already set
				if(mArea != 0)
				{
					return	true;
				}

				if(map.mGlobals.NumGFXAreas >= MAX_AREAS)
				{
					Map.Print("CreateAreas_r:  Max Areas.\n");
					return	false;
				}

				//Once we find a normal leaf, flood out marking the current area
				//stopping at other areas leafs, and solid leafs (unpassable leafs)
				if(!FillAreas_r(map.mGlobals.NumGFXAreas++, map))
				{
					return	false;
				}

				return	true;
			}

			if(!mChildren[0].CreateAreas_r(map))
			{
				return	false;
			}
			if(!mChildren[1].CreateAreas_r(map))
			{
				return	false;
			}
			return	true;
		}


		bool FinishAreaPortals_r(Map map)
		{
			GBSPModel	Model;

			if(mPlaneNum != PlanePool.PLANENUM_LEAF)
			{
				if(!mChildren[0].FinishAreaPortals_r(map))
				{
					return	false;
				}
				if(!mChildren[1].FinishAreaPortals_r(map))
				{
					return	false;
				}
			}

			if((mContents & GBSPBrush.BSP_CONTENTS_AREA2) == 0)
			{
				return	true;	//Only interested in area portals
			}

			if(mArea != 0)
			{
				return	true;		// Already set...
			}

			Model	=map.ModelForLeafNode(this);

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
			UInt32	c1, c2, Contents;

			// Stop at leafs, and start returning contents
			if(mPlaneNum == PlanePool.PLANENUM_LEAF)
			{
				return	mContents;
			}

			c1	=mChildren[0].ClusterContents();
			c2	=mChildren[1].ClusterContents();

			Contents	=(c1 | c2);	//Or together children, and return

			if(((c1 & GBSPBrush.BSP_CONTENTS_SOLID2) == 0)
				|| ((c2 & GBSPBrush.BSP_CONTENTS_SOLID2) == 0))
			{
				Contents	&=~GBSPBrush.BSP_CONTENTS_SOLID2;
			}
			return	(Contents);
		}


		internal bool GetVisInfo(List<VISPortal> visPortals,
			Dictionary<Int32, VISLeaf> visLeafs,
			PlanePool pool, int numLeafClusters)
		{
			if(mPlaneNum == PlanePool.PLANENUM_LEAF || mDetail)
			{
				if((mContents & GBSPBrush.BSP_CONTENTS_SOLID2) != 0)
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


		internal bool GetFaceVertIndexNumbers_r(GBSPGlobals gg)
		{
			GBSPFace	Face, Next;

			if(mPlaneNum == PlanePool.PLANENUM_LEAF)
			{
				return	true;
			}

			for(Face = mFaces;Face != null;Face = Next)
			{
				Next	=Face.mNext;

				if(Face.mMerged != null
					|| Face.mSplit[0] != null
					|| Face.mSplit[1] != null)
				{
					continue;
				}

				if(!Face.GetFaceVertIndexNumbers(gg))
				{
					return	false;
				}
			}

			if(!mChildren[0].GetFaceVertIndexNumbers_r(gg))
			{
				return	false;
			}
			if(!mChildren[1].GetFaceVertIndexNumbers_r(gg))
			{
				return	false;
			}
			return	true;
		}


		internal bool PrepPortalFile_r(GBSPGlobals gg)
		{
			GBSPNode	[]Nodes	=new GBSPNode[2];
			GBSPPortal	Portal;
			Int32		Side;

			//Stop at leafs, and detail nodes (stop at beginning of clusters)
			if(mPlaneNum == PlanePool.PLANENUM_LEAF || mDetail)
			{
				if((mContents & GBSPBrush.BSP_CONTENTS_SOLID2) != 0)
				{
					return	true;
				}

				//Give this portal it's leaf number...
				mPortalLeafNum	=gg.NumPortalLeafs;
				gg.NumPortalLeafs++;

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
					gg.NumPortals++;
				}

				return	true;
			}

			mPortalLeafNum = -1;

			if(mPortals != null)
			{
				Map.Print("*WARNING* PrepPortalFile_r:  Node with portal.\n");
			}

			if(!mChildren[0].PrepPortalFile_r(gg))
			{
				return	false;
			}
			if(!mChildren[1].PrepPortalFile_r(gg))
			{
				return	false;
			}
			return	true;
		}


		internal bool SavePortalFile_r(GBSPGlobals gg, BinaryWriter bw, PlanePool pool)
		{
			GBSPNode	[]Nodes	=new GBSPNode[2];
			GBSPPortal	Portal;
			Int32		i, Side, Side2, Cluster;
			GBSPPlane	Plane;
			GBSPPoly	Poly;

			if(mPlaneNum == PlanePool.PLANENUM_LEAF || mDetail)
			{
				//Can't see from solid
				if((mContents & GBSPBrush.BSP_CONTENTS_SOLID2) != 0)
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

					if(Poly.mVerts.Count < 3)
					{
						Map.Print("SavePortalFile_r:  Portal poly verts < 3.\n");
						return	false;
					}

					bw.Write(Poly.mVerts.Count);

					if(Side == 0)
					{
						//If on front side, reverse so it points to the other leaf
						for(i=Poly.mVerts.Count - 1;i >=0;i--)
						{
							bw.Write(Poly.mVerts[i].X);
							bw.Write(Poly.mVerts[i].Y);
							bw.Write(Poly.mVerts[i].Z);
						}
					}					
					else
					{
						//It's allready pointing to the other leaf
						foreach(Vector3 vert in Poly.mVerts)
						{
							bw.Write(vert.X);
							bw.Write(vert.Y);
							bw.Write(vert.Z);
						}
					}

					Side2	=Side;

					Plane	=new GBSPPlane(Poly);
					if(Vector3.Dot(pool.mPlanes[Portal.mPlaneNum].mNormal, Plane.mNormal) < 0.99f)
					{
						Side2	=(Side2 == 0)? 1 : 0;
					}

					if(Nodes[Side2].mCluster < 0
						|| Nodes[Side2].mCluster > gg.NumLeafClusters)
					{
						Map.Print("SavePortalFile_r:  Bad Leaf Cluster Number.\n");
						return	false;
					}

					Cluster	=Nodes[Side2].mCluster;
					bw.Write(Cluster);

					int	Side2Opposite	=(Side2 == 0)? 1 : 0;
						
					if (Nodes[Side2Opposite].mCluster < 0
						|| Nodes[Side2Opposite].mCluster > gg.NumLeafClusters)
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

			if(!mChildren[0].SavePortalFile_r(gg, bw, pool))
			{
				return	false;
			}
			if(!mChildren[1].SavePortalFile_r(gg, bw, pool))
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


		internal bool FixTJunctions_r(GBSPGlobals gg, TexInfoPool tip)
		{
			GBSPFace	Face, Next;

			if(mPlaneNum == PlanePool.PLANENUM_LEAF)
			{
				return	true;
			}

			for(Face = mFaces;Face != null;Face = Next)
			{
				Next	=Face.mNext;

				if(Face.mMerged != null ||
					Face.mSplit[0] != null ||
					Face.mSplit[1] != null)
				{
					continue;
				}

				Face.FixTJunctions(gg, this, tip);
			}
			
			if(!mChildren[0].FixTJunctions_r(gg, tip))
			{
				return	false;
			}
			if(!mChildren[1].FixTJunctions_r(gg, tip))
			{
				return	false;
			}
			return	true;
		}

		internal int PrepGFXNodes_r(GBSPGlobals gg, Int32 Original)
		{
			Int32	CurrentNode;

			//Prep the leaf and it's portals
			if(mPlaneNum == PlanePool.PLANENUM_LEAF)
			{
				if((mContents & GBSPBrush.BSP_CONTENTS_SOLID2) != 0)
				{
					gg.NumSolidLeafs++;	// Remember how many solid leafs there are
				}

				mNumPortals		=0;
				mFirstPortal	=-1;

				// To be able to save out portal LeafTo's
				mPortalLeafNum	=gg.NumGFXLeafs;

				//Count num gfx leaf faces here, so we know how big to make the array
				//later, when they are saved out...
				gg.NumGFXLeafFaces	+=mNumLeafFaces;

				//Increase the number of leafs
				gg.NumGFXLeafs++;

				return -(gg.NumGFXLeafs);
			}
				
			CurrentNode	=gg.NumGFXNodes;

			PrepGFXNode(gg);

			gg.NumGFXNodes++;

			mChildrenID[0]	=mChildren[0].PrepGFXNodes_r(gg, mChildrenID[0]);
			mChildrenID[1]	=mChildren[1].PrepGFXNodes_r(gg, mChildrenID[1]);

			return CurrentNode;
		}


		void PrepGFXNode(GBSPGlobals gg)
		{
			GBSPFace	Face;
			Int32		NumFaces;
			Int32		i;

			NumFaces	=0;
			mFirstFace	=gg.NumGFXFaces;

			for(Face=mFaces;Face != null;Face=Face.mNext)
			{
				if(Face.mVisible == 0)
				{
					continue;
				}

				if(Face.mMerged != null ||
					Face.mSplit[0] != null ||
					Face.mSplit[1] != null)
				{
					continue;
				}

				//Skip output of face, if IndexVerts not > 0
				//NOTE - The leaf faces output stage will also skip these same faces...
				if(Face.mNumIndexVerts <= 0)
				{
					continue;
				}

				Face.mFirstIndexVert	=gg.NumGFXVertIndexList;
				Face.mOutputNum			=gg.NumGFXFaces;

				for(i=0;i < Face.mNumIndexVerts;i++)
				{
					gg.GFXVertIndexList[gg.NumGFXVertIndexList]	=Face.mIndexVerts[i];
					gg.NumGFXVertIndexList++;
				}
				gg.NumGFXFaces++;
				NumFaces++;
			}
			mNumFaces	=NumFaces;
		}


		internal bool SaveGFXLeafs_r(GBSPGlobals gg, BinaryWriter bw)
		{
			GFXLeaf	GLeaf	=new GFXLeaf();
			Int32	i;

			if(mPlaneNum == PlanePool.PLANENUM_LEAF)
			{
				GLeaf.mContents	=mContents;

				GLeaf.mMins	=mBounds.mMins;
				GLeaf.mMaxs =mBounds.mMaxs;

				GLeaf.mFirstFace	=gg.NumGFXLeafFaces;
				GLeaf.mFirstPortal	=mFirstPortal;
				GLeaf.mNumPortals	=mNumPortals;

				GLeaf.mCluster	=mCluster;	//CHANGE: CLUSTER
				GLeaf.mArea		=mArea;

				GLeaf.mFirstSide	=mFirstSide;
				GLeaf.mNumSides		=mNumSides;

				GLeaf.mNumFaces	=0;

				for(i=0;i < mNumLeafFaces;i++)
				{
					if(mLeafFaces[i].mVisible == 0)
					{
						continue;
					}
					
					//Don't output mark face if it was skipped in the face output stage
					//(or it will reference an invalid face...)
					if(mLeafFaces[i].mNumIndexVerts <= 0)
					{
						continue;
					}

					gg.GFXLeafFaces[gg.NumGFXLeafFaces]	=mLeafFaces[i].mOutputNum;
					gg.NumGFXLeafFaces++;

					GLeaf.mNumFaces++;
				}

				gg.TotalLeafSize++;

				GLeaf.Write(bw);

				return	true;
			}

			if(!mChildren[0].SaveGFXLeafs_r(gg, bw))
			{
				return	false;
			}
			if(!mChildren[1].SaveGFXLeafs_r(gg, bw))
			{
				return	false;
			}
			return	true;
		}


		internal bool SaveGFXFaces_r(BinaryWriter bw)
		{
			GBSPFace	Face;
			GFXFace		GFace	=new GFXFace();

			if(mPlaneNum == PlanePool.PLANENUM_LEAF)
			{
				return	true;
			}
			
			for(Face = mFaces;Face != null;Face = Face.mNext)
			{
				if(Face.mVisible == 0)
				{
					continue;
				}

				if(Face.mMerged != null
					|| Face.mSplit[0] != null
					|| Face.mSplit[1] != null)
				{
					continue;
				}

				if(Face.mNumIndexVerts > 0)
				{
					GFace.mFirstVert	=Face.mFirstIndexVert;
					GFace.mNumVerts		=Face.mNumIndexVerts;
					GFace.mPlaneNum		=Face.mPlaneNum;
					GFace.mPlaneSide	=Face.mPlaneSide;
					GFace.mTexInfo		=Face.mTexInfo;
					GFace.mLWidth		=0;
					GFace.mLHeight		=0;
					GFace.mLightOfs		=-1;	//No light info yet
					GFace.mLTypes[0]	=255;	//Of course, no styles yet either
					GFace.mLTypes[1]	=255;
					GFace.mLTypes[2]	=255;
					GFace.mLTypes[3]	=255;

					GFace.Write(bw);
				}
			}
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
	}
}
