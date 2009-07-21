using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	public class AnimCreationParameters
	{
		public List<float>	mTimes;		//keyframe times
		public List<float>	mValues;	//key values
		public List<float>	mControl1;	//first control point per value
		public List<float>	mControl2;	//second control point per value
		public NodeElement	mOperand;	//reference to value being animated
	}


	public abstract class Anim
	{
		List<float>	mTimes;		//keyframe times
		List<float>	mValues;	//key values
		List<float>	mControl1;	//first control point per value
		List<float>	mControl2;	//second control point per value
		float		mTotalTime;	//total time of the animation

		protected NodeElement	mOperand;	//reference to value being animated


		public Anim(AnimCreationParameters acp)
		{
			//init our lists
			mTimes		=new List<float>();
			mValues		=new List<float>();
			mControl1	=new List<float>();
			mControl2	=new List<float>();

			//copy elements so no references are left around
			foreach(float f in acp.mTimes)
			{
				mTimes.Add(f);
			}
			foreach(float f in acp.mValues)
			{
				mValues.Add(f);
			}
			foreach(float f in acp.mControl1)
			{
				mControl1.Add(f);
			}
			foreach(float f in acp.mControl2)
			{
				mControl2.Add(f);
			}

			//this is a reference
			mOperand	=acp.mOperand;

			//figure out total time
			float	start, end;
			start	=mTimes[0];
			end		=mTimes[mTimes.Count - 1];

			mTotalTime	=end - start;
		}


		public void Animate(float time)
		{
			//make sure the time is not before our start
			if(time < mTimes[0])
			{
				return;		//not ready to animate yet
			}

			//bring the passed in time value into
			//the space of our animation
			float	animTime	=time % mTotalTime;

			//locate the key index to start with
			int	startIndex;
			for(startIndex = 0;startIndex < mTimes.Count;startIndex++)
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

			float	val	=GetBezierPosition(percentage,
				mValues[startIndex], mControl1[startIndex],
				mControl2[startIndex + 1], mValues[startIndex + 1]);

			//just use keyframe
			val	=mValues[startIndex];

			ApplyValueToOperand(val);
		}


		protected abstract void ApplyValueToOperand(float val);


		private static float B1(float t)
		{
			return	(t * t * t);
		}


		private static float B2(float t)
		{
			return	3 * (1 - t) * (t * t);
		}


		private static float B3(float t)
		{
			return	3 * ((1 - t) * (1 - t)) * t;
		}


		private static float B4(float t)
		{
			return	(1 - t) * (1 - t) * (1 - t);
		}


		//in the collada file, there are four inputs marked as:
		//INPUT, OUTPUT, IN_TANGENT, OUT_TANGENT
		//these correspond to time, pos1 and pos2, control1, and control2
		public static float GetBezierPosition(float	percent,
			float	p0,
			float	p1,
			float	p2,
			float	p3)
		{
			p1	=1.0f - p1;
			p2	=1.0f - p2;

			float	pos	=(B4(percent) * p0) +
				(B3(percent) * p1) +
				(B2(percent) * p2) +
				(B1(percent) * p3);
			return	pos;
		}
	}
}