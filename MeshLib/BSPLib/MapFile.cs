using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public partial class Map
	{
		public delegate IReadWriteable[] CreateRWArray(Int32 count);

		void SaveArray(IReadWriteable []arr, BinaryWriter bw)
		{
			bw.Write(arr.Length);
			foreach(IReadWriteable obj in arr)
			{
				obj.Write(bw);
			}
		}


		object[] LoadArray(BinaryReader br, CreateRWArray crwa)
		{
			int	count	=br.ReadInt32();

			IReadWriteable	[]arr	=crwa(count);

			for(int i=0;i < count;i++)
			{
				arr[i].Read(br);
			}
			return	arr;
		}


		void SaveGFXEntDataList(BinaryWriter bw)
		{
			bw.Write(mEntities.Count);
			foreach(MapEntity me in mEntities)
			{
				me.Write(bw);
			}
		}

		ArrayType []InitArray<ArrayType>(int count) where ArrayType : new()
		{
			ArrayType	[]ret	=new ArrayType[count];
			for(int i=0;i < count;i++)
			{
				ret[i]	=new ArrayType();
			}
			return	ret;
		}


		void SaveGFXEntData(BinaryWriter bw)
		{
			bw.Write(mGFXEntities.Length);
			foreach(MapEntity me in mGFXEntities)
			{
				me.Write(bw);
			}
		}


		void SaveGFXLightData(BinaryWriter bw)
		{
			bw.Write(mGFXLightData.Length);
			bw.Write(mGFXLightData, 0, mGFXLightData.Length);
		}


		void LoadGFXLightData(BinaryReader br)
		{
			int	count		=br.ReadInt32();
			mGFXLightData	=br.ReadBytes(count);
		}


		void SaveGFXVertIndexes(BinaryWriter bw)
		{
			bw.Write(mGFXVertIndexes.Length);
			foreach(Int32 vi in mGFXVertIndexes)
			{
				bw.Write(vi);
			}
		}


		void LoadGFXVertIndexes(BinaryReader br)
		{
			int	count	=br.ReadInt32();

			mGFXVertIndexes	=new Int32[count];
			for(int i=0;i < count;i++)
			{
				int	idx	=br.ReadInt32();

				mGFXVertIndexes[i]	=idx;
			}
		}


		void SaveGFXVisData(BinaryWriter bw)
		{
			bw.Write(mGFXVisData.Length);
			bw.Write(mGFXVisData, 0, mGFXVisData.Length);
		}


		void LoadGFXVisData(BinaryReader br)
		{
			int	count	=br.ReadInt32();
			mGFXVisData	=br.ReadBytes(count);
		}


		void SaveGFXMaterialVisData(BinaryWriter bw)
		{
			bw.Write(mGFXMaterialVisData.Length);
			bw.Write(mGFXMaterialVisData, 0, mGFXMaterialVisData.Length);
		}


		void LoadGFXMaterialVisData(BinaryReader br)
		{
			int	count			=br.ReadInt32();
			mGFXMaterialVisData	=br.ReadBytes(count);
		}


		void SaveGFXPlanes(BinaryWriter bw)
		{
			bw.Write(mGFXPlanes.Length);
			foreach(GFXPlane gp in mGFXPlanes)
			{
				gp.Write(bw);
			}
		}


		void SaveVisdGFXFaces(BinaryWriter bw)
		{
			bw.Write(mGFXFaces.Length);
			foreach(GFXFace f in mGFXFaces)
			{
				f.Write(bw);
			}
		}


		void SaveGFXAreasAndPortals(BinaryWriter bw)
		{
			bw.Write(mGFXAreas.Length);
			foreach(GFXArea a in mGFXAreas)
			{
				a.Write(bw);
			}

			bw.Write(mGFXAreaPortals.Length);
			foreach(GFXAreaPortal ap in mGFXAreaPortals)
			{
				ap.Write(bw);
			}
		}


		void SaveGFXClusters(BinaryWriter bw)
		{
			bw.Write(mGFXClusters.Length);
			foreach(GFXCluster clust in mGFXClusters)
			{
				clust.Write(bw);
			}
		}


		void SaveVisdGFXClusters(BinaryWriter bw)
		{
			bw.Write(mGFXClusters.Length);
			foreach(GFXCluster gc in mGFXClusters)
			{
				gc.Write(bw);
			}
		}


		void SaveVisdGFXLeafs(BinaryWriter bw)
		{
			bw.Write(mGFXLeafs.Length);
			foreach(GFXLeaf leaf in mGFXLeafs)
			{
				leaf.Write(bw);
			}
		}


		void SaveGFXLeafs(BinaryWriter bw, NodeCounter nc)
		{
			bw.Write(nc.mNumGFXLeafs);

			int	TotalLeafSize	=0;

			List<Int32>	gfxLeafFaces	=new List<Int32>();

			foreach(GBSPModel mod in mModels)
			{
				//Save all the leafs for this model
				if(!mod.SaveGFXLeafs_r(bw, gfxLeafFaces, ref TotalLeafSize))
				{
					Map.Print("SaveGFXLeafs:  SaveGFXLeafs_r failed.\n");
					return;
				}
			}

			mGFXLeafFaces	=gfxLeafFaces.ToArray();

			bw.Write(nc.mNumGFXLeafFaces);
			foreach(Int32 leafFace in mGFXLeafFaces)
			{
				bw.Write(leafFace);
			}
		}
		
		
		void LoadGFXLeafFaces(BinaryReader br)
		{
			int	count	=br.ReadInt32();

			mGFXLeafFaces	=new Int32[count];
			for(int i=0;i < count;i++)
			{
				Int32	lf	=br.ReadInt32();
				mGFXLeafFaces[i]	=lf;
			}
		}


		void SaveGFXLeafs(BinaryWriter bw)
		{
			bw.Write(mGFXLeafs.Length);
			foreach(GFXLeaf leaf in mGFXLeafs)
			{
				leaf.Write(bw);
			}
		}


		void SaveVisdGFXLeafFaces(BinaryWriter bw)
		{
			bw.Write(mGFXLeafFaces.Length);
			foreach(Int32 lf in mGFXLeafFaces)
			{
				bw.Write(lf);
			}
		}


		void SaveVisdGFXLeafSides(BinaryWriter bw)
		{
			bw.Write(mGFXLeafSides.Length);
			foreach(GFXLeafSide ls in mGFXLeafSides)
			{
				ls.Write(bw);
			}
		}


		void SaveVisdGFXNodes(BinaryWriter bw)
		{
			bw.Write(mGFXNodes.Length);
			foreach(GFXNode gn in mGFXNodes)
			{
				gn.Write(bw);
			}
		}


		void SaveGFXTexInfos(BinaryWriter bw)
		{
			bw.Write(mGFXTexInfos.Length);
			foreach(GFXTexInfo tex in mGFXTexInfos)
			{
				tex.Write(bw);
			}
		}


		void SaveGFXModelDataFromList(BinaryWriter bw)
		{
			bw.Write(mModels.Count);
			foreach(GBSPModel mod in mModels)
			{
				mod.ConvertToGFXAndSave(bw);
			}			
		}


		void SaveGFXModelData(BinaryWriter bw)
		{
			bw.Write(mGFXModels.Length);
			foreach(GFXModel gmod in mGFXModels)
			{
				gmod.Write(bw);
			}
		}


		void SaveGFXLeafSides(BinaryWriter bw)
		{
			bw.Write(mGFXLeafSides.Length);
			foreach(GFXLeafSide ls in mGFXLeafSides)
			{
				ls.Write(bw);
			}
		}


		void SaveGFXNodes(BinaryWriter bw, NodeCounter nc)
		{
			bw.Write(nc.mNumGFXNodes);			
			foreach(GBSPModel mod in mModels)
			{
				if(!mod.SaveGFXNodes_r(bw))
				{
					return;
				}
			}
		}


		void SaveGFXFaces(BinaryWriter bw, NodeCounter nc)
		{
			bw.Write(nc.mNumGFXFaces);

			foreach(GBSPModel mod in mModels)
			{
				mod.SaveGFXFaces_r(bw);
			}
		}


		void SaveEmptyGFXClusters(BinaryWriter bw, NodeCounter nc)
		{
			bw.Write(nc.mNumLeafClusters);

			GFXCluster	GCluster	=new GFXCluster();

			for(int i=0;i < nc.mNumLeafClusters;i++)
			{
				GCluster.mVisOfs	=-1;

				GCluster.Write(bw);
			}
		}


		void SaveGFXVerts(BinaryWriter bw)
		{
			bw.Write(mGFXVerts.Length);
			foreach(Vector3 vert in mGFXVerts)
			{
				bw.Write(vert.X);
				bw.Write(vert.Y);
				bw.Write(vert.Z);
			}
		}


		void LoadGFXVerts(BinaryReader br)
		{
			int	count	=br.ReadInt32();

			mGFXVerts	=new Vector3[count];
			for(int i=0;i < count;i++)
			{
				Vector3	vert	=Vector3.Zero;
				vert.X	=br.ReadSingle();
				vert.Y	=br.ReadSingle();
				vert.Z	=br.ReadSingle();

				mGFXVerts[i]	=vert;
			}
		}


		void SaveGFXRGBVerts(BinaryWriter bw)
		{
			bw.Write(mGFXRGBVerts.Length);
			foreach(Vector3 vert in mGFXRGBVerts)
			{
				bw.Write(vert.X);
				bw.Write(vert.Y);
				bw.Write(vert.Z);
			}
		}


		void LoadGFXRGBVerts(BinaryReader br)
		{
			int	count	=br.ReadInt32();

			mGFXRGBVerts	=new Vector3[count];
			for(int i=0;i < count;i++)
			{
				Vector3	vert	=Vector3.Zero;
				vert.X	=br.ReadSingle();
				vert.Y	=br.ReadSingle();
				vert.Z	=br.ReadSingle();

				mGFXRGBVerts[i]	=vert;
			}
		}
	}
}
