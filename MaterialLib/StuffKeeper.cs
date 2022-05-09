using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SharpGen.Runtime;
using UtilityLib;
using Vortice.DXGI;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.D3DCompiler;
using Vortice.Mathematics;
using Vortice.WIC;


namespace MaterialLib;

//hangs on to stuff like shaders and textures
public class StuffKeeper
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

	[Flags]
	internal enum	ShaderEntryType : byte
	{
		None,
		Vertex		=1,
		Pixel		=2,
		Compute		=4,
		Geometry	=8,
		Hull		=16,
		Domain		=32
	}

	IncludeFX	mIFX;

	//for texture loading
	IWICImagingFactory	mIF	=new IWICImagingFactory();

	//game directory
	string	mGameRootDir;

	//entry points for shaders
	Dictionary<string, List<string>>	mVSEntryPoints	=new Dictionary<string, List<string>>();
	Dictionary<string, List<string>>	mPSEntryPoints	=new Dictionary<string, List<string>>();

	//compiled shader bytecode
	Dictionary<string, byte[]>	mVSCode	=new Dictionary<string, byte[]>();
	Dictionary<string, byte[]>	mPSCode	=new Dictionary<string, byte[]>();
	Dictionary<string, byte[]>	mGSCode	=new Dictionary<string, byte[]>();
	Dictionary<string, byte[]>	mDSCode	=new Dictionary<string, byte[]>();
	Dictionary<string, byte[]>	mHSCode	=new Dictionary<string, byte[]>();
	Dictionary<string, byte[]>	mCSCode	=new Dictionary<string, byte[]>();

	//shaders
	Dictionary<string, ID3D11VertexShader>	mVShaders	=new Dictionary<string, ID3D11VertexShader>();
	Dictionary<string, ID3D11PixelShader>	mPShaders	=new Dictionary<string, ID3D11PixelShader>();

	//texture 2ds
	Dictionary<string, ID3D11Texture2D>	mTexture2s	=new Dictionary<string, ID3D11Texture2D>();

	//font texture 2ds
	Dictionary<string, ID3D11Texture2D>	mFontTexture2s	=new Dictionary<string, ID3D11Texture2D>();

	//list of texturey things
	Dictionary<string, ID3D11Resource>	mResources	=new Dictionary<string, ID3D11Resource>();

	//list of shader resource views for stuff like textures
	Dictionary<string, ID3D11ShaderResourceView>	mSRVs	=new Dictionary<string, ID3D11ShaderResourceView>();

	//shader resource views for fonts
	Dictionary<string, ID3D11ShaderResourceView>	mFontSRVs	=new Dictionary<string, ID3D11ShaderResourceView>();

	//list of parameter variables per shader
//	Dictionary<string, List<EffectVariable>>	mVars	=new Dictionary<string, List<EffectVariable>>();

	//data driven set of ignored and hidden parameters (see text file)
//	Dictionary<string, List<string>>	mIgnoreData	=new Dictionary<string,List<string>>();
//	Dictionary<string, List<string>>	mHiddenData	=new Dictionary<string,List<string>>();

	//list of font data
	Dictionary<string, Font>	mFonts	=new Dictionary<string, Font>();

	public enum ShaderModel
	{
		SM5, SM41, SM4, SM2
	};

	public event EventHandler	eCompileNeeded;	//shader compiles needed, passes the number
	public event EventHandler	eCompileDone;	//fired as each compile finishes


	public StuffKeeper() { }

	public void Init(GraphicsDevice gd, string gameRootDir)
	{
		mGameRootDir	=gameRootDir;
		mIFX			=new IncludeFX(gameRootDir);

		switch(gd.GD.FeatureLevel)
		{
			case	FeatureLevel.Level_11_0:
				Construct(gd, ShaderModel.SM5);
				break;
			case	FeatureLevel.Level_10_1:
				Construct(gd, ShaderModel.SM41);
				break;
			case	FeatureLevel.Level_10_0:
				Construct(gd, ShaderModel.SM4);
				break;
			case	FeatureLevel.Level_9_3:
				Construct(gd, ShaderModel.SM2);
				break;
			default:
				Debug.Assert(false);	//only support the above
				Construct(gd, ShaderModel.SM2);
				break;
		}
	}


	void Construct(GraphicsDevice gd, ShaderModel sm)
	{
		LoadEntryPoints();
		LoadShaders(gd.GD, sm);
		SaveHeaderTimeStamps(sm);
		LoadResources(gd);
		LoadFonts(gd);
//		LoadParameterData();

//		GrabVariables();
	}


//	internal Dictionary<string, List<string>> GetIgnoreData()
//	{
//		return	mIgnoreData;
//	}


//	internal Dictionary<string, List<string>> GetHideData()
//	{
//		return	mHiddenData;
//	}


	internal ID3D11Texture2D GetTexture2D(string name)
	{
		if(!mTexture2s.ContainsKey(name))
		{
			return	null;
		}
		return	mTexture2s[name];
	}


	internal Font GetFont(string name)
	{
		if(!mFonts.ContainsKey(name))
		{
			return	null;
		}
		return	mFonts[name];
	}

/*
	internal List<EffectVariable> GetEffectVariables(string fxName)
	{
		if(!mVars.ContainsKey(fxName))
		{
			return	null;
		}

		return	mVars[fxName];
	}

	internal string NameForEffect(Effect fx)
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


	internal List<EffectVariable> GrabVariables(string fx)
	{
		if(mVars.ContainsKey(fx))
		{
			return	mVars[fx];
		}
		return	new List<EffectVariable>();
	}


	internal Effect EffectForName(string fxName)
	{
		if(mFX.ContainsKey(fxName))
		{
			return	mFX[fxName];
		}
		return	null;
	}


	public List<string> GetEffectList()
	{
		List<string>	ret	=new List<string>();

		foreach(KeyValuePair<string, Effect> fx in mFX)
		{
			ret.Add(fx.Key);
		}
		return	ret;
	}


	internal EffectVariable GetVariable(string fxName, string varName)
	{
		if(!mFX.ContainsKey(fxName))
		{
			return	null;
		}

		return	mVars[fxName].FirstOrDefault(v => v.Description.Name == varName);
	}*/


	//This can be used to generate input layouts
	public byte[]	GetVSCompiledCode(string name)
	{
		if(!mVSCode.ContainsKey(name))
		{
			return	null;
		}
		return	mVSCode[name];
	}


	public ID3D11VertexShader	GetVertexShader(string name)
	{
		if(!mVShaders.ContainsKey(name))
		{
			return	null;
		}
		return	mVShaders[name];
	}

	public ID3D11PixelShader	GetPixelShader(string name)
	{
		if(!mPShaders.ContainsKey(name))
		{
			return	null;
		}
		return	mPShaders[name];
	}


	public ID3D11ShaderResourceView GetSRV(string name)
	{
		if(!mSRVs.ContainsKey(name))
		{
			return	null;
		}
		return	mSRVs[name];
	}


	internal ID3D11ShaderResourceView GetFontSRV(string name)
	{
		if(!mFontSRVs.ContainsKey(name))
		{
			return	null;
		}
		return	mFontSRVs[name];
	}


	internal ID3D11ShaderResourceView ResourceForName(string fxName)
	{
		if(mSRVs.ContainsKey(fxName))
		{
			return	mSRVs[fxName];
		}
		return	null;
	}


	//for tools mainly
	public List<string> GetTexture2DList()
	{
		List<string>	ret	=new List<string>();

		foreach(KeyValuePair<string, ID3D11Texture2D> tex in mTexture2s)
		{
			ret.Add(tex.Key);
		}
		return	ret;
	}


	public List<string> GetFontList()
	{
		List<string>	ret	=new List<string>();

		//sort fonts by height
		IOrderedEnumerable<KeyValuePair<string, Font>>	sorted
			=mFonts.OrderBy(fnt => fnt.Value.GetCharacterHeight());

		foreach(KeyValuePair<string, Font> font in sorted)
		{
			ret.Add(font.Key);
		}
		return	ret;
	}


	public void AddMap(string name, ID3D11ShaderResourceView srv)
	{
		if(mSRVs.ContainsKey(name))
		{
			mSRVs.Remove(name);
		}
		mSRVs.Add(name, srv);
	}


	public void AddTex(ID3D11Device dev, string name, byte []texData, int w, int h)
	{
		if(mTexture2s.ContainsKey(name))
		{
			return;
		}

		ID3D11Texture2D	tex	=MakeTexture(dev, texData, w, h);

		mResources.Add(name, tex as ID3D11Resource);
		mTexture2s.Add(name, tex);

		ID3D11ShaderResourceView	srv	=dev.CreateShaderResourceView(tex);
		srv.DebugName	=name;

		mSRVs.Add(name, srv);
	}

/*
	public bool AddTexToAtlas(TexAtlas atlas, string texName, GraphicsDevice gd,
		out double scaleU, out double scaleV, out double uoffs, out double voffs)
	{
		scaleU	=scaleV	=uoffs	=voffs	=0.0;

		if(!mTexture2s.ContainsKey(texName))
		{
			return	false;
		}

		//even though we already have this loaded, there appears
		//to be no way to get at the bits, so load it again
		int		w, h;
		Color	[]colArray	=LoadPNGWIC(mIF, mGameRootDir + "\\Textures\\" + texName + ".png",
								out w, out h);

		return	atlas.Insert(colArray, w, h,
			out scaleU, out scaleV, out uoffs, out voffs);
	}*/


	public void FreeAll()
	{
		mIF.Dispose();

		foreach(KeyValuePair<string, ID3D11Texture2D> tex in mTexture2s)
		{
			tex.Value.Dispose();
		}
		mTexture2s.Clear();

		foreach(KeyValuePair<string, ID3D11Texture2D> tex in mFontTexture2s)
		{
			tex.Value.Dispose();
		}
		mFontTexture2s.Clear();

		foreach(KeyValuePair<string, ID3D11Resource> res in mResources)
		{
			res.Value.Dispose();
		}
		mResources.Clear();

		foreach(KeyValuePair<string, ID3D11ShaderResourceView> srv in mSRVs)
		{
			srv.Value.Dispose();
		}
		mSRVs.Clear();

		foreach(KeyValuePair<string, ID3D11ShaderResourceView> srv in mFontSRVs)
		{
			srv.Value.Dispose();
		}
		mFontSRVs.Clear();

		foreach(KeyValuePair<string, ID3D11VertexShader> shd in mVShaders)
		{
			shd.Value.Dispose();
		}
		mVShaders.Clear();

		foreach(KeyValuePair<string, ID3D11PixelShader> shd in mPShaders)
		{
			shd.Value.Dispose();
		}
		mPShaders.Clear();
/*
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

		foreach(KeyValuePair<string, Effect> fx in mFX)
		{
			fx.Value.Dispose();
		}
		mFX.Clear();*/
	}


	//sometimes dx11 thinks a rendertarget is still bound as a resource
	//setting that resource to null and then calling this will give it
	//a kick in the pants to actually free the resource up
	/*
	public void HackyTechniqueRefresh(ID3D11DeviceContext dc, string fx, string tech)
	{
		if(!mFX.ContainsKey(fx))
		{
			return;
		}

		EffectTechnique	et	=mFX[fx].GetTechniqueByName(tech);

		if(et == null || !et.IsValid)
		{
			return;
		}

		EffectPass	ep	=et.GetPassByIndex(0);

		ep.Apply(dc);

		ep.Dispose();
		et.Dispose();
	}*/


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


	void LoadFonts(GraphicsDevice gd)
	{
		//see if Fonts folder exists in Content
		if(!Directory.Exists(mGameRootDir + "/Fonts"))
		{
			return;
		}

		DirectoryInfo	di	=new DirectoryInfo(mGameRootDir + "/Fonts/");

		FileInfo[]		fi	=di.GetFiles("*.png", SearchOption.AllDirectories);
		foreach(FileInfo f in fi)
		{
			LoadFontTexture(gd, f.DirectoryName, f.Name);

			string	extLess	=FileUtil.StripExtension(f.Name);

			Font	font	=new Font(f.DirectoryName + "/" + extLess + ".dat");
			mFonts.Add(extLess, font);
		}
	}


	void ReadEntryPoints(StreamReader sr, Dictionary<string, List<string>> dict)
	{
		string	curShader	="";
		for(;;)
		{
			string	line	=sr.ReadLine();
			if(line.StartsWith("//"))
			{
				continue;	//comment
			}

			//python style!
			if(line.StartsWith("\t"))
			{
				Debug.Assert(curShader != "");

				dict[curShader].Add(line.Trim());
			}
			else
			{
				curShader	=FileUtil.StripExtension(line);
				dict.Add(curShader, new List<string>());
			}

			if(sr.EndOfStream)
			{
				break;
			}
		}
	}


	void LoadEntryPoints()
	{
		//see if Shader folder exists in Content
		if(!Directory.Exists(mGameRootDir + "/Shaders"))
		{
			return;
		}
		if(!File.Exists(mGameRootDir + "/Shaders/VSEntryPoints.txt"))
		{
			return;
		}
		if(!File.Exists(mGameRootDir + "/Shaders/PSEntryPoints.txt"))
		{
			return;
		}

		FileStream	fs	=new FileStream(mGameRootDir + "/Shaders/VSEntryPoints.txt", FileMode.Open, FileAccess.Read);
		if(fs == null)
		{
			return;
		}

		StreamReader	sr	=new StreamReader(fs);
		if(sr == null)
		{
			fs.Close();
			return;
		}

		ReadEntryPoints(sr, mVSEntryPoints);

		sr.Close();
		fs.Close();

		fs	=new FileStream(mGameRootDir + "/Shaders/PSEntryPoints.txt", FileMode.Open, FileAccess.Read);
		if(fs == null)
		{
			return;
		}

		sr	=new StreamReader(fs);
		if(sr == null)
		{
			fs.Close();
			return;
		}

		ReadEntryPoints(sr, mPSEntryPoints);

		sr.Close();
		fs.Close();
	}


	//load all shaders in the shaders folder
	void LoadShaders(ID3D11Device dev, ShaderModel sm)
	{
		//need 2 of these for some reason
		ShaderMacro []macs	=new ShaderMacro[2];

		macs[0]	=new ShaderMacro(sm.ToString(), 1);

		//see if Shader folder exists in Content
		if(Directory.Exists(mGameRootDir + "/Shaders"))
		{
			DirectoryInfo	di	=new DirectoryInfo(mGameRootDir + "/Shaders/");

			bool	bHeaderSame	=false;
			if(Directory.Exists(mGameRootDir + "/CompiledShaders"))
			{
				//see if a precompiled exists
				if(Directory.Exists(mGameRootDir + "/CompiledShaders/" + macs[0].Name))
				{
					DirectoryInfo	preDi	=new DirectoryInfo(
						mGameRootDir + "/CompiledShaders/" + macs[0].Name);

					bHeaderSame	=CheckHeaderTimeStamps(preDi, di);
				}
			}

			List<string>	dirNames	=new List<string>();
			List<string>	fileNames	=new List<string>();

			FileInfo[]		fi	=di.GetFiles("*.hlsl", SearchOption.AllDirectories);
			foreach(FileInfo f in fi)
			{
				if(bHeaderSame)
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
				dirNames.Add(f.DirectoryName);
				fileNames.Add(f.Name);
			}

			if(fileNames.Count > 0)
			{
				Debug.Assert(fileNames.Count == dirNames.Count);

				Misc.SafeInvoke(eCompileNeeded, fileNames.Count);

				for(int i=0;i < fileNames.Count;i++)
				{
					string	noExt	=FileUtil.StripExtension(fileNames[i]);

					//vertexstuff
					if(mVSEntryPoints.ContainsKey(noExt))
					{
						foreach(string entryPoint in mVSEntryPoints[noExt])
						{
							if(mVSCode.ContainsKey(entryPoint))
							{
								continue;	//already loaded
							}

							LoadShader(dev, dirNames[i], fileNames[i],
								entryPoint, ShaderEntryType.Vertex, sm, macs);
						}
					}

					//pixelstuff
					if(mPSEntryPoints.ContainsKey(noExt))
					{
						foreach(string entryPoint in mPSEntryPoints[noExt])
						{
							if(mPSCode.ContainsKey(entryPoint))
							{
								continue;	//already loaded
							}

							LoadShader(dev, dirNames[i], fileNames[i],
								entryPoint, ShaderEntryType.Pixel, sm, macs);
						}
					}

					Misc.SafeInvoke(eCompileDone, i + 1);
				}
			}
		}

		//create shaders
		foreach(KeyValuePair<string, byte []> code in mVSCode)
		{
			mVShaders.Add(code.Key,	dev.CreateVertexShader(code.Value));
		}
		foreach(KeyValuePair<string, byte []> code in mPSCode)
		{
			mPShaders.Add(code.Key,	dev.CreatePixelShader(code.Value));
		}
	}


	void AddCompiledCode(ShaderEntryType set, string name, byte []code)
	{
		switch(set)
		{
			case	ShaderEntryType.Compute:
				mCSCode.Add(name, code);
				break;
			case	ShaderEntryType.Domain:
				mDSCode.Add(name, code);
				break;
			case	ShaderEntryType.Geometry:
				mGSCode.Add(name, code);
				break;
			case	ShaderEntryType.Hull:
				mHSCode.Add(name, code);
				break;
			case	ShaderEntryType.Pixel:
				mPSCode.Add(name, code);
				break;
			case	ShaderEntryType.Vertex:
				mVSCode.Add(name, code);
				break;
		}
	}


	void LoadCompiledShader(ID3D11Device dev, string dir, string file, ShaderMacro []macs)
	{
		string	fullPath	=dir + "\\" + file;

		FileStream		fs	=new FileStream(fullPath, FileMode.Open, FileAccess.Read);
		BinaryReader	br	=new BinaryReader(fs);

		int	len	=br.ReadInt32();

		ShaderEntryType	set	=(ShaderEntryType)br.ReadByte();

		byte	[]code	=br.ReadBytes(len);

		string	justName	=FileUtil.StripExtension(file);

		AddCompiledCode(set, justName, code);

		br.Close();
		fs.Close();
	}


	void SaveHeaderTimeStamps(ShaderModel sm)
	{
		if(!Directory.Exists(mGameRootDir + "/CompiledShaders"))
		{
			return;
		}

		DirectoryInfo	src	=new DirectoryInfo(mGameRootDir + "/Shaders/");

		if(!src.Exists)
		{
			return;
		}

		DirectoryInfo	di	=new DirectoryInfo(mGameRootDir + "/CompiledShaders/" + sm.ToString() + "/");

		FileStream	fs	=new FileStream(
			di.FullName + "Header.TimeStamps",
			FileMode.Create, FileAccess.Write);

		Debug.Assert(fs != null);

		BinaryWriter	bw	=new BinaryWriter(fs);

		Dictionary<string, DateTime>	stamps	=GetHeaderTimeStamps(src);

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
		FileInfo[]		fi	=di.GetFiles("*.cso", SearchOption.AllDirectories);

		Dictionary<string, DateTime>	ret	=new Dictionary<string, DateTime>();

		foreach(FileInfo f in fi)
		{
			ret.Add(f.Name, f.LastWriteTime);
		}
		return	ret;
	}


	//returns true if headers haven't changed
	bool CheckHeaderTimeStamps(DirectoryInfo preDi, DirectoryInfo srcDi)
	{
		//see if there is a binary file here that contains the
		//timestamps of the cso files
		FileInfo[]	hTime	=preDi.GetFiles("Header.TimeStamps", SearchOption.TopDirectoryOnly);
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

		Dictionary<string, DateTime>	onDisk	=GetHeaderTimeStamps(srcDi);

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


	string	VersionString(ShaderModel sm)
	{
		switch(sm)
		{
			case	ShaderModel.SM2:
			return	"2_0";
			case	ShaderModel.SM4:
			return	"4_0";
			case	ShaderModel.SM41:
			return	"4_1";
			case	ShaderModel.SM5:
			return	"5_0";
		}
		return	"69";
	}


	string	ProfileFromSM(ShaderModel sm, ShaderEntryType set)
	{
		switch(set)
		{
			case	ShaderEntryType.Compute:
			return	"cs_" + VersionString(sm);
			case	ShaderEntryType.Geometry:
			return	"gs_" + VersionString(sm);
			case	ShaderEntryType.Pixel:
			return	"ps_" + VersionString(sm);
			case	ShaderEntryType.Vertex:
			return	"vs_" + VersionString(sm);
			case	ShaderEntryType.Domain:
			return	"ds_" + VersionString(sm);
			case	ShaderEntryType.Hull:
			return	"hs_" + VersionString(sm);
		}
		return	"broken";
	}


	void LoadShader(ID3D11Device dev, string dir, string file,
		string entryPoint, ShaderEntryType set,
		ShaderModel sm, ShaderMacro []macs)
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
		Blob	codeBlob, errBlob;
		string	profile	=ProfileFromSM(sm, set);
		string	extLess	=FileUtil.StripExtension(file);

		Result	res	=Compiler.CompileFromFile(fullPath, macs, mIFX,
			entryPoint, profile, ShaderFlags.None,
			EffectFlags.None, out codeBlob, out errBlob);
		if(res != Result.Ok)
		{
			Console.WriteLine(errBlob.AsString());
			return;
		}

		byte	[]code	=codeBlob.AsBytes();

		FileStream	fs	=new FileStream(mGameRootDir + "/CompiledShaders/"
			+ macs[0].Name + "/" + entryPoint + ".cso",
			FileMode.Create, FileAccess.Write);

		BinaryWriter	bw	=new BinaryWriter(fs);

		bw.Write(code.Length);
		bw.Write((byte)set);
		bw.Write(code, 0, code.Length);

		bw.Close();
		fs.Close();

		AddCompiledCode(set, entryPoint, code);
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

		int	w, h;
		byte	[]colArray	=LoadPNGWIC(mIF, path + "\\" + fileName, out w, out h);

		PreMultAndLinear(colArray, w, h);

		ID3D11Texture2D	finalTex	=MakeTexture(gd.GD, colArray, w, h);

		mResources.Add(extLess, finalTex as ID3D11Resource);
		mTexture2s.Add(extLess, finalTex);

		ID3D11ShaderResourceView	srv	=gd.GD.CreateShaderResourceView(finalTex);
		srv.DebugName	=extLess;

		mSRVs.Add(extLess, srv);
	}


	void LoadFontTexture(GraphicsDevice gd, string path, string fileName)
	{
		int	texIndex	=path.LastIndexOf("Fonts");

		string	afterTex	="";

		if((texIndex + 6) < path.Length)
		{
			afterTex	=path.Substring(texIndex + 6);
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

		int	w,h;
		byte	[]colors	=LoadPNGWIC(mIF, path + "\\" + fileName, out w, out h);

		PreMultAndLinear(colors, w, h);

		ID3D11Texture2D	finalTex	=MakeTexture(gd.GD, colors, w, h);

		mResources.Add(extLess, finalTex as ID3D11Resource);
		mFontTexture2s.Add(extLess, finalTex);

		ID3D11ShaderResourceView	srv	=gd.GD.CreateShaderResourceView(finalTex);
		srv.DebugName	=extLess;

		mFontSRVs.Add(extLess, srv);
	}


	unsafe ID3D11Texture2D MakeTexture(ID3D11Device dev, byte []colors, int width, int height)
	{
		Texture2DDescription	texDesc	=new Texture2DDescription();
		texDesc.ArraySize				=1;
		texDesc.BindFlags				=BindFlags.ShaderResource;
		texDesc.CPUAccessFlags			=CpuAccessFlags.None;
		texDesc.MipLevels				=1;
		texDesc.MiscFlags				=ResourceOptionFlags.None;
		texDesc.Usage					=ResourceUsage.Immutable;
		texDesc.Width					=width;
		texDesc.Height					=height;
		texDesc.Format					=Format.R8G8B8A8_UNorm;
		texDesc.SampleDescription		=new SampleDescription(1, 0);

		ID3D11Texture2D	tex;

		//alloc temp space for color data
		IntPtr	texData	=Marshal.AllocHGlobal(width * height * 4);

		Marshal.Copy(colors, 0, texData, width * height * 4);

		SubresourceData	[]srd	=new SubresourceData[1];

		srd[0]	=new SubresourceData(texData, width * 4);
				
		tex	=dev.CreateTexture2D(texDesc, srd);

		Marshal.FreeHGlobal(texData);

		return	tex;
	}

/*
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
					ev.Dispose();
					break;
				}

				if(!mVars.ContainsKey(fx.Key))
				{
					mVars.Add(fx.Key, new List<EffectVariable>());
				}
				mVars[fx.Key].Add(ev);
			}
		}
	}*/


	void PreMultAndLinear(byte []colors, int width, int height)
	{
		float	oo255	=1.0f / 255.0f;

		for(int y=0;y < height;y++)
		{
			int	ofs	=y * width * 4;

			for(int x=0;x < width;x+=4)
			{
				int	ofsX	=ofs + (x * 4);

				byte	cR	=colors[ofsX];
				byte	cG	=colors[ofsX + 1];
				byte	cB	=colors[ofsX + 2];
				byte	cA	=colors[ofsX + 3];

				float	xc	=cR * oo255;
				float	yc	=cG * oo255;
				float	zc	=cB * oo255;
				float	wc	=cA * oo255;

				//convert to linear
				xc	=(float)Math.Pow(xc, 2.2);
				yc	=(float)Math.Pow(yc, 2.2);
				zc	=(float)Math.Pow(zc, 2.2);

				//premultiply alpha
				xc	*=wc;
				yc	*=wc;
				zc	*=wc;

				colors[ofsX]		=(byte)(xc * 255.0f);
				colors[ofsX + 1]	=(byte)(yc * 255.0f);
				colors[ofsX + 2]	=(byte)(zc * 255.0f);
			}
		}
	}


	public static byte[] LoadPNGWIC(IWICImagingFactory wif, string path,
										out int w, out int h)
	{		
		IWICBitmapDecoder	pbd	=wif.CreateDecoderFromFileName(path,
			FileAccess.Read,DecodeOptions.CacheOnDemand);

		IWICBitmapFrameDecode	bfd	=pbd.GetFrame(0);

		IWICFormatConverter	conv	=wif.CreateFormatConverter();

		conv.Initialize(bfd, PixelFormat.Format32bppRGBA);

		w	=bfd.Size.Width;
		h	=bfd.Size.Height;

		byte	[]colArray	=new byte[w * h * 4];

		conv.CopyPixels(w * 4, colArray);

		conv.Dispose();
		bfd.Dispose();
		pbd.Dispose();

		return	colArray;
	}


	public ID3D11InputLayout	MakeLayout(
		ID3D11Device 			dev,
		string 					vsEntry,
		InputElementDescription []ied)
	{
		if(!mVSCode.ContainsKey(vsEntry))
		{
			return	null;
		}

		return	dev.CreateInputLayout(ied, mVSCode[vsEntry]);
	}


	public static ID3D11Buffer	MakeConstantBuffer(ID3D11Device dev, int size)
	{
		BufferDescription	cbDesc	=new BufferDescription();

		//these are kind of odd, but change any one and it breaks		
		cbDesc.BindFlags			=BindFlags.ConstantBuffer;
		cbDesc.ByteWidth			=size;
		cbDesc.CPUAccessFlags		=CpuAccessFlags.None;	//you'd think write, but nope
		cbDesc.MiscFlags			=ResourceOptionFlags.None;
		cbDesc.Usage				=ResourceUsage.Default;	//you'd think dynamic here but nope
		cbDesc.StructureByteStride	=0;

		//alloc
		return	dev.CreateBuffer(cbDesc);
	}


/*
	void LoadParameterData()
	{
		if(!Directory.Exists(mGameRootDir + "/Shaders"))
		{
			return;
		}

		if(!File.Exists(mGameRootDir + "/Shaders/ParameterData.txt"))
		{
			return;
		}

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
	}*/
}