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
		float	mStartSize, mStartAlpha;
		float	mEmitMS;
		int		mDurationMS;	//negative for forever
		int		mMaxParticles;
		int		mCurNumParticles;
		Vector3	mGravity;

		//particle behaviour
		//velocities in units per second
		float	mRotationalVelocityMin, mRotationalVelocityMax;
		float	mVelocityMin, mVelocityMax;
		float	mSizeVelocityMin, mSizeVelocityMax;
		float	mAlphaVelocityMin, mAlphaVelocityMax;
		int		mLifeMin, mLifeMax;

		//state data
		bool	mbOn;
		int		mCurDuration;
		bool	mbBuffer;
		float	mEmitProgress;
		int		mEmitted;

		//particle work buffers
		Particle	[]mParticles1;
		Particle	[]mParticles2;

		//events
		internal event EventHandler	eFinished;


		internal Emitter(int maxParticles,
			Vector3 pos, Vector3 gravity,
			float startSize, float startAlpha,
			int durationMS, float emitMS,
			float rotVelMin, float rotVelMax, float velMin,
			float velMax, float sizeVelMin, float sizeVelMax,
			float alphaVelMin, float alphaVelMax,
			int lifeMin, int lifeMax)
		{
			mPosition				=pos;
			mGravity				=gravity;
			mStartSize				=startSize;
			mStartAlpha				=startAlpha;
			mEmitMS					=emitMS;
			mDurationMS				=durationMS;
			mRotationalVelocityMin	=rotVelMin;
			mRotationalVelocityMax	=rotVelMax;
			mVelocityMin			=velMin;
			mVelocityMax			=velMax;
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
			mbOn			=bOn;
			mEmitProgress	=0f;
		}


		internal Particle []Update(int msDelta, out int numParticles)
		{
			numParticles	=0;

			if(!mbOn)
			{
				return	null;
			}

			if(mDurationMS > 0)
			{
				mCurDuration	-=msDelta;
			}

			//update existing
			Particle	[]buf	=(mbBuffer)? mParticles1 : mParticles2;
			Particle	[]buf2	=(mbBuffer)? mParticles2 : mParticles1;

			mbBuffer	=!mbBuffer;

			int	idx	=0;
			for(int i=0;i < mCurNumParticles;i++)
			{
				if(!buf[i].Update(msDelta, mGravity))
				{
					buf2[idx++]	=buf[i];
				}
			}

			if(mCurDuration > 0)
			{
				float	emitCount	=msDelta * mEmitMS;

				mEmitProgress	+=emitCount;

				int	newParticles	=(int)(mEmitProgress - mEmitted);

				if((newParticles + idx) >= mMaxParticles)
				{
					newParticles	=mMaxParticles - idx;
				}

				mEmitted	+=newParticles;

				for(int i=0;i < newParticles;i++)
				{
					buf2[idx++]	=Emit();
				}
			}

			if(mCurDuration <= 0)
			{
				int	k=0;
				k++;
			}

			numParticles		=idx;
			mCurNumParticles	=idx;

			if(mCurNumParticles <= 0)
			{
				if(mCurDuration <= 0)
				{
					Misc.SafeInvoke(eFinished, this);
					return	null;
				}
			}
			return	buf2;
		}


		Particle Emit()
		{
			Particle	ret	=new Particle();

			ret.mPosition		=mPosition;
			ret.mSize			=mStartSize;
			ret.mRotation		=0;
			ret.mAlpha			=mStartAlpha;
			ret.mLifeRemaining	=mRand.Next(mLifeMin, mLifeMax);

			ret.mVelocity	=Mathery.RandomDirection(mRand)
				* Mathery.RandomFloatNext(mRand, mVelocityMin, mVelocityMax);

			ret.mRotationalVelocity	=Mathery.RandomFloatNext(mRand, mRotationalVelocityMin, mRotationalVelocityMax);

			ret.mSizeVelocity	=Mathery.RandomFloatNext(mRand, mSizeVelocityMin, mSizeVelocityMax);

			ret.mAlphaVelocity	=Mathery.RandomFloatNext(mRand, mAlphaVelocityMin, mAlphaVelocityMax);

			return	ret;
		}
	}
}
