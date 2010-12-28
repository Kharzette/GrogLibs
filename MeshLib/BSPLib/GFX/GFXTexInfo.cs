using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class GFXTexInfo : IReadWriteable
	{
		public Vector3	[]mVecs			=new Vector3[2];
		public float	[]mShift		=new float[2];
		public float	[]mDrawScale	=new float[2];
		public UInt32	mFlags;
		public float	mFaceLight;
		public float	mReflectiveScale;
		public float	mAlpha;
		public float	mMipMapBias;
		public string	mMaterial;	//index into MaterialLib


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
			bw.Write(mMaterial);
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
			mMaterial			=br.ReadString();
		}


		internal bool IsLightMapped()
		{
			return	((mFlags & TexInfo.NO_LIGHTMAP) == 0);
		}


		internal bool IsAlpha()
		{
			return	((mFlags & TexInfo.TRANS) != 0);
		}


		internal bool IsSky()
		{
			return	((mFlags & TexInfo.SKY) != 0);
		}


		internal bool IsMirror()
		{
			return	((mFlags & TexInfo.MIRROR) != 0);
		}


		internal bool IsGouraud()
		{
			return	((mFlags & TexInfo.GOURAUD) != 0);
		}


		internal bool IsFlat()
		{
			return	((mFlags & TexInfo.FLAT) != 0);
		}


		internal bool IsFullBright()
		{
			return	((mFlags & TexInfo.FULLBRIGHT) != 0);
		}


		internal bool IsLight()
		{
			return	((mFlags & TexInfo.LIGHT) != 0);
		}


		internal Vector2 GetTexCoord(Vector3 vert)
		{
			Vector2	ret	=Vector2.Zero;

			ret.X	=Vector3.Dot(vert, mVecs[0]);
			ret.Y	=Vector3.Dot(vert, mVecs[1]);

			return	ret;
		}
	}
}
