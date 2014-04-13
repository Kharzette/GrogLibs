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
using Buffer = SharpDX.Direct3D11.Buffer;
using Color = SharpDX.Color;
using Device = SharpDX.Direct3D11.Device;


namespace MaterialLib
{
	public class EffectVariableValue
	{
		internal EffectVariable	mVar;
		internal object			mValue;

		public string Name
		{
			get { return mVar.Description.Name; }
		}

		public object Value
		{
			get { return ValueAsString(mValue); }
			set { mValue = ValueFromString(value); }
		}


		object ValueFromString(object val)
		{
			if(!(val is string))
			{
				return	val;
			}

			string	sz	=(string)val;

			if(mVar.TypeInfo.Description.Class == ShaderVariableClass.MatrixColumns)
			{
				return	Misc.StringToMatrix(sz);
			}
			else if(mVar.TypeInfo.Description.Class == ShaderVariableClass.Object)
			{
				return	sz;
			}
			else if(mVar.TypeInfo.Description.Class == ShaderVariableClass.Scalar)
			{
				if(mVar.TypeInfo.Description.Type == ShaderVariableType.Float)
				{
					if(mVar.TypeInfo.Description.Elements > 0)
					{
						return	ParseFloatArray(sz);
					}
					else
					{
						float	fval;
						Mathery.TryParse(sz, out fval);
						return	fval;
					}
				}
				else if(mVar.TypeInfo.Description.Type == ShaderVariableType.Bool)
				{
					bool	bVal;
					Mathery.TryParse(sz, out bVal);
					return	bVal;
				}
				else
				{
					Debug.Assert(false);
				}
			}
			else if(mVar.TypeInfo.Description.Class == ShaderVariableClass.Struct)
			{
				Debug.Assert(false);
			}
			else if(mVar.TypeInfo.Description.Class == ShaderVariableClass.Vector)
			{
				if(mVar.TypeInfo.Description.Columns == 2)
				{
					return	Misc.StringToVector2(sz);
				}
				else if(mVar.TypeInfo.Description.Columns == 3)
				{
					return	Misc.StringToVector3(sz);
				}
				else if(mVar.TypeInfo.Description.Columns == 4)
				{
					return	Misc.StringToVector4(sz);
				}
				else
				{
					Debug.Assert(false);
				}
			}
			return	null;
		}


		string ValueAsString(object val)
		{
			if(val == null)
			{
				return	"";
			}

			if(mVar.TypeInfo.Description.Class == ShaderVariableClass.MatrixColumns)
			{
				if(mVar.TypeInfo.Description.Elements > 0)
				{
					return	"Big Ass MatArray";
				}
				else
				{
					return	Misc.MatrixToString((Matrix)val);
				}
			}
			else if(mVar.TypeInfo.Description.Class == ShaderVariableClass.Object)
			{
				if(val is string)	//still in texname form?
				{
					return	(string)val;
				}
				else
				{
					return	((Texture2D)val).DebugName;
				}
			}
			else if(mVar.TypeInfo.Description.Class == ShaderVariableClass.Scalar)
			{
				if(mVar.TypeInfo.Description.Type == ShaderVariableType.Float)
				{
					if(mVar.TypeInfo.Description.Elements > 0)
					{
						return	Misc.FloatArrayToString((float [])val);
					}
					else
					{
						return	Misc.FloatToString((float)val);
					}
				}
				else if(mVar.TypeInfo.Description.Type == ShaderVariableType.Bool)
				{
					return	((bool)val).ToString(System.Globalization.CultureInfo.InvariantCulture);
				}
				else
				{
					Debug.Assert(false);
				}
			}
			else if(mVar.TypeInfo.Description.Class == ShaderVariableClass.Struct)
			{
				Debug.Assert(false);
			}
			else if(mVar.TypeInfo.Description.Class == ShaderVariableClass.Vector)
			{
				if(mVar.TypeInfo.Description.Columns == 2)
				{
					return	Misc.VectorToString((Vector2)val);
				}
				else if(mVar.TypeInfo.Description.Columns == 3)
				{
					return	Misc.VectorToString((Vector3)val);
				}
				else if(mVar.TypeInfo.Description.Columns == 4)
				{
					return	Misc.VectorToString((Vector4)val);
				}
				else
				{
					Debug.Assert(false);
				}
			}
			return	null;
		}

		float[] ParseFloatArray(string floats)
		{
			string	[]toks	=floats.Split(' ');

			List<float>	ret	=new List<float>();

			foreach(string tok in toks)
			{
				float	f;
				if(UtilityLib.Mathery.TryParse(tok, out f))
				{
					ret.Add(f);
				}
			}
			return	ret.ToArray();
		}


		internal void Write(BinaryWriter bw)
		{
			bw.Write(Name);
			bw.Write(ValueAsString(mValue));
		}


		internal void Read(BinaryReader br)
		{
			string	val	=br.ReadString();
			if(val == "")
			{
				mValue	=null;
				return;
			}

			mValue	=ValueFromString(val);
		}
	};

	public class MaterialLib
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


		List<EffectVariable> GrabVariables(string fx)
		{
			if(mVars.ContainsKey(fx))
			{
				return	mVars[fx];
			}
			return	new List<EffectVariable>();
		}


		//editor constructor, loads & compiles all
		public MaterialLib(Device dev, ShaderModel sm)
		{
			LoadShaders(dev, sm);
			LoadParameterData();

			GrabVariables();
		}


		public ShaderBytecode GetMaterialSignature(string matName, int pass)
		{
			if(!mMats.ContainsKey(matName))
			{
				return	null;
			}
			return	mMats[matName].GetPassSignature(pass);
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


		//load all shaders in the shaders folder
		void LoadShaders(Device dev, ShaderModel sm)
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
					//hacks until all the shaders build properly
					if(f.Name == "BSP.fx" || f.Name == "Character.fx"
						|| f.Name == "Post.fx")
					{
						continue;
					}

					LoadShader(dev, f.DirectoryName, f.Name, macs);
				}
			}
		}


		void LoadShader(Device dev, string dir, string file, ShaderMacro []macs)
		{
			string	fullPath	=dir + "\\" + file;

			CompilationResult	shdRes	=ShaderBytecode.CompileFromFile(
				fullPath, "fx_5_0", ShaderFlags.Debug, EffectFlags.None, macs, mIFX);

			Effect	fx	=new Effect(dev, shdRes);
			if(fx != null)
			{
				mFX.Add(file, fx);
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
