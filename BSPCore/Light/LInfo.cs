using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPCore
{
	public class LInfo
	{
		Dictionary<Int32, Vector3 []>	mRGBLData	=new Dictionary<Int32, Vector3[]>();

		float	mMinU, mMinV;
		float	mMaxU, mMaxV;
		Int32	mLMinU, mLMinV;
		Int32	mLMaxU, mLMaxV;
		Int32	mLSizeU, mLSizeV;

		Int32	mNumLTypes;

		public const int	MAX_LTYPE_INDEX		=256;
		public const int	MAX_LMAP_SIZE		=1024;
		public const int	MAX_LTYPES			=4;


		internal Int32 GetNumLightTypes()
		{
			return	mNumLTypes;
		}


		internal Vector3 []GetRGBLightData(Int32 lightIndex)
		{
			if(mRGBLData.ContainsKey(lightIndex))
			{
				return	mRGBLData[lightIndex];
			}
			return	null;
		}


		internal void CalcInfo(float minU, float minV, float maxU, float maxV, int lightGridSize)
		{
			//Get the Texture U/V mins/max, and Grid aligned lmap mins/max/size
			mMinU	=minU;
			mMinV	=minV;
			mMaxU	=maxU;
			mMaxV	=maxV;

			minU	=(float)Math.Floor(minU / lightGridSize);
			minV	=(float)Math.Floor(minV / lightGridSize);

			maxU	=(float)Math.Ceiling(maxU / lightGridSize);
			maxV	=(float)Math.Ceiling(maxV / lightGridSize);

			mLMinU	=(Int32)minU;
			mLMinV	=(Int32)minV;
			mLMaxU	=(Int32)maxU;
			mLMaxV	=(Int32)maxV;

			mLSizeU	=(Int32)(maxU - minU);
			mLSizeV	=(Int32)(maxV - minV);

			if((mLSizeU + 1) > MAX_LMAP_SIZE || (mLSizeV + 1) > MAX_LMAP_SIZE)
			{
				CoreEvents.Print("CalcFaceInfo:  Mega huge face will break the atlas!\n");
			}
		}


		internal void AllocLightType(int lightIndex, Int32 size)
		{
			if(!mRGBLData.ContainsKey(lightIndex))
			{
				if(mNumLTypes >= LInfo.MAX_LTYPES)
				{
					CoreEvents.Print("Max Light Types on face.\n");
					return;
				}
				mRGBLData.Add(lightIndex, new Vector3[size]);
				mNumLTypes++;
			}
		}


		internal void FreeLightType(int lightIndex)
		{
			mRGBLData.Remove(lightIndex);
		}


		internal void CalcMids(out float MidU, out float MidV)
		{
			MidU	=(mMaxU + mMinU) * 0.5f;
			MidV	=(mMaxV + mMinV) * 0.5f;
		}


		internal void CalcSizeAndStart(Vector2 uvOffset, int lightGridSize,
			out int w, out int h, out float startU, out float startV)
		{
			w		=(mLSizeU) + 1;
			h		=(mLSizeV) + 1;
			startU	=((float)mLMinU + uvOffset.X) * (float)lightGridSize;
			startV	=((float)mLMinV + uvOffset.Y) * (float)lightGridSize;
		}


		internal Int32 GetLWidth()
		{
			return	mLSizeU + 1;
		}


		internal Int32 GetLHeight()
		{
			return	mLSizeV + 1;
		}


		internal Int32 CalcSize()
		{
			return	(mLSizeU + 1) * (mLSizeV + 1);
		}
	}
}
