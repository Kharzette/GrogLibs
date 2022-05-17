using System;
using System.IO;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;


namespace MeshLib;

public class SubAnim
{
	KeyFrame	[]mKeys;
	float		[]mTimes;
	float		mTotalTime;
	int			mLastTimeIndex;	//cache the last time used to
								//quicken key finding

	KeyFrame	mBone;		//reference to corresponding bone val
	string		mBoneName;


	public SubAnim() { }
	public SubAnim(string boneName, List<float> times, List<KeyFrame> keys)
	{
		Debug.Assert(times.Count == keys.Count);

		mBone		=null;
		mBoneName	=UtilityLib.Misc.AssignValue(boneName);
		mTimes		=new float[times.Count];
		mKeys		=new KeyFrame[times.Count];

		for(int i=0;i < times.Count;i++)
		{
			mTimes[i]	=times[i];
			mKeys[i]	=new KeyFrame(keys[i]);
		}
		mTotalTime	=times[times.Count - 1] - times[0];
	}


	public string GetBoneName()
	{
		return	mBoneName;
	}


	public KeyFrame[] GetKeys()
	{
		return	mKeys;
	}


	internal float []GetTimes()
	{
		return	mTimes;
	}


	internal void SetBoneRef(KeyFrame boneKey)
	{
		mBone	=boneKey;
	}


	internal float GetTotalTime()
	{
		return	mTotalTime;
	}


	internal float GetStartTime()
	{
		return	mTimes[0];
	}


	internal int GetNumKeys()
	{
		return	mTimes.Length;
	}


	internal void Write(BinaryWriter bw)
	{
		bw.Write(mBoneName);

		//keyframe times
		bw.Write(mTimes.Length);
		foreach(float time in mTimes)
		{
			bw.Write(time);
		}

		//key values
		bw.Write(mKeys.Length);
		foreach(KeyFrame key in mKeys)
		{
			key.Write(bw);
		}

		//total time
		bw.Write(mTotalTime);
	}


	internal void Read(BinaryReader br)
	{
		mBoneName	=br.ReadString();

		int	numTimes	=br.ReadInt32();

		mTimes	=new float[numTimes];

		for(int i=0;i < numTimes;i++)
		{
			mTimes[i]	=br.ReadSingle();
		}

		int	numKeys	=br.ReadInt32();

		mKeys	=new KeyFrame[numKeys];
		
		for(int i=0;i < numKeys;i++)
		{
			mKeys[i]	=new KeyFrame();
			mKeys[i].Read(br);
		}

		mTotalTime	=br.ReadSingle();
	}


	internal void Animate(float time, ref KeyFrame key)
	{
		//make sure the time is in range
		float	animTime	=time;
		if(time < mTimes[0])
		{
			//bring into range
			animTime	=mTimes[0];
		}
		else if(time > mTimes[mTimes.Length - 1])
		{
			animTime	=mTimes[mTimes.Length - 1];
		}

		//locate the key index to start with
		int	startIndex;
		for(startIndex = mLastTimeIndex;startIndex < mTimes.Length;startIndex++)
		{
			if(startIndex > 0)
			{
				if(animTime < mTimes[startIndex] && animTime >= mTimes[startIndex - 1])
				{
					//back up one
					startIndex		=Math.Max(startIndex - 1, 0);
					mLastTimeIndex	=startIndex;
					break;	//found
				}
			}
			else
			{
				if(animTime <= mTimes[startIndex])
				{
					//back up one
					startIndex		=Math.Max(startIndex - 1, 0);
					mLastTimeIndex	=startIndex;
					break;	//found
				}
			}
		}

		if(startIndex >= mTimes.Length)
		{
			//wasn't found, search all
			for(startIndex = 0;startIndex < mTimes.Length;startIndex++)
			{
				if(animTime <= mTimes[startIndex])
				{
					//back up one
					startIndex		=Math.Max(startIndex - 1, 0);
					mLastTimeIndex	=startIndex;
					break;	//found
				}
			}
		}

		Debug.Assert(startIndex < mTimes.Length);

		//figure out the percentage between pos1 and pos2
		//get the deltatime
		float	percentage	=mTimes[startIndex + 1] - mTimes[startIndex];

		//convert to percentage
		percentage	=1.0f / percentage;

		//multiply by amount beyond p1
		percentage	*=(animTime - mTimes[startIndex]);

		Debug.Assert(percentage >= 0.0f && percentage <= 1.0f);

		KeyFrame.Lerp(mKeys[startIndex], mKeys[startIndex + 1], percentage, key);
	}


	internal void Animate(float time)
	{
		if(mBone == null)
		{
			return;
		}

		Animate(time, ref mBone);
	}


	public void Transform(Matrix4x4 trans)
	{
		foreach(KeyFrame kf in mKeys)
		{
			kf.Transform(trans);
		}
	}


	public void ConvertToLeftHanded()
	{
		foreach(KeyFrame kf in mKeys)
		{
			kf.ConvertToLeftHanded();
		}
	}
}