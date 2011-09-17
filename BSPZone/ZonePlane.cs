using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;

namespace BSPZone
{
	public struct ZonePlane : UtilityLib.IReadWriteable
	{
		public Vector3	mNormal;
		public float	mDist;
		public UInt32	mType;	//PLANE_X, PLANE_Y, etc...

		internal const UInt32	PLANE_X		=0;
		internal const UInt32	PLANE_Y		=1;
		internal const UInt32	PLANE_Z		=2;
		internal const UInt32	PLANE_ANYX	=3;
		internal const UInt32	PLANE_ANYY	=4;
		internal const UInt32	PLANE_ANYZ	=5;
		internal const UInt32	PLANE_ANY	=6;

		internal const UInt32	PSIDE_FRONT		=1;
		internal const UInt32	PSIDE_BACK		=2;
		internal const UInt32	PSIDE_BOTH		=(PSIDE_FRONT | PSIDE_BACK);
		internal const UInt32	PSIDE_FACING	=4;


		public void Write(BinaryWriter bw)
		{
			bw.Write(mNormal.X);
			bw.Write(mNormal.Y);
			bw.Write(mNormal.Z);
			bw.Write(mDist);
			bw.Write(mType);
		}


		public void Read(BinaryReader br)
		{
			mNormal.X	=br.ReadSingle();
			mNormal.Y	=br.ReadSingle();
			mNormal.Z	=br.ReadSingle();
			mDist		=br.ReadSingle();
			mType		=br.ReadUInt32();
		}


		public float DistanceFast(Vector3 pos)
		{
			switch(mType)
			{
				case PLANE_X:
					return	pos.X - mDist;
				case PLANE_Y:
					return	pos.Y - mDist;
				case PLANE_Z:
					return	pos.Z - mDist;

				default:
					return	Vector3.Dot(pos, mNormal) - mDist;
			}
		}


		internal void Inverse()
		{
			mNormal	=-mNormal;
			mDist	=-mDist;
		}
	}
}
