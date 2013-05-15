using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using UtilityLib;


namespace BSPZone
{
	public class IntermissionHelper
	{
		Zone	mZone;

		//data
		List<ZoneEntity>	mIMEntities	=new List<ZoneEntity>();


		public void Initlialize(Zone zone)
		{
			mZone	=zone;

			mIMEntities	=mZone.GetEntitiesStartsWith("info_player_intermission");
		}


		public void GetRandomIntermissionData(Random mRand, out Vector3 pos,
			out int pitch, out int yaw, out int roll)
		{
			//defaults
			pos		=Vector3.Zero;
			yaw		=0;
			pitch	=0;
			roll	=0;

			if(mIMEntities.Count <= 0)
			{
				return;
			}

			int	choice	=mRand.Next(0, mIMEntities.Count);

			ZoneEntity	ze	=mIMEntities[choice];

			ze.GetOrigin(out pos);
			Vector3	orient;
			if(ze.GetVectorNoConversion("angles", out orient))
			{
				//coordinate system goblinry
				yaw		=(int)-orient.Y - 90;
				pitch	=(int)orient.X;
				roll	=(int)orient.Z;
			}
		}


		public void GetIntermissionDataNearestTo(Vector3 nearPos, out Vector3 pos,
			out int pitch, out int yaw, out int roll)
		{
			//defaults
			pos		=Vector3.Zero;
			yaw		=0;
			pitch	=0;
			roll	=0;

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
			Vector3	orient;
			if(nearest.GetVectorNoConversion("angles", out orient))
			{
				//coordinate system goblinry
				yaw		=(int)-orient.Y - 90;
				pitch	=(int)orient.X;
				roll	=(int)orient.Z;
			}
		}
	}
}
