using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BSPZone
{
	public class Zone
	{
		class WorldLeaf
		{
			public Int32	mVisFrame;
			public Int32	mParent;
		}

		ZoneModel	[]mZoneModels;
		ZoneNode	[]mZoneNodes;
		ZoneLeaf	[]mZoneLeafs;
		ZonePlane	[]mZonePlanes;
		ZoneEntity	[]mZoneEntities;

		VisCluster		[]mVisClusters;
		VisArea			[]mVisAreas;
		VisAreaPortal	[]mVisAreaPortals;

		byte	[]mVisData;
		byte	[]mMaterialVisData;

		//vis stuff
		Int32		mCurrentLeaf;
		Int32		mCurFrameStatic;
		Int32		[]mClusterVisFrame;
		WorldLeaf	[]mLeafData;
		Int32		[]mNodeParents;
		Int32		[]mNodeVisFrame;

		int	mLightMapGridSize;
		int	mNumVisLeafBytes;
		int	mNumVisMaterialBytes;


		public void Write(string fileName)
		{
			FileStream	file	=UtilityLib.FileUtil.OpenTitleFile(fileName,
									FileMode.OpenOrCreate, FileAccess.Write);

			BinaryWriter	bw	=new BinaryWriter(file);

			UtilityLib.FileUtil.WriteArray(mZoneModels, bw);
			UtilityLib.FileUtil.WriteArray(mZoneNodes, bw);
			UtilityLib.FileUtil.WriteArray(mZoneLeafs, bw);
			UtilityLib.FileUtil.WriteArray(mVisClusters, bw);
			UtilityLib.FileUtil.WriteArray(mVisAreas, bw);
			UtilityLib.FileUtil.WriteArray(mVisAreaPortals, bw);
			UtilityLib.FileUtil.WriteArray(mZonePlanes, bw);
			UtilityLib.FileUtil.WriteArray(mZoneEntities, bw);
			UtilityLib.FileUtil.WriteArray(mVisData, bw);
			UtilityLib.FileUtil.WriteArray(mMaterialVisData, bw);
			bw.Write(mLightMapGridSize);
			bw.Write(mNumVisLeafBytes);
			bw.Write(mNumVisMaterialBytes);

			bw.Close();
			file.Close();
		}


		public void Read(string fileName)
		{
			FileStream	file	=UtilityLib.FileUtil.OpenTitleFile(fileName,
									FileMode.Open, FileAccess.Read);

			BinaryReader	br	=new BinaryReader(file);

			mZoneModels		=UtilityLib.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return UtilityLib.FileUtil.InitArray<ZoneModel>(count); }) as ZoneModel[];
			mZoneNodes		=UtilityLib.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return UtilityLib.FileUtil.InitArray<ZoneNode>(count); }) as ZoneNode[];
			mZoneLeafs		=UtilityLib.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return UtilityLib.FileUtil.InitArray<ZoneLeaf>(count); }) as ZoneLeaf[];
			mVisClusters	=UtilityLib.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return UtilityLib.FileUtil.InitArray<VisCluster>(count); }) as VisCluster[];
			mVisAreas		=UtilityLib.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return UtilityLib.FileUtil.InitArray<VisArea>(count); }) as VisArea[];
			mVisAreaPortals	=UtilityLib.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return UtilityLib.FileUtil.InitArray<VisAreaPortal>(count); }) as VisAreaPortal[];
			mZonePlanes		=UtilityLib.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return UtilityLib.FileUtil.InitArray<ZonePlane>(count); }) as ZonePlane[];
			mZoneEntities	=UtilityLib.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return UtilityLib.FileUtil.InitArray<ZoneEntity>(count); }) as ZoneEntity[];

			mVisData			=UtilityLib.FileUtil.ReadByteArray(br);
			mMaterialVisData	=UtilityLib.FileUtil.ReadByteArray(br);

			mLightMapGridSize		=br.ReadInt32();
			mNumVisLeafBytes		=br.ReadInt32();
			mNumVisMaterialBytes	=br.ReadInt32();

			//make clustervisframe
			mClusterVisFrame	=new int[mVisClusters.Length];
			mNodeParents		=new int[mZoneNodes.Length];
			mNodeVisFrame		=new int[mZoneNodes.Length];
			mLeafData			=new WorldLeaf[mZoneLeafs.Length];

			//fill in leafdata with blank worldleafs
			for(int i=0;i < mZoneLeafs.Length;i++)
			{
				mLeafData[i]	=new WorldLeaf();
			}

			FindParents_r(mZoneModels[0].mRootNode[0], -1);

			br.Close();
			file.Close();
		}


		void FindParents_r(Int32 Node, Int32 Parent)
		{
			if(Node < 0)		// At a leaf, mark leaf parent and return
			{
				mLeafData[-(Node+1)].mParent	=Parent;
				return;
			}

			//At a node, mark node parent, and keep going till hitting a leaf
			mNodeParents[Node]	=Parent;

			// Go down front and back markinf parents on the way down...
			FindParents_r(mZoneNodes[Node].mChildren[0], Node);
			FindParents_r(mZoneNodes[Node].mChildren[1], Node);
		}
	}
}
