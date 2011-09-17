using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BSPZone
{
	internal class VisAreaPortal : UtilityLib.IReadWriteable
	{
		internal Int32	mModelNum;
		internal Int32	mArea;

		public void Write(BinaryWriter bw)
		{
			bw.Write(mModelNum);
			bw.Write(mArea);
		}

		public void Read(BinaryReader br)
		{
			mModelNum	=br.ReadInt32();
			mArea		=br.ReadInt32();
		}
	}
}
