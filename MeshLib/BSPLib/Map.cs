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
	public class GBSPGlobals
	{
		//GFX Stuff
		public GBSPHeader		GBSPHeader;				// Header
		public GFXSkyData		GFXSkyData;
		public GFXModel			[]GFXModels;			// Model data
		public GFXNode			[]GFXNodes;				// Nodes
		public GFXBNode			[]GFXBNodes;			// Bevel Clip Nodes
		public GFXLeaf			[]GFXLeafs;				// Leafs
		public GFXCluster		[]GFXClusters;			// CHANGE: CLUSTER
		public GFXArea			[]GFXAreas;			
		public GFXAreaPortal	[]GFXAreaPortals;
		public GFXPlane			[]GFXPlanes;			// Planes
		public GFXFace			[]GFXFaces;				// Faces
		public Int32			[]GFXLeafFaces;
		public GFXLeafSide		[]GFXLeafSides;
		public Vector3			[]GFXVerts;				// Verts
		public Int32			[]GFXVertIndexList;		// Index list
		public Vector3			[]GFXRGBVerts;
		public byte				[]GFXEntData;
		public GFXTexInfo		[]GFXTexInfo;			// TexInfo
		public byte				[]GFXLightData;			// Lightmap data
		public byte				[]GFXVisData;			// Vis data
		public GFXPortal		[]GFXPortals;			// Portal data

		//GFX counters
		public Int32		NumGFXModels;
		public Int32		NumGFXNodes;
		public Int32		NumGFXBNodes;
		public Int32		NumGFXLeafs;
		public Int32		NumGFXClusters;				// CHANGE: CLUSTER
		public Int32		NumGFXAreas; 
		public Int32		NumGFXAreaPortals;
		public Int32		NumGFXPlanes;
		public Int32		NumGFXFaces;
		public Int32		NumGFXLeafFaces;
		public Int32		NumGFXLeafSides;
		public Int32		NumGFXVerts;
		public Int32		NumGFXVertIndexList;		// For RGB verts, and regular verts
		public Int32		NumGFXRGBVerts;
		public Int32		NumGFXEntData;
		public Int32		NumGFXTexInfo;
		public Int32		NumGFXLightData;
		public Int32		NumGFXVisData;
		public Int32		NumGFXPortals;

		//GBSP Stuff
		public GBSPLeafSide	[]LeafSides		=new GBSPLeafSide[MAX_LEAF_SIDES];
		public Int32		TotalVisibleLeafs;
		public Int32		NumSolidBrushes;
		public Int32		NumCutBrushes;
		public Int32		NumHollowCutBrushes;
		public Int32		NumDetailBrushes;
		public Int32		NumTotalBrushes;
		public Int32		NumEntities;
		public Int32		NumSolidLeafs;
		public Int32		NumLeafClusters;
		public Vector3		[]VertNormals;
		public Int32		NumSubdivides;
		public float		SubdivideSize	=235.0f;
		public Int32		NumSubdivided;
		public Int32		NumMerged;
		public bool			gCountVerts;
		public Int32		gTotalVerts;
		public Int32		gPeekVerts;	
		public Vector3		NodeMins, NodeMaxs;
		public Vector3		TreeMins, TreeMaxs;
		public Int32		NumLeafSides;
		public Int32		NumLeafBevels;
		public Int32		NumMakeFaces	=0;
		public Int32		NumVisNodes;
		public Int32		NumNonVisNodes;
		public Int32		MergedNodes;
		public Vector3		EdgeStart, EdgeDir;
		public Int32		CNumLeafSides;
		public Int32		[]LPlaneNumbers	=new Int32[MAX_TEMP_LEAF_SIDES];
		public Int32		[]LPlaneSides	=new Int32[MAX_TEMP_LEAF_SIDES];
		public Int32		TotalLeafSize;

		//portal stuff
		public GBSPNode		OutsideNode;
		public Int32		NumPortals;
		public Int32		NumPortalLeafs;
		public Int32		CurrentFill;
		public bool			HitEntity		=false;
		public Int32		EntityHit		=0;
		public GBSPNode		HitNode;
		public Int32		NumRemovedLeafs;

		//vert stuff
		public Int32	NumTempIndexVerts;
		public Int32	NumWeldedVerts;
		public Int32	NumEdgeVerts;
		public Int32	TotalIndexVerts;
		public Int32	[]TempIndexVerts	=new Int32[MAX_TEMP_INDEX_VERTS];
		public Vector3	[]WeldedVerts		=new Vector3[MAX_WELDED_VERTS];
		public Int32	[]EdgeVerts			=new Int32[MAX_WELDED_VERTS];
		public Int32	NumFixedFaces;
		public Int32	NumTJunctions;

		//light stuff
		public RADPatch		[]FacePatches;
		public RADPatch		[]PatchList;
		public float		[]RecAmount;
		public Int32		NumPatches;
		public Int32		NumReceivers;
		public float		LightScale		=1.00f;
		public float		EntityScale		=1.00f;
		public float		MaxLight		=230.0f;
		public Int32		NumSamples		=5;
		public Vector3		MinLight;
		public Int32		NumLMaps;
		public Int32		LightOffset;
		public Int32		RGBMaps			=0;
		public Int32		REGMaps			=0;
		public LInfo		[]Lightmaps;
		public FInfo		[]FaceInfo;

		//vis stuff
		public Int32		CanSee;
		public Int32		SrcLeaf;
		public Int32		MightSee;
		public Int32		NumVisPortals;		// Total portals
		public Int32		NumVisPortalBytes;	// Total portals / 8
		public Int32		NumVisPortalLongs;	// Total portalbytes / sizeof(uint32)
		public VISPortal	[]VisPortals;		// NumVisPortals
		public byte			[]PortalSeen;		// Temp vis array
		public byte			[]PortalBits;
		public Int32		NumVisLeafs;		// Total VisLeafs
		public Int32		NumVisLeafBytes;	// NumVisLeaf / 8
		public Int32		NumVisLeafLongs;	// NumVisBytes / sizeof(uInt32)
		public byte			[]LeafVisBits;		// Should be NumVisLeafs * (NumVisLeafs / 8)
		public VISLeaf		[]VisLeafs;			// NumVisLeafs
		public bool			bVisPortals;

		//build flags
		public bool		FixTJuncts				=true;
		public bool		LVerbose				=true;
		public bool		DoRadiosity				=true;
		public float	PatchSize				=128.0f;
		public Int32	NumBounce				=8;
		public bool		FastPatch				=true;
		public bool		ExtraLightCorrection	=true;
		public float	ReflectiveScale			=1.0f;
		public bool		Verbose					=true;
		public bool		EntityVerbose			=false;
		public bool		VisVerbose				=false;
		public bool		NoSort					=false;
		public bool		FullVis					=true;
		public bool		OriginalVerbose;

		//constants
		public const int	MAX_BSP_MODELS			=2048;
		public const int	MAX_WELDED_VERTS		=64000;
		public const int	MAX_TEMP_INDEX_VERTS	=1024;
		public const int	MAX_PATCHES				=65000;
		public const int	MAX_LTYPE_INDEX			=12;
		public const int	MAX_LTYPES				=4;
		public const int	LGRID_SIZE				=16;
		public const int	MAX_LMAP_SIZE			=18;
		public const int	MAX_LEAF_SIDES			=64000 * 2;
		public const int	MAX_TEMP_LEAF_SIDES		=100;
		public const int	MAX_AREAS				=256;
		public const int	MAX_AREA_PORTALS		=1024;
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
		public GBSPGlobals	mGlobals	=new GBSPGlobals();

		List<MapEntity>	mEntities;

		bool		mbBuilding;
		int			mMaxCPUCores;
		bool		mbBevel;

		GBSPNode	mRoot;

		//models
		internal List<GBSPModel>	mModels	=new List<GBSPModel>();

		//brushes
		List<MapBrush>	mMapBrushes		=new List<MapBrush>();
		List<GBSPBrush>	mGBSPBrushes	=new List<GBSPBrush>();

		//planes
		PlanePool	mPlanePool	=new PlanePool();

		//texinfos
		TexInfoPool	mTIPool	=new TexInfoPool();

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

			mGlobals.NumLeafSides			=0;
			mGlobals.NumLeafClusters		=0;
			mGlobals.NumLeafBevels			=0;
			mGlobals.NumSolidBrushes		=0;
			mGlobals.NumCutBrushes			=0;
			mGlobals.NumHollowCutBrushes	=0;
			mGlobals.NumDetailBrushes		=0;
			mGlobals.NumTotalBrushes		=0;


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
								Brush.SkipVMFEditorBlock(sr);
							}
							else if(s == "cordon")
							{
								Brush.SkipVMFEditorBlock(sr);
							}
						}
					}
				}
			}

			foreach(MapEntity e in mEntities)
			{
				foreach(MapBrush mb in e.mBrushes)
				{
					if((mb.mContents & GBSPBrush.BSP_CONTENTS_DETAIL2) != 0)
					{
						mGlobals.NumDetailBrushes++;
					}
					else if((mb.mContents & GBSPBrush.BSP_CONTENTS_SOLID2) != 0)
					{
						mGlobals.NumSolidBrushes++;
					}
					mGlobals.NumTotalBrushes++;
				}
			}

			InsertModelNumbers();

			Print("Brush file load complete\n");
		}
		#endregion


		#region Queries
		public void GetTriangles(List<Vector3> verts, List<UInt32> indexes, string drawChoice)
		{
			if(drawChoice == "Map Brushes")
			{
				foreach(GBSPModel mod in mModels)
				{
					mod.GetTriangles(verts, indexes, true);
				}
			}
			else if(drawChoice == "Trouble Brushes")
			{
				lock(BspNode.TroubleBrushes)
				{
					foreach(Brush b in BspNode.TroubleBrushes)
					{
						b.SealFaces();
						b.GetTriangles(verts, indexes, false);
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
				mRoot.GetTriangles(verts, indexes, true);
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


		void ProcessEntities()
		{
			int	index	=0;

			bool	OldVerbose	=mGlobals.Verbose;

			foreach(MapEntity me in mEntities)
			{
				if(me.mBrushes.Count == 0)
				{
					index++;
					continue;
				}

				GBSPModel	mod	=new GBSPModel();

				me.GetOrigin(out mod.mOrigin);

				if(index == 0)
				{
					mod.ProcessWorldModel(mGlobals, me.mBrushes, mEntities, mPlanePool, mTIPool);
				}
				else
				{
					if(!mGlobals.EntityVerbose)
					{
						mGlobals.Verbose	=false;
					}

					mod.ProcessSubModel(mGlobals, me.mBrushes, mPlanePool, mTIPool);
				}
				mModels.Add(mod);
				index++;
			}
			mGlobals.Verbose	=OldVerbose;
		}


		public void SaveGBSPFile(string fileName)
		{
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
			GBSPBrush	Brush;

			if(Node.mPlaneNum != PlanePool.PLANENUM_LEAF)
			{
				Print("ModelForLeafNode:  Node not a leaf!\n");
				return	null;
			}

			for(Brush = Node.mBrushList;Brush != null;Brush = Brush.mNext)
			{
				if(Brush.mOriginal.mEntityNum != 0)
				{
					break;
				}
			}

			if(Brush == null)
			{
				return	null;
			}

			return	mModels[mEntities[Brush.mOriginal.mEntityNum].mModelNum];
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


		void BeginGBSPModels()
		{
			mGlobals.NumLeafClusters	=0;
			mGlobals.NumSolidLeafs		=0;
			mGlobals.NumLeafSides		=0;
			mGlobals.NumLeafBevels		=0;

			// Clear all gfx variables
			mGlobals.NumGFXPortals			=0;
			mGlobals.NumGFXNodes			=0;
			mGlobals.NumGFXBNodes			=0;
			mGlobals.NumGFXLeafs			=0;
			mGlobals.NumGFXFaces			=0;
			mGlobals.NumGFXVerts			=0;
			mGlobals.NumGFXVertIndexList	=0;
			mGlobals.NumGFXLeafFaces		=0;
		}


		public void BuildTree(BSPBuildParams prms)
		{
			BeginGBSPModels();

			mbBuilding				=true;
			mMaxCPUCores			=prms.mMaxCores;
			mGlobals.Verbose		=prms.mbVerbose;
			mGlobals.EntityVerbose	=prms.mbEntityVerbose;

			ProcessEntities();

			Print("Build GBSP Complete\n");
		}


		public bool MoveLine(ref Line ln)
		{
			return	false;
		}


		public bool MoveLine(ref Line ln, float radius)
		{
			return	false;
		}


		public bool RayCast(Vector3 p1, Vector3 p2, ref List<ClipSegment> segs)
		{
			return	false;
		}


		public void RayCast3(Vector3 mStart, Vector3 mEnd, List<Ray> rayParts)
		{
		}


		bool FixModelTJunctions()
		{
			Int32	i;

			Print(" --- Weld Model Verts --- \n");

			mGlobals.NumWeldedVerts		=0;
			mGlobals.TotalIndexVerts	=0;

			for(i=0;i < mModels.Count;i++)
			{
				if(!mModels[i].mRootNode[0].GetFaceVertIndexNumbers_r(mGlobals))
				{
					return	false;
				}
			}

			//Skip if asked to do so...
			if(!mGlobals.FixTJuncts)
			{
				return	true;
			}


			Map.Print(" --- Fix Model TJunctions --- \n");

			mGlobals.TotalIndexVerts	=0;
			mGlobals.NumTJunctions		=0;
			mGlobals.NumFixedFaces		=0;

			for (i=0;i < mModels.Count;i++)
			{
				if(!mModels[i].mRootNode[0].FixTJunctions_r(mGlobals, mTIPool))
				{
					return false;
				}
			}

			if(mGlobals.Verbose)
			{
				Print(" Num TJunctions        : " + mGlobals.NumTJunctions + "\n");
				Print(" Num Fixed Faces       : " + mGlobals.NumFixedFaces + "\n");
			}
			return true;
		}
		
		
		bool PrepAllGBSPModels(string visFile)
		{
			Int32	i;

			//restore verbose since bsp stage turns it off for entities
			mGlobals.Verbose	=mGlobals.OriginalVerbose;

			for(i=0;i < mModels.Count;i++)
			{
				//turn back off for entities if asked
				if(i > 0 && !mGlobals.EntityVerbose)
				{
					mGlobals.Verbose	=false;
				}

				if(!mModels[i].PrepGBSPModel(visFile, i == 0, mPlanePool, this))
				{
					Map.Print("PrepAllGBSPModels:  Could not prep model " + i + "\n");
					return	false;
				}
			}
			return	true;
		}


		bool SaveGFXModelData(BinaryWriter bw)
		{
			Int32		i;
			GBSPChunk	Chunk	=new GBSPChunk();
			GFXModel	GModel	=new GFXModel();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_MODELS;
			Chunk.mElements	=mModels.Count;

			Chunk.Write(bw);

			for(i=0;i < mModels.Count;i++)
			{
				GModel.mRootNode[0]		=mModels[i].mRootNodeID[0];
				GModel.mOrigin			=mModels[i].mOrigin;
				GModel.mMins			=mModels[i].mBounds.mMins;
				GModel.mMaxs			=mModels[i].mBounds.mMaxs;
				GModel.mRootNode[1]		=mModels[i].mRootNodeID[1];
				GModel.mFirstFace		=mModels[i].mFirstFace;
				GModel.mNumFaces		=mModels[i].mNumFaces;
				GModel.mFirstLeaf		=mModels[i].mFirstLeaf;
				GModel.mNumLeafs		=mModels[i].mNumLeafs;
				GModel.mFirstCluster	=mModels[i].mFirstCluster;
				GModel.mNumClusters		=mModels[i].mNumClusters;
				GModel.mAreas[0]		=mModels[i].mAreas[0];
				GModel.mAreas[1]		=mModels[i].mAreas[1];

				GModel.Write(bw);
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

			if(!FixModelTJunctions())
			{
				Map.Print("ConvertGBSPToFile:  FixModelTJunctions failed.\n");
				return	false;
			}

			mGlobals.GFXVertIndexList	=new Int32[mGlobals.TotalIndexVerts];
			mGlobals.NumGFXVerts		=mGlobals.NumWeldedVerts;
			mGlobals.GFXVerts			=mGlobals.WeldedVerts;

			if(!PrepAllGBSPModels(VisFile))
			{
				Print("ConvertGBSPToFile:  Could not prep models.\n");
				return	false;
			}

			BinaryWriter	bw	=new BinaryWriter(file);

			GBSPHeader	header	=new GBSPHeader();
			header.mTAG			="GBSP";
			header.mVersion		=GBSPChunk.GBSP_VERSION;
			header.mBSPTime		=DateTime.Now;

			GBSPChunk	chunk	=new GBSPChunk();
			chunk.mType			=GBSPChunk.GBSP_CHUNK_HEADER;
			chunk.mElements		=1;
			chunk.Write(bw, header);

			mGlobals.NumGFXLeafSides	=mGlobals.NumLeafSides;

			mGlobals.GFXLeafSides	=new GFXLeafSide[mGlobals.NumGFXLeafSides];

			for(int i=0;i < mGlobals.NumLeafSides;i++)
			{
				mGlobals.GFXLeafSides[i]	=new GFXLeafSide();
				mGlobals.GFXLeafSides[i].mPlaneNum	=mGlobals.LeafSides[i].mPlaneNum;
				mGlobals.GFXLeafSides[i].mPlaneSide	=mGlobals.LeafSides[i].mPlaneSide;
			}

			//GHook.Printf("Saving GFX Model Data\n");
			if(!SaveGFXModelData(bw))
			{
				Map.Print("ConvertGBSPToFile:  SaveGFXModelData failed.\n");
				return	false;
			}
			if(!SaveGFXNodes(bw))
			{
				return	false;
			}
			if(!SaveGFXLeafs(bw))
			{
				Map.Print("ConvertGBSPToFile:  SaveGFXLeafs failed.\n");
				return	false;
			}
			if(!SaveGFXClusters(bw))
			{
				return	false;
			}
			if(!SaveGFXAreasAndPortals(bw))
			{
				return	false;
			}
			if(!SaveGFXLeafSides(bw))
			{
				return	false;
			}
			if(!SaveGFXFaces(bw))
			{
				return	false;
			}
			if(!SaveGFXPlanes(bw))
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
			if(!SaveGFXTexInfos(bw))
			{
				return	false;
			}
			if(!SaveGFXEntData(bw))
			{
				return	false;
			}

			//do save texinfo
			
			Chunk.mType		=GBSPChunk.GBSP_CHUNK_END;
			Chunk.mElements	=0;
			Chunk.Write(bw);

			bw.Close();

			Map.Print(" --- Save GBSP File --- \n");
		 	
			Map.Print("Num Models           : " + mModels.Count + "\n");
			Map.Print("Num Nodes            : " + mGlobals.NumGFXNodes + "\n");
			Map.Print("Num Solid Leafs      : " + mGlobals.NumSolidLeafs + "\n");
			Map.Print("Num Total Leafs      : " + mGlobals.NumGFXLeafs + "\n");
			Map.Print("Num Clusters         : " + mGlobals.NumGFXClusters + "\n");
			Map.Print("Num Areas            : " + (mGlobals.NumGFXAreas - 1) + "\n");
			Map.Print("Num Area Portals     : " + mGlobals.NumGFXAreaPortals + "\n");
			Map.Print("Num Leafs Sides      : " + mGlobals.NumGFXLeafSides + "\n");
			Map.Print("Num Planes           : " + mPlanePool.mPlanes.Count + "\n");
			Map.Print("Num Faces            : " + mGlobals.NumGFXFaces + "\n");
			Map.Print("Num Leaf Faces       : " + mGlobals.NumGFXLeafFaces + "\n");
			Map.Print("Num Vert Index       : " + mGlobals.NumGFXVertIndexList + "\n");
			Map.Print("Num Verts            : " + mGlobals.NumGFXVerts + "\n");
			Map.Print("Num FaceInfo         : " + mGlobals.NumGFXTexInfo + "\n");

			mGlobals.GFXVertIndexList		=null;
			mGlobals.NumGFXVertIndexList	=0;
			mGlobals.GFXVerts				=null;
			mGlobals.NumGFXEntData			=0;
			
			//Reset these back, since we did not actually create them, we just
			//pointed them to the GBSP_LeafSide structure version (same structure size/type)
			mGlobals.GFXLeafSides		=null;
			mGlobals.NumGFXLeafSides	=0;

			FreeGBSPFile();

			return	true;
		}


		private bool SaveGFXTexInfos(BinaryWriter bw)
		{
			GBSPChunk	Chunk	=new GBSPChunk();;

			//save tex info
			mGlobals.NumGFXTexInfo	=mTIPool.mTexInfos.Count;

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_TEXINFO;
			Chunk.mElements	=mGlobals.NumGFXTexInfo;

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


		void FreeGBSPFile()
		{
			mGlobals.GFXModels			=null;
			mGlobals.GFXNodes			=null;
			mGlobals.GFXBNodes			=null;
			mGlobals.GFXLeafs			=null;
			mGlobals.GFXClusters		=null;		// CHANGE: CLUSTER
			mGlobals.GFXAreas			=null;
			mGlobals.GFXPlanes			=null;
			mGlobals.GFXFaces			=null;
			mGlobals.GFXLeafFaces		=null;
			mGlobals.GFXLeafSides		=null;
			mGlobals.GFXVerts			=null;
			mGlobals.GFXVertIndexList	=null;
			mGlobals.GFXRGBVerts		=null;
			mGlobals.GFXEntData			=null;			
			mGlobals.GFXTexInfo			=null;
			mGlobals.GFXLightData		=null;
			mGlobals.GFXVisData			=null;
			mGlobals.GFXPortals			=null;

			mGlobals.NumGFXModels			=0;
			mGlobals.NumGFXNodes			=0;
			mGlobals.NumGFXBNodes			=0;
			mGlobals.NumGFXLeafs			=0;
			mGlobals.NumGFXClusters			=0;		// CHANGE: CLUSTER
			mGlobals.NumGFXAreas			=0;
			mGlobals.NumGFXPlanes			=0;
			mGlobals.NumGFXFaces			=0;
			mGlobals.NumGFXLeafFaces		=0;
			mGlobals.NumGFXLeafSides		=0;
			mGlobals.NumGFXVerts			=0;
			mGlobals.NumGFXVertIndexList	=0;
			mGlobals.NumGFXRGBVerts			=0;

			mGlobals.NumGFXEntData		=0;
			mGlobals.NumGFXTexInfo		=0;
			mGlobals.NumGFXLightData	=0;
			mGlobals.NumGFXVisData		=0;
			mGlobals.NumGFXPortals		=0;
		}


		bool SaveGFXEntData(BinaryWriter bw)
		{
			foreach(MapEntity me in mEntities)
			{
				me.Write(bw);
			}
			return	true;
		}


		bool SaveGFXVertIndexList(BinaryWriter bw)
		{
			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_VERT_INDEX;
			Chunk.mElements =mGlobals.NumGFXVertIndexList;

			if(!Chunk.Write(bw, mGlobals.GFXVertIndexList))
			{
				Print("SaveGFXvertIndexList:  There was an error saving the VertIndexList.\n");
				return	false;
			}
			return	true;
		}

		private bool SaveGFXVerts(BinaryWriter bw)
		{
			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_VERTS;
			Chunk.mElements =mGlobals.NumGFXVerts;

			if(!Chunk.Write(bw, mGlobals.GFXVerts))
			{
				Print("There was an error writing the verts.\n");
				return	false;
			}
			return	true;
		}

		private bool SaveGFXPlanes(BinaryWriter bw)
		{
			Int32		i;
			GBSPChunk	Chunk	=new GBSPChunk();
			GFXPlane	GPlane	=new GFXPlane();

			mGlobals.NumGFXPlanes	=mPlanePool.mPlanes.Count;

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_PLANES;
			Chunk.mElements	=mGlobals.NumGFXPlanes;

			Chunk.Write(bw);

			for(i=0;i < mGlobals.NumGFXPlanes;i++)
			{
				GPlane.mNormal	=mPlanePool.mPlanes[i].mNormal;
				GPlane.mDist	=mPlanePool.mPlanes[i].mDist;
				GPlane.mType	=mPlanePool.mPlanes[i].mType;

				GPlane.Write(bw);
			}
			return	true;
		}


		bool SaveGFXFaces(BinaryWriter bw)
		{
			Int32		i;
			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_FACES;
			Chunk.mElements =mGlobals.NumGFXFaces;

			Chunk.Write(bw);

			for(i=0;i < mModels.Count;i++)
			{
				if(!mModels[i].mRootNode[0].SaveGFXFaces_r(bw))
				{
					return	false;
				}
			}
			return	true;
		}


		bool SaveGFXLeafSides(BinaryWriter bw)
		{
			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_LEAF_SIDES;
			Chunk.mElements =mGlobals.NumGFXLeafSides;

			if(!Chunk.Write(bw, mGlobals.GFXLeafSides))
			{
				Print("There was an error writing the verts.\n");
				return	false;
			}
			return	true;
		}


		bool SaveGFXAreasAndPortals(BinaryWriter bw)
		{
			GBSPChunk	Chunk	=new GBSPChunk();

			//
			// Save the areas first
			//
			Chunk.mType		=GBSPChunk.GBSP_CHUNK_AREAS;
			Chunk.mElements =mGlobals.NumGFXAreas;

			Chunk.Write(bw, mGlobals.GFXAreas);

			//
			//	Then, save the areaportals
			//
			Chunk.mType		=GBSPChunk.GBSP_CHUNK_AREA_PORTALS;
			Chunk.mElements =mGlobals.NumGFXAreaPortals;

			Chunk.Write(bw, mGlobals.GFXAreaPortals);

			return	true;
		}


		bool SaveGFXClusters(BinaryWriter bw)
		{
			Int32		i;
			GBSPChunk	Chunk		=new GBSPChunk();
			GFXCluster	GCluster	=new GFXCluster();

			mGlobals.NumGFXClusters	=mGlobals.NumLeafClusters;
			Chunk.mType		=GBSPChunk.GBSP_CHUNK_CLUSTERS;
			Chunk.mElements =mGlobals.NumGFXClusters;

			Chunk.Write(bw);

			for(i=0;i < mGlobals.NumGFXClusters;i++)
			{
				GCluster.mVisOfs	=-1;

				GCluster.Write(bw);
			}

			return	true;
		}


		bool SaveGFXLeafs(BinaryWriter bw)
		{
			Int32		i;
			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_LEAFS;
			Chunk.mElements	=mGlobals.NumGFXLeafs;

			Chunk.Write(bw);

			mGlobals.TotalLeafSize	=0;

			//NumGFXLeafFaces was counted earlier in the PrepGfxNodes Stage
			mGlobals.GFXLeafFaces	=new Int32[mGlobals.NumGFXLeafFaces];

			//We must reset this...
			mGlobals.NumGFXLeafFaces	=0;

			for(i=0;i < mModels.Count;i++)
			{
				//Save all the leafs for this model
				if(!mModels[i].mRootNode[0].SaveGFXLeafs_r(mGlobals, bw))
				{
					Map.Print("SaveGFXLeafs:  SaveGFXLeafs_r failed.\n");
					return	false;
				}
			}

			//Save gfx leaf faces here...
			Chunk.mType		=GBSPChunk.GBSP_CHUNK_LEAF_FACES;
			Chunk.mElements =mGlobals.NumGFXLeafFaces;

			Chunk.Write(bw, mGlobals.GFXLeafFaces);

			return	true;
		}


		bool SaveGFXNodes(BinaryWriter bw)
		{
			Int32		i;
			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_NODES;
			Chunk.mElements	=mGlobals.NumGFXNodes;

			Chunk.Write(bw);
			
			for(i=0;i < mModels.Count; i++)
			{
				if(!mModels[i].mRootNode[0].SaveGFXNodes_r(bw))
				{
					return	false;
				}
			}
			return	true;
		}


		internal bool FinishAreas()
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

			mGlobals.NumGFXAreaPortals	=0;

			//Area 0 is the invalid area, set it here, and skip it in the loop below
			mGlobals.GFXAreas[0].FirstAreaPortal	=0;
			mGlobals.GFXAreas[0].NumAreaPortals		=0;
			
			for(int i=1;i < mGlobals.NumGFXAreas;i++)
			{
				GFXArea	area			=mGlobals.GFXAreas[i];
				area.FirstAreaPortal	=mGlobals.NumGFXAreas;

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

					if(mGlobals.NumGFXAreaPortals >= GBSPGlobals.MAX_AREA_PORTALS)
					{
						Print("FinishAreas:  Max area portals.\n");
						return	false;
					}

					GFXAreaPortal	p	=mGlobals.GFXAreaPortals[mGlobals.NumGFXAreaPortals];

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
					mGlobals.NumGFXAreaPortals++;
				}

				area.NumAreaPortals	=mGlobals.NumGFXAreaPortals - area.FirstAreaPortal;
			}
			return	true;
		}


		public void LightGBSPFile(string fileName, LightParams prms)
		{
			mGlobals.ExtraLightCorrection	=prms.mbExtraSamples;
			mGlobals.FastPatch				=prms.mbFastPatch;
			mGlobals.DoRadiosity			=prms.mbRadiosity;
			mGlobals.EntityScale			=prms.mLightScale;
			mGlobals.MinLight.X				=prms.mMinLight.X;
			mGlobals.MinLight.Y				=prms.mMinLight.Y;
			mGlobals.MinLight.Z				=prms.mMinLight.Z;
			mGlobals.ReflectiveScale		=prms.mMirrorReflect;
			mGlobals.NumBounce				=prms.mNumBounces;
			mGlobals.PatchSize				=prms.mPatchSize;
		}


		public void VisGBSPFile(string fileName, VisParams prms)
		{
		}
	}
}
