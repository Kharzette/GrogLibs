using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.IO;
using System.Text;


namespace BSPCore
{
	public class GBSPSaveParameters
	{
		public BSPBuildParams	mBSPParams;
		public string			mFileName;
	}


	public class LightParameters
	{
		public BSPBuildParams						mBSPParams;
		public LightParams							mLightParams;
		public string								mFileName;
	}
}
