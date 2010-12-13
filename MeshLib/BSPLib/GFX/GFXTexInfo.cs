using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class GFXTexInfo
	{
		public Vector3	[]mVecs			=new Vector3[2];
		public float	[]mShift		=new float[2];
		public float	[]mDrawScale	=new float[2];
		public UInt32	mFlags;
		public float	mFaceLight;
		public float	mReflectiveScale;
		public float	mAlpha;
		public float	mMipMapBias;
		public Int32	mTexture;

		public void Write(BinaryWriter bw)
		{
			bw.Write(mVecs[0].X);
			bw.Write(mVecs[0].Y);
			bw.Write(mVecs[0].Z);
			bw.Write(mVecs[1].X);
			bw.Write(mVecs[1].Y);
			bw.Write(mVecs[1].Z);
			bw.Write(mShift[0]);
			bw.Write(mShift[1]);
			bw.Write(mDrawScale[0]);
			bw.Write(mDrawScale[1]);
			bw.Write(mFlags);
			bw.Write(mFaceLight);
			bw.Write(mReflectiveScale);
			bw.Write(mAlpha);
			bw.Write(mMipMapBias);
			bw.Write(mTexture);
		}

		public void Read(BinaryReader br)
		{
			mVecs[0].X			=br.ReadSingle();
			mVecs[0].Y			=br.ReadSingle();
			mVecs[0].Z			=br.ReadSingle();
			mVecs[1].X			=br.ReadSingle();
			mVecs[1].Y			=br.ReadSingle();
			mVecs[1].Z			=br.ReadSingle();
			mShift[0]			=br.ReadSingle();
			mShift[1]			=br.ReadSingle();
			mDrawScale[0]		=br.ReadSingle();
			mDrawScale[1]		=br.ReadSingle();
			mFlags				=br.ReadUInt32();
			mFaceLight			=br.ReadSingle();
			mReflectiveScale	=br.ReadSingle();
			mAlpha				=br.ReadSingle();
			mMipMapBias			=br.ReadSingle();
			mTexture			=br.ReadInt32();
		}
	}
}
