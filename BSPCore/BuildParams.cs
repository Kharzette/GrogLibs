using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework;


namespace BSPCore
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
		public bool		mbSeamCorrection;
		public bool		mbRadiosity;
		public bool		mbFastPatch;
		public int		mPatchSize;
		public int		mNumBounces;
		public float	mLightScale;
		public Vector3	mMinLight;
		public float	mSurfaceReflect;
		public int		mMaxIntensity;
		public int		mLightGridSize;
		public int		mAtlasSize;
	}


	public class VisParams
	{
		public bool	mbFullVis;
		public bool	mbSortPortals;
		public bool mbDistribute;
		public int	mNumRetries;
		public int	mGranularity;
	}
}
