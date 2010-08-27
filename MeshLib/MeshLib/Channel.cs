using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MeshLib
{
	public class Channel
	{
		public enum ChannelType
		{
			ROTATE,
			SCALE,
			SKEW,
			TRANSLATE,
			LOOKAT,
			MATRIX
		}

		public enum AxisTarget
		{
			X, Y, Z, W
		}

		ChannelTarget	mTarget;		//reference to the target
		AxisTarget		mAxis;			//which part do we modify
		string			mTargetNode;	//node name for target
		string			mTargetSID;		//SID of target channel
		

		public Channel() {}
		public Channel(string targNode, string targSID, AxisTarget at, Skeleton gs)
		{
			mTargetNode	=targNode;
			mTargetSID	=targSID;
			mAxis		=at;

			if(!gs.GetChannelTarget(mTargetNode, mTargetSID, out mTarget))
			{
				Debug.WriteLine("GetChannelTarget failed in Channel Constructor!");
			}
		}


		public Channel(string targNode, string targSID)
		{
			mTargetNode	=targNode;
			mTargetSID	=targSID;
		}


		public Channel(ChannelTarget gct, AxisTarget at)
		{
			mTarget	=gct;
			mAxis	=at;
		}


		public void FixTarget(Skeleton sk)
		{
			if(!sk.GetChannelTarget(mTargetNode, mTargetSID, out mTarget))
			{
				Debug.WriteLine("Couldn't find channeltarget in FixTarget()");
			}
		}


		public void SetValue(float val)
		{
			mTarget.SetValue(val, mAxis);
		}


		public ChannelType GetChannelType()
		{
			return	mTarget.GetChannelType();
		}


		//channel target will be refd from
		//the skeleton
		public void Write(BinaryWriter bw)
		{
			bw.Write((UInt32)mAxis);
			bw.Write(mTargetNode);
			bw.Write(mTargetSID);
		}


		public void Read(BinaryReader br)
		{
			mAxis		=(AxisTarget)br.ReadUInt32();
			mTargetNode	=br.ReadString();
			mTargetSID	=br.ReadString();
		}
	}
}