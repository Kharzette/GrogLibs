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
		Dictionary<UInt64, TriEdge>	mEdgeMatrix	=new Dictionary<UInt64, TriEdge>();

		List<TriEdge>	mEdges		=new List<TriEdge>();
		List<Tri>		mTriList	=new List<Tri>();

		GBSPPlane		mPlane;
		List<RADPatch>	mPoints	=new List<RADPatch>();

		public const float	MIN_MAX_BOUNDS2		=Bounds.MIN_MAX_BOUNDS * 2;


		public TriPatch()
		{
		}


		internal TriPatch(GBSPPlane plane)
		{
			mPlane		=plane;
		}


		Tri	AllocTriangle()
		{
			Tri	ret	=new Tri();
			mTriList.Add(ret);
			return	ret;
		}


		internal bool SampleTriangulation(Vector3 pnt, out Vector3 color)
		{
			if(mPoints.Count == 0)
			{
				color	=Vector3.Zero;
				return	true;
			}
			if(mPoints.Count == 1)
			{
				color	=mPoints[0].mRadFinal;
				return	true;
			}
			
			//See of the Point is inside a tri in the patch
			foreach(Tri t in mTriList)
			{
				if(!t.IsPointInside(pnt))
				{
					continue;
				}
				LerpTriangle(t, pnt, out color);

				return	true;
			}

			foreach(TriEdge e in mEdges)
			{
				if(e.mTri != null)
				{
					continue;		// not an exterior edge
				}

				float	d	=Vector3.Dot(pnt, e.mNormal) - e.mDist;
				if(d < 0)
				{
					continue;	// not in front of edge
				}

				RADPatch	p0	=mPoints[e.p0];
				RADPatch	p1	=mPoints[e.p1];

				Vector3	v1	=p1.GetOrigin() - p0.GetOrigin();
				v1.Normalize();

				Vector3	v2	=pnt - p0.GetOrigin();
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
			
			if(!FindClosestTriPoint(pnt, out color))
			{
				Map.Print("SampleTriangulation:  Could not find closest Color.\n");
				return	false;
			}
			return	true;
		}


		bool FindClosestTriPoint(Vector3 pnt, out Vector3 col)
		{
			col	=Vector3.Zero;

			//Search for nearest Point
			float		bestDist	=TriPatch.MIN_MAX_BOUNDS2;
			RADPatch	bestPatch	=null;

			foreach(RADPatch p0 in mPoints)
			{
				Vector3	v1	=pnt - p0.GetOrigin();
				float	d	=v1.Length();
				if(d < bestDist)
				{
					bestDist	=d;
					bestPatch	=p0;
				}
			}
			if(bestPatch == null)
			{
				Map.Print("FindClosestTriPoint: No Points.\n");
				return	false;
			}
			col	=bestPatch.mRadFinal;
			return	true;
		}


		internal bool TriangulatePoints()
		{
			//zero out edgematrix
			mEdgeMatrix.Clear();

			if(mPoints.Count < 2)
			{
				return	true;
			}

			//Find the two closest Points
			float	bestd	=MIN_MAX_BOUNDS2;
			int		bp1		=0;
			int		bp2		=0;
			for(int i=0;i < mPoints.Count;i++)
			{
				Vector3	p1	=mPoints[i].GetOrigin();
				for(int j=i+1;j < mPoints.Count;j++)
				{
					Vector3	p2	=mPoints[j].GetOrigin();
					Vector3	v1	=p2 - p1;
					float	d	=v1.Length();
					if(d < bestd && d > .05f)
					{
						bestd	=d;
						bp1		=i;
						bp2		=j;
					}
				}
			}

			TriEdge	e	=FindEdge(bp1, bp2);
			if(e == null)
			{
				Map.Print("There was an error finding an edge.\n");
				return	false;
			}
			TriEdge	e2	=FindEdge(bp2, bp1);
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


		void LerpTriangle(Tri t, Vector3 pnt, out Vector3 color)
		{
			RADPatch	p1	=mPoints[t.mEdges[0].p0];
			RADPatch	p2	=mPoints[t.mEdges[1].p0];
			RADPatch	p3	=mPoints[t.mEdges[2].p0];

			Vector3	bse	=p1.mRadFinal;
			Vector3	d1	=p2.mRadFinal - bse;
			Vector3	d2	=p3.mRadFinal - bse;

			float	x	=Vector3.Dot(pnt, t.mEdges[0].mNormal) - t.mEdges[0].mDist;
			float	y	=Vector3.Dot(pnt, t.mEdges[2].mNormal) - t.mEdges[2].mDist;
			float	y1	=Vector3.Dot(p2.GetOrigin(), t.mEdges[2].mNormal) - t.mEdges[2].mDist;
			float	x2	=Vector3.Dot(p3.GetOrigin(), t.mEdges[0].mNormal) - t.mEdges[0].mDist;

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
			if(e.mTri != null)
			{
				return	true;
			}

			Vector3	p0		=mPoints[e.p0].GetOrigin();
			Vector3	p1		=mPoints[e.p1].GetOrigin();
			float	best	=1.1f;
			int		bestp	=0;
			for(int i=0;i < mPoints.Count;i++)
			{
				Vector3	p	=mPoints[i].GetOrigin();

				if(Vector3.Dot(p, e.mNormal) - e.mDist < 0.0f)
				{
					continue;
				}

				Vector3	v1	=p0 - p;
				Vector3	v2	=p1 - p;

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
				
				float	ang	=Vector3.Dot(v1, v2);
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
			
			Tri	nt	=AllocTriangle();
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
			for(int i=0;i < 3;i++)
			{
				nt.mEdges[i].mTri	=nt;
			}

			TriEdge	e2	=FindEdge(bestp, e.p1);
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


		void AddTriEdgeToMatrix(int idx1, int idx2, TriEdge edge)
		{
			UInt64	key	=((UInt64)idx1 << 32);
			key	|=((UInt32)idx2);

			if(mEdgeMatrix.ContainsKey(key))
			{
				Map.Print("Tri edge matrix already contains key " + key + " !\n");
				return;
			}

			mEdgeMatrix.Add(key, edge);
		}


		bool EdgeMatrixContainsKey(int idx1, int idx2)
		{
			UInt64	key	=((UInt64)idx1 << 32);
			key	|=((UInt32)idx2);

			return	mEdgeMatrix.ContainsKey(key);
		}


		TriEdge GetEdgeMatrixValue(int idx1, int idx2)
		{
			UInt64	key	=((UInt64)idx1 << 32);
			key	|=((UInt32)idx2);

			return	mEdgeMatrix[key];
		}


		TriEdge FindEdge(int p0, int p1)
		{
			if(EdgeMatrixContainsKey(p0, p1))
			{
				return	GetEdgeMatrixValue(p0, p1);
			}

			Vector3	v1	=mPoints[p1].GetOrigin() - mPoints[p0].GetOrigin();
			v1.Normalize();

			Vector3	normal	=Vector3.Cross(v1, mPlane.mNormal);
			float	dist	=Vector3.Dot(mPoints[p0].GetOrigin(), normal);

			TriEdge	e	=new TriEdge();
			e.p0		=p0;
			e.p1		=p1;
			e.mTri		=null;
			e.mNormal	=normal;
			e.mDist		=dist;
			AddTriEdgeToMatrix(p0, p1, e);
			mEdges.Add(e);

			//Go ahead and make the reverse edge ahead of time
			TriEdge	be	=new TriEdge();
			be.p0		=p1;
			be.p1		=p0;
			be.mTri		=null;
			be.mNormal	=-normal;
			be.mDist	=-dist;
			AddTriEdgeToMatrix(p1, p0, be);
			mEdges.Add(be);

			return	e;
		}


		internal bool AddPoint(RADPatch patch)
		{
			mPoints.Add(patch);
			return	true;
		}
	}
}
