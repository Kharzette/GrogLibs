using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using SharpDX;
using UtilityLib;
using InputLib;


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

		//push velocity from riding on or being pushed by models
		Vector3	mPushVelocity;

		//position and momentum
		Vector3	mPosition;
		Vector3	mVelocity;

		//collision box, sized for this mobile
		BoundingBox	mBox;
		Vector3		mBoxMiddleOffset;

		//offset from the boundingbox center to the eye position
		Vector3		mEyeHeight;

		//constants
		const float MidAirMoveScale		=0.01f;
		const float	JumpVelocity		=1.25f;
		const float	Friction			=0.6f;
		const float	MinCamDist			=10f;
		const float	CamCollisionRadius	=4f;
		const float	GravityConstant		=4f / 1000f;


		public Mobile(object owner, float boxWidth, float boxHeight,
			float eyeHeight, bool bPushable, TriggerHelper th)
		{
			mParent				=owner;
			mbPushable			=bPushable;
			mTHelper			=th;

			SetBoxShape(boxWidth, boxHeight, eyeHeight);
		}


		public void SetBoxShape(float boxWidth, float boxHeight, float eyeHeight)
		{
			mBox				=Misc.MakeBox(boxWidth, boxHeight);
			mEyeHeight			=Vector3.UnitY * eyeHeight;
			mBoxMiddleOffset	=Vector3.UnitY * ((mBox.Maximum.Y - mBox.Minimum.Y) * 0.5f);
		}


		public void SetZone(Zone z)
		{
			mZone	=z;

			if(mbPushable)
			{
				mZone.RegisterPushable(this, mBox, mPosition, mModelOn);
			}

			//clear state
			mbOnGround	=false;
		}


		//for initial start pos and teleports
		public void SetGroundPos(Vector3 pos)
		{
			mPosition	=pos + mBoxMiddleOffset;
		}


		public int GetModelOn()
		{
			return	mModelOn;
		}


		public BoundingBox GetBounds()
		{
			return	mBox;
		}


		public BoundingBox GetTransformedBound()
		{
			BoundingBox	ret	=mBox;

			ret.Minimum	+=mPosition;
			ret.Maximum	+=mPosition;

			return	ret;
		}


		public Vector3 GetGroundPos()
		{
			return	mPosition - mBoxMiddleOffset;
		}


		public Vector3 GetMiddlePos()
		{
			return	mPosition;
		}


		public Vector3 GetEyePos()
		{
			return	(mPosition - mBoxMiddleOffset + mEyeHeight);
		}


		public void ApplyForce(Vector3 force)
		{
			mVelocity	+=force;
		}


		public void Jump()
		{
			if(mbOnGround)
			{
				if(mModelOn > 0)
				{
					mVelocity	+=(mPushVelocity * 0.5f);
				}
				mVelocity	+=Vector3.UnitY * JumpVelocity;
			}
		}


		//helps AI come to a stop without sliding beyond a goal
		public void KillVelocity()
		{
			mVelocity	=Vector3.Zero;
		}


		public LocomotionState DetermineLocoState(Vector3 moveDelta, Vector3 camForward)
		{
			LocomotionState	ls	=LocomotionState.Idle;

			float	moveLen	=moveDelta.Length();
			if(moveLen > 0.001f)
			{
				//need a leveled out forward direction
				Vector3	dir		=camForward;
				Vector3	side	=Vector3.Cross(dir, Vector3.Up);
				dir				=Vector3.Cross(side, Vector3.Up);

				//check the direction moving vs axis
				moveDelta	/=moveLen;

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
			bool bMoving,			//is the player moving?  if so pop direction to viewdir
			ref Vector3 mobForward,	//returns a valid direction for a character mesh
			out Vector3 mobCamPos,	//the new adjusted camera position
			out bool bFirstPerson)	//if something is blocking, pop to first person
		{
			//grab the orientation from the just updated player steering
			//the camera would be a frame behind here as it ordinarily
			//hasn't been updated yet
			Matrix	orientation	=
				Matrix.RotationY(MathUtil.DegreesToRadians(ps.Yaw)) *
				Matrix.RotationX(MathUtil.DegreesToRadians(ps.Pitch)) *
				Matrix.RotationZ(MathUtil.DegreesToRadians(ps.Roll));

			//transpose to get it out of wacky camera land
			orientation		=Matrix.Transpose(orientation);

			//grab transpose forward
			Vector3	forward	=orientation.Forward;

			if(bMoving)
			{
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
			}

			//camera positions are always negated
			camPos	=-camPos;

			//transform the shoulder offset to get it into player space
			shoulderOffset	=Vector3.TransformCoordinate(shoulderOffset, orientation);

			//for the third person camera, back the position out
			//along the updated forward vector
			mobCamPos	=camPos + (-forward * ps.Zoom) + shoulderOffset;

			Collision	col;
			if(mZone.TraceAll(CamCollisionRadius, null, camPos, mobCamPos, out col))
			{
				mobCamPos	=col.mIntersection;
			}

			Vector3	camRay	=mobCamPos - camPos;
			float	len2	=camRay.Length();
				
			//if really short, just use first person
			if(len2 < MinCamDist)
			{
				mobCamPos		=camPos;
				bFirstPerson	=true;
			}
			else
			{
				bFirstPerson	=false;
			}
		}


		public void MoveWithoutCollision(Vector3 endPos, int msDelta,
			bool bTriggerCheck, bool bDistCheck)
		{
			if(mZone == null)
			{
				return;
			}

			Vector3	moveDelta	=endPos - mPosition;

			mPosition	=endPos;
			if(mbPushable)
			{
				mZone.UpdatePushable(this, mPosition, mModelOn);
			}

			//do a trigger check if requested
			if(bTriggerCheck)
			{
				mTHelper.CheckMobile(this, mBox,
					mPosition, endPos, msDelta);
			}
		}


		//ins and outs are ground based
		public void Move(Vector3 endPos, int msDelta, bool bWorldOnly,
			bool bAffectVelocity, bool bFly, bool bTriggerCheck, bool bDistCheck,
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
				mVelocity.Y	-=(GravityConstant * msDelta);	//gravity
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
			if(mZone.BipedMoveBox(mBox, mPosition, endPos, mbOnGround, bWorldOnly, bDistCheck,
				out endPos, out bUsedStairs, ref mModelOn))
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
			if(mZone == null)
			{
				return	false;
			}

			Vector3	moveDelta	=(tryPos + mBoxMiddleOffset) - mPosition;

			//clear movedelta y so the move is flat
			moveDelta.Y	=0f;

			Vector3	endPos		=mPosition + moveDelta;

			//move it through the bsp
			bool	bUsedStairs	=false;
			int		modelOn		=-1;
			bool	bOnGround	=mZone.BipedMoveBox(mBox, mPosition, endPos, mbOnGround, false, true,
									out endPos, out bUsedStairs, ref modelOn);

			//test distance without the Y
			tryPos.Y	=0f;
			endPos.Y	=0f;

			float	dist	=Vector3.Distance(tryPos, endPos);
			return	(dist < error);
		}


		public bool TrySphere(Vector3 tryPos, float radius, bool bModelsToo)
		{
			tryPos.Y	+=radius;

			bool	bHit	=false;

			Collision	col;
			if(bModelsToo)
			{
				bHit	=mZone.TraceAll(radius, null, tryPos, tryPos + Vector3.UnitY, out col);
			}
			else
			{
				RayTrace	rt	=new RayTrace(tryPos, tryPos + Vector3.UnitY);
				rt.mRadius		=radius;

				bHit	=mZone.TraceNode(rt, tryPos, tryPos + Vector3.UnitY, 0);
			}

			return	!bHit;
		}


		//return false if push leaves mobile in solid
		internal bool Push(Vector3 delta, int modelIndex)
		{
			//add to push velocity
			mPushVelocity	+=delta;

			//grab starting position
			Vector3	startPos	=mPosition;

			Vector3	pushedTo, camTo;
			Move(delta + GetGroundPos(), 1,
				true, false, false, true, false, out pushedTo, out camTo);

			SetGroundPos(pushedTo);

			//see if still intersecting
			ZonePlane	hitPlane	=ZonePlane.Blank;
			if(mZone.IntersectBoxModel(mBox, mPosition, modelIndex, ref hitPlane))
			{
				//try to resolve
				Vector3	resolvePos;
				int		modelOn;
				if(mZone.ResolvePosition(mBox, mPosition, out resolvePos, out modelOn))
				{
					mPosition	=resolvePos;
				}
				else
				{
					return	false;
				}
			}
			return	true;
		}


		internal void ClearPushVelocity()
		{
			mPushVelocity	=Vector3.Zero;
		}
	}
}
