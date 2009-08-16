using System;
using System.Collections.Generic;
using System.Xml;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	public class GameSubAnim
	{
		float	[]mTimes;		//keyframe times
		float	[]mValues;		//key values
		float	[]mControl1;	//first control point per value
		float	[]mControl2;	//second control point per value
		float	mTotalTime;		//total time of the animation

		GameChannel	mTarget;	//target channel


		public GameSubAnim(int numKeys, float totalTime,
			GameChannel	targ,
			List<float>	times,		//keyframe times
			List<float>	values,	//key values
			List<float>	control1,	//first control point per value
			List<float>	control2)	//second control point per value
		{
			mTotalTime	=totalTime;
			mTarget		=targ;

			mTimes		=new float[numKeys];
			mValues		=new float[numKeys];
			mControl1	=new float[numKeys];
			mControl2	=new float[numKeys];

			for(int i=0;i < numKeys;i++)
			{
				mTimes[i]		=times[i];
				mValues[i]		=values[i];
				mControl1[i]	=control1[i];
				mControl2[i]	=control2[i];
			}
		}


		public void Animate(float time, GameSkeleton gs)
		{
			//make sure the time is not before our start
			if(time < mTimes[0])
			{
				Debug.WriteLine("Key time out of range in Animate!");
				return;		//not ready to animate yet
			}

			//bring the passed in time value into
			//the space of our animation
			float	animTime	=time % mTotalTime;

			//locate the key index to start with
			int	startIndex;
			for(startIndex = 0;startIndex < mTimes.Length;startIndex++)
			{
				if(animTime < mTimes[startIndex])
				{
					//back up one
					startIndex--;
					break;	//found
				}
			}

			//figure out the percentage between pos1 and pos2
			//get the deltatime
			float	percentage	=mTimes[startIndex + 1] - mTimes[startIndex];

			//convert to percentage
			percentage	=1.0f / percentage;

			//multiply by amount beyond p1
			percentage	*=(animTime - mTimes[startIndex]);

			Debug.Assert(percentage >= 0.0f && percentage <= 1.0f);

			//can't get this bezier stuff to work
			/*
			float	val	=GetBezierPosition(percentage,
				mValues[startIndex], mControl1[startIndex],
				mControl2[startIndex + 1], mValues[startIndex + 1]);

			float	val2	=LongWay(percentage, mValues[startIndex], mControl1[startIndex],
				mControl2[startIndex + 1], mValues[startIndex + 1]);
			*/

//			Quaternion	qd	=Quaternion.Lerp(mRotation[startIndex + 1], mRotation[startIndex], 1.0f - percentage);
//			Vector3		sd	=Vector3.Lerp(mScale[startIndex + 1], mScale[startIndex], 1.0f - percentage);
//			Vector3		td	=Vector3.Lerp(mTrans[startIndex + 1], mTrans[startIndex], 1.0f - percentage);

			float value	=MathHelper.Lerp(mValues[startIndex], mValues[startIndex + 1], percentage);
//			float	value	=mValues[0];

			mTarget.SetValue(value);
		}
	}
}
