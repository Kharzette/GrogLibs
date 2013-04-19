using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using UtilityLib;


namespace BSPZone
{
	public class LiftHelper
	{
		Zone	mZone;

		class Lift
		{
			internal int		mModelIndex;
			internal Vector3	mOrigin;
			internal bool		mbOpening;

			internal SoundEffectInstance	mSoundInstance;

			internal AudioEmitter	mEmitter;

			internal Mover3	mMover	=new Mover3();

			const float	TravelTime	=3f;


			internal void Fire()
			{
				mbOpening	=!mbOpening;

				if(mSoundInstance != null)
				{
					mSoundInstance.Play();
				}

				if(mbOpening)
				{
					if(mMover.Done())
					{
						mMover.SetUpMove(mOrigin, mOrigin - Vector3.UnitY * 272f, TravelTime, 0.2f, 0.2f);
					}
					else
					{
						mMover.SetUpMove(mMover.GetPos(), mOrigin - Vector3.UnitY * 272f, TravelTime, 0.2f, 0.2f);
					}
				}
				else
				{
					if(mMover.Done())
					{
						mMover.SetUpMove(mOrigin - Vector3.UnitY * 272f, mOrigin, TravelTime, 0.2f, 0.2f);
					}
					else
					{
						mMover.SetUpMove(mMover.GetPos(), mOrigin, TravelTime, 0.2f, 0.2f);
					}
				}
			}


			internal void Update(int msDelta, Zone z, AudioListener lis)
			{
				if(mMover.Done())
				{
					return;
				}

				mMover.Update(msDelta);

				z.MoveModelTo(mModelIndex, mMover.GetPos());

				mEmitter.Position	=mMover.GetPos() * Audio.InchWorldScale;

				if(mSoundInstance != null)
				{
					mSoundInstance.Apply3D(lis, mEmitter);
				}
			}
		}

		Dictionary<int, Lift>	mLifts	=new Dictionary<int, Lift>();


		public LiftHelper() {}

		public void Initialize(Zone zone, Audio aud, AudioListener lis)
		{
			mZone	=zone;

			List<ZoneEntity>	Lifts	=zone.GetEntitiesStartsWith("func_plat");
			foreach(ZoneEntity ze in Lifts)
			{
				int		model;
				Vector3	org;

				ze.GetInt("Model", out model);
				ze.GetVectorNoConversion("ModelOrigin", out org);

				Lift	d			=new Lift();
				d.mModelIndex		=model;
				d.mOrigin			=org;
				d.mEmitter			=new AudioEmitter();
				d.mSoundInstance	=aud.GetInstance("DoorMove", false);

				d.mEmitter.Position	=org;

				if(d.mSoundInstance != null)
				{
					d.mSoundInstance.Apply3D(lis, d.mEmitter);
				}

				mLifts.Add(model, d);
			}
		}


		public void Update(int msDelta, AudioListener lis)
		{
			foreach(KeyValuePair<int, Lift> Lift in mLifts)
			{
				Lift.Value.Update(msDelta, mZone, lis);
			}
		}


		public void FireLift(int modelIndex)
		{
			if(!mLifts.ContainsKey(modelIndex))
			{
				return;
			}

			mLifts[modelIndex].Fire();
		}
	}
}
