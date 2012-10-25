using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using BSPZone;


namespace PathLib
{
	public delegate void PathCB(List<Vector3> resultPath);


	public struct Line
	{
		public Vector2	mP1;
		public Vector2	mP2;

		public static Line Zero;
	}


	public class PathGrid
	{
		//collision hull the pathing lives in
		protected Zone	mBSP;

		//pathing on threads
		List<PathFinder>	mActivePathing	=new List<PathFinder>();
		List<PathCB>		mCallBacks		=new List<PathCB>();	

		//a radius that determines how close a node
		//can get to floors / walls / ceilings
		//important because bigger mobiles will need
		//a bigger radius
		protected float	mNodeRadius;

		//how many path nodes per area (lower is more dense)
		protected int	mGridDensity;

		//drawing stuff
		VertexBuffer		mNodeVB, mConVB, mPathVB;
		IndexBuffer			mNodeIB, mConIB, mPathIB;

		VertexPositionColor	[]mNodeVerts;
		VertexPositionColor	[]mConVerts;
		VertexPositionColor	[]mPathVerts;
		int					[]mNodeIndexs;
		int					[]mConIndexs;
		int					[]mPathIndexs;

		List<PathNode>	mNodery	=new List<PathNode>();


		protected PathGrid() { }


		public static PathGrid CreatePathGrid(bool b2D)
		{
			if(b2D)
			{
				return new PathGrid2D();
			}
			else
			{
				return	new PathGrid();
			}
		}


		public void GenerateFromPoints(List<Vector3> points, float radius)
		{
			mNodeRadius	=radius;

			mNodery.Clear();
			foreach(Vector3 point in points)
			{
				PathNode	pn	=new PathNode();
				pn.mPosition	=point;

				mNodery.Add(pn);
			}
		}


		public virtual void GenerateGrid(Zone zone, int gridDensity, float nodeRadius)
		{
			mBSP			=zone;
			mGridDensity	=gridDensity;
			mNodeRadius		=nodeRadius;
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

			//draw node dots
			gd.SetVertexBuffer(mNodeVB);
			gd.Indices	=mNodeIB;

			//drawing dots seems to no longer be supported?
//			gd.RenderState.PointSize	=5;
//			bfx.CurrentTechnique.Passes[0].Apply();
//			gd.DrawIndexedPrimitives(PrimitiveType.LineList,
//				0, 0, mNodeVerts.Length, 0, mNodeVerts.Length);


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
			//node positions drawn as square dots
			List<Vector3>	nodePoints	=GetNodePositions();

			mNodeVB	=new VertexBuffer(gd, VertexPositionColor.VertexDeclaration, nodePoints.Count * 16, BufferUsage.WriteOnly);
			mNodeIB	=new IndexBuffer(gd, IndexElementSize.ThirtyTwoBits, nodePoints.Count * 4, BufferUsage.WriteOnly);

			mNodeVerts	=new VertexPositionColor[nodePoints.Count];
			mNodeIndexs	=new int[nodePoints.Count];

			int	idx	=0;
			foreach(Vector3 pos in nodePoints)
			{
				mNodeIndexs[idx]			=idx;
				mNodeVerts[idx].Position.X	=pos.X;
				mNodeVerts[idx].Position.Y	=pos.Y;
				mNodeVerts[idx].Position.Z	=pos.Z;
				mNodeVerts[idx++].Color		=Color.Blue;
			}

			mNodeVB.SetData<VertexPositionColor>(mNodeVerts);
			mNodeIB.SetData<int>(mNodeIndexs);
			
			//node connections
			List<Line>	conLines	=GetConnectionLines();

			mConVB	=new VertexBuffer(gd, VertexPositionColor.VertexDeclaration, conLines.Count * 2 * 16, BufferUsage.WriteOnly);
			mConIB	=new IndexBuffer(gd, IndexElementSize.ThirtyTwoBits, conLines.Count * 2 * 4, BufferUsage.WriteOnly);

			mConVerts	=new VertexPositionColor[conLines.Count * 2];
			mConIndexs	=new int[conLines.Count * 2];

			idx	=0;
			foreach(Line ln in conLines)
			{
				//coords y and z swapped
				mConIndexs[idx]				=idx;
				mConVerts[idx].Position.X	=ln.mP1.X;
				mConVerts[idx].Position.Z	=ln.mP1.Y;
				mConVerts[idx].Position.Y	=0.0f;
				mConVerts[idx++].Color		=Color.BlueViolet;

				mConIndexs[idx]				=idx;
				mConVerts[idx].Position.X	=ln.mP2.X;
				mConVerts[idx].Position.Z	=ln.mP2.Y;
				mConVerts[idx].Position.Y	=0.0f;
				mConVerts[idx++].Color		=Color.Red;
			}

			mConVB.SetData<VertexPositionColor>(mConVerts);
			mConIB.SetData<int>(mConIndexs);
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


		protected virtual List<Line> GetConnectionLines()
		{
			return	null;
		}


		protected virtual List<Vector3> GetNodePositions()
		{
			return	null;
		}
	}
}
