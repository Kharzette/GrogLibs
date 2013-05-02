using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using UtilityLib;


namespace BSPZone
{
	public partial class Zone
	{
		List<MobileAI>	mMobs	=new List<MobileAI>();

		BoundingBox	mMiniBox	=BoundingBox.CreateFromSphere(new BoundingSphere(Vector3.Zero, 4f));


		public void AddMob(MobileAI mob)
		{
			mMobs.Add(mob);
		}


		//returns true if hit a mobileAI
		public bool RayCollideMobiles(Vector3 start, Vector3 end,
			UInt32 state, UInt32 faction, out MobileAI hit)
		{
			List<MobileAI>	inView	=GetAITargetsInLOS(start, state, faction);

			//first ray cast against the world
			Vector3		impacto		=Vector3.Zero;
			int			hitModel	=0;
			ZonePlane	planeHit	=ZonePlane.Blank;

			bool	bHit	=Trace_All(mMiniBox, start, end, ref hitModel, ref impacto, ref planeHit);

			float	bestDist;

			if(bHit)
			{
				bestDist	=Vector3.Distance(start, impacto);
			}
			else
			{
				bestDist	=float.MaxValue;
			}

			hit		=null;
			foreach(MobileAI mob in inView)
			{
				BoundingBox	worldBound	=mob.GetWorldBounds();

				float?	dist	=Mathery.RayIntersectBox(start, end, worldBound);
				if(dist == null)
				{
					continue;
				}

				if(dist.Value < bestDist)
				{
					bestDist	=dist.Value;
					hit			=mob;
				}
			}
			return	(hit != null);
		}


		public List<MobileAI> GetAITargets(Vector3 pov, UInt32 hasCustomState,
			UInt32 faction, MobileAI.Visibility vis, float maxDistance)
		{
			switch(vis)
			{
				case	MobileAI.Visibility.InLOS:
					return	GetAITargetsInLOS(pov, hasCustomState, faction);
				case	MobileAI.Visibility.InLOSRange:
					return	GetAITargetsInLOSRange(pov, hasCustomState, faction, maxDistance);
				case	MobileAI.Visibility.InPVS:
					return	GetAITargetsInPVS(pov, hasCustomState, faction);
				case	MobileAI.Visibility.InPVSRange:
					return	GetAITargetsInPVSRange(pov, hasCustomState, faction, maxDistance);
				case	MobileAI.Visibility.ZoneWide:
					return	GetAITargetsZoneWide(hasCustomState, faction);
				case	MobileAI.Visibility.ZoneWideRange:
					return	GetAITargetsZoneWideRange(pov, hasCustomState, faction, maxDistance);
			}

			return	new List<MobileAI>();	//empty list if nothin
		}


		//find the closest mobile in the list to the point pos
		public static MobileAI FindClosestTo(List<MobileAI> targs, Vector3 pos)
		{
			MobileAI	ret	=null;

			float	bestDist	=float.MaxValue;
			foreach(MobileAI mob in targs)
			{
				Vector3	mobPos	=mob.GetPosition();
				float	dist	=Vector3.Distance(mobPos, pos);
				if(dist < bestDist)
				{
					ret			=mob;
					bestDist	=dist;
				}
			}
			return	ret;
		}


		//finds the middle of a bunch of mobiles
		public static Vector3 FindCenter(List<MobileAI> targs)
		{
			Vector3	ret	=Vector3.Zero;

			foreach(MobileAI mob in targs)
			{
				ret	+=mob.GetPosition();
			}
			ret	/=targs.Count;

			return	ret;
		}


		List<MobileAI>	GetAITargetsZoneWide(UInt32 cState, UInt32 faction)
		{
			List<MobileAI>	ret	=new List<MobileAI>();

			foreach(MobileAI mob in mMobs)
			{
				if(!Misc.bFlagSet(mob.mCustomState, cState))
				{
					continue;
				}
				if(Misc.bFlagSet(mob.GetFaction(), faction))
				{
					ret.Add(mob);
				}
			}
			return	ret;
		}


		List<MobileAI>	GetAITargetsZoneWideRange(Vector3 pov,
			UInt32 cState, UInt32 faction, float maxDist)
		{
			List<MobileAI>	ret	=new List<MobileAI>();

			List<MobileAI>	zoneWide	=GetAITargetsZoneWide(cState, faction);

			foreach(MobileAI mob in zoneWide)
			{
				Vector3	mobPos	=mob.GetPosition();

				float	dist	=Vector3.Distance(pov, mobPos);
				if(dist > maxDist)
				{
					continue;
				}

				ret.Add(mob);
			}
			return	ret;
		}


		List<MobileAI>	GetAITargetsInPVS(Vector3 pov, UInt32 cState, UInt32 faction)
		{
			List<MobileAI>	ret	=new List<MobileAI>();

			List<MobileAI>	zoneWideOnFact	=GetAITargetsZoneWide(cState, faction);

			foreach(MobileAI mob in zoneWideOnFact)
			{
				Vector3	mobPos	=mob.GetPosition();

				if(!IsVisibleFrom(pov, mobPos))
				{
					continue;
				}
				ret.Add(mob);
			}
			return	ret;
		}


		List<MobileAI>	GetAITargetsInPVSRange(Vector3 pov, UInt32 cState, UInt32 faction, float maxDist)
		{
			List<MobileAI>	ret	=new List<MobileAI>();

			List<MobileAI>	inPVS	=GetAITargetsInPVS(pov, cState, faction);

			foreach(MobileAI mob in inPVS)
			{
				Vector3	mobPos	=mob.GetPosition();

				float	dist	=Vector3.Distance(pov, mobPos);
				if(dist > maxDist)
				{
					continue;
				}

				ret.Add(mob);
			}
			return	ret;
		}


		List<MobileAI>	GetAITargetsInLOS(Vector3 pov, UInt32 cState, UInt32 faction)
		{
			List<MobileAI>	ret	=new List<MobileAI>();

			List<MobileAI>	inPVS	=GetAITargetsInPVS(pov, cState, faction);

			foreach(MobileAI mob in inPVS)
			{
				Vector3	mobPos	=mob.GetPosition();

				Vector3		impacto		=Vector3.Zero;
				int			hitModel	=0;
				ZonePlane	planeHit	=ZonePlane.Blank;
				if(Trace_All(mMiniBox, pov, mobPos, ref hitModel, ref impacto, ref planeHit))
				{
					continue;
				}

				ret.Add(mob);
			}
			return	ret;
		}


		List<MobileAI>	GetAITargetsInLOSRange(Vector3 pov, UInt32 cState, UInt32 faction, float maxDistance)
		{
			List<MobileAI>	ret	=new List<MobileAI>();

			List<MobileAI>	inPVS	=GetAITargetsInPVS(pov, cState, faction);

			foreach(MobileAI mob in inPVS)
			{
				Vector3	mobPos	=mob.GetPosition();

				float	dist	=Vector3.Distance(pov, mobPos);
				if(dist > maxDistance)
				{
					continue;
				}

				Vector3		impacto		=Vector3.Zero;
				int			hitModel	=0;
				ZonePlane	planeHit	=ZonePlane.Blank;
				if(Trace_All(mMiniBox, pov, mobPos, ref hitModel, ref impacto, ref planeHit))
				{
					continue;
				}

				ret.Add(mob);
			}
			return	ret;
		}
	}
}