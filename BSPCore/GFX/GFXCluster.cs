﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BSPCore
{
	public class GFXCluster : UtilityLib.IReadWriteable
	{
		public Int32	mVisOfs;

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