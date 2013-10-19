using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using UtilityLib;


namespace PathLib
{
	public class PathGraph
	{
		//game lookup for path nodes
		Dictionary<int, List<PathNode>>	mGameLeafPathNodes	=new Dictionary<int, List<PathNode>>();

		//pathing on threads
		List<PathFinder>	mActivePathing	=new List<PathFinder>();
		List<PathCB>		mCallBacks		=new List<PathCB>();	

		//drawing stuff
		VertexBuffer		mNodeVB, mConVB;
		IndexBuffer			mNodeIB, mConIB;

		VertexPositionColor	[]mNodeVerts;
		VertexPositionColor	[]mConVerts;
		UInt16				[]mNodeIndexs;
		UInt16				[]mConIndexs;

		List<PathNode>	mNodery	=new List<PathNode>();

		//delegates used by whatever holds the world data
		//be it bsp or terrain or whatever
		public delegate void	PathCB(List<Vector3> resultPath);
		public delegate void	GetWalkableFaces(out List<List<Vector3>> faces, out List<int> leaves);
		public delegate Int32	FindLeaf(Vector3 pos);
		public delegate bool	IsPositionValid(Vector3 pos);
		public delegate bool	CanReach(Vector3 start, Vector3 end);

		PathGraph() { }


		public static PathGraph CreatePathGrid()
		{
			return	new PathGraph();
		}


		void AddNodeToLeafIndex(List<int> leaves, int idx, PathNode pn)
		{
			int	leaf	=leaves[idx];
			if(mGameLeafPathNodes.ContainsKey(leaf))
			{
				mGameLeafPathNodes[leaf].Add(pn);
			}
			else
			{
				mGameLeafPathNodes.Add(leaf, new List<PathNode>());
				mGameLeafPathNodes[leaf].Add(pn);
			}
		}


		public void GenerateGraph(GetWalkableFaces getWalkable,
			int gridSize, float stepHeight,
			IsPositionValid isPosOK, CanReach canReach)
		{
			mNodery.Clear();

			List<List<Vector3>>	polys;
			List<int>			leaves;

			//grab the walkable faces from the map
			getWalkable(out polys, out leaves);

			for(int i=0;i < polys.Count;i++)
			{
				ConvexPoly	cp	=new ConvexPoly(polys[i]);

				List<Vector3>	gridPoints	=cp.GetGridPoints(gridSize);

				foreach(Vector3 gp in gridPoints)
				{
					PathNode	pn	=new PathNode(cp, gp);

					mNodery.Add(pn);

					AddNodeToLeafIndex(leaves, i, pn);
				}
			}

			BuildGriddyConnectivity(gridSize, canReach);

//			PruneSkinny(isPosOK);
		}


		public void Read(BinaryReader br)
		{
		}


		public void Write(BinaryWriter bw)
		{
		}


		public void Update()
		{
			for(int i=0;i < mActivePathing.Count;i++)
			{
				PathFinder	pf	=mActivePathing[i];

				if(pf.IsDone())
				{
					//call the callback
					mCallBacks[i](pf.GetResultPath());

					//remove ref to callback
					mCallBacks.RemoveAt(i);

					//nuke the path finder
					mActivePathing.RemoveAt(i);
					i--;
				}
			}
		}


		//this is useful for obstacles like locked doors
		public bool SetPathConnectionPassable(Vector3 pos, FindLeaf findLeaf, bool bPassable)
		{
			//figure out which node this is on / near
			int	leafNode	=findLeaf(pos);

			if(leafNode >= 0)
			{
				return	false;
			}

			if(!mGameLeafPathNodes.ContainsKey(leafNode))
			{
				return	false;
			}

			PathNode	conNode;

			if(mGameLeafPathNodes[leafNode].Count == 1)
			{
				conNode	=mGameLeafPathNodes[leafNode][0];
			}
			else
			{
				conNode	=FindBestLeafNode(pos, mGameLeafPathNodes[leafNode]);
				if(conNode == null)
				{
					return	false;
				}
			}

			foreach(PathConnection pc in conNode.mConnections)
			{
				pc.mbPassable	=bPassable;

				foreach(PathConnection opc in pc.mConnectedTo.mConnections)
				{
					if(opc.mConnectedTo == conNode)
					{
						opc.mbPassable	=bPassable;
						break;	//should only be one per
					}
				}
			}
			return	true;
		}


		public Vector3 GetNodePosition(int index)
		{
			if(index < 0 || index >= mNodery.Count)
			{
				return	Vector3.Zero;
			}

			PathNode	pn	=mNodery[index];

			return	pn.mPoint;
		}


		//TODO: this should find a spot within line of sight of the target
		public void FindPathRangeLOS(Vector3 start, Vector3 target, float minRange, float maxRange, PathCB notify)
		{
		}


		void FindPath(PathNode start, PathNode end, PathCB notify)
		{
			PathFinder	pf	=new PathFinder();

			pf.StartPath(start, end);

			mActivePathing.Add(pf);
			mCallBacks.Add(notify);

			ThreadPool.QueueUserWorkItem(PathFindCB, pf);			
		}


		public bool FindPath(Vector3 start, Vector3 end, PathCB notify, FindLeaf findLeaf)
		{
			int	startNode	=findLeaf(start);
			int	endNode		=findLeaf(end);

			if(startNode >= 0 || endNode >= 0)
			{
				return	false;
			}

			if(!mGameLeafPathNodes.ContainsKey(startNode))
			{
				return	false;
			}

			if(!mGameLeafPathNodes.ContainsKey(endNode))
			{
				return	false;
			}

			PathNode	stNode, eNode;

			if(mGameLeafPathNodes[startNode].Count == 1)
			{
				stNode	=mGameLeafPathNodes[startNode][0];
			}
			else
			{
				stNode	=FindBestLeafNode(start, mGameLeafPathNodes[startNode]);
				if(stNode == null)
				{
					return	false;
				}
			}

			if(mGameLeafPathNodes[endNode].Count == 1)
			{
				eNode	=mGameLeafPathNodes[endNode][0];
			}
			else
			{
				eNode	=FindBestLeafNode(end, mGameLeafPathNodes[endNode]);
				if(eNode == null)
				{
					return	false;
				}
			}

			FindPath(stNode, eNode, notify);

			return	true;
		}


		public bool GetInfoAboutLocation(Vector3 groundPos, FindLeaf findLeaf,
			out int numConnections, out int myIndex, List<int> indexesConnectedTo)
		{
			numConnections	=-1;
			myIndex			=-1;
			int	startNode	=findLeaf(groundPos);

			if(startNode >= 0)
			{
				return	false;
			}

			if(!mGameLeafPathNodes.ContainsKey(startNode))
			{
				return	false;
			}

			List<PathNode>	pNodes	=mGameLeafPathNodes[startNode];

			PathNode	best	=FindBestLeafNode(groundPos, pNodes);
			if(best == null)
			{
				return	false;
			}

			myIndex	=mNodery.IndexOf(best);

			numConnections	=best.mConnections.Count;

			foreach(PathConnection con in best.mConnections)
			{
				indexesConnectedTo.Add(mNodery.IndexOf(con.mConnectedTo));
			}
			return	true;
		}


		PathNode FindBestLeafNode(Vector3 pos, List<PathNode> leafNodes)
		{
			float		bestDist		=float.MinValue;
			PathNode	bestNode	=null;

			//check griddy
			bestDist	=float.MaxValue;
			foreach(PathNode pn in leafNodes)
			{
				float	dist	=Vector3.Distance(pn.mPoint, pos);
				if(dist < bestDist)
				{
					bestNode	=pn;
					bestDist	=dist;
				}
			}
			return	bestNode;
		}


		public void Render(GraphicsDevice gd, BasicEffect bfx)
		{
			if(mConVB != null)
			{
				//draw connection lines first
				gd.SetVertexBuffer(mConVB);
				gd.Indices	=mConIB;

				bfx.CurrentTechnique.Passes[0].Apply();
				gd.DrawIndexedPrimitives(PrimitiveType.LineList,
					0, 0, mConVerts.Length, 0, mConIndexs.Length / 2);
			}

			if(mNodeVB != null)
			{
				//draw node polys
				gd.SetVertexBuffer(mNodeVB);
				gd.Indices	=mNodeIB;

				bfx.CurrentTechnique.Passes[0].Apply();
				gd.DrawIndexedPrimitives(PrimitiveType.TriangleList,
					0, 0, mNodeVerts.Length, 0, mNodeVerts.Length);
			}
		}


		public void BuildDrawInfo(GraphicsDevice gd)
		{
			List<ConvexPoly>	built	=new List<ConvexPoly>();

			List<int>		vertCounts	=new List<int>();
			List<Vector3>	nodePoints	=new List<Vector3>();
			List<UInt16>	indexes		=new List<UInt16>();
			foreach(PathNode pn in mNodery)
			{
				if(built.Contains(pn.mPoly))
				{
					continue;
				}

				int	count	=nodePoints.Count;
				pn.mPoly.GetTriangles(nodePoints, indexes);
				vertCounts.Add(nodePoints.Count - count);
			}

			UInt16	idx		=0;
			if(nodePoints.Count > 0)
			{
				mNodeVB	=new VertexBuffer(gd, VertexPositionColor.VertexDeclaration,
					nodePoints.Count * 16, BufferUsage.WriteOnly);
				mNodeIB	=new IndexBuffer(gd, IndexElementSize.SixteenBits,
					indexes.Count * 2, BufferUsage.WriteOnly);

				mNodeVerts	=new VertexPositionColor[nodePoints.Count];
				mNodeIndexs	=indexes.ToArray();

				Random	rnd	=new Random();

				Color	randColor	=Mathery.RandomColor(rnd);

				int		pcnt	=0;
				int		poly	=0;
				foreach(Vector3 pos in nodePoints)
				{
					mNodeVerts[idx].Position.X	=pos.X;
					mNodeVerts[idx].Position.Y	=pos.Y;
					mNodeVerts[idx].Position.Z	=pos.Z;
					mNodeVerts[idx++].Color		=randColor;
					pcnt++;
					if(pcnt >= vertCounts[poly])
					{
						pcnt	=0;
						poly++;
						randColor	=Mathery.RandomColor(rnd);
					}
				}

				mNodeVB.SetData<VertexPositionColor>(mNodeVerts);
				mNodeIB.SetData<UInt16>(mNodeIndexs);
			}
			
			//node connections
			List<Edge>	conLines	=new List<Edge>();
			foreach(PathNode pn in mNodery)
			{
				foreach(PathConnection pc in pn.mConnections)
				{
					Edge	ln	=new Edge();

					ln.mA	=pn.mPoint + Vector3.UnitY;
					ln.mB	=pc.mConnectedTo.mPoint + Vector3.UnitY;

					conLines.Add(ln);
				}
			}

			if(conLines.Count <= 0)
			{
				return;
			}
			mConVB	=new VertexBuffer(gd, VertexPositionColor.VertexDeclaration, conLines.Count * 2 * 16, BufferUsage.WriteOnly);
			mConIB	=new IndexBuffer(gd, IndexElementSize.SixteenBits, conLines.Count * 2 * 2, BufferUsage.WriteOnly);

			mConVerts	=new VertexPositionColor[conLines.Count * 2];
			mConIndexs	=new UInt16[conLines.Count * 2];

			idx	=0;
			foreach(Edge ln in conLines)
			{
				//coords y and z swapped
				mConIndexs[idx]			=idx;
				mConVerts[idx].Position	=ln.mA;
				mConVerts[idx++].Color	=Color.BlueViolet;

				mConIndexs[idx]			=idx;
				mConVerts[idx].Position	=ln.mB;
				mConVerts[idx++].Color	=Color.Red;
			}

			mConVB.SetData<VertexPositionColor>(mConVerts);
			mConIB.SetData<UInt16>(mConIndexs);
		}


		void PathFindCB(Object threadContext)
		{
			PathFinder pf	=(PathFinder)threadContext;

			pf.Go();
		}


		//look for nodes next to an obstacle that might prevent
		//a mobile from reaching the center due to collision
		void PruneSkinny(IsPositionValid isPosOK)
		{
			//discourage use of these nodes
			List<PathNode>	badNodes	=new List<PathNode>();
			foreach(PathNode pn in mNodery)
			{
				//check center
				Vector3	center	=pn.mPoint;

				if(!isPosOK(center))
				{
					badNodes.Add(pn);
					continue;
				}
			}
			PenalizeNodes(badNodes);

			badNodes.Clear();

			//now check shared edges
			checkShared:
			foreach(PathNode pn in mNodery)
			{
				foreach(PathConnection pc in pn.mConnections)
				{
					if(pc.mbUseEdge)
					{
						if(!isPosOK(pc.mEdgeBetween))
						{
							//break this connection
							Disconnect(pn, pc.mConnectedTo);
							goto	checkShared;
						}
					}
					else
					{
						Vector3	centerBetween	=pn.mPoint;

						centerBetween	-=pc.mConnectedTo.mPoint;
						centerBetween	*=0.5f;
						centerBetween	=pn.mPoint - centerBetween;

						if(!isPosOK(centerBetween))
						{
							//break this connection
							Disconnect(pn, pc.mConnectedTo);
							goto	checkShared;
						}
					}
				}
			}
		}


		void Disconnect(PathNode pn, PathNode pn2)
		{
			//remove connections to pn
			List<PathConnection>	nukeCons	=new List<PathConnection>();
			foreach(PathConnection pc in pn.mConnections)
			{
				if(pc.mConnectedTo != pn2)
				{
					continue;
				}

				foreach(PathConnection pc2 in pn2.mConnections)
				{
					if(pc2.mConnectedTo == pn)
					{
						pc.mConnectedTo.mConnections.Remove(pc2);
						break;
					}
				}

				pn.mConnections.Remove(pc);
				break;
			}
		}


		void PenalizeNodes(List<PathNode> prunes)
		{
			foreach(PathNode pn in prunes)
			{
				pn.mHScorePenalty	=100;
			}
		}


		bool CanReachTwoMoves(Vector3 start, Vector3 end, Vector3 mid, CanReach canReach)
		{
			if(!canReach(start, mid))
			{
				return	false;
			}
			if(!canReach(mid, end))
			{
				return	false;
			}
			return	true;
		}


		void BuildGriddyConnectivity(int gridSize, CanReach canReach)
		{
			float	halfGS	=gridSize / 2f;
			float	sqTwo	=(float)Math.Sqrt(2.0);

			//fudge a bit
			sqTwo	+=0.1f;

			float	gridSQ2	=gridSize * sqTwo;

			foreach(PathNode pn in mNodery)
			{
				int	pnIndex	=mNodery.IndexOf(pn);

				foreach(PathNode pn2 in mNodery)
				{
					if(pn == pn2)
					{
						continue;
					}

					//debuggery
					int	pnIdx	=mNodery.IndexOf(pn);
					int	pn2Idx	=mNodery.IndexOf(pn2);

					//good place to break if you have 2 tricksy nodes
					if(pnIdx == 83 && pn2Idx == 78)
					{
						int	gack	=0;
						gack++;
					}

					//make sure we are not already connected
					bool	bFound	=false;
					foreach(PathConnection con in pn.mConnections)
					{
						if(con.mConnectedTo == pn2)
						{
							bFound	=true;
							break;
						}
					}
					if(bFound)
					{
						continue;
					}

					//test xz distance
					Vector3	flatPN2	=pn2.mPoint;

					flatPN2.Y	=pn.mPoint.Y;

					float	xzDist	=Vector3.Distance(pn.mPoint, flatPN2);

					if(xzDist > gridSQ2)
					{
						continue;
					}

					//test 3D distance
					float	dist	=Vector3.Distance(pn.mPoint, pn2.mPoint);
					if(dist > (1.5f * gridSQ2))
					{
						continue;
					}

					//see if p1 and p2 are planar
					Vector3	pnNorm, pn2Norm;
					float	pnDist, pn2Dist;
					pn.mPoly.GetPlane(out pnNorm, out pnDist);
					pn2.mPoly.GetPlane(out pn2Norm, out pn2Dist);

					bool	bUseEdge;

					//planar?
					float	close	=pn2Dist - pnDist;
					if(!(Mathery.CompareVectorEpsilon(pnNorm, pn2Norm, 0.001f)
						&& (close > -0.01f && close < 0.01f)))
					{
						//there are three ways to do this:
						//can use the shortest edge between the two nodes
						//or use the longest, or try to follow the general
						//direction of the path only modifying it to hit the edge
						//we should opt for the latter first, then shortest,
						//then longest, trying all 3 till one or none is found
						bUseEdge	=true;

						//if these are not coplanar, should be in seperate polys
						Debug.Assert(pn.mPoly != pn2.mPoly);

						Edge	e1	=pn.mPoly.GetSharedEdge(pn2.mPoly);
						Edge	e2	=pn2.mPoly.GetSharedEdge(pn.mPoly);

						//happens when connected via one vert
						if(e1 == null || e2 == null)
						{
							continue;
						}

						//find the shortest line between the path and edge
						Vector3	shortA, shortB;
						if(!Mathery.ShortestLineBetweenTwoLines(e1.mA, e1.mB, pn.mPoint, pn2.mPoint,
							out shortA, out shortB))
						{
							continue;
						}

						//see if the short line goes up or down from the edge
						Vector3	e1Center	=e1.GetCenter();

						Vector3	upVec	=shortB - shortA;
						if(upVec.Y < 0f)
						{
							upVec	=-upVec;
							shortB	+=(upVec * 2);
						}

						//trace pn to edge
						if(!CanReachTwoMoves(pn.mPoint, pn2.mPoint, shortB, canReach))
						{
							//failed so try short/long edge centers

							Edge	shortEdge	=null;
							Edge	longEdge	=null;

							//try to short edge
							if(e1.Length() > e2.Length())
							{
								shortEdge	=e2;
								longEdge	=e1;
							}
							else
							{
								shortEdge	=e1;
								longEdge	=e2;
							}

							Vector3	cent	=shortEdge.GetCenter();
							if(!CanReachTwoMoves(pn.mPoint, pn2.mPoint, cent, canReach))
							{
								cent	=longEdge.GetCenter();
								if(!CanReachTwoMoves(pn.mPoint, pn2.mPoint, cent, canReach))
								{
									continue;
								}
							}
						}
					}
					else
					{
						bUseEdge	=false;

						//test direct move
						if(!canReach(pn.mPoint, pn2.mPoint))
						{
							continue;
						}
					}

					PathConnection	pc	=new PathConnection();
					pc.mConnectedTo		=pn2;
					pc.mDistanceBetween	=pn.DistanceBetweenNodes(pn2);
					pc.mbPassable		=true;
					pc.mbUseEdge		=bUseEdge;

					pn.mConnections.Add(pc);
				}
			}
		}


			//look for connections made by stair steps
/*			foreach(PathNode pn in mNodery)
			{
				BoundingBox	bound	=pn.mPoly.GetBounds();

				//expand box by stair height
				bound.Min.Y	-=stepHeight;
				bound.Max.Y	+=stepHeight;

				foreach(PathNode pn2 in mNodery)
				{
					if(pn == pn2)
					{
						continue;
					}

					//make sure we are not already connected
					bool	bFound	=false;
					foreach(PathConnection con in pn.mConnections)
					{
						if(con.mConnectedTo == pn2)
						{
							bFound	=true;
							break;
						}
					}
					if(bFound)
					{
						continue;
					}

					BoundingBox	bound2	=pn2.mPoly.GetBounds();

					if(bound.Contains(bound2) == ContainmentType.Disjoint)
					{
						continue;
					}

					Edge	edge	=pn.mPoly.GetSharedEdgeXZ(pn2.mPoly);
					if(edge == null)
					{
						continue;
					}

					PathConnection	pc		=new PathConnection();
					pc.mConnectedTo			=pn2;
					pc.mDistanceBetween	=pn.DistanceBetweenNodes(pn2);
					pc.mEdgeBetween			=edge;
					pc.mbPassable			=true;

					pn.mConnections.Add(pc);
				}
			}*/

	}
}
