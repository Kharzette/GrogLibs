using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MeshLib
{
	public class Anim
	{
		List<FloatKeys>	[]mControllerAnims;

		string	mName;		//animation name for the library
		bool	mbLooping;
		bool	mbPingPong;


		public string Name
		{
			get { return mName; }
			set { mName = value; }
		}
		public bool Looping
		{
			get { return mbLooping; }
			set { mbLooping = value; }
		}
		public bool PingPong
		{
			get { return mbPingPong; }
			set { mbPingPong = value; }
		}
		public int NumKeyFrames
		{
			get {
				int	numKeys	=0;
				foreach(List<FloatKeys> sal in mControllerAnims)
				{
					foreach(FloatKeys sa in sal)
					{
						numKeys	+=sa.GetNumKeys();
					}
				}
				return	numKeys;
			}

		}


		public Anim()
		{
		}


		public void Reduce(float maxError)
		{
			foreach(List<FloatKeys> sal in mControllerAnims)
			{
				foreach(FloatKeys sa in sal)
				{
					sa.Reduce(maxError);
				}
			}
		}


		public Anim(int numControllers)
		{
			mControllerAnims	=new List<FloatKeys>[numControllers];
		}


		public void AddControllerSubAnims(int cidx, List<FloatKeys> anims)
		{
			mControllerAnims[cidx]	=anims;
		}


		public void Write(BinaryWriter bw)
		{
			bw.Write(mName);
			bw.Write(mbLooping);
			bw.Write(mbPingPong);

			bw.Write(mControllerAnims.Length);
			foreach(List<FloatKeys> sal in mControllerAnims)
			{
				bw.Write(sal.Count);
				foreach(FloatKeys sa in sal)
				{
					sa.Write(bw);
				}
			}
		}


		public void Read(BinaryReader br)
		{
			mName		=br.ReadString();
			mbLooping	=br.ReadBoolean();
			mbPingPong	=br.ReadBoolean();

			int	numSAL	=br.ReadInt32();

			mControllerAnims	=new List<FloatKeys>[numSAL];

			for(int i=0;i < numSAL;i++)
			{
				int numSubAnims	=br.ReadInt32();
				mControllerAnims[i]	=new List<FloatKeys>();
				for(int j=0;j < numSubAnims;j++)
				{
					FloatKeys sa	=new FloatKeys();
					sa.Read(br);

					mControllerAnims[i].Add(sa);
				}
			}
		}


		public void FixChannels(Skeleton sk)
		{
			for(int i=0;i < mControllerAnims.Length;i++)
			{
				List<FloatKeys>	subs	=mControllerAnims[i];

				foreach(FloatKeys an in subs)
				{
					an.FixChannels(sk);
				}
			}
		}


		public void Animate(int cidx, float time)
		{
			List<FloatKeys>	subs	=mControllerAnims[cidx];

			foreach(FloatKeys an in subs)
			{
				an.Animate(time);
			}
		}


		public void Animate(float time)
		{
			for(int i=0;i < mControllerAnims.Length;i++)
			{
				List<FloatKeys>	subs	=mControllerAnims[i];

				foreach(FloatKeys an in subs)
				{
					an.Animate(time);
				}
			}
		}


		public void Consolidate()
		{
			List<string>	handled	=new List<string>();

			for(int i=0;i < mControllerAnims.Length;i++)
			{
				List<FloatKeys>	subs	=mControllerAnims[i];

				foreach(FloatKeys an in subs)
				{
					string			targNode	=an.GetTargetNode();
					List<FloatKeys>	related		=new List<FloatKeys>();

					related.Add(an);

					foreach(FloatKeys an2 in subs)
					{
						if(an2 == an)
						{
							continue;
						}
						if(an2.GetTargetNode() == targNode)
						{
							related.Add(an2);
						}
					}

					//keep a list of already handled nodetargets
					//eliminate 1.0 scales and the like

					int	j=0;
					j++;
				}
			}
		}
	}
}