using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SharpDX;

namespace MeshLib
{
	public class Anim
	{
		SubAnim	[]mSubAnims;

		string	mName;		//animation name for the library
		bool	mbLooping;
		bool	mbPingPong;

		Dictionary<string, SubAnim>	mSubAnimsByBone	=new Dictionary<string, SubAnim>();


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

		public float StartTime
		{
			get {
				float	startTime	=0.0f;
				foreach(SubAnim sa in mSubAnims)
				{
					if(startTime < sa.GetStartTime())
					{
						startTime	=sa.GetStartTime();
					}
				}
				return	startTime;
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

				mSubAnimsByBone.Add(sa.GetBoneName(), sa);
			}
		}


		public void TransformBoneAnim(string bone, Matrix trans)
		{
			bool	bFound	=false;
			for(int i=0;i < mSubAnims.Length;i++)
			{
				string	boneName	=mSubAnims[i].GetBoneName();
				if(boneName != bone)
				{
					continue;
				}

				bFound	=true;
				mSubAnims[i].Transform(trans);
			}

			if(!bFound)
			{
				//no animation on bone, create a basic identity key
				List<float>		times	=new List<float>();
				List<KeyFrame>	keys	=new List<KeyFrame>();

				KeyFrame	kf	=new KeyFrame();
				kf.mScale		=Vector3.One;
				kf.mRotation	=Quaternion.Identity;
				kf.Transform(trans);

				keys.Add(kf);
				keys.Add(new KeyFrame(kf));

				float	startTime	=StartTime;
				float	endTime		=StartTime + TotalTime;

				if(startTime >= endTime)
				{
					endTime	+=0.1f;		//empty anim?
				}

				times.Add(startTime);
				times.Add(endTime);

				SubAnim	sa	=new SubAnim(bone, times, keys);

				SubAnim	[]newSubs	=new SubAnim[mSubAnims.Length + 1];

				newSubs[0]	=sa;

				for(int i=0;i < mSubAnims.Length;i++)
				{
					newSubs[i + 1]	=mSubAnims[i];
				}

				mSubAnims	=newSubs;
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


		public void AnimateBone(string boneName, float time, ref KeyFrame key)
		{
			if(mSubAnimsByBone.ContainsKey(boneName))
			{
				mSubAnimsByBone[boneName].Animate(time, ref key);
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

				Matrix	brokenMat	=Matrix.Scaling(brokenKeys[keyIndex].mScale) *
					Matrix.RotationQuaternion(brokenKeys[keyIndex].mRotation) *
					Matrix.Translation(brokenKeys[keyIndex].mPosition);

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