using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;


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


	public class LightFacesParameters
	{
		public int		mCore, mCores;
		public int		mStartFace;
		public int		mEndFace;
		public Vector3	[]mVertNormals;
		public bool		mbSuccess;
		public object	mProg;

		public LightParameters	mParams;
		public BinaryWriter		mBW;
		public FileStream		mFS;
		public GFXHeader		mHeader;
		public string			mRecFile;
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
