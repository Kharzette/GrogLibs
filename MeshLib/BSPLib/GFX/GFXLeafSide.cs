﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BSPLib
{
	public class GFXLeafSide
	{
		public Int32	mPlaneNum;
		public Int32	mPlaneSide;

		public void Write(BinaryWriter bw)
		{
			bw.Write(mPlaneNum);
			bw.Write(mPlaneSide);
		}

		public void Read(BinaryReader br)
		{
			mPlaneNum	=br.ReadInt32();
			mPlaneSide	=br.ReadInt32();
		}
	}
}