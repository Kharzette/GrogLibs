using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Character
{
	public class ChannelTarget
	{
		Vector4					mValue;	//value to animate
		Channel.ChannelType	mType;	//target of animation
		string					mSID;	//identifier so channels can match up
		

		public ChannelTarget(Channel.ChannelType type, string sid)
		{
			mType	=type;
			mSID	=sid;
		}


		public Channel.ChannelType GetChannelType()
		{
			return	mType;
		}


		public string GetSID()
		{
			return	mSID;
		}


		public void SetValue(Vector4 val)
		{
			mValue	=val;
		}


		public void SetValue(float val, Channel.AxisTarget targ)
		{
			switch(targ)
			{
				case Channel.AxisTarget.X:
					mValue.X	=val;
					break;
				case Channel.AxisTarget.Y:
					mValue.Y	=val;
					break;
				case Channel.AxisTarget.Z:
					mValue.Z	=val;
					break;
				case Channel.AxisTarget.W:
					mValue.W	=val;
					break;
			}
		}


		public bool IsRotation()
		{
			return	mType == Channel.ChannelType.ROTATE;
		}


		public Matrix GetMatrix()
		{
			Matrix	ret	=Matrix.Identity;
			switch(mType)
			{
				case Channel.ChannelType.ROTATE:
					Vector3	axis;
					axis.X	=mValue.X;
					axis.Y	=mValue.Y;
					axis.Z	=mValue.Z;
					ret	=Matrix.CreateFromAxisAngle(axis, MathHelper.ToRadians(mValue.W));
					break;
					
				case Channel.ChannelType.SCALE:
					Vector3	s;
					s.X	=mValue.X;
					s.Y	=mValue.Y;
					s.Z	=mValue.Z;
					ret	=Matrix.CreateScale(s);
					break;

				case Channel.ChannelType.TRANSLATE:
					Vector3 t;
					t.X	=mValue.X;
					t.Y	=mValue.Y;
					t.Z	=mValue.Z;
					ret	=Matrix.CreateTranslation(t);
					break;
			}
			return	ret;
		}
	}
}