using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX;
using SharpDX.X3DAudio;
using UtilityLib;
using AudioLib;
using BSPZone;


namespace EntityLib
{
	public class BModelStages
	{
		internal int	mCurStage;
		internal bool	mbForward;
		internal bool	mbActive;
		internal bool	mbBlocked;	//smashed into a solid object
		internal bool	mbLooping;

		internal List<BModelMoveStage>	mStages	=new List<BModelMoveStage>();


		public BModelStages(bool bLoop, BModelMoveStage stage)
		{
			mbLooping	=bLoop;
			mStages.Add(stage);
		}


		public BModelStages(bool bLoop, List<BModelMoveStage> stages)
		{
			mbLooping	=bLoop;
			mStages.AddRange(stages);
		}


		public BModelStages(bool bLoop, BModelMoveStage	[]stages)
		{
			mbLooping	=bLoop;
			mStages.AddRange(stages);
		}


		internal void FireCurrent()
		{
			mStages[mCurStage].Fire(mbForward);
		}
	}


	public class BModelMoveStage
	{
		internal	Audio	mAudio;	//ref to audio module

		internal int		mModelIndex;
		internal Vector3	mOrigin;
		internal bool		mbForward;
		internal Vector3	mMoveAxis;
		internal float		mMoveAmount;
		internal Vector3	mRotationTarget, mRotationRate;	//xyz
		internal bool		mbRotateToTarget;
		internal float		mStageInterval;
		internal float		mEaseIn, mEaseOut;

		internal string		mSoundForward, mSoundBackward;
		internal Emitter	mEmitter;

		internal Mover3	mMover		=new Mover3();
		internal Mover3	mRotator	=new Mover3();


		public BModelMoveStage(int modelIndex, Vector3 org,
							   float stageInterval,
							   float easeIn, float easeOut)
		{
			mModelIndex		=modelIndex;
			mOrigin			=org;
			mStageInterval	=stageInterval;
			mEaseIn			=easeIn;
			mEaseOut		=easeOut;
		}


		public Vector3 GetOrigin()
		{
			return	mOrigin;
		}


		public void SetMovement(float moveAmount, Vector3 axis, float interval)
		{
			mMoveAmount		=moveAmount;
			mMoveAxis		=axis;
			mStageInterval	=interval;
		}


		internal void Fire(bool bForward)
		{
			if(mbForward == bForward)
			{
				return;
			}
			mbForward	=bForward;

			if(mbForward)
			{
				if(mSoundForward != null)
				{
					mAudio.PlayAtLocation(mSoundForward, 1f, false, mEmitter);
				}

				if(mMover.Done())
				{
					mMover.SetUpMove(mOrigin, mOrigin + mMoveAxis * mMoveAmount,
						mStageInterval, mEaseIn, mEaseOut);
				}
				else
				{
					mMover.SetUpMove(mMover.GetPos(), mOrigin + mMoveAxis * mMoveAmount,
						mStageInterval, mEaseIn, mEaseOut);
				}

				if(mbRotateToTarget)
				{
					if(mRotator.Done())
					{
						mRotator.SetUpMove(Vector3.Zero, mRotationTarget,
							mStageInterval, mEaseIn, mEaseOut);
					}
					else
					{
						mRotator.SetUpMove(mRotator.GetPos(), mRotationTarget,
							mStageInterval, mEaseIn, mEaseOut);
					}
				}
			}
			else
			{
				if(mSoundBackward != null)
				{
					mAudio.PlayAtLocation(mSoundBackward, 1f, false, mEmitter);
				}
				if(mMover.Done())
				{
					mMover.SetUpMove(mOrigin + mMoveAxis * mMoveAmount,
						mOrigin, mStageInterval, mEaseIn, mEaseOut);
				}
				else
				{
					mMover.SetUpMove(mMover.GetPos(), mOrigin,
						mStageInterval, mEaseIn, mEaseOut);
				}

				if(mbRotateToTarget)
				{
					if(mRotator.Done())
					{
						mRotator.SetUpMove(mRotationTarget, Vector3.Zero,
							mStageInterval, mEaseIn, mEaseOut);
					}
					else
					{
						mRotator.SetUpMove(mRotationTarget, mRotator.GetPos(),
							mStageInterval, mEaseIn, mEaseOut);
					}
				}
			}
		}


		internal bool Update(float secDelta, BSPZone.Zone z, out bool bBlocked)
		{
			Debug.Assert(secDelta > 0f);	//zero deltatimes are not good for this stuff

			bBlocked	=false;
			if(mMover.Done())
			{
				if(mbRotateToTarget && mRotator.Done())
				{
					return	true;
				}
				if(!mbRotateToTarget && mRotationRate == Vector3.Zero)
				{
					return	true;
				}
			}

			if(!mMover.Done())
			{
				mMover.Update(secDelta);

				//do the move
				if(!z.MoveModelTo(mModelIndex, mMover.GetPos()))
				{
					bBlocked	=true;
				}
			}

			//update rotation if any
			if(mbRotateToTarget)
			{
				if(!mRotator.Done())
				{
					Vector3	rotPreUpdate	=mRotator.GetPos();

					mRotator.Update(secDelta);

					Vector3	rotPostUpdate	=mRotator.GetPos();

					//halt after an axis is blocked
					if(z.RotateModelX(mModelIndex,
						rotPostUpdate.X - rotPreUpdate.X))
					{
						bBlocked	=true;
					}
					else
					{
						if(z.RotateModelY(mModelIndex, rotPostUpdate.Y - rotPreUpdate.Y))
						{
							bBlocked	=true;
						}
						else
						{
							if(z.RotateModelZ(mModelIndex, rotPostUpdate.Z - rotPreUpdate.Z))
							{
								bBlocked	=true;
							}
						}
					}
				}
			}
			else
			{
				Vector3	rotAmount	=mRotationRate * secDelta;

				if(rotAmount != Vector3.Zero)
				{
					if(z.RotateModelX(mModelIndex, rotAmount.X))
					{
						bBlocked	=true;
					}
					else
					{
						if(z.RotateModelY(mModelIndex, rotAmount.Y))
						{
							bBlocked	=true;
						}
						else
						{
							if(z.RotateModelZ(mModelIndex, rotAmount.Z))
							{
								bBlocked	=true;
							}
						}
					}
				}
			}

			if(mEmitter != null)
			{
				mEmitter.Position	=mMover.GetPos();
			}

			return	false;
		}
	}
}
