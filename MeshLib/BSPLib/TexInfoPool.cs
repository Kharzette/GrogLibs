using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using MaterialLib;

namespace BSPLib
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
				gtex.mDrawScale[0]		=tex.mDrawScaleU;
				gtex.mDrawScale[1]		=tex.mDrawScaleV;
				gtex.mFaceLight			=tex.mFaceLight;
				gtex.mFlags				=tex.mFlags;
				gtex.mMipMapBias		=1.0f;	//is this right?
				gtex.mReflectiveScale	=tex.mReflectiveScale;
				gtex.mShift[0]			=tex.mShiftU;
				gtex.mShift[1]			=tex.mShiftV;
				gtex.mMaterial			=tex.mMaterial;
				gtex.mVecs[0]			=tex.mUVec;
				gtex.mVecs[1]			=tex.mVVec;

				gtex.Write(bw);
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


		List<Material>	GetMaterials()
		{
			List<string>	mats	=new List<string>();
			foreach(TexInfo tex in mTexInfos)
			{
				if(!mats.Contains(tex.mMaterial))
				{
					mats.Add(tex.mMaterial);
				}
			}

			//build material list
			List<Material>	ret	=new List<Material>();
			foreach(string matName in mats)
			{
				Material	mat	=new Material();
				mat.Name	=matName;
				if(matName.EndsWith("Alpha"))
				{
					mat.Alpha	=true;
				}
				else if(matName.EndsWith("Mirror"))
				{
					mat.Alpha	=true;
				}
				ret.Add(mat);
			}
			return	ret;
		}
	}
}
