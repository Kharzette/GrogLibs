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


		public void GenerateGraph(GetWalkableFaces getWalkable,
			float? gridSize, float stepHeight,
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

				if(gridSize == null)
				{
					PathNode	pn	=new PathNode(cp);
					mNodery.Add(pn);

					int	leaf	=leaves[i];
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
				else
				{
					//find grid aligned points within cp
					List<Vector3>	gridPoints	=cp.GetGridPoints((int)gridSize.Value);

					foreach(Vector3 pnt in gridPoints)
					{
						PathNode	pn	=new PathNode(pnt + Vector3.UnitY);
						mNodery.Add(pn);

						int	leaf	=leaves[i];
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
				}
			}

			if(gridSize == null)
			{
				BuildConnectivity(stepHeight);
			}
			else
			{
				BuildGriddyConnectivity(gridSize.Value, canReach);
			}

			PruneSkinny(isPosOK);
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

			if(pn.mPoly != null)
			{
				return	pn.mPoly.GetCenter();
			}
			else
			{
				return	pn.mPoint;
			}
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
			float		bestSum		=float.MinValue;
			PathNode	bestNode	=null;
			foreach(PathNode pn in leafNodes)
			{
				if(pn.mPoly == null)
				{
					continue;
				}

				float	sum	=pn.mPoly.ComputeAngleSum(pos);
				if(sum > bestSum)
				{
					bestNode	=pn;
					bestSum		=sum;
				}
			}

			if(bestNode != null)
			{
				return	bestNode;
			}

			//check griddy
			bestSum	=float.MaxValue;
			foreach(PathNode pn in leafNodes)
			{
				if(pn.mPoly != null)
				{
					continue;
				}

				float	sum	=Vector3.Distance(pn.mPoint, pos);
				if(sum < bestSum)
				{
					bestNode	=pn;
					bestSum		=sum;
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
			List<int>		vertCounts	=new List<int>();
			List<Vector3>	nodePoints	=new List<Vector3>();
			List<UInt16>	indexes		=new List<UInt16>();
			foreach(PathNode pn in mNodery)
			{
				if(pn.mPoly != null)
				{
					int	count	=nodePoints.Count;
					pn.mPoly.GetTriangles(nodePoints, indexes);
					vertCounts.Add(nodePoints.Count - count);
				}
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

					Edge	between	=pn.FindEdgeBetween(pc.mConnectedTo);

					if(between != null)
					{
						ln.mA	=pn.mPoly.GetCenter() + Vector3.UnitY;
						ln.mB	=between.GetCenter() + Vector3.UnitY;

						conLines.Add(ln);

						ln	=new Edge();

						ln.mA	=between.GetCenter() + Vector3.UnitY;
						ln.mB	=pc.mConnectedTo.mPoly.GetCenter() + Vector3.UnitY;

						conLines.Add(ln);
					}
					else
					{
						if(pn.mPoly != null)
						{
							ln.mA	=pn.mPoly.GetCenter() + Vector3.UnitY;
						}
						else
						{
							ln.mA	=pn.mPoint + Vector3.UnitY;
						}

						if(pc.mConnectedTo.mPoly != null)
						{
							ln.mB	=pc.mConnectedTo.mPoly.GetCenter() + Vector3.UnitY;
						}
						else
						{
							ln.mB	=pc.mConnectedTo.mPoint + Vector3.UnitY;
						}

						conLines.Add(ln);
					}
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
				Vector3	center	=Vector3.Zero;

				if(pn.mPoly != null)
				{
					pn.mPoly.GetCenter();
				}
				else
				{
					center	=pn.mPoint;
				}

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
					if(pc.mEdgeBetween != null)
					{
						Vector3	edgeCenter	=pc.mEdgeBetween.GetCenter();

						if(!isPosOK(edgeCenter))
						{
							//break this connection
							Disconnect(pn, pc.mConnectedTo);
							goto	checkShared;
						}
					}
					else
					{
						Vector3	centerBetween	=Vector3.Zero;

						if(pn.mPoly != null)
						{
							centerBetween	=pn.mPoly.GetCenter();
						}
						else
						{
							centerBetween	=pn.mPoint;
						}

						if(pc.mConnectedTo.mPoly != null)
						{
							centerBetween	-=pc.mConnectedTo.mPoly.GetCenter();
							centerBetween	*=0.5f;
							centerBetween	=pn.mPoly.GetCenter() - centerBetween;
						}
						else
						{
							centerBetween	-=pc.mConnectedTo.mPoint;
							centerBetween	*=0.5f;
							centerBetween	=pn.mPoint - centerBetween;
						}

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


		void BuildGriddyConnectivity(float gridSize, CanReach canReach)
		{
			float		halfGS	=gridSize / 2f;

			foreach(PathNode pn in mNodery)
			{
				int	pnIndex	=mNodery.IndexOf(pn);

				foreach(PathNode pn2 in mNodery)
				{
					if(pn == pn2)
					{
						continue;
					}

					int	pn2Index	=mNodery.IndexOf(pn2);

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

					if(xzDist > (gridSize + 0.1f))
					{
						continue;
					}

					//test 3D distance
					float	dist	=Vector3.Distance(pn.mPoint, pn2.mPoint);
					if(dist > (1.5f * dist))
					{
						continue;
					}

					//cast a ray
					if(!canReach(pn.mPoint, pn2.mPoint))
					{
						continue;
					}

					PathConnection	pc		=new PathConnection();
					pc.mConnectedTo			=pn2;
					pc.mDistanceToCenter	=pn.CenterToCenterDistance(pn2);
					pc.mEdgeBetween			=null;
					pc.mbPassable			=true;

					pn.mConnections.Add(pc);
				}
			}
		}


		void BuildConnectivity(float stepHeight)
		{
			//find connectivity via shared edge
			foreach(PathNode pn in mNodery)
			{
				BoundingBox	bound	=pn.mPoly.GetBounds();

				int	pnIndex	=mNodery.IndexOf(pn);

				foreach(PathNode pn2 in mNodery)
				{
					if(pn == pn2)
					{
						continue;
					}

					int	pn2Index	=mNodery.IndexOf(pn2);

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

					Edge	edge	=pn.mPoly.GetSharedEdge(pn2.mPoly);
					if(edge == null)
					{
						continue;
					}

					PathConnection	pc		=new PathConnection();
					pc.mConnectedTo			=pn2;
					pc.mDistanceToCenter	=pn.CenterToCenterDistance(pn2);
					pc.mEdgeBetween			=edge;
					pc.mbPassable			=true;

					pn.mConnections.Add(pc);
				}
			}

			//look for connections made by stair steps
			foreach(PathNode pn in mNodery)
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
					pc.mDistanceToCenter	=pn.CenterToCenterDistance(pn2);
					pc.mEdgeBetween			=edge;
					pc.mbPassable			=true;

					pn.mConnections.Add(pc);
				}
			}

			//find connectivity via shared vert
			foreach(PathNode pn in mNodery)
			{
				BoundingBox	bound	=pn.mPoly.GetBounds();

				int	pnIndex	=mNodery.IndexOf(pn);

				foreach(PathNode pn2 in mNodery)
				{
					if(pn == pn2)
					{
						continue;
					}

					int	pn2Index	=mNodery.IndexOf(pn2);

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

					//they overlap so might be possible to go directly there
					PathConnection	pc		=new PathConnection();
					pc.mConnectedTo			=pn2;
					pc.mDistanceToCenter	=pn.CenterToCenterDistance(pn2);
					pc.mEdgeBetween			=null;
					pc.mbPassable			=true;

					pn.mConnections.Add(pc);
				}
			}
		}
	}
}
