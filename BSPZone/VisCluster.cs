using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BSPZone
{
	internal class VisCluster : UtilityLib.IReadWriteable
	{
		internal Int32	mVisOfs;

		public void Write(BinaryWriter bw)
		{
			bw.Write(mVisOfs);
		}

		public void Read(BinaryReader br)
		{
			mVisOfs	=br.ReadInt32();
		}
	}
}
