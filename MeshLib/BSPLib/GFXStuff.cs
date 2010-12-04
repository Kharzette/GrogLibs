using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class GFXArea
	{
		public Int32	NumAreaPortals;
		public Int32	FirstAreaPortal;

		internal void Write(BinaryWriter bw)
		{
			bw.Write(NumAreaPortals);
			bw.Write(FirstAreaPortal);
		}

		internal void Read(BinaryReader br)
		{
			NumAreaPortals	=br.ReadInt32();
			FirstAreaPortal	=br.ReadInt32();
		}
	}


	public class GFXAreaPortal
	{
		public Int32	mModelNum;
		public Int32	mArea;

		internal void Write(BinaryWriter bw)
		{
			bw.Write(mModelNum);
			bw.Write(mArea);
		}

		internal void Read(BinaryReader br)
		{
			mModelNum	=br.ReadInt32();
			mArea		=br.ReadInt32();
		}
	}


	public class GFXModel
	{
		public Int32		[]mRootNode	=new Int32[2];	// Top level Node in GFXNodes/GFXBNodes
		public Vector3		mMins;
		public Vector3		mMaxs;
		public Vector3		mOrigin;						// Center of model
		public Int32		mFirstFace;					// First face in GFXFaces
		public Int32		mNumFaces;					// Number of faces
		public Int32		mFirstLeaf;					// First leaf in GFXLeafs;
		public Int32		mNumLeafs;					// Number of leafs (not including solid leaf)
		public Int32		mFirstCluster;
		public Int32		mNumClusters;
		public Int32		[]mAreas	=new Int32[2];		// Area on each side of the model


		internal void Write(BinaryWriter bw)
		{
			bw.Write(mRootNode[0]);
			bw.Write(mRootNode[1]);
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
			bw.Write(mFirstLeaf);
			bw.Write(mNumLeafs);
			bw.Write(mFirstCluster);
			bw.Write(mNumClusters);
			bw.Write(mAreas[0]);
			bw.Write(mAreas[1]);
		}

		internal void Read(BinaryReader br)
		{
			mRootNode[0]	=br.ReadInt32();
			mRootNode[1]	=br.ReadInt32();
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
			mFirstLeaf		=br.ReadInt32();
			mNumLeafs		=br.ReadInt32();
			mFirstCluster	=br.ReadInt32();
			mNumClusters	=br.ReadInt32();
			mAreas[0]		=br.ReadInt32();
			mAreas[1]		=br.ReadInt32();
		}
	}


	public interface IChunkable
	{
		void Write(BinaryWriter bw);
		void Read(BinaryReader br);
	}


	public class GFXSkyData
	{
		public Vector3	mAxis;						// Axis of rotation
		public float	mDpm;						// Degres per minute
		public Int32	[]mTextures	=new Int32[6];	// Texture indexes for all six sides...
		public float	mDrawScale;
	}


	public class GFXPlane
	{
		public Vector3	mNormal;
		public float	mDist;
		public UInt32	mType;	//PLANE_X, PLANE_Y, etc...

		internal void Write(BinaryWriter bw)
		{
			bw.Write(mNormal.X);
			bw.Write(mNormal.Y);
			bw.Write(mNormal.Z);
			bw.Write(mDist);
			bw.Write(mType);
		}

		internal void Read(BinaryReader br)
		{
			mNormal.X	=br.ReadSingle();
			mNormal.Y	=br.ReadSingle();
			mNormal.Z	=br.ReadSingle();
			mDist		=br.ReadSingle();
			mType		=br.ReadUInt32();
		}
	}


	public class GFXBNode
	{
		Int32	[]mChildren	=new Int32[2];
		Int32	mPlaneNum;

		internal void Write(BinaryWriter bw)
		{
			bw.Write(mChildren[0]);
			bw.Write(mChildren[1]);
			bw.Write(mPlaneNum);
		}

		internal void Read(BinaryReader br)
		{
			mChildren[0]	=br.ReadInt32();
			mChildren[1]	=br.ReadInt32();
			mPlaneNum		=br.ReadInt32();
		}
	}


	public class GFXNode
	{
		public Int32	[]mChildren	=new Int32[2];
		public Int32	mNumFaces;
		public Int32	mFirstFace;
		public Int32	mPlaneNum;
		public Vector3	mMins, mMaxs;


		internal void Write(BinaryWriter bw)
		{
			bw.Write(mChildren[0]);
			bw.Write(mChildren[1]);
			bw.Write(mNumFaces);
			bw.Write(mFirstFace);
			bw.Write(mPlaneNum);
			bw.Write(mMins.X);
			bw.Write(mMins.Y);
			bw.Write(mMins.Z);
			bw.Write(mMaxs.X);
			bw.Write(mMaxs.Y);
			bw.Write(mMaxs.Z);
		}

		internal void Read(BinaryReader br)
		{
			mChildren[0]	=br.ReadInt32();
			mChildren[1]	=br.ReadInt32();
			mNumFaces		=br.ReadInt32();
			mFirstFace		=br.ReadInt32();
			mPlaneNum		=br.ReadInt32();
			mMins.X			=br.ReadSingle();
			mMins.Y			=br.ReadSingle();
			mMins.Z			=br.ReadSingle();
			mMaxs.X			=br.ReadSingle();
			mMaxs.Y			=br.ReadSingle();
			mMaxs.Z			=br.ReadSingle();
		}
	}


	public class GFXLeafSide
	{
		public Int32	mPlaneNum;
		public Int32	mPlaneSide;

		internal void Write(BinaryWriter bw)
		{
			bw.Write(mPlaneNum);
			bw.Write(mPlaneSide);
		}

		internal void Read(BinaryReader br)
		{
			mPlaneNum	=br.ReadInt32();
			mPlaneSide	=br.ReadInt32();
		}
	}


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

		internal void Write(BinaryWriter bw)
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

		internal void Read(BinaryReader br)
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


	public class GFXFace
	{
		public Int32	mFirstVert;
		public Int32	mNumVerts;
		public Int32	mPlaneNum;
		public Int32	mPlaneSide;
		public Int32	mTexInfo;
		public Int32	mLightOfs;
		public Int32	mLWidth;
		public Int32	mLHeight;
		public byte		[]mLTypes	=new byte[4];

		internal void Write(BinaryWriter bw)
		{
			bw.Write(mFirstVert);
			bw.Write(mNumVerts);
			bw.Write(mPlaneNum);
			bw.Write(mPlaneSide);
			bw.Write(mTexInfo);
			bw.Write(mLightOfs);
			bw.Write(mLWidth);
			bw.Write(mLHeight);
			bw.Write(mLTypes[0]);
			bw.Write(mLTypes[1]);
			bw.Write(mLTypes[2]);
			bw.Write(mLTypes[3]);
		}

		internal void Read(BinaryReader br)
		{
			mFirstVert	=br.ReadInt32();
			mNumVerts	=br.ReadInt32();
			mPlaneNum	=br.ReadInt32();
			mPlaneSide	=br.ReadInt32();
			mTexInfo	=br.ReadInt32();
			mLightOfs	=br.ReadInt32();
			mLWidth		=br.ReadInt32();
			mLHeight	=br.ReadInt32();
			mLTypes[0]	=br.ReadByte();
			mLTypes[1]	=br.ReadByte();
			mLTypes[2]	=br.ReadByte();
			mLTypes[3]	=br.ReadByte();
		}
	}


	public class GFXTexInfo
	{
		public Vector3	[]mVecs			=new Vector3[2];
		public float	[]mShift		=new float[2];
		public float	[]mDrawScale	=new float[2];
		public UInt32	mFlags;
		public float	mFaceLight;
		public float	mReflectiveScale;
		public float	mAlpha;
		public float	mMipMapBias;
		public Int32	mTexture;

		internal void Write(BinaryWriter bw)
		{
			bw.Write(mVecs[0].X);
			bw.Write(mVecs[0].Y);
			bw.Write(mVecs[0].Z);
			bw.Write(mVecs[1].X);
			bw.Write(mVecs[1].Y);
			bw.Write(mVecs[1].Z);
			bw.Write(mShift[0]);
			bw.Write(mShift[1]);
			bw.Write(mDrawScale[0]);
			bw.Write(mDrawScale[1]);
			bw.Write(mFlags);
			bw.Write(mFaceLight);
			bw.Write(mReflectiveScale);
			bw.Write(mAlpha);
			bw.Write(mMipMapBias);
			bw.Write(mTexture);
		}

		internal void Read(BinaryReader br)
		{
			mVecs[0].X			=br.ReadSingle();
			mVecs[0].Y			=br.ReadSingle();
			mVecs[0].Z			=br.ReadSingle();
			mVecs[1].X			=br.ReadSingle();
			mVecs[1].Y			=br.ReadSingle();
			mVecs[1].Z			=br.ReadSingle();
			mShift[0]			=br.ReadSingle();
			mShift[1]			=br.ReadSingle();
			mDrawScale[0]		=br.ReadSingle();
			mDrawScale[1]		=br.ReadSingle();
			mFlags				=br.ReadUInt32();
			mFaceLight			=br.ReadSingle();
			mReflectiveScale	=br.ReadSingle();
			mAlpha				=br.ReadSingle();
			mMipMapBias			=br.ReadSingle();
			mTexture			=br.ReadInt32();
		}
	}


	public class GFXPortal
	{
		public Vector3	mOrigin;
		public Int32	mLeafTo;

		internal void Write(BinaryWriter bw)
		{
			bw.Write(mOrigin.X);
			bw.Write(mOrigin.Y);
			bw.Write(mOrigin.Z);
			bw.Write(mLeafTo);
		}

		internal void Read(BinaryReader br)
		{
			mOrigin.X	=br.ReadSingle();
			mOrigin.Y	=br.ReadSingle();
			mOrigin.Z	=br.ReadSingle();
			mLeafTo		=br.ReadInt32();
		}
	}


	public class GFXCluster
	{
		public Int32	mVisOfs;

		internal void Write(BinaryWriter bw)
		{
			bw.Write(mVisOfs);
		}

		internal void Read(BinaryReader br)
		{
			mVisOfs	=br.ReadInt32();
		}
	}



	public class GBSPChunk
	{
		public Int32	mType;
		public Int32	mElements;

		public const int	GBSP_VERSION				=15;
		public const int	GBSP_CHUNK_HEADER			=0;
		public const int	GBSP_CHUNK_MODELS			=1;
		public const int	GBSP_CHUNK_NODES			=2;
		public const int	GBSP_CHUNK_BNODES			=3;
		public const int	GBSP_CHUNK_LEAFS			=4;
		public const int	GBSP_CHUNK_CLUSTERS			=5;	
		public const int	GBSP_CHUNK_AREAS			=6;	
		public const int	GBSP_CHUNK_AREA_PORTALS		=7;	
		public const int	GBSP_CHUNK_LEAF_SIDES		=8;
		public const int	GBSP_CHUNK_PORTALS			=9;
		public const int	GBSP_CHUNK_PLANES			=10;
		public const int	GBSP_CHUNK_FACES			=11;
		public const int	GBSP_CHUNK_LEAF_FACES		=12;
		public const int	GBSP_CHUNK_VERT_INDEX		=13;
		public const int	GBSP_CHUNK_VERTS			=14;
		public const int	GBSP_CHUNK_RGB_VERTS		=15;
		public const int	GBSP_CHUNK_ENTDATA			=16;
		public const int	GBSP_CHUNK_TEXINFO			=17;
		public const int	GBSP_CHUNK_TEXTURES			=18 ;
		public const int	GBSP_CHUNK_TEXDATA			=19;
		public const int	GBSP_CHUNK_LIGHTDATA		=20;
		public const int	GBSP_CHUNK_VISDATA			=21;
		public const int	GBSP_CHUNK_SKYDATA			=22;
		public const int	GBSP_CHUNK_PALETTES			=23;
		public const int	GBSP_CHUNK_MOTIONS			=24;
		public const int	GBSP_CHUNK_END				=0xffff;


		internal bool Write(BinaryWriter bw, IChunkable obj)
		{
			bw.Write(mType);
			bw.Write(mElements);

			obj.Write(bw);

			return	true;
		}

		internal bool Write(BinaryWriter bw, int[] ints)
		{
			foreach(int i in ints)
			{
				bw.Write(i);
			}
			return	true;
		}

		internal bool Write(BinaryWriter bw, Vector3[] vecs)
		{
			foreach(Vector3 vec in vecs)
			{
				bw.Write(vec.X);
				bw.Write(vec.Y);
				bw.Write(vec.Z);
			}
			return	true;
		}

		internal bool Write(BinaryWriter bw, GFXLeafSide[] ls)
		{
			foreach(GFXLeafSide g in ls)
			{
				g.Write(bw);
			}
			return	true;
		}

		internal bool Write(BinaryWriter bw, GFXArea[] areas)
		{
			foreach(GFXArea a in areas)
			{
				a.Write(bw);
			}
			return	true;
		}

		internal bool Write(BinaryWriter bw, GFXAreaPortal[] aps)
		{
			foreach(GFXAreaPortal ap in aps)
			{
				ap.Write(bw);
			}
			return	true;
		}

		internal bool Write(BinaryWriter bw)
		{
			bw.Write(mType);
			bw.Write(mElements);

			return	true;
		}
	}
}
