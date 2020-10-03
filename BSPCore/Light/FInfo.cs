using System;
using System.IO;
using System.Collections.Generic;
using SharpDX;
using UtilityLib;


namespace BSPCore
{
	public class FInfo
	{
		Int32		mFace;
		GBSPPlane	mPlane;
		Vector3		mT2WVecU;
		Vector3		mT2WVecV;
		Vector3		mTexOrg;
		Vector3		mCenter;
		Vector3		[]mPoints;


		public FInfo()
		{
		}


		//only used by the recording stuff
		public FInfo(FInfo copyMe)
		{
			mT2WVecU	=copyMe.mT2WVecU;
			mT2WVecV	=copyMe.mT2WVecV;
			mTexOrg		=copyMe.mTexOrg;
			mCenter		=copyMe.mCenter;
		}


		//only used by the recording stuff
		public void WriteVecs(BinaryWriter bw)
		{
			FileUtil.WriteVector3(bw, mT2WVecU);
			FileUtil.WriteVector3(bw, mT2WVecV);
			FileUtil.WriteVector3(bw, mTexOrg);
			FileUtil.WriteVector3(bw, mCenter);
		}


		//only used by the recording stuff
		public void ReadVecs(BinaryReader br)
		{
			mT2WVecU	=FileUtil.ReadVector3(br);
			mT2WVecV	=FileUtil.ReadVector3(br);
			mTexOrg		=FileUtil.ReadVector3(br);
			mCenter		=FileUtil.ReadVector3(br);
		}


		public void GetVecs(out Vector3 texOrg, out Vector3 t2WU,
							out Vector3 t2WV, out Vector3 center)
		{
			texOrg	=mTexOrg;
			t2WU	=mT2WVecU;
			t2WV	=mT2WVecV;
			center	=mCenter;
		}


		internal Int32 GetFaceIndex()
		{
			return	mFace;
		}


		internal void CalcFaceLightInfo(LInfo lightInfo, List<Vector3> verts, int lightGridSize)
		{
			float	minU	=Bounds.MIN_MAX_BOUNDS;
			float	minV	=Bounds.MIN_MAX_BOUNDS;
			float	maxU	=-Bounds.MIN_MAX_BOUNDS;
			float	maxV	=-Bounds.MIN_MAX_BOUNDS;

			mCenter	=Vector3.Zero;

			GBSPPlane	pln;

			pln.mNormal	=mPlane.mNormal;
			pln.mDist	=mPlane.mDist;
			pln.mType	=mPlane.mType;

			Vector3	vecU;
			Vector3	vecV;
			GBSPPlane.TextureAxisFromPlaneGrog(pln.mNormal, out vecU, out vecV);

			foreach(Vector3 vert in verts)
			{
				float	d	=Vector3.Dot(vert, vecU);
				if(d > maxU)
				{
					maxU	=d;
				}
				if(d < minU)
				{
					minU	=d;
				}

				d	=Vector3.Dot(vert, vecV);
				if(d > maxV)
				{
					maxV	=d;
				}
				if(d < minV)
				{
					minV	=d;
				}

				mCenter	+=vert;
			}

			mCenter	/=verts.Count;

			lightInfo.CalcInfo(minU, minV, maxU, maxV, lightGridSize);

			//Get the texture normal from the texture vecs
			//swapping for sharpdx
			Vector3	texNormal	=Vector3.Cross(vecV, vecU);
			texNormal.Normalize();
			
			//Flip it towards plane normal
			float	distScale	=Vector3.Dot(texNormal, mPlane.mNormal);
			if(distScale == 0.0f)
			{
				CoreEvents.Print("CalcFaceInfo:  Invalid Texture vectors for face.\n");
			}
			if(distScale < 0)
			{
				distScale	=-distScale;
				texNormal	=-texNormal;
			}	

			distScale	=1 / distScale;

			//Get the tex to world vectors
			//U
			float	len		=vecU.Length();
			float	dist	=Vector3.Dot(vecU, mPlane.mNormal);

			dist	*=distScale;

			mT2WVecU	=vecU + texNormal * -dist;
			mT2WVecU	*=((1.0f / len) * (1.0f / len));

			//V
			len		=vecV.Length();
			dist	=Vector3.Dot(vecV, mPlane.mNormal);

			dist	*=distScale;

			mT2WVecV	=vecV + texNormal * -dist;
			mT2WVecV	*=((1.0f / len) * (1.0f / len));

			mTexOrg.X	=-vecU.Z * mT2WVecU.X - vecV.Z * mT2WVecV.X;
			mTexOrg.Y	=-vecU.Z * mT2WVecU.Y - vecV.Z * mT2WVecV.Y;
			mTexOrg.Z	=-vecU.Z * mT2WVecU.Z - vecV.Z * mT2WVecV.Z;

			float Dist	=Vector3.Dot(mTexOrg, mPlane.mNormal)
							- mPlane.mDist - 1;
			Dist	*=distScale;

			mTexOrg	=mTexOrg + texNormal * -Dist;
		}


		static unsafe void BlastArray(bool []arr)
		{
			int	len	=arr.Length;
			fixed(bool *pArr = arr)
			{
				bool	*pA	=pArr;

				for(int i=0;i < len / 8;i++)
				{
					*((UInt64 *)pA)	=(UInt64)0;

					pA	+=8;
				}

				for(int i=0;i < len % 8;i++)
				{
					*pA	=false;

					pA++;
				}
			}
		}


		internal void CalcFacePoints(Matrix modelMat, Matrix modelInv, int modelIndex,
			LInfo lightInfo, int lightGridSize, Vector2 UVOfs,
			bool bExtraLightCorrection,
			UtilityLib.TSPool<bool []> boolPool,
			CoreDelegates.IsPointInSolid pointInSolid,
			CoreDelegates.RayCollision rayCollide)
		{
			bool	[]InSolid	=boolPool.GetFreeItem();

			BlastArray(InSolid);

			float	midU, midV;
			lightInfo.CalcMids(out midU, out midV);

			Vector3	faceMid	=mTexOrg + mT2WVecU * midU + mT2WVecV * midV;

			float	startU, startV;
			Int32	width, height;
			lightInfo.CalcSizeAndStart(UVOfs, lightGridSize, out width, out height, out startU, out startV);

			for(int v=0;v < height;v++)
			{
				for(int u=0;u < width;u++)
				{
					int	gridIdx	=(v * width) + u;

					float	curU	=startU + u * lightGridSize;
					float	curV	=startV + v * lightGridSize;

					Vector3	point	=mTexOrg + mT2WVecU * curU + mT2WVecV * curV;

					mPoints[gridIdx]	=Vector3.TransformCoordinate(point, modelMat);

					InSolid[gridIdx]	=pointInSolid(mPoints[gridIdx]);

					//This code block would adjust trace points towards the center
					//of the face to avoid darkened corners and creases.
					//However the distance moved was sometimes unreasonable, and would
					//exceed lightGridSize.  If checked for the distance would sometimes
					//be less along an edge as the point became closer to the center
					//leaving a sort of "notch" in the lightmaps.
					if(false)
					{
						if(InSolid[gridIdx])
						{
							Vector3	colResult;
							if(rayCollide(faceMid, mPoints[gridIdx], modelIndex, modelInv, out colResult))
							{
								Vector3	vect	=faceMid - mPoints[gridIdx];
								vect.Normalize();

								colResult	+=vect;

								float	adjDist	=Vector3.Distance(colResult, mPoints[gridIdx]);
								if(adjDist < (lightGridSize * 0.5f))
								{
									mPoints[gridIdx]	=colResult + vect;
								}
							}
						}
					}
				}
			}

			if(!bExtraLightCorrection)
			{
				boolPool.FlagFreeItem(InSolid);
				return;
			}

			for(int v=0;v < mPoints.Length;v++)
			{
				if(!InSolid[v])
				{
					//Point is good, leave it alone
					continue;
				}

				Vector3	bestPoint	=faceMid;
				float	bestDist	=Bounds.MIN_MAX_BOUNDS;
				
				for(int u=0;u < mPoints.Length;u++)
				{
					if(mPoints[v] == mPoints[u])
					{
						continue;	//We know this point is bad
					}

					if(InSolid[u])
					{
						continue;	//We know this point is bad
					}

					//At this point, we have a good point,
					//now see if it's closer than the current good point
					Vector3	vect	=mPoints[u] - mPoints[v];
					float	Dist	=vect.Length();
					if(Dist < bestDist)
					{
						bestDist	=Dist;
						bestPoint	=mPoints[u];

						if(Dist <= (lightGridSize - 0.1f))
						{
							break;	//This should be good enough...
						}
					}
				}
				mPoints[v]	=bestPoint;
			}

			//free cached vis stuff
			boolPool.FlagFreeItem(InSolid);
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
			mPlane	=new GBSPPlane(pln);
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
}
