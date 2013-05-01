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
		public class FuncEventArgs : EventArgs
		{
			public TriggerContextEventArgs	mTCEA;
			public bool						mbTriggerState;

			public FuncEventArgs(TriggerContextEventArgs tcea, bool bTrigState) : base()
			{
				mTCEA			=tcea;
				mbTriggerState	=bTrigState;
			}
		}

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
			//unwire from old
			if(mZone != null)
			{
				mZone.eTriggerHit			-=OnTriggerHit;
				mZone.eTriggerOutOfRange	-=OnTriggerLeaving;
			}

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


		public void CheckMobile(object triggerer, BoundingBox playerBox, Vector3 startPos, Vector3 endPos, int msDelta)
		{
			if(mZone == null)
			{
				return;
			}

			mZone.BoxTriggerCheck(triggerer, playerBox, startPos, endPos, msDelta);
		}


		void OnTriggerLeaving(object sender, EventArgs ea)
		{
			ZoneEntity	ze	=sender as ZoneEntity;
			if(ze == null)
			{
				return;
			}

			TriggerContextEventArgs	tcea	=ea as TriggerContextEventArgs;
			if(tcea == null)
			{
				return;
			}

			string	targ	=ze.GetTarget();
			if(targ == "")
			{
				return;
			}

			//leaving doesn't necessarily mean the trigger
			//is turned off, might be someone else still in
			if(ze.mData["triggered"] == "true")
			{
				//only really interested if the trigger is off
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
				else if(className.StartsWith("misc_"))
				{
					Misc.SafeInvoke(eMisc, zet, tcea);
				}
				else if(className.StartsWith("func_"))
				{
					FuncEventArgs	fea	=new FuncEventArgs(tcea, ze.GetValue("triggered") == "true");
					Misc.SafeInvoke(eFunc, zet, fea);
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

			TriggerContextEventArgs	tcea	=ea as TriggerContextEventArgs;
			if(tcea == null)
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
					TriggerTeleport(zet, tcea);
				}
				else if(className == "misc_change_level")
				{
					string	level	=zet.GetValue("nextlevel");

					Misc.SafeInvoke(eChangeMap, level);
				}
				else if(className.StartsWith("ammo_") ||
					className.StartsWith("weapon_") ||
					className.StartsWith("item_") ||
					className.StartsWith("key_"))
				{
					Misc.SafeInvoke(ePickUp, zet, tcea);
				}
				else if(className.StartsWith("misc_"))
				{
					Misc.SafeInvoke(eMisc, zet, tcea);
				}
				else if(className.StartsWith("func_"))
				{
					FuncEventArgs	fea	=new FuncEventArgs(tcea, ze.GetValue("triggered") == "true");
					Misc.SafeInvoke(eFunc, zet, fea);
				}
			}

			//invoke a message as well if need be
			if(ze.mData.ContainsKey("message"))
			{
				Misc.SafeInvoke(eMessage, ze.mData["message"], tcea);
			}
		}


		void TriggerLight(ZoneEntity zet)
		{
			int	switchNum;
			if(!zet.GetInt("LightSwitchNum", out switchNum))
			{
				return;
			}

			mZone.SwitchCachedLight(zet);

			//switch!
			mSwitchLight(switchNum, zet.ToggleEntityActivated());
		}


		void TriggerTeleport(ZoneEntity ent, TriggerContextEventArgs tcea)
		{
			Vector3	dst;
			if(!ent.GetOrigin(out dst))
			{
				return;
			}

			Nullable<Vector3>	dest	=new Nullable<Vector3>(dst);

			Misc.SafeInvoke(eTeleport, dest, tcea);
		}
	}
}
