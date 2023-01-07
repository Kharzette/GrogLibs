using System;
using UtilityLib;
using BSPZone;

namespace EntityLib;

public class BModelMover : Component
{
	//the actual entity might have more states
	//but for model moving this is all we need
	public enum States
	{
		Moving	=0,
		Forward	=1,
		Blocked	=2
	}

	int	mModelIdx;

	BModelStages	mModelStages;
	Zone			mZone;


	public BModelMover(int idx, BModelStages bms, Zone z,
						Entity owner) : base(owner)
	{
		mModelIdx		=idx;
		mModelStages	=bms;
		mZone			=z;
	}


	public int GetModelIndex()
	{
		return	mModelIdx;
	}


	public override void Update(UpdateTimer time)
	{
		BModelStages	ms	=mModelStages;
		if(!ms.mbActive)
		{
			return;
		}

		BModelMoveStage	mms	=ms.mStages[ms.mCurStage];

		float	secDelta	=time.GetUpdateDeltaSeconds();

		bool	bBlocked, bDone	=mms.Update(secDelta, mZone, out bBlocked);//, lis);

		if(bDone)
		{
			ms.mbBlocked	=false;	//release blocked state if set

			if(ms.mbForward)
			{
				if(ms.mStages.Count > (ms.mCurStage + 1))
				{
					ms.mCurStage++;
					mms	=ms.mStages[ms.mCurStage];

					mms.mbForward	=!mms.mbForward;
					mms.Fire(ms.mbForward);
				}
				else if(ms.mbLooping)
				{
					ms.mCurStage	=0;
					mms	=ms.mStages[ms.mCurStage];

					mms.mbForward	=!mms.mbForward;
					mms.Fire(ms.mbForward);
				}
				else
				{
					ms.mbActive	=false;
				}
			}
			else
			{
				if((ms.mCurStage - 1) >= 0)
				{
					ms.mCurStage--;
					mms	=ms.mStages[ms.mCurStage];

					mms.mbForward	=!mms.mbForward;
					mms.Fire(ms.mbForward);
				}
				else if(ms.mbLooping)
				{
					ms.mCurStage	=ms.mStages.Count - 1;
					mms	=ms.mStages[ms.mCurStage];

					mms.mbForward	=!mms.mbForward;
					mms.Fire(ms.mbForward);
				}
				else
				{
					ms.mbActive	=false;
				}
			}
		}
	}


	public override void StateChange(Enum state, UInt32 value)
	{
		if(state.Equals(States.Moving))
		{
			if(value == 1)
			{
				if(mModelStages.mbActive)
				{
					//already active
				}
				else
				{
					mModelStages.mbActive	=true;
					mModelStages.FireCurrent();
				}
			}
			else
			{
				//this will rarely be used as it can leave
				//models stuck halfway
				mModelStages.mbActive	=false;
			}
		}
		else if(state.Equals(States.Forward))
		{
			mModelStages.mbForward	=(value != 0);
		}
		else if(state.Equals(States.Blocked))
		{
			if(mModelStages.mbBlocked)
			{
				//already set
				return;
			}

			mModelStages.mbBlocked	=true;

			//reverse direction
			mModelStages.mbForward	=!mModelStages.mbForward;

			mModelStages.FireCurrent();
		}			
	}
}