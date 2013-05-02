using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using UtilityLib;


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
		public const UInt32	CELLSHADE	=(1<<7);
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

			foreach(ZoneEntity e in mZoneEntities)
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
			List<ZoneEntity>	ret	=new List<ZoneEntity>();
			foreach(ZoneEntity ze in mZoneEntities)
			{
				if(ze.mData.ContainsKey("targetname"))
				{
					if(targName.Contains(ze.mData["targetname"]))
					{
						ret.Add(ze);
					}
				}
			}
			return	ret;
		}


		public List<ZoneEntity> GetEntities(string className)
		{
			List<ZoneEntity>	ret	=new List<ZoneEntity>();
			foreach(ZoneEntity ze in mZoneEntities)
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


		public List<ZoneEntity> GetEntitiesStartsWith(string startText)
		{
			List<ZoneEntity>	ret	=new List<ZoneEntity>();
			foreach(ZoneEntity ze in mZoneEntities)
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


		List<ZoneLight>	GetLightsInLOS(Vector3 pos)
		{
			List<ZoneLight>	ret	=new List<ZoneLight>();

			foreach(KeyValuePair<ZoneEntity, ZoneLight> zl in mLightCache)
			{
				if(IsVisibleFrom(pos, zl.Value.mPosition))
				{
					Vector3	intersection	=Vector3.Zero;
					bool	bHitLeaf		=false;
					Int32	leafHit			=0;
					Int32	nodeHit			=0;

					if(!RayIntersect(pos, zl.Value.mPosition, 0,
						ref intersection, ref bHitLeaf, ref leafHit, ref nodeHit))
					{
						ret.Add(zl.Value);
					}
				}
			}
			return	ret;
		}


		//for assigning character lights
		public ZoneLight GetStrongestLightInLOS(Vector3 pos,
			ZoneEntity sunEnt, GetStyleStrength gss)
		{
			List<ZoneLight>	visLights	=GetLightsInLOS(pos);

			//look for distance minus strength
			float		bestDist	=float.MaxValue;
			ZoneLight	bestLight	=null;
			foreach(ZoneLight zl in visLights)
			{
				if(!zl.mbOn || zl.mbSun)
				{
					continue;
				}

				float	dist	=Vector3.Distance(pos, zl.mPosition);

				if(dist >= zl.mStrength)
				{
					continue;
				}

				if(zl.mStyle != 0)
				{
					dist	-=(zl.mStrength * gss(zl.mStyle));
				}
				else
				{
					dist	-=zl.mStrength;
				}

				if(dist < bestDist)
				{
					bestLight	=zl;
					bestDist	=dist;
				}
			}

			if(sunEnt != null && mLightCache.ContainsKey(sunEnt))
			{
				//first check if the sun can overpower the
				//already chosen regular light
				ZoneLight	sunLight	=mLightCache[sunEnt];				

				if(bestLight != null)
				{
					float	bestLightPower	=bestDist;
					if(bestDist > bestLight.mStrength)
					{
						bestLightPower	*=(bestLight.mStrength / bestDist);
					}

					if(bestLightPower > sunLight.mStrength)
					{
						return	bestLight;
					}				
				}

				Vector3	impacto	=Vector3.Zero;
				int		leafHit	=0;
				int		nodeHit	=0;
				if(RayCollide(pos, -sunLight.mPosition * 10000 + pos,	//pos contains ray direction
					ref impacto, ref leafHit, ref nodeHit))
				{
					ZoneNode	zn	=mZoneNodes[nodeHit];

					if(zn.mNumFaces == 1)
					{
						if(Misc.bFlagSet(SKY, mDebugFaces[zn.mFirstFace].mFlags))
						{
							return	mLightCache[sunEnt];
						}
					}
					else
					{
						for(int i=0;i < zn.mNumFaces;i++)
						{
							DebugFace	df	=mDebugFaces[i + zn.mFirstFace];

							float	sum	=ComputeAngleSum(df, impacto);
							if(sum < (MathHelper.TwoPi - 0.0001f))
							{
								continue;
							}

							if(Misc.bFlagSet(SKY, df.mFlags))
							{
								return	mLightCache[sunEnt];
							}
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

						yaw		=MathHelper.ToRadians(yaw);
						pitch	=MathHelper.ToRadians(pitch);
						roll	=MathHelper.ToRadians(roll);

						Matrix	rotMat	=Matrix.CreateFromYawPitchRoll(yaw, pitch, roll);
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
