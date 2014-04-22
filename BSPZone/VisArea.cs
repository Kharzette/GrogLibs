using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BSPZone
{
	internal class VisArea
	{
		internal Int32	NumAreaPortals;
		internal Int32	FirstAreaPortal;

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
