using System;
using System.IO;


namespace BSPCore
{
	public class GFXHeader
	{
		public UInt32	mTag;

		public bool		mbHasVis;
		public bool		mbHasLight;
		public bool		mbHasMaterialVis;

		public void Write(BinaryWriter bw)
		{
			bw.Write(mTag);
			bw.Write(mbHasVis);
			bw.Write(mbHasLight);
			bw.Write(mbHasMaterialVis);
		}

		public void Read(BinaryReader br)
		{
			mTag				=br.ReadUInt32();
			mbHasVis			=br.ReadBoolean();
			mbHasLight			=br.ReadBoolean();
			mbHasMaterialVis	=br.ReadBoolean();
		}
	}
}
