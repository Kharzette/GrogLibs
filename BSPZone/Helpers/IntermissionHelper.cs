using System;
using System.Numerics;
using System.Collections.Generic;
using System.Text;
using UtilityLib;


namespace BSPZone
{
	public class IntermissionHelper
	{
		Zone	mZone;

		//data
		List<ZoneEntity>	mIMEntities	=new List<ZoneEntity>();


		public void Initialize(Zone zone)
		{
			mZone	=zone;

			mIMEntities	=mZone.GetEntitiesStartsWith("info_player_intermission");
		}


		public void GetRandomIntermissionData(Random mRand,
			out Vector3 pos, out Vector3 lookDir)
		{
			//defaults
			pos		=Vector3.Zero;
			lookDir	=Vector3.UnitX;

			if(mIMEntities.Count <= 0)
			{
				return;
			}

			int	choice	=mRand.Next(0, mIMEntities.Count);

			ZoneEntity	ze	=mIMEntities[choice];

			ze.GetOrigin(out pos);
			ze.GetDirectionFromAngles("angles", out lookDir);
		}


		public void GetIntermissionDataNearestTo(Vector3 nearPos,
			out Vector3 pos, out Vector3 lookDir)
		{
			//defaults
			pos		=Vector3.Zero;
			lookDir	=Vector3.UnitX;

			if(mIMEntities.Count <= 0)
			{
				return;
			}

			float		bestDist	=float.MaxValue;
			ZoneEntity	nearest		=null;
			foreach(ZoneEntity ze in mIMEntities)
			{
				Vector3	entPos;
				ze.GetOrigin(out entPos);

				float	dist	=Vector3.Distance(entPos, nearPos);
				if(dist < bestDist)
				{
					nearest		=ze;
					bestDist	=dist;
				}
			}

			nearest.GetOrigin(out pos);
			nearest.GetDirectionFromAngles("angles", out lookDir);
		}
	}
}
