using System;
using System.Collections.Generic;
using System.Xml;
using System.Diagnostics;
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
		public string		mNodeName;
	}


	public abstract class Anim
	{
		List<float>	mTimes;		//keyframe times
		List<float>	mValues;	//key values
		List<float>	mControl1;	//first control point per value
		List<float>	mControl2;	//second control point per value
		float		mTotalTime;	//total time of the animation
		string		mNodeName;	//needed for conversion

		protected NodeElement	mOperand;	//reference to value being animated


		public Anim(AnimCreationParameters acp)
		{
			mNodeName	=acp.mNodeName;

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


		public string GetOperandSID()
		{
			return	mOperand.GetSID();
		}


		public List<float> GetTimes()
		{
			return	mTimes;
		}


		public List<float> GetValues()
		{
			return	mValues;
		}


		public List<float> GetControl1()
		{
			return	mControl1;
		}


		public List<float> GetControl2()
		{
			return	mControl2;
		}


		public int GetNumKeys()
		{
			return	mTimes.Count;
		}


		public float GetTotalTime()
		{
			return	mTotalTime;
		}


		public string GetNodeName()
		{
			return	mNodeName;
		}


		//for conversion to gamechannels
		public Character.Channel.ChannelType GetChannelType()
		{
			if(mOperand is Rotate)
			{
				return	Character.Channel.ChannelType.ROTATE;
			}
			else if(mOperand is Scale)
			{
				return	Character.Channel.ChannelType.SCALE;
			}
			else if(mOperand is Translate)
			{
				return	Character.Channel.ChannelType.TRANSLATE;
			}

			//todo: some kind of error here maybe?
			return Character.Channel.ChannelType.LOOKAT;
		}


		public Character.Channel.AxisTarget GetChannelTarget()
		{
			Character.Channel.ChannelType	ct	=GetChannelType();

			if(ct == Character.Channel.ChannelType.ROTATE)
			{
				if(this is RotateXAnim)
				{
					return	Character.Channel.AxisTarget.X;
				}
				else if(this is RotateYAnim)
				{
					return	Character.Channel.AxisTarget.Y;
				}
				else if(this is RotateZAnim)
				{
					return	Character.Channel.AxisTarget.Z;
				}
				else if(this is RotateWAnim)
				{
					return	Character.Channel.AxisTarget.W;
				}
			}
			else if(ct == Character.Channel.ChannelType.SCALE)
			{
				if(this is ScaleXAnim)
				{
					return	Character.Channel.AxisTarget.X;
				}
				else if(this is ScaleYAnim)
				{
					return	Character.Channel.AxisTarget.Y;
				}
				else if(this is ScaleZAnim)
				{
					return	Character.Channel.AxisTarget.Z;
				}
			}
			else if(ct == Character.Channel.ChannelType.TRANSLATE)
			{
				if(this is TransXAnim)
				{
					return	Character.Channel.AxisTarget.X;
				}
				else if(this is TransYAnim)
				{
					return	Character.Channel.AxisTarget.Y;
				}
				else if(this is TransZAnim)
				{
					return	Character.Channel.AxisTarget.Z;
				}
			}
			return	Character.Channel.AxisTarget.W;
		}


		public float GetTimeForKey(int idx)
		{
			if(idx < 0 || idx > mTimes.Count)
			{
				Debug.WriteLine("Keyframe index out of range in Animate(int)!");
				return	0.0f;
			}

			return	mTimes[idx];
		}

		//this overload anims by keyframe index
		public void Animate(int idx)
		{
			if(idx < 0 || idx > mTimes.Count)
			{
				Debug.WriteLine("Keyframe index out of range in Animate(int)!");
				return;
			}

			ApplyValueToOperand(mValues[idx]);
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

			//can't get this bezier stuff to work
			/*
			float	val	=GetBezierPosition(percentage,
				mValues[startIndex], mControl1[startIndex],
				mControl2[startIndex + 1], mValues[startIndex + 1]);

			float	val2	=LongWay(percentage, mValues[startIndex], mControl1[startIndex],
				mControl2[startIndex + 1], mValues[startIndex + 1]);
			*/

			float	val3	=mValues[startIndex + 1] - mValues[startIndex];
			val3	*=percentage;
			val3	+=mValues[startIndex];

			Debug.Assert(percentage >= 0.0f && percentage <= 1.0f);

			//just use keyframe
			//val	=mValues[startIndex];

			ApplyValueToOperand(val3);
		}


		protected abstract void ApplyValueToOperand(float val);


		public static float LongWay(float t, float p0, float p1, float p2, float p3)
		{
			return	((1 - t) * (1 - t) * (1 - t)) * p0 +
				3 * ((1 - t) * (1 - t)) * t * p1 +
				3 * (1 - t) * (t * t) * p2 +
				(t * t * t) * p3;
		}


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
			float	pos	=(B4(percent) * p0) +
				(B3(percent) * p1) +
				(B2(percent) * p2) +
				(B1(percent) * p3);
			return	pos;
		}
	}
}