using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
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
			return	BoundingBox.CreateFromPoints(mVerts);
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
			bnd.Max.X	=(float)Math.Ceiling(bnd.Max.X / gridSize);
			bnd.Max.Z	=(float)Math.Ceiling(bnd.Max.Z / gridSize);

			bnd.Min.X	=(float)Math.Floor(bnd.Min.X / gridSize);
			bnd.Min.Z	=(float)Math.Floor(bnd.Min.Z / gridSize);

			int	xSize	=(int)(bnd.Max.X - bnd.Min.X);
			int	zSize	=(int)(bnd.Max.Z - bnd.Min.Z);

			bnd.Min.X	*=gridSize;
			bnd.Min.Z	*=gridSize;
			bnd.Max.X	*=gridSize;
			bnd.Max.Z	*=gridSize;

			int	halfGrid	=(int)(gridSize * 0.5f);

			if(xSize <= 0 || zSize <= 0)
			{
				return	ret;
			}

			float	closeToTwoPi	=6.2f;

			for(int z=0;z < zSize;z++)
			{
				int	zLoc	=(int)bnd.Min.Z;

				zLoc	+=halfGrid;
				zLoc	+=z * gridSize;

				for(int x=0;x < xSize;x++)
				{
					int	xLoc	=(int)bnd.Min.X;

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

					float	angSum	=ComputeAngleSum(onPlane);

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
			foreach(Edge me in mEdges)
			{
				foreach(Edge oe in other.mEdges)
				{
					if(!me.IsColinear(oe))
					{
						continue;
					}
					if(me.AlmostEqual(oe))
					{
						return	me;
					}
				}
			}
			return	null;
		}

		//only checks the x and z
		//used for pathing over stair steps
		internal Edge GetSharedEdgeXZ(ConvexPoly other)
		{
			foreach(Edge me in mEdges)
			{
				foreach(Edge oe in other.mEdges)
				{
					if(!me.IsColinear(oe))
					{
						continue;
					}
					if(me.AlmostEqualXZ(oe))
					{
						return	me;
					}
				}
			}
			return	null;
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

		internal void GetTriangles(List<Vector3> verts, List<UInt16> indexes)
		{
			int	ofs		=verts.Count;

			UInt16	offset	=(UInt16)ofs;

			//triangulate the brush face points
			foreach(Vector3 pos in mVerts)
			{
				verts.Add(pos);
			}

			int i	=0;
			for(i=1;i < mVerts.Count - 1;i++)
			{
				//initial vertex
				indexes.Add(offset);
				indexes.Add((UInt16)(offset + i));
				indexes.Add((UInt16)(offset + ((i + 1) % mVerts.Count)));
			}
		}

		//uses the add up the angles trick to determine point in poly
		internal float ComputeAngleSum(Vector3 point)
		{
			float	dotSum	=0f;
			for(int i=0;i < mVerts.Count;i++)
			{
				int	vIdx0	=i;
				int	vIdx1	=((i + 1) % mVerts.Count);

				Vector3	v1	=mVerts[vIdx0] - point;
				Vector3	v2	=mVerts[vIdx1] - point;

				float	len1	=v1.Length();
				float	len2	=v2.Length();

				if((len1 * len2) < 0.0001f)
				{
					return	MathHelper.TwoPi;
				}

				v1	/=len1;
				v2	/=len2;

				float	dot	=Vector3.Dot(v1, v2);

				if(dot > 1f)
				{
					dot	=1f;
				}

				dotSum	+=(float)Math.Acos(dot);
			}
			return	dotSum;
		}
	}
}
