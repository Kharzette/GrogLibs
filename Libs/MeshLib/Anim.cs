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
		SubAnim	[]mSubAnims;

		string	mName;		//animation name for the library
		bool	mbLooping;
		bool	mbPingPong;


		public string Name
		{
			get { return mName; }
			set { mName = value; }
		}
		public float TotalTime
		{
			get {
				float	totTime	=0.0f;
				foreach(SubAnim sa in mSubAnims)
				{
					if(totTime < sa.GetTotalTime())
					{
						totTime	=sa.GetTotalTime();
					}
				}
				return	totTime;
			}
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
				foreach(SubAnim sa in mSubAnims)
				{
					numKeys	+=sa.GetNumKeys();
				}
				return	numKeys;
			}
		}


		public Anim() { }
		public Anim(List<SubAnim> subs)
		{
			mSubAnims	=new SubAnim[subs.Count];

			for(int i=0;i < subs.Count;i++)
			{
				mSubAnims[i]	=subs[i];
			}
		}


		public void Write(BinaryWriter bw)
		{
			bw.Write(mName);
			bw.Write(mbLooping);
			bw.Write(mbPingPong);

			bw.Write(mSubAnims.Length);
			foreach(SubAnim sa in mSubAnims)
			{
				sa.Write(bw);
			}
		}


		public void Read(BinaryReader br)
		{
			mName		=br.ReadString();
			mbLooping	=br.ReadBoolean();
			mbPingPong	=br.ReadBoolean();

			int	numSA	=br.ReadInt32();

			mSubAnims	=new SubAnim[numSA];

			for(int i=0;i < numSA;i++)
			{
				SubAnim	sa	=new SubAnim();

				sa.Read(br);

				mSubAnims[i]	=sa;
			}
		}


		public void LoadKinectMotionDat(string fn, Dictionary<string, int> kinectJoints)
		{
			FileStream		fs	=new FileStream(fn, FileMode.Open, FileAccess.Read);
			StreamReader	sr	=new StreamReader(fs);

			while(!sr.EndOfStream)
			{
				string	frame	=sr.ReadLine();
				string	[]toks	=frame.Split(' ');

				
			}
		}


		public void SetBoneRefs(Skeleton skel)
		{
			for(int i=0;i < mSubAnims.Length;i++)
			{
				string	boneName	=mSubAnims[i].GetBoneName();

				KeyFrame	kf	=skel.GetBoneKey(boneName);

				mSubAnims[i].SetBoneRef(kf);
			}
		}


		public void Animate(float time)
		{
			for(int i=0;i < mSubAnims.Length;i++)
			{
				mSubAnims[i].Animate(time);
			}
		}
	}
}