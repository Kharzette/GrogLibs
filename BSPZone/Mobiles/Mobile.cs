﻿using System;
using System.Text;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using UtilityLib;
using InputLib;
using Vortice.Mathematics;


namespace BSPZone;

public class Mobile
{
	public enum LocomotionState
	{
		Idle, Walk, WalkBack, WalkLeft, WalkRight, Stumbling
	}

	//owner of this mobile
	public readonly object	mParent;

	//zone to collide against
	Zone	mZone;

	//model index standing on, -1 for midair or sliding down
	int	mModelOn;

	//true if there is solid footing underneath
	bool	mbOnGround;

	//true if there's something underfoot, but not good traction
	bool	mbBadFooting;

	//plane underfoot if on ground or bad footing
	ZonePlane	mUnderFoot;

	//true if this object can be pushed by models
	bool	mbPushable;

	//push velocity from riding on or being pushed by models
	Vector3	mPushVelocity;

	//position
	Vector3	mPosition;

	//collision box, sized for this mobile
	BoundingBox	mBox;
	Vector3		mBoxMiddleOffset;

	//offset from the boundingbox center to the eye position
	Vector3		mEyeHeight;

	//constants
	const float	MinCamDist			=10f;
	const float	CamCollisionRadius	=4f;


	public Mobile(object owner, float boxWidth, float boxHeight,
		float eyeHeight, bool bPushable)
	{
		mParent				=owner;
		mbPushable			=bPushable;

		SetBoxShape(boxWidth, boxHeight, eyeHeight);
	}


	public void SetBoxShape(float boxWidth, float boxHeight, float eyeHeight)
	{
		mBox				=Misc.MakeBox(boxWidth, boxHeight);
		mEyeHeight			=Vector3.UnitY * eyeHeight;
		mBoxMiddleOffset	=Vector3.UnitY * ((mBox.Max.Y - mBox.Min.Y) * 0.5f);
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


	public void DropToGround(bool bUseModels)
	{
		mPosition	=mZone.DropToGround(mBox, mPosition, bUseModels);

		SetFooting();
	}


	public void SetFooting()
	{
		Vector3	donutCare;
		bool	bStairs;

		//do a nothing move to establish footing and ground plane
		//mainly used for pathfinding connection tests
		mbOnGround	=mZone.BipedMoveBox(mBox, mPosition, mPosition,
			mbOnGround, true, true, false, out mUnderFoot, out donutCare,
			out bStairs, out mbBadFooting, ref mModelOn);
	}


	public bool IsOnGround()
	{
		return	mbOnGround;
	}


	public bool IsBadFooting()
	{
		return	mbBadFooting;
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
		return	new BoundingBox(mBox.Min + mPosition, mBox.Max + mPosition);
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


	public LocomotionState DetermineLocoState(Vector3 moveDelta, Vector3 camForward)
	{
		LocomotionState	ls	=LocomotionState.Idle;

		if(mbBadFooting)
		{
			return	LocomotionState.Stumbling;
		}

		float	moveLen	=moveDelta.Length();
		if(moveLen > 0.001f)
		{
			//need a leveled out forward direction
			Vector3	dir		=camForward;
			Vector3	side	=Vector3.Cross(dir, Vector3.UnitY);
			dir				=Vector3.Cross(side, Vector3.UnitY);

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
		Matrix4x4	orientation	=
			Matrix4x4.CreateRotationY(MathHelper.ToRadians(ps.Yaw)) *
			Matrix4x4.CreateRotationX(MathHelper.ToRadians(ps.Pitch)) *
			Matrix4x4.CreateRotationZ(MathHelper.ToRadians(ps.Roll));

		//transpose to get it out of wacky camera land
		orientation		=Matrix4x4.Transpose(orientation);

		//grab transpose forward
		Vector3	forward	=orientation.Forward();

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
		Mathery.TransformCoordinate(shoulderOffset, orientation, out shoulderOffset);

		//for the third person camera, back the position out
		//along the updated forward vector

		//todo replace player steering zoom factor
		float	psZoom	=5f;
		mobCamPos	=camPos + (-forward * psZoom) + shoulderOffset;

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
		Debug.Assert(msDelta > 0);

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
	}


	//ins and outs are ground based
	public void Move(Vector3 endPos, float msDelta, bool bWorldOnly,
		bool bFly, bool bMoveAlongGround, bool bDistCheck,
		out Vector3 retPos, out Vector3 camPos)
	{
		Debug.Assert(msDelta > 0f);

		retPos	=Vector3.Zero;
		camPos	=Vector3.Zero;

		if(mZone == null)
		{
			return;
		}

		if(bFly)
		{
			mPosition	=endPos + mBoxMiddleOffset;
			retPos		=endPos;
			camPos		=-endPos;
			if(mbPushable)
			{
				mZone.UpdatePushable(this, mPosition, mModelOn);
			}
			return;
		}

		//adjust to box middle
		endPos	+=mBoxMiddleOffset;

		Vector3	moveDelta	=endPos - mPosition;

		//adjust onto the ground plane if desired and good footing
		if(mbOnGround && bMoveAlongGround
			&& mUnderFoot.mNormal != Vector3.Zero)
		{
			mUnderFoot.MoveAlong(ref moveDelta);
		}

		endPos	=mPosition + moveDelta;

		//move it through the bsp
		bool	bUsedStairs	=false;

		mbOnGround	=mZone.BipedMoveBox(mBox, mPosition, endPos,
			mbOnGround, bWorldOnly, bDistCheck, mbOnGround && bMoveAlongGround,
			out mUnderFoot, out endPos, out bUsedStairs,
			out mbBadFooting, ref mModelOn);

		retPos	=endPos - mBoxMiddleOffset;

		//pop up to eye height, and negate
		camPos	=-(endPos - mBoxMiddleOffset + mEyeHeight);

		mPosition	=endPos;
		if(mbPushable)
		{
			mZone.UpdatePushable(this, mPosition, mModelOn);
		}
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
		Vector3	endPos		=mPosition + moveDelta;

		//move it through the bsp
		ZonePlane	groundPlane;
		bool		bUsedStairs	=false;
		int			modelOn		=-1;
		bool		bBadFooting	=false;
		bool		bOnGround	=mZone.BipedMoveBox(mBox, mPosition, endPos,
			mbOnGround, false, true, true,
			out groundPlane, out endPos,
			out bUsedStairs, out bBadFooting, ref modelOn);

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


	public bool CheckPosition()
	{
		ZonePlane	zp	=ZonePlane.Blank;
		return	mZone.IntersectBoxModel(mBox, mPosition, 0, ref zp);
	}


	//return false if push leaves mobile in solid
	internal bool Push(Vector3 delta, int modelIndex)
	{
		//add to push velocity
		mPushVelocity	+=delta;

		//grab starting position
		Vector3	startPos	=mPosition;

		Vector3	pushedTo, camTo;
		Move(delta + GetGroundPos(), 1f,
			true, false, false, false, out pushedTo, out camTo);

		SetGroundPos(pushedTo);

		//see if still intersecting
		ZonePlane	hitPlane	=ZonePlane.Blank;
		if(mZone.IntersectBoxModel(mBox, mPosition, modelIndex, ref hitPlane))
		{
			//try to resolve
			Vector3	resolvePos;
			int		modelOn;
			bool	bBadFooting;
			if(mZone.ResolvePosition(mBox, mPosition, out mUnderFoot,
				out resolvePos, out bBadFooting, out modelOn))
			{
				mPosition	=resolvePos;
			}
			else
			{
				return	false;
			}
		}

		SetFooting();

		return	mbOnGround;
	}


	internal void ClearPushVelocity()
	{
		mPushVelocity	=Vector3.Zero;
	}


	public UInt32 GetWorldContents()
	{
		return	mZone.GetWorldContents(mPosition);
	}
}