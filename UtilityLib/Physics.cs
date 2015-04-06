using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;

namespace UtilityLib
{
	public class Physics
	{
		public struct PhyState
		{
			public Vector3		mPosition, mMomentum, mAngMomentum;
			public Quaternion	mOrient;

			public Vector3		mVelocity, mAngVelocity;
			public Quaternion	mSpin;

			public float	mMass, mInertiaTensor, mFriction;


			internal void Recalculate()
			{
				mVelocity		=mMomentum / mMass;
				mAngVelocity	=mAngMomentum / mInertiaTensor;

				mOrient.Normalize();

				mSpin.X	=mAngVelocity.X;
				mSpin.Y	=mAngVelocity.Y;
				mSpin.Z	=mAngVelocity.Z;
				mSpin.W	=0f;

				mSpin	*=0.5f;
				mSpin	*=mOrient;
			}
		}

		public struct Derivative
		{
			public Vector3		mVelocity, mForce, mTorque;
			public Quaternion	mSpin;
		}

		PhyState	mPrev, mCur;

		//external forces
		Vector3	mExternalDV;


		public Physics()
		{
			mCur.mMomentum	=Vector3.Zero;
			mCur.mOrient	=Quaternion.Identity;
		}


		public void Update(float dt)
		{
			mPrev	=mCur;

			Integrate(ref mCur, dt);
		}


		public void ClearVerticalMomentum()
		{
			mCur.mMomentum.Y	=0f;
		}


		public void SetProps(float mass, float it, float friction)
		{
			mCur.mMass			=mass;
			mCur.mInertiaTensor	=it;
			mCur.mFriction		=friction;

			mCur.Recalculate();
			mPrev	=mCur;
		}


		public void SetFriction(float friction)
		{
			mCur.mFriction	=friction;
		}


		public void SetPosition(Vector3 pos)
		{
			mCur.mPosition	=pos;
		}


		public void ApplyForce(Vector3 force)
		{
			mExternalDV	+=force / mCur.mMass;
		}


		public Vector3 GetVelocity()
		{
			return	mCur.mVelocity;
		}


		public Vector3 GetPosition()
		{
			return	mCur.mPosition;
		}


		public Quaternion GetOrient()
		{
			return	mCur.mOrient;
		}


		void Integrate(ref PhyState state, float dt)
		{
			Derivative	a	=evaluate(state, dt);
			Derivative	b	=evaluate(ref state, dt*0.5f, a);
			Derivative	c	=evaluate(ref state, dt*0.5f, b);
			Derivative	d	=evaluate(ref state, dt, c);
		
			state.mPosition		+=1.0f/6.0f * dt * (a.mVelocity + 2.0f*(b.mVelocity + c.mVelocity) + d.mVelocity);
			state.mMomentum		+=1.0f/6.0f * dt * (a.mForce + 2.0f*(b.mForce + c.mForce) + d.mForce);
			state.mOrient		+=1.0f/6.0f * dt * (a.mSpin + 2.0f*(b.mSpin + c.mSpin) + d.mSpin);
			state.mAngMomentum	+=1.0f/6.0f * dt * (a.mTorque + 2.0f*(b.mTorque + c.mTorque) + d.mTorque);

			state.Recalculate();
		}


		Derivative evaluate(PhyState state, float dt)
		{
			Derivative	output;
			output.mVelocity	=state.mVelocity;
			output.mSpin		=state.mSpin;

			forces(state, out output.mForce, out output.mTorque);

			return	output;
		}


		Derivative evaluate(ref PhyState state, float dt, Derivative derivative)
		{
			state.mPosition		+=derivative.mVelocity * dt;
			state.mMomentum		+=derivative.mForce * dt;
			state.mOrient		+=derivative.mSpin * dt;
			state.mAngMomentum	+=derivative.mTorque * dt;
			state.Recalculate();
		
			Derivative	output;
			output.mVelocity	=state.mVelocity;
			output.mSpin		=state.mSpin;

			forces(state, out output.mForce, out output.mTorque);

			return	output;
		}


		void forces(PhyState state, out Vector3 force, out Vector3 torque)
		{
			//friction
			Vector3	frictionForce	=state.mMass * state.mVelocity;
			frictionForce	*=-state.mFriction;

			force	=mExternalDV + frictionForce;
			torque	=Vector3.Zero;

			//clear after update
			mExternalDV	=Vector3.Zero;
		}


		void forcesRandomish(PhyState state, float t, out Vector3 force, out Vector3 torque)
		{
			force	=-10 * state.mPosition;
	
			// sine force to add some randomness to the motion
			force.X += 10f * (float)Math.Sin(t * 0.9f + 0.5f);
			force.Y += 11f * (float)Math.Sin(t * 0.5f + 0.4f);
			force.Z += 12f * (float)Math.Sin(t * 0.7f + 0.9f);

			// sine torque to get some spinning action
			torque.X = 1.0f * (float)Math.Sin(t * 0.9f + 0.5f);
			torque.Y = 1.1f * (float)Math.Sin(t * 0.5f + 0.4f);
			torque.Z = 1.2f * (float)Math.Sin(t * 0.7f + 0.9f);

			// damping torque so we dont spin too fast
			torque	-=0.2f * state.mAngVelocity;
		}
	}
}
