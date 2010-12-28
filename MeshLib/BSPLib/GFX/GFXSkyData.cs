using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;

namespace BSPLib
{
	public class GFXSkyData : IReadWriteable
	{
		public Vector3	mAxis;						// Axis of rotation
		public float	mDpm;						// Degres per minute
		public Int32	[]mTextures	=new Int32[6];	// Texture indexes for all six sides...
		public float	mDrawScale;

		public void Write(BinaryWriter bw)
		{
			bw.Write(mAxis.X);
			bw.Write(mAxis.Y);
			bw.Write(mAxis.Z);
			bw.Write(mDpm);
			bw.Write(mTextures[0]);
			bw.Write(mTextures[1]);
			bw.Write(mTextures[2]);
			bw.Write(mTextures[3]);
			bw.Write(mTextures[4]);
			bw.Write(mTextures[5]);
			bw.Write(mDrawScale);
		}

		public void Read(BinaryReader br)
		{
			mAxis.X			=br.ReadSingle();
			mAxis.Y			=br.ReadSingle();
			mAxis.Z			=br.ReadSingle();
			mDpm			=br.ReadSingle();
			mTextures[0]	=br.ReadInt32();
			mTextures[1]	=br.ReadInt32();
			mTextures[2]	=br.ReadInt32();
			mTextures[3]	=br.ReadInt32();
			mTextures[4]	=br.ReadInt32();
			mTextures[5]	=br.ReadInt32();
			mDrawScale		=br.ReadSingle();
		}
	}
}
