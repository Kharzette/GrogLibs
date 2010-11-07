using System;
using System.Collections.Generic;
using System.Xml;
using System.Diagnostics;
//using System.Reflection;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MeshLib
{
	public class FloatKeys
	{
		float	[]mTimes;		//keyframe times
		float	[]mValues;		//key values
		float	mTotalTime;		//total time of the animation

		Channel	mTarget;	//target channel


		public FloatKeys()
		{
		}


		public int GetNumKeys()
		{
			return	mTimes.Length;
		}


		public string GetTargetNode()
		{
			return	mTarget.GetTargetNode();
		}


		public FloatKeys(int numKeys, float totalTime,
			Channel	targ,
			List<float>	times,		//keyframe times
			List<float>	values,	//key values
			List<float>	control1,	//first control point per value
			List<float>	control2)	//second control point per value
		{
			mTotalTime	=totalTime;
			mTarget		=targ;

			mTimes		=new float[numKeys];
			mValues		=new float[numKeys];

			for(int i=0;i < numKeys;i++)
			{
				mTimes[i]		=times[i];
				mValues[i]		=values[i];
			}
		}


		public void Write(BinaryWriter bw)
		{
			//keyframe times
			bw.Write(mTimes.Length);
			foreach(float time in mTimes)
			{
				bw.Write(time);
			}

			//key values
			bw.Write(mValues.Length);
			foreach(float val in mValues)
			{
				bw.Write(val);
			}

			//total time
			bw.Write(mTotalTime);

			//channel
			mTarget.Write(bw);
		}


		public void Read(BinaryReader br)
		{
			int	num	=br.ReadInt32();
			mTimes	=new float[num];
			for(int i=0;i < num;i++)
			{
				mTimes[i]	=br.ReadSingle();
			}

			num	=br.ReadInt32();
			mValues	=new float[num];
			for(int i=0;i < num;i++)
			{
				mValues[i]	=br.ReadSingle();
			}

			mTotalTime	=br.ReadSingle();

			mTarget	=new Channel();
			mTarget.Read(br);
		}


		public void FixChannels(Skeleton sk)
		{
			mTarget.FixTarget(sk);
		}


		//attempt to reduce the number of
		//keyframes through interpolation
		public void Reduce(float maxError)
		{
			//don't bother unless more than 2 keys
			if(mTimes.Length < 3)
			{
				return;
			}

			//for rotational values, radians are used
			//and smaller values make a much bigger
			//difference.
			if(mTarget.GetChannelType() == Channel.ChannelType.ROTATE)
			{
				maxError	=MathHelper.ToRadians(maxError);
			}

			int	startIndex	=0;
			int	endIndex	=2;

			float	errorAccum	=0.0f;
			float	startTime	=mTimes[startIndex];
			float	endTime		=mTimes[endIndex];

			while(true)
			{
				while(errorAccum < maxError && errorAccum > -maxError)
				{
					//lerp to hit the keyframe endIndex - 1
					float	percentage	=1.0f - (1.0f / (endIndex - startIndex));

					float value	=MathHelper.Lerp(mValues[startIndex], mValues[endIndex], percentage);
					errorAccum	+=Math.Abs(mValues[endIndex - 1] - value);
					endIndex++;
					if(endIndex >= mTimes.Length)
					{
						break;
					}
				}

				if(endIndex < mTimes.Length)
				{
					//back up one as we went over error tolerance
//					endIndex--;
				}
				endIndex--;
				//gank all keys between start and end
				float	[]newTimes	=new float[mTimes.Length - (endIndex - startIndex - 1)];
				float	[]newVals	=new float[mTimes.Length - (endIndex - startIndex - 1)];

				for(int i=0;i <= startIndex;i++)
				{
					newTimes[i]	=mTimes[i];
					newVals[i]	=mValues[i];
				}
				for(int i=endIndex;i < mTimes.Length;i++)
				{
					newTimes[startIndex + 1 + (i - endIndex)]	=mTimes[i];
					newVals[startIndex + 1 + (i - endIndex)]	=mValues[i];
				}
				mTimes	=newTimes;
				mValues	=newVals;

				//prepare to run again
				startIndex++;
				endIndex	=startIndex + 2;
				errorAccum	=0.0f;
				if(endIndex >= mTimes.Length)
				{
					break;
				}
			}
		}


		public void Animate(float time)
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

			//Bring to start
			animTime	+=mTimes[0];

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

			float value	=MathHelper.Lerp(mValues[startIndex], mValues[startIndex + 1], percentage);

			mTarget.SetValue(value);
		}
	}
}
