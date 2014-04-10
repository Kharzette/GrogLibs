using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
	public class MaterialLib
	{
		internal class IncludeFX : CallbackBase, Include
		{
			static string includeDirectory = "";
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


		public void CreateMaterial(string name)
		{
			Material	mat	=new Material(name);

			mMats.Add(name, mat);
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
					LoadShader(dev, f.DirectoryName, f.Name, macs);
				}
			}
		}


		void LoadShader(Device dev, string dir, string file, ShaderMacro []macs)
		{
			string	fullPath	=dir + "\\" + file;

			CompilationResult	shdRes	=ShaderBytecode.CompileFromFile(
				fullPath, "fx_5_0", ShaderFlags.Debug, EffectFlags.None, macs);

			file	=FileUtil.StripExtension(file);

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
