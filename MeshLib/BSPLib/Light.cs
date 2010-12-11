using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class LInfo
	{
		public Vector3	[][]RGBLData	=new Vector3[MAX_LTYPE_INDEX][];
		public Int32	NumLTypes;
		public bool		RGB;
		public float	[]Mins		=new float[2];
		public float	[]Maxs		=new float[2];
		public Int32	[]LMaxs		=new int[2];
		public Int32	[]LMins		=new int[2];
		public Int32	[]LSize		=new int[2];

		public const int	MAX_LTYPE_INDEX		=12;
		public const int	MAX_LMAP_SIZE		=130;
		public const int	MAX_LTYPES			=4;
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

		public const int	LGRID_SIZE	=16;
	}


	public class RADPatch
	{
		public RADPatch		mNext;				//Next patch in list
		public GBSPPoly		mPoly;				//Poly for patch	(Not used thoughout entire life)
		public Vector3		mOrigin;				//Origin
		public Int32		mLeaf;				//Leaf patch is looking into
		public float		mArea;				//Area of patch
		public GBSPPlane	mPlane;				//Plane
		public UInt16		mNumReceivers;
		public RADReceiver	[]mReceivers;		//What patches this patch emits to
		public Int32		mNumSamples;			//Number of samples lightmaps has contributed
		public Vector3		mRadStart;			//Power of patch from original lightmap
		public Vector3		mRadSend;			//How much to send each bounce
		public Vector3		mRadReceive;			//How much received from current bounce
		public Vector3		mRadFinal;			//How much received from all bounces (what to add back to the lightmap)
		public Vector3		mReflectivity;
		public Bounds		mBounds;


		internal bool CalcInfo()
		{
			mBounds	=new Bounds();
			foreach(Vector3 pnt in mPoly.mVerts)
			{
				mBounds.AddPointToBounds(pnt);
			}
			return	true;
		}
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


	public class TriEdge
	{
		public Int32		p0, p1;
		public Vector3		mNormal;
		public float		mDist;
		public Tri			mTri;
	}


	public class Tri
	{
		public TriEdge	[]mEdges	=new TriEdge[3];
	}


	public class PlaneFace
	{
		public PlaneFace	mNext;
		public Int32		mGFXFace;
	}


	public class TriPatch
	{
		public int			mNumPoints;
		public int			mNumEdges;
		public int			mNumTris;
		public GBSPPlane	mPlane;
		public TriEdge		[][]mEdgeMatrix;
		public RADPatch		[]mPoints;
		public TriEdge		[]mEdges;
		public Tri			[]mTriList;

		public const Int32	MAX_TRI_POINTS		=1024;
		public const Int32	MAX_TRI_EDGES		=(MAX_TRI_POINTS * 6);
		public const Int32	MAX_TRI_TRIS		=(MAX_TRI_POINTS * 2);
		public const float	MIN_MAX_BOUNDS2		=Bounds.MIN_MAX_BOUNDS * 2;


		public TriPatch()
		{
			mEdgeMatrix	=new TriEdge[MAX_TRI_POINTS][];
			for(int i=0;i < MAX_TRI_POINTS;i++)
			{
				mEdgeMatrix[i]	=new TriEdge[MAX_TRI_POINTS];
			}
			mPoints		=new RADPatch[MAX_TRI_POINTS];
			mEdges		=new TriEdge[MAX_TRI_EDGES];
			mTriList	=new Tri[MAX_TRI_TRIS];
		}
	}


	public class RADReceiver
	{
		public UInt16	mPatch;
		public UInt16	mAmount;


		internal void Read(System.IO.BinaryReader br)
		{
			mPatch	=br.ReadUInt16();
			mAmount	=br.ReadUInt16();
		}


		internal void Write(System.IO.BinaryWriter bw)
		{
			bw.Write(mPatch);
			bw.Write(mAmount);
		}
	}
}
