﻿using System;
using System.Numerics;
using System.IO;


namespace BSPCore;

public class GFXLeaf
{
	public UInt32	mContents;
	public Vector3	mMins, mMaxs;
	public Int32	mFirstFace;
	public Int32	mNumFaces;
	public Int32	mFirstPortal;
	public Int32	mNumPortals;
	public Int32	mCluster;
	public Int32	mArea;
	public Int32	mFirstSide;
	public Int32	mNumSides;


	public GFXLeaf() {	}
	public GFXLeaf(QLeaf ql)
	{
		mContents		=(uint)ql.mContents;
		mCluster		=ql.mCluster;
		mArea			=ql.mArea;
		mFirstPortal	=(Int32)ql.mFirstLeafFace;

		mMins	=new Vector3(ql.mMinX, ql.mMinY, ql.mMinZ);
		mMaxs	=new Vector3(ql.mMinX, ql.mMinY, ql.mMinZ);
	}

	public void Write(BinaryWriter bw)
	{
		bw.Write(mContents);
		bw.Write(mMins.X);
		bw.Write(mMins.Y);
		bw.Write(mMins.Z);
		bw.Write(mMaxs.X);
		bw.Write(mMaxs.Y);
		bw.Write(mMaxs.Z);
		bw.Write(mFirstFace);
		bw.Write(mNumFaces);
		bw.Write(mFirstPortal);
		bw.Write(mNumPortals);
		bw.Write(mCluster);
		bw.Write(mArea);
		bw.Write(mFirstSide);
		bw.Write(mNumSides);
	}

	public void Read(BinaryReader br)
	{
		mContents		=br.ReadUInt32();
		mMins.X			=br.ReadSingle();
		mMins.Y			=br.ReadSingle();
		mMins.Z			=br.ReadSingle();
		mMaxs.X			=br.ReadSingle();
		mMaxs.Y			=br.ReadSingle();
		mMaxs.Z			=br.ReadSingle();
		mFirstFace		=br.ReadInt32();
		mNumFaces		=br.ReadInt32();
		mFirstPortal	=br.ReadInt32();
		mNumPortals		=br.ReadInt32();
		mCluster		=br.ReadInt32();
		mArea			=br.ReadInt32();
		mFirstSide		=br.ReadInt32();
		mNumSides		=br.ReadInt32();
	}
}