using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Numerics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SharpGen.Runtime;
using UtilityLib;
using Vortice.DXGI;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.Direct3D11.Shader;
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

		static string includeDirectory = "ShadersWin64\\";
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
	IWICImagingFactory	mIF;

	//game directory
	string	mGameRootDir;

	//constant buffer keeper
	CBKeeper	mCBKeeper;

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

	//high precision shaders
	//These need full F32 norms and texcoords etc
	List<string>	mHighPrecisionVS	=new List<string>();

	//shaders
	Dictionary<string, ID3D11VertexShader>	mVShaders	=new Dictionary<string, ID3D11VertexShader>();
	Dictionary<string, ID3D11PixelShader>	mPShaders	=new Dictionary<string, ID3D11PixelShader>();

	//input layouts
	Dictionary<string, ID3D11InputLayout>	mLayouts	=new Dictionary<string, ID3D11InputLayout>();

	//renderstates... was better when these were in hlsl
	Dictionary<string, ID3D11BlendState>		mBlends	=new Dictionary<string, ID3D11BlendState>();
	Dictionary<string, ID3D11DepthStencilState>	mDSSs	=new Dictionary<string, ID3D11DepthStencilState>();
	Dictionary<string, ID3D11SamplerState>		mSSs	=new Dictionary<string, ID3D11SamplerState>();

	//texture 2ds
	Dictionary<string, ID3D11Texture2D>	mTexture2s	=new Dictionary<string, ID3D11Texture2D>();

	//cubemaps
	Dictionary<string, ID3D11Texture2D>	mTextureCubes	=new Dictionary<string, ID3D11Texture2D>();

	//font texture 2ds
	Dictionary<string, ID3D11Texture2D>	mFontTexture2s	=new Dictionary<string, ID3D11Texture2D>();

	//list of texturey things
	Dictionary<string, ID3D11Resource>	mResources	=new Dictionary<string, ID3D11Resource>();

	//list of shader resource views for stuff like textures
	Dictionary<string, ID3D11ShaderResourceView>	mSRVs	=new Dictionary<string, ID3D11ShaderResourceView>();

	//shader resource views for fonts
	Dictionary<string, ID3D11ShaderResourceView>	mFontSRVs	=new Dictionary<string, ID3D11ShaderResourceView>();

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
		mCBKeeper		=new CBKeeper(gd.GD);
		mIFX			=new IncludeFX(gameRootDir);
		mIF				=new IWICImagingFactory();

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
		SaveSourceFileDates(sm);
		LoadResources(gd);
		LoadFonts(gd);
		MakeCommonRenderStates(gd);
	}


	internal ID3D11SamplerState	GetSamplerState(string name)
	{
		if(name != null && mSSs.ContainsKey(name))
		{
			return	mSSs[name];
		}
		return	null;
	}


	internal ID3D11DepthStencilState	GetDepthStencilState(string name)
	{
		if(name != null && mDSSs.ContainsKey(name))
		{
			return	mDSSs[name];
		}
		return	null;
	}


	internal ID3D11BlendState	GetBlendState(string name)
	{
		if(name != null && mBlends.ContainsKey(name))
		{
			return	mBlends[name];
		}
		return	null;
	}


	internal ID3D11Texture2D GetTexture2D(string name)
	{
		if(name == null)
		{
			return	null;
		}
		if(mTexture2s.ContainsKey(name))
		{
			return	mTexture2s[name];
		}
		if(mTextureCubes.ContainsKey(name))
		{
			return	mTextureCubes[name];
		}
		return	null;
	}


	internal Font GetFont(string name)
	{
		if(name == null || !mFonts.ContainsKey(name))
		{
			return	null;
		}
		return	mFonts[name];
	}


	//This can be used to generate input layouts
	public byte[]	GetVSCompiledCode(string name)
	{
		if(name == null || !mVSCode.ContainsKey(name))
		{
			return	null;
		}
		return	mVSCode[name];
	}


	public ID3D11VertexShader	GetVertexShader(string name)
	{
		if(name == null || !mVShaders.ContainsKey(name))
		{
			return	null;
		}
		return	mVShaders[name];
	}

	public ID3D11PixelShader	GetPixelShader(string name)
	{
		if(name == null || !mPShaders.ContainsKey(name))
		{
			return	null;
		}
		return	mPShaders[name];
	}


	public ID3D11ShaderResourceView GetSRV(string name)
	{
		if(name == null || !mSRVs.ContainsKey(name))
		{
			return	null;
		}
		return	mSRVs[name];
	}


	internal ID3D11ShaderResourceView GetFontSRV(string name)
	{
		if(name == null || !mFontSRVs.ContainsKey(name))
		{
			return	null;
		}
		return	mFontSRVs[name];
	}


	internal ID3D11ShaderResourceView ResourceForName(string name)
	{
		if(name == null || mSRVs.ContainsKey(name))
		{
			return	mSRVs[name];
		}
		return	null;
	}


	//for tools mainly
	public List<string> GetTexture2DList(bool bCubesToo)
	{
		List<string>	ret	=new List<string>();

		foreach(KeyValuePair<string, ID3D11Texture2D> tex in mTexture2s)
		{
			ret.Add(tex.Key);
		}

		if(bCubesToo)
		{
			foreach(KeyValuePair<string, ID3D11Texture2D> cube in mTextureCubes)
			{
				ret.Add(cube.Key);
			}
		}

		return	ret;
	}

	public Vector2	GetTextureSize(string texName)
	{
		Vector2	ret	=Vector2.Zero;
		if(mTexture2s.ContainsKey(texName))
		{
			ret.X	=mTexture2s[texName].Description.Width;
			ret.Y	=mTexture2s[texName].Description.Height;
		}
		else if(mTextureCubes.ContainsKey(texName))
		{
			ret.X	=mTextureCubes[texName].Description.Width;
			ret.Y	=mTextureCubes[texName].Description.Height;
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


	//see which hlsl this entry came from
	//this is handy to tell what sort of material bits are needed
	public string GetHLSLName(string vsEntry)
	{
		foreach(KeyValuePair<string, List<string>> files in mVSEntryPoints)
		{
			if(files.Value.Contains(vsEntry))
			{
				return	files.Key;
			}
		}
		return	null;
	}


	Format	SemanticToFormat(string sem, bool b32Floats, RegisterComponentMaskFlags usage)
	{
		if(sem == "POSITION")
		{
			return	Format.R32G32B32_Float;
		}
		else if(sem == "NORMAL")
		{
			return	b32Floats?	Format.R32G32B32A32_Float : Format.R16G16B16A16_Float;
		}
		else if(sem == "BLENDINDICES")
		{
			return	Format.R8G8B8A8_UInt;
		}
		else if(sem == "BLENDWEIGHTS")
		{
			return	Format.R16G16B16A16_Float;
		}
		else if(sem == "TEXCOORD")
		{
			if(usage == RegisterComponentMaskFlags.All)
			{
				return	b32Floats?	Format.R32G32B32A32_Float : Format.R16G16B16A16_Float;
			}
			else
			{
				return	b32Floats?	Format.R32G32_Float : Format.R16G16_Float;
			}
		}
		else if(sem == "COLOR")
		{
			return	Format.R8G8B8A8_UNorm;
		}
		return	Format.Unknown;
	}


	int	SemanticToSize(string sem, bool b32Floats, RegisterComponentMaskFlags usage)
	{
		if(sem == "POSITION")
		{
			return	12;
		}
		else if(sem == "NORMAL")
		{
			return	b32Floats? 16 : 8;
		}
		else if(sem == "BLENDINDICES")
		{
			return	4;
		}
		else if(sem == "BLENDWEIGHTS")
		{
			return	8;
		}
		else if(sem == "TEXCOORD")
		{
			if(usage == RegisterComponentMaskFlags.All)
			{
				return	b32Floats? 16 : 8;
			}
			else
			{
				return	b32Floats? 8 : 4;
			}
		}
		else if(sem == "COLOR")
		{
			return	4;
		}
		return	0;
	}


	ID3D11InputLayout	MakeLayout(ID3D11Device gd, string vsEntry, bool b32Floats)
	{
		if(!mVSCode.ContainsKey(vsEntry))
		{
			return	null;
		}

		ID3D11ShaderReflection	sr	=Compiler.Reflect<ID3D11ShaderReflection>(mVSCode[vsEntry]);

		InputElementDescription	[]ied	=new InputElementDescription[sr.InputParameters.Length];

		int	ofs	=0;
		for(int i=0;i < sr.InputParameters.Length;i++)
		{
			ShaderParameterDescription	spd	=sr.InputParameters[i];

			ied[i]	=new InputElementDescription(spd.SemanticName,
				spd.SemanticIndex, SemanticToFormat(spd.SemanticName, b32Floats, spd.UsageMask), ofs, 0);
			ofs		+=SemanticToSize(spd.SemanticName, b32Floats, spd.UsageMask);
		}

		sr.Release();

		return	gd.CreateInputLayout(ied, mVSCode[vsEntry]);
	}


	public ID3D11InputLayout GetOrCreateLayout(string vsEntry)
	{
		if(mLayouts.ContainsKey(vsEntry))
		{
			return	mLayouts[vsEntry];
		}

		//grab device from any shader stored
		ID3D11Device	dev	=mVShaders.FirstOrDefault().Value.Device;

		ID3D11InputLayout	ret	=MakeLayout(dev, vsEntry, mHighPrecisionVS.Contains(vsEntry));
		if(ret != null)
		{
			mLayouts.Add(vsEntry, ret);
		}
		return	ret;
	}


	//try to grab a dc from the most commonly loaded stuff
	internal ID3D11DeviceContext GetDC()
	{
		if(mVShaders.Count == 0)
		{
			if(mTexture2s.Count == 0)
			{
				return	null;
			}
			return	mTexture2s.FirstOrDefault().Value.Device.ImmediateContext;
		}
		return	mVShaders.FirstOrDefault().Value.Device.ImmediateContext;
	}


	public List<string> GetVSEntryList()
	{
		List<string>	ret	=new List<string>();

		foreach(KeyValuePair<string, List<string>> entry in mVSEntryPoints)
		{
			ret.AddRange(entry.Value);
		}
		return	ret;
	}


	public List<string> GetPSEntryList()
	{
		List<string>	ret	=new List<string>();

		foreach(KeyValuePair<string, List<string>> entry in mPSEntryPoints)
		{
			ret.AddRange(entry.Value);
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


	public void AddTex(ID3D11Device dev, string name, Color []texData, int w, int h)
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


	public CBKeeper GetCBKeeper()
	{
		return	mCBKeeper;
	}


	public System.Drawing.Bitmap	GetTextureBitmap(string texName)
	{
		if(texName == null)
		{
			return	null;
		}

		if(!mTexture2s.ContainsKey(texName))
		{
			return	null;
		}

		//even though we already have this loaded, there appears
		//to be no way to get at the bits, so load it again
		int		w, h;
		byte	[]colArray	=LoadPNGWIC(mIF, mGameRootDir + "\\Textures\\" + texName + ".png",
								out w, out h);

		if(w == 0 && h == 0)
		{
			return	null;
		}

		byte	[]rgb	=new byte[(w * h) * 3];

		//loadpngwic provides RGBA and bitmap wants BGR
		for(int i=0;i < (colArray.Length / 4);i++)
		{
			byte	A, R, G, B;

			R	=colArray[i * 4];
			G	=colArray[1 + (i * 4)];
			B	=colArray[2 + (i * 4)];
			A	=colArray[3 + (i * 4)];

			rgb[i * 3]			=B;
			rgb[1 + (i * 3)]	=G;
			rgb[2 + (i * 3)]	=R;
		}

		System.Drawing.Bitmap				bm		=new System.Drawing.Bitmap(w, h);
		System.Drawing.Rectangle			bmRect	=new System.Drawing.Rectangle(0, 0, w, h);
		System.Drawing.Imaging.BitmapData	bmd		=new System.Drawing.Imaging.BitmapData();

		bmd	=bm.LockBits(bmRect, System.Drawing.Imaging.ImageLockMode.WriteOnly,
			System.Drawing.Imaging.PixelFormat.Format24bppRgb);

		IntPtr	ptr	=bmd.Scan0;

		int	colSize	=(w * h * 3);

		Marshal.Copy(rgb, 0, ptr, colSize);

		bm.UnlockBits(bmd);

		return	bm;
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

		mCBKeeper.FreeAll();

		foreach(KeyValuePair<string, ID3D11Texture2D> tex in mTexture2s)
		{
			tex.Value.Dispose();
		}
		mTexture2s.Clear();

		foreach(KeyValuePair<string, ID3D11Texture2D> tex in mTextureCubes)
		{
			tex.Value.Dispose();
		}
		mTextureCubes.Clear();

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

		//renderstates
		foreach(KeyValuePair<string, ID3D11BlendState> bld in mBlends)
		{
			bld.Value.Dispose();
		}
		foreach(KeyValuePair<string, ID3D11DepthStencilState> dss in mDSSs)
		{
			dss.Value.Dispose();
		}
		foreach(KeyValuePair<string, ID3D11SamplerState> dss in mSSs)
		{
			dss.Value.Dispose();
		}
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

		//cubes
		if(Directory.Exists(mGameRootDir + "/TextureCubes"))
		{
			DirectoryInfo	di	=new DirectoryInfo(mGameRootDir + "/TextureCubes");

			//cubes will be little dirs with nx ny nz px py pz files in them
			DirectoryInfo	[]subDirs	=di.GetDirectories();
			foreach(DirectoryInfo sub in subDirs)
			{
				if(File.Exists(sub.FullName + "/nx.png") &&
					File.Exists(sub.FullName + "/ny.png") &&
					File.Exists(sub.FullName + "/nz.png") &&
					File.Exists(sub.FullName + "/px.png") &&
					File.Exists(sub.FullName + "/py.png") &&
					File.Exists(sub.FullName + "/pz.png"))
				{
					LoadTextureCube(gd, sub.FullName);
				}
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
		dict.Clear();

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
		if(!Directory.Exists(mGameRootDir + "/ShadersWin64"))
		{
			return;
		}
		if(!File.Exists(mGameRootDir + "/ShadersWin64/VSEntryPoints.txt"))
		{
			return;
		}
		if(!File.Exists(mGameRootDir + "/ShadersWin64/PSEntryPoints.txt"))
		{
			return;
		}

		FileStream	fs	=new FileStream(mGameRootDir + "/ShadersWin64/VSEntryPoints.txt", FileMode.Open, FileAccess.Read);
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

		//mark the bsp entry points as high precision
		foreach(KeyValuePair<string, List<string>> vs in mVSEntryPoints)
		{
			if(vs.Key == "BSP")
			{
				foreach(string ent in vs.Value)
				{
					mHighPrecisionVS.Add(ent);
				}
			}
		}

		fs	=new FileStream(mGameRootDir + "/ShadersWin64/PSEntryPoints.txt", FileMode.Open, FileAccess.Read);
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


	//if there's no source files at all, just load compiled
	void LoadCompiledShaders(ID3D11Device dev, ShaderModel sm)
	{
		//need 2 of these for some reason
		ShaderMacro []macs	=new ShaderMacro[2];

		macs[0]	=new ShaderMacro(sm.ToString(), 1);

		if(Directory.Exists(mGameRootDir + "/CompiledShaders"))
		{
			//see if a precompiled exists
			if(Directory.Exists(mGameRootDir + "/CompiledShaders/" + macs[0].Name))
			{
				DirectoryInfo	preDi	=new DirectoryInfo(
					mGameRootDir + "/CompiledShaders/" + macs[0].Name);

				//vert
				foreach(KeyValuePair<string, List<string>> ent in mVSEntryPoints)
				{
					foreach(string e in ent.Value)
					{
						if(mVSCode.ContainsKey(e))
						{
							//already loaded
							continue;
						}

						FileInfo[]	preFi	=preDi.GetFiles(e + ".cso", SearchOption.TopDirectoryOnly);

						if(preFi.Length == 1)
						{
							LoadCompiledShader(dev, preFi[0].DirectoryName, preFi[0].Name, macs);
						}
					}
				}

				//pixel
				foreach(KeyValuePair<string, List<string>> ent in mPSEntryPoints)
				{
					foreach(string e in ent.Value)
					{
						if(mPSCode.ContainsKey(e))
						{
							//already loaded
							continue;
						}

						FileInfo[]	preFi	=preDi.GetFiles(e + ".cso", SearchOption.TopDirectoryOnly);

						if(preFi.Length == 1)
						{
							LoadCompiledShader(dev, preFi[0].DirectoryName, preFi[0].Name, macs);
						}
					}
				}
			}
		}
	}


	void CompileShaders(List<string> srcFiles, ID3D11Device dev, ShaderModel sm)
	{
		if(srcFiles.Count == 0)
		{
			return;
		}

		//need 2 of these for some reason
		ShaderMacro []macs	=new ShaderMacro[2];

		macs[0]	=new ShaderMacro(sm.ToString(), 1);

		//count up how many compiles needed
		List<string>	counted	=new List<string>();
		foreach(string srcFile in srcFiles)
		{
			string	src	=FileUtil.StripExtension(srcFile);

			if(mVSEntryPoints.ContainsKey(src))
			{
				foreach(string entry in mVSEntryPoints[src])
				{
					if(!mVSCode.ContainsKey(entry))
					{
						if(!counted.Contains(entry))
						{
							counted.Add(entry);
						}
					}
				}
			}

			if(mPSEntryPoints.ContainsKey(src))
			{
				foreach(string entry in mPSEntryPoints[src])
				{
					if(!mPSCode.ContainsKey(entry))
					{
						if(!counted.Contains(entry))
						{
							counted.Add(entry);
						}
					}
				}
			}
		}

		//notify of how many to go
		Misc.SafeInvoke(eCompileNeeded, counted.Count);

		counted.Clear();

		int	compiled	=0;
		foreach(string srcFile in srcFiles)
		{
			string	src	=FileUtil.StripExtension(srcFile);

			//vertex entry points
			if(mVSEntryPoints.ContainsKey(src))
			{
				foreach(string entry in mVSEntryPoints[src])
				{
					if(mVSCode.ContainsKey(entry))
					{
						continue;	//already loaded
					}

					CompileShader(dev, mGameRootDir + "/ShadersWin64", srcFile,
						entry, ShaderEntryType.Vertex, sm, macs);

					Misc.SafeInvoke(eCompileDone, ++compiled);
				}
			}

			//pixel entry points
			if(mPSEntryPoints.ContainsKey(src))
			{
				foreach(string entry in mPSEntryPoints[src])
				{
					if(mPSCode.ContainsKey(entry))
					{
						continue;	//already loaded
					}

					CompileShader(dev, mGameRootDir + "/ShadersWin64", srcFile,
						entry, ShaderEntryType.Pixel, sm, macs);

					Misc.SafeInvoke(eCompileDone, ++compiled);
				}
			}
		}
	}


	//load all shaders in the shaders folder, compiling as needed
	void LoadShaders(ID3D11Device dev, ShaderModel sm)
	{
		//need 2 of these for some reason
		ShaderMacro []macs	=new ShaderMacro[2];

		macs[0]	=new ShaderMacro(sm.ToString(), 1);

		string	srcDir	=mGameRootDir + "/ShadersWin64";
		string	cmpDir	=mGameRootDir + "/CompiledShaders/" + macs[0].Name;

		//see if Shader folder exists in Content
		if(!Directory.Exists(srcDir))
		{
			//if no source just try to load compiled
			LoadCompiledShaders(dev, sm);
			return;
		}

		//see if compiled folder exists
		if(!Directory.Exists(cmpDir))
		{
			Directory.CreateDirectory(cmpDir);
		}

		DirectoryInfo	sdi	=new DirectoryInfo(srcDir);
		DirectoryInfo	cdi	=new DirectoryInfo(cmpDir);

		//see which source needs compile
		List<string>	needsCompile	=CheckSourceTimeStamps(cdi, sdi);

		//if any are headers, just compile everything
		bool	bCompileAll	=false;
		foreach(string nc in needsCompile)
		{
			if(nc.EndsWith(".hlsli"))
			{
				bCompileAll	=true;
				break;
			}
		}

		if(bCompileAll)
		{
			CompileShaders(GetShaderSourceFiles(), dev, sm);
		}
		else
		{
			CompileShaders(needsCompile, dev, sm);
		}

		if(!bCompileAll)
		{
			//the ones we didn't compile just need loading from disc
			LoadCompiledShaders(dev, sm);
		}

		//create shaders from bytecode
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


	void SaveSourceFileDates(ShaderModel sm)
	{
		if(!Directory.Exists(mGameRootDir + "/CompiledShaders"))
		{
			return;
		}

		DirectoryInfo	src	=new DirectoryInfo(mGameRootDir + "/ShadersWin64/");

		if(!src.Exists)
		{
			return;
		}

		DirectoryInfo	di	=new DirectoryInfo(mGameRootDir + "/CompiledShaders/" + sm.ToString() + "/");

		FileStream	fs	=new FileStream(
			di.FullName + "ShaderSource.TimeStamps",
			FileMode.Create, FileAccess.Write);

		Debug.Assert(fs != null);

		BinaryWriter	bw	=new BinaryWriter(fs);

		Dictionary<string, DateTime>	stamps	=GetSourceTimeStamps(src);

		bw.Write(stamps.Count);
		foreach(KeyValuePair<string, DateTime> time in stamps)
		{
			bw.Write(time.Key);
			bw.Write(time.Value.Ticks);
		}

		bw.Close();
		fs.Close();
	}


	List<string>	GetShaderSourceFiles()
	{
		List<string>	files	=new List<string>();

		if(Directory.Exists(mGameRootDir + "/ShadersWin64"))
		{
			DirectoryInfo	di	=new DirectoryInfo(mGameRootDir + "/ShadersWin64/");

			FileInfo	[]fi	=di.GetFiles("*.hlsl", SearchOption.AllDirectories);
			foreach(FileInfo f in fi)
			{
				files.Add(f.Name);
			}

			//headers too
			fi	=di.GetFiles("*.hlsli", SearchOption.AllDirectories);
			foreach(FileInfo f in fi)
			{
				files.Add(f.Name);
			}
		}
		return	files;
	}


	Dictionary<string, DateTime> GetSourceTimeStamps(DirectoryInfo di)
	{
		FileInfo[]		fi	=di.GetFiles("*.hlsl", SearchOption.AllDirectories);

		Dictionary<string, DateTime>	ret	=new Dictionary<string, DateTime>();

		foreach(FileInfo f in fi)
		{
			ret.Add(f.Name, f.LastWriteTime);
		}

		//headers too
		fi	=di.GetFiles("*.hlsli", SearchOption.AllDirectories);
		foreach(FileInfo f in fi)
		{
			ret.Add(f.Name, f.LastWriteTime);
		}
		return	ret;
	}


	//returns a list of source files that have changed and probably need recompile
	List<string> CheckSourceTimeStamps(DirectoryInfo preDi, DirectoryInfo srcDi)
	{
		FileInfo[]	hTime	=preDi.GetFiles("ShaderSource.TimeStamps", SearchOption.TopDirectoryOnly);
		if(hTime.Length != 1)
		{
			return	GetShaderSourceFiles();
		}

		FileStream	fs	=new FileStream(hTime[0].DirectoryName + "\\" + hTime[0].Name, FileMode.Open, FileAccess.Read);
		if(fs == null)
		{
			return	GetShaderSourceFiles();
		}

		List<string>	needsCompile	=new List<string>();

		BinaryReader	br	=new BinaryReader(fs);

		//load stored dates for previously compiled
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

		//get current on disc dates for source files
		Dictionary<string, DateTime>	onDisk	=GetSourceTimeStamps(srcDi);

		//check the timestamp data against the dates
		foreach(KeyValuePair<string, DateTime> tstamp in onDisk)
		{
			//no record of tstamp source file?
			if(!times.ContainsKey(tstamp.Key))
			{
				if(!needsCompile.Contains(tstamp.Key))
				{
					needsCompile.Add(tstamp.Key);
				}
				continue;
			}

			//record is older than current source?
			if(times[tstamp.Key] < tstamp.Value)
			{
				if(!needsCompile.Contains(tstamp.Key))
				{
					needsCompile.Add(tstamp.Key);
				}
				continue;
			}
		}

		return	needsCompile;
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


	void CompileShader(ID3D11Device dev, string dir, string file,
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


	void LoadTextureCube(GraphicsDevice gd, string path)
	{
		int	texIndex	=path.LastIndexOf("TextureCubes");

		string	afterTex	="";

		if((texIndex +13) < path.Length)
		{
			afterTex	=path.Substring(texIndex + 13);
		}

		int	w, h;

		byte	[][]colArray	=new byte[6][];

		colArray[0]	=LoadPNGWIC(mIF, path + "/px.png", out w, out h);
		colArray[1]	=LoadPNGWIC(mIF, path + "/nx.png", out w, out h);
		colArray[2]	=LoadPNGWIC(mIF, path + "/py.png", out w, out h);
		colArray[3]	=LoadPNGWIC(mIF, path + "/ny.png", out w, out h);
		colArray[4]	=LoadPNGWIC(mIF, path + "/pz.png", out w, out h);
		colArray[5]	=LoadPNGWIC(mIF, path + "/nz.png", out w, out h);

		for(int i=0;i < 6;i++)
		{
			PreMultAndLinear(colArray[i], w, h);
		}

		ID3D11Texture2D	finalTex	=MakeTextureCube(gd.GD, colArray, w, h);

		mResources.Add(afterTex, finalTex as ID3D11Resource);
		mTextureCubes.Add(afterTex, finalTex);

		ID3D11ShaderResourceView	srv	=gd.GD.CreateShaderResourceView(finalTex);
		srv.DebugName	=afterTex;

		mSRVs.Add(afterTex, srv);
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


	unsafe ID3D11Texture2D MakeTextureCube(ID3D11Device dev, byte [][]colors, int width, int height)
	{
		Texture2DDescription	texDesc	=new Texture2DDescription();
		texDesc.ArraySize				=6;
		texDesc.BindFlags				=BindFlags.ShaderResource;
		texDesc.CPUAccessFlags			=CpuAccessFlags.None;
		texDesc.MipLevels				=1;
		texDesc.MiscFlags				=ResourceOptionFlags.TextureCube;
		texDesc.Usage					=ResourceUsage.Immutable;
		texDesc.Width					=width;
		texDesc.Height					=height;
		texDesc.Format					=Format.R8G8B8A8_UNorm;
		texDesc.SampleDescription		=new SampleDescription(1, 0);

		ID3D11Texture2D	tex;

		SubresourceData	[]srd	=new SubresourceData[6];

		//alloc temp space for color data
		List<IntPtr>	texData	=new List<IntPtr>();
		for(int i=0;i < 6;i++)
		{
			IntPtr	td	=Marshal.AllocHGlobal(width * height * 4);

			Marshal.Copy(colors[i], 0, td, width * height * 4);

			texData.Add(td);

			srd[i]	=new SubresourceData(td, width * 4);
		}
				
		tex	=dev.CreateTexture2D(texDesc, srd);

		for(int i=0;i < 6;i++)
		{
			Marshal.FreeHGlobal(texData[i]);
		}

		return	tex;
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


	unsafe ID3D11Texture2D MakeTexture(ID3D11Device dev, Color []colors, int width, int height)
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

		SubresourceData	[]srd	=new SubresourceData[1];

		fixed(void *pColors = colors)
		{
			srd[0]	=new SubresourceData(pColors, width * 4);
				
			tex	=dev.CreateTexture2D(texDesc, srd);
		}
		return	tex;
	}


	void PreMultAndLinear(byte []colors, int width, int height)
	{
		float	oo255	=1.0f / 255.0f;

		for(int y=0;y < height;y++)
		{
			int	ofs	=y * width * 4;

			for(int x=0;x < width;x++)
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
		FileStream	fs	=new FileStream(path, FileMode.Open, FileAccess.Read);

		IWICBitmapDecoder	pbd	=wif.CreateDecoderFromStream(fs);

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

		fs.Close();

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


	void MakeCommonRenderStates(GraphicsDevice gd)
	{
		//samplers
		SamplerDescription	sd	=new SamplerDescription(
			Filter.MinMagMipLinear,
			TextureAddressMode.Clamp,
			TextureAddressMode.Clamp,
			TextureAddressMode.Clamp,
			0f, 16,
			ComparisonFunction.Less,
			0, float.MaxValue);
		mSSs.Add("LinearClamp", gd.GD.CreateSamplerState(sd));

		sd.Filter				=Filter.MinMagMipLinear;
		sd.AddressU				=TextureAddressMode.Wrap;
		sd.AddressV				=TextureAddressMode.Wrap;
		sd.AddressW				=TextureAddressMode.Wrap;		
		mSSs.Add("LinearWrap", gd.GD.CreateSamplerState(sd));

		sd.Filter	=Filter.MinMagMipPoint;
		mSSs.Add("PointWrap", gd.GD.CreateSamplerState(sd));

		sd.AddressU				=TextureAddressMode.Wrap;
		sd.AddressV				=TextureAddressMode.Wrap;
		sd.AddressW				=TextureAddressMode.Wrap;
		mSSs.Add("PointClamp", gd.GD.CreateSamplerState(sd));

		
		//depth stencils
		DepthStencilDescription	dsd	=new DepthStencilDescription(true,
			DepthWriteMask.All, ComparisonFunction.Less);
		mDSSs.Add("EnableDepth", gd.GD.CreateDepthStencilState(dsd));

		dsd.DepthWriteMask	=DepthWriteMask.Zero;
		dsd.DepthFunc		=ComparisonFunction.Equal;
		mDSSs.Add("ShadowDepth", gd.GD.CreateDepthStencilState(dsd));

		dsd.DepthEnable		=false;
		dsd.DepthFunc		=ComparisonFunction.Always;
		mDSSs.Add("DisableDepth", gd.GD.CreateDepthStencilState(dsd));

		dsd.DepthEnable		=true;
		dsd.DepthFunc		=ComparisonFunction.Less;
		mDSSs.Add("DisableDepthWrite", gd.GD.CreateDepthStencilState(dsd));

		dsd.DepthWriteMask	=DepthWriteMask.All;
		dsd.DepthFunc		=ComparisonFunction.Always;
		mDSSs.Add("DisableDepthTest", gd.GD.CreateDepthStencilState(dsd));

		//blendstates, note that BlendOp has been moved to rendertarget?
		//blend op and blend op alpha are both ADD on this one
		BlendDescription	bd	=new BlendDescription(Blend.One,
			Blend.InverseSourceAlpha, Blend.Zero, Blend.Zero);
		mBlends.Add("AlphaBlending", gd.GD.CreateBlendState(bd));

		//blend op and blend op alpha are both MIN on this one
		bd	=new BlendDescription(Blend.One,
			Blend.One, Blend.One, Blend.One);
		mBlends.Add("MultiChannelDepth", gd.GD.CreateBlendState(bd));

		//blend op is REV_SUBTRACT and blend op alpha is ADD
		bd	=new BlendDescription(Blend.One,
			Blend.One, Blend.One, Blend.One);
		mBlends.Add("ShadowBlending", gd.GD.CreateBlendState(bd));

		bd	=new BlendDescription(Blend.One, Blend.Zero);
		mBlends.Add("NoBlending", gd.GD.CreateBlendState(bd));
	}
}