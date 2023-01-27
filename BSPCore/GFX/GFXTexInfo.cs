using System;
using System.Numerics;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;


namespace BSPCore;

public class GFXTexInfo
{
	public Vector3	mVecU;
	public Vector3	mVecV;
	public float	mShiftU;
	public float	mShiftV;
	public float	mDrawScaleU;
	public float	mDrawScaleV;
	public UInt32	mFlags;
	public float	mAlpha;
	public string	mMaterial;	//index into MaterialLib


	public void Write(BinaryWriter bw)
	{
		bw.Write(mVecU.X);
		bw.Write(mVecU.Y);
		bw.Write(mVecU.Z);
		bw.Write(mVecV.X);
		bw.Write(mVecV.Y);
		bw.Write(mVecV.Z);
		bw.Write(mShiftU);
		bw.Write(mShiftV);
		bw.Write(mDrawScaleU);
		bw.Write(mDrawScaleV);
		bw.Write(mFlags);
		bw.Write(mAlpha);
		bw.Write(mMaterial);
	}


	public void Read(BinaryReader br)
	{
		mVecU.X				=br.ReadSingle();
		mVecU.Y				=br.ReadSingle();
		mVecU.Z				=br.ReadSingle();
		mVecV.X				=br.ReadSingle();
		mVecV.Y				=br.ReadSingle();
		mVecV.Z				=br.ReadSingle();
		mShiftU				=br.ReadSingle();
		mShiftV				=br.ReadSingle();
		mDrawScaleU			=br.ReadSingle();
		mDrawScaleV			=br.ReadSingle();
		mFlags				=br.ReadUInt32();
		mAlpha				=br.ReadSingle();
		mMaterial			=br.ReadString();
	}
}