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
		private List<Face> mFaces;

		public const float	MIN_MAX_BOUNDS	=15192.0f;


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
		}
		#endregion


		#region Modifications
		internal void AddFaces(List<Face> faces)
		{
			foreach(Face f in faces)
			{
//				if((f.mFlags & Face.HINT) != 0)
//				{
//					continue;
//				}
				if((f.mFlags & Face.FACE_DETAIL) != 0)
				{
					continue;
				}
//				if((f.mFlags & Face.FACE_HIDDEN) != 0)
//				{
//					continue;
//				}
				if((f.mFlags & Face.TEX_CLIP) != 0)
				{
					continue;
				}
			}
			mFaces.AddRange(faces);
		}


		internal void AddPlane(Plane p)
		{
			Face	f	=new Face(p, null);
			mFaces.Add(f);

			SealFaces();
		}


		internal void RemoveVeryThinSides()
		{
			List<Face>	nuke	=new List<Face>();
			foreach(Face f in mFaces)
			{
				if(f.IsHowThin(1.0f))
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
		internal List<Face> GetFaces()
		{
			return	mFaces;
		}


		internal void AddToBounds(ref Bounds bnd)
		{
			foreach(Face f in mFaces)
			{
				f.AddToBounds(ref bnd);
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
				if(f.IsHowThin(1.0f))
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


		List<Face> GetPortalNeighbors(Face f)
		{
			List<Face>		ret		=new List<Face>();
			List<Vector3>	pnts	=f.GetPoints();

			foreach(Vector3 pnt in pnts)
			{
				foreach(Face brushFace in mFaces)
				{
					if(f == brushFace || !f.IsPortal())
					{
						continue;
					}
					if(brushFace.ContainsPoint(pnt))
					{
						if(!ret.Contains(brushFace))
						{
							ret.Add(brushFace);
						}
					}
				}
			}
			return	ret;
		}


		internal void GetTriangles(List<Vector3> tris, List<UInt16> ind)
		{
			foreach(Face f in mFaces)
			{
				if((f.mFlags & Face.HINT) != 0)
				{
					continue;
				}
//				if((f.mFlags & Face.FACE_DETAIL) != 0)
//				{
//					continue;
//				}
				if((f.mFlags & Face.FACE_HIDDEN) != 0)
				{
					continue;
				}
				if((f.mFlags & Face.TEX_CLIP) != 0)
				{
					continue;
				}
				f.GetTriangles(tris, ind);
			}
		}


		internal void GetPlanes(List<Plane> planes)
		{
			foreach(Face f in mFaces)
			{
				planes.Add(f.GetPlane());
			}
		}


		internal void GetPortalFaces(List<PortalFace> portals)
		{
			foreach(Face f in mFaces)
			{
				if(f.IsPortal())				
				{
					PortalFace	pf	=new PortalFace();
					pf.mFace	=f;
					pf.mBrush	=this;
					portals.Add(pf);
				}
			}
		}


		internal void GetPortals(List<Face> portals)
		{
			foreach(Face f in mFaces)
			{
				if(f.IsPortal())				
				{
					portals.Add(f);
				}
			}
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
					if(!f.ReadVMFSideBlock(sr))
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
				float	score	=GetSplitFaceScore(f, brushList);

				if((f.mFlags & Face.HINT) != 0)
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


		static Int64 recCount	=0;


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
				//Debug.WriteLine("Doing the mostly on side thing");
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


		internal void MarkPortal(Face portal)
		{
			foreach(Face f in mFaces)
			{
				Face	port	=new Face(portal);
				if(port.PortalClipByFace(f))
				{
					if(!port.IsTiny())
					{
						Face	pf	=GetFaceForPlane(port.GetPlane());
						pf.mFlags	|=Face.PORTAL;
					}
				}
			}
		}


		//the passed in face is actually a real
		//face that lives in a brush somewhere
		internal void MergePortal(Face portal, List<PortalFace> outPortals)
		{
			if(!portal.IsPortal())
			{
				return;
			}
			Face	clipped	=new Face(portal);

			ClipFaceByBrushBack(clipped, true);

			if(clipped.IsTiny())
			{
				return;
			}

			foreach(Face f in mFaces)
			{
				if(f == portal)
				{
					return;
				}
				if(clipped.WouldPortalClipBehind(f))
				{
					Face	clipFront	=new Face(portal);

					List<Face>	fronts	=ClipFaceByBrushFront(clipFront, false);

					if(fronts.Count > 0)
					{
						foreach(Face frnt in fronts)
						{
							PortalFace	pf	=new PortalFace();
							pf.mFace	=frnt;
							pf.mBrush	=this;

							outPortals.Add(pf);
						}
					}

					//clear portal flag
					portal.mFlags	&=(~Face.PORTAL);
					return;
				}
			}
		}
		#endregion
	}
}
