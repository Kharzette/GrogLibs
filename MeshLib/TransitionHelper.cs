using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace MeshLib
{
	public class TransitionHelper
	{
		string		mTransFrom	="";
		string		mTransTo	="";
		bool		mbTransitioning;
		int			mFromTime, mToTime;
		float		mBlendPercentage;
		float		mBlendRate;

		//cached anim times
		Dictionary<string, int>	mTotalTimes	=new Dictionary<string, int>();
		Dictionary<string, int>	mStartTimes	=new Dictionary<string, int>();


		public TransitionHelper(AnimLib lib, float blendRate)
		{
			mBlendRate	=blendRate;

			GrabTimeInfo(lib);
		}


		void GrabTimeInfo(AnimLib lib)
		{
			List<Anim>	anims	=lib.GetAnims();

			foreach(Anim an in anims)
			{
				mTotalTimes.Add(an.Name, (int)(an.TotalTime * 1000));
				mStartTimes.Add(an.Name, (int)(an.StartTime * 1000));
			}
		}


		public void Update(int msDelta, string curAnim, bool bCurLooped, Character chr)
		{
			Debug.Assert(msDelta > 0);

			if(mbTransitioning)
			{
				if(curAnim != mTransTo)
				{
					//switching animation targets midblend!
					if(mBlendPercentage > 0.5f)
					{
						mTransFrom	=mTransTo;
						mFromTime	=mToTime;
					}

					mTransTo			=curAnim;
					mToTime				=mStartTimes[mTransTo];
					mBlendPercentage	=0f;
				}
			}
			else if(curAnim == mTransTo)
			{
				mToTime	+=msDelta;

				int	startTimeTo		=mStartTimes[mTransTo];
				int	totTimeTo		=mTotalTimes[mTransTo];

				if(mToTime > (startTimeTo + totTimeTo))
				{
					if(bCurLooped)
					{
						mToTime	-=totTimeTo;
					}
					else
					{
						mToTime	=totTimeTo;
					}
				}
			}
			else if(mTransTo != "")
			{
				mbTransitioning	=true;
				mTransFrom		=mTransTo;
				mTransTo		=curAnim;
				mFromTime		=mToTime;
				mToTime			=mStartTimes[mTransTo];
			}
			else
			{
				mTransTo	=curAnim;
				mToTime		=mStartTimes[mTransTo];
			}

			if(mbTransitioning)
			{
				int	startTimeFrom	=mStartTimes[mTransFrom];
				int	totTimeFrom		=mTotalTimes[mTransFrom];
				int	startTimeTo		=mStartTimes[mTransTo];
				int	totTimeTo		=mTotalTimes[mTransTo];

				mToTime	+=msDelta;
				if(mToTime > (startTimeTo + totTimeTo))
				{
					if(bCurLooped)
					{
						mToTime	-=totTimeTo;
					}
					else
					{
						mToTime	=totTimeTo;
					}
				}

				mFromTime	+=msDelta;
				if(mFromTime > (startTimeFrom + totTimeFrom))
				{
					mFromTime	-=totTimeFrom;
				}

				mBlendPercentage	+=(mBlendRate * msDelta);
				if(mBlendPercentage >= 1f)
				{
					mbTransitioning		=false;
					mBlendPercentage	=0f;
				}
			}

			if(mbTransitioning)
			{
				chr.Blend(mTransFrom, (float)mFromTime / 1000f,
					mTransTo, (float)mToTime / 1000f, mBlendPercentage);
			}
			else
			{
				chr.Animate(mTransTo, (float)mToTime / 1000f);
			}
		}
	}
}
