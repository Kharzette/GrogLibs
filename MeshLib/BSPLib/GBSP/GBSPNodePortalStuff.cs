using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	internal partial class GBSPNode
	{
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


		bool PartitionPortals_r(PlanePool pool, bool bVisPortals)
		{
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

			GBSPNode	front	=mChildren[0];
			GBSPNode	back	=mChildren[1];

			GBSPPlane	thisPlane	=pool.mPlanes[mPlaneNum];

			//Create a new portal
			GBSPPoly	newPoly;
			if(!CreatePolyOnNode(out newPoly, pool))
			{
				Map.Print("PartitionPortals_r:  CreatePolyOnNode failed.\n");
				return false;
			}

			//Clip it against all other portals attached to this node
			Int32	side	=0;
			for(GBSPPortal port = mPortals;port != null && newPoly.VertCount() > 2;port=port.mNext[side])
			{
				if(port.mNodes[0] == this)
				{
					side	=0;
				}
				else if(port.mNodes[1] == this)
				{
					side	=1;
				}
				else
				{
					Map.Print("PartitionPortals_r:  Portal does not look at either node.\n");
					return false;
				}

				GBSPPlane	portPlane	=pool.mPlanes[port.mPlaneNum];

				if(!newPoly.ClipPolyEpsilon(0.001f, portPlane, side != 0))
				{
					Map.Print("PartitionPortals_r:  There was an error clipping the poly.\n");
					return false;
				}

				if(newPoly.VertCount() < 3)
				{
					Map.Print("PartitionPortals_r:  Portal was cut away.\n");
					break;
				}
			}
			
			if(newPoly.IsTiny())
			{
				newPoly	=null;
			}

			if(newPoly != null)
			{
				GBSPPortal	newPort	=new GBSPPortal();
				if(newPort == null)
				{
					Map.Print("PartitionPortals_r:  Out of memory for portal.\n");
					return	false;
				}
				newPort.mPoly		=newPoly;
				newPort.mPlaneNum	=mPlaneNum;
				newPort.mOnNode		=this;

				if(!newPort.Check())
				{
					Map.Print("PartiionPortals_r:  Check Portal failed.\n");
					return	false;
				}
				else
				{
					AddPortalToNodes(newPort, front, back);
				}
			}
			
			//Partition all portals by this node
			GBSPPortal	next;
			for(GBSPPortal port = mPortals;port != null;port = next)
			{
				if(port.mNodes[0] == this)
				{
					side	=0;
				}
				else if(port.mNodes[1] == this)
				{
					side	=1;
				}
				else
				{
					Map.Print("PartitionPortals_r:  Portal does not look at either node.\n");
					return	false;
				}

				next	=port.mNext[side];

				//Remember the node on the back side
				GBSPNode	other	=(side == 0)? port.mNodes[1] : port.mNodes[0];
				port.mNodes[0].RemovePortal(port);
				port.mNodes[1].RemovePortal(port);

				GBSPPoly	frontPoly, backPoly;
				if(!port.mPoly.SplitEpsilon(0.001f, thisPlane, out frontPoly, out backPoly, false))
				{
					Map.Print("PartitionPortals_r:  Could not split portal.\n");
					return false;
				}

				if(frontPoly != null && frontPoly.IsTiny())
				{
					frontPoly	=null;
				}

				if(backPoly != null && backPoly.IsTiny())
				{
					backPoly	=null;
				}
				
				if(frontPoly == null && backPoly == null)
				{
					continue;
				}
				
				if(frontPoly == null)
				{
					port.mPoly	=backPoly;
					if(side != 0)
					{
						AddPortalToNodes(port, other, back);
					}
					else
					{
						AddPortalToNodes(port, back, other);
					}
					continue;
				}

				if(backPoly == null)
				{
					port.mPoly	=frontPoly;
					if(side != 0)
					{
						AddPortalToNodes(port, other, front);
					}
					else
					{
						AddPortalToNodes(port, front, other);
					}
					continue;
				}

				//Portal was split
				port.mPoly	=frontPoly;
				GBSPPortal	newPort		=new GBSPPortal(port);
				if(newPort == null)
				{
					Map.Print("PartitionPortals_r:  Out of memory for portal.\n");
					return	false;
				}
				newPort.mPoly	=backPoly;
				
				if(side != 0)
				{
					AddPortalToNodes(port, other, front);
					AddPortalToNodes(newPort, other, back);
				}
				else
				{
					AddPortalToNodes(port, front, other);
					AddPortalToNodes(newPort, back, other);
				}
			}

			if(mPortals != null)
			{
				Map.Print("*WARNING* PartitionPortals_r:  Portals still on node after distribution...\n");
			}
			
			if(!front.PartitionPortals_r(pool, bVisPortals))
			{
				return	false;
			}
			if(!back.PartitionPortals_r(pool, bVisPortals))
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
			int	side	=0;
			for(GBSPPortal p = mPortals;p != null;p=p.mNext[side])
			{
				Debug.Assert(!(p.mNodes[0] == this && p.mNodes[1] == this));
				Debug.Assert(p.mNodes[0] == this || p.mNodes[1] == this);

				side	=(p.mNodes[1] == this)? 1 : 0;

				if(p == port)
				{
					int	side2			=(port.mNodes[1] == this)? 1 : 0;
					mPortals			=port.mNext[side2];
					port.mNodes[side2]	=null;
					return;
				}
				else if(p.mNext[side] == port)
				{
					int side2			=(port.mNodes[1] == this)? 1 : 0;
					p.mNext[side]		=port.mNext[side2];
					port.mNodes[side2]	=null;
					return;
				}
			}
		}


		internal static bool AddPortalToNodes(GBSPPortal port, GBSPNode front, GBSPNode back)
		{
			if(port.mNodes[0] != null || port.mNodes[1] != null)
			{
				Map.Print("LinkPortal:  Portal allready looks at one of the nodes.\n");
				return	false;
			}

			port.mNodes[0]	=front;
			port.mNext[0]	=front.mPortals;
			front.mPortals	=port;

			port.mNodes[1]	=back;
			port.mNext[1]	=back.mPortals;
			back.mPortals	=port;

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
			outsideNode.mbDetail		=false;
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

			//Create 6 portals, and point to the outside and the RootNode
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
				int	side	=0;
				for(GBSPPortal port=mPortals;port != null;port=port.mNext[side])
				{
					side	=(port.mOnNode == port.mNodes[0])? 0 : 1;

					port.mPoly.GetTriangles(verts, indexes, bCheckFlags);
				}
				return;
			}

			mChildren[0].GetPortalTriangles(verts, indexes, bCheckFlags);
			mChildren[1].GetPortalTriangles(verts, indexes, bCheckFlags);
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


		bool FillLeafs_r(bool bFill, Int32 dist, int curFill,
			ref bool bHitEnt, ref GBSPNode hitNode)
		{
			if((mContents & Contents.BSP_CONTENTS_SOLID2) != 0)
			{
				return	true;
			}

			if(mCurrentFill == curFill)
			{
				return	true;
			}

			mCurrentFill	=curFill;
			mOccupied		=dist;

			if(bFill)
			{
				//Preserve user contents
				mContents	&=0xffff0000;
				mContents	|=Contents.BSP_CONTENTS_SOLID2;
			}
			else 
			{
				if(mEntity != 0)
				{
					bHitEnt		=true;
					hitNode		=this;
					return	true;
				}
			}

			int	side;
			for(GBSPPortal port=mPortals;port != null;port = port.mNext[side])
			{
				if(port.mNodes[0] == this)
				{
					side	=0;
				}
				else if(port.mNodes[1] == this)
				{
					side	=1;
				}
				else
				{
					Map.Print("FillLeafs_r:  Portal does not look at either node.\n");
					return	false;
				}
				
				if(!port.mNodes[(side == 0)? 1 : 0].FillLeafs_r(bFill,
					dist + 1, curFill, ref bHitEnt, ref hitNode))
				{
					return	false;
				}
			}

			return	true;
		}


		bool FillLeafs2_r(int curFill)
		{
			if((mContents & Contents.BSP_CONTENTS_SOLID2) != 0)
			{
				return	true;
			}

			if(mCurrentFill == curFill)
			{
				return	true;
			}

			mCurrentFill	=curFill;

			int	side;
			for(GBSPPortal port=mPortals;port != null;port = port.mNext[side])
			{
				if(port.mNodes[0] == this)
				{
					side	=0;
				}
				else if(port.mNodes[1] == this)
				{
					side	=1;
				}
				else
				{
					Map.Print("RemoveOutside2_r:  Portal does not look at either node.\n");
					return	false;
				}

				//Go though the portal to the node on the other side (!side)
				if(!port.mNodes[(side==0)? 1 : 0].FillLeafs2_r(curFill))
				{
					return	false;
				}
			}

			return	true;
		}


		bool FillFromEntities(Int32 curFill, List<MapEntity> ents, PlanePool pool)
		{
			bool	bEmpty	=false;
			for(int i=1;i < ents.Count;i++)	//Don't use the world as an entity (skip 0)!!
			{
				MapEntity	e	=ents[i];
				Vector3		org	=Vector3.Zero;

				if(!e.GetOrigin(out org))
				{
					continue;
				}

				GBSPNode	node	=FindLeaf(org, pool);

				if((node.mContents & Contents.BSP_CONTENTS_SOLID2) != 0)
				{
					continue;
				}
				
				//There is at least one entity in empty space...
				bEmpty	=true;
				
				if(!node.FillLeafs2_r(curFill))
				{
					return	false;
				}
			}

			if(!bEmpty)
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
			Map.Print(" --- Remove Hidden Leafs --- \n");

			GBSPNode	outsideNode	=oNode;

			Int32	side	=(outsideNode.mPortals.mNodes[0] == outsideNode)? 1 : 0;

			int	numRemovedLeafs	=0;

			if(!PlaceEntities(ents, pool))
			{
				return	-1;
			}

			bool		bHitEntity	=false;
			GBSPNode	hitNode		=null;
			int			currentFill	=1;

			if(!outsideNode.mPortals.mNodes[side].FillLeafs_r(false, 1, currentFill, ref bHitEntity, ref hitNode))
			{
				return -1;
			}

			if(bHitEntity)
			{
				Map.Print("*****************************************\n");
				Map.Print("*           *** LEAK ***                *\n");
				Map.Print("* Level is NOT sealed.                  *\n");
				Map.Print("* Optimal removal will not be performed.*\n");
				Map.Print("*****************************************\n");

//				WriteLeakFile("Test", HitNode, ONode);
				return	-1;
			}

			currentFill	=2;
			
			if(!FillFromEntities(currentFill, ents, pool))
			{
				return	-1;
			}
			
			FillUnTouchedLeafs_r(currentFill, ref numRemovedLeafs);

			if(bVerbose)
			{
				Map.Print("Removed Leafs          : " + numRemovedLeafs + "\n");
			}

			return	numRemovedLeafs;
		}


		void MarkVisibleSides_r(PlanePool pool)
		{
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

			Int32	side;
			for(GBSPPortal p=mPortals;p != null;p=p.mNext[side])
			{
				side	=(p.mNodes[1] == this)? 1 : 0;

				if(p.mOnNode == null)
				{
					continue;		// Outside node (assert for it here!!!)
				}

				if(p.mSideFound == 0)
				{
					p.FindPortalSide(pool);
				}

				if(p.mSide != null)
				{
					p.mSide.mFlags	|=GBSPSide.SIDE_VISIBLE;
				}

				if(p.mSide != null)
				{
					int	sOpposite	=(side == 0)? 1 : 0;
					if(((p.mNodes[sOpposite].mContents & Contents.BSP_CONTENTS_SOLID2) == 0)
						&& ((p.mNodes[side].mContents & Contents.BSP_CONTENTS_SHEET) != 0)
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


		static bool FreePortals_r(GBSPNode node)
		{
			if(node == null)
			{
				return	true;
			}
			
			Int32		side;
			GBSPPortal	nextPort;
			for(GBSPPortal port=node.mPortals;port != null;port = nextPort)
			{
				if(port.mNodes[0] == node)
				{
					side	=0;
				}
				else if(port.mNodes[1] == node)
				{
					side	=1;
				}
				else
				{
					Map.Print("FreePortals_r:  Portal does not look at either node.\n");
					return	false;
				}

				nextPort	=port.mNext[side];

				port.mNodes[0].RemovePortal(port);
				port.mNodes[1].RemovePortal(port);

				//free portal here
			}

			node.mPortals	=null;

			if(node.mPlaneNum == PlanePool.PLANENUM_LEAF)
			{
				return	true;
			}

			if(!FreePortals_r(node.mChildren[0]))
			{
				return	false;
			}

			if(!FreePortals_r(node.mChildren[1]))
			{
				return	false;
			}
			return	true;
		}


		internal bool FreePortals()
		{
			return	FreePortals_r(this);
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

				Int32		side;
				GBSPNode	[]nodes	=new GBSPNode[2];
				for(GBSPPortal port=mPortals;port != null;port=port.mNext[side])
				{					
					nodes[0]	=port.mNodes[0];
					nodes[1]	=port.mNodes[1];
			
					side	=(nodes[1] == this)? 1 : 0;

					if(port.mPoly == null)
					{
						continue;
					}

					if(!port.CanSeeThroughPortal())
					{
						continue;
					}
					
					if(nodes[0].mCluster == nodes[1].mCluster)	
					{
						Map.Print("GetVisInfo:  Portal seperating the same cluster.\n");
						return	false;
					}
					VISPortal	vport	=new VISPortal();

					vport.mPoly	=new GBSPPoly(port.mPoly);
					if(side == 0)
					{
						vport.mPoly.Reverse();
					}

					Int32	side2	=side;

					vport.mPlane	=new GBSPPlane(vport.mPoly);

					if(Vector3.Dot(pool.mPlanes[port.mPlaneNum].mNormal, vport.mPlane.mNormal) < 0.99f)
					{
						if(side != 0)
						{
							side2	=0;
						}
						else
						{
							side2	=1;
						}
					}

					if(nodes[side2].mCluster < 0 || nodes[side2].mCluster > numLeafClusters)
					{
						Map.Print("GetVisInfo: Bad leaf cluster number.\n");
						return	false;
					}

					Int32	leafFrom	=nodes[side2].mCluster;
					Int32	oppSide2	=(side2 == 0)? 1 : 0;
					if(nodes[oppSide2].mCluster < 0 || nodes[oppSide2].mCluster > numLeafClusters)
					{
						Map.Print("GetVisInfo: Bad leaf cluster number 2.\n");
						return	false;
					}

					Int32	leafTo	=nodes[oppSide2].mCluster;

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
					vport.mLeaf		=leafTo;
					vport.mNext		=leaf.mPortals;
					leaf.mPortals	=vport;

					vport.CalcPortalInfo();

					visPortals.Add(vport);
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


		internal bool PrepPortalFile_r(ref int numPortalLeafs, ref int numPortals)
		{
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
				int	side;
				for(GBSPPortal port=mPortals;port != null;port=port.mNext[side])
				{
					GBSPNode	[]nodes	=new GBSPNode[2];
					nodes[0]	=port.mNodes[0];
					nodes[1]	=port.mNodes[1];
			
					side	=(nodes[1] == this)? 1 : 0;

					if(port.mPoly == null)
					{
						Map.Print("*WARNING*  SavePortalFile_r:  Portal with NULL poly.\n");
						continue;
					}

					if(!port.CanSeeThroughPortal())
					{
						continue;
					}
					
					if(nodes[0].mCluster == nodes[1].mCluster)	
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
				int	side;
				for(GBSPPortal port=mPortals;port!=null;port=port.mNext[side])
				{
					GBSPNode	[]nodes	=new GBSPNode[2];
					nodes[0]	=port.mNodes[0];
					nodes[1]	=port.mNodes[1];

					side	=(nodes[1] == this)? 1 : 0;

					if(port.mPoly == null)
					{
						continue;
					}

					if(!port.CanSeeThroughPortal())
					{
						continue;
					}

					if(nodes[0].mCluster == nodes[1].mCluster)	
					{
						Map.Print("PrepPortalFile_r:  Portal seperating the same cluster.\n");
						return	false;
					}

					GBSPPoly	poly	=port.mPoly;

					if(poly.VertCount() < 3)
					{
						Map.Print("SavePortalFile_r:  Portal poly verts < 3.\n");
						return	false;
					}

					bw.Write(poly.VertCount());

					if(side == 0)
					{
						//If on front side, reverse so it points to the other leaf
						poly.WriteReverse(bw);
					}					
					else
					{
						//It's allready pointing to the other leaf
						poly.Write(bw);
					}

					int	side2	=side;

					GBSPPlane	plane	=new GBSPPlane(poly);
					if(Vector3.Dot(pool.mPlanes[port.mPlaneNum].mNormal, plane.mNormal) < 0.99f)
					{
						side2	=(side2 == 0)? 1 : 0;
					}

					if(nodes[side2].mCluster < 0
						|| nodes[side2].mCluster > numLeafClusters)
					{
						Map.Print("SavePortalFile_r:  Bad Leaf Cluster Number.\n");
						return	false;
					}

					int	clust	=nodes[side2].mCluster;
					bw.Write(clust);

					int	Side2Opposite	=(side2 == 0)? 1 : 0;
						
					if (nodes[Side2Opposite].mCluster < 0
						|| nodes[Side2Opposite].mCluster > numLeafClusters)
					{
						Map.Print("SavePortalFile_r:  Bad Leaf Cluster Number.\n");
						return	false;
					}

					clust	=nodes[Side2Opposite].mCluster;
					bw.Write(clust);
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
				Map.Print("Num Leaf Sides       : " + leafSides.Count + "\n");
				Map.Print("Num Leaf Bevels      : " + numLeafBevels + "\n");
			}
			return	true;
		}


		bool CreateLeafSides_r(PlanePool pool, ref int numLeafBevels,
			List<GFXLeafSide> leafSides)
		{
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

				GBSPPortal	nextPort;
				for(GBSPPortal port=mPortals;port != null;port=nextPort)
				{
					Int32	side	=(port.mNodes[0] == this)? 1 : 0;
					nextPort		=port.mNext[(side == 0)? 1 : 0];

					int	i;
					for(i=0;i < CNumLeafSides;i++)
					{
						if(LPlaneNumbers[i] == port.mPlaneNum
							&& LPlaneSides[i] == side)
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
						LPlaneNumbers.Add(port.mPlaneNum);
						LPlaneSides.Add(side);
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
				for(int Axis=0;Axis < 3;Axis++)
				{
					for(int Dir=-1;Dir <= 1;Dir += 2)
					{
						//See if the plane is allready in the sides
						int	i;
						GBSPPlane	plane	=new GBSPPlane();
						for(i=0;i < cNumLeafSides;i++)
						{
							plane	=pool.mPlanes[LPlaneNumbers[i]];
								
							if(LPlaneSides[i] != 0)
							{
								plane.Inverse();
							}
							if(UtilityLib.Mathery.VecIdx(plane.mNormal, Axis) == Dir)
							{
								break;
							}
						}
						if(i >= cNumLeafSides)
						{
							//Add a new axial aligned side
							plane.mNormal	=Vector3.Zero;

							UtilityLib.Mathery.VecIdxAssign(ref plane.mNormal, Axis, Dir);

							//get the mins/maxs from the gbsp brush
							if(Dir == 1)
							{
								plane.mDist	=UtilityLib.Mathery.VecIdx(bnd.mMaxs, Axis);
							}
							else
							{
								plane.mDist	=-UtilityLib.Mathery.VecIdx(bnd.mMins, Axis);
							}

							sbyte	side;
							LPlaneNumbers.Add(pool.FindPlane(plane, out side));
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
			
			for(int i=0;i < cNumLeafSides;i++)
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
			bnd	=new Bounds();
			bnd.Clear();

			if(mPlaneNum != PlanePool.PLANENUM_LEAF)
			{
				Map.Print("GetLeafBBoxFromPortals:  Not a leaf.\n");
				return	false;
			}

			Int32	side;
			for(GBSPPortal port=mPortals;port != null;port=port.mNext[side])
			{
				side	=(port.mNodes[1] == this)? 1 : 0;

				port.mPoly.AddToBounds(bnd);
			}
			return	true;
		}


		bool FillAreas_r(Int32 Area, Map.ModelForLeafNode modForLeaf)
		{
			if((mContents & Contents.BSP_CONTENTS_SOLID2) != 0)
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
			Int32	side;
			for(GBSPPortal port=mPortals;port != null;port=port.mNext[side])
			{
				side	=(port.mNodes[1] == this)? 1 : 0;
				
				if(!port.mNodes[(side == 0)? 1 : 0].FillAreas_r(Area, modForLeaf))
				{
					return	false;
				}
			}
			return	true;
		}


		internal bool CreateAreas_r(ref int numAreas, Map.ModelForLeafNode modForLeaf)
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


		internal bool FinishAreaPortals_r(Map.ModelForLeafNode modForLeaf)
		{
			GBSPModel	mod;

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

			mod	=modForLeaf(this);

			if(mod == null)
			{
				Map.Print("FinishAreaPortals_r:  No model for leaf.\n");
				return	false;
			}

			//Set to first area that flooded into portal
			mArea	=mod.mAreas[0];
			mod.mbAreaPortal	=true;
			
			return	true;
		}
	}
}
