using System;
using System.Collections.Generic;
using System.Threading;
using System.Text;

namespace BSPLib
{
	public class VisFloodParameters
	{
		//data to work on
		public Dictionary<VISPortal, Int32>	mPortIndexer;
		public bool							[]mPortalSeen;

		public int				mCore, mCores;
		public ManualResetEvent	mDoneEvent;
		public bool				mbVerbose;
	}


	public class VisParameters
	{
		public BSPBuildParams	mBSPParams;
		public VisParams		mVisParams;
		public string			mFileName;
	}


	public class GBSPSaveParameters
	{
		public BSPBuildParams	mBSPParams;
		public string			mFileName;
	}


	public class LightParameters
	{
		public BSPBuildParams	mBSPParams;
		public LightParameters	mLightParams;
		public string			mFileName;
	}
}
