using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPZone
{
	//handles switchable lights, pickups, level changes etc
	public class TriggerHelper
	{
		//delegate used by indoormesh
		public delegate void SwitchLight(int light, bool bOn);

		Zone		mZone;
		SwitchLight	mSwitchLight;

		//public events
		public event EventHandler	eTeleport;
		public event EventHandler	ePickUp;
		public event EventHandler	eChangeMap;
		public event EventHandler	eMessage;
		public event EventHandler	eRecipe;
		public event EventHandler	eStatChange;
		public event EventHandler	eInRangeOf;
		public event EventHandler	eOutOfRangeOf;


		public void Initialize(Zone zone, SwitchLight sl)
		{
			mZone			=zone;
			mSwitchLight	=sl;

			mZone.eTriggerHit			+=OnTriggerHit;
			mZone.eTriggerOutOfRange	+=OnTriggerLeaving;

			List<ZoneEntity>	switchedOn	=mZone.GetSwitchedOnLights();
			foreach(ZoneEntity ze in switchedOn)
			{
				int	switchNum;
				if(ze.GetInt("LightSwitchNum", out switchNum))
				{
					sl(switchNum, true);
				}
			}
		}


		public void Clear()
		{
			mZone.eTriggerHit	-=OnTriggerHit;

			mZone			=null;
			mSwitchLight	=null;
		}


		public void CheckPlayer(BoundingBox playerBox, Vector3 startPos, Vector3 endPos, int msDelta)
		{
			if(mZone == null)
			{
				return;
			}

			mZone.BoxTriggerCheck(playerBox, startPos, endPos, msDelta);
		}


		void OnTriggerLeaving(object sender, EventArgs ea)
		{
			ZoneEntity	ze	=sender as ZoneEntity;
			if(ze == null)
			{
				return;
			}

			string	targ	=ze.GetTarget();
			if(targ == "")
			{
				return;
			}

			List<ZoneEntity>	targs	=mZone.GetEntitiesByTargetName(targ);
			foreach(ZoneEntity zet in targs)
			{
				UtilityLib.Misc.SafeInvoke(eOutOfRangeOf, zet);
			}
		}


		void OnTriggerHit(object sender, EventArgs ea)
		{
			ZoneEntity	ze	=sender as ZoneEntity;
			if(ze == null)
			{
				return;
			}

			string	targ	=ze.GetTarget();
			if(targ == "")
			{
				return;
			}

			List<ZoneEntity>	targs	=mZone.GetEntitiesByTargetName(targ);
			foreach(ZoneEntity zet in targs)
			{
				string	className	=zet.GetValue("classname");

				if(ze.GetValue("classname") == "trigger_gravity")
				{
					UtilityLib.Misc.SafeInvoke(eInRangeOf, zet);
				}
				else if(className.StartsWith("light") || className.StartsWith("_light"))
				{
					TriggerLight(zet);
				}
				else if(className == "info_null")
				{
					string	statChange	=zet.GetValue("Stat");

					statChange	+=" " + zet.GetValue("StatDelta");

					UtilityLib.Misc.SafeInvoke(eStatChange, statChange);
				}
				else if(className.Contains("teleport_destination")
					|| className.Contains("misc_teleporter_dest"))
				{
					TriggerTeleport(zet);
				}
				else if(className == "target_changelevel")
				{
					string	level	=zet.GetValue("map");

					UtilityLib.Misc.SafeInvoke(eChangeMap, level);
				}
				else if(className.StartsWith("ammo_") ||
					className.StartsWith("weapon_") ||
					className.StartsWith("item_") ||
					className.StartsWith("key_"))
				{
					UtilityLib.Misc.SafeInvoke(ePickUp, zet);
				}
				else if(className.StartsWith("misc_"))
				{
					UtilityLib.Misc.SafeInvoke(eRecipe, zet);
				}
			}

			//invoke a message as well if need be
			if(ze.mData.ContainsKey("message"))
			{
				UtilityLib.Misc.SafeInvoke(eMessage, ze.mData["message"]);
			}
		}


		void TriggerLight(ZoneEntity zet)
		{
			int	switchNum;
			if(!zet.GetInt("LightSwitchNum", out switchNum))
			{
				return;
			}

			//see if already on
			bool	bOn	=true;

			int	spawnFlags;
			if(zet.GetInt("spawnflags", out spawnFlags))
			{
				if(UtilityLib.Misc.bFlagSet(spawnFlags, 1))
				{
					bOn	=false;
				}
			}

			//switch!
			bOn	=!bOn;
			mSwitchLight(switchNum, bOn);
		}


		void TriggerTeleport(ZoneEntity ent)
		{
			Vector3	dst;
			if(!ent.GetOrigin(out dst))
			{
				return;
			}

			Nullable<Vector3>	dest	=new Nullable<Vector3>(dst);

			UtilityLib.Misc.SafeInvoke(eTeleport, dest);
		}
	}
}
