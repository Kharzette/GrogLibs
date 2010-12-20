using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class LInfo
	{
		Vector3	[][]mRGBLData	=new Vector3[MAX_LTYPE_INDEX][];
		float	[]mMins			=new float[2];
		float	[]mMaxs			=new float[2];
		Int32	[]mLMaxs		=new int[2];
		Int32	[]mLMins		=new int[2];
		Int32	[]mLSize		=new int[2];
		Int32	mNumLTypes;

		public const int	MAX_LTYPE_INDEX		=12;
		public const int	MAX_LMAP_SIZE		=1024;
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
}
