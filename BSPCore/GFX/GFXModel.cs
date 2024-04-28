using System;
using System.Numerics;
using System.IO;


namespace BSPCore;

public class GFXModel
{
	public Int32		mRootNode;				// Top level Node in GFXNodes/GFXBNodes
	public Vector3		mMins;
	public Vector3		mMaxs;
	public Vector3		mOrigin;				// Center of model
	public Int32		mFirstFace;				// First face in GFXFaces
	public Int32		mNumFaces;				// Number of faces


	public GFXModel()
	{		
	}

	public GFXModel(QModel conv)
	{
		mRootNode	=conv.mHeadNode;
		mMins		=conv.mBounds.mMins;
		mMaxs		=conv.mBounds.mMaxs;
		mOrigin		=conv.mOrigin;
		mFirstFace	=conv.mFirstFace;
		mNumFaces	=conv.mNumFaces;
	}


	public void Write(BinaryWriter bw)
	{
		bw.Write(mRootNode);
		bw.Write(mMins.X);
		bw.Write(mMins.Y);
		bw.Write(mMins.Z);
		bw.Write(mMaxs.X);
		bw.Write(mMaxs.Y);
		bw.Write(mMaxs.Z);
		bw.Write(mOrigin.X);
		bw.Write(mOrigin.Y);
		bw.Write(mOrigin.Z);
		bw.Write(mFirstFace);
		bw.Write(mNumFaces);
	}

	public void Read(BinaryReader br)
	{
		mRootNode		=br.ReadInt32();
		mMins.X			=br.ReadSingle();
		mMins.Y			=br.ReadSingle();
		mMins.Z			=br.ReadSingle();
		mMaxs.X			=br.ReadSingle();
		mMaxs.Y			=br.ReadSingle();
		mMaxs.Z			=br.ReadSingle();
		mOrigin.X		=br.ReadSingle();
		mOrigin.Y		=br.ReadSingle();
		mOrigin.Z		=br.ReadSingle();
		mFirstFace		=br.ReadInt32();
		mNumFaces		=br.ReadInt32();
	}
}