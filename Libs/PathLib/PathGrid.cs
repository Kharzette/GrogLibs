using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using BSPZone;


namespace SpriteMapLib
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
		PathNode[,]	mNodes;
		Zone		mBsp;

		int	mGridDensity;
		int	mXOffset, mZOffset;
		int	mXSize, mZSize;

		//a radius that determines how close a node
		//can get to floors / walls / ceilings
		//important because bigger mobiles will need
		//a bigger radius
		float	mNodeRadius;

		//pathing on threads
		List<PathFinder>	mActivePathing	=new List<PathFinder>();
		List<PathCB>		mCallBacks		=new List<PathCB>();	

		//drawing stuff
		VertexBuffer		mNodeVB, mConVB, mPathVB;
		IndexBuffer			mNodeIB, mConIB, mPathIB;

		VertexPositionColor	[]mNodeVerts;
		VertexPositionColor	[]mConVerts;
		VertexPositionColor	[]mPathVerts;
		int					[]mNodeIndexs;
		int					[]mConIndexs;
		int					[]mPathIndexs;



		//TODO: this needs to be totally reworked
		//Best thing to do is generate grids from the
		//OnGround surfaces of the level
		public void GenerateGrid(Zone tree, int gridDensity, float nodeRadius)
		{
			mBsp			=tree;
			mGridDensity	=gridDensity;
			mNodeRadius		=nodeRadius;

			//get the extents of the tree
			Vector3	mins, maxs;
			tree.GetBounds(out mins, out maxs);

			int	numXNodes	=(int)((maxs.X - mins.X) / gridDensity);
			int	numZNodes	=(int)((maxs.Z - mins.Z) / gridDensity);

			numXNodes++;
			numZNodes++;

			mXSize	=numXNodes;
			mZSize	=numZNodes;

			//build a 2d array of nodes
			mNodes	=new PathNode[numZNodes, numXNodes];

			mZOffset	=((int)mins.Z / gridDensity) * gridDensity;
			mXOffset	=((int)mins.X / gridDensity) * gridDensity;

			//set positions
			for(int z=0;z < numZNodes;z++)
			{
				int	zpos	=mZOffset + (z * gridDensity);

				for(int x=0;x < numXNodes;x++)
				{
					int	xpos	=mXOffset + (x * gridDensity);

					mNodes[z, x]	=new PathNode();

					mNodes[z, x].mPosition.X	=xpos;
					mNodes[z, x].mPosition.Z	=zpos;
				}
			}

			//check grid points against tree
			for(int z=0;z < numZNodes;z++)
			{
				for(int x=0;x < numXNodes;x++)
				{
					//boost in Y off the floor a bit
					Vector3	pos	=mNodes[z, x].mPosition + Vector3.UnitY * mNodeRadius;
					if(tree.IsSphereInSolid(pos, mNodeRadius))
					{
						mNodes[z, x]	=null;
					}
				}
			}

			//build connectivity data
			for(int z=0;z < numZNodes;z++)
			{
				for(int x=0;x < numXNodes;x++)
				{
					if(mNodes[z, x] == null)
					{
						continue;
					}

					//check connectivity to upper left
					if(z > 0 && x > 0)
					{
						mNodes[z, x].ConnectIfLOS(mNodes[z - 1, x - 1], tree);
					}

					//check straight up
					if(z > 0)
					{
						mNodes[z, x].ConnectIfLOS(mNodes[z - 1, x], tree);
					}

					//check upper right
					if(z > 0 && x < (numXNodes - 1))
					{
						mNodes[z, x].ConnectIfLOS(mNodes[z - 1, x + 1], tree);
					}

					//check left
					if(x > 0)
					{
						mNodes[z, x].ConnectIfLOS(mNodes[z, x - 1], tree);
					}

					//check right
					if(x < (numXNodes - 1))
					{
						mNodes[z, x].ConnectIfLOS(mNodes[z, x + 1], tree);
					}

					//check lower left
					if(z < (numZNodes - 1) && x > 0)
					{
						mNodes[z, x].ConnectIfLOS(mNodes[z + 1, x - 1], tree);
					}

					//check straight down
					if(z < (numZNodes - 1))
					{
						mNodes[z, x].ConnectIfLOS(mNodes[z + 1, x], tree);
					}

					//check lower right
					if(z < (numZNodes - 1) && x < (numXNodes - 1))
					{
						mNodes[z, x].ConnectIfLOS(mNodes[z + 1, x + 1], tree);
					}
				}
			}
		}


		public void Read(BinaryReader br)
		{
			mGridDensity	=br.ReadInt32();
			mXSize			=br.ReadInt32();
			mZSize			=br.ReadInt32();
			mXOffset		=br.ReadInt32();
			mZOffset		=br.ReadInt32();

			mNodes	=new PathNode[mZSize, mXSize];

			for(int z=0;z < mZSize;z++)
			{
				for(int x=0;x < mXSize;x++)
				{
					bool	bNotNull	=br.ReadBoolean();
					if(bNotNull)
					{
						mNodes[z, x]	=new PathNode();
						mNodes[z, x].Read(br);
					}
					else
					{
						mNodes[z, x]	=null;
					}
				}
			}

			//repair connections
			for(int z=0;z < mZSize;z++)
			{
				for(int x=0;x < mXSize;x++)
				{
					PathNode	pn	=mNodes[z, x];

					if(pn == null)
					{
						continue;
					}

					List<PathConnection>	fixedList	=new List<PathConnection>();
					foreach(PathConnection pc in pn.mConnections)
					{
						PathConnection	newPC	=new PathConnection();
						newPC.mDistance			=pc.mDistance;
						newPC.mConnectedTo		=GetNodeNearest(pc.mConnectedTo.mPosition);

						fixedList.Add(newPC);
					}
					pn.mConnections.Clear();
					pn.mConnections	=fixedList;
				}
			}
		}


		public void Write(BinaryWriter bw)
		{
			bw.Write(mGridDensity);
			bw.Write(mXSize);
			bw.Write(mZSize);
			bw.Write(mXOffset);
			bw.Write(mZOffset);

			for(int z=0;z < mZSize;z++)
			{
				for(int x=0;x < mXSize;x++)
				{
					bw.Write(mNodes[z, x] != null);

					if(mNodes[z, x] != null)
					{
						mNodes[z, x].Write(bw);
					}
				}
			}
		}


		private void PathFindCB(Object threadContext)
		{
			PathFinder pf	=(PathFinder)threadContext;

			pf.Go();
		}


		public void Update(float msDelta)
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


		List<Line> GetConnectionLines()
		{
			List<Line>	lines	=new List<Line>();

			foreach(PathNode n in mNodes)
			{
				if(n != null)
				{
					foreach(PathConnection pc in n.mConnections)
					{
						//note the coord change
						Line	ln	=Line.Zero;
						ln.mP1.X	=n.mPosition.X;
						ln.mP1.Y	=n.mPosition.Z;
						ln.mP2.X	=pc.mConnectedTo.mPosition.X;
						ln.mP2.Y	=pc.mConnectedTo.mPosition.Z;

						lines.Add(ln);
					}
				}
			}
			return	lines;
		}


		List<Vector2> GetNodePositions()
		{
			List<Vector2>	positions	=new List<Vector2>();

			foreach(PathNode n in mNodes)
			{
				if(n != null)
				{
					//watch the coord conversion here
					Vector2	pos	=Vector2.Zero;
					pos.X	=n.mPosition.X;
					pos.Y	=n.mPosition.Z;
					positions.Add(pos);
				}
			}
			return	positions;
		}


		int ClampToXSize(int x)
		{
			if(x < 0)
			{
				x	=0;
			}
			else if(x >= mXSize)
			{
				x	=mXSize - 1;
			}
			return	x;
		}


		int ClampToZSize(int z)
		{
			if(z < 0)
			{
				z	=0;
			}
			else if(z >= mZSize)
			{
				z	=mZSize - 1;
			}
			return	z;
		}


		PathNode GetNodeNearest(Vector3 pos)
		{
			int	x, z;

			x	=(int)(((pos.X - mXOffset) / (float)mGridDensity) + 0.5f);
			z	=(int)(((pos.Z - mZOffset) / (float)mGridDensity) + 0.5f);

			x	=ClampToXSize(x);
			z	=ClampToXSize(z);

			return	mNodes[z, x];
		}


		List<PathNode>	GetNodesAtRangeFrom(PathNode node, float minRange, float maxRange)
		{
			List<PathNode>	ret	=new List<PathNode>();

			int	minX	=ClampToXSize((int)(node.mPosition.X - maxRange));
			int	minZ	=ClampToZSize((int)(node.mPosition.Z - maxRange));
			int	maxX	=ClampToXSize((int)(node.mPosition.X + maxRange));
			int	maxZ	=ClampToZSize((int)(node.mPosition.Z + maxRange));

			for(int z=minZ;z < maxZ;z++)
			{
				for(int x=minX;x < maxX;x++)
				{
					float	range	=node.GetDistance(mNodes[z, x]);
					if(range >= minRange && range <= maxRange)
					{
						ret.Add(mNodes[z, x]);
					}
				}
			}
			return	ret;
		}


		public void FindPathRangeLOS(Vector3 start, Vector3 target, float minRange, float maxRange, PathCB notify)
		{
			//grab node at target point
			PathNode	endPN	=GetNodeNearest(target);

			//find nodes that fall into the range constraints
			List<PathNode>	ring	=GetNodesAtRangeFrom(endPN, minRange, maxRange);

			//see which of those have eyes on the target
			List<PathNode>	canSee	=new List<PathNode>();
			foreach(PathNode pn in ring)
			{
				Vector3	impacto	=Vector3.Zero;
				int		leafHit	=0;
				int		nodeHit	=0;
				if(!mBsp.RayCollide(pn.mPosition, target, ref impacto, ref leafHit, ref nodeHit))
				{
					canSee.Add(pn);
				}
			}

			//take the closest one as the crow flies
			//note that this might be the worst possible choice
			float		minDist	=float.MaxValue;
			PathNode	nearest	=null;
			foreach(PathNode pn in canSee)
			{
				Vector3	distVec	=Vector3.Zero;

				distVec	=pn.mPosition - target;
				float	dist	=distVec.LengthSquared();
				if(dist < minDist)
				{
					minDist	=dist;
					nearest	=pn;
				}
			}

			//good enough
			if(nearest != null)
			{
				FindPath(start, nearest.GetPosition(), notify);
			}
		}


		public void FindPath(Vector3 start, Vector3 end, PathCB notify)
		{
			PathNode	startPN	=GetNodeNearest(start);
			if(startPN == null)
			{
				return;	//TODO: find a valid nearby node
			}
			PathNode	endPN	=GetNodeNearest(end);
			if(endPN == null)
			{
				return;	//TODO: find a valid nearby node
			}

			PathFinder	pf	=new PathFinder();

			pf.StartPath(startPN, endPN);

			mActivePathing.Add(pf);
			mCallBacks.Add(notify);

			ThreadPool.QueueUserWorkItem(PathFindCB, pf);			
		}


		public void Render(GraphicsDevice gd, BasicEffect bfx)
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


		public void BuildDrawInfoPathFound(GraphicsDevice gd, List<Vector3> path)
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


		public void BuildDrawInfo(GraphicsDevice gd)
		{
			//node positions drawn as square dots
			List<Vector2>	nodePoints	=GetNodePositions();

			mNodeVB	=new VertexBuffer(gd, VertexPositionColor.VertexDeclaration, nodePoints.Count * 16, BufferUsage.WriteOnly);
			mNodeIB	=new IndexBuffer(gd, IndexElementSize.ThirtyTwoBits, nodePoints.Count * 4, BufferUsage.WriteOnly);

			mNodeVerts	=new VertexPositionColor[nodePoints.Count];
			mNodeIndexs	=new int[nodePoints.Count];

			int	idx	=0;
			foreach(Vector2 pos in nodePoints)
			{
				//note the coordinate system change
				mNodeIndexs[idx]			=idx;
				mNodeVerts[idx].Position.X	=pos.X;
				mNodeVerts[idx].Position.Z	=pos.Y;
				mNodeVerts[idx].Position.Y	=0.0f;
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
	}
}
