using System;
using System.IO;
using System.Numerics;
using System.Collections.Generic;
using Vortice.Mathematics;
using UtilityLib;


namespace PathLib
{
	internal class ConvexPoly
	{
		List<Vector3>	mVerts	=new List<Vector3>();
		List<Edge>		mEdges	=new List<Edge>();


		internal ConvexPoly(List<Vector3> verts)
		{
			mVerts.AddRange(verts);

			SnapVerts();

			mEdges	=CalcEdges();
		}


		internal ConvexPoly(BinaryReader br)
		{
			int	vCount	=br.ReadInt32();
			for(int i=0;i < vCount;i++)
			{
				Vector3	v	=FileUtil.ReadVector3(br);
				mVerts.Add(v);
			}

			int	eCount	=br.ReadInt32();
			for(int i=0;i < eCount;i++)
			{
				Edge	e	=new Edge();

				e.mA	=FileUtil.ReadVector3(br);
				e.mB	=FileUtil.ReadVector3(br);

				mEdges.Add(e);
			}
		}


		void SnapVerts()
		{
			List<Vector3>	snapped	=new List<Vector3>();
			foreach(Vector3 v in mVerts)
			{
				Vector3	snap	=Vector3.Zero;

				snap.X	=(float)Math.Round(v.X);
				snap.Y	=(float)Math.Round(v.Y);
				snap.Z	=(float)Math.Round(v.Z);

				snapped.Add(snap);
			}

			//elminate dupes
			for(int i=0;i < snapped.Count;i++)
			{
				for(int j=0;j < snapped.Count;j++)
				{
					if(i == j)
					{
						continue;
					}

					if(snapped[i] == snapped[j])
					{
						snapped.RemoveAt(j);
						j--;
						i	=0;
					}
				}
			}

			mVerts.Clear();
			mVerts.AddRange(snapped);
		}


		internal void Write(BinaryWriter bw)
		{
			bw.Write(mVerts.Count);
			foreach(Vector3 vert in mVerts)
			{
				FileUtil.WriteVector3(bw, vert);
			}

			bw.Write(mEdges.Count);
			foreach(Edge e in mEdges)
			{
				FileUtil.WriteVector3(bw, e.mA);
				FileUtil.WriteVector3(bw, e.mB);
			}
		}


		internal float Area()
		{
			float	total	=0.0f;
			for(int i=2;i < mVerts.Count;i++)
			{
				Vector3	vect1	=mVerts[i - 1] - mVerts[0];
				Vector3	vect2	=mVerts[i] - mVerts[0];

				Vector3	cross	=Vector3.Cross(vect1, vect2);

				total	+=0.5f * cross.Length();
			}
			return	total;
		}


		internal int GetEdgeCount()
		{
			return	mVerts.Count;
		}


		//fill missing with any edges not in found
		internal void GetMissingEdges(List<Edge> found, List<Edge> missing)
		{
			foreach(Edge e in mEdges)
			{
				if(found.Contains(e))
				{
					continue;
				}
				missing.Add(e);
			}
		}


		internal BoundingBox GetBounds()
		{
			return	BoundingBox.CreateFromPoints(mVerts.ToArray());
		}


		List<Edge> CalcEdges()
		{
			List<Edge>	ret	=new List<Edge>();

			for(int i=0;i < mVerts.Count;i++)
			{
				Edge	e	=new Edge();

				int	idx	=i + 1;
				if(idx >= mVerts.Count)
				{
					idx	-=mVerts.Count;
				}

				e.mA	=mVerts[i];
				e.mB	=mVerts[idx];

				ret.Add(e);
			}

			return	ret;
		}


		internal float DistToNearestEdge(Vector3 pos)
		{
			float	bestDist	=float.MaxValue;
			Edge	nearest		=null;
			foreach(Edge e in mEdges)
			{
				float	dist	=e.Distance(pos);
				if(dist < bestDist)
				{
					bestDist	=dist;
					nearest		=e;
				}
			}
			return	bestDist;
		}


		//find centerpoints for imaginary grid snapped polys of gridSize
		internal List<Vector3> GetGridPoints(int gridSize)
		{
			List<Vector3>	ret	=new List<Vector3>();

			BoundingBox	bnd	=GetBounds();

			Vector3	norm;
			float	dist;
			Mathery.PlaneFromVerts(mVerts, out norm, out dist);

			if(norm.Y < 0.95f)
			{
				int	gack	=0;
				gack++;
			}

			//this is a lot easier since we step in xz
			//snap bounds to grid
			Vector3	snapMax	=Vector3.Zero;
			Vector3	snapMin	=Vector3.Zero;

			snapMax.X	=(float)Math.Ceiling(bnd.Max.X / gridSize);
			snapMax.Z	=(float)Math.Ceiling(bnd.Max.Z / gridSize);

			snapMin.X	=(float)Math.Floor(bnd.Min.X / gridSize);
			snapMin.Z	=(float)Math.Floor(bnd.Min.Z / gridSize);

			int	xSize	=(int)(snapMax.X - snapMin.X);
			int	zSize	=(int)(snapMax.Z - snapMin.Z);

			snapMin.X	*=gridSize;
			snapMin.Z	*=gridSize;
			snapMax.X	*=gridSize;
			snapMax.Z	*=gridSize;

			int	halfGrid	=(int)(gridSize * 0.5f);

			if(xSize <= 0 || zSize <= 0)
			{
				return	ret;
			}

			float	closeToTwoPi	=6.2f;

			for(int z=0;z < zSize;z++)
			{
				int	zLoc	=(int)snapMin.Z;

				zLoc	+=halfGrid;
				zLoc	+=z * gridSize;

				for(int x=0;x < xSize;x++)
				{
					int	xLoc	=(int)snapMin.X;

					xLoc	+=halfGrid;
					xLoc	+=x * gridSize;

					Vector3	coordA	=Vector3.Zero;
					Vector3	coordB	=Vector3.Zero;

					coordA.X	=coordB.X	=xLoc;
					coordA.Z	=coordB.Z	=zLoc;

					//hopefully the 8k rule is enforced
					coordA.Y	=8192f;
					coordB.Y	=-8192f;

					float	distA	=Vector3.Dot(coordA, norm) - dist;
					float	distB	=Vector3.Dot(coordB, norm) - dist;

					float	ratio	=distA / (distA - distB);

					coordB	=coordA - coordB;

					Vector3	onPlane	=coordA - coordB * ratio;

					float	angSum	=Mathery.ComputeAngleSum(onPlane, mVerts);

					if(angSum >= closeToTwoPi)
					{
						ret.Add(onPlane);
					}
				}
			}
			return	ret;
		}


		internal void GetPlane(out Vector3 normal, out float dist)
		{
			Mathery.PlaneFromVerts(mVerts, out normal, out dist);
		}


		internal Edge GetSharedEdge(ConvexPoly other)
		{
			Edge	ret			=null;
			float	bestOverlap	=0f;
			foreach(Edge me in mEdges)
			{
				foreach(Edge oe in other.mEdges)
				{
					if(!me.IsColinear(oe))
					{
						continue;
					}

					float	over	=me.GetOverlap(oe);

					//todo:  take max overlapped edge
					if(over > bestOverlap)
					{
						bestOverlap	=over;
						ret			=oe;
					}
				}
			}
			return	ret;
		}


		//only checks the x and z
		//used for pathing over stair steps or short geom
		internal Edge GetSharedEdgeXZ(ConvexPoly other, float maxDist)
		{
			Edge	temp		=new Edge();
			Edge	ret			=null;
			float	bestOverlap	=0f;
			foreach(Edge me in mEdges)
			{
				foreach(Edge oe in other.mEdges)
				{
					//take a vertical distance from one of the points
					float	dist	=me.mA.Y - oe.mA.Y;
					if(dist > maxDist)
					{
						continue;
					}

					temp.mA	=oe.mA;
					temp.mB	=oe.mB;

					temp.mA.Y	+=dist;
					temp.mB.Y	+=dist;

					if(me.IsColinear(temp))
					{
						float	over	=me.GetOverlap(temp);
						if(over > bestOverlap)
						{
							bestOverlap	=over;
							ret			=oe;
						}
					}
				}
			}
			return	ret;
		}


		internal Vector3 GetCenter()
		{
			Vector3	ret	=Vector3.Zero;
			foreach(Vector3 vert in mVerts)
			{
				ret	+=vert;
			}
			ret	/=mVerts.Count;

			return	ret;
		}


		internal void GetTriangles(List<Vector3> verts, List<UInt32> indexes)
		{
			int	ofs		=verts.Count;

			UInt32	offset	=(UInt32)ofs;

			//triangulate
			foreach(Vector3 pos in mVerts)
			{
				verts.Add(pos);
			}

			int i	=0;
			for(i=1;i < mVerts.Count - 1;i++)
			{
				indexes.Add((UInt32)(offset + ((i + 1) % mVerts.Count)));
				indexes.Add((UInt32)(offset + i));
				indexes.Add(offset);
			}
		}
	}
}
