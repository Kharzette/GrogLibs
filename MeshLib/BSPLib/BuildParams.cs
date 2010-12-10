using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class BSPBuildParams
	{
		public int	mMaxCores;
		public bool	mbVerbose;
		public bool	mbEntityVerbose;
		public bool mbFixTJunctions;
	}


	public class LightParams
	{
		public bool		mbExtraSamples;
		public bool		mbRadiosity;
		public bool		mbFastPatch;
		public int		mPatchSize;
		public int		mNumBounces;
		public float	mLightScale;
		public Vector3	mMinLight;
		public float	mMirrorReflect;
		public int		mMaxIntensity;
	}


	public class VisParams
	{
		public bool	mbFullVis;
		public bool	mbSortPortals;
	}
}
