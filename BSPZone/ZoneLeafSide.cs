using System;
using System.IO;

namespace BSPZone
{
	public class ZoneLeafSide
	{
		public Int32	mPlaneNum;
		public bool		mbFlipSide;

		public void Write(BinaryWriter bw)
		{
			bw.Write(mPlaneNum);
			bw.Write(mbFlipSide);
		}

		public void Read(BinaryReader br)
		{
			mPlaneNum	=br.ReadInt32();
			mbFlipSide	=br.ReadBoolean();
		}
	}
}
