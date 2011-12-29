using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;
using System.ComponentModel;


namespace BSPCore
{
	public class MapEntity : UtilityLib.IReadWriteable
	{
		BindingList<MapBrush>	mBrushes	=new BindingList<MapBrush>();

		internal Dictionary<string, string>	mData		=new Dictionary<string, string>();
		internal Int32						mModelNum;


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

			if(!UtilityLib.Mathery.TryParse(szVec[0], out org.X))
			{
				return	false;
			}
			if(!UtilityLib.Mathery.TryParse(szVec[1], out org.Y))
			{
				return	false;
			}
			if(!UtilityLib.Mathery.TryParse(szVec[2], out org.Z))
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


		internal bool GetInt(string key, out int val)
		{
			val	=0;
			if(!mData.ContainsKey(key))
			{
				return	false;
			}
			if(!Int32.TryParse(mData[key], out val))
			{
				return	false;
			}
			return	true;
		}


		public bool GetFloat(string key, out float val)
		{
			val	=0.0f;
			if(!mData.ContainsKey(key))
			{
				return	false;
			}
			if(!UtilityLib.Mathery.TryParse(mData[key], out val))
			{
				return	false;
			}
			return	true;
		}


		public bool GetVector(string key, out Vector3 org)
		{
			if(GetVectorNoConversion(key, out org))
			{
				//flip x
				org.X	=-org.X;

				//swap y and z
				float	zTemp	=org.Z;
				org.Z	=org.Y;
				org.Y	=zTemp;

				return	true;
			}
			else
			{
				return	false;
			}
		}


		public bool GetVectorNoConversion(string key, out Vector3 org)
		{
			org	=Vector3.Zero;
			if(!mData.ContainsKey(key))
			{
				return	false;
			}
			string	[]szVec	=mData[key].Split(' ');
			if(szVec.Length != 3)
			{
				return	false;
			}

			if(!UtilityLib.Mathery.TryParse(szVec[0], out org.X))
			{
				return	false;
			}
			if(!UtilityLib.Mathery.TryParse(szVec[1], out org.Y))
			{
				return	false;
			}
			if(!UtilityLib.Mathery.TryParse(szVec[2], out org.Z))
			{
				return	false;
			}

			return	true;
		}


		public bool GetLightValue(out float dist)
		{
			dist	=250;

			if(mData.ContainsKey("light"))
			{
				if(!UtilityLib.Mathery.TryParse(mData["light"], out dist))
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

				UtilityLib.Mathery.TryParse(elements[0], out val.X);
				UtilityLib.Mathery.TryParse(elements[1], out val.Y);
				UtilityLib.Mathery.TryParse(elements[2], out val.Z);
				UtilityLib.Mathery.TryParse(elements[3], out val.W);
				return	true;
			}
			else if(mData.ContainsKey("light"))
			{
				val		=Vector4.One * 255.0f;
				UtilityLib.Mathery.TryParse(mData["light"], out val.W);
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

			if(!UtilityLib.Mathery.TryParse(szVec[0], out color.X))
			{
				return	false;
			}
			if(!UtilityLib.Mathery.TryParse(szVec[1], out color.Y))
			{
				return	false;
			}
			if(!UtilityLib.Mathery.TryParse(szVec[2], out color.Z))
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
					SkipVMFEditorBlock(sr);
				}
				else if(s == "solid")
				{
					MapBrush	b	=new MapBrush();

					if(b.ReadVMFSolidBlock(sr, pool, tiPool, entityNum))
					{
						b.MakePolys(pool, true);
						b.FixContents(true);

						if(mData["classname"] == "func_detail")
						{
							b.mContents	|=Contents.BSP_CONTENTS_DETAIL2;
						}
						mBrushes.Add(b);
					}
				}
				else if(s == "connections")
				{
					SkipVMFEditorBlock(sr);
				}
				else if(s.StartsWith("}"))
				{
					return;	//entity done
				}
			}
		}


		static internal void SkipVMFEditorBlock(StreamReader sr)
		{
			string	s	="";
			while((s = sr.ReadLine()) != null)
			{
				s	=s.Trim();
				if(s.StartsWith("}"))
				{
					return;	//editor done
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
					MapEntity.SkipVMFEditorBlock(sr);
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
						b.MakePolys(pool, true);
						b.FixContents(true);
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
						b.MakePolys(pool, true);
						b.FixContents(false);
						mBrushes.Add(b);
					}
				}
				else if(s.StartsWith("}"))
				{
					return;	//entity done
				}
			}
		}


		public void Read2(BinaryReader br)
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


		public void Read(BinaryReader br)
		{
			int	dataCount	=br.ReadInt32();

			//see if this is C++ genesis
			bool	bCPP	=true;
			long	pos		=br.BaseStream.Position;
			string	test	=br.ReadString();
			if(test != "" && char.IsLetter(test[0]))
			{
				bCPP	=false;
			}

			//skip back before the test
			br.BaseStream.Seek(pos, SeekOrigin.Begin);

			mData	=new Dictionary<string, string>();
			for(int i=0;i < dataCount;i++)
			{
				if(bCPP)
				{
					Int32	strSize	=br.ReadInt32();
					string	key	=new string(br.ReadChars(strSize));
					strSize	=br.ReadInt32();
					string	value	=new string(br.ReadChars(strSize));

					key		=key.Substring(0, key.Length - 1);
					value	=value.Substring(0, value.Length - 1);

					if(mData.ContainsKey(key))
					{
						CoreEvents.Print("Same key in entity!\n");
					}
					else
					{
						mData.Add(key, value);
					}
				}
				else
				{
					string	key		=br.ReadString();
					string	value	=br.ReadString();

					mData.Add(key, value);
				}
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


		internal void GetLightType(out UInt32 type)
		{
			string	className	="";
			if(mData.ContainsKey("classname"))
			{
				className	=mData["classname"];
			}
			if(className == "light_spot")
			{
				type	=DirectLight.DLight_Spot;
			}
			else
			{
				type	=DirectLight.DLight_Point;
			}
		}


		internal void GetTriangles(List<Vector3> verts, List<uint> indexes, bool bGetClipOnly)
		{
			if(mBrushes.Count > 0)
			{
				foreach(MapBrush mb in mBrushes)
				{
					if(bGetClipOnly)
					{
						if((mb.mContents & Contents.BSP_CONTENTS_CLIP2) != 0)
						{
							mb.GetTriangles(verts, indexes, false);
						}
					}
					else
					{
						mb.GetTriangles(verts, indexes, false);
					}
				}
			}
		}

		internal void CountBrushes(ref int numDetails, ref int numSolids, ref int numTotal)
		{
			foreach(MapBrush mb in mBrushes)
			{
				if((mb.mContents & Contents.BSP_CONTENTS_DETAIL2) != 0)
				{
					numDetails++;
				}
				else if((mb.mContents & Contents.BSP_CONTENTS_SOLID2) != 0)
				{
					numSolids++;
				}
				numTotal++;
			}
		}

		public BindingList<MapBrush> GetBrushes()
		{
			return	mBrushes;
		}


		internal int GetBrushCount()
		{
			return	mBrushes.Count;
		}
	}
}
