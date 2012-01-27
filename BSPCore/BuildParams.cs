using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework;


namespace BSPCore
{
	public class BSPBuildParams
	{
		public int	mMaxThreads;
		public bool	mbVerbose;
		public bool	mbEntityVerbose;
		public bool	mbFixTJunctions;
		public bool	mbSlickAsGouraud;
		public bool	mbWarpAsMirror;
		public bool	mbSkyEmitLight;
		public bool	mbLavaEmitLight;
		public bool	mbWindowEmitLight;
		public bool	mbWindowTransparent;
		public bool	mbTransparentDetail;
	}


	public class LightParams
	{
		public bool		mbSeamCorrection;
		public bool		mbSurfaceLighting;
		public int		mSurfLightFrequency;
		public int		mSurfLightStrength;
		public int		mNumSamples;	//1 to 9
		public Vector3	mMinLight;
		public int		mMaxIntensity;
		public int		mLightGridSize;
	}


	public class VisParams
	{
		public bool	mbFullVis;
		public bool	mbSortPortals;
		public bool mbDistribute;
		public bool mbResume;
	}
}
