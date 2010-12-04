using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class MapEntity
	{
		public List<MapBrush>				mBrushes	=new List<MapBrush>();
		public Dictionary<string, string>	mData		=new Dictionary<string, string>();
		public Int32						mModelNum;
		public UInt32						mFlags;


		public bool GetOrigin(out Vector3 org)
		{
			org	=Vector3.Zero;
			if(!mData.ContainsKey("origin"))
			{
				return	false;
			}
			string	[]szVec	=mData["origin"].Split(' ');
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


		public bool GetLightValue(out float dist)
		{
			dist	=250;

			if(mData.ContainsKey("light"))
			{
				if(!Single.TryParse(mData["light"], out dist))
				{
					return	false;
				}
				return	true;
			}
			return	false;
		}


		public bool GetLightValue(out Vector4 val)
		{
			val	=Vector4.Zero;

			if(mData.ContainsKey("_light"))
			{
				string	[]elements	=mData["_light"].Split(' ');

				Single.TryParse(elements[0], out val.X);
				Single.TryParse(elements[1], out val.Y);
				Single.TryParse(elements[2], out val.Z);
				Single.TryParse(elements[3], out val.W);
				return	true;
			}
			return	false;
		}


		//omni lights use position as a vector
		public bool IsLightOmni()
		{
			return	mData.ContainsKey("omni");
		}


		public bool IsLightEnvironment()
		{
			return	(mData["classname"] == "light_environment");
		}


		public bool GetColor(out Vector3 color)
		{
			color	=Vector3.One;
			string	val	="";
			if(mData.ContainsKey("color"))
			{
				val	=mData["color"];
			}
			else if(mData.ContainsKey("_color"))
			{
				val	=mData["_color"];
			}
			else
			{
				return	false;
			}
			string	[]szVec	=val.Split(' ');

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


		//read a single entity block
		internal void ReadVMFEntBlock(StreamReader sr, int entityNum, PlanePool pool, TexInfoPool tiPool)
		{
			string	s	="";
			while((s = sr.ReadLine()) != null)
			{
				s	=s.Trim();
				if(s.StartsWith("\""))
				{
					string	[]tokens;
					tokens	=s.Split('\"');

					if(mData.ContainsKey(tokens[1]))
					{
						mData[tokens[1]]	=tokens[3];
					}
					else
					{
						mData.Add(tokens[1], tokens[3]);
					}
				}
				else if(s == "editor")
				{
					Brush.SkipVMFEditorBlock(sr);
				}
				else if(s == "solid")
				{
					MapBrush	b	=new MapBrush();

					if(b.ReadVMFSolidBlock(sr, pool, tiPool, entityNum))
					{
						b.MakePolys(pool);
						b.FixContents();
						mBrushes.Add(b);
					}
				}
				else if(s == "connections")
				{
					Brush.SkipVMFEditorBlock(sr);
				}
				else if(s.StartsWith("}"))
				{
					return;	//entity done
				}
			}
		}


		void SkipVMFGroupBlock(StreamReader sr)
		{
			string	s	="";
			while((s = sr.ReadLine()) != null)
			{
				s	=s.Trim();
				if(s.StartsWith("}"))
				{
					return;	//skip done
				}
				else if(s == "editor")
				{
					Brush.SkipVMFEditorBlock(sr);
				}
			}
		}


		//read a single entity block
		internal void ReadVMFWorldBlock(StreamReader sr, int entityNum, PlanePool pool, TexInfoPool tiPool)
		{
			string	s	="";
			while((s = sr.ReadLine()) != null)
			{
				s	=s.Trim();
				if(s == "solid")
				{
					MapBrush	b	=new MapBrush();

					if(b.ReadVMFSolidBlock(sr, pool, tiPool, entityNum))
					{
						b.MakePolys(pool);
						b.FixContents();
						mBrushes.Add(b);
					}
				}
				else if(s == "group")
				{
					SkipVMFGroupBlock(sr);
				}
				else if(s.StartsWith("\""))
				{
					string	[]tokens;
					tokens	=s.Split('\"');

					mData.Add(tokens[1], tokens[3]);
				}
				else if(s.StartsWith("}"))
				{
					return;	//entity done
				}
			}
		}


		//read's hammer files
		internal void ReadFromVMF(StreamReader sr, int entityNum, PlanePool pool, TexInfoPool tiPool)
		{
			string	s	="";
			while((s = sr.ReadLine()) != null)
			{
				s	=s.Trim();
				if(s == "entity")
				{
					ReadVMFEntBlock(sr, entityNum, pool, tiPool);
					return;
				}
				else if(s == "world")
				{
					ReadVMFWorldBlock(sr, entityNum, pool, tiPool);
					return;
				}
			}
		}


		//old school quake maps
		internal void ReadFromMap(StreamReader sr, PlanePool pool, TexInfoPool tiPool, int entityNum)
		{
			string	s	="";
			while((s = sr.ReadLine()) != null)
			{
				s	=s.Trim();
				if(s.StartsWith("\""))
				{
					string	[]tokens;
					tokens	=s.Split('\"');

					if(mData.ContainsKey(tokens[1]))
					{
						mData[tokens[1]]	=tokens[3];
					}
					else
					{
						mData.Add(tokens[1], tokens[3]);
					}
				}
				else if(s == "{")
				{
					MapBrush	b	=new MapBrush();

					if(b.ReadFromMap(sr, pool, tiPool, entityNum))
					{
						b.MakePolys(pool);
						b.FixContents();
						mBrushes.Add(b);
					}
				}
				else if(s.StartsWith("}"))
				{
					return;	//entity done
				}
			}
		}


		public void Read(BinaryReader br)
		{
			int	dataCount	=br.ReadInt32();

			mData	=new Dictionary<string, string>();
			for(int i=0;i < dataCount;i++)
			{
				string	key		=br.ReadString();
				string	value	=br.ReadString();

				mData.Add(key, value);
			}
		}


		public void Write(BinaryWriter bw)
		{
			//write out # of key value pairs
			bw.Write(mData.Count);

			foreach(KeyValuePair<string, string> pair in mData)
			{
				bw.Write(pair.Key);
				bw.Write(pair.Value);
			}
		}
	}
}
