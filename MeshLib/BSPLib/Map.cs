using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.Diagnostics;
using Microsoft.Xna.Framework.Storage;


namespace BSPLib
{
	public class WorldLeaf
	{
		public Int32	VisFrame;
		public Int32	Parent;
	}

	public class GBSPHeader : IChunkable
	{
		public string	mTAG;
		public Int32	mVersion;
		public DateTime	mBSPTime;

		public void Write(BinaryWriter bw)
		{
			bw.Write(mTAG);
			bw.Write(mVersion);
			bw.Write(mBSPTime.ToBinary());
		}

		public void Read(BinaryReader br)
		{
			mTAG		=br.ReadString();
			mVersion	=br.ReadInt32();
			mBSPTime	=DateTime.FromBinary(br.ReadInt64());
		}
	}


	public class Map
	{
		List<MapEntity>	mEntities;

		Int32		CurrentLeaf;
		Int32		CurFrameStatic;
		Int32		[]ClusterVisFrame;
		WorldLeaf	[]LeafData;
		Int32		[]NodeParents;
		Int32		[]NodeVisFrame;

		//models
		internal List<GBSPModel>	mModels	=new List<GBSPModel>();

		//brushes
		List<MapBrush>	mMapBrushes		=new List<MapBrush>();
		List<GBSPBrush>	mGBSPBrushes	=new List<GBSPBrush>();

		//planes
		PlanePool	mPlanePool	=new PlanePool();

		//texinfos
		TexInfoPool	mTIPool	=new TexInfoPool();

		//texnames
		List<string>	mTexNames	=new List<string>();

		//list of bad brushes for debug draw
		public static List<MapBrush>	TroubleBrushes	=new List<MapBrush>();

		//gfx data
		GFXModel		[]mGFXModels;
		GFXNode			[]mGFXNodes;
		GFXBNode		[]mGFXBNodes;
		GFXLeaf			[]mGFXLeafs;
		GFXCluster		[]mGFXClusters;
		GFXArea			[]mGFXAreas;
		GFXAreaPortal	[]mGFXAreaPortals;
		GFXPortal		[]mGFXPortals;
		GFXPlane		[]mGFXPlanes;
		GFXFace			[]mGFXFaces;
		Int32			[]mGFXLeafFaces;
		GFXLeafSide		[]mGFXLeafSides;
		Vector3			[]mGFXVerts;
		Int32			[]mGFXVertIndexes;
		Vector3			[]mGFXRGBVerts;
		GFXTexInfo		[]mGFXTexInfos;
		MapEntity		[]mGFXEntities;
		byte			[]mGFXLightData;
		byte			[]mGFXVisData;

		//vis related stuff
		VISPortal	[]VisPortals;
		VISPortal	[]VisSortedPortals;
		VISLeaf		[]VisLeafs;
		Int32		NumVisLeafBytes, NumVisPortalBytes;

		//light related stuff
		Vector3							[]VertNormals;
		Dictionary<Int32, DirectLight>	DirectClusterLights	=new Dictionary<Int32, DirectLight>();
		List<DirectLight>				DirectLights		=new List<DirectLight>();
		LInfo							[]mLightMaps;
		FInfo							[]mFaceInfos;
		RADPatch						[]mFacePatches;
		RADPatch						[]mPatchList;
		Int32							NumPatches, NumReceivers;

		//area stuff
		List<GFXArea>		mAreas		=new List<GFXArea>();
		List<GFXAreaPortal>	mAreaPorts	=new List<GFXAreaPortal>();

		//build settings
		BSPBuildParams	mBSPParms;
		LightParams		mLightParams;
		VisParams		mVisParams;

		public event EventHandler	eCPUCoresInUseChanged;
		public event EventHandler	eNumMapFacesChanged;
		public event EventHandler	eNumDrawFacesChanged;
		public event EventHandler	eNumCollisionFacesChanged;
		public event EventHandler	eNumPortalsChanged;
		public event EventHandler	eProgressChanged;

		static public event EventHandler	ePrint;

		event EventHandler	eDrawCSGDone;
		event EventHandler	eCollisionCSGDone;


		#region Constructors
		public Map() { }


		//reads a text brush file
		public Map(string mapFileName)
		{
			mEntities	=new List<MapEntity>();

			int	numSolids	=0;
			int	numDetails	=0;
			int	numTotal	=0;

			if(File.Exists(mapFileName))
			{
				using(StreamReader sr = File.OpenText(mapFileName))
				{
					string	s	="";

					//see if this is a .map or a .vmf
					if(mapFileName.EndsWith(".map"))
					{
						while((s = sr.ReadLine()) != null)
						{
							s	=s.Trim();
							if(s == "{")
							{
								MapEntity	e	=new MapEntity();
								e.ReadFromMap(sr, mPlanePool, mTIPool, mEntities.Count);
								mEntities.Add(e);
							}
						}
					}
					else
					{
						while((s = sr.ReadLine()) != null)
						{
							s	=s.Trim();
							if(s == "entity")
							{
								MapEntity	e	=new MapEntity();
								e.ReadVMFEntBlock(sr, mEntities.Count, mPlanePool, mTIPool);
								mEntities.Add(e);
							}
							else if(s == "world")
							{
								MapEntity	e	=new MapEntity();
								e.ReadVMFWorldBlock(sr, mEntities.Count, mPlanePool, mTIPool);
								mEntities.Add(e);
							}
							else if(s == "cameras")
							{
								MapEntity.SkipVMFEditorBlock(sr);
							}
							else if(s == "cordon")
							{
								MapEntity.SkipVMFEditorBlock(sr);
							}
						}
					}
				}
			}

			foreach(MapEntity e in mEntities)
			{
				foreach(MapBrush mb in e.mBrushes)
				{
					if((mb.mContents & Contents.BSP_CONTENTS_DETAIL2) != 0)
					{
						numDetails++;
					}
					else if((mb.mContents & Contents.BSP_CONTENTS_SOLID2) != 0)
					{
						numSolids++;
					}
					numTotal++;
				}
			}

			InsertModelNumbers();

			Print("Brush file load complete\n");
			Print("" + numSolids + " solid brushes\n");
			Print("" + numDetails + " detail brushes\n");
			Print("" + numTotal + " total brushes\n");
		}
		#endregion


		#region Queries
		public void GetTriangles(Vector3 pos, List<Vector3> verts, List<UInt32> indexes, string drawChoice)
		{
			if(drawChoice == "Map Brushes")
			{
				foreach(MapEntity ent in mEntities)
				{
					if(ent.mBrushes.Count > 0)
					{
						foreach(MapBrush mb in ent.mBrushes)
						{
							mb.GetTriangles(verts, indexes, false);
						}
						break;
					}
				}
			}
			else if(drawChoice == "Trouble Brushes")
			{
				lock(TroubleBrushes)
				{
					foreach(MapBrush mb in TroubleBrushes)
					{
						mb.GetTriangles(verts, indexes, false);
					}
				}
			}
			else if(drawChoice == "Draw Brushes")
			{
				foreach(GBSPBrush b in mGBSPBrushes)
				{
					b.GetTriangles(verts, indexes, true);
				}
			}
			else if(drawChoice == "Collision Brushes")
			{
			}
			else if(drawChoice == "Draw Tree")
			{
				if(mGFXModels != null && mGFXModels.Length > 0)
				{
					int	root	=mGFXModels[0].mRootNode[0];

					VisWorld(root, pos);

					RenderBSPFrontBack_r2(root, pos, verts, indexes, true);
				}
				else
				{
					Print("No GFXModels to draw!\n");
				}
			}
			else if(drawChoice == "Collision Tree")
			{
			}
			else if(drawChoice == "Portals")
			{
			}
			else if(drawChoice == "Portal Tree")
			{
			}
		}


		public bool ClassifyPoint(Vector3 pnt)
		{
			return	false;
		}


		public Vector3 GetPlayerStartPos()
		{
			foreach(MapEntity e in mEntities)
			{
				if(e.mData.ContainsKey("classname"))
				{
					if(e.mData["classname"] != "info_player_start")
					{
						continue;
					}
				}
				else
				{
					continue;
				}

				Vector3	ret	=Vector3.Zero;
				if(e.GetOrigin(out ret))
				{
					return	ret;
				}
			}
			return	Vector3.Zero;
		}


		public Vector3 GetFirstLightPos()
		{
			foreach(MapEntity e in mEntities)
			{
				if(e == GetWorldSpawnEntity())
				{
					continue;
				}
				float dist;
				if(e.GetLightValue(out dist))
				{
					Vector3	ret;
					e.GetOrigin(out ret);
					return	ret;
				}
			}
			return	Vector3.Zero;
		}


		public MapEntity GetWorldSpawnEntity()
		{
			foreach(MapEntity e in mEntities)
			{
				if(e.mData.ContainsKey("classname"))
				{
					if(e.mData["classname"] == "worldspawn")
					{
						return	e;
					}
				}
			}
			return	null;
		}
		#endregion


		#region IO
		public void Write(string fileName)
		{
			FileStream	file	=UtilityLib.FileUtil.OpenTitleFile(fileName,
									FileMode.OpenOrCreate, FileAccess.Write);

			BinaryWriter	bw	=new BinaryWriter(file);

			bw.Write(mEntities.Count);

			//write all entities
			foreach(MapEntity e in mEntities)
			{
				e.Write(bw);
			}

			//write bsps

			//write brush lists
		}


		public void Read(string fileName)
		{
			FileStream	file	=UtilityLib.FileUtil.OpenTitleFile(fileName,
									FileMode.Open, FileAccess.Read);

			BinaryReader	br	=new BinaryReader(file);

			int	numEnts	=br.ReadInt32();

			mEntities	=new List<MapEntity>();
			for(int i=0;i < numEnts;i++)
			{
				MapEntity	e	=new MapEntity();
				e.Read(br);

				mEntities.Add(e);
			}
		}
		#endregion


		bool ProcessEntities()
		{
			int	index	=0;

			foreach(MapEntity me in mEntities)
			{
				if(me.mBrushes.Count == 0)
				{
					index++;
					continue;
				}

				GBSPModel	mod	=new GBSPModel();

				Vector3	org;
				me.GetOrigin(out org);

				mod.SetOrigin(org);

				if(index == 0)
				{
					if(!mod.ProcessWorldModel(me.mBrushes, mEntities,
						mPlanePool, mTIPool, mBSPParms.mbVerbose))
					{
						return	false;
					}
				}
				else
				{
					if(!mod.ProcessSubModel(me.mBrushes, mPlanePool,
						mTIPool, mBSPParms.mbEntityVerbose))
					{
						return	false;
					}
				}
				mModels.Add(mod);
				index++;
			}
			return	true;
		}


		public void SaveGBSPFile(string fileName, BSPBuildParams parms)
		{
			mBSPParms	=parms;

			ConvertGBSPToFile(fileName);

			Print("GBSP save complete\n");
		}


		internal void UpdateNumPortals(int numPortals)
		{
			if(eNumPortalsChanged != null)
			{
				eNumPortalsChanged(numPortals, null);
			}
		}


		static internal void Print(string str)
		{
			EventHandler	evt	=ePrint;
			if(evt != null)
			{
				if(str.EndsWith("\n"))
				{
					evt(str, null);
				}
				else
				{
					evt(str + "\n", null);
				}
			}
		}


		internal GBSPModel ModelForLeafNode(GBSPNode Node)
		{
			if(Node.IsLeaf())
			{
				Print("ModelForLeafNode:  Node not a leaf!\n");
				return	null;
			}

			int	entNum	=Node.GetOriginalEntityNum();
			if(entNum == -1)
			{
				return	null;
			}

			return	mModels[mEntities[entNum].mModelNum];
		}


		bool InsertModelNumbers()
		{
			Int32	i, NumModels	=0;

			for(i=0;i < mEntities.Count;i++)
			{
				if(mEntities[i].mBrushes.Count == 0)	//No model if no brushes
				{
					continue;
				}
				
				mEntities[i].mModelNum	=NumModels;

				if(i != 0)
				{
					mEntities[i].mData.Add("Model", "" + NumModels);
				}
				NumModels++;
			}
			return	true;
		}


		public bool BuildTree(BSPBuildParams prms)
		{
			mBSPParms	=prms;

			if(ProcessEntities())
			{
				Print("Build GBSP Complete\n");
				return	true;
			}
			else
			{
				Print("Compilation failed\n");
				return	false;
			}
		}


		public void RayCast3(Vector3 mStart, Vector3 mEnd, List<Ray> rayParts)
		{
		}


		bool FixModelTJunctions(FaceFixer ff)
		{
			Print(" --- Weld Model Verts --- \n");

			for(int i=0;i < mModels.Count;i++)
			{
				if(!mModels[i].GetFaceVertIndexNumbers(ff))
				{
					return	false;
				}
			}

			//Skip if asked to do so...
			if(!mBSPParms.mbFixTJunctions)
			{
				return	true;
			}


			Map.Print(" --- Fix Model TJunctions --- \n");

			for(int i=0;i < mModels.Count;i++)
			{
				if(!mModels[i].FixTJunctions(ff, mTIPool))
				{
					return false;
				}
			}

			if(mBSPParms.mbVerbose)
			{
				Print(" Num TJunctions        : " + ff.NumTJunctions + "\n");
				Print(" Num Fixed Faces       : " + ff.NumFixedFaces + "\n");
			}
			return true;
		}


		bool CreateAreas(GBSPModel worldModel, NodeCounter nc)
		{
			Print(" --- Create Area Leafs --- \n");

			//Clear all model area info
			foreach(GBSPModel mod in mModels)
			{
				mod.mAreas[0]		=mod.mAreas[1]	=0;
				mod.mbAreaPortal	=false;
			}

			int	numAreas	=1;

			if(!worldModel.CreateAreas(ref numAreas, ModelForLeafNode))
			{
				Map.Print("Could not create model areas.\n");
				return	false;
			}

			if(!worldModel.FinishAreaPortals(ModelForLeafNode))
			{
				Map.Print("CreateAreas: FinishAreaPortals_r failed.\n");
				return	false;
			}

			if(!FinishAreas(numAreas))
			{
				Map.Print("Could not finalize model areas.\n");
				return	false;
			}

			foreach(GBSPModel mod in mModels)
			{
				mod.PrepNodes(nc);
			}

			return	true;
		}
		
		
		bool PrepAllGBSPModels(string visFile, NodeCounter nc)
		{
			Int32	i;

			List<GFXLeafSide>	leafSides	=new List<GFXLeafSide>();
			for(i=0;i < mModels.Count;i++)
			{
				if(!mModels[i].PrepGBSPModel(visFile, i == 0,
					(i == 0)? mBSPParms.mbVerbose : mBSPParms.mbEntityVerbose,
					mPlanePool,
					ref nc.mNumLeafClusters,
					leafSides))
				{
					Map.Print("PrepAllGBSPModels:  Could not prep model " + i + "\n");
					return	false;
				}

				//create areas
				if(i == 0)
				{
					CreateAreas(mModels[i], nc);
				}
			}

			mGFXLeafSides	=leafSides.ToArray();

			return	true;
		}


		bool SaveGFXModelDataFromList(BinaryWriter bw)
		{
			if(mModels.Count <= 0)
			{
				return	true;
			}
			Int32		i;
			GBSPChunk	Chunk	=new GBSPChunk();
			GFXModel	GModel	=new GFXModel();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_MODELS;
			Chunk.mElements	=mModels.Count;

			Chunk.Write(bw);

			for(i=0;i < mModels.Count;i++)
			{
				mModels[i].ConvertToGFXAndSave(bw);
			}			
			return	true;	
		}


		bool SaveGFXModelDataNoGlobals(BinaryWriter bw)
		{
			if(mGFXModels == null || mGFXModels.Length <= 0)
			{
				return	true;
			}
			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_MODELS;
			Chunk.mElements	=mGFXModels.Length;

			Chunk.Write(bw);

			foreach(GFXModel gmod in mGFXModels)
			{
				gmod.Write(bw);
			}
			
			return	true;	
		}


		bool SaveGFXLeafSidesNoGlobals(BinaryWriter bw)
		{
			if(mGFXLeafSides.Length <= 0)
			{
				return	true;
			}
			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_LEAF_SIDES;
			Chunk.mElements =mGFXLeafSides.Length;

			if(!Chunk.Write(bw, mGFXLeafSides))
			{
				Print("There was an error writing the verts.\n");
				return	false;
			}
			return	true;
		}


		bool SaveGFXNodesNoGlobals(BinaryWriter bw, NodeCounter nc)
		{
			if(nc.mNumGFXNodes <= 0)
			{
				return	true;
			}
			Int32		i;
			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_NODES;
			Chunk.mElements	=nc.mNumGFXNodes;

			Chunk.Write(bw);
			
			for(i=0;i < mModels.Count; i++)
			{
				if(!mModels[i].SaveGFXNodes_r(bw))
				{
					return	false;
				}
			}
			return	true;
		}


		bool SaveGFXFacesNoGlobals(BinaryWriter bw, NodeCounter nc)
		{
			if(nc.mNumGFXFaces <= 0)
			{
				return	true;
			}
			Int32		i;
			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_FACES;
			Chunk.mElements =nc.mNumGFXFaces;

			Chunk.Write(bw);

			for(i=0;i < mModels.Count;i++)
			{
				if(!mModels[i].SaveGFXFaces_r(bw))
				{
					return	false;
				}
			}
			return	true;
		}


		bool SaveEmptyGFXClustersNoGlobals(BinaryWriter bw, NodeCounter nc)
		{
			if(nc.mNumLeafClusters <= 0)
			{
				return	true;
			}
			Int32		i;
			GBSPChunk	Chunk		=new GBSPChunk();
			GFXCluster	GCluster	=new GFXCluster();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_CLUSTERS;
			Chunk.mElements =nc.mNumLeafClusters;

			Chunk.Write(bw);

			for(i=0;i < nc.mNumLeafClusters;i++)
			{
				GCluster.mVisOfs	=-1;

				GCluster.Write(bw);
			}
			return	true;
		}


		internal bool ConvertGBSPToFile(string fileName)
		{
			FileStream	file	=UtilityLib.FileUtil.OpenTitleFile(fileName,
									FileMode.OpenOrCreate, FileAccess.Write);

			if(file == null)
			{
				Map.Print("ConvertGBSPToFile:  geVFile_OpenNewSystem failed.\n");
				return	false;
			}

			GBSPChunk	Chunk	=new GBSPChunk();

			string	VisFile	=fileName;

			FaceFixer	ff	=new FaceFixer();

			if(!FixModelTJunctions(ff))
			{
				Map.Print("ConvertGBSPToFile:  FixModelTJunctions failed.\n");
				return	false;
			}

			mGFXVerts		=ff.GetWeldedVertArray();

			NodeCounter	nc	=new NodeCounter();

			if(!PrepAllGBSPModels(VisFile, nc))
			{
				Print("ConvertGBSPToFile:  Could not prep models.\n");
				return	false;
			}

			mGFXVertIndexes	=nc.GetIndexArray();

			BinaryWriter	bw	=new BinaryWriter(file);

			GBSPHeader	header	=new GBSPHeader();
			header.mTAG			="GBSP";
			header.mVersion		=GBSPChunk.GBSP_VERSION;
			header.mBSPTime		=DateTime.Now;

			GBSPChunk	chunk	=new GBSPChunk();
			chunk.mType			=GBSPChunk.GBSP_CHUNK_HEADER;
			chunk.mElements		=1;
			chunk.Write(bw, header);

			//GHook.Printf("Saving GFX Model Data\n");
			if(!SaveGFXModelDataFromList(bw))
			{
				Map.Print("ConvertGBSPToFile:  SaveGFXModelData failed.\n");
				return	false;
			}
			if(!SaveGFXNodesNoGlobals(bw, nc))
			{
				return	false;
			}
			if(!SaveGFXLeafsNoGlobals(bw, nc))
			{
				Map.Print("ConvertGBSPToFile:  SaveGFXLeafs failed.\n");
				return	false;
			}
			if(!SaveEmptyGFXClustersNoGlobals(bw, nc))
			{
				return	false;
			}

			//set gfx area stuff from lists
			mGFXAreas		=mAreas.ToArray();
			mGFXAreaPortals	=mAreaPorts.ToArray();

			if(!SaveGFXAreasAndPortalsNoGlobals(bw))
			{
				return	false;
			}
			if(!SaveGFXLeafSidesNoGlobals(bw))
			{
				return	false;
			}
			if(!SaveGFXFacesNoGlobals(bw, nc))
			{
				return	false;
			}

			mGFXPlanes	=mPlanePool.GetGFXArray();

			if(!SaveVisdGFXPlanesNoGlobals(bw))
			{
				return	false;
			}
			if(!SaveGFXVertsNoGlobals(bw))
			{
				return	false;
			}
			if(!SaveGFXVertIndexListNoGlobals(bw))
			{
				return	false;
			}
			if(!SaveGFXTexInfos(bw))
			{
				return	false;
			}

			if(!SaveGFXEntDataList(bw))
			{
				return	false;
			}
			
			Chunk.mType		=GBSPChunk.GBSP_CHUNK_END;
			Chunk.mElements	=0;
			Chunk.Write(bw);

			bw.Close();
			file.Close();

			Map.Print(" --- Save GBSP File --- \n");
		 	
			Map.Print("Num Models           : " + mModels.Count + "\n");
			Map.Print("Num Nodes            : " + nc.mNumGFXNodes + "\n");
			Map.Print("Num Solid Leafs      : " + nc.mNumSolidLeafs + "\n");
			Map.Print("Num Total Leafs      : " + nc.mNumGFXLeafs + "\n");
			Map.Print("Num Clusters         : " + nc.mNumLeafClusters + "\n");
			Map.Print("Num Areas            : " + (mGFXAreas.Length - 1) + "\n");
			Map.Print("Num Area Portals     : " + mGFXAreaPortals.Length + "\n");
			Map.Print("Num Leafs Sides      : " + mGFXLeafSides.Length + "\n");
			Map.Print("Num Planes           : " + mPlanePool.mPlanes.Count + "\n");
			Map.Print("Num Faces            : " + nc.mNumGFXFaces + "\n");
			Map.Print("Num Leaf Faces       : " + nc.mNumGFXLeafFaces + "\n");
			Map.Print("Num Vert Index       : " + nc.VertIndexListCount + "\n");
			Map.Print("Num Verts            : " + mGFXVerts.Length + "\n");
			Map.Print("Num FaceInfo         : " + mTIPool.mTexInfos.Count + "\n");

			FreeGBSPFileNoGlobals();

			return	true;
		}


		bool SaveTextureNames(BinaryWriter bw)
		{
			if(mTexNames.Count <= 0)
			{
				return	true;
			}
			GBSPChunk	Chunk	=new GBSPChunk();;
			Chunk.mType			=GBSPChunk.GBSP_CHUNK_TEXTURES;
			Chunk.mElements		=mTexNames.Count;

			Chunk.Write(bw);

			foreach(string tex in mTexNames)
			{
				bw.Write(tex);
			}
			return	true;
		}


		bool LoadTextureNames(BinaryReader br)
		{
			Int32	cType	=br.ReadInt32();
			Debug.Assert(cType == GBSPChunk.GBSP_CHUNK_TEXTURES);

			Int32	numEl	=br.ReadInt32();
			for(int i=0;i < numEl;i++)
			{
				string	tex	=br.ReadString();
				mTexNames.Add(tex);
			}
			return	true;
		}


		bool SaveGFXTexInfos(BinaryWriter bw)
		{
			if(mTIPool.mTexInfos.Count <= 0)
			{
				return	true;
			}
			GBSPChunk	Chunk	=new GBSPChunk();;

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_TEXINFO;
			Chunk.mElements	=mTIPool.mTexInfos.Count;

			Chunk.Write(bw);

			foreach(TexInfo tex in mTIPool.mTexInfos)
			{
				GFXTexInfo	gtex	=new GFXTexInfo();

				gtex.mAlpha				=tex.mAlpha;
				gtex.mDrawScale[0]		=tex.mDrawScaleU;
				gtex.mDrawScale[1]		=tex.mDrawScaleV;
				gtex.mFaceLight			=tex.mFaceLight;
				gtex.mFlags				=tex.mFlags;
				gtex.mMipMapBias		=1.0f;	//is this right?
				gtex.mReflectiveScale	=tex.mReflectiveScale;
				gtex.mShift[0]			=tex.mShiftU;
				gtex.mShift[1]			=tex.mShiftV;
				gtex.mTexture			=-1;			//no texture stuff yet
				gtex.mVecs[0]			=tex.mUVec;
				gtex.mVecs[1]			=tex.mVVec;

				gtex.Write(bw);
			}
			return	true;
		}


		bool SaveVisdGFXTexInfosNoGlobals(BinaryWriter bw)
		{
			if(mGFXTexInfos == null || mGFXTexInfos.Length <= 0)
			{
				return	true;
			}
			GBSPChunk	Chunk	=new GBSPChunk();;

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_TEXINFO;
			Chunk.mElements	=mGFXTexInfos.Length;

			Chunk.Write(bw);

			foreach(GFXTexInfo tex in mGFXTexInfos)
			{
				tex.Write(bw);
			}
			return	true;
		}


		void FreeGBSPFileNoGlobals()
		{
			mGFXModels		=null;
			mGFXNodes		=null;
			mGFXBNodes		=null;
			mGFXLeafs		=null;
			mGFXClusters	=null;		// CHANGE: CLUSTER
			mGFXAreas		=null;
			mGFXPlanes		=null;
			mGFXFaces		=null;
			mGFXLeafFaces	=null;
			mGFXLeafSides	=null;
			mGFXVerts		=null;
			mGFXVertIndexes	=null;
			mGFXRGBVerts	=null;
			mGFXEntities	=null;			
			mGFXTexInfos	=null;
			mGFXLightData	=null;
			mGFXVisData		=null;
			mGFXPortals		=null;
		}


		bool SaveGFXEntDataList(BinaryWriter bw)
		{
			if(mEntities.Count <= 0)
			{
				return	true;
			}
			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_ENTDATA;
			Chunk.mElements =mEntities.Count;

			Chunk.Write(bw);

			foreach(MapEntity me in mEntities)
			{
				me.Write(bw);
			}
			return	true;
		}


		bool SaveGFXEntDataNoGlobals(BinaryWriter bw)
		{
			if(mGFXEntities == null || mGFXEntities.Length <= 0)			
			{
				return	true;
			}
			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_ENTDATA;
			Chunk.mElements =mGFXEntities.Length;

			Chunk.Write(bw);

			foreach(MapEntity me in mGFXEntities)
			{
				me.Write(bw);
			}
			return	true;
		}


		bool SaveGFXLightDataNoGlobals(BinaryWriter bw)
		{
			if(mGFXLightData == null || mGFXLightData.Length <= 0)
			{
				return	true;
			}
			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_LIGHTDATA;
			Chunk.mElements =mGFXLightData.Length;

			Chunk.Write(bw, mGFXLightData);
			return	true;
		}


		bool SaveGFXVertIndexListNoGlobals(BinaryWriter bw)
		{
			if(mGFXVertIndexes == null || mGFXVertIndexes.Length <= 0)
			{
				return	true;
			}
			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_VERT_INDEX;
			Chunk.mElements =mGFXVertIndexes.Length;

			if(!Chunk.Write(bw, mGFXVertIndexes))
			{
				Print("SaveGFXvertIndexList:  There was an error saving the VertIndexList.\n");
				return	false;
			}
			return	true;
		}


		bool SaveGFXVisDataNoGlobals(BinaryWriter bw)
		{
			if(mGFXVisData == null || mGFXVisData.Length <= 0)
			{
				return	true;
			}
			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_VISDATA;
			Chunk.mElements =mGFXVisData.Length;

			if(!Chunk.Write(bw, mGFXVisData))
			{
				Print("SaveGFXvertIndexList:  There was an error saving the VertIndexList.\n");
				return	false;
			}
			return	true;
		}


		bool SaveGFXVertsNoGlobals(BinaryWriter bw)
		{
			if(mGFXVerts == null || mGFXVerts.Length <= 0)
			{
				return	true;
			}
			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_VERTS;
			Chunk.mElements =mGFXVerts.Length;

			if(!Chunk.Write(bw, mGFXVerts))
			{
				Print("There was an error writing the verts.\n");
				return	false;
			}
			return	true;
		}


		bool SaveGFXRGBVertsNoGlobals(BinaryWriter bw)
		{
			if(mGFXRGBVerts == null || mGFXRGBVerts.Length <= 0)
			{
				return	true;
			}
			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_RGB_VERTS;
			Chunk.mElements =mGFXRGBVerts.Length;

			if(!Chunk.Write(bw, mGFXRGBVerts))
			{
				Print("There was an error writing the rgb verts.\n");
				return	false;
			}
			return	true;
		}


		bool SaveVisdGFXPlanesNoGlobals(BinaryWriter bw)
		{
			if(mGFXPlanes == null || mGFXPlanes.Length <= 0)
			{
				return	true;
			}
			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_PLANES;
			Chunk.mElements	=mGFXPlanes.Length;

			Chunk.Write(bw);

			foreach(GFXPlane gp in mGFXPlanes)
			{
				gp.Write(bw);
			}
			return	true;
		}


		bool SaveVisdGFXFacesNoGlobals(BinaryWriter bw)
		{
			if(mGFXFaces == null || mGFXFaces.Length <= 0)
			{
				return	true;
			}
			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_FACES;
			Chunk.mElements =mGFXFaces.Length;

			Chunk.Write(bw);

			foreach(GFXFace f in mGFXFaces)
			{
				f.Write(bw);
			}
			return	true;
		}


		bool SaveGFXPortalsNoGlobals(BinaryWriter bw)
		{
			if(mGFXPortals == null || mGFXPortals.Length <= 0)
			{
				return	true;
			}
			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_PORTALS;
			Chunk.mElements =mGFXPortals.Length;

			Chunk.Write(bw);

			foreach(GFXPortal port in mGFXPortals)
			{
				port.Write(bw);
			}
			return	true;
		}


		bool SaveGFXBNodesNoGlobals(BinaryWriter bw)
		{
			if(mGFXBNodes == null || mGFXBNodes.Length <= 0)
			{
				return	true;
			}
			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_BNODES;
			Chunk.mElements =mGFXBNodes.Length;

			Chunk.Write(bw);

			foreach(GFXBNode gbn in mGFXBNodes)
			{
				gbn.Write(bw);
			}
			return	true;
		}


		bool SaveGFXAreasAndPortalsNoGlobals(BinaryWriter bw)
		{
			GBSPChunk	Chunk	=new GBSPChunk();

			if(mGFXAreas != null && mGFXAreas.Length > 0)
			{
				//
				// Save the areas first
				//
				Chunk.mType		=GBSPChunk.GBSP_CHUNK_AREAS;
				Chunk.mElements =mGFXAreas.Length;

				Chunk.Write(bw, mGFXAreas);
			}

			if(mGFXAreaPortals != null && mGFXAreaPortals.Length > 0)
			{
				//
				//	Then, save the areaportals
				//
				Chunk.mType		=GBSPChunk.GBSP_CHUNK_AREA_PORTALS;
				Chunk.mElements =mGFXAreaPortals.Length;

				Chunk.Write(bw, mGFXAreaPortals);
			}
			return	true;
		}


		bool SaveGFXClustersNoGlobals(BinaryWriter bw)
		{
			if(mGFXClusters.Length <= 0)
			{
				return	true;
			}
			GBSPChunk	Chunk	=new GBSPChunk();
			Chunk.mType			=GBSPChunk.GBSP_CHUNK_CLUSTERS;
			Chunk.mElements		=mGFXClusters.Length;

			Chunk.Write(bw);

			foreach(GFXCluster clust in mGFXClusters)
			{
				clust.Write(bw);
			}
			return	true;
		}


		bool SaveVisdGFXClustersNoGlobals(BinaryWriter bw)
		{
			if(mGFXClusters == null || mGFXClusters.Length <= 0)
			{
				return	true;
			}
			GBSPChunk	Chunk		=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_CLUSTERS;
			Chunk.mElements =mGFXClusters.Length;

			Chunk.Write(bw);

			foreach(GFXCluster gc in mGFXClusters)
			{
				gc.Write(bw);
			}
			return	true;
		}


		bool SaveVisdGFXLeafsNoGlobals(BinaryWriter bw)
		{
			if(mGFXLeafs == null || mGFXLeafs.Length <= 0)
			{
				return	true;
			}
			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_LEAFS;
			Chunk.mElements	=mGFXLeafs.Length;

			Chunk.Write(bw);

			foreach(GFXLeaf leaf in mGFXLeafs)
			{
				leaf.Write(bw);
			}
			return	true;
		}


		bool SaveGFXLeafsNoGlobals(BinaryWriter bw, NodeCounter nc)
		{
			if(nc.mNumGFXLeafs <= 0)
			{
				return	true;
			}
			Int32		i;
			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_LEAFS;
			Chunk.mElements	=nc.mNumGFXLeafs;

			Chunk.Write(bw);

			int	TotalLeafSize	=0;

			List<Int32>	gfxLeafFaces	=new List<Int32>();

			for(i=0;i < mModels.Count;i++)
			{
				//Save all the leafs for this model
				if(!mModels[i].SaveGFXLeafs_r(bw, gfxLeafFaces, ref TotalLeafSize))
				{
					Map.Print("SaveGFXLeafs:  SaveGFXLeafs_r failed.\n");
					return	false;
				}
			}

			mGFXLeafFaces	=gfxLeafFaces.ToArray();

			//Save gfx leaf faces here...
			Chunk.mType		=GBSPChunk.GBSP_CHUNK_LEAF_FACES;
			Chunk.mElements =nc.mNumGFXLeafFaces;

			Chunk.Write(bw, mGFXLeafFaces);

			return	true;
		}
		
		
		bool SaveGFXLeafsNoGlobals(BinaryWriter bw)
		{
			if(mGFXLeafs.Length <= 0)
			{
				return	true;
			}
			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_LEAFS;
			Chunk.mElements	=mGFXLeafs.Length;

			Chunk.Write(bw);

			foreach(GFXLeaf leaf in mGFXLeafs)
			{
				leaf.Write(bw);
			}
			return	true;
		}


		bool SaveVisdGFXLeafFacesAndSidesNoGlobals(BinaryWriter bw)
		{
			GBSPChunk	Chunk	=new GBSPChunk();

			if(mGFXLeafFaces.Length > 0)
			{
				Chunk.mType		=GBSPChunk.GBSP_CHUNK_LEAF_FACES;
				Chunk.mElements	=mGFXLeafFaces.Length;
				Chunk.Write(bw, mGFXLeafFaces);
			}

			if(mGFXLeafSides.Length > 0)
			{
				Chunk.mType		=GBSPChunk.GBSP_CHUNK_LEAF_SIDES;
				Chunk.mElements	=mGFXLeafSides.Length;
				Chunk.Write(bw, mGFXLeafSides);
			}

			return	true;
		}


		bool SaveVisdGFXNodesNoGlobals(BinaryWriter bw)
		{
			if(mGFXNodes == null || mGFXNodes.Length <= 0)
			{
				return	true;
			}
			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_NODES;
			Chunk.mElements	=mGFXNodes.Length;

			Chunk.Write(bw);

			foreach(GFXNode gn in mGFXNodes)
			{
				gn.Write(bw);
			}
			return	true;
		}


		internal bool FinishAreas(int numAreas)
		{
			//First, go through and print out all errors pertaining to model areas
			for(int i=1;i < mModels.Count;i++)
			{
				if(!mModels[i].mbAreaPortal)
				{
					continue;
				}

				if(mModels[i].mAreas[0] == 0)
				{
					Print("*WARNING* FinishAreas:  AreaPortal did not touch any areas!\n");
				}
				else if(mModels[i].mAreas[1] == 0)
				{
					Print("*WARNING* FinishAreas:  AreaPortal only touched one area.\n");
				}
			}

			//Area 0 is the invalid area, set it here, and skip it in the loop below
			GFXArea	areaZero	=new GFXArea();
			areaZero.FirstAreaPortal	=0;
			areaZero.NumAreaPortals		=0;
			mAreas.Add(areaZero);
			
			for(int i=1;i < numAreas;i++)
			{
				GFXArea	area			=new GFXArea();
				area.FirstAreaPortal	=mAreas.Count;

				for(int m=1;m < mModels.Count;m++)
				{
					int	a0	=mModels[m].mAreas[0];
					int	a1	=mModels[m].mAreas[1];

					if(a0 == 0 || a1 == 0)
					{
						continue;
					}

					if(a0 == a1)
					{
						continue;
					}

					if(a0 != i && a1 != i)
					{
						continue;
					}

					if(mAreaPorts.Count >= GFXAreaPortal.MAX_AREA_PORTALS)
					{
						Print("FinishAreas:  Max area portals.\n");
						return	false;
					}

					GFXAreaPortal	p	=new GFXAreaPortal();

					//Grab the area on the opposite side of the portal
					if(a0 == i)
					{
						p.mArea =a1;
					}
					else if(a1 == i)
					{
						p.mArea	=a0;
					}
					p.mModelNum	=m;	//Set the portals model number

					mAreaPorts.Add(p);
				}

				area.NumAreaPortals	=mAreaPorts.Count - area.FirstAreaPortal;
			}
			return	true;
		}


		bool MakeVertNormalsNoGlobals()
		{
			VertNormals	=new Vector3[mGFXVerts.Length];

			if(VertNormals == null)
			{
				Print("MakeVertNormals:  Out of memory for normals.\n");
				return	false;
			}

			for(int i=0;i < mGFXFaces.Length;i++)
			{
				GFXFace	f	=mGFXFaces[i];

				Vector3	Normal	=mGFXPlanes[f.mPlaneNum].mNormal;

				if(f.mPlaneSide != 0)
				{
					Normal	=-Normal;
				}

				for(int v=0;v < f.mNumVerts;v++)
				{
					Int32	vn	=f.mFirstVert + v;

					Int32	Index	=mGFXVertIndexes[vn];

					VertNormals[Index]	=VertNormals[Index] + Normal;
				}
			}

			for(int i=0;i < mGFXVerts.Length;i++)
			{
				VertNormals[i].Normalize();
			}
			return	true;
		}


		bool CalcFaceInfoNoGlobals(FInfo FaceInfo, LInfo LightInfo)
		{
			Int32	i, k;
			Vector3	Vert;
			float	Val;
			float	[]Mins	=new float[2];
			float	[]Maxs	=new float[2];
			Int32	Face	=FaceInfo.Face;
			Vector3	TexNormal;
			float	DistScale;
			float	Dist, Len;
			Int32	indOffset;
			
			for (i=0; i<2; i++)
			{
				Mins[i]	=Bounds.MIN_MAX_BOUNDS;
				Maxs[i]	=-Bounds.MIN_MAX_BOUNDS;
			}

			Vector3	[]vecs	=new Vector3[2];

			GBSPPlane	pln;

			pln.mNormal	=FaceInfo.Plane.mNormal;
			pln.mDist	=FaceInfo.Plane.mDist;
			pln.mType	=FaceInfo.Plane.mType;

			GBSPPoly.TextureAxisFromPlane(pln, out vecs[0], out vecs[1]);

			FaceInfo.Center	=Vector3.Zero;

			indOffset	=mGFXFaces[Face].mFirstVert;

			for(i=0;i < mGFXFaces[Face].mNumVerts;i++, indOffset++)
			{
				int	vIndex	=mGFXVertIndexes[indOffset];
				Vert		=mGFXVerts[vIndex];
				for(k=0;k < 2;k++)
				{
					Val	=Vector3.Dot(Vert, vecs[k]);

					if(Val > Maxs[k])
					{
						Maxs[k]	=Val;
					}
					if (Val < Mins[k])
					{
						Mins[k]	=Val;
					}
				}

				//Find center
				FaceInfo.Center	+=Vert;
			}

			// Finish center
			FaceInfo.Center	/=mGFXFaces[Face].mNumVerts;

			// Get the Texture U/V mins/max, and Grid aligned lmap mins/max/size
			for(i=0;i < 2;i++)
			{
				LightInfo.Mins[i]	=Mins[i];
				LightInfo.Maxs[i]	=Maxs[i];

				Mins[i]	=(float)Math.Floor(Mins[i] / FInfo.LGRID_SIZE);
				Maxs[i]	=(float)Math.Ceiling(Maxs[i] / FInfo.LGRID_SIZE);

				LightInfo.LMins[i]	=(Int32)Mins[i];
				LightInfo.LMaxs[i]	=(Int32)Maxs[i];
				LightInfo.LSize[i]	=(Int32)(Maxs[i] - Mins[i]);

				if((LightInfo.LSize[i] + 1) > LInfo.MAX_LMAP_SIZE)
				//if (LightInfo.LSize[i] > 17)
				{
					Print("CalcFaceInfo:  Face was not subdivided correctly.\n");
					return	false;
				}
			}

			//Get the texture normal from the texture vecs
			TexNormal	=Vector3.Cross(vecs[0], vecs[1]);
			TexNormal.Normalize();
			
			//Flip it towards plane normal
			DistScale	=Vector3.Dot(TexNormal, FaceInfo.Plane.mNormal);
			if(DistScale == 0.0f)
			{
				Print("CalcFaceInfo:  Invalid Texture vectors for face.\n");
				return	false;
			}
			if(DistScale < 0)
			{
				DistScale	=-DistScale;
				TexNormal	=-TexNormal;
			}	

			DistScale	=1 / DistScale;

			//Get the tex to world vectors
			for(i=0;i < 2;i++)
			{
				Len		=vecs[i].Length();
				Dist	=Vector3.Dot(vecs[i], FaceInfo.Plane.mNormal);
				Dist	*=DistScale;

				FaceInfo.T2WVecs[i]	=vecs[i] + TexNormal * -Dist;
				FaceInfo.T2WVecs[i]	*=((1.0f / Len) * (1.0f / Len));
			}

			for(i=0;i < 3;i++)
			{
				UtilityLib.Mathery.VecIdxAssign(ref FaceInfo.TexOrg, i,
					-vecs[0].Z * UtilityLib.Mathery.VecIdx(FaceInfo.T2WVecs[0], i)
					-vecs[1].Z * UtilityLib.Mathery.VecIdx(FaceInfo.T2WVecs[1], i));
			}

			Dist	=Vector3.Dot(FaceInfo.TexOrg, FaceInfo.Plane.mNormal)
						- FaceInfo.Plane.mDist - 1;
			Dist	*=DistScale;
			FaceInfo.TexOrg	=FaceInfo.TexOrg + TexNormal * -Dist;

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
					Normal	=mFaceInfos[FaceNum].Plane.mNormal;
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
					if(RayCollisionNoGlobals(pVert, DLight.mOrigin, ref colResult))
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


		bool LightFacesNoGlobals(int numSamples, bool bExtraSamples)
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
				mFaceInfos[i].Plane.mNormal	=mGFXPlanes[pnum].mNormal;
				mFaceInfos[i].Plane.mDist	=mGFXPlanes[pnum].mDist;
				mFaceInfos[i].Plane.mType	=mGFXPlanes[pnum].mType;
				if(pside != 0)
				{
					mFaceInfos[i].Plane.Inverse();
				}				
				mFaceInfos[i].Face	=i;

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
								
//				if((tex.mFlags & TexInfo.TEXINFO_FLAT) != 0)
//				{
//					face.FlatShadeFace();
//					continue;
//				}
				
				//Faces with no lightmap don't need to light them 
				if((tex.mFlags & TexInfo.TEXINFO_NO_LIGHTMAP) != 0)
				{
					continue;
				}

				if(!CalcFaceInfoNoGlobals(mFaceInfos[i], mLightMaps[i]))
				{
					return	false;
				}
			
				Int32	Size	=(mLightMaps[i].LSize[0] + 1)
					* (mLightMaps[i].LSize[1] + 1);

				mFaceInfos[i].Points	=new Vector3[Size];

				if(mFaceInfos[i].Points == null)
				{
					Print("LightFaces:  Out of memory for face points.\n");
					return	false;
				}
				
				for(s=0;s < numSamples;s++)
				{
					//Hook.Printf("Sample  : %3i of %3i\n", s+1, NumSamples);
					CalcFacePointsNoGlobals(mFaceInfos[i], mLightMaps[i], UOfs[s], VOfs[s], bExtraSamples);

					if(!ApplyLightsToFaceNoGlobals(mFaceInfos[i], mLightMaps[i], 1 / (float)numSamples))
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
			Int32		i, k;
			RADPatch	Patch;
			
			//Only contribute light type 0 to patches
			if(mLightMaps[Face].RGBLData[0] == null)
			{
				return;
			}

			//Check each patch and see if the points lands in it's BBox
			for(Patch = mFacePatches[Face];Patch != null;Patch=Patch.mNext)
			{
				Vector3	[]pVert	=mFaceInfos[Face].Points;
				Vector3	[]pRGB	=mLightMaps[Face].RGBLData[0];	// Only contribute light type 0 to patches

				int	vertOfs	=0;
				int	rgbOfs	=0;

				Patch.mNumSamples	=0;
				//geVec3d_Clear(&Patch->RadStart);

				for(i=0;i < mFaceInfos[Face].NumPoints;i++)
				{
					for(k=0;k < 3;k++)
					{
						if(UtilityLib.Mathery.VecIdx(Patch.mBounds.mMins, k)
							> UtilityLib.Mathery.VecIdx(pVert[vertOfs], k) + 16)
						{
							break;
						}
						if(UtilityLib.Mathery.VecIdx(Patch.mBounds.mMaxs, k)
							< UtilityLib.Mathery.VecIdx(pVert[vertOfs], k) - 16)
						{
							break;
						}
					}

					if(k == 3)
					{
						//Add the Color to the patch 
						Patch.mNumSamples++;
						Patch.mRadStart	+=pRGB[rgbOfs];
					}
					rgbOfs++;
					vertOfs++;
				}				
				if(Patch.mNumSamples != 0)
				{
					Patch.mRadStart	*=(1.0f / (float)Patch.mNumSamples);
				}
			}
		}


		bool ApplyLightsToFaceNoGlobals(FInfo FaceInfo, LInfo LightInfo, float Scale)
		{
			Int32		c, v;
//			Vector3		*Verts;
			float		Dist;
			Int32		LType;
//			Vector3		*pRGBLData;
			Vector3		Normal, Vect;
			float		Val, Angle;
//			byte		*VisData;
			Int32		Leaf, Cluster;
			float		Intensity;
			DirectLight	DLight;

			Normal	=FaceInfo.Plane.mNormal;

//			Verts = FaceInfo.Points;

			for(v=0;v < FaceInfo.NumPoints;v++)
			{
				Int32	nodeLandedIn	=FindLeafLandedInNoGlobals(0, FaceInfo.Points[v]);
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
						Vect	=DLight.mOrigin - FaceInfo.Points[v];
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
						if(RayCollisionNoGlobals(FaceInfo.Points[v], DLight.mOrigin, ref colResult))
						{
							goto	Skip;	//Ray is in shadow
						}

						LType	=DLight.mLType;

						//If the data for this LType has not been allocated, allocate it now...
						if(LightInfo.RGBLData[LType] == null)
						{
							if(LightInfo.NumLTypes >= LInfo.MAX_LTYPES)
							{
								Map.Print("Max Light Types on face.\n");
								return	false;
							}
						
							LightInfo.RGBLData[LType]	=new Vector3[FaceInfo.NumPoints];
							LightInfo.NumLTypes++;
						}

						LightInfo.RGBLData[LType][v]	+=DLight.mColor * (Val * Scale);

						Skip:;
					}
				}
			}

			return	true;
		}


		void CalcFacePointsNoGlobals(FInfo FaceInfo, LInfo LightInfo, float UOfs, float VOfs, bool bExtraLightCorrection)
		{
			Vector3	FaceMid;
			float	MidU, MidV, StartU, StartV, CurU, CurV;
			Int32	u, v, Width, Height, Leaf;
			byte	[]InSolid	=new byte[LInfo.MAX_LMAP_SIZE * LInfo.MAX_LMAP_SIZE];

			MidU	=(LightInfo.Maxs[0] + LightInfo.Mins[0]) * 0.5f;
			MidV	=(LightInfo.Maxs[1] + LightInfo.Mins[1]) * 0.5f;

			FaceMid	=FaceInfo.TexOrg + FaceInfo.T2WVecs[0] * MidU
						+ FaceInfo.T2WVecs[1] * MidV;

			Width	=(LightInfo.LSize[0]) + 1;
			Height	=(LightInfo.LSize[1]) + 1;
			StartU	=((float)LightInfo.LMins[0]+UOfs) * (float)FInfo.LGRID_SIZE;
			StartV	=((float)LightInfo.LMins[1]+VOfs) * (float)FInfo.LGRID_SIZE;

			FaceInfo.NumPoints = Width*Height;

			for(v=0;v < Height;v++)
			{
				for(u=0;u < Width;u++)
				{
					CurU	=StartU + u * FInfo.LGRID_SIZE;
					CurV	=StartV + v * FInfo.LGRID_SIZE;

					FaceInfo.Points[(v * Width) + u]
						=FaceInfo.TexOrg + FaceInfo.T2WVecs[0] * CurU +
							FaceInfo.T2WVecs[1] * CurV;
					
					Int32	nodeLandedIn	=FindLeafLandedInNoGlobals(0, FaceInfo.Points[(v * Width) + u]);
					Leaf	=-(nodeLandedIn + 1);

					//Pre-compute if this point is in solid space, so we can re-use it in the code below
					if((mGFXLeafs[Leaf].mContents & Contents.BSP_CONTENTS_SOLID2) != 0)
					{
						InSolid[(v * Width) + u]	=1;
					}
					else
					{
						InSolid[(v * Width) + u]	=0;
					}

					if(!bExtraLightCorrection)
					{
						if(InSolid[(v * Width) + u] != 0)
						{
							Vector3	colResult	=Vector3.Zero;
							if(RayCollisionNoGlobals(FaceMid,
								FaceInfo.Points[(v * Width) + u], ref colResult))
							{
								Vector3	vect	=FaceMid - FaceInfo.Points[(v * Width) + u];
								vect.Normalize();
								FaceInfo.Points[(v * Width) + u]	=colResult + vect;
							}
						}
					}
				}
			}

			if(!bExtraLightCorrection)
			{
				return;
			}

//			pPoint = FaceInfo.Points;
//			pInSolid = InSolid;
			/*
			for (v=0; v< FaceInfo.NumPoints; v++, pPoint++, pInSolid++)
			{
				uint8		*pInSolid2;
				geVec3d		*pPoint2, *pBestPoint;
				geFloat		BestDist, Dist;

				if (!(*pInSolid))
					continue;						//  Point is good, leav it alone

				pPoint2 = FaceInfo.Points;
				pInSolid2 = InSolid;
				pBestPoint = &FaceMid;
				BestDist = Bounds.MIN_MAX_BOUNDS;
				
				for (u=0; u< FaceInfo.NumPoints; u++, pPoint2++, pInSolid2++)
				{
					if (pPoint == pPoint2)			
						continue;					// We know this point is bad

					if (*pInSolid2)
						continue;					// We know this point is bad

					// At this point, we have a good point, now see if it's closer than the current good point
					geVec3d_Subtract(pPoint2, pPoint, &Vect);
					Dist = geVec3d_Length(&Vect);
					if (Dist < BestDist)
					{
						BestDist = Dist;
						pBestPoint = pPoint2;

						if (Dist <= (GBSPGlobals.LGRID_SIZE-0.1f))
							break;					// This should be good enough...
					}
				}

				*pPoint = *pBestPoint;
			}*/
		}


		bool RayIntersectNoGlobals(Vector3 Front, Vector3 Back, Int32 Node, ref Vector3 intersectionPoint, ref bool hitLeaf)
		{
			float	Fd, Bd, Dist;
			Int32	Side;
			Vector3	I;

			if(Node < 0)						
			{
				Int32	Leaf	=-(Node+1);

				if((mGFXLeafs[Leaf].mContents
					& Contents.BSP_CONTENTS_SOLID2) != 0)
				{
					return	true;	//Ray collided with solid space
				}
				else 
				{
					return	false;	//Ray collided with empty space
				}
			}
			GFXNode		n	=mGFXNodes[Node];
			GFXPlane	p	=mGFXPlanes[n.mPlaneNum];

			Fd	=p.DistanceFast(Front);
			Bd	=p.DistanceFast(Back);

			if(Fd >= -1 && Bd >= -1)
			{
				return(RayIntersectNoGlobals(Front, Back, n.mChildren[0], ref intersectionPoint, ref hitLeaf));
			}
			if(Fd < 1 && Bd < 1)
			{
				return(RayIntersectNoGlobals(Front, Back, n.mChildren[1], ref intersectionPoint, ref hitLeaf));
			}

			Side	=(Fd < 0)? 1 : 0;
			Dist	=Fd / (Fd - Bd);

			I	=Front + Dist * (Back - Front);

			//Work our way to the front, from the back side.  As soon as there
			//is no more collisions, we can assume that we have the front portion of the
			//ray that is in empty space.  Once we find this, and see that the back half is in
			//solid space, then we found the front intersection point...
			if(RayIntersectNoGlobals(Front, I, n.mChildren[Side], ref intersectionPoint, ref hitLeaf))
			{
				return	true;
			}
			else if(RayIntersectNoGlobals(I, Back, n.mChildren[(Side == 0)? 1 : 0], ref intersectionPoint, ref hitLeaf))
			{
				if(!hitLeaf)
				{
					intersectionPoint	=I;
					hitLeaf				=true;
				}
				return	true;
			}
			return	false;
		}


		bool RayCollisionNoGlobals(Vector3 Front, Vector3 Back, ref Vector3 I)
		{
			bool	hitLeaf	=false;
			if(RayIntersectNoGlobals(Front, Back, mGFXModels[0].mRootNode[0], ref I, ref hitLeaf))
			{
				return	true;
			}
			return	false;
		}


		void FinalizeRGBVertsNoGlobals(Vector3 minLight, float maxLight)
		{
			for(int i=0;i < mGFXRGBVerts.Length;i++)
			{
				mGFXRGBVerts[i]	+=minLight;

				mGFXRGBVerts[i]	=Vector3.Clamp(mGFXRGBVerts[i],
					Vector3.Zero, Vector3.One * maxLight);
			}
		}


		void FreeDirectLightsNoGlobals()
		{
			DirectLights.Clear();
			DirectClusterLights.Clear();
		}


		bool SaveLightMapsNoGlobals(BinaryWriter f, ref int numRGBMaps)
		{
//			LInfo		*L;
			Int32		i, j, k,l, Size;
			float		Max, Max2;
			byte		[]LData	=new byte[LInfo.MAX_LMAP_SIZE * LInfo.MAX_LMAP_SIZE * 3 * 4];
//			byte		*pLData;
			long		Pos1, Pos2;
			Int32		NumLTypes;
//			FInfo		*pFaceInfo;
			Int32		LDataOfs	=0;

//			geVFile_Tell(f, &Pos1);
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
				mGFXFaces[i].mLWidth	=L.LSize[0] + 1;
				mGFXFaces[i].mLHeight	=L.LSize[1] + 1;
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
				Size	=mFaceInfos[i].NumPoints;

				Vector3	minLight	=mLightParams.mMinLight;

				//Create style 0, if min light is set, and style 0 does not exist
				if((L.RGBLData[0] == null) &&
					(minLight.X > 1
					|| minLight.Y > 1
					|| minLight.Z > 1))
				{
					L.RGBLData[0]	=new Vector3[Size];
					if(L.RGBLData[0] == null)
					{
						Print("SaveLightmaps:  Out of memory for lightmap.\n");
						return	false;
					}
					L.NumLTypes++;
					for(int ld=0;ld < L.RGBLData[0].Length;ld++)
					{
						L.RGBLData[0][ld]	=Vector3.Zero;
					}
				}
				
				//At this point, if no styles hit the face, skip it...
				if(L.NumLTypes == 0)
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
					if(L.RGBLData[k] == null)
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
//					pLData = LData;
//					geVec3d *pRGB = L.RGBLData[k];

					for(j=0;j < Size;j++)//, pRGB++)
					{
						Vector3	WorkRGB	=L.RGBLData[k][j] * mLightParams.mLightScale;

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

					L.RGBLData[k]	=null;
				}

				if(L.NumLTypes != NumLTypes)
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


		bool FinishWritingLightNoGlobals(BinaryWriter bw)
		{
			GBSPHeader	header	=new GBSPHeader();
			header.mTAG			="GBSP";
			header.mVersion		=GBSPChunk.GBSP_VERSION;
			header.mBSPTime		=DateTime.Now;

			GBSPChunk	chunk	=new GBSPChunk();
			chunk.mType			=GBSPChunk.GBSP_CHUNK_HEADER;
			chunk.mElements		=1;
			chunk.Write(bw, header);

			if(!SaveGFXRGBVertsNoGlobals(bw))
			{
				return	false;
			}
			if(!SaveVisdGFXFacesNoGlobals(bw))
			{
				return	false;
			}
			chunk.mType		=GBSPChunk.GBSP_CHUNK_END;
			chunk.mElements	=0;
			chunk.Write(bw);

			return	true;
		}


		bool StartWritingLightNoGlobals(BinaryWriter bw)
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

			if(!SaveGFXModelDataNoGlobals(bw))
			{
				return	false;
			}
			if(!SaveVisdGFXNodesNoGlobals(bw))
			{
				return	false;
			}
			if(!SaveGFXPortalsNoGlobals(bw))
			{
				return	false;
			}
			if(!SaveGFXBNodesNoGlobals(bw))
			{
				return	false;
			}
			if(!SaveGFXLeafsNoGlobals(bw))
			{
				return	false;
			}
			if(!SaveGFXAreasAndPortalsNoGlobals(bw))
			{
				return	false;
			}
			if(!SaveGFXClustersNoGlobals(bw))
			{
				return	false;
			}
			if(!SaveVisdGFXPlanesNoGlobals(bw))
			{
				return	false;
			}
			if(!SaveVisdGFXLeafFacesAndSidesNoGlobals(bw))
			{
				return	false;
			}
			if(!SaveGFXVertsNoGlobals(bw))
			{
				return	false;
			}
			if(!SaveGFXVertIndexListNoGlobals(bw))
			{
				return	false;
			}
			if(!SaveGFXEntDataNoGlobals(bw))
			{
				return	false;
			}
			if(!SaveVisdGFXTexInfosNoGlobals(bw))
			{
				return	false;
			}
			if(!SaveGFXVisDataNoGlobals(bw))
			{
				return	false;
			}
			return	true;
		}


		public bool VisGBSPFile(string fileName, VisParams prms, BSPBuildParams prms2)
		{
			Print(" --- Vis GBSP File --- \n");

			mVisParams	=prms;
			mBSPParms	=prms2;

			// Fill in the global bsp data
			if(!LoadGBSPFileNoGlobals(fileName))
			{
				Print("PvsGBSPFile:  Could not load GBSP file: " + fileName + "\n");
				return	false;
			}
			string	PFile;

			//Clean out any old vis data
			FreeFileVisData();

			//Open the bsp file for writing
			FileStream	fs	=UtilityLib.FileUtil.OpenTitleFile(fileName,
				FileMode.OpenOrCreate, FileAccess.Write);

			BinaryWriter	bw	=null;

			if(fs == null)
			{
				Print("VisGBSPFile:  Could not open GBSP file for writing: " + fileName + "\n");
				goto	ExitWithError;
			}

			bw	=new BinaryWriter(fs);

			// Prepare the portal file name
			int	extPos	=fileName.LastIndexOf(".");
			PFile		=fileName.Substring(0, extPos);
			PFile		+=".gpf";
			
			//Load the portal file
			if(!LoadPortalFileNoGlobals(PFile))
			{
				goto	ExitWithError;
			}

			Print("NumPortals           : " + VisPortals.Length + "\n");
			
			//Write out everything but vis info
			if(!StartWritingVisNoGlobals(bw))
			{
				goto	ExitWithError;
			}

			//Vis'em
			if(!VisAllLeafsNoGlobals())
			{
				goto	ExitWithError;
			}

			//Save the leafs, clusters, vis data, etc
			if(!FinishWritingVisNoGlobals(bw))
			{
				goto	ExitWithError;
			}

			//Free all the vis stuff
			FreeAllVisDataNoGlobals();

			//Free any remaining leftover bsp data
			FreeGBSPFileNoGlobals();

			bw.Close();
			fs.Close();
			bw	=null;
			fs	=null;
			
			return	true;

			// ==== ERROR ====
			ExitWithError:
			{
				Print("PvsGBSPFile:  Could not vis the file: " + fileName + "\n");

				if(bw != null)
				{
					bw.Close();
				}
				if(fs != null)
				{
					fs.Close();
				}

				FreeAllVisDataNoGlobals();
				FreeGBSPFileNoGlobals();

				return	false;
			}
		}


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

			Int32	nodeLandedIn	=FindLeafLandedInNoGlobals(0, Patch.mOrigin);
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
					bool	Created	=(mLightMaps[i].RGBLData[0] != null);

					int	rgbOfs	=0;				
					for(k=0;k < mFaceInfos[i].NumPoints;k++, rgbOfs++)
					{
						pPoint	=mFaceInfos[i].Points[k];
						if(!SampleTriangulation(pPoint, Tri, out Add))
						{
							Print("AbsorbPatches:  Could not sample from patch triangles.\n");
							continue;
						}

						if(!Created)
						{
							if(Add.X > 0 || Add.Y > 0 || Add.Z > 0)
							{
								if(mLightMaps[i].NumLTypes > LInfo.MAX_LTYPES)
								{
									Print("AbsorbPatches:  Too many Light Types on Face.\n");
									return	false;
								}

								mLightMaps[i].RGBLData[0]	=new Vector3[mFaceInfos[i].NumPoints];
								if(mLightMaps[i].RGBLData[0] == null)
								{
									Print("AbsorbPAtches:  Out of memory for lightmap.\n");
									return	false;
								}
								mLightMaps[i].NumLTypes++;
								Created	=true;
							}
						}
						if(Created)
						{
							mLightMaps[i].RGBLData[0][k]	+=Add;
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
				if(RayCollisionNoGlobals(Patch.mOrigin, Patch2.mOrigin, ref colResult))
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
			float		Megs;


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

//			Megs	=(float)NumReceivers * sizeof(RAD_Receiver) / (1024*1024);
//			GHook.Printf("Num Receivers        : %5i, Megs %2.2f\n", NumReceivers, Megs);

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
			
			if(!LoadGBSPFileNoGlobals(fileName))
			{
				Print("LightGBSPFile:  Could not load GBSP file: " + fileName + "\n");
				return	false;
			}
			
			//Allocate some RGBLight data now
			mGFXRGBVerts	=new Vector3[mGFXVertIndexes.Length];

			if(!MakeVertNormalsNoGlobals())
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

			if(!CreateDirectLightsNoGlobals())
			{
				Print("LightGBSPFile:  Could not create main lights.\n");
				goto	ExitWithError;
			}
			
			//Light faces, and apply to patches
			if(!LightFacesNoGlobals(5, false))	//Light all the faces lightmaps, and apply to patches
			{
				goto	ExitWithError;
			}

			FreeDirectLightsNoGlobals();

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

			FinalizeRGBVertsNoGlobals(lightParams.mMinLight, lightParams.mMaxIntensity);

			if(!StartWritingLightNoGlobals(bw))	//Open bsp file and save all current bsp data (except lightmaps)
			{
				goto	ExitWithError;
			}

			int	numRGBMaps	=0;

			if(!SaveLightMapsNoGlobals(bw, ref numRGBMaps))
			{
				goto	ExitWithError;
			}

			if(!FinishWritingLightNoGlobals(bw))	//Write the END chunk to the file
			{
				goto	ExitWithError;
			}

			bw.Close();
			file.Close();

			CleanupLightNoGlobals();

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
				CleanupLightNoGlobals();

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


		void CleanupLightNoGlobals()
		{
			FreeDirectLightsNoGlobals();
			FreePatches();
			FreeLightMapsNoGlobals();
			FreeReceivers();

			VertNormals	=null;

			FreeGBSPFileNoGlobals();
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


		void FreeLightMapsNoGlobals()
		{
			mLightMaps	=null;
			mFaceInfos	=null;
		}


		bool CreateDirectLightsNoGlobals()
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

				Int32	nodeLandedIn	=FindLeafLandedInNoGlobals(mGFXModels[0].mRootNode[0], DLight.mOrigin);
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


		bool IsCPPGenesis(BinaryReader br)
		{
			int	chunkType	=br.ReadInt32();
			int	size		=br.ReadInt32();
			int	elements	=br.ReadInt32();
			
			char	[]tag	=new char[5];
			tag[0]	=br.ReadChar();
			tag[1]	=br.ReadChar();
			tag[2]	=br.ReadChar();
			tag[3]	=br.ReadChar();
			tag[4]	=br.ReadChar();

			//go back to beginning of stream
			br.BaseStream.Seek(0, SeekOrigin.Begin);

			string	stag	=new string(tag);
			stag	=stag.Substring(0, 4);
			if(stag == "GBSP")
			{
				return	true;
			}
			return	false;
		}


		public bool LoadGBSPFileNoGlobals(string fileName)
		{
			FileStream	file	=UtilityLib.FileUtil.OpenTitleFile(fileName,
									FileMode.Open, FileAccess.Read);

			if(file == null)
			{
				return	false;
			}

			BinaryReader	br	=new BinaryReader(file);

			UInt32		LastGoodChunkType	=0;
			while(true)
			{
				GBSPChunk	chunk		=new GBSPChunk();
				UInt32		chunkType	=0;

				object	obj	=chunk.Read(br, out chunkType);
				if(obj == null)
				{
					Print("Chunk read failed.  Last good chunk type was " + LastGoodChunkType + "\n");
					br.Close();
					file.Close();
					return	false;
				}
				switch(chunkType)
				{
					case GBSPChunk.GBSP_CHUNK_HEADER:
					{
						GBSPHeader	head	=obj as GBSPHeader;
						if(head.mTAG != "GBSP")
						{
							br.Close();
							file.Close();
							return	false;
						}
						if(head.mVersion != GBSPChunk.GBSP_VERSION)
						{
							br.Close();
							file.Close();
							return	false;
						}
						break;
					}
					case GBSPChunk.GBSP_CHUNK_MODELS:
					{
						mGFXModels	=obj as GFXModel[];
						break;
					}
					case GBSPChunk.GBSP_CHUNK_NODES:
					{
						mGFXNodes	=obj as GFXNode[];
						break;
					}
					case GBSPChunk.GBSP_CHUNK_BNODES:
					{
						mGFXBNodes	=obj as GFXBNode[];
						break;
					}
					case GBSPChunk.GBSP_CHUNK_LEAFS:
					{
						mGFXLeafs	=obj as GFXLeaf[];
						break;
					}
					case GBSPChunk.GBSP_CHUNK_CLUSTERS:
					{
						mGFXClusters	=obj as GFXCluster[];
						break;
					}
					case GBSPChunk.GBSP_CHUNK_AREAS:
					{
						mGFXAreas	=obj as GFXArea[];
						break;
					}
					case GBSPChunk.GBSP_CHUNK_AREA_PORTALS:
					{
						mGFXAreaPortals	=obj as GFXAreaPortal[];
						break;
					}
					case GBSPChunk.GBSP_CHUNK_PORTALS:
					{
						mGFXPortals	=obj as GFXPortal[];
						break;
					}
					case GBSPChunk.GBSP_CHUNK_PLANES:
					{
						mGFXPlanes	=obj as GFXPlane[];
						break;
					}
					case GBSPChunk.GBSP_CHUNK_FACES:
					{
						mGFXFaces	=obj as GFXFace[];
						break;
					}
					case GBSPChunk.GBSP_CHUNK_LEAF_FACES:
					{
						mGFXLeafFaces	=obj as Int32[];
						break;
					}
					case GBSPChunk.GBSP_CHUNK_LEAF_SIDES:
					{
						mGFXLeafSides	=obj as GFXLeafSide[];
						break;
					}
					case GBSPChunk.GBSP_CHUNK_VERTS:
					{
						mGFXVerts	=obj as Vector3[];
						break;
					}
					case GBSPChunk.GBSP_CHUNK_VERT_INDEX:
					{
						mGFXVertIndexes	=obj as Int32[];
						break;
					}
					case GBSPChunk.GBSP_CHUNK_RGB_VERTS:
					{
						mGFXRGBVerts	=obj as Vector3[];
						break;
					}
					case GBSPChunk.GBSP_CHUNK_TEXINFO:
					{
						mGFXTexInfos	=obj as GFXTexInfo[];
						break;
					}
					case GBSPChunk.GBSP_CHUNK_ENTDATA:
					{
						mGFXEntities	=obj as MapEntity[];
						break;
					}
					case GBSPChunk.GBSP_CHUNK_LIGHTDATA:
					{
						mGFXLightData	=obj as byte[];
						break;
					}
					case GBSPChunk.GBSP_CHUNK_VISDATA:
					{
						mGFXVisData	=obj as byte[];
						break;
					}
					case GBSPChunk.GBSP_CHUNK_SKYDATA:
					{
						break;
					}
					case GBSPChunk.GBSP_CHUNK_END:
					{
						break;
					}
					default:
					{
						br.Close();
						file.Close();
						return	false;
					}
				}
				if(chunkType == GBSPChunk.GBSP_CHUNK_END)
				{
					break;
				}
				LastGoodChunkType	=chunkType;
			}

			br.Close();
			file.Close();

			//make clustervisframe
			ClusterVisFrame	=new int[mGFXClusters.Length];
			NodeParents		=new int[mGFXNodes.Length];
			NodeVisFrame	=new int[mGFXNodes.Length];
			LeafData		=new WorldLeaf[mGFXLeafs.Length];

			//fill in leafdata with blank worldleafs
			for(int i=0;i < mGFXLeafs.Length;i++)
			{
				LeafData[i]	=new WorldLeaf();
			}

			FindParents_r(mGFXModels[0].mRootNode[0], -1);

			Print("Load complete\n");

			return	true;
		}


		bool FinishWritingVisNoGlobals(BinaryWriter bw)
		{
			if(!SaveVisdGFXLeafsNoGlobals(bw))
			{
				return	false;
			}
			if(!SaveVisdGFXClustersNoGlobals(bw))
			{
				return	false;
			}
			if(!SaveGFXVisDataNoGlobals(bw))
			{
				return	false;
			}

			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_END;
			Chunk.mElements	=0;
			Chunk.Write(bw);

			return	true;
		}


		bool VisAllLeafsNoGlobals()
		{
			Int32	i;

			//Create PortalSeen array.  This is used by Vis flooding routines
			//This is deleted below...
			bool	[]PortalSeen	=new bool[VisPortals.Length];

			//create a dictionary to map a vis portal back to an index
			Dictionary<VISPortal, Int32>	portIndexer	=new Dictionary<VISPortal, Int32>();
			for(i=0;i < VisPortals.Length;i++)
			{
				portIndexer.Add(VisPortals[i], i);
			}

			//Flood all the leafs with the fast method first...
			for(i=0;i < VisLeafs.Length; i++)
			{
				FloodLeafPortalsFastNoGlobals(i, PortalSeen, portIndexer);
			}

			//Sort the portals with MightSee
			if(mVisParams.mbSortPortals)
			{
				SortPortalsNoGlobals();
			}

			if(mVisParams.mbFullVis)
			{
				if(!FloodPortalsSlow(portIndexer, PortalSeen))
				{
					return	false;
				}
			}

			//Don't need this anymore...
			PortalSeen	=null;

			mGFXVisData	=new byte[VisLeafs.Length * NumVisLeafBytes];
			if(mGFXVisData == null)
			{
				Print("VisAllLeafs:  Out of memory for LeafVisBits.\n");
				goto	ExitWithError;
			}

			int	TotalVisibleLeafs	=0;

			for(i=0;i < VisLeafs.Length;i++)
			{
				int	leafSee	=0;
				
				if(!CollectLeafVisBitsNoGlobals(i, ref leafSee))
				{
					goto	ExitWithError;
				}
				TotalVisibleLeafs	+=leafSee;
			}

			Print("Total visible areas           : " + TotalVisibleLeafs + "\n");
			Print("Average visible from each area: " + TotalVisibleLeafs / VisLeafs.Length + "\n");

			return	true;

			// ==== ERROR ====
			ExitWithError:
			{
				// Free all the global vis data
				FreeAllVisDataNoGlobals();

				return	false;
			}
		}


		bool FloodPortalsSlow(Dictionary<VISPortal, Int32> visIndexer, bool []PortalSeen)
		{
			VISPortal	Portal;
			VISPStack	PStack	=new VISPStack();
			Int32		i, k;

			for(k=0;k < VisPortals.Length;k++)
			{
				VisPortals[k].mDone	=false;
			}

			for(k=0;k < VisPortals.Length;k++)
			{
				Portal	=VisSortedPortals[k];
				
				Portal.mFinalVisBits	=new byte[NumVisPortalBytes];

				//This portal can't see anyone yet...
				for(i=0;i < NumVisPortalBytes;i++)
				{
					Portal.mFinalVisBits[i]	=0;
				}
				for(i=0;i < VisPortals.Length;i++)
				{
					PortalSeen[i]	=false;
				}

				int	CanSee	=0;
				
				for(i=0;i < NumVisPortalBytes;i++)
				{
					PStack.mVisBits[i]	=Portal.mVisBits[i];
				}

				//Setup Source/Pass
				PStack.mSource	=new GBSPPoly(Portal.mPoly);
				PStack.mPass	=null;

				if(!Portal.FloodPortalsSlow_r(Portal, PStack, visIndexer, ref CanSee, VisLeafs))
				{
					return	false;
				}

				PStack.mSource	=null;
				Portal.mDone	=true;

				if(mBSPParms.mbVerbose)
				{
					Print("Portal: " + (k + 1) + " - Fast Vis: "
						+ Portal.mMightSee + ", Full Vis: "
						+ Portal.mCanSee + "\n");
				}
			}			
			return	true;
		}


		bool CollectLeafVisBitsNoGlobals(int LeafNum, ref int leafSee)
		{
			VISPortal	Portal, SPortal;
			VISLeaf		Leaf;
			Int32		k, Bit, SLeaf, LeafBitsOfs;
			
			Leaf	=VisLeafs[LeafNum];

			LeafBitsOfs	=LeafNum * NumVisLeafBytes;

			byte	[]PortalBits	=new byte[NumVisPortalBytes];

			//'OR' all portals that this portal can see into one list
			for(Portal=Leaf.mPortals;Portal != null;Portal=Portal.mNext)
			{
				if(Portal.mFinalVisBits != null)
				{
					//Try to use final vis info first
					for(k=0;k < NumVisPortalBytes;k++)
					{
						PortalBits[k]	|=Portal.mFinalVisBits[k];
					}
				}
				else if(Portal.mVisBits != null)
				{
					for(k=0;k < NumVisPortalBytes;k++)
					{
						PortalBits[k]	|=Portal.mVisBits[k];
					}
				}
				else
				{
					Map.Print("No VisInfo for portal.\n");
					return	false;
				}

				Portal.mVisBits			=null;
				Portal.mFinalVisBits	=null;
			}

			// Take this list, and or all leafs that each visible portal looks in to
			for(k=0;k < VisPortals.Length;k++)
			{
				if((PortalBits[k >> 3] & (1 << (k & 7))) != 0)
				{
					SPortal	=VisPortals[k];
					SLeaf	=SPortal.mLeaf;
					Debug.Assert((1 << (SLeaf & 7)) < 256);
					mGFXVisData[LeafBitsOfs + (SLeaf >> 3)]	|=(byte)(1 << (SLeaf & 7));
				}
			}
					
			Bit	=1 << (LeafNum & 7);

			Debug.Assert(Bit < 256);

			//He should not have seen himself (yet...)
			if((mGFXVisData[LeafBitsOfs + (LeafNum >> 3)] & Bit) != 0)
			{
				Map.Print("*WARNING* CollectLeafVisBits:  Leaf:" + LeafNum + " can see himself!\n");
			}
			mGFXVisData[LeafBitsOfs + (LeafNum >> 3)]	|=(byte)Bit;

			for(k=0;k < VisLeafs.Length;k++)
			{
				Bit	=(1 << (k & 7));

				if((mGFXVisData[LeafBitsOfs + (k>>3)] & Bit) != 0)
				{
					leafSee++;
				}
			}

			if(leafSee == 0)
			{
				Map.Print("CollectLeafVisBits:  Leaf can see nothing.\n");
				return	false;
			}

			mGFXClusters[LeafNum].mVisOfs	=LeafBitsOfs;

			return	true;
		}


		void SortPortalsNoGlobals()
		{
			List<VISPortal>	sortMe	=new List<VISPortal>(VisPortals);

			sortMe.Sort(new VisPortalComparer());

			VisSortedPortals	=sortMe.ToArray();
		}


		void FloodLeafPortalsFastNoGlobals(int leafNum, bool []portSeen,
			Dictionary<VISPortal, Int32> visIndexer)
		{
			VISLeaf		Leaf;
			VISPortal	Portal;

			Leaf	=VisLeafs[leafNum];

			if(Leaf.mPortals == null)
			{
				//GHook.Printf("*WARNING* FloodLeafPortalsFast:  Leaf with no portals.\n");
				return;
			}
			
			int	srcLeaf	=leafNum;

			for(Portal=Leaf.mPortals;Portal != null;Portal=Portal.mNext)
			{
				Portal.mVisBits	=new byte[NumVisPortalBytes];

				//This portal can't see anyone yet...
				for(int i=0;i < NumVisPortalBytes;i++)
				{
					Portal.mVisBits[i]	=0;
				}
				for(int i=0;i < VisPortals.Length;i++)
				{
					portSeen[i]	=false;
				}

				int	mightSee	=0;
				
				Portal.FloodPortalsFastNoGlobals_r(Portal, visIndexer,
					portSeen, VisLeafs, srcLeaf, ref mightSee);
			}
		}


		bool StartWritingVisNoGlobals(BinaryWriter bw)
		{
			GBSPHeader	header	=new GBSPHeader();
			header.mTAG			="GBSP";
			header.mVersion		=GBSPChunk.GBSP_VERSION;
			header.mBSPTime		=DateTime.Now;

			GBSPChunk	chunk	=new GBSPChunk();
			chunk.mType			=GBSPChunk.GBSP_CHUNK_HEADER;
			chunk.mElements		=1;
			chunk.Write(bw, header);

			if(!SaveGFXModelDataNoGlobals(bw))
			{
				return	false;
			}
			if(!SaveVisdGFXNodesNoGlobals(bw))
			{
				return	false;
			}
			if(!SaveGFXPortalsNoGlobals(bw))
			{
				return	false;
			}
			if(!SaveGFXBNodesNoGlobals(bw))
			{
				return	false;
			}
			if(!SaveVisdGFXPlanesNoGlobals(bw))
			{
				return	false;
			}
			if(!SaveVisdGFXFacesNoGlobals(bw))
			{
				return	false;
			}
			if(!SaveGFXAreasAndPortalsNoGlobals(bw))
			{
				return	false;
			}
			if(!SaveVisdGFXLeafFacesAndSidesNoGlobals(bw))
			{
				return	false;
			}
			if(!SaveGFXVertsNoGlobals(bw))
			{
				return	false;
			}
			if(!SaveGFXVertIndexListNoGlobals(bw))
			{
				return	false;
			}
			if(!SaveGFXRGBVertsNoGlobals(bw))
			{
				return	false;
			}
			if(!SaveGFXEntDataNoGlobals(bw))
			{
				return	false;
			}
			if(!SaveVisdGFXTexInfosNoGlobals(bw))
			{
				return	false;
			}
			if(!SaveGFXLightDataNoGlobals(bw))
			{
				return	false;
			}
			return	true;
		}


		void FreeAllVisDataNoGlobals()
		{
			mGFXVisData		=null;

			if(VisPortals != null)
			{
				for(int i=0;i < VisPortals.Length;i++)
				{
					VisPortals[i].mPoly			=null;
					VisPortals[i].mFinalVisBits	=null;
					VisPortals[i].mVisBits		=null;
				}

				VisPortals	=null;
			}
			VisPortals			=null;
			VisSortedPortals	=null;
			VisLeafs			=null;

			FreeGBSPFileNoGlobals();	//Free rest of GBSP GFX data
		}


		bool LoadPortalFileNoGlobals(string PFile)
		{
			Int32		LeafFrom, LeafTo;
			VISPortal	pPortal;
			VISLeaf		pLeaf;
			GBSPPoly	pPoly;
			Int32		i, NumVerts;
			string		TAG;

			pPoly	=null;

			FileStream	fs	=UtilityLib.FileUtil.OpenTitleFile(PFile,
				FileMode.Open, FileAccess.Read);

			BinaryReader	br	=null;

			if(fs == null)		// opps
			{
				Print("LoadPortalFile:  Could not open " + PFile + " for reading.\n");
				goto	ExitWithError;
			}

			br	=new BinaryReader(fs);
			
			// 
			//	Check the TAG
			//
			TAG	=br.ReadString();
			if(TAG != "GBSP_PRTFILE")
			{
				Print("LoadPortalFile:  " + PFile + " is not a GBSP Portal file.\n");
				goto	ExitWithError;
			}

			//
			//	Get the number of portals
			//
			int	NumVisPortals	=br.ReadInt32();
			if(NumVisPortals >= VISPStack.MAX_TEMP_PORTALS)
			{
				Print("LoadPortalFile:  Max portals for temp buffers.\n");
				goto	ExitWithError;
			}
			
			VisPortals	=new VISPortal[NumVisPortals];
			if(VisPortals == null)
			{
				Print("LoadPortalFile:  Out of memory for VisPortals.\n");
				goto	ExitWithError;
			}
			
			VisSortedPortals	=new VISPortal[NumVisPortals];
			if(VisSortedPortals == null)
			{
				Print("LoadPortalFile:  Out of memory for VisSortedPortals.\n");
				goto ExitWithError;
			}

			//
			//	Get the number of leafs
			//
			int	NumVisLeafs	=br.ReadInt32();
			if(NumVisLeafs > mGFXLeafs.Length)
			{
				goto	ExitWithError;
			}
			
			VisLeafs	=new VISLeaf[NumVisLeafs];
			if(VisLeafs == null)
			{
				Print("LoadPortalFile:  Out of memory for VisLeafs.\n");
				goto ExitWithError;
			}

			//fill arrays with blank objects
			for(i=0;i < NumVisLeafs;i++)
			{
				VisLeafs[i]	=new VISLeaf();
			}

			//
			//	Load in the portals
			//
			for(i=0;i < NumVisPortals;i++)
			{
				//alloc blank portal
				VisPortals[i]	=new VISPortal();

				NumVerts	=br.ReadInt32();

				pPoly	=new GBSPPoly();

				for(int j=0;j < NumVerts;j++)
				{
					Vector3	vert;
					vert.X	=br.ReadSingle();
					vert.Y	=br.ReadSingle();
					vert.Z	=br.ReadSingle();

					pPoly.AddVert(vert);
				}

				LeafFrom	=br.ReadInt32();
				LeafTo		=br.ReadInt32();
				
				if(LeafFrom >= NumVisLeafs || LeafFrom < 0)
				{
					Print("LoadPortalFile:  Invalid LeafFrom: " + LeafFrom + "\n");
					goto	ExitWithError;
				}

				if(LeafTo >= NumVisLeafs || LeafTo < 0)
				{
					Print("LoadPortalFile:  Invalid LeafTo: " + LeafTo + "\n");
					goto	ExitWithError;
				}

				pLeaf	=VisLeafs[LeafFrom];
				pPortal	=VisPortals[i];

				pPortal.mPoly	=pPoly;
				pPortal.mLeaf	=LeafTo;
				pPortal.mPlane	=new GBSPPlane(pPoly);
				pPortal.mNext	=pLeaf.mPortals;
				pLeaf.mPortals	=pPortal;

				pPortal.CalcPortalInfo();
			}
			
			NumVisLeafBytes	=((NumVisLeafs+63)&~63) >> 3;
			NumVisPortalBytes	=((NumVisPortals+63)&~63) >> 3;

			br.Close();
			fs.Close();
			br	=null;
			fs	=null;

			return	true;

			// ==== ERROR ===
			ExitWithError:
			{
				if(br != null)
				{
					br.Close();
				}
				if(fs != null)
				{
					fs.Close();
				}

				VisPortals			=null;
				VisSortedPortals	=null;
				VisLeafs			=null;
				pPoly				=null;

				return	false;
			}
		}


		Int32	FindLeafLandedInNoGlobals(Int32 node, Vector3 pos)
		{
			float		Dist1;
			GFXNode		pNode;
			Int32		Side;

			if(node < 0)		// At leaf, no more recursing
			{
				return	node;
			}

			pNode	=mGFXNodes[node];
			
			//Get the distance that the eye is from this plane
			Dist1	=mGFXPlanes[pNode.mPlaneNum].DistanceFast(pos);

			if(Dist1 < 0)
			{
				Side	=1;
			}
			else
			{
				Side	=0;
			}
			
			//Go down the side we are on first, then the other side
			Int32	ret	=0;
			ret	=FindLeafLandedInNoGlobals(pNode.mChildren[Side], pos);
			if(ret < 0)
			{
				return	ret;
			}
			ret	=FindLeafLandedInNoGlobals(pNode.mChildren[(Side == 0)? 1 : 0], pos);
			return	ret;
		}


		bool VisWorld(Int32 rootNode, Vector3 pos)
		{
			Int32	i, Area;
			Int32	Leaf, Cluster;
			GFXLeaf	pLeaf;

			Int32	node	=FindLeafLandedInNoGlobals(rootNode, -pos);

			Leaf	=-(node + 1);
			Area	=mGFXLeafs[Leaf].mArea;

			CurrentLeaf	=Leaf;
			CurFrameStatic++;			// Make all old vis info obsolete

			Cluster	=mGFXLeafs[Leaf].mCluster;

			if(Cluster == -1 || mGFXClusters[Cluster].mVisOfs == -1)
			{
				return	true;
			}

			/*
			if (Area)
				Vis_FloodAreas_r(World, Area);

			World->VisInfo = GE_TRUE;
			*/

			//VisData = &GFXVisData[GFXClusters[Cluster].VisOfs];

			int	ofs	=mGFXClusters[Cluster].mVisOfs;

			// Mark all visible clusters
			for(i=0;i < mGFXModels[0].mNumClusters;i++)
			{
				if((mGFXVisData[ofs + (i >> 3)] & (1 << (i & 7))) != 0)
				{
					ClusterVisFrame[i]	=CurFrameStatic;
				}
			}

			//Go through and find all visible leafs based on the visible clusters the leafs are in
			for(i=0;i < mGFXModels[0].mNumLeafs;i++)
			{
				pLeaf	=mGFXLeafs[mGFXModels[0].mFirstLeaf + i];

				Cluster	=pLeaf.mCluster;

				if(Cluster == -1)	// No cluster info for this leaf (must be solid)
				{
					continue;
				}

				//If the cluster is not visible, then the leaf is not visible
				if(ClusterVisFrame[Cluster] != CurFrameStatic)
				{
					continue;
				}
				
				//If the area is not visible, then the leaf is not visible
//				if (World->CurrentBSP->AreaVisFrame[pLeaf->Area] != World->CurFrameStatic)
//					continue;

				//Mark all visible nodes by bubbling up the tree from the leaf
				MarkVisibleParents(i);

				//Mark the leafs vis frame to worlds current frame
				LeafData[i].VisFrame	=CurFrameStatic;
					
//				pFace = &GFXLeafFaces[pLeaf->FirstFace];

				// Go ahead and vis surfaces here...
//				for (k=0; k< pLeaf->NumFaces; k++)
//				{
					// Update each surface infos visframe thats touches each visible leaf
//					SurfInfo[*pFace++].VisFrame = World->CurFrameStatic;
//				}
			}
			return	true;
		}


		void MarkVisibleParents(Int32 Leaf)
		{
			Int32		Node;

			Debug.Assert(Leaf >= 0);
			Debug.Assert(Leaf < mGFXLeafs.Length);

			//Find the leafs parent
			Node	=LeafData[Leaf].Parent;

			// Bubble up the tree from the current node, marking them as visible
			while(Node >= 0)
			{
				NodeVisFrame[Node]	=CurFrameStatic;
				Node	=NodeParents[Node];
			}
		}


		void RenderBSPFrontBack_r2(Int32 Node, Vector3 pos,
			List<Vector3> verts, List<uint> indexes, bool bCheck)
		{
			float		Dist1;
			GFXNode		pNode;
			Int32		Side;

			if(Node < 0)		// At leaf, no more recursing
			{
				Int32		Leaf;

				Leaf	=-(Node+1);

				if(LeafData[Leaf].VisFrame != CurFrameStatic)
				{
					return;
				}

				Debug.Assert(Leaf >= 0 && Leaf < mGFXLeafs.Length);

				for(int i=0;i < mGFXLeafs[Leaf].mNumFaces;i++)
				{
					int		ofs		=verts.Count;
					UInt32	offset	=(UInt32)ofs;
					int		face	=mGFXLeafFaces[mGFXLeafs[Leaf].mFirstFace + i];
					int		nverts	=mGFXFaces[face].mNumVerts;
					int		fvert	=mGFXFaces[face].mFirstVert;

					for(int j=fvert;j < (fvert + nverts);j++)
					{
						int	idx	=mGFXVertIndexes[j];
						verts.Add(mGFXVerts[idx]);
					}

					int k	=0;
					for(k=1;k < nverts-1;k++)
					{
						//initial vertex
						indexes.Add(offset);
						indexes.Add((UInt32)(offset + k));
						indexes.Add((UInt32)(offset + ((k + 1) % nverts)));
					}
				}
				return;
			}

//			if(NodeVisFrame[Node] != CurFrameStatic)
//			{
//				return;
//			}

			pNode	=mGFXNodes[Node];
			
			//Get the distance that the eye is from this plane
			Dist1	=mGFXPlanes[pNode.mPlaneNum].DistanceFast(pos);

			if(Dist1 < 0)
			{
				Side	=1;
			}
			else
			{
				Side	=0;
			}
			
			//Go down the side we are on first, then the other side
			RenderBSPFrontBack_r2(pNode.mChildren[Side], pos, verts, indexes, bCheck);
			RenderBSPFrontBack_r2(pNode.mChildren[(Side == 0)? 1 : 0], pos, verts, indexes, bCheck);
		}


		void FindParents_r(Int32 Node, Int32 Parent)
		{
			if(Node < 0)		// At a leaf, mark leaf parent and return
			{
				LeafData[-(Node+1)].Parent	=Parent;
				return;
			}

			//At a node, mark node parent, and keep going till hitting a leaf
			NodeParents[Node]	=Parent;

			// Go down front and back markinf parents on the way down...
			FindParents_r(mGFXNodes[Node].mChildren[0], Node);
			FindParents_r(mGFXNodes[Node].mChildren[1], Node);
		}


		void MakeMaterials()
		{
			List<string>	texList	=new List<string>();
			foreach(GFXTexInfo ti in mGFXTexInfos)
			{
				MaterialLib.Material	mat	=new MaterialLib.Material();
				mat.Name	="" + ti.mTexture;
			}
		}


		void FreeFileVisData()
		{
			mGFXVisData		=null;
		}
	}
}
