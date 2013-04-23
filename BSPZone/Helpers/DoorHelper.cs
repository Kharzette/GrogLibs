using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using UtilityLib;


namespace BSPZone
{
	public class DoorHelper
	{
		Zone	mZone;

		class Door
		{
			internal int		mModelIndex;
			internal Vector3	mOrigin;
			internal bool		mbOpening;
			internal Vector3	mMoveAxis;
			internal float		mMoveAmount;
			internal float		mMoveInterval;
			internal float		mEaseIn, mEaseOut;

			internal SoundEffectInstance	mSoundInstance;

			internal AudioEmitter	mEmitter;

			internal Mover3	mMover	=new Mover3();

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
						mMover.SetUpMove(mOrigin, mOrigin + mMoveAxis * mMoveAmount,
							mMoveInterval, mEaseIn, mEaseOut);
					}
					else
					{
						mMover.SetUpMove(mMover.GetPos(), mOrigin + mMoveAxis * mMoveAmount,
							mMoveInterval, mEaseIn, mEaseOut);
					}
				}
				else
				{
					if(mMover.Done())
					{
						mMover.SetUpMove(mOrigin + mMoveAxis * mMoveAmount,
							mOrigin, mMoveInterval, mEaseIn, mEaseOut);
					}
					else
					{
						mMover.SetUpMove(mMover.GetPos(), mOrigin,
							mMoveInterval, mEaseIn, mEaseOut);
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

		Dictionary<int, Door>	mDoors	=new Dictionary<int, Door>();


		public DoorHelper(){}


		public void Initialize(Zone zone, Audio aud, AudioListener lis)
		{
			mZone	=zone;

			List<ZoneEntity>	doors	=zone.GetEntitiesStartsWith("func_door");
			foreach(ZoneEntity ze in doors)
			{
				int		model;
				Vector3	org;

				ze.GetInt("Model", out model);
				ze.GetVectorNoConversion("ModelOrigin", out org);

				Door	d			=new Door();
				d.mModelIndex		=model;
				d.mOrigin			=org;
				d.mEmitter			=new AudioEmitter();
				d.mSoundInstance	=aud.GetInstance("DoorMove", false);

				d.mEmitter.Position	=org;

				if(d.mSoundInstance != null)
				{
					d.mSoundInstance.Apply3D(lis, d.mEmitter);
				}

				ze.GetFloat("move_amount", out d.mMoveAmount);
				ze.GetFloat("move_interval", out d.mMoveInterval);
				ze.GetFloat("ease_in", out d.mEaseIn);
				ze.GetFloat("ease_out", out d.mEaseOut);
				ze.GetDirectionFromAngles("move_angles", out d.mMoveAxis);

				mDoors.Add(model, d);
			}
		}


		public void Update(int msDelta, AudioListener lis)
		{
			foreach(KeyValuePair<int, Door> door in mDoors)
			{
				door.Value.Update(msDelta, mZone, lis);
			}
		}


		public void FireDoor(int modelIndex)
		{
			if(!mDoors.ContainsKey(modelIndex))
			{
				return;
			}

			mDoors[modelIndex].Fire();
		}
	}
}
