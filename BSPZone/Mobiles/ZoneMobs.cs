using System;
using System.Collections.Generic;
using System.Text;
using SharpDX;
using UtilityLib;


namespace BSPZone
{
	public partial class Zone
	{
		List<MobileAI>	mMobs	=new List<MobileAI>();

		BoundingBox	mMiniBox	=BoundingBox.FromSphere(new BoundingSphere(Vector3.Zero, 1f));


		public void AddMob(MobileAI mob)
		{
			mMobs.Add(mob);
		}


		//returns true if hit world
		public bool RayCollideMobiles(Vector3 start, Vector3 end,
			UInt32 state, UInt32 faction,
			out MobileAI hit, out Vector3 worldHit)
		{
			List<MobileAI>	inView	=GetAITargetsInPVS(start, state, faction);

			worldHit	=Vector3.Zero;

			//first ray cast against the world
			float		bestDist;
			Collision	col;

			bool	bHit	=TraceAll(null, null, start, end, out col);
			if(bHit)
			{
				worldHit	=col.mIntersection;
				bestDist	=Vector3.Distance(start, worldHit);
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
			return	bHit;
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
				Vector3	mobPos	=mob.GetGroundPosition();
				float	dist	=Vector3.Distance(mobPos, pos);
				if(dist < bestDist)
				{
					ret			=mob;
					bestDist	=dist;
				}
			}
			return	ret;
		}


		//finds the middle ground of a bunch of mobiles
		public static Vector3 FindCenter(List<MobileAI> targs)
		{
			Vector3	ret	=Vector3.Zero;

			foreach(MobileAI mob in targs)
			{
				ret	+=mob.GetGroundPosition();
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
				Vector3	mobPos	=mob.GetMiddlePosition();

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
				Vector3	mobPos	=mob.GetMiddlePosition();

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
				Vector3	mobPos	=mob.GetMiddlePosition();

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
				Vector3	mobPos	=mob.GetMiddlePosition();

				Collision	col;
				if(TraceAll(null, null, pov, mobPos, out col))
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
				Vector3	mobPos	=mob.GetMiddlePosition();

				float	dist	=Vector3.Distance(pov, mobPos);
				if(dist > maxDistance)
				{
					continue;
				}
				
				Collision	col;
				if(TraceAll(null, null, pov, mobPos, out col))
				{
					continue;
				}

				ret.Add(mob);
			}
			return	ret;
		}
	}
}