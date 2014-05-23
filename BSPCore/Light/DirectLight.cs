using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using SharpDX;


namespace BSPCore
{
	internal class DirectLight
	{
		internal Int32		mLType;
		internal Vector3	mOrigin;
		internal Vector3	mNormal;
		internal float		mCone;	//used if a spotlight
		internal Vector3	mColor;
		internal float		mIntensity;
		internal UInt32		mType;

		internal const Int32		DLight_Blank	=0;
		internal const Int32		DLight_Point	=1;
		internal const Int32		DLight_Spot		=2;
		internal const Int32		DLight_Surface	=4;
		internal const Int32		DLight_Sun		=8;
	}
}
