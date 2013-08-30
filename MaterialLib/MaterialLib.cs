using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using UtilityLib;


namespace MaterialLib
{
	public partial class MaterialLib
	{
		Dictionary<string, Material>	mMats	=new Dictionary<string, Material>();
		Dictionary<string, Effect>		mFX		=new Dictionary<string, Effect>();
		Dictionary<string, Texture2D>	mMaps	=new Dictionary<string, Texture2D>();
		Dictionary<string, TextureCube>	mCubes	=new Dictionary<string, TextureCube>();

		//state block pool
		StateBlockPool	mStateBlockPool	=new StateBlockPool();

		//references to the content managers
		ContentManager	mGameContent;	//may contain game specific shaders
		ContentManager	mShaderLib;		//shared shaders used by every game


		//two content managers, tool or game
		public MaterialLib(GraphicsDevice gd, ContentManager cm, ContentManager scm, bool bTool)
		{
			mGameContent	=cm;
			mShaderLib		=scm;

			if(bTool)
			{
				LoadShaders();
				LoadTextures();
				LoadCubes(gd);
			}
		}


		//for just material creation, nothing else will work
		public MaterialLib()
		{
		}


		public Material CreateMaterial()
		{
			Material	mat	=new Material(mStateBlockPool, mMaps, mCubes);

			return	mat;
		}


		public Material GetMaterial(string matName)
		{
			if(mMats.ContainsKey(matName))
			{
				return	mMats[matName];
			}
			return	null;
		}


		public Effect GetShader(string shaderName)
		{
			if(mFX.ContainsKey(shaderName))
			{
				return	mFX[shaderName];
			}
			return	null;
		}


		public Dictionary<string, Material> GetMaterials()
		{
			return	mMats;
		}


		public Dictionary<string, Effect> GetShaders()
		{
			return	mFX;
		}


		public Dictionary<string, Texture2D> GetTextures()
		{
			return	mMaps;
		}


		public Dictionary<string, TextureCube> GetTextureCubes()
		{
			return	mCubes;
		}


		//this only saves referenced stuff
		//the tool side will still need to enumerate
		//all the textures / shaders
		public void SaveToFile(string fileName)
		{
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
				mat.Value.GetTexturesInUse(texInUse);
				mat.Value.GetTextureCubesInUse(cubeInUse);

				string	shd	=mat.Value.ShaderName;
				if(!shdInUse.Contains(shd))
				{
					shdInUse.Add(shd);
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
				mat.Value.Write(bw);
			}

			bw.Close();
			fs.Close();
		}


		public bool ReadFromFile(string fileName, bool bTool, GraphicsDevice gd)
		{
			Stream	file	=null;
			if(bTool)
			{
				file	=new FileStream(fileName, FileMode.Open, FileAccess.Read);
			}
			else
			{
				file	=UtilityLib.FileUtil.OpenTitleFile(fileName);
			}

			if(file == null)
			{
				return	false;
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
				return	false;
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

			StripTextureExtensions(texNeeded);
			if(!bTool)
			{
				LoadNewContent(gd, shdNeeded, texNeeded, cubeNeeded);
			}

			//load the actual material values
			int	numMaterials	=br.ReadInt32();
			for(int i=0;i < numMaterials;i++)
			{
				Material	m	=new Material(mStateBlockPool, mMaps, mCubes);

				m.Read(br);
				mMats.Add(m.Name, m);
			}

			br.Close();
			file.Close();

			return	true;
		}


		//tool side only
		public bool MergeFromFile(string fileName)
		{
			Stream	file	=new FileStream(fileName, FileMode.Open, FileAccess.Read);

			if(file == null)
			{
				return	false;
			}
			BinaryReader	br	=new BinaryReader(file);

			//read magic number
			UInt32	magic	=br.ReadUInt32();

			if(magic != 0xFA77DA77)
			{
				br.Close();
				file.Close();
				return	false;
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
				Material	m	=new Material(mStateBlockPool, mMaps, mCubes);

				m.Read(br);

				while(mMats.ContainsKey(m.Name))
				{
					m.Name	+="2";
				}

				mMats.Add(m.Name, m);
			}

			br.Close();
			file.Close();

			return	true;
		}


		public void AddMaterial(Material mat)
		{
			mMats.Add(mat.Name, mat);

			mat.UpdateTexPointers(mMaps, mCubes);
		}


		public void NukeMaterial(string key)
		{
			if(mMats.ContainsKey(key))
			{
				mMats.Remove(key);
			}
		}


		//fill in the material emissive colors
		//based on an average of the colors in the textures
		public void AssignEmissives()
		{
			foreach(KeyValuePair<string, Material> mat in mMats)
			{
				AssignEmissive(mat.Key);
			}
		}


		//writes out the emissive values in the material lib
		//this is useful for the lighting stage, so radiosity
		//can pick up material colours to bounce around
		public void SaveEmissives(string fileName)
		{
			string	emmName	=UtilityLib.FileUtil.StripExtension(fileName);

			emmName	+=".Emissives";

			FileStream		fs	=new FileStream(emmName, FileMode.Create, FileAccess.Write);
			BinaryWriter	bw	=new BinaryWriter(fs);

			UInt32	magic	=0xED1551BE;
			bw.Write(magic);

			bw.Write(mMats.Count);
			foreach(KeyValuePair<string, Material> mat in mMats)
			{
				bw.Write(mat.Value.Name);
				bw.Write(mat.Value.Emissive.PackedValue);
			}

			bw.Close();
			fs.Close();
		}


		internal void StripTextureExtensions(List<string> texNames)
		{
			for(int i=0;i < texNames.Count;i++)
			{
				texNames[i]	=FileUtil.StripExtension(texNames[i]);
			}
		}


		void AssignEmissive(string matName)
		{
			if(!mMats.ContainsKey(matName))
			{
				return;
			}

			Material	mat	=mMats[matName];
			object		val	=mat.GetParameterValue("mTexture");
			if(val == null)
			{
				return;
			}

			Texture2D	map		=val as Texture2D;
			if(map == null)
			{
				return;
			}

			int	size	=map.Width * map.Height;

			Color	[]colors	=new Color[size];
			map.GetData<Color>(colors);

			Int64	red		=0;
			Int64	green	=0;
			Int64	blue	=0;
			foreach(Color c in colors)
			{
				red		+=c.R;
				green	+=c.G;
				blue	+=c.B;
			}

			red		/=size;
			green	/=size;
			blue	/=size;

			Debug.Assert(red < 256);
			Debug.Assert(green < 256);
			Debug.Assert(blue < 256);

			mat.SetEmissive((byte)red, (byte)green, (byte)blue);
		}


		public void BoostTexSizes(bool bUp)
		{
			foreach(KeyValuePair<string, Material> mat in mMats)
			{
				BoostTexSize(mat.Key, bUp);
			}
		}


		public void BoostTexSize(string matName, bool bUp)
		{
			if(mMats.ContainsKey(matName))
			{
				Vector2	val	=Vector2.Zero;

				object	texSize	=mMats[matName].GetParameterValue("mTexSize");

				if(texSize is string)
				{
					val	=Misc.StringToVector2(texSize as string);
				}
				else
				{
					val	=(Vector2)texSize;
				}

				if(bUp)
				{
					val	*=2;
				}
				else
				{
					val	*=0.5f;
				}
				mMats[matName].SetParameter("mTexSize", val);
			}
		}


		public void GuessTextures()
		{
			//match by name
			foreach(KeyValuePair<string, Material> mat in mMats)
			{
				object	curVal	=mat.Value.GetParameterValue("mTexture");
				if(curVal is Texture)
				{
					continue;
				}

				string	rawMatName	=mat.Key;
				if(rawMatName.Contains("*"))
				{
					rawMatName	=rawMatName.Substring(0, rawMatName.IndexOf('*'));
				}

				foreach(KeyValuePair<string, Texture2D> tex in mMaps)
				{
					if(tex.Key.Contains(rawMatName)
						|| tex.Key.Contains(rawMatName.ToLower()))
					{
						mat.Value.SetParameter("mTexture", tex.Value);
						mat.Value.SetParameter("mbTextureEnabled", true);
						mat.Value.SetParameter("mTexSize",
							(Vector2.UnitX * tex.Value.Width)
							+ (Vector2.UnitY * tex.Value.Height));
						break;
					}
				}
			}

			if(mCubes.Count <= 0)
			{
				return;
			}

			TextureCube	firstCube	=null;
			foreach(KeyValuePair<string, TextureCube> cube in mCubes)
			{
				firstCube	=cube.Value;
				break;
			}

			//look for sky materials
			foreach(KeyValuePair<string, Material> mat in mMats)
			{
				if(!mat.Key.EndsWith("*Sky"))
				{
					continue;
				}

				mat.Value.SetParameter("mTexture", firstCube);
			}
		}


		public void NukeAllMaterials()
		{
			mMats.Clear();
		}


		public void SetTriLightValues(Vector4 colorVal, Vector3 lightDir)
		{
			foreach(KeyValuePair<string, Material> mat in mMats)
			{
				mat.Value.SetTriLightValues(colorVal, lightDir);
			}
		}


		public void AddMap(string name, Texture2D map)
		{
			//check if exists
			if(mMaps.ContainsKey(name))
			{
				mMaps.Remove(name);
			}
			mMaps.Add(name, map);
		}


		public Texture2D GetTexture(string name)
		{
			if(mMaps.ContainsKey(name))
			{
				return	mMaps[name];
			}
			return	null;
		}


		public TextureCube GetTextureCube(string name)
		{
			if(mCubes.ContainsKey(name))
			{
				return	mCubes[name];
			}
			return	null;
		}


		public Effect GetMaterialShader(string name)
		{
			if(mMats.ContainsKey(name))
			{
				if(mFX.ContainsKey(mMats[name].ShaderName))
				{
					return	mFX[mMats[name].ShaderName];
				}
			}
			return	null;
		}


		//only used by the tools, this makes sure
		//the names used as keys in the dictionaries
		//match the name properties in the objects
		public void UpdateDictionaries()
		{
			restart:
			foreach(KeyValuePair<string, Material> mat in mMats)
			{
				if(mat.Key != mat.Value.Name)
				{
					mMats.Remove(mat.Key);
					mMats.Add(mat.Value.Name, mat.Value);
					goto restart;
				}
			}
		}


		//updates a shader with a material's props
		public void ApplyParameters(string matName)
		{
			if(!mMats.ContainsKey(matName))
			{
				return;
			}

			Material	mat	=mMats[matName];

			Effect	fx	=GetMaterialShader(matName);
			if(fx == null)
			{
				return;
			}

			mat.ApplyShaderParameters(fx);
		}


		public void SetMaterialParameter(string matName, string name, object val)
		{
			if(!mMats.ContainsKey(matName))
			{
				return;
			}

			mMats[matName].SetParameter(name, val);
		}


		public void SetParameterOnAll(string paramName, object val)
		{
			foreach(KeyValuePair<string, Material> mat in mMats)
			{
				mat.Value.SetParameter(paramName, val);
			}
		}


		public void DrawMap(string map, SpriteBatch sb)
		{
			if(!mMaps.ContainsKey(map))
			{
				return;
			}

			sb.Begin();
			sb.Draw(mMaps[map], Vector2.One * 10.0f, Color.White);
			sb.End();
		}


		public void RefreshShaderParameters()
		{
			foreach(KeyValuePair<string, Material> mat in mMats)
			{
				string	shader	=mat.Value.ShaderName;

				if(!mFX.ContainsKey(shader))
				{
					continue;
				}
				Effect	fx	=mFX[shader];

				mat.Value.UpdateShaderParameters(fx);
			}
		}


		public void UpdateWVP(Matrix world, Matrix view, Matrix proj, Vector3 eyePos)
		{
			SetParameterOnAll("mWorld", world);
			SetParameterOnAll("mView", view);
			SetParameterOnAll("mProjection", proj);
			SetParameterOnAll("mEyePos", eyePos);
		}


		void PurgeStates()
		{
			List<BlendState>		bss	=new List<BlendState>();
			List<DepthStencilState>	dss	=new List<DepthStencilState>();
			List<RasterizerState>	rss	=new List<RasterizerState>();
			foreach(KeyValuePair<string, Material> mat in mMats)
			{
				bss.Add(mat.Value.BlendState);
				dss.Add(mat.Value.DepthState);
				rss.Add(mat.Value.RasterState);
			}

			mStateBlockPool.PurgeBlendStates(bss);
			mStateBlockPool.PurgeDepthStates(dss);
			mStateBlockPool.PurgeRasterStates(rss);
		}


		//load up content requested by materials
		void LoadNewContent(GraphicsDevice gd, List<string> shdNeeded,
			List<string> texNeeded, List<string> cubeNeeded)
		{
			//eliminate duplicates
			List<string>	texs	=new List<string>();
			foreach(string tex in texNeeded)
			{
				if(tex == "LightMapAtlas")
				{
					continue;
				}
				if(!texs.Contains(tex))
				{
					texs.Add(tex);
				}
			}

			List<string>	cubeTexs	=new List<string>();
			foreach(string tex in cubeNeeded)
			{
				if(tex == "LightMapAtlas")
				{
					continue;
				}
				if(!cubeTexs.Contains(tex))
				{
					cubeTexs.Add(tex);
				}
			}

			//load shaders
			foreach(string shd in shdNeeded)
			{
				if(shd != null && shd != "" && !mFX.ContainsKey(shd))
				{
					Effect	fx	=null;
					if(File.Exists(mGameContent.RootDirectory +
						"/" + shd + ".xnb"))
					{
						fx	=mGameContent.Load<Effect>(shd);
					}
					else if(File.Exists(mShaderLib.RootDirectory +
						"/" + shd + ".xnb"))
					{
						fx	=mShaderLib.Load<Effect>(shd);
					}

					if(fx != null)
					{
						mFX.Add(shd, fx);
					}
				}
			}

			//load textures
			//shouldn't really be any textures in the shader lib
			//but I check for it anyway if not found
			foreach(string tex in texs)
			{
				if(tex == "")
				{
					continue;
				}

				Texture2D	t	=null;

				//I used to have special code here to strip + out of
				//the key to load a texture, because the xbox can't have
				//any + characters in a filename, but the tex file has
				//to be renamed anyway, so it's not that hard to change it
				//in the material lib.  So yea don't do filenames with +
				//some older formats used to key with the textures path on front
				int		tdPos	=tex.LastIndexOf("Textures");
				if(tdPos != -1)
				{
					string	key		=tex.Substring(tdPos + 9, tex.Length - 9);
					if(!LoadTexture(mGameContent, tex, key))
					{
						LoadTexture(mShaderLib, tex, tex);
					}
				}
				else
				{
					string	path	="Textures/" + tex;
					if(!LoadTexture(mGameContent, path, tex))
					{
						LoadTexture(mShaderLib, path, tex);
					}
				}

				if(t != null)
				{
					mMaps.Add(tex, t);
				}
			}

			//load cubetex
			foreach(string tex in cubeTexs)
			{
				if(tex == "")
				{
					continue;
				}

				int		tdPos	=tex.LastIndexOf("TextureCubes");
				if(tdPos != -1)
				{
					string	key		=tex.Substring(tdPos + 12, tex.Length - 12);
					if(!LoadTextureCube(gd, mGameContent, tex, key))
					{
						LoadTextureCube(gd, mShaderLib, tex, tex);
					}
				}
				else
				{
					string	path	="TextureCubes/" + tex;
					if(!LoadTextureCube(gd, mGameContent, path, tex))
					{
						LoadTextureCube(gd, mShaderLib, path, tex);
					}
				}
			}
		}


		//load shaders in the content/shaders folder
		void LoadShaders()
		{
#if XBOX
			//this is a toolside method, stubbed out on xbox
			return;
#else
			//see if Shader folder exists in Content
			if(Directory.Exists(mShaderLib.RootDirectory + "/Shaders"))
			{
				DirectoryInfo	di	=new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory
					+ mShaderLib.RootDirectory + "/Shaders/");

				FileInfo[]		fi	=di.GetFiles("*.xnb", SearchOption.AllDirectories);
				foreach(FileInfo f in fi)
				{
					LoadShader(f.DirectoryName, f.Name, mShaderLib);
				}
			}

			//try game as well
			if(Directory.Exists(mGameContent.RootDirectory + "/Shaders"))
			{
				DirectoryInfo	di	=new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory
					+ mGameContent.RootDirectory + "/Shaders/");

				FileInfo[]		fi	=di.GetFiles("*.xnb", SearchOption.AllDirectories);
				foreach(FileInfo f in fi)
				{
					LoadShader(f.DirectoryName, f.Name, mGameContent);
				}
			}
#endif
		}


		Texture2D LoadTex(ContentManager cm, string fileName)
		{
			if(!File.Exists(cm.RootDirectory + "/" + fileName + ".xnb"))
			{
				return	null;
			}
			return	cm.Load<Texture2D>(fileName);
		}


		bool LoadTexture(ContentManager cm, string fileName, string key)
		{
			if(mMaps.ContainsKey(key))
			{
				return	false;
			}

			Texture2D	tex	=LoadTex(cm, fileName);
			if(tex == null)
			{
				return	false;
			}

			tex.Name	=key;

			mMaps.Add(key, tex);

			return	true;
		}


		void LoadShader(string dirName, string fileName, ContentManager cm)
		{
			string	path	=dirName + "\\" + fileName;

			//strip off extension
			path	=UtilityLib.FileUtil.StripExtension(path);

			//strip back the content dir
			path	=path.Substring(path.LastIndexOf(cm.RootDirectory)
				+ cm.RootDirectory.Length + 1);

			//load shader
			Effect	fx	=cm.Load<Effect>(path);

			mFX.Add(path, fx);
		}


		string StripCubeDesignator(string fn)
		{
			if(fn.EndsWith("Up"))
			{
				return	fn.Substring(0, fn.Length - 2);
			}
			else if(fn.EndsWith("Down"))
			{
				return	fn.Substring(0, fn.Length - 4);
			}
			else if(fn.EndsWith("Left"))
			{
				return	fn.Substring(0, fn.Length - 4);
			}
			else if(fn.EndsWith("Right"))
			{
				return	fn.Substring(0, fn.Length - 5);
			}
			else if(fn.EndsWith("Front"))
			{
				return	fn.Substring(0, fn.Length - 5);
			}
			else if(fn.EndsWith("Back"))
			{
				return	fn.Substring(0, fn.Length - 4);
			}
			return	"";
		}


		void SetCubeFace(TextureCube cube, Texture2D face, CubeMapFace cmf)
		{
			Debug.Assert(face.Format == SurfaceFormat.Color);

			Color	[]faceTexels	=new Color[face.Width * face.Height];

			face.GetData<Color>(faceTexels);

			if(cmf == CubeMapFace.PositiveY || cmf == CubeMapFace.NegativeY)
			{
				List<Color>	revTex	=new List<Color>();
				for(int y=0;y < face.Height;y++)
				{
					for(int x=face.Width - 1;x >= 0;x--)
					{
						revTex.Add(faceTexels[(x * face.Width) + y]);
					}
				}

				if(cmf == CubeMapFace.NegativeY)
				{
					revTex.Reverse();
				}

				faceTexels	=revTex.ToArray();
			}

			cube.SetData<Color>(cmf, faceTexels);
		}


		bool LoadTextureCube(GraphicsDevice gd, ContentManager cm, string fn, string key)
		{
			Texture2D	up		=LoadTex(mGameContent, fn + "Up");
			Texture2D	down	=LoadTex(mGameContent, fn + "Down");
			Texture2D	left	=LoadTex(mGameContent, fn + "Left");
			Texture2D	right	=LoadTex(mGameContent, fn + "Right");
			Texture2D	front	=LoadTex(mGameContent, fn + "Front");
			Texture2D	back	=LoadTex(mGameContent, fn + "Back");

			Debug.Assert((up.Width & down.Width & left.Width & right.Width & front.Width & back.Width) == up.Width);
			Debug.Assert((up.Height & down.Height & left.Height & right.Height & front.Height & back.Height) == up.Height);
			Debug.Assert(up.Width == up.Height);

			TextureCube	tc	=new TextureCube(gd, up.Width, false, SurfaceFormat.Color);
			SetCubeFace(tc, up, CubeMapFace.PositiveY);
			SetCubeFace(tc, down, CubeMapFace.NegativeY);
			SetCubeFace(tc, left, CubeMapFace.PositiveX);
			SetCubeFace(tc, right, CubeMapFace.NegativeX);
			SetCubeFace(tc, front, CubeMapFace.PositiveZ);
			SetCubeFace(tc, back, CubeMapFace.NegativeZ);

			tc.Name	=key;

			mCubes.Add(key, tc);

			return	true;
		}


		void LoadCubes(GraphicsDevice gd)
		{
#if XBOX
			//this is a toolside method, stubbed out on xbox
			return;
#else
			if(Directory.Exists(mGameContent.RootDirectory + "/TextureCubes"))
			{
				DirectoryInfo	di	=new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory
					+ mGameContent.RootDirectory + "/TextureCubes/");

				//make a list to keep track of duplicates (6 tex files per cube)
				Dictionary<string, int>		baseNamesUsed	=new Dictionary<string, int>();
				Dictionary<string, string>	dirs			=new Dictionary<string, string>();

				FileInfo[]		fi	=di.GetFiles("*.xnb", SearchOption.AllDirectories);
				foreach(FileInfo f in fi)
				{
					string	fn	=UtilityLib.FileUtil.StripExtension(f.Name);
					string	dir	=f.DirectoryName;

					//strip off the end to get the texture's base name
					fn	=StripCubeDesignator(fn);

					Debug.Assert(fn != "");

					if(fn == "")
					{
						continue;
					}

					if(baseNamesUsed.ContainsKey(fn))
					{
						baseNamesUsed[fn]++;
						continue;
					}

					baseNamesUsed.Add(fn, 1);
					dirs.Add(fn, dir);
				}

				Debug.Assert(dirs.Count == baseNamesUsed.Count);

				foreach(KeyValuePair<string, int> bases in baseNamesUsed)
				{
					Debug.Assert(bases.Value == 6);
					if(bases.Value != 6)
					{
						continue;
					}

					string	dir	=dirs[bases.Key];
					string	fn	=bases.Key;

					//strip back the content dir
					string	path	=dir.Substring(dir.LastIndexOf(mGameContent.RootDirectory)
						+ mGameContent.RootDirectory.Length + 1);

					//strip texturecubes
					int	texPos	=path.LastIndexOf("TextureCubes");
					if(texPos < 0 || path.Length <= 12)
					{
						LoadTextureCube(gd, mGameContent, path + "/" + fn, fn);
					}
					else
					{
						string	key	=path.Substring(texPos + 13,
							path.Length - 13);

						LoadTextureCube(gd, mGameContent, path + "/" + fn, key + "/" + fn);
					}
				}
			}
#endif
		}


		void LoadTextures()
		{
#if XBOX
			//this is a toolside method, stubbed out on xbox
			return;
#else
			if(Directory.Exists(mGameContent.RootDirectory + "/Textures"))
			{
				DirectoryInfo	di	=new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory
					+ mGameContent.RootDirectory + "/Textures/");

				//stupid getfiles won't take multiple wildcards
				FileInfo[]		fi	=di.GetFiles("*.xnb", SearchOption.AllDirectories);
				foreach(FileInfo f in fi)
				{
					string	fn	=UtilityLib.FileUtil.StripExtension(f.Name);

					string	dir	=f.DirectoryName;

					//strip back the content dir
					string	path	=dir.Substring(dir.LastIndexOf(mGameContent.RootDirectory)
						+ mGameContent.RootDirectory.Length + 1);

					//strip textures
					int	texPos	=path.LastIndexOf("Textures");
					if(texPos < 0 || path.Length <= 8)
					{
						//probably won't work, but try it
						LoadTexture(mGameContent, path + "/" + fn, fn);
					}
					else
					{
						string	key	=path.Substring(texPos + 9,
							path.Length - 9);

						LoadTexture(mGameContent, path + "/" + fn,
							UtilityLib.FileUtil.ConvertPathSlashes(key) + "/" + fn);
					}
				}
			}
#endif
		}
	}
}
