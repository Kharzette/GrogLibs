using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Storage;

namespace MeshLib
{
	public class AnimLib
	{
		Dictionary<string, Anim>	mAnims	=new Dictionary<string, Anim>();

		Skeleton	mSkeleton;

		//map of kinect bone names to indexes
		Dictionary<string, int>	mKinectJoints	=new Dictionary<string, int>();
		Dictionary<int, string>	mKinectIndexes	=new Dictionary<int, string>();

		//list of influences from kinect bones to biped bones
		//this will be loaded externally (it's a text file)
		Dictionary<string, List<string>>	mBoneMerger	=new Dictionary<string,List<string>>();


		public AnimLib()
		{
			InitKinectDictionary();
		}


		public float GetAnimTime(string key)
		{
			if(mAnims.ContainsKey(key))
			{
				return	mAnims[key].TotalTime;
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
		//doesn't have any new bones
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

			foreach(string bone in newBones)
			{
				if(!existingBones.Contains(bone))
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


		public void Animate(string anim, float time)
		{
			if(mAnims.ContainsKey(anim))
			{
				mAnims[anim].Animate(time);
			}
		}


		public void LoadKinectMotionDat(string fn)
		{
			FileStream		fs	=new FileStream(fn, FileMode.Open, FileAccess.Read);
			StreamReader	sr	=new StreamReader(fs);

			Dictionary<int, List<Vector3>>	boneMovement	=new Dictionary<int,List<Vector3>>();

			foreach(KeyValuePair<int, string> kinectJoint in mKinectIndexes)
			{
				boneMovement.Add(kinectJoint.Key, new List<Vector3>());
			}

			while(!sr.EndOfStream)
			{
				string	frame	=sr.ReadLine();
				string	[]toks	=frame.Split(' ');
				
				for(int i=0;i < boneMovement.Count;i++)
				{
					Vector3	pos	=Vector3.Zero;
					UtilityLib.Mathery.TryParse(toks[(i * 3)], out pos.X);
					UtilityLib.Mathery.TryParse(toks[(i * 3) + 1], out pos.Y);
					UtilityLib.Mathery.TryParse(toks[(i * 3) + 2], out pos.Z);

					boneMovement[i + 1].Add(pos);
				}
			}

			//watch out for huge negative values
		}


		void InitKinectDictionary()
		{
			mKinectJoints.Clear();
			mKinectJoints.Add("XN_SKEL_HEAD", 1);
			mKinectJoints.Add("XN_SKEL_NECK", 2);
			mKinectJoints.Add("XN_SKEL_TORSO", 3);
			mKinectJoints.Add("XN_SKEL_WAIST", 4);
			mKinectJoints.Add("XN_SKEL_LEFT_COLLAR", 5);
			mKinectJoints.Add("XN_SKEL_LEFT_SHOULDER", 6);
			mKinectJoints.Add("XN_SKEL_LEFT_ELBOW", 7);
			mKinectJoints.Add("XN_SKEL_LEFT_WRIST", 8);
			mKinectJoints.Add("XN_SKEL_LEFT_HAND", 9);
			mKinectJoints.Add("XN_SKEL_LEFT_FINGERTIP", 10);
			mKinectJoints.Add("XN_SKEL_RIGHT_COLLAR", 11);
			mKinectJoints.Add("XN_SKEL_RIGHT_SHOULDER", 12);
			mKinectJoints.Add("XN_SKEL_RIGHT_ELBOW", 13);
			mKinectJoints.Add("XN_SKEL_RIGHT_WRIST", 14);
			mKinectJoints.Add("XN_SKEL_RIGHT_HAND", 15);
			mKinectJoints.Add("XN_SKEL_RIGHT_FINGERTIP", 16);
			mKinectJoints.Add("XN_SKEL_LEFT_HIP", 17);
			mKinectJoints.Add("XN_SKEL_LEFT_KNEE", 18);
			mKinectJoints.Add("XN_SKEL_LEFT_ANKLE", 19);
			mKinectJoints.Add("XN_SKEL_LEFT_FOOT", 20);
			mKinectJoints.Add("XN_SKEL_RIGHT_HIP", 21);
			mKinectJoints.Add("XN_SKEL_RIGHT_KNEE", 22);
			mKinectJoints.Add("XN_SKEL_RIGHT_ANKLE", 23);
			mKinectJoints.Add("XN_SKEL_RIGHT_FOOT", 24);

			mKinectIndexes.Clear();
			mKinectIndexes.Add(1, "XN_SKEL_HEAD");
			mKinectIndexes.Add(2, "XN_SKEL_NECK");
			mKinectIndexes.Add(3, "XN_SKEL_TORSO");
			mKinectIndexes.Add(4, "XN_SKEL_WAIST");
			mKinectIndexes.Add(5, "XN_SKEL_LEFT_COLLAR");
			mKinectIndexes.Add(6, "XN_SKEL_LEFT_SHOULDER");
			mKinectIndexes.Add(7, "XN_SKEL_LEFT_ELBOW");
			mKinectIndexes.Add(8, "XN_SKEL_LEFT_WRIST");
			mKinectIndexes.Add(9, "XN_SKEL_LEFT_HAND");
			mKinectIndexes.Add(10, "XN_SKEL_LEFT_FINGERTIP");
			mKinectIndexes.Add(11, "XN_SKEL_RIGHT_COLLAR");
			mKinectIndexes.Add(12, "XN_SKEL_RIGHT_SHOULDER");
			mKinectIndexes.Add(13, "XN_SKEL_RIGHT_ELBOW");
			mKinectIndexes.Add(14, "XN_SKEL_RIGHT_WRIST");
			mKinectIndexes.Add(15, "XN_SKEL_RIGHT_HAND");
			mKinectIndexes.Add(16, "XN_SKEL_RIGHT_FINGERTIP");
			mKinectIndexes.Add(17, "XN_SKEL_LEFT_HIP");
			mKinectIndexes.Add(18, "XN_SKEL_LEFT_KNEE");
			mKinectIndexes.Add(19, "XN_SKEL_LEFT_ANKLE");
			mKinectIndexes.Add(20, "XN_SKEL_LEFT_FOOT");
			mKinectIndexes.Add(21, "XN_SKEL_RIGHT_HIP");
			mKinectIndexes.Add(22, "XN_SKEL_RIGHT_KNEE");
			mKinectIndexes.Add(23, "XN_SKEL_RIGHT_ANKLE");
			mKinectIndexes.Add(24, "XN_SKEL_RIGHT_FOOT");
		}


		public bool LoadBoneMap(string fileName)
		{
			Stream	file	=null;
			file	=new FileStream(fileName, FileMode.Open, FileAccess.Read);
			if(file == null)
			{
				return	false;
			}
			StreamReader	sr	=new StreamReader(file);

			while(!sr.EndOfStream)
			{
				string	line	=sr.ReadLine();

				string	[]toks	=line.Split(' ', '\t');

				//first one will be the game bone
				string	bone	=toks[0];

				bone	=bone.Trim();

				if(bone == "")
				{
					continue;
				}

				//rest go in the map list
				List<string>	influences	=new List<string>();
				for(int i=1;i < toks.Length;i++)
				{
					string	trimmed	=toks[i].Trim();

					if(trimmed != "")
					{
						influences.Add(trimmed);
					}
				}

				mBoneMerger.Add(bone, influences);
			}
			return	true;
		}
	}
}