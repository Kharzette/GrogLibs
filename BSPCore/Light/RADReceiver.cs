using System;
using System.Collections.Generic;
using System.Text;

namespace BSPLib
{
	public class RADReceiver
	{
		public UInt16	mPatch;
		public UInt16	mAmount;


		internal void Read(System.IO.BinaryReader br)
		{
			mPatch	=br.ReadUInt16();
			mAmount	=br.ReadUInt16();
		}


		internal void Write(System.IO.BinaryWriter bw)
		{
			bw.Write(mPatch);
			bw.Write(mAmount);
		}
	}
}
