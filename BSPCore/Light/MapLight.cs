using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.Xna.Framework;


namespace BSPCore
{
	public partial class Map
	{
		//light related stuff
		Dictionary<Int32, DirectLight>	DirectClusterLights	=new Dictionary<Int32, DirectLight>();
		List<DirectLight>				DirectLights		=new List<DirectLight>();
		LInfo							[]mLightMaps;
		FInfo							[]mFaceInfos;
		RADPatch						[]mFacePatches;
		RADPatch						[]mPatchList;
		Int32							NumPatches, NumReceivers;
		Vector2							[]mSampleOffsets;


		void CalcPatchReflectivity(Int32 Face, RADPatch Patch, float surfaceReflect,
					CoreDelegates.GetEmissiveForMaterial c4m)
		{
			string	trueName	=GFXTexInfo.ScryTrueName(mGFXFaces[Face], mGFXTexInfos[mGFXFaces[Face].mTexInfo]);

			Patch.mReflectivity	=c4m(trueName) * 255.0f;
			Patch.mReflectivity	*=surfaceReflect;
		}


		bool PatchNeedsSplit(RADPatch patch, out GBSPPlane plane, bool bFastPatch, float patchSize)
		{
			Int32	i;

			if(bFastPatch)
			{
				for(i=0;i < 3;i++)
				{
					float	dist	=UtilityLib.Mathery.VecIdx(patch.mBounds.mMaxs, i)
								- UtilityLib.Mathery.VecIdx(patch.mBounds.mMins, i);
					
					if(dist > patchSize)
					{
						//Cut it right through the center...
						plane.mNormal	=Vector3.Zero;
						UtilityLib.Mathery.VecIdxAssign(ref plane.mNormal, i, 1.0f);
						plane.mDist	=(UtilityLib.Mathery.VecIdx(patch.mBounds.mMaxs, i)
							+ UtilityLib.Mathery.VecIdx(patch.mBounds.mMins, i))
								/ 2.0f;
						plane.mType	=GBSPPlane.PLANE_ANY;
						return	true;
					}
				}
			}
			else
			{
				for(i=0;i < 3;i++)
				{
					float	min	=UtilityLib.Mathery.VecIdx(patch.mBounds.mMins, i) + 1.0f;
					float	max	=UtilityLib.Mathery.VecIdx(patch.mBounds.mMaxs, i) - 1.0f;

					if(Math.Floor(min / patchSize)
						< Math.Floor(max / patchSize))
					{
						plane.mNormal	=Vector3.Zero;
						UtilityLib.Mathery.VecIdxAssign(ref plane.mNormal, i, 1.0f);
						plane.mDist	=patchSize * (1.0f + (float)Math.Floor(min / patchSize));
						plane.mType	=GBSPPlane.PLANE_ANY;
						return	true;
					}
				}
			}
			plane	=new GBSPPlane();
			return	false;
		}


		bool BuildPatch(Int32 f, float surfReflect, int patchSize, bool bFastPatch,
			CoreDelegates.GetEmissiveForMaterial c4m)
		{
			mFacePatches[f]	=new RADPatch();
			if(mFacePatches[f] == null)
			{
				CoreEvents.Print("BuildPatch:  Could not allocate patch.\n");
				return	false;
			}

			CalcPatchReflectivity(f, mFacePatches[f], surfReflect, c4m);
			
			mFacePatches[f].AllocPoly(mGFXFaces[f], mGFXVertIndexes, mGFXVerts);

			if(!mFacePatches[f].CalcInfo())
			{
				CoreEvents.Print("BuildPatch:  Could not calculate patch info.\n");
				return	false;
			}

			mFacePatches[f]	=RADPatch.SubdivideFacePatches(mFacePatches[f],
				bFastPatch, patchSize, ref NumPatches);

			if(mFacePatches[f] == null)
			{
				CoreEvents.Print("BuildPatch:  Could not subdivide patch.\n");
				return	false;
			}
			return	true;
		}


		bool FinalizePatchInfo(Int32 face, RADPatch patch)
		{
			GFXFace	f	=mGFXFaces[face];
			return	patch.Finalize(mGFXPlanes[f.mPlaneNum], f.mPlaneSide, FindNodeLandedIn);
		}


		bool BuildPatches(float surfReflect, int patchSize, bool bFastPatch, bool bVerbose,
			CoreDelegates.GetEmissiveForMaterial c4m)
		{
			Int32	i;

			CoreEvents.Print("--- Build Patches --- \n");

			mFacePatches	=new RADPatch[mGFXFaces.Length];
			if(mFacePatches == null)
			{
				CoreEvents.Print("BuildPatches:  Not enough memory for patches.\n");
				return	false;
			}

			for(i=0;i < mFacePatches.Length;i++)
			{
				if(!BuildPatch(i, surfReflect, patchSize, bFastPatch, c4m))
				{
					return	false;
				}
			}

			if(!FinalizePatches())
			{
				CoreEvents.Print("BuildPatches:  Could not finalize face patches.\n");
				return	false;
			}

			if(bVerbose)
			{
				CoreEvents.Print("Num Patches          : " + mFacePatches.Length + "\n");
			}
			return	true;
		}


		bool FinalizePatches()
		{
			RADPatch	patch;

			NumPatches	=0;
			for(int i=0;i < mGFXFaces.Length;i++)
			{
				for(patch=mFacePatches[i];patch!=null;patch=patch.mNext)
				{
					FinalizePatchInfo(i, patch);
					NumPatches++;
				}
			}
			mPatchList	=new RADPatch[NumPatches];
			if(mPatchList == null)
			{
				CoreEvents.Print("FinalizePatches:  Out of memory for patch list.\n");
				return	false;
			}
			
			//Build the patch list, so we can use indexing, instead of pointers (for receivers)...
			int	k	=0;
			for(int i=0;i < mGFXFaces.Length;i++)
			{
				for(patch=mFacePatches[i];patch != null;patch=patch.mNext)
				{
					mPatchList[k]	=patch;
					k++;
				}
			}
			return	true;
		}


		bool LoadReceiverFile(string fileName)
		{
			if(!File.Exists(fileName))
			{
				return	false;
			}
			FileStream	fs	=new FileStream(fileName,
								FileMode.Open, FileAccess.Read);

			if(fs == null)
			{
				return	false;
			}

			BinaryReader	br	=new BinaryReader(fs);

			DateTime	dt			=DateTime.FromBinary(br.ReadInt64());
			Int32		numPatches	=br.ReadInt32();

			//Make sure the number of patches in the receiver file
			//matches the number loaded for this BSP
			if(numPatches != NumPatches)
			{
				CoreEvents.Print("*WARNING*  LoadReceiverFile:  NumPatches do not match, skipping...\n");
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
			CoreEvents.Print("--- Save Receiver File --- \n");

			FileStream	fs	=new FileStream(fileName,
								FileMode.Create, FileAccess.Write);

			if(fs == null)
			{
				CoreEvents.Print("SaveReceiverFile:  Could not open receiver file for writing...\n");
				return	false;
			}

			BinaryWriter	bw	=new BinaryWriter(fs);

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


		float CollectPatchLight()
		{
			float	total	=0.0f;
			
			for(int i=0;i < NumPatches;i++)
			{
				RADPatch	patch	=mPatchList[i];
				
				//Add receive amount to Final amount
				patch.Collect(ref total);
			}
			return	total;
		}


		bool BouncePatches(int numBounces, bool bVerbose)
		{
			CoreEvents.Print("--- Bounce Patches --- \n");
			
			for(int i=0;i < NumPatches;i++)
			{
				//Set each patches first pass send amount with what was obtained
				//from their lightmaps...
				mPatchList[i].SetFirstPassSendAmount();
			}

			for(int i=0;i < numBounces;i++)
			{
				if(bVerbose)
				{
					CoreEvents.Print("Bounce: " + (i + 1) + ",");
				}
				
				//For each patch, send it's energy to each pre-computed receiver
				for(int j=0;j < NumPatches;j++)
				{
					RADPatch	patch	=mPatchList[j];
					patch.Send(mPatchList);
				}

				//For each patch, collect any light it might have received
				//and throw into patch RadFinal
				float	total	=CollectPatchLight();

				if(bVerbose)
				{
					CoreEvents.Print("Energy: " + total + "\n");
				}
			}
			
			for(int j=0;j < NumPatches;j++)
			{
				RADPatch	patch	=mPatchList[j];
				if(!patch.Check())
				{
					return	false;
				}
			}
			return	true;
		}


		PlaneFace	[]LinkPlaneFaces()
		{
			PlaneFace	[]ret	=new PlaneFace[mGFXPlanes.Length];

			for(int i=0;i < mGFXFaces.Length;i++)
			{
				PlaneFace	p	=new PlaneFace();
				int	PlaneNum	=mGFXFaces[i].mPlaneNum;
				p.mGFXFace		=i;
				p.mNext			=ret[PlaneNum];
				ret[PlaneNum]	=p;
			}
			return	ret;
		}


		void GetFaceMinsMaxs(Int32 face, out Bounds bnd)
		{
			bnd	=new Bounds();
			for(int i=0;i < mGFXFaces[face].mNumVerts;i++)
			{
				int	Index	=mGFXVertIndexes[mGFXFaces[face].mFirstVert + i];
				bnd.AddPointToBounds(mGFXVerts[Index]);
			}			
		}


		bool AbsorbPatches(int patchSize)
		{
			//We need all the faces that belong to each Plane
			PlaneFace	[]planeFaces	=LinkPlaneFaces();

			for(int i=0;i < mGFXFaces.Length;i++)
			{
				UInt32	Flags;
				GFXFace	pGFXFace;

				pGFXFace	=mGFXFaces[i];

				Flags	=mGFXTexInfos[mGFXFaces[i].mTexInfo].mFlags;

				if (((Flags & TexInfo.NO_LIGHTMAP) != 0)
					&& ((Flags & TexInfo.GOURAUD)) == 0)
				{
					continue;
				}

				GBSPPlane	plane;
				plane.mNormal	=mGFXPlanes[mGFXFaces[i].mPlaneNum].mNormal;
				plane.mDist		=mGFXPlanes[mGFXFaces[i].mPlaneNum].mDist;
				plane.mType		=GBSPPlane.PLANE_ANY;

				TriPatch	tri	=new TriPatch(plane);
				if(tri == null)
				{
					CoreEvents.Print("AbsorbPatches:  Tri_PatchCreate failed.\n");
					return	false;
				}
				
				int	planeNum	=mGFXFaces[i].mPlaneNum;
				int	planeSide	=mGFXFaces[i].mPlaneSide;
				
				RADPatch	opatch	=mFacePatches[i];

				Bounds	bounds;
				GetFaceMinsMaxs(i, out bounds);
				
				for(PlaneFace planeFace=planeFaces[planeNum];planeFace != null;planeFace=planeFace.mNext)
				{
					int	faceNum	=planeFace.mGFXFace;

					if(mGFXFaces[faceNum].mPlaneSide != planeSide)
					{
						continue;
					}

					RADPatch.BuildTriPatchFromList(mFacePatches[faceNum], bounds, tri,
													patchSize);
				}
				if(!tri.TriangulatePoints())
				{
					CoreEvents.Print("AbsorbPatches:  Could not triangulate patches.\n");
					return	false;
				}
				
				if((Flags & TexInfo.GOURAUD) != 0)
				{
					for(int k=0;k < pGFXFace.mNumVerts;k++)
					{
						Int32	vn;

						vn	=pGFXFace.mFirstVert + k;

						Vector3	point	=mGFXVerts[mGFXVertIndexes[vn]];

						Vector3	add;
						tri.SampleTriangulation(point, out add);

						mGFXRGBVerts[vn]	+=add;
					}
				}
				else
				{
					Vector3	[]rgb	=mLightMaps[i].GetRGBLightData(0);
					bool	Created	=(rgb != null);

					Vector3	[]facePoints	=mFaceInfos[i].GetPoints();

					int	rgbOfs	=0;				
					for(int k=0;k < facePoints.Length;k++, rgbOfs++)
					{
						Vector3	point	=facePoints[k];
						Vector3	add;
						if(!tri.SampleTriangulation(point, out add))
						{
							CoreEvents.Print("AbsorbPatches:  Could not sample from patch triangles.\n");
							continue;
						}

						if(!Created)
						{
							if(add.X > 0 || add.Y > 0 || add.Z > 0)
							{
								mLightMaps[i].AllocLightType(0, facePoints.Length);
								Created	=true;
							}
						}
						if(Created)
						{
							rgb	=mLightMaps[i].GetRGBLightData(0);
							rgb[k]	+=add;
						}
					}
				}
				tri			=null;
			}

			planeFaces	=null;

			return	true;
		}


		bool FindPatchReceivers(RADPatch patch, float []recAmount, byte []visData)
		{
			GFXLeaf	leaf	=mGFXLeafs[patch.mLeaf];
			Int32	clust	=leaf.mCluster;
			Int32	area	=leaf.mArea;

			bool	bVisInfo;
			Int32	visOfs	=0;
			if(clust >= 0 && mGFXClusters[clust].mVisOfs >= 0)
			{
				visOfs	=mGFXClusters[clust].mVisOfs;
				bVisInfo	=true;
			}
			else
			{
				bVisInfo	=false;
			}

			float	total	=0.0f;
			Vector3	norm	=patch.GetPlaneNormal();

			//For each face, go through all it's patches
			for(int i=0;i < NumPatches;i++)
			{
				RADPatch	patch2	=mPatchList[i];
				
				recAmount[i]	=0.0f;

				if(patch2 == patch)
				{
					continue;
				}

				leaf	=mGFXLeafs[patch2.mLeaf];

				//Radiosity only bounces in it's original area
				if(leaf.mArea != area)
				{
					continue;
				}

				if(bVisInfo)
				{
					clust	=leaf.mCluster;
					if(clust >= 0 && ((visData[visOfs + clust>>3]
						& (1 << (clust & 7))) == 0))
					{
						continue;
					}
				}

				Vector3	vect;
				float	dist	=patch.DistVecBetween(patch2, out vect);

				//if (Dist > PatchSize)
				if(dist == 0.0f)
				{
					continue;	// Error
				}
				
				float	scale	=Vector3.Dot(vect, norm);
				scale			*=-Vector3.Dot(vect, patch2.GetPlaneNormal());

				if(scale <= 0)
				{
					continue;
				}

				if(patch.RayCastBetween(patch2, RayCollide))
				{
					//blocked by something in the world
					continue;
				}

				float	amount	=scale * patch2.GetArea() / (dist * dist);
				if(amount <= 0.0f)
				{
					continue;
				}
				recAmount[i]	=amount;

				//Add the receiver
				total	+=amount;
				NumReceivers++;
				patch.mNumReceivers++;
			}

			patch.mReceivers	=new RADReceiver[patch.mNumReceivers];
			int	roffs	=0;
			for(int i=0;i < NumPatches;i++)
			{
				if(recAmount[i] == 0.0f)
				{
					continue;
				}
				patch.mReceivers[roffs]			=new RADReceiver();
				patch.mReceivers[roffs].mPatch	=(UInt16)i;
				patch.mReceivers[roffs].mAmount	=(UInt16)(recAmount[i] * 0x10000 / total);
				roffs++;
			}
			return	true;
		}


		bool CalcReceivers(string fileName, byte []visData)
		{
			NumReceivers	=0;

			//Try to load the receiver file first!!!
			if(LoadReceiverFile(fileName))
			{
				CoreEvents.Print("--- Found receiver file ---\n");
				return	true;
			}

			CoreEvents.Print(" --- Calculating receivers from scratch ---\n");

			float	[]recAmount	=new float[mPatchList.Length];

			object	prog	=ProgressWatcher.RegisterProgress(0, mPatchList.Length, 0);
			for(int i=0;i < mPatchList.Length;i++)
			{
				RADPatch	patch	=mPatchList[i];

				if(!FindPatchReceivers(patch, recAmount, visData))
				{
					CoreEvents.Print("CalcReceivers:  There was an error calculating receivers.\n");
					return	false;
				}
				ProgressWatcher.UpdateProgress(prog, i);
			}
			recAmount	=null;

			ProgressWatcher.DestroyProgress(prog);

			CoreEvents.Print("Num Receivers        : " + NumReceivers + "\n");

			// Save receiver file for later retreival
			if(!SaveReceiverFile(fileName))
			{
				CoreEvents.Print("CalcReceivers:  Failed to save receiver file...\n");
				return	false;
			}
			return	true;
		}


		byte []LoadVisData(string fileName)
		{
			string	sansExt	=UtilityLib.FileUtil.StripExtension(fileName);

			//no longer sans I guess
			sansExt	+=".VisData";

			if(!File.Exists(sansExt))
			{
				return	null;
			}

			FileStream		fs	=new FileStream(sansExt, FileMode.Open, FileAccess.Read);
			BinaryReader	br	=new BinaryReader(fs);

			UInt32	magic	=br.ReadUInt32();

			if(magic != 0x715da7aa)
			{
				return	null;
			}

			byte	[]ret	=null;

			ret	=UtilityLib.FileUtil.ReadByteArray(br);

			//check material vis, but
			//donut need it for lighting
			byte	[]donutNeed	=UtilityLib.FileUtil.ReadByteArray(br);

			//load clusters
			mGFXClusters	=UtilityLib.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return UtilityLib.FileUtil.InitArray<GFXCluster>(count); }) as GFXCluster[];

			br.Close();
			fs.Close();

			return	ret;
		}


		void LightGBSPFileCB(object threadContext)
		{
			LightParameters	lp	=threadContext as LightParameters;

			string	recFile;

			CoreEvents.Print(" --- Radiosity GBSP File --- \n");

			BinaryWriter	bw		=null;
			FileStream		file	=null;
			
			GFXHeader	header	=LoadGBSPFile(lp.mFileName);
			if(header == null)
			{
				CoreEvents.Print("LightGBSPFile:  Could not load GBSP file: " + lp.mFileName + "\n");
				CoreEvents.FireLightDoneDoneEvent(false, null);
				return;
			}

			CoreEvents.FireNumPlanesChangedEvent(mGFXPlanes.Length, null);
			CoreEvents.FireNumVertsChangedEvent(mGFXVertIndexes.Length, null);

			byte	[]visData	=LoadVisData(lp.mFileName);

			//ensure vis is built
			if(visData == null)
			{
				CoreEvents.Print("No vis data for lighting.  Please run a vis on the map before attempting light.\n");
				CoreEvents.FireLightDoneDoneEvent(false, null);
				return;
			}

			CoreEvents.FireNumClustersChangedEvent(mGFXClusters.Length, null);

			mLightMapGridSize	=lp.mLightParams.mLightGridSize;
			
			//Allocate some RGBLight data now
			mGFXRGBVerts	=new Vector3[mGFXVertIndexes.Length];

			Vector3	[]vertNormals	=MakeSmoothVertNormals();
			if(vertNormals == null)
			{
				CoreEvents.Print("LightGBSPFile:  MakeVertNormals failed...\n");
				goto	ExitWithError;
			}

			//Make sure no existing light exist...
			mGFXLightData	=null;

			//Get the receiver file name
			int	extPos	=lp.mFileName.LastIndexOf(".");
			recFile		=lp.mFileName.Substring(0, extPos);
			recFile		+=".rec";

			file	=new FileStream(lp.mFileName,
									FileMode.OpenOrCreate, FileAccess.Write);

			if(file == null)
			{
				CoreEvents.Print("LightGBSPFile:  Could not open GBSP file for writing: " + lp.mFileName + "\n");
				goto	ExitWithError;
			}
			bw	=new BinaryWriter(file);

			DateTime	startTime	=DateTime.Now;

			CoreEvents.Print("Starting light at " + startTime + "\n");
			CoreEvents.Print("Num Faces            : " + mGFXFaces.Length + "\n");

			//Build the patches (before direct lights are created)
			if(lp.mLightParams.mbRadiosity)
			{
				if(!BuildPatches(lp.mLightParams.mSurfaceReflect,
					lp.mLightParams.mPatchSize, lp.mLightParams.mbFastPatch,
					lp.mBSPParams.mbVerbose, lp.mC4M))
				{
					goto	ExitWithError;
				}
			}

			if(!CreateDirectLights(lp.mLightParams.mbRadiosity))
			{
				CoreEvents.Print("LightGBSPFile:  Could not create main lights.\n");
				goto	ExitWithError;
			}

			//Light faces, and apply to patches
			if(!LightFaces(lp, vertNormals, visData))	//Light all the faces lightmaps, and apply to patches
			{
				goto	ExitWithError;
			}

			FreeDirectLights();

			if(lp.mLightParams.mbRadiosity)
			{
				//Pre-calc how much light is distributed to each patch from every patch
				if(!CalcReceivers(recFile, visData))	
				{
					goto	ExitWithError;
				}

				//Bounce patches around to their receivers
				if(!BouncePatches(lp.mLightParams.mNumBounces, lp.mBSPParams.mbVerbose))	//Bounce them around
				{
					goto	ExitWithError;
				}
			
				FreeReceivers();		//Don't need these anymore

				//Apply the patches back into the light maps
				if(!AbsorbPatches(lp.mLightParams.mPatchSize))	//Apply the patches to the lightmaps
				{
					goto	ExitWithError;
				}			
				FreePatches();	//Don't need these anymore...
			}

			FinalizeRGBVerts(lp.mLightParams.mMinLight, lp.mLightParams.mMaxIntensity);

			int	numRGBMaps	=0;

			//grab combined lightmap data
			List<byte>	lightData	=BuildLightMaps(ref numRGBMaps,
				lp.mLightParams.mMinLight, lp.mLightParams.mLightScale,
				lp.mLightParams.mMaxIntensity);

			if(lightData == null)
			{
				goto	ExitWithError;
			}
			mGFXLightData	=lightData.ToArray();

			WriteLight(bw, header.mbHasMaterialVis);

			bw.Close();
			file.Close();

			CleanupLight();

			DateTime	done	=DateTime.Now;

			CoreEvents.Print("Finished light at " + done + "\n");
			CoreEvents.Print(done - startTime + " elapsed\n");
			CoreEvents.Print("Num Light Maps       : " + numRGBMaps + "\n");

			CoreEvents.FireLightDoneDoneEvent(true, null);
			return;

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

				CoreEvents.FireLightDoneDoneEvent(false, null);
				return;
			}
		}


		public void LightGBSPFile(string fileName, CoreDelegates.GetEmissiveForMaterial c4m,
			LightParams lightParams, BSPBuildParams buildParams)
		{
			LightParameters	lp	=new LightParameters();
			lp.mBSPParams	=buildParams;
			lp.mLightParams	=lightParams;
			lp.mC4M			=c4m;
			lp.mFileName	=fileName;

			ThreadPool.QueueUserWorkItem(LightGBSPFileCB, lp);
		}


		Vector3	[]MakeSmoothVertNormals()
		{
			Vector3	[]ret	=new Vector3[mGFXVerts.Length];
			if(ret == null)
			{
				CoreEvents.Print("MakeSmoothVertNormals:  Out of memory for normals.\n");
				return	null;
			}

			for(int i=0;i < mGFXFaces.Length;i++)
			{
				GFXFace		f	=mGFXFaces[i];
				GFXTexInfo	tex	=mGFXTexInfos[f.mTexInfo];

				//grab face normal
				Vector3	Normal	=mGFXPlanes[f.mPlaneNum].mNormal;

				if(f.mPlaneSide != 0)
				{
					Normal	=-Normal;
				}

				//adds adjacent faces, not sure what happens
				//if you get stuff that is two sided
				//this smooths normals for gouraud
				for(int v=0;v < f.mNumVerts;v++)
				{
					Int32	vn	=f.mFirstVert + v;

					Int32	Index	=mGFXVertIndexes[vn];

					ret[Index]	=ret[Index] + Normal;
				}
			}

			for(int i=0;i < mGFXVerts.Length;i++)
			{
				ret[i].Normalize();
			}
			return	ret;
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


		void MakeSampleOffsets()
		{
			mSampleOffsets		=new Vector2[9];
			mSampleOffsets[0]	=Vector2.Zero;
			mSampleOffsets[1]	=Vector2.One * -0.5f;
			mSampleOffsets[2]	=Vector2.UnitX * 0.5f + Vector2.UnitY * -0.5f;
			mSampleOffsets[3]	=Vector2.One * 0.5f;
			mSampleOffsets[4]	=Vector2.UnitX * -0.25f + Vector2.UnitY * 0.25f;
			mSampleOffsets[5]	=Vector2.One * -0.25f;
			mSampleOffsets[6]	=Vector2.UnitX * 0.25f + Vector2.UnitY * -0.25f;
			mSampleOffsets[7]	=Vector2.One * 0.25f;
			mSampleOffsets[8]	=Vector2.UnitX * -0.25f + Vector2.UnitY * 0.25f;
		}


		bool CreateDirectLights(bool bRadiosity)
		{
			Int32	numDirectLights	=0;
			Int32	numSurfLights	=0;

			DirectClusterLights.Clear();

			// Create the entity lights first
			for(int i=0;i < mGFXEntities.Length;i++)
			{
				MapEntity	ent	=mGFXEntities[i];

				if(!(ent.mData.ContainsKey("light")
					|| ent.mData.ContainsKey("_light")
					|| ent.mData.ContainsKey("light_torch_small_walltorch")
					))
				{
					continue;
				}

				DirectLight	dLight	=new DirectLight();

				Vector4	colorVec	=Vector4.Zero;
				if(!ent.GetLightValue(out colorVec))
				{
					CoreEvents.Print("Warning:  Light entity, couldn't get color\n");
					colorVec.W	=200.0f;	//default
				}

				Vector3	color;
				color.X	=colorVec.X;
				color.Y	=colorVec.Y;
				color.Z	=colorVec.Z;

				//Default it to 255/255/255 if no light is specified
				if(color.Length() < 1.0f)
				{
					color	=Vector3.One;
				}
				else
				{
					color.Normalize();
				}

				if(!ent.GetOrigin(out dLight.mOrigin))
				{
					CoreEvents.Print("Warning:  Light entity, couldn't get origin\n");
				}

				dLight.mColor		=color;
				dLight.mIntensity	=colorVec.W;// * mGlobals.EntityScale;
				dLight.mType		=DirectLight.DLight_Point;	//default
				
				//animated light styles
				ent.GetInt("style", out dLight.mLType);

				Vector3	Angles;
				if(ent.GetVectorNoConversion("angles", out Angles))
				{
					//hammer style
					Vector3	Angles2	=Vector3.Zero;
					Angles2.X	=MathHelper.ToRadians(Angles.X);
					Angles2.Y	=MathHelper.ToRadians(Angles.Y);
					Angles2.Z	=MathHelper.ToRadians(Angles.Z);

					//coord system conversion needs this
					Angles2.Y	+=(float)(Math.PI / 2.0f);

					Matrix	mat	=Matrix.Identity;

					//check for pitch
					float	pitch	=0.0f;
					if(ent.GetFloat("pitch", out pitch))
					{
						pitch	=MathHelper.ToRadians(pitch);

						//todo test and make sure this order is correct
						mat		=Matrix.CreateRotationX(Angles2.Y)
							* Matrix.CreateRotationY(pitch)
							* Matrix.CreateRotationZ(Angles2.Z);
//							CreateFromYawPitchRoll(Angles2.Y, pitch, Angles2.Z); 
					}
					else
					{
						//todo test the order
						mat	=Matrix.CreateRotationX(Angles2.X)
							* Matrix.CreateRotationY(Angles2.Y)
							* Matrix.CreateRotationZ(Angles2.Z);
//						mat	=Matrix.CreateFromYawPitchRoll(Angles2.Y, Angles2.X, Angles2.Z); 
					}

					dLight.mNormal	=mat.Forward;

					dLight.mAngle	=(float)Math.Cos(dLight.mAngle / 180.0f * Math.PI);					

					//check for cone
					float	cone	=0.0f;
					if(ent.GetFloat("_cone", out cone))
					{
						dLight.mAngle	=MathHelper.ToRadians(cone);
					}

					//check for inner cone
					float	inner	=0.0f;
					if(ent.GetFloat("_inner_cone", out inner))					
					{
						//no use for this yet
//						dLight.mAngle	+=MathHelper.ToRadians(inner);
					}
				}
				else if(ent.GetVector("mangle", out Angles))
				{
					//quake 1 style
					Vector3	Angles2	=Vector3.Zero;
					Angles2.X	=(Angles.X / 180) * (float)Math.PI;
					Angles2.Y	=(Angles.Y / 180) * (float)Math.PI;
					Angles2.Z	=(Angles.Z / 180) * (float)Math.PI;

//					Matrix	mat	=Matrix.CreateFromYawPitchRoll(Angles2.X, Angles2.Y, Angles2.Z); 

					Matrix	mat	=Matrix.CreateRotationX(Angles2.X) *
						Matrix.CreateRotationY(Angles2.Y) *
						Matrix.CreateRotationZ(Angles2.Z);

					Angles2	=mat.Left;
					dLight.mNormal.X	=-Angles2.X;
					dLight.mNormal.Y	=-Angles2.Y;
					dLight.mNormal.Z	=-Angles2.Z;

					dLight.mAngle	=(float)Math.Cos(dLight.mAngle / 180.0f * Math.PI);					
				}

				//Find out what type of light it is by it's classname...
				ent.GetLightType(out dLight.mType);

				Int32	nodeLandedIn	=FindNodeLandedIn(mGFXModels[0].mRootNode, dLight.mOrigin);
				Int32	leaf	=-(nodeLandedIn + 1);
				Int32	clust	=mGFXLeafs[leaf].mCluster;

				if(clust < 0)
				{
					CoreEvents.Print("*WARNING* CreateLights:  Light in solid leaf at " + dLight.mOrigin + "\n");
					continue;
				}
				if(DirectClusterLights.ContainsKey(clust))
				{
					dLight.mNext	=DirectClusterLights[clust];
					DirectClusterLights[clust]	=dLight;
				}
				else
				{
					dLight.mNext	=null;
					DirectClusterLights.Add(clust, dLight);
				}

				DirectLights.Add(dLight);
				numDirectLights++;
			}

			CoreEvents.Print("Num Normal Lights   : " + numDirectLights + "\n");

			//Stop here if no radisosity is going to be done
			if(!bRadiosity)
			{
				return	true;
			}
			
			//Now create the radiosity direct lights (surface emitters)
			for(int i=0;i < mGFXFaces.Length;i++)
			{
				GFXTexInfo	tex	=mGFXTexInfos[mGFXFaces[i].mTexInfo];

				//Only look at surfaces that want to emit light
				if((tex.mFlags & TexInfo.LIGHT) == 0)
				{
					continue;
				}

				for(RADPatch p=mFacePatches[i];p != null;p=p.mNext)
				{
					Int32	leaf	=p.mLeaf;
					Int32	clust	=mGFXLeafs[leaf].mCluster;

					if(clust < 0)
					{
						continue;	//Skip, solid
					}

					DirectLight	dLight	=new DirectLight();

					p.InitDLight(dLight, tex.mFaceLight);

					//Insert this surface direct light into the list of lights
					if(DirectClusterLights.ContainsKey(clust))
					{
						dLight.mNext	=DirectClusterLights[clust];
						DirectClusterLights[clust]	=dLight;
					}
					else
					{
						dLight.mNext	=null;
						DirectClusterLights.Add(clust, dLight);
					}

					DirectLights.Add(dLight);
					numSurfLights++;
				}
			}
			CoreEvents.Print("Num Surf Lights     : " + numSurfLights + "\n");
			return	true;
		}


		bool CalcFaceInfo(FInfo faceInfo, LInfo lightInfo, int lightGridSize)
		{
			Int32	fidx	=faceInfo.GetFaceIndex();
			Int32	indOffset;
			
			List<Vector3>	verts	=new List<Vector3>();
			indOffset				=mGFXFaces[fidx].mFirstVert;

			for(int i=0;i < mGFXFaces[fidx].mNumVerts;i++, indOffset++)
			{
				int	vIndex	=mGFXVertIndexes[indOffset];
				verts.Add(mGFXVerts[vIndex]);
			}

			faceInfo.CalcFaceLightInfo(lightInfo, verts, lightGridSize);

			return	true;
		}


		bool VertexShadeFace(Int32 faceNum, Vector3 []vertNormals)
		{
			if(mGFXRGBVerts == null || mGFXRGBVerts.Length == 0)
			{
				return	false;
			}

			GFXFace		gfxFace		=mGFXFaces[faceNum];
			GFXTexInfo	tex			=mGFXTexInfos[gfxFace.mTexInfo];
			Int32		numVerts	=gfxFace.mNumVerts;

			for(int v=0;v < gfxFace.mNumVerts;v++)
			{
				Int32	vn		=gfxFace.mFirstVert + v;
				Int32	index	=mGFXVertIndexes[vn];
				Vector3	vert	=mGFXVerts[index];

				if(tex.IsLight())
				{
					//lights should glow
					mGFXRGBVerts[vn]	=Vector3.One * 255.0f;
				}

				Vector3	norm;
				if(tex.IsFlat())
				{
					norm	=mFaceInfos[faceNum].GetPlaneNormal();
				}
				else
				{
					norm	=vertNormals[index];
				}
				
				for(int i=0;i < DirectLights.Count;i++)
				{
					DirectLight	dLight	=DirectLights[i];

					float	intensity	=dLight.mIntensity;

					//Find the angle between the light, and the face normal
					Vector3	vect	=dLight.mOrigin - vert;				
					float	dist	=vect.Length();
					vect.Normalize();

					float	angle	=Vector3.Dot(vect, norm);

					if(angle <= 0.001f)
					{
						goto Skip;
					}

					float	val;
					switch(dLight.mType)
					{
						case DirectLight.DLight_Point:
						{
							val	=(intensity - dist) * angle;//(Angle*0.5f+0.5f);
							break;
						}
						case DirectLight.DLight_Spot:
						{
							float Angle2	=-Vector3.Dot(vect, dLight.mNormal);
							if(Angle2 < dLight.mAngle)
							{
								goto Skip;
							}

							val	=(intensity - dist) * angle;
							break;
						}
						case DirectLight.DLight_Surface:
						{
							float Angle2	=-Vector3.Dot(vect, dLight.mNormal);
							if(Angle2 <= 0.001f)
							{
								goto Skip;	//Behind light surface
							}
							val	=(intensity / (dist * dist)) * angle * Angle2;
							break;
						}
						default:
						{
							CoreEvents.Print("ApplyLightsToFace:  Invalid light.\n");
							return	false;
						}
					}
					if(val <= 0.0f)
					{
						goto	Skip;
					}

					//This is the slowest test, so make it last
					Vector3	colResult	=Vector3.Zero;
					if(RayCollide(vert, dLight.mOrigin, ref colResult))
					{
						goto	Skip;	//Ray is in shadow
					}
					mGFXRGBVerts[vn]	+=(dLight.mColor * val);

					Skip:;				
				}
			}
			return	true;
		}


		void TransferLightToPatches(Int32 face, int lightGridSize)
		{
			GFXFace	gfxFace	=mGFXFaces[face];

			for(RADPatch patch=mFacePatches[face];patch != null;patch=patch.mNext)
			{
				Vector3	rgb, vert;
				Int32	rgbOfs	=gfxFace.mFirstVert;

				patch.ResetSamples();
				for(int i=0;i < gfxFace.mNumVerts;i++)
				{
					Int32	k;

					vert	=mGFXVerts[mGFXVertIndexes[i+gfxFace.mFirstVert]];
					rgb		=mGFXRGBVerts[rgbOfs];	//bug in original genesis
					for(k=0;k < 3;k++)
					{
						if(UtilityLib.Mathery.VecIdx(patch.mBounds.mMins, k)
							> UtilityLib.Mathery.VecIdx(vert, k) + lightGridSize)
						{
							break;
						}				
						if(UtilityLib.Mathery.VecIdx(patch.mBounds.mMaxs, k)
							< UtilityLib.Mathery.VecIdx(vert, k) - lightGridSize)
						{
							break;
						}				
					}

					if(k == 3)
					{
						//Add the Color to the patch
						patch.AddSample(rgb);
					}
					rgbOfs++;
				}
				patch.AverageRadStart();
			}
		}


		bool LightFaces(LightParameters lp, Vector3 []vertNormals, byte []visData)
		{
			mLightMaps	=new LInfo[mGFXFaces.Length];

			if(mLightMaps == null)
			{
				CoreEvents.Print("LightFaces:  Out of memory for Lightmaps.\n");
				return	false;
			}

			mFaceInfos	=new FInfo[mGFXFaces.Length];

			if(mFaceInfos == null)
			{
				CoreEvents.Print("LightFaces:  Out of memory for FaceInfo.\n");
				return	false;
			}

			for(int i=0;i < mGFXFaces.Length;i++)
			{
				mLightMaps[i]	=new LInfo();
				mFaceInfos[i]	=new FInfo();
			}

			object	prog	=ProgressWatcher.RegisterProgress(0, mGFXFaces.Length, 0);

			UtilityLib.TSPool<bool []>	boolPool	=new UtilityLib.TSPool<bool[]>(() => new bool[LInfo.MAX_LMAP_SIZE * LInfo.MAX_LMAP_SIZE]);

			//avoid going nutso with threads
			ParallelOptions	po			=new ParallelOptions();
			po.MaxDegreeOfParallelism	=lp.mBSPParams.mMaxThreads;

			Parallel.For(0, mGFXFaces.Length, po, i =>
			{
				ProgressWatcher.UpdateProgressIncremental(prog);

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

				if(tex.IsGouraud() || tex.IsFlat())
				{
					if(!VertexShadeFace(i, vertNormals))
					{
						CoreEvents.Print("LightFaces:  VertexShadeFace failed...\n");
						return;
					}
					
					if(lp.mLightParams.mbRadiosity)
					{
						TransferLightToPatches(i, lp.mLightParams.mLightGridSize);
					}
				}
				else if(tex.IsLightMapped())
				{
					if(!CalcFaceInfo(mFaceInfos[i], mLightMaps[i], lp.mLightParams.mLightGridSize))
					{
						return;
					}
			
					Int32	size	=mLightMaps[i].CalcSize();

					mFaceInfos[i].AllocPoints(size);

					for(int s=0;s < lp.mLightParams.mNumSamples;s++)
					{
						CalcFacePoints(mFaceInfos[i], mLightMaps[i], lp.mLightParams.mLightGridSize,
							mSampleOffsets[s], lp.mLightParams.mbSeamCorrection, boolPool);

						if(!ApplyLightsToFace(mFaceInfos[i], mLightMaps[i],
							1 / (float)lp.mLightParams.mNumSamples, visData))
						{
							return;
						}
					}
				
					if(lp.mLightParams.mbRadiosity)
					{
						// Update patches for this face
						ApplyLightmapToPatches(i, lp.mLightParams.mLightGridSize);
					}
				}
			});

			ProgressWatcher.Clear();

			return	true;
		}


		void ApplyLightmapToPatches(Int32 face, int lightGridSize)
		{
			mLightMaps[face].ApplyLightToPatchList(mFacePatches[face],
				lightGridSize, mFaceInfos[face].GetPoints());
		}


		bool ApplyLightsToFace(FInfo faceInfo, LInfo lightInfo, float scale, byte []visData)
		{
			Vector3	norm	=faceInfo.GetPlaneNormal();

			Vector3	[]facePoints	=faceInfo.GetPoints();

			for(int v=0;v < facePoints.Length;v++)
			{
				Int32	nodeLandedIn	=FindNodeLandedIn(0, facePoints[v]);
				Int32	leaf	=-(nodeLandedIn + 1);

				if(leaf < 0 || leaf >= mGFXLeafs.Length)
				{
					CoreEvents.Print("ApplyLightsToFace:  Invalid leaf num.\n");
					return	false;
				}

				Int32	clust	=mGFXLeafs[leaf].mCluster;
				if(clust < 0)
				{
					continue;
				}

				if(clust >= mGFXClusters.Length)
				{
					CoreEvents.Print("*WARNING* ApplyLightsToFace:  Invalid cluster num.\n");
					continue;
				}

				for(int c=0;c < mGFXClusters.Length;c++)
				{
					//vis is broken right now
					if((visData[mGFXClusters[clust].mVisOfs + (c >> 3)] & (1 << (c & 7))) == 0)
					{
						continue;
					}

					if(!DirectClusterLights.ContainsKey(c))
					{
						continue;
					}

					for(DirectLight dLight=DirectClusterLights[c];dLight != null;dLight=dLight.mNext)
					{
						float	intensity	=dLight.mIntensity;
					
						//Find the angle between the light, and the face normal
						Vector3	vect	=dLight.mOrigin - facePoints[v];
						float	dist	=vect.Length();
						vect.Normalize();

						float	angle	=Vector3.Dot(vect, norm);
						if(angle <= 0.001f)
						{
							goto	Skip;
						}
						
						float	val;
						switch(dLight.mType)
						{
							case DirectLight.DLight_Point:
							{
								val	=(intensity - dist) * angle;
								break;
							}
							case DirectLight.DLight_Spot:
							{
								float	Angle2	=-Vector3.Dot(vect, dLight.mNormal);

								if(Angle2 < dLight.mAngle)
								{
									goto	Skip;
								}

								val	=(intensity - dist) * angle;
								break;
							}
							case DirectLight.DLight_Surface:
							{
								float	Angle2	=-Vector3.Dot(vect, dLight.mNormal);
								if(Angle2 <= 0.001f)
								{
									goto	Skip;	// Behind light surface
								}

								val	=(intensity / (dist * dist)) * angle * Angle2;
								break;
							}
							default:
							{
								CoreEvents.Print("ApplyLightsToFace:  Invalid light.\n");
								return	false;
							}
						}

						if(val <= 0.0f)
						{
							goto	Skip;
						}

						// This is the slowest test, so make it last
						Vector3	colResult	=Vector3.Zero;
						if(RayCollide(facePoints[v], dLight.mOrigin, ref colResult))
						{
							goto	Skip;	//Ray is in shadow
						}

						Int32	lightType	=dLight.mLType;

						//If the data for this LType has not been allocated, allocate it now...
						lightInfo.AllocLightType(lightType, facePoints.Length);

						Vector3	[]rgb	=lightInfo.GetRGBLightData(lightType);

						rgb[v]	+=dLight.mColor * (val * scale);

						Skip:;
					}
				}
			}

			return	true;
		}


		void CalcFacePoints(FInfo faceInfo, LInfo lightInfo, int lightGridSize,
			Vector2 UVOfs, bool bExtraLightCorrection,
			UtilityLib.TSPool<bool []> boolPool)
		{
			faceInfo.CalcFacePoints(lightInfo, lightGridSize,
				UVOfs, bExtraLightCorrection, boolPool,
				IsPointInSolidSpace, RayCollide);
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


		List<byte> BuildLightMaps(ref int numRGBMaps, Vector3 minLight, float lightScale, float maxIntensity)
		{
			Int32	LDataOfs	=0;
			byte	[]LData		=new byte[LInfo.MAX_LMAP_SIZE * LInfo.MAX_LMAP_SIZE * 3 * 4];

			List<byte>	lightData	=new List<byte>();
			
			//Reset the light offset
			int	LightOffset	=0;
			numRGBMaps		=0;
			
			//Go through all the faces
			for(int i=0;i < mGFXFaces.Length;i++)
			{
				LInfo	L		=mLightMaps[i];
				FInfo	fInfo	=mFaceInfos[i];

				// Set face defaults
				mGFXFaces[i].mLightOfs	=-1;
				mGFXFaces[i].mLWidth	=L.GetLWidth();
				mGFXFaces[i].mLHeight	=L.GetLHeight();
				mGFXFaces[i].mLType0	=255;
				mGFXFaces[i].mLType1	=255;
				mGFXFaces[i].mLType2	=255;
				mGFXFaces[i].mLType3	=255;
				
				//Skip special faces with no lightmaps
				if((mGFXTexInfos[mGFXFaces[i].mTexInfo].mFlags
					& TexInfo.NO_LIGHTMAP) != 0)
				{
					continue;
				}

				//Get the size of map
				int	size	=fInfo.GetPoints().Length;

				Vector3	[]rgb	=L.GetRGBLightData(0);

				//Create style 0, if min light is set, and style 0 does not exist
				if((rgb == null) &&
					(minLight.X > 1 || minLight.Y > 1 || minLight.Z > 1))
				{
					L.AllocLightType(0, size);
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

				numRGBMaps++;
				int	numLTypes	=0;		// Reset number of LTypes for this face
				for(int k=0;k < LInfo.MAX_LTYPE_INDEX;k++)
				{
					rgb	=L.GetRGBLightData(k);
					if(rgb == null)
					{
						continue;
					}

					if(numLTypes >= LInfo.MAX_LTYPES)
					{
						CoreEvents.Print("SaveLightmaps:  Max LightTypes on face.\n");
						return	null;
					}

					if(numLTypes == 0)
					{
						mGFXFaces[i].mLType0	=(byte)k;
					}
					else if(numLTypes == 1)
					{
						mGFXFaces[i].mLType1	=(byte)k;
					}
					else if(numLTypes == 2)
					{
						mGFXFaces[i].mLType2	=(byte)k;
					}
					else if(numLTypes == 3)
					{
						mGFXFaces[i].mLType3	=(byte)k;
					}
					numLTypes++;

					LDataOfs	=0;

					for(int j=0;j < size;j++)
					{
						Vector3	WorkRGB	=rgb[j] * lightScale;

						if(k == 0)
						{
							WorkRGB	+=minLight;
						}
						
						float	max	=0.0f;

						for(int l=0;l < 3;l++)
						{
							float	Val	=UtilityLib.Mathery.VecIdx(WorkRGB, l);

							if(Val < 1.0f)
							{
								Val	=1.0f;
								UtilityLib.Mathery.VecIdxAssign(ref WorkRGB, l, Val);
							}

							if(Val > max)
							{
								max	=Val;
							}
						}

						Debug.Assert(max > 0.0f);
						
						float	max2	=Math.Min(max, maxIntensity);

						for(int l=0;l < 3;l++)
						{
							LData[LDataOfs]	=(byte)(UtilityLib.Mathery.VecIdx(WorkRGB, l) * (max2 / max));
							LDataOfs++;
							LightOffset++;
						}
					}

					for(int lidx=0;lidx < (3 * size);lidx++)
					{
						lightData.Add(LData[lidx]);
					}
					L.FreeLightType(k);
				}

				if(L.GetNumLightTypes() != numLTypes)
				{
					CoreEvents.Print("SaveLightMaps:  Num LightTypes was incorrectly calculated.\n");
					return	null;
				}
			}

			CoreEvents.Print("Light Data Size      : " + LightOffset + "\n");

			return	lightData;
		}
	}
}
