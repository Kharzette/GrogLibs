using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace UtilityLib
{
	public class GameState
	{
		//to make sure no more than one at a time
		bool	mbTransitioning;

		//push here if already transitioning
		Queue<Enum>	mTransQueue	=new Queue<Enum>();

		//active state
		Enum		mCurState, mPrevState;

		//pause vars
		public bool	mbPlayerPaused;				//pause menu
		public bool	mbPaused;					//inactivity due to guide or something
		public bool	mbControllerDisconnected;	//differing pause message

		public event EventHandler	eTransitioningFrom;
		public event EventHandler	eTransitioningTo;
		public event EventHandler	eTransitionedTo;


		public bool CurStateIs(Enum stateVal)
		{
			return	(mCurState.CompareTo(stateVal) == 0);
		}

		public bool PrevStateIs(Enum stateVal)
		{
			return	(mPrevState.CompareTo(stateVal) == 0);
		}


		public void TransitionBack()
		{
			Transition(mPrevState);
		}


		public void Transition(Enum newState)
		{
			if(mCurState == newState)
			{
				return;
			}

			if(mbTransitioning)
			{
				mTransQueue.Enqueue(newState);
				return;
			}

			mbTransitioning	=true;

			Enum	from	=mCurState;
			Enum	to		=newState;

			Misc.SafeInvoke(eTransitioningFrom, from);

			mPrevState	=mCurState;

			Misc.SafeInvoke(eTransitioningTo, to);

			mCurState	=newState;

			Misc.SafeInvoke(eTransitionedTo, to);

			mbTransitioning	=false;

			CheckQueue();
		}


		public bool CheckPause()
		{
			Debug.Assert(!(mbPaused && mbControllerDisconnected));

			if(mbPaused)
			{
				if(!mbPaused && !mbControllerDisconnected)
				{
					mbPaused	=true;
				}
				return	true;
			}
			else if(mbControllerDisconnected)
			{
				if(!mbPaused && !mbControllerDisconnected)
				{
					mbControllerDisconnected	=true;
				}
				return	true;
			}

			if(mbPaused)
			{
				mbPaused	=false;
			}
			else if(mbControllerDisconnected)
			{
				mbControllerDisconnected	=false;
			}
			return	false;
		}


		void CheckQueue()
		{
			if(mTransQueue.Count == 0)
			{
				return;
			}

			Enum	nextTrans	=mTransQueue.Dequeue();
			Transition(nextTrans);
		}
	}
}
