using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPCore
{
	public class VisParameters
	{
		public BSPBuildParams	mBSPParams;
		public VisParams		mVisParams;
		public string			mFileName;
		public List<string>		mEndPoints;
	}


	public class GBSPSaveParameters
	{
		public BSPBuildParams	mBSPParams;
		public string			mFileName;
	}


	public class LightParameters
	{
		public BSPBuildParams						mBSPParams;
		public LightParams							mLightParams;
		public VisParams							mVisParams;
		public CoreDelegates.GetEmissiveForMaterial	mC4M;
		public string								mFileName;
	}
}
