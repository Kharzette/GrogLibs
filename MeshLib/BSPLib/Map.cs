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
		public DirectLight	[]DirectClusterLights	=new DirectLight[MAX_DIRECT_CLUSTER_LIGHTS];
		public DirectLight	[]DirectLights			=new DirectLight[MAX_DIRECT_LIGHTS];
		public RADPatch		[]FacePatches;
		public RADPatch		[]PatchList;
		public float		[]RecAmount;
		public LInfo		[]Lightmaps;
		public FInfo		[]FaceInfo;
		public float		LightScale		=1.00f;
		public float		EntityScale		=1.00f;
		public float		MaxLight		=230.0f;
		public Int32		NumSamples		=5;
		public Vector3		MinLight;
		public Int32		LightOffset;
		public Int32		RGBMaps			=0;
		public Int32		REGMaps			=0;
		public Int32		NumPatches;
		public Int32		NumReceivers;
		public Int32		NumLMaps;
		public Int32		NumDirectLights;

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

		//collision / raycasting
		public bool		HitLeaf;
		public Int32	GlobalPlane;
		public Int32	GlobalNode;
		public Int32	GlobalSide;
		public Vector3	GlobalI;

		//constants
		public const int	MAX_BSP_MODELS				=2048;
		public const int	MAX_WELDED_VERTS			=64000;
		public const int	MAX_TEMP_INDEX_VERTS		=1024;
		public const int	MAX_PATCHES					=65000;
		public const int	MAX_LTYPE_INDEX				=12;
		public const int	MAX_LTYPES					=4;
		public const int	LGRID_SIZE					=16;
		public const int	MAX_LMAP_SIZE				=130;
		public const int	MAX_LEAF_SIDES				=64000 * 2;
		public const int	MAX_TEMP_LEAF_SIDES			=100;
		public const int	MAX_AREAS					=256;
		public const int	MAX_AREA_PORTALS			=1024;
		public const int	MAX_TEMP_PORTALS			=25000;
		public const int	MAX_DIRECT_CLUSTER_LIGHTS	=25000;
		public const int	MAX_DIRECT_LIGHTS			=25000;
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

				VisWorld(root, pos);

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
			if(mGlobals.NumGFXModels <= 0)
			{
				return	true;
			}
			Int32		i;
			GBSPChunk	Chunk	=new GBSPChunk();
			GFXModel	GModel	=new GFXModel();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_MODELS;
			Chunk.mElements	=mGlobals.NumGFXModels;

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


		bool SaveGFXTexInfos(BinaryWriter bw)
		{
			if(mTIPool.mTexInfos.Count <= 0)
			{
				return	true;
			}
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


		bool SaveVisdGFXTexInfos(BinaryWriter bw)
		{
			if(mGlobals.NumGFXTexInfo <= 0)
			{
				return	true;
			}
			GBSPChunk	Chunk	=new GBSPChunk();;

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_TEXINFO;
			Chunk.mElements	=mGlobals.NumGFXTexInfo;

			Chunk.Write(bw);

			for(int i=0;i < mGlobals.NumGFXTexInfo;i++)
			{
				mGlobals.GFXTexInfo[i].Write(bw);
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


		bool SaveGFXEntData(BinaryWriter bw)
		{
			if(mGlobals.NumGFXEntData <= 0)			
			{
				return	true;
			}
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
			if(mGlobals.NumGFXLightData <= 0)
			{
				return	true;
			}
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
			if(mGlobals.NumGFXVertIndexList <= 0)
			{
				return	true;
			}
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
			if(mGlobals.NumGFXVisData <= 0)
			{
				return	true;
			}
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
			if(mGlobals.NumGFXVerts <= 0)
			{
				return	true;
			}
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
			if(mGlobals.NumGFXRGBVerts <= 0)
			{
				return	true;
			}
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
			if(mPlanePool.mPlanes.Count <= 0)
			{
				return	true;
			}
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


		bool SaveVisdGFXPlanes(BinaryWriter bw)
		{
			if(mGlobals.NumGFXPlanes <= 0)
			{
				return	true;
			}
			Int32		i;
			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_PLANES;
			Chunk.mElements	=mGlobals.NumGFXPlanes;

			Chunk.Write(bw);

			for(i=0;i < mGlobals.NumGFXPlanes;i++)
			{
				mGlobals.GFXPlanes[i].Write(bw);
			}
			return	true;
		}


		bool SaveGFXFaces(BinaryWriter bw)
		{
			if(mGlobals.NumGFXFaces <= 0)
			{
				return	true;
			}
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


		bool SaveVisdGFXFaces(BinaryWriter bw)
		{
			if(mGlobals.NumGFXFaces <= 0)
			{
				return	true;
			}
			Int32		i;
			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_FACES;
			Chunk.mElements =mGlobals.NumGFXFaces;

			Chunk.Write(bw);

			for(i=0;i < mGlobals.NumGFXFaces;i++)
			{
				mGlobals.GFXFaces[i].Write(bw);
			}
			return	true;
		}


		bool SaveGFXPortals(BinaryWriter bw)
		{
			if(mGlobals.NumGFXPortals <= 0)
			{
				return	true;
			}
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
			if(mGlobals.NumGFXBNodes <= 0)
			{
				return	true;
			}
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
			if(mGlobals.NumGFXLeafSides <= 0)
			{
				return	true;
			}
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

			if(mGlobals.NumGFXAreas > 0)
			{
				//
				// Save the areas first
				//
				Chunk.mType		=GBSPChunk.GBSP_CHUNK_AREAS;
				Chunk.mElements =mGlobals.NumGFXAreas;

				Chunk.Write(bw, mGlobals.GFXAreas);
			}

			if(mGlobals.NumGFXAreaPortals > 0)
			{
				//
				//	Then, save the areaportals
				//
				Chunk.mType		=GBSPChunk.GBSP_CHUNK_AREA_PORTALS;
				Chunk.mElements =mGlobals.NumGFXAreaPortals;

				Chunk.Write(bw, mGlobals.GFXAreaPortals);
			}
			return	true;
		}


		bool SaveGFXClusters(BinaryWriter bw)
		{
			if(mGlobals.NumLeafClusters <= 0)
			{
				return	true;
			}
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


		bool SaveVisdGFXClusters(BinaryWriter bw)
		{
			if(mGlobals.NumGFXClusters <= 0)
			{
				return	true;
			}
			Int32		i;
			GBSPChunk	Chunk		=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_CLUSTERS;
			Chunk.mElements =mGlobals.NumGFXClusters;

			Chunk.Write(bw);

			for(i=0;i < mGlobals.NumGFXClusters;i++)
			{
				mGlobals.GFXClusters[i].Write(bw);
			}
			return	true;
		}


		bool SaveVisdGFXLeafs(BinaryWriter bw)
		{
			if(mGlobals.NumGFXLeafs <= 0)
			{
				return	true;
			}
			Int32		i;
			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_LEAFS;
			Chunk.mElements	=mGlobals.NumGFXLeafs;

			Chunk.Write(bw);

			for(i=0;i < Chunk.mElements;i++)
			{
				mGlobals.GFXLeafs[i].Write(bw);
			}
			return	true;
		}


		bool SaveGFXLeafs(BinaryWriter bw)
		{
			if(mGlobals.NumGFXLeafs <= 0)
			{
				return	true;
			}
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


		bool SaveVisdGFXLeafFacesAndSides(BinaryWriter bw)
		{
			GBSPChunk	Chunk	=new GBSPChunk();

			if(mGlobals.NumGFXLeafFaces > 0)
			{
				Chunk.mType		=GBSPChunk.GBSP_CHUNK_LEAF_FACES;
				Chunk.mElements	=mGlobals.NumGFXLeafFaces;
				Chunk.Write(bw, mGlobals.GFXLeafFaces);
			}

			if(mGlobals.NumGFXLeafSides > 0)
			{
				Chunk.mType		=GBSPChunk.GBSP_CHUNK_LEAF_SIDES;
				Chunk.mElements	=mGlobals.NumGFXLeafSides;
				Chunk.Write(bw, mGlobals.GFXLeafSides);
			}

			return	true;
		}


		bool SaveGFXNodes(BinaryWriter bw)
		{
			if(mGlobals.NumGFXNodes <= 0)
			{
				return	true;
			}
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


		bool SaveVisdGFXNodes(BinaryWriter bw)
		{
			if(mGlobals.NumGFXNodes <= 0)
			{
				return	true;
			}
			Int32		i;
			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_NODES;
			Chunk.mElements	=mGlobals.NumGFXNodes;

			Chunk.Write(bw);
			
			for(i=0;i < mGlobals.NumGFXNodes; i++)
			{
				mGlobals.GFXNodes[i].Write(bw);
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


		bool MakeVertNormals()
		{
			mGlobals.VertNormals	=new Vector3[mGlobals.NumGFXVerts];

			if(mGlobals.VertNormals == null)
			{
				Print("MakeVertNormals:  Out of memory for normals.\n");
				return	false;
			}

			for(int i=0;i < mGlobals.NumGFXFaces;i++)
			{
				GFXFace	f	=mGlobals.GFXFaces[i];

				Vector3	Normal	=mGlobals.GFXPlanes[f.mPlaneNum].mNormal;

				if(f.mPlaneSide != 0)
				{
					Normal	=-Normal;
				}

				for(int v=0;v < f.mNumVerts;v++)
				{
					Int32	vn	=f.mFirstVert + v;

					Int32	Index	=mGlobals.GFXVertIndexList[vn];

					mGlobals.VertNormals[Index]	=mGlobals.VertNormals[Index] + Normal;
				}
			}

			for(int i=0;i < mGlobals.NumGFXVerts;i++)
			{
				mGlobals.VertNormals[i].Normalize();
			}
			return	true;
		}


		bool CalcFaceInfo(FInfo FaceInfo, LInfo LightInfo)
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
				Mins[i]	=Brush.MIN_MAX_BOUNDS;
				Maxs[i]	=-Brush.MIN_MAX_BOUNDS;
			}

			Vector3	[]vecs	=new Vector3[2];

			GBSPPlane	pln;

			pln.mNormal	=FaceInfo.Plane.mNormal;
			pln.mDist	=FaceInfo.Plane.mDist;
			pln.mType	=FaceInfo.Plane.mType;

			GBSPPoly.TextureAxisFromPlane(pln, out vecs[0], out vecs[1]);

			FaceInfo.Center	=Vector3.Zero;

			indOffset	=mGlobals.GFXFaces[Face].mFirstVert;

			for(i=0;i < mGlobals.GFXFaces[Face].mNumVerts;i++, indOffset++)
			{
				int	vIndex	=mGlobals.GFXVertIndexList[indOffset];
				Vert		=mGlobals.GFXVerts[vIndex];
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
			FaceInfo.Center	/=mGlobals.GFXFaces[Face].mNumVerts;

			// Get the Texture U/V mins/max, and Grid aligned lmap mins/max/size
			for(i=0;i < 2;i++)
			{
				LightInfo.Mins[i]	=Mins[i];
				LightInfo.Maxs[i]	=Maxs[i];

				Mins[i]	=(float)Math.Floor(Mins[i] / GBSPGlobals.LGRID_SIZE);
				Maxs[i]	=(float)Math.Ceiling(Maxs[i] / GBSPGlobals.LGRID_SIZE);

				LightInfo.LMins[i]	=(Int32)Mins[i];
				LightInfo.LMaxs[i]	=(Int32)Maxs[i];
				LightInfo.LSize[i]	=(Int32)(Maxs[i] - Mins[i]);

				if((LightInfo.LSize[i] + 1) > GBSPGlobals.MAX_LMAP_SIZE)
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


		bool LightFaces()
		{
			Int32	i, s;
			bool	Hit;
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

			mGlobals.Lightmaps	=new LInfo[mGlobals.NumGFXFaces];

			if(mGlobals.Lightmaps == null)
			{
				Print("LightFaces:  Out of memory for Lightmaps.\n");
				return	false;
			}

			mGlobals.FaceInfo	=new FInfo[mGlobals.NumGFXFaces];

			if(mGlobals.FaceInfo == null)
			{
				Print("LightFaces:  Out of memory for FaceInfo.\n");
				return	false;
			}

			for(i=0;i < mGlobals.NumGFXFaces;i++)
			{
				mGlobals.Lightmaps[i]	=new LInfo();
				mGlobals.FaceInfo[i]	=new FInfo();
			}

			mGlobals.NumLMaps	=0;

			Perc	=mGlobals.NumGFXFaces / 20;

			for(i=0;i < mGlobals.NumGFXFaces;i++)
			{
				Hit	=false;

				if(Perc != 0)
				{
					if(((i % Perc) == 0) &&	(i / Perc) <= 20)
					{
						Print("." + (i/Perc));
					}
				}

				int	pnum	=mGlobals.GFXFaces[i].mPlaneNum;
				int	pside	=mGlobals.GFXFaces[i].mPlaneSide;
				mGlobals.FaceInfo[i].Plane.mNormal	=mGlobals.GFXPlanes[pnum].mNormal;
				mGlobals.FaceInfo[i].Plane.mDist	=mGlobals.GFXPlanes[pnum].mDist;
				mGlobals.FaceInfo[i].Plane.mType	=mGlobals.GFXPlanes[pnum].mType;
				if(pside != 0)
				{
					mGlobals.FaceInfo[i].Plane.Inverse();
				}				
				mGlobals.FaceInfo[i].Face	=i;

				//kbaird - commented this section, not saving texinfos atm
		/*		if (GFXTexInfo[GFXFaces[i].TexInfo].Flags & TEXINFO_GOURAUD)
				{
					if (!GouraudShadeFace(i))
					{
						GHook.Error("LightFaces:  GouraudShadeFace failed...\n");
						return GE_FALSE;
					}
					
					if (DoRadiosity)
						TransferLightToPatches(i);
					continue;
				}*/
				
				/*
				if (GFXTexInfo[GFXFaces[i].TexInfo].Flags & TEXINFO_FLAT)
				{
					FlatShadeFace(i);
					continue;
				}
				*/

		//		if (GFXTexInfo[GFXFaces[i].TexInfo].Flags & TEXINFO_NO_LIGHTMAP)
		//			continue;		// Faces with no lightmap don't need to light them 


				if(!CalcFaceInfo(mGlobals.FaceInfo[i], mGlobals.Lightmaps[i]))
				{
					return	false;
				}
			
				Int32	Size	=(mGlobals.Lightmaps[i].LSize[0] + 1)
					* (mGlobals.Lightmaps[i].LSize[1] + 1);

				mGlobals.FaceInfo[i].Points	=new Vector3[Size];

				if(mGlobals.FaceInfo[i].Points == null)
				{
					Print("LightFaces:  Out of memory for face points.\n");
					return	false;
				}
				
				for(s=0;s < mGlobals.NumSamples;s++)
				{
					//Hook.Printf("Sample  : %3i of %3i\n", s+1, NumSamples);
					CalcFacePoints(mGlobals.FaceInfo[i], mGlobals.Lightmaps[i], UOfs[s], VOfs[s]);

					if(!ApplyLightsToFace(mGlobals.FaceInfo[i], mGlobals.Lightmaps[i], 1 / (float)mGlobals.NumSamples))
					{
						return	false;
					}
				}
				
//				if (DoRadiosity)
//				{
//					// Update patches for this face
//					ApplyLightmapToPatches(i);
//				}
			}			
			Print("\n");
			return	true;
		}


		bool ApplyLightsToFace(FInfo FaceInfo, LInfo LightInfo, float Scale)
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
				Int32	nodeLandedIn	=FindLeafLandedIn(0, FaceInfo.Points[v]);
				Leaf	=-(nodeLandedIn + 1);

				if(Leaf < 0 || Leaf >= mGlobals.NumGFXLeafs)
				{
					Print("ApplyLightsToFace:  Invalid leaf num.\n");
					return	false;
				}

				Cluster	=mGlobals.GFXLeafs[Leaf].mCluster;

				if(Cluster < 0)
				{
					continue;
				}

				if(Cluster >= mGlobals.NumGFXClusters)
				{
					Print("*WARNING* ApplyLightsToFace:  Invalid cluster num.\n");
					continue;
				}

//				VisData = &mGlobals.GFXVisData[mGlobals.GFXClusters[Cluster].mVisOfs];
				
				for(c=0;c < mGlobals.NumGFXClusters;c++)
				{
					if((mGlobals.GFXVisData[mGlobals.GFXClusters[Cluster].mVisOfs + (c>>3)] & (1<<(c&7))) == 0)
					{
						continue;
					}

					for(DLight=mGlobals.DirectClusterLights[c];DLight != null;DLight=DLight.mNext)
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
						if(RayCollision(FaceInfo.Points[v], DLight.mOrigin, ref colResult))
						{
							goto	Skip;	//Ray is in shadow
						}

						LType	=DLight.mLType;

						//If the data for this LType has not been allocated, allocate it now...
						if(LightInfo.RGBLData[LType] == null)
						{
							if(LightInfo.NumLTypes >= GBSPGlobals.MAX_LTYPES)
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


		void CalcFacePoints(FInfo FaceInfo, LInfo LightInfo, float UOfs, float VOfs)
		{
			Vector3	FaceMid, I;
			float	MidU, MidV, StartU, StartV, CurU, CurV;
			Int32	i, u, v, Width, Height, Leaf;
			Vector3	Vect;
			byte	[]InSolid	=new byte[GBSPGlobals.MAX_LMAP_SIZE * GBSPGlobals.MAX_LMAP_SIZE];

			MidU	=(LightInfo.Maxs[0] + LightInfo.Mins[0]) * 0.5f;
			MidV	=(LightInfo.Maxs[1] + LightInfo.Mins[1]) * 0.5f;

			FaceMid	=FaceInfo.TexOrg + FaceInfo.T2WVecs[0] * MidU
						+ FaceInfo.T2WVecs[1] * MidV;

			Width	=(LightInfo.LSize[0]) + 1;
			Height	=(LightInfo.LSize[1]) + 1;
			StartU	=((float)LightInfo.LMins[0]+UOfs) * (float)GBSPGlobals.LGRID_SIZE;
			StartV	=((float)LightInfo.LMins[1]+VOfs) * (float)GBSPGlobals.LGRID_SIZE;

			FaceInfo.NumPoints = Width*Height;

//			pPoint = &FaceInfo.Points[0];
//			pInSolid = InSolid;

			for(v=0;v < Height;v++)
			{
				for(u=0;u < Width;u++)
				{
					CurU	=StartU + u * GBSPGlobals.LGRID_SIZE;
					CurV	=StartV + v * GBSPGlobals.LGRID_SIZE;

					FaceInfo.Points[(v * Width) + u]
						=FaceInfo.TexOrg + FaceInfo.T2WVecs[0] * CurU +
							FaceInfo.T2WVecs[1] * CurV;
					
					Int32	nodeLandedIn	=FindLeafLandedIn(0, FaceInfo.Points[(v * Width) + u]);
					Leaf	=-(nodeLandedIn + 1);

					//Pre-compute if this point is in solid space, so we can re-use it in the code below
					if((mGlobals.GFXLeafs[Leaf].mContents & GBSPBrush.BSP_CONTENTS_SOLID2) != 0)
					{
						InSolid[(v * Width) + u]	=1;
					}
					else
					{
						InSolid[(v * Width) + u]	=0;
					}

					if(!mGlobals.ExtraLightCorrection)
					{
						if(InSolid[(v * Width) + u] != 0)
						{
//							if(RayCollision(&FaceMid, pPoint, &I))
//							{
//								geVec3d_Subtract(&FaceMid, pPoint, &Vect);
//								geVec3d_Normalize(&Vect);
//								geVec3d_Add(&I, &Vect, pPoint);
//							}
						}
					}
				}
			}

			if(!mGlobals.ExtraLightCorrection)
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
				BestDist = MIN_MAX_BOUNDS;
				
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


		bool RayIntersect(Vector3 Front, Vector3 Back, Int32 Node)
		{
			float	Fd, Bd, Dist;
			Int32	Side;
			Vector3	I;

			if(Node < 0)						
			{
				Int32	Leaf	=-(Node+1);

				if((mGlobals.GFXLeafs[Leaf].mContents
					& GBSPBrush.BSP_CONTENTS_SOLID2) != 0)
				{
					return	true;	//Ray collided with solid space
				}
				else 
				{
					return	false;	//Ray collided with empty space
				}
			}
			GFXNode		n	=mGlobals.GFXNodes[Node];
			GFXPlane	p	=mGlobals.GFXPlanes[n.mPlaneNum];

			Fd	=p.DistanceFast(Front);
			Bd	=p.DistanceFast(Back);

			if(Fd >= -1 && Bd >= -1)
			{
				return(RayIntersect(Front, Back, n.mChildren[0]));
			}
			if(Fd < 1 && Bd < 1)
			{
				return(RayIntersect(Front, Back, n.mChildren[1]));
			}

			Side	=(Fd < 0)? 1 : 0;
			Dist	=Fd / (Fd - Bd);

			I	=Front + Dist * (Back - Front);

			//Work our way to the front, from the back side.  As soon as there
			//is no more collisions, we can assume that we have the front portion of the
			//ray that is in empty space.  Once we find this, and see that the back half is in
			//solid space, then we found the front intersection point...
			if(RayIntersect(Front, I, n.mChildren[Side]))
			{
				return	true;
			}
			else if(RayIntersect(I, Back, n.mChildren[(Side == 0)? 1 : 0]))
			{
				if(!mGlobals.HitLeaf)
				{
					mGlobals.GlobalPlane	=n.mPlaneNum;
					mGlobals.GlobalSide		=Side;
					mGlobals.GlobalI		=I;
					mGlobals.GlobalNode		=Node;
					mGlobals.HitLeaf		=true;
				}
				return	true;
			}
			return	false;
		}


		bool RayCollision(Vector3 Front, Vector3 Back, ref Vector3 I)
		{
			mGlobals.HitLeaf	=false;
			if(RayIntersect(Front, Back, mGlobals.GFXModels[0].mRootNode[0]))
			{
				I	=mGlobals.GlobalI;			// Set the intersection point
				return	true;
			}
			return	false;
		}


		void FinalizeRGBVerts()
		{
			for(int i=0;i < mGlobals.NumGFXRGBVerts;i++)
			{
				mGlobals.GFXRGBVerts[i]	+=mGlobals.MinLight;

				mGlobals.GFXRGBVerts[i]	=Vector3.Clamp(mGlobals.GFXRGBVerts[i],
					Vector3.Zero, Vector3.One * mGlobals.MaxLight);
			}
		}


		void FreeDirectLights()
		{
			for(int i=0;i < GBSPGlobals.MAX_DIRECT_LIGHTS;i++)
			{
				mGlobals.DirectLights[i]	=null;
			}
			for(int i=0;i < GBSPGlobals.MAX_DIRECT_CLUSTER_LIGHTS;i++)
			{
				mGlobals.DirectClusterLights[i]	=null;
			}
			mGlobals.NumDirectLights	=0;
		}


		bool SaveLightmaps(BinaryWriter f)
		{
//			LInfo		*L;
			Int32		i, j, k,l, Size;
			float		Max, Max2;
			byte		[]LData	=new byte[GBSPGlobals.MAX_LMAP_SIZE * GBSPGlobals.MAX_LMAP_SIZE * 3 * 4];
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
			mGlobals.LightOffset	=0;
			
			//Go through all the faces
			for(i=0;i < mGlobals.NumGFXFaces;i++)
			{
				LInfo	L			=mGlobals.Lightmaps[i];
				FInfo	pFaceInfo	=mGlobals.FaceInfo[i];
				
				// Set face defaults
				mGlobals.GFXFaces[i].mLightOfs	=-1;
				mGlobals.GFXFaces[i].mLWidth	=L.LSize[0] + 1;
				mGlobals.GFXFaces[i].mLHeight	=L.LSize[1] + 1;
				mGlobals.GFXFaces[i].mLTypes[0]	=255;
				mGlobals.GFXFaces[i].mLTypes[1]	=255;
				mGlobals.GFXFaces[i].mLTypes[2]	=255;
				mGlobals.GFXFaces[i].mLTypes[3]	=255;
				
				//Skip special faces with no lightmaps
				if((mGlobals.GFXTexInfo[mGlobals.GFXFaces[i].mTexInfo].mFlags
					& TexInfo.TEXINFO_NO_LIGHTMAP) != 0)
				{
					continue;
				}

				//Get the size of map
				Size	=mGlobals.FaceInfo[i].NumPoints;

				//Create style 0, if min light is set, and style 0 does not exist
				if((L.RGBLData[0] == null) &&
					(mGlobals.MinLight.X > 1
					|| mGlobals.MinLight.Y > 1
					|| mGlobals.MinLight.Z > 1))
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
				mGlobals.GFXFaces[i].mLightOfs	=mGlobals.LightOffset;

				//At this point, all lightmaps are currently RGB
				byte	RGB2	=1;
				
				if(RGB2 != 0)
				{
					mGlobals.RGBMaps++;
				}
				else
				{
					mGlobals.REGMaps++;
				}

				f.Write(RGB2);

				mGlobals.LightOffset++;		//Skip the rgb light byte
				
				NumLTypes	=0;		// Reset number of LTypes for this face
				for(k=0;k < GBSPGlobals.MAX_LTYPE_INDEX;k++)
				{
					if(L.RGBLData[k] == null)
					{
						continue;
					}

					if(NumLTypes >= GBSPGlobals.MAX_LTYPES)
					{
						Print("SaveLightmaps:  Max LightTypes on face.\n");
						return	false;
					}
						 
					mGlobals.GFXFaces[i].mLTypes[NumLTypes]	=(byte)k;
					NumLTypes++;

					LDataOfs	=0;
//					pLData = LData;
//					geVec3d *pRGB = L.RGBLData[k];

					for(j=0;j < Size;j++)//, pRGB++)
					{
						Vector3	WorkRGB	=L.RGBLData[k][j] * mGlobals.LightScale;

						if(k == 0)
						{
							WorkRGB	+=mGlobals.MinLight;
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
						
						Max2	=Math.Min(Max, mGlobals.MaxLight);

						for(l=0;l < 3;l++)
						{
							LData[LDataOfs]	=(byte)(UtilityLib.Mathery.VecIdx(WorkRGB, l) * (Max2 / Max));
							LDataOfs++;
							mGlobals.LightOffset++;
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

			Print("Light Data Size      : " + mGlobals.LightOffset + "\n");

			Pos2	=f.BaseStream.Position;

			f.BaseStream.Seek(Pos1, SeekOrigin.Begin);

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_LIGHTDATA;
			Chunk.mElements =mGlobals.LightOffset;

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

			BinaryWriter	bw		=null;
			FileStream		file	=null;
			
			if(!LoadGBSPFile(fileName))
			{
				Print("LightGBSPFile:  Could not load GBSP file: " + fileName + "\n");
				return	false;
			}
			
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

//			if(!ConvertGFXEntDataToEntities())
//			{
//				goto	ExitWithError;
//			}

			file	=UtilityLib.FileUtil.OpenTitleFile(fileName,
									FileMode.OpenOrCreate, FileAccess.Write);

			if(file == null)
			{
				Print("LightGBSPFile:  Could not open GBSP file for writing: " + fileName + "\n");
				goto	ExitWithError;
			}
			bw	=new BinaryWriter(file);

			Print("Num Faces            : " + mGlobals.NumGFXFaces + "\n");

			//Build the patches (before direct lights are created)
//			if(mGlobals.DoRadiosity)
//			{
//				if(!BuildPatches())
//				{
//					goto	ExitWithError;
//				}
//			}

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

			/*
			if(mGlobals.DoRadiosity)
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
			}*/

			FinalizeRGBVerts();

			if(!StartWritingLight(bw))	//Open bsp file and save all current bsp data (except lightmaps)
			{
				goto	ExitWithError;
			}

			if(!SaveLightmaps(bw))	//Save them
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
			}
		}


		void CleanupLight()
		{
			FreeDirectLights();
			FreePatches();
			FreeLightmaps();
			FreeReceivers();

			mGlobals.VertNormals	=null;

			FreeGBSPFile();
		}


		void FreePatches()
		{
			if(mGlobals.PatchList != null)
			{
				for(int i=0;i < mGlobals.NumPatches;i++)
				{
					mGlobals.PatchList[i]	=null;
				}
				mGlobals.NumPatches	=0;
				mGlobals.PatchList	=null;
			}
			mGlobals.FacePatches	=null;
		}


		void FreeReceivers()
		{
			mGlobals.NumReceivers	=0;
		}


		void FreeLightmaps()
		{
			for(int i=0;i < mGlobals.NumGFXFaces;i++)
			{
				if(mGlobals.FaceInfo != null)
				{
					mGlobals.FaceInfo[i].Points	=null;
				}
				if(mGlobals.Lightmaps != null)
				{
					for(int k=0;k < GBSPGlobals.MAX_LTYPE_INDEX;k++)
					{
						mGlobals.Lightmaps[i].RGBLData[k]	=null;
					}
				}
			}

			mGlobals.FaceInfo	=null;
			mGlobals.Lightmaps	=null;
		}


		bool CreateDirectLights()
		{
			Int32		i, Leaf, Cluster;
			Vector3		Color;
			MapEntity	Entity;
			DirectLight	DLight;
			RADPatch	Patch;
			Vector3		Angles;
			Vector3		Angles2;
			Matrix		XForm;
			GFXTexInfo	pTexInfo;
			Int32		NumSurfLights;

			mGlobals.NumDirectLights	=0;
			NumSurfLights				=0;

			for(i=0;i < GBSPGlobals.MAX_DIRECT_CLUSTER_LIGHTS;i++)
			{
				mGlobals.DirectClusterLights[i]	=null;
			}

			// Create the entity lights first
			for(i=0;i < mGlobals.NumGFXEntData;i++)
			{
				Entity	=mGlobals.GFXEntData[i];

				if(!(Entity.mData.ContainsKey("light")
					|| Entity.mData.ContainsKey("_light")))
				{
					continue;
				}

				if(mGlobals.NumDirectLights + 1 >= GBSPGlobals.MAX_DIRECT_LIGHTS)
				{
					Print("*WARNING* Max lights.\n");
					goto	Done;
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

				//fix coordinate system
		//		DLight.mOrigin.X	=-DLight.mOrigin.X;
			//	float	temp		=DLight.mOrigin.Z;
				//DLight.mOrigin.Y	=DLight.mOrigin.Z;
		//		DLight.mOrigin.Z	=temp;

				DLight.mColor		=Color;
				DLight.mIntensity	=colorVec.W * mGlobals.EntityScale;
				DLight.mType		=DirectLight.DLight_Point;	//hardcode for now

				/*
				if(GetVectorForKey2 (Entity, "Angles", &Angles))
				{
					Angles2.X = (Angles.X / (geFloat)180) * GE_PI;
					Angles2.Y = (Angles.Y / (geFloat)180) * GE_PI;
					Angles2.Z = (Angles.Z / (geFloat)180) * GE_PI;

					geXForm3d_SetEulerAngles(&XForm, &Angles2);

					geXForm3d_GetLeft(&XForm, &Angles2);
					DLight.mNormal.X = -Angles2.X;
					DLight.mNormal.Y = -Angles2.Y;
					DLight.mNormal.Z = -Angles2.Z;

					DLight.mAngle = FloatForKey(Entity, "Arc");
					DLight.mAngle = (float)cos(DLight.mAngle/180.0f*GE_PI);
					
				}*/

				// Find out what type of light it is by it's classname...
//				if (!stricmp(Entity.mClassName, "Light"))
//					DLight.mType = DLight_Point;
//				else if (!stricmp(Entity.mClassName, "SpotLight"))
//					DLight.mType = DLight_Spot;
					

				Int32	nodeLandedIn	=FindLeafLandedIn(mGlobals.GFXModels[0].mRootNode[0], DLight.mOrigin);
				Leaf	=-(nodeLandedIn + 1);
				Cluster	=mGlobals.GFXLeafs[Leaf].mCluster;

				if(Cluster < 0)
				{
					Print("*WARNING* CreateLights:  Light in solid leaf.\n");
					continue;
				}
				
				if(Cluster >= GBSPGlobals.MAX_DIRECT_CLUSTER_LIGHTS)
				{
					Print("*WARNING* CreateLights:  Max cluster for light.\n");
					continue;
				}

				DLight.mNext	=mGlobals.DirectClusterLights[Cluster];
				mGlobals.DirectClusterLights[Cluster]	=DLight;

				mGlobals.DirectLights[mGlobals.NumDirectLights++]	=DLight;						
			}

			Print("Num Normal Lights   : " + mGlobals.NumDirectLights + "\n");

//			if (!DoRadiosity)		// Stop here if no radisosity is going to be done
				return	true;
			
			// Now create the radiosity direct lights (surface emitters)
			/*
			for (i=0; i< NumGFXFaces; i++)
			{
				pTexInfo = &GFXTexInfo[GFXFaces[i].TexInfo];

				// Only look at surfaces that want to emit light
				if (!(pTexInfo.mFlags & TEXINFO_LIGHT))
					continue;

				for (Patch = FacePatches[i]; Patch; Patch = Patch.mNext)
				{
					Leaf = Patch.mLeaf;
					Cluster = GFXLeafs[Leaf].Cluster;

					if (Cluster < 0)
						continue;			// Skip, solid

					if (Cluster >= MAX_DIRECT_CLUSTER_LIGHTS)
					{
						GHook.Printf("*WARNING* CreateLights:  Max cluster for surface light.\n");
						continue;
					}

					if (NumDirectLights+1 >= MAX_DIRECT_LIGHTS)
					{
						GHook.Printf("*WARNING* Max lights.\n");
						goto Done;
					}

					DLight = AllocDirectLight();

					if (!DLight)
						return GE_FALSE;

					DLight.mOrigin = Patch.mOrigin;
					DLight.mColor = Patch.mReflectivity;

					DLight.mNormal = Patch.mPlane.Normal;
					DLight.mType = DLight_Surface;
					
					DLight.mIntensity = pTexInfo.mFaceLight * Patch.mArea;
					// Make sure the emitter ends up with some light too
					geVec3d_AddScaled(&Patch.mRadFinal, &Patch.mReflectivity, DLight.mIntensity, &Patch.mRadFinal);

					// Insert this surface direct light into the list of lights
					DLight.mNext = DirectClusterLights[Cluster];
					DirectClusterLights[Cluster] = DLight;

					DirectLights[NumDirectLights++] = DLight;
					NumSurfLights++;
				}
			}*/

			Done:

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


		public bool LoadGBSPFile(string fileName)
		{
			FileStream	file	=UtilityLib.FileUtil.OpenTitleFile(fileName,
									FileMode.Open, FileAccess.Read);

			if(file == null)
			{
				return	false;
			}

			BinaryReader	br	=new BinaryReader(file);

			bool	bIsCPP	=IsCPPGenesis(br);

			int		LastGoodChunkType	=0;
			while(true)
			{
				GBSPChunk	chunk	=new GBSPChunk();
				if(!chunk.Read(br, mGlobals, bIsCPP))
				{
					Print("Chunk read failed.  Last good chunk type was " + LastGoodChunkType + "\n");
					br.Close();
					file.Close();
					return	false;
				}
				LastGoodChunkType	=chunk.mType;

				if(chunk.mType == GBSPChunk.GBSP_CHUNK_END)
				{
					break;
				}
			}

			br.Close();
			file.Close();

			//make clustervisframe
			ClusterVisFrame	=new int[mGlobals.NumGFXClusters];
			NodeParents		=new int[mGlobals.NumGFXNodes];
			NodeVisFrame	=new int[mGlobals.NumGFXNodes];
			LeafData		=new WorldLeaf[mGlobals.NumGFXLeafs];

			//fill in leafdata with blank worldleafs
			for(int i=0;i < mGlobals.NumGFXLeafs;i++)
			{
				LeafData[i]	=new WorldLeaf();
			}

			FindParents(mGlobals.GFXModels[0].mRootNode[0]);

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
			if(!LoadGBSPFile(fileName))
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
			if(!LoadPortalFile(PFile))
			{
				goto	ExitWithError;
			}

//			string	compFile	=PFile;

//			int	sIdx	=compFile.LastIndexOf('s');

//			compFile	=compFile.Substring(0, sIdx);
//			compFile	+=".gpf";

//			ComparePortalFiles(PFile, compFile);

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

		void ComparePortalFiles(string pfile1, string pfile2)
		{
			Int32		LeafFrom, LeafTo;
			VISPortal	pPortal;
			VISLeaf		pLeaf;
			GBSPPoly	pPoly;
			Int32		i, NumVerts;
			string		TAG;

			pPoly	=null;

			FileStream	fs1	=UtilityLib.FileUtil.OpenTitleFile(pfile1,
				FileMode.Open, FileAccess.Read);
			FileStream	fs2	=UtilityLib.FileUtil.OpenTitleFile(pfile2,
				FileMode.Open, FileAccess.Read);

			BinaryReader	br1	=new BinaryReader(fs1);
			BinaryReader	br2	=new BinaryReader(fs2);
			
			// 
			//	Check the TAG
			//
			TAG	=new string(br2.ReadChars(12));
			TAG	=br1.ReadString();
			if(TAG != "GBSP_PRTFILE")
			{
				Print("LoadPortalFile:  " + pfile1 + " is not a GBSP Portal file.\n");
				goto	ExitWithError;
			}

			//
			//	Get the number of portals
			//
			mGlobals.NumVisPortals	=br1.ReadInt32();
			mGlobals.NumVisPortals	=br2.ReadInt32();
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
			mGlobals.NumVisLeafs	=br1.ReadInt32();
			mGlobals.NumVisLeafs	=br2.ReadInt32();
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

				NumVerts	=br1.ReadInt32();
				NumVerts	=br2.ReadInt32();

				pPoly	=new GBSPPoly();

				for(int j=0;j < NumVerts;j++)
				{
					Vector3	vert1, vert2;
					vert1.X	=br1.ReadSingle();
					vert1.Y	=br1.ReadSingle();
					vert1.Z	=br1.ReadSingle();
					vert2.X	=br2.ReadSingle();
					vert2.Y	=br2.ReadSingle();
					vert2.Z	=br2.ReadSingle();

					if(!UtilityLib.Mathery.CompareVector(vert1, vert2))
					{
						int	gack	=0;
						gack++;
					}

					pPoly.mVerts.Add(vert1);
				}

				LeafFrom	=br1.ReadInt32();
				int	blah1	=br2.ReadInt32();
				LeafTo		=br1.ReadInt32();
				int blah2	=br2.ReadInt32();

				if(LeafFrom != blah1 || LeafTo != blah2)
				{
					int	barkspawn	=0;
					barkspawn++;
				}
				
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

			br1.Close();
			fs1.Close();
			br1	=null;
			fs1	=null;
			br2.Close();
			fs2.Close();
			br2	=null;
			fs2	=null;

			return;

			// ==== ERROR ===
			ExitWithError:
			{
				mGlobals.VisPortals			=null;
				mGlobals.VisSortedPortals	=null;
				mGlobals.VisLeafs			=null;
				pPoly						=null;
			}
		}


		bool FinishWritingVis(BinaryWriter bw)
		{
			if(!SaveVisdGFXLeafs(bw))
			{
				return	false;
			}
			if(!SaveVisdGFXClusters(bw))
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
				if(!FloodPortalsSlow(portIndexer))
				{
					return	false;
				}
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


		bool FloodPortalsSlow(Dictionary<VISPortal, Int32> visIndexer)
		{
			VISPortal	Portal;
			Int32		PNum;
			VISPStack	PStack	=new VISPStack();
			Int32		i, k;

			for(k=0;k < mGlobals.NumVisPortals;k++)
			{
				mGlobals.VisPortals[k].mDone	=false;
			}

			for(k=0;k < mGlobals.NumVisPortals;k++)
			{
				Portal	=mGlobals.VisSortedPortals[k];
				
				Portal.mFinalVisBits	=new byte[mGlobals.NumVisPortalBytes];

				//This portal can't see anyone yet...
				for(i=0;i < mGlobals.NumVisPortalBytes;i++)
				{
					Portal.mFinalVisBits[i]	=0;
				}
				for(i=0;i < mGlobals.NumVisPortals;i++)
				{
					mGlobals.PortalSeen[i]	=0;
				}

				mGlobals.CanSee	=0;
				
				for(i=0;i < mGlobals.NumVisPortalBytes;i++)
				{
					PStack.mVisBits[i]	=Portal.mVisBits[i];
				}

				//Setup Source/Pass
				PStack.mSource	=new GBSPPoly(Portal.mPoly);
				PStack.mPass	=null;

				if(!Portal.FloodPortalsSlow_r(mGlobals, Portal, PStack, visIndexer))
				{
					return	false;
				}

				PStack.mSource	=null;
				Portal.mDone	=true;

				PNum	=visIndexer[Portal];

				if(mGlobals.VisVerbose)
				{
					Print("Portal: " + (k + 1) + " - Fast Vis: "
						+ Portal.mMightSee + ", Full Vis: "
						+ Portal.mCanSee + "\n");
				}
			}			
			return	true;
		}


		bool CollectLeafVisBits(int LeafNum)
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

			FileStream	fs	=UtilityLib.FileUtil.OpenTitleFile("PortalFloods.txt",
				FileMode.Create, FileAccess.Write);

			BinaryWriter	bw	=new BinaryWriter(fs);

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
				
				Portal.FloodPortalsFast_r(mGlobals, Portal, visIndexer, bw);
			}

			bw.Close();
			fs.Close();
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
			if(!SaveVisdGFXPlanes(bw))
			{
				return	false;
			}
			if(!SaveVisdGFXFaces(bw))
			{
				return	false;
			}
			if(!SaveGFXAreasAndPortals(bw))
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
			if(!SaveGFXRGBVerts(bw))
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
			if(!SaveGFXLightData(bw))
			{
				return	false;
			}
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

			FileStream	dbgFile	=UtilityLib.FileUtil.OpenTitleFile("PrtPlanes.txt",
				FileMode.Create, FileAccess.Write);

			BinaryWriter	bw	=new BinaryWriter(dbgFile);

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

				bw.Write(pPortal.mPlane.mNormal.X);
				bw.Write(pPortal.mPlane.mNormal.Y);
				bw.Write(pPortal.mPlane.mNormal.Z);
				bw.Write(pPortal.mPlane.mDist);

				pPortal.CalcPortalInfo();
			}
			
			mGlobals.NumVisLeafBytes	=((mGlobals.NumVisLeafs+63)&~63) >> 3;
			mGlobals.NumVisPortalBytes	=((mGlobals.NumVisPortals+63)&~63) >> 3;

			mGlobals.NumVisPortalLongs	=mGlobals.NumVisPortalBytes / sizeof(UInt32);
			mGlobals.NumVisLeafLongs	=mGlobals.NumVisLeafBytes / sizeof(UInt32);

			bw.Close();
			dbgFile.Close();
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


		Int32	FindLeafLandedIn(Int32 node, Vector3 pos)
		{
			float		Dist1;
			GFXNode		pNode;
			Int32		Side;

			if(node < 0)		// At leaf, no more recursing
			{
				return	node;
			}

			pNode	=mGlobals.GFXNodes[node];
			
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
			Int32	ret	=0;
			ret	=FindLeafLandedIn(pNode.mChildren[Side], pos);
			if(ret < 0)
			{
				return	ret;
			}
			ret	=FindLeafLandedIn(pNode.mChildren[(Side == 0)? 1 : 0], pos);
			return	ret;
		}


		void GatherBSPTriangles(Int32 Node, Vector3 pos,
			List<Vector3> verts, List<uint> indexes, bool bCheck)
		{
			Int32	nodeLandedIn	=FindLeafLandedIn(Node, pos);
			Int32	Leaf;

			Leaf	=-(nodeLandedIn + 1);

			Debug.Assert(Leaf >= 0 && Leaf < mGlobals.NumGFXLeafs);

			int	clust	=mGlobals.GFXLeafs[Leaf].mCluster;
			int	visOfs	=mGlobals.GFXClusters[clust].mVisOfs;

			//mGlobals.GFXVisData[visOfs]
		}


		bool VisWorld(Int32 rootNode, Vector3 pos)
		{
			Int32	k, i, Area;
			Int32	Leaf, Cluster;
			GFXLeaf	pLeaf;

			Int32	node	=FindLeafLandedIn(rootNode, -pos);

			Leaf	=-(node + 1);
			Area	=mGlobals.GFXLeafs[Leaf].mArea;

			CurrentLeaf	=Leaf;
			CurFrameStatic++;			// Make all old vis info obsolete

			Cluster	=mGlobals.GFXLeafs[Leaf].mCluster;

			if(Cluster == -1 || mGlobals.GFXClusters[Cluster].mVisOfs == -1)
			{
				return	true;
			}

			/*
			if (Area)
				Vis_FloodAreas_r(World, Area);

			World->VisInfo = GE_TRUE;
			*/

			//VisData = &GFXVisData[GFXClusters[Cluster].VisOfs];

			int	ofs	=mGlobals.GFXClusters[Cluster].mVisOfs;

			// Mark all visible clusters
			for(i=0;i < mGlobals.GFXModels[0].mNumClusters;i++)
			{
				if((mGlobals.GFXVisData[ofs + (i >> 3)] & (1 << (i & 7))) != 0)
				{
					ClusterVisFrame[i]	=CurFrameStatic;
				}
			}

			//Go through and find all visible leafs based on the visible clusters the leafs are in
			for(i=0;i < mGlobals.GFXModels[0].mNumLeafs;i++)
			{
				pLeaf	=mGlobals.GFXLeafs[mGlobals.GFXModels[0].mFirstLeaf + i];
				Int32	pFace;

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
			Debug.Assert(Leaf < mGlobals.NumGFXLeafs);

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

//			if(NodeVisFrame[Node] != CurFrameStatic)
//			{
//				return;
//			}

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


		void FindParents(Int32 root)
		{
			FindParents_r(root, -1);
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
			FindParents_r(mGlobals.GFXNodes[Node].mChildren[0], Node);
			FindParents_r(mGlobals.GFXNodes[Node].mChildren[1], Node);
		}


		void FreeFileVisData()
		{
			mGlobals.GFXVisData		=null;
			mGlobals.NumGFXVisData	=0;
		}
	}
}
