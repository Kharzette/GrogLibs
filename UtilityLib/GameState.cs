using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace UtilityLib
{
	public class GameState
	{
		//list of all possible states
		List<string>	mStates	=new List<string>();

		//to make sure no more than one at a time
		bool	mbTransitioning;

		//push here if already transitioning
		Queue<string>	mTransQueue	=new Queue<string>();

		//active state
		string		mCurState, mPrevState;

		//pause vars
		public bool	mbPlayerPaused;				//pause menu
		public bool	mbPaused;					//inactivity due to guide or something
		public bool	mbControllerDisconnected;	//differing pause message

		public event EventHandler	eTransitioningFrom;
		public event EventHandler	eTransitioningTo;
		public event EventHandler	eTransitionedTo;


		public string CurState
		{
			get { return mCurState; }
		}

		public string PrevState
		{
			get { return mPrevState; }
		}


		public GameState()
		{
		}


		public void AddState(string state)
		{
			if(mStates.Contains(state))
			{
				return;
			}
			mStates.Add(state);
		}


		public void Transition(string newState)
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

			string	from	=mCurState;
			string	to		=newState;

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

			string	nextTrans	=mTransQueue.Dequeue();
			Transition(nextTrans);
		}
	}
}
