using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;


namespace BSPCore
{
	internal partial class GBSPNode
	{
		internal bool CreatePortals(GBSPNode outNode, bool bVis, bool bVerbose,
			PlanePool pool, Vector3 nodeMins, Vector3 nodeMaxs)
		{
			if(bVerbose)
			{
				CoreEvents.Print(" --- Create Portals --- \n");
			}

			if(!CreateAllOutsidePortals(pool, ref outNode, nodeMins, nodeMaxs))
			{
				CoreEvents.Print("CreatePortals:  Could not create bbox portals.\n");
				return	false;
			}

			if(!PartitionPortals_r(pool, bVis))
			{
				CoreEvents.Print("CreatePortals:  Could not partition portals.\n");
				return	false;
			}
			return	true;
		}


		bool SplitNodePortals(PlanePool pool)
		{
			GBSPPlane	thisPlane	=pool.mPlanes[mPlaneNum];

			//dupe the portal list so removes work
			List<GBSPPortal>	ports	=new List<GBSPPortal>(mPortals);

			foreach(GBSPPortal p in ports)
			{
				bool	bFront	=(this == p.mFrontNode);

				GBSPNode	otherNode	=(bFront)? p.mBackNode : p.mFrontNode;

				p.mFrontNode.mPortals.Remove(p);
				p.mBackNode.mPortals.Remove(p);

				GBSPPoly	front, back;
				p.mPoly.SplitEpsilon(UtilityLib.Mathery.VCompareEpsilon, thisPlane, out front, out back, false);

				if(front != null && front.IsTiny())
				{
					front	=null;
				}

				if(back != null && back.IsTiny())
				{
					back	=null;
				}
				
				if(front == null && back == null)
				{
					continue;
				}
				
				if(front == null)
				{
					p.mPoly	=back;
					if(bFront)
					{
						mBack.mPortals.Add(p);
						otherNode.mPortals.Add(p);
						p.mFrontNode	=mBack;
						p.mBackNode		=otherNode;
					}
					else
					{
						otherNode.mPortals.Add(p);
						mBack.mPortals.Add(p);
						p.mFrontNode	=otherNode;
						p.mBackNode		=mBack;
					}
					continue;
				}

				if(back == null)
				{
					p.mPoly	=front;
					if(bFront)
					{
						mFront.mPortals.Add(p);
						otherNode.mPortals.Add(p);
						p.mFrontNode	=mFront;
						p.mBackNode		=otherNode;
					}
					else
					{
						otherNode.mPortals.Add(p);
						mFront.mPortals.Add(p);
						p.mFrontNode	=otherNode;
						p.mBackNode		=mFront;
					}
					continue;
				}

				//Portal was split
				p.mPoly	=front;

				GBSPPortal	newPort	=new GBSPPortal(p);
				if(newPort == null)
				{
					CoreEvents.Print("SplitNodePortals_r:  Out of memory for portal.\n");
					return	false;
				}
				newPort.mPoly	=back;
				
				if(bFront)
				{
					mFront.mPortals.Add(p);
					otherNode.mPortals.Add(p);
					mBack.mPortals.Add(newPort);
					otherNode.mPortals.Add(newPort);
					p.mFrontNode		=mFront;
					p.mBackNode			=otherNode;
					newPort.mFrontNode	=mBack;
					newPort.mBackNode	=otherNode;
				}
				else
				{
					otherNode.mPortals.Add(p);
					mFront.mPortals.Add(p);
					otherNode.mPortals.Add(newPort);
					mBack.mPortals.Add(newPort);
					p.mFrontNode		=otherNode;
					p.mBackNode			=mFront;
					newPort.mFrontNode	=otherNode;
					newPort.mBackNode	=mBack;
				}
			}

			Debug.Assert(mPortals.Count == 0);

			return	true;
		}


		static	int	portDumps	=0;
		void DumpPortals()
		{
			FileStream		fs	=new FileStream("Portals" + portDumps++ + ".portals", FileMode.Create, FileAccess.Write);
			BinaryWriter	bw	=new BinaryWriter(fs);

			bw.Write(mPortals.Count);
			foreach(GBSPPortal p in mPortals)
			{
				p.mPoly.Write(bw);
			}

			bw.Close();
			fs.Close();
		}


		void DumpPortals_r(BinaryWriter bw)
		{
//			bw.Write(mPortals.Count);
			foreach(GBSPPortal p in mPortals)
			{
				if(mPlaneNum == -1)
				{
					p.mPoly.Write(bw);
				}
			}

			if(mFront != null)
			{
				mFront.DumpPortals_r(bw);
			}
			if(mBack != null)
			{
				mBack.DumpPortals_r(bw);
			}
		}


		int CountPortals_r()
		{
			int	ret	=0;
			if(mFront != null)
			{
				ret	+=mFront.CountPortals_r();
			}
			if(mBack != null)
			{
				ret	+=mBack.CountPortals_r();
			}

			if(this.mPlaneNum == -1)
			{
				ret	+=mPortals.Count;
			}

			return	ret;
		}


		internal void DumpAllPortals()
		{
			int	portCount	=CountPortals_r();

			FileStream		fs	=new FileStream("Portals" + portDumps++ + ".portals", FileMode.Create, FileAccess.Write);
			BinaryWriter	bw	=new BinaryWriter(fs);

			bw.Write(portCount);

			DumpPortals_r(bw);

			bw.Close();
			fs.Close();
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

			GBSPPlane	thisPlane	=pool.mPlanes[mPlaneNum];

			//Create a new portal
			if(!CreatePortalOnNode(pool))
			{
				CoreEvents.Print("PartitionPortals_r:  CreatePortalOnNode failed.\n");
				return false;
			}

			if(!SplitNodePortals(pool))
			{
				CoreEvents.Print("PartitionPortals_r:  SplitNodePortals failed.\n");
				return false;
			}

			if(mPortals.Count > 0)
			{
				CoreEvents.Print("*WARNING* PartitionPortals_r:  Portals still on node after distribution...\n");
			}
			
			if(!mFront.PartitionPortals_r(pool, bVisPortals))
			{
				return	false;
			}
			if(!mBack.PartitionPortals_r(pool, bVisPortals))
			{
				return	false;
			}
			return	true;
		}


		bool CreateAllOutsidePortals(PlanePool pool, ref GBSPNode outsideNode,
			Vector3 nodeMins, Vector3 nodeMaxs)
		{
			outsideNode.mPlaneNum	=PlanePool.PLANENUM_LEAF;
			outsideNode.mContents	=Contents.BSP_CONTENTS_SOLID2;

			//So there won't be NULL volume leafs when we create the outside portals
			//note genesis uses 128 for outside space, Q2 uses 8
			for(int k=0;k < 3;k++)
			{
				if(UtilityLib.Mathery.VecIdx(nodeMins, k) - 8.0f
					<= -Bounds.MIN_MAX_BOUNDS ||
					UtilityLib.Mathery.VecIdx(nodeMaxs, k) + 8.0f
					>= Bounds.MIN_MAX_BOUNDS)
				{
					CoreEvents.Print("CreateAllOutsidePortals:  World BOX out of range...\n");
					return	false;
				}
			}

			nodeMins	-=(Vector3.One * 8.0f);
			nodeMaxs	+=(Vector3.One * 8.0f);

			List<GBSPPortal>	outPorts	=new List<GBSPPortal>();

			for(int i=0;i < 3;i++)
			{
				for(int j=0;j < 2;j++)
				{
					GBSPPortal	p	=new GBSPPortal();

					p.mPlane.mNormal	=UtilityLib.Mathery.AxialNormals[i];
					if(j == 0)
					{
						p.mPlane.mDist	=Vector3.Dot(p.mPlane.mNormal, nodeMins);
					}
					else
					{
						p.mPlane.mDist	=Vector3.Dot(p.mPlane.mNormal, nodeMaxs);
						p.mPlane.Inverse();
					}

					p.mPoly	=new GBSPPoly(p.mPlane);

					mPortals.Add(p);
					outsideNode.mPortals.Add(p);

					p.mFrontNode	=this;
					p.mBackNode		=outsideNode;

					outPorts.Add(p);
				}
			}

			//clip all behind all others
			foreach(GBSPPortal portA in outPorts)
			{
				foreach(GBSPPortal portB in outPorts)
				{
					if(portA == portB)
					{
						continue;
					}

					if(!portA.mPoly.ClipPolyEpsilon(UtilityLib.Mathery.ON_EPSILON, portB.mPlane, false))
					{
						CoreEvents.Print("CreateAllOutsidePortals:  There was an error clipping the portal.\n");
						return	false;
					}
					if(portA.mPoly.VertCount() < 3)
					{
						CoreEvents.Print("CreateAllOutsidePortals:  Portal was clipped away.\n");
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

				foreach(GBSPPortal port in mPortals)
				{
					port.mPoly.GetTriangles(verts, indexes, bCheckFlags);
				}
				return;
			}

			mFront.GetPortalTriangles(verts, indexes, bCheckFlags);
			mBack.GetPortalTriangles(verts, indexes, bCheckFlags);
		}


		void CalcBoundsFromPortals()
		{
			mBounds.Clear();

			foreach(GBSPPortal p in mPortals)
			{
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

			foreach(GBSPPortal port in mPortals)
			{
				if(port.mFrontNode == this)
				{
					if(!port.mFrontNode.FillLeafs_r(bFill, dist + 1, curFill, ref bHitEnt, ref hitNode))
					{
						return	false;
					}
				}
				else if(port.mBackNode == this)
				{
					if(!port.mBackNode.FillLeafs_r(bFill, dist + 1, curFill, ref bHitEnt, ref hitNode))
					{
						return	false;
					}
				}
				else
				{
					CoreEvents.Print("FillLeafs_r:  Portal does not look at either node.\n");
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

			foreach(GBSPPortal port in mPortals)
			{
				if(port == null)
				{
					CoreEvents.Print("RemoveOutside2_r:  Portal does not look at either node.\n");
					return	false;
				}

				//Go though the portal to the node on the opposite side
				if(port.mFrontNode == this)
				{
					if(!port.mBackNode.FillLeafs2_r(curFill))
					{
						return	false;
					}
				}
				else
				{
					if(!port.mFrontNode.FillLeafs2_r(curFill))
					{
						return	false;
					}
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
				CoreEvents.Print("FillFromEntities:  No valid entities for operation.\n");
				return	false;
			}
			return	true;
		}


		void FillUnTouchedLeafs_r(Int32 curFill, ref int numRemovedLeafs)
		{
			if(mPlaneNum != PlanePool.PLANENUM_LEAF)
			{
				mFront.FillUnTouchedLeafs_r(curFill, ref numRemovedLeafs);
				mBack.FillUnTouchedLeafs_r(curFill, ref numRemovedLeafs);
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
			if(bVerbose)
			{
				CoreEvents.Print(" --- Remove Hidden Leafs --- \n");
			}

			GBSPNode	outsideNode	=oNode;

			int	numRemovedLeafs	=0;

			if(!PlaceEntities(ents, pool))
			{
				return	-1;
			}

			bool		bHitEntity	=false;
			GBSPNode	hitNode		=null;
			int			currentFill	=1;
			GBSPNode	oppositeNode;
			if(outsideNode.mPortals[0].mFrontNode == outsideNode)
			{
				oppositeNode	=outsideNode.mPortals[0].mBackNode;
			}
			else
			{
				oppositeNode	=outsideNode.mPortals[0].mFrontNode;
			}

			if(!oppositeNode.FillLeafs_r(false, 1, currentFill, ref bHitEntity, ref hitNode))
			{
				return	-1;
			}

			if(bHitEntity)
			{
				CoreEvents.Print("*****************************************\n");
				CoreEvents.Print("*           *** LEAK ***                *\n");
				CoreEvents.Print("* Level is NOT sealed.                  *\n");
				CoreEvents.Print("* Optimal removal will not be performed.*\n");
				CoreEvents.Print("*****************************************\n");

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
				CoreEvents.Print("Removed Leafs          : " + numRemovedLeafs + "\n");
			}

			return	numRemovedLeafs;
		}


		void MarkVisibleSides_r(PlanePool pool)
		{
			//Recurse to leafs 
			if(mPlaneNum != PlanePool.PLANENUM_LEAF)
			{
				mFront.MarkVisibleSides_r(pool);
				mBack.MarkVisibleSides_r(pool);
				return;
			}

			// Empty (air) leafs don't have faces
			if(mContents == 0)
			{
				return;
			}

			foreach(GBSPPortal p in mPortals)
			{
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
					GBSPNode	oppositeNode, theNode;
					if(p.mBackNode == this)
					{
						oppositeNode	=p.mFrontNode;
						theNode			=p.mBackNode;
					}
					else
					{
						oppositeNode	=p.mBackNode;
						theNode			=p.mFrontNode;
					}

					if(((oppositeNode.mContents & Contents.BSP_CONTENTS_SOLID2) == 0)
						&& ((theNode.mContents & Contents.BSP_CONTENTS_SHEET) != 0)
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
				CoreEvents.Print("--- Map Portals to Brushes ---\n");
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

			node.mPortals.Clear();
			
			if(node.mPlaneNum == PlanePool.PLANENUM_LEAF)
			{
				return	true;
			}

			if(!FreePortals_r(node.mFront))
			{
				return	false;
			}

			if(!FreePortals_r(node.mBack))
			{
				return	false;
			}
			return	true;
		}


		internal bool FreePortals()
		{
			return	FreePortals_r(this);
		}


		void FillLeafNumbers_r(ref int numPortalLeafs, ref int numPortals)
		{
			if(mPlaneNum == PlanePool.PLANENUM_LEAF)
			{
				if((mContents & Contents.BSP_CONTENTS_SOLID2) != 0)
				{
					mPortalLeafNum	=-1;
				}
				else
				{
					mPortalLeafNum	=numPortalLeafs;
				}
				return;
			}

			mPortalLeafNum	=numPortalLeafs;

			mFront.FillLeafNumbers_r(ref numPortalLeafs, ref numPortals);
			mBack.FillLeafNumbers_r(ref numPortalLeafs, ref numPortals);
		}


		internal void NumberLeafs_r(ref int numPortalLeafs, ref int numPortals)
		{
			if(mPlaneNum != PlanePool.PLANENUM_LEAF && !mbDetail)
			{
				mPortalLeafNum	=-99;
				mFront.NumberLeafs_r(ref numPortalLeafs, ref numPortals);
				mBack.NumberLeafs_r(ref numPortalLeafs, ref numPortals);
				return;
			}

			if((mContents & Contents.BSP_CONTENTS_SOLID2) != 0)
			{
				mPortalLeafNum	=-1;
				return;
			}

			FillLeafNumbers_r(ref numPortalLeafs, ref numPortals);
			numPortalLeafs++;
			int	side;

			foreach(GBSPPortal port in mPortals)
			{
				GBSPNode	[]nodes	=new GBSPNode[2];
				nodes[0]	=port.mFrontNode;
				nodes[1]	=port.mBackNode;
			
				side	=(nodes[1] == this)? 1 : 0;

				if(side == 1)
				{
					continue;
				}

				if(port.mPoly == null)
				{
					CoreEvents.Print("*WARNING*  SavePortalFile_r:  Portal with NULL poly.\n");
					continue;
				}

				if(!port.CanSeeThroughPortal())
				{
					continue;
				}
					
				if(nodes[0].mCluster == nodes[1].mCluster)	
				{
					CoreEvents.Print("PrepPortalFile_r:  Portal seperating the same cluster.\n");
				}
					
				//This portal is good...
				numPortals++;
			}
		}


		internal bool PrepPortalFile_r(ref int numPortalLeafs, ref int numPortals)
		{
			//Stop at leafs, and detail nodes (stop at beginning of clusters)
			if(mPlaneNum != PlanePool.PLANENUM_LEAF && !mbDetail)
			{
				mPortalLeafNum	=-99;
				if(!mFront.PrepPortalFile_r(ref numPortalLeafs, ref numPortals))
				{
					return	false;
				}
				if(!mBack.PrepPortalFile_r(ref numPortalLeafs, ref numPortals))
				{
					return	false;
				}
				return	true;
			}

			if((mContents & Contents.BSP_CONTENTS_SOLID2) != 0)
			{
				mPortalLeafNum	=-1;
				return	true;
			}

			//Give this portal it's leaf number...
			mPortalLeafNum	=numPortalLeafs;
			numPortalLeafs++;

			if(mPortals == null)
			{
				CoreEvents.Print("*WARNING* PrepPortalFile_r:  Leaf without any portals.\n");
				return	true;
			}

			//Save out all the portals that belong to this leaf...
			int	side;
			foreach(GBSPPortal port in mPortals)
			{
				GBSPNode	[]nodes	=new GBSPNode[2];
				nodes[0]	=port.mFrontNode;
				nodes[1]	=port.mBackNode;
			
				side	=(nodes[1] == this)? 1 : 0;

				if(side == 1)
				{
					continue;
				}

				if(port.mPoly == null)
				{
					CoreEvents.Print("*WARNING*  SavePortalFile_r:  Portal with NULL poly.\n");
					continue;
				}

				if(!port.CanSeeThroughPortal())
				{
					continue;
				}
					
				if(nodes[0].mCluster == nodes[1].mCluster)	
				{
					CoreEvents.Print("PrepPortalFile_r:  Portal seperating the same cluster.\n");
					return	false;
				}
					
				//This portal is good...
				numPortals++;
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
				foreach(GBSPPortal port in mPortals)
				{
					GBSPNode	[]nodes	=new GBSPNode[2];
					nodes[0]	=port.mFrontNode;
					nodes[1]	=port.mBackNode;

					side	=(nodes[1] == this)? 1 : 0;

					if(side == 1)
					{
						continue;	//only write the forward facing
					}

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
						CoreEvents.Print("PrepPortalFile_r:  Portal seperating the same cluster.\n");
						return	false;
					}

					GBSPPoly	poly	=port.mPoly;

					if(poly.VertCount() < 3)
					{
						CoreEvents.Print("SavePortalFile_r:  Portal poly verts < 3.\n");
						return	false;
					}

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
					if(Vector3.Dot(port.mPlane.mNormal, plane.mNormal) < 0.99f)
					{
						side2	=(side2 == 0)? 1 : 0;
					}

					if(nodes[side2].mCluster < 0
						|| nodes[side2].mCluster > numLeafClusters)
					{
						CoreEvents.Print("SavePortalFile_r:  Bad Leaf Cluster Number.\n");
						return	false;
					}

					int	clust	=nodes[side2].mCluster;
					bw.Write(clust);

					int	Side2Opposite	=(side2 == 0)? 1 : 0;
						
					if (nodes[Side2Opposite].mCluster < 0
						|| nodes[Side2Opposite].mCluster > numLeafClusters)
					{
						CoreEvents.Print("SavePortalFile_r:  Bad Leaf Cluster Number.\n");
						return	false;
					}

					clust	=nodes[Side2Opposite].mCluster;
					bw.Write(clust);
				}
				return	true;
			}

			if(mPortals.Count > 0)
			{
				CoreEvents.Print("*WARNING* SavePortalFile_r:  Node with portal.\n");
			}

			if(!mFront.SavePortalFile_r(bw, pool, numLeafClusters))
			{
				return	false;
			}
			if(!mBack.SavePortalFile_r(bw, pool, numLeafClusters))
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

			if(ogSides.Count == 0)
			{
				//likely created by the cube partitioning
				return	null;
			}

			bool	bFoundExact	=false;
			foreach(GBSPSide side in ogSides)
			{
				if((side.mFlags & GBSPSide.SIDE_NODE) != 0)
				{
					continue;		// Side not visible (result of a csg'd topbrush)
				}

				//First, Try an exact match
				if(side.mPlaneNum == mPlaneNum)
				{
					bFoundExact	=true;
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

				//try opposite
				p2.Inverse();
				dot	=Vector3.Dot(p1.mNormal, p2.mNormal);
				if(dot > bestDot)
				{
					bestDot		=dot;
					bestSide	=side;
				}
			}

			if(bestSide == null)
			{
				CoreEvents.Print("WARNING: Could not map portal to original brush...\n");
			}

			if(!bFoundExact)
			{
				CoreEvents.Print("WARNING: Could not map portal to original brush exactly!\n");
				CoreEvents.Print("Error factor is " + bestDot + "\n");
			}

			return	bestSide;
		}


		internal bool CreateLeafSides(PlanePool pool, List<GFXLeafSide> leafSides,
			GBSPNode outsideNode, bool bVerbose)
		{
			if(bVerbose)
			{
				CoreEvents.Print(" --- Create Leaf Sides --- \n");
			}

			int	numLeafBevels	=0;
			
			if(!CreateLeafSides_r(pool, ref numLeafBevels, leafSides, outsideNode))
			{
				return	false;
			}

			if(bVerbose)
			{
				CoreEvents.Print("Num Leaf Sides       : " + leafSides.Count + "\n");
				CoreEvents.Print("Num Leaf Bevels      : " + numLeafBevels + "\n");
			}
			return	true;
		}


		//todo:  rewrite all of this
		bool CreateLeafSides_r(PlanePool pool, ref int numLeafBevels,
			List<GFXLeafSide> leafSides, GBSPNode outsideNode)
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
					CoreEvents.Print("*WARNING* CreateLeafSides:  Contents leaf with no portals!\n");
					return	true;
				}

				//Reset number of sides for this solid leaf (should we save out other contents?)
				//	(this is just for a collision hull for now...)
				int	CNumLeafSides	=0;

				List<Int32>	LPlaneNumbers	=new List<int>();
				List<Int32>	LPlaneSides		=new List<int>();

				foreach(GBSPPortal port in mPortals)
				{
					GBSPPlane	portPlane	=port.mPlane;

					int	numPlanes	=pool.mPlanes.Count;

					sbyte	portSide;
					int		portPlaneNum	=pool.FindPlane(portPlane, out portSide);

					int	actualSide	=(port.mFrontNode == this)? 1 : 0;

					if(portSide != 0)
					{
						actualSide	=(actualSide == 0)? 1 : 0;
					}

					//make sure we aren't adding new planes at this late stage
					//unless the outside node is involved
					if(port.mBackNode != outsideNode)
					{
						Debug.Assert(numPlanes == pool.mPlanes.Count);
					}

					int	i;
					for(i=0;i < CNumLeafSides;i++)
					{
						if(LPlaneNumbers[i] == portPlaneNum
							&& LPlaneSides[i] == actualSide)
						{
							break;
						}
					}

					if(i >= CNumLeafSides)
					{
						LPlaneNumbers.Add(portPlaneNum);
						LPlaneSides.Add(actualSide);
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

			if(!mFront.CreateLeafSides_r(pool, ref numLeafBevels, leafSides, outsideNode))
			{
				return	false;
			}
			if(!mBack.CreateLeafSides_r(pool, ref numLeafBevels, leafSides, outsideNode))
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
				CoreEvents.Print("FinishLeafSides:  Could not get leaf portal BBox.\n");
				return	false;
			}
			
			if(cNumLeafSides < 4)
			{
				CoreEvents.Print("*WARNING*  FinishLeafSides:  Incomplete leaf volume.\n");
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
								CoreEvents.Print("FinishLeafSides:  Could not create the plane.\n");
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
				CoreEvents.Print("GetLeafBBoxFromPortals:  Not a leaf.\n");
				return	false;
			}

			foreach(GBSPPortal port in mPortals)
			{
				port.mPoly.AddToBounds(bnd);
			}
			return	true;
		}
		
		
		bool FillAreas_r(Int32 Area, CoreDelegates.ModelForLeafNode modForLeaf)
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
					CoreEvents.Print("FillAreas_r:  No model for leaf.\n");
					return	false;
				}

				if(Model.mAreaFront == Area || Model.mAreaBack == Area)
				{
					return	true;	//Already flooded into this portal from this area
				}

				if(Model.mAreaFront == 0)
				{
					Model.mAreaFront	=Area;
				}
				else if(Model.mAreaBack == 0)
				{
					Model.mAreaBack	=Area;
				}
				else
				{
					CoreEvents.Print("*WARNING* FillAreas_r:  Area Portal touched more than 2 areas.\n");
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
			foreach(GBSPPortal port in mPortals)
			{
				//flood through opposite node
				if(port.mBackNode == this)
				{
					if(!port.mFrontNode.FillAreas_r(Area, modForLeaf))
					{
						return	false;
					}
				}
				else
				{
					if(!port.mBackNode.FillAreas_r(Area, modForLeaf))
					{
						return	false;
					}
				}
			}
			return	true;
		}


		internal bool CreateAreas_r(ref int numAreas, CoreDelegates.ModelForLeafNode modForLeaf)
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

			if(!mFront.CreateAreas_r(ref numAreas, modForLeaf))
			{
				return	false;
			}
			if(!mBack.CreateAreas_r(ref numAreas, modForLeaf))
			{
				return	false;
			}
			return	true;
		}


		internal bool FinishAreaPortals_r(CoreDelegates.ModelForLeafNode modForLeaf)
		{
			GBSPModel	mod;

			if(mPlaneNum != PlanePool.PLANENUM_LEAF)
			{
				if(!mFront.FinishAreaPortals_r(modForLeaf))
				{
					return	false;
				}
				if(!mBack.FinishAreaPortals_r(modForLeaf))
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
				CoreEvents.Print("FinishAreaPortals_r:  No model for leaf.\n");
				return	false;
			}

			//Set to first area that flooded into portal
			mArea				=mod.mAreaFront;
			mod.mbAreaPortal	=true;
			
			return	true;
		}
	}
}
