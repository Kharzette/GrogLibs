using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPCore
{
	public class GBSPPoly
	{
		public Vector3	[]mVerts;

		internal const float	EDGE_LENGTH			=0.1f;
		internal const float	DEGENERATE_EPSILON	=0.001f;
		internal const float	COLINEAR_EPSILON	=0.0001f;


		public GBSPPoly(int numVerts)
		{
			if(numVerts > 0)
			{
				mVerts	=new Vector3[numVerts];
			}
		}


		internal GBSPPoly(Vector3 p1, Vector3 p2, Vector3 p3)
		{
			mVerts	=new Vector3[3];

			mVerts[0]	=p1;
			mVerts[1]	=p2;
			mVerts[2]	=p3;
		}


		internal GBSPPoly(GFXFace f, Int32 []vertInds, Vector3 []verts)
		{
			mVerts	=new Vector3[f.mNumVerts];
			for(int i=0;i < f.mNumVerts;i++)
			{
				int	ind	=vertInds[i + f.mFirstVert];

				mVerts[i]	=verts[ind];
			}
			RemoveDegenerateEdges();			
		}


		public GBSPPoly(GBSPPoly copyMe)
		{
			if(copyMe.mVerts == null)
			{
				mVerts	=null;
				return;
			}
			mVerts	=new Vector3[copyMe.mVerts.Length];
			copyMe.mVerts.CopyTo(mVerts, 0);
		}


		internal GBSPPoly(GBSPPlane p)
		{
			Vector3	rightVec, upVec, org, vert;

			mVerts	=new Vector3[4];

			if(!TextureAxisFromPlane(p, out rightVec, out upVec))
			{
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

			mVerts[0]	=vert;

			vert	=org + rightVec;
			vert	+=upVec;

			mVerts[1]	=vert;

			vert	=org + rightVec;
			vert	-=upVec;

			mVerts[2]	=vert;

			vert	=org - rightVec;
			vert	-=upVec;

			mVerts[3]	=vert;
		}


		public int VertCount()
		{
			if(mVerts == null)
			{
				return	0;
			}
			return	mVerts.Length;
		}


		public static bool TextureAxisFromPlane(GBSPPlane pln, out Vector3 xv, out Vector3 yv)
		{
			Int32	bestAxis;
			float	dot, best;
			
			best		=0.0f;
			bestAxis	=-1;

			xv	=Vector3.Zero;
			yv	=Vector3.Zero;
			
			for(int i=0;i < 3;i++)
			{
				dot	=Math.Abs(UtilityLib.Mathery.VecIdx(pln.mNormal, i));
				if(dot > best)
				{
					best		=dot;
					bestAxis	=i;
				}
			}

			//note that this is set up for quake 1 texcoords
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
					xv.X	=-1.0f;
					xv.Y	=0.0f;
					xv.Z	=0.0f;

					yv.X	=0.0f;
					yv.Y	=0.0f;
					yv.Z	=-1.0f;
					break;
				case 2:						// Z
					xv.X	=-1.0f;
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
					CoreEvents.Print("GetTextureAxis: No Axis found.");
					return false;
			}
			return	true;
		}


		public bool ClipPoly(GBSPPlane plane, bool bFlip, ClipPools cPools)
		{
			return	ClipPolyEpsilon(UtilityLib.Mathery.ON_EPSILON, plane, bFlip, cPools);
		}


		public bool ClipPolyEpsilon(float epsilon, GBSPPlane plane, bool flipTest, ClipPools cPools)
		{
			if(mVerts.Length > ClipPools.ClipArraySize)
			{
				CoreEvents.Print("ClipPoly:  Too many verts.\n");
				return	false;
			}

			Vector3	normal	=plane.mNormal;
			float	dist	=plane.mDist;

			if(flipTest)
			{
				normal	=-normal;
				dist	=-dist;
			}

			float	[]VDist		=cPools.mClipFloats.GetFreeItem();
			Int32	[]VSides	=cPools.mClipInts.GetFreeItem();

			Int32	countSides0	=0;
			Int32	countSides1	=0;
			Int32	countSides2	=0;

			for(int i=0;i < mVerts.Length;i++)
			{
				VDist[i]	=Vector3.Dot(mVerts[i], normal) - dist;
				if(VDist[i] > epsilon)
				{
					VSides[i]	=0;
					countSides0++;
				}
				else if(VDist[i] < -epsilon)
				{
					VSides[i]	=1;
					countSides1++;
				}
				else
				{
					VSides[i]	=2;
					countSides2++;
				}
			}

			if(countSides0 == 0)
			{
				cPools.FreeVerts(mVerts);
				mVerts	=null;
				cPools.mClipFloats.FlagFreeItem(VDist);
				cPools.mClipInts.FlagFreeItem(VSides);
				return	true;
			}
			if(countSides1 == 0)
			{
				cPools.mClipFloats.FlagFreeItem(VDist);
				cPools.mClipInts.FlagFreeItem(VSides);
				return	true;
			}

			Vector3	[]frontVerts	=cPools.mClipVecs.GetFreeItem();

			int	idx	=0;
			for(int i=0;i < mVerts.Length;i++)
			{
				Vector3	vert1	=mVerts[i];

				if(VSides[i] == 2)
				{
					frontVerts[idx++]	=vert1;
					continue;
				}

				if(VSides[i] == 0)
				{
					frontVerts[idx++]	=vert1;
				}

				int	nextVert	=(i + 1) % mVerts.Length;

				if(VSides[nextVert] == 2 || VSides[nextVert] == VSides[i])
				{
					continue;
				}

				Vector3	vert2	=mVerts[nextVert];
				float	scale	=VDist[i] / (VDist[i] - VDist[nextVert]);

				frontVerts[idx++]	=(vert1 + (vert2 - vert1) * scale);
			}

			cPools.FreeVerts(mVerts);
			mVerts	=null;

			if(idx > 2)
			{
				mVerts	=cPools.DupeVerts(frontVerts, idx);
			}

			cPools.mClipFloats.FlagFreeItem(VDist);
			cPools.mClipInts.FlagFreeItem(VSides);
			cPools.mClipVecs.FlagFreeItem(frontVerts);

			if(idx < 3)
			{
				return	false;
			}

			return	true;
		}


		internal bool IsTiny()
		{
			if(mVerts == null || mVerts.Length < 3)
			{
				return	true;
			}

			int	edges	=0;

			for(int i=0;i < mVerts.Length;i++)
			{
				int	j	=(i == mVerts.Length - 1) ? 0 : i + 1;

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
			return	SplitEpsilon(UtilityLib.Mathery.ON_EPSILON, plane,
						out polyFront, out polyBack, flipTest);
		}


		internal bool SplitEpsilon(float epsilon, GBSPPlane plane,
			out GBSPPoly polyFront, out GBSPPoly polyBack, bool flipTest)
		{
			polyFront	=null;
			polyBack	=null;

			if(mVerts == null)
			{
				return	false;
			}

			if(mVerts.Length > 100)
			{
				CoreEvents.Print("ClipPoly:  Too many verts.\n");
				return	false;
			}

			Vector3	normal	=plane.mNormal;
			float	dist	=plane.mDist;

			if(flipTest)
			{
				normal	=-normal;
				dist	=-dist;
			}

			float	[]VDist			=new float[mVerts.Length];
			Int32	[]VSides		=new int[mVerts.Length];
			Int32	frontCount		=0;
			Int32	backCount		=0;

			List<Vector3>	frontVerts	=new List<Vector3>();
			List<Vector3>	backVerts	=new List<Vector3>();

			for(int i=0;i < mVerts.Length;i++)
			{
				VDist[i]	=Vector3.Dot(mVerts[i], normal) - dist;
				if(VDist[i] > epsilon)
				{
					VSides[i]	=0;
					frontCount++;
				}
				else if(VDist[i] < -epsilon)
				{
					VSides[i]	=1;
					backCount++;
				}
				else
				{
					VSides[i]	=2;
				}
			}

			if(frontCount == 0)
			{
				polyBack	=new GBSPPoly(this);
				return	true;
			}
			if(backCount == 0)
			{
				polyFront	=new GBSPPoly(this);
				return	true;
			}

			for(int i=0;i < mVerts.Length;i++)
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

				int	nextVert	=(i + 1) % mVerts.Length;

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
				polyFront			=new GBSPPoly(0);
				polyFront.mVerts	=frontVerts.ToArray();
			}

			if(backVerts.Count < 3)
			{
				return	false;
			}
			else
			{
				polyBack		=new GBSPPoly(0);
				polyBack.mVerts	=backVerts.ToArray();
			}

			return	true;
		}


		internal void RemoveDegenerateEdges()
		{
			bool	Bad	=false;

			List<Vector3>	newVerts	=new List<Vector3>();

			for(int i=0;i < mVerts.Length;i++)
			{
				Vector3	V1	=mVerts[i];
				Vector3	V2	=mVerts[(i + 1) % mVerts.Length];

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
				mVerts	=newVerts.ToArray();
			}
		}


		internal bool EdgeExist(Vector3 edge10, Vector3 edge11,
			out Int32 edgeIndexOut0, out Int32 edgeIndexOut1)
		{
			Int32		i;
			Vector3		edge20, edge21;

			for(i=0;i < mVerts.Length;i++)
			{
				edge20	=mVerts[i];
				edge21	=mVerts[(i + 1) % mVerts.Length];

				if(UtilityLib.Mathery.CompareVector(edge10, edge20))
				{
					if(UtilityLib.Mathery.CompareVector(edge11, edge21))
					{
						edgeIndexOut0	=i;
						edgeIndexOut1	=(i + 1) % mVerts.Length;
						return	true;
					}
				}
			}

			edgeIndexOut0	=0;
			edgeIndexOut1	=0;

			return	false;
		}


		internal float Area()
		{
			float	total	=0.0f;
			for(int i=2;i < VertCount();i++)
			{
				Vector3	vect1	=mVerts[i - 1] - mVerts[0];
				Vector3	vect2	=mVerts[i] - mVerts[0];

				Vector3	cross	=Vector3.Cross(vect1, vect2);

				total	+=0.5f * cross.Length();
			}
			return	total;
		}


		public void GetTriangles(List<Vector3> verts, List<UInt32> indexes, bool bCheckFlags)
		{
			int	ofs		=verts.Count;

			UInt32	offset	=(UInt32)ofs;

			//triangulate the brush face points
			foreach(Vector3 pos in mVerts)
			{
				verts.Add(pos);
			}

			int i	=0;
			for(i=1;i < mVerts.Length-1;i++)
			{
				//initial vertex
				indexes.Add(offset);
				indexes.Add((UInt32)(offset + i));
				indexes.Add((UInt32)(offset + ((i + 1) % mVerts.Length)));
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

			for(int i=0;i < mVerts.Length;i++)
			{
				//initial vertex
				indexes.Add((UInt32)(offset + i));
				indexes.Add((UInt32)(offset + ((i + 1) % mVerts.Length)));
			}
		}


		//lazy
		public void Reverse()
		{
			List<Vector3>	flip	=new List<Vector3>(mVerts);

			flip.Reverse();

			mVerts	=flip.ToArray();
		}


		static internal GBSPPoly Merge(GBSPPoly p1, GBSPPoly p2, Vector3 normal, PlanePool pool)
		{
			bool		keep1	=true, keep2	=true;

			if(p1.mVerts.Length == -1 || p2.mVerts.Length == -1)
			{
				return	null;
			}

			int	numVerts	=p1.mVerts.Length;

			//
			// Go through each edge of p1, and see if the reverse of it exist in p2
			//
			Int32		edgeIndex0	=0;
			Int32		edgeIndex1	=0;
			Vector3		edge10		=Vector3.Zero;
			Vector3		edge11		=Vector3.Zero;
			int			i;
			for(i=0;i < numVerts;i++)		
			{
				edge11	=p1.mVerts[i];
				edge10	=p1.mVerts[(i + 1) % numVerts];

				if(p2.EdgeExist(edge10, edge11, out edgeIndex0, out edgeIndex1))
				{
					break;
				}
			}

			if(i >= numVerts)							// Did'nt find an edge, return nothing
			{
				return	null;
			}

			int	numVerts2	=p2.mVerts.Length;

			//
			//	See if the 2 joined make a convex poly, connect them, and return new one
			//

			//Get the normal of the edge just behind edge1
			Vector3	v1	=p1.mVerts[(i + numVerts - 1) % numVerts];
			v1	-=edge11;

			Vector3	normal2	=Vector3.Cross(normal, v1);
			normal2.Normalize();

			Vector3	v2	=p2.mVerts[(edgeIndex1 + 1) % numVerts2] - p2.mVerts[edgeIndex1];

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
			v1	-=edge10;

			normal2	=Vector3.Cross(normal, v1);
			normal2.Normalize();

			v2	=p2.mVerts[(edgeIndex0 + numVerts2 - 1) % numVerts2] -
						p2.mVerts[edgeIndex0];

			dot	=Vector3.Dot(v2, normal2);
			if(dot > COLINEAR_EPSILON)
			{
				return	null;	//Edge makes a non-convex poly
			}
			if(dot >= -COLINEAR_EPSILON)	//Drop point, on colinear edge
			{
				keep2	=false;
			}

			//count total verts
			int	numNewVerts	=0;
			for(int k = (i + 1) % numVerts;k != i;k = (k + 1) % numVerts)
			{
				if(k == (i + 1) % numVerts && !keep2)
				{
					continue;
				}
				numNewVerts++;
			}

			int	temp	=i;
			i			=edgeIndex0;

			for(int k = (i + 1) % numVerts2;k != i;k = (k + 1) % numVerts2)
			{
				if(k == (i + 1) % numVerts2 && !keep1)
				{
					continue;
				}
				numNewVerts++;
			}

			i	=temp;

			//make a new poly
			GBSPPoly	ret	=new GBSPPoly(numNewVerts);
			int			j	=0;
			for(int k = (i + 1) % numVerts;k != i;k = (k + 1) % numVerts)
			{
				if(k == (i + 1) % numVerts && !keep2)
				{
					continue;
				}
				ret.mVerts[j++]	=p1.mVerts[k];
			}

			i	=edgeIndex0;

			for(int k = (i + 1) % numVerts2;k != i;k = (k + 1) % numVerts2)
			{
				if(k == (i + 1) % numVerts2 && !keep1)
				{
					continue;
				}
				ret.mVerts[j++]	=(p2.mVerts[k]);
			}
			return	ret;
		}


		public float Radius()
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


		public bool AnyPartBehind(GBSPPlane p)
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


		public Vector3 Center()
		{
			Vector3	ret	=Vector3.Zero;

			foreach(Vector3 vert in mVerts)
			{
				ret	+=vert;
			}
			ret	/=mVerts.Length;

			return	ret;
		}


		public bool AnyPartInFront(GBSPPlane p)
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
			if(mVerts == null)
			{
				return;
			}
			foreach(Vector3 pnt in mVerts)
			{
				bnd.AddPointToBounds(pnt);
			}
		}


		internal void Free()
		{
			mVerts	=null;
		}


		internal bool Check(bool bVerb, Vector3 norm, float dist)
		{
			int	i;
			for(i=0;i < mVerts.Length;i++)
			{
				Vector3	v1	=mVerts[i];
				Vector3	v2	=mVerts[(i + 1) % mVerts.Length];

				//Check for degenreate edge
				Vector3	vect1	=v2 - v1;
				float	d		=vect1.Length();
				if(Math.Abs(d) < DEGENERATE_EPSILON)
				{
					if(bVerb)
					{
						CoreEvents.Print("WARNING CheckFace:  Degenerate Edge.\n");
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
						CoreEvents.Print("WARNING CheckFace:  Non planar: " + d + "\n");
					}
					return	false;
				}

				Vector3	edgeNorm	=Vector3.Cross(norm, vect1);
				edgeNorm.Normalize();
				float	edgeDist	=Vector3.Dot(v1, edgeNorm);
				
				//Check for convexity
				for(int j=0;j < mVerts.Length;j++)
				{
					d	=Vector3.Dot(mVerts[j], edgeNorm) - edgeDist;
					if(d > UtilityLib.Mathery.ON_EPSILON)
					{
						if(bVerb)
						{
							CoreEvents.Print("CheckFace:  Face not convex.\n");
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


		internal void GetSplitMaxDist(GBSPPlane plane, sbyte side, ref float frontDist, ref float backDist)
		{
			if(mVerts == null)
			{
				return;
			}
			foreach(Vector3 vert in mVerts)
			{
				float	d	=plane.DistanceFast(vert);

				if(side != 0)
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


		public bool SeperatorClip(GBSPPoly source, GBSPPoly pass, bool bFlipClip, ClipPools cPools)
		{
			for(int i=0;i < source.mVerts.Length;i++)
			{
				int	l	=(i + 1) % source.mVerts.Length;

				Vector3	v1	=source.mVerts[l] - source.mVerts[i];

				for(int j=0;j < pass.mVerts.Length;j++)
				{
					Vector3	v2	=pass.mVerts[j] - source.mVerts[i];

					GBSPPlane	plane	=new GBSPPlane();
					plane.mNormal	=Vector3.Cross(v1, v2);

					float	len		=plane.mNormal.Length();
					plane.mNormal	/=len;
					
					if(len < UtilityLib.Mathery.ON_EPSILON)
					{
						continue;
					}
					
					plane.mDist	=Vector3.Dot(pass.mVerts[j], plane.mNormal);						

					bool	bFlipTest	=false;
					int		k;
					for(k=0;k < source.mVerts.Length;k++)
					{
						if(k == i || k == l)
						{
							continue;
						}

						float	d	=Vector3.Dot(source.mVerts[k], plane.mNormal) - plane.mDist;
						if(d < -UtilityLib.Mathery.ON_EPSILON)
						{
							bFlipTest	=false;
							break;
						}
						else if(d > UtilityLib.Mathery.ON_EPSILON)
						{
							bFlipTest	=true;
							break;
						}
					}
					if(k == source.mVerts.Length)
					{
						continue;
					}
					if(bFlipTest)
					{
						plane.Inverse();
					}

					Int32	count0	=0;
					Int32	count2	=0;
					for(k=0;k < pass.mVerts.Length;k++)
					{
						if(k==j)
						{
							continue;
						}
						float	d	=Vector3.Dot(pass.mVerts[k], plane.mNormal) - plane.mDist;
						if(d < -UtilityLib.Mathery.ON_EPSILON)
						{
							break;
						}
						else if(d > UtilityLib.Mathery.ON_EPSILON)
						{
							count0++;
						}
						else
						{
							count2++;
						}
					}
					if(k != pass.mVerts.Length)
					{
						continue;	
					}
						
					if(count0 == 0)
					{
						continue;
					}
					if(!ClipPoly(plane, bFlipClip, cPools))
					{
						CoreEvents.Print("ClipToPortals:  Error clipping portal.\n");
						return	false;
					}

					if(mVerts == null || mVerts.Length < 3)
					{
						return	true;
					}
				}
			}			
			return	true;
		}


		internal bool IsMaxExtents()
		{
			for(int i=0;i < mVerts.Length;i++)
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
			bw.Write(mVerts.Length);
			for(int i=mVerts.Length - 1;i >=0;i--)
			{
				bw.Write(mVerts[i].X);
				bw.Write(mVerts[i].Y);
				bw.Write(mVerts[i].Z);
			}
		}


		public void Write(System.IO.BinaryWriter bw)
		{
			bw.Write(mVerts.Length);
			foreach(Vector3 vert in mVerts)
			{
				bw.Write(vert.X);
				bw.Write(vert.Y);
				bw.Write(vert.Z);
			}
		}


		public void Read(System.IO.BinaryReader br)
		{
			int	count	=br.ReadInt32();

			mVerts	=new Vector3[count];
			for(int i=0;i < count;i++)
			{
				mVerts[i].X	=br.ReadSingle();
				mVerts[i].Y	=br.ReadSingle();
				mVerts[i].Z	=br.ReadSingle();
			}
		}
	}
}
