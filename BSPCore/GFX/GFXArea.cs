using System;
using System.IO;

namespace BSPCore
{
	//area stuff doesn't really work yet
	public class GFXArea
	{
		public Int32	NumAreaPortals;
		public Int32	FirstAreaPortal;

		public void Write(BinaryWriter bw)
		{
			bw.Write(NumAreaPortals);
			bw.Write(FirstAreaPortal);
		}

		public void Read(BinaryReader br)
		{
			NumAreaPortals	=br.ReadInt32();
			FirstAreaPortal	=br.ReadInt32();
		}
	}
}
