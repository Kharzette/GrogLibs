using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MeshLib
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


		internal List<string> GetBoneNames()
		{
			return	mBoneNames;
		}


		public void SetBoneNames(string []bnames)
		{
			foreach(string n in bnames)
			{
				mBoneNames.Add(n);
			}
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
				Matrix	mat	=Matrix.Identity;

				mat.M11	=br.ReadSingle();
				mat.M12	=br.ReadSingle();
				mat.M13	=br.ReadSingle();
				mat.M14	=br.ReadSingle();
				mat.M21	=br.ReadSingle();
				mat.M22	=br.ReadSingle();
				mat.M23	=br.ReadSingle();
				mat.M24	=br.ReadSingle();
				mat.M31	=br.ReadSingle();
				mat.M32	=br.ReadSingle();
				mat.M33	=br.ReadSingle();
				mat.M34	=br.ReadSingle();
				mat.M41	=br.ReadSingle();
				mat.M42	=br.ReadSingle();
				mat.M43	=br.ReadSingle();
				mat.M44	=br.ReadSingle();

				mInverseBindPoses.Add(mat);
			}

			mBindShapeMatrix.M11	=br.ReadSingle();
			mBindShapeMatrix.M12	=br.ReadSingle();
			mBindShapeMatrix.M13	=br.ReadSingle();
			mBindShapeMatrix.M14	=br.ReadSingle();
			mBindShapeMatrix.M21	=br.ReadSingle();
			mBindShapeMatrix.M22	=br.ReadSingle();
			mBindShapeMatrix.M23	=br.ReadSingle();
			mBindShapeMatrix.M24	=br.ReadSingle();
			mBindShapeMatrix.M31	=br.ReadSingle();
			mBindShapeMatrix.M32	=br.ReadSingle();
			mBindShapeMatrix.M33	=br.ReadSingle();
			mBindShapeMatrix.M34	=br.ReadSingle();
			mBindShapeMatrix.M41	=br.ReadSingle();
			mBindShapeMatrix.M42	=br.ReadSingle();
			mBindShapeMatrix.M43	=br.ReadSingle();
			mBindShapeMatrix.M44	=br.ReadSingle();
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
				bw.Write(m.M11);
				bw.Write(m.M12);
				bw.Write(m.M13);
				bw.Write(m.M14);
				bw.Write(m.M21);
				bw.Write(m.M22);
				bw.Write(m.M23);
				bw.Write(m.M24);
				bw.Write(m.M31);
				bw.Write(m.M32);
				bw.Write(m.M33);
				bw.Write(m.M34);
				bw.Write(m.M41);
				bw.Write(m.M42);
				bw.Write(m.M43);
				bw.Write(m.M44);
			}

			bw.Write(mBindShapeMatrix.M11);
			bw.Write(mBindShapeMatrix.M12);
			bw.Write(mBindShapeMatrix.M13);
			bw.Write(mBindShapeMatrix.M14);
			bw.Write(mBindShapeMatrix.M21);
			bw.Write(mBindShapeMatrix.M22);
			bw.Write(mBindShapeMatrix.M23);
			bw.Write(mBindShapeMatrix.M24);
			bw.Write(mBindShapeMatrix.M31);
			bw.Write(mBindShapeMatrix.M32);
			bw.Write(mBindShapeMatrix.M33);
			bw.Write(mBindShapeMatrix.M34);
			bw.Write(mBindShapeMatrix.M41);
			bw.Write(mBindShapeMatrix.M42);
			bw.Write(mBindShapeMatrix.M43);
			bw.Write(mBindShapeMatrix.M44);
		}
	}
}