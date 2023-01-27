using System;
using System.IO;
using System.Text;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using Vortice.Mathematics;
using Vortice.Mathematics.PackedVector;
using MaterialLib;
using MeshLib;
using UtilityLib;

using MatLib	=MaterialLib.MaterialLib;


namespace BSPCore;

public struct QEdge
{
	public UInt16	v0, v1;
}

public struct QFace
{
	public UInt16	mPlaneNum;
	public Int16	mSide;

	public int		mFirstEdge;
	public Int16	mNumEdges;
	public Int16	mTexInfo;

	public Color	mStyles;
	public int		mLightOfs;		
}

public struct QModel
{
	public Bounds	mBounds;
	public Vector3	mOrigin;
	public int		mHeadNode;
	public int		mFirstFace, mNumFaces;
}

public class QBSPFile
{
	public Vector3		[]mVerts;
	public GBSPPlane	[]mPlanes;
	public TexInfo		[]mTexInfos;
	public QEdge		[]mEdges;
	public QFace		[]mFaces;
	public int			[]mSurfEdges;
	public QModel		[]mModels;
	public byte			[]mLightData;

	const int	HeaderLumps	=19;

	public QBSPFile(string fileName)
	{
		FileStream	fs	=new FileStream(fileName, FileMode.Open, FileAccess.Read);
		if(fs == null)
		{
			return;
		}

		BinaryReader	br	=new BinaryReader(fs);

		UInt32	magic	=br.ReadUInt32();
		if(magic != 0x50534249)	//IBSP
		{
			br.Close();
			fs.Close();
			return;
		}

		UInt32	version	=br.ReadUInt32();
		if(version != 38)
		{
			br.Close();
			fs.Close();
			return;
		}

		uint	[]offsets	=new uint[HeaderLumps];
		uint	[]lens		=new uint[HeaderLumps];

		for(int i=0;i < HeaderLumps;i++)
		{
			offsets[i]	=br.ReadUInt32();
			lens[i]		=br.ReadUInt32();
		}

		//verts are index 2
		br.BaseStream.Seek(offsets[2], SeekOrigin.Begin);

		uint	numVerts	=lens[2] / 12;	//vec3 size
		mVerts	=new Vector3[numVerts];
		for(int i=0;i < numVerts;i++)
		{
			mVerts[i]	=FileUtil.ReadVector3(br);
		}

		//planes are index 1
		br.BaseStream.Seek(offsets[1], SeekOrigin.Begin);

		uint	numPlanes	=lens[1] / 20;	//plane size
		mPlanes	=new GBSPPlane[numPlanes];

		for(int i=0;i < numPlanes;i++)
		{
			mPlanes[i].mNormal	=FileUtil.ReadVector3(br);
			mPlanes[i].mDist	=br.ReadSingle();
			mPlanes[i].mType	=br.ReadUInt32();
		}


		//load texinfos, index 5
		br.BaseStream.Seek(offsets[5], SeekOrigin.Begin);

		uint	numTexInfo	=lens[5] / (44 + 32);

		mTexInfos	=new TexInfo[numTexInfo];

		for(int i=0;i < numTexInfo;i++)
		{
			mTexInfos[i]	=new TexInfo();

			mTexInfos[i].QRead(br);

			mTexInfos[i].mUVec	=Vector3.TransformNormal(
				mTexInfos[i].mUVec, Map.mGrogTransform);
			mTexInfos[i].mVVec	=Vector3.TransformNormal(
				mTexInfos[i].mVVec, Map.mGrogTransform);
		}

		//load edges, index 11
		br.BaseStream.Seek(offsets[11], SeekOrigin.Begin);

		uint	numEdges	=lens[11] / 4;

		mEdges	=new QEdge[numEdges];
		for(int i=0;i < numEdges;i++)
		{
			mEdges[i].v0	=br.ReadUInt16();
			mEdges[i].v1	=br.ReadUInt16();
		}

		//load faces, index 6
		br.BaseStream.Seek(offsets[6], SeekOrigin.Begin);

		uint	numFaces	=lens[6] / 20;

		mFaces	=new QFace[numFaces];
		for(int i=0;i < numFaces;i++)
		{
			mFaces[i].mPlaneNum		=br.ReadUInt16();
			mFaces[i].mSide			=br.ReadInt16();
			mFaces[i].mFirstEdge	=br.ReadInt32();
			mFaces[i].mNumEdges		=br.ReadInt16();
			mFaces[i].mTexInfo		=br.ReadInt16();
			mFaces[i].mStyles		=br.ReadUInt32();
			mFaces[i].mLightOfs		=br.ReadInt32();
		}

		//need surfedges, index 12
		br.BaseStream.Seek(offsets[12], SeekOrigin.Begin);

		uint	numSurfEdges	=lens[12] / 4;

		mSurfEdges	=new int[numSurfEdges];
		for(int i=0;i < numSurfEdges;i++)
		{
			mSurfEdges[i]	=br.ReadInt32();
		}

		//models, lucky 13
		br.BaseStream.Seek(offsets[13], SeekOrigin.Begin);

		uint	numModels	=lens[13] / 48;

		mModels	=new QModel[numModels];
		for(int i=0;i < numModels;i++)
		{
			mModels[i].mBounds	=new Bounds();

			mModels[i].mBounds.mMins	=FileUtil.ReadVector3(br);
			mModels[i].mBounds.mMaxs	=FileUtil.ReadVector3(br);

			mModels[i].mOrigin	=FileUtil.ReadVector3(br);

			mModels[i].mHeadNode	=br.ReadInt32();

			mModels[i].mFirstFace	=br.ReadInt32();
			mModels[i].mNumFaces	=br.ReadInt32();
		}

		//light data, lucky 7
		br.BaseStream.Seek(offsets[7], SeekOrigin.Begin);

		uint	numLight	=lens[7];

		mLightData	=new byte[numLight];

		br.Read(mLightData, 0, (int)numLight);

		br.Close();
		fs.Close();
	}


	public void	GetDrawData(List<Vector3> verts, List<Vector3> norms, List<Color> cols, List<UInt16> inds)
	{
		Random	rng		=new Random();
		UInt16	count	=0;
		for(int i=0;i < mFaces.Length;i++)
		{
			Color	col	=Mathery.RandomColor(rng);

			Vector3	norm	=mPlanes[mFaces[i].mPlaneNum].mNormal;

			if(mFaces[i].mSide != 0)
			{
				norm	=-norm;
			}

			norm	=Vector3.TransformNormal(norm, Map.mGrogTransform);

			int	firstEdge	=mFaces[i].mFirstEdge;

			for(int j=1;j < mFaces[i].mNumEdges - 1;j++)
			{
				int	edge	=mSurfEdges[firstEdge];

				UInt16	index	=0;
				if(edge < 0)
				{
					index	=mEdges[-edge].v1;
				}
				else
				{
					index	=mEdges[edge].v0;
				}
				verts.Add(Vector3.Transform(mVerts[index], Map.mGrogTransform));
				inds.Add(count++);
				norms.Add(norm);
				cols.Add(col);

				edge	=mSurfEdges[firstEdge + j];
				index	=0;
				if(edge < 0)
				{
					index	=mEdges[-edge].v1;
				}
				else
				{
					index	=mEdges[edge].v0;
				}

				verts.Add(Vector3.Transform(mVerts[index], Map.mGrogTransform));
				inds.Add(count++);
				norms.Add(norm);
				cols.Add(col);

				edge	=mSurfEdges[firstEdge + ((j + 1) % mFaces[i].mNumEdges)];
				index	=0;
				if(edge < 0)
				{
					index	=mEdges[-edge].v1;
				}
				else
				{
					index	=mEdges[edge].v0;
				}

				verts.Add(Vector3.Transform(mVerts[index], Map.mGrogTransform));
				inds.Add(count++);
				norms.Add(norm);
				cols.Add(col);
			}
		}		
	}
}