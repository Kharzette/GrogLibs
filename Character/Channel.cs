using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	public class GameChannel
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

		public enum ChannelTarget
		{
			X, Y, Z, W
		}

		GameChannelTarget	mTarget;		//reference to the target
		ChannelTarget		mTargetType;	//which part do we modify
		string				mTargetNode;	//node name for target
		string				mTargetSID;		//SID of target channel
		

		public GameChannel(string targNode, string targSID)
		{
			mTargetNode	=targNode;
			mTargetSID	=targSID;
		}


		public GameChannel(GameChannelTarget gct, ChannelTarget ct)
		{
			mTarget		=gct;
			mTargetType	=ct;
		}


		public void SetValue(float val)
		{
			mTarget.SetValue(val, mTargetType);
		}
	}
}