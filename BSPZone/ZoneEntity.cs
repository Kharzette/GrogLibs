using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPZone
{
	public class ZoneEntity : UtilityLib.IReadWriteable
	{
		public Dictionary<string, string>	mData		=new Dictionary<string, string>();
//		internal Int32						mModelNum;


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


		public string GetTarget()
		{
			if(!mData.ContainsKey("target"))
			{
				return	"";
			}
			return	mData["target"];
		}


		public string GetTargetName()
		{
			if(!mData.ContainsKey("targetname"))
			{
				return	"";
			}
			return	mData["targetname"];
		}


		public string GetValue(string key)
		{
			if(!mData.ContainsKey(key))
			{
				return	"";
			}
			return	mData[key];
		}


		public bool GetInt(string key, out int val)
		{
			val	=0;
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


		internal bool GetFloat(string key, out float val)
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


		internal bool GetLightValue(out float dist)
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


		internal bool GetLightValue(out Vector4 val)
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
		internal bool IsLightOmni()
		{
			return	mData.ContainsKey("omni");
		}


		internal bool IsLightEnvironment()
		{
			return	(mData["classname"] == "light_environment");
		}


		internal bool GetColor(out Vector3 color)
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


		internal bool IsLight()
		{
			Vector4	val;
			return	GetLightValue(out val);
		}
	}
}
