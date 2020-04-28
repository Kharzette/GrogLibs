using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using SharpDX;
using UtilityLib;

using DynLight	=MaterialLib.DynamicLights.DynLight;


namespace BSPZone
{
	public partial class Zone
	{
		public class ZoneLight
		{
			public float	mStrength;
			public Vector3	mPosition;
			public Vector3	mColor;
			public int		mStyle;
			public bool		mbOn;			//on by default
			public bool		mbSwitchable;	//switchable lights
			public bool		mbSun;			//sun light
		}

		//just a list of which model indexes are triggers
		List<int>	mNonCollidingModels	=new List<int>();

		Dictionary<ZoneEntity, ZoneLight>	mLightCache	=new Dictionary<ZoneEntity, ZoneLight>();

		public delegate float GetStyleStrength(int styleIndex);

		//face flag constants from core texinfos
		public const UInt32	MIRROR		=(1<<0);
		public const UInt32	FULLBRIGHT	=(1<<1);
		public const UInt32	SKY			=(1<<2);
		public const UInt32	EMITLIGHT	=(1<<3);
		public const UInt32	TRANSPARENT	=(1<<4);
		public const UInt32	GOURAUD		=(1<<5);
		public const UInt32	FLAT		=(1<<6);
		public const UInt32	CELSHADE	=(1<<7);
		public const UInt32	NO_LIGHTMAP	=(1<<15);


		public List<ZoneEntity> GetSwitchedOnLights()
		{
			List<ZoneEntity>	ret	=new List<ZoneEntity>();

			foreach(KeyValuePair<ZoneEntity, ZoneLight> zl in mLightCache)
			{
				if(zl.Value.mbSwitchable && zl.Value.mbOn)
				{
					ret.Add(zl.Key);
				}
			}
			return	ret;
		}


		public Vector3 GetPlayerStartPos(out float angle)
		{
			angle	=0f;

			foreach(ZoneEntity e in mEntities)
			{
				if(e.mData.ContainsKey("classname"))
				{
					if(e.mData["classname"] != "info_player_start")
					{
						continue;
					}
				}
				else
				{
					continue;
				}

				Vector3	ret	=Vector3.Zero;
				if(e.GetOrigin(out ret))
				{
					e.GetFloat("angle", out angle);
					return	ret;
				}
			}
			return	Vector3.Zero;
		}


		public List<ZoneEntity> GetEntitiesByTargetName(string targName)
		{
			//targetnames can have multiple entries
			string	[]targs	=targName.Split(' ');

			List<ZoneEntity>	ret	=new List<ZoneEntity>();
			foreach(ZoneEntity ze in mEntities)
			{
				if(ze.mData.ContainsKey("targetname"))
				{
					string	checkName	=ze.mData["targetname"];

					foreach(string targ in targs)					
					{
						if(targ == checkName)
						{
							ret.Add(ze);
						}
					}
				}
			}
			return	ret;
		}


		public List<ZoneEntity> GetEntitiesByTarget(string targ)
		{
			//targets can have multiple entries
			string	[]targs	=targ.Split(' ');

			List<ZoneEntity>	ret	=new List<ZoneEntity>();
			foreach(ZoneEntity ze in mEntities)
			{
				if(ze.mData.ContainsKey("target"))
				{
					string	checkName	=ze.mData["target"];

					foreach(string trg in targs)					
					{
						if(targ == checkName)
						{
							ret.Add(ze);
						}
					}
				}
			}
			return	ret;
		}


		public List<ZoneEntity> GetEntitiesTargetNameStartsWith(string targName)
		{
			List<ZoneEntity>	ret	=new List<ZoneEntity>();
			foreach(ZoneEntity ze in mEntities)
			{
				string	tName	=ze.GetTargetName();
				if(tName.StartsWith(targName))
				{
					ret.Add(ze);
				}
			}
			return	ret;
		}


		public List<ZoneEntity> GetEntities(string className)
		{
			List<ZoneEntity>	ret	=new List<ZoneEntity>();
			foreach(ZoneEntity ze in mEntities)
			{
				if(ze.mData.ContainsKey("classname"))
				{
					if(ze.mData["classname"] == className)
					{
						ret.Add(ze);
					}
				}
			}
			return	ret;
		}


		internal string GenerateUniqueTargetName()
		{
			startOver:
			string	ret	=Mathery.RandomString(8);

			foreach(ZoneEntity ze in mEntities)
			{
				string	tn	=ze.GetTargetName();

				if(tn == ret)
				{
					goto	startOver;
				}
			}
			return	ret;
		}


		internal void AddEntity(ZoneEntity newEnt)
		{
			mEntities.Add(newEnt);
		}


		public List<ZoneEntity> GetEntitiesStartsWith(string startText)
		{
			List<ZoneEntity>	ret	=new List<ZoneEntity>();
			foreach(ZoneEntity ze in mEntities)
			{
				if(ze.mData.ContainsKey("classname"))
				{
					if(ze.mData["classname"].StartsWith(startText))
					{
						ret.Add(ze);
					}
				}
			}
			return	ret;
		}


		public bool GetRandomPointInsideModelEntity(ZoneEntity ent, Random rand, out Vector3 point)
		{
			string	idxStr	=ent.GetValue("Model");

			int	modIdx;
			if(!Int32.TryParse(idxStr, out modIdx))
			{
				point	=Vector3.Zero;
				return	false;
			}

			ZoneModel	zm	=mZoneModels[modIdx];
			BoundingBox	bb	=zm.mBounds;

			Matrix	modXForm	=zm.mTransform;

			bb.Maximum	=Vector3.TransformCoordinate(bb.Maximum, modXForm);
			bb.Minimum	=Vector3.TransformCoordinate(bb.Minimum, modXForm);

			int	iterations	=0;
			point			=Vector3.Zero;
			for(;iterations < 50;iterations++)
			{
				int	x	=rand.Next((int)bb.Minimum.X, (int)bb.Maximum.X);
				int	y	=rand.Next((int)bb.Minimum.Y, (int)bb.Maximum.Y);
				int	z	=rand.Next((int)bb.Minimum.Z, (int)bb.Maximum.Z);

				point	=new Vector3(x, y, z);

				point	=Vector3.TransformCoordinate(point, zm.mInvertedTransform);

				RayTrace	rt	=new RayTrace(point, point);

				rt.mCollision.mModelHit	=modIdx;

				if(TraceNodeTrigger(rt, point, point, zm.mRootNode))
				{
					point	=Vector3.TransformCoordinate(point, zm.mTransform);
					break;
				}
			}
			return	(iterations < 50);
		}


		//rets will either be zone or dyn lights
		List<object>	GetLightsInLOS(Vector3 pos, MaterialLib.DynamicLights dyn)
		{
			List<object>	ret	=new List<object>();

			foreach(KeyValuePair<ZoneEntity, ZoneLight> zl in mLightCache)
			{
				if(IsVisibleFrom(pos, zl.Value.mPosition))
				{
					Collision	col;
					if(!TraceAll(null, null, pos, zl.Value.mPosition, out col))
					{
						ret.Add(zl.Value);
					}
				}
			}

			if(dyn == null)
			{
				return	ret;
			}

			Dictionary<int, DynLight>	dyns	=dyn.GetDynLights();
			foreach(KeyValuePair<int, DynLight> zl in dyns)
			{
				if(IsVisibleFrom(pos, zl.Value.mPosition))
				{
					Collision	col;
					if(!TraceAll(null, null, pos, zl.Value.mPosition, out col))
					{

						ret.Add(zl.Value);
					}
				}
			}

			return	ret;
		}


		public List<object> GetAffectingLights(Vector3 pos,
			ZoneEntity sunEnt, GetStyleStrength gss, MaterialLib.DynamicLights dyn)
		{
			List<object>	inRange	=new List<object>();
			List<object>	losd	=GetLightsInLOS(pos, dyn);

			//check attenuation
			foreach(object light in losd)
			{
				bool	bOn		=LightHelper.GetLightOn(light);
				bool	bSun	=LightHelper.GetLightSun(light);
				if(!bOn || bSun)
				{
					continue;
				}

				Vector3	lpos	=LightHelper.GetLightPosition(light);
				float	dist	=Vector3.Distance(lpos, pos);
				float	atten	=0;
				int		style	=LightHelper.GetLightStyle(light);
				float	str		=LightHelper.GetLightStrength(light);

				if(style != 0)
				{
					atten	=(str * gss(style));
				}
				else
				{
					atten	=str;
				}

				if(dist <= atten)
				{
					inRange.Add(light);
				}
			}

			//see if the sun is shining on pos
			if(sunEnt != null && mLightCache.ContainsKey(sunEnt))
			{
				ZoneLight	sunLight	=mLightCache[sunEnt];				

				Collision	col;
				if(TraceAll(null, null, pos, -sunLight.mPosition * 10000 + pos, out col))
				{
					if(col.mFaceHit != null)
					{
						if(Misc.bFlagSet(SKY, col.mFaceHit.mFlags))
						{
							inRange.Add(mLightCache[sunEnt]);
						}
					}
				}
			}
			return	inRange;
		}


		//for assigning character lights
		public object GetStrongestLightInLOS(Vector3 pos,
			ZoneEntity sunEnt, GetStyleStrength gss, MaterialLib.DynamicLights dyn)
		{
			List<object>	visLights	=GetLightsInLOS(pos, dyn);

			//look for distance minus strength
			float	bestDist	=float.MaxValue;
			object	bestLight	=null;
			foreach(object light in visLights)
			{
				bool	bOn		=LightHelper.GetLightOn(light);
				bool	bSun	=LightHelper.GetLightSun(light);
				if(!bOn || bSun)
				{
					continue;
				}

				Vector3	lpos	=LightHelper.GetLightPosition(light);
				float	dist	=Vector3.Distance(lpos, pos);
				float	str		=LightHelper.GetLightStrength(light);

				if(dist >= str)
				{
					continue;
				}

				int	style	=LightHelper.GetLightStyle(light);

				if(style != 0)
				{
					dist	-=(str * gss(style));
				}
				else
				{
					dist	-=str;
				}

				if(dist < bestDist)
				{
					bestLight	=light;
					bestDist	=dist;
				}
			}

			float	bestStrength	=LightHelper.GetLightStrength(bestLight);

			if(sunEnt != null && mLightCache.ContainsKey(sunEnt))
			{
				//first check if the sun can overpower the
				//already chosen regular light
				ZoneLight	sunLight	=mLightCache[sunEnt];				

				if(bestLight != null)
				{
					float	bestLightPower	=bestDist;
					if(bestDist > bestStrength)
					{
						bestLightPower	*=(bestStrength / bestDist);
					}

					if(bestLightPower > sunLight.mStrength)
					{
						return	bestLight;
					}				
				}
				
				Collision	col;
				if(TraceAll(null, null, pos, -sunLight.mPosition * 10000 + pos, out col))	//pos contains ray direction
				{
					if(col.mFaceHit != null)
					{
						if(Misc.bFlagSet(SKY, col.mFaceHit.mFlags))
						{
							return	mLightCache[sunEnt];
						}
					}
				}
			}

			return	bestLight;
		}


		internal void SwitchCachedLight(ZoneEntity ze)
		{
			if(!mLightCache.ContainsKey(ze))
			{
				return;
			}
			mLightCache[ze].mbOn	=!mLightCache[ze].mbOn;
		}


		void BuildNonCollidingModelsList()
		{
			List<ZoneEntity>	trigs	=GetEntitiesStartsWith("trigger_");
			List<ZoneEntity>	regs	=GetEntitiesStartsWith("func_region");

			//combine
			trigs.AddRange(regs);

			foreach(ZoneEntity ze in trigs)
			{
				string	mod	=ze.GetValue("Model");
				if(mod == null || mod == "")
				{
					continue;
				}

				int	modIdx	=Convert.ToInt32(mod);

				mNonCollidingModels.Add(modIdx);
			}
		}


		void BuildLightCache()
		{
			List<ZoneEntity>	lights	=GetEntitiesStartsWith("light");

			foreach(ZoneEntity ze in lights)
			{
				ZoneLight	zl	=new ZoneLight();

				ze.GetOrigin(out zl.mPosition);
				if(!ze.GetLightValue(out zl.mStrength))
				{
					if(ze.GetFloat("strength", out zl.mStrength))
					{
						zl.mbSun	=true;

						//stuff the direction in the position field
						Vector3	angles;
						if(!ze.GetVectorNoConversion("angles", out angles))
						{
							continue;	//something wrong with the entity
						}
						float	yaw		=angles.Y - 90;
						float	pitch	=angles.X;
						float	roll	=angles.Z;

						yaw		=MathUtil.DegreesToRadians(yaw);
						pitch	=MathUtil.DegreesToRadians(pitch);
						roll	=MathUtil.DegreesToRadians(roll);

						Matrix	rotMat	=Matrix.RotationYawPitchRoll(yaw, pitch, roll);
						zl.mPosition	=rotMat.Backward;
					}
				}
				ze.GetColor(out zl.mColor);

				//check for switchable lights
				zl.mbOn	=true;
				int	switchNum;
				if(ze.GetInt("LightSwitchNum", out switchNum))
				{
					zl.mbSwitchable	=true;
					int	activated;
					if(ze.GetInt("activated", out activated))
					{
						if(activated == 0)
						{
							zl.mbOn	=false;
						}
					}
					else
					{
						zl.mbOn	=false;
					}
				}

				//check for styled lights
				ze.GetInt("style", out zl.mStyle);

				mLightCache.Add(ze, zl);
			}
		}
	}
}
