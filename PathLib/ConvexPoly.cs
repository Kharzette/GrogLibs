using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using BSPZone;
using UtilityLib;


namespace PathLib
{
	public class ConvexPoly
	{
		internal class Edge
		{
			internal Vector3	mA, mB;

			const float	OverlapEpsilon	=16f;	//need to overlap by this much

			internal bool IsColinear(Edge other)
			{
				Vector3	edgeVec1	=mA - mB;
				Vector3	edgeVec2	=other.mA - other.mB;

				Vector3	testVec	=Vector3.Cross(edgeVec1, edgeVec2);
				return	testVec.Equals(Vector3.Zero);
			}

			internal bool AlmostEqual(Edge other)
			{
				if(Mathery.CompareVector(mA, other.mA)
					&& Mathery.CompareVector(mB, other.mB))
				{
					return	true;
				}
				if(Mathery.CompareVector(mB, other.mA)
					&& Mathery.CompareVector(mA, other.mB))
				{
					return	true;
				}
				return	false;
			}
		}

		public List<Vector3>	mVerts	=new List<Vector3>();
		public ZonePlane		mPlane;


		public ConvexPoly(List<Vector3> verts, ZonePlane plane)
		{
			mVerts.AddRange(verts);
			mPlane	=plane;
		}

		internal BoundingBox GetBounds()
		{
			return	BoundingBox.CreateFromPoints(mVerts);
		}

		List<Edge> GetEdges()
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

		internal bool IsAdjacent(ConvexPoly other)
		{
			List<Edge>	myEdges		=GetEdges();
			List<Edge>	otherEdges	=other.GetEdges();

			foreach(Edge me in myEdges)
			{
				foreach(Edge oe in otherEdges)
				{
					if(!me.IsColinear(oe))
					{
						continue;
					}
					if(me.AlmostEqual(oe))
					{
						return	true;
					}
				}
			}
			return	false;
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
	}
}
