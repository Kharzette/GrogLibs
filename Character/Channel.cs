using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Character
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


		public void SetValue(float val)
		{
			mTarget.SetValue(val, mAxis);
		}
	}
}