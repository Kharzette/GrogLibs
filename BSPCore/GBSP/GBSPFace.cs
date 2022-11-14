using System;
using System.IO;
using System.Numerics;
using System.Collections.Generic;
using Vortice.Mathematics;


namespace BSPCore
{
	internal class GBSPFace
	{
		GBSPPoly	mPoly;
		UInt32		mFrontContents, mBackContents;
		Int32		mTexInfo;
		Int32		mPlaneNum;
		bool		mbFlipSide;

		Int32		mEntity;	//Originating entity

		bool		mbVisible;			

		//For GFX file saving
		internal Int32	mOutputNum;	

		internal Int32	[]mIndexVerts;
		internal Int32	mFirstIndexVert;

		internal GBSPPortal	mPortal;

		internal const float	COLINEAR_EPSILON	=0.0001f;


		internal GBSPFace() { }
		internal GBSPFace(GBSPFace copyMe)
		{
			mPoly			=new GBSPPoly(copyMe.mPoly);
			mFrontContents	=copyMe.mFrontContents;
			mBackContents	=copyMe.mBackContents;
			mTexInfo		=copyMe.mTexInfo;
			mPlaneNum		=copyMe.mPlaneNum;
			mbFlipSide		=copyMe.mbFlipSide;
			mEntity			=copyMe.mEntity;
			mbVisible		=copyMe.mbVisible;
			mOutputNum		=copyMe.mOutputNum;
			mIndexVerts		=copyMe.mIndexVerts;
			mFirstIndexVert	=copyMe.mFirstIndexVert;
			mPortal			=copyMe.mPortal;
		}


		internal GBSPFace(GBSPPortal port, bool bFlip)
		{
			mTexInfo	=port.mSide.mTexInfo;
			mPlaneNum	=port.mSide.mPlaneNum;
			mbFlipSide	=bFlip;
			mPortal		=port;
			mbVisible	=true;

			if(bFlip)
			{
				mPoly	=new GBSPPoly(port.mPoly);
				mPoly.Reverse();
			}
			else
			{
				mPoly	=new GBSPPoly(port.mPoly);
			}
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

				if(f.mbFlipSide)
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
			if(mbFlipSide)
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


		internal void GetTriangles(
			Color matColor,
			PlanePool pp,
			List<Vector3> verts,
			List<Vector3> norms,
			List<Color> colors,
			List<UInt16> indexes, bool bCheckFlags)
		{
			if(mPoly != null)
			{
				mPoly.GetTriangles(pp.mPlanes[mPlaneNum], matColor, verts, norms, colors, indexes, bCheckFlags);
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

				if(f.mIndexVerts.Length > 0)
				{
					GFXFace	GFace	=new GFXFace();

					GFace.mFirstVert	=f.mFirstIndexVert;
					GFace.mNumVerts		=f.mIndexVerts.Length;
					GFace.mPlaneNum		=f.mPlaneNum;
					GFace.mbFlipSide	=f.mbFlipSide;
					GFace.mTexInfo		=f.mTexInfo;
					GFace.mLWidth		=0;
					GFace.mLHeight		=0;
					GFace.mLightOfs		=-1;	//No light info yet
					GFace.mLType0		=255;	//Of course, no styles yet either
					GFace.mLType1		=255;
					GFace.mLType2		=255;
					GFace.mLType3		=255;

					GFace.Write(bw);
				}
			}
		}
	}
}