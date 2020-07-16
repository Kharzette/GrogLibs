using System;
using SharpDX;
using UtilityLib;


namespace EntityLib
{
	public class PickUp : Component
	{
		public enum State
		{
			Static, Spinning, Bobbing, WaitingRespawn
		}

		StaticMeshComp	mSMC;

		public readonly Vector3		mPosition;

		internal float		mYaw, mPitch, mRoll;
		internal bool		mbActive, mbSpinning;

		const float	YawPerMS	=0.007f;
		const float	Hover		=20f;


		public PickUp(Vector3 pos, Entity owner) : base(owner)
		{
			mPosition	=pos;
			mbActive	=true;
			mbSpinning	=true;	//default
		}


		public override void Update(UpdateTimer time)
		{
			if(!mbActive)
			{
				return;
			}

			if(mbSpinning)
			{
				mYaw	+=YawPerMS * time.GetUpdateDeltaMilliSeconds();
				Mathery.WrapAngleDegrees(ref mYaw);
			}

			if(mSMC == null)
			{
				//lazy init
				mSMC	=mOwner.GetComponent(typeof(StaticMeshComp)) as StaticMeshComp;
			}
			if(mSMC == null)
			{
				return;
			}

			//need a 90 degree bump to get the pitch started properly
			mSMC.mMat	=Matrix.RotationY(MathUtil.PiOverTwo);
			mSMC.mMat	*=Matrix.RotationYawPitchRoll(mYaw, mPitch, mRoll);

			if(mbSpinning)
			{
				mSMC.mMat	*=Matrix.Translation(mPosition + Vector3.UnitY * Hover);
			}
			else
			{
				mSMC.mMat	*=Matrix.Translation(mPosition);
			}
		}


		public override void StateChange(Enum state, UInt32 value)
		{
			if(state.Equals(State.WaitingRespawn))
			{
				mSMC.StateChange(StaticMeshComp.State.Visible, 0);
				mbActive	=false;
			}
			else if(state.Equals(State.Spinning))
			{
				mbSpinning	=(value != 0);
			}
		}
	}
}