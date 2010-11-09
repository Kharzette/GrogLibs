using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	public class ChannelTarget
	{
		Vector4				mValue;	//value to animate
		Channel.ChannelType	mType;	//target of animation
		string				mSID;	//identifier so channels can match up
		

		public ChannelTarget() { }
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


		public void Write(BinaryWriter bw)
		{
			bw.Write(mValue.X);
			bw.Write(mValue.Y);
			bw.Write(mValue.Z);
			bw.Write(mValue.W);
			bw.Write((UInt32)mType);
			bw.Write(mSID);
		}


		public void Read(BinaryReader br)
		{
			mValue		=Vector4.Zero;
			mValue.X	=br.ReadSingle();
			mValue.Y	=br.ReadSingle();
			mValue.Z	=br.ReadSingle();
			mValue.W	=br.ReadSingle();
			mType		=(Channel.ChannelType)br.ReadUInt32();
			mSID		=br.ReadString();
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
					Vector3	axis	=Vector3.Zero;
					axis.X	=mValue.X;
					axis.Y	=mValue.Y;
					axis.Z	=mValue.Z;
					ret	=Matrix.CreateFromAxisAngle(axis, MathHelper.ToRadians(mValue.W));
					break;
					
				case Channel.ChannelType.SCALE:
					Vector3	s	=Vector3.Zero;
					s.X	=mValue.X;
					s.Y	=mValue.Y;
					s.Z	=mValue.Z;
					ret	=Matrix.CreateScale(s);
					break;

				case Channel.ChannelType.TRANSLATE:
					Vector3 t	=Vector3.Zero;
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