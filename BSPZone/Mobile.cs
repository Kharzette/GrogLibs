using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using UtilityLib;


namespace BSPZone
{
	public class Mobile
	{
		//zone to collide against
		Zone	mZone;

		//trigger helper
		TriggerHelper	mTHelper;

		//model index standing on, -1 for midair or sliding down
		int	mModelOn;

		//true if there is solid footing underneath
		bool	mbOnGround;

		//true if this object can be pushed by models
		bool	mbPushable;

		//position and momentum
		Vector3	mPosition;
		Vector3	mVelocity;

		//collision box, sized for this mobile
		BoundingBox	mCharBox;

		//offset from the boundingbox center to the eye position
		Vector3		mEyeHeight;

		//constants
		const float MidAirMoveScale	=0.01f;
		const float	JumpVelocity	=4.0f;


		public Mobile(float boxWidth, float boxHeight, float eyeHeight, bool bPushable, TriggerHelper th)
		{
			mCharBox	=Misc.MakeBox(boxWidth, boxHeight);
			mEyeHeight	=Vector3.UnitY * (eyeHeight + mCharBox.Min.Y);
			mbPushable	=bPushable;
			mTHelper	=th;
		}


		public void SetZone(Zone z)
		{
			mZone	=z;
		}


		public Vector3 GetPosition()
		{
			return	mPosition;
		}


		//for initial start pos and teleports
		public void SetPosition(Vector3 pos)
		{
			mPosition	=pos;
		}


		public void Jump()
		{
			if(mbOnGround)
			{
				mVelocity	+=Vector3.UnitY * JumpVelocity;
			}
		}


		public void Move(Vector3 endPos, int msDelta,
			bool bAffectVelocity, bool bFly, bool bTriggerCheck,
			out Vector3 retPos, out Vector3 camPos)
		{
			retPos	=Vector3.Zero;
			camPos	=Vector3.Zero;

			if(mZone == null)
			{
				return;
			}

			Vector3	moveDelta	=endPos - mPosition;

			if(bFly)
			{
				retPos	=mPosition	=endPos;
				camPos	=-mPosition;
				if(mbPushable)
				{
					mZone.SetPushable(mCharBox, mPosition, mModelOn);
				}
				return;
			}

			//if not on the ground, limit midair movement
			if(!mbOnGround && bAffectVelocity)
			{
				moveDelta.X	*=MidAirMoveScale;
				moveDelta.Z	*=MidAirMoveScale;
				mVelocity.Y	-=((9.8f / 1000.0f) * msDelta);	//gravity
			}

			//get ideal final position
			if(bAffectVelocity)
			{
				endPos	=mPosition + mVelocity + moveDelta;
			}
			else
			{
				endPos	=mPosition + moveDelta;
			}

			//move it through the bsp
			bool	bUsedStairs	=false;
			if(mZone.BipedMoveBox(mCharBox, mPosition, endPos, mbOnGround, out endPos, out bUsedStairs, ref mModelOn))
			{
				mbOnGround	=true;

				//on ground, friction velocity
				if(bAffectVelocity)
				{
					mVelocity	=endPos - mPosition;
					mVelocity	*=0.6f;

					//clamp really small velocities
					if(mVelocity.X < 0.001f && mVelocity.X > -0.001f)
					{
						mVelocity.X	=0.0f;
					}
					if(mVelocity.Y < 0.001f && mVelocity.Y > -0.001f)
					{
						mVelocity.Y	=0.0f;
					}
					if(mVelocity.Z < 0.001f && mVelocity.Z > -0.001f)
					{
						mVelocity.Z	=0.0f;
					}

					if(bUsedStairs)
					{
						mVelocity.Y	=0.0f;
					}
				}
			}
			else
			{
				if(bAffectVelocity)
				{
					mVelocity	=endPos - mPosition;
				}
				mbOnGround	=false;
			}

			retPos	=endPos;

			//pop up to eye height, and negate
			camPos	=-(endPos + mEyeHeight);

			//do a trigger check if requested
			if(bTriggerCheck)
			{
				mTHelper.CheckPlayer(mCharBox, mPosition, endPos, msDelta);
			}

			mPosition	=endPos;
			if(mbPushable)
			{
				mZone.SetPushable(mCharBox, mPosition, mModelOn);
			}
		}
	}
}
