using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MeshLib
{
	public class GSNode
	{
		string			mName;
		List<GSNode>	mChildren	=new List<GSNode>();

		//current pos / rot / scale
		KeyFrame	mKeyValue	=new KeyFrame();



		public GSNode()
		{
		}


		public void AddChild(GSNode kid)
		{
			mChildren.Add(kid);
		}


		public void SetName(string name)
		{
			mName	=UtilityLib.Misc.AssignValue(name);
		}


		public Matrix GetMatrix()
		{
			Matrix	mat	=Matrix.CreateScale(mKeyValue.mScale) *
				Matrix.CreateFromQuaternion(mKeyValue.mRotation) *
				Matrix.CreateTranslation(mKeyValue.mPosition);

			return	mat;
		}


		public bool GetMatrixForBone(string boneName, out Matrix ret)
		{
			if(boneName == mName)
			{
				ret	=GetMatrix();
				return	true;
			}

			foreach(GSNode n in mChildren)
			{
				if(n.GetMatrixForBone(boneName, out ret))
				{
					ret	*=GetMatrix();
					return	true;
				}
			}
			ret	=Matrix.Identity;
			return	false;
		}


		public void Read(BinaryReader br)
		{
			mName	=br.ReadString();

			mKeyValue.Read(br);

			int	numChildren	=br.ReadInt32();
			for(int i=0;i < numChildren;i++)
			{
				GSNode	n	=new GSNode();
				n.Read(br);

				mChildren.Add(n);
			}
		}


		public void Write(BinaryWriter bw)
		{
			bw.Write(mName);

			mKeyValue.Write(bw);

			bw.Write(mChildren.Count);
			foreach(GSNode n in mChildren)
			{
				n.Write(bw);
			}
		}


		internal void GetBoneNames(List<string> names)
		{
			foreach(GSNode n in mChildren)
			{
				n.GetBoneNames(names);
			}
			names.Add(mName);
		}


		internal bool GetBoneKey(string bone, out KeyFrame ret)
		{
			if(mName == bone)
			{
				ret	=mKeyValue;
				return	true;
			}

			foreach(GSNode n in mChildren)
			{
				if(n.GetBoneKey(bone, out ret))
				{
					return	true;
				}
			}
			ret	=null;
			return	false;
		}


		public void SetKey(KeyFrame keyFrame)
		{
			mKeyValue	=keyFrame;
		}
	}


	public class Skeleton
	{
		List<GSNode>	mRoots	=new List<GSNode>();


		public Skeleton()
		{
		}


		public void AddRoot(GSNode gsn)
		{
			mRoots.Add(gsn);
		}


		public void GetBoneNames(List<string> names)
		{
			foreach(GSNode n in mRoots)
			{
				n.GetBoneNames(names);
			}
		}


		public bool GetMatrixForBone(string boneName, out Matrix ret)
		{
			foreach(GSNode n in mRoots)
			{
				if(n.GetMatrixForBone(boneName, out ret))
				{
					return	true;
				}
			}
			ret	=Matrix.Identity;
			return	false;
		}


		public void Read(BinaryReader br)
		{
			int	numRoots	=br.ReadInt32();

			for(int i=0;i < numRoots;i++)
			{
				GSNode	n	=new GSNode();

				n.Read(br);

				mRoots.Add(n);
			}
		}


		public void Write(BinaryWriter bw)
		{
			bw.Write(mRoots.Count);

			foreach(GSNode n in mRoots)
			{
				n.Write(bw);
			}
		}


		public KeyFrame GetBoneKey(string bone)
		{
			KeyFrame	ret	=null;
			foreach(GSNode n in mRoots)
			{
				if(n.GetBoneKey(bone, out ret))
				{
					return	ret;
				}
			}
			return	ret;
		}
	}
}