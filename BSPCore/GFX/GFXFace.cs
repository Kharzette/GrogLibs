using System;
using System.Collections.Generic;
using System.IO;
using System.Text;


namespace BSPCore
{
	public class GFXFace : UtilityLib.IReadWriteable
	{
		public Int32	mFirstVert;
		public Int32	mNumVerts;
		public Int32	mPlaneNum;
		public bool		mbFlipSide;
		public Int32	mTexInfo;
		public Int32	mLightOfs;
		public Int32	mLWidth;
		public Int32	mLHeight;
		public byte		mLType0;
		public byte		mLType1;
		public byte		mLType2;
		public byte		mLType3;

		public void Write(BinaryWriter bw)
		{
			bw.Write(mFirstVert);
			bw.Write(mNumVerts);
			bw.Write(mPlaneNum);
			bw.Write(mbFlipSide);
			bw.Write(mTexInfo);
			bw.Write(mLightOfs);
			bw.Write(mLWidth);
			bw.Write(mLHeight);
			bw.Write(mLType0);
			bw.Write(mLType1);
			bw.Write(mLType2);
			bw.Write(mLType3);
		}

		public void WriteDebug(BinaryWriter bw, GFXTexInfo tex)
		{
			bw.Write(mFirstVert);
			bw.Write(mNumVerts);
			bw.Write(mPlaneNum);
			bw.Write(mbFlipSide);
			bw.Write(tex.mFlags);
			bw.Write(tex.mMaterial);
		}

		public void Read(BinaryReader br)
		{
			mFirstVert	=br.ReadInt32();
			mNumVerts	=br.ReadInt32();
			mPlaneNum	=br.ReadInt32();
			mbFlipSide	=br.ReadBoolean();
			mTexInfo	=br.ReadInt32();
			mLightOfs	=br.ReadInt32();
			mLWidth		=br.ReadInt32();
			mLHeight	=br.ReadInt32();
			mLType0		=br.ReadByte();
			mLType1		=br.ReadByte();
			mLType2		=br.ReadByte();
			mLType3		=br.ReadByte();
		}
	}
}
