using System;
using SharpDX;
using UtilityLib;


namespace EntityLib
{
	public class PickUp : Component
	{
		enum State
		{
			Static, Spinning, Bobbing, WaitingRespawn
		}

		StaticMeshComp	mSMC;

		public readonly Vector3		mPosition;

		internal float		mYaw, mPitch, mRoll;

		const float	YawPerMS	=0.007f;
		const float	Hover		=20f;


		public PickUp(Vector3 pos, Entity owner) : base(owner)
		{
			mPosition	=pos;
			mPosition	+=Vector3.UnitY * Hover;
		}


		public override void Update(UpdateTimer time)
		{
			mYaw	+=YawPerMS * time.GetUpdateDeltaMilliSeconds();
			Mathery.WrapAngleDegrees(ref mYaw);

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
			mSMC.mMat	*=Matrix.Translation(mPosition);
		}


		public override void StateChange(Enum state, UInt32 value)
		{
		}
	}
}