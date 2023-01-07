using System;
using System.Collections.Generic;
using UtilityLib;
using BSPZone;
using SharpDX;


namespace EntityLib;

public class Trigger : Component
{
	Zone		mZone;
	BoundingBox	mBox;
	Int32		mModelNum;
	bool		mbTriggered;
	bool		mbTriggerOnce;
	bool		mbTriggerStandIn;
	double		mTimeSinceTriggered;
	double		mDelay;
	string		mTarget;

	List<TriggerAble>	mTATargets	=new List<TriggerAble>();

	List<object>	mTriggeringObjects	=new List<object>();


	public Trigger(Zone z, int modelNum, BoundingBox box,
					bool bOnce, bool bStandIn, string target,
					double delay, Entity owner) : base(owner)
	{
		mZone				=z;
		mModelNum			=modelNum;
		mBox				=box;
		mbTriggerOnce		=bOnce;
		mbTriggerStandIn	=bStandIn;
		mTarget				=target;
	}


	public override void Update(UpdateTimer time)
	{
	}


	public override void StateChange(Enum state, UInt32 value)
	{
	}


	//for debugging
	public void GetTriggerGeometry(List<Vector3> verts, List<Int32> inds)
	{
		Int32	curIndex	=0;
		if(mbTriggerOnce && mbTriggered)
		{
			return;
		}

		Vector3	[]corners	=mBox.GetCorners();

		foreach(Vector3 v in corners)
		{
			verts.Add(v);
		}

		//wireframe lines
		//front face
		inds.Add(curIndex);
		inds.Add(curIndex + 1);
		inds.Add(curIndex + 1);
		inds.Add(curIndex + 2);
		inds.Add(curIndex + 2);
		inds.Add(curIndex + 3);
		inds.Add(curIndex + 3);
		inds.Add(curIndex);

		//back face
		inds.Add(curIndex + 4);
		inds.Add(curIndex + 5);
		inds.Add(curIndex + 5);
		inds.Add(curIndex + 6);
		inds.Add(curIndex + 6);
		inds.Add(curIndex + 7);
		inds.Add(curIndex + 7);
		inds.Add(curIndex + 4);

		//connections for sides
		inds.Add(curIndex);
		inds.Add(curIndex + 4);
		inds.Add(curIndex + 1);
		inds.Add(curIndex + 5);
		inds.Add(curIndex + 2);
		inds.Add(curIndex + 6);
		inds.Add(curIndex + 3);
		inds.Add(curIndex + 7);

		curIndex	+=8;
	}


	public void BoxTriggerCheck(object triggerer, BoundingBox box,
		Vector3 start, Vector3 end, float msDelta)
	{
		//if a one shot and already tripped, skip
		if(mbTriggerOnce && mbTriggered)
		{
			return;
		}

		//if a stand in and the triggerer already in the list, skip
		if(mbTriggerStandIn &&
			(mbTriggered && mTriggeringObjects.Contains(triggerer)))
		{
			return;
		}

		if(mZone.TriggerTrace(start, end, box, mModelNum))
		{
			mTimeSinceTriggered	+=msDelta;
			if(mTimeSinceTriggered > mDelay)
			{
				mbTriggered					=true;
				mTimeSinceTriggered			=0;

				//track who or what triggered if a stand in
				if(mbTriggerStandIn)
				{
					mTriggeringObjects.Add(triggerer);
				}

				TriggerTarget(1);
			}
		}

		//check for expiring standins
		if(!mbTriggerStandIn)
		{
			return;
		}

		if(!mbTriggered)
		{
			return;
		}

		if(!mTriggeringObjects.Contains(triggerer))
		{
			return;
		}

		if(mZone.TriggerTrace(start, end, mBox, mModelNum))
		{
			return;
		}

		mTriggeringObjects.Remove(triggerer);

		//set to non triggered only if no mobiles inside
		if(mTriggeringObjects.Count <= 0)
		{
			mbTriggered			=false;
			mTimeSinceTriggered	=0;
		}
		TriggerTarget(0);
	}


	internal void TriggerTarget(uint val)
	{
		if(mTATargets.Count > 0)
		{
			foreach(TriggerAble ta in mTATargets)
			{
				ta.StateChange(TriggerAble.TState.Triggered, val);
			}
			return;
		}

		//lazy init
		List<Component>	targs	=mOwner.mBoss.GetEntityComponents(typeof(TargetName));

		foreach(TargetName tn in targs)
		{
			if(tn.mTargetName == mTarget)
			{
				TriggerAble	ta	=tn.mOwner.GetComponent(typeof(TriggerAble)) as TriggerAble;
				if(ta != null)
				{
					mTATargets.Add(ta);
				}
				else
				{
					//none there, make one
					ta	=new TriggerAble(tn.mOwner);
					tn.mOwner.AddComponent(ta);

					mTATargets.Add(ta);
				}
			}
		}

		//try again
		if(mTATargets.Count > 0)
		{
			foreach(TriggerAble ta in mTATargets)
			{
				ta.StateChange(TriggerAble.TState.Triggered, val);
			}
		}
	}
}