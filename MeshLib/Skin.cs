using System;
using System.Collections.Generic;
using System.IO;
using SharpDX;
using UtilityLib;


namespace MeshLib
{
	//this is now a master skin, one per character
	//all mesh parts will index in the same way
	//The tool will need to make sure the inverse bind poses
	//are all the same for each bone
	public class Skin
	{
		List<string>	mBoneNames			=new List<string>();
		List<Matrix>	mInverseBindPoses	=new List<Matrix>();
		Matrix			mMaxAdjust;	//coordinate system stuff

		//generated map of bone name to index
		Dictionary<string, int>	mBoneNameIndexes	=new Dictionary<string, int>();


		public Skin()
		{
			mMaxAdjust	=Matrix.RotationYawPitchRoll(0,
										MathUtil.DegreesToRadians(-90),
										MathUtil.DegreesToRadians(180));
		}


		internal List<string> GetBoneNames()
		{
			return	mBoneNames;
		}


		public int GetBoneIndex(string boneName)
		{
			if(!mBoneNameIndexes.ContainsKey(boneName))
			{
				return	-1;
			}
			return	mBoneNameIndexes[boneName];
		}


		public void SetBoneNamesAndPoses(Dictionary<string, Matrix> invBindPoses)
		{
			mBoneNames.Clear();
			mInverseBindPoses.Clear();

			foreach(KeyValuePair<string, Matrix> bp in invBindPoses)
			{
				if(mBoneNames.Contains(bp.Key))
				{
					continue;
				}
				mBoneNames.Add(bp.Key);
				mInverseBindPoses.Add(bp.Value);
			}
			CalcNameToIndexMap();
		}


		public Matrix GetBoneByName(string name, Skeleton sk)
		{
			Matrix	ret	=Matrix.Identity;

			sk.GetMatrixForBone(name, out ret);

			int	idx	=mBoneNameIndexes[name];

			//multiply by inverse bind pose
			ret	=mInverseBindPoses[idx] * ret * mMaxAdjust;

			return	ret;
		}


		public Matrix GetBoneByNameNoBind(string name, Skeleton sk)
		{
			Matrix	ret	=Matrix.Identity;

			sk.GetMatrixForBone(name, out ret);

			return	ret * mMaxAdjust;
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


		public void Read(BinaryReader br)
		{
			mBoneNames.Clear();
			mInverseBindPoses.Clear();

			int	numNames	=br.ReadInt32();
			for(int i=0;i < numNames;i++)
			{
				string	name	=br.ReadString();

				mBoneNames.Add(name);
			}

			int	numInvs	=br.ReadInt32();
			for(int i=0;i < numInvs;i++)
			{
				Matrix	mat	=FileUtil.ReadMatrix(br);
				mInverseBindPoses.Add(mat);
			}

			CalcNameToIndexMap();
		}


		public void Write(BinaryWriter bw)
		{
			bw.Write(mBoneNames.Count);
			foreach(string name in mBoneNames)
			{
				bw.Write(name);
			}

			bw.Write(mInverseBindPoses.Count);
			foreach(Matrix m in mInverseBindPoses)
			{
				FileUtil.WriteMatrix(bw, m);
			}
		}


		void CalcNameToIndexMap()
		{
			for(int i=0;i < mBoneNames.Count;i++)
			{
				mBoneNameIndexes.Add(mBoneNames[i], i);
			}
		}
	}
}