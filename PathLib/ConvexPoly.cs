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


		internal ConvexPoly(List<Vector3> verts)
		{
			mVerts.AddRange(verts);
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

		internal Edge GetSharedEdge(ConvexPoly other)
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

				dotSum	+=(float)Math.Acos(dot);
			}
			return	dotSum;
		}
	}
}
