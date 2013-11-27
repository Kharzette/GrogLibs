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

			BuildGriddyConnectivity(gridSize, stepHeight, canReach);

//			PruneSkinny(isPosOK);
		}


		public void Load(string fileName)
		{
			FileStream	fs	=new FileStream(fileName, FileMode.Open, FileAccess.Read);
			if(fs == null)
			{
				return;
			}

			BinaryReader	br	=new BinaryReader(fs);
			if(br == null)
			{
				return;
			}

			UInt32	magic	=br.ReadUInt32();
			if(magic != 0xBA711FAD)
			{
				return;
			}

			int	nodeCount	=br.ReadInt32();

			mNodery.Clear();

			for(int i=0;i < nodeCount;i++)
			{
				PathNode	pn	=new PathNode(br);

				mNodery.Add(pn);
			}

			foreach(PathNode pn in mNodery)
			{
				pn.ReadConnections(br, mNodery);
			}

			mGameLeafPathNodes.Clear();

			int	glpnCount	=br.ReadInt32();
			for(int i=0;i < glpnCount;i++)
			{
				int	key		=br.ReadInt32();
				int	nCount	=br.ReadInt32();

				List<PathNode>	nodes	=new List<PathNode>();
				for(int j=0;j < nCount;j++)
				{
					int	nidx	=br.ReadInt32();

					nodes.Add(mNodery[nidx]);
				}

				mGameLeafPathNodes.Add(key, nodes);
			}
		}


		public void Save(string fileName)
		{
			FileStream	fs	=new FileStream(fileName, FileMode.Create, FileAccess.Write);
			if(fs == null)
			{
				return;
			}

			BinaryWriter	bw	=new BinaryWriter(fs);
			if(bw == null)
			{
				return;
			}

			bw.Write(0xBA711FAD);

			bw.Write(mNodery.Count);
			foreach(PathNode pn in mNodery)
			{
				pn.Write(bw);
			}
			foreach(PathNode pn in mNodery)
			{
				pn.WriteConnections(bw, mNodery);
			}

			bw.Write(mGameLeafPathNodes.Count);

			foreach(KeyValuePair<int, List<PathNode>> gameLeaf in mGameLeafPathNodes)
			{
				bw.Write(gameLeaf.Key);

				bw.Write(gameLeaf.Value.Count);

				foreach(PathNode pn in gameLeaf.Value)
				{
					int	idx	=mNodery.IndexOf(pn);

					bw.Write(idx);
				}
			}

			bw.Close();
			fs.Close();
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


		//helper function to move along a path
		public static bool MoveAlongPath(List<Vector3> nodes,
			ref int curNode, ref Vector3 pos,
			int msDelta, float speed,
			out Vector3 direction)
		{
			//this will make it really clear when something is wrong
			//as it will make a nanny matrix
			direction	=Vector3.Zero;

			if(nodes.Count < 2)
			{
				return	true;
			}
			if(curNode < 0 || curNode >= nodes.Count)
			{
				return	true;
			}

			if((curNode + 1) >= nodes.Count)
			{
				//at end node
				pos	=nodes[curNode];
				curNode++;

				//use last 2 nodes for direction
				direction	=(nodes[nodes.Count - 2] - nodes[nodes.Count - 1]);
				direction.Normalize();
				return	true;
			}

			float	speedLen	=speed * msDelta;
			bool	bMoving		=true;

			for(;bMoving;)
			{
				Vector3	current	=nodes[curNode];
				Vector3	next	=nodes[curNode + 1];

				direction	=next - pos;

				float	dirLen	=direction.Length();

				if(speedLen > dirLen)
				{
					//have a bit more momentum to push towards another node
					pos	=next;
					curNode++;

					if(curNode == (nodes.Count - 1))
					{
						//reached the destination
						direction	/=dirLen;
						curNode		=0;

						return	true;
					}
					continue;
				}
				else
				{
					direction	/=dirLen;
					pos			+=direction * speedLen;
					bMoving		=false;
				}
			}			

			return	false;
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


		//projects up, then forward
		bool CanReachStairStep(Vector3 start, Vector3 end, Vector3 mid, CanReach canReach)
		{
			//stepping up or down?
			if(start.Y < end.Y)
			{
				//get an up target for the start
				Vector3	startUp	=start + (Vector3.Up * (mid.Y - start.Y));

				if(!canReach(start, startUp))
				{
					return	false;
				}
				if(!canReach(startUp, mid))
				{
					return	false;
				}
				if(!canReach(mid, end))
				{
					return	false;
				}
			}
			else
			{
				//get an up target for the end
				Vector3	endUp	=end + (Vector3.Up * (mid.Y - end.Y));

				if(!canReach(start, mid))
				{
					return	false;
				}
				if(!canReach(mid, endUp))
				{
					return	false;
				}
				if(!canReach(endUp, end))
				{
					return	false;
				}
			}
			return	true;
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


		void BuildGriddyConnectivity(int gridSize, float stepHeight, CanReach canReach)
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
					//(find via bsptest, stand on them)
//					if(pnIdx == 183 && pn2Idx == 161)
//					{
//						int	gack	=0;
//						gack++;
//					}

					//for stair check
					bool	bStairs	=false;

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
						bUseEdge	=true;

						//if these are not coplanar, should be in seperate polys
						Debug.Assert(pn.mPoly != pn2.mPoly);

						Edge	e1	=pn.mPoly.GetSharedEdge(pn2.mPoly);
						Edge	e2	=pn2.mPoly.GetSharedEdge(pn.mPoly);

						//happens when connected via one vert
						if(e1 == null || e2 == null)
						{
							//stairs?
							e1	=pn.mPoly.GetSharedEdgeXZ(pn2.mPoly, stepHeight);
							e2	=pn2.mPoly.GetSharedEdgeXZ(pn.mPoly, stepHeight);
							if(e1 == null || e2 == null)
							{
								continue;
							}

							bStairs	=true;

							//check endpoint distance to Y plane
							float	e1Dist	=Vector3.Dot(Vector3.Up, e1.mA);
							float	e2Dist	=Vector3.Dot(Vector3.Up, e2.mA);

							float	yDist	=e2Dist - e1Dist;

							if(yDist < -stepHeight || yDist > stepHeight)
							{
								continue;
							}

							//want e1 to be the "highest" edge
							if(e1Dist < e2Dist)
							{
								Edge	temp	=e2;

								e2	=e1;
								e1	=temp;
							}
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
						if(!bStairs)
						{
							if(!CanReachTwoMoves(pn.mPoint, pn2.mPoint, shortB, canReach))
							{
								continue;
							}
						}
						else
						{
							if(!CanReachStairStep(pn.mPoint, pn2.mPoint, shortB, canReach))
							{
								continue;
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
	}
}
