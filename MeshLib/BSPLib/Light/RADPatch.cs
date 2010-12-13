using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPLib
{
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


		internal void Send(RADPatch []patchList)
		{
			Vector3	Send	=mRadSend / (float)0x10000;

			//Send light out to each pre-computed receiver
			for(int k=0;k < mNumReceivers;k++)
			{
				RADReceiver	Receiver	=mReceivers[k];
				RADPatch	RPatch		=patchList[Receiver.mPatch];

				RPatch.mRadReceive	+=Send * Receiver.mAmount;
			}
		}


		internal bool Check()
		{
			for(int i=0;i < 3;i++)
			{
				if(UtilityLib.Mathery.VecIdx(mRadFinal, i) < 0.0f)
				{
					Map.Print("CheckPatch:  Bad final radiosity Color in patch.\n");
					return	false;
				}
			}
			return	true;
		}


		internal bool Finalize(GFXPlane facePlane, int planeSide, Map.GetNodeLandedIn findNode)
		{
			if(mPoly == null)
			{
				Map.Print("FinalizePatchInfo:  No Poly!\n");
				return	false;
			}

			mOrigin	=mPoly.Center();

			mPlane.mNormal	=facePlane.mNormal;
			mPlane.mDist	=facePlane.mDist;
			mPlane.mType	=GBSPPlane.PLANE_ANY;

			if(planeSide != 0)
			{
				mPlane.Inverse();
			}
			mOrigin	+=mPlane.mNormal * 2.0f;

			Int32	nodeLandedIn	=findNode(0, mOrigin);
			mLeaf	=-(nodeLandedIn + 1);

			mArea	=mPoly.Area();
			if(mArea < 1.0f)
			{
				mArea	=1.0f;
			}
			mPoly	=null;

			return	true;
		}

		static internal RADPatch SubdivideFacePatches(RADPatch Patch, bool bFastPatch, int patchSize, ref int numPatches)
		{
			RADPatch	CPatch, NewPatch, NextPatch;
			GBSPPoly	Poly, FPoly, BPoly;
			GBSPPlane	Plane;

			for(CPatch=Patch;CPatch != null;CPatch=NextPatch)
			{
				NextPatch	=CPatch.mNext;

				if(CPatch.PatchNeedsSplit(bFastPatch, patchSize, out Plane))
				{
					numPatches++;

					Poly	=CPatch.mPoly;
					if(!Poly.Split(Plane, out FPoly, out BPoly, false))
					{
						return	null;
					}
					
					if(FPoly == null || BPoly == null)
					{
						Map.Print("SubdivideFacePatches:  Patch was not split.\n");
						return	null;
					}
					
					NewPatch	=new RADPatch();
					if(NewPatch == null)
					{
						Map.Print("SubdivideFacePatches:  Out of memory for new patch.\n");
						return	null;
					}

					//Make it take on all the attributes of it's parent
					NewPatch.mArea			=CPatch.mArea;
					NewPatch.mBounds		=CPatch.mBounds;
					NewPatch.mLeaf			=CPatch.mLeaf;
					NewPatch.mNumReceivers	=CPatch.mNumReceivers;
					NewPatch.mNumSamples	=CPatch.mNumSamples;
					NewPatch.mOrigin		=CPatch.mOrigin;
					NewPatch.mPlane			=CPatch.mPlane;
					NewPatch.mRadFinal		=CPatch.mRadFinal;
					NewPatch.mRadReceive	=CPatch.mRadReceive;
					NewPatch.mRadSend		=CPatch.mRadSend;
					NewPatch.mRadStart		=CPatch.mRadStart;
					NewPatch.mReceivers		=CPatch.mReceivers;
					NewPatch.mReflectivity	=CPatch.mReflectivity;

					NewPatch.mNext	=NextPatch;
					NewPatch.mPoly	=FPoly;
					if(!NewPatch.CalcInfo())
					{
						Map.Print("SubdivideFacePatches:  Could not calculate patch info.\n");
						return	null;
					}

					//Re-use the first patch
					CPatch.mNext	=NewPatch;
					CPatch.mPoly	=BPoly;

					if(!CPatch.CalcInfo())
					{
						Map.Print("SubdivideFacePatches:  Could not calculate patch info.\n");
						return	null;
					}

					NextPatch	=CPatch;	// Keep working from here till satisfied...
				}
			}
			return Patch;
		}


		bool PatchNeedsSplit(bool bFastPatch, int patchSize, out GBSPPlane Plane)
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
}
