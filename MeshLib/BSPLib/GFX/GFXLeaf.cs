using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class GFXLeaf
	{
		public UInt32	mContents;
		public Vector3	mMins, mMaxs;
		public Int32	mFirstFace;
		public Int32	mNumFaces;
		public Int32	mFirstPortal;
		public Int32	mNumPortals;
		public Int32	mCluster;
		public Int32	mArea;
		public Int32	mFirstSide;
		public Int32	mNumSides;

		public void Write(BinaryWriter bw)
		{
			bw.Write(mContents);
			bw.Write(mMins.X);
			bw.Write(mMins.Y);
			bw.Write(mMins.Z);
			bw.Write(mMaxs.X);
			bw.Write(mMaxs.Y);
			bw.Write(mMaxs.Z);
			bw.Write(mFirstFace);
			bw.Write(mNumFaces);
			bw.Write(mFirstPortal);
			bw.Write(mNumPortals);
			bw.Write(mCluster);
			bw.Write(mArea);
			bw.Write(mFirstSide);
			bw.Write(mNumSides);
		}

		public void Read(BinaryReader br)
		{
			mContents		=br.ReadUInt32();
			mMins.X			=br.ReadSingle();
			mMins.Y			=br.ReadSingle();
			mMins.Z			=br.ReadSingle();
			mMaxs.X			=br.ReadSingle();
			mMaxs.Y			=br.ReadSingle();
			mMaxs.Z			=br.ReadSingle();
			mFirstFace		=br.ReadInt32();
			mNumFaces		=br.ReadInt32();
			mFirstPortal	=br.ReadInt32();
			mNumPortals		=br.ReadInt32();
			mCluster		=br.ReadInt32();
			mArea			=br.ReadInt32();
			mFirstSide		=br.ReadInt32();
			mNumSides		=br.ReadInt32();
		}
	}
}
