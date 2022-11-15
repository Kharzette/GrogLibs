using System;
using System.Numerics;
using System.Collections.Generic;
using UtilityLib;
using System.Diagnostics;


namespace ParticleLib
{
	public class Emitter
	{
		//if these change, modify Entities.qrk to match
		public enum Shapes
		{
			//todo, filled or solid (right now is solid)
			Point, Sphere, Box, Line, Plane
		}

		//randomizer
		Random	mRand	=new Random();

		//basic info
		public Vector3	mPosition;
		public float	mStartSize;
		public Vector4	mStartColor;
		public float	mEmitMS;
		public Shapes	mShape;
		public float	mShapeSize;
		public Vector3	mLineAxis;	//axis for the "Line" emit shape

		int	mMaxParticles;
		int	mCurNumParticles;

		//particle behaviour
		//velocities in units per second
		public float	mRotationalVelocityMin, mRotationalVelocityMax;
		public float	mVelocityMin, mVelocityMax;
		public float	mSizeVelocityMin, mSizeVelocityMax;
		public Vector4	mColorVelocityMin, mColorVelocityMax;
		public int		mLifeMin, mLifeMax;
		public float	mGravityStrength;
		public Vector3	mGravityLocation;
		public float	mVelocityCap;	//a hard cap on velocity due to gravity etc

		//state data
		public bool	mbOn;
		bool		mbBuffer;
		float		mEmitFrac;
		int			mEmitted;

		//particle work buffers
		Particle	[]mParticles1;
		Particle	[]mParticles2;


		public Emitter(int maxParticles, Shapes shape, float shapeSize,
			Vector3 pos, Vector4 startColor,
			Vector3 gravPos, float gs,
			float startSize, float emitMS,
			float rotVelMin, float rotVelMax,
			float velMin, float velMax, float velCap,
			float sizeVelMin, float sizeVelMax,
			Vector4 colorVelMin, Vector4 colorVelMax,
			int lifeMin, int lifeMax)
		{
			mShape					=shape;
			mShapeSize				=shapeSize;
			mPosition				=pos;
			mStartColor				=startColor;
			mGravityLocation		=gravPos;
			mGravityStrength		=gs;
			mStartSize				=startSize;
			mEmitMS					=emitMS;
			mRotationalVelocityMin	=rotVelMin;
			mRotationalVelocityMax	=rotVelMax;
			mVelocityMin			=velMin;
			mVelocityMax			=velMax;
			mVelocityCap			=velCap;
			mSizeVelocityMin		=sizeVelMin;
			mSizeVelocityMax		=sizeVelMax;
			mLifeMin				=lifeMin;
			mLifeMax				=lifeMax;
			mColorVelocityMin		=colorVelMin;
			mColorVelocityMax		=colorVelMax;

			mLineAxis	=Vector3.UnitX;	//default

			mMaxParticles	=maxParticles;

			mParticles1	=new Particle[maxParticles];
			mParticles2	=new Particle[maxParticles];
		}


		internal void Activate(bool bOn)
		{
			mbOn		=bOn;
			mEmitFrac	=0f;
		}


		internal Particle []Update(float msDelta, out int numParticles)
		{
			Debug.Assert(msDelta > 0f);	//zero deltatimes are not good for this stuff

			numParticles	=0;

			//update existing
			Particle	[]buf	=(mbBuffer)? mParticles1 : mParticles2;
			Particle	[]buf2	=(mbBuffer)? mParticles2 : mParticles1;

			mbBuffer	=!mbBuffer;

			int	idx	=0;
			for(int i=0;i < mCurNumParticles;i++)
			{
				if(!buf[i].Update(msDelta, mGravityLocation, mGravityStrength, mVelocityCap))
				{
					buf2[idx++]	=buf[i];
				}
			}

			if(mbOn)
			{
				float	emitCount	=msDelta * mEmitMS;

				//fractional progress
				mEmitFrac	+=emitCount;

				int	newParticles	=(int)(mEmitFrac);

				//subtract off int amount
				mEmitFrac	-=(int)mEmitFrac;

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

			numParticles		=idx;
			mCurNumParticles	=idx;

			if(idx == 0)
			{
				return	null;
			}

			return	buf2;
		}


		Vector3	PositionForShape()
		{
			if(mShape == Shapes.Point)
			{
				return	mPosition;
			}

			Vector3	ret			=Vector3.Zero;
			float	sizeOverTwo	=mShapeSize * 0.5f;

			if(mShape == Shapes.Box)
			{
				ret.X	=Mathery.RandomFloatNext(mRand, -sizeOverTwo, sizeOverTwo);
				ret.Y	=Mathery.RandomFloatNext(mRand, -sizeOverTwo, sizeOverTwo);
				ret.Z	=Mathery.RandomFloatNext(mRand, -sizeOverTwo, sizeOverTwo);
			}
			else if(mShape == Shapes.Line)
			{
				ret	=mLineAxis * Mathery.RandomFloatNext(mRand, -sizeOverTwo, sizeOverTwo);
			}
			else if(mShape == Shapes.Plane)
			{
				ret.X	=Mathery.RandomFloatNext(mRand, -sizeOverTwo, sizeOverTwo);
				ret.Z	=Mathery.RandomFloatNext(mRand, -sizeOverTwo, sizeOverTwo);
			}
			else if(mShape == Shapes.Sphere)
			{
				ret	=Mathery.RandomDirection(mRand);
				ret	*=Mathery.RandomFloatNext(mRand, -sizeOverTwo, sizeOverTwo);
			}

			ret	+=mPosition;

			return	ret;
		}


		Particle Emit()
		{
			Particle	ret	=new Particle();

			ret.mPosition		=PositionForShape();
			ret.mSize			=mStartSize;
			ret.mRotation		=0;
			ret.mColor			=mStartColor;
			ret.mLifeRemaining	=mRand.Next(mLifeMin, mLifeMax);

			ret.mVelocity	=Mathery.RandomDirection(mRand)
				* Mathery.RandomFloatNext(mRand, mVelocityMin, mVelocityMax);

			ret.mColorVelocity.X	=Mathery.RandomFloatNext(mRand,
				mColorVelocityMin.X, mColorVelocityMax.X);
			ret.mColorVelocity.Y	=Mathery.RandomFloatNext(mRand,
				mColorVelocityMin.Y, mColorVelocityMax.Y);
			ret.mColorVelocity.Z	=Mathery.RandomFloatNext(mRand,
				mColorVelocityMin.Z, mColorVelocityMax.Z);
			ret.mColorVelocity.W	=Mathery.RandomFloatNext(mRand,
				mColorVelocityMin.W, mColorVelocityMax.W);

			ret.mRotationalVelocity	=Mathery.RandomFloatNext(mRand, mRotationalVelocityMin, mRotationalVelocityMax);

			ret.mSizeVelocity	=Mathery.RandomFloatNext(mRand, mSizeVelocityMin, mSizeVelocityMax);

			return	ret;
		}


		internal string GetEntityFields(string entity)
		{
			//quark doesn't like vector4s
			Vector3	colorMin	=new Vector3(mColorVelocityMin.X, mColorVelocityMin.Y, mColorVelocityMin.Z);
			Vector3	colorMax	=new Vector3(mColorVelocityMax.X, mColorVelocityMax.Y, mColorVelocityMax.Z);
			Vector3	startColXYZ	=new Vector3(mStartColor.X, mStartColor.Y, mStartColor.Z);

			ParticleBoss.AddField(ref entity, "origin", Misc.VectorToString(mPosition));
			ParticleBoss.AddField(ref entity, "start_color", Misc.VectorToString(startColXYZ));
			ParticleBoss.AddField(ref entity, "start_alpha", "" + Misc.FloatToString(mStartColor.W, 2));
			ParticleBoss.AddField(ref entity, "max_particles", "" + mMaxParticles);
			ParticleBoss.AddField(ref entity, "shape", "" + (int)mShape);
			ParticleBoss.AddField(ref entity, "shape_size", "" + Misc.FloatToString(mShapeSize, 1));
			ParticleBoss.AddField(ref entity, "grav_loc", "" + Misc.VectorToString(mGravityLocation));
			ParticleBoss.AddField(ref entity, "grav_strength", "" + Misc.FloatToString(mGravityStrength, 3));
			ParticleBoss.AddField(ref entity, "start_size", "" + Misc.FloatToString(mStartSize, 1));
			ParticleBoss.AddField(ref entity, "emit_ms", "" + Misc.FloatToString(mEmitMS, 3));
			ParticleBoss.AddField(ref entity, "velocity_min", "" + Misc.FloatToString(mVelocityMin * 1000f, 2));
			ParticleBoss.AddField(ref entity, "velocity_max", "" + Misc.FloatToString(mVelocityMax * 1000f, 2));
			ParticleBoss.AddField(ref entity, "size_velocity_min", "" + Misc.FloatToString(mSizeVelocityMin * 1000f, 2));
			ParticleBoss.AddField(ref entity, "size_velocity_max", "" + Misc.FloatToString(mSizeVelocityMax * 1000f, 2));
			ParticleBoss.AddField(ref entity, "spin_velocity_min", "" + Misc.FloatToString(mRotationalVelocityMin * 1000f, 2));
			ParticleBoss.AddField(ref entity, "spin_velocity_max", "" + Misc.FloatToString(mRotationalVelocityMax * 1000f, 2));
			ParticleBoss.AddField(ref entity, "alpha_velocity_min", "" + Misc.FloatToString(mColorVelocityMin.W * 10000f, 2));
			ParticleBoss.AddField(ref entity, "alpha_velocity_max", "" + Misc.FloatToString(mColorVelocityMax.W * 10000f, 2));
			ParticleBoss.AddField(ref entity, "lifetime_min", "" + Misc.FloatToString(mLifeMin / 1000f, 2));
			ParticleBoss.AddField(ref entity, "lifetime_max", "" + Misc.FloatToString(mLifeMax / 1000f, 2));
			ParticleBoss.AddField(ref entity, "color_velocity_min", Misc.VectorToString(colorMin * 10000f, 2));
			ParticleBoss.AddField(ref entity, "color_velocity_max", Misc.VectorToString(colorMax * 10000f, 2));
			ParticleBoss.AddField(ref entity, "activated", (mbOn)? "1" : "0");
			ParticleBoss.AddField(ref entity, "velocity_cap", "" + Misc.FloatToString(mVelocityCap, 2));

			return	entity;
		}
	}
}
