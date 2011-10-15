using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPCore
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
		public string		mMaterial;

		//genesis texinfo flags
		public const UInt32	MIRROR		=(1<<0);
		public const UInt32	FULLBRIGHT	=(1<<1);
		public const UInt32	SKY			=(1<<2);
		public const UInt32	LIGHT		=(1<<3);
		public const UInt32	TRANS		=(1<<4);
		public const UInt32	GOURAUD		=(1<<5);
		public const UInt32	FLAT		=(1<<6);
		public const UInt32	NO_LIGHTMAP	=(1<<15);

		//constants
		public const float	FaceLightIntensity	=20.0f;


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
