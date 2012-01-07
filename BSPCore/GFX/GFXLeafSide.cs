using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BSPCore
{
	public class GFXLeafSide : UtilityLib.IReadWriteable
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
