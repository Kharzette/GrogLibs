using	System;
using	System.Diagnostics;
using	System.Collections.Generic;
using	System.IO;
using	System.Text;
using	Microsoft.Xna.Framework;
using	Microsoft.Xna.Framework.Graphics;

namespace BSPLib
{
	//a brush is a convex volume bounded
	//by planes.  The space defined as being
	//on the back side of all the planes in
	//the brush
	public class Brush
	{
		List<Face>	mFaces;
		Bounds		mBounds;
		UInt32		mContents;

		public const float	MIN_MAX_BOUNDS	=15192.0f;

		public const UInt32	CONTENTS_SOLID			=1;		// an eye is never valid in a solid
		public const UInt32	CONTENTS_WINDOW			=2;		// translucent, but not watery
		public const UInt32	CONTENTS_AUX			=4;
		public const UInt32	CONTENTS_LAVA			=8;
		public const UInt32	CONTENTS_SLIME			=16;
		public const UInt32	CONTENTS_WATER			=32;
		public const UInt32	CONTENTS_MIST			=64;
		public const UInt32	LAST_VISIBLE_CONTENTS	=64;

		// remaining contents are non-visible, and don't eat brushes

		public const UInt32	CONTENTS_AREAPORTAL		=0x8000;
		public const UInt32	CONTENTS_PLAYERCLIP		=0x10000;
		public const UInt32	CONTENTS_MONSTERCLIP	=0x20000;

		// currents can be added to any other contents, and may be mixed
		public const UInt32	CONTENTS_CURRENT_0		=0x40000;
		public const UInt32	CONTENTS_CURRENT_90		=0x80000;
		public const UInt32	CONTENTS_CURRENT_180	=0x100000;
		public const UInt32	CONTENTS_CURRENT_270	=0x200000;
		public const UInt32	CONTENTS_CURRENT_UP		=0x400000;
		public const UInt32	CONTENTS_CURRENT_DOWN	=0x800000;

		public const UInt32	CONTENTS_ORIGIN			=0x1000000;	// removed before bsping an entity

		public const UInt32	CONTENTS_MONSTER		=0x2000000;	// should never be on a brush, only in game
		public const UInt32	CONTENTS_DEADMONSTER	=0x4000000;
		public const UInt32	CONTENTS_DETAIL			=0x8000000;	// brushes to be added after vis leafs
		public const UInt32	CONTENTS_TRANSLUCENT	=0x10000000;	// auto set if any surface has trans
		public const UInt32	CONTENTS_LADDER			=0x20000000;
		public const UInt32	CONTENTS_STRUCTURAL		=0x10000000;	// brushes used for the bsp
		public const UInt32	CONTENTS_TRIGGER		=0x40000000;
		public const UInt32	CONTENTS_NODROP			=0x80000000;	// don't leave bodies or items (death fog, lava)


		#region Constructors
		public Brush()
		{
			mFaces  =new List<Face>();
		}


		public Brush(Brush b)
		{
			mFaces	=new List<Face>();

			foreach(Face f in b.mFaces)
			{
				mFaces.Add(new Face(f));
			}

			if(b.mBounds != null)
			{
				mBounds		=new Bounds(b.mBounds);
			}

			mContents	=b.mContents;
		}
		#endregion


		#region Modifications
		internal void PromoteClips()
		{
			if((mContents & (CONTENTS_STRUCTURAL
				| CONTENTS_PLAYERCLIP | CONTENTS_MONSTERCLIP))
				!= 0)
			{
				mContents	|=CONTENTS_SOLID;
			}
		}


		internal void AddFaces(List<Face> faces)
		{
			foreach(Face f in faces)
			{
				if((f.mFlags & (Face.SURF_HINT
					| Face.SURF_LADDER | Face.SURF_SKIP))
					!= 0)
				{
					continue;
				}
				mFaces.Add(f);
			}
		}


		internal void AddPlane(Plane p)
		{
			Face	f	=new Face(p, null);
			mFaces.Add(f);

			SealFaces();
		}


		internal void BoundBrush()
		{
			mBounds	=new Bounds();
			foreach(Face f in mFaces)
			{
				f.AddToBounds(mBounds);
			}
		}


		internal void RemoveVeryThinSides()
		{
			List<Face>	nuke	=new List<Face>();
			foreach(Face f in mFaces)
			{
				if(f.IsHowThin(0.5f))
				{
					nuke.Add(f);
				}
			}

			foreach(Face f in nuke)
			{
				mFaces.Remove(f);
			}
			SealFaces();
		}
		#endregion


		#region Queries
		internal void AddToBounds(Bounds bnd)
		{
			foreach(Face f in mFaces)
			{
				f.AddToBounds(bnd);
			}
		}


		internal bool ContainsPlane(Plane bev)
		{
			foreach(Face f in mFaces)
			{
				Plane	fplane	=f.GetPlane();

				if(UtilityLib.Mathery.CompareVectorEpsilon(
					bev.mNormal, fplane.mNormal,
					UtilityLib.Mathery.VCompareEpsilon))
				{
					return	true;
				}
			}
			return	false;
		}


		private Face GetFaceForPlane(Plane plane)
		{
			foreach(Face f in mFaces)
			{
				Plane	fplane	=f.GetPlane();

				if(UtilityLib.Mathery.CompareVectorEpsilon(
					plane.mNormal, fplane.mNormal,
					UtilityLib.Mathery.VCompareEpsilon))
				{
					return	f;
				}
			}
			return	null;
		}


		internal bool IsValid()
		{
			if(mFaces.Count < 3)
			{
				return	false;
			}
			foreach(Face f in mFaces)
			{
				if(f.IsTiny())
				{
					return	false;
				}
				else if(f.IsHuge())
				{
					return	false;
				}
			}
			return	true;
		}


		internal bool IsPointInside(Vector3 pnt)
		{
			foreach(Face f in mFaces)
			{
				if(!f.IsPointBehind(pnt))
				{
					return	false;
				}
			}
			return	true;
		}


		//checks to see if the brush
		//is of small volume, at least
		//on one side
		bool IsThin()
		{
			foreach(Face f in mFaces)
			{
				if(f.IsHowThin(5.0f))
				{
					return	true;
				}
			}
			return	false;
		}


		internal bool IsVeryThin()
		{
			foreach(Face f in mFaces)
			{
				if(f.IsHowThin(0.5f))
				{
					return	true;
				}
			}
			return	false;
		}


		bool Intersects(Face checkFace)
		{
			Face	tempFace	=new Face(checkFace);
			foreach(Face f in mFaces)
			{
				if(!tempFace.ClipByFace(f, false, true))
				{
					return	false;
				}
			}
			return	true;
		}


		internal bool Intersects(Brush b)
		{
			foreach(Face f in mFaces)
			{
				foreach(Face f2 in b.mFaces)
				{
					if(Intersects(f2))
					{
						return	true;
					}
				}
			}
			return false;
		}


		bool IsBrushMostlyOnFrontSide(Plane p)
		{
			float frontDist, backDist;

			frontDist	=0.0f;
			backDist	=0.0f;
			foreach(Face f in mFaces)
			{
				f.GetFaceMinMaxDistancesFromPlane(p, ref frontDist, ref backDist);
			}
			if(frontDist > (-backDist))
			{
				return true;
			}
			return false;
		}


		internal void GetTriangles(List<Vector3> tris, List<UInt16> ind)
		{
			foreach(Face f in mFaces)
			{
				if((f.mFlags & Face.SURF_SKIP) != 0)
				{
					continue;
				}
				if((f.mFlags & Face.SURF_NODRAW) != 0)
				{
					continue;
				}
				if((f.mFlags & Face.SURF_HINT) != 0)
				{
					continue;
				}
				f.GetTriangles(tris, ind);
			}
		}


		internal List<Face> GetFaces()
		{
			return	mFaces;
		}


		internal void GetPlanes(List<Plane> planes)
		{
			foreach(Face f in mFaces)
			{
				planes.Add(f.GetPlane());
			}
		}


		bool IsBehind(Plane ax)
		{
			foreach(Face f in mFaces)
			{
				if(!f.IsBehind(ax))
				{
					return	false;
				}
			}
			return	true;
		}
		#endregion


		#region IO
		internal static void SkipVMFEditorBlock(StreamReader sr)
		{
			string	s	="";
			while((s = sr.ReadLine()) != null)
			{
				s	=s.Trim();
				if(s.StartsWith("}"))
				{
					return;	//editor done
				}
			}
		}


		internal bool ReadVMFSolidBlock(StreamReader sr)
		{
			string	s	="";
			bool	ret	=true;
			while((s = sr.ReadLine()) != null)
			{
				s	=s.Trim();
				if(s == "side")
				{
					Face	f	=new Face();
					mContents	=f.ReadVMFSideBlock(sr);

					if(mContents == CONTENTS_AUX)
					{
						ret	=false;
					}
					mFaces.Add(f);
				}
				else if(s.StartsWith("}"))
				{
					return	ret;	//entity done
				}
				else if(s == "editor")
				{
					//skip editor block
					SkipVMFEditorBlock(sr);
				}
			}
			return	ret;
		}


		internal void MakeFaceFromMapLine(string szLine)
		{
			mFaces.Add(new Face(szLine));
		}


		internal void Read(BinaryReader br)
		{
			int	cnt	=br.ReadInt32();
			for(int i=0;i < cnt;i++)
			{
				Face	f	=new Face();
				f.Read(br);
				mFaces.Add(f);
			}
		}


		internal void Write(BinaryWriter bw)
		{
			bw.Write(mFaces.Count);
			foreach(Face f in mFaces)
			{
				f.Write(bw);
			}
		}
		#endregion


		#region CSG
		void ClipFaceByBrushBack(Face clipFace, bool keepOn)
		{
			foreach(Face f in mFaces)
			{
				clipFace.ClipByFace(f, false, keepOn);
				if(clipFace.IsTiny())
				{
					return;	//already destroyed
				}
			}
		}


		List<Face> ClipFaceByBrushFront(Face clipFace, bool keepOn)
		{
			List<Face>	ret	=new List<Face>();
			foreach(Face f in mFaces)
			{
				Face	cf	=new Face(clipFace, false);
				cf.ClipByFace(f, true, keepOn);
				clipFace.ClipByFace(f, false, keepOn);
				if(cf.IsTiny())
				{
					continue;
				}
				ret.Add(cf);
			}
			return	ret;
		}


		float GetSplitFaceScore(Face sf, List<Brush> brushList)
		{
			Plane	p	=sf.GetPlane();

			//test balance
			List<Brush> copyList	=new List<Brush>(brushList);
			List<Brush> front		=new List<Brush>();
			List<Brush> back		=new List<Brush>();

			int		numThins	=0;
			float	score		=696969.69f;
			foreach(Brush b2 in copyList)
			{
				Brush	bf, bb;

				b2.SplitBrush(sf, out bf, out bb);

				if(bb != null && bb.IsValid())
				{
					back.Add(bb);
				}
				if(bf != null && bf.IsValid())
				{
					front.Add(bf);
				}
			}

			//check for small volumes created
			//by this split
			int	splitThins	=0;
			foreach(Brush b in front)
			{
				if(b.IsThin())
				{
					splitThins++;
				}
			}
			foreach(Brush b in back)
			{
				if(b.IsThin())
				{
					splitThins++;
				}
			}
			splitThins	-=numThins;
			if(splitThins < 0)
			{
				splitThins	=0;
			}

			if(front.Count !=0 && back.Count != 0)
			{
				if(front.Count > back.Count)
				{
					score	=(float)front.Count / back.Count;
				}
				else
				{
					score	=(float)back.Count / front.Count;
				}

				score	+=splitThins * 5;
			}
			return	score;
		}


		//return the best score in the brush
		internal float GetBestSplittingFaceScore(List<Brush> brushList)
		{
			float	BestScore	=696969.0f;	//0.5f is the goal

			foreach(Face f in mFaces)
			{
				if((f.mFlags & (Face.SURF_LADDER | Face.SURF_SKIP)) != 0)
				{
					continue;
				}
				float	score	=GetSplitFaceScore(f, brushList);

				if((f.mFlags & Face.SURF_HINT) != 0)
				{
					if(score < 69000.0f)
					{
						score	-=100.0f;
					}
				}

				if(score < BestScore)
				{
					BestScore	=score;
				}
			}

			return	BestScore;
		}


		//return the best plane in the brush
		internal Face GetBestSplittingFace(List<Brush> brushList)
		{
			float	BestScore	=696969.0f;	//0.5f is the goal
			int		BestIndex	=0;

			foreach(Face f in mFaces)
			{
				float	score	=GetSplitFaceScore(f, brushList);
				if(score < BestScore)
				{
					BestScore	=score;
					BestIndex	=mFaces.IndexOf(f);
				}
			}

			//return the best plane
			return mFaces[BestIndex];
		}


		//brush b gobbles thisbrush
		//returns a bunch of parts
		internal bool SubtractBrush(Brush b, out List<Brush> outside)
		{
			Brush	bf, bb, inside;

			outside	=new List<Brush>();

			if(b.mFaces.Count == 0)
			{
				return false;
			}

			if((b.mContents & 127) == 0)
			{
				//brush b can't gobble anything
				return	false;
			}

			if((b.mContents & 127) > (mContents & 127))
			{
				//brush b can't gobble thisbrush
				return	false;
			}

			inside  =new Brush(this);

			bf = bb = null;

			foreach(Face f2 in b.mFaces)
			{
				inside.SplitBrush(f2, out bf, out bb);

				if(bf != null && bf.IsValid())
				{
					outside.Add(bf);
				}
				inside = bb;
				if(bb == null || !bb.IsValid())
				{
					break;
				}
			}

			if(inside == null || !inside.IsValid())
			{
				return false;
			}
			return true;
		}


		//expands all faces to seal cracks
		internal void SealFaces()
		{
		RESTART2:
			//check that faces are valid
			foreach(Face f in mFaces)
			{
				if(f.IsTiny())
				{
					mFaces.Remove(f);
					goto RESTART2;
				}
			}

		RESTART:
			//clip every face behind every other face
			foreach(Face f in mFaces)
			{
				f.Expand();

				foreach(Face f2 in mFaces)
				{
					if(f == f2)
					{
						continue;
					}
					Face	f3	=new Face(f);

					if(!f3.ClipByFace(f2, false, false))
					{
						mFaces.Remove(f);
						goto RESTART;
					}
					f.ClipByFace(f2, false, false);
				}
			}
		}


		internal void SplitBrush(Face splitBy, out Brush bf, out Brush bb)
		{
			float fDist, bDist;
			Plane p	=splitBy.GetPlane();

			fDist	=-Plane.EPSILON;
			bDist	=Plane.EPSILON;

			foreach(Face f in mFaces)
			{
				f.GetFaceMinMaxDistancesFromPlane(p, ref fDist, ref bDist);
			}

			if(bDist >= Plane.EPSILON)
			{
				//all in front
				bf  =new Brush(this);
				bb  =null;
				return;
			}
			if(fDist <= -Plane.EPSILON)
			{
				//all behind
				bb  =new Brush(this);
				bf  =null;
				return;
			}

			//create a split face
			Face	splitFace	=new Face(p, splitBy);

			//clip against all brush faces
			foreach(Face f in mFaces)
			{
				if(!splitFace.ClipByFace(f, false, false))
				{
					break;
				}
			}

			if(splitFace.IsTiny())
			{
				if(IsBrushMostlyOnFrontSide(p))
				{
					bf	=new Brush(this);
					bb	=null;
				}
				else
				{
					bb	=new Brush(this);
					bf	=null;
				}
				return;
			}

			bb  =new Brush(this);
			bf  =new Brush(this);

			foreach(Face f in bb.mFaces)
			{
				f.ClipByFace(splitFace, false, false);
			}
			foreach(Face f in bf.mFaces)
			{
				f.ClipByFace(splitFace, true, false);
			}

			//add split poly to both sides
			bb.mFaces.Add(new Face(splitFace));
			bf.mFaces.Add(new Face(splitFace, true));

			bb.SealFaces();
			bf.SealFaces();
		}


		internal void BevelBrush()
		{
			//add axial planes
			for(int i=0;i < 6;i++)
			{
				Plane	ax;
				ax.mNormal	=UtilityLib.Mathery.AxialNormals[i];

				if(i < 3)
				{
					ax.mDistance	=Vector3.Dot(mBounds.mMaxs, ax.mNormal);
				}
				else
				{
					ax.mDistance	=Vector3.Dot(mBounds.mMins, ax.mNormal);
				}

				if(ContainsPlane(ax))
				{
					continue;
				}

				Face	axFace	=new Face(ax, mFaces[0]);
				axFace.mFlags	|=Face.SURF_NODRAW;

				//add to the brush, but don't clip
				//the other faces.  Seal will destroy things
				mFaces.Add(axFace);
			}

			if(mFaces.Count == 6)
			{
				return;	//cube
			}

			//check edges
			foreach(Face f in mFaces)
			{
				List<Edge>	edges	=new List<Edge>();
				f.GetEdges(edges);

				foreach(Edge e in edges)
				{
					if(e.IsAxial() || e.Length() < 0.5f)
					{
						continue;
					}

					//try to bevel using axial planes
					for(int i=0;i < 6;i++)
					{
						Plane	ax;
						ax.mNormal	=Vector3.Cross(e.GetNormal(),
							UtilityLib.Mathery.AxialNormals[i]);

						if(ax.mNormal.Length() < 0.5f)
						{
							continue;
						}

						ax.mNormal.Normalize();
						ax.mDistance	=Vector3.Dot(e.mP0, ax.mNormal);

						//see if already added
						if(ContainsPlane(ax))
						{
							continue;
						}

						//see if rest of the brush is behind this plane
						if(IsBehind(ax))
						{
							Face	axFace	=new Face(ax, mFaces[0]);
							axFace.mFlags	|=Face.SURF_NODRAW;

							mFaces.Add(axFace);
						}
					}
				}
			}
		}


		internal void Expand(float dist)
		{
			foreach(Face f in mFaces)
			{
				f.Move(dist);
			}
			SealFaces();
		}
		#endregion
	}
}
