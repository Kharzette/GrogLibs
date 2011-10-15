using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPCore
{
	public class RADPatch
	{
		public RADPatch		mNext;				//Next patch in list
		public Int32		mLeaf;				//Leaf patch is looking into
		public UInt16		mNumReceivers;
		public RADReceiver	[]mReceivers;		//What patches this patch emits to
		public Vector3		mRadFinal;			//How much received from all bounces (what to add back to the lightmap)
		public Vector3		mReflectivity;
		public Bounds		mBounds;

		GBSPPoly	mPoly;			//Poly for patch	(Not used thoughout entire life)
		Vector3		mOrigin;		//Origin
		float		mArea;			//Area of patch
		GBSPPlane	mPlane;			//Plane
		Int32		mNumSamples;	//Number of samples lightmaps has contributed
		Vector3		mRadStart;		//Power of patch from original lightmap
		Vector3		mRadSend;		//How much to send each bounce
		Vector3		mRadReceive;	//How much received from current bounce


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


		internal void ResetSamples()
		{
			mNumSamples	=0;
		}


		internal Vector3 GetPlaneNormal()
		{
			return	mPlane.mNormal;
		}


		internal Vector3 GetOrigin()
		{
			return	mOrigin;
		}


		internal float GetArea()
		{
			return	mArea;
		}


		internal void SetFirstPassSendAmount()
		{
			mRadSend	=mRadStart * mReflectivity * mArea;
		}


		internal void Collect(ref float total)
		{
			//Add receive amount to Final amount
			mRadFinal	+=mRadReceive / mArea;
			mRadSend	=mRadReceive * mReflectivity;

			total	+=mRadSend.X + mRadSend.Y + mRadSend.Z;

			mRadReceive	=Vector3.Zero;
		}


		internal float DistVecBetween(RADPatch other, out Vector3 vec)
		{
			vec	=other.mOrigin - mOrigin;

			float	ret	=vec.Length();

			vec.Normalize();

			return	ret;
		}


		internal bool RayCastBetween(RADPatch other, CoreDelegates.RayCollision ray)
		{
			Vector3	imp	=Vector3.Zero;
			return	ray(mOrigin, other.mOrigin, ref imp);
		}


		internal void InitDLight(DirectLight dLight, float faceLight)
		{
			dLight.mOrigin	=mOrigin;
			dLight.mColor	=mReflectivity;
			dLight.mNormal	=mPlane.mNormal;
			dLight.mType	=DirectLight.DLight_Surface;
			
			dLight.mIntensity	=faceLight * mArea;

			//Make sure the emitter ends up with some light too
			mRadFinal	+=mReflectivity * dLight.mIntensity;
		}


		static internal bool BuildTriPatchFromList(RADPatch startPatch,
			Bounds bounds, TriPatch tri, int patchSize)
		{
			for(RADPatch patch=startPatch;patch != null;patch=patch.mNext)
			{
				int	k;
				for(k=0;k < 3;k++)
				{
					if(Utility64.Mathery.VecIdx(patch.mOrigin, k)
						< Utility64.Mathery.VecIdx(bounds.mMins, k) - (patchSize * 2))
					{
						break;
					}
					if(Utility64.Mathery.VecIdx(patch.mOrigin, k)
						> Utility64.Mathery.VecIdx(bounds.mMaxs, k) + (patchSize * 2))
					{
						break;
					}
				}
				if(k != 3)
				{
					continue;
				}
				
				if(!tri.AddPoint(patch))
				{
					Map.Print("AbsorbPatches:  Could not add patch to triangulation.\n");
					return	false;
				}						
			}
			return	true;
		}


		internal bool Check()
		{
			for(int i=0;i < 3;i++)
			{
				if(Utility64.Mathery.VecIdx(mRadFinal, i) < 0.0f)
				{
					Map.Print("CheckPatch:  Bad final radiosity Color in patch.\n");
					return	false;
				}
			}
			return	true;
		}


		internal bool Finalize(GFXPlane facePlane, int planeSide, CoreDelegates.GetNodeLandedIn findNode)
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
					Dist	=Utility64.Mathery.VecIdx(mBounds.mMaxs, i)
								- Utility64.Mathery.VecIdx(mBounds.mMins, i);
					
					if(Dist > patchSize)
					{
						//Cut it right through the center...
						Plane.mNormal	=Vector3.Zero;
						Utility64.Mathery.VecIdxAssign(ref Plane.mNormal, i, 1.0f);
						Plane.mDist	=(Utility64.Mathery.VecIdx(mBounds.mMaxs, i)
							+ Utility64.Mathery.VecIdx(mBounds.mMins, i))
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
					Min	=Utility64.Mathery.VecIdx(mBounds.mMins, i) + 1.0f;
					Max	=Utility64.Mathery.VecIdx(mBounds.mMaxs, i) - 1.0f;

					if(Math.Floor(Min / patchSize)
						< Math.Floor(Max / patchSize))
					{
						Plane.mNormal	=Vector3.Zero;
						Utility64.Mathery.VecIdxAssign(ref Plane.mNormal, i, 1.0f);
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


		static internal void ApplyLightList(RADPatch rpList, int lightGridSize, Vector3 []rgb, Vector3 []facePoints)
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
						if(Utility64.Mathery.VecIdx(p.mBounds.mMins, k)
							> Utility64.Mathery.VecIdx(facePoints[vertOfs], k) + lightGridSize)
						{
							break;
						}
						if(Utility64.Mathery.VecIdx(p.mBounds.mMaxs, k)
							< Utility64.Mathery.VecIdx(facePoints[vertOfs], k) - lightGridSize)
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


		internal void AllocPoly(GFXFace gfxFace, int[] indexes, Vector3[] verts)
		{
			mPoly	=new GBSPPoly(gfxFace, indexes, verts);
		}


		internal void AddSample(Vector3 rgb)
		{
			mNumSamples++;
			mRadStart	+=rgb;
		}


		internal void AverageRadStart()
		{
			if(mNumSamples > 0)
			{
				mRadStart	/=mNumSamples;
			}
		}
	}
}
