using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
//using Microsoft.Kinect;


namespace MeshLib
{
	public class AnimLib
	{
		Dictionary<string, Anim>	mAnims	=new Dictionary<string, Anim>();

		Skeleton	mSkeleton;


		public AnimLib()
		{
		}


		public float GetAnimTime(string key)
		{
			if(mAnims.ContainsKey(key))
			{
				return	mAnims[key].TotalTime;
			}
			return	-1;
		}


		public float GetAnimStartTime(string key)
		{
			if(mAnims.ContainsKey(key))
			{
				return	mAnims[key].StartTime;
			}
			return	-1;
		}


		public void AddAnim(Anim an)
		{
			mAnims.Add(an.Name, an);
		}


		public void NukeAnim(string key)
		{
			if(mAnims.ContainsKey(key))
			{
				mAnims.Remove(key);

				if(mAnims.Count == 0)
				{
					mSkeleton	=null;
				}
			}
		}


		public void NukeAll()
		{
			mAnims.Clear();
			mSkeleton	=null;
		}


		public Skeleton GetSkeleton()
		{
			return	mSkeleton;
		}


		public void Reduce(string key, float maxError)
		{
			if(mAnims.ContainsKey(key))
			{
//				mAnims[key].Reduce(maxError);
			}
		}


		//ensure the passed in skeleton
		//has all the old bones
		public bool CheckSkeleton(Skeleton sk)
		{
			if(mSkeleton == null)
			{
				return	false;
			}

			List<string>	existingBones	=new List<string>();
			List<string>	newBones		=new List<string>();

			mSkeleton.GetBoneNames(existingBones);
			sk.GetBoneNames(newBones);

			foreach(string bone in existingBones)
			{
				if(!newBones.Contains(bone))
				{
					return	false;
				}
			}
			return	true;
		}


		public void SetSkeleton(Skeleton sk)
		{
			mSkeleton	=sk;
		}


		//used mainly by gui
		public List<Anim>	GetAnims()
		{
			List<Anim>	ret	=new List<Anim>();

			foreach(KeyValuePair<string, Anim> anms in mAnims)
			{
				ret.Add(anms.Value);
			}
			return	ret;
		}


		//this only saves referenced stuff
		//the tool side will still need to enumerate
		//all the textures / shaders
		public void SaveToFile(string fileName)
		{
			FileStream	file	=new FileStream(fileName, FileMode.Create, FileAccess.Write);

			BinaryWriter	bw	=new BinaryWriter(file);

			//write a magic number identifying anim libs
			UInt32	magic	=0xA91BA7E;

			bw.Write(magic);

			//save skeleton
			mSkeleton.Write(bw);

			//write number of anims
			bw.Write(mAnims.Count);

			foreach(KeyValuePair<string, Anim> mat in mAnims)
			{
				mat.Value.Write(bw);
			}

			bw.Close();
			file.Close();
		}


		public bool ReadFromFile(string fileName, bool bTool)
		{
			Stream			file	=null;
			if(bTool)
			{
				file	=new FileStream(fileName, FileMode.Open, FileAccess.Read);
			}
			else
			{
				file	=UtilityLib.FileUtil.OpenTitleFile(fileName);
			}

			if(file == null)
			{
				return	false;
			}
			BinaryReader	br	=new BinaryReader(file);

			//clear existing data
			mAnims.Clear();

			//read magic number
			UInt32	magic	=br.ReadUInt32();

			if(magic != 0xA91BA7E)
			{
				br.Close();
				file.Close();
				return	false;
			}

			mSkeleton	=new Skeleton();
			mSkeleton.Read(br);

			int	numAnims	=br.ReadInt32();

			for(int i=0;i < numAnims;i++)
			{
				Anim	an	=new Anim();

				an.Read(br);

				an.SetBoneRefs(mSkeleton);

				mAnims.Add(an.Name, an);
			}

			br.Close();
			file.Close();

			return	true;
		}


		public void UpdateBoneRefs()
		{
			foreach(KeyValuePair<string, Anim> ans in mAnims)
			{
				ans.Value.SetBoneRefs(mSkeleton);
			}
		}


		//only used by the tools, this makes sure
		//the names used as keys in the dictionaries
		//match the name properties in the objects
		public void UpdateDictionaries()
		{
			restart:
			foreach(KeyValuePair<string, Anim> an in mAnims)
			{
				if(an.Key != an.Value.Name)
				{
					mAnims.Remove(an.Key);
					mAnims.Add(an.Value.Name, an.Value);
					goto restart;
				}
			}
		}


		public void Blend(string anim1, float anim1Time,
			string anim2, float anim2Time, float percentage)
		{
			if(!mAnims.ContainsKey(anim1)
				|| !mAnims.ContainsKey(anim2))
			{
				return;
			}

			Anim	an1	=mAnims[anim1];
			Anim	an2	=mAnims[anim2];

			List<string>	boneNames	=new List<string>();

			mSkeleton.GetBoneNames(boneNames);

			KeyFrame	workKey1	=new KeyFrame();
			KeyFrame	workKey2	=new KeyFrame();

			foreach(string boneName in boneNames)
			{
				an1.AnimateBone(boneName, anim1Time, ref workKey1);
				an2.AnimateBone(boneName, anim2Time, ref workKey2);

				KeyFrame.Lerp(workKey1, workKey2, percentage, mSkeleton.GetBoneKey(boneName));
			}
		}


		public void Animate(string anim, float time)
		{
			if(mAnims.ContainsKey(anim))
			{
				mAnims[anim].Animate(time);
			}
		}


		/*
		public void CreateKinectAnimation(IEnumerable<KinectMap> mapping,
			CaptureData data, string animName)
		{
			Debug.Assert(data.mFrames.Count == data.mTimes.Count);

			if(mSkeleton == null)
			{
				return;
			}

			//create a mapping dictionary
			Dictionary<JointType, KinectMap>	betterMapping	=new Dictionary<JointType, KinectMap>();
			foreach(KinectMap km in mapping)
			{
				betterMapping.Add(km.Joint, km);
			}

			//subanim data to yank out of the kinect data
			Dictionary<string, List<KeyFrame>>	keys	=new Dictionary<string, List<KeyFrame>>();
			Dictionary<string, List<float>>		ktimes	=new Dictionary<string, List<float>>();

			for(int i=0;i < data.mFrames.Count;i++)
			{
				List<Quaternion>	frame		=data.mFrames[i];
				List<JointType>		joints		=data.mJoints[i];
				float				curTime		=data.mTimes[i];

				Debug.Assert(frame.Count == joints.Count);

				for(int j=0;j < frame.Count;j++)
				{
					if(!betterMapping.ContainsKey(joints[j]))
					{
						continue;
					}

					KinectMap	km	=betterMapping[joints[j]];

					string	name	=km.CharBone;
					if(name == null || name == "")
					{
						continue;
					}

					if(!keys.ContainsKey(name))
					{
						keys.Add(name, new List<KeyFrame>());
					}

					KeyFrame	kf		=new KeyFrame();
					KeyFrame	boneKey	=mSkeleton.GetBoneKey(name);

					if(boneKey == null)
					{
						continue;
					}

					kf.mRotation	=frame[j];
					kf.mPosition	=boneKey.mPosition;
					kf.mScale		=boneKey.mScale;

					//do the adjustments
					Quaternion	rotX	=Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathHelper.ToRadians(km.RotX));
					Quaternion	rotY	=Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathHelper.ToRadians(km.RotY));
					Quaternion	rotZ	=Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.ToRadians(km.RotZ));

					Quaternion	rot	=Quaternion.Concatenate(rotX, rotY);
					rot				=Quaternion.Concatenate(rot, rotZ);
					kf.mRotation	=Quaternion.Concatenate(kf.mRotation, rot);

					keys[name].Add(kf);

					//do the times
					if(!ktimes.ContainsKey(name))
					{
						ktimes.Add(name, new List<float>());
					}

					ktimes[name].Add(curTime);
				}
			}

			List<SubAnim>	subAnims	=new List<SubAnim>();

			foreach(KeyValuePair<string, List<KeyFrame>> sub in keys)
			{
				if(!ktimes.ContainsKey(sub.Key))
				{
					continue;
				}

				List<float>	times	=ktimes[sub.Key];
				SubAnim		sa		=new SubAnim(sub.Key, times, sub.Value);

				subAnims.Add(sa);
			}

			Anim	kinAnim	=new Anim(subAnims);

			kinAnim.Name	=animName;

			mAnims.Add(animName, kinAnim);

			kinAnim.SetBoneRefs(mSkeleton);
		}*/
	}
}