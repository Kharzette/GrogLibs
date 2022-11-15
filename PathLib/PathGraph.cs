using System;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using UtilityLib;


namespace PathLib
{
	public class PathGraph
	{
		//for debug draw of the connections
		public class LineSeg
		{
			public Vector3	mA, mB;
		}

		//game lookup for path nodes
		Dictionary<int, List<PathNode>>	mGameLeafPathNodes	=new Dictionary<int, List<PathNode>>();

		//pathing on threads
		List<PathFinder>	mActivePathing	=new List<PathFinder>();
		List<PathCB>		mCallBacks		=new List<PathCB>();

		//occupation info for pathfinding
		List<PathNode>	mOccupation	=new List<PathNode>();

		//game specific info on node occupation
		List<object>	[]mNodeOccupation;

		List<PathNode>	mNodery	=new List<PathNode>();

		//delegates used by whatever holds the world data
		//be it bsp or terrain or whatever
		public delegate void	PathCB(List<Vector3> resultPath);
		public delegate void	GetWalkableFaces(out List<List<Vector3>> faces, out List<int> leaves);
		public delegate Int32	FindLeaf(Vector3 pos);
		public delegate bool	IsPositionValid(ref Vector3 pos);
		public delegate bool	CanReach(Vector3 start, Vector3 end);

		//constants
		float	MinEdgeDistance	=4f;


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
			CanReach canReach, IsPositionValid isValid)
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
#if REJECT_EDGE
					//reject points too close to an edge
					float	dist	=cp.DistToNearestEdge(gp);
					if(dist < MinEdgeDistance)
					{
						continue;
					}
#endif

					//reject points that are too near something already created
					bool	bTooClose	=false;
					foreach(PathNode pnn in mNodery)
					{
						float	dist	=pnn.mPoint.Distance(gp);
						if(dist < (gridSize * 0.5f))
						{
							bTooClose	=true;
							break;
						}
					}
					if(bTooClose)
					{
						continue;
					}

					Vector3	adjusted	=gp;
					if(!isValid(ref adjusted))
					{
						continue;
					}
					PathNode	pn	=new PathNode(cp, adjusted);

					mNodery.Add(pn);

					AddNodeToLeafIndex(leaves, i, pn);
				}
			}

			//alloc occupation list
			mNodeOccupation	=new List<object>[mNodery.Count];
			for(int i=0;i < mNodery.Count;i++)
			{
				mNodeOccupation[i]	=new List<object>();
			}

			BuildGriddyConnectivity(gridSize, stepHeight, canReach);
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

			//alloc occupation list
			mNodeOccupation	=new List<object>[mNodery.Count];
			for(int i=0;i < mNodery.Count;i++)
			{
				mNodeOccupation[i]	=new List<object>();
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


		public void Clear()
		{
			mGameLeafPathNodes.Clear();
			mActivePathing.Clear();
			mCallBacks.Clear();
			mOccupation.Clear();
			mNodery.Clear();

			mNodeOccupation	=null;
		}


		public void GetStats(out int numNodes, out int numCons, out int avgCon)
		{
			numNodes	=mNodery.Count;
			numCons		=0;
			avgCon		=0;

			foreach(PathNode pn in mNodery)
			{
				numCons	+=pn.mConnections.Count;
				avgCon	+=numCons;
			}

			avgCon	/=numNodes;
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


		//for debug drawing
		public void GetNodePolys(List<Vector3> verts, List<UInt32> indexes,
			List<Vector3> normals, List<int> vertCounts)
		{
			foreach(PathNode pn in mNodery)
			{
				int	count	=verts.Count;

				Vector3	norm;
				float	dist;
				pn.mPoly.GetPlane(out norm, out dist);

				pn.mPoly.GetTriangles(verts, indexes);

				for(int i=count;i < verts.Count;i++)
				{
					normals.Add(norm);
				}

				vertCounts.Add(verts.Count - count);
			}
		}


		//debug draw connections
		public List<LineSeg> GetNodeConnections()
		{
			List<LineSeg>	ret	=new List<LineSeg>();

			foreach(PathNode pn in mNodery)
			{
				foreach(PathConnection pc in pn.mConnections)
				{
					if(!pc.mbPassable)
					{
						continue;
					}

					LineSeg	ln	=new LineSeg();

					ln.mA	=pn.mPoint;
					ln.mB	=pc.mConnectedTo.mPoint;

					ret.Add(ln);
				}
			}
			return	ret;
		}


		public List<object> GetNodeOccupants(int index)
		{
			return	mNodeOccupation[index];
		}


		public void OccupyNode(int index, object obj)
		{
			Debug.Assert(!mNodeOccupation[index].Contains(obj));

			mNodeOccupation[index].Add(obj);
			mOccupation.Add(mNodery[index]);
		}


		public void LeaveNode(int index, object obj)
		{
			Debug.Assert(mNodeOccupation[index].Contains(obj));

			mNodeOccupation[index].Remove(obj);
			mOccupation.Remove(mNodery[index]);
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
			Debug.Assert(msDelta > 0);

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
				pos	=nodes[curNode] + Vector3.UnitY;
				curNode++;

				//use last 2 nodes for direction
				direction	=(nodes[nodes.Count - 2] - nodes[nodes.Count - 1]);
				direction	=Vector3.Normalize(direction);
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
					pos			+=(direction * speedLen);
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


		void PathFindCB(Object threadContext)
		{
			PathFinder pf	=(PathFinder)threadContext;

			pf.Go(mOccupation);
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
				Vector3	startUp	=start + (Vector3.UnitY * (mid.Y - start.Y));

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
				Vector3	endUp	=end + (Vector3.UnitY * (mid.Y - end.Y));

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
					//(find via TestPathing)
//					if(pnIdx == 138 && pn2Idx == 137)
//					{
//						int	gack	=0;
//						gack++;
//					}

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

					//only allow upward movement amounts of a stairstep...
					//this will also prevent steep slope climbing
					//TODO: use the GROUND_PLANE stuff instead?
					float	upDist	=pn.mPoint.Y - pn2.mPoint.Y;
					if(upDist < -stepHeight)
					{
						continue;
					}

					//try the move
					if(canReach(pn.mPoint, pn2.mPoint))
					{
						PathConnection	pc	=new PathConnection();
						pc.mConnectedTo		=pn2;
						pc.mDistanceBetween	=pn.DistanceBetweenNodes(pn2);
						pc.mbPassable		=true;
						pc.mbUseEdge		=false;

						pn.mConnections.Add(pc);
					}
				}
			}
		}
	}
}
