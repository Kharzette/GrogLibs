using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class TexInfo
	{
		public Vector3		mUVec, mVVec;
		public float		mShiftU, mShiftV;
		public float		mDrawScaleU, mDrawScaleV;
		public UInt32		mFlags;
		public float		mFaceLight, mReflectiveScale;
		public float		mLightMapScale;
		public float		mAlpha;
		public string		mTexture;

		//genesis texinfo flags
		public const UInt32	TEXINFO_MIRROR		=(1<<0);
		public const UInt32	TEXINFO_FULLBRIGHT	=(1<<1);
		public const UInt32	TEXINFO_SKY			=(1<<2);
		public const UInt32	TEXINFO_LIGHT		=(1<<3);
		public const UInt32	TEXINFO_TRANS		=(1<<4);
		public const UInt32	TEXINFO_GOURAD		=(1<<5);
		public const UInt32	TEXINFO_FLAT		=(1<<6);
		public const UInt32	TEXINFO_NO_LIGHTMAP	=(1<<15);


		internal bool Compare(TexInfo other)
		{
			if(mUVec != other.mUVec)
			{
				return	false;
			}
			if(mVVec != other.mVVec)
			{
				return	false;
			}
			if(mShiftU != other.mShiftU)
			{
				return	false;
			}
			if(mShiftV != other.mShiftV)
			{
				return	false;
			}
			if(mDrawScaleU != other.mDrawScaleU)
			{
				return	false;
			}
			if(mDrawScaleV != other.mDrawScaleV)
			{
				return	false;
			}
			if(mFlags != other.mFlags)
			{
				return	false;
			}
			if(mFaceLight != other.mFaceLight)
			{
				return	false;
			}
			if(mReflectiveScale != other.mReflectiveScale)
			{
				return	false;
			}
			if(mLightMapScale != other.mLightMapScale)
			{
				return	false;
			}
			if(mAlpha != other.mAlpha)
			{
				return	false;
			}
			if(mTexture != other.mTexture)
			{
				return	false;
			}
			return	true;
		}
	}
}
