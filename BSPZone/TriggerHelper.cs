using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using UtilityLib;


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
		public event EventHandler	eMisc;
		public event EventHandler	eFunc;


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
				string	className	=zet.GetValue("classname");

				if(className.StartsWith("light") || className.StartsWith("_light"))
				{
					TriggerLight(zet);
				}
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

				if(className.StartsWith("light") || className.StartsWith("_light"))
				{
					TriggerLight(zet);
				}
				else if(className.Contains("teleport_destination")
					|| className.Contains("misc_teleporter_dest"))
				{
					TriggerTeleport(zet);
				}
				else if(className == "target_changelevel")
				{
					string	level	=zet.GetValue("map");

					Misc.SafeInvoke(eChangeMap, level);
				}
				else if(className.StartsWith("ammo_") ||
					className.StartsWith("weapon_") ||
					className.StartsWith("item_") ||
					className.StartsWith("key_"))
				{
					Misc.SafeInvoke(ePickUp, zet);
				}
				else if(className.StartsWith("misc_"))
				{
					Misc.SafeInvoke(eMisc, zet);
				}
				else if(className.StartsWith("func_"))
				{
					Misc.SafeInvoke(eFunc, zet);
				}
			}

			//invoke a message as well if need be
			if(ze.mData.ContainsKey("message"))
			{
				Misc.SafeInvoke(eMessage, ze.mData["message"]);
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
				if(Misc.bFlagSet(spawnFlags, 1))
				{
					bOn	=false;

					//flip bit in entity data
					Misc.ClearFlag(ref spawnFlags, 1);
					zet.SetInt("spawnflags", spawnFlags);					
				}
				else
				{
					spawnFlags	|=1;
					zet.SetInt("spawnflags", spawnFlags);
				}
			}
			else
			{
				zet.SetInt("spawnflags", 1);
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

			Misc.SafeInvoke(eTeleport, dest);
		}
	}
}
