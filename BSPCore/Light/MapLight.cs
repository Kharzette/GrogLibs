using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using SharpDX;
using UtilityLib;


namespace BSPCore
{
	public partial class Map
	{
		//lights per cluster
		Dictionary<Int32, List<DirectLight>>	mDirectClusterLights
			=new Dictionary<Int32,List<DirectLight>>();

		//list of all lights
		List<DirectLight>	mDirectLights		=new List<DirectLight>();

		LInfo	[]mLightMaps;
		FInfo	[]mFaceInfos;
		Vector2	[]mSampleOffsets;

		const float	SunRayDist	=10000f;


		void GetFaceMinsMaxs(Int32 face, out Bounds bnd)
		{
			bnd	=new Bounds();
			for(int i=0;i < mGFXFaces[face].mNumVerts;i++)
			{
				int	Index	=mGFXVertIndexes[mGFXFaces[face].mFirstVert + i];
				bnd.AddPointToBounds(mGFXVerts[Index]);
			}			
		}


		byte []LoadVisData(string fileName)
		{
			string	sansExt	=FileUtil.StripExtension(fileName);

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

			ret	=FileUtil.ReadByteArray(br);

			//check material vis, but
			//donut need it for lighting
			byte	[]donutNeed	=FileUtil.ReadByteArray(br);

			//load clusters
			mGFXClusters	=FileUtil.ReadArray<GFXCluster>(br);

			br.Close();
			fs.Close();

			return	ret;
		}


		void LightGBSPFileCB(object threadContext)
		{
			LightParameters	lp	=threadContext as LightParameters;

			CoreEvents.Print(" --- Light GBSP File --- \n");

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
			if(visData == null && !lp.mBSPParams.mbBuildAsBModel)
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

			file	=new FileStream(lp.mFileName, FileMode.Open, FileAccess.Write);
			if(file == null)
			{
				CoreEvents.Print("LightGBSPFile:  Could not open GBSP file for writing: " + lp.mFileName + "\n");
				goto	ExitWithError;
			}
			bw	=new BinaryWriter(file);

			DateTime	startTime	=DateTime.Now;

			CoreEvents.Print("Starting light at " + startTime + "\n");
			CoreEvents.Print("Num Faces\t\t: " + mGFXFaces.Length + "\n");

			if(!CreateDirectLights(lp))
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

			FinalizeRGBVerts(lp.mLightParams.mMinLight, lp.mLightParams.mMaxIntensity);

			int	numRGBMaps	=0;

			//grab combined lightmap data
			List<byte>	lightData	=BuildLightMaps(ref numRGBMaps,
				lp.mLightParams.mMinLight, lp.mLightParams.mMaxIntensity);

			if(lightData == null)
			{
				goto	ExitWithError;
			}
			mGFXLightData	=lightData.ToArray();

			WriteLight(bw, header.mbHasMaterialVis, !lp.mBSPParams.mbBuildAsBModel);

			bw.Close();
			file.Close();

			//save entities, lightswitch stuff can modify
			string	entName	=FileUtil.StripExtension(lp.mFileName);
			entName			+=".EntData";

			file	=new FileStream(entName, FileMode.Create, FileAccess.Write);
			bw		=new BinaryWriter(file);
			SaveGFXEntData(bw);

			bw.Close();
			file.Close();

			//save recorded stuff if needed
			if(lp.mLightParams.mbRecording)
			{
				Debug.Assert(lp.mLightParams.mFacePoints.Count == lp.mLightParams.mFacePlanes.Count);

				string	sansExt	=FileUtil.StripExtension(lp.mFileName) + ".LightExplore";
				FileStream	fs	=new FileStream(sansExt, FileMode.Create, FileAccess.Write);
				if(fs == null)
				{
					CoreEvents.Print("Couldn't open light record output file: " + sansExt + "\n");
					return;
				}

				bw	=new BinaryWriter(fs);
				if(bw == null)
				{
					fs.Close();
					CoreEvents.Print("Couldn't open light record output file: " + sansExt + "\n");
					return;
				}

				//sample count for drawing extras differently
				bw.Write(lp.mLightParams.mNumSamples);

				bw.Write(lp.mLightParams.mFacePoints.Count);

				for(int i=0;i < lp.mLightParams.mFacePoints.Count;i++)
				{
					bw.Write(lp.mLightParams.mFacePoints[i].Count);

					for(int j=0;j < lp.mLightParams.mFacePoints[i].Count;j++)
					{
						FileUtil.WriteVector3(bw, lp.mLightParams.mFacePoints[i][j]);
					}

					//write plane for face i
					lp.mLightParams.mFacePlanes[i].Write(bw);

					//write finfo
					if(lp.mLightParams.mFInfos.ContainsKey(i))
					{
						bw.Write(true);
						lp.mLightParams.mFInfos[i].WriteVecs(bw);
					}
					else
					{
						bw.Write(false);
					}
				}

				bw.Close();
				fs.Close();

				CoreEvents.Print("Wrote " + lp.mLightParams.mFacePoints.Count
								+ " faces points and planes to file: " + sansExt + "\n");
			}
			CleanupLight();

			DateTime	done	=DateTime.Now;

			CoreEvents.Print("Finished light at " + done + "\n");
			CoreEvents.Print(done - startTime + " elapsed\n");
			CoreEvents.Print("Num Light Maps\t\t: " + numRGBMaps + "\n");

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


		public void LightGBSPFile(string fileName,
			LightParams lightParams, BSPBuildParams buildParams)
		{
			LightParameters	lp	=new LightParameters();
			lp.mBSPParams	=buildParams;
			lp.mLightParams	=lightParams;
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

				if(f.mbFlipSide)
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


		void CleanupLight()
		{
			FreeDirectLights();
			FreeLightMaps();
			FreeGBSPFile();
		}


		void FreeLightMaps()
		{
			mLightMaps	=null;
			mFaceInfos	=null;
		}


		//pattern goes like
		//
		//
		//			8	1	5
		//
		//			3	0	2
		//
		//			6	4	7
		//
		//
		void MakeSampleOffsets()
		{
			mSampleOffsets		=new Vector2[9];
			mSampleOffsets[0]	=Vector2.Zero;
			mSampleOffsets[1]	=Vector2.UnitY;
			mSampleOffsets[2]	=Vector2.UnitX;
			mSampleOffsets[3]	=-Vector2.UnitX;
			mSampleOffsets[4]	=-Vector2.UnitY;
			mSampleOffsets[5]	=Vector2.UnitX + Vector2.UnitY;
			mSampleOffsets[6]	=-Vector2.UnitX - Vector2.UnitY;
			mSampleOffsets[7]	=Vector2.UnitX - Vector2.UnitY;
			mSampleOffsets[8]	=-Vector2.UnitX + Vector2.UnitY;

			//sample points closer to the grid
			for(int i=0;i < 9;i++)
			{
				mSampleOffsets[i]	*=0.25f;
			}
		}


		//use extra samples to try to push a light trace point out of solid
		bool CorrectLightPoint(ref Vector3 pos, Vector3 t2wU, Vector3 t2wV)
		{
			for(int i=1;i < 9;i++)
			{
				Vector3	delta	=pos + t2wU * mSampleOffsets[i].X * mLightMapGridSize
					+ t2wV * mSampleOffsets[i].Y * mLightMapGridSize;

				if(!IsPointInSolidSpace(delta))
				{
					pos	=delta;
					return	true;
				}
			}
			return	false;
		}


		bool CreateDirectLights(LightParameters lp)
		{
			Int32	numDirectLights	=0;

			mDirectClusterLights.Clear();

			int	switchable	=32;	//switchable lights

			//store a switchable style per target name
			//this allows one switch to hit multiple lights
			Dictionary<string, Int32>	targetNameStyles	=new Dictionary<string, Int32>();

			// Create the entity lights first
			for(int i=0;i < mGFXEntities.Length;i++)
			{
				MapEntity	ent	=mGFXEntities[i];

				if(!ent.mData.ContainsKey("classname"))
				{
					continue;
				}

				string	className	=ent.mData["classname"];
				if(!className.StartsWith("light") && !className.StartsWith("_light"))
				{
					continue;
				}

				DirectLight	dLight	=new DirectLight();

				Vector3	color;
				Vector4	colorVec	=Vector4.Zero;
				if(!ent.GetLightValue(out colorVec))
				{
					//might be a sunlight which doesn't have a strength value
					if(ent.GetVectorNoConversion("_color", out color))
					{
						colorVec.X	=color.X;
						colorVec.Y	=color.Y;
						colorVec.Z	=color.Z;
						colorVec	*=300.0f;	//default
					}
					else
					{
						//some ents have no value, 200 default
						colorVec.W	=200f;
					}
				}

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
					color	=Misc.ColorNormalize(color);
				}

				if(!ent.GetOrigin(out dLight.mOrigin))
				{
					CoreEvents.Print("Warning:  Light entity, couldn't get origin\n");
				}

				dLight.mColor		=color;
				dLight.mIntensity	=colorVec.W;
				dLight.mType		=DirectLight.DLight_Point;	//default
				
				//animated light styles
				ent.GetInt("style", out dLight.mLType);

				//check for switchable
				if(ent.mData.ContainsKey("targetname"))
				{
					string	targName	=ent.mData["targetname"];

					if(targetNameStyles.ContainsKey(targName))
					{
						dLight.mLType	=targetNameStyles[targName];
					}
					else
					{
						targetNameStyles.Add(targName, switchable);
						dLight.mLType	=switchable++;
					}

					if(!ent.mData.ContainsKey("LightSwitchNum"))
					{
						ent.mData.Add("LightSwitchNum", "" + dLight.mLType);
					}
				}

				//Find out what type of light it is by it's classname...
				ent.GetLightType(out dLight.mType);

				if(dLight.mType == DirectLight.DLight_Spot)
				{
					//check for cone
					float	cone	=0.0f;
					if(ent.GetFloat("_cone", out cone))
					{
						dLight.mCone	=MathUtil.DegreesToRadians(cone);
					}
					else
					{
						//default of 10 degrees
						dLight.mCone	=MathUtil.DegreesToRadians(10f);
					}

					//should be a target
					if(ent.mData.ContainsKey("target"))
					{
						bool	bFound	=false;
						Vector3	targPos	=Vector3.Zero;
						string	targ	=ent.mData["target"];
						for(int j=0;j < mGFXEntities.Length;j++)
						{
							if(mGFXEntities[j].mData.ContainsKey("targetname"))
							{
								if(mGFXEntities[j].mData["targetname"] == targ)
								{
									if(mGFXEntities[j].GetOrigin(out targPos))
									{
										bFound	=true;
										break;
									}
								}
							}
						}

						if(bFound)
						{
							dLight.mNormal	=targPos - dLight.mOrigin;
							dLight.mNormal.Normalize();
						}
					}
				}
				else if(dLight.mType == DirectLight.DLight_Sun)
				{
					Vector3	orient;
					float	yaw		=0f;
					float	pitch	=0f;
					float	roll	=0f;
					if(ent.GetVectorNoConversion("angles", out orient))
					{
						//coordinate system goblinry
						pitch	=(int)-orient.X;
						yaw		=-90 + (int)-orient.Y;
						roll	=(int)orient.Z;
					}

					yaw		=MathUtil.DegreesToRadians(yaw);
					pitch	=MathUtil.DegreesToRadians(pitch);
					roll	=MathUtil.DegreesToRadians(roll);

					Matrix	rotMat	=Matrix.RotationYawPitchRoll(yaw, pitch, roll);
					dLight.mNormal	=rotMat.Forward;

					ent.GetFloat("strength", out dLight.mIntensity);
				}

				Int32	nodeLandedIn	=FindNodeLandedIn(mGFXModels[0].mRootNode, dLight.mOrigin);
				Int32	leaf	=-(nodeLandedIn + 1);
				Int32	clust	=mGFXLeafs[leaf].mCluster;

				if(clust < 0)
				{
					CoreEvents.Print("*WARNING* CreateLights:  Light in solid leaf at " + dLight.mOrigin + "\n");
					continue;
				}

				if(dLight.mType != DirectLight.DLight_Sun)
				{
					if(mDirectClusterLights.ContainsKey(clust))
					{
						mDirectClusterLights[clust].Add(dLight);
					}
					else
					{
						mDirectClusterLights.Add(clust, new List<DirectLight>());
						mDirectClusterLights[clust].Add(dLight);
					}
				}
				mDirectLights.Add(dLight);
				numDirectLights++;
			}

			CoreEvents.Print("Num Direct Lights\t: " + numDirectLights + "\n");

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

			faceInfo.CalcFaceLightInfo(lightInfo, verts, lightGridSize,
				mGFXTexInfos[mGFXFaces[fidx].mTexInfo]);

			return	true;
		}


		bool VertexShadeFace(Int32 faceNum, Vector3 []vertNormals,
			Matrix modelMat, Matrix modelInv, int modelIndex)
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

				vert	=Vector3.TransformCoordinate(vert, modelMat);

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
				
				for(int i=0;i < mDirectLights.Count;i++)
				{
					DirectLight	dLight	=mDirectLights[i];

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

					float	val	=0f;
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
							if(Angle2 < (1.0f - dLight.mCone))
							{
								goto Skip;
							}

							val	=(intensity - dist) * angle;
							break;
						}
						case DirectLight.DLight_Sun:
						{
							//Find the angle between the light, and the vert
							Vector3	sunRay		=vert + dLight.mNormal * -SunRayDist;
							Vector3	normRay		=sunRay;
							normRay.Normalize();

							float	angle2	=Vector3.Dot(normRay, norm);
							if(angle2 <= 0.001f)
							{
								goto	Skip;
							}

							Vector3	colResult	=Vector3.Zero;
							GFXFace	faceHit		=null;
							if(RayCollideToFace(vert, sunRay, modelIndex, modelInv, ref faceHit))
							{
								if(faceHit != null)
								{
									if(mGFXTexInfos[faceHit.mTexInfo].IsSky())
									{
										val	=(angle2 * dLight.mIntensity);
									}
								}
								else
								{
									//this is normal for verts as they aren't popped out into
									//empty space the way facepoints are for lightmaps, so often
									//the vert is considered in solid space
									//CoreEvents.Print("Light ray hit with no face returned!\n");
								}
							}
							else
							{
								CoreEvents.Print("Sunlight ray miss!\n");
							}
							break;
						}
						default:
						{
							CoreEvents.Print("VertexShadeFace:  Invalid light.\n");
							return	false;
						}
					}
					if(val <= 0.0f)
					{
						goto	Skip;
					}

					//This is the slowest test, so make it last
					if(dLight.mType == DirectLight.DLight_Sun)
					{
						mGFXRGBVerts[vn]	+=(dLight.mColor * val);
					}
					else
					{
						Vector3	colResult;
						if(RayCollide(vert, dLight.mOrigin, modelIndex, modelInv, out colResult))
						{
							goto	Skip;	//Ray is in shadow
						}
						mGFXRGBVerts[vn]	+=(dLight.mColor * val);
					}
					Skip:;				
				}
			}
			return	true;
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

			//sky clusters for sunlight lighting
			List<int>	skyClusters	=null;
			if(visData != null)
			{
				skyClusters	=FindSkyClusters(visData);
			}

			//need to build a data structure that has a model index per face
			Dictionary<int, int>	modelForFace	=new Dictionary<int, int>();
			for(int i=0;i < mGFXModels.Length;i++)
			{
				GFXModel	mod	=mGFXModels[i];

				int	ff	=mod.mFirstFace;
				int	nf	=mod.mNumFaces;

				for(int j=ff;j < (ff + nf);j++)
				{
					modelForFace.Add(j, i);
				}
			}

			//need a bunch of model transforms, just translation for now
			Dictionary<int, Matrix>	modelTransforms	=GetModelTransforms();

			//inverted transforms for raycasts
			Dictionary<int, Matrix>	modelInvs		=new Dictionary<int, Matrix>();
			foreach(KeyValuePair<int, Matrix> modelX in modelTransforms)
			{
				modelInvs.Add(modelX.Key, Matrix.Invert(modelX.Value));
			}

			object	prog	=ProgressWatcher.RegisterProgress(0, mGFXFaces.Length, 0);

			TSPool<bool []>	boolPool	=new TSPool<bool[]>(() => new bool[LInfo.MAX_LMAP_SIZE * LInfo.MAX_LMAP_SIZE]);

			if(lp.mLightParams.mbRecording)
			{
				//clear existing
				lp.mLightParams.mFacePlanes.Clear();
				lp.mLightParams.mFacePoints.Clear();
				lp.mLightParams.mFInfos.Clear();

				for(int i=0;i < mGFXFaces.Length;i++)
				{
					int		pnum	=mGFXFaces[i].mPlaneNum;
					bool	bFlip	=mGFXFaces[i].mbFlipSide;

					GFXPlane	pln	=new GFXPlane();

					pln.mNormal	=mGFXPlanes[pnum].mNormal;
					pln.mDist	=mGFXPlanes[pnum].mDist;
					pln.mType	=mGFXPlanes[pnum].mType;

					if(bFlip)
					{
						pln.Inverse();
					}
					lp.mLightParams.mFacePoints.Add(i, new List<Vector3>());
					lp.mLightParams.mFacePlanes.Add(i, pln);
				}
			}

			//avoid going nutso with threads
			ParallelOptions	po			=new ParallelOptions();
			po.MaxDegreeOfParallelism	=lp.mBSPParams.mMaxThreads;

			Parallel.For(0, mGFXFaces.Length, po, i =>
//			for(int i=0;i < mGFXFaces.Length;i++)
			{
				ProgressWatcher.UpdateProgressIncremental(prog);

				int		pnum	=mGFXFaces[i].mPlaneNum;
				bool	bFlip	=mGFXFaces[i].mbFlipSide;

				GFXPlane	pln	=new GFXPlane();

				pln.mNormal	=mGFXPlanes[pnum].mNormal;
				pln.mDist	=mGFXPlanes[pnum].mDist;
				pln.mType	=mGFXPlanes[pnum].mType;

				if(bFlip)
				{
					pln.Inverse();
				}

				mFaceInfos[i].SetPlane(pln);
				mFaceInfos[i].SetFaceIndex(i);

				GFXTexInfo	tex			=mGFXTexInfos[mGFXFaces[i].mTexInfo];
				GFXFace		face		=mGFXFaces[i];
				int			modelIndex	=modelForFace[i];
				Matrix		modelMat	=modelTransforms[modelIndex];
				Matrix		modelInv	=modelInvs[modelIndex];

				if(tex.IsGouraud() || tex.IsFlat())
				{
					if(!VertexShadeFace(i, vertNormals, modelMat, modelInv, modelIndex))
					{
						CoreEvents.Print("LightFaces:  VertexShadeFace failed...\n");
//						continue;
						return;
					}					
				}
				else if(tex.IsLightMapped())
				{
					if(!CalcFaceInfo(mFaceInfos[i], mLightMaps[i], lp.mLightParams.mLightGridSize))
					{
//						continue;
						return;
					}

					if(lp.mLightParams.mbRecording)
					{
						FInfo	dupeFI	=new FInfo(mFaceInfos[i]);
						lock(lp.mLightParams.mFInfos)
						{
							lp.mLightParams.mFInfos.Add(i, dupeFI);
						}
					}
			
					Int32	size	=mLightMaps[i].CalcSize();

					mFaceInfos[i].AllocPoints(size);

					for(int s=0;s < lp.mLightParams.mNumSamples;s++)
					{
						CalcFacePoints(modelMat, modelInv, modelIndex, mFaceInfos[i], mLightMaps[i],
							lp.mLightParams.mLightGridSize,
							mSampleOffsets[s], lp.mLightParams.mbSeamCorrection, boolPool);

						if(lp.mLightParams.mbRecording)
						{
							Vector3	[]fp	=mFaceInfos[i].GetPoints();

							foreach(Vector3 pos in fp)
							{
								lp.mLightParams.mFacePoints[i].Add(pos);
							}
						}

						if(visData != null && visData.Length != 0 && !lp.mBSPParams.mbBuildAsBModel)
						{
							if(!ApplyLightsToFace(mFaceInfos[i], mLightMaps[i],
								modelInv, modelIndex, skyClusters,
								1 / (float)lp.mLightParams.mNumSamples, visData))
							{
//								continue;
								return;
							}
						}
						else
						{
							if(!ApplyLightsToFaceNoVis(mFaceInfos[i], mLightMaps[i],
								modelInv, modelIndex,
								1 / (float)lp.mLightParams.mNumSamples, visData))
							{
//								continue;
								return;
							}
						}
					}				
				}
			});

			ProgressWatcher.Clear();

			return	true;
		}


		bool ApplyLightsToFace(FInfo faceInfo, LInfo lightInfo,
			Matrix modelInv, int modelIndex, List<int> skyClusters,
			float scale, byte []visData)
		{
			Vector3	norm	=faceInfo.GetPlaneNormal();

			Vector3	[]facePoints	=faceInfo.GetPoints();

			//grab out the sun entity if there is one
			DirectLight	sunLight	=null;
			foreach(DirectLight dl in mDirectLights)
			{
				if(dl.mType == DirectLight.DLight_Sun)
				{
					sunLight	=dl;
					break;
				}
			}

			//sunlight pass
			if(sunLight != null)
			{
				for(int v=0;v < facePoints.Length;v++)
				{
					Int32	nodeLandedIn	=FindNodeLandedIn(0, facePoints[v]);
					Int32	leaf			=-(nodeLandedIn + 1);

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

					//do sunlight first if needed
					//this vis check doesn't actually work
					//sometimes you might have no vis to a sky face
					//but still need to cast rays if fully darkened
					if(skyClusters.Contains(clust))
					{
						//Find the angle between the light, and the face normal
						Vector3	sunRay		=facePoints[v] + sunLight.mNormal * -SunRayDist;
						Vector3	normRay		=sunRay;
						normRay.Normalize();

						float	angle	=Vector3.Dot(normRay, norm);
						if(angle <= 0.001f)
						{
							continue;
						}

						Vector3	colResult	=Vector3.Zero;
						GFXFace	faceHit		=null;
						if(RayCollideToFace(facePoints[v], sunRay, modelIndex, modelInv, ref faceHit))
						{
							if(faceHit != null)
							{
								if(mGFXTexInfos[faceHit.mTexInfo].IsSky())
								{
									Int32	lightType	=sunLight.mLType;

									//If the data for this LType has not been allocated, allocate it now...
									lightInfo.AllocLightType(lightType, facePoints.Length);

									Vector3	[]rgb	=lightInfo.GetRGBLightData(lightType);
									if(rgb == null)
									{
										continue;	//max light styles on face?
									}

									rgb[v]	+=sunLight.mColor * (angle * scale * sunLight.mIntensity);
								}
							}
							else
							{
								CoreEvents.Print("Light ray hit with no face returned!\n");
							}
						}
						else
						{
							CoreEvents.Print("Sunlight ray miss!\n");
						}
					}
				}
			}

			//normal light pass
			for(int v=0;v < facePoints.Length;v++)
			{
				Int32	nodeLandedIn	=FindNodeLandedIn(0, facePoints[v]);
				Int32	leaf			=-(nodeLandedIn + 1);

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
					if((visData[mGFXClusters[clust].mVisOfs + (c >> 3)] & (1 << (c & 7))) == 0)
					{
						continue;
					}

					if(!mDirectClusterLights.ContainsKey(c))
					{
						continue;
					}

					foreach(DirectLight dLight in mDirectClusterLights[c])
					{
						if(dLight.mType == DirectLight.DLight_Sun)
						{
							continue;	//done above
						}

						float	intensity	=dLight.mIntensity;
					
						//Find the angle between the light, and the face normal
						Vector3	vect	=dLight.mOrigin - facePoints[v];
						float	dist	=vect.Length();
						if(dist == 0.0f)
						{
							continue;
						}
						vect.Normalize();

						float	angle	=Vector3.Dot(vect, norm);
						if(angle <= 0.001f)
						{
							continue;
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

								if(Angle2 < (1.0f - dLight.mCone))
								{
									continue;
								}

								val	=(intensity - dist) * angle;
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
							continue;
						}

						// This is the slowest test, so make it last
						Vector3	colResult;
						if(RayCollide(facePoints[v], dLight.mOrigin, modelIndex, modelInv, out colResult))
						{
							continue;	//Ray is in shadow
						}

						Int32	lightType	=dLight.mLType;

						//If the data for this LType has not been allocated, allocate it now...
						lightInfo.AllocLightType(lightType, facePoints.Length);

						Vector3	[]rgb	=lightInfo.GetRGBLightData(lightType);
						if(rgb == null)
						{
							continue;	//max light styles on face?
						}

						rgb[v]	+=dLight.mColor * (val * scale);

						Debug.Assert(!float.IsNaN(rgb[v].X));
						Debug.Assert(!float.IsNaN(rgb[v].Y));
						Debug.Assert(!float.IsNaN(rgb[v].Z));
					}
				}
			}

			return	true;
		}


		//use this when there is no vis information
		bool ApplyLightsToFaceNoVis(FInfo faceInfo, LInfo lightInfo,
			Matrix modelInv, int modelIndex,
			float scale, byte []visData)
		{
			Vector3	norm	=faceInfo.GetPlaneNormal();

			Vector3	[]facePoints	=faceInfo.GetPoints();

			//grab out the sun entity if there is one
			DirectLight	sunLight	=null;
			foreach(DirectLight dl in mDirectLights)
			{
				if(dl.mType == DirectLight.DLight_Sun)
				{
					sunLight	=dl;
					break;
				}
			}

			//sunlight pass
			for(int v=0;v < facePoints.Length;v++)
			{
				Int32	nodeLandedIn	=FindNodeLandedIn(0, facePoints[v]);
				Int32	leaf			=-(nodeLandedIn + 1);

				if(leaf < 0 || leaf >= mGFXLeafs.Length)
				{
					CoreEvents.Print("ApplyLightsToFace:  Invalid leaf num.\n");
					return	false;
				}

				//do sunlight first if needed
				if(sunLight != null)
				{
					//Find the angle between the light, and the face normal
					Vector3	sunRay	=facePoints[v] + sunLight.mNormal * -SunRayDist;
					Vector3	normRay	=sunRay;
					normRay.Normalize();

					float	angle	=Vector3.Dot(normRay, norm);
					if(angle <= 0.001f)
					{
						continue;
					}

					Vector3	colResult	=Vector3.Zero;
					GFXFace	faceHit		=null;
					if(RayCollideToFace(facePoints[v], sunRay, modelIndex, modelInv, ref faceHit))
					{
						if(faceHit != null)
						{
							if(mGFXTexInfos[faceHit.mTexInfo].IsSky())
							{
								Int32	lightType	=sunLight.mLType;

								//If the data for this LType has not been allocated, allocate it now...
								lightInfo.AllocLightType(lightType, facePoints.Length);

								Vector3	[]rgb	=lightInfo.GetRGBLightData(lightType);
								if(rgb == null)
								{
									continue;	//max light styles on face?
								}

								rgb[v]	+=sunLight.mColor * (angle * scale * sunLight.mIntensity);
							}
						}
						else
						{
							CoreEvents.Print("Light ray hit with no face returned!\n");
						}
					}
					else
					{
						CoreEvents.Print("Sunlight ray miss!\n");
					}
				}
			}

			//normal light pass
			for(int v=0;v < facePoints.Length;v++)
			{
				foreach(DirectLight dLight in mDirectLights)
				{
					if(dLight.mType == DirectLight.DLight_Sun)
					{
						continue;	//done above
					}

					float	intensity	=dLight.mIntensity;
					
					//Find the angle between the light, and the face normal
					Vector3	vect	=dLight.mOrigin - facePoints[v];
					float	dist	=vect.Length();
					if(dist == 0.0f)
					{
						continue;
					}
					vect.Normalize();

					float	angle	=Vector3.Dot(vect, norm);
					if(angle <= 0.001f)
					{
						continue;
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

							if(Angle2 < (1.0f - dLight.mCone))
							{
								continue;
							}

							val	=(intensity - dist) * angle;
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
						continue;
					}

					// This is the slowest test, so make it last
					Vector3	colResult;
					if(RayCollide(facePoints[v], dLight.mOrigin, modelIndex, modelInv, out colResult))
					{
						continue;	//Ray is in shadow
					}

					Int32	lightType	=dLight.mLType;

					//If the data for this LType has not been allocated, allocate it now...
					lightInfo.AllocLightType(lightType, facePoints.Length);

					Vector3	[]rgb	=lightInfo.GetRGBLightData(lightType);
					if(rgb == null)
					{
						continue;	//max light styles on face?
					}

					rgb[v]	+=dLight.mColor * (val * scale);

					Debug.Assert(!float.IsNaN(rgb[v].X));
					Debug.Assert(!float.IsNaN(rgb[v].Y));
					Debug.Assert(!float.IsNaN(rgb[v].Z));
				}
			}
			return	true;
		}


		void CalcFacePoints(Matrix modelMat, Matrix modelInv, int modelIndex,
			FInfo faceInfo, LInfo lightInfo, int lightGridSize,
			Vector2 UVOfs, bool bExtraLightCorrection,
			TSPool<bool []> boolPool)
		{
			faceInfo.CalcFacePoints(modelMat, modelInv, modelIndex, lightInfo, lightGridSize,
				UVOfs, bExtraLightCorrection, boolPool,
				IsPointInSolidSpace, RayCollide, CorrectLightPoint);
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
			mDirectLights.Clear();
			mDirectClusterLights.Clear();
		}


		//returns a list of the clusters that can possibly see sky
		List<int>	FindSkyClusters(byte []visData)
		{
			//need to mark clusters that contain sky portals
			List<int>	clustersHaveSky	=new List<int>();
			for(int i=0;i < mGFXLeafs.Length;i++)
			{
				GFXLeaf	leaf	=mGFXLeafs[i];

				bool	bSkyLeaf	=false;
				for(int j=0;j < leaf.mNumFaces;j++)
				{
					GFXFace	f	=mGFXFaces[mGFXLeafFaces[j + leaf.mFirstFace]];

					GFXTexInfo	ti	=mGFXTexInfos[f.mTexInfo];

					if(ti.IsSky())
					{
						bSkyLeaf	=true;
						break;
					}
				}

				if(!bSkyLeaf)
				{
					continue;
				}

				Int32	clust	=leaf.mCluster;
				if(clust < 0)
				{
					continue;
				}

				if(!clustersHaveSky.Contains(clust))
				{
					clustersHaveSky.Add(clust);
				}
			}

			List<int>	clustersCanSeeSky	=new List<int>();
			for(int clust=0;clust < mGFXClusters.Length;clust++)
			{
				for(int chs=0;chs < clustersHaveSky.Count;chs++)
				{
					int	c	=clustersHaveSky[chs];
					if((visData[mGFXClusters[clust].mVisOfs + (c >> 3)] & (1 << (c & 7))) == 0)
					{
						continue;
					}

					//can see!
					clustersCanSeeSky.Add(clust);
					break;
				}
			}

			return	clustersCanSeeSky;
		}


		List<byte> BuildLightMaps(ref int numRGBMaps, Vector3 minLight, float maxIntensity)
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
						Vector3	WorkRGB	=rgb[j];

						if(k == 0)
						{
							WorkRGB	+=minLight;
						}
						
						for(int l=0;l < 3;l++)
						{
							float	val	=WorkRGB[l];

							LData[LDataOfs]	=(byte)(Math.Min(val, maxIntensity));
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

			CoreEvents.Print("Light Data Size\t\t: " + LightOffset + "\n");

			return	lightData;
		}
	}
}