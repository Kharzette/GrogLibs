using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class FInfo
	{
		Int32		mFace;
		GFXPlane	mPlane		=new GFXPlane();
		Vector3		[]mT2WVecs	=new Vector3[2];
		Vector3		mTexOrg;
		Vector3		[]mPoints;
		Vector3		mCenter;

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


		internal void CalcFacePoints(LInfo LightInfo, float UOfs, float VOfs,
			bool bExtraLightCorrection, Map.IsPointInSolid pointInSolid,
			Map.RayCollision rayCollide)
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
}
