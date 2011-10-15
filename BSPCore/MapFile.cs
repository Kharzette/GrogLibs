using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework;
using BSPCore;


namespace BSPCore
{
	public partial class Map
	{
		public GFXHeader LoadGBSPFile(string fileName)
		{
			FileStream	file	=new FileStream(fileName, FileMode.Open, FileAccess.Read);
			if(file == null)
			{
				return	null;
			}

			BinaryReader	br	=new BinaryReader(file);

			//read header
			GFXHeader	header	=new GFXHeader();
			header.Read(br);

			if(header.mTag != 0x47425350)	//"GBSP"
			{
				return	null;
			}

			//read regular bsp crap
			mGFXModels		=Utility64.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return Utility64.FileUtil.InitArray<GFXModel>(count); }) as GFXModel[];
			mGFXNodes		=Utility64.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return Utility64.FileUtil.InitArray<GFXNode>(count); }) as GFXNode[];
			mGFXLeafs		=Utility64.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return Utility64.FileUtil.InitArray<GFXLeaf>(count); }) as GFXLeaf[];

			LoadGFXLeafFaces(br);

			mGFXClusters	=Utility64.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return Utility64.FileUtil.InitArray<GFXCluster>(count); }) as GFXCluster[];
			mGFXAreas		=Utility64.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return Utility64.FileUtil.InitArray<GFXArea>(count); }) as GFXArea[];
			mGFXAreaPortals	=Utility64.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return Utility64.FileUtil.InitArray<GFXAreaPortal>(count); }) as GFXAreaPortal[];
			mGFXLeafSides	=Utility64.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return Utility64.FileUtil.InitArray<GFXLeafSide>(count); }) as GFXLeafSide[];
			mGFXFaces		=Utility64.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return Utility64.FileUtil.InitArray<GFXFace>(count); }) as GFXFace[];
			mGFXPlanes		=Utility64.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return Utility64.FileUtil.InitArray<GFXPlane>(count); }) as GFXPlane[];

			LoadGFXVerts(br);
			LoadGFXVertIndexes(br);

			mGFXTexInfos	=Utility64.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return Utility64.FileUtil.InitArray<GFXTexInfo>(count); }) as GFXTexInfo[];
			mGFXEntities	=Utility64.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return Utility64.FileUtil.InitArray<MapEntity>(count); }) as MapEntity[];

			if(header.mbHasVis)
			{
				LoadGFXVisData(br);
			}
			if(header.mbHasMaterialVis)
			{
				LoadGFXMaterialVisData(br);
			}
			if(header.mbHasLight)
			{
				LoadGFXRGBVerts(br);
				LoadGFXLightData(br);
			}

			br.Close();
			file.Close();

			Print("Load complete\n");

			return	header;
		}


		void FreeGBSPFile()
		{
			mGFXModels			=null;
			mGFXNodes			=null;
			mGFXLeafs			=null;
			mGFXClusters		=null;		// CHANGE: CLUSTER
			mGFXAreas			=null;
			mGFXPlanes			=null;
			mGFXFaces			=null;
			mGFXLeafFaces		=null;
			mGFXLeafSides		=null;
			mGFXVerts			=null;
			mGFXVertIndexes		=null;
			mGFXRGBVerts		=null;
			mGFXEntities		=null;			
			mGFXTexInfos		=null;
			mGFXLightData		=null;
			mGFXVisData			=null;
			mGFXMaterialVisData	=null;
		}


		void WriteVis(BinaryWriter bw, bool bHasLight, bool bMaterialVis)
		{
			GFXHeader	header	=new GFXHeader();

			header.mTag				=0x47425350;	//"GBSP"
			header.mbHasLight		=bHasLight;
			header.mbHasVis			=true;
			header.mbHasMaterialVis	=bMaterialVis;
			header.Write(bw);

			SaveGFXModelData(bw);
			SaveVisdGFXNodes(bw);
			SaveVisdGFXLeafs(bw);
			SaveVisdGFXLeafFaces(bw);
			SaveVisdGFXClusters(bw);
			SaveGFXAreasAndPortals(bw);
			SaveVisdGFXLeafSides(bw);
			SaveVisdGFXFaces(bw);
			SaveGFXPlanes(bw);
			SaveGFXVerts(bw);
			SaveGFXVertIndexes(bw);
			SaveGFXTexInfos(bw);
			SaveGFXEntData(bw);

			SaveGFXVisData(bw);
			if(bMaterialVis)
			{
				SaveGFXMaterialVisData(bw);
			}
			if(bHasLight)
			{
				SaveGFXRGBVerts(bw);
				SaveGFXLightData(bw);
			}
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
			bw.Write(mLightMapGridSize);
			bw.Write(mGFXLightData.Length);
			bw.Write(mGFXLightData, 0, mGFXLightData.Length);
		}


		void LoadGFXLightData(BinaryReader br)
		{
			mLightMapGridSize	=br.ReadInt32();
			int	count			=br.ReadInt32();
			mGFXLightData		=br.ReadBytes(count);
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
