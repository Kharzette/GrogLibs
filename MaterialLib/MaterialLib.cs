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

		public enum ShaderModel
		{
			SM5, SM41, SM4, SM2
		};


		//editor constructor, loads & compiles all
		public MaterialLib(Device dev, ShaderModel sm)
		{
			LoadShaders(dev, sm);

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


		public void NukeMaterial(string matName)
		{
			if(!mMats.ContainsKey(matName))
			{
				return;
			}
			mMats[matName].Clear();
			mMats.Remove(matName);
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


		public BindingList<EffectVariableValue> GetMaterialVariables(string matName)
		{
			if(!mMats.ContainsKey(matName))
			{
				return	null;
			}
			return	mMats[matName].GetVariables();
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
	}
}
