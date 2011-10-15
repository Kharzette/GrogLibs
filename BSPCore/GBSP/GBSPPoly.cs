using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPCore
{
	internal class GBSPPoly
	{
		List<Vector3>	mVerts	=new List<Vector3>();

		internal const float	EDGE_LENGTH			=0.1f;
		internal const float	DEGENERATE_EPSILON	=0.001f;
		internal const float	COLINEAR_EPSILON	=0.0001f;


		internal GBSPPoly() { }
		internal GBSPPoly(GFXFace f, Int32 []vertInds, Vector3 []verts)
		{
			for(int i=0;i < f.mNumVerts;i++)
			{
				int	ind	=vertInds[i + f.mFirstVert];

				mVerts.Add(verts[ind]);
			}
			RemoveDegenerateEdges();			
		}


		internal GBSPPoly(GBSPPoly copyMe)
		{
			mVerts.Clear();
			foreach(Vector3 vert in copyMe.mVerts)
			{
				mVerts.Add(vert);
			}
		}


		internal GBSPPoly(GBSPPlane p)
		{
			Vector3	rightVec, upVec, org, vert;

			if(!TextureAxisFromPlane(p, out rightVec, out upVec))
			{
				mVerts.Clear();
			}

			Vector3.Cross(ref p.mNormal, ref upVec, out rightVec);
			Vector3.Cross(ref p.mNormal, ref rightVec, out upVec);

			upVec.Normalize();
			rightVec.Normalize();

			org			=p.mDist * p.mNormal;
			upVec		*=Bounds.MIN_MAX_BOUNDS;
			rightVec	*=Bounds.MIN_MAX_BOUNDS;

			vert	=org - rightVec;
			vert	+=upVec;

			mVerts.Add(vert);

			vert	=org + rightVec;
			vert	+=upVec;

			mVerts.Add(vert);

			vert	=org + rightVec;
			vert	-=upVec;

			mVerts.Add(vert);

			vert	=org - rightVec;
			vert	-=upVec;

			mVerts.Add(vert);
		}


		internal int VertCount()
		{
			return	mVerts.Count;
		}


		internal static bool TextureAxisFromPlane(GBSPPlane pln, out Vector3 xv, out Vector3 yv)
		{
			Int32	bestAxis;
			float	dot, best;
			
			best		=0.0f;
			bestAxis	=-1;

			xv	=Vector3.Zero;
			yv	=Vector3.Zero;
			
			for(int i=0;i < 3;i++)
			{
				dot	=Math.Abs(Utility64.Mathery.VecIdx(pln.mNormal, i));
				if(dot > best)
				{
					best		=dot;
					bestAxis	=i;
				}
			}

			switch(bestAxis)
			{
				case 0:						// X
					xv.X	=0.0f;
					xv.Y	=0.0f;
					xv.Z	=1.0f;

					yv.X	=0.0f;
					yv.Y	=-1.0f;
					yv.Z	=0.0f;
					break;
				case 1:						// Y
					xv.X	=1.0f;
					xv.Y	=0.0f;
					xv.Z	=0.0f;

					yv.X	=0.0f;
					yv.Y	=0.0f;
					yv.Z	=1.0f;
					break;
				case 2:						// Z
					xv.X	=1.0f;
					xv.Y	=0.0f;
					xv.Z	=0.0f;

					yv.X	=0.0f;
					yv.Y	=-1.0f;
					yv.Z	=0.0f;
					break;
				default:
					xv.X	=0.0f;
					xv.Y	=0.0f;
					xv.Z	=1.0f;

					yv.X	=0.0f;
					yv.Y	=-1.0f;
					yv.Z	=0.0f;
					Map.Print("GetTextureAxis: No Axis found.");
					return false;
			}
			return	true;
		}


		internal bool ClipPoly(GBSPPlane plane, bool bFlip)
		{
			return	ClipPolyEpsilon(Utility64.Mathery.ON_EPSILON, plane, bFlip);
		}


		internal bool ClipPolyEpsilon(float epsilon, GBSPPlane plane, bool flipTest)
		{
			if(mVerts.Count > 100)
			{
				Map.Print("ClipPoly:  Too many verts.\n");
				return	false;
			}

			Vector3	normal	=plane.mNormal;
			float	dist	=plane.mDist;

			if(flipTest)
			{
				normal	=-normal;
				dist	=-dist;
			}

			float	[]VDist			=new float[mVerts.Count];
			Int32	[]VSides		=new int[mVerts.Count];
			Int32	[]countSides	=new int[3];

			List<Vector3>	frontVerts	=new List<Vector3>();

			for(int i=0;i < mVerts.Count;i++)
			{
				VDist[i]	=Vector3.Dot(mVerts[i], normal) - dist;
				if(VDist[i] > epsilon)
				{
					VSides[i]	=0;
				}
				else if(VDist[i] < -epsilon)
				{
					VSides[i]	=1;
				}
				else
				{
					VSides[i]	=2;
				}

				countSides[VSides[i]]++;
			}

			if(countSides[0] == 0)
			{
				mVerts.Clear();
				return	true;
			}
			if(countSides[1] == 0)
			{
				return	true;
			}

			for(int i=0;i < mVerts.Count;i++)
			{
				Vector3	vert1	=mVerts[i];

				if(VSides[i] == 2)
				{
					frontVerts.Add(vert1);
					continue;
				}

				if(VSides[i] == 0)
				{
					frontVerts.Add(vert1);
				}

				int	nextVert	=(i + 1) % mVerts.Count;

				if(VSides[nextVert] == 2 || VSides[nextVert] == VSides[i])
				{
					continue;
				}

				Vector3	vert2	=mVerts[nextVert];
				float	scale	=VDist[i] / (VDist[i] - VDist[nextVert]);

				frontVerts.Add(vert1 + (vert2 - vert1) * scale);
			}

			mVerts.Clear();
			mVerts	=frontVerts;

			if(mVerts.Count < 3)
			{
				mVerts.Clear();
				return	false;
			}

			return	true;
		}


		internal bool IsTiny()
		{
			if(mVerts.Count < 3)
			{
				return	true;
			}

			int	edges	=0;

			for(int i=0;i < mVerts.Count;i++)
			{
				int	j	=(i == mVerts.Count - 1) ? 0 : i + 1;

				Vector3	delta	=mVerts[j] - mVerts[i];

				float	len	=delta.Length();

				if(len > EDGE_LENGTH)
				{
					if(++edges == 3)
					{
						return	false;
					}
				}
			}

			return	true;
		}


		internal bool Split(GBSPPlane plane, out GBSPPoly polyFront,
							out GBSPPoly polyBack, bool flipTest)
		{
			return	SplitEpsilon(Utility64.Mathery.ON_EPSILON, plane,
						out polyFront, out polyBack, flipTest);
		}


		internal bool SplitEpsilon(float epsilon, GBSPPlane plane,
			out GBSPPoly polyFront, out GBSPPoly polyBack, bool flipTest)
		{
			polyFront	=null;
			polyBack	=null;

			if(mVerts.Count > 100)
			{
				Map.Print("ClipPoly:  Too many verts.\n");
				return	false;
			}

			Vector3	normal	=plane.mNormal;
			float	dist	=plane.mDist;

			if(flipTest)
			{
				normal	=-normal;
				dist	=-dist;
			}

			float	[]VDist			=new float[mVerts.Count];
			Int32	[]VSides		=new int[mVerts.Count];
			Int32	[]countSides	=new int[3];

			List<Vector3>	frontVerts	=new List<Vector3>();
			List<Vector3>	backVerts	=new List<Vector3>();

			for(int i=0;i < mVerts.Count;i++)
			{
				VDist[i]	=Vector3.Dot(mVerts[i], normal) - dist;
				if(VDist[i] > epsilon)
				{
					VSides[i]	=0;
				}
				else if(VDist[i] < -epsilon)
				{
					VSides[i]	=1;
				}
				else
				{
					VSides[i]	=2;
				}

				countSides[VSides[i]]++;
			}

			if(countSides[0] == 0)
			{
				polyBack	=new GBSPPoly(this);
				return	true;
			}
			if(countSides[1] == 0)
			{
				polyFront	=new GBSPPoly(this);
				return	true;
			}

			for(int i=0;i < mVerts.Count;i++)
			{
				Vector3	vert1	=mVerts[i];

				if(VSides[i] == 2)
				{
					frontVerts.Add(vert1);
					backVerts.Add(vert1);
					continue;
				}

				if(VSides[i] == 0)
				{
					frontVerts.Add(vert1);
				}
				else if(VSides[i] == 1)
				{
					backVerts.Add(vert1);
				}

				int	nextVert	=(i + 1) % mVerts.Count;

				if(VSides[nextVert] == 2 || VSides[nextVert] == VSides[i])
				{
					continue;
				}

				Vector3	vert2	=mVerts[nextVert];
				float	scale	=VDist[i] / (VDist[i] - VDist[nextVert]);

				Vector3	splitVert	=vert1 + (vert2 - vert1) * scale;

				frontVerts.Add(splitVert);
				backVerts.Add(splitVert);
			}

			if(frontVerts.Count < 3)
			{
				return	false;
			}
			else
			{
				polyFront	=new GBSPPoly();
				polyFront.mVerts	=frontVerts;
			}

			if(backVerts.Count < 3)
			{
				return	false;
			}
			else
			{
				polyBack	=new GBSPPoly();
				polyBack.mVerts	=backVerts;
			}

			return	true;
		}


		internal void RemoveDegenerateEdges()
		{
			bool	Bad	=false;

			List<Vector3>	newVerts	=new List<Vector3>();

			for(int i=0;i < mVerts.Count;i++)
			{
				Vector3	V1	=mVerts[i];
				Vector3	V2	=mVerts[(i + 1) % mVerts.Count];

				Vector3	Vec	=V1 - V2;

				if(Vec.Length() > DEGENERATE_EPSILON)
				{
					newVerts.Add(V1);
				}
				else
				{
					Bad	=true;
				}
			}

			if(Bad)
			{
				mVerts	=newVerts;
			}
		}


		internal bool EdgeExist(Vector3 []edge1, out Int32 []edgeIndexOut)
		{
			Int32		i;
			Vector3		[]edge2	=new Vector3[2];

			edgeIndexOut	=new int[2];

			for(i=0;i < mVerts.Count;i++)
			{
				edge2[0]	=mVerts[i];
				edge2[1]	=mVerts[(i + 1) % mVerts.Count];

				if(Utility64.Mathery.CompareVector(edge1[0], edge2[0]))
				{
					if(Utility64.Mathery.CompareVector(edge1[1], edge2[1]))
					{
						edgeIndexOut[0]	=i;
						edgeIndexOut[1]	=(i + 1) % mVerts.Count;
						return	true;
					}
				}
			}
			return	false;
		}


		internal void Reverse()
		{
			mVerts.Reverse();
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


		internal void GetTriangles(List<Vector3> verts, List<uint> indexes, bool bCheckFlags)
		{
			int	ofs		=verts.Count;

			UInt32	offset	=(UInt32)ofs;

			//triangulate the brush face points
			foreach(Vector3 pos in mVerts)
			{
				verts.Add(pos);
			}

			int i	=0;
			for(i=1;i < mVerts.Count-1;i++)
			{
				//initial vertex
				indexes.Add(offset);
				indexes.Add((UInt32)(offset + i));
				indexes.Add((UInt32)(offset + ((i + 1) % mVerts.Count)));
			}
		}


		internal void GetLines(List<Vector3> verts, List<uint> indexes, bool bCheckFlags)
		{
			int	ofs		=verts.Count;

			UInt32	offset	=(UInt32)ofs;

			//triangulate the brush face points
			foreach(Vector3 pos in mVerts)
			{
				verts.Add(pos);
			}

			for(int i=0;i < mVerts.Count;i++)
			{
				//initial vertex
				indexes.Add((UInt32)(offset + i));
				indexes.Add((UInt32)(offset + ((i + 1) % mVerts.Count)));
			}
		}


		static internal GBSPPoly Merge(GBSPPoly p1, GBSPPoly p2, Vector3 normal, PlanePool pool)
		{
			bool		keep1	=true, keep2	=true;

			if(p1.mVerts.Count == -1 || p2.mVerts.Count == -1)
			{
				return	null;
			}

			int	numVerts	=p1.mVerts.Count;

			//
			// Go through each edge of p1, and see if the reverse of it exist in p2
			//
			Int32		[]edgeIndex	=new Int32[2];
			Vector3		[]edge1		=new Vector3[2];
			int			i;
			for(i=0;i < numVerts;i++)		
			{
				edge1[1]	=p1.mVerts[i];
				edge1[0]	=p1.mVerts[(i + 1) % numVerts];

				if(p2.EdgeExist(edge1, out edgeIndex))
				{
					break;
				}
			}

			if(i >= numVerts)							// Did'nt find an edge, return nothing
			{
				return	null;
			}

			int	numVerts2	=p2.mVerts.Count;

			//
			//	See if the 2 joined make a convex poly, connect them, and return new one
			//

			//Get the normal of the edge just behind edge1
			Vector3	v1	=p1.mVerts[(i + numVerts - 1) % numVerts];
			v1	-=edge1[1];

			Vector3	normal2	=Vector3.Cross(normal, v1);
			normal2.Normalize();

			Vector3	v2	=p2.mVerts[(edgeIndex[1] + 1) % numVerts2] - p2.mVerts[edgeIndex[1]];

			float	dot		=Vector3.Dot(v2, normal2);
			if(dot > COLINEAR_EPSILON)
			{
				return null;			//Edge makes a non-convex poly
			}
			if(dot >= -COLINEAR_EPSILON)	//Drop point, on colinear edge
			{
				keep1	=false;
			}

			//Get the normal of the edge just behind edge1
			v1	=p1.mVerts[(i+2)%numVerts];
			v1	-=edge1[0];

			normal2	=Vector3.Cross(normal, v1);
			normal2.Normalize();

			v2	=p2.mVerts[(edgeIndex[0] + numVerts2 - 1) % numVerts2] -
						p2.mVerts[edgeIndex[0]];

			dot	=Vector3.Dot(v2, normal2);
			if(dot > COLINEAR_EPSILON)
			{
				return	null;	//Edge makes a non-convex poly
			}
			if(dot >= -COLINEAR_EPSILON)	//Drop point, on colinear edge
			{
				keep2	=false;
			}
			
			//
			// Make a new poly, free the old ones...
			//
			GBSPPoly	ret	=new GBSPPoly();

			int	numNewVerts	=0;
			for(int k = (i + 1) % numVerts;k != i;k = (k + 1) % numVerts)
			{
				if(k == (i + 1) % numVerts && !keep2)
				{
					continue;
				}
				ret.mVerts.Add(p1.mVerts[k]);
				numNewVerts++;
			}

			i	=edgeIndex[0];

			for(int k = (i + 1) % numVerts2;k != i;k = (k + 1) % numVerts2)
			{
				if(k == (i + 1) % numVerts2 && !keep1)
				{
					continue;
				}
				ret.mVerts.Add(p2.mVerts[k]);
				numNewVerts++;
			}
			return	ret;
		}


		internal float Radius()
		{
			Vector3	center	=Center();

			float	bestDist	=0.0f;
			foreach(Vector3	vert in mVerts)
			{
				Vector3	toCent	=vert - center;
				float	dist	=toCent.Length();
				if(dist > bestDist)
				{
					bestDist	=dist;
				}
			}
			return	bestDist;
		}


		internal GBSPPlane GenPlane()
		{
			GBSPPlane	ret	=new GBSPPlane(mVerts);

			return	ret;
		}


		internal bool AnyPartBehind(GBSPPlane p)
		{
			foreach(Vector3 vert in mVerts)
			{
				float	d	=Vector3.Dot(p.mNormal, vert) - p.mDist;
				if(d < -Utility64.Mathery.ON_EPSILON)
				{
					return	true;
				}
			}
			return	false;
		}


		internal Vector3 Center()
		{
			Vector3	ret	=Vector3.Zero;

			foreach(Vector3 vert in mVerts)
			{
				ret	+=vert;
			}
			ret	/=mVerts.Count;

			return	ret;
		}


		internal bool AnyPartInFront(GBSPPlane p)
		{
			foreach(Vector3 vert in mVerts)
			{
				float	d	=Vector3.Dot(p.mNormal, vert) - p.mDist;
				if(d > Utility64.Mathery.ON_EPSILON)
				{
					return	true;
				}
			}
			return	false;
		}


		internal void AddToBounds(Bounds bnd)
		{
			foreach(Vector3 pnt in mVerts)
			{
				bnd.AddPointToBounds(pnt);
			}
		}


		internal void AddVert(Vector3 v)
		{
			mVerts.Add(v);
		}


		internal void Free()
		{
			mVerts.Clear();
			mVerts	=null;
		}


		internal bool Check(bool bVerb, Vector3 norm, float dist)
		{
			int	i;
			for(i=0;i < mVerts.Count;i++)
			{
				Vector3	v1	=mVerts[i];
				Vector3	v2	=mVerts[(i + 1) % mVerts.Count];

				//Check for degenreate edge
				Vector3	vect1	=v2 - v1;
				float	d		=vect1.Length();
				if(Math.Abs(d) < DEGENERATE_EPSILON)
				{
					if(bVerb)
					{
						Map.Print("WARNING CheckFace:  Degenerate Edge.\n");
					}
					return	false;
				}

				//Check for planar
				d	=Vector3.Dot(v1, norm) - dist;
				if(d > Utility64.Mathery.ON_EPSILON
					|| d < -Utility64.Mathery.ON_EPSILON)
				{
					if(bVerb)
					{
						Map.Print("WARNING CheckFace:  Non planar: " + d + "\n");
					}
					return	false;
				}

				Vector3	edgeNorm	=Vector3.Cross(norm, vect1);
				edgeNorm.Normalize();
				float	edgeDist	=Vector3.Dot(v1, edgeNorm);
				
				//Check for convexity
				for(int j=0;j < mVerts.Count;j++)
				{
					d	=Vector3.Dot(mVerts[j], edgeNorm) - edgeDist;
					if(d > Utility64.Mathery.ON_EPSILON)
					{
						if(bVerb)
						{
							Map.Print("CheckFace:  Face not convex.\n");
						}
						return	false;
					}
				}
			}
			return	true;
		}


		internal UInt32 GetMaxDistance(GBSPPlane plane, ref float max)
		{
			UInt32	side	=GBSPPlane.PSIDE_FRONT;
			foreach(Vector3 vert in mVerts)
			{
				float	d	=Vector3.Dot(vert, plane.mNormal) - plane.mDist;
				if(d > max)
				{
					max		=d;
					side	=GBSPPlane.PSIDE_FRONT;
				}
				if(-d > max)
				{
					max		=-d;
					side	=GBSPPlane.PSIDE_BACK;
				}
			}
			return	side;
		}


		//returns dist from plane to vert zero for volume
		internal float GetCornerDistance(GBSPPlane plane)
		{
			return	Vector3.Dot(plane.mNormal, mVerts[0]) - plane.mDist;
		}


		internal void SplitSideTest(GBSPPlane plane,
									out int front, out int back,
									ref float frontDist, ref float backDist)
		{
			front	=back	=0;
			foreach(Vector3 vert in mVerts)
			{
				float d	=plane.DistanceFast(vert);

				if(d > frontDist)
				{
					frontDist	=d;
				}
				else if(d < backDist)
				{
					backDist	=d;
				}

				if(d > 0.1f)
				{
					front	=1;
				}
				else if(d < -0.1)
				{
					back	=1;
				}
			}
		}


		internal void GetSplitMaxDist(GBSPPlane plane, int pside,
			ref float frontDist, ref float backDist)
		{
			foreach(Vector3 vert in mVerts)
			{
				float	d	=plane.DistanceFast(vert);

				if(pside != 0)
				{
					d	=-d;
				}

				if(d > frontDist)
				{
					frontDist	=d;
				}
				else if(d < backDist)
				{
					backDist	=d;
				}
			}
		}


		internal bool SeperatorClip(GBSPPoly source, GBSPPoly pass,
									bool bFlipClip, ref GBSPPoly dest)
		{
			for(int i=0;i < source.mVerts.Count;i++)
			{
				int	l	=(i + 1) % source.mVerts.Count;

				Vector3	v1	=source.mVerts[l] - source.mVerts[i];

				for(int j=0;j < pass.mVerts.Count;j++)
				{
					Vector3	v2	=pass.mVerts[j] - source.mVerts[i];

					GBSPPlane	plane		=new GBSPPlane();
					plane.mNormal	=Vector3.Cross(v1, v2);

					float	len	=plane.mNormal.Length();
					plane.mNormal.Normalize();
					
					if(len < Utility64.Mathery.ON_EPSILON)
					{
						continue;
					}
					
					plane.mDist	=Vector3.Dot(pass.mVerts[j], plane.mNormal);						

					bool	bFlipTest	=false;
					int		k;
					for(k=0;k < source.mVerts.Count;k++)
					{
						if(k == i || k == l)
						{
							continue;
						}

						float	d	=Vector3.Dot(source.mVerts[k], plane.mNormal) - plane.mDist;
						if(d < -Utility64.Mathery.ON_EPSILON)
						{
							bFlipTest	=false;
							break;
						}
						else if(d > Utility64.Mathery.ON_EPSILON)
						{
							bFlipTest	=true;
							break;
						}
					}
					if(k == source.mVerts.Count)
					{
						continue;
					}
					if(bFlipTest)
					{
						plane.Inverse();
					}

					Int32	[]counts	=new Int32[3];
					counts[0] = counts[1] = counts[2] = 0;

					for(k=0;k < pass.mVerts.Count;k++)
					{
						if(k==j)
						{
							continue;
						}
						float	d	=Vector3.Dot(pass.mVerts[k], plane.mNormal) - plane.mDist;
						if(d < -Utility64.Mathery.ON_EPSILON)
						{
							break;
						}
						else if(d > Utility64.Mathery.ON_EPSILON)
						{
							counts[0]++;
						}
						else
						{
							counts[2]++;
						}
					}
					if(k != pass.mVerts.Count)
					{
						continue;	
					}
						
					if(counts[0] == 0)
					{
						continue;
					}
					if(!ClipPoly(plane, bFlipClip))
					{
						Map.Print("ClipToPortals:  Error clipping portal.\n");
						return	false;
					}

					if(mVerts.Count < 3)
					{
						dest	=null;
						return	true;
					}
				}
			}			
			dest	=this;
			return	true;
		}


		internal bool IsMaxExtents()
		{
			for(int i=0;i < mVerts.Count;i++)
			{
				for(int k=0;k < 3;k++)
				{
					float	val	=Utility64.Mathery.VecIdx(mVerts[i], k);

					if(val == Bounds.MIN_MAX_BOUNDS)
					{
						return	true;
					}
					if(val == -Bounds.MIN_MAX_BOUNDS)
					{
						return	true;
					}
				}
			}
			return	false;
		}


		internal void WriteReverse(System.IO.BinaryWriter bw)
		{
			bw.Write(mVerts.Count);
			for(int i=mVerts.Count - 1;i >=0;i--)
			{
				bw.Write(mVerts[i].X);
				bw.Write(mVerts[i].Y);
				bw.Write(mVerts[i].Z);
			}
		}


		internal void Write(System.IO.BinaryWriter bw)
		{
			bw.Write(mVerts.Count);
			foreach(Vector3 vert in mVerts)
			{
				bw.Write(vert.X);
				bw.Write(vert.Y);
				bw.Write(vert.Z);
			}
		}


		internal void Read(System.IO.BinaryReader br)
		{
			int	count	=br.ReadInt32();
			for(int i=0;i < count;i++)
			{
				Vector3	vert	=Vector3.Zero;

				vert.X	=br.ReadSingle();
				vert.Y	=br.ReadSingle();
				vert.Z	=br.ReadSingle();

				mVerts.Add(vert);
			}
		}
	}
}
