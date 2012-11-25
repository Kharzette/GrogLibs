using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using UtilityLib;


namespace ParticleLib
{
	internal class Emitter
	{
		//randomizer
		Random	mRand	=new Random();

		//basic info
		Vector3	mPosition;
		float	mStartSize;
		int		mEmitPerSecond;
		int		mDurationMS;	//negative for forever
		int		mMaxParticles;

		//particle behaviour
		//velocities in units per second
		int	mRotationalVelocityMin, mRotationalVelocityMax;
		int	mVelocityMin, mVelocityMax;
		int	mSizeVelocityMin, mSizeVelocityMax;
		int	mAlphaVelocityMin, mAlphaVelocityMax;
		int	mLifeMin, mLifeMax;

		//state data
		bool	mbOn;
		int		mCurDuration;
		bool	mbBuffer;

		//particle work buffers
		Particle	[]mParticles1;
		Particle	[]mParticles2;

		//events
		internal event EventHandler	eFinished;


		internal Emitter(int maxParticles,
			Vector3 pos, float startSize,
			int durationMS, int emitPerSecond,
			int rotVelMin, int rotVelMax, int velMin,
			int velMax, int sizeVelMin, int sizeVelMax,
			int alphaVelMin, int alphaVelMax,
			int lifeMin, int lifeMax)
		{
			mPosition				=pos;
			mStartSize				=startSize;
			mEmitPerSecond			=emitPerSecond;
			mDurationMS				=durationMS;
			mRotationalVelocityMin	=rotVelMin;
			mRotationalVelocityMax	=rotVelMax;
			mVelocityMin			=rotVelMin;
			mVelocityMax			=rotVelMax;
			mSizeVelocityMin		=sizeVelMin;
			mSizeVelocityMax		=sizeVelMax;
			mAlphaVelocityMin		=alphaVelMin;
			mAlphaVelocityMax		=alphaVelMax;
			mLifeMin				=lifeMin;
			mLifeMax				=lifeMax;

			mCurDuration	=durationMS;
			mMaxParticles	=maxParticles;

			mParticles1	=new Particle[maxParticles];
			mParticles2	=new Particle[maxParticles];
		}


		internal void Activate(bool bOn)
		{
			mbOn	=bOn;
		}


		internal Particle []Update(int msDelta)
		{
			if(!mbOn)
			{
				return	null;
			}

			if(mDurationMS > 0)
			{
				mCurDuration	-=msDelta;
				if(mCurDuration < 0)
				{
					Misc.SafeInvoke(eFinished, this);
				}
			}

			//update existing
			Particle	[]buf	=(mbBuffer)? mParticles1 : mParticles2;
			Particle	[]buf2	=(mbBuffer)? mParticles2 : mParticles1;

			mbBuffer	=!mbBuffer;

			int	idx	=0;
			for(int i=0;i < buf.Length;i++)
			{
				if(!buf[i].Update(msDelta))
				{
					buf2[idx++]	=buf[i];
				}
			}

			int	newParticles	=(int)(msDelta * (mEmitPerSecond / 1000f));

			if((newParticles + idx) >= mMaxParticles)
			{
				newParticles	=mMaxParticles - idx;
			}

			for(int i=0;i < newParticles;i++)
			{
				buf2[idx++]	=Emit();
			}

			return	buf2;
		}


		Particle Emit()
		{
			Particle	ret	=new Particle();

			ret.mPosition		=mPosition;
			ret.mSize			=mStartSize;
			ret.mRotation		=0;
			ret.mLifeRemaining	=mRand.Next(mLifeMin, mLifeMax);

			ret.mVelocity	=Mathery.RandomDirection(mRand)
				* (mRand.Next(mVelocityMin, mVelocityMax) / 1000f);

			ret.mRotationalVelocity	=(mRand.Next(mRotationalVelocityMin, mRotationalVelocityMax) / 1000f);

			ret.mSizeVelocity	=(mRand.Next(mSizeVelocityMin, mSizeVelocityMax) / 1000f);

			ret.mAlphaVelocity	=(mRand.Next(mAlphaVelocityMin, mAlphaVelocityMax) / 1000f);

			return	ret;
		}
	}
}
