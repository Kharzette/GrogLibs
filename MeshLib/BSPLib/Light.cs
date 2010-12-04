using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class LInfo
	{
		Vector3		[]RGBLData	=new Vector3[GBSPGlobals.MAX_LTYPE_INDEX];
		Int32		NumLTypes;
		bool		RGB;
		float		[]Mins		=new float[2];
		float		[]Maxs		=new float[2];
		Int32		[]LMaxs		=new int[2];
		Int32		[]LMins		=new int[2];
		Int32		[]LSize		=new int[2];
	}

	public class FInfo
	{
		Int32		Face;
		GFXPlane	Plane;
		Vector3		[]T2WVecs	=new Vector3[2];
		Vector3		TexOrg;
		Vector3		[]Points;
		Int32		NumPoints;

		Vector3		Center;
		float		Radius;
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


	public class RADReceiver
	{
		UInt16	Patch;
		UInt16	Amount;
	}
}
