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
		public MapEntity		[]GFXEntData;
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
		public VISPortal	[]VisSortedPortals;
		public byte			[]PortalSeen;		// Temp vis array
		public byte			[]PortalBits;
		public Int32		NumVisLeafs;		// Total VisLeafs
		public Int32		NumVisLeafBytes;	// NumVisLeaf / 8
		public Int32		NumVisLeafLongs;	// NumVisBytes / sizeof(uInt32)
		public byte			[]LeafVisBits;		// Should be NumVisLeafs * (NumVisLeafs / 8)
		public VISLeaf		[]VisLeafs;			// NumVisLeafs
		public bool			bVisPortals;
		public Int32		LeafSee;

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
		public const int	MAX_TEMP_PORTALS		=25000;
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
				int	root	=mGlobals.GFXModels[0].mRootNode[0];

				RenderBSPFrontBack_r2(root, pos, verts, indexes, true);
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
					if(!mod.ProcessWorldModel(mGlobals, me.mBrushes, mEntities, mPlanePool, mTIPool))
					{
						return	false;
					}
				}
				else
				{
					if(!mGlobals.EntityVerbose)
					{
						mGlobals.Verbose	=false;
					}

					if(!mod.ProcessSubModel(mGlobals, me.mBrushes, mPlanePool, mTIPool))
					{
						return	false;
					}
				}
				mModels.Add(mod);
				index++;
			}

			mGlobals.Verbose	=OldVerbose;

			return	true;
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


		public bool BuildTree(BSPBuildParams prms)
		{
			BeginGBSPModels();

			mbBuilding				=true;
			mMaxCPUCores			=prms.mMaxCores;
			mGlobals.Verbose		=prms.mbVerbose;
			mGlobals.EntityVerbose	=prms.mbEntityVerbose;

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


		bool SaveGFXModelDataFromList(BinaryWriter bw)
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


		bool SaveGFXModelData(BinaryWriter bw)
		{
			Int32		i;
			GBSPChunk	Chunk	=new GBSPChunk();
			GFXModel	GModel	=new GFXModel();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_MODELS;
			Chunk.mElements	=mModels.Count;

			Chunk.Write(bw);

			for(i=0;i < mGlobals.NumGFXModels;i++)
			{
				mGlobals.GFXModels[i].Write(bw);
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
			if(!SaveGFXModelDataFromList(bw))
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
			if(!SaveGFXEntDataList(bw))
			{
				return	false;
			}

			//do save texinfo
			
			Chunk.mType		=GBSPChunk.GBSP_CHUNK_END;
			Chunk.mElements	=0;
			Chunk.Write(bw);

			bw.Close();
			file.Close();

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


		bool SaveGFXEntDataList(BinaryWriter bw)
		{
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


		bool SaveGFXEntData(BinaryWriter bw)
		{
			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_ENTDATA;
			Chunk.mElements =mGlobals.NumGFXEntData;

			Chunk.Write(bw);

			for(int i=0;i < mGlobals.NumGFXEntData;i++)
			{
				mGlobals.GFXEntData[i].Write(bw);
			}
			return	true;
		}


		bool SaveGFXLightData(BinaryWriter bw)
		{
			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_LIGHTDATA;
			Chunk.mElements =mGlobals.NumGFXLightData;

			Chunk.Write(bw);

			for(int i=0;i < mGlobals.NumGFXLightData;i++)
			{
				bw.Write(mGlobals.GFXLightData[i]);
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


		bool SaveGFXVisData(BinaryWriter bw)
		{
			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_VISDATA;
			Chunk.mElements =mGlobals.NumGFXVisData;

			if(!Chunk.Write(bw, mGlobals.GFXVisData))
			{
				Print("SaveGFXvertIndexList:  There was an error saving the VertIndexList.\n");
				return	false;
			}
			return	true;
		}


		bool SaveGFXVerts(BinaryWriter bw)
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


		bool SaveGFXRGBVerts(BinaryWriter bw)
		{
			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_RGB_VERTS;
			Chunk.mElements =mGlobals.NumGFXRGBVerts;

			if(!Chunk.Write(bw, mGlobals.GFXRGBVerts))
			{
				Print("There was an error writing the rgb verts.\n");
				return	false;
			}
			return	true;
		}


		bool SaveGFXPlanes(BinaryWriter bw)
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


		bool SaveGFXPortals(BinaryWriter bw)
		{
			Int32		i;
			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_PORTALS;
			Chunk.mElements =mGlobals.NumGFXPortals;

			Chunk.Write(bw);

			for(i=0;i < mGlobals.NumGFXPortals;i++)
			{
				mGlobals.GFXPortals[i].Write(bw);
			}
			return	true;
		}


		bool SaveGFXBNodes(BinaryWriter bw)
		{
			Int32		i;
			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_BNODES;
			Chunk.mElements =mGlobals.NumGFXBNodes;

			Chunk.Write(bw);

			for(i=0;i < mGlobals.NumGFXBNodes;i++)
			{
				mGlobals.GFXBNodes[i].Write(bw);
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


		public bool LightGBSPFile(string fileName, LightParams prms)
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

			string	RecFile;

			mGlobals.LVerbose	=true;

			if(prms.mbExtraSamples)
			{
				mGlobals.NumSamples	=5;
			}
			else
			{
				mGlobals.NumSamples	=1;
			}

			Print(" --- Radiosity GBSP File --- \n");
			
			if(!LoadGBSPFile(fileName))
			{
				Print("LightGBSPFile:  Could not load GBSP file: " + fileName + "\n");
				return	false;
			}
			return	true;
			/*
			//Allocate some RGBLight data now
			mGlobals.NumGFXRGBVerts	=mGlobals.NumGFXVertIndexList;
			mGlobals.GFXRGBVerts	=new Vector3[mGlobals.NumGFXRGBVerts];

			if(!MakeVertNormals())
			{
				Print("LightGBSPFile:  MakeVertNormals failed...\n");
				goto	ExitWithError;
			}

			//Make sure no existing light exist...
			mGlobals.GFXLightData		=null;
			mGlobals.NumGFXLightData	=0;

			//Get the receiver file name
			int	extPos	=fileName.LastIndexOf(".");
			RecFile		=fileName.Substring(0, extPos);
			RecFile		+=".rec";

			if(!ConvertGFXEntDataToEntities())
			{
				goto	ExitWithError;
			}

			FileStream	file	=UtilityLib.FileUtil.OpenTitleFile(fileName,
									FileMode.OpenOrCreate, FileAccess.Write);

			if(file == null)
			{
				Print("LightGBSPFile:  Could not open GBSP file for writing: " + fileName + "\n");
				goto	ExitWithError;
			}
			BinaryWriter	bw	=new BinaryWriter(file);

			Print("Num Faces            : " + mGlobals.NumGFXFaces + "\n");

			//Build the patches (before direct lights are created)
			if(mGlobals.DoRadiosity)
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
			if(!LightFaces())		//Light all the faces lightmaps, and apply to patches
			{
				goto	ExitWithError;
			}

			FreeDirectLights();

			if(mGlobals.DoRadiosity)
			{
				//Pre-calc how much light is distributed to each patch from every patch
				if(!CalcReceivers(RecFile))	
				{
					goto ExitWithError;
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

			FinalizeRGBVerts();

			if(!StartWriting(f))	//Open bsp file and save all current bsp data (except lightmaps)
			{
				goto	ExitWithError;
			}

			if(!SaveLightmaps(f))	//Save them
			{
				goto	ExitWithError;
			}

			if(!FinishWriting(f))	//Write the END chunk to the file
			{
				goto	ExitWithError;
			}

			bw.Close();
			file.Close();

			CleanupLight();

			Print("Num Light Maps       : " + mGlobals.RGBMaps + "\n");

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
			}*/
		}


		public bool LoadGBSPFile(string fileName)
		{
			FileStream	file	=UtilityLib.FileUtil.OpenTitleFile(fileName,
									FileMode.Open, FileAccess.Read);

			if(file == null)
			{
				return	false;
			}

			BinaryReader	br	=new BinaryReader(file);

			while(true)
			{
				GBSPChunk	chunk	=new GBSPChunk();
				if(!chunk.Read(br, mGlobals))
				{
					return	false;
				}

				if(chunk.mType == GBSPChunk.GBSP_CHUNK_END)
				{
					break;
				}
			}

			br.Close();
			file.Close();

			Print("Load complete\n");

			return	true;
		}


		public bool VisGBSPFile(string fileName, VisParams prms, BSPBuildParams prms2)
		{
			mGlobals.FullVis	=prms.mbFullVis;
			mGlobals.NoSort		=!prms.mbSortPortals;
			mGlobals.VisVerbose	=prms2.mbVerbose;

			Print(" --- Vis GBSP File --- \n");

			// Fill in the global bsp data
			/*
			if(!LoadGBSPFile(fileName))
			{
				Print("PvsGBSPFile:  Could not load GBSP file: " + fileName + "\n");
				return	false;
			}*/
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
			if(!LoadPortalFile(PFile))
			{
				goto	ExitWithError;
			}

			Print("NumPortals           : " + mGlobals.NumVisPortals + "\n");
			
			//Write out everything but vis info
			if(!StartWritingVis(bw))
			{
				goto	ExitWithError;
			}

			//Vis'em
			if(!VisAllLeafs())
			{
				goto	ExitWithError;
			}

			//Record the vis data
			mGlobals.NumGFXVisData	=mGlobals.NumVisLeafs * mGlobals.NumVisLeafBytes;
			mGlobals.GFXVisData		=mGlobals.LeafVisBits;

			//Save the leafs, clusters, vis data, etc
			if(!FinishWritingVis(bw))
			{
				goto	ExitWithError;
			}

			//Free all the vis stuff
			FreeAllVisData();

			//Free any remaining leftover bsp data
			FreeGBSPFile();

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

				FreeAllVisData();
				FreeGBSPFile();

				return	false;
			}
		}


		bool FinishWritingVis(BinaryWriter bw)
		{
			if(!SaveGFXLeafs(bw))
			{
				return	false;
			}
			if(!SaveGFXClusters(bw))
			{
				return	false;
			}
			if(!SaveGFXVisData(bw))
			{
				return	false;
			}

			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_END;
			Chunk.mElements	=0;
			Chunk.Write(bw);

			return	true;
		}


		bool VisAllLeafs()
		{
			Int32	i;

			//Create PortalSeen array.  This is used by Vis flooding routines
			//This is deleted below...
			mGlobals.PortalSeen	=new byte[mGlobals.NumVisPortals];

			//create a dictionary to map a vis portal back to an index
			Dictionary<VISPortal, Int32>	portIndexer	=new Dictionary<VISPortal, Int32>();
			for(i=0;i < mGlobals.NumVisPortals;i++)
			{
				portIndexer.Add(mGlobals.VisPortals[i], i);
			}

			//Flood all the leafs with the fast method first...
			for(i=0;i < mGlobals.NumVisLeafs; i++)
			{
				FloodLeafPortalsFast(i, portIndexer);
			}

			//Sort the portals with MightSee
			SortPortals();

			if(mGlobals.FullVis)
			{
				/*if(!FloodPortalsSlow())
				{
					return	false;
				}*/
			}

			//Don't need this anymore...
			mGlobals.PortalSeen	=null;

			mGlobals.LeafVisBits	=new byte[mGlobals.NumVisLeafs * mGlobals.NumVisLeafBytes];
			if(mGlobals.LeafVisBits == null)
			{
				Print("VisAllLeafs:  Out of memory for LeafVisBits.\n");
				goto	ExitWithError;
			}

			mGlobals.TotalVisibleLeafs	=0;

			mGlobals.PortalBits	=new byte[mGlobals.NumVisPortalBytes];

			if(mGlobals.PortalBits == null)
			{
				goto	ExitWithError;
			}

			for(i=0;i < mGlobals.NumVisLeafs;i++)
			{
				mGlobals.LeafSee	=0;
				
				if(!CollectLeafVisBits(i))
				{
					goto	ExitWithError;
				}
				mGlobals.TotalVisibleLeafs	+=mGlobals.LeafSee;
			}
			mGlobals.PortalBits	=null;

			Print("Total visible areas           : " + mGlobals.TotalVisibleLeafs + "\n");
			Print("Average visible from each area: " + mGlobals.TotalVisibleLeafs / mGlobals.NumVisLeafs + "\n");

			return	true;

			// ==== ERROR ====
			ExitWithError:
			{
				// Free all the global vis data
				FreeAllVisData();

				return	false;
			}
		}

		private bool CollectLeafVisBits(int LeafNum)
		{
			VISPortal	Portal, SPortal;
			VISLeaf		Leaf;
			Int32		k, Bit, SLeaf, LeafBitsOfs;
			
			Leaf	=mGlobals.VisLeafs[LeafNum];

			LeafBitsOfs	=LeafNum * mGlobals.NumVisLeafBytes;

			for(int i=0;i < mGlobals.NumVisPortalBytes;i++)
			{
				mGlobals.PortalBits[i]	=0;
			}

			//'OR' all portals that this portal can see into one list
			for(Portal=Leaf.mPortals;Portal != null;Portal=Portal.mNext)
			{
				if(Portal.mFinalVisBits != null)
				{
					//Try to use final vis info first
					for(k=0;k < mGlobals.NumVisPortalBytes;k++)
					{
						mGlobals.PortalBits[k]	|=Portal.mFinalVisBits[k];
					}
				}
				else if(Portal.mVisBits != null)
				{
					for(k=0;k < mGlobals.NumVisPortalBytes;k++)
					{
						mGlobals.PortalBits[k]	|=Portal.mVisBits[k];
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
			for (k=0; k< mGlobals.NumVisPortals; k++)
			{
				if((mGlobals.PortalBits[k >> 3] & (1 << (k & 7))) != 0)
				{
					SPortal	=mGlobals.VisPortals[k];
					SLeaf	=SPortal.mLeaf;
					Debug.Assert((1 << (SLeaf & 7)) < 256);
					mGlobals.LeafVisBits[LeafBitsOfs + (SLeaf >> 3)]	|=(byte)(1 << (SLeaf & 7));
				}
			}
					
			Bit	=1 << (LeafNum & 7);

			Debug.Assert(Bit < 256);

			//He should not have seen himself (yet...)
			if((mGlobals.LeafVisBits[LeafBitsOfs + (LeafNum >> 3)] & Bit) != 0)
			{
				Map.Print("*WARNING* CollectLeafVisBits:  Leaf:" + LeafNum + " can see himself!\n");
			}
			mGlobals.LeafVisBits[LeafBitsOfs + (LeafNum >> 3)]	|=(byte)Bit;

			for(k=0;k < mGlobals.NumVisLeafs;k++)
			{
				Bit	=(1 << (k & 7));

				if((mGlobals.LeafVisBits[LeafBitsOfs + (k>>3)] & Bit) != 0)
				{
					mGlobals.LeafSee++;
				}
			}

			if(mGlobals.LeafSee == 0)
			{
				Map.Print("CollectLeafVisBits:  Leaf can't see nothing.\n");
				return	false;
			}

			mGlobals.GFXClusters[LeafNum].mVisOfs	=LeafBitsOfs;

			return	true;
		}


		void SortPortals()
		{
			List<VISPortal>	sortMe	=new List<VISPortal>(mGlobals.VisPortals);

			sortMe.Sort(new VisPortalComparer());

			mGlobals.VisSortedPortals	=sortMe.ToArray();
		}


		void FloodLeafPortalsFast(int leafNum, Dictionary<VISPortal, Int32> visIndexer)
		{
			VISLeaf		Leaf;
			VISPortal	Portal;

			Leaf	=mGlobals.VisLeafs[leafNum];

			if(Leaf.mPortals == null)
			{
				//GHook.Printf("*WARNING* FloodLeafPortalsFast:  Leaf with no portals.\n");
				return;
			}
			
			mGlobals.SrcLeaf	=leafNum;

			for(Portal=Leaf.mPortals;Portal != null;Portal=Portal.mNext)
			{
				Portal.mVisBits	=new byte[mGlobals.NumVisPortalBytes];

				//This portal can't see anyone yet...
				for(int i=0;i < mGlobals.NumVisPortalBytes;i++)
				{
					Portal.mVisBits[i]	=0;
				}
				for(int i=0;i < mGlobals.NumVisPortals;i++)
				{
					mGlobals.PortalSeen[i]	=0;
				}

				mGlobals.MightSee	=0;
				
				Portal.FloodPortalsFast_r(mGlobals, Portal, visIndexer);
			}
		}


		bool StartWritingVis(BinaryWriter bw)
		{
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
			if(!SaveGFXNodes(bw))
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
			if(!SaveGFXPlanes(bw))
			{
				return	false;
			}
			if(!SaveGFXFaces(bw))
			{
				return	false;
			}
			if(!SaveGFXAreasAndPortals(bw))
			{
				return	false;
			}
			if(!SaveGFXLeafs(bw))
			{
				return	false;
			}
			if(!SaveGFXLeafSides(bw))
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
			if(!SaveGFXRGBVerts(bw))
			{
				return	false;
			}
			if(!SaveGFXEntData(bw))
			{
				return	false;
			}
			if(!SaveGFXTexInfos(bw))
			{
				return	false;
			}
			if(!SaveGFXLightData(bw))
			{
				return	false;
			}
			
			chunk.mType		=GBSPChunk.GBSP_CHUNK_END;
			chunk.mElements	=0;
			chunk.Write(bw);

			return	true;
		}


		void FreeAllVisData()
		{
			mGlobals.LeafVisBits	=null;
			mGlobals.GFXVisData		=null;
			mGlobals.NumGFXVisData	=0;

			if(mGlobals.VisPortals != null)
			{
				for(int i=0;i < mGlobals.NumVisPortals;i++)
				{
					mGlobals.VisPortals[i].mPoly			=null;
					mGlobals.VisPortals[i].mFinalVisBits	=null;
					mGlobals.VisPortals[i].mVisBits			=null;
				}

				mGlobals.VisPortals	=null;
			}
			mGlobals.VisPortals			=null;
			mGlobals.VisSortedPortals	=null;
			mGlobals.PortalSeen			=null;
			mGlobals.VisLeafs			=null;

			FreeGBSPFile();		// Free rest of GBSP GFX data
		}


		bool LoadPortalFile(string PFile)
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
			mGlobals.NumVisPortals	=br.ReadInt32();
			if(mGlobals.NumVisPortals >= GBSPGlobals.MAX_TEMP_PORTALS)
			{
				Print("LoadPortalFile:  Max portals for temp buffers.\n");
				goto	ExitWithError;
			}
			
			mGlobals.VisPortals	=new VISPortal[mGlobals.NumVisPortals];
			if(mGlobals.VisPortals == null)
			{
				Print("LoadPortalFile:  Out of memory for VisPortals.\n");
				goto	ExitWithError;
			}
			
			mGlobals.VisSortedPortals	=new VISPortal[mGlobals.NumVisPortals];
			if(mGlobals.VisSortedPortals == null)
			{
				Print("LoadPortalFile:  Out of memory for VisSortedPortals.\n");
				goto ExitWithError;
			}

			//
			//	Get the number of leafs
			//
			mGlobals.NumVisLeafs	=br.ReadInt32();
			if(mGlobals.NumVisLeafs > mGlobals.NumGFXLeafs)
			{
				goto	ExitWithError;
			}
			
			mGlobals.VisLeafs	=new VISLeaf[mGlobals.NumVisLeafs];
			if(mGlobals.VisLeafs == null)
			{
				Print("LoadPortalFile:  Out of memory for VisLeafs.\n");
				goto ExitWithError;
			}

			//fill arrays with blank objects
			for(i=0;i < mGlobals.NumVisLeafs;i++)
			{
				mGlobals.VisLeafs[i]	=new VISLeaf();
			}

			//
			//	Load in the portals
			//
			for(i=0;i < mGlobals.NumVisPortals;i++)
			{
				//alloc blank portal
				mGlobals.VisPortals[i]	=new VISPortal();

				NumVerts	=br.ReadInt32();

				pPoly	=new GBSPPoly();

				for(int j=0;j < NumVerts;j++)
				{
					Vector3	vert;
					vert.X	=br.ReadSingle();
					vert.Y	=br.ReadSingle();
					vert.Z	=br.ReadSingle();

					pPoly.mVerts.Add(vert);
				}

				LeafFrom	=br.ReadInt32();
				LeafTo		=br.ReadInt32();
				
				if(LeafFrom >= mGlobals.NumVisLeafs || LeafFrom < 0)
				{
					Print("LoadPortalFile:  Invalid LeafFrom: " + LeafFrom + "\n");
					goto	ExitWithError;
				}

				if(LeafTo >= mGlobals.NumVisLeafs || LeafTo < 0)
				{
					Print("LoadPortalFile:  Invalid LeafTo: " + LeafTo + "\n");
					goto	ExitWithError;
				}

				pLeaf	=mGlobals.VisLeafs[LeafFrom];
				pPortal	=mGlobals.VisPortals[i];

				pPortal.mPoly	=pPoly;
				pPortal.mLeaf	=LeafTo;
				pPortal.mPlane	=new GBSPPlane(pPoly);
				pPortal.mNext	=pLeaf.mPortals;
				pLeaf.mPortals	=pPortal;

				pPortal.CalcPortalInfo();
			}
			
			mGlobals.NumVisLeafBytes	=((mGlobals.NumVisLeafs+63)&~63) >> 3;
			mGlobals.NumVisPortalBytes	=((mGlobals.NumVisPortals+63)&~63) >> 3;

			mGlobals.NumVisPortalLongs	=mGlobals.NumVisPortalBytes / sizeof(UInt32);
			mGlobals.NumVisLeafLongs	=mGlobals.NumVisLeafBytes / sizeof(UInt32);

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

				mGlobals.VisPortals			=null;
				mGlobals.VisSortedPortals	=null;
				mGlobals.VisLeafs			=null;
				pPoly						=null;

				return	false;
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

				Debug.Assert(Leaf >= 0 && Leaf < mGlobals.NumGFXLeafs);

				for(int i=0;i < mGlobals.GFXLeafs[Leaf].mNumFaces;i++)
				{
					int		ofs		=verts.Count;
					UInt32	offset	=(UInt32)ofs;
					int		face	=mGlobals.GFXLeafFaces[mGlobals.GFXLeafs[Leaf].mFirstFace + i];
					int		nverts	=mGlobals.GFXFaces[face].mNumVerts;
					int		fvert	=mGlobals.GFXFaces[face].mFirstVert;

					for(int j=fvert;j < (fvert + nverts);j++)
					{
						int	idx	=mGlobals.GFXVertIndexList[j];
						verts.Add(mGlobals.GFXVerts[idx]);
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

			pNode	=mGlobals.GFXNodes[Node];
			
			//Get the distance that the eye is from this plane
			Dist1	=mGlobals.GFXPlanes[pNode.mPlaneNum].DistanceFast(pos);

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


		void FreeFileVisData()
		{
			mGlobals.GFXVisData		=null;
			mGlobals.NumGFXVisData	=0;
		}
	}
}
