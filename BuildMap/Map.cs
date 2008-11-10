using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.Diagnostics;
using Microsoft.Xna.Framework.Storage;


namespace BuildMap
{
	public class Entity
	{
		public	List<String>	mKey;
		public	List<String>	mValue;
		public	List<Brush>		mBrushes;


		public Entity()
		{
			mKey	=new List<string>();
			mValue	=new List<string>();
			mBrushes=new List<Brush>();
		}


		public bool GetOrigin(out Vector3 org)
		{
			org	=Vector3.Zero;
			foreach(string s in mKey)
			{
				if(s == "origin")
				{
					string	[]szVec	=mValue[mKey.IndexOf(s)].Split(' ');

					if(szVec.Length != 3)
					{
						return	false;
					}

					if(!Single.TryParse(szVec[0], out org.X))
					{
						return	false;
					}
					if(!Single.TryParse(szVec[1], out org.Y))
					{
						return	false;
					}
					if(!Single.TryParse(szVec[2], out org.Z))
					{
						return	false;
					}
					//flip x
					org.X	=-org.X;

					//swap y and z
					float	zTemp	=org.Z;
					org.Z	=org.Y;
					org.Y	=zTemp;
					return	true;
				}
			}
			return	false;
		}


		public bool GetLightValue(out float dist)
		{
			dist	=250;
			foreach(string s in mKey)
			{
				if(s == "light")
				{
					if(!Single.TryParse(mValue[mKey.IndexOf(s)], out dist))
					{
						return	false;
					}
					return	true;
				}
			}
			return	false;
		}


		public bool GetColor(out Vector3 color)
		{
			color	=Vector3.One;
			foreach(string s in mKey)
			{
				if(s == "color" || s == "_color")
				{
					string	[]szVec	=mValue[mKey.IndexOf(s)].Split(' ');

					if(szVec.Length != 3)
					{
						return	false;
					}

					if(!Single.TryParse(szVec[0], out color.X))
					{
						return	false;
					}
					if(!Single.TryParse(szVec[1], out color.Y))
					{
						return	false;
					}
					if(!Single.TryParse(szVec[2], out color.Z))
					{
						return	false;
					}
					return	true;
				}
			}
			return	false;
		}


		public void ReadFromMap(StreamReader sr)
		{
            string	s			="";
			Brush	b			=null;
			bool	brushComing	=false;
			bool	patchComing	=false;
			bool	patchBrush	=false;

            while((s = sr.ReadLine()) != null)
            {
				s	=s.Trim();
				if(s.StartsWith("\""))
				{
					string[]	tokens;
					tokens	=s.Split('\"');

					mKey.Add(tokens[1]);
					mValue.Add(tokens[3]);
				}
				else if(s.StartsWith("{"))
				{
					if(!patchComing)
					{
						//brush coming I think
						b			=new Brush();
						brushComing	=true;
					}
				}
				else if(s.StartsWith("}"))
				{
					if(brushComing)
					{
						brushComing	=false;
						
						//seal the brush
						b.SealFaces();
						
						if(b.IsValid())
						{
							mBrushes.Add(b);
						}
					}
					else if(patchComing)
					{
						patchComing	=false;
					}
					else if(patchBrush)
					{
						patchBrush	=false;	//I'll support these someday maybe
					}
					else
					{
						return;	//entity done
					}
				}
				else if(s.StartsWith("("))
				{
					if(brushComing)
					{
						b.MakeFaceFromMapLine(s);
					}
				}
				else if(s.StartsWith("patchDef2"))
				{
					brushComing	=false;
					patchComing	=true;
					patchBrush	=true;
					b			=null;
				}
			}
		}


		public void WriteToFile(BinaryWriter bw)
		{
			int	i;

			Debug.Assert(mKey.Count == mValue.Count);

			//write out # of key value pairs
			bw.Write(mKey.Count);

			for(i=0;i < mKey.Count;i++)
			{
				bw.Write(mKey[i]);
				bw.Write(mValue[i]);
			}

			//brushes?
			bw.Write(mBrushes != null);

			//any brushes to write?
			if(mBrushes != null)
			{
				//write number of brushes as uint32
				bw.Write(mBrushes.Count);

				foreach(Brush b in mBrushes)
				{
					b.WriteToFile(bw);
				}
			}			
		}
	};

    class Map
    {
		List<Entity>	mEntities;
		BspTree			mTree;
		TexAtlas		mAtlas;

		List<KeyValuePair<string, Texture2D>>	mTextures;

        //reads a .map file
        public Map(string mapFileName)
        {
			mEntities	=new List<Entity>();


            if(File.Exists(mapFileName))
            {
                using(StreamReader sr = File.OpenText(mapFileName))
                {
                    string s = "";

                    while((s = sr.ReadLine()) != null)
                    {
						s	=s.Trim();
						if(s.StartsWith("{"))
						{
							Entity	e	=new Entity();

							e.ReadFromMap(sr);

							mEntities.Add(e);
						}
					}
				}
			}
        }


		public void Draw(GraphicsDevice g, Effect fx, Vector3 camPos)
		{
			mTree.Draw(g, fx, camPos);
		}


        public void Draw(GraphicsDevice g, Effect fx)
        {
			fx.Parameters["LightMap"].SetValue(mAtlas.GetAtlasTexture());
			fx.Parameters["LightMapEnabled"].SetValue(true);
			fx.Parameters["FullBright"].SetValue(false);

			foreach(Entity e in mEntities)
			{
				foreach(Brush b in e.mBrushes)
				{
					b.Draw(g, fx);
				}
			}
        }


		public void BuildVertexInfo()
		{
			foreach(Entity e in mEntities)
			{
				foreach(Brush b in e.mBrushes)
				{
					b.BuildVertexInfo();
				}
			}
		}


		public void BuildVertexBuffers(GraphicsDevice g)
		{
			foreach(Entity e in mEntities)
			{
				foreach(Brush b in e.mBrushes)
				{
					b.BuildVertexBuffers(g);
				}
			}
		}


		public bool ClassifyPoint(Vector3 pnt)
		{
			return	mTree.ClassifyPoint(pnt);
		}


		public void	BuildTree(bool bLightTree)
		{
			//look for the worldspawn
			Entity e	=GetWorldSpawnEntity();
			List<Brush>	copy;

			if(bLightTree)
			{
				copy	=new List<Brush>();
				//take out clip brushes
				//we don't want them blocking
				//light raycasts
				foreach(Brush b in e.mBrushes)
				{
					if(b.IsClip())
					{
						continue;
					}
					copy.Add(b);
				}
			}
			else
			{
				copy	=new List<Brush>(e.mBrushes);
			}

			//use the copy so we have the old ones around to draw
			mTree	=new BspTree(copy);
		}

		public struct LightParameters
		{
			public	GraphicsDevice		g;
			public	BspNode				root;
			public	List<Brush>			brushList;
			public	ManualResetEvent	doneEvent;
			public	int					core, cores;
		}


		private	void LightBrushesThreadCB(Object threadContext)
		{
			LightParameters	p	=(LightParameters)threadContext;

			Console.WriteLine("Thread doing light brushes half to end\n");

			for(int i=0;i < mEntities.Count;i++)
			{
				Vector3	lightPos, clr;
				float	lightVal;
				if(mEntities[i] == GetWorldSpawnEntity())
				{
					continue;
				}
				if(!mEntities[i].GetLightValue(out lightVal))
				{
					continue;
				}
				if(!mEntities[i].GetOrigin(out lightPos))
				{
					continue;
				}
				mEntities[i].GetColor(out clr);

				//-1 for zero based index, -1 for main thread
				if(p.core == p.cores - 2)
				{
					//go to the end, sometimes there's a remainder
					LightBrushes(p.g, p.brushList, lightPos, lightVal, clr,
						(p.brushList.Count / p.cores) * (p.core + 1),
						p.brushList.Count);
				}
				else
				{
					LightBrushes(p.g, p.brushList, lightPos, lightVal, clr,
						(p.brushList.Count / p.cores) * (p.core + 1),
						(p.brushList.Count / p.cores) * (p.core + 2));
				}
			}

			Console.WriteLine("Thread done lighting\n");
			p.doneEvent.Set();
		}


		private	void LightBrushes(GraphicsDevice g, List<Brush> bl,
			Vector3 lightPos, float lightVal, Vector3 color,
			int	startIndex, int endIndex)
		{
			Debug.Assert(endIndex <= bl.Count);
			for(int i=startIndex;i < endIndex;i++)
			{
				bl[i].LightBrush(g, mTree.GetRoot(), lightPos, lightVal, color);
			}
		}


		public void LightAllBrushes(GraphicsDevice g)
		{
			//find worldspawn brush list
			Entity	wse	=GetWorldSpawnEntity();

			int	cores	=System.Environment.ProcessorCount;

			if(cores < 2)
			{
				cores	=2;	//lazy
			}

			//spin off extra threads to process chunks of the map
			ManualResetEvent[]	res	=new ManualResetEvent[cores - 1];

			for(int i=0;i < (cores - 1);i++)
			{
				res[i]	=new ManualResetEvent(false);
			}

			LightParameters	p	=new LightParameters();

			Console.WriteLine("Main thread doing front part of light brushes\n");
	
			if(mEntities.Count < 2)
			{
				Console.WriteLine("Need at least 2 entities for threading to work\n");
			}

			p.g			=g;
			p.root		=mTree.GetRoot();
			p.brushList	=wse.mBrushes;
			p.cores		=cores;

			for(int i=0;i < (cores - 1);i++)
			{
				p.doneEvent	=res[i];
				p.core		=i;
				ThreadPool.QueueUserWorkItem(LightBrushesThreadCB, p);
			}

			for(int i=0;i < mEntities.Count;i++)
			{
				Vector3	lightPos, clr;
				float	lightVal;
				if(mEntities[i] == GetWorldSpawnEntity())
				{
					continue;
				}
				if(!mEntities[i].GetLightValue(out lightVal))
				{
					continue;
				}
				if(!mEntities[i].GetOrigin(out lightPos))
				{
					continue;
				}
				mEntities[i].GetColor(out clr);

				LightBrushes(g, wse.mBrushes, lightPos, lightVal, clr, 0, (wse.mBrushes.Count / cores));
			}
			Console.WriteLine("Main thread done, waiting on second thread\n");
			WaitHandle.WaitAll(res);
		}


		public Vector3 GetFirstLightPos()
		{
			foreach(Entity e in mEntities)
			{
				if(e == GetWorldSpawnEntity())
				{
					continue;
				}
				float	dist;
				if(e.GetLightValue(out dist))
				{
					Vector3	ret;
					e.GetOrigin(out ret);
					return	ret;
				}
			}
			return	Vector3.Zero;
		}


		public void LoadTextures(ContentManager cm)
		{
			mTextures	=new List<KeyValuePair<string, Texture2D>>();

			List<string>	texFiles	=new List<string>();

			Entity	e	=GetWorldSpawnEntity();

			foreach(Brush b in e.mBrushes)
			{
				b.GetTexFileNames(ref texFiles);
			}

			foreach(string fn in texFiles)
			{
				Texture2D	tex	=cm.Load<Texture2D>("textures\\" + fn);
				KeyValuePair<string, Texture2D>	kv;

				kv	=new KeyValuePair<string,Texture2D>(fn, tex);
				mTextures.Add(kv);
			}
		}


		public void Save(string fileName)
		{
			FileStream	file	=OpenTitleFile(fileName,
				FileMode.Open, FileAccess.Write);

			BinaryWriter	bw	=new BinaryWriter(file);

			bw.Write(mEntities.Count);

			//write all entities
			foreach(Entity e in mEntities)
			{
				e.WriteToFile(bw);
			}

			//write bsp
			mTree.WriteToFile(bw);

			//write lightmap atlas
			Texture2D	tex	=mAtlas.GetAtlasTexture();

			Color[]	col	=new Color[(tex.Width * tex.Height)];

			tex.GetData<Color>(col);

			//write width and height
			bw.Write(tex.Width);
			bw.Write(tex.Height);

			for(int i=0;i < (tex.Width * tex.Height);i++)
			{
				bw.Write(col[i].PackedValue);
			}
		}


		public static FileStream OpenTitleFile(string fileName,
			FileMode mode, FileAccess access)
		{
			string fullPath	=Path.Combine(
				StorageContainer.TitleLocation, fileName);

			if(!File.Exists(fullPath) &&
				(access == FileAccess.Write || access == FileAccess.ReadWrite))
			{
				return	File.Create(fullPath);
			}
			else
			{
				return File.Open(fullPath, mode, access);
			}
		}


		public void SetTexturePointers()
		{
			Entity	e	=GetWorldSpawnEntity();

			foreach(Brush b in e.mBrushes)
			{
				b.SetTexturePointers(mTextures);
			}
		}


		public int GetFirstSurface(out Vector3[] surfPoints)
		{
			Entity	e	=GetWorldSpawnEntity();

			foreach(Brush b in e.mBrushes)
			{
				if(e.mBrushes.IndexOf(b) < 0)
				{
					continue;
				}
				int	ret	=b.GetFirstSurface(out surfPoints);
				if(ret > 0)
				{
					return	ret;
				}
			}
			surfPoints	=null;
			return	0;
		}


		public Entity GetWorldSpawnEntity()
		{
			foreach(Entity e in mEntities)
			{
				foreach(string s in e.mKey)
				{
					if(s == "classname")
					{
						if(e.mValue[e.mKey.IndexOf(s)] == "worldspawn")
						{
							return	e;
						}
					}
				}
			}
			return	null;
		}


		public void AtlasLightMaps(GraphicsDevice g)
		{
			Entity	wse	=GetWorldSpawnEntity();

			mAtlas	=new TexAtlas(g);

			foreach(Brush b in wse.mBrushes)
			{
				b.AtlasLightMaps(g, mAtlas);
			}
		}


        public void RemoveOverlap()
        {
            int			i, j;
			List<Brush>	brushes	=null;

			Entity	wse	=GetWorldSpawnEntity();

			brushes	=wse.mBrushes;

            i = 1;
        startoveragain:
            if (i > 0)
            {
                i--;
            }

            for (; i < brushes.Count; i++)
            {
                for(j = 0;j < brushes.Count;j++)
                {
                    if(i == j)
                    {
                        continue;
                    }

                    if(!brushes[i].Intersects(brushes[j]))
                    {
                        continue;
                    }

                    List<Brush> cutup = new List<Brush>();
                    List<Brush> cutup2 = new List<Brush>();

                    if(brushes[i].SubtractBrush(brushes[j], out cutup))
                    {
                        //make sure the brush returned is
                        //not the one passed in
                        if(cutup.Count == 1)
                        {
                            if(brushes[i].Equals(cutup[0]))
                            {
                                continue;
                            }
                        }

                        if(cutup.Count == 0)
                        {
                            //Debug.WriteLine("Subtract returned true, but an empty list");
                        }
                    }
                    else
                    {
                        cutup.Clear();
                    }

                    if(brushes[j].SubtractBrush(brushes[i], out cutup2))
                    {
                        //make sure the brush returned is
                        //not the one passed in
                        if(cutup2.Count == 1)
                        {
                            if(brushes[j].Equals(cutup2[0]))
                            {
                                continue;
                            }
                        }

                        if(cutup2.Count == 0)
                        {
                            //Debug.WriteLine("Subtract returned true, but an empty list");
                        }
                    }
                    else
                    {
                        cutup2.Clear();
                    }

                    if(cutup.Count==0 && cutup2.Count==0)
                    {
                        continue;
                    }

                    if(cutup.Count > 4 && cutup2.Count > 4)
                    {
                        continue;
                    }

                    if(cutup.Count < cutup2.Count)
                    {
                        cutup2.Clear();

                        foreach(Brush b in cutup)
                        {
                            brushes.Add(b);
                        }
                        cutup.Clear();
                        brushes.RemoveAt(i);
                        goto startoveragain;
                    }
                    else
                    {
                        cutup.Clear();

                        foreach(Brush b in cutup2)
                        {
                            brushes.Add(b);
                        }
                        cutup2.Clear();
                        brushes.RemoveAt(j);
                        goto startoveragain;
                    }
                }
            }

            bool    bDone   =false;

            //see if any got completely eaten
            while(!bDone)
            {
                bDone = true;
                foreach(Brush b in brushes)
                {
                    if(!b.IsValid())
                    {
                        Debug.WriteLine("Brush totally clipped away");

                        brushes.Remove(b);
                        bDone = false;
                        break;
                    }
                }
            }
        }
    }
}
