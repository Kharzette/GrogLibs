using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;


namespace BSPCore
{
	internal class GBSPFace
	{
		GBSPFace	mOriginal;
		GBSPPoly	mPoly;
		UInt32		mFrontContents, mBackContents;
		Int32		mTexInfo;
		Int32		mPlaneNum;
		Int32		mPlaneSide;

		Int32		mEntity;	//Originating entity

		bool		mbVisible;			

		//For GFX file saving
		internal Int32	mOutputNum;	

		internal Int32	[]mIndexVerts;
		internal Int32	mFirstIndexVert;

		internal GBSPPortal	mPortal;
		internal GBSPFace	mSplit0, mSplit1;
		internal GBSPFace	mMerged;

		internal const float	COLINEAR_EPSILON	=0.0001f;


		internal GBSPFace() { }
		internal GBSPFace(GBSPFace copyMe)
		{
			mPoly			=new GBSPPoly(copyMe.mPoly);
			mOriginal		=copyMe.mOriginal;
			mFrontContents	=copyMe.mFrontContents;
			mBackContents	=copyMe.mBackContents;
			mTexInfo		=copyMe.mTexInfo;
			mPlaneNum		=copyMe.mPlaneNum;
			mPlaneSide		=copyMe.mPlaneSide;
			mEntity			=copyMe.mEntity;
			mbVisible		=copyMe.mbVisible;
			mOutputNum		=copyMe.mOutputNum;
			mIndexVerts		=copyMe.mIndexVerts;
			mFirstIndexVert	=copyMe.mFirstIndexVert;
			mPortal			=copyMe.mPortal;
			mSplit0			=copyMe.mSplit0;
			mSplit1			=copyMe.mSplit1;
			mMerged			=copyMe.mMerged;
		}


		internal GBSPFace(GBSPPortal port, Int32 pside)
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


		static GBSPFace MergeFace(GBSPFace face1, GBSPFace face2, PlanePool pool)
		{
			//
			// Planes and Sides MUST match before even trying to merge
			//
			if(face1.mPlaneNum != face2.mPlaneNum)
			{
				return	null;
			}
			if (face1.mPlaneSide != face2.mPlaneSide)
			{
				return null;
			}

			if((face1.mFrontContents & Contents.BSP_MERGE_SEP_CONTENTS)
				!= (face2.mFrontContents & Contents.BSP_MERGE_SEP_CONTENTS))
			{
				return	null;
			}
			if((face1.mBackContents & Contents.BSP_MERGE_SEP_CONTENTS)
				!= (face2.mBackContents & Contents.BSP_MERGE_SEP_CONTENTS))
			{
				return	null;
			}

			if(face1.mTexInfo != face2.mTexInfo)
			{
				return	null;
			}
			
			GBSPPoly	poly1	=face1.mPoly;
			GBSPPoly	poly2	=face2.mPoly;

			Vector3	norm	=pool.mPlanes[face1.mPlaneNum].mNormal;	// Get the normal
			if(face1.mPlaneSide != 0)
			{
				norm	=Vector3.Zero - norm;
			}

			GBSPPoly	newPoly	=GBSPPoly.Merge(poly1, poly2, norm, pool);
			if(newPoly == null)
			{
				return	null;
			}			
			GBSPFace	newFace		=new GBSPFace(face2);
			newFace.mPoly	=newPoly;
			if(newFace == null)
			{
				CoreEvents.Print("*WARNING* MergeFace:  Out of memory for new face!\n");
				return	null;
			}

			face1.mMerged	=newFace;
			face2.mMerged	=newFace;

			return	newFace;
		}


		internal static bool MergeFaceList(List<GBSPFace> faces, PlanePool pool, ref int numMerged)
		{
			restartMerge:
			for(int i=0;i < faces.Count;i++)
			{
				GBSPFace	face1	=faces[i];
				if(face1.mPoly.VertCount() == -1)
				{
					continue;
				}

				if(face1.mMerged != null || face1.mSplit0 != null || face1.mSplit1 != null)
				{
					continue;
				}

				for(int j=0;j < faces.Count;j++)
				{
					GBSPFace	face2	=faces[j];
					if(face2 == face1)
					{
						break;
					}
					if(face2.mPoly.VertCount() == -1)
					{
						continue;
					}

					if(face2.mMerged != null || face2.mSplit0 != null || face2.mSplit1 != null)
					{
						continue;
					}
					
					GBSPFace	merged	=MergeFace(face1, face2, pool);

					if(merged == null)
					{
						continue;
					}

					merged.mPoly.RemoveDegenerateEdges();
					
					if(!merged.Check(false, pool))
					{
						merged.Free();
						face1.mMerged	=null;
						face2.mMerged	=null;
						continue;
					}

					numMerged++;

					//Add the Merged to the end of the face list 
					//so it will be checked against all the faces again
					faces.Add(merged);
					goto	restartMerge;
				}
			}
			return	true;
		}


		internal static void DumpFaceList(List<GBSPFace> faces, PlanePool pp, string fileName)
		{
			FileStream		fs	=new FileStream(fileName, FileMode.Create, FileAccess.Write);
			BinaryWriter	bw	=new BinaryWriter(fs);

			bw.Write(faces.Count);

			foreach(GBSPFace f in faces)
			{
				f.mPoly.Write(bw);

				Vector3	center	=f.mPoly.Center();

				GBSPPlane	p	=pp.mPlanes[f.mPlaneNum];

				if(f.mPlaneSide != 0)
				{
					p.Inverse();
				}

				bw.Write(center.X);
				bw.Write(center.Y);
				bw.Write(center.Z);

				center	+=(p.mNormal * 5.0f);

				bw.Write(center.X);
				bw.Write(center.Y);
				bw.Write(center.Z);
			}

			bw.Close();
			fs.Close();
		}


		void Free()
		{
			if(mPoly != null)
			{
				mPoly.Free();
			}
		}


		bool Check(bool bVerb, PlanePool pool)
		{
			if(mPoly.VertCount() < 3)
			{
				if(bVerb)
				{
					CoreEvents.Print("CheckFace:  NumVerts < 3.\n");
				}
				return	false;
			}
			
			Vector3	norm	=pool.mPlanes[mPlaneNum].mNormal;
			float	dist	=pool.mPlanes[mPlaneNum].mDist;
			if(mPlaneSide != 0)
			{
				norm	=-norm;
				dist	=-dist;
			}

			//
			//	Check for degenerate edges, convexity, and make sure it's planar
			//
			return	mPoly.Check(bVerb, norm, dist);
		}


		internal void SetContents(int idx, UInt32 val)
		{
			if(idx == 0)
			{
				mFrontContents	=val;
			}
			else
			{
				mBackContents	=val;
			}
		}


		internal UInt32 GetContents(int idx)
		{
			if(idx == 0)
			{
				return	mFrontContents;
			}
			return	mBackContents;
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


		static internal bool GetFaceListVertIndexNumbers(List<GBSPFace> list, FaceFixer ff)
		{
			foreach(GBSPFace f in list)
			{
				if(f.mMerged != null
					|| f.mSplit0 != null
					|| f.mSplit1 != null)
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


		static internal bool FixFaceListTJunctions(List<GBSPFace> list, FaceFixer ff, TexInfoPool tip)
		{
			foreach(GBSPFace f in list)
			{
				if(f.mMerged != null
					|| f.mSplit0 != null
					|| f.mSplit1 != null)
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
		internal static int PrepFaceList(List<GBSPFace> list, NodeCounter nc)
		{
			int	numFaces	=0;
			foreach(GBSPFace f in list)
			{
				if(!f.mbVisible)
				{
					continue;
				}

				if(f.mMerged != null ||
					f.mSplit0 != null ||
					f.mSplit1 != null)
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


		internal static void ConvertListToGFXAndSave(List<GBSPFace> list, BinaryWriter bw)
		{
			foreach(GBSPFace f in list)
			{
				if(!f.mbVisible)
				{
					continue;
				}

				if(f.mMerged != null
					|| f.mSplit0 != null
					|| f.mSplit1 != null)
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