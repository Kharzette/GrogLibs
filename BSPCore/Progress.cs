using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSPCore
{
	public class ProgressEventArgs : EventArgs
	{
		public int	mMin;
		public int	mMax;
		public int	mCurrent;
		public int	mIndex;
	}


	class Progress
	{
		public int	mMin;
		public int	mMax;
		public int	mCurrent;


		public Progress()
		{
		}
	}


	public static class ProgressWatcher
	{
		static List<Progress>	mProgs	=new List<Progress>();

		public static event EventHandler	eProgressUpdated;


		public static object RegisterProgress(int min, int max, int current)
		{
			Progress	outProg	=new Progress();

			outProg.mMin		=min;
			outProg.mMax		=max;
			outProg.mCurrent	=current;

			lock(mProgs)
			{
				mProgs.Add(outProg);
			}

			return	outProg;
		}


		public static void DestroyProgress(object prog)
		{
			Progress	pr	=prog as Progress;
			if(pr == null)
			{
				return;
			}

			int	index	=-1;

			lock(mProgs)
			{
				if(mProgs.Contains(pr))
				{
					index	=mProgs.IndexOf(pr);
					mProgs.Remove(pr);
				}
			}

			//clear the bar for the next use
			ProgressEventArgs	pea		=new ProgressEventArgs();
			pea.mCurrent	=0;
			pea.mIndex		=index;
			pea.mMax		=0;
			pea.mMin		=0;

			UtilityLib.Misc.SafeInvoke(eProgressUpdated, null, pea);
		}


		public static void Clear()
		{
			List<int>	resets	=new List<int>();
			lock(mProgs)
			{
				foreach(Progress prog in mProgs)
				{
					resets.Add(mProgs.IndexOf(prog));
				}
				mProgs.Clear();
			}

			foreach(int i in resets)
			{
				//clear the bar for the next use
				ProgressEventArgs	pea		=new ProgressEventArgs();
				pea.mCurrent	=0;
				pea.mIndex		=i;
				pea.mMax		=0;
				pea.mMin		=0;

				UtilityLib.Misc.SafeInvoke(eProgressUpdated, null, pea);
			}
		}


		public static void UpdateProgress(object prog, int cur)
		{
			Progress	pr	=prog as Progress;
			if(pr == null)
			{
				return;
			}

			bool				bFound	=false;
			ProgressEventArgs	pea		=null;
			lock(mProgs)
			{
				if(mProgs.Contains(pr))
				{
					bFound		=true;
					pr.mCurrent	=cur;

					pea				=new ProgressEventArgs();
					pea.mCurrent	=cur;
					pea.mIndex		=mProgs.IndexOf(pr);
					pea.mMax		=pr.mMax;
					pea.mMin		=pr.mMin;
				}
			}
			if(bFound)
			{
				UtilityLib.Misc.SafeInvoke(eProgressUpdated, null, pea);
			}
		}


		public static void UpdateProgress(object prog, int min, int max, int cur)
		{
			Progress	pr	=prog as Progress;
			if(pr == null)
			{
				return;
			}

			bool				bFound	=false;
			ProgressEventArgs	pea		=null;
			lock(mProgs)
			{
				if(mProgs.Contains(pr))
				{
					bFound		=true;
					pr.mMin		=min;
					pr.mMax		=max;
					pr.mCurrent	=cur;

					pea				=new ProgressEventArgs();
					pea.mCurrent	=cur;
					pea.mIndex		=mProgs.IndexOf(pr);
					pea.mMax		=max;
					pea.mMin		=min;
				}
			}
			if(bFound)
			{
				UtilityLib.Misc.SafeInvoke(eProgressUpdated, null, pea);
			}
		}


		public static void UpdateProgressIncremental(object prog)
		{
			Progress	pr	=prog as Progress;
			if(pr == null)
			{
				return;
			}

			bool				bFound	=false;
			ProgressEventArgs	pea		=null;
			lock(mProgs)
			{
				if(mProgs.Contains(pr))
				{
					bFound		=true;
					pr.mCurrent++;

					pea				=new ProgressEventArgs();
					pea.mCurrent	=pr.mCurrent;
					pea.mIndex		=mProgs.IndexOf(pr);
					pea.mMax		=pr.mMax;
					pea.mMin		=pr.mMin;
				}
			}
			if(bFound)
			{
				UtilityLib.Misc.SafeInvoke(eProgressUpdated, null, pea);
			}
		}
	}
}
