using System;
using System.Collections.Generic;
using SharpDX;
using UtilityLib;


namespace EntityLib;

public class PickUp : Component
{
	public enum State
	{
		Static, Spinning, Bobbing, WaitingRespawn
	}

	StaticMeshComp	mSMC;

	public readonly	int		mSpinningPart;	//part index to spin, -1 if all

	internal bool		mbActive, mbSpinning;
	
	float	mSpinYaw;		//yaw for spinning pickups

	const float	YawPerMS	=0.007f;
	const float	Hover		=20f;


	public PickUp(Entity owner, int spinPart) : base(owner)
	{
		mbActive		=true;
		mbSpinning		=true;	//default
		mSpinningPart	=spinPart;

		mSMC	=mOwner.GetComponent(typeof(StaticMeshComp)) as StaticMeshComp;
	}


	public float	GetYaw()
	{
		return	mSpinYaw;
	}


	public StaticMeshComp	GetSMC()
	{
		return	mSMC;
	}


	public override void Update(UpdateTimer time)
	{
		if(!mbActive)
		{
			return;
		}

		if(mSMC == null)
		{
			return;
		}
		
		if(mbSpinning)
		{
			mSpinYaw	+=YawPerMS * time.GetUpdateDeltaMilliSeconds();
			Mathery.WrapAngleDegrees(ref mSpinYaw);
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