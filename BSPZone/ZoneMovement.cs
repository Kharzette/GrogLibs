using System;
using System.IO;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using UtilityLib;


namespace BSPZone;

public partial class Zone
{
	//for a subdivision of a ray
	public struct TraceSegment
	{
		public Vector3	mStart, mEnd;
		public UInt32	mContents;
	}

	Dictionary<object, Pushable>	mPushables	=new Dictionary<object, Pushable>();

	const float	FootDistance	=1f;


	#region Bipedal Movement
	bool FootCheck(BoundingBox box, Vector3 footPos, float dist,
		out ZonePlane groundPlane, out int modelOn, out bool bBadFooting)
	{
		//see if the feet are still on the ground
		Vector3		footCheck	=footPos - Vector3.UnitY * dist;

		modelOn		=-1;
		bBadFooting	=false;
		groundPlane	=ZonePlane.Blank;

		RayTrace	rt	=new RayTrace(footPos, footCheck);

		rt.mBounds	=box;
		if(TraceNode(rt, footPos, footCheck, 0))
		{
			if(rt.mCollision.mbStartInside)
			{
				bBadFooting	=true;
				return	false;
			}

			if(rt.mCollision.mPlaneHit.IsGround())
			{
				bBadFooting	=false;
				modelOn		=0;
				groundPlane	=rt.mCollision.mPlaneHit;
				return	true;
			}
			bBadFooting	=true;
		}

		//try models
		Collision	col;
		if(TraceModelsBox(box, footPos, footCheck, out col))
		{
			if(col.mbStartInside)
			{
				bBadFooting	=true;
				return	false;
			}

			if(col.mPlaneHit.IsGround())
			{
				bBadFooting	=false;
				modelOn		=col.mModelHit;
				groundPlane	=col.mPlaneHit;
				return	true;
			}
			bBadFooting	=true;
		}
		return	false;
	}


	//check for small steps that a bipedal would be able to surmount
	bool StairMove(BoundingBox box, Vector3 start, Vector3 end, Vector3 stairAxis,
		float stepHeight, float originalLen, ref Vector3 stepPos,
		out ZonePlane stairPlane, out int modelOn)
	{
		//first trace up from the start point to world
		//to make sure there's head room
		Vector3	stairStart	=start;
		Vector3	stairEnd	=start + stairAxis * stepHeight;

		stairPlane	=ZonePlane.Blank;

		RayTrace	rt	=new RayTrace(stairStart, stairEnd);
		rt.mBounds		=box;
		if(TraceNode(rt, stairStart, stairEnd, 0))
		{
			//hit noggin, just use previous point
			modelOn	=-1;
			return	false;
		}

		//do nogginry check for models too
		Collision	col;
		if(TraceModelsBox(box, stairStart, stairEnd, out col))
		{
			//hit noggin, just use previous point
			modelOn	=-1;
			return	false;
		}

		//movebox from start step height to end step height
		stepPos	=Vector3.Zero;

		bool		bBadFooting;
		bool		bGroundStep	=MoveBox(box, start + stairAxis * stepHeight,
			end + stairAxis * stepHeight, false, false,
			out stairPlane,	out stepPos, out modelOn, out bBadFooting);

		if(!bGroundStep)
		{
			//trace down to world by step height x2 and make sure
			//we land on a ground surface
			Vector3	stepStart	=stepPos;
			Vector3	stepEnd		=stepPos - Vector3.UnitY * (stepHeight * 2f);
			rt.mOriginalStart	=stepStart;
			rt.mOriginalEnd		=stepEnd;

			bool	bStepDownWorldHit	=TraceNode(rt, stepStart, stepEnd, 0);

			float	stepDist	=0f;
			if(bStepDownWorldHit)
			{
				if(rt.mCollision.mPlaneHit.IsGround())
				{
					//landed on the ground
					stepPos		=rt.mCollision.mIntersection;
					stairPlane	=rt.mCollision.mPlaneHit;
					bGroundStep	=true;

					stairPlane.ReflectPosition(ref stepPos);

					stepDist	=Vector3.Distance(stepStart, stepPos);
				}
			}
			
			//try models
			if(TraceModelsBox(box, stepStart, stepEnd, out col))
			{
				if(col.mPlaneHit.IsGround())
				{
					//landed on the ground
					bool	bUseModelHit	=false;
					if(bStepDownWorldHit)
					{
						float	modDist	=col.mIntersection.Distance(stepStart);

						//take the shortest distance collision
						if(modDist < stepDist)
						{
							bUseModelHit	=true;
						}
					}
					else
					{
						bUseModelHit	=true;
					}

					if(bUseModelHit)
					{
						stepPos		=col.mIntersection;
						stairPlane	=rt.mCollision.mPlaneHit;
						bGroundStep	=true;
						col.mPlaneHit.ReflectPosition(ref stepPos);
					}
				}
			}
		}

		Vector3	moveVec	=stepPos - start;

		//bias original a little
		if(!bGroundStep || moveVec.Length() <= (originalLen * 1.1f))
		{
			//earlier move was better
			return	false;
		}

		return	true;
	}


	public void BipedMoveBoxDebug(BoundingBox box, Vector3 start, Vector3 end, List<Vector3> segments)
	{
		MoveBoxDebug(box, start, end, segments);
	}


	//returns true if on ground
	//this one assumes 2 legs, so navigates stairs
	public bool BipedMoveBox(BoundingBox box, Vector3 start, Vector3 end,
		bool bPrevOnGround, bool bWorldOnly, bool bDistCheck, bool bStepDown,
		out ZonePlane groundPlane, out Vector3 finalPos,
		out bool bUsedStairs, out bool bBadFooting, ref int modelOn)
	{
		bUsedStairs	=false;
		bBadFooting	=false;

		//first check if we are moving at all
		Vector3	moveVec	=end - start;
		float	delt	=moveVec.Length();
		if(bDistCheck && delt < Mathery.ANGLE_EPSILON)
		{
			//didn't move enough to bother
			finalPos	=start;
			return		FootCheck(box, start, FootDistance,
				out groundPlane, out modelOn, out bBadFooting);
		}

		//try the standard box move
		int		firstModelOn;
		bool	bGround	=MoveBox(box, start, end, bWorldOnly, bStepDown,
			out groundPlane, out finalPos, out firstModelOn, out bBadFooting);

		//see how far it went
		moveVec	=finalPos - start;

		float	deltMove	=moveVec.Length();
		if(delt / deltMove < 1.333f)
		{
			//3/4 the movement energy at least was expended
			//good enough
			modelOn	=firstModelOn;
			return	bGround;
		}

		//see if original movement is mostly non vertical
		moveVec	=end - start;
		moveVec.Normalize();
		float	vert	=Vector3.Dot(Vector3.UnitY, moveVec);

		if(vert > RampAngle || vert < -RampAngle)
		{
			//no need to try stairs if just falling or climbing
			modelOn	=firstModelOn;
			return	bGround;
		}

		//only attempt stair stepping if biped was previously on ground
		if(!bPrevOnGround)
		{
			modelOn	=firstModelOn;
			return	bGround;
		}

		//try a step at a quarter stair height
		//this can get us over cracks where the
		//returned plane is one of the extra axials
		Vector3		stairPos	=Vector3.Zero;
		ZonePlane	stairPlane;
		if(StairMove(box, start, end, Vector3.UnitY, StepHeight * 0.25f,
			deltMove, ref stairPos, out stairPlane, out modelOn))
		{
			finalPos	=stairPos;
			bUsedStairs	=true;
			groundPlane	=stairPlane;
			return		true;
		}

		//try a full step height
		if(StairMove(box, start, end, Vector3.UnitY, StepHeight,
			deltMove, ref stairPos, out stairPlane, out modelOn))
		{
			finalPos	=stairPos;
			bUsedStairs	=true;
			groundPlane	=stairPlane;
			return		true;
		}

		modelOn	=firstModelOn;

		if(bGround)
		{
			return	true;
		}

		//earlier move was better
		return	bGround;
	}


	//returns true if on ground
	//this is a move generated by being pushed by a model
	public void BipedModelPush(BoundingBox box, Vector3 start,
		Vector3 end, int modelIndex, ref Vector3 finalPos)
	{
		//first check if we are moving at all
		Vector3	moveVec	=end - start;
		float	delt	=moveVec.Length();
		if(delt <= 0f)
		{
			//didn't move enough to bother
			finalPos	=start;
		}

		//try the standard box move
		MoveBoxModelPush(box, start, end, modelIndex, ref finalPos);
	}
	#endregion


	#region Models Pushing
	public void RegisterPushable(Mobile context, BoundingBox box, Vector3 center, int modelOn)
	{
		if(mPushables.ContainsKey(context))
		{
			//if already in the list just update the data
			mPushables[context].mBox			=box;
			mPushables[context].mMobile			=context;
			mPushables[context].mModelOn		=modelOn;
			mPushables[context].mWorldCenter	=center;
		}
		else
		{
			mPushables.Add(context, new Pushable(context, box, center, modelOn));
		}
	}


	public void UnRegisterPushable(object context)
	{
		if(mPushables.ContainsKey(context))
		{
			mPushables.Remove(context);
		}
	}


	public void UpdatePushable(object context, Vector3 center, int modelOn)
	{
		if(!mPushables.ContainsKey(context))
		{
			return;
		}

		mPushables[context].mWorldCenter	=center;
		mPushables[context].mModelOn		=modelOn;
	}


	//positions should be in the middle base of the box
	//returns true if on the ground
	void MoveBoxModelPush(BoundingBox box,
		Vector3 start, Vector3 end, int modelIndex, ref Vector3 finalPos)
	{
		Vector3		impacto		=Vector3.Zero;
		int			i			=0;

		List<ZonePlane>	hitPlanes	=new List<ZonePlane>();

		for(i=0;i < MaxMoveBoxIterations;i++)
		{
			RayTrace	rt	=new RayTrace(start, end);
			ZoneModel	zm	=mZoneModels[modelIndex];

			//first trace up from the start point to world
			//to make sure there's head room
			rt.mBounds			=box;
			if(!TraceFakeOrientedBoxModel(rt, start, end, zm))
			{
				break;
			}

			if(rt.mCollision.mbStartInside)
			{
				break;	//in solid
			}

			ZonePlane	zp	=rt.mCollision.mPlaneHit;

			end	=zp.ReflectPosition(start, end);

			if(!hitPlanes.Contains(zp))
			{
				hitPlanes.Add(zp);
			}
		}

		finalPos	=end;
		if(i == MaxMoveBoxIterations)
		{
			//can't solve!
			finalPos	=start;
		}
	}
	#endregion


	#region Box Movement
	//returns true if move was a success and endpoint is safe
	bool MoveBoxWorld(BoundingBox box, Vector3 start, Vector3 end,
		out Vector3 finalPos)
	{
		int	i	=0;

		Vector3	newEnd		=end;
		Vector3	newStart	=start;

		List<ZonePlane>	hitPlanes	=new List<ZonePlane>();
		for(i=0;i < MaxMoveBoxIterations;i++)
		{
			RayTrace	rt	=new RayTrace(newStart, newEnd);
			rt.mBounds		=box;

			bool	bHitSomething	=TraceNode(rt, newStart, newEnd, 0);
			if(!bHitSomething)
			{
				break;
			}

			ZonePlane	zp	=rt.mCollision.mPlaneHit;

			if(rt.mCollision.mbStartInside)
			{
				//see if start is exactly plane on
				float	checkDist	=zp.Distance(newStart);
				if(checkDist == 0f)
				{
					zp.ReflectPosition(ref newStart);
					continue;
				}

				//TODO: report and solve via intersection
				//can't solve!
				finalPos	=start;
				return	false;
			}


			newEnd	=zp.ReflectPosition(newStart, newEnd);

			if(!hitPlanes.Contains(zp))
			{
				hitPlanes.Add(zp);
			}
		}

		finalPos	=newEnd;
		if(i == MaxMoveBoxIterations)
		{
			//this is usually caused by oblique planes causing
			//the reflected motion to bounce back and forth

			//get all the collision points along the motion
			List<Vector3>	contacts	=GetCollisions(hitPlanes, start, end);

			//get distance start to end
			float	motionDistance	=start.Distance(end);

			//stop at the closest contact to start
			float	bestDist	=float.MaxValue;
			Vector3	bestCon		=start;
			foreach(Vector3 con in contacts)
			{
				float	startDist	=con.Distance(start);
				float	endDist		=con.Distance(end);

				if(endDist > motionDistance)
				{
					//this contact is in front of the start!
					//this can only mean the starting position
					//was in solid
					finalPos	=start;
					return	false;
				}

				if(startDist < bestDist)
				{
					bestDist	=startDist;
					bestCon		=con;
				}
			}

			if(bestCon.Distance(start) <= Mathery.VCompareEpsilon)
			{
				//so close might as well use the start position
				finalPos	=start;
				return	true;
			}
			//push back along the vector a bit
			Vector3	motionVec	=end - start;

			motionVec	/=motionDistance;
			motionVec	*=Mathery.VCompareEpsilon;

			finalPos	=bestCon - motionVec;
			return	true;
		}
		return	true;
	}


	List<Vector3>	GetCollisions(List<ZonePlane> planes, Vector3 start, Vector3 end)
	{
		List<Vector3>	ret	=new List<Vector3>();

		foreach(ZonePlane zp in planes)
		{
			float	startDist	=zp.Distance(start);
			float	endDist		=zp.Distance(end);

			if(startDist == endDist)
			{
				continue;
			}

//				if(startDist == 0 || endDist == 0)
//				{
//					continue;
//				}

			if(startDist < 0 && endDist < 0)
			{
				continue;
			}

			if(startDist > 0 && endDist > 0)
			{
				continue;
			}

			float	ratio	=startDist / (startDist - endDist);

			ret.Add(start + ratio * (end - start));
		}

		return	ret;
	}


	//returns true if move was a success and endpoint is safe
	bool MoveBoxModels(BoundingBox box, Vector3 start, Vector3 end,
		out Vector3 finalPos)
	{
		List<ZonePlane>	hitPlanes	=new List<ZonePlane>();

		//do model collisions
		int	i;
		for(i=0;i < MaxMoveBoxIterations;i++)
		{
			Collision	col;

			bool	bHitSomething	=TraceModelsBox(box, start, end, out col);
			if(!bHitSomething)
			{
				break;
			}

			if(col.mbStartInside)
			{
				//TODO: report and solve via intersection
				//can't solve!  Use end of world collision
				finalPos	=end;
				return	false;
			}

			end	=col.mPlaneHit.ReflectPosition(start, end);

			if(!hitPlanes.Contains(col.mPlaneHit))
			{
				hitPlanes.Add(col.mPlaneHit);
			}
		}

		finalPos	=end;
		if(i == MaxMoveBoxIterations)
		{
			//can't solve!
			finalPos	=end;
			return	false;
		}
		return	true;
	}


	//use this whenever a collision returns in solid, it
	//will try to work free of the solid space
	public bool ResolvePosition(BoundingBox box, Vector3 pos,
		out ZonePlane groundPlane, out Vector3 finalPos,
		out bool bBadFooting, out int modelOn)
	{
		finalPos	=pos;
		bBadFooting	=false;

		int	i;
		for(i=0;i < MaxMoveBoxIterations;i++)
		{
			bool	bWorldIntersect	=false;
			bool	bModelIntersect	=false;

			//solve world via intersection
			ZonePlane	zp	=ZonePlane.Blank;
			if(IntersectBoxNode(box, pos, 0, ref zp))
			{
				bWorldIntersect	=true;
				zp.ReflectPosition(ref pos);
			}

			for(int j=1;j < mZoneModels.Length;j++)
			{
				if(mNonCollidingModels.Contains(i))
				{
					continue;	//don't bother vs triggers etc
				}

				ZonePlane	zp2	=ZonePlane.Blank;
				if(IntersectBoxModel(box, pos, j, ref zp))
				{
					bModelIntersect	=true;
					zp.ReflectPosition(ref pos);
				}
			}

			if(!bWorldIntersect && !bModelIntersect)
			{
				break;
			}
		}

		if(i < MaxMoveBoxIterations)
		{
			finalPos	=pos;

			return	FootCheck(box, finalPos, FootDistance,
				out groundPlane, out modelOn, out bBadFooting);
		}

		modelOn		=-1;
		groundPlane	=ZonePlane.Blank;

		return	false;
	}


	//positions should be in the middle of the box
	//returns true if on the ground
	public bool MoveBox(BoundingBox box, Vector3 start, Vector3 end,
		bool bWorldOnly, bool bStepDown,
		out ZonePlane groundPlane, out Vector3 finalPos,
		out int modelOn, out bool bBadFooting)
	{
		bBadFooting	=false;

		bool	bWorldOk	=MoveBoxWorld(box, start, end, out finalPos);
		if(!bWorldOk)
		{
			return	ResolvePosition(box, start, out groundPlane,
				out finalPos, out bBadFooting, out modelOn);
		}

		if(!bWorldOnly)
		{
			Vector3	modelPos;
			if(!MoveBoxModels(box, start, finalPos, out modelPos))
			{
				FootCheck(box, finalPos, FootDistance, out groundPlane, out modelOn, out bBadFooting);

				//if models are messed up, just use the world position
				//and report on ground so player can move
				//TODO: maybe rethink this after improving collision response
				modelOn		=0;
				bBadFooting	=false;

				return	true;
			}
			finalPos	=modelPos;
		}

		//move made, check footing
		bool	bGround	=FootCheck(box, finalPos, FootDistance,
			out groundPlane, out modelOn, out bBadFooting);

		if(!bGround && bStepDown)
		{
			//see if running down a ramp or stairs
			Collision	col;
			if(TraceAll(null, box, finalPos, finalPos + Vector3.UnitY * -StepDownHeight, out col))
			{
				if(!col.mbStartInside)
				{
					finalPos	=col.mIntersection;
					col.mPlaneHit.ReflectPosition(ref finalPos);
					return	FootCheck(box, finalPos, FootDistance,
						out groundPlane, out modelOn, out bBadFooting);
				}
			}
		}
		return	bGround;
	}
	#endregion


	#region Model Movement
	//this is an actual move, not a delta
	//returns false if blocked
	public bool MoveModelTo(int modelIndex, Vector3 pos)
	{
		if(mZoneModels.Length <= modelIndex)
		{
			return	true;
		}

		ZoneModel	zm	=mZoneModels[modelIndex];

		Vector3	oldPos		=zm.mPosition;

		//do the actual move
		zm.SetPosition(pos);

		foreach(KeyValuePair<object, Pushable> pa in mPushables)
		{
			//if any are riding on this model, move them too
			if(pa.Value.mModelOn == modelIndex)
			{
				if(!pa.Value.mMobile.Push(pos - oldPos, modelIndex))
				{
					//reset position
					zm.SetPosition(oldPos);
					//also need to reverse the model's motion here
					return	false;
				}
			}
		}

		if(!CollideModel(modelIndex))
		{
			//reset position
			zm.SetPosition(oldPos);

			//also need to reverse the model's motion here
			return	false;
		}
		return	true;
	}


	//return false if blocked
	public bool RotateModelX(int modelIndex, float degrees)
	{
		if(mZoneModels.Length <= modelIndex)
		{
			return	true;
		}

		if(degrees == 0f)
		{
			return	true;
		}

		ZoneModel	zm	=mZoneModels[modelIndex];

		//do the actual rotation
		zm.RotateX(degrees);

		if(!CollideModel(modelIndex))
		{
			//reset rotation
			zm.RotateX(-degrees);

			//also need to reverse the model's motion here
			return	false;
		}
		return	true;
	}


	//return false if blocked
	public bool RotateModelY(int modelIndex, float degrees)
	{
		if(mZoneModels.Length <= modelIndex)
		{
			return	true;
		}

		if(degrees == 0f)
		{
			return	true;
		}

		ZoneModel	zm	=mZoneModels[modelIndex];

		Matrix	oldMatInv	=zm.mInvertedTransform;

		//do the actual rotation
		zm.RotateY(degrees);

		Matrix	newMat		=zm.mTransform;

		//stuff rotating in y, bipeds should be able to stand on it
		//as it moves, might look odd at high speeds
		foreach(KeyValuePair<object, Pushable> pa in mPushables)
		{
			//if any are riding on this model, move them too
			if(pa.Value.mModelOn == modelIndex)
			{
				//get riding on position relative to model's previous frame position
				Vector3	start	=Vector3.TransformCoordinate(pa.Value.mWorldCenter, oldMatInv);

				//transform by this frame's mat
				Vector3	end	=Vector3.TransformCoordinate(start, newMat);

				//get delta
				end	-=pa.Value.mWorldCenter;

				if(end == Vector3.Zero)
				{
					continue;
				}

				if(!pa.Value.mMobile.Push(end, modelIndex))
				{
					//reset rotation
					zm.RotateY(-degrees);

					//also need to reverse the model's motion here
					return	false;
				}
			}
		}

		if(!CollideModel(modelIndex))
		{
			//reset rotation
			zm.RotateY(-degrees);

			//also need to reverse the model's motion here
			return	false;
		}
		return	true;
	}


	//return false if blocked
	public bool RotateModelZ(int modelIndex, float degrees)
	{
		if(mZoneModels.Length <= modelIndex)
		{
			return	true;
		}

		if(degrees == 0f)
		{
			return	true;
		}

		ZoneModel	zm	=mZoneModels[modelIndex];

		//do the actual rotation
		zm.RotateZ(degrees);

		if(!CollideModel(modelIndex))
		{
			//reset rotation
			zm.RotateZ(-degrees);

			//also need to reverse the model's motion here
			return	false;
		}
		return	true;
	}


	//return false if a push leaves a pushable in solid
	bool CollideModel(int modelIndex)
	{
		foreach(KeyValuePair<object, Pushable> pa in mPushables)
		{
			Vector3	worldPos	=pa.Value.mWorldCenter;
			Vector3	pos			=worldPos;

			bool	bAny	=false;

			//resolve intersection with this model
			int	i;
			for(i=0;i < MaxMoveBoxIterations;i++)
			{
				ZonePlane	hitPlane	=ZonePlane.Blank;

				//check for an intersection
				if(!IntersectBoxModel(pa.Value.mBox, pos, modelIndex, ref hitPlane))
				{
					break;
				}

				bAny	=true;

				hitPlane.ReflectPosition(ref pos);
			}

			if(!bAny)
			{
				continue;
			}

			if(i == MaxMoveBoxIterations)
			{
				return	false;
			}

			if(pos != worldPos)
			{
				if(!pa.Value.mMobile.Push(pos - worldPos, modelIndex))
				{
					return	false;
				}
			}
		}
		return	true;
	}
	#endregion


	#region Debug Stuff
	//simulate movement and record the positions involved
	public void MoveBoxDebug(BoundingBox box, Vector3 start, Vector3 end, List<Vector3> segments)
	{
		MoveBoxWorldDebug(box, start, end, segments);
		MoveBoxModelsDebug(box, start, end, segments);
	}


	//for debugging TraceAll
	public void TraceDebug(BoundingBox? box, float? radius,
		Vector3 start, Vector3 end, List<Vector3> segments)
	{
		Collision	col;

		//record the original ray
		segments.Add(start);
		segments.Add(end);

		if(TraceAll(radius, box, start, end, out col))
		{
			//add a little nub for the collision point
			segments.Add(col.mIntersection);
			segments.Add(col.mIntersection + col.mPlaneHit.mNormal * 5f);

			DebugFace	df	=col.mFaceHit;
			if(df == null)
			{
				return;
			}

			//boost off the plane a little
			Vector3	abovePlane	=col.mPlaneHit.mNormal * Mathery.VCompareEpsilon;

			for(int v=0;v < df.mNumVerts;v++)
			{
				int	idx0	=mDebugIndexes[v + df.mFirstVert];
				int	idx1	=mDebugIndexes[((v + 1) % df.mNumVerts) + df.mFirstVert];

				//boost off the plane a unit
				segments.Add(mDebugVerts[idx0] + abovePlane);
				segments.Add(mDebugVerts[idx1] + abovePlane);
			}
		}
	}


	//record the plane reflections of a movement against all models
	void MoveBoxModelsDebug(BoundingBox box, Vector3 start, Vector3 end, List<Vector3> segments)
	{
		List<ZonePlane>	hitPlanes	=new List<ZonePlane>();

		//do model collisions
		for(int i=0;i < MaxMoveBoxIterations;i++)
		{
			segments.Add(start);
			segments.Add(end);

			Collision	col;

			bool	bHitSomething	=TraceModelsBox(box, start, end, out col);
			if(!bHitSomething)
			{
				break;
			}

			if(col.mbStartInside)
			{
				return;
			}

			end	=col.mPlaneHit.ReflectPosition(start, end);

			if(!hitPlanes.Contains(col.mPlaneHit))
			{
				hitPlanes.Add(col.mPlaneHit);
			}
		}
	}


	//record the plane reflections of a movement against the world model
	void MoveBoxWorldDebug(BoundingBox box, Vector3 start, Vector3 end, List<Vector3> segments)
	{
		int	i	=0;

		List<ZonePlane>	hitPlanes	=new List<ZonePlane>();
		for(i=0;i < MaxMoveBoxIterations;i++)
		{
			RayTrace	rt	=new RayTrace(start, end);

			rt.mBounds	=box;

			segments.Add(start);
			segments.Add(end);

			bool	bHitSomething	=TraceNode(rt, start, end, 0);
			if(!bHitSomething)
			{
				break;
			}

			if(rt.mCollision.mbStartInside)
			{
				return;
			}

			ZonePlane	zp	=rt.mCollision.mPlaneHit;

			end	=zp.ReflectPosition(start, end);

			if(!hitPlanes.Contains(zp))
			{
				hitPlanes.Add(zp);
			}
		}
	}


	//useful for debugging or rays that need to start in solid
	public void TraceSegWorld(Vector3 start, Vector3 end, Int32 node, List<TraceSegment> segz)
	{
		if(node < 0)
		{
			Int32		leafIdx		=-(node + 1);
			ZoneLeaf	zl			=mZoneLeafs[leafIdx];

			TraceSegment	seg;

			seg.mContents	=zl.mContents;
			seg.mStart		=start;
			seg.mEnd		=end;

			segz.Add(seg);
			return;
		}

		ZoneNode	zn	=mZoneNodes[node];
		ZonePlane	p	=mZonePlanes[zn.mPlaneNum];

		Vector3	clipStart, clipEnd;
		if(PartBehind(p, 0f, start, end, out clipStart, out clipEnd))
		{
			TraceSegWorld(clipStart, clipEnd, zn.mBack, segz);
		}
		if(PartFront(p, 0f, start, end, out clipStart, out clipEnd))
		{
			TraceSegWorld(clipStart, clipEnd, zn.mFront, segz);
		}
	}
	#endregion
}