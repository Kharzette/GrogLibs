using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class LInfo
	{
		public Vector3	[][]RGBLData	=new Vector3[GBSPGlobals.MAX_LTYPE_INDEX][];
		public Int32	NumLTypes;
		public bool		RGB;
		public float	[]Mins		=new float[2];
		public float	[]Maxs		=new float[2];
		public Int32	[]LMaxs		=new int[2];
		public Int32	[]LMins		=new int[2];
		public Int32	[]LSize		=new int[2];
	}

	public class FInfo
	{
		public Int32	Face;
		public GFXPlane	Plane		=new GFXPlane();
		public Vector3	[]T2WVecs	=new Vector3[2];
		public Vector3	TexOrg;
		public Vector3	[]Points;
		public Int32	NumPoints;

		public Vector3	Center;
		public float	Radius;
	}


	public class RADPatch
	{
		RADPatch	Next;				// Next patch in list
		GBSPPoly	Poly;				// Poly for patch	(Not used thoughout entire life)
		Vector3		Origin;				// Origin
		Int32		Leaf;				// Leaf patch is looking into
		float		Area;				// Area of patch
		GBSPPlane	Plane;				// Plane
		UInt16		NumReceivers;
		RADReceiver	[]Receivers;			// What patches this patch emits to
		Int32		NumSamples;			// Number of samples lightmaps has contributed
		Vector3		RadStart;			// Power of patch from original lightmap
		Vector3		RadSend;			// How much to send each bounce
		Vector3		RadReceive;			// How much received from current bounce
		Vector3		RadFinal;			// How much received from all bounces (what to add back to the lightmap)
		Vector3		Reflectivity;
		Vector3		Mins;				// Mins/ Max of patch
		Vector3		Maxs;
	}


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


	public class RADReceiver
	{
		UInt16	Patch;
		UInt16	Amount;
	}
}
