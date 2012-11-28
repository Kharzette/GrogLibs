using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;


namespace ParticleLib
{
	internal struct Particle
	{
		internal Vector3	mPosition;
		internal float		mSize;
		internal float		mRotation;
		internal float		mAlpha;
		internal int		mLifeRemaining;

		internal Vector3	mVelocity;
		internal float		mRotationalVelocity;
		internal float		mSizeVelocity;
		internal float		mAlphaVelocity;


		//return true if expired
		internal bool Update(int msDelta, Vector3 gravity)
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

			mVelocity	+=(gravity * msDelta);

			mAlpha	=MathHelper.Clamp(mAlpha, 0f, 1f);
			mSize	=MathHelper.Clamp(mSize, 0f, 10000f);

			return	false;
		}
	}
}
