using System;
using System.Collections.Generic;
using System.Xml;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	public class GSNode
	{
		private	Matrix			mBone;
		private	string			mName;
		private List<GSNode>	mChildren	=new List<GSNode>();


		//init from a scenenode
		public GSNode(SceneNode sn)
		{
			mName	=sn.GetName();
			mBone	=sn.GetMatrix();

			Dictionary<string, SceneNode>	kids	=sn.GetChildren();

			foreach(KeyValuePair<string, SceneNode> k in kids)
			{
				GSNode	n	=new GSNode(k.Value);

				mChildren.Add(n);
			}
		}


		public bool GetMatrixForBone(string boneName, out Matrix ret)
		{
			if(boneName == mName)
			{
				ret	=mBone;
				return	true;
			}

			foreach(GSNode n in mChildren)
			{
				if(n.GetMatrixForBone(boneName, out ret))
				{
					return	true;
				}
			}
			ret	=Matrix.Identity;
			return	false;
		}


		public void AdjustRootMatrixForMax()
		{
			mBone	*=Matrix.CreateFromYawPitchRoll(0, MathHelper.ToRadians(-90), MathHelper.ToRadians(180));
		}
	}


	public class GameSkeleton
	{
		private	List<GSNode>	mRoots	=new List<GSNode>();


		public GameSkeleton(Dictionary<string, SceneNode> sns)
		{
			//extract collada scene nodes into gameskel
			foreach(KeyValuePair<string, SceneNode> sn in sns)
			{
				GSNode	n	=new GSNode(sn.Value);
				mRoots.Add(n);

				//adjust for max coordinate system
				n.AdjustRootMatrixForMax();
			}
		}


		public bool GetMatrixForBone(string boneName, out Matrix ret)
		{
			foreach(GSNode n in mRoots)
			{
				if(GetMatrixForBone(boneName, out ret))
				{
					return	true;
				}
			}
			ret	=Matrix.Identity;
			return	false;
		}
	}
}