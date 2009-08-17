using System;
using System.Collections.Generic;
using System.Xml;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Character
{
	public class GSNode
	{
		string			mName;
		List<GSNode>	mChildren	=new List<GSNode>();

		//channels for animating
		List<ChannelTarget>	mChannels	=new List<ChannelTarget>();



		public GSNode()
		{
		}


		public void AddChild(GSNode kid)
		{
			mChildren.Add(kid);
		}


		public void SetName(string name)
		{
			mName	=name;
		}



		public void SetChannels(List<ChannelTarget> chans)
		{
			mChannels	=chans;
		}


		public Matrix GetMatrix()
		{
			//compose from elements
			Matrix mat	=Matrix.Identity;

			//this should probably be cached
			foreach(ChannelTarget gc in mChannels)
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


		public bool GetChannelTarget(string node, string sid, out ChannelTarget gct)
		{
			if(mName == node)
			{
				foreach(ChannelTarget gc in mChannels)
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
	}


	public class Skeleton
	{
		private	List<GSNode>	mRoots	=new List<GSNode>();


		public Skeleton()
		{
		}


		public void AddRoot(GSNode gsn)
		{
			mRoots.Add(gsn);
		}


		public bool GetChannelTarget(string node, string sid, out ChannelTarget gct)
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