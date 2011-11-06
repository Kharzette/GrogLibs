using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BSPCore
{
	public class GFXArea : UtilityLib.IReadWriteable
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
