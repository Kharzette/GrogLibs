using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using UtilityLib;
using PathLib;


namespace BSPZone
{
	public class MobileAI
	{
		public enum Visibility
		{
			ZoneWide, InPVS, InLOS,
			ZoneWideRange, InPVSRange, InLOSRange
		}

		//the important stuff
		Zone		mZone;
		Mobile		mMob;
		Random		mRand;
		PathGraph	mGraph;

		//custom state set by the game
		//could be dead or injured or invulnerable or whatever
		public UInt32 mCustomState;

		//faction value set by the game
		UInt32	mFaction;

		//movement speed
		float	mSpeed;

		//a mobile to follow or approach
		MobileAI	mApproachTarget;

		//whenever a pathfind fails, it gets added to this
		//list.  The AI can use it to avoid these in the future
		List<MobileAI>	mCannotPathToMob	=new List<MobileAI>();
		List<Vector3>	mCannotPathToLoc	=new List<Vector3>();

		//a goal in plain sight that should be able to be moved to without pathing
		Vector3	mDirectMoveGoal;
		int		mGoalMoveTime;

		//some basic thinking states
		bool	mbAwaitingPath, mbFollowingPath;
		bool	mbMovingToGoal, mbApproaching, mbSurrounding;

		//for approaching another mobile
		float		mDesiredDistance;
		Vector3		mApproachDirection;
		BoundingBox	mTinyBox;	//for doing raycasts

		//path being traversed
		List<Vector3>	mPath	=new List<Vector3>();

		//tracking time taken to get from a path node to the next
		int	mPathTime;


		//constants
		const int	PathTimeOut			=5000;
		const int	MoveToGoalTimeOut	=10000;
		const int	ApproachIterations	=5;

		public const float	CloseEnough				=5f;
		const float			EyeToFloorTestHeight	=100f;	//should cover quite tall folk


		public MobileAI(UInt32 fact, Mobile mob, Random rnd, float speed)
		{
			mFaction	=fact;
			mMob		=mob;
			mRand		=rnd;
			mSpeed		=speed;

			//helps with surrounding an enemy
			mApproachDirection	=Mathery.RandomDirectionXZ(mRand);

			//for casting down to floor
			mTinyBox	=Misc.MakeBox(1f, 1f);
		}


		public UInt32 GetFaction()
		{
			return	mFaction;
		}


		public Vector3 GetGroundPosition()
		{
			return	mMob.GetGroundPosition();
		}


		public Vector3 GetMiddlePosition()
		{
			return	mMob.GetMiddlePosition();
		}


		public bool IsOnGround()
		{
			return	mMob.IsOnGround();
		}


		public bool IsApproaching()
		{
			return	mbApproaching;
		}


		public bool IsAwaitingPath()
		{
			return	mbAwaitingPath;
		}


		public bool IsFollowingPath()
		{
			return	mbFollowingPath;
		}


		public BoundingBox GetWorldBounds()
		{
			return	mMob.GetTransformedBound();
		}


		public object GetParent()
		{
			return	mMob.mParent;
		}


		public Vector3 GetEyePosition()
		{
			return	mMob.GetEyePos();
		}


		public void SetSpeed(float speed)
		{
			mSpeed	=speed;
		}


		public void ClearCannotPathLists()
		{
			mCannotPathToLoc.Clear();
			mCannotPathToMob.Clear();
		}


		public void ClearGoals()
		{
			mbApproaching	=false;
			mbSurrounding	=false;
			mbAwaitingPath	=false;
			mbFollowingPath	=false;
			mbMovingToGoal	=false;
			mPath.Clear();
		}


		public void StopHere()
		{
			ClearGoals();
			mMob.KillVelocity();
		}


		public bool TravelToLocation(Vector3 location, bool bOverride)
		{
			//make sure we haven't already set this up
			if(mbFollowingPath || mbAwaitingPath && !bOverride)
			{
				return	true;	//already handled
			}

			//check bad path locations from prior knowledge
			foreach(Vector3 badPos in mCannotPathToLoc)
			{
				if(Mathery.CompareVector(badPos, location))
				{
					return	false;
				}
			}

			mbMovingToGoal	=false;
			mbApproaching	=false;
			mbSurrounding	=false;
			mbAwaitingPath	=false;
			mbFollowingPath	=false;
			mApproachTarget	=null;

			mDirectMoveGoal	=location;

			Vector3	myPos	=mMob.GetGroundPosition();

			//see if that spot is a straight walk over
			if(mMob.TryMoveTo(location, CloseEnough))
			{
				mbMovingToGoal	=true;
				mGoalMoveTime	=MoveToGoalTimeOut;
			}
			else
			{
				myPos			=mZone.DropToGround(myPos, false);
				location		=mZone.DropToGround(location, false);
				mbAwaitingPath	=mGraph.FindPath(myPos, location, OnPatherDone, mZone.FindWorldNodeLandedIn);
			}
			return	true;
		}


		public void SurroundMobile(MobileAI target, float desiredDistance)
		{
			//make sure we haven't already set this up
			if(mbApproaching || mbFollowingPath)
			{
				if(mApproachTarget == target)
				{
					return;	//already handled
				}
			}
			mbApproaching		=true;
			mbSurrounding		=true;
			mApproachTarget		=target;
			mDesiredDistance	=desiredDistance;

			mbAwaitingPath	=false;
			mbFollowingPath	=false;
			mbMovingToGoal	=false;
		}


		public void ApproachMobile(MobileAI target, float desiredDistance)
		{
			//make sure we haven't already set this up
			if(mbApproaching || mbFollowingPath)
			{
				if(mApproachTarget == target)
				{
					return;	//already handled
				}
			}

			//avoid trying the same thing repeatedly
			if(mCannotPathToMob.Contains(target))
			{
				mbApproaching	=false;
				mApproachTarget	=null;
				return;
			}

			mbApproaching		=true;
			mApproachTarget		=target;
			mDesiredDistance	=desiredDistance;

			mbSurrounding	=false;
			mbAwaitingPath	=false;
			mbFollowingPath	=false;
			mbMovingToGoal	=false;
		}


		public void ChangeLevel(Zone z, PathGraph pg)
		{
			mZone	=z;
			mGraph	=pg;

			mZone.AddMob(this);
		}


		public bool UpdateAI(int msDelta)
		{
			Vector3	startPos	=mMob.GetGroundPosition();

			Vector3	endPos	=startPos;
			Vector3	camPos	=startPos;

			//not much to do while falling
			if(!mMob.IsOnGround())
			{
				mMob.Move(endPos, msDelta, false, true, false, true, out endPos, out camPos);
				return	false;
			}

			bool	bMoved	=false;

			if(mbApproaching)
			{
				bMoved	=UpdateApproaching(msDelta, startPos, ref endPos);
			}
			else if(mbMovingToGoal)
			{
				bMoved	=UpdateMoveToGoal(msDelta, startPos, ref endPos);
			}
			else if(mbFollowingPath)
			{
				bMoved	=UpdateFollowPath(msDelta, startPos, ref endPos);
			}
			mMob.Move(endPos, msDelta, false, true, false, true, out endPos, out camPos);

			return	bMoved;
		}


		//returns true if moved
		bool UpdateFollowPath(int msDelta, Vector3 startPos, ref Vector3 endPos)
		{
			if(mPath.Count <= 0)
			{
				mbFollowingPath	=false;
				mMob.KillVelocity();
			}
			else
			{
				Vector3	target	=mPath[0] + Vector3.UnitY;

				Vector3	compareTarget	=target;

				float	yDiff	=Math.Abs(target.Y - mPath[0].Y);
				if(yDiff < Zone.StepHeight)
				{
					//fix for stairs
					compareTarget.Y	=startPos.Y;
				}

				if(Mathery.CompareVectorEpsilon(compareTarget, startPos, CloseEnough))
				{
					mPath.RemoveAt(0);
					mPathTime	=0;
					ComputeApproach(target, msDelta, out endPos);
					return	true;
				}
				else
				{
					mPathTime	+=msDelta;
					if(mPathTime > PathTimeOut)
					{
						mbFollowingPath	=false;	//try another route, taking too long
						mPathTime		=0;
					}
					else
					{
						ComputeApproach(target, msDelta, out endPos);
						return	true;
					}
				}
			}
			return	false;
		}


		//returns true if moved
		bool UpdateMoveToGoal(int msDelta, Vector3 startPos, ref Vector3 endPos)
		{
			mGoalMoveTime	-=msDelta;
			if(mGoalMoveTime < 0)
			{
				mbMovingToGoal	=false;	//taking too long!
			}
			else
			{
				if(Mathery.CompareVectorEpsilon(mDirectMoveGoal, startPos, CloseEnough))
				{
					mbMovingToGoal	=false;
					mMob.KillVelocity();
				}
				else
				{
					ComputeApproach(mDirectMoveGoal, msDelta, out endPos);
					return	true;
				}
			}
			return	false;
		}


		//returns true if moved
		bool UpdateApproaching(int msDelta, Vector3 startPos, ref Vector3 endPos)
		{
			if(mApproachTarget != null)
			{
				if(ApproachMobile(msDelta, out endPos))
				{
					mbApproaching	=false;
					mMob.KillVelocity();
				}
				return	true;
			}
			else
			{
				mbApproaching	=false;
			}
			return	false;
		}


		//return true if at goal
		bool ApproachMobile(int msDelta, out Vector3 endPos)
		{
			//take the eye position to raycast down
			Vector3	approachPos	=mApproachTarget.GetEyePosition();
			Vector3	myPos		=mMob.GetGroundPosition();

			endPos	=myPos;
			if(!mbApproaching)
			{
				return	false;
			}

			//if not surrounding, set mApproachDirection to
			//the delta vector between them
			if(!mbSurrounding)
			{
				//step back along a vector from us to them by desired distance
				Vector3	aimVec	=approachPos - myPos;

				//level out aimVec
				aimVec.Y	=0f;

				//check
				if(aimVec.LengthSquared() > 0.001f)
				{
					aimVec.Normalize();
					mApproachDirection	=aimVec;
				}
			}

			Vector3	tryPos;
			if(FindSpotAroundPosition(approachPos,
				(mDesiredDistance - CloseEnough), out tryPos))
			{
				//see if that spot is a straight walk over
				if(mMob.TryMoveTo(tryPos, CloseEnough))
				{
					return	ComputeApproach(tryPos, msDelta, out endPos);
				}
				else
				{
					//can't get directly to it, so path there
					myPos			=mZone.DropToGround(myPos, false);
					tryPos			=mZone.DropToGround(tryPos, false);				
					mbApproaching	=false;
					mbAwaitingPath	=mGraph.FindPath(myPos, tryPos,
						OnPatherDone, mZone.FindWorldNodeLandedIn);
				}
			}
			else
			{
				//no spot could be found around that position
				//better to just make another decision
				mbApproaching	=false;
			}

			return	false;
		}


		//given a position, find a spot nearby to stand that is valid
		//uses mApproachDirection, but changes it if it doesn't work
		//this is best used for mobiles by providing the head or eye pos
		bool FindSpotAroundPosition(Vector3 pos, float distance, out Vector3 goodPos)
		{
			bool	bSuccess	=false;

			goodPos	=Vector3.Zero;

			//approach direction might put the location in a wall
			//make a few guesses
			int	iterations	=ApproachIterations;
			while(iterations-- > 0)
			{
				//adjust outward to approach position
				Vector3	approachPos	=pos - (mApproachDirection * distance);

				//raycast between to make sure there's no obstacle in between
				int			modelHit	=0;
				Vector3		impacto		=Vector3.Zero;
				ZonePlane	planeHit	=ZonePlane.Blank;
				if(mZone.TraceAllRay(approachPos, pos,
					ref modelHit, ref impacto, ref planeHit))
				{
					//hit something on the way
					//try a different approach direction
					mApproachDirection	=Mathery.RandomDirectionXZ(mRand);
					continue;
				}

				//raycast down to make sure there's a valid floor beneath
				modelHit	=0;
				impacto		=Vector3.Zero;
				planeHit	=ZonePlane.Blank;
				if(!mZone.TraceAllRay(approachPos,
					approachPos - (Vector3.UnitY * EyeToFloorTestHeight),
					ref modelHit, ref impacto, ref planeHit))
				{
					//no floor underneath or eye in solid?
					//try another approach direction
					mApproachDirection	=Mathery.RandomDirectionXZ(mRand);
					continue;
				}

				//check for a startpoint in solid
				if(planeHit == ZonePlane.Blank)
				{
					mApproachDirection	=Mathery.RandomDirectionXZ(mRand);
					continue;
				}

				//use the impact point
				approachPos	=impacto;

				if(mMob.TryStandingSpot(approachPos))
				{
					goodPos		=approachPos;
					bSuccess	=true;
					break;	//the approach direction is valid
				}

				//pick a new direction if that one is in the wall
				mApproachDirection	=Mathery.RandomDirectionXZ(mRand);
			}
			return	bSuccess;
		}


		//return true if at goal
		bool ComputeApproach(Vector3 target, int msDelta, out Vector3 endPos)
		{
			Vector3	myPos		=mMob.GetGroundPosition();
			Vector3	aim			=myPos - target;

			//flatten for the nearness detection
			//this can cause some hilarity if AI goals are off the ground
			//AI controlled characters can appear to suddenly become
			//Michael Jordan and leap up trying to get to the goal
			Vector3	flatAim	=aim;
			
			flatAim.Y	=0;

			float	targetLen	=flatAim.Length();
			if(targetLen <= 0f)
			{
				endPos	=target;
				return	true;
			}

			flatAim	/=targetLen;

			flatAim	*=mSpeed * msDelta;

			float	moveLen	=flatAim.Length();

			if(moveLen > targetLen)
			{
				//arrive at goal
				endPos	=target;
				return	true;
			}

			//do unflattened move
			aim.Normalize();
			aim	*=mSpeed * msDelta;

			endPos	=myPos - aim;
			return	false;
		}


		void OnPatherDone(List<Vector3> resultPath)
		{
			mPath.Clear();

			if(!mbAwaitingPath)
			{
				return;
			}
			mbAwaitingPath	=false;

			foreach(Vector3 spot in resultPath)
			{
				mPath.Add(spot);
			}

			if(mPath.Count == 0)
			{
				if(mApproachTarget != null)
				{
					mCannotPathToMob.Add(mApproachTarget);
				}
				else
				{
					mCannotPathToLoc.Add(mDirectMoveGoal);
				}
			}

			mbFollowingPath	=(mPath.Count > 0);
		}
	}
}