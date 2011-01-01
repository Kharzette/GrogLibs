using System;
using System.Collections.Generic;
using System.IO;
using System.Text;


namespace BSPLib
{
	public class GFXFace : UtilityLib.IReadWriteable
	{
		public Int32	mFirstVert;
		public Int32	mNumVerts;
		public Int32	mPlaneNum;
		public Int32	mPlaneSide;
		public Int32	mTexInfo;
		public Int32	mLightOfs;
		public Int32	mLWidth;
		public Int32	mLHeight;
		public byte		[]mLTypes	=new byte[4];

		public void Write(BinaryWriter bw)
		{
			bw.Write(mFirstVert);
			bw.Write(mNumVerts);
			bw.Write(mPlaneNum);
			bw.Write(mPlaneSide);
			bw.Write(mTexInfo);
			bw.Write(mLightOfs);
			bw.Write(mLWidth);
			bw.Write(mLHeight);
			bw.Write(mLTypes[0]);
			bw.Write(mLTypes[1]);
			bw.Write(mLTypes[2]);
			bw.Write(mLTypes[3]);
		}

		public void Read(BinaryReader br)
		{
			mFirstVert	=br.ReadInt32();
			mNumVerts	=br.ReadInt32();
			mPlaneNum	=br.ReadInt32();
			mPlaneSide	=br.ReadInt32();
			mTexInfo	=br.ReadInt32();
			mLightOfs	=br.ReadInt32();
			mLWidth		=br.ReadInt32();
			mLHeight	=br.ReadInt32();
			mLTypes[0]	=br.ReadByte();
			mLTypes[1]	=br.ReadByte();
			mLTypes[2]	=br.ReadByte();
			mLTypes[3]	=br.ReadByte();
		}
	}
}
