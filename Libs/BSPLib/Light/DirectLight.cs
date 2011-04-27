using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class DirectLight
	{
		public DirectLight	mNext;
		public Int32		mLType;
		public Vector3		mOrigin;
		public Vector3		mNormal;
		public float		mAngle;
		public Vector3		mColor;
		public float		mIntensity;
		public UInt32		mType;

		public const UInt32		DLight_Blank	=0;
		public const UInt32		DLight_Point	=1;
		public const UInt32		DLight_Spot		=2;
		public const UInt32		DLight_Surface	=4;
	}
}
