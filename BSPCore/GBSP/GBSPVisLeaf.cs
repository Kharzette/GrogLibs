using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;


namespace BSPCore
{
	class GBSPVisLeaf
	{
		internal GBSPVisPortal	mPortals;
		internal Int32			mMightSee;
		internal Int32			mCanSee;


		internal void Write(BinaryWriter bw)
		{
			if(mPortals == null)
			{
				bw.Write(-1);
			}
			else
			{
				bw.Write(mPortals.mPortNum);
			}
			bw.Write(mMightSee);
			bw.Write(mCanSee);
		}


		internal void Read(BinaryReader br, GBSPVisPortal[] ports)
		{
			Int32	idx	=br.ReadInt32();

			if(idx >= 0)
			{
				mPortals	=ports[idx];
			}
			mMightSee	=br.ReadInt32();
			mCanSee		=br.ReadInt32();
		}
	}
}
