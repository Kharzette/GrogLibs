using System;
using System.Text;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using Vortice.Mathematics;


namespace BSPCore
{
	public class GBSPPoly
	{
		public Vector3	[]mVerts;

		internal const float	EDGE_LENGTH			=0.1f;
		internal const float	DEGENERATE_EPSILON	=0.001f;
		internal const float	COLINEAR_EPSILON	=0.0001f;


		#region Construction
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

			if(!GBSPPlane.TextureAxisFromPlaneGrog(p.mNormal, out rightVec, out upVec))
			{
				CoreEvents.Print("Bad plane passed into poly constructor!\n");
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
		#endregion


		#region Queries
		public int VertCount()
		{
			if(mVerts == null)
			{
				return	0;
			}
			return	mVerts.Length;
		}


		internal bool IsMaxExtents()
		{
			for(int i=0;i < mVerts.Length;i++)
			{
				for(int k=0;k < 3;k++)
				{
					float	val	=mVerts[i][k];

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


		internal float Area()
		{
			float	total	=0.0f;
			for(int i=2;i < VertCount();i++)
			{
				Vector3	vect1	=mVerts[i - 1] - mVerts[0];
				Vector3	vect2	=mVerts[i] - mVerts[0];

				//not sure if this ordering is correct, but since
				//only the length is used, should be ok
				Vector3	cross	=Vector3.Cross(vect1, vect2);

				total	+=0.5f * cross.Length();
			}
			return	total;
		}


		public void GetTriangles(GBSPPlane facePlane,
			Color matColor,
			List<Vector3> verts,
			List<Vector3> normals,
			List<Color> colors,
			List<UInt16> indexes, bool bCheckFlags)
		{
			Debug.Assert((verts.Count + mVerts.Length) < UInt16.MaxValue);

			UInt16	ofs		=(UInt16)verts.Count;

			//triangulate the brush face points
			foreach(Vector3 pos in mVerts)
			{
				verts.Add(pos);
				normals.Add(facePlane.mNormal);
				colors.Add(matColor);
			}

			int i	=0;
			for(i=1;i < mVerts.Length-1;i++)
			{
				//initial vertex
				indexes.Add((UInt16)(ofs + ((i + 1) % mVerts.Length)));
				indexes.Add((UInt16)(ofs + i));
				indexes.Add(ofs);
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
		#endregion


		#region Carving Operations
		public bool ClipPoly(GBSPPlane plane, bool bFlip, ClipPools cPools)
		{
			return	ClipPolyEpsilon(UtilityLib.Mathery.ON_EPSILON, plane, bFlip, cPools);
		}


		//this is the optimized version that prevents the garbage
		//collection from being overwhelmed during heavy clipping
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


		internal void SplitSideTest(GBSPPlane plane,
									out int front, out int back,
									ref float frontDist, ref float backDist)
		{
			front	=back	=0;
			foreach(Vector3 vert in mVerts)
			{
				float d	=plane.Distance(vert);

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


		internal void GetSplitMaxDist(GBSPPlane plane, bool side, ref float frontDist, ref float backDist)
		{
			if(mVerts == null)
			{
				return;
			}
			foreach(Vector3 vert in mVerts)
			{
				float	d	=plane.Distance(vert);

				if(side)
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


		//used from the vis library
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

					//testing reversing this for sharpdx
					plane.mNormal	=Vector3.Cross(v2, v1);

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
		#endregion


		#region Edge Stuff
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
		#endregion


		//lazy
		public void Reverse()
		{
			List<Vector3>	flip	=new List<Vector3>(mVerts);

			flip.Reverse();

			mVerts	=flip.ToArray();
		}


		internal void Free()
		{
			mVerts	=null;
		}


		internal void Move(Vector3 delta)
		{
			for(int i=0;i < mVerts.Length;i++)
			{
				mVerts[i]	+=delta;
			}
		}


		internal Int32[] IndexVerts(FaceFixer ff)
		{
			return	ff.IndexFaceVerts(mVerts);
		}


		#region IO
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
		#endregion
	}
}
