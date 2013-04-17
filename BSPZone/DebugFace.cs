using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;


namespace BSPZone
{
	public class DebugFace : UtilityLib.IReadWriteable
	{
		public Int32	mFirstVert;
		public Int32	mNumVerts;
		public Int32	mPlaneNum;
		public bool		mbFlipSide;
		public UInt32	mFlags;		//mirror, sky etc
		public string	mMaterial;


		public void Write(BinaryWriter bw)
		{
			bw.Write(mFirstVert);
			bw.Write(mNumVerts);
			bw.Write(mPlaneNum);
			bw.Write(mbFlipSide);
			bw.Write(mFlags);
			bw.Write(mMaterial);
		}

		public void Read(BinaryReader br)
		{
			mFirstVert	=br.ReadInt32();
			mNumVerts	=br.ReadInt32();
			mPlaneNum	=br.ReadInt32();
			mbFlipSide	=br.ReadBoolean();
			mFlags		=br.ReadUInt32();
			mMaterial	=br.ReadString();
		}
	}
}
