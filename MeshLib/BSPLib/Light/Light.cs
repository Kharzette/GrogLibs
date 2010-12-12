using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class LInfo
	{
		Vector3	[][]mRGBLData	=new Vector3[MAX_LTYPE_INDEX][];
		Int32	mNumLTypes;
		float	[]mMins		=new float[2];
		float	[]mMaxs		=new float[2];
		Int32	[]mLMaxs		=new int[2];
		Int32	[]mLMins		=new int[2];
		Int32	[]mLSize		=new int[2];

		public const int	MAX_LTYPE_INDEX		=12;
		public const int	MAX_LMAP_SIZE		=130;
		public const int	MAX_LTYPES			=4;


		internal Int32 GetNumLightTypes()
		{
			return	mNumLTypes;
		}


		internal Vector3 []GetRGBLightData(Int32 lightIndex)
		{
			if(lightIndex > mNumLTypes)
			{
				return	null;
			}
			return	mRGBLData[lightIndex];
		}


		internal void ApplyLightToPatchList(RADPatch rp, Vector3 []facePoints)
		{
			if(mRGBLData[0] == null)
			{
				return;
			}
			RADPatch.ApplyLightList(rp, mRGBLData[0], facePoints);
		}


		internal void CalcInfo(float[] mins, float []maxs)
		{
			//Get the Texture U/V mins/max, and Grid aligned lmap mins/max/size
			for(int i=0;i < 2;i++)
			{
				mMins[i]	=mins[i];
				mMaxs[i]	=maxs[i];

				mins[i]	=(float)Math.Floor(mins[i] / FInfo.LGRID_SIZE);
				maxs[i]	=(float)Math.Ceiling(maxs[i] / FInfo.LGRID_SIZE);

				mLMins[i]	=(Int32)mins[i];
				mLMaxs[i]	=(Int32)maxs[i];
				mLSize[i]	=(Int32)(maxs[i] - mins[i]);

				if((mLSize[i] + 1) > LInfo.MAX_LMAP_SIZE)
				{
					Map.Print("CalcFaceInfo:  Face was not subdivided correctly.\n");
				}
			}
		}


		internal void AllocLightType(int lightIndex, Int32 size)
		{
			if(mRGBLData[lightIndex] == null)
			{
				if(mNumLTypes >= LInfo.MAX_LTYPES)
				{
					Map.Print("Max Light Types on face.\n");
					return;
				}
			
				mRGBLData[lightIndex]	=new Vector3[size];
				mNumLTypes++;
			}
		}


		internal void FreeLightType(int lightIndex)
		{
			mRGBLData[lightIndex]	=null;
		}


		internal void CalcMids(out float MidU, out float MidV)
		{
			MidU	=(mMaxs[0] + mMins[0]) * 0.5f;
			MidV	=(mMaxs[1] + mMins[1]) * 0.5f;
		}


		internal void CalcSizeAndStart(float uOffset, float vOffset,
			out int w, out int h, out float startU, out float startV)
		{
			w		=(mLSize[0]) + 1;
			h		=(mLSize[1]) + 1;
			startU	=((float)mLMins[0] + uOffset) * (float)FInfo.LGRID_SIZE;
			startV	=((float)mLMins[1] + vOffset) * (float)FInfo.LGRID_SIZE;
		}


		internal Int32 GetLWidth()
		{
			return	mLSize[0] + 1;
		}


		internal Int32 GetLHeight()
		{
			return	mLSize[1] + 1;
		}


		internal Int32 CalcSize()
		{
			return	(mLSize[0] + 1)	* (mLSize[1] + 1);
		}
	}


	public class FInfo
	{
		Int32		mFace;
		GFXPlane	mPlane		=new GFXPlane();
		Vector3		[]mT2WVecs	=new Vector3[2];
		Vector3		mTexOrg;
		Vector3		[]mPoints;
		Vector3		mCenter;
		float		mRadius;

		public const int	LGRID_SIZE	=16;


		internal Int32 GetFaceIndex()
		{
			return	mFace;
		}


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

		internal delegate bool IsPointInSolid(Vector3 pos);
		internal delegate bool RayCollision(Vector3 front, Vector3 back, ref Vector3 Impacto);

		internal void CalcFacePoints(LInfo LightInfo, float UOfs, float VOfs,
			bool bExtraLightCorrection, IsPointInSolid pointInSolid,
			RayCollision rayCollide)
		{
			Vector3	FaceMid;
			float	MidU, MidV, StartU, StartV, CurU, CurV;
			Int32	u, v, Width, Height;
			bool	[]InSolid	=new bool[LInfo.MAX_LMAP_SIZE * LInfo.MAX_LMAP_SIZE];


			LightInfo.CalcMids(out MidU, out MidV);

			FaceMid	=mTexOrg + mT2WVecs[0] * MidU + mT2WVecs[1] * MidV;

			LightInfo.CalcSizeAndStart(UOfs, VOfs, out Width, out Height, out StartU, out StartV);

			for(v=0;v < Height;v++)
			{
				for(u=0;u < Width;u++)
				{
					CurU	=StartU + u * FInfo.LGRID_SIZE;
					CurV	=StartV + v * FInfo.LGRID_SIZE;

					mPoints[(v * Width) + u]
						=mTexOrg + mT2WVecs[0] * CurU +
							mT2WVecs[1] * CurV;

					InSolid[(v * Width) + u]	=pointInSolid(mPoints[(v * Width) + u]);

					if(!bExtraLightCorrection)
					{
						if(InSolid[(v * Width) + u])
						{
							Vector3	colResult	=Vector3.Zero;
							if(rayCollide(FaceMid,
								mPoints[(v * Width) + u], ref colResult))
							{
								Vector3	vect	=FaceMid - mPoints[(v * Width) + u];
								vect.Normalize();
								mPoints[(v * Width) + u]	=colResult + vect;
							}
						}
					}
				}
			}

			if(!bExtraLightCorrection)
			{
				return;
			}

			for(v=0;v < mPoints.Length;v++)
			{
				float	BestDist, Dist;

				if(!InSolid[v])
				{
					//Point is good, leave it alone
					continue;
				}

				Vector3	pBestPoint	=FaceMid;
				BestDist	=Bounds.MIN_MAX_BOUNDS;
				
				for(u=0;u < mPoints.Length;u++)
				{
					if(mPoints[v] == mPoints[u])
					{
						continue;	//We know this point is bad
					}

					if(InSolid[u])
					{
						continue;	// We know this point is bad
					}

					//At this point, we have a good point,
					//now see if it's closer than the current good point
					Vector3	Vect	=mPoints[u] - mPoints[v];
					Dist	=Vect.Length();
					if(Dist < BestDist)
					{
						BestDist	=Dist;
						pBestPoint	=mPoints[u];

						if(Dist <= (FInfo.LGRID_SIZE - 0.1f))
						{
							break;	//This should be good enough...
						}
					}
				}
				mPoints[v]	=pBestPoint;
			}

			//free cached vis stuff
			InSolid	=null;
		}


		internal void SetFaceIndex(int fidx)
		{
			mFace	=fidx;
		}


		internal Vector3 GetPlaneNormal()
		{
			return	mPlane.mNormal;
		}


		internal void SetPlane(GFXPlane pln)
		{
			mPlane	=pln;
		}


		internal Vector3[] GetPoints()
		{
			return	mPoints;
		}


		internal void AllocPoints(int size)
		{
			mPoints	=new Vector3[size];
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


		internal bool PatchNeedsSplit(bool bFastPatch, int patchSize, out GBSPPlane Plane)
		{
			Int32	i;

			if(bFastPatch)
			{
				float	Dist;
				
				for(i=0;i < 3;i++)
				{
					Dist	=UtilityLib.Mathery.VecIdx(mBounds.mMaxs, i)
								- UtilityLib.Mathery.VecIdx(mBounds.mMins, i);
					
					if(Dist > patchSize)
					{
						//Cut it right through the center...
						Plane.mNormal	=Vector3.Zero;
						UtilityLib.Mathery.VecIdxAssign(ref Plane.mNormal, i, 1.0f);
						Plane.mDist	=(UtilityLib.Mathery.VecIdx(mBounds.mMaxs, i)
							+ UtilityLib.Mathery.VecIdx(mBounds.mMins, i))
								/ 2.0f;
						Plane.mType	=GBSPPlane.PLANE_ANY;
						return	true;
					}
				}
			}
			else
			{
				float	Min, Max;
				for(i=0;i < 3;i++)
				{
					Min	=UtilityLib.Mathery.VecIdx(mBounds.mMins, i) + 1.0f;
					Max	=UtilityLib.Mathery.VecIdx(mBounds.mMaxs, i) - 1.0f;

					if(Math.Floor(Min / patchSize)
						< Math.Floor(Max / patchSize))
					{
						Plane.mNormal	=Vector3.Zero;
						UtilityLib.Mathery.VecIdxAssign(ref Plane.mNormal, i, 1.0f);
						Plane.mDist	=patchSize * (1.0f + (float)Math.Floor(Min / patchSize));
						Plane.mType	=GBSPPlane.PLANE_ANY;
						return	true;
					}
				}
			}
			Plane	=new GBSPPlane();
			return	false;
		}


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
