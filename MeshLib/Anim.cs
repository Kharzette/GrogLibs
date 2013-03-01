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
			set { mName = UtilityLib.Misc.AssignValue(value); }
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


		public void FixDetatchedSkeleton(Skeleton skel, string brokenNode)
		{
			//make sure we have keys
			KeyFrame	[]brokenKeys	=null;
			float		[]times			=null;
			foreach(SubAnim sa in mSubAnims)
			{
				if(sa.GetBoneName() == brokenNode)
				{
					brokenKeys	=sa.GetKeys();
					times		=sa.GetTimes();
				}
			}

			if(brokenKeys == null)
			{
				return;	//nothing to fix
			}

			//see if the node is parented
			string	parent;
			if(!skel.GetBoneParentName(brokenNode, out parent))
			{
				//nothing to do
				return;
			}

			//animate and figure out where the parent bone is
			int	keyIndex	=0;
			foreach(float time in times)
			{
				Animate(time);

				Matrix	parentMat;
				skel.GetMatrixForBone(parent, out parentMat);

				parentMat	=Matrix.Invert(parentMat);

				Matrix	brokenMat	=Matrix.CreateScale(brokenKeys[keyIndex].mScale) *
					Matrix.CreateFromQuaternion(brokenKeys[keyIndex].mRotation) *
					Matrix.CreateTranslation(brokenKeys[keyIndex].mPosition);

				skel.GetMatrixForBone(brokenNode, out brokenMat);

//				brokenMat	=Matrix.Invert(brokenMat);

				brokenMat	*=parentMat;

//				brokenMat	=Matrix.Invert(brokenMat);

				brokenMat.Decompose(out brokenKeys[keyIndex].mScale,
					out brokenKeys[keyIndex].mRotation,
					out brokenKeys[keyIndex].mPosition);

				keyIndex++;
			}
		}
	}
}