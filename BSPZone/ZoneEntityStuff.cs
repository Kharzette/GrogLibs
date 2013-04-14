using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.Xna.Framework;


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
		}

		Dictionary<ZoneEntity, ZoneLight>	mLightCache	=new Dictionary<ZoneEntity, ZoneLight>();

		public delegate float GetStyleStrength(int styleIndex);


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


		public Vector3 GetPlayerStartPos()
		{
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
		public ZoneLight GetStrongestLightInLOS(Vector3 pos, GetStyleStrength gss)
		{
			List<ZoneLight>	visLights	=GetLightsInLOS(pos);

			//look for distance minus strength
			float		bestDist	=float.MaxValue;
			ZoneLight	bestLight	=null;
			foreach(ZoneLight zl in visLights)
			{
				float	dist	=Vector3.Distance(pos, zl.mPosition);

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

			return	bestLight;
		}


		void BuildLightCache()
		{
			foreach(ZoneEntity ze in mZoneEntities)
			{
				if(ze.IsLight())
				{
					ZoneLight	zl	=new ZoneLight();

					ze.GetOrigin(out zl.mPosition);
					ze.GetLightValue(out zl.mStrength);
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
}
