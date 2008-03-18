using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
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
				if(s == "color")
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
					//brush coming I think
					b			=new Brush();
					brushComing	=true;					
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
					else
					{
						return;	//entity done
					}
				}
				else if(s.StartsWith("("))
				{
					b.MakeFaceFromMapLine(s);
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
			foreach(Entity e in mEntities)
			{
				foreach(Brush b in e.mBrushes)
				{
					b.Draw(g, fx);
				}
			}
        }


		public void BuildVertexInfo(GraphicsDevice g)
		{
			foreach(Entity e in mEntities)
			{
				foreach(Brush b in e.mBrushes)
				{
					b.BuildVertexInfo(g);
				}
			}
		}


		public void BuildBSPVertexInfo(GraphicsDevice g)
		{
			mTree.BuildVertexInfo(g);
			foreach(Entity e in mEntities)
			{
				foreach(Brush b in e.mBrushes)
				{
					b.BuildVertexInfo(g);
				}
			}
		}


		public bool ClassifyPoint(Vector3 pnt)
		{
			return	mTree.ClassifyPoint(pnt);
		}


		public void	BuildTree()
		{
			//look for the worldspawn
			Entity e	=GetWorldSpawnEntity();

			//ordinarily we'd junk the original brushes
			List<Brush>	copy	=new List<Brush>(e.mBrushes);

			//use the copy so we have the old ones around to draw
			mTree	=new BspTree(copy);
		}


		private	void LightBrushes(GraphicsDevice g, List<Brush> bl, Vector3 lightPos, float lightVal, Vector3 color)
		{
			foreach(Brush b in bl)
			{
				b.LightBrush(g, mTree.GetRoot(), lightPos, lightVal, color);
			}
		}


		public void LightAllBrushes(GraphicsDevice g)
		{
			//find worldspawn brush list
			Entity	wse	=GetWorldSpawnEntity();

			foreach(Entity e in mEntities)
			{
				Vector3	lightPos, clr;
				float	lightVal;
				if(e == GetWorldSpawnEntity())
				{
					continue;
				}
				if(!e.GetLightValue(out lightVal))
				{
					continue;
				}
				if(!e.GetOrigin(out lightPos))
				{
					continue;
				}
				e.GetColor(out clr);
				LightBrushes(g, wse.mBrushes, lightPos, lightVal, clr);
			}
		}


		public void LightAllBspFaces(GraphicsDevice g)
		{
			//find worldspawn brush list
			Entity	wse	=GetWorldSpawnEntity();

			foreach(Entity e in mEntities)
			{
				Vector3	lightPos, clr;
				float	lightVal;
				if(e == GetWorldSpawnEntity())
				{
					continue;
				}
				if(!e.GetLightValue(out lightVal))
				{
					continue;
				}
				if(!e.GetOrigin(out lightPos))
				{
					continue;
				}
				e.GetColor(out clr);
				mTree.Light(g, lightPos, lightVal, clr);
			}
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


		public void BuildPortals()
		{
			mTree.BuildPortals();
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


		public int GetFirstBSPSurface(out Vector3[] surfPoints)
		{
			int	ret	=mTree.GetFirstBSPSurface(out surfPoints);
			if(ret > 0)
			{
				return	ret;
			}
			return	0;
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


		public void GetThriceLightmaps(out List<Texture2D> list)
		{
			GetWorldSpawnEntity().mBrushes[0].GetThriceLightmaps(out list);
		}


		public void DrawPortals(GraphicsDevice g, Effect fx, Vector3 camPos)
		{
			mTree.DrawPortals(g, fx, camPos);
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


        public void RemoveOverlap()
        {
            int			i, j;
			List<Brush>	brushes	=null;

			//look for the worldspawn
			foreach(Entity e in mEntities)
			{
				foreach(string s in e.mKey)
				{
					if(s == "classname")
					{
						if(e.mValue[e.mKey.IndexOf(s)] == "worldspawn")
						{
							brushes	=e.mBrushes;
							break;
						}
					}
				}
				if(brushes != null)
				{
					break;
				}
			}

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
