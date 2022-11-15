using System;
using System.Numerics;
using System.Collections.Generic;
using UtilityLib;


namespace ParticleLib
{
	internal struct Particle
	{
		internal Vector3	mPosition;
		internal float		mSize;
		internal float		mRotation;
		internal float		mLifeRemaining;
		internal Vector4	mColor;

		internal Vector3	mVelocity;
		internal float		mRotationalVelocity;
		internal float		mSizeVelocity;
		internal Vector4	mColorVelocity;		//transparency in W


		//return true if expired
		internal bool Update(float msDelta,
			Vector3 gravLoc, float gravStr,
			float maxVelocity)
		{
			mLifeRemaining	-=msDelta;
			if(mLifeRemaining < 0)
			{
				return	true;
			}

			mPosition	+=(mVelocity * msDelta);
			mColor		+=(mColorVelocity * msDelta);
			mSize		+=(mSizeVelocity * msDelta);
			mRotation	+=(mRotationalVelocity * msDelta);

			Vector3	gravVec	=gravLoc - mPosition;

			float	gravDist	=gravVec.Length();

			//normalize
			gravVec	/=gravDist;

			if(gravDist < 1f)
			{
				gravDist	=1f;
			}
			gravVec	*=(gravStr / (gravDist * gravDist));

			mVelocity	+=(gravVec * msDelta) / 1000f;

			//clamp velocity
			float	velLen	=mVelocity.Length();

			if(velLen > maxVelocity)
			{
				mVelocity	*=(maxVelocity / velLen);
			}

			mSize	=MathUtil.Clamp(mSize, 0f, 10000f);
			mColor	=Mathery.ClampVector(mColor, Vector4.Zero, Vector4.One);

			return	false;
		}
	}
}
