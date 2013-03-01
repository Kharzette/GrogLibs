using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Kinect;


namespace MeshLib
{
	//class only used for the storage and processing of
	//raw data collected from the kinect
	public class CaptureData
	{
		public List<List<Quaternion>>	mFrames		=new List<List<Quaternion>>();
		public List<List<JointType>>	mJoints		=new List<List<JointType>>();
		public List<float>				mTimes	=new List<float>();


		public void Add(Microsoft.Kinect.Skeleton []data, float time)
		{
			int	index	=mFrames.Count;

			if(data[0].TrackingState != SkeletonTrackingState.Tracked)
			{
				return;
			}

			mFrames.Add(new List<Quaternion>());
			mJoints.Add(new List<JointType>());

			foreach(BoneOrientation bone in data[0].BoneOrientations)
			{
				Quaternion	quat	=Quaternion.Identity;

				quat.X	=bone.HierarchicalRotation.Quaternion.X;
				quat.Y	=bone.HierarchicalRotation.Quaternion.Y;
				quat.Z	=bone.HierarchicalRotation.Quaternion.Z;
				quat.W	=bone.HierarchicalRotation.Quaternion.W;

				mFrames[index].Add(quat);
				mJoints[index].Add(bone.EndJoint);
			}

			mTimes.Add(time);
		}
		
		
		public void Clear()
		{
			mFrames.Clear();
			mJoints.Clear();
			mTimes.Clear();
		}


		public void Write(BinaryWriter bw)
		{
			bw.Write(mFrames.Count);
			foreach(List<Quaternion> frame in mFrames)
			{
				bw.Write(frame.Count);

				foreach(Quaternion quat in frame)
				{
					bw.Write(quat.X);
					bw.Write(quat.Y);
					bw.Write(quat.Z);
					bw.Write(quat.W);
				}
			}

			bw.Write(mJoints.Count);
			foreach(List<JointType> frameJoints in mJoints)
			{
				bw.Write(frameJoints.Count);

				foreach(JointType jt in frameJoints)
				{
					bw.Write((UInt32)jt);
				}
			}

			bw.Write(mTimes.Count);

			foreach(float time in mTimes)
			{
				bw.Write(time);
			}
		}


		public void Read(BinaryReader br)
		{
			mFrames.Clear();
			mJoints.Clear();
			mTimes.Clear();

			int	frameCount	=br.ReadInt32();

			for(int i=0;i < frameCount;i++)
			{
				int	quatCount	=br.ReadInt32();

				mFrames.Add(new List<Quaternion>());

				for(int j=0;j < quatCount;j++)
				{
					Quaternion	q	=Quaternion.Identity;

					q.X	=br.ReadSingle();
					q.Y	=br.ReadSingle();
					q.Z	=br.ReadSingle();
					q.W	=br.ReadSingle();

					mFrames[i].Add(q);
				}
			}

			int	jointFrames	=br.ReadInt32();
			for(int i=0;i < jointFrames;i++)
			{
				int	jointCount	=br.ReadInt32();

				mJoints.Add(new List<JointType>());

				for(int j=0;j < jointCount;j++)
				{
					mJoints[i].Add((JointType)br.ReadUInt32());
				}
			}

			int	timeCount	=br.ReadInt32();
			for(int i=0;i < timeCount;i++)
			{
				mTimes.Add(br.ReadSingle());
			}
		}


		public void TrimStart(int p)
		{
			mFrames.RemoveRange(0, p);
			mJoints.RemoveRange(0, p);
			mTimes.RemoveRange(0, p);

			float	firstTime	=mTimes[0];

			//not sure how to adjust a whole list of value types
			List<float>	dupe	=new List<float>(mTimes);

			mTimes.Clear();

			foreach(float f in dupe)
			{
				mTimes.Add(f - firstTime);
			}
		}


		public void TrimEnd(int p)
		{
			mFrames.RemoveRange(mFrames.Count - p, p);
			mJoints.RemoveRange(mJoints.Count - p, p);
			mTimes.RemoveRange(mTimes.Count - p, p);
		}
	}
}
