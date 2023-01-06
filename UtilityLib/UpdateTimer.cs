using System;
using System.Threading;
using System.Diagnostics;


namespace UtilityLib
{
	public class UpdateTimer
	{
		//time stats
		long	mLastTimeStamp;	//previous time
		long	mTimeNow;		//time as of last Stamp() call
		long	mMaxDelta;		//biggest allowed deltatime

		//time step related
		bool	mbFixedStep;		//use a fixed time stamp for game/physics updates?
		bool	mbSpendRemainder;	//spend or roll over the small remainder?
		long	mStep;				//fixed step in tics
		long	mFullUpdateTime;	//counts down over the updates for this frame


		public UpdateTimer(bool bFixed, bool bSpendRemainder)
		{
			mbFixedStep			=bFixed;
			mbSpendRemainder	=bSpendRemainder;

			SetMaxDeltaSeconds(0.1f);	//default
		}


		public long GetRenderUpdateDeltaTics()
		{
			long	tics	=Delta();
			if(mbFixedStep && !mbSpendRemainder)
			{
				//subtract remainder
				//return	tics - mFullUpdateTime;
				return	tics;
			}
			else
			{
				return	tics;
			}
		}


		public float GetRenderUpdateDeltaSeconds()
		{
			return	TicsToSeconds(GetRenderUpdateDeltaTics());
		} 


		public float GetRenderUpdateDeltaMilliSeconds()
		{
			return	TicsToMilliSeconds(GetRenderUpdateDeltaTics());
		} 


		public long GetUpdateDeltaTics()
		{
			if(mbFixedStep)
			{
				if(mFullUpdateTime >= mStep)
				{
					return	mStep;
				}

				if(mbSpendRemainder && mFullUpdateTime > 0)
				{
					return	mFullUpdateTime;
				}
				return	0L;
			}
			else
			{
				return	mFullUpdateTime;
			}
		}


		public float GetUpdateDeltaSeconds()
		{
			long	tics	=GetUpdateDeltaTics();

			return	TicsToSeconds(tics);
		}


		public float GetUpdateDeltaMilliSeconds()
		{
			long	tics	=GetUpdateDeltaTics();

			return	TicsToMilliSeconds(tics);
		}


		public void UpdateDone()
		{
			if(mbFixedStep)
			{
				if(mFullUpdateTime >= mStep)
				{
					mFullUpdateTime	-=mStep;
				}
				else
				{
					if(mbSpendRemainder)
					{
						mFullUpdateTime	=0;
					}
				}
			}
			else
			{
				mFullUpdateTime	=0;
			}
		}


		//allows a user preference on the maximum
		//deltatime allowed (for game stability)
		public void SetMaxDeltaTics(long tics)
		{
			mMaxDelta	=tics;
		}


		public void SetMaxDeltaMilliSeconds(float milliSeconds)
		{
			mMaxDelta	=MilliSecondsToTics(milliSeconds);
		}


		public void SetMaxDeltaSeconds(float seconds)
		{
			mMaxDelta	=SecondsToTics(seconds);
		}


		public void SetFixedTimeStepTics(long tics)
		{
			mStep	=tics;
		}


		public void SetFixedTimeStepSeconds(float seconds)
		{
			mStep	=SecondsToTics(seconds);
		}


		public void SetFixedTimeStepMilliSeconds(float milliSeconds)
		{
			mStep	=MilliSecondsToTics(milliSeconds);
		}


		public void Stamp()
		{
			mLastTimeStamp	=mTimeNow;

			mTimeNow	=Stopwatch.GetTimestamp();

			mFullUpdateTime	+=Delta();
		}


		public long DeltaTics()
		{
			return	Delta();
		}


		//deltas are reasonably safe in floats
		public float DeltaSeconds()
		{
			long	tics	=Delta();

			return	TicsToSeconds(tics);
		}


		//deltas are reasonably safe in floats
		public float DeltaMilliSeconds()
		{
			long	tics	=Delta();

			return	TicsToMilliSeconds(tics);
		}


		long Delta()
		{
			long	delta	=mTimeNow - mLastTimeStamp;

			//will this ever overflow?
			Debug.Assert(delta < long.MaxValue);

			return	Math.Min(delta, mMaxDelta);
		}


		float TicsToSeconds(long tics)
		{
			double	secs	=tics / (double)Stopwatch.Frequency;

			return	(float)secs;
		}


		float TicsToMilliSeconds(long tics)
		{
			double	msecs	=tics / (double)Stopwatch.Frequency;

			msecs	*=1000.0;

			return	(float)msecs;
		}


		long SecondsToTics(float seconds)
		{
			double	tics	=(double)seconds * (double)Stopwatch.Frequency;

			return	(long)tics;
		}


		long MilliSecondsToTics(float milliSeconds)
		{
			double	msFreq	=Stopwatch.Frequency / 1000.0;

			return	(long)((double)milliSeconds * msFreq);
		}
	}
}
