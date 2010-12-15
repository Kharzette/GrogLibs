using System;
using System.Collections.Generic;
using System.Text;
using MaterialLib;

namespace BSPLib
{
	public class TexInfoPool
	{
		internal List<TexInfo>	mTexInfos	=new List<TexInfo>();


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

					if((tex.mFlags & TexInfo.TEXINFO_MIRROR) != 0)
					{
						tex.mMaterial	=tname + "Mirror";
					}
					else if((tex.mFlags & TexInfo.TEXINFO_TRANS) != 0)
					{
						tex.mMaterial	=tname + "Alpha";
					}
					else
					{
						tex.mMaterial	=tname;
					}
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
