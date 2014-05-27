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
			static string includeDirectory = "Shaders\\";
			public void Close(Stream stream)
			{
				stream.Close();
				stream.Dispose();
			}

			public Stream Open(IncludeType type, string fileName, Stream parentStream)
			{
				return	new FileStream(includeDirectory + fileName, FileMode.Open);
			}
		}

		IncludeFX	mIFX	=new IncludeFX();

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


		//editor constructor, loads or compiles all
		public MaterialLib(Device dev, ShaderModel sm, bool bUsePreCompiled)
		{
			Construct(dev, sm, bUsePreCompiled);
		}


		public MaterialLib(Device dev, FeatureLevel fl, bool bUsePreCompiled)
		{
			switch(fl)
			{
				case	FeatureLevel.Level_11_0:
					Construct(dev, ShaderModel.SM5, bUsePreCompiled);
					break;
				case	FeatureLevel.Level_10_1:
					Construct(dev, ShaderModel.SM41, bUsePreCompiled);
					break;
				case	FeatureLevel.Level_10_0:
					Construct(dev, ShaderModel.SM4, bUsePreCompiled);
					break;
				case	FeatureLevel.Level_9_3:
					Construct(dev, ShaderModel.SM2, bUsePreCompiled);
					break;
				default:
					Debug.Assert(false);	//only support the above
					Construct(dev, ShaderModel.SM2, bUsePreCompiled);
					break;
			}
		}


		void Construct(Device dev, ShaderModel sm, bool bUsePreCompiled)
		{
			LoadShaders(dev, sm, bUsePreCompiled);
			LoadResources(dev);
			LoadParameterData();

			GrabVariables();
		}


		public void NukeAllMaterials()
		{
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


		public void SetLightMapsToAtlas()
		{
			foreach(KeyValuePair<string, Material> mat in mMats)
			{
				mat.Value.SetEffectParameter("mLightMap", mSRVs["LightMapAtlas"]);
			}
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

			for(int i=0;;i++)
			{
				EffectTechnique	et	=matFX.GetTechniqueByIndex(i);
				if(et == null)
				{
					break;
				}
				if(!et.IsValid)
				{
					break;
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


		void LoadResources(Device dev)
		{
			//see if Textures folder exists in Content
			if(Directory.Exists("Textures"))
			{
				DirectoryInfo	di	=new DirectoryInfo(
					AppDomain.CurrentDomain.BaseDirectory + "/Textures/");

				FileInfo[]		fi	=di.GetFiles("*.png", SearchOption.AllDirectories);
				foreach(FileInfo f in fi)
				{
					LoadTexture(dev, f.DirectoryName, f.Name);
				}
			}
		}


		//load all shaders in the shaders folder
		void LoadShaders(Device dev, ShaderModel sm, bool bUsePreCompiled)
		{
			ShaderMacro []macs	=new ShaderMacro[1];

			macs[0]	=new ShaderMacro(sm.ToString(), 1);

			//see if Shader folder exists in Content
			if(Directory.Exists("Shaders"))
			{
				DirectoryInfo	di	=new DirectoryInfo(
					AppDomain.CurrentDomain.BaseDirectory + "/Shaders/");

				FileInfo[]		fi	=di.GetFiles("*.fx", SearchOption.AllDirectories);
				foreach(FileInfo f in fi)
				{
					if(bUsePreCompiled)
					{
						if(Directory.Exists("CompiledShaders"))
						{
							//see if a precompiled exists
							if(Directory.Exists(AppDomain.CurrentDomain.BaseDirectory +
								"/CompiledShaders/" + macs[0].Name))
							{
								DirectoryInfo	preDi	=new DirectoryInfo(
									AppDomain.CurrentDomain.BaseDirectory +
									"/CompiledShaders/" + macs[0].Name);

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


		void LoadShader(Device dev, string dir, string file, ShaderMacro []macs)
		{
			if(!Directory.Exists("CompiledShaders"))
			{
				Directory.CreateDirectory("CompiledShaders");
			}

			if(!Directory.Exists("CompiledShaders/" + macs[0].Name))
			{
				Directory.CreateDirectory("CompiledShaders/" + macs[0].Name);
			}

			string	fullPath	=dir + "\\" + file;

			CompilationResult	shdRes	=ShaderBytecode.CompileFromFile(
				fullPath, "fx_5_0", ShaderFlags.Debug, EffectFlags.None, macs, mIFX);


			FileStream	fs	=new FileStream("CompiledShaders/"
				+ macs[0].Name + "/" + file + ".Compiled",
				FileMode.Create, FileAccess.Write);

			BinaryWriter	bw	=new BinaryWriter(fs);

			bw.Write(shdRes.Bytecode.Data.Length);
			bw.Write(shdRes.Bytecode.Data, 0, shdRes.Bytecode.Data.Length);

			bw.Close();
			fs.Close();

			Effect	fx	=new Effect(dev, shdRes);
			if(fx != null)
			{
				mFX.Add(file, fx);
			}
		}


		void LoadTexture(Device dev, string path, string fileName)
		{
			string	extLess	=FileUtil.StripExtension(fileName);

			ImageLoadInformation	loadInfo	=new ImageLoadInformation();

			loadInfo.BindFlags		=BindFlags.ShaderResource;
			loadInfo.CpuAccessFlags	=CpuAccessFlags.None;
			loadInfo.Depth			=0;
			loadInfo.Filter			=FilterFlags.SRgbIn | FilterFlags.None;	//pngs are srgb
			loadInfo.Format			=Format.R8G8B8A8_UNorm_SRgb;
			loadInfo.Usage			=ResourceUsage.Default;

			Resource	res	=Texture2D.FromFile(dev, path + "\\" + fileName, loadInfo);

			if(res != null)
			{
				mResources.Add(extLess, res);
				mTexture2s.Add(extLess, res as Texture2D);

				ShaderResourceView	srv	=new ShaderResourceView(dev, res);

				srv.DebugName	=extLess;

				mSRVs.Add(extLess, srv);
			}
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
			FileStream		fs	=new FileStream("Shaders/ParameterData.txt", FileMode.Open, FileAccess.Read);
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
