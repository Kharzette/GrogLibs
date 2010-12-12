using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class GBSPFace
	{
		GBSPFace	mNext;
		GBSPFace	mOriginal;
		GBSPPoly	mPoly;
		UInt32		[]mContents	=new UInt32[2];
		Int32		mTexInfo;
		Int32		mPlaneNum;
		Int32		mPlaneSide;

		Int32		mEntity;	//Originating entity

		bool		mbVisible;			

		//For GFX file saving
		public Int32		mOutputNum;	

		public Int32		[]mIndexVerts;
		public Int32		mFirstIndexVert;

		public GBSPPortal	mPortal;
		public GBSPFace		[]mSplit	=new GBSPFace[2];
		public GBSPFace		mMerged;

		public const float	COLINEAR_EPSILON	=0.0001f;


		public GBSPFace() { }
		public GBSPFace(GBSPFace copyMe)
		{
			mNext			=copyMe.mNext;
			mOriginal		=copyMe.mOriginal;
			mPoly			=new GBSPPoly(copyMe.mPoly);
			mContents[0]	=copyMe.mContents[0];
			mContents[1]	=copyMe.mContents[1];
			mTexInfo		=copyMe.mTexInfo;
			mPlaneNum		=copyMe.mPlaneNum;
			mPlaneSide		=copyMe.mPlaneSide;
			mEntity			=copyMe.mEntity;
			mbVisible		=copyMe.mbVisible;
			mOutputNum		=copyMe.mOutputNum;
			mIndexVerts		=copyMe.mIndexVerts;
			mFirstIndexVert	=copyMe.mFirstIndexVert;
			mPortal			=copyMe.mPortal;
			mSplit[0]		=copyMe.mSplit[0];
			mSplit[1]		=copyMe.mSplit[1];
			mMerged			=copyMe.mMerged;
		}


		public GBSPFace(GBSPPortal port, Int32 pside)
		{
			mTexInfo	=port.mSide.mTexInfo;
			mPlaneNum	=port.mSide.mPlaneNum;
			mPlaneSide	=pside;
			mPortal		=port;
			mbVisible	=true;

			if(pside != 0)
			{
				mPoly	=new GBSPPoly(port.mPoly);
				mPoly.Reverse();
			}
			else
			{
				mPoly	=new GBSPPoly(port.mPoly);
			}
		}

		static GBSPFace MergeFace(GBSPFace Face1, GBSPFace Face2, PlanePool pool)
		{
			GBSPPoly	NewPoly = null, Poly1, Poly2;
			Vector3		Normal1;
			GBSPFace	NewFace	=null;

			//
			// Planes and Sides MUST match before even trying to merge
			//
			if(Face1.mPlaneNum != Face2.mPlaneNum)
			{
				return	null;
			}
			if (Face1.mPlaneSide != Face2.mPlaneSide)
			{
				return null;
			}

			if((Face1.mContents[0] & Contents.BSP_MERGE_SEP_CONTENTS)
				!= (Face2.mContents[0] & Contents.BSP_MERGE_SEP_CONTENTS))
			{
				return	null;
			}
			if((Face1.mContents[1] & Contents.BSP_MERGE_SEP_CONTENTS)
				!= (Face2.mContents[1] & Contents.BSP_MERGE_SEP_CONTENTS))
			{
				return	null;
			}

			if(Face1.mTexInfo != Face2.mTexInfo)
			{
				return	null;
			}
			
			Poly1	=Face1.mPoly;
			Poly2	=Face2.mPoly;

			Normal1	=pool.mPlanes[Face1.mPlaneNum].mNormal;	// Get the normal
			if(Face1.mPlaneSide != 0)
			{
				Normal1	=Vector3.Zero - Normal1;
			}

			NewPoly	=GBSPPoly.Merge(Poly1, Poly2, Normal1, pool);
			if(NewPoly == null)
			{
				return	null;
			}			
			NewFace			=new GBSPFace(Face2);
			NewFace.mPoly	=NewPoly;
			if(NewFace == null)
			{
				Map.Print("*WARNING* MergeFace:  Out of memory for new face!\n");
				return	null;
			}

			Face1.mMerged	=NewFace;
			Face2.mMerged	=NewFace;

			return	NewFace;
		}


		internal static bool MergeFaceList2(GBSPFace Faces, PlanePool pool, ref int NumMerged)
		{
			GBSPFace	Face1, Face2, End, Merged;

			for(Face1 = Faces;Face1 != null;Face1 = Face1.mNext)
			{
				if(Face1.mPoly.VertCount() == -1)
				{
					continue;
				}

				if(Face1.mMerged != null || Face1.mSplit[0] != null || Face1.mSplit[1] != null)
				{
					continue;
				}

				for (Face2 = Faces ; Face2 != Face1 ; Face2 = Face2.mNext)
				{
					if(Face2.mPoly.VertCount() == -1)
					{
						continue;
					}

					if(Face2.mMerged != null || Face2.mSplit[0] != null || Face2.mSplit[1] != null)
					{
						continue;
					}
					
					Merged	=MergeFace(Face1, Face2, pool);

					if(Merged == null)
					{
						continue;
					}

					Merged.mPoly.RemoveDegenerateEdges();
					
					if(!Merged.Check(false, pool))
					{
						Merged.Free();
						Face1.mMerged	=null;
						Face2.mMerged	=null;
						continue;
					}

					NumMerged++;

					//Add the Merged to the end of the face list 
					//so it will be checked against all the faces again
					for(End = Faces;End.mNext != null;End = End.mNext);
						
					Merged.mNext	=null;
					End.mNext		=Merged;
					break;
				}
			}
			return	true;
		}


		private void Free()
		{
			if(mPoly != null)
			{
				mPoly.Free();
			}
		}


		private bool Check(bool Verb, PlanePool pool)
		{
			Vector3		Normal;
			float		PDist;
			
			if(mPoly.VertCount() < 3)
			{
				if(Verb)
				{
					Map.Print("CheckFace:  NumVerts < 3.\n");
				}
				return	false;
			}
			
			Normal	=pool.mPlanes[mPlaneNum].mNormal;
			PDist	=pool.mPlanes[mPlaneNum].mDist;
			if(mPlaneSide != 0)
			{
				Normal	=-Normal;
				PDist	=-PDist;
			}

			//
			//	Check for degenerate edges, convexity, and make sure it's planar
			//
			return	mPoly.Check(Verb, Normal, PDist);
		}


		internal void SetContents(int idx, UInt32 val)
		{
			mContents[idx]	=val;
		}


		internal UInt32 GetContents(int idx)
		{
			return	mContents[idx];
		}


		internal void GetTriangles(List<Vector3> verts, List<uint> indexes, bool bCheckFlags)
		{
			if(mPoly != null)
			{
				mPoly.GetTriangles(verts, indexes, bCheckFlags);
			}
		}


		bool FixTJunctions(FaceFixer ff, TexInfoPool tip)
		{
			return	ff.FixTJunctions(ref mIndexVerts, tip.mTexInfos[mTexInfo]);
		}


		static internal void FreeFaceList(GBSPFace listHead)
		{
			GBSPFace	Next	=null;
			for(GBSPFace f=listHead;f != null;f=Next)
			{
				Next	=f.mNext;
				f		=null;
			}
		}


		static internal void AddToListStart(ref GBSPFace listHead, GBSPFace newFace)
		{
			newFace.mNext	=listHead;
			listHead		=newFace;
		}


		static internal bool GetFaceListVertIndexNumbers(GBSPFace listHead, FaceFixer ff)
		{
			for(GBSPFace f=listHead;f != null;f = f.mNext)
			{
				if(f.mMerged != null
					|| f.mSplit[0] != null
					|| f.mSplit[1] != null)
				{
					continue;
				}

				if(!f.GetFaceVertIndexNumbers(ff))
				{
					return	false;
				}
			}
			return	true;
		}


		static internal bool FixFaceListTJunctions(GBSPFace listHead, FaceFixer ff, TexInfoPool tip)
		{
			for(GBSPFace f=listHead;f != null;f = f.mNext)
			{
				if(f.mMerged != null
					|| f.mSplit[0] != null
					|| f.mSplit[1] != null)
				{
					continue;
				}

				f.FixTJunctions(ff, tip);
			}
			return	true;
		}


		bool GetFaceVertIndexNumbers(FaceFixer ff)
		{
			mIndexVerts		=mPoly.IndexVerts(ff);
			return	true;
		}


		internal bool IsVisible()
		{
			return	mbVisible;
		}


		//prepares faces for writing
		internal static int PrepFaceList(GBSPFace listHead, NodeCounter nc)
		{
			int	numFaces	=0;
			for(GBSPFace f=listHead;f != null;f=f.mNext)
			{
				if(!f.mbVisible)
				{
					continue;
				}

				if(f.mMerged != null ||
					f.mSplit[0] != null ||
					f.mSplit[1] != null)
				{
					continue;
				}

				//Skip output of face, if IndexVerts not > 0
				//NOTE - The leaf faces output stage will also skip these same faces...
				if(f.mIndexVerts.Length <= 0)
				{
					continue;
				}

				f.mFirstIndexVert	=nc.VertIndexListCount;
				f.mOutputNum		=nc.mNumGFXFaces;

				for(int i=0;i < f.mIndexVerts.Length;i++)
				{
					nc.AddIndex(f.mIndexVerts[i]);
				}
				nc.mNumGFXFaces++;
				numFaces++;
			}
			return	numFaces;
		}


		internal static void ConvertListToGFXAndSave(GBSPFace listHead, BinaryWriter bw)
		{
			for(GBSPFace f=listHead;f != null;f=f.mNext)
			{
				if(!f.mbVisible)
				{
					continue;
				}

				if(f.mMerged != null
					|| f.mSplit[0] != null
					|| f.mSplit[1] != null)
				{
					continue;
				}

				if(f.mIndexVerts.Length > 0)
				{
					GFXFace	GFace	=new GFXFace();

					GFace.mFirstVert	=f.mFirstIndexVert;
					GFace.mNumVerts		=f.mIndexVerts.Length;
					GFace.mPlaneNum		=f.mPlaneNum;
					GFace.mPlaneSide	=f.mPlaneSide;
					GFace.mTexInfo		=f.mTexInfo;
					GFace.mLWidth		=0;
					GFace.mLHeight		=0;
					GFace.mLightOfs		=-1;	//No light info yet
					GFace.mLTypes[0]	=255;	//Of course, no styles yet either
					GFace.mLTypes[1]	=255;
					GFace.mLTypes[2]	=255;
					GFace.mLTypes[3]	=255;

					GFace.Write(bw);
				}
			}
		}
	}
}