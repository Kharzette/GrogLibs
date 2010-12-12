using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public partial class Map
	{
		//light related stuff
		Vector3							[]VertNormals;
		Dictionary<Int32, DirectLight>	DirectClusterLights	=new Dictionary<Int32, DirectLight>();
		List<DirectLight>				DirectLights		=new List<DirectLight>();
		LInfo							[]mLightMaps;
		FInfo							[]mFaceInfos;
		RADPatch						[]mFacePatches;
		RADPatch						[]mPatchList;
		Int32							NumPatches, NumReceivers;


		void CalcPatchReflectivity(Int32 Face, RADPatch Patch)
		{
//			GFXTexture		*pTexture;
//			Vector3			Color;
//			Int32			i, Size;
//			byte			*pGFXTexData;
//			DRV_Palette		*Palette;
			GFXTexInfo		pTexInfo;
//			float			Scale;
			
			pTexInfo	=mGFXTexInfos[mGFXFaces[Face].mTexInfo];
//			pTexture = &GFXTextures[pTexInfo->Texture];

//			pGFXTexData = &GFXTexData[pTexture->Offset];
//			Size = pTexture->Width*pTexture->Height;

//			Palette = &GFXPalettes[pTexture->PaletteIndex];

//			for (i=0; i< Size; i++, pGFXTexData++)
//			{
//				DRV_RGB *	RGB;

//				RGB = &(*Palette)[*pGFXTexData];
//				Color.X += (geFloat)RGB->r;
//				Color.Y += (geFloat)RGB->g;
//				Color.Z += (geFloat)RGB->b;
//			}

//			geVec3d_Scale(&Color, 1.0f/(geFloat)Size, &Color);
//			geVec3d_Scale(&Color, 1.0f/255.0f, &Color);

//			Scale = ColorNormalize(&Color, &Patch->Reflectivity);
			
//			if (Scale < 0.5f)
//			{
//				Scale *= 2;
//				geVec3d_Scale(&Patch->Reflectivity, Scale, &Patch->Reflectivity);
//			}

			//hard coding a value here till I get textures
			Patch.mReflectivity	=Vector3.One * 100.0f;
			Patch.mReflectivity	*=pTexInfo.mReflectiveScale;

//			geVec3d_Scale(&Patch->Reflectivity, ReflectiveScale*pTexInfo->ReflectiveScale, &Patch->Reflectivity);
		}


		bool PatchNeedsSplit(RADPatch Patch, out GBSPPlane Plane)
		{
			Int32	i;

			if(mLightParams.mbFastPatch)
			{
				float	Dist;
				
				for(i=0;i < 3;i++)
				{
					Dist	=UtilityLib.Mathery.VecIdx(Patch.mBounds.mMaxs, i)
								- UtilityLib.Mathery.VecIdx(Patch.mBounds.mMins, i);
					
					if(Dist > mLightParams.mPatchSize)
					{
						//Cut it right through the center...
						Plane.mNormal	=Vector3.Zero;
						UtilityLib.Mathery.VecIdxAssign(ref Plane.mNormal, i, 1.0f);
						Plane.mDist	=(UtilityLib.Mathery.VecIdx(Patch.mBounds.mMaxs, i)
							+ UtilityLib.Mathery.VecIdx(Patch.mBounds.mMins, i))
								/ 2.0f;
						Plane.mType	=GBSPPlane.PLANE_ANY;
						return	true;
					}
				}
			}
			else
			{
				float	Min, Max;
				for(i=0;i < 3;i++)
				{
					Min	=UtilityLib.Mathery.VecIdx(Patch.mBounds.mMins, i) + 1.0f;
					Max	=UtilityLib.Mathery.VecIdx(Patch.mBounds.mMaxs, i) - 1.0f;

					if(Math.Floor(Min / mLightParams.mPatchSize)
						< Math.Floor(Max / mLightParams.mPatchSize))
					{
						Plane.mNormal	=Vector3.Zero;
						UtilityLib.Mathery.VecIdxAssign(ref Plane.mNormal, i, 1.0f);
						Plane.mDist	=mLightParams.mPatchSize * (1.0f + (float)Math.Floor(Min / mLightParams.mPatchSize));
						Plane.mType	=GBSPPlane.PLANE_ANY;
						return	true;
					}
				}
			}
			Plane	=new GBSPPlane();
			return	false;
		}


		RADPatch SubdivideFacePatches(RADPatch Patch)
		{
			RADPatch	CPatch, NewPatch, NextPatch;
			GBSPPoly	Poly, FPoly, BPoly;
			GBSPPlane	Plane;

			for(CPatch=Patch;CPatch != null;CPatch=NextPatch)
			{
				NextPatch	=CPatch.mNext;

				if(PatchNeedsSplit(CPatch, out Plane))
				{
					NumPatches++;

					Poly	=CPatch.mPoly;
					if(!Poly.Split(Plane, out FPoly, out BPoly, false))
					{
						return	null;
					}
					
					if(FPoly == null || BPoly == null)
					{
						Print("SubdivideFacePatches:  Patch was not split.\n");
						return	null;
					}
					
					NewPatch	=new RADPatch();
					if(NewPatch == null)
					{
						Print("SubdivideFacePatches:  Out of memory for new patch.\n");
						return	null;
					}

					//Make it take on all the attributes of it's parent
					NewPatch.mArea			=CPatch.mArea;
					NewPatch.mBounds		=CPatch.mBounds;
					NewPatch.mLeaf			=CPatch.mLeaf;
					NewPatch.mNumReceivers	=CPatch.mNumReceivers;
					NewPatch.mNumSamples	=CPatch.mNumSamples;
					NewPatch.mOrigin		=CPatch.mOrigin;
					NewPatch.mPlane			=CPatch.mPlane;
					NewPatch.mRadFinal		=CPatch.mRadFinal;
					NewPatch.mRadReceive	=CPatch.mRadReceive;
					NewPatch.mRadSend		=CPatch.mRadSend;
					NewPatch.mRadStart		=CPatch.mRadStart;
					NewPatch.mReceivers		=CPatch.mReceivers;
					NewPatch.mReflectivity	=CPatch.mReflectivity;

					NewPatch.mNext	=NextPatch;
					NewPatch.mPoly	=FPoly;
					if(!NewPatch.CalcInfo())
					{
						Print("SubdivideFacePatches:  Could not calculate patch info.\n");
						return	null;
					}

					//Re-use the first patch
					CPatch.mNext	=NewPatch;
					CPatch.mPoly	=BPoly;

					if(!CPatch.CalcInfo())
					{
						Print("SubdivideFacePatches:  Could not calculate patch info.\n");
						return	null;
					}

					NextPatch	=CPatch;	// Keep working from here till satisfied...
				}
			}
			return Patch;
		}


		bool BuildPatch(Int32 Face)
		{
			mFacePatches[Face]	=new RADPatch();
			if(mFacePatches[Face] == null)
			{
				Print("BuildPatch:  Could not allocate patch.\n");
				return	false;
			}

			CalcPatchReflectivity(Face, mFacePatches[Face]);
			
			mFacePatches[Face].mPoly	=new GBSPPoly(mGFXFaces[Face], mGFXVertIndexes, mGFXVerts);

			if(!mFacePatches[Face].CalcInfo())
			{
				Print("BuildPatch:  Could not calculate patch info.\n");
				return	false;
			}

			mFacePatches[Face]	=SubdivideFacePatches(mFacePatches[Face]);

			if(mFacePatches[Face] == null)
			{
				Print("BuildPatch:  Could not subdivide patch.\n");
				return	false;
			}
			return	true;
		}


		bool FinalizePatchInfo(Int32 Face, RADPatch Patch)
		{
			GBSPPoly	Poly;

			Poly	=Patch.mPoly;

			if(Poly == null)
			{
				Print("FinalizePatchInfo:  No Poly!\n");
				return	false;
			}

			Patch.mOrigin	=Poly.Center();

			Patch.mPlane.mNormal	=mGFXPlanes[mGFXFaces[Face].mPlaneNum].mNormal;
			Patch.mPlane.mDist		=mGFXPlanes[mGFXFaces[Face].mPlaneNum].mDist;
			Patch.mPlane.mType		=GBSPPlane.PLANE_ANY;

			if(mGFXFaces[Face].mPlaneSide != 0)
			{
				Patch.mPlane.Inverse();
			}
			Patch.mOrigin	+=Patch.mPlane.mNormal * 2.0f;

			Int32	nodeLandedIn	=FindNodeLandedIn(0, Patch.mOrigin);
			Patch.mLeaf	=-(nodeLandedIn + 1);

			Patch.mArea	=Patch.mPoly.Area();
			if(Patch.mArea < 1.0f)
			{
				Patch.mArea	=1.0f;
			}
			Patch.mPoly	=null;

			return	true;
		}


		bool BuildPatches()
		{
			Int32	i;

			Print("--- Build Patches --- \n");

			mFacePatches	=new RADPatch[mGFXFaces.Length];
			if(mFacePatches == null)
			{
				Print("BuildPatches:  Not enough memory for patches.\n");
				return	false;
			}

			for(i=0;i < mFacePatches.Length;i++)
			{
				if(!BuildPatch(i))
				{
					return	false;
				}
			}

			if(!FinalizePatches())
			{
				Print("BuildPatches:  Could not finalize face patches.\n");
				return	false;
			}

			if(mBSPParms.mbVerbose)
			{
				Print("Num Patches          : " + mFacePatches.Length + "\n");
			}
			return	true;
		}


		bool FinalizePatches()
		{
			RADPatch	Patch;
			Int32		i, k;

			NumPatches	=0;
			for(i=0;i < mGFXFaces.Length;i++)
			{
				for(Patch=mFacePatches[i];Patch!=null;Patch=Patch.mNext)
				{
					FinalizePatchInfo(i, Patch);
					NumPatches++;
				}
			}
			mPatchList	=new RADPatch[NumPatches];
			if(mPatchList == null)
			{
				Print("FinalizePatches:  Out of memory for patch list.\n");
				return	false;
			}
			
			//Build the patch list, so we can use indexing, instead of pointers (for receivers)...
			k	=0;
			for(i=0;i < mGFXFaces.Length;i++)
			{
				for(Patch=mFacePatches[i];Patch != null;Patch=Patch.mNext)
				{
					mPatchList[k]	=Patch;
					k++;
				}
			}
			return	true;
		}


		bool LoadReceiverFile(string fileName)
		{
			if(!UtilityLib.FileUtil.FileExists(fileName))
			{
				return	false;
			}
			FileStream	fs	=UtilityLib.FileUtil.OpenTitleFile(fileName,
								FileMode.Open, FileAccess.Read);

			if(fs == null)
			{
				return	false;
			}

			BinaryReader	br	=new BinaryReader(fs);

			UInt32		ver			=br.ReadUInt32();
			DateTime	dt			=DateTime.FromBinary(br.ReadInt64());
			Int32		numPatches	=br.ReadInt32();

			if(ver != GBSPChunk.GBSP_VERSION)
			{
				Print("*WARNING*  LoadReceiverFile:  Versions do not match, skipping...\n");
				br.Close();
				fs.Close();
				return	false;
			}
			
			//Make sure the number of patches in the receiver file
			//matches the number loaded for this BSP
			if(numPatches != NumPatches)
			{
				Print("*WARNING*  LoadReceiverFile:  NumPatches do not match, skipping...\n");
				br.Close();
				fs.Close();
				return	false;
			}

			//Load Patch receivers
			for(int i=0; i< NumPatches; i++)
			{
				Int32 numReceivers	=br.ReadInt32();

				mPatchList[i].mReceivers	=new RADReceiver[numReceivers];

				for(int j=0;j < numReceivers;j++)
				{
					mPatchList[i].mReceivers[j]	=new RADReceiver();
					mPatchList[i].mReceivers[j].Read(br);
				}
			}

			br.Close();
			fs.Close();

			return	true;
		}


		bool SaveReceiverFile(string fileName)
		{
			Print("--- Save Receiver File --- \n");

			FileStream	fs	=UtilityLib.FileUtil.OpenTitleFile(fileName,
								FileMode.Create, FileAccess.Write);

			if(fs == null)
			{
				Print("SaveReceiverFile:  Could not open receiver file for writing...\n");
				return	false;
			}

			BinaryWriter	bw	=new BinaryWriter(fs);

			bw.Write(GBSPChunk.GBSP_VERSION);
			bw.Write(DateTime.Now.ToBinary());
			bw.Write(NumPatches);

			//Patches
			for(int i=0;i < NumPatches;i++)
			{
				Int32 numReceivers	=mPatchList[i].mNumReceivers;

				bw.Write(numReceivers);

				for(int j=0;j < numReceivers;j++)
				{
					mPatchList[i].mReceivers[j].Write(bw);
				}			
			}

			bw.Close();
			fs.Close();

			return	true;
		}


		void SendPatch(RADPatch Patch)
		{
			Vector3		Send;
			RADPatch	RPatch;
			Int32		k;
			RADReceiver	Receiver;

			Send	=Patch.mRadSend / (float)0x10000;

			//Send light out to each pre-computed receiver
			for(k=0;k < Patch.mNumReceivers;k++)
			{
				Receiver	=Patch.mReceivers[k];
				RPatch		=mPatchList[Receiver.mPatch];

				RPatch.mRadReceive	+=Send * Receiver.mAmount;
			}
		}


		float CollectPatchLight()
		{
			float	total	=0.0f;
			
			for(int i=0;i < NumPatches;i++)
			{
				RADPatch	patch	=mPatchList[i];
				
				//Add receive amount to Final amount
				patch.mRadFinal	+=patch.mRadReceive / patch.mArea;
				patch.mRadSend	=patch.mRadReceive * patch.mReflectivity;

				total	+=patch.mRadSend.X + patch.mRadSend.Y + patch.mRadSend.Z;

				patch.mRadReceive	=Vector3.Zero;
			}
			return	total;
		}


		bool BouncePatches()
		{
			Int32		i, j;
			RADPatch	Patch;
			float		Total;

			Print("--- Bounce Patches --- \n");
			
			for(i=0;i < NumPatches;i++)
			{
				//Set each patches first pass send amount with what was obtained
				//from their lightmaps... 
				Patch			=mPatchList[i];
				Patch.mRadSend	=Patch.mRadStart * Patch.mReflectivity * Patch.mArea;
			}

			for(i=0;i < mLightParams.mNumBounces;i++)
			{
				if(mBSPParms.mbVerbose)
				{
					Print("Bounce: " + (i + 1) + ",");
				}
				
				//For each patch, send it's energy to each pre-computed receiver
				for(j=0;j < NumPatches;j++)
				{
					Patch	=mPatchList[j];
					SendPatch(Patch);
				}

				//For each patch, collect any light it might have received
				//and throw into patch RadFinal
				Total	=CollectPatchLight();

				if(mBSPParms.mbVerbose)
				{
					Print("Energy: " + Total + "\n");
				}
			}
			
			for(j=0;j < NumPatches;j++)
			{
				Patch	=mPatchList[j];
				if(!CheckPatch(Patch))
				{
					return	false;
				}
			}
			return	true;
		}


		PlaneFace	[]LinkPlaneFaces()
		{
			PlaneFace	PFace;
			Int32		i, PlaneNum;

			PlaneFace	[]ret	=new PlaneFace[mGFXPlanes.Length];

			for(i=0;i < mGFXFaces.Length;i++)
			{
				PFace			=new PlaneFace();
				PlaneNum		=mGFXFaces[i].mPlaneNum;
				PFace.mGFXFace	=i;
				PFace.mNext		=ret[PlaneNum];
				ret[PlaneNum]	=PFace;
			}
			return	ret;
		}


		void GetFaceMinsMaxs(Int32 Face, out Bounds bnd)
		{
			Int32	i, Index;

			bnd	=new Bounds();
			for(i=0;i < mGFXFaces[Face].mNumVerts;i++)
			{
				Index	=mGFXVertIndexes[mGFXFaces[Face].mFirstVert + i];
				bnd.AddPointToBounds(mGFXVerts[Index]);
			}			
		}


		bool AddPointToTriangulation(RADPatch patch, TriPatch TriPatch)
		{
			int	pnum	=TriPatch.mNumPoints;
			if(pnum == TriPatch.MAX_TRI_POINTS)
			{
				Print("TriPatch->NumPoints == MAX_TRI_POINTS");
				return	false;
			}
			TriPatch.mPoints[pnum]	=patch;
			TriPatch.mNumPoints++;

			return	true;
		}


		TriEdge FindEdge(TriPatch TriPatch, int p0, int p1)
		{
			TriEdge	e, be;
			Vector3	v1;
			Vector3	normal;
			float	dist;

			if(TriPatch.mEdgeMatrix[p0][p1] != null)
			{
				return	TriPatch.mEdgeMatrix[p0][p1];
			}

			if(TriPatch.mNumEdges > TriPatch.MAX_TRI_EDGES - 2)
			{
				Print("TriPatch.mNumEdges > MAX_TRI_EDGES - 2");
				return	null;
			}

			v1	=TriPatch.mPoints[p1].mOrigin - TriPatch.mPoints[p0].mOrigin;
			v1.Normalize();

			normal	=Vector3.Cross(v1, TriPatch.mPlane.mNormal);
			dist	=Vector3.Dot(TriPatch.mPoints[p0].mOrigin, normal);

			e			=TriPatch.mEdges[TriPatch.mNumEdges];
			e.p0		=p0;
			e.p1		=p1;
			e.mTri		=null;
			e.mNormal	=normal;
			e.mDist		=dist;
			TriPatch.mNumEdges++;
			TriPatch.mEdgeMatrix[p0][p1]	=e;

			//Go ahead and make the reverse edge ahead of time
			be			=TriPatch.mEdges[TriPatch.mNumEdges];
			be.p0		=p1;
			be.p1		=p0;
			be.mTri		=null;
			be.mNormal	=-normal;
			be.mDist	=-dist;
			TriPatch.mNumEdges++;
			TriPatch.mEdgeMatrix[p1][p0]	=be;

			return	e;
		}


		Tri	AllocTriangle(TriPatch TriPatch)
		{
			if(TriPatch.mNumTris >= TriPatch.MAX_TRI_TRIS)
			{
				Print("TriPatch->NumTris >= MAX_TRI_TRIS");
				return	null;
			}
			TriPatch.mTriList[TriPatch.mNumTris]	=new Tri();
			Tri	ret	=TriPatch.mTriList[TriPatch.mNumTris];
			TriPatch.mNumTris++;

			return	ret;
		}


		bool Tri_Edge_r(TriPatch TriPatch, TriEdge e)
		{
			int		i, bestp	=0;
			Vector3	v1, v2;
			Vector3	p0, p1, p;
			float	best, ang;
			Tri		nt;
			TriEdge	e2;

			if(e.mTri != null)
			{
				return	true;
			}

			p0		=TriPatch.mPoints[e.p0].mOrigin;
			p1		=TriPatch.mPoints[e.p1].mOrigin;
			best	=1.1f;
			for(i=0;i < TriPatch.mNumPoints;i++)
			{
				p	=TriPatch.mPoints[i].mOrigin;

				if(Vector3.Dot(p, e.mNormal) - e.mDist < 0.0f)
				{
					continue;
				}

				v1	=p0 - p;
				v2	=p1 - p;

				if(v1.Length() == 0.0f)
				{
					continue;
				}
				if(v2.Length() == 0.0f)
				{
					continue;
				}

				v1.Normalize();
				v2.Normalize();				
				
				ang	=Vector3.Dot(v1, v2);
				if(ang < best)
				{
					best	=ang;
					bestp	=i;
				}
			}
			if(best >= 1)
			{
				return true;
			}
			
			nt	=AllocTriangle(TriPatch);
			if(nt == null)
			{
				Print("Tri_Edge_r:  Could not allocate triangle.\n");
				return	false;
			}
			nt.mEdges[0]	=e;
			if(nt.mEdges[0] == null)
			{
				Print("Tri_Edge_r:  There was an error finding an edge.\n");
				return	false;
			}
			nt.mEdges[1]	=FindEdge(TriPatch, e.p1, bestp);
			if(nt.mEdges[1] == null)
			{
				Print("Tri_Edge_r:  There was an error finding an edge.\n");
				return	false;
			}
			nt.mEdges[2]	=FindEdge(TriPatch, bestp, e.p0);
			if(nt.mEdges[2] == null)
			{
				Print("Tri_Edge_r:  There was an error finding an edge.\n");
				return	false;
			}
			for(i=0;i < 3;i++)
			{
				nt.mEdges[i].mTri	=nt;
			}

			e2	=FindEdge(TriPatch, bestp, e.p1);
			if(e2 == null)
			{
				Print("Tri_Edge_r:  There was an error finding an edge.\n");
				return	false;
			}
			if(!Tri_Edge_r(TriPatch, e2))
			{
				return	false;
			}
			
			e2	=FindEdge(TriPatch, e.p0, bestp);
			if(e2 == null)
			{
				Print("Tri_Edge_r:  There was an error finding an edge.\n");
				return	false;
			}
			if(!Tri_Edge_r(TriPatch, e2))
			{
				return	false;
			}
			return	true;
		}


		bool TriPointInside(Tri Tri, Vector3 Point)
		{
			for(int i=0;i < 3;i++)
			{
				float	Dist;
				TriEdge	pEdge;

				pEdge	=Tri.mEdges[i];

				Dist	=Vector3.Dot(pEdge.mNormal, Point) - pEdge.mDist;

				if(Dist < 0.0f)
				{
					return	false;
				}
			}
			return	true;
		}


		void LerpTriangle(TriPatch TriPatch, Tri t, Vector3 Point, out Vector3 color)
		{
			RADPatch	p1, p2, p3;
			Vector3		bse, d1, d2;
			float		x, y, y1, x2;

			p1	=TriPatch.mPoints[t.mEdges[0].p0];
			p2	=TriPatch.mPoints[t.mEdges[1].p0];
			p3	=TriPatch.mPoints[t.mEdges[2].p0];

			bse	=p1.mRadFinal;
			d1	=p2.mRadFinal - bse;
			d2	=p3.mRadFinal - bse;

			x	=Vector3.Dot(Point, t.mEdges[0].mNormal) - t.mEdges[0].mDist;
			y	=Vector3.Dot(Point, t.mEdges[2].mNormal) - t.mEdges[2].mDist;
			y1	=Vector3.Dot(p2.mOrigin, t.mEdges[2].mNormal) - t.mEdges[2].mDist;
			x2	=Vector3.Dot(p3.mOrigin, t.mEdges[0].mNormal) - t.mEdges[0].mDist;

			if(Math.Abs(y1) < UtilityLib.Mathery.ON_EPSILON
				|| Math.Abs(x2) < UtilityLib.Mathery.ON_EPSILON)
			{
				color	=bse;
				return;
			}

			color	=bse + d2 * (x / x2);
			color	+=d1 * (y / y1);
		}


		bool SampleTriangulation(Vector3 Point, TriPatch TriPatch, out Vector3 color)
		{
			Tri			t;
			TriEdge		e;
			float		d;
			RADPatch	p0, p1;
			Vector3		v1, v2;

			if(TriPatch.mNumPoints == 0)
			{
				color	=Vector3.Zero;
				return	true;
			}
			if(TriPatch.mNumPoints == 1)
			{
				color	=TriPatch.mPoints[0].mRadFinal;
				return	true;
			}
			
			//See of the Point is inside a tri in the patch
			for(int j=0;j < TriPatch.mNumTris;j++)
			{
				t	=TriPatch.mTriList[j];
				if(!TriPointInside(t, Point))
				{
					continue;
				}
				LerpTriangle(TriPatch, t, Point, out color);

				return	true;
			}
			
			for(int j=0;j < TriPatch.mNumEdges;j++)
			{
				e	=TriPatch.mEdges[j];
				if(e.mTri != null)
				{
					continue;		// not an exterior edge
				}

				d	=Vector3.Dot(Point, e.mNormal) - e.mDist;
				if(d < 0)
				{
					continue;	// not in front of edge
				}

				p0	=TriPatch.mPoints[e.p0];
				p1	=TriPatch.mPoints[e.p1];

				v1	=p1.mOrigin - p0.mOrigin;
				v1.Normalize();

				v2	=Point - p0.mOrigin;
				d	=Vector3.Dot(v2, v1);
				if(d < 0)
				{
					continue;
				}
				if(d > 1)
				{
					continue;
				}
				color	=p0.mRadFinal + (d * p1.mRadFinal -p0.mRadFinal);

				return	true;
			}
			
			if(!FindClosestTriPoint(Point, TriPatch, out color))
			{
				Print("SampleTriangulation:  Could not find closest Color.\n");
				return	false;
			}
			return	true;
		}


		bool FindClosestTriPoint(Vector3 Point, TriPatch Tri, out Vector3 col)
		{
			Int32		i;
			RADPatch	p0, BestPatch;
			float		BestDist, d;
			Vector3		v1;

			col	=Vector3.Zero;

			//Search for nearest Point
			BestDist	=TriPatch.MIN_MAX_BOUNDS2;
			BestPatch	=null;

			for(i=0;i < Tri.mNumPoints;i++)
			{
				p0	=Tri.mPoints[i];
				v1	=Point - p0.mOrigin;
				d	=v1.Length();
				if(d < BestDist)
				{
					BestDist	=d;
					BestPatch	=p0;
				}
			}
			if(BestPatch == null)
			{
				Print("FindClosestTriPoint: No Points.\n");
				return	false;
			}

			col	=BestPatch.mRadFinal;
			return	true;
		}


		bool TriangulatePoints(TriPatch TriPatch)
		{
			float	d, bestd;
			Vector3	v1;
			int		bp1, bp2, i, j;
			Vector3	p1, p2;
			TriEdge	e, e2;

			//zero out edgematrix
			for(i=0;i < TriPatch.mNumPoints;i++)
			{
				for(j=0;j < TriPatch.mNumPoints;j++)
				{
					TriPatch.mEdgeMatrix[i][j]	=new TriEdge();
				}
			}

			if(TriPatch.mNumPoints < 2)
			{
				return	true;
			}

			//Find the two closest Points
			bestd	=TriPatch.MIN_MAX_BOUNDS2;
			bp1		=0;
			bp2		=0;
			for(i=0;i < TriPatch.mNumPoints;i++)
			{
				p1	=TriPatch.mPoints[i].mOrigin;
				for(j=i+1;j < TriPatch.mNumPoints;j++)
				{
					p2	=TriPatch.mPoints[j].mOrigin;
					v1	=p2 - p1;
					d	=v1.Length();
					if(d < bestd && d > .05f)
					{
						bestd	=d;
						bp1		=i;
						bp2		=j;
					}
				}
			}

			e	=FindEdge(TriPatch, bp1, bp2);
			if(e == null)
			{
				Print("There was an error finding an edge.\n");
				return	false;
			}
			e2	=FindEdge(TriPatch, bp2, bp1);
			if(e2 == null)
			{
				Print("There was an error finding an edge.\n");
				return	false;
			}
			if(!Tri_Edge_r(TriPatch, e))
			{
				return	false;
			}
			if(!Tri_Edge_r(TriPatch, e2))
			{
				return	false;
			}
			return	true;
		}


		bool AbsorbPatches()
		{
			TriPatch	Tri;
			GBSPPlane	Plane;
			Vector3		Add;
			Vector3		pPoint;
			Int32		i, k, PNum, FNum, PSide;
			RADPatch	Patch, OPatch;
			PlaneFace	PFace;

			//We need all the faces that belong to each Plane
			PlaneFace	[]planeFaces	=LinkPlaneFaces();

			for(i=0;i < mGFXFaces.Length;i++)
			{
				UInt32	Flags;
				GFXFace	pGFXFace;

				pGFXFace	=mGFXFaces[i];

//				pPoint	=mFaceInfos[i].mPoints;
//				pRGB	=mLightMaps[i].RGBLData[0];

				Flags	=mGFXTexInfos[mGFXFaces[i].mTexInfo].mFlags;

				if (((Flags & TexInfo.TEXINFO_NO_LIGHTMAP) != 0)
					&& ((Flags & TexInfo.TEXINFO_GOURAUD)) == 0)
				{
					continue;
				}

				Plane.mNormal	=mGFXPlanes[mGFXFaces[i].mPlaneNum].mNormal;
				Plane.mDist		=mGFXPlanes[mGFXFaces[i].mPlaneNum].mDist;
				Plane.mType		=GBSPPlane.PLANE_ANY;

				Tri	=TriPatchCreate(Plane);
				if(Tri == null)
				{
					Print("AbsorbPatches:  Tri_PatchCreate failed.\n");
					return	false;
				}
				
				PNum	=mGFXFaces[i].mPlaneNum;
				PSide	=mGFXFaces[i].mPlaneSide;
				
				OPatch	=mFacePatches[i];

				Bounds	bounds;
				GetFaceMinsMaxs(i, out bounds);
				
				for(PFace=planeFaces[PNum];PFace != null;PFace=PFace.mNext)
				{
					FNum	=PFace.mGFXFace;

					if(mGFXFaces[FNum].mPlaneSide != PSide)
					{
						continue;
					}

					for(Patch=mFacePatches[FNum];Patch != null;Patch=Patch.mNext)
					{
						for(k=0;k < 3;k++)
						{
							if(UtilityLib.Mathery.VecIdx(Patch.mOrigin, k)
								< UtilityLib.Mathery.VecIdx(bounds.mMins, k) - (mLightParams.mPatchSize * 2))
							{
								break;
							}
							if(UtilityLib.Mathery.VecIdx(Patch.mOrigin, k)
								> UtilityLib.Mathery.VecIdx(bounds.mMaxs, k) + (mLightParams.mPatchSize * 2))
							{
								break;
							}
						}
						if(k != 3)
						{
							continue;
						}
						
						if(!AddPointToTriangulation(Patch, Tri))
						{
							Print("AbsorbPatches:  Could not add patch to triangulation.\n");
							return	false;
						}						
					}
				}
				if(!TriangulatePoints(Tri))
				{
					Print("AbsorbPatches:  Could not triangulate patches.\n");
					return	false;
				}
				
				if((Flags & TexInfo.TEXINFO_GOURAUD) != 0)
				{
					for(k=0;k < pGFXFace.mNumVerts;k++)
					{
						Int32	vn;

						vn	=pGFXFace.mFirstVert + k;

						pPoint	=mGFXVerts[mGFXVertIndexes[vn]];

						SampleTriangulation(pPoint, Tri, out Add);

						mGFXRGBVerts[vn]	+=Add;
					}
				}
				else
				{
					Vector3	[]rgb	=mLightMaps[i].GetRGBLightData(0);
					bool	Created	=(rgb != null);

					Vector3	[]facePoints	=mFaceInfos[i].GetPoints();

					int	rgbOfs	=0;				
					for(k=0;k < facePoints.Length;k++, rgbOfs++)
					{
						pPoint	=facePoints[k];
						if(!SampleTriangulation(pPoint, Tri, out Add))
						{
							Print("AbsorbPatches:  Could not sample from patch triangles.\n");
							continue;
						}

						if(!Created)
						{
							if(Add.X > 0 || Add.Y > 0 || Add.Z > 0)
							{
								mLightMaps[i].AllocLightType(0, facePoints.Length);
								Created	=true;
							}
						}
						if(Created)
						{
							rgb	=mLightMaps[i].GetRGBLightData(0);
							rgb[k]	+=Add;
						}
					}
				}
				Tri			=null;
			}

			planeFaces	=null;

			return	true;
		}


		TriPatch TriPatchCreate(GBSPPlane Plane)
		{
			TriPatch	Patch	=new TriPatch();

			Patch.mNumPoints	=0;
			Patch.mNumEdges		=0;
			Patch.mNumTris		=0;
			Patch.mPlane		=Plane;

			return	Patch;
		}


		bool CheckPatch(RADPatch Patch)
		{
			for(int i=0;i < 3;i++)
			{
				if(UtilityLib.Mathery.VecIdx(Patch.mRadFinal, i) < 0.0f)
				{
					Print("CheckPatch:  Bad final radiosity Color in patch.\n");
					return	false;
				}
			}
			return	true;
		}


		bool FindPatchReceivers(RADPatch Patch, float []recAmount)
		{
			RADPatch	Patch2;
			bool		VisInfo;
			float		Dist;
			float		Amount;
			float		Total, Scale;
			Int32		i, Cluster;
			Vector3		Vect, Normal;
			GFXLeaf		pLeaf;
			Int32		Area, VisOfs	=0;

			pLeaf	=mGFXLeafs[Patch.mLeaf];
			Cluster	=pLeaf.mCluster;
			Area	=pLeaf.mArea;

			if(Cluster >= 0 && mGFXClusters[Cluster].mVisOfs >= 0)
			{
				VisOfs	=mGFXClusters[Cluster].mVisOfs;
				VisInfo	=true;
			}
			else
			{
				VisInfo	=false;
			}
			Total	=0.0f;
			Normal	=Patch.mPlane.mNormal;

			//For each face, go through all it's patches
			for(i=0;i < NumPatches;i++)
			{
				Patch2	=mPatchList[i];
				
				recAmount[i]	=0.0f;

				if(Patch2 == Patch)
				{
					continue;
				}

				pLeaf	=mGFXLeafs[Patch2.mLeaf];

				//Radiosity only bounces in it's original area
				if(pLeaf.mArea != Area)
				{
					continue;
				}

				if(VisInfo)
				{
					Cluster	=pLeaf.mCluster;
					if(Cluster >= 0 && ((mGFXVisData[VisOfs + Cluster>>3]
						& (1 << (Cluster & 7))) == 0))
					{
						continue;
					}
				}
				Vect	=Patch2.mOrigin - Patch.mOrigin;
				Dist	=Vect.Length();
				Vect.Normalize();

				//if (Dist > PatchSize)
				if(Dist == 0.0f)
				{
					continue;	// Error
				}
				
				Scale	=Vector3.Dot(Vect, Normal);
				Scale	*=-Vector3.Dot(Vect, Patch2.mPlane.mNormal);

				if(Scale <= 0)
				{
					continue;
				}

				Vector3	colResult	=Vector3.Zero;
				if(RayCollision(Patch.mOrigin, Patch2.mOrigin, ref colResult))
				{
					//blocked by something in the world
					continue;
				}
				Amount	=Scale * Patch2.mArea / (Dist * Dist);

				if(Amount <= 0.0f)
				{
					continue;
				}
				recAmount[i]	=Amount;

				//Add the receiver
				Total	+=Amount;
				NumReceivers++;
				Patch.mNumReceivers++;
			}

			Patch.mReceivers	=new RADReceiver[Patch.mNumReceivers];
			int	roffs	=0;
			for(i=0;i < NumPatches;i++)
			{
				if(recAmount[i] == 0.0f)
				{
					continue;
				}
				Patch.mReceivers[roffs]			=new RADReceiver();
				Patch.mReceivers[roffs].mPatch	=(UInt16)i;
				Patch.mReceivers[roffs].mAmount	=(UInt16)(recAmount[i] * 0x10000 / Total);
				roffs++;
			}
			return	true;
		}


		bool CalcReceivers(string fileName)
		{
			Int32		i;
			RADPatch	Patch;
			Int32		Perc;

			NumReceivers	=0;

			//Try to load the receiver file first!!!
			if(LoadReceiverFile(fileName))
			{
				Print("--- Found receiver file ---\n");
				return	true;
			}

			Print(" --- Calculating receivers from scratch ---\n");

			float	[]recAmount	=new float[mPatchList.Length];

			Perc	=(mPatchList.Length / 20);
			for(i=0;i < mPatchList.Length;i++)
			{
				if(Perc != 0)
				{
					if(((i % Perc) == 0) && (i / Perc) <= 20)
					{
						Print("." + (i / Perc));
					}
				}				
				Patch	=mPatchList[i];

				if(!FindPatchReceivers(Patch, recAmount))
				{
					Print("CalcReceivers:  There was an error calculating receivers.\n");
					return	false;
				}
			}
			Print("\n");

			recAmount	=null;

			Print("Num Receivers        : " + NumReceivers + "\n");

			// Save receiver file for later retreival
			if(!SaveReceiverFile(fileName))
			{
				Print("CalcReceivers:  Failed to save receiver file...\n");
				return	false;
			}
			return	true;
		}


		public bool LightGBSPFile(string fileName,
			LightParams lightParams, BSPBuildParams buildParams)
		{
			string	RecFile;

			mLightParams	=lightParams;
			mBSPParms		=buildParams;

			Print(" --- Radiosity GBSP File --- \n");

			BinaryWriter	bw		=null;
			FileStream		file	=null;
			
			if(!LoadGBSPFile(fileName))
			{
				Print("LightGBSPFile:  Could not load GBSP file: " + fileName + "\n");
				return	false;
			}
			
			//Allocate some RGBLight data now
			mGFXRGBVerts	=new Vector3[mGFXVertIndexes.Length];

			if(!MakeVertNormals())
			{
				Print("LightGBSPFile:  MakeVertNormals failed...\n");
				goto	ExitWithError;
			}

			//Make sure no existing light exist...
			mGFXLightData	=null;

			//Get the receiver file name
			int	extPos	=fileName.LastIndexOf(".");
			RecFile		=fileName.Substring(0, extPos);
			RecFile		+=".rec";

			file	=UtilityLib.FileUtil.OpenTitleFile(fileName,
									FileMode.OpenOrCreate, FileAccess.Write);

			if(file == null)
			{
				Print("LightGBSPFile:  Could not open GBSP file for writing: " + fileName + "\n");
				goto	ExitWithError;
			}
			bw	=new BinaryWriter(file);

			Print("Num Faces            : " + mGFXFaces.Length + "\n");

			//Build the patches (before direct lights are created)
			if(mLightParams.mbRadiosity)
			{
				if(!BuildPatches())
				{
					goto	ExitWithError;
				}
			}

			if(!CreateDirectLights())
			{
				Print("LightGBSPFile:  Could not create main lights.\n");
				goto	ExitWithError;
			}
			
			//Light faces, and apply to patches
			if(!LightFaces(5, false))	//Light all the faces lightmaps, and apply to patches
			{
				goto	ExitWithError;
			}

			FreeDirectLights();

			if(mLightParams.mbRadiosity)
			{
				//Pre-calc how much light is distributed to each patch from every patch
				if(!CalcReceivers(RecFile))	
				{
					goto	ExitWithError;
				}

				//Bounce patches around to their receivers
				if(!BouncePatches())	//Bounce them around
				{
					goto	ExitWithError;
				}
			
				FreeReceivers();		//Don't need these anymore

				//Apply the patches back into the light maps
				if(!AbsorbPatches())	//Apply the patches to the lightmaps
				{
					goto	ExitWithError;
				}			
				FreePatches();	//Don't need these anymore...
			}

			FinalizeRGBVerts(lightParams.mMinLight, lightParams.mMaxIntensity);

			if(!StartWritingLight(bw))	//Open bsp file and save all current bsp data (except lightmaps)
			{
				goto	ExitWithError;
			}

			int	numRGBMaps	=0;

			if(!SaveLightMaps(bw, ref numRGBMaps))
			{
				goto	ExitWithError;
			}

			if(!FinishWritingLight(bw))	//Write the END chunk to the file
			{
				goto	ExitWithError;
			}

			bw.Close();
			file.Close();

			CleanupLight();

			Print("Num Light Maps       : " + numRGBMaps + "\n");

			return	true;

			ExitWithError:
			{
				if(bw != null)
				{
					bw.Close();
				}
				if(file != null)
				{
					file.Close();
				}
				CleanupLight();

				return	false;
			}
		}


		void FreeReceivers()
		{
			NumReceivers	=0;

			for(int i=0;i < NumPatches;i++)
			{
				if(mPatchList[i].mNumReceivers > 0)
				{
					mPatchList[i].mReceivers	=null;
				}
			}
		}


		void CleanupLight()
		{
			FreeDirectLights();
			FreePatches();
			FreeLightMaps();
			FreeReceivers();

			VertNormals	=null;

			FreeGBSPFile();
		}


		void FreePatches()
		{
			for(int i=0;i < NumPatches;i++)
			{
				mPatchList[i]	=null;
			}
			NumPatches	=0;

			mPatchList		=null;
			mFacePatches	=null;
		}


		void FreeLightMaps()
		{
			mLightMaps	=null;
			mFaceInfos	=null;
		}


		bool CreateDirectLights()
		{
			Int32		i, Leaf, Cluster;
			Vector3		Color;
			MapEntity	Entity;
			DirectLight	DLight;

			Int32	NumDirectLights	=0;
			Int32	NumSurfLights	=0;

			DirectClusterLights.Clear();

			// Create the entity lights first
			for(i=0;i < mGFXEntities.Length;i++)
			{
				Entity	=mGFXEntities[i];

				if(!(Entity.mData.ContainsKey("light")
					|| Entity.mData.ContainsKey("_light")))
				{
					continue;
				}

				DLight	=new DirectLight();

				Vector4	colorVec	=Vector4.Zero;
				if(!Entity.GetLightValue(out colorVec))
				{
					Print("Warning:  Light entity, couldn't get color\n");
				}

				Color.X	=colorVec.X;
				Color.Y	=colorVec.Y;
				Color.Z	=colorVec.Z;

				//Default it to 255/255/255 if no light is specified
				if(Color.Length() < 1.0f)
				{
					Color	=Vector3.One;
				}
				else
				{
					Color.Normalize();
				}

				if(!Entity.GetOrigin(out DLight.mOrigin))
				{
					Print("Warning:  Light entity, couldn't get origin\n");
				}

				DLight.mColor		=Color;
				DLight.mIntensity	=colorVec.W;// * mGlobals.EntityScale;
				DLight.mType		=DirectLight.DLight_Point;	//hardcode for now

				Vector3	Angles;
				if(!Entity.GetVector("Angles", out Angles))
				{
					Vector3	Angles2	=Vector3.Zero;
					Angles2.X	=(Angles.X / 180) * (float)Math.PI;
					Angles2.Y	=(Angles.Y / 180) * (float)Math.PI;
					Angles2.Z	=(Angles.Z / 180) * (float)Math.PI;

					Matrix	mat	=Matrix.CreateFromYawPitchRoll(Angles2.X, Angles2.Y, Angles2.Z); 

					Angles2	=mat.Left;
					DLight.mNormal.X	=-Angles2.X;
					DLight.mNormal.Y	=-Angles2.Y;
					DLight.mNormal.Z	=-Angles2.Z;

					if(!Entity.GetFloat("Arc", out DLight.mAngle))
					{
						Print("Arc element of entity not found!\n");
					}
					DLight.mAngle	=(float)Math.Cos(DLight.mAngle / 180.0f * Math.PI);					
				}

				//Find out what type of light it is by it's classname...
				Entity.GetLightType(out DLight.mType);

				Int32	nodeLandedIn	=FindNodeLandedIn(mGFXModels[0].mRootNode[0], DLight.mOrigin);
				Leaf	=-(nodeLandedIn + 1);
				Cluster	=mGFXLeafs[Leaf].mCluster;

				if(Cluster < 0)
				{
					Print("*WARNING* CreateLights:  Light in solid leaf.\n");
					continue;
				}
				if(DirectClusterLights.ContainsKey(Cluster))
				{
					DLight.mNext	=DirectClusterLights[Cluster];
					DirectClusterLights[Cluster]	=DLight;
				}
				else
				{
					DLight.mNext	=null;
					DirectClusterLights.Add(Cluster, DLight);
				}

				DirectLights.Add(DLight);
				NumDirectLights++;
			}

			Print("Num Normal Lights   : " + NumDirectLights + "\n");

			//Stop here if no radisosity is going to be done
			if(!mLightParams.mbRadiosity)
			{
				return	true;
			}
			
			//Now create the radiosity direct lights (surface emitters)
			for(i=0;i < mGFXFaces.Length;i++)
			{
				GFXTexInfo	pTexInfo	=mGFXTexInfos[mGFXFaces[i].mTexInfo];

				//Only look at surfaces that want to emit light
				if((pTexInfo.mFlags & TexInfo.TEXINFO_LIGHT) == 0)
				{
					continue;
				}

				for(RADPatch Patch=mFacePatches[i];Patch != null;Patch=Patch.mNext)
				{
					Leaf	=Patch.mLeaf;
					Cluster	=mGFXLeafs[Leaf].mCluster;

					if(Cluster < 0)
					{
						continue;	//Skip, solid
					}

					DLight	=new DirectLight();

					DLight.mOrigin	=Patch.mOrigin;
					DLight.mColor	=Patch.mReflectivity;
					DLight.mNormal	=Patch.mPlane.mNormal;
					DLight.mType	=DirectLight.DLight_Surface;
					
					DLight.mIntensity	=pTexInfo.mFaceLight * Patch.mArea;

					//Make sure the emitter ends up with some light too
					Patch.mRadFinal	+=Patch.mReflectivity * DLight.mIntensity;

					//Insert this surface direct light into the list of lights
					if(DirectClusterLights.ContainsKey(Cluster))
					{
						DLight.mNext	=DirectClusterLights[Cluster];
						DirectClusterLights[Cluster]	=DLight;
					}
					else
					{
						DLight.mNext	=null;
						DirectClusterLights.Add(Cluster, DLight);
					}

					DirectLights.Add(DLight);
					NumSurfLights++;
				}
			}
			Print("Num Surf Lights     : " + NumSurfLights + "\n");
			return	true;
		}


		bool CalcFaceInfo(FInfo FaceInfo, LInfo LightInfo)
		{
			Int32	fidx	=FaceInfo.GetFaceIndex();
			Int32	indOffset;
			
			List<Vector3>	verts	=new List<Vector3>();
			indOffset				=mGFXFaces[fidx].mFirstVert;

			for(int i=0;i < mGFXFaces[fidx].mNumVerts;i++, indOffset++)
			{
				int	vIndex	=mGFXVertIndexes[indOffset];
				verts.Add(mGFXVerts[vIndex]);
			}

			FaceInfo.CalcFaceLightInfo(LightInfo, verts);

			return	true;
		}


		bool GouraudShadeFace(Int32 FaceNum)
		{
			Int32		NumVerts;
			DirectLight	DLight;
			GFXFace		pGFXFace;
			Int32		v;
			GFXTexInfo	pGFXTexInfo;

			if(mGFXRGBVerts == null || mGFXRGBVerts.Length == 0)
			{
				return	false;
			}

			pGFXFace	=mGFXFaces[FaceNum];
			
			pGFXTexInfo	=mGFXTexInfos[pGFXFace.mTexInfo];

			NumVerts	=pGFXFace.mNumVerts;

			for(v=0;v < pGFXFace.mNumVerts;v++)
			{
				Int32		vn, Index, i;
				Vector3		pVert, Normal;
				float		Dist, Angle, Val, Intensity;

				vn	=pGFXFace.mFirstVert + v;

				Index	=mGFXVertIndexes[vn];
				pVert	=mGFXVerts[Index];

				if((pGFXTexInfo.mFlags & TexInfo.TEXINFO_FLAT) != 0)
				{
					Normal	=mFaceInfos[FaceNum].GetPlaneNormal();
				}
				else
				{
					Normal	=VertNormals[Index];
				}
				
				for(i=0;i < DirectLights.Count;i++)
				{
					Vector3	Vect;

					DLight	=DirectLights[i];

					Intensity	=DLight.mIntensity;

					//Find the angle between the light, and the face normal
					Vect	=DLight.mOrigin - pVert;				
					Dist	=Vect.Length();
					Vect.Normalize();

					Angle	=Vector3.Dot(Vect, Normal);

					if(Angle <= 0.001f)
					{
						goto Skip;
					}
						
					switch(DLight.mType)
					{
						case DirectLight.DLight_Point:
						{
							Val	=(Intensity - Dist) * Angle;//(Angle*0.5f+0.5f);
							break;
						}
						case DirectLight.DLight_Spot:
						{
							float Angle2	=-Vector3.Dot(Vect, DLight.mNormal);
							if(Angle2 < DLight.mAngle)
							{
								goto Skip;
							}

							Val	=(Intensity - Dist) * Angle;
							break;
						}
						case DirectLight.DLight_Surface:
						{
							float Angle2	=-Vector3.Dot(Vect, DLight.mNormal);
							if(Angle2 <= 0.001f)
							{
								goto Skip;	//Behind light surface
							}
							Val	=(Intensity / (Dist * Dist)) * Angle * Angle2;
							break;
						}
						default:
						{
							Print("ApplyLightsToFace:  Invalid light.\n");
							return	false;
						}
					}
					if(Val <= 0.0f)
					{
						goto	Skip;
					}

					//This is the slowest test, so make it last
					Vector3	colResult	=Vector3.Zero;
					if(RayCollision(pVert, DLight.mOrigin, ref colResult))
					{
						goto	Skip;	//Ray is in shadow
					}
					mGFXRGBVerts[vn]	+=(DLight.mColor * Val);

					Skip:;				
				}
			}
			return	true;
		}


		void TransferLightToPatches(Int32 Face)
		{
			GFXFace		pGFXFace;
			RADPatch	Patch;

			pGFXFace	=mGFXFaces[Face];

			for(Patch=mFacePatches[Face];Patch != null;Patch=Patch.mNext)
			{
				Vector3	pRGB, pVert;
				Int32	i, rgbOfs	=pGFXFace.mFirstVert;

				pRGB	=mGFXRGBVerts[rgbOfs];

				Patch.mNumSamples	=0;
				//geVec3d_Clear(&Patch.mRadStart);

				for(i=0;i < pGFXFace.mNumVerts;i++)
				{
					Int32	k;

					pVert	=mGFXVerts[mGFXVertIndexes[i+pGFXFace.mFirstVert]];

					for(k=0;k < 3;k++)
					{
						if(UtilityLib.Mathery.VecIdx(Patch.mBounds.mMins, k)
							> UtilityLib.Mathery.VecIdx(pVert, k) + 16)
						{
							break;
						}				
						if(UtilityLib.Mathery.VecIdx(Patch.mBounds.mMaxs, k)
							< UtilityLib.Mathery.VecIdx(pVert, k) - 16)
						{
							break;
						}				
					}

					if(k == 3)
					{
						//Add the Color to the patch 
						Patch.mNumSamples++;
						Patch.mRadStart	+=pRGB;
					}
					rgbOfs++;
					pRGB	=mGFXRGBVerts[rgbOfs];
				}
				
				if(Patch.mNumSamples != 0)
				{
					Patch.mRadStart	*=(1.0f / Patch.mNumSamples);
				}
			}
		}


		bool LightFaces(int numSamples, bool bExtraSamples)
		{
			Int32	i, s;
			Int32	Perc;

			float	[]UOfs	=new float[5];
			float	[]VOfs	=new float[5];

			UOfs[0]	=0.0f;
			UOfs[1]	=-0.5f;
			UOfs[2]	=0.5f;
			UOfs[3]	=0.5f;
			UOfs[4]	=-0.5f;
			VOfs[0]	=0.0f;
			VOfs[1]	=-0.5f;
			VOfs[2]	=-0.5f;
			VOfs[3]	=0.5f;
			VOfs[4]	=0.5f;

			mLightMaps	=new LInfo[mGFXFaces.Length];

			if(mLightMaps == null)
			{
				Print("LightFaces:  Out of memory for Lightmaps.\n");
				return	false;
			}

			mFaceInfos	=new FInfo[mGFXFaces.Length];

			if(mFaceInfos == null)
			{
				Print("LightFaces:  Out of memory for FaceInfo.\n");
				return	false;
			}

			for(i=0;i < mGFXFaces.Length;i++)
			{
				mLightMaps[i]	=new LInfo();
				mFaceInfos[i]	=new FInfo();
			}

			Perc	=mGFXFaces.Length / 20;

			for(i=0;i < mGFXFaces.Length;i++)
			{
				if(Perc != 0)
				{
					if(((i % Perc) == 0) &&	(i / Perc) <= 20)
					{
						Print("." + (i/Perc));
					}
				}

				int	pnum	=mGFXFaces[i].mPlaneNum;
				int	pside	=mGFXFaces[i].mPlaneSide;
				GFXPlane	pln	=new GFXPlane();
				pln.mNormal	=mGFXPlanes[pnum].mNormal;
				pln.mDist	=mGFXPlanes[pnum].mDist;
				pln.mType	=mGFXPlanes[pnum].mType;
				if(pside != 0)
				{
					pln.Inverse();
				}
				mFaceInfos[i].SetPlane(pln);
				mFaceInfos[i].SetFaceIndex(i);

				GFXTexInfo	tex		=mGFXTexInfos[mGFXFaces[i].mTexInfo];
				GFXFace		face	=mGFXFaces[i];

				if((tex.mFlags & TexInfo.TEXINFO_GOURAUD) != 0)
				{
					if(!GouraudShadeFace(i))
					{
						Map.Print("LightFaces:  GouraudShadeFace failed...\n");
						return	false;
					}
					
					if(mLightParams.mbRadiosity)
					{
						TransferLightToPatches(i);
					}
					continue;
				}
								
				//Faces with no lightmap don't need to light them 
				if((tex.mFlags & TexInfo.TEXINFO_NO_LIGHTMAP) != 0)
				{
					continue;
				}

				if(!CalcFaceInfo(mFaceInfos[i], mLightMaps[i]))
				{
					return	false;
				}
			
				Int32	Size	=mLightMaps[i].CalcSize();

				mFaceInfos[i].AllocPoints(Size);

				for(s=0;s < numSamples;s++)
				{
					//Hook.Printf("Sample  : %3i of %3i\n", s+1, NumSamples);
					CalcFacePoints(mFaceInfos[i], mLightMaps[i], UOfs[s], VOfs[s], bExtraSamples);

					if(!ApplyLightsToFace(mFaceInfos[i], mLightMaps[i], 1 / (float)numSamples))
					{
						return	false;
					}
				}
				
				if(mLightParams.mbRadiosity)
				{
					// Update patches for this face
					ApplyLightmapToPatches(i);
				}
			}			
			Print("\n");
			return	true;
		}


		void ApplyLightmapToPatches(Int32 Face)
		{
			mLightMaps[Face].ApplyLightToPatchList(mFacePatches[Face], mFaceInfos[Face].GetPoints());
		}


		bool ApplyLightsToFace(FInfo FaceInfo, LInfo LightInfo, float Scale)
		{
			Int32		c, v;
			float		Dist;
			Int32		LType;
			Vector3		Normal, Vect;
			float		Val, Angle;
			Int32		Leaf, Cluster;
			float		Intensity;
			DirectLight	DLight;

			Normal	=FaceInfo.GetPlaneNormal();

			Vector3	[]facePoints	=FaceInfo.GetPoints();

			for(v=0;v < facePoints.Length;v++)
			{
				Int32	nodeLandedIn	=FindNodeLandedIn(0, facePoints[v]);
				Leaf	=-(nodeLandedIn + 1);

				if(Leaf < 0 || Leaf >= mGFXLeafs.Length)
				{
					Print("ApplyLightsToFace:  Invalid leaf num.\n");
					return	false;
				}

				Cluster	=mGFXLeafs[Leaf].mCluster;

				if(Cluster < 0)
				{
					continue;
				}

				if(Cluster >= mGFXClusters.Length)
				{
					Print("*WARNING* ApplyLightsToFace:  Invalid cluster num.\n");
					continue;
				}

				for(c=0;c < mGFXClusters.Length;c++)
				{
					if((mGFXVisData[mGFXClusters[Cluster].mVisOfs + (c>>3)] & (1<<(c&7))) == 0)
					{
						continue;
					}

					if(!DirectClusterLights.ContainsKey(c))
					{
						continue;
					}

					for(DLight=DirectClusterLights[c];DLight != null;DLight=DLight.mNext)
					{
						Intensity	=DLight.mIntensity;
					
						//Find the angle between the light, and the face normal
						Vect	=DLight.mOrigin - facePoints[v];
						Dist	=Vect.Length();
						Vect.Normalize();

						Angle	=Vector3.Dot(Vect, Normal);
						if(Angle <= 0.001f)
						{
							goto	Skip;
						}
						
						switch(DLight.mType)
						{
							case DirectLight.DLight_Point:
							{
								Val	=(Intensity - Dist) * Angle;
								break;
							}
							case DirectLight.DLight_Spot:
							{
								float	Angle2	=-Vector3.Dot(Vect, DLight.mNormal);

								if(Angle2 < DLight.mAngle)
								{
									goto	Skip;
								}

								Val	=(Intensity - Dist) * Angle;
								break;
							}
							case DirectLight.DLight_Surface:
							{
								float	Angle2	=-Vector3.Dot(Vect, DLight.mNormal);
								if(Angle2 <= 0.001f)
								{
									goto	Skip;	// Behind light surface
								}

								Val	=(Intensity / (Dist * Dist)) * Angle * Angle2;
								break;
							}
							default:
							{
								Print("ApplyLightsToFace:  Invalid light.\n");
								return	false;
							}
						}

						if(Val <= 0.0f)
						{
							goto	Skip;
						}

						// This is the slowest test, so make it last
						Vector3	colResult	=Vector3.Zero;
						if(RayCollision(facePoints[v], DLight.mOrigin, ref colResult))
						{
							goto	Skip;	//Ray is in shadow
						}

						LType	=DLight.mLType;

						//If the data for this LType has not been allocated, allocate it now...
						LightInfo.AllocLightType(LType, facePoints.Length);

						Vector3	[]rgb	=LightInfo.GetRGBLightData(LType);

						rgb[v]	+=DLight.mColor * (Val * Scale);

						Skip:;
					}
				}
			}

			return	true;
		}


		void CalcFacePoints(FInfo FaceInfo, LInfo LightInfo, float UOfs, float VOfs, bool bExtraLightCorrection)
		{
			FaceInfo.CalcFacePoints(LightInfo, UOfs, VOfs, bExtraLightCorrection, IsPointInSolid, RayCollision);
		}


		void FinalizeRGBVerts(Vector3 minLight, float maxLight)
		{
			for(int i=0;i < mGFXRGBVerts.Length;i++)
			{
				mGFXRGBVerts[i]	+=minLight;

				mGFXRGBVerts[i]	=Vector3.Clamp(mGFXRGBVerts[i],
					Vector3.Zero, Vector3.One * maxLight);
			}
		}


		void FreeDirectLights()
		{
			DirectLights.Clear();
			DirectClusterLights.Clear();
		}


		bool SaveLightMaps(BinaryWriter f, ref int numRGBMaps)
		{
			Int32		i, j, k,l, Size;
			float		Max, Max2;
			byte		[]LData	=new byte[LInfo.MAX_LMAP_SIZE * LInfo.MAX_LMAP_SIZE * 3 * 4];
			long		Pos1, Pos2;
			Int32		NumLTypes;
			Int32		LDataOfs	=0;

			Pos1	=f.BaseStream.Position;
			
			// Write out fake chunk (so we can write the real one here later)
			GBSPChunk	Chunk	=new GBSPChunk();
			Chunk.mType		=GBSPChunk.GBSP_CHUNK_LIGHTDATA;
			Chunk.mElements	=0;

			Chunk.Write(f);

			//Reset the light offset
			int	LightOffset	=0;
			numRGBMaps		=0;
			int	REGMaps		=0;
			
			//Go through all the faces
			for(i=0;i < mGFXFaces.Length;i++)
			{
				LInfo	L			=mLightMaps[i];
				FInfo	pFaceInfo	=mFaceInfos[i];
				
				// Set face defaults
				mGFXFaces[i].mLightOfs	=-1;
				mGFXFaces[i].mLWidth	=L.GetLWidth();
				mGFXFaces[i].mLHeight	=L.GetLHeight();
				mGFXFaces[i].mLTypes[0]	=255;
				mGFXFaces[i].mLTypes[1]	=255;
				mGFXFaces[i].mLTypes[2]	=255;
				mGFXFaces[i].mLTypes[3]	=255;
				
				//Skip special faces with no lightmaps
				if((mGFXTexInfos[mGFXFaces[i].mTexInfo].mFlags
					& TexInfo.TEXINFO_NO_LIGHTMAP) != 0)
				{
					continue;
				}

				//Get the size of map
				Size	=mFaceInfos[i].GetPoints().Length;

				Vector3	minLight	=mLightParams.mMinLight;

				Vector3	[]rgb	=L.GetRGBLightData(0);

				//Create style 0, if min light is set, and style 0 does not exist
				if((rgb == null) &&
					(minLight.X > 1 || minLight.Y > 1 || minLight.Z > 1))
				{
					L.AllocLightType(0, Size);
					rgb	=L.GetRGBLightData(0);
					for(int ld=0;ld < rgb.Length;ld++)
					{
						rgb[ld]	=Vector3.Zero;
					}
				}
				
				//At this point, if no styles hit the face, skip it...
				if(L.GetNumLightTypes() == 0)
				{
					continue;
				}

				//Mark the start of the lightoffset
				mGFXFaces[i].mLightOfs	=LightOffset;

				//At this point, all lightmaps are currently RGB
				byte	RGB2	=1;
				
				if(RGB2 != 0)
				{
					numRGBMaps++;
				}
				else
				{
					REGMaps++;
				}

				f.Write(RGB2);

				LightOffset++;		//Skip the rgb light byte
				
				NumLTypes	=0;		// Reset number of LTypes for this face
				for(k=0;k < LInfo.MAX_LTYPE_INDEX;k++)
				{
					rgb	=L.GetRGBLightData(k);
					if(rgb == null)
					{
						continue;
					}

					if(NumLTypes >= LInfo.MAX_LTYPES)
					{
						Print("SaveLightmaps:  Max LightTypes on face.\n");
						return	false;
					}
						 
					mGFXFaces[i].mLTypes[NumLTypes]	=(byte)k;
					NumLTypes++;

					LDataOfs	=0;

					for(j=0;j < Size;j++)
					{
						Vector3	WorkRGB	=rgb[j] * mLightParams.mLightScale;

						if(k == 0)
						{
							WorkRGB	+=minLight;
						}
						
						Max	=0.0f;

						for(l=0;l < 3;l++)
						{
							float	Val	=UtilityLib.Mathery.VecIdx(WorkRGB, l);

							if(Val < 1.0f)
							{
								Val	=1.0f;
								UtilityLib.Mathery.VecIdxAssign(ref WorkRGB, l, Val);
							}

							if(Val > Max)
							{
								Max	=Val;
							}
						}

						Debug.Assert(Max > 0.0f);
						
						Max2	=Math.Min(Max, mLightParams.mMaxIntensity);

						for(l=0;l < 3;l++)
						{
							LData[LDataOfs]	=(byte)(UtilityLib.Mathery.VecIdx(WorkRGB, l) * (Max2 / Max));
							LDataOfs++;
							LightOffset++;
						}
					}

					f.Write(LData, 0, 3 * Size);

					L.FreeLightType(k);
				}

				if(L.GetNumLightTypes() != NumLTypes)
				{
					Print("SaveLightMaps:  Num LightTypes was incorrectly calculated.\n");
					return	false;
				}
			}

			Print("Light Data Size      : " + LightOffset + "\n");

			Pos2	=f.BaseStream.Position;

			f.BaseStream.Seek(Pos1, SeekOrigin.Begin);

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_LIGHTDATA;
			Chunk.mElements =LightOffset;

			Chunk.Write(f);

			f.BaseStream.Seek(Pos2, SeekOrigin.Begin);

			return	true;
		}


		bool FinishWritingLight(BinaryWriter bw)
		{
			GBSPHeader	header	=new GBSPHeader();
			header.mTAG			="GBSP";
			header.mVersion		=GBSPChunk.GBSP_VERSION;
			header.mBSPTime		=DateTime.Now;

			GBSPChunk	chunk	=new GBSPChunk();
			chunk.mType			=GBSPChunk.GBSP_CHUNK_HEADER;
			chunk.mElements		=1;
			chunk.Write(bw, header);

			if(!SaveGFXRGBVerts(bw))
			{
				return	false;
			}
			if(!SaveVisdGFXFaces(bw))
			{
				return	false;
			}
			chunk.mType		=GBSPChunk.GBSP_CHUNK_END;
			chunk.mElements	=0;
			chunk.Write(bw);

			return	true;
		}


		bool StartWritingLight(BinaryWriter bw)
		{
			// Write out everything but the light data
			// Don't include LIGHT_DATA since it was allready saved out...

			GBSPHeader	header	=new GBSPHeader();
			header.mTAG			="GBSP";
			header.mVersion		=GBSPChunk.GBSP_VERSION;
			header.mBSPTime		=DateTime.Now;

			GBSPChunk	chunk	=new GBSPChunk();
			chunk.mType			=GBSPChunk.GBSP_CHUNK_HEADER;
			chunk.mElements		=1;
			chunk.Write(bw, header);

			if(!SaveGFXModelData(bw))
			{
				return	false;
			}
			if(!SaveVisdGFXNodes(bw))
			{
				return	false;
			}
			if(!SaveGFXPortals(bw))
			{
				return	false;
			}
			if(!SaveGFXBNodes(bw))
			{
				return	false;
			}
			if(!SaveGFXLeafs(bw))
			{
				return	false;
			}
			if(!SaveGFXAreasAndPortals(bw))
			{
				return	false;
			}
			if(!SaveGFXClusters(bw))
			{
				return	false;
			}
			if(!SaveVisdGFXPlanes(bw))
			{
				return	false;
			}
			if(!SaveVisdGFXLeafFacesAndSides(bw))
			{
				return	false;
			}
			if(!SaveGFXVerts(bw))
			{
				return	false;
			}
			if(!SaveGFXVertIndexList(bw))
			{
				return	false;
			}
			if(!SaveGFXEntData(bw))
			{
				return	false;
			}
			if(!SaveVisdGFXTexInfos(bw))
			{
				return	false;
			}
			if(!SaveGFXVisData(bw))
			{
				return	false;
			}
			return	true;
		}
	}
}
