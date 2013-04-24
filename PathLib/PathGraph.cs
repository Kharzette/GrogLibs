using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using BSPZone;
using UtilityLib;


namespace PathLib
{
	public class PathGraph
	{
		//collision hull the pathing lives in
		Zone	mBSP;

		//bsp node lookup for path nodes
		Dictionary<int, List<PathNode>>	mBSPLeafPathNodes	=new Dictionary<int, List<PathNode>>();

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

		public delegate void PathCB(List<Vector3> resultPath);

		PathGraph() { }


		public static PathGraph CreatePathGrid()
		{
			return	new PathGraph();
		}


		public virtual void GenerateGraph(Zone zone)
		{
			mBSP	=zone;

			mNodery.Clear();

			List<List<Vector3>>	polys;
			List<ZonePlane>		planes;
			List<int>			leaves;

			//grab the walkable faces from the map
			mBSP.GetWalkableFaces(out polys, out planes, out leaves);

			Debug.Assert(polys.Count == planes.Count);

			for(int i=0;i < planes.Count;i++)
			{
				ConvexPoly	cp	=new ConvexPoly(polys[i], planes[i]);

				PathNode	pn	=new PathNode(cp);

				mNodery.Add(pn);

				int	leaf	=leaves[i];
				if(mBSPLeafPathNodes.ContainsKey(leaf))
				{
					mBSPLeafPathNodes[leaf].Add(pn);
				}
				else
				{
					mBSPLeafPathNodes.Add(leaf, new List<PathNode>());
					mBSPLeafPathNodes[leaf].Add(pn);
				}
			}

			BuildConnectivity();
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
					mCallBacks[i](pf.mResultPath);

					//nuke the path finder
					mActivePathing.RemoveAt(i);
					i--;
				}
			}
		}


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


		public bool FindPath(Vector3 start, Vector3 end, PathCB notify)
		{
			int	startNode	=mBSP.FindNodeLandedIn(0, start);
			int	endNode		=mBSP.FindNodeLandedIn(0, end);

			if(startNode >= 0 || endNode >= 0)
			{
				return	false;
			}

			if(!mBSPLeafPathNodes.ContainsKey(startNode))
			{
				return	false;
			}

			if(!mBSPLeafPathNodes.ContainsKey(endNode))
			{
				return	false;
			}

			PathNode	stNode, eNode;

			if(mBSPLeafPathNodes[startNode].Count == 1)
			{
				stNode	=mBSPLeafPathNodes[startNode][0];
			}
			else
			{
				stNode	=FindBestLeafNode(start, mBSPLeafPathNodes[startNode]);
				if(stNode == null)
				{
					return	false;
				}
			}

			if(mBSPLeafPathNodes[endNode].Count == 1)
			{
				eNode	=mBSPLeafPathNodes[endNode][0];
			}
			else
			{
				eNode	=FindBestLeafNode(end, mBSPLeafPathNodes[endNode]);
				if(eNode == null)
				{
					return	false;
				}
			}

			//take the first for now
			FindPath(stNode, eNode, notify);

			return	true;
		}


		PathNode FindBestLeafNode(Vector3 pos, List<PathNode> leafNodes)
		{
			float		bestDist	=float.MaxValue;
			PathNode	bestNode	=null;
			foreach(PathNode pn in leafNodes)
			{
				Vector3	middle	=pn.mPoly.GetCenter();
				float	dist	=Vector3.Distance(middle, pos);
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
			//draw connection lines first
			gd.SetVertexBuffer(mConVB);
			gd.Indices	=mConIB;

			bfx.CurrentTechnique.Passes[0].Apply();
			gd.DrawIndexedPrimitives(PrimitiveType.LineList,
				0, 0, mConVerts.Length, 0, mConIndexs.Length / 2);

			//draw node polys
			gd.SetVertexBuffer(mNodeVB);
			gd.Indices	=mNodeIB;

			bfx.CurrentTechnique.Passes[0].Apply();
			gd.DrawIndexedPrimitives(PrimitiveType.TriangleList,
				0, 0, mNodeVerts.Length, 0, mNodeVerts.Length);
		}


		public void BuildDrawInfo(GraphicsDevice gd)
		{
			List<int>		vertCounts	=new List<int>();
			List<Vector3>	nodePoints	=new List<Vector3>();
			List<UInt16>	indexes		=new List<UInt16>();
			foreach(PathNode pn in mNodery)
			{
				int	count	=nodePoints.Count;
				pn.mPoly.GetTriangles(nodePoints, indexes);
				vertCounts.Add(nodePoints.Count - count);
			}

			mNodeVB	=new VertexBuffer(gd, VertexPositionColor.VertexDeclaration,
				nodePoints.Count * 16, BufferUsage.WriteOnly);
			mNodeIB	=new IndexBuffer(gd, IndexElementSize.SixteenBits,
				indexes.Count * 2, BufferUsage.WriteOnly);

			mNodeVerts	=new VertexPositionColor[nodePoints.Count];
			mNodeIndexs	=indexes.ToArray();

			Random	rnd	=new Random();

			Color	randColor	=Mathery.RandomColor(rnd);

			UInt16	idx		=0;
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
			
			//node connections
			List<Edge>	conLines	=new List<Edge>();
			foreach(PathNode pn in mNodery)
			{
				foreach(PathConnection pc in pn.mConnections)
				{
					Edge	ln	=new Edge();

					ln.mA	=pn.mPoly.GetCenter() + Vector3.UnitY;
					ln.mB	=pc.mConnectedTo.mPoly.GetCenter() + Vector3.UnitY;

					conLines.Add(ln);
				}
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


		void BuildConnectivity()
		{
			foreach(PathNode pn in mNodery)
			{
				BoundingBox	bound	=pn.mPoly.GetBounds();

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

					Edge	edge	=pn.mPoly.GetSharedEdge(pn2.mPoly);
					if(edge == null)
					{
						continue;
					}

					PathConnection	pc		=new PathConnection();
					pc.mConnectedTo			=pn2;
					pc.mDistanceToCenter	=pn.CenterToCenterDistance(pn2);
					pc.mEdgeBetween			=edge;

					pn.mConnections.Add(pc);
				}
			}
		}
	}
}
