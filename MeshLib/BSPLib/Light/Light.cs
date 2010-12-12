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


		internal void ApplyLightToPatchList(RADPatch rp, Vector3 []facePoints)
		{
			if(RGBLData[0] == null)
			{
				return;
			}
			RADPatch.ApplyLightList(rp, RGBLData[0], facePoints);
		}


		internal void CalcInfo(float[] mins, float []maxs)
		{
			//Get the Texture U/V mins/max, and Grid aligned lmap mins/max/size
			for(int i=0;i < 2;i++)
			{
				Mins[i]	=mins[i];
				Maxs[i]	=maxs[i];

				mins[i]	=(float)Math.Floor(mins[i] / FInfo.LGRID_SIZE);
				maxs[i]	=(float)Math.Ceiling(maxs[i] / FInfo.LGRID_SIZE);

				LMins[i]	=(Int32)mins[i];
				LMaxs[i]	=(Int32)maxs[i];
				LSize[i]	=(Int32)(maxs[i] - mins[i]);

				if((LSize[i] + 1) > LInfo.MAX_LMAP_SIZE)
				{
					Map.Print("CalcFaceInfo:  Face was not subdivided correctly.\n");
				}
			}
		}
	}


	public class FInfo
	{
		public Int32	mFace;
		public GFXPlane	mPlane		=new GFXPlane();
		public Vector3	[]mT2WVecs	=new Vector3[2];
		public Vector3	mTexOrg;
		public Vector3	[]mPoints;
		public Int32	mNumPoints;
		public Vector3	mCenter;
		public float	mRadius;

		public const int	LGRID_SIZE	=16;


		internal void CalcFaceLightInfo(LInfo lightInfo, List<Vector3> verts)
		{
			float	[]mins	=new float[2];
			float	[]maxs	=new float[2];

			for(int i=0;i < 2;i++)
			{
				mins[i]	=Bounds.MIN_MAX_BOUNDS;
				maxs[i]	=-Bounds.MIN_MAX_BOUNDS;
			}

			mCenter	=Vector3.Zero;

			GBSPPlane	pln;

			pln.mNormal	=mPlane.mNormal;
			pln.mDist	=mPlane.mDist;
			pln.mType	=mPlane.mType;

			Vector3	[]vecs	=new Vector3[2];
			GBSPPoly.TextureAxisFromPlane(pln, out vecs[0], out vecs[1]);

			foreach(Vector3 vert in verts)
			{
				for(int i=0;i < 2;i++)
				{
					float	d	=Vector3.Dot(vert, vecs[i]);

					if(d > maxs[i])
					{
						maxs[i]	=d;
					}
					if(d < mins[i])
					{
						mins[i]	=d;
					}
				}
				mCenter	+=vert;
			}

			mCenter	/=verts.Count;

			lightInfo.CalcInfo(mins, maxs);

			//Get the texture normal from the texture vecs
			Vector3	texNormal	=Vector3.Cross(vecs[0], vecs[1]);
			texNormal.Normalize();
			
			//Flip it towards plane normal
			float	distScale	=Vector3.Dot(texNormal, mPlane.mNormal);
			if(distScale == 0.0f)
			{
				Map.Print("CalcFaceInfo:  Invalid Texture vectors for face.\n");
			}
			if(distScale < 0)
			{
				distScale	=-distScale;
				texNormal	=-texNormal;
			}	

			distScale	=1 / distScale;

			//Get the tex to world vectors
			for(int i=0;i < 2;i++)
			{
				float	len		=vecs[i].Length();
				float	dist	=Vector3.Dot(vecs[i], mPlane.mNormal);
				dist	*=distScale;

				mT2WVecs[i]	=vecs[i] + texNormal * -dist;
				mT2WVecs[i]	*=((1.0f / len) * (1.0f / len));
			}

			for(int i=0;i < 3;i++)
			{
				UtilityLib.Mathery.VecIdxAssign(ref mTexOrg, i,
					-vecs[0].Z * UtilityLib.Mathery.VecIdx(mT2WVecs[0], i)
					-vecs[1].Z * UtilityLib.Mathery.VecIdx(mT2WVecs[1], i));
			}

			float Dist	=Vector3.Dot(mTexOrg, mPlane.mNormal)
							- mPlane.mDist - 1;
			Dist	*=distScale;
			mTexOrg	=mTexOrg + texNormal * -Dist;
		}
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
			mPoly.AddToBounds(mBounds);
			return	true;
		}


		static internal void ApplyLightList(RADPatch rpList, Vector3 []rgb, Vector3 []facePoints)
		{
			//Check each patch and see if the points lands in it's BBox
			for(RADPatch p=rpList;p != null;p=p.mNext)
			{
				int	vertOfs	=0;
				int	rgbOfs	=0;

				p.mNumSamples	=0;

				for(int i=0;i < facePoints.Length;i++)
				{
					int	k	=0;
					for(k=0;k < 3;k++)
					{
						if(UtilityLib.Mathery.VecIdx(p.mBounds.mMins, k)
							> UtilityLib.Mathery.VecIdx(facePoints[vertOfs], k) + 16)
						{
							break;
						}
						if(UtilityLib.Mathery.VecIdx(p.mBounds.mMaxs, k)
							< UtilityLib.Mathery.VecIdx(facePoints[vertOfs], k) - 16)
						{
							break;
						}
					}

					if(k == 3)
					{
						//Add the Color to the patch 
						p.mNumSamples++;
						p.mRadStart	+=rgb[rgbOfs];
					}
					rgbOfs++;
					vertOfs++;
				}				
				if(p.mNumSamples != 0)
				{
					p.mRadStart	*=(1.0f / (float)p.mNumSamples);
				}
			}
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
