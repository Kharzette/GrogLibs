using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;


namespace InputLib
{
	//common methods of moving a player around
	public class PlayerSteering
	{
		public enum SteeringMethod
		{
			None,
			TwinStick,		//left analog moves, right analog turns the camera
			Fly,			//left aims, right moves, no leveling out of the movement
			ThirdPerson,	//same as firstperson?
			FirstPerson,	//leveled out (no Y) movement like fly
			Platformer,		//not really finished
			XCOM			//left moves a "cursor", right moves camera
		}

		SteeringMethod	mMethod;

		//movement settings
		float	mGroundSpeed		=0.1f;
		float	mMouseSensitivity	=0.01f;
		float	mKeySensitivity		=0.1f;	//for key turning
		float	mGamePadSensitivity	=0.25f;
		float	mTurnSpeed			=1.0f;
		float	mWheelScrollSpeed	=0.04f;
		bool	mbInvertYAxis		=false;
		bool	mbUsePadIfPossible	=true;

		//sprint settings
		float	SprintForwardFactor	=1.8f;
		float	SprintBackFactor	=1.5f;
		float	SprintLeftFactor	=1.5f;
		float	SprintRightFactor	=1.5f;

		//position info
		Vector3	mPosition, mDelta;
		Vector3	mCursorPos;
		float	mPitch, mYaw, mRoll;
		float	mZoom	=80f;		//default
		bool	mbMovedThisFrame;	//true if the player gave movement input

		//for mouselook
		bool	mbRightClickToTurn;

		//constants
		const float	PitchClamp	=80.0f;

		//mapped actions from the game
		Enum	mZoomIn, mZoomOut;
		Enum	mMoveLeftRight, mMoveLeft, mMoveRight;
		Enum	mMoveForwardBack, mMoveForward, mMoveBack;
		Enum	mTurnBoth, mTurnLeft, mTurnRight;
		Enum	mPitchBoth, mPitchUp, mPitchDown;
		Enum	mMoveForwardFast, mMoveBackFast;
		Enum	mMoveLeftFast, mMoveRightFast;


		public PlayerSteering()
		{
		}

		public void SetMoveEnums(Enum moveForwardBack, Enum moveLeftRight,
			Enum moveForward, Enum moveBack, Enum moveLeft, Enum moveRight,
			Enum moveForwardFast, Enum moveBackFast, Enum moveLeftFast, Enum moveRightFast)
		{
			mMoveLeftRight		=moveLeftRight;
			mMoveForwardBack	=moveForwardBack;
			mMoveForward		=moveForward;
			mMoveBack			=moveBack;
			mMoveLeft			=moveLeft;
			mMoveRight			=moveRight;
			mMoveForwardFast	=moveForwardFast;
			mMoveBackFast		=moveBackFast;
			mMoveLeftFast		=moveLeftFast;
			mMoveRightFast		=moveRightFast;
		}

		public void SetTurnEnums(Enum turn, Enum turnLeft, Enum turnRight)
		{
			mTurnBoth		=turn;
			mTurnLeft		=turnLeft;
			mTurnRight		=turnRight;
		}

		public void SetPitchEnums(Enum pitch, Enum pitchUp, Enum pitchDown)
		{
			mPitchBoth	=pitch;
			mPitchUp	=pitchUp;
			mPitchDown	=pitchDown;
		}

		public SteeringMethod Method
		{
			get { return mMethod; }
			set { mMethod = value; }
		}

		public Vector3 CursorPos
		{
			get { return mCursorPos; }
			set { mCursorPos = value; }
		}

		public bool RightClickToTurn
		{
			get { return mbRightClickToTurn; }
			set { mbRightClickToTurn = value; }
		}

		public bool MovedThisFrame
		{
			get { return mbMovedThisFrame; }
			set { mbMovedThisFrame = value; }
		}

		public float Speed
		{
			get { return mGroundSpeed; }
			set { mGroundSpeed = value; }
		}

		public float TurnSpeed
		{
			get { return mTurnSpeed; }
			set { mTurnSpeed = value; }
		}

		public Vector3 Delta
		{
			get { return mDelta; }
			set { mDelta = value; }
		}

		public float Pitch
		{
			get { return mPitch; }
			set { mPitch = value; }
		}

		public float Yaw
		{
			get { return mYaw; }
			set { mYaw = value; }
		}

		public float Roll
		{
			get { return mRoll; }
			set { mRoll = value; }
		}

		public float Zoom
		{
			get { return mZoom; }
			set { mZoom = value; }
		}

		public float MouseSensitivity
		{
			get { return mMouseSensitivity; }
			set { mMouseSensitivity = value; }
		}

		public float GamePadSensitivity
		{
			get { return mGamePadSensitivity; }
			set { mGamePadSensitivity = value; }
		}

		public bool InvertYAxis
		{
			get { return mbInvertYAxis; }
			set { mbInvertYAxis = value; }
		}

		public bool UseGamePadIfPossible
		{
			get { return mbUsePadIfPossible; }
			set { mbUsePadIfPossible = value; }
		}


		public Vector3 Update(Vector3 pos,
			Vector3 camForward, Vector3 camLeft, Vector3 camUp,
			List<Input.InputAction> actions)
		{
			mbMovedThisFrame	=false;

			mPosition	=pos;
			if(mMethod == SteeringMethod.None)
			{
				return	pos;
			}

			foreach(Input.InputAction act in actions)
			{
				if(act.mAction.Equals(mZoomIn))
				{
					mZoom	-=act.mMultiplier * 0.04f;
					mZoom	=MathUtil.Clamp(mZoom, 5f, 500f);
				}
				else if(act.mAction.Equals(mZoomOut))
				{
					mZoom	+=act.mMultiplier * 0.04f;
					mZoom	=MathUtil.Clamp(mZoom, 5f, 500f);
				}
			}

			Vector3	moveVec	=Vector3.Zero;

			if(mMethod == SteeringMethod.FirstPerson
				|| mMethod == SteeringMethod.ThirdPerson)
			{
				UpdateGroundMovement(camForward, camLeft, camUp, actions, out moveVec);
			}
			else if(mMethod == SteeringMethod.Fly)
			{
				UpdateFly(camForward, camLeft, camUp, actions, out moveVec);
			}
			else if(mMethod == SteeringMethod.TwinStick)
			{
				UpdateGroundMovement(camForward, camLeft, camUp, actions, out moveVec);
			}
			else if(mMethod == SteeringMethod.Platformer)
			{
				throw(new NotImplementedException());
			}
			else if(mMethod == SteeringMethod.XCOM)
			{
				throw(new NotImplementedException());
			}

			//camera direction stuff is backwards
			moveVec	=-moveVec;

			mDelta	=moveVec;

			return	mPosition + moveVec;
		}


		void UpdateFly(Vector3 camForward, Vector3 camLeft, Vector3 camUp,
			List<Input.InputAction> actions, out Vector3 moveVec)
		{
			GetTurn(actions);

			GetMove(camForward, camLeft, camUp, actions, out moveVec);
		}


		void UpdateGroundMovement(Vector3 camForward, Vector3 camLeft, Vector3 camUp,
			List<Input.InputAction> actions, out Vector3 moveVec)
		{
			GetTurn(actions);

			GetMove(camForward, camLeft, camUp, actions, out moveVec);
			GetTurn(actions);

			//zero out up/down
			moveVec.Y	=0.0f;
		}


		void GetMove(Vector3 camForward, Vector3 camLeft, Vector3 camUp,
			List<Input.InputAction> actions,
			out Vector3 moveVec)
		{
			moveVec	=Vector3.Zero;

			float	actionMult	=0f;
			int		multCount	=0;
			foreach(Input.InputAction act in actions)
			{
				if(act.mAction.Equals(mMoveLeftRight))
				{
					mbMovedThisFrame	=true;
					moveVec				-=camLeft;
					actionMult			+=act.mMultiplier;
					multCount++;
				}
				else if(act.mAction.Equals(mMoveForwardBack))
				{
					mbMovedThisFrame	=true;
					moveVec				-=camForward;
					actionMult			+=act.mMultiplier;
					multCount++;
				}
				else if(act.mAction.Equals(mMoveForward))
				{
					mbMovedThisFrame	=true;
					moveVec				+=camForward;
					actionMult			+=act.mMultiplier;
					multCount++;
				}
				else if(act.mAction.Equals(mMoveBack))
				{
					mbMovedThisFrame	=true;
					moveVec				-=camForward;
					actionMult			+=act.mMultiplier;
					multCount++;
				}
				else if(act.mAction.Equals(mMoveLeft))
				{
					mbMovedThisFrame	=true;
					moveVec				+=camLeft;
					actionMult			+=act.mMultiplier;
					multCount++;
				}
				else if(act.mAction.Equals(mMoveRight))
				{
					mbMovedThisFrame	=true;
					moveVec				-=camLeft;
					actionMult			+=act.mMultiplier;
					multCount++;
				}
				else if(act.mAction.Equals(mMoveForwardFast))
				{
					mbMovedThisFrame	=true;
					moveVec				+=camForward;
					actionMult			+=act.mMultiplier * SprintForwardFactor;
					multCount++;
				}
				else if(act.mAction.Equals(mMoveBackFast))
				{
					mbMovedThisFrame	=true;
					moveVec				-=camForward;
					actionMult			+=act.mMultiplier * SprintBackFactor;
					multCount++;
				}
				else if(act.mAction.Equals(mMoveLeftFast))
				{
					mbMovedThisFrame	=true;
					moveVec				+=camLeft;
					actionMult			+=act.mMultiplier * SprintLeftFactor;
					multCount++;
				}
				else if(act.mAction.Equals(mMoveRightFast))
				{
					mbMovedThisFrame	=true;
					moveVec				-=camLeft;
					actionMult			+=act.mMultiplier * SprintRightFactor;
					multCount++;
				}
			}

			if(mbMovedThisFrame)
			{
				moveVec.Normalize();

				actionMult	/=multCount;

				moveVec	*=actionMult * mGroundSpeed;
			}
		}


		void GetTurn(List<Input.InputAction> actions)
		{
			foreach(Input.InputAction act in actions)
			{
				if(act.mAction.Equals(mPitchBoth))
				{
					float	pitchAmount	=act.mMultiplier * 0.4f;

					if(mbInvertYAxis)
					{
						mPitch	+=pitchAmount;
					}
					else
					{
						mPitch	-=pitchAmount;
					}
				}
				else if(act.mAction.Equals(mPitchUp))
				{
					float	pitchAmount	=act.mMultiplier * 0.4f;

					if(mbInvertYAxis)
					{
						mPitch	+=pitchAmount;
					}
					else
					{
						mPitch	-=pitchAmount;
					}
				}
				else if(act.mAction.Equals(mPitchDown))
				{
					float	pitchAmount	=act.mMultiplier * 0.4f;

					if(!mbInvertYAxis)
					{
						mPitch	+=pitchAmount;
					}
					else
					{
						mPitch	-=pitchAmount;
					}
				}
				else if(act.mAction.Equals(mTurnBoth))
				{
					float	delta	=act.mMultiplier * 0.4f;
					mYaw	-=delta;
				}
				else if(act.mAction.Equals(mTurnLeft))
				{
					float	delta	=act.mMultiplier * 0.4f;
					mYaw	+=delta;
				}
				else if(act.mAction.Equals(mTurnRight))
				{
					float	delta	=act.mMultiplier * 0.4f;
					mYaw	-=delta;
				}
			}

			if(mPitch > PitchClamp)
			{
				mPitch	=PitchClamp;
			}
			else if(mPitch < -PitchClamp)
			{
				mPitch	=-PitchClamp;
			}
		}
	}
}
