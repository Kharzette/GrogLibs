using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Text;

namespace BSPLib
{
	public class VisFloodParameters
	{
		//data to work on
		public Dictionary<VISPortal, Int32>	mPortIndexer;
		public bool							[]mPortalSeen;

		public int			mCore, mCores;
		public bool			mbVerbose, mbHasLight;
		public FileStream	mFS;
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
		public BSPBuildParams				mBSPParams;
		public LightParams					mLightParams;
		public VisParams					mVisParams;
		public Map.GetEmissiveForMaterial	mC4M;
		public string						mFileName;
	}
}
