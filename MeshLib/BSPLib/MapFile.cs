using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Text;

namespace BSPLib
{
	public partial class Map
	{
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
				gtex.mMaterial			=tex.mMaterial;
				gtex.mVecs[0]			=tex.mUVec;
				gtex.mVecs[1]			=tex.mVVec;

				gtex.Write(bw);
			}
			return	true;
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


		bool SaveGFXLightData(BinaryWriter bw)
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


		bool SaveGFXVertIndexList(BinaryWriter bw)
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


		bool SaveGFXVisData(BinaryWriter bw)
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




		bool SaveVisdGFXPlanes(BinaryWriter bw)
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


		bool SaveVisdGFXFaces(BinaryWriter bw)
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


		bool SaveGFXPortals(BinaryWriter bw)
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


		bool SaveGFXBNodes(BinaryWriter bw)
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


		bool SaveGFXAreasAndPortals(BinaryWriter bw)
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


		bool SaveGFXClusters(BinaryWriter bw)
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


		bool SaveVisdGFXClusters(BinaryWriter bw)
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


		bool SaveVisdGFXLeafs(BinaryWriter bw)
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


		bool SaveGFXLeafs(BinaryWriter bw, NodeCounter nc)
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
		
		
		bool SaveGFXLeafs(BinaryWriter bw)
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


		bool SaveVisdGFXLeafFacesAndSides(BinaryWriter bw)
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


		bool SaveVisdGFXNodes(BinaryWriter bw)
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


		bool SaveVisdGFXTexInfos(BinaryWriter bw)
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


		bool SaveGFXModelData(BinaryWriter bw)
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


		bool SaveGFXLeafSides(BinaryWriter bw)
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


		bool SaveGFXNodes(BinaryWriter bw, NodeCounter nc)
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


		bool SaveGFXFaces(BinaryWriter bw, NodeCounter nc)
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


		bool SaveEmptyGFXClusters(BinaryWriter bw, NodeCounter nc)
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


		bool SaveGFXVerts(BinaryWriter bw)
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


		bool SaveGFXRGBVerts(BinaryWriter bw)
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
	}
}
