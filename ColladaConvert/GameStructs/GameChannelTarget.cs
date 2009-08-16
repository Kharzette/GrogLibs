using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	public class GameChannelTarget
	{
		Vector4					mValue;	//value to animate
		GameChannel.ChannelType	mType;	//target of animation
		string					mSID;	//identifier so channels can match up
		

		public GameChannelTarget(GameChannel.ChannelType type, string sid)
		{
			mType	=type;
			mSID	=sid;
		}


		public GameChannel.ChannelType GetChannelType()
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


		public void SetValue(float val, GameChannel.ChannelTarget targ)
		{
			switch(targ)
			{
				case GameChannel.ChannelTarget.X:
					mValue.X	=val;
					break;
				case GameChannel.ChannelTarget.Y:
					mValue.Y	=val;
					break;
				case GameChannel.ChannelTarget.Z:
					mValue.Z	=val;
					break;
				case GameChannel.ChannelTarget.W:
					mValue.W	=val;
					break;
			}
		}


		public bool IsRotation()
		{
			return	mType == GameChannel.ChannelType.ROTATE;
		}


		public Matrix GetMatrix()
		{
			Matrix	ret	=Matrix.Identity;
			switch(mType)
			{
				case GameChannel.ChannelType.ROTATE:
					/*
					Quaternion	q;
					q.X	=mValue.X;
					q.Y	=mValue.Y;
					q.Z	=mValue.Z;
					q.W	=MathHelper.ToRadians(mValue.W);
					ret	=Matrix.CreateFromQuaternion(q);
					*/
					Vector3	axis;
					axis.X	=mValue.X;
					axis.Y	=mValue.Y;
					axis.Z	=mValue.Z;
					return	Matrix.CreateFromAxisAngle(axis, MathHelper.ToRadians(mValue.W));
					break;
					
				case GameChannel.ChannelType.SCALE:
					Vector3	s;
					s.X	=mValue.X;
					s.Y	=mValue.Y;
					s.Z	=mValue.Z;
					ret	=Matrix.CreateScale(s);
					break;

				case GameChannel.ChannelType.TRANSLATE:
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