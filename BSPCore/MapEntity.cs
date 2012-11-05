using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;
using System.ComponentModel;
using UtilityLib;


namespace BSPCore
{
	public class MapEntity : UtilityLib.IReadWriteable
	{
		BindingList<MapBrush>	mBrushes	=new BindingList<MapBrush>();

		internal Dictionary<string, string>	mData		=new Dictionary<string, string>();
		internal Int32						mModelNum;	//donut use this, use the key/value


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


		//this will either use the origin brush or
		//the center of the model
		public void SetModelOrigin()
		{
			//is this even a bmodel?
			if(mBrushes.Count == 0)
			{
				return;
			}

			//model number set?
			if(!mData.ContainsKey("Model"))
			{
				return;
			}

			//see if an origin already exists
			if(mData.ContainsKey("ModelOrigin"))
			{
				mData.Remove("ModelOrigin");	//blast it
			}

			//world org is 0 0 0
			if(mData["Model"] == "0")
			{
				mData.Add("ModelOrigin", Misc.VectorToString(Vector3.Zero));
			}
			else
			{
				//check for an origin brush
				foreach(MapBrush mb in mBrushes)
				{
					if(Misc.bFlagSet(mb.mContents, Contents.BSP_CONTENTS_ORIGIN))
					{
						//grab the origin
						Vector3	org	=mb.mBounds.GetCenter();
						mData.Add("ModelOrigin", Misc.VectorToString(org));
						return;
					}
				}

				//none found?  Just use the center of the entire model
				Bounds	bnd		=new Bounds();
				bool	bFirst	=true;
				foreach(MapBrush mb in mBrushes)
				{
					if(bFirst)
					{
						bnd		=mb.mBounds;
						bFirst	=false;
					}
					else
					{
						bnd.Merge(bnd, mb.mBounds);
					}
				}

				Vector3	org2	=bnd.GetCenter();
				mData.Add("ModelOrigin", Misc.VectorToString(org2));
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

			if(mData.ContainsKey("_color"))
			{
				string	[]elements	=mData["_color"].Split(' ');

				UtilityLib.Mathery.TryParse(elements[0], out val.X);
				UtilityLib.Mathery.TryParse(elements[1], out val.Y);
				UtilityLib.Mathery.TryParse(elements[2], out val.Z);

				val	*=255.0f;
			}

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
				if(val.X + val.Y + val.Z == 0.0f)
				{
					val		=Vector4.One * 255.0f;
				}
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


		//old school quake maps
		internal void ReadFromMap(StreamReader sr, TexInfoPool tiPool,
			int entityNum, BSPBuildParams prms)
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
					if(b.ReadFromMap(sr, tiPool, entityNum, prms))
					{
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
			if(test != "" && (char.IsLetter(test[0]) || test[0] == '_'))
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


		internal void MoveBrushesToOrigin()
		{
			Vector3	org	=Vector3.Zero;

			if(!GetVectorNoConversion("ModelOrigin", out org))
			{
				CoreEvents.Print("Unable to grab origin in model " + mModelNum + "\n");
				return;
			}

			//move brushes to origin
			foreach(MapBrush mb in mBrushes)
			{
				mb.MovePolys(-org);
			}
		}


		internal void MakeBrushPolys(PlanePool pool, ClipPools cp)
		{
			//pool planes
			foreach(MapBrush mb in mBrushes)
			{
				mb.PoolPlanes(pool);
			}

			foreach(MapBrush mb in mBrushes)
			{
				mb.MakePolys(pool, true, cp);
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
