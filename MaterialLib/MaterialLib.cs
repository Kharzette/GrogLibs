using System;
using System.IO;
using System.Numerics;
using System.Collections.Generic;


namespace MaterialLib;

public partial class MaterialLib
{
	//list of materials in the library (user or tool made)
	Dictionary<string, Material>	mMats	=new Dictionary<string, Material>();


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


	public bool MaterialHasTexture(string matName)
	{
		if(!mMats.ContainsKey(matName))
		{
			return	false;
		}

		return	mMats[matName].HasTexture();
	}
}