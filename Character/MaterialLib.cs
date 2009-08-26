using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Storage;

namespace Character
{
	public class ShaderParameters
	{
		string					mName;
		EffectParameterClass	mClass;
		EffectParameterType		mType;
		string					mValue;


		public ShaderParameters()
		{
			//init these strings to "" instead of null
			//so that blank values save and load properly
			mName	="";
			mValue	="";
		}

		public string Name
		{
			get { return mName; }
			set { mName = value; }
		}
		public EffectParameterClass Class
		{
			get { return mClass; }
			set { mClass = value; }
		}
		public EffectParameterType Type
		{
			get { return mType; }
			set { mType = value; }
		}
		public string Value
		{
			get { return mValue; }
			set { mValue = value; }
		}


		public void Write(BinaryWriter bw)
		{
			bw.Write(mName);
			bw.Write((UInt32)mClass);
			bw.Write((UInt32)mType);
			bw.Write(mValue);
		}


		public void Read(BinaryReader br)
		{
			mName	=br.ReadString();
			mClass	=(EffectParameterClass)br.ReadUInt32();
			mType	=(EffectParameterType)br.ReadUInt32();
			mValue	=br.ReadString();
		}
	}

	public class Material
	{
		string	mShaderName;	//name of the shader
		string	mName;			//name of the overall material
		string	mTechnique;		//technique to use with this material

		//parameters for the chosen shader
		List<ShaderParameters>	mParameters	=new List<ShaderParameters>();


		public string Name
		{
			get { return mName; }
			set { mName = value; }
		}
		public string ShaderName
		{
			get { return mShaderName; }
			set { mShaderName = value; }
		}
		public string Technique
		{
			get { return mTechnique; }
			set { mTechnique = value; }
		}
		public List<ShaderParameters> Parameters
		{
			get { return mParameters; }
			set { mParameters = value; }
		}


		public void Write(BinaryWriter bw)
		{
			bw.Write(mName);
			bw.Write(mShaderName);
			bw.Write(mTechnique);

			bw.Write(mParameters.Count);
			foreach(ShaderParameters sp in mParameters)
			{
				sp.Write(bw);
			}
		}


		public void Read(BinaryReader br)
		{
			mName		=br.ReadString();
			mShaderName	=br.ReadString();
			mTechnique	=br.ReadString();

			int	numParameters	=br.ReadInt32();
			for(int i=0;i < numParameters;i++)
			{
				ShaderParameters	sp	=new ShaderParameters();
				sp.Read(br);

				mParameters.Add(sp);
			}
		}


		public List<string>	GetReferencedTextures()
		{
			List<string>	ret	=new List<string>();

			foreach(ShaderParameters sp in mParameters)
			{
				if(sp.Type == EffectParameterType.Texture)
				{
					if(sp.Value != null && sp.Value != "")
					{
						ret.Add(sp.Value);
					}
				}
			}
			return	ret;
		}
	}


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


		//game side constructor, loads only what is needed
		public MaterialLib(GraphicsDevice gd, ContentManager cm, string fn)
		{
			mContent	=cm;

			ReadFromFile(fn);
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
			FileStream	file	=OpenTitleFile(fileName,
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
		}


		public bool ReadFromFile(string fileName)
		{
			FileStream	file	=OpenTitleFile(fileName,
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
				Effect	fx	=mContent.Load<Effect>(shd);

				mFX.Add(shd, fx);
			}

			//load textures
			foreach(string tex in texs)
			{
				if(tex == "")
				{
					continue;
				}
				string	texPath	="Textures/" + tex;
				Texture2D	t	=mContent.Load<Texture2D>(texPath);

				mMaps.Add(tex, t);
			}

			return	true;
		}


		public void AddMaterial(Material mat)
		{
			mMats.Add(mat.Name, mat);
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

			Material	mat	=mMats[matName];

			//set technique
			if(mat.Technique != "")
			{
				fx.CurrentTechnique	=fx.Techniques[mat.Technique];
			}

			foreach(ShaderParameters sp in mat.Parameters)
			{
				if(sp.Value == null || sp.Value == "")
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
						break;

					case EffectParameterClass.Scalar:
						if(sp.Type == EffectParameterType.Single)
						{
							fx.Parameters[sp.Name].SetValue(
								Convert.ToSingle(sp.Value));
						}
						break;

					case EffectParameterClass.Vector:
						//get the number of columns
						EffectParameter	ep	=fx.Parameters[sp.Name];

						if(ep.ColumnCount == 2)
						{
							Vector2	vec	=Vector2.Zero;
							string	[]tokens;
							tokens	=sp.Value.Split(' ');
							vec.X	=Convert.ToSingle(tokens[0]);
							vec.Y	=Convert.ToSingle(tokens[1]);
							ep.SetValue(vec);
						}
						else if(ep.ColumnCount == 3)
						{
							Vector3	vec	=Vector3.Zero;
							string	[]tokens;
							tokens	=sp.Value.Split(' ');
							vec.X	=Convert.ToSingle(tokens[0]);
							vec.Y	=Convert.ToSingle(tokens[1]);
							vec.Z	=Convert.ToSingle(tokens[2]);
							ep.SetValue(vec);
						}
						else if(ep.ColumnCount == 4)
						{
							Vector4	vec	=Vector4.Zero;
							string	[]tokens;
							tokens	=sp.Value.Split(' ');
							vec.X	=Convert.ToSingle(tokens[0]);
							vec.Y	=Convert.ToSingle(tokens[1]);
							vec.Z	=Convert.ToSingle(tokens[2]);
							vec.W	=Convert.ToSingle(tokens[3]);
							ep.SetValue(vec);
						}
						else
						{
							Debug.Assert(false);
						}
						break;
				}
			}
		}


		public void UpdateEffects(Matrix world, Matrix view, Matrix proj)
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

				string	path	=f.DirectoryName;
				path	+="\\" + f.Name;

				//create an element
				Texture2D	tex	=Texture2D.FromFile(gd, path);

				//strip extension
				path	=f.Name.Substring(0, f.Name.LastIndexOf('.'));

				mMaps.Add(path, tex);
			}
#endif
		}


		public static FileStream OpenTitleFile(string fileName,
			FileMode mode, FileAccess access)
		{
			string	fullPath	=Path.Combine(
									StorageContainer.TitleLocation,
									fileName);

			if(!File.Exists(fullPath) &&
				(access == FileAccess.Write ||
				access == FileAccess.ReadWrite))
			{
				return	File.Create(fullPath);
			}
			else
			{
				return	File.Open(fullPath, mode, access);
			}
		}
	}
}
