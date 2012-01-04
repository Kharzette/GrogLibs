using System;
using System.Collections.Generic;
using System.Text;
using System.IO;


namespace BSPCore
{
	public class TexInfoPool
	{
		internal List<TexInfo>	mTexInfos	=new List<TexInfo>();


		internal void Write(BinaryWriter bw)
		{
			bw.Write(mTexInfos.Count);
			foreach(TexInfo tex in mTexInfos)
			{
				GFXTexInfo	gtex	=new GFXTexInfo();

				gtex.mAlpha				=tex.mAlpha;
				gtex.mDrawScaleU		=tex.mDrawScaleU;
				gtex.mDrawScaleV		=tex.mDrawScaleV;
				gtex.mFaceLight			=tex.mFaceLight;
				gtex.mFlags				=tex.mFlags;
				gtex.mMipMapBias		=1.0f;	//is this right?
				gtex.mReflectiveScale	=tex.mReflectiveScale;
				gtex.mShiftU			=tex.mShiftU;
				gtex.mShiftV			=tex.mShiftV;
				gtex.mMaterial			=tex.mMaterial;
				gtex.mVecU				=tex.mUVec;
				gtex.mVecV				=tex.mVVec;

				gtex.Write(bw);
			}
		}


		internal void Read(BinaryReader br)
		{
			int	cnt	=br.ReadInt32();
			for(int i=0;i < cnt;i++)
			{
				GFXTexInfo	gtex	=new GFXTexInfo();

				gtex.Read(br);

				TexInfo	tex	=new TexInfo();

				tex.mAlpha				=gtex.mAlpha;
				tex.mDrawScaleU			=gtex.mDrawScaleU;
				tex.mDrawScaleV			=gtex.mDrawScaleV;
				tex.mFaceLight			=gtex.mFaceLight;
				tex.mFlags				=gtex.mFlags;
				tex.mReflectiveScale	=gtex.mReflectiveScale;
				tex.mShiftU				=gtex.mShiftU;
				tex.mShiftV				=gtex.mShiftV;
				tex.mMaterial			=gtex.mMaterial;
				tex.mTexture			=gtex.mMaterial;
				tex.mUVec				=gtex.mVecU;
				tex.mVVec				=gtex.mVecV;

				mTexInfos.Add(tex);
			}
		}


		internal int Add(TexInfo ti)
		{
			foreach(TexInfo tex in mTexInfos)
			{
				if(tex.Compare(ti))
				{
					return	mTexInfos.IndexOf(tex);
				}
			}

			mTexInfos.Add(ti);

			return	mTexInfos.IndexOf(ti);
		}


		internal void AssignMaterials()
		{
			//get a unique list of textures used
			List<string>	textures	=new List<string>();
			foreach(TexInfo tex in mTexInfos)
			{
				if(!textures.Contains(tex.mTexture))
				{
					textures.Add(tex.mTexture);
				}
			}

			//for every unique texture, check texinfos
			//for material differences
			foreach(string tname in textures)
			{
				foreach(TexInfo tex in mTexInfos)
				{
					if(tex.mTexture != tname)
					{
						continue;
					}

					tex.mMaterial	=tname;
				}
			}
		}
	}
}
