using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Character
{
	public class Material
	{
		string	mMap;			//name of the texmap
		string	mShaderName;	//name of the shader
		string	mName;			//name of the overall material


		public string Name
		{
			get { return mName; }
			set { mName = value; }
		}
		public string Map
		{
			get { return mMap; }
			set { mMap = value; }
		}
		public string ShaderName
		{
			get { return mShaderName; }
			set { mShaderName = value; }
		}
	}


	public class MaterialLib
	{
		Dictionary<string, Material>	mMats	=new Dictionary<string, Material>();
		Dictionary<string, Effect>		mFX		=new Dictionary<string, Effect>();
		Dictionary<string, Texture2D>	mMaps	=new Dictionary<string, Texture2D>();


		public MaterialLib(GraphicsDevice gd, ContentManager cm)
		{
			LoadShaders(cm);
			LoadTextures(gd);

			InitializeEffects();
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


		public void AddMaterial(Material mat)
		{
			mMats.Add(mat.Name, mat);
		}


		public Texture2D GetMaterialTexture(string name)
		{
			if(mMats.ContainsKey(name))
			{
				return	mMaps[mMats[name].Map];
			}
			return	null;
		}


		public Effect GetMaterialShader(string name)
		{
			if(mMats.ContainsKey(name))
			{
				return	mFX[mMats[name].ShaderName];
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


		private void InitializeEffects()
		{
			Vector4	[]lightColor	=new Vector4[3];
			lightColor[0]	=new Vector4(0.9f, 0.9f, 0.9f, 1.0f);
			lightColor[1]	=new Vector4(0.6f, 0.5f, 0.5f, 1.0f);
			lightColor[2]	=new Vector4(0.1f, 0.1f, 0.1f, 1.0f);

			Vector4	ambColor	=new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
			Vector3	lightDir	=new Vector3(1.0f, -1.0f, 0.1f);
			lightDir.Normalize();

			foreach(KeyValuePair<string, Effect> fx in mFX)
			{
				if(fx.Value.Parameters["mLightColor"] != null)
				{
					fx.Value.Parameters["mLightColor"].SetValue(lightColor);
				}
				if(fx.Value.Parameters["mLightDirection"] != null)
				{
					fx.Value.Parameters["mLightDirection"].SetValue(lightDir);
				}
				if(fx.Value.Parameters["mAmbientColor"] != null)
				{
					fx.Value.Parameters["mAmbientColor"].SetValue(ambColor);
				}
				if(fx.Value.Parameters["mLightColor"] != null)
				{
					fx.Value.Parameters["mLightColor"].SetValue(lightColor);
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
		private void LoadShaders(ContentManager cm)
		{
			DirectoryInfo	di	=new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "../../../Content/Shaders/");

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
				Effect	fx	=cm.Load<Effect>(path);

				mFX.Add(path, fx);
			}
		}


		private void LoadTextures(GraphicsDevice gd)
		{
			DirectoryInfo	di	=new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "../../../Content/Textures/");

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

				mMaps.Add(f.Name, tex);
			}
		}
	}
}
