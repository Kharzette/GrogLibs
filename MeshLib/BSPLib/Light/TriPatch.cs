using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	class TriEdge
	{
		public Int32		p0, p1;
		public Vector3		mNormal;
		public float		mDist;
		public Tri			mTri;
	}


	class Tri
	{
		public TriEdge	[]mEdges	=new TriEdge[3];


		internal bool IsPointInside(Vector3 Point)
		{
			for(int i=0;i < 3;i++)
			{
				float	Dist;
				TriEdge	pEdge;

				pEdge	=mEdges[i];

				Dist	=Vector3.Dot(pEdge.mNormal, Point) - pEdge.mDist;

				if(Dist < 0.0f)
				{
					return	false;
				}
			}
			return	true;
		}
	}


	public class TriPatch
	{
		TriEdge		[][]mEdgeMatrix;
		TriEdge		[]mEdges;
		Tri			[]mTriList;

		int			mNumPoints;
		int			mNumEdges;
		int			mNumTris;
		GBSPPlane	mPlane;
		RADPatch	[]mPoints;

		public const Int32	MAX_TRI_POINTS		=1024;
		public const Int32	MAX_TRI_EDGES		=(MAX_TRI_POINTS * 6);
		public const Int32	MAX_TRI_TRIS		=(MAX_TRI_POINTS * 2);
		public const float	MIN_MAX_BOUNDS2		=Bounds.MIN_MAX_BOUNDS * 2;


		public TriPatch()
		{
			mEdgeMatrix	=new TriEdge[MAX_TRI_POINTS][];
			for(int i=0;i < MAX_TRI_POINTS;i++)
			{
				mEdgeMatrix[i]	=new TriEdge[MAX_TRI_POINTS];
			}
			mPoints		=new RADPatch[MAX_TRI_POINTS];
			mEdges		=new TriEdge[MAX_TRI_EDGES];
			mTriList	=new Tri[MAX_TRI_TRIS];
		}


		internal TriPatch(GBSPPlane plane)
		{
			mEdgeMatrix	=new TriEdge[MAX_TRI_POINTS][];
			for(int i=0;i < MAX_TRI_POINTS;i++)
			{
				mEdgeMatrix[i]	=new TriEdge[MAX_TRI_POINTS];
			}
			mPoints		=new RADPatch[MAX_TRI_POINTS];
			mEdges		=new TriEdge[MAX_TRI_EDGES];
			mTriList	=new Tri[MAX_TRI_TRIS];
			mPlane		=plane;
		}


		Tri	AllocTriangle()
		{
			if(mNumTris >= MAX_TRI_TRIS)
			{
				Map.Print("mNumTris >= MAX_TRI_TRIS");
				return	null;
			}
			mTriList[mNumTris]	=new Tri();
			Tri	ret	=mTriList[mNumTris];
			mNumTris++;

			return	ret;
		}


		internal bool SampleTriangulation(Vector3 Point, out Vector3 color)
		{
			Tri			t;
			TriEdge		e;
			float		d;
			RADPatch	p0, p1;
			Vector3		v1, v2;

			if(mNumPoints == 0)
			{
				color	=Vector3.Zero;
				return	true;
			}
			if(mNumPoints == 1)
			{
				color	=mPoints[0].mRadFinal;
				return	true;
			}
			
			//See of the Point is inside a tri in the patch
			for(int j=0;j < mNumTris;j++)
			{
				t	=mTriList[j];
				if(!t.IsPointInside(Point))
				{
					continue;
				}
				LerpTriangle(t, Point, out color);

				return	true;
			}
			
			for(int j=0;j < mNumEdges;j++)
			{
				e	=mEdges[j];
				if(e.mTri != null)
				{
					continue;		// not an exterior edge
				}

				d	=Vector3.Dot(Point, e.mNormal) - e.mDist;
				if(d < 0)
				{
					continue;	// not in front of edge
				}

				p0	=mPoints[e.p0];
				p1	=mPoints[e.p1];

				v1	=p1.mOrigin - p0.mOrigin;
				v1.Normalize();

				v2	=Point - p0.mOrigin;
				d	=Vector3.Dot(v2, v1);
				if(d < 0)
				{
					continue;
				}
				if(d > 1)
				{
					continue;
				}
				color	=p0.mRadFinal + (d * p1.mRadFinal -p0.mRadFinal);

				return	true;
			}
			
			if(!FindClosestTriPoint(Point, out color))
			{
				Map.Print("SampleTriangulation:  Could not find closest Color.\n");
				return	false;
			}
			return	true;
		}


		bool FindClosestTriPoint(Vector3 Point, out Vector3 col)
		{
			Int32		i;
			RADPatch	p0, BestPatch;
			float		BestDist, d;
			Vector3		v1;

			col	=Vector3.Zero;

			//Search for nearest Point
			BestDist	=TriPatch.MIN_MAX_BOUNDS2;
			BestPatch	=null;

			for(i=0;i < mNumPoints;i++)
			{
				p0	=mPoints[i];
				v1	=Point - p0.mOrigin;
				d	=v1.Length();
				if(d < BestDist)
				{
					BestDist	=d;
					BestPatch	=p0;
				}
			}
			if(BestPatch == null)
			{
				Map.Print("FindClosestTriPoint: No Points.\n");
				return	false;
			}

			col	=BestPatch.mRadFinal;
			return	true;
		}


		internal bool TriangulatePoints()
		{
			float	d, bestd;
			Vector3	v1;
			int		bp1, bp2, i, j;
			Vector3	p1, p2;
			TriEdge	e, e2;

			//zero out edgematrix
			for(i=0;i < mNumPoints;i++)
			{
				for(j=0;j < mNumPoints;j++)
				{
					mEdgeMatrix[i][j]	=new TriEdge();
				}
			}

			if(mNumPoints < 2)
			{
				return	true;
			}

			//Find the two closest Points
			bestd	=MIN_MAX_BOUNDS2;
			bp1		=0;
			bp2		=0;
			for(i=0;i < mNumPoints;i++)
			{
				p1	=mPoints[i].mOrigin;
				for(j=i+1;j < mNumPoints;j++)
				{
					p2	=mPoints[j].mOrigin;
					v1	=p2 - p1;
					d	=v1.Length();
					if(d < bestd && d > .05f)
					{
						bestd	=d;
						bp1		=i;
						bp2		=j;
					}
				}
			}

			e	=FindEdge(bp1, bp2);
			if(e == null)
			{
				Map.Print("There was an error finding an edge.\n");
				return	false;
			}
			e2	=FindEdge(bp2, bp1);
			if(e2 == null)
			{
				Map.Print("There was an error finding an edge.\n");
				return	false;
			}
			if(!Tri_Edge_r(e))
			{
				return	false;
			}
			if(!Tri_Edge_r(e2))
			{
				return	false;
			}
			return	true;
		}


		void LerpTriangle(Tri t, Vector3 Point, out Vector3 color)
		{
			RADPatch	p1, p2, p3;
			Vector3		bse, d1, d2;
			float		x, y, y1, x2;

			p1	=mPoints[t.mEdges[0].p0];
			p2	=mPoints[t.mEdges[1].p0];
			p3	=mPoints[t.mEdges[2].p0];

			bse	=p1.mRadFinal;
			d1	=p2.mRadFinal - bse;
			d2	=p3.mRadFinal - bse;

			x	=Vector3.Dot(Point, t.mEdges[0].mNormal) - t.mEdges[0].mDist;
			y	=Vector3.Dot(Point, t.mEdges[2].mNormal) - t.mEdges[2].mDist;
			y1	=Vector3.Dot(p2.mOrigin, t.mEdges[2].mNormal) - t.mEdges[2].mDist;
			x2	=Vector3.Dot(p3.mOrigin, t.mEdges[0].mNormal) - t.mEdges[0].mDist;

			if(Math.Abs(y1) < UtilityLib.Mathery.ON_EPSILON
				|| Math.Abs(x2) < UtilityLib.Mathery.ON_EPSILON)
			{
				color	=bse;
				return;
			}

			color	=bse + d2 * (x / x2);
			color	+=d1 * (y / y1);
		}


		bool Tri_Edge_r(TriEdge e)
		{
			int		i, bestp	=0;
			Vector3	v1, v2;
			Vector3	p0, p1, p;
			float	best, ang;
			Tri		nt;
			TriEdge	e2;

			if(e.mTri != null)
			{
				return	true;
			}

			p0		=mPoints[e.p0].mOrigin;
			p1		=mPoints[e.p1].mOrigin;
			best	=1.1f;
			for(i=0;i < mNumPoints;i++)
			{
				p	=mPoints[i].mOrigin;

				if(Vector3.Dot(p, e.mNormal) - e.mDist < 0.0f)
				{
					continue;
				}

				v1	=p0 - p;
				v2	=p1 - p;

				if(v1.Length() == 0.0f)
				{
					continue;
				}
				if(v2.Length() == 0.0f)
				{
					continue;
				}

				v1.Normalize();
				v2.Normalize();				
				
				ang	=Vector3.Dot(v1, v2);
				if(ang < best)
				{
					best	=ang;
					bestp	=i;
				}
			}
			if(best >= 1)
			{
				return true;
			}
			
			nt	=AllocTriangle();
			if(nt == null)
			{
				Map.Print("Tri_Edge_r:  Could not allocate triangle.\n");
				return	false;
			}
			nt.mEdges[0]	=e;
			if(nt.mEdges[0] == null)
			{
				Map.Print("Tri_Edge_r:  There was an error finding an edge.\n");
				return	false;
			}
			nt.mEdges[1]	=FindEdge(e.p1, bestp);
			if(nt.mEdges[1] == null)
			{
				Map.Print("Tri_Edge_r:  There was an error finding an edge.\n");
				return	false;
			}
			nt.mEdges[2]	=FindEdge(bestp, e.p0);
			if(nt.mEdges[2] == null)
			{
				Map.Print("Tri_Edge_r:  There was an error finding an edge.\n");
				return	false;
			}
			for(i=0;i < 3;i++)
			{
				nt.mEdges[i].mTri	=nt;
			}

			e2	=FindEdge(bestp, e.p1);
			if(e2 == null)
			{
				Map.Print("Tri_Edge_r:  There was an error finding an edge.\n");
				return	false;
			}
			if(!Tri_Edge_r(e2))
			{
				return	false;
			}
			
			e2	=FindEdge(e.p0, bestp);
			if(e2 == null)
			{
				Map.Print("Tri_Edge_r:  There was an error finding an edge.\n");
				return	false;
			}
			if(!Tri_Edge_r(e2))
			{
				return	false;
			}
			return	true;
		}


		TriEdge FindEdge(int p0, int p1)
		{
			TriEdge	e, be;
			Vector3	v1;
			Vector3	normal;
			float	dist;

			if(mEdgeMatrix[p0][p1] != null)
			{
				return	mEdgeMatrix[p0][p1];
			}

			if(mNumEdges > MAX_TRI_EDGES - 2)
			{
				Map.Print("mNumEdges > MAX_TRI_EDGES - 2");
				return	null;
			}

			v1	=mPoints[p1].mOrigin - mPoints[p0].mOrigin;
			v1.Normalize();

			normal	=Vector3.Cross(v1, mPlane.mNormal);
			dist	=Vector3.Dot(mPoints[p0].mOrigin, normal);

			e			=mEdges[mNumEdges];
			e.p0		=p0;
			e.p1		=p1;
			e.mTri		=null;
			e.mNormal	=normal;
			e.mDist		=dist;
			mNumEdges++;
			mEdgeMatrix[p0][p1]	=e;

			//Go ahead and make the reverse edge ahead of time
			be			=mEdges[mNumEdges];
			be.p0		=p1;
			be.p1		=p0;
			be.mTri		=null;
			be.mNormal	=-normal;
			be.mDist	=-dist;
			mNumEdges++;
			mEdgeMatrix[p1][p0]	=be;

			return	e;
		}


		internal bool AddPoint(RADPatch patch)
		{
			int	pnum	=mNumPoints;
			if(pnum == MAX_TRI_POINTS)
			{
				Map.Print("TriPatch->NumPoints == MAX_TRI_POINTS");
				return	false;
			}
			mPoints[pnum]	=patch;
			mNumPoints++;

			return	true;
		}
	}
}
