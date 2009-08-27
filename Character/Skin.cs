using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Character
{
	public class Skin
	{
		List<string>	mBoneNames			=new List<string>();
		List<Matrix>	mInverseBindPoses	=new List<Matrix>();
		Matrix			mBindShapeMatrix;
		Matrix			mMaxAdjust;	//coordinate system stuff


		public Skin()
		{
			mMaxAdjust	=Matrix.CreateFromYawPitchRoll(0,
										MathHelper.ToRadians(-90),
										MathHelper.ToRadians(180));
		}


		public void SetBoneNames(List<string> bnames)
		{
			mBoneNames	=bnames;
		}


		public void SetInverseBindPoses(List<Matrix> mats)
		{
			mInverseBindPoses	=mats;
		}


		public void SetBindShapeMatrix(Matrix mat)
		{
			mBindShapeMatrix	=mat;
		}


		public Matrix GetBindShapeMatrix()
		{
			return	mBindShapeMatrix;
		}


		public int GetNumBones()
		{
			return	mBoneNames.Count;
		}


		public Matrix GetBoneByIndex(int idx, Skeleton sk)
		{
			Matrix	ret	=Matrix.Identity;

			sk.GetMatrixForBone(mBoneNames[idx], out ret);

			//multiply by inverse bind pose
			ret	=mInverseBindPoses[idx] * ret * mMaxAdjust;

			return	ret;
		}
	}
}