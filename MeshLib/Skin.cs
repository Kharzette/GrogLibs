using System;
using System.Diagnostics;
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
		Dictionary<int, Matrix>	mInverseBindPoses	=new Dictionary<int, Matrix>();


		public Skin()
		{
		}


		//adds to existing
		public void SetBonePoses(Dictionary<int, Matrix> invBindPoses)
		{
			foreach(KeyValuePair<int, Matrix> bp in invBindPoses)
			{
				if(mInverseBindPoses.ContainsKey(bp.Key))
				{
					//if bone name already added, make sure the
					//inverse bind pose is the same for this skin
					Debug.Assert(Mathery.CompareMatrix(bp.Value, mInverseBindPoses[bp.Key], Mathery.VCompareEpsilon));
					mInverseBindPoses[bp.Key]	=bp.Value;
				}
				else
				{
					mInverseBindPoses.Add(bp.Key, bp.Value);
				}
			}
		}


		//I think this is used for gamecode manipulation of bones
		public Matrix GetBoneByName(string name, Skeleton sk)
		{
			Matrix	ret	=Matrix.Identity;

			sk.GetMatrixForBone(name, out ret);

			int	idx	=sk.GetBoneIndex(name);

			//multiply by inverse bind pose
			ret	=mInverseBindPoses[idx] * ret;

			return	ret;
		}


		public Matrix GetBoneByNameNoBind(string name, Skeleton sk)
		{
			Matrix	ret	=Matrix.Identity;

			sk.GetMatrixForBone(name, out ret);

			return	ret;
		}


		public Matrix GetBoneByIndex(int idx, Skeleton sk)
		{
			Matrix	ret	=Matrix.Identity;

			sk.GetMatrixForBone(idx, out ret);

			Matrix	ibp	=Matrix.Identity;

			if(mInverseBindPoses.ContainsKey(idx))
			{
				ibp	=mInverseBindPoses[idx];
			}

			//multiply by inverse bind pose
			ret	=ibp * ret;

			return	ret;
		}


		public void Read(BinaryReader br)
		{
			mInverseBindPoses.Clear();

			int	numIBP	=br.ReadInt32();
			for(int i=0;i < numIBP;i++)
			{
				int	idx	=br.ReadInt32();

				Matrix	mat	=FileUtil.ReadMatrix(br);

				mInverseBindPoses.Add(idx, mat);
			}
		}


		public void Write(BinaryWriter bw)
		{
			bw.Write(mInverseBindPoses.Count);
			foreach(KeyValuePair<int, Matrix> ibp in mInverseBindPoses)
			{
				bw.Write(ibp.Key);
				FileUtil.WriteMatrix(bw, ibp.Value);
			}
		}
	}
}