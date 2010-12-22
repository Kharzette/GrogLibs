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

		//reference to the content manager
		ContentManager	mContent;


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


		//this merges in all the content directory
		//textures for the tool chain after loading
		//a material lib from a file
		public void LoadToolTextures(GraphicsDevice gd)
		{
			LoadTextures(gd);
			LoadShaders();
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
			FileStream	file	=UtilityLib.FileUtil.OpenTitleFile(fileName,
									FileMode.Open, FileAccess.Write);

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
			FileStream	file	=UtilityLib.FileUtil.OpenTitleFile(fileName,
									FileMode.Open, FileAccess.Read);

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
				Material	m	=new Material();

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
					string	texPath	=tex.Substring(0, tex.LastIndexOf('.'));
					Texture2D	t	=mContent.Load<Texture2D>(texPath);

					mMaps.Add(tex, t);
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

			foreach(ShaderParameters sp in mat.Parameters)
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
								ep.SetValue(Convert.ToSingle(sp.Value));
							}
						}
						else if(sp.Type == EffectParameterType.Bool)
						{
							fx.Parameters[sp.Name].SetValue(
								Convert.ToBoolean(sp.Value));
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
							vec.X	=Convert.ToSingle(tokens[0]);
							vec.Y	=Convert.ToSingle(tokens[1]);
							ep2.SetValue(vec);
						}
						else if(ep2.ColumnCount == 3)
						{
							Vector3	vec	=Vector3.Zero;
							string	[]tokens;
							tokens	=sp.Value.Split(' ');
							vec.X	=Convert.ToSingle(tokens[0]);
							vec.Y	=Convert.ToSingle(tokens[1]);
							vec.Z	=Convert.ToSingle(tokens[2]);
							ep2.SetValue(vec);
						}
						else if(ep2.ColumnCount == 4)
						{
							Vector4	vec	=Vector4.Zero;
							string	[]tokens;
							tokens	=sp.Value.Split(' ');
							vec.X	=Convert.ToSingle(tokens[0]);
							vec.Y	=Convert.ToSingle(tokens[1]);
							vec.Z	=Convert.ToSingle(tokens[2]);
							vec.W	=Convert.ToSingle(tokens[3]);
							ep2.SetValue(vec);
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

				f	=Convert.ToSingle(tok);
				ret.Add(f);
			}
			return	ret.ToArray();
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


		public void UpdateWVP(Matrix world, Matrix view, Matrix proj)
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
		private void LoadShaders()
		{
#if XBOX
			//this is a toolside method, stubbed out on xbox
			return;
#else
			DirectoryInfo	di	=new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory
				+ "../../../Content/Shaders/");

			//stupid getfiles won't take multiple wildcards
			FileInfo[]		fi	=di.GetFiles("*.fx", SearchOption.AllDirectories);

			foreach(FileInfo f in fi)
			{
				Console.WriteLine("{0,-25} {1,25}", f.Name, f.LastWriteTime);

				string	path	=f.DirectoryName.Substring(f.DirectoryName.LastIndexOf("Content") + 8);
				path	+="\\" + f.Name;

				//strip extension
				path	=path.Substring(0, path.LastIndexOf('.'));

				//load shader
				Effect	fx	=mContent.Load<Effect>(path);

				mFX.Add(path, fx);
			}
#endif
		}


		private void LoadTextures(GraphicsDevice gd)
		{
#if XBOX
			//this is a toolside method, stubbed out on xbox
			return;
#else
			DirectoryInfo	di	=new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory
				+ "../../../Content/Textures/");

			//stupid getfiles won't take multiple wildcards
			FileInfo[]		fi0	=di.GetFiles("*.bmp", SearchOption.AllDirectories);
			FileInfo[]		fi1	=di.GetFiles("*.dds", SearchOption.AllDirectories);
			FileInfo[]		fi2	=di.GetFiles("*.dib", SearchOption.AllDirectories);
			FileInfo[]		fi3	=di.GetFiles("*.hdr", SearchOption.AllDirectories);
			FileInfo[]		fi4	=di.GetFiles("*.jpg", SearchOption.AllDirectories);
			FileInfo[]		fi5	=di.GetFiles("*.pfm", SearchOption.AllDirectories);
			FileInfo[]		fi6	=di.GetFiles("*.png", SearchOption.AllDirectories);
			FileInfo[]		fi7	=di.GetFiles("*.ppm", SearchOption.AllDirectories);
			FileInfo[]		fi8	=di.GetFiles("*.tga", SearchOption.AllDirectories);

			//merge these
			List<FileInfo>	fi	=new List<FileInfo>();
			foreach(FileInfo f in fi0)
			{
				fi.Add(f);
			}
			foreach(FileInfo f in fi1)
			{
				fi.Add(f);
			}
			foreach(FileInfo f in fi2)
			{
				fi.Add(f);
			}
			foreach(FileInfo f in fi3)
			{
				fi.Add(f);
			}
			foreach(FileInfo f in fi4)
			{
				fi.Add(f);
			}
			foreach(FileInfo f in fi5)
			{
				fi.Add(f);
			}
			foreach(FileInfo f in fi6)
			{
				fi.Add(f);
			}
			foreach(FileInfo f in fi7)
			{
				fi.Add(f);
			}
			foreach(FileInfo f in fi8)
			{
				fi.Add(f);
			}

			foreach(FileInfo f in fi)
			{
				Console.WriteLine("{0,-25} {1,25}", f.Name, f.LastWriteTime);

				string	relPath	=f.DirectoryName.Substring(f.DirectoryName.LastIndexOf("Content") + 8);
				relPath	+="\\" + f.Name;

				string	fullPath	=f.DirectoryName + "\\" + f.Name;

				if(!mMaps.ContainsKey(relPath))
				{
					//create an element
					Texture2D	tex	=Texture2D.FromFile(gd, fullPath);

					mMaps.Add(relPath, tex);
				}
			}
#endif
		}
	}
}
