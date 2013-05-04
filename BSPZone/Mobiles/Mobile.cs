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
		public enum LocomotionState
		{
			Idle, Walk, WalkBack, WalkLeft, WalkRight
		}

		//owner of this mobile
		public readonly object	mParent;

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
		BoundingBox	mBox;
		Vector3		mBoxMiddleOffset;

		//offset from the boundingbox center to the eye position
		Vector3		mEyeHeight;

		//camera stuff if needed
		BoundingBox	mCamBox;

		//constants
		const float MidAirMoveScale	=0.03f;
		const float	JumpVelocity	=1.5f;
		const float	Friction		=0.6f;
		const float	MinCamDist		=10f;


		public Mobile(object owner, float boxWidth, float boxHeight,
			float eyeHeight, bool bPushable, TriggerHelper th)
		{
			mParent				=owner;
			mBox				=Misc.MakeBox(boxWidth, boxHeight);
			mEyeHeight			=Vector3.UnitY * eyeHeight;
			mBoxMiddleOffset	=Vector3.UnitY * ((mBox.Max.Y - mBox.Min.Y) * 0.5f);
			mbPushable			=bPushable;
			mTHelper			=th;

			//small box for camera collision
			mCamBox	=Misc.MakeBox(2f, 2f);
		}


		public void SetZone(Zone z)
		{
			mZone	=z;
			mZone.RegisterPushable(this, mBox, mPosition, mModelOn);

			//clear state
			mbOnGround	=false;
		}


		//for initial start pos and teleports
		public void SetGroundPosition(Vector3 pos)
		{
			mPosition	=pos + mBoxMiddleOffset;
		}


		public BoundingBox GetBounds()
		{
			return	mBox;
		}


		public BoundingBox GetTransformedBound()
		{
			BoundingBox	ret	=mBox;

			ret.Min	+=mPosition;
			ret.Max	+=mPosition;

			return	ret;
		}


		//helps AI come to a stop without sliding
		public void KillVelocity()
		{
			mVelocity	=Vector3.Zero;
		}


		public Vector3 GetGroundPosition()
		{
			return	mPosition - mBoxMiddleOffset;
		}


		public Vector3 GetMiddlePosition()
		{
			return	mPosition;
		}


		public Vector3 GetEyePos()
		{
			return	(mPosition - mBoxMiddleOffset + mEyeHeight);
		}


		public void Jump()
		{
			if(mbOnGround)
			{
				mVelocity	+=Vector3.UnitY * JumpVelocity;
			}
		}


		public LocomotionState DetermineLocoState(Vector3 moveDelta, Vector3 camForward)
		{
			LocomotionState	ls	=LocomotionState.Idle;

			if(moveDelta.LengthSquared() > 0.001f)
			{
				//need a leveled out forward direction
				Vector3	dir		=camForward;
				Vector3	side	=Vector3.Cross(dir, Vector3.Up);
				dir				=Vector3.Cross(side, Vector3.Up);

				//check the direction moving vs axis
				moveDelta.Normalize();
				float	forwardDot	=Vector3.Dot(dir, moveDelta);
				float	leftDot		=Vector3.Dot(side, moveDelta);

				if(Math.Abs(forwardDot) > Math.Abs(leftDot))
				{
					if(forwardDot < 0f)
					{
						ls	=LocomotionState.Walk;
					}
					else
					{
						ls	=LocomotionState.WalkBack;
					}
				}
				else
				{
					if(leftDot < 0f)
					{
						ls	=LocomotionState.WalkLeft;
					}
					else
					{
						ls	=LocomotionState.WalkRight;
					}
				}
			}

			return	ls;
		}


		//takes a campos from a Move output and adjusts it to work
		//for a typical third person camera with reticle aiming
		public void ThirdPersonOrient(PlayerSteering ps, Vector3 camPos,
			Vector3 shoulderOffset,	//adjustment to move the reticle slightly to the side
			out Vector3 mobForward,	//returns a valid direction for a character mesh
			out Vector3 mobCamPos,	//the new adjusted camera position
			out bool bFirstPerson)	//if something is blocking, pop to first person
		{
			//grab the orientation from the just updated player steering
			//the camera would be a frame behind here as it ordinarily
			//hasn't been updated yet
			Matrix	orientation	=
				Matrix.CreateRotationY(MathHelper.ToRadians(ps.Yaw)) *
				Matrix.CreateRotationX(MathHelper.ToRadians(ps.Pitch)) *
				Matrix.CreateRotationZ(MathHelper.ToRadians(ps.Roll));

			//transpose to get it out of wacky camera land
			orientation		=Matrix.Transpose(orientation);

			//grab transpose forward
			Vector3	forward	=orientation.Forward;

			//level out for mobforward
			mobForward		=forward;
			mobForward.Y	=0f;

			//make sure valid
			float	len	=mobForward.Length();

			//should be valid so long as the pitchclamp
			//values are reasonable in playersteering
			Debug.Assert(len > 0f);
			if(len > 0f)
			{
				mobForward	/=len;
			}
			else
			{
				mobForward	=Vector3.UnitX;
			}

			//camera positions are always negated
			camPos	=-camPos;

			//transform the shoulder offset to get it into player space
			shoulderOffset	=Vector3.Transform(shoulderOffset, orientation);

			//for the third person camera, back the position out
			//along the updated forward vector
			mobCamPos	=camPos + (-forward * ps.Zoom) + shoulderOffset;

			Vector3		impacto		=Vector3.Zero;
			ZonePlane	planeHit	=ZonePlane.Blank;
			int			modelHit	=0;
			if(mZone.Trace_All(mCamBox, camPos, mobCamPos, ref modelHit, ref impacto, ref planeHit))
			{
				mobCamPos	=impacto;
			}

			Vector3	camRay	=mobCamPos - camPos;
			len				=camRay.Length();
				
			//if really short, just use first person
			if(len < MinCamDist)
			{
				mobCamPos		=camPos;
				bFirstPerson	=true;
			}
			else
			{
				bFirstPerson	=false;
			}
		}


		//ins and outs are ground based
		public void Move(Vector3 endPos, int msDelta,
			bool bAffectVelocity, bool bFly, bool bTriggerCheck,
			out Vector3 retPos, out Vector3 camPos)
		{
			retPos	=Vector3.Zero;
			camPos	=Vector3.Zero;

			//adjust to box middle
			endPos	+=mBoxMiddleOffset;

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
					mZone.UpdatePushable(this, mPosition, mModelOn);
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
			if(mZone.BipedMoveBox(mBox, mPosition, endPos, mbOnGround, out endPos, out bUsedStairs, ref mModelOn))
			{
				mbOnGround	=true;

				//on ground, friction velocity
				if(bAffectVelocity)
				{
					mVelocity	=endPos - mPosition;
					mVelocity	*=Friction;

					//clamp really small velocities
					Mathery.TinyToZero(ref mVelocity);

					//prevent stairsteps from launching the velocity
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

			retPos	=endPos - mBoxMiddleOffset;

			//pop up to eye height, and negate
			camPos	=-(endPos - mBoxMiddleOffset + mEyeHeight);

			//do a trigger check if requested
			if(bTriggerCheck)
			{
				mTHelper.CheckMobile(this, mBox,
					mPosition, endPos, msDelta);
			}

			mPosition	=endPos;
			if(mbPushable)
			{
				mZone.UpdatePushable(this, mPosition, mModelOn);
			}
		}


		public bool IsOnGround()
		{
			return	mbOnGround;
		}


		//try a simplified move to see if a position
		//can be reached without pathfinding
		//miss C++ const methods for something like this
		public bool TryMoveTo(Vector3 tryPos, float error)
		{
			if(mZone == null || !mbOnGround)
			{
				return	false;
			}

			Vector3	moveDelta	=(tryPos + mBoxMiddleOffset) - mPosition;
			Vector3	endPos		=mPosition + moveDelta;

			//move it through the bsp
			bool	bUsedStairs	=false;
			int		modelOn		=-1;
			bool	bOnGround	=mZone.BipedMoveBox(mBox, mPosition, endPos, mbOnGround,
									out endPos, out bUsedStairs, ref modelOn);

			//test distance without the Y
			tryPos.Y	=0f;
			endPos.Y	=0f;

			float	dist	=Vector3.Distance(tryPos, endPos);
			return	(dist < error);
		}


		public bool TryStandingSpot(Vector3 tryPos)
		{
			if(!mbOnGround)
			{
				return	false;
			}

			tryPos	+=mBoxMiddleOffset;

			int			modelHit	=0;
			Vector3		impacto		=Vector3.Zero;
			ZonePlane	planeHit	=ZonePlane.Blank;

			bool	bHit	=mZone.Trace_All(mBox, tryPos,
				tryPos + Vector3.UnitY, ref modelHit, ref impacto, ref planeHit);

			return	!bHit;
		}
	}
}
