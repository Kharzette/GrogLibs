using System;
using System.Numerics;
using System.IO;


namespace BSPCore
{
	public class GFXNode
	{
		public Int32	mFront, mBack;
		public Int32	mNumFaces;
		public Int32	mFirstFace;
		public Int32	mPlaneNum;
		public Vector3	mMins, mMaxs;


		public void Write(BinaryWriter bw)
		{
			bw.Write(mFront);
			bw.Write(mBack);
			bw.Write(mNumFaces);
			bw.Write(mFirstFace);
			bw.Write(mPlaneNum);
			bw.Write(mMins.X);
			bw.Write(mMins.Y);
			bw.Write(mMins.Z);
			bw.Write(mMaxs.X);
			bw.Write(mMaxs.Y);
			bw.Write(mMaxs.Z);
		}

		public void Read(BinaryReader br)
		{
			mFront		=br.ReadInt32();
			mBack		=br.ReadInt32();
			mNumFaces	=br.ReadInt32();
			mFirstFace	=br.ReadInt32();
			mPlaneNum	=br.ReadInt32();
			mMins.X		=br.ReadSingle();
			mMins.Y		=br.ReadSingle();
			mMins.Z		=br.ReadSingle();
			mMaxs.X		=br.ReadSingle();
			mMaxs.Y		=br.ReadSingle();
			mMaxs.Z		=br.ReadSingle();
		}
	}
}
