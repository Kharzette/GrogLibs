using System;
using System.IO;

namespace BSPZone;

internal class VisCluster
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