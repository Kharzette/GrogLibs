using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	internal class VisFloodParameters
	{
		//data to work on
		internal Dictionary<VISPortal, Int32>	mPortIndexer;
		internal bool							[]mPortalSeen;

		internal int		mCore, mCores;
		internal bool		mbVerbose, mbHasLight;
		internal FileStream	mFS;
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


	internal class LightFacesParameters
	{
		internal int		mCore, mCores;
		internal int		mStartFace;
		internal int		mEndFace;
		internal Vector3	[]mVertNormals;
		internal bool		mbSuccess;
		internal object		mProg;

		internal LightParameters	mParams;
		internal BinaryWriter		mBW;
		internal FileStream			mFS;
		internal GFXHeader			mHeader;
		internal string				mRecFile;
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
