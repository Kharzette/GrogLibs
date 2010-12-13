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


	public partial class Map
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

		//method delegates
		internal delegate GBSPModel ModelForLeafNode(GBSPNode n);
		internal delegate bool IsPointInSolid(Vector3 pos);
		internal delegate bool RayCollision(Vector3 front, Vector3 back, ref Vector3 Impacto);
		internal delegate Int32 GetNodeLandedIn(Int32 node, Vector3 pos);

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


		internal GBSPModel GetModelForLeafNode(GBSPNode Node)
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
			if(!SaveGFXNodes(bw, nc))
			{
				return	false;
			}
			if(!SaveGFXLeafs(bw, nc))
			{
				Map.Print("ConvertGBSPToFile:  SaveGFXLeafs failed.\n");
				return	false;
			}
			if(!SaveEmptyGFXClusters(bw, nc))
			{
				return	false;
			}

			//set gfx area stuff from lists
			mGFXAreas		=mAreas.ToArray();
			mGFXAreaPortals	=mAreaPorts.ToArray();

			if(!SaveGFXAreasAndPortals(bw))
			{
				return	false;
			}
			if(!SaveGFXLeafSides(bw))
			{
				return	false;
			}
			if(!SaveGFXFaces(bw, nc))
			{
				return	false;
			}

			mGFXPlanes	=mPlanePool.GetGFXArray();

			if(!SaveVisdGFXPlanes(bw))
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

			FreeGBSPFile();

			return	true;
		}


		void FreeGBSPFile()
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


		bool MakeVertNormals()
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


		bool IsPointInSolidSpace(Vector3 pos)
		{
			Int32	node	=FindNodeLandedIn(0, pos);

			Int32	Leaf	=-(node + 1);

			return	((mGFXLeafs[Leaf].mContents & Contents.BSP_CONTENTS_SOLID2) != 0);
		}


		bool RayIntersect(Vector3 Front, Vector3 Back, Int32 Node, ref Vector3 intersectionPoint, ref bool hitLeaf)
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
				return(RayIntersect(Front, Back, n.mChildren[0], ref intersectionPoint, ref hitLeaf));
			}
			if(Fd < 1 && Bd < 1)
			{
				return(RayIntersect(Front, Back, n.mChildren[1], ref intersectionPoint, ref hitLeaf));
			}

			Side	=(Fd < 0)? 1 : 0;
			Dist	=Fd / (Fd - Bd);

			I	=Front + Dist * (Back - Front);

			//Work our way to the front, from the back side.  As soon as there
			//is no more collisions, we can assume that we have the front portion of the
			//ray that is in empty space.  Once we find this, and see that the back half is in
			//solid space, then we found the front intersection point...
			if(RayIntersect(Front, I, n.mChildren[Side], ref intersectionPoint, ref hitLeaf))
			{
				return	true;
			}
			else if(RayIntersect(I, Back, n.mChildren[(Side == 0)? 1 : 0], ref intersectionPoint, ref hitLeaf))
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


		bool RayCollide(Vector3 Front, Vector3 Back, ref Vector3 I)
		{
			bool	hitLeaf	=false;
			if(RayIntersect(Front, Back, mGFXModels[0].mRootNode[0], ref I, ref hitLeaf))
			{
				return	true;
			}
			return	false;
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


		Int32	FindNodeLandedIn(Int32 node, Vector3 pos)
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
			ret	=FindNodeLandedIn(pNode.mChildren[Side], pos);
			if(ret < 0)
			{
				return	ret;
			}
			ret	=FindNodeLandedIn(pNode.mChildren[(Side == 0)? 1 : 0], pos);
			return	ret;
		}


		bool VisWorld(Int32 rootNode, Vector3 pos)
		{
			Int32	i, Area;
			Int32	Leaf, Cluster;
			GFXLeaf	pLeaf;

			Int32	node	=FindNodeLandedIn(rootNode, -pos);

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
	}
}
