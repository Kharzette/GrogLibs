using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class GFXBNode
	{
		Int32	[]mChildren	=new Int32[2];
		Int32	mPlaneNum;

		public void Write(BinaryWriter bw)
		{
			bw.Write(mChildren[0]);
			bw.Write(mChildren[1]);
			bw.Write(mPlaneNum);
		}

		public void Read(BinaryReader br)
		{
			mChildren[0]	=br.ReadInt32();
			mChildren[1]	=br.ReadInt32();
			mPlaneNum		=br.ReadInt32();
		}
	}
}
