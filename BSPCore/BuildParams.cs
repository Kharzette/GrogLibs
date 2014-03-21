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
		public bool	mbBuildAsBModel;
		public bool	mbFixTJunctions;
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
		public bool		mbDynamicLighting;	//sets materials to shader3

		//recording
		public bool	mbRecording;

		//recorded face points light traces to
		public Dictionary<int, List<Vector3>>	mFacePoints	=new Dictionary<int, List<Vector3>>();

		public Dictionary<int, GFXPlane>	mFacePlanes	=new Dictionary<int, GFXPlane>();
	}


	public class VisParams
	{
		public bool	mbFullVis;
		public bool	mbSortPortals;
		public bool mbDistribute;
		public bool mbResume;
	}
}
