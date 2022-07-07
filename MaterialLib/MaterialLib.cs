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


	public void CloneMaterial(string toClone, string newName)
	{
		if(!mMats.ContainsKey(toClone))
		{
			return;
		}

		Material	clone	=mMats[toClone].Clone(newName);

		mMats.Add(newName, clone);
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


	public void SetLightDirection(Vector3 lightDir)
	{
		foreach(KeyValuePair<string, Material> mat in mMats)
		{
			mat.Value.SetlightDirection(lightDir);
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


	public void Save(string fileName)
	{
		FileStream	fs	=new FileStream(fileName, FileMode.Create, FileAccess.Write);
		if(fs == null)
		{
			return;
		}

		BinaryWriter	bw	=new BinaryWriter(fs);
		if(bw == null)
		{
			fs.Close();
			return;
		}

		bw.Write(0xFA77DA77);	//FALLDALL?  FATTDATT?

		bw.Write(mMats.Count);	//num materials

		foreach(KeyValuePair<string, Material> m in mMats)
		{
			m.Value.Save(bw);
		}

		bw.Close();
		fs.Close();
	}


	public void Load(string fileName, bool bMerge)
	{
		FileStream	fs	=new FileStream(fileName, FileMode.Open, FileAccess.Read);
		if(fs == null)
		{
			return;
		}

		BinaryReader	br	=new BinaryReader(fs);
		if(br == null)
		{
			fs.Close();
			return;
		}

		if(!bMerge)
		{
			//dump existing
			mMats.Clear();
		}
		
		UInt32	magic	=br.ReadUInt32();		
		if(magic != 0xFA77DA77)
		{
			br.Close();
			fs.Close();
			return;
		}

		int	numMats	=br.ReadInt32();
		for(int i=0;i < numMats;i++)
		{
			Material	m	=new Material("derp", false, false);

			m.Load(br);

			//probably will never happen but check for collisions
			if(mMats.ContainsKey(m.Name))
			{
				//overwrite I guess!?
				mMats.Remove(m.Name);
			}

			mMats.Add(m.Name, m);
		}

		br.Close();
		fs.Close();
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


	public void CheckMaterialType(string name)
	{
		if(!mMats.ContainsKey(name))
		{
			return;
		}

		Material	mat	=mMats[name];

		bool	bBsp	=false;
		bool	bChar	=false;

		string	hlslFile	=mSKeeper.GetHLSLName(mat.VSName);
		if(hlslFile == "BSP")
		{
			bBsp	=true;
		}
		else if(hlslFile == "Character")
		{
			bChar	=true;
		}

		mat.ChangeType(bBsp, bChar);
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


	//grab device context from some loaded resource
	public ID3D11DeviceContext GetDC()
	{
		return	mSKeeper.GetDC();
	}


	public CBKeeper	GetCBKeeper()
	{
		return	mSKeeper.GetCBKeeper();
	}


	public void SetMaterialShadersAndLayout(string matName)
	{
		if(!mMats.ContainsKey(matName))
		{
			return;
		}

		Material	m	=mMats[matName];

		ID3D11VertexShader	vs	=mSKeeper.GetVertexShader(m.VSName);
		ID3D11PixelShader	ps	=mSKeeper.GetPixelShader(m.PSName);

		if(vs == null || ps == null)
		{
			return;
		}

		ID3D11DeviceContext	dc	=vs.Device.ImmediateContext;

		dc.VSSetShader(vs);
		dc.PSSetShader(ps);

		dc.IASetInputLayout(mSKeeper.GetOrCreateLayout(m.VSName));
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


	public BSPMat GetMaterialBSPMat(string matName)
	{
		if(!mMats.ContainsKey(matName))
		{
			return	null;
		}

		return	mMats[matName].mBSPVars;
	}


	public MeshMat GetMaterialMeshMat(string matName)
	{
		if(!mMats.ContainsKey(matName))
		{
			return	null;
		}

		return	mMats[matName].mMeshVars;
	}
}