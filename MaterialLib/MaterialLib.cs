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
using Buffer	=SharpDX.Direct3D11.Buffer;
using Color		=SharpDX.Color;
using Device	=SharpDX.Direct3D11.Device;
using Resource	=SharpDX.Direct3D11.Resource;


namespace MaterialLib
{
	public partial class MaterialLib
	{
		internal class IncludeFX : CallbackBase, Include
		{
			string	mRootDir;

			internal IncludeFX(string rootDir)
			{
				mRootDir	=rootDir;
			}

			static string includeDirectory = "Shaders\\";
			public void Close(Stream stream)
			{
				stream.Close();
				stream.Dispose();
			}

			public Stream Open(IncludeType type, string fileName, Stream parentStream)
			{
				return	new FileStream(mRootDir + "\\" + includeDirectory + fileName, FileMode.Open);
			}
		}

		IncludeFX	mIFX;

		//game directory
		string	mGameRootDir;

		//list of materials in the library (user or tool made)
		Dictionary<string, Material>	mMats	=new Dictionary<string, Material>();

		//list of shaders available
		Dictionary<string, Effect>	mFX	=new Dictionary<string, Effect>();

		//texture 2ds
		Dictionary<string, Texture2D>	mTexture2s	=new Dictionary<string, Texture2D>();

		//list of texturey things
		Dictionary<string, Resource>	mResources	=new Dictionary<string, Resource>();

		//list of shader resource views for stuff like textures
		Dictionary<string, ShaderResourceView>	mSRVs	=new Dictionary<string, ShaderResourceView>();

		//list of parameter variables per shader
		Dictionary<string, List<EffectVariable>>	mVars	=new Dictionary<string, List<EffectVariable>>();

		//data driven set of ignored and hidden parameters (see text file)
		Dictionary<string, List<string>>	mIgnoreData	=new Dictionary<string,List<string>>();
		Dictionary<string, List<string>>	mHiddenData	=new Dictionary<string,List<string>>();

		public enum ShaderModel
		{
			SM5, SM41, SM4, SM2
		};


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

			//write which textures and shaders used
			List<string>	texInUse	=new List<string>();
			List<string>	shdInUse	=new List<string>();
			List<string>	cubeInUse	=new List<string>();
			foreach(KeyValuePair<string, Material> mat in mMats)
			{
//				mat.Value.GetTexturesInUse(texInUse);
//				mat.Value.GetTextureCubesInUse(cubeInUse);

				string	shdName	=NameForEffect(mat.Value.Shader);
				if(shdName != "" && !shdInUse.Contains(shdName))
				{
					shdInUse.Add(shdName);
				}
			}

			bw.Write(shdInUse.Count);
			foreach(string shd in shdInUse)
			{
				bw.Write(shd);
			}

			bw.Write(texInUse.Count);
			foreach(string tex in texInUse)
			{
				bw.Write(tex);
			}

			bw.Write(cubeInUse.Count);
			foreach(string cube in cubeInUse)
			{
				bw.Write(cube);
			}

			//write materials
			bw.Write(mMats.Count);
			foreach(KeyValuePair<string, Material> mat in mMats)
			{
				mat.Value.Write(bw, NameForEffect);
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

			//load the referenced textures and shaders
			//though these aren't used for anything tool side
			List<string>	texNeeded	=new List<string>();
			List<string>	shdNeeded	=new List<string>();
			List<string>	cubeNeeded	=new List<string>();

			//read shaders in use
			int	numShd	=br.ReadInt32();
			for(int i=0;i < numShd;i++)
			{
				shdNeeded.Add(br.ReadString());
			}

			//read textures in use
			int	numTex	=br.ReadInt32();
			for(int i=0;i < numTex;i++)
			{
				texNeeded.Add(br.ReadString());
			}

			//read cubes in use
			int	numCube	=br.ReadInt32();
			for(int i=0;i < numCube;i++)
			{
				cubeNeeded.Add(br.ReadString());
			}

			int	numMaterials	=br.ReadInt32();

			for(int i=0;i < numMaterials;i++)
			{
				Material	m	=new Material("temp");

				m.Read(br, EffectForName, GrabVariables);

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

			//load the referenced textures and shaders
			List<string>	texNeeded	=new List<string>();
			List<string>	shdNeeded	=new List<string>();
			List<string>	cubeNeeded	=new List<string>();

			//read shaders in use
			int	numShd	=br.ReadInt32();
			for(int i=0;i < numShd;i++)
			{
				shdNeeded.Add(br.ReadString());
			}

			//read textures in use
			int	numTex	=br.ReadInt32();
			for(int i=0;i < numTex;i++)
			{
				texNeeded.Add(br.ReadString());
			}

			//read cubes in use
			int	numCube	=br.ReadInt32();
			for(int i=0;i < numCube;i++)
			{
				cubeNeeded.Add(br.ReadString());
			}

//			LoadNewContent(gd, shdNeeded, texNeeded, cubeNeeded);

			//load the actual material values
			int	numMaterials	=br.ReadInt32();
			for(int i=0;i < numMaterials;i++)
			{
				Material	m	=new Material("temp");

				m.Read(br, EffectForName, GrabVariables);
				mMats.Add(m.Name, m);

				//set resourcy parameters to resource values
				m.SetResources(ResourceForName);
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


		string NameForEffect(Effect fx)
		{
			if(mFX.ContainsValue(fx))
			{
				foreach(KeyValuePair<string, Effect> ef in mFX)
				{
					if(ef.Value == fx)
					{
						return	ef.Key;
					}
				}
			}
			return	"";
		}


		Effect EffectForName(string fxName)
		{
			if(mFX.ContainsKey(fxName))
			{
				return	mFX[fxName];
			}
			return	null;
		}


		ShaderResourceView ResourceForName(string fxName)
		{
			if(mSRVs.ContainsKey(fxName))
			{
				return	mSRVs[fxName];
			}
			return	null;
		}


		List<EffectVariable> GrabVariables(string fx)
		{
			if(mVars.ContainsKey(fx))
			{
				return	mVars[fx];
			}
			return	new List<EffectVariable>();
		}


		public MaterialLib(GraphicsDevice gd, string gameRootDir, bool bUsePreCompiled)
		{
			mGameRootDir	=gameRootDir;
			mIFX			=new IncludeFX(gameRootDir);

			switch(gd.GD.FeatureLevel)
			{
				case	FeatureLevel.Level_11_0:
					Construct(gd, ShaderModel.SM5, bUsePreCompiled);
					break;
				case	FeatureLevel.Level_10_1:
					Construct(gd, ShaderModel.SM41, bUsePreCompiled);
					break;
				case	FeatureLevel.Level_10_0:
					Construct(gd, ShaderModel.SM4, bUsePreCompiled);
					break;
				case	FeatureLevel.Level_9_3:
					Construct(gd, ShaderModel.SM2, bUsePreCompiled);
					break;
				default:
					Debug.Assert(false);	//only support the above
					Construct(gd, ShaderModel.SM2, bUsePreCompiled);
					break;
			}
		}


		void Construct(GraphicsDevice gd, ShaderModel sm, bool bUsePreCompiled)
		{
			LoadShaders(gd.GD, sm, bUsePreCompiled);
			SaveHeaderTimeStamps();
			LoadResources(gd);
			LoadParameterData();

			GrabVariables();
		}


		public void FreeAll()
		{
			NukeAllMaterials();

			foreach(KeyValuePair<string, Effect> fx in mFX)
			{
				fx.Value.Dispose();
			}
			mFX.Clear();

			foreach(KeyValuePair<string, Texture2D> tex in mTexture2s)
			{
				tex.Value.Dispose();
			}
			mTexture2s.Clear();

			foreach(KeyValuePair<string, Resource> res in mResources)
			{
				res.Value.Dispose();
			}
			mResources.Clear();

			foreach(KeyValuePair<string, ShaderResourceView> srv in mSRVs)
			{
				srv.Value.Dispose();
			}
			mSRVs.Clear();

			foreach(KeyValuePair<string, List<EffectVariable>> effList in mVars)
			{
				foreach(EffectVariable efv in effList.Value)
				{
					efv.Dispose();
				}
				effList.Value.Clear();
			}
			mVars.Clear();

			mIgnoreData.Clear();
			mHiddenData.Clear();

			//celstuff
			if(mCelResources != null)
			{
				foreach(ShaderResourceView srv in mCelResources)
				{
					srv.Dispose();
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


		public void AddMap(string name, ShaderResourceView srv)
		{
			if(mSRVs.ContainsKey(name))
			{
				mSRVs.Remove(name);
			}
			mSRVs.Add(name, srv);
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


		public void SetLightMapsToAtlas()
		{
			if(!mSRVs.ContainsKey("LightMapAtlas"))
			{
				return;
			}

			foreach(KeyValuePair<string, Material> mat in mMats)
			{
				mat.Value.SetEffectParameter("mLightMap", mSRVs["LightMapAtlas"]);
			}
		}


		//textures in the particles dir
		public List<string> GetParticleTextures()
		{
			List<string>	ret	=new List<string>();

			foreach(KeyValuePair<string, Texture2D> tex in mTexture2s)
			{
				if(tex.Key.StartsWith("Particles"))
				{
					ret.Add(tex.Key);
				}
			}
			return	ret;
		}


		public void GuessTextures()
		{
			foreach(KeyValuePair<string, Material> mat in mMats)
			{
				string	rawMatName	=mat.Key;
				if(rawMatName.Contains("*"))
				{
					rawMatName	=rawMatName.Substring(0, rawMatName.IndexOf('*'));
				}

				foreach(KeyValuePair<string, Resource> tex in mResources)
				{
					if(tex.Key.Contains(rawMatName)
						|| tex.Key.Contains(rawMatName.ToLower()))
					{
						if(!mTexture2s.ContainsKey(tex.Key))
						{
							continue;
						}
						if(!mSRVs.ContainsKey(tex.Key))
						{
							continue;
						}

						Vector2	texSize	=Vector2.Zero;

						texSize.X	=mTexture2s[tex.Key].Description.Width;
						texSize.Y	=mTexture2s[tex.Key].Description.Height;

						mat.Value.SetEffectParameter("mTexture", mSRVs[tex.Key]);
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
			mMats[matName].GuessParameterVisibility(mIgnoreData, mHiddenData);
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
			return	EffectForName(fxName);
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

			if(!mFX.ContainsValue(matFX))
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


		public List<string> GetEffects()
		{
			List<string>	ret	=new List<string>();

			foreach(KeyValuePair<string, Effect> fx in mFX)
			{
				ret.Add(fx.Key);
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


		//for variables that are usually ignored in the materials
		//but some of the materials end up using it
		public void SetEffectParameter(string fxName, string varName, Matrix []mats)
		{
			if(!mFX.ContainsKey(fxName))
			{
				return;
			}

			EffectVariable	var	=mVars[fxName].FirstOrDefault(v => v.Description.Name == varName);
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

			if(!mFX.ContainsValue(matFX))
			{
				return	null;
			}
			foreach(KeyValuePair<string, Effect> fx in mFX)
			{
				if(fx.Value == matFX)
				{
					return	fx.Key;
				}
			}
			return	null;
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

			if(!mFX.ContainsKey(fxName))
			{
				return;
			}

			mMats[matName].Shader	=mFX[fxName];

			//send variables over
			mMats[matName].SetVariables(mVars[fxName]);
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

			if(!mSRVs.ContainsKey(texName))
			{
				return;
			}

			Material	mat	=mMats[matName];

			if(mat.Shader == null)
			{
				return;
			}
			mat.SetEffectParameter(varName, mSRVs[texName]);
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


		public void ApplyMaterialPass(string matName, DeviceContext dc, int pass)
		{
			if(!mMats.ContainsKey(matName))
			{
				return;
			}
			Material	mat	=mMats[matName];

			mat.ApplyPass(dc, pass);
		}


		void LoadResources(GraphicsDevice gd)
		{
			//see if Textures folder exists in Content
			if(Directory.Exists(mGameRootDir + "/Textures"))
			{
				DirectoryInfo	di	=new DirectoryInfo(mGameRootDir + "/Textures/");

				FileInfo[]		fi	=di.GetFiles("*.png", SearchOption.AllDirectories);
				foreach(FileInfo f in fi)
				{
					LoadTexture(gd, f.DirectoryName, f.Name);
				}
			}
		}


		//load all shaders in the shaders folder
		void LoadShaders(Device dev, ShaderModel sm, bool bUsePreCompiled)
		{
			ShaderMacro []macs	=new ShaderMacro[1];

			macs[0]	=new ShaderMacro(sm.ToString(), 1);

			//see if Shader folder exists in Content
			if(Directory.Exists(mGameRootDir + "/Shaders"))
			{
				DirectoryInfo	di	=new DirectoryInfo(mGameRootDir + "/Shaders/");

				bool	bHeaderSame	=CheckHeaderTimeStamps(di);

				FileInfo[]		fi	=di.GetFiles("*.fx", SearchOption.AllDirectories);
				foreach(FileInfo f in fi)
				{
					if(bUsePreCompiled && bHeaderSame)
					{
						if(Directory.Exists(mGameRootDir + "/CompiledShaders"))
						{
							//see if a precompiled exists
							if(Directory.Exists(mGameRootDir + "/CompiledShaders/" + macs[0].Name))
							{
								DirectoryInfo	preDi	=new DirectoryInfo(
									mGameRootDir + "/CompiledShaders/" + macs[0].Name);

								FileInfo[]	preFi	=preDi.GetFiles(f.Name + ".Compiled", SearchOption.TopDirectoryOnly);

								if(preFi.Length == 1)
								{
									if(f.LastWriteTime <= preFi[0].LastWriteTime)
									{
										LoadCompiledShader(dev, preFi[0].DirectoryName, preFi[0].Name, macs);
										continue;
									}
								}
							}
						}
					}
					LoadShader(dev, f.DirectoryName, f.Name, macs);
				}
			}
		}


		void LoadCompiledShader(Device dev, string dir, string file, ShaderMacro []macs)
		{
			string	fullPath	=dir + "\\" + file;

			FileStream		fs	=new FileStream(fullPath, FileMode.Open, FileAccess.Read);
			BinaryReader	br	=new BinaryReader(fs);

			int	len	=br.ReadInt32();

			byte	[]code	=br.ReadBytes(len);

			Effect	fx	=new Effect(dev, code);
			if(fx != null)
			{
				mFX.Add(file.Substring(0, file.Length - 9), fx);
			}
		}


		void SaveHeaderTimeStamps()
		{
			DirectoryInfo	di	=new DirectoryInfo(mGameRootDir + "/Shaders/");

			FileStream	fs	=new FileStream(
				di.FullName + "Header.TimeStamps",
				FileMode.Create, FileAccess.Write);

			Debug.Assert(fs != null);

			BinaryWriter	bw	=new BinaryWriter(fs);

			Dictionary<string, DateTime>	stamps	=GetHeaderTimeStamps(di);

			bw.Write(stamps.Count);
			foreach(KeyValuePair<string, DateTime> time in stamps)
			{
				bw.Write(time.Key);
				bw.Write(time.Value.Ticks);
			}

			bw.Close();
			fs.Close();
		}


		Dictionary<string, DateTime> GetHeaderTimeStamps(DirectoryInfo di)
		{
			FileInfo[]		fi	=di.GetFiles("*.fxh", SearchOption.AllDirectories);

			Dictionary<string, DateTime>	ret	=new Dictionary<string, DateTime>();

			foreach(FileInfo f in fi)
			{
				ret.Add(f.Name, f.LastWriteTime);
			}
			return	ret;
		}


		//returns true if headers haven't changed
		bool CheckHeaderTimeStamps(DirectoryInfo di)
		{
			//see if there is a binary file here that contains the
			//timestamps of the fxh files
			FileInfo[]	hTime	=di.GetFiles("Header.TimeStamps", SearchOption.TopDirectoryOnly);
			if(hTime.Length != 1)
			{
				return	false;
			}

			FileStream	fs	=new FileStream(hTime[0].DirectoryName + "\\" + hTime[0].Name, FileMode.Open, FileAccess.Read);
			if(fs == null)
			{
				return	false;
			}

			BinaryReader	br	=new BinaryReader(fs);

			Dictionary<string, DateTime>	times	=new Dictionary<string, DateTime>();

			int	count	=br.ReadInt32();
			for(int i=0;i < count;i++)
			{
				string	fileName	=br.ReadString();
				long	time		=br.ReadInt64();

				DateTime	t	=new DateTime(time);

				times.Add(fileName, t);
			}

			br.Close();
			fs.Close();

			Dictionary<string, DateTime>	onDisk	=GetHeaderTimeStamps(di);

			//check the timestamp data against the dates
			if(onDisk.Count != times.Count)
			{
				return	false;
			}

			foreach(KeyValuePair<string, DateTime> tstamp in onDisk)
			{
				if(!times.ContainsKey(tstamp.Key))
				{
					return	false;
				}

				if(times[tstamp.Key] < tstamp.Value)
				{
					return	false;
				}
			}
			return	true;
		}


		void LoadShader(Device dev, string dir, string file, ShaderMacro []macs)
		{
			if(!Directory.Exists(mGameRootDir + "/CompiledShaders"))
			{
				Directory.CreateDirectory(mGameRootDir + "/CompiledShaders");
			}

			if(!Directory.Exists(mGameRootDir + "/CompiledShaders/" + macs[0].Name))
			{
				Directory.CreateDirectory(mGameRootDir + "/CompiledShaders/" + macs[0].Name);
			}

			string	fullPath	=dir + "\\" + file;

			CompilationResult	shdRes	=ShaderBytecode.CompileFromFile(
				fullPath, "fx_5_0", ShaderFlags.Debug, EffectFlags.None, macs, mIFX);

			Effect	fx	=new Effect(dev, shdRes);
			if(fx == null)
			{
				return;
			}

			Debug.Assert(fx.IsValid);

			//do a validity check on all techniques and passes
			for(int i=0;i < fx.Description.TechniqueCount;i++)
			{
				EffectTechnique	et	=fx.GetTechniqueByIndex(i);

				Debug.Assert(et.IsValid);

				for(int j=0;j < et.Description.PassCount;j++)
				{
					EffectPass	ep	=et.GetPassByIndex(j);

					Debug.Assert(ep.IsValid);
				}
			}

			FileStream	fs	=new FileStream(mGameRootDir + "/CompiledShaders/"
				+ macs[0].Name + "/" + file + ".Compiled",
				FileMode.Create, FileAccess.Write);

			BinaryWriter	bw	=new BinaryWriter(fs);

			bw.Write(shdRes.Bytecode.Data.Length);
			bw.Write(shdRes.Bytecode.Data, 0, shdRes.Bytecode.Data.Length);

			bw.Close();
			fs.Close();

			mFX.Add(file, fx);
		}


		void LoadTexture(GraphicsDevice gd, string path, string fileName)
		{
			int	texIndex	=path.LastIndexOf("Textures");

			string	afterTex	="";

			if((texIndex + 9) < path.Length)
			{
				afterTex	=path.Substring(texIndex + 9);
			}
			string	extLess	="";

			if(afterTex != "")
			{
				extLess	=afterTex + "\\" + FileUtil.StripExtension(fileName);
			}
			else
			{
				extLess	=FileUtil.StripExtension(fileName);
			}

			ImageLoadInformation	loadInfo	=new ImageLoadInformation();

			loadInfo.BindFlags		=BindFlags.None;
			loadInfo.CpuAccessFlags	=CpuAccessFlags.Read | CpuAccessFlags.Write;
			loadInfo.Depth			=0;
			loadInfo.Filter			=FilterFlags.None;
			loadInfo.Format			=Format.R8G8B8A8_UNorm;
			loadInfo.Usage			=ResourceUsage.Staging;

			Resource	res	=Texture2D.FromFile(gd.GD, path + "\\" + fileName, loadInfo);
			if(res == null)
			{
				return;
			}

			DataBox	db	=gd.DC.MapSubresource(res, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);

			Texture2D	tex	=res as Texture2D;

			PreMultAndLinear(db, tex.Description.Width * tex.Description.Height);

			Texture2D	finalTex	=MakeTexture(gd.GD, db, tex.Description.Width, tex.Description.Height);

			mResources.Add(extLess, finalTex as Resource);
			mTexture2s.Add(extLess, finalTex);

			ShaderResourceView	srv	=new ShaderResourceView(gd.GD, finalTex);

			srv.DebugName	=extLess;

			mSRVs.Add(extLess, srv);

			tex.Dispose();
			res.Dispose();
		}


		Texture2D MakeTexture(Device dev, DataBox db, int width, int height)
		{
			Texture2DDescription	texDesc	=new Texture2DDescription();
			texDesc.ArraySize				=1;
			texDesc.BindFlags				=BindFlags.ShaderResource;
			texDesc.CpuAccessFlags			=CpuAccessFlags.None;
			texDesc.MipLevels				=1;
			texDesc.OptionFlags				=ResourceOptionFlags.None;
			texDesc.Usage					=ResourceUsage.Immutable;
			texDesc.Width					=width;
			texDesc.Height					=height;
			texDesc.Format					=Format.R8G8B8A8_UNorm;
			texDesc.SampleDescription		=new SampleDescription(1, 0);

			DataBox	[]dbs	=new DataBox[1];

			dbs[0]	=db;

			Texture2D	tex	=new Texture2D(dev, texDesc, dbs);

			return	tex;
		}


		void GrabVariables()
		{
			foreach(KeyValuePair<string, Effect> fx in mFX)
			{
				for(int i=0;;i++)
				{
					EffectVariable	ev	=fx.Value.GetVariableByIndex(i);
					if(ev == null)
					{
						break;
					}
					if(!ev.IsValid)
					{
						break;
					}

					if(!mVars.ContainsKey(fx.Key))
					{
						mVars.Add(fx.Key, new List<EffectVariable>());
					}
					mVars[fx.Key].Add(ev);
				}
			}
		}


		unsafe void PreMultAndLinear(DataBox db, int len)
		{
			if(db.IsEmpty)
			{
				return;
			}

			var	pSrc	=(Color *)@db.DataPointer;

			for(int i=0;i < len;i++)
			{
				Color	c	=*(pSrc + i);

				Vector4	vColor	=c.ToVector4();

				//convert to linear
				vColor.X	=(float)Math.Pow(vColor.X, 2.2);
				vColor.Y	=(float)Math.Pow(vColor.Y, 2.2);
				vColor.Z	=(float)Math.Pow(vColor.Z, 2.2);

				vColor.X	*=vColor.W;
				vColor.Y	*=vColor.W;
				vColor.Z	*=vColor.W;

				Color	done	=new Color(vColor);

				*(pSrc + i)	=done;
			}
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


		void LoadParameterData()
		{
			FileStream		fs	=new FileStream(mGameRootDir + "/Shaders/ParameterData.txt", FileMode.Open, FileAccess.Read);
			StreamReader	sr	=new StreamReader(fs);

			string			curTechnique	="";
			string			curCategory		="";
			List<string>	curStuff		=new List<string>();
			for(;;)
			{
				string	line	=sr.ReadLine();
				if(line.StartsWith("//"))
				{
					continue;
				}

				//python style!
				if(line.StartsWith("\t\t"))
				{
					Debug.Assert(curTechnique != "");
					Debug.Assert(curCategory != "");

					if(curCategory == "Ignored")
					{
						curStuff.Add(line.Trim());
					}
					else if(curCategory == "Hidden")
					{
						curStuff.Add(line.Trim());
					}
					else
					{
						Debug.Assert(false);
					}
				}
				else if(line.StartsWith("\t"))
				{
					if(curStuff.Count > 0)
					{
						if(curCategory == "Ignored")
						{
							mIgnoreData.Add(curTechnique, curStuff);
						}
						else if(curCategory == "Hidden")
						{
							mHiddenData.Add(curTechnique, curStuff);
						}
						else
						{
							Debug.Assert(false);
						}
						curStuff	=new List<string>();
					}
					curCategory	=line.Trim();
				}
				else
				{
					if(curStuff.Count > 0)
					{
						if(curCategory == "Ignored")
						{
							mIgnoreData.Add(curTechnique, curStuff);
						}
						else if(curCategory == "Hidden")
						{
							mHiddenData.Add(curTechnique, curStuff);
						}
						else
						{
							Debug.Assert(false);
						}
						curStuff	=new List<string>();
					}
					curCategory		="";
					curTechnique	=line.Trim();
				}

				if(sr.EndOfStream)
				{
					if(curStuff.Count > 0)
					{
						if(curCategory == "Ignored")
						{
							mIgnoreData.Add(curTechnique, curStuff);
						}
						else if(curCategory == "Hidden")
						{
							mHiddenData.Add(curTechnique, curStuff);
						}
						else
						{
							Debug.Assert(false);
						}
						curStuff	=new List<string>();
					}
					break;
				}
			}

			sr.Close();
			fs.Close();
		}
	}
}
