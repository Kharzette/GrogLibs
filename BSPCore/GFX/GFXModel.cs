using System;
using System.Numerics;
using System.IO;


namespace BSPCore
{
	public class GFXModel
	{
		public Int32		mRootNode;				// Top level Node in GFXNodes/GFXBNodes
		public Vector3		mMins;
		public Vector3		mMaxs;
		public Vector3		mOrigin;				// Center of model
		public Int32		mFirstFace;				// First face in GFXFaces
		public Int32		mNumFaces;				// Number of faces
		public Int32		mFirstLeaf;				// First leaf in GFXLeafs;
		public Int32		mNumLeafs;				// Number of leafs (not including solid leaf)
		public Int32		mFirstCluster;
		public Int32		mNumClusters;
		public Int32		mAreaFront, mAreaBack;	// Area on each side of the model


		public void Write(BinaryWriter bw)
		{
			bw.Write(mRootNode);
			bw.Write(mMins.X);
			bw.Write(mMins.Y);
			bw.Write(mMins.Z);
			bw.Write(mMaxs.X);
			bw.Write(mMaxs.Y);
			bw.Write(mMaxs.Z);
			bw.Write(mOrigin.X);
			bw.Write(mOrigin.Y);
			bw.Write(mOrigin.Z);
			bw.Write(mFirstFace);
			bw.Write(mNumFaces);
			bw.Write(mFirstLeaf);
			bw.Write(mNumLeafs);
			bw.Write(mFirstCluster);
			bw.Write(mNumClusters);
			bw.Write(mAreaFront);
			bw.Write(mAreaBack);
		}

		public void Read(BinaryReader br)
		{
			mRootNode		=br.ReadInt32();
			mMins.X			=br.ReadSingle();
			mMins.Y			=br.ReadSingle();
			mMins.Z			=br.ReadSingle();
			mMaxs.X			=br.ReadSingle();
			mMaxs.Y			=br.ReadSingle();
			mMaxs.Z			=br.ReadSingle();
			mOrigin.X		=br.ReadSingle();
			mOrigin.Y		=br.ReadSingle();
			mOrigin.Z		=br.ReadSingle();
			mFirstFace		=br.ReadInt32();
			mNumFaces		=br.ReadInt32();
			mFirstLeaf		=br.ReadInt32();
			mNumLeafs		=br.ReadInt32();
			mFirstCluster	=br.ReadInt32();
			mNumClusters	=br.ReadInt32();
			mAreaFront		=br.ReadInt32();
			mAreaBack		=br.ReadInt32();
		}
	}
}
