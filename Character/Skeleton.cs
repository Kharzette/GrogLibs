using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
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


		public void Read(BinaryReader br)
		{
			mName	=br.ReadString();

			int	numChan	=br.ReadInt32();
			mChannels.Clear();
			for(int i=0;i < numChan;i++)
			{
				ChannelTarget	ct	=new ChannelTarget();
				ct.Read(br);

				mChannels.Add(ct);
			}

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

			bw.Write(mChannels.Count);
			foreach(ChannelTarget ct in mChannels)
			{
				ct.Write(bw);
			}

			bw.Write(mChildren.Count);
			foreach(GSNode n in mChildren)
			{
				n.Write(bw);
			}
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
	}
}