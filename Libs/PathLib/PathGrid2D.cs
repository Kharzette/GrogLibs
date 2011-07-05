using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using BSPZone;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace PathLib
{
	internal class PathGrid2D : PathGrid
	{
		PathNode[,]	mNodes;

		int	mXOffset, mZOffset;
		int	mXSize, mZSize;


		public override void GenerateGrid(Zone tree, int gridDensity, float nodeRadius)
		{
			base.GenerateGrid(tree, gridDensity, nodeRadius);

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


		public override void Read(BinaryReader br)
		{
			base.Read(br);

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


		public override void Write(BinaryWriter bw)
		{
			base.Write(bw);

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


		protected override List<Line> GetConnectionLines()
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


		protected override List<Vector3> GetNodePositions()
		{
			List<Vector3>	positions	=new List<Vector3>();

			foreach(PathNode n in mNodes)
			{
				if(n != null)
				{
					Vector3	pos	=Vector3.Zero;
					pos.X	=n.mPosition.X;
					pos.Y	=n.mPosition.Y;
					pos.Z	=n.mPosition.Z;
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


		public override void FindPathRangeLOS(Vector3 start, Vector3 target, float minRange, float maxRange, PathCB notify)
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
				if(!mBSP.RayCollide(pn.mPosition, target, ref impacto, ref leafHit, ref nodeHit))
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


		public override void FindPath(Vector3 start, Vector3 end, PathCB notify)
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
			FindPath(startPN, endPN, notify);
		}
	}
}
