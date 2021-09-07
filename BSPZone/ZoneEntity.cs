using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SharpDX;
using UtilityLib;


namespace BSPZone
{
	public class ZoneEntity
	{
		public Dictionary<string, string>	mData		=new Dictionary<string, string>();


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

			if(!Mathery.TryParse(szVec[0], out org.X))
			{
				return	false;
			}
			if(!Mathery.TryParse(szVec[1], out org.Y))
			{
				return	false;
			}
			if(!Mathery.TryParse(szVec[2], out org.Z))
			{
				return	false;
			}

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


		public bool GetBool(string key, out bool val)
		{
			val	=false;
			if(!mData.ContainsKey(key))
			{
				return	false;
			}
			if(!Mathery.TryParse(mData[key], out val))
			{
				return	false;
			}
			return	true;
		}


		public bool GetInt(string key, out int val)
		{
			val	=0;
			if(!mData.ContainsKey(key))
			{
				return	false;
			}
			if(!Mathery.TryParse(mData[key], out val))
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
			if(!Mathery.TryParse(mData[key], out val))
			{
				return	false;
			}
			return	true;
		}


		public bool GetDirectionFromAngles(string key, out Vector3 dir)
		{
			Matrix	orient;
			if(GetMatrixFromAngles(key, out orient))
			{
				dir		=orient.Forward;
				return	true;
			}
			dir		=Vector3.Zero;
			return	false;
		}


		public bool GetCorrectedAngles(string key, out int pitch, out int yaw, out int roll)
		{
			Vector3	orient;
			if(GetVectorNoConversion(key, out orient))
			{
				//coordinate system goblinry
				pitch	=(int)-orient.X;
				yaw		=-90 + (int)-orient.Y;
				roll	=(int)orient.Z;	//roll shouldn't ever really be used I think
			}
			else
			{
				pitch	=yaw	=roll	=0;
				return	false;
			}
			return	true;
		}


		public bool GetMatrixFromAngles(string key, out Matrix orientation)
		{
			int		iPitch, iYaw, iRoll;
			float	yaw, pitch, roll;

			if(!GetCorrectedAngles(key, out iPitch, out iYaw, out iRoll))
			{
				orientation	=Matrix.Identity;
				return		false;
			}

			yaw		=MathUtil.DegreesToRadians(iYaw);
			pitch	=MathUtil.DegreesToRadians(iPitch);
			roll	=MathUtil.DegreesToRadians(iRoll);

			orientation	=Matrix.RotationYawPitchRoll(yaw, pitch, roll);

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

			if(!Mathery.TryParse(szVec[0], out org.X))
			{
				return	false;
			}
			if(!Mathery.TryParse(szVec[1], out org.Y))
			{
				return	false;
			}
			if(!Mathery.TryParse(szVec[2], out org.Z))
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
				if(!Mathery.TryParse(mData["light"], out dist))
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

				Mathery.TryParse(elements[0], out val.X);
				Mathery.TryParse(elements[1], out val.Y);
				Mathery.TryParse(elements[2], out val.Z);
				Mathery.TryParse(elements[3], out val.W);
				return	true;
			}
			else if(mData.ContainsKey("light"))
			{
				val		=Vector4.One * 255.0f;
				Mathery.TryParse(mData["light"], out val.W);
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

			if(!Mathery.TryParse(szVec[0], out color.X))
			{
				return	false;
			}
			if(!Mathery.TryParse(szVec[1], out color.Y))
			{
				return	false;
			}
			if(!Mathery.TryParse(szVec[2], out color.Z))
			{
				return	false;
			}
			return	true;
		}


		public void SetInt(string key, int val)
		{
			if(!mData.ContainsKey(key))
			{
				mData.Add(key, "" + val);
			}
			mData[key]	="" + val;	//should be ok
		}


		public bool IsActivated()
		{
			//see if already on
			int	activated;
			if(GetInt("activated", out activated))
			{
				return	activated != 0;
			}
			return	false;
		}


		//flips and returns new state
		public bool ToggleEntityActivated()
		{
			//see if already on
			int	activated;

			if(IsActivated())
			{
				activated	=0;
			}
			else
			{
				activated	=1;
			}

			SetInt("activated", activated);

			return	(activated != 0);
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
