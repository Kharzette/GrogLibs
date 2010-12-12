using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class GBSPPoly
	{
		List<Vector3>	mVerts	=new List<Vector3>();

		public const float	EDGE_LENGTH			=0.1f;
		public const float	DEGENERATE_EPSILON	=0.001f;
		public const float	COLINEAR_EPSILON	=0.0001f;


		public GBSPPoly() { }
		internal GBSPPoly(GFXFace Face, Int32 []vertInds, Vector3 []verts)
		{
			for(int i=0;i < Face.mNumVerts;i++)
			{
				int	ind	=vertInds[i + Face.mFirstVert];

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


		public GBSPPoly(GBSPPlane p)
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


		internal static bool TextureAxisFromPlane(GBSPPlane pln, out Vector3 Xv, out Vector3 Yv)
		{
			Int32	BestAxis;
			float	Dot,Best;
			
			Best		=0.0f;
			BestAxis	=-1;

			Xv	=Vector3.Zero;
			Yv	=Vector3.Zero;
			
			for(int i=0;i < 3;i++)
			{
				Dot	=Math.Abs(UtilityLib.Mathery.VecIdx(pln.mNormal, i));
				if(Dot > Best)
				{
					Best		=Dot;
					BestAxis	=i;
				}
			}

			switch(BestAxis)
			{
				case 0:						// X
					Xv.X	=0;
					Xv.Y	=0;
					Xv.Z	=1;

					Yv.X	=0;
					Yv.Y	=-1;
					Yv.Z	=0;
					break;
				case 1:						// Y
					Xv.X	=1;
					Xv.Y	=0;
					Xv.Z	=0;

					Yv.X	=0;
					Yv.Y	=0;
					Yv.Z	=1;
					break;
				case 2:						// Z
					Xv.X	=1;
					Xv.Y	=0;
					Xv.Z	=0;

					Yv.X	=0;
					Yv.Y	=-1;
					Yv.Z	=0;
					break;
				default:
					Map.Print("TextureAxisFromPlane: No Axis found.\n");
					return	false;
			}

			return	true;
		}


		internal bool ClipPoly(GBSPPlane plane, bool bFlip)
		{
			return	ClipPolyEpsilon(UtilityLib.Mathery.ON_EPSILON, plane, bFlip);
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


		internal bool Split(GBSPPlane plane, out GBSPPoly polyFront, out GBSPPoly polyBack, bool flipTest)
		{
			return	SplitEpsilon(UtilityLib.Mathery.ON_EPSILON, plane, out polyFront, out polyBack, flipTest);
		}


		internal bool SplitEpsilon(float epsilon, GBSPPlane plane, out GBSPPoly polyFront, out GBSPPoly polyBack, bool flipTest)
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


		internal bool EdgeExist(Vector3 []Edge1, out Int32 []EdgeIndexOut)
		{
			Int32		i;
			Vector3		[]Edge2	=new Vector3[2];

			EdgeIndexOut	=new int[2];

			for(i=0;i < mVerts.Count;i++)
			{
				Edge2[0]	=mVerts[i];
				Edge2[1]	=mVerts[(i + 1) % mVerts.Count];

				if(UtilityLib.Mathery.CompareVector(Edge1[0], Edge2[0]))
				{
					if(UtilityLib.Mathery.CompareVector(Edge1[1], Edge2[1]))
					{
						EdgeIndexOut[0]	=i;
						EdgeIndexOut[1]	=(i + 1) % mVerts.Count;
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


		static internal GBSPPoly Merge(GBSPPoly p1, GBSPPoly p2, Vector3 normal, PlanePool pool)
		{
			Vector3		[]edge1	=new Vector3[2];
			Int32		i, numVerts, numVerts2;
			Int32		[]edgeIndex	=new Int32[2];
			Int32		numNewVerts;
			Vector3		normal2, v1, v2;
			float		dot;
			bool		keep1	=true, keep2	=true;

			if(p1.mVerts.Count == -1 || p2.mVerts.Count == -1)
			{
				return	null;
			}

			numVerts	=p1.mVerts.Count;

			//
			// Go through each edge of p1, and see if the reverse of it exist in p2
			//
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

			numVerts2	=p2.mVerts.Count;

			//
			//	See if the 2 joined make a convex poly, connect them, and return new one
			//

			//Get the normal of the edge just behind edge1
			v1	=p1.mVerts[(i + numVerts - 1) % numVerts];
			v1	-=edge1[1];

			normal2	=Vector3.Cross(normal, v1);
			normal2.Normalize();

			v2	=p2.mVerts[(edgeIndex[1] + 1) % numVerts2] - p2.mVerts[edgeIndex[1]];

			dot		=Vector3.Dot(v2, normal2);
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
			numNewVerts	=0;

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
			GBSPPlane	ret;
			int			i;

			ret.mNormal	=Vector3.Zero;

			//catches colinear points now
			for(i=0;i < mVerts.Count;i++)
			{
				//gen a plane normal from the cross of edge vectors
				Vector3	v1  =mVerts[i] - mVerts[(i + 1) % mVerts.Count];
				Vector3	v2  =mVerts[(i + 2) % mVerts.Count] - mVerts[(i + 1) % mVerts.Count];

				ret.mNormal   =Vector3.Cross(v1, v2);

				if(!ret.mNormal.Equals(Vector3.Zero))
				{
					break;
				}
				//try the next three if there are three
			}
			if(i >= mVerts.Count)
			{
				//need a talky flag
				//in some cases this isn't worthy of a warning
				Map.Print("Face with no normal!");
				ret.mNormal	=Vector3.UnitX;
				ret.mDist	=0.0f;
				ret.mType	=GBSPPlane.PLANE_ANY;
				return	ret;
			}

			ret.mNormal.Normalize();
			ret.mDist	=Vector3.Dot(mVerts[1], ret.mNormal);
			ret.mType	=GBSPPlane.GetPlaneType(ret.mNormal);

			return	ret;
		}


		internal bool AnyPartBehind(GBSPPlane p)
		{
			foreach(Vector3 vert in mVerts)
			{
				float	d	=Vector3.Dot(p.mNormal, vert) - p.mDist;
				if(d < -UtilityLib.Mathery.ON_EPSILON)
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
				if(d > UtilityLib.Mathery.ON_EPSILON)
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
				if(d > UtilityLib.Mathery.ON_EPSILON
					|| d < -UtilityLib.Mathery.ON_EPSILON)
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
					if(d > UtilityLib.Mathery.ON_EPSILON)
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
			Int32		i, j, k, l;
			GBSPPlane	Plane		=new GBSPPlane();
			Vector3		v1, v2;
			float		d;
			float		Length;
			Int32		[]Counts	=new Int32[3];
			bool		FlipTest;

			for(i=0;i < source.mVerts.Count;i++)
			{
				l	=(i + 1) % source.mVerts.Count;

				v1	=source.mVerts[l] - source.mVerts[i];

				for(j=0;j < pass.mVerts.Count;j++)
				{
					v2	=pass.mVerts[j] - source.mVerts[i];

					Plane.mNormal	=Vector3.Cross(v1, v2);

					Length	=Plane.mNormal.Length();
					Plane.mNormal.Normalize();
					
					if(Length < UtilityLib.Mathery.ON_EPSILON)
					{
						continue;
					}
					
					Plane.mDist	=Vector3.Dot(pass.mVerts[j], Plane.mNormal);						
					FlipTest	=false;
					for(k=0;k < source.mVerts.Count;k++)
					{
						if(k == i || k == l)
						{
							continue;
						}

						d	=Vector3.Dot(source.mVerts[k], Plane.mNormal) - Plane.mDist;
						if(d < -UtilityLib.Mathery.ON_EPSILON)
						{
							FlipTest	=false;
							break;
						}
						else if(d > UtilityLib.Mathery.ON_EPSILON)
						{
							FlipTest	=true;
							break;
						}
					}
					if(k == source.mVerts.Count)
					{
						continue;
					}
					if(FlipTest)
					{
						Plane.Inverse();
					}
					Counts[0] = Counts[1] = Counts[2] = 0;

					for(k=0;k < pass.mVerts.Count;k++)
					{
						if(k==j)
						{
							continue;
						}
						d	=Vector3.Dot(pass.mVerts[k], Plane.mNormal) - Plane.mDist;
						if(d < -UtilityLib.Mathery.ON_EPSILON)
						{
							break;
						}
						else if(d > UtilityLib.Mathery.ON_EPSILON)
						{
							Counts[0]++;
						}
						else
						{
							Counts[2]++;
						}
					}
					if(k != pass.mVerts.Count)
					{
						continue;	
					}
						
					if(Counts[0] == 0)
					{
						continue;
					}
					if(!ClipPoly(Plane, bFlipClip))
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
					float	val	=UtilityLib.Mathery.VecIdx(mVerts[i], k);

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


		internal Int32[] IndexVerts(FaceFixer ff)
		{
			return	ff.IndexFaceVerts(mVerts);
		}


		internal void WriteReverse(System.IO.BinaryWriter bw)
		{
			for(int i=mVerts.Count - 1;i >=0;i--)
			{
				bw.Write(mVerts[i].X);
				bw.Write(mVerts[i].Y);
				bw.Write(mVerts[i].Z);
			}
		}


		internal void Write(System.IO.BinaryWriter bw)
		{
			foreach(Vector3 vert in mVerts)
			{
				bw.Write(vert.X);
				bw.Write(vert.Y);
				bw.Write(vert.Z);
			}
		}
	}
}
