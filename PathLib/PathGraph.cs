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
	public delegate void PathCB(List<ConvexPoly> resultPath);


	public struct Line
	{
		public Vector2	mP1;
		public Vector2	mP2;

		public static Line Zero;
	}


	public class PathGraph
	{
		//collision hull the pathing lives in
		protected Zone	mBSP;

		//pathing on threads
		List<PathFinder>	mActivePathing	=new List<PathFinder>();
		List<PathCB>		mCallBacks		=new List<PathCB>();	

		//drawing stuff
		VertexBuffer		mNodeVB, mConVB, mPathVB;
		IndexBuffer			mNodeIB, mConIB, mPathIB;

		VertexPositionColor	[]mNodeVerts;
		VertexPositionColor	[]mConVerts;
		VertexPositionColor	[]mPathVerts;
		UInt16				[]mNodeIndexs;
		UInt16				[]mConIndexs;
		int					[]mPathIndexs;

		List<PathNode>	mNodery	=new List<PathNode>();


		protected PathGraph() { }


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

			//grab the walkable faces from the map
			mBSP.GetWalkableFaces(out polys, out planes);

			Debug.Assert(polys.Count == planes.Count);

			for(int i=0;i < planes.Count;i++)
			{
				ConvexPoly	cp	=new ConvexPoly(polys[i], planes[i]);

				PathNode	pn	=new PathNode(cp);

				mNodery.Add(pn);
			}

			BuildConnectivity();
		}


		public virtual void Read(BinaryReader br)
		{
		}


		public virtual void Write(BinaryWriter bw)
		{
		}


		public virtual void Update(float msDelta)
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


		public virtual void FindPathRangeLOS(Vector3 start, Vector3 target, float minRange, float maxRange, PathCB notify)
		{
		}


		protected virtual void FindPath(PathNode start, PathNode end, PathCB notify)
		{
			PathFinder	pf	=new PathFinder();

			pf.StartPath(start, end);

			mActivePathing.Add(pf);
			mCallBacks.Add(notify);

			ThreadPool.QueueUserWorkItem(PathFindCB, pf);			
		}


		public virtual void FindPath(Vector3 start, Vector3 end, PathCB notify)
		{
		}


		public virtual void Render(GraphicsDevice gd, BasicEffect bfx)
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


			//draw pathfinding
			if(mPathVB == null || mPathVerts.Length <= 0)
			{
				return;
			}

			gd.SetVertexBuffer(mPathVB);
			gd.Indices	=mPathIB;

			bfx.CurrentTechnique.Passes[0].Apply();
			gd.DrawIndexedPrimitives(PrimitiveType.LineList,
				0, 0, mPathVerts.Length, 0, mPathIndexs.Length / 2);
		}


		public virtual void BuildDrawInfo(GraphicsDevice gd)
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
			List<ConvexPoly.Edge>	conLines	=new List<ConvexPoly.Edge>();
			foreach(PathNode pn in mNodery)
			{
				foreach(PathConnection pc in pn.mConnections)
				{
					ConvexPoly.Edge	ln	=new ConvexPoly.Edge();

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
			foreach(ConvexPoly.Edge ln in conLines)
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


		public virtual void BuildDrawInfoForPath(GraphicsDevice gd, List<Vector3> path)
		{
			if(path.Count < 2)
			{
				return;
			}

			//a solved path
			mPathVB	=new VertexBuffer(gd, typeof(VertexPositionColor), path.Count * 16, BufferUsage.WriteOnly);
			mPathIB	=new IndexBuffer(gd, IndexElementSize.ThirtyTwoBits, (path.Count - 1) * 2 * 4, BufferUsage.WriteOnly);

			mPathVerts	=new VertexPositionColor[path.Count];
			mPathIndexs	=new int[(path.Count - 1) * 2];

			int	idx	=0;
			foreach(Vector3 pn in path)
			{
				mPathVerts[idx].Position.X	=pn.X;
				mPathVerts[idx].Position.Y	=pn.Y;
				mPathVerts[idx].Position.Z	=pn.Z;
				mPathVerts[idx++].Color		=Color.Yellow;
			}

			idx	=0;
			bool	bToggle	=false;
			for(int i=0;i < (path.Count - 1) * 2;i++)
			{
				mPathIndexs[i]	=idx++;

				if(bToggle)
				{
					idx--;
				}
				bToggle	=!bToggle;
			}

			mPathVB.SetData<VertexPositionColor>(mPathVerts);
			mPathIB.SetData<int>(mPathIndexs);
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

					BoundingBox	bound2	=pn2.mPoly.GetBounds();

					if(bound.Contains(bound2) == ContainmentType.Disjoint)
					{
						continue;
					}

					if(!pn.mPoly.IsAdjacent(pn2.mPoly))
					{
						continue;
					}

					PathConnection	pc		=new PathConnection();
					pc.mConnectedTo			=pn2;
					pc.mDistanceToCenter	=pn.CenterToCenterDistance(pn2);

					pn.mConnections.Add(pc);
				}
			}
		}
	}
}
