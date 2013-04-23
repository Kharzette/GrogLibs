using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using UtilityLib;


namespace BSPZone
{
	public class BasicModelHelper
	{
		Zone	mZone;

		class SingleStageModel
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
		
		class DoubleStageModel
		{
			internal int		mModelIndex;
			internal Vector3	mOrigin;
			internal bool		mbOpening, mbStageTwo;
			internal Vector3	mMoveAxis1, mMoveAxis2;
			internal float		mMoveAmount1, mMoveAmount2;
			internal float		mMoveInterval1, mMoveInterval2;
			internal float		mEaseIn1, mEaseOut1;
			internal float		mEaseIn2, mEaseOut2;

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
					if(mbStageTwo)
					{
						Vector3	stageOneEnd	=mOrigin + mMoveAxis1 * mMoveAmount1;
						if(mMover.Done())
						{
							mMover.SetUpMove(stageOneEnd,
								stageOneEnd + mMoveAxis2 * mMoveAmount2,
								mMoveInterval2, mEaseIn2, mEaseOut2);
						}
						else
						{
							mMover.SetUpMove(mMover.GetPos(),
								stageOneEnd + mMoveAxis2 * mMoveAmount2,
								mMoveInterval2, mEaseIn2, mEaseOut2);
						}
					}
					else
					{
						if(mMover.Done())
						{
							mMover.SetUpMove(mOrigin, mOrigin + mMoveAxis1 * mMoveAmount1,
								mMoveInterval1, mEaseIn1, mEaseOut1);
						}
						else
						{
							mMover.SetUpMove(mMover.GetPos(), mOrigin + mMoveAxis1 * mMoveAmount1,
								mMoveInterval1, mEaseIn1, mEaseOut1);
						}
					}
				}
				else
				{
					if(mbStageTwo)
					{
						Vector3	stageOneEnd	=mOrigin + mMoveAxis1 * mMoveAmount1;
						if(mMover.Done())
						{
							mMover.SetUpMove(stageOneEnd + mMoveAxis2 * mMoveAmount2,
								stageOneEnd, mMoveInterval2, mEaseIn2, mEaseOut2);
						}
						else
						{
							mMover.SetUpMove(mMover.GetPos(), stageOneEnd,
								mMoveInterval2, mEaseIn2, mEaseOut2);
						}
					}
					else
					{
						if(mMover.Done())
						{
							mMover.SetUpMove(mOrigin + mMoveAxis1 * mMoveAmount1,
								mOrigin, mMoveInterval1, mEaseIn1, mEaseOut1);
						}
						else
						{
							mMover.SetUpMove(mMover.GetPos(), mOrigin,
								mMoveInterval1, mEaseIn1, mEaseOut1);
						}
					}
				}
			}


			internal void Update(int msDelta, Zone z, AudioListener lis)
			{
				if(mMover.Done())
				{
					if(mbOpening)
					{
						if(!mbStageTwo)
						{
							mbStageTwo			=true;
							Vector3	stageOneEnd	=mOrigin + mMoveAxis1 * mMoveAmount1;
							mMover.SetUpMove(stageOneEnd,
								stageOneEnd + mMoveAxis2 * mMoveAmount2,
								mMoveInterval2, mEaseIn2, mEaseOut2);
						}
					}
					else
					{
						if(mbStageTwo)
						{
							mbStageTwo			=false;
							Vector3	stageOneEnd	=mOrigin + mMoveAxis1 * mMoveAmount1;
							mMover.SetUpMove(stageOneEnd, mOrigin,
								mMoveInterval2, mEaseIn2, mEaseOut2);
						}
					}
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

		Dictionary<int, SingleStageModel>	mSSMs	=new Dictionary<int, SingleStageModel>();
		Dictionary<int, DoubleStageModel>	mDSMs	=new Dictionary<int, DoubleStageModel>();


		public BasicModelHelper(){}


		public void Initialize(Zone zone, Audio aud, AudioListener lis)
		{
			mZone	=zone;

			//grab doors and lifts
			List<ZoneEntity>	doors	=zone.GetEntitiesStartsWith("func_door");
			List<ZoneEntity>	Lifts	=zone.GetEntitiesStartsWith("func_plat");

			//dump them together
			doors.AddRange(Lifts);

			foreach(ZoneEntity ze in doors)
			{
				int		model;
				Vector3	org;

				ze.GetInt("Model", out model);
				ze.GetVectorNoConversion("ModelOrigin", out org);

				SingleStageModel	d			=new SingleStageModel();
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

				mSSMs.Add(model, d);
			}

			//grab secret doors for the 2 stage stuff
			List<ZoneEntity>	walls	=zone.GetEntitiesStartsWith("func_wall");
			foreach(ZoneEntity ze in walls)
			{
				int		model;
				Vector3	org;

				ze.GetInt("Model", out model);
				ze.GetVectorNoConversion("ModelOrigin", out org);

				DoubleStageModel	d			=new DoubleStageModel();
				d.mModelIndex		=model;
				d.mOrigin			=org;
				d.mEmitter			=new AudioEmitter();
				d.mSoundInstance	=aud.GetInstance("DoorMove", false);

				d.mEmitter.Position	=org;

				if(d.mSoundInstance != null)
				{
					d.mSoundInstance.Apply3D(lis, d.mEmitter);
				}

				ze.GetFloat("move_amount_1", out d.mMoveAmount1);
				ze.GetFloat("move_interval_1", out d.mMoveInterval1);
				ze.GetFloat("ease_in_1", out d.mEaseIn1);
				ze.GetFloat("ease_out_1", out d.mEaseOut1);
				ze.GetFloat("move_amount_2", out d.mMoveAmount2);
				ze.GetFloat("move_interval_2", out d.mMoveInterval2);
				ze.GetFloat("ease_in_2", out d.mEaseIn2);
				ze.GetFloat("ease_out_2", out d.mEaseOut2);
				ze.GetDirectionFromAngles("move_angles_1", out d.mMoveAxis1);
				ze.GetDirectionFromAngles("move_angles_2", out d.mMoveAxis2);

				mDSMs.Add(model, d);
			}
		}


		public void Update(int msDelta, AudioListener lis)
		{
			foreach(KeyValuePair<int, SingleStageModel> ssm in mSSMs)
			{
				ssm.Value.Update(msDelta, mZone, lis);
			}

			foreach(KeyValuePair<int, DoubleStageModel> dsm in mDSMs)
			{
				dsm.Value.Update(msDelta, mZone, lis);
			}
		}


		public void ToggleMovement(int modelIndex)
		{
			if(!mSSMs.ContainsKey(modelIndex))
			{
				if(!mDSMs.ContainsKey(modelIndex))
				{
					return;
				}
				mDSMs[modelIndex].Fire();
				return;
			}

			mSSMs[modelIndex].Fire();
		}
	}
}
