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
		List<SubAnim>	[]mControllerAnims;

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
				foreach(List<SubAnim> sal in mControllerAnims)
				{
					foreach(SubAnim sa in sal)
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
			foreach(List<SubAnim> sal in mControllerAnims)
			{
				foreach(SubAnim sa in sal)
				{
					sa.Reduce(maxError);
				}
			}
		}


		public Anim(int numControllers)
		{
			mControllerAnims	=new List<SubAnim>[numControllers];
		}


		public void AddControllerSubAnims(int cidx, List<SubAnim> anims)
		{
			mControllerAnims[cidx]	=anims;
		}


		public void Write(BinaryWriter bw)
		{
			bw.Write(mName);
			bw.Write(mbLooping);
			bw.Write(mbPingPong);

			bw.Write(mControllerAnims.Length);
			foreach(List<SubAnim> sal in mControllerAnims)
			{
				bw.Write(sal.Count);
				foreach(SubAnim sa in sal)
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

			mControllerAnims	=new List<SubAnim>[numSAL];

			for(int i=0;i < numSAL;i++)
			{
				int numSubAnims	=br.ReadInt32();
				mControllerAnims[i]	=new List<SubAnim>();
				for(int j=0;j < numSubAnims;j++)
				{
					SubAnim sa	=new SubAnim();
					sa.Read(br);

					mControllerAnims[i].Add(sa);
				}
			}
		}


		public void FixChannels(Skeleton sk)
		{
			for(int i=0;i < mControllerAnims.Length;i++)
			{
				List<SubAnim>	subs	=mControllerAnims[i];

				foreach(SubAnim an in subs)
				{
					an.FixChannels(sk);
				}
			}
		}


		public void Animate(int cidx, float time)
		{
			List<SubAnim>	subs	=mControllerAnims[cidx];

			foreach(SubAnim an in subs)
			{
				an.Animate(time);
			}
		}


		public void Animate(float time)
		{
			for(int i=0;i < mControllerAnims.Length;i++)
			{
				List<SubAnim>	subs	=mControllerAnims[i];

				foreach(SubAnim an in subs)
				{
					an.Animate(time);
				}
			}
		}
	}
}