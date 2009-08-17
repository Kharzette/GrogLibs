using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace ColladaConvert
{
	public class GameMaterial
	{
		public List<Texture2D>		mMaps	=new List<Texture2D>();
		public string				mShaderName;
	}


	public class MaterialLib
	{
		Dictionary<string, GameMaterial>	mMats	=new Dictionary<string, GameMaterial>();
		Dictionary<string, Effect>			mFX		=new Dictionary<string, Effect>();


		public MaterialLib(GraphicsDevice gd, ContentManager cm)
		{
			LoadShaders(cm);
			LoadTextures(gd);

			InitializeEffects();
		}


		public GameMaterial GetMaterial(string name)
		{
			if(mMats.ContainsKey(name))
			{
				return	mMats[name];
			}
			return	null;
		}


		public Effect GetShader(string name)
		{
			if(mFX.ContainsKey(name))
			{
				return	mFX[name];
			}
			return	null;
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

				GameMaterial	mat	=new GameMaterial();

				mat.mMaps.Add(tex);

				mMats.Add(f.Name, mat);

			}
		}
	}
}
