using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class GBSPFace
	{
		public GBSPFace		mNext;
		public GBSPFace		mOriginal;
		public GBSPPoly		mPoly;
		public UInt32		[]mContents	=new UInt32[2];
		public Int32		mTexInfo;
		public Int32		mPlaneNum;
		public Int32		mPlaneSide;

		public Int32		mEntity;	//Originating entity

		public bool			mbVisible;			

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

		static GBSPFace MergeFace(GBSPFace Face1, GBSPFace Face2, PlanePool pool)
		{
			Vector3		[]Edge1	=new Vector3[2];
			Int32		i, k, NumVerts, NumVerts2;
			Int32		[]EdgeIndex	=new Int32[2];
			Int32		NumNewVerts;
			GBSPPoly	NewPoly = null, Poly1, Poly2;
			Vector3		Normal1, Normal2, Vec1, Vec2;
			GBSPFace	NewFace	=null;
			float		Dot;
			//int32		Start, End;
			bool		Keep1	=true, Keep2	=true;

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

			if(Poly1.mVerts.Count == -1 || Poly2.mVerts.Count == -1)
			{
				return	null;
			}

			NumVerts	=Poly1.mVerts.Count;

			//
			// Go through each edge of Poly1, and see if the reverse of it exist in Poly2
			//
			for(i=0;i < NumVerts;i++)		
			{
				Edge1[1]	=Poly1.mVerts[i];
				Edge1[0]	=Poly1.mVerts[(i + 1) % NumVerts];

				if(Poly2.EdgeExist(Edge1, out EdgeIndex))
				{
					break;
				}
			}

			if(i >= NumVerts)							// Did'nt find an edge, return nothing
			{
				return	null;
			}

			NumVerts2	=Poly2.mVerts.Count;

			//
			//	See if the 2 joined make a convex poly, connect them, and return new one
			//
			Normal1	=pool.mPlanes[Face1.mPlaneNum].mNormal;	// Get the normal
			if(Face1.mPlaneSide != 0)
			{
				Normal1	=Vector3.Zero - Normal1;
			}

			//Get the normal of the edge just behind edge1
			Vec1	=Poly1.mVerts[(i + NumVerts - 1) % NumVerts];
			Vec1	-=Edge1[1];

			Normal2	=Vector3.Cross(Normal1, Vec1);
			Normal2.Normalize();

			Vec2	=Poly2.mVerts[(EdgeIndex[1] + 1) % NumVerts2] - Poly2.mVerts[EdgeIndex[1]];

			Dot		=Vector3.Dot(Vec2, Normal2);
			if(Dot > COLINEAR_EPSILON)
			{
				return null;			//Edge makes a non-convex poly
			}
			if(Dot >= -COLINEAR_EPSILON)	//Drop point, on colinear edge
			{
				Keep1	=false;
			}

			//Get the normal of the edge just behind edge1
			Vec1	=Poly1.mVerts[(i+2)%NumVerts];
			Vec1	-=Edge1[0];

			Normal2	=Vector3.Cross(Normal1, Vec1);
			Normal2.Normalize();

			Vec2	=Poly2.mVerts[(EdgeIndex[0] + NumVerts2 - 1) % NumVerts2] -
						Poly2.mVerts[EdgeIndex[0]];

			Dot	=Vector3.Dot(Vec2, Normal2);
			if(Dot > COLINEAR_EPSILON)
			{
				return	null;	//Edge makes a non-convex poly
			}
			if(Dot >= -COLINEAR_EPSILON)	//Drop point, on colinear edge
			{
				Keep2	=false;
			}

			//if (NumVerts+NumVerts2 > 30)
			//	return null;
			
			NewFace			=new GBSPFace();
			NewFace.mPoly	=new GBSPPoly();
			NewPoly			=NewFace.mPoly;
			if(NewFace == null)
			{
				Map.Print("*WARNING* MergeFace:  Out of memory for new face!\n");
				return	null;
			}

			//
			// Make a new poly, free the old ones...
			//
			NumNewVerts	=0;

			for(k = (i + 1) % NumVerts;k != i;k = (k + 1) % NumVerts)
			{
				if(k == (i + 1) % NumVerts && !Keep2)
				{
					continue;
				}
				NewPoly.mVerts.Add(Poly1.mVerts[k]);
				NumNewVerts++;
			}

			i	=EdgeIndex[0];

			for(k = (i + 1) % NumVerts2;k != i;k = (k + 1) % NumVerts2)
			{
				if(k == (i + 1) % NumVerts2 && !Keep1)
				{
					continue;
				}
				NewPoly.mVerts.Add(Poly2.mVerts[k]);
				NumNewVerts++;
			}

			NewFace			=new GBSPFace(Face2);
			NewFace.mPoly	=NewPoly;

			//Hook.Printf("Merged face: %i\n", NumNewVerts);

			Face1.mMerged	=NewFace;
			Face2.mMerged	=NewFace;

			return	NewFace;
		}


		internal static bool MergeFaceList2(GBSPFace Faces, PlanePool pool, ref int NumMerged)
		{
			GBSPFace	Face1, Face2, End, Merged;

			for(Face1 = Faces;Face1 != null;Face1 = Face1.mNext)
			{
				if(Face1.mPoly.mVerts.Count == -1)
				{
					continue;
				}

				if(Face1.mMerged != null || Face1.mSplit[0] != null || Face1.mSplit[1] != null)
				{
					continue;
				}

				for (Face2 = Faces ; Face2 != Face1 ; Face2 = Face2.mNext)
				{
					if(Face2.mPoly.mVerts.Count == -1)
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
				mPoly.mVerts.Clear();
			}
		}


		private bool Check(bool Verb, PlanePool pool)
		{
			Int32		i, j;
			Vector3		Vect1, Normal, V1, V2, EdgeNormal;
			float		Dist, PDist, EdgeDist;
			
			if(mPoly.mVerts.Count < 3)
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
			for(i=0;i < mPoly.mVerts.Count;i++)
			{
				V1	=mPoly.mVerts[i];
				V2	=mPoly.mVerts[(i + 1) % mPoly.mVerts.Count];

				//Check for degenreate edge
				Vect1	=V2 - V1;
				Dist	=Vect1.Length();
				if(Math.Abs(Dist) < GBSPPoly.DEGENERATE_EPSILON)
				{
					if(Verb)
					{
						Map.Print("WARNING CheckFace:  Degenerate Edge.\n");
					}
					return	false;
				}

				//Check for planar
				Dist	=Vector3.Dot(V1, Normal) - PDist;
				if(Dist > UtilityLib.Mathery.ON_EPSILON
					|| Dist < -UtilityLib.Mathery.ON_EPSILON)
				{
					if(Verb)
					{
						Map.Print("WARNING CheckFace:  Non planar: " + Dist + "\n");
					}
					return	false;
				}

				EdgeNormal	=Vector3.Cross(Normal, Vect1);
				EdgeNormal.Normalize();
				EdgeDist	=Vector3.Dot(V1, EdgeNormal);
				
				//Check for convexity
				for(j=0;j < mPoly.mVerts.Count;j++)
				{
					Dist	=Vector3.Dot(mPoly.mVerts[j], EdgeNormal) - EdgeDist;
					if(Dist > UtilityLib.Mathery.ON_EPSILON)
					{
						if(Verb)
						{
							Map.Print("CheckFace:  Face not convex.\n");
						}
						return	false;
					}
				}
			}
			return	true;
		}


		internal void GetTriangles(List<Vector3> verts, List<uint> indexes, bool bCheckFlags)
		{
			if(mPoly != null)
			{
				mPoly.GetTriangles(verts, indexes, bCheckFlags);
			}
		}


		internal bool FixTJunctions(FaceFixer ff, TexInfoPool tip)
		{
			return	ff.FixTJunctions(ref mIndexVerts, tip.mTexInfos[mTexInfo]);
		}


		internal bool GetFaceVertIndexNumbers(FaceFixer ff)
		{
			mIndexVerts		=ff.IndexFaceVerts(mPoly.mVerts);
			return	true;
		}
	}
}