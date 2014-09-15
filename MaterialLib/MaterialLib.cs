using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;
using UtilityLib;

using SharpDX;
using SharpDX.DXGI;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.Direct3D;

//ambiguous stuff
using Color		=SharpDX.Color;
using Device	=SharpDX.Direct3D11.Device;
using Resource	=SharpDX.Direct3D11.Resource;


namespace MaterialLib
{
	public partial class MaterialLib
	{
		//list of materials in the library (user or tool made)
		Dictionary<string, Material>	mMats	=new Dictionary<string, Material>();

		StuffKeeper	mKeeper;


		public void SaveToFile(string fileName)
		{
			if(!CheckReadyToSave())
			{
				return;	//not ready
			}

			FileStream		fs	=new FileStream(fileName, FileMode.Create, FileAccess.Write);
			BinaryWriter	bw	=new BinaryWriter(fs);

			//write a magic number identifying matlibs
			UInt32	magic	=0xFA77DA77;

			bw.Write(magic);

			//write materials
			bw.Write(mMats.Count);
			foreach(KeyValuePair<string, Material> mat in mMats)
			{
				mat.Value.Write(bw, mKeeper.NameForEffect);
			}

			bw.Close();
			fs.Close();
		}


		public void MergeFromFile(string fileName)
		{
			Stream	file	=new FileStream(fileName, FileMode.Open, FileAccess.Read);
			if(file == null)
			{
				return;
			}
			BinaryReader	br	=new BinaryReader(file);
			//read magic number
			UInt32	magic	=br.ReadUInt32();

			if(magic != 0xFA77DA77)
			{
				br.Close();
				file.Close();
				return;
			}

			int	numMaterials	=br.ReadInt32();

			for(int i=0;i < numMaterials;i++)
			{
				Material	m	=new Material("temp");

				m.Read(br, mKeeper.EffectForName, mKeeper.GrabVariables);

				while(mMats.ContainsKey(m.Name))
				{
					m.Name	+="2";
				}

				mMats.Add(m.Name, m);
			}

			br.Close();
			file.Close();
		}


		public void ReadFromFile(string fileName)
		{
			Stream	file	=new FileStream(fileName, FileMode.Open, FileAccess.Read);
			if(file == null)
			{
				return;
			}
			BinaryReader	br	=new BinaryReader(file);

			//clear existing data
			mMats.Clear();

			//read magic number
			UInt32	magic	=br.ReadUInt32();

			if(magic != 0xFA77DA77)
			{
				br.Close();
				file.Close();
				return;
			}

			//load the actual material values
			int	numMaterials	=br.ReadInt32();
			for(int i=0;i < numMaterials;i++)
			{
				Material	m	=new Material("temp");

				m.Read(br, mKeeper.EffectForName, mKeeper.GrabVariables);
				mMats.Add(m.Name, m);

				//set resourcy parameters to resource values
				m.SetResources(mKeeper.ResourceForName);
			}

			br.Close();
			file.Close();
		}


		bool CheckReadyToSave()
		{
			foreach(KeyValuePair<string, Material> mat in mMats)
			{
				if(mat.Value.Name == null || mat.Value.Name == "")
				{
					return	false;
				}

				if(mat.Value.Shader == null)
				{
					return	false;
				}

				if(mat.Value.Technique == null)
				{
					return	false;
				}
			}
			return	true;
		}


		public MaterialLib(GraphicsDevice gd, StuffKeeper sk)
		{
			mKeeper	=sk;
		}


		public void FreeAll()
		{
			NukeAllMaterials();

			//celstuff
			if(mCelResources != null)
			{
				foreach(ShaderResourceView srv in mCelResources)
				{
					if(srv != null)
					{
						srv.Dispose();
					}
				}
			}
			if(mCelTex2Ds != null)
			{
				foreach(Texture2D tex in mCelTex2Ds)
				{
					if(tex != null)
					{
						tex.Dispose();
					}
				}
			}
			if(mCelTex1Ds != null)
			{
				foreach(Texture1D tex in mCelTex1Ds)
				{
					if(tex != null)
					{
						tex.Dispose();
					}
				}
			}
			mCelResources	=null;
			mCelTex2Ds		=null;
			mCelTex1Ds		=null;
		}


		public void NukeAllMaterials()
		{
			foreach(KeyValuePair<string, Material> mat in mMats)
			{
				mat.Value.Clear();
			}
			mMats.Clear();
		}


		public void CreateMaterial(string name)
		{
			Material	mat	=new Material(name);

			mMats.Add(name, mat);
		}


		public void CloneMaterial(string existing, string newMat)
		{
			if(!mMats.ContainsKey(existing))
			{
				return;
			}
			Material	mat	=mMats[existing].Clone(newMat);
			if(mat != null)
			{
				mMats.Add(mat.Name, mat);
			}
		}


		public void NukeMaterial(string matName)
		{
			if(!mMats.ContainsKey(matName))
			{
				return;
			}
			mMats[matName].Clear();
			mMats.Remove(matName);
		}


		public void UpdateWVP(Matrix world, Matrix view, Matrix projection, Vector3 eyePos)
		{
			SetParameterForAll("mWorld", world);
			SetParameterForAll("mView", view);
			SetParameterForAll("mProjection", projection);
			SetParameterForAll("mEyePos", eyePos);
		}


		public void SetTriLightValues(Vector4 col0, Vector4 col1, Vector4 col2, Vector3 lightDir)
		{
			foreach(KeyValuePair<string, Material> mat in mMats)
			{
				mat.Value.SetEffectParameter("mLightColor0", col0);
				mat.Value.SetEffectParameter("mLightColor1", col1);
				mat.Value.SetEffectParameter("mLightColor2", col2);
				mat.Value.SetEffectParameter("mLightDirection", lightDir);
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

			mat.SetEffectParameter("mLightColor0", col0);
			mat.SetEffectParameter("mLightColor1", col1);
			mat.SetEffectParameter("mLightColor2", col2);
			mat.SetEffectParameter("mLightDirection", lightDir);
		}


		public void SetLightMapsToAtlas()
		{
			ShaderResourceView	srv	=mKeeper.GetSRV("LightMapAtlas");
			if(srv == null)
			{
				return;
			}

			foreach(KeyValuePair<string, Material> mat in mMats)
			{
				mat.Value.SetEffectParameter("mLightMap", srv);
			}
		}


		//look for any string paths that should be texture SRVs
		public void FixTextureVariables(string matName)
		{
			if(!mMats.ContainsKey(matName))
			{
				return;
			}

			mMats[matName].SetResources(mKeeper.ResourceForName);
		}


		public void GuessTextures()
		{
			List<string>	textures	=mKeeper.GetTexture2DList();

			foreach(KeyValuePair<string, Material> mat in mMats)
			{
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
						Texture2D			tex2D	=mKeeper.GetTexture2D(tex);
						ShaderResourceView	srv		=mKeeper.GetSRV(tex);

						if(tex2D == null || srv == null)
						{
							continue;
						}

						Vector2	texSize	=Vector2.Zero;

						texSize.X	=tex2D.Description.Width;
						texSize.Y	=tex2D.Description.Height;

						mat.Value.SetEffectParameter("mTexture", srv);
						mat.Value.SetEffectParameter("mbTextureEnabled", true);
						mat.Value.SetEffectParameter("mTexSize", texSize);
						break;
					}
				}
			}
		}


		public void GuessParameterVisibility(string matName)
		{
			if(!mMats.ContainsKey(matName))
			{
				return;
			}
			mMats[matName].GuessParameterVisibility(
				mKeeper.GetIgnoreData(),
				mKeeper.GetHideData());
		}


		public void ResetParameterVisibility(string matName)
		{
			if(!mMats.ContainsKey(matName))
			{
				return;
			}
			mMats[matName].ResetParameterVisibility();
		}


		public object GetMaterialValue(string matName, string varName)
		{
			if(!mMats.ContainsKey(matName))
			{
				return	null;
			}

			BindingList<EffectVariableValue>	vars	=mMats[matName].GetGUIVariables();

			foreach(EffectVariableValue evv in vars)
			{
				if(evv.Name == varName)
				{
					return	evv.Value;
				}
			}
			return	null;
		}


		public Effect GetEffect(string fxName)
		{
			return	mKeeper.EffectForName(fxName);
		}


		public List<string> GetMaterialTechniques(string matName)
		{
			List<string>	ret	=new List<string>();

			if(!mMats.ContainsKey(matName))
			{
				return	ret;
			}

			Effect	matFX	=mMats[matName].Shader;
			if(matFX == null)
			{
				return	ret;
			}

			for(int i=0;i < matFX.Description.TechniqueCount;i++)
			{
				EffectTechnique	et	=matFX.GetTechniqueByIndex(i);
				if(et == null)
				{
					continue;
				}
				if(!et.IsValid)
				{
					continue;
				}
				ret.Add(et.Description.Name);
			}
			return	ret;
		}


		public BindingList<EffectVariableValue> GetMaterialGUIVariables(string matName)
		{
			if(!mMats.ContainsKey(matName))
			{
				return	null;
			}
			return	mMats[matName].GetGUIVariables();
		}


		public string GetMaterialTechnique(string matName)
		{
			if(!mMats.ContainsKey(matName))
			{
				return	null;
			}
			Material	mat	=mMats[matName];

			if(mat.Technique == null)
			{
				return	 null;
			}
			return	mat.Technique.Description.Name;
		}


		public void ClearResourceParameter(DeviceContext dc, string fxName, string tech, string varName)
		{
			EffectVariable	var	=mKeeper.GetVariable(fxName, varName);
			if(var == null)
			{
				return;
			}
			var.AsShaderResource().SetResource(null);

			mKeeper.HackyTechniqueRefresh(dc, fxName, tech);
		}


		//for variables that are usually ignored in the materials
		//but some of the materials end up using it
		public void SetEffectParameter(string fxName, string varName, Matrix []mats)
		{
			EffectVariable	var	=mKeeper.GetVariable(fxName, varName);
			if(var == null)
			{
				return;
			}

			var.AsMatrix().SetMatrix(mats);
		}


		public int GetNumMaterialPasses(string matName)
		{
			if(!mMats.ContainsKey(matName))
			{
				return	-1;
			}

			return	mMats[matName].GetNumPasses();
		}


		public string GetMaterialEffect(string matName)
		{
			if(!mMats.ContainsKey(matName))
			{
				return	null;
			}

			Effect	matFX	=mMats[matName].Shader;
			if(matFX == null)
			{
				return	null;
			}
			return	mKeeper.NameForEffect(matFX);
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


		public void SetMaterialEffect(string matName, string fxName)
		{
			if(!mMats.ContainsKey(matName))
			{
				return;
			}

			Effect	fx	=mKeeper.EffectForName(fxName);
			if(fx == null)
			{
				return;
			}

			mMats[matName].Shader	=fx;

			//send variables over
			mMats[matName].SetVariables(mKeeper.GetEffectVariables(fxName));
		}


		public void SetMaterialTechnique(string matName, string techName)
		{
			if(!mMats.ContainsKey(matName))
			{
				return;
			}
			Material	mat	=mMats[matName];

			if(mat.Shader == null)
			{
				return;
			}
			mat.Technique	=mat.Shader.GetTechniqueByName(techName);
		}


		public void SetParameterForAll(string varName, object value)
		{
			foreach(KeyValuePair<string, Material> mat in mMats)
			{
				mat.Value.SetEffectParameter(varName, value);
			}
		}


		public bool MaterialExists(string matName)
		{
			return	mMats.ContainsKey(matName);
		}


		public void SetMaterialTexture(string matName, string varName, string texName)
		{
			if(!mMats.ContainsKey(matName))
			{
				return;
			}

			ShaderResourceView	srv	=mKeeper.GetSRV(texName);
			if(srv == null)
			{
				return;
			}

			Material	mat	=mMats[matName];

			if(mat.Shader == null)
			{
				return;
			}
			mat.SetEffectParameter(varName, srv);
		}


		public void SetMaterialFontTexture(string matName, string varName, string texName)
		{
			if(!mMats.ContainsKey(matName))
			{
				return;
			}

			ShaderResourceView	srv	=mKeeper.GetFontSRV(texName);
			if(srv == null)
			{
				return;
			}

			Material	mat	=mMats[matName];

			if(mat.Shader == null)
			{
				return;
			}
			mat.SetEffectParameter(varName, srv);
		}


		public void SetMaterialParameter(string matName, string varName, object value)
		{
			if(!mMats.ContainsKey(matName))
			{
				return;
			}
			Material	mat	=mMats[matName];

			if(mat.Shader == null)
			{
				return;
			}
			mat.SetEffectParameter(varName, value);
		}


		//game side slight speedup
		//keeps the effect stuff from needing to constantly
		//search through dictionaries when setting up a draw call
		public void FinalizeMaterials()
		{
			foreach(KeyValuePair<string, Material> mat in mMats)
			{
				mat.Value.FinalizeMat();
			}
		}


		public void ApplyMaterialPass(string matName, DeviceContext dc, int pass)
		{
			if(!mMats.ContainsKey(matName))
			{
				return;
			}
			Material	mat	=mMats[matName];

			mat.ApplyPass(dc, pass);
		}


		public void HideMaterialVariables(string matName, List<string> toHide)
		{
			if(!mMats.ContainsKey(matName))
			{
				return;
			}
			Material	mat	=mMats[matName];

			mat.Hide(toHide);
		}


		public void IgnoreMaterialVariables(string matName, List<string> toIgnore)
		{
			if(!mMats.ContainsKey(matName))
			{
				return;
			}
			Material	mat	=mMats[matName];

			mat.Ignore(toIgnore);
		}


		internal Texture2D GetTexture2D(string texName)
		{
			return	mKeeper.GetTexture2D(texName);
		}


		internal Font GetFont(string fontName)
		{
			return	mKeeper.GetFont(fontName);
		}
	}
}
