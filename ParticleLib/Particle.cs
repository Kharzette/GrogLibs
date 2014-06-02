using System;
using System.Collections.Generic;
using System.Text;
using SharpDX;


namespace ParticleLib
{
	internal struct Particle
	{
		internal Vector3	mPosition;
		internal float		mSize;
		internal float		mRotation;
		internal float		mAlpha;
		internal float		mLifeRemaining;

		internal Vector3	mVelocity;
		internal float		mRotationalVelocity;
		internal float		mSizeVelocity;
		internal float		mAlphaVelocity;


		//return true if expired
		internal bool Update(float msDelta, Vector3 gravity)
		{
			mLifeRemaining	-=msDelta;
			if(mLifeRemaining < 0)
			{
				return	true;
			}

			mPosition	+=(mVelocity * msDelta);
			mSize		+=(mSizeVelocity * msDelta);
			mRotation	+=(mRotationalVelocity * msDelta);
			mAlpha		+=(mAlphaVelocity * msDelta);

			mVelocity	+=(gravity * msDelta) / 1000f;

			mAlpha	=MathUtil.Clamp(mAlpha, 0f, 1f);
			mSize	=MathUtil.Clamp(mSize, 0f, 10000f);

			return	false;
		}
	}
}
