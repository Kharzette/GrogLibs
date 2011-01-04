using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Storage;


namespace MaterialLib
{
	public class MaterialLib
	{
		Dictionary<string, Material>	mMats	=new Dictionary<string, Material>();
		Dictionary<string, Effect>		mFX		=new Dictionary<string, Effect>();
		Dictionary<string, Texture2D>	mMaps	=new Dictionary<string, Texture2D>();

		//state block pool
		StateBlockPool	mStateBlockPool	=new StateBlockPool();

		//references to the content managers
		ContentManager	mContent, mSharedContent;


		//tool side constructor, loads up everything
		public MaterialLib(GraphicsDevice gd, ContentManager cm)
		{
			mContent	=cm;

			LoadShaders();
			LoadTextures(gd);
		}


		//file loader, game or tool
		public MaterialLib(GraphicsDevice gd, ContentManager cm, string fn, bool bTool)
		{
			mContent	=cm;

			ReadFromFile(fn, bTool);
			if(bTool)
			{
				LoadToolTextures(gd);
			}
		}


		//two content managers
		public MaterialLib(GraphicsDevice gd, ContentManager cm, ContentManager scm, bool bTool)
		{
			mContent		=cm;
			mSharedContent	=scm;

			if(bTool)
			{
				LoadShaders();
				LoadTextures(gd);
			}
		}


		//for just material creation, nothing else will work
		public MaterialLib()
		{
		}


		//this merges in all the content directory
		//textures for the tool chain after loading
		//a material lib from a file
		public void LoadToolTextures(GraphicsDevice gd)
		{
			LoadTextures(gd);
			LoadShaders();
		}


		public Material CreateMaterial()
		{
			Material	mat	=new Material(mStateBlockPool);

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


		//this only saves referenced stuff
		//the tool side will still need to enumerate
		//all the textures / shaders
		public void SaveToFile(string fileName)
		{
			Stream	file	=UtilityLib.FileUtil.OpenTitleFile(fileName);

			BinaryWriter	bw	=new BinaryWriter(file);

			//write a magic number identifying matlibs
			UInt32	magic	=0xFA77DA77;

			bw.Write(magic);

			//write number of materials
			bw.Write(mMats.Count);

			foreach(KeyValuePair<string, Material> mat in mMats)
			{
				mat.Value.Write(bw);
			}

			bw.Close();
			file.Close();
		}


		public bool ReadFromFile(string fileName, bool bTool)
		{
			Stream	file	=UtilityLib.FileUtil.OpenTitleFile(fileName);

			BinaryReader	br	=new BinaryReader(file);

			//clear existing data
			mMaps.Clear();
			mMats.Clear();
			mFX.Clear();

			//read magic number
			UInt32	magic	=br.ReadUInt32();

			if(magic != 0xFA77DA77)
			{
				br.Close();
				file.Close();
				return	false;
			}

			int	numMaterials	=br.ReadInt32();

			//list the referenced textures and shaders
			List<string>	texNeeded	=new List<string>();
			List<string>	shdNeeded	=new List<string>();

			for(int i=0;i < numMaterials;i++)
			{
				Material	m	=new Material(mStateBlockPool);

				m.Read(br);

				mMats.Add(m.Name, m);

				texNeeded.AddRange(m.GetReferencedTextures());

				if(!shdNeeded.Contains(m.ShaderName))
				{
					shdNeeded.Add(m.ShaderName);
				}
			}

			if(!bTool)
			{
				//eliminate duplicates
				List<string>	texs	=new List<string>();
				foreach(string tex in texNeeded)
				{
					if(!texs.Contains(tex))
					{
						texs.Add(tex);
					}
				}

				//load shaders
				foreach(string shd in shdNeeded)
				{
					if(shd != null && shd != "")
					{
						Effect	fx	=mContent.Load<Effect>(shd);

						mFX.Add(shd, fx);
					}
				}

				//load textures
				foreach(string tex in texs)
				{
					if(tex == "")
					{
						continue;
					}

					//strip extension
					//if no extension, it is likely
					//not loaded from content
					int	dotPos	=tex.LastIndexOf('.');
					if(dotPos != -1)
					{
						string	texPath	=tex.Substring(0, dotPos);
						Texture2D	t	=mContent.Load<Texture2D>(texPath);

						mMaps.Add(tex, t);
					}
				}
			}

			br.Close();
			file.Close();

			return	true;
		}


		public void AddMaterial(Material mat)
		{
			mMats.Add(mat.Name, mat);
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


		void AssignEmissive(string matName)
		{
			if(!mMats.ContainsKey(matName))
			{
				return;
			}

			Material	mat	=mMats[matName];
			string		val	=mat.GetParameterValue("mTexture");
			if(val == "")
			{
				return;
			}

			if(!mMaps.ContainsKey(val))
			{
				return;
			}

			Texture2D	map		=mMaps[val];
			int			size	=map.Width * map.Height;

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
				string	val	=mMats[matName].GetParameterValue("mTexSize");
				if(val == "")
				{
					return;
				}

				Vector2	size	=Vector2.Zero;

				string	[]toks	=val.Split(' ');
				UtilityLib.Mathery.TryParse(toks[0], out size.X);
				UtilityLib.Mathery.TryParse(toks[1], out size.Y);

				if(bUp)
				{
					size	*=2;
				}
				else
				{
					size	*=0.5f;
				}
				mMats[matName].SetParameter("mTexSize", "" + size.X + " " + size.Y);
			}
		}


		public void GuessTextures()
		{
			foreach(KeyValuePair<string, Material> mat in mMats)
			{
				if(mat.Value.GetParameterValue("mTexture") != "")
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
					if(tex.Key.Contains(rawMatName))
					{
						mat.Value.SetParameter("mTexture", tex.Key);
						mat.Value.SetParameter("mbTextureEnabled", "true");
						mat.Value.SetParameter("mTexSize", "" + tex.Value.Width + " " + tex.Value.Height);
						break;
					}
				}
			}
		}


		public void NukeAllMaterials()
		{
			mMats.Clear();
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
			Effect	fx	=GetMaterialShader(matName);
			if(fx == null)
			{
				return;
			}

			Material	mat	=mMats[matName];

			//set technique
			if(mat.Technique != "")
			{
				if(fx.Techniques[mat.Technique] == null)
				{
					return;
				}
				fx.CurrentTechnique	=fx.Techniques[mat.Technique];
			}

			List<ShaderParameters>	matParms	=mat.GetRealShaderParameters();

			foreach(ShaderParameters sp in matParms)
			{
				if(sp.Value == null || sp.Value == "" || fx.Parameters[sp.Name] == null)
				{
					continue;	//skip anything blank
				}
				switch(sp.Class)
				{
					case EffectParameterClass.Object:
						if(sp.Type == EffectParameterType.Texture)
						{
							fx.Parameters[sp.Name].SetValue(mMaps[sp.Value]);
						}
						else
						{
							fx.Parameters[sp.Name].SetValue(sp.Value);
						}
						break;

					case EffectParameterClass.Scalar:
						if(sp.Type == EffectParameterType.Single)
						{
							EffectParameter	ep	=fx.Parameters[sp.Name];
							if(ep.Elements.Count > 1)
							{
								float	[]vals	=ParseFloatArray(sp.Value);
								ep.SetValue(vals);
							}
							else
							{
								float	val;
								if(UtilityLib.Mathery.TryParse(sp.Value, out val))
								{
									ep.SetValue(val);
								}
							}
						}
						else if(sp.Type == EffectParameterType.Bool)
						{
							bool	val;
							if(UtilityLib.Mathery.TryParse(sp.Value, out val))
							{
								fx.Parameters[sp.Name].SetValue(val);
							}
						}
						break;

					case EffectParameterClass.Vector:
						//get the number of columns
						EffectParameter	ep2	=fx.Parameters[sp.Name];

						if(ep2.ColumnCount == 2)
						{
							Vector2	vec	=Vector2.Zero;
							string	[]tokens;
							tokens	=sp.Value.Split(' ');
							if(UtilityLib.Mathery.TryParse(tokens[0], out vec.X))
							{
								if(UtilityLib.Mathery.TryParse(tokens[1], out vec.Y))
								{
									ep2.SetValue(vec);
								}
							}
						}
						else if(ep2.ColumnCount == 3)
						{
							Vector3	vec	=Vector3.Zero;
							string	[]tokens;
							tokens	=sp.Value.Split(' ');
							if(UtilityLib.Mathery.TryParse(tokens[0], out vec.X))
							{
								if(UtilityLib.Mathery.TryParse(tokens[1], out vec.Y))
								{
									if(UtilityLib.Mathery.TryParse(tokens[2], out vec.Z))
									{
										ep2.SetValue(vec);
									}
								}
							}
						}
						else if(ep2.ColumnCount == 4)
						{
							Vector4	vec	=Vector4.Zero;
							string	[]tokens;
							tokens	=sp.Value.Split(' ');
							if(UtilityLib.Mathery.TryParse(tokens[0], out vec.X))
							{
								if(UtilityLib.Mathery.TryParse(tokens[1], out vec.Y))
								{
									if(UtilityLib.Mathery.TryParse(tokens[2], out vec.Z))
									{
										if(UtilityLib.Mathery.TryParse(tokens[3], out vec.W))
										{
											ep2.SetValue(vec);
										}
									}
								}
							}
						}
						else
						{
							Debug.Assert(false);
						}
						break;
				}
			}
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


		public void SetParameterOnAll(string paramName, Vector3 vec)
		{
			foreach(KeyValuePair<string, Material> mat in mMats)
			{
				string	val	="" + vec.X + " " + vec.Y + " " + vec.Z;
				mat.Value.SetParameter(paramName, val);
			}
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
			foreach(KeyValuePair<string, Effect> fx in mFX)
			{
				if(fx.Value.Parameters["mWorld"] != null)
				{
					fx.Value.Parameters["mWorld"].SetValue(world);
				}
				if(fx.Value.Parameters["mView"] != null)
				{
					fx.Value.Parameters["mView"].SetValue(view);
				}
				if(fx.Value.Parameters["mProjection"] != null)
				{
					fx.Value.Parameters["mProjection"].SetValue(proj);
				}
			}

			//update eyepos in material parameters
			SetParameterOnAll("mEyePos", eyePos);
		}


		public void DrawMap(string map, SpriteBatch sb)
		{
			if(!mMaps.ContainsKey(map))
			{
				return;
			}

			sb.Begin();
			sb.Draw(mMaps[map], Vector2.One * 130.0f, Color.White);
			sb.End();
		}


		//load shaders in the content/shaders folder
		void LoadShaders()
		{
#if XBOX
			//this is a toolside method, stubbed out on xbox
			return;
#else
			//see if Shader folder exists in Content
			if(Directory.Exists("Content/Shaders"))
			{
				DirectoryInfo	di	=new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory
					+ "Content/Shaders/");

				FileInfo[]		fi	=di.GetFiles("*.xnb", SearchOption.AllDirectories);
				foreach(FileInfo f in fi)
				{
					LoadShader(f.DirectoryName, f.Name);
				}
			}

			//try shared as well
			if(Directory.Exists("SharedContent/Shaders"))
			{
				DirectoryInfo	di	=new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory
					+ "SharedContent/Shaders/");

				FileInfo[]		fi	=di.GetFiles("*.xnb", SearchOption.AllDirectories);
				foreach(FileInfo f in fi)
				{
					LoadSharedShader(f.DirectoryName, f.Name);
				}
			}
#endif
		}


		void LoadTexture(GraphicsDevice gd, string dirName, string fileName)
		{
			string	path	=dirName + "\\" + fileName;

			FileStream	fs	=new FileStream(path, FileMode.Open, FileAccess.Read);

			Texture2D	tex	=Texture2D.FromStream(gd, fs);

			path	=UtilityLib.FileUtil.StripExtension(path);
			path	=path.Substring(path.LastIndexOf("Content") + 8);
			mMaps.Add(path, tex);
		}


		void LoadSharedTexture(GraphicsDevice gd, string dirName, string fileName)
		{
			string	path	=dirName + "\\" + fileName;

			FileStream	fs	=new FileStream(path, FileMode.Open, FileAccess.Read);

			Texture2D	tex	=Texture2D.FromStream(gd, fs);

			path	=UtilityLib.FileUtil.StripExtension(path);
			path	=path.Substring(path.LastIndexOf("SharedContent") + 14);
			mMaps.Add(path, tex);
		}


		void LoadShader(string dirName, string fileName)
		{
			string	path	=dirName + "\\" + fileName;

			path	=UtilityLib.FileUtil.StripExtension(path);
			path	=path.Substring(path.LastIndexOf("Content") + 8);

			//load shader
			Effect	fx	=mContent.Load<Effect>(path);

			mFX.Add(path, fx);
		}


		void LoadSharedShader(string dirName, string fileName)
		{
			string	path	=dirName + "\\" + fileName;

			path	=UtilityLib.FileUtil.StripExtension(path);
			path	=path.Substring(path.LastIndexOf("SharedContent") + 14);

			//load shader
			Effect	fx	=mSharedContent.Load<Effect>(path);

			mFX.Add(path, fx);
		}


		void LoadTextures(GraphicsDevice gd)
		{
#if XBOX
			//this is a toolside method, stubbed out on xbox
			return;
#else
			if(Directory.Exists("../../../Content/Textures"))
			{
				DirectoryInfo	di	=new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory
					+ "../../../Content/Textures/");

				//stupid getfiles won't take multiple wildcards
				FileInfo[]		fi	=di.GetFiles("*.png", SearchOption.AllDirectories);
				foreach(FileInfo f in fi)
				{
					LoadTexture(gd, f.DirectoryName, f.Name);
				}
			}
			if(Directory.Exists("SharedContent/Textures"))
			{
				DirectoryInfo	di	=new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory
					+ "SharedContent/Textures/");

				//stupid getfiles won't take multiple wildcards
				FileInfo[]		fi	=di.GetFiles("*.xnb", SearchOption.AllDirectories);
				foreach(FileInfo f in fi)
				{
					LoadSharedTexture(gd, f.DirectoryName, f.Name);
				}
			}
#endif
		}
	}
}
