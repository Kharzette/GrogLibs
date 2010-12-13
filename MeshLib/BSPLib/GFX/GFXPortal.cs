using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class GFXPortal
	{
		public Vector3	mOrigin;
		public Int32	mLeafTo;

		public void Write(BinaryWriter bw)
		{
			bw.Write(mOrigin.X);
			bw.Write(mOrigin.Y);
			bw.Write(mOrigin.Z);
			bw.Write(mLeafTo);
		}

		public void Read(BinaryReader br)
		{
			mOrigin.X	=br.ReadSingle();
			mOrigin.Y	=br.ReadSingle();
			mOrigin.Z	=br.ReadSingle();
			mLeafTo		=br.ReadInt32();
		}
	}
}
