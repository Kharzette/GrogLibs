using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using UtilityLib;


namespace BSPZone
{
	//handles placement and interaction of static items in a zone
	public class StaticHelper
	{
		public delegate void DrawStatic(Matrix local, ZoneEntity ze, Vector3 pos);

		class PickUp
		{
			internal Vector3	mPosition;
			internal ZoneEntity	mEntity;
			internal Matrix		mTransform;
			internal float		mYaw;
			internal bool		mbPickUp;

			internal void UpdateTransform()
			{
				mTransform	=Matrix.CreateRotationY(MathHelper.ToRadians(mYaw));
				mTransform	*=Matrix.CreateTranslation(mPosition);
			}
		}

		Zone	mZone;

		//data
		List<ZoneEntity>	mStaticEntities	=new List<ZoneEntity>();
		List<PickUp>		mPickUps		=new List<PickUp>();

		//pickups about to be nuked
		List<PickUp>	mNuking	=new List<PickUp>();

		public event EventHandler	ePickUp;

		const float	PickUpDistance	=32.0f;
		const float	YawPerMS		=0.05f;


		public void Initialize(Zone zone)
		{
			mZone	=zone;

			mStaticEntities.Clear();
			mPickUps.Clear();

			//grab out typical static entities
			List<ZoneEntity>	ents	=mZone.GetEntitiesStartsWith("weapon_");
			ents.AddRange(mZone.GetEntitiesStartsWith("ammo_"));
			ents.AddRange(mZone.GetEntitiesStartsWith("key_"));
			ents.AddRange(mZone.GetEntitiesStartsWith("item_"));
			ents.AddRange(mZone.GetEntitiesStartsWith("misc_skeleton"));	//LD Hack!

			foreach(ZoneEntity ze in ents)
			{
				mStaticEntities.Add(ze);

				Vector3	pos;
				ze.GetOrigin(out pos);

				PickUp	pu		=new PickUp();
				pu.mEntity		=ze;
				pu.mPosition	=pos;
				if(ze.GetValue("classname") != "misc_skeleton")
				{
					pu.mbPickUp	=true;
				}

				mPickUps.Add(pu);
			}
		}


		public void Update(Vector3 playerPos, int msDelta)
		{
			foreach(PickUp pu in mPickUps)
			{
				if(!pu.mbPickUp)
				{
					continue;
				}
				float	dist	=Vector3.Distance(pu.mPosition, playerPos);

				//close enough to grab?
				if(dist < PickUpDistance)
				{
					mNuking.Add(pu);
					Misc.SafeInvoke(ePickUp, pu.mEntity);
				}
			}

			foreach(PickUp pu in mNuking)
			{
				mPickUps.Remove(pu);
			}
			mNuking.Clear();

			//update transforms
			foreach(PickUp pu in mPickUps)
			{
				if(pu.mbPickUp)
				{
					pu.mYaw	+=(YawPerMS * msDelta);
					Mathery.WrapAngleDegrees(ref pu.mYaw);
				}

				pu.UpdateTransform();
			}
		}


		public void Draw(DrawStatic ds)
		{
			foreach(PickUp pu in mPickUps)
			{
				ds(pu.mTransform, pu.mEntity, pu.mPosition);
			}
		}
	}
}
