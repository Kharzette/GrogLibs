using System;
using System.Numerics;
using System.Collections.Generic;


namespace BSPCore
{
	public enum MapType
	{
		GrogLibs,
		Quake1, Quake2, Quake3,
		Valve
	}

	public class BSPBuildParams
	{
		public int		mMaxThreads;
		public bool		mbVerbose;
		public bool		mbBuildAsBModel;
		public bool		mbFixTJunctions;
		public string	mMapName;
		public MapType	mMapType;
	}


	public class LightParams
	{
		public bool		mbSeamCorrection;
		public int		mNumSamples;	//1 to 9
		public Vector3	mMinLight;
		public int		mMaxIntensity;
		public int		mLightGridSize;

		//recording
		public bool	mbRecording;

		//recorded data from the lighting process
		public Dictionary<int, List<Vector3>>	mFacePoints	=new Dictionary<int, List<Vector3>>();
		public Dictionary<int, GFXPlane>		mFacePlanes	=new Dictionary<int, GFXPlane>();
		public Dictionary<int, FInfo>			mFInfos		=new Dictionary<int, FInfo>();
	}


	public class VisParams
	{
		public bool	mbFullVis;
		public bool	mbSortPortals;
		public bool mbResume;
	}
}
