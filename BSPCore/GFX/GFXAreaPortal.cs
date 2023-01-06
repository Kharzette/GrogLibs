﻿using System;
using System.IO;

namespace BSPCore
{
	public class GFXAreaPortal
	{
		public Int32	mModelNum;
		public Int32	mArea;

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

		public const int	MAX_AREA_PORTALS	=1024;
	}
}
