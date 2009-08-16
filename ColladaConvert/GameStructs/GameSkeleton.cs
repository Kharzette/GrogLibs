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
		string			mName;
		List<GSNode>	mChildren	=new List<GSNode>();

		//channels for animating
		List<GameChannelTarget>	mChannels	=new List<GameChannelTarget>();


		//init from a scenenode
		public GSNode(SceneNode sn)
		{
			mName	=sn.GetName();

			mChannels	=sn.GetGameChannels();

			Dictionary<string, SceneNode>	kids	=sn.GetChildren();

			foreach(KeyValuePair<string, SceneNode> k in kids)
			{
				GSNode	n	=new GSNode(k.Value);

				mChildren.Add(n);
			}
		}


		public Matrix GetMatrix()
		{
			//compose from elements
			Matrix mat	=Matrix.Identity;

			//this should probably be cached
			foreach(GameChannelTarget gc in mChannels)
			{
				if(gc.IsRotation())
				{
					mat	=gc.GetMatrix() * mat;
				}
				else
				{
					mat	*=gc.GetMatrix();
				}
			}

			return	mat;
		}


		public bool GetChannelTarget(string node, string sid, out GameChannelTarget gct)
		{
			if(mName == node)
			{
				foreach(GameChannelTarget gc in mChannels)
				{
					if(gc.GetSID() == sid)
					{
						gct	=gc;
						return	true;
					}
				}
			}
			else
			{
				foreach(GSNode n in mChildren)
				{
					if(n.GetChannelTarget(node, sid, out gct))
					{
						return	true;
					}
				}
			}
			gct	=null;
			return	false;
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


//		public void AdjustRootMatrixForMax()
//		{
//			mBone	*=Matrix.CreateFromYawPitchRoll(0, MathHelper.ToRadians(-90), MathHelper.ToRadians(180));
//		}
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
				//n.AdjustRootMatrixForMax();
			}
		}


		public bool GetChannelTarget(string node, string sid, out GameChannelTarget gct)
		{
			foreach(GSNode n in mRoots)
			{
				if(n.GetChannelTarget(node, sid, out gct))
				{
					return	true;
				}
			}
			gct	=null;
			return	false;
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
	}
}