using System;
using System.Diagnostics;
using System.Numerics;


namespace UtilityLib
{
	//thanks to Kevin Lau for the math help
	public class Mover
	{
		//passed in setup stuff
		float		mTargetPos;
		float		mStartPos;
		float		mEaseInPercent;		//percentage of time to ease in
		float		mEaseOutPercent;	//percentage of time to ease out
		float		mTravelTime;

		//calculated stuff
		float		mStage1Accel;		//accel for easein stage
		float		mStage3Accel;		//decel for easeout stage
		float		mMaxVelocity;		//maximum velocity for stage 2
		float		mTotalDistance;
		float		mStage1EndPos;		//position at the end of stage 1
		float		mStage2EndPos;		//position at the end of stage 2

		//stuff moving on the fly
		float		mCurPos;
		float		mCurTime;
		bool		mbDone;

		const float	MinimumDistance		=0.001f;


		public Mover()
		{
			mbDone	=true;	//start done
		}


		public float GetPos()
		{
			return	mCurPos;
		}


		public bool	Done()
		{
			return	mbDone;
		}


		//all times in seconds
		public void SetUpMove(float startPos, float endPos, float travelTime,
			float easeInPercent, float easeOutPercent)
		{
			//grab movement vital stats
			mStartPos	=startPos;
			mTargetPos	=endPos;
			mTravelTime	=travelTime;

			//ease percentages
			mEaseInPercent	=easeInPercent;
			mEaseOutPercent	=easeOutPercent;

			//set up calculations
			mCurPos		=startPos;
			mCurTime	=0.0f;

			float	distVec	=endPos - startPos;

			mTotalDistance	=distVec;
			if(mTotalDistance < MinimumDistance)
			{
				mbDone	=true;
				return;
			}

			float	timeSlice	=0.5f * easeInPercent * travelTime * travelTime * (2 - easeInPercent - easeOutPercent);

			mStage1Accel	=mTotalDistance / timeSlice;

			mStage3Accel	=-mStage1Accel * 0.5f * (easeInPercent / easeOutPercent);

			float	stage1Time	=(easeInPercent * travelTime);

			//figure out what the max velocity will be
			mMaxVelocity	=mStage1Accel * stage1Time;

			float	distScaled	=0.5f * (mStage1Accel * stage1Time * stage1Time);
			mStage1EndPos		=mStartPos + distScaled;
								
			float	timeAtMaxVelocity	=(travelTime * (1.0f - (easeInPercent + easeOutPercent)));
			distScaled		=distVec * (mMaxVelocity * timeAtMaxVelocity);
			mStage2EndPos	=mStage1EndPos + distScaled;

			mbDone	=false;
		}


		public void Update(float secDelta)
		{
			Debug.Assert(secDelta > 0f);

			//get the direction vector
			float	dir	=mTargetPos - mStartPos;

			//get current time
			mCurTime	+=secDelta;

			//limited movement
			if(mCurTime > mTravelTime)
			{
				mCurPos	=mTargetPos;
				mbDone	=true;
				return;
			}

			//figure out where we are along
			//the path of motion based on time
			float	scalar	=mCurTime / mTravelTime;

			//see if our current position falls within
			//the boundaries of ease in / out
			if(scalar < mEaseInPercent)
			{
				//use the stage one accel
				float	curVelocity	=0.5f * mStage1Accel * mCurTime;

				dir	*=curVelocity * mCurTime;

				mCurPos	=mStartPos + dir;
			}
			else if(scalar > (1.0f - mEaseOutPercent))
			{
				//reduce time to zero for stage
				float	timeRampingUp		=(mTravelTime * mEaseInPercent);
				float	timeAtMaxVelocity	=(mTravelTime * (1.0f - (mEaseInPercent + mEaseOutPercent)));
				float	stage3Time			=(mCurTime - timeAtMaxVelocity - timeRampingUp);

				float	curVelocity	=mMaxVelocity + (mStage3Accel * stage3Time);

				dir	*=curVelocity * stage3Time;

				mCurPos	=mStage2EndPos + dir;
			}
			else
			{
				//second stage is maximum velocity
				//reduce time to zero for stage start
				float	timeRampingUp	=(mTravelTime * mEaseInPercent);
				dir		*=mMaxVelocity * (mCurTime - timeRampingUp);
				mCurPos	=mStage1EndPos + dir;
			}
		}
	}


	public class Mover2
	{
		//passed in setup stuff
		Vector2		mTargetPos;
		Vector2		mStartPos;
		float		mEaseInPercent;		//percentage of time to ease in
		float		mEaseOutPercent;	//percentage of time to ease out
		float		mTravelTime;

		//calculated stuff
		float		mStage1Accel;		//accel for easein stage
		float		mStage3Accel;		//decel for easeout stage
		float		mMaxVelocity;		//maximum velocity for stage 2
		float		mTotalDistance;
		Vector2		mStage1EndPos;		//position at the end of stage 1
		Vector2		mStage2EndPos;		//position at the end of stage 2

		//stuff moving on the fly
		Vector2		mCurPos;
		float		mCurTime;
		bool		mbDone;

		const float	MinimumDistance		=0.001f;


		public Mover2()
		{
			mbDone	=true;	//start done
		}


		public Vector2	GetPos()
		{
			return	mCurPos;
		}


		public bool	Done()
		{
			return	mbDone;
		}


		//all times in seconds
		public void SetUpMove(Vector2 startPos, Vector2 endPos, float travelTime,
			float easeInPercent, float easeOutPercent)
		{
			//grab movement vital stats
			mStartPos	=startPos;
			mTargetPos	=endPos;
			mTravelTime	=travelTime;

			//ease percentages
			mEaseInPercent	=easeInPercent;
			mEaseOutPercent	=easeOutPercent;

			//set up calculations
			mCurPos		=startPos;
			mCurTime	=0.0f;

			Vector2	distVec	=endPos - startPos;

			mTotalDistance	=distVec.Length();
			if(mTotalDistance < MinimumDistance)
			{
				mbDone	=true;
				return;
			}

			distVec	/=mTotalDistance;

			float	timeSlice	=0.5f * easeInPercent * travelTime * travelTime * (2 - easeInPercent - easeOutPercent);

			mStage1Accel	=mTotalDistance / timeSlice;

			mStage3Accel	=-mStage1Accel * 0.5f * (easeInPercent / easeOutPercent);

			float	stage1Time	=(easeInPercent * travelTime);

			//figure out what the max velocity will be
			mMaxVelocity	=mStage1Accel * stage1Time;

			Vector2	distScaled	=distVec * 0.5f * (mStage1Accel * stage1Time * stage1Time);
			mStage1EndPos		=mStartPos + distScaled;
								
			float	timeAtMaxVelocity	=(travelTime * (1.0f - (easeInPercent + easeOutPercent)));
			distScaled		=distVec * (mMaxVelocity * timeAtMaxVelocity);
			mStage2EndPos	=mStage1EndPos + distScaled;

			mbDone	=false;
		}


		public void Update(float secDelta)
		{
			Debug.Assert(secDelta > 0f);

			//get the direction vector
			Vector2	dir	=mTargetPos - mStartPos;

			//make unit
			dir	=Vector2.Normalize(dir);

			//get current time
			mCurTime	+=secDelta;

			//limited movement
			if(mCurTime > mTravelTime)
			{
				mCurPos	=mTargetPos;
				mbDone	=true;
				return;
			}

			//figure out where we are along
			//the path of motion based on time
			float	scalar	=mCurTime / mTravelTime;

			//see if our current position falls within
			//the boundaries of ease in / out
			if(scalar < mEaseInPercent)
			{
				//use the stage one accel
				float	curVelocity	=0.5f * mStage1Accel * mCurTime;

				dir	*=curVelocity * mCurTime;

				mCurPos	=mStartPos + dir;
			}
			else if(scalar > (1.0f - mEaseOutPercent))
			{
				//reduce time to zero for stage
				float	timeRampingUp		=(mTravelTime * mEaseInPercent);
				float	timeAtMaxVelocity	=(mTravelTime * (1.0f - (mEaseInPercent + mEaseOutPercent)));
				float	stage3Time			=(mCurTime - timeAtMaxVelocity - timeRampingUp);

				float	curVelocity	=mMaxVelocity + (mStage3Accel * stage3Time);

				dir	*=curVelocity * stage3Time;

				mCurPos	=mStage2EndPos + dir;
			}
			else
			{
				//second stage is maximum velocity
				//reduce time to zero for stage start
				float	timeRampingUp	=(mTravelTime * mEaseInPercent);
				dir		*=mMaxVelocity * (mCurTime - timeRampingUp);
				mCurPos	=mStage1EndPos + dir;
			}
		}
	}


	public class Mover3
	{
		//passed in setup stuff
		Vector3		mTargetPos;
		Vector3		mStartPos;
		float		mEaseInPercent;		//percentage of time to ease in
		float		mEaseOutPercent;	//percentage of time to ease out
		float		mTravelTime;

		//calculated stuff
		float		mStage1Accel;		//accel for easein stage
		float		mStage3Accel;		//decel for easeout stage
		float		mMaxVelocity;		//maximum velocity for stage 2
		float		mTotalDistance;
		Vector3		mStage1EndPos;		//position at the end of stage 1
		Vector3		mStage2EndPos;		//position at the end of stage 2

		//stuff moving on the fly
		Vector3		mCurPos;
		float		mCurTime;
		bool		mbDone;

		const float	MinimumDistance		=0.001f;


		public Mover3()
		{
			mbDone	=true;	//start done
		}


		public Vector3	GetPos()
		{
			return	mCurPos;
		}


		public bool	Done()
		{
			return	mbDone;
		}


		//all times in seconds
		public void SetUpMove(Vector3 startPos, Vector3 endPos, float travelTime,
			float easeInPercent, float easeOutPercent)
		{
			//grab movement vital stats
			mStartPos	=startPos;
			mTargetPos	=endPos;
			mTravelTime	=travelTime;

			//ease percentages
			mEaseInPercent	=easeInPercent;
			mEaseOutPercent	=easeOutPercent;

			//set up calculations
			mCurPos		=startPos;
			mCurTime	=0.0f;

			Vector3	distVec	=endPos - startPos;

			mTotalDistance	=distVec.Length();
			if(mTotalDistance < MinimumDistance)
			{
				mbDone	=true;
				return;
			}

			distVec	/=mTotalDistance;

			float	timeSlice	=0.5f * easeInPercent * travelTime * travelTime * (2 - easeInPercent - easeOutPercent);

			mStage1Accel	=mTotalDistance / timeSlice;

			mStage3Accel	=-mStage1Accel * 0.5f * (easeInPercent / easeOutPercent);

			float	stage1Time	=(easeInPercent * travelTime);

			//figure out what the max velocity will be
			mMaxVelocity	=mStage1Accel * stage1Time;

			Vector3	distScaled	=distVec * 0.5f * (mStage1Accel * stage1Time * stage1Time);
			mStage1EndPos		=mStartPos + distScaled;
								
			float	timeAtMaxVelocity	=(travelTime * (1.0f - (easeInPercent + easeOutPercent)));
			distScaled		=distVec * (mMaxVelocity * timeAtMaxVelocity);
			mStage2EndPos	=mStage1EndPos + distScaled;

			mbDone	=false;
		}


		public void Update(float secDelta)
		{
			Debug.Assert(secDelta > 0f);

			//get the direction vector
			Vector3	dir	=mTargetPos - mStartPos;

			//make unit
			dir	=Vector3.Normalize(dir);

			//get current time
			mCurTime	+=secDelta;

			//limited movement
			if(mCurTime > mTravelTime)
			{
				mCurPos	=mTargetPos;
				mbDone	=true;
				return;
			}

			//figure out where we are along
			//the path of motion based on time
			float	scalar	=mCurTime / mTravelTime;

			//see if our current position falls within
			//the boundaries of ease in / out
			if(scalar < mEaseInPercent)
			{
				//use the stage one accel
				float	curVelocity	=0.5f * mStage1Accel * mCurTime;

				dir	*=curVelocity * mCurTime;

				mCurPos	=mStartPos + dir;
			}
			else if(scalar > (1.0f - mEaseOutPercent))
			{
				//reduce time to zero for stage
				float	timeRampingUp		=(mTravelTime * mEaseInPercent);
				float	timeAtMaxVelocity	=(mTravelTime * (1.0f - (mEaseInPercent + mEaseOutPercent)));
				float	stage3Time			=(mCurTime - timeAtMaxVelocity - timeRampingUp);

				float	curVelocity	=mMaxVelocity + (mStage3Accel * stage3Time);

				dir	*=curVelocity * stage3Time;

				mCurPos	=mStage2EndPos + dir;
			}
			else
			{
				//second stage is maximum velocity
				//reduce time to zero for stage start
				float	timeRampingUp	=(mTravelTime * mEaseInPercent);
				dir		*=mMaxVelocity * (mCurTime - timeRampingUp);
				mCurPos	=mStage1EndPos + dir;
			}
		}
	}


	public class Mover4
	{
		//passed in setup stuff
		Vector4		mTargetPos;
		Vector4		mStartPos;
		float		mEaseInPercent;		//percentage of time to ease in
		float		mEaseOutPercent;	//percentage of time to ease out
		float		mTravelTime;

		//calculated stuff
		float		mStage1Accel;		//accel for easein stage
		float		mStage3Accel;		//decel for easeout stage
		float		mMaxVelocity;		//maximum velocity for stage 2
		float		mTotalDistance;
		Vector4		mStage1EndPos;		//position at the end of stage 1
		Vector4		mStage2EndPos;		//position at the end of stage 2

		//stuff moving on the fly
		Vector4		mCurPos;
		float		mCurTime;
		bool		mbDone;

		const float	MinimumDistance		=0.001f;


		public Mover4()
		{
			mbDone	=true;	//start done
		}


		public Vector4	GetPos()
		{
			return	mCurPos;
		}


		public bool	Done()
		{
			return	mbDone;
		}


		//all times in seconds
		public void SetUpMove(Vector4 startPos, Vector4 endPos, float travelTime,
			float easeInPercent, float easeOutPercent)
		{
			//grab movement vital stats
			mStartPos	=startPos;
			mTargetPos	=endPos;
			mTravelTime	=travelTime;

			//ease percentages
			mEaseInPercent	=easeInPercent;
			mEaseOutPercent	=easeOutPercent;

			//set up calculations
			mCurPos		=startPos;
			mCurTime	=0.0f;

			Vector4	distVec	=endPos - startPos;

			mTotalDistance	=distVec.Length();
			if(mTotalDistance < MinimumDistance)
			{
				mbDone	=true;
				return;
			}

			distVec	/=mTotalDistance;

			float	timeSlice	=0.5f * easeInPercent * travelTime * travelTime * (2 - easeInPercent - easeOutPercent);

			mStage1Accel	=mTotalDistance / timeSlice;

			mStage3Accel	=-mStage1Accel * 0.5f * (easeInPercent / easeOutPercent);

			float	stage1Time	=(easeInPercent * travelTime);

			//figure out what the max velocity will be
			mMaxVelocity	=mStage1Accel * stage1Time;

			Vector4	distScaled	=distVec * 0.5f * (mStage1Accel * stage1Time * stage1Time);
			mStage1EndPos		=mStartPos + distScaled;
								
			float	timeAtMaxVelocity	=(travelTime * (1.0f - (easeInPercent + easeOutPercent)));
			distScaled		=distVec * (mMaxVelocity * timeAtMaxVelocity);
			mStage2EndPos	=mStage1EndPos + distScaled;

			mbDone	=false;
		}


		public void Update(float secDelta)
		{
			Debug.Assert(secDelta > 0f);

			//get the direction vector
			Vector4	dir	=mTargetPos - mStartPos;

			//make unit
			dir	=Vector4.Normalize(dir);

			//get current time
			mCurTime	+=secDelta;

			//limited movement
			if(mCurTime > mTravelTime)
			{
				mCurPos	=mTargetPos;
				mbDone	=true;
				return;
			}

			//figure out where we are along
			//the path of motion based on time
			float	scalar	=mCurTime / mTravelTime;

			//see if our current position falls within
			//the boundaries of ease in / out
			if(scalar < mEaseInPercent)
			{
				//use the stage one accel
				float	curVelocity	=0.5f * mStage1Accel * mCurTime;

				dir	*=curVelocity * mCurTime;

				mCurPos	=mStartPos + dir;
			}
			else if(scalar > (1.0f - mEaseOutPercent))
			{
				//reduce time to zero for stage
				float	timeRampingUp		=(mTravelTime * mEaseInPercent);
				float	timeAtMaxVelocity	=(mTravelTime * (1.0f - (mEaseInPercent + mEaseOutPercent)));
				float	stage3Time			=(mCurTime - timeAtMaxVelocity - timeRampingUp);

				float	curVelocity	=mMaxVelocity + (mStage3Accel * stage3Time);

				dir	*=curVelocity * stage3Time;

				mCurPos	=mStage2EndPos + dir;
			}
			else
			{
				//second stage is maximum velocity
				//reduce time to zero for stage start
				float	timeRampingUp	=(mTravelTime * mEaseInPercent);
				dir		*=mMaxVelocity * (mCurTime - timeRampingUp);
				mCurPos	=mStage1EndPos + dir;
			}
		}
	}
}
