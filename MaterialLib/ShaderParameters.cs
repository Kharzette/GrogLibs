using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;


namespace MaterialLib
{
	public class ShaderParameters
	{
		string					mName;
		EffectParameterClass	mClass;
		EffectParameterType		mType;
		string					mValue;


		public ShaderParameters()
		{
			//init these strings to "" instead of null
			//so that blank values save and load properly
			mName	="";
			mValue	="";
		}

		public string Name
		{
			get { return mName; }
			set { mName = UtilityLib.Misc.AssignValue(value); }
		}
		public EffectParameterClass Class
		{
			get { return mClass; }
			set { mClass = value; }
		}
		public EffectParameterType Type
		{
			get { return mType; }
			set { mType = value; }
		}
		public string Value
		{
			get { return mValue; }
			set { mValue = UtilityLib.Misc.AssignValue(value); }
		}


		public void Write(BinaryWriter bw)
		{
			bw.Write(mName);
			bw.Write((UInt32)mClass);
			bw.Write((UInt32)mType);
			bw.Write(mValue);
		}


		public void Read(BinaryReader br)
		{
			mName	=br.ReadString();
			mClass	=(EffectParameterClass)br.ReadUInt32();
			mType	=(EffectParameterType)br.ReadUInt32();
			mValue	=br.ReadString();
		}
	}
}
