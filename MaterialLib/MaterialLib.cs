using System;
using System.IO;
using System.Numerics;
using System.ComponentModel;
using System.Collections.Generic;
using Vortice.Direct3D11;
using UtilityLib;


namespace MaterialLib;

public partial class MaterialLib
{
	//list of materials in the library (user or tool made)
	Dictionary<string, Material>	mMats	=new Dictionary<string, Material>();

	StuffKeeper	mSKeeper;


	public MaterialLib(GraphicsDevice gd, StuffKeeper sk)
	{
		mSKeeper	=sk;
	}


	public void FreeAll()
	{
		NukeAllMaterials();
	}


	public void NukeAllMaterials()
	{
		mMats.Clear();
	}


	public void CreateMaterial(string name, bool bBSP, bool bCharacter)
	{
		Material	mat	=new Material(name, bBSP, bCharacter);

		mMats.Add(name, mat);
	}


	public void NukeMaterial(string matName)
	{
		if(!mMats.ContainsKey(matName))
		{
			return;
		}
		mMats.Remove(matName);
	}


	public void SetTriLightValues(Vector4 col0, Vector4 col1, Vector4 col2, Vector3 lightDir)
	{
		foreach(KeyValuePair<string, Material> mat in mMats)
		{
			mat.Value.SetTrilightValues(col1, col1, col2, lightDir);
		}
	}


	public void SetTriLightValues(string matName,
		Vector4 col0, Vector4 col1, Vector4 col2, Vector3 lightDir)
	{
		if(!mMats.ContainsKey(matName))
		{
			return;
		}

		Material	mat	=mMats[matName];

		mat.SetTrilightValues(col1, col1, col2, lightDir);
	}


	public void SetMaterialID(string matName, int id)
	{
		if(!mMats.ContainsKey(matName))
		{
			return;
		}

		Material	mat	=mMats[matName];

		mat.SetMaterialID(id);
	}


	public void SetWorld(string matName, Matrix4x4 world)
	{
		if(!mMats.ContainsKey(matName))
		{
			return;
		}

		Material	mat	=mMats[matName];

		mat.SetWorld(world);
	}


	public void GuessTextures()
	{
		List<string>	textures	=mSKeeper.GetTexture2DList();

		foreach(KeyValuePair<string, Material> mat in mMats)
		{
			//only applies to bsp materials
			if(mat.Value.mBSPVars == null)
			{
				continue;
			}

			string	rawMatName	=mat.Key;
			if(rawMatName.Contains("*"))
			{
				rawMatName	=rawMatName.Substring(0, rawMatName.IndexOf('*'));
			}

			foreach(string tex in textures)
			{
				if(tex.Contains(rawMatName)
					|| tex.Contains(rawMatName.ToLower()))
				{
					ID3D11Texture2D				tex2D	=mSKeeper.GetTexture2D(tex);
					ID3D11ShaderResourceView	srv		=mSKeeper.GetSRV(tex);

					if(tex2D == null || srv == null)
					{
						continue;
					}

					Vector2	texSize	=Vector2.Zero;

					texSize.X	=tex2D.Description.Width;
					texSize.Y	=tex2D.Description.Height;

					mat.Value.mBSPVars.Texture			=tex;
					mat.Value.mBSPVars.TextureEnabled	=true;
					mat.Value.mBSPVars.TextureSize		=texSize;
					break;
				}
			}
		}
	}


	public bool RenameMaterial(string name, string newName)
	{
		if(name == null || newName == null)
		{
			return	false;
		}
		if(!mMats.ContainsKey(name))
		{
			return	false;
		}
		if(mMats.ContainsKey(newName))
		{
			return	false;
		}
		
		Material	mat	=mMats[name];

		mMats.Remove(name);

		mat.Name	=newName;

		mMats.Add(newName, mat);

		return	true;
	}


	public List<string> GetMaterialNames()
	{
		List<string>	ret	=new List<string>();

		foreach(KeyValuePair<string, Material> mat in mMats)
		{
			ret.Add(mat.Key);
		}
		return	ret;
	}


	public bool MaterialExists(string matName)
	{
		return	mMats.ContainsKey(matName);
	}


	public void SetMaterialVShader(string matName, string VShader)
	{
		if(!mMats.ContainsKey(matName))
		{
			return;
		}

		mMats[matName].VSName	=VShader;
	}


	public void SetMaterialPShader(string matName, string PShader)
	{
		if(!mMats.ContainsKey(matName))
		{
			return;
		}

		mMats[matName].PSName	=PShader;
	}


	public void SetMaterialTexture0(string matName, string texName)
	{
		if(!mMats.ContainsKey(matName))
		{
			return;
		}

		ID3D11ShaderResourceView	srv	=mSKeeper.GetSRV(texName);
		if(srv == null)
		{
			return;
		}

		Material	mat	=mMats[matName];

		mat.mMeshVars.Texture0	=texName;
	}


	public void ApplyMaterial(string matName, ID3D11DeviceContext dc)
	{
		if(!mMats.ContainsKey(matName))
		{
			return;
		}
		Material	mat	=mMats[matName];

		mat.Apply(dc, mSKeeper);
	}


	internal ID3D11Texture2D GetTexture2D(string texName)
	{
		return	mSKeeper.GetTexture2D(texName);
	}


	internal Font GetFont(string fontName)
	{
		return	mSKeeper.GetFont(fontName);
	}


	public string GetMaterialVShader(string matName)
	{
		if(!mMats.ContainsKey(matName))
		{
			return	null;
		}

		return	mMats[matName].VSName;
	}


	public string GetMaterialPShader(string matName)
	{
		if(!mMats.ContainsKey(matName))
		{
			return	null;
		}

		return	mMats[matName].PSName;
	}


	public string GetMaterialTexture0(string matName)
	{
		if(!mMats.ContainsKey(matName))
		{
			return	null;
		}

		return	mMats[matName].mMeshVars.Texture0;
	}
}