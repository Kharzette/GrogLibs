using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class GFXArea
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


	public class GFXAreaPortal
	{
		public Int32	mModelNum;
		public Int32	mArea;

		public void Write(BinaryWriter bw)
		{
			bw.Write(mModelNum);
			bw.Write(mArea);
		}

		public void Read(BinaryReader br)
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


		public void Write(BinaryWriter bw)
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
			Int32	motionPointerFakeThing	=0;
			bw.Write(motionPointerFakeThing);
		}

		public void Read(BinaryReader br)
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
			Int32	fake	=br.ReadInt32();
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

		public void Write(BinaryWriter bw)
		{
			bw.Write(mAxis.X);
			bw.Write(mAxis.Y);
			bw.Write(mAxis.Z);
			bw.Write(mDpm);
			bw.Write(mTextures[0]);
			bw.Write(mTextures[1]);
			bw.Write(mTextures[2]);
			bw.Write(mTextures[3]);
			bw.Write(mTextures[4]);
			bw.Write(mTextures[5]);
			bw.Write(mDrawScale);
		}

		public void Read(BinaryReader br)
		{
			mAxis.X			=br.ReadSingle();
			mAxis.Y			=br.ReadSingle();
			mAxis.Z			=br.ReadSingle();
			mDpm			=br.ReadSingle();
			mTextures[0]	=br.ReadInt32();
			mTextures[1]	=br.ReadInt32();
			mTextures[2]	=br.ReadInt32();
			mTextures[3]	=br.ReadInt32();
			mTextures[4]	=br.ReadInt32();
			mTextures[5]	=br.ReadInt32();
			mDrawScale		=br.ReadSingle();
		}
	}


	public class GFXPlane
	{
		public Vector3	mNormal;
		public float	mDist;
		public UInt32	mType;	//PLANE_X, PLANE_Y, etc...

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

		internal float DistanceFast(Vector3 pos)
		{
			switch(mType)
			{
				case GBSPPlane.PLANE_X:
					return	pos.X - mDist;
				case GBSPPlane.PLANE_Y:
					return	pos.Y - mDist;
				case GBSPPlane.PLANE_Z:
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


	public class GFXBNode
	{
		Int32	[]mChildren	=new Int32[2];
		Int32	mPlaneNum;

		public void Write(BinaryWriter bw)
		{
			bw.Write(mChildren[0]);
			bw.Write(mChildren[1]);
			bw.Write(mPlaneNum);
		}

		public void Read(BinaryReader br)
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


		public void Write(BinaryWriter bw)
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

		public void Read(BinaryReader br)
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

		public void Write(BinaryWriter bw)
		{
			bw.Write(mPlaneNum);
			bw.Write(mPlaneSide);
		}

		public void Read(BinaryReader br)
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

		public void Write(BinaryWriter bw)
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

		public void Read(BinaryReader br)
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

		public void Write(BinaryWriter bw)
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

		public void Read(BinaryReader br)
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

		public void Write(BinaryWriter bw)
		{
			bw.Write(mOrigin.X);
			bw.Write(mOrigin.Y);
			bw.Write(mOrigin.Z);
			bw.Write(mLeafTo);
		}

		public void Read(BinaryReader br)
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

		public void Write(BinaryWriter bw)
		{
			bw.Write(mVisOfs);
		}

		public void Read(BinaryReader br)
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
			bw.Write(mType);
			bw.Write(mElements);

			for(int i=0;i < mElements;i++)
			{
				bw.Write(ints[i]);
			}
			return	true;
		}

		internal bool Write(BinaryWriter bw, byte[] bytes)
		{
			bw.Write(mType);
			bw.Write(mElements);

			for(int i=0;i < mElements;i++)
			{
				bw.Write(bytes[i]);
			}
			return	true;
		}

		internal bool Write(BinaryWriter bw, Vector3[] vecs)
		{
			bw.Write(mType);
			bw.Write(mElements);

			for(int i=0;i < mElements;i++)
			{
				bw.Write(vecs[i].X);
				bw.Write(vecs[i].Y);
				bw.Write(vecs[i].Z);
			}
			return	true;
		}

		internal bool Write(BinaryWriter bw, GFXLeafSide[] ls)
		{
			bw.Write(mType);
			bw.Write(mElements);

			for(int i=0;i < mElements;i++)
			{
				ls[i].Write(bw);
			}
			return	true;
		}

		internal bool Write(BinaryWriter bw, GFXArea[] areas)
		{
			bw.Write(mType);
			bw.Write(mElements);

			for(int i=0;i < mElements;i++)
			{
				areas[i].Write(bw);
			}
			return	true;
		}

		internal bool Write(BinaryWriter bw, GFXAreaPortal[] aps)
		{
			bw.Write(mType);
			bw.Write(mElements);

			for(int i=0;i < mElements;i++)
			{
				aps[i].Write(bw);
			}
			return	true;
		}

		internal bool Write(BinaryWriter bw)
		{
			bw.Write(mType);
			bw.Write(mElements);

			return	true;
		}

		//this one spits the data back generic style
		internal object Read(BinaryReader br, out UInt32 chunkType)
		{
			chunkType	=br.ReadUInt32();
			mElements	=br.ReadInt32();

			switch(chunkType)
			{
				case GBSP_CHUNK_HEADER:
				{
					return	ReadChunkData(br, typeof(GBSPHeader), false, 0) as GBSPHeader;
				}
				case GBSP_CHUNK_MODELS:
				{
                    return	ReadChunkData(br, typeof(GFXModel), true, mElements) as GFXModel[];
				}
				case GBSP_CHUNK_NODES:
				{
					return	ReadChunkData(br, typeof(GFXNode), true, mElements) as GFXNode[];
				}
				case GBSP_CHUNK_BNODES:
				{
					return	ReadChunkData(br, typeof(GFXBNode), true, mElements) as GFXBNode[];
				}
				case GBSP_CHUNK_LEAFS:
				{
					return	ReadChunkData(br, typeof(GFXLeaf), true, mElements) as GFXLeaf[];
				}
				case GBSP_CHUNK_CLUSTERS:
				{
					return	ReadChunkData(br, typeof(GFXCluster), true, mElements) as GFXCluster[];
				}
				case GBSP_CHUNK_AREAS:
				{
					return	ReadChunkData(br, typeof(GFXArea), true, mElements) as GFXArea[];
				}
				case GBSP_CHUNK_AREA_PORTALS:
				{
					return	ReadChunkData(br, typeof(GFXAreaPortal), true, mElements) as GFXAreaPortal[];
				}
				case GBSP_CHUNK_PORTALS:
				{
					return	ReadChunkData(br, typeof(GFXPortal), true, mElements) as GFXPortal[];
				}
				case GBSP_CHUNK_PLANES:
				{
					return	ReadChunkData(br, typeof(GFXPlane), true, mElements) as GFXPlane[];
				}
				case GBSP_CHUNK_FACES:
				{
					return	ReadChunkData(br, typeof(GFXFace), true, mElements) as GFXFace[];
				}
				case GBSP_CHUNK_LEAF_FACES:
				{
					return	ReadChunkData(br, typeof(int), true, mElements) as int[];
				}
				case GBSP_CHUNK_LEAF_SIDES:
				{
					return	ReadChunkData(br, typeof(GFXLeafSide), true, mElements) as GFXLeafSide[];
				}
				case GBSP_CHUNK_VERTS:
				{
					return	ReadChunkData(br, typeof(Vector3), true, mElements) as Vector3[];
				}
				case GBSP_CHUNK_VERT_INDEX:
				{
					return	ReadChunkData(br, typeof(int), true, mElements) as int[];
				}
				case GBSP_CHUNK_RGB_VERTS:
				{
					return	ReadChunkData(br, typeof(Vector3), true, mElements) as Vector3[];
				}
				case GBSP_CHUNK_TEXINFO:
				{
					return	ReadChunkData(br, typeof(GFXTexInfo), true, mElements) as GFXTexInfo[];
				}
				case GBSP_CHUNK_ENTDATA:
				{
					return	ReadChunkData(br, typeof(MapEntity), true, mElements) as MapEntity[];
				}
				case GBSP_CHUNK_LIGHTDATA:
				{
					return	ReadChunkData(br, typeof(byte), true, mElements) as byte[];
				}
				case GBSP_CHUNK_VISDATA:
				{
					return	ReadChunkData(br, typeof(byte), true, mElements) as byte[];
				}
				case GBSP_CHUNK_SKYDATA:
				{
					return	ReadChunkData(br, typeof(GFXSkyData), false, 0) as GFXSkyData;
				}
				case GBSP_CHUNK_END:
				{
					break;
				}
				default:
					return	false;
			}
			return	true;
		}

		internal bool Read(BinaryReader br, GBSPGlobals gg, bool bCPP)
		{
			if(!bCPP)
			{
				return	Read(br, gg);
			}

			int	size	=0;
			mType		=br.ReadInt32();
			size		=br.ReadInt32();
			mElements	=br.ReadInt32();

			switch(mType)
			{
				case GBSP_CHUNK_HEADER:
				{
					char	[]tag	=new char[5];
					gg.GBSPHeader	=new GBSPHeader();
					tag[0]	=br.ReadChar();
					tag[1]	=br.ReadChar();
					tag[2]	=br.ReadChar();
					tag[3]	=br.ReadChar();

					gg.GBSPHeader.mTAG		=new string(tag);
					gg.GBSPHeader.mTAG		=gg.GBSPHeader.mTAG.Substring(0, 4);
					gg.GBSPHeader.mVersion	=br.ReadInt32();
					gg.GBSPHeader.mVersion	=br.ReadInt32();	//some random int in there

					br.BaseStream.Seek(size - 12, SeekOrigin.Current);


					if(gg.GBSPHeader.mTAG != "GBSP")
					{
						return	false;
					}
					if(gg.GBSPHeader.mVersion != GBSP_VERSION)
					{
						return	false;
					}
					break;
				}
				case GBSP_CHUNK_MODELS:
				{
					gg.NumGFXModels	=mElements;
					gg.GFXModels	=new GFXModel[gg.NumGFXModels];
                    gg.GFXModels    =ReadChunkData(br, typeof(GFXModel), true, mElements) as GFXModel[];
					break;
				}
				case GBSP_CHUNK_NODES:
				{
					gg.NumGFXNodes	=mElements;
					gg.GFXNodes		=new GFXNode[gg.NumGFXNodes];
					gg.GFXNodes		=ReadChunkData(br, typeof(GFXNode), true, mElements) as GFXNode[];
					break;
				}
				case GBSP_CHUNK_BNODES:
				{
					gg.NumGFXBNodes	=mElements;
					gg.GFXBNodes	=new GFXBNode[gg.NumGFXBNodes];
					gg.GFXBNodes    =ReadChunkData(br, typeof(GFXBNode), true, mElements) as GFXBNode[];
					break;
				}
				case GBSP_CHUNK_LEAFS:
				{
					gg.NumGFXLeafs	=mElements;
					gg.GFXLeafs		=new GFXLeaf[gg.NumGFXLeafs];
					gg.GFXLeafs		=ReadChunkData(br, typeof(GFXLeaf), true, mElements) as GFXLeaf[];
					break;
				}
				case GBSP_CHUNK_CLUSTERS:
				{
					gg.NumGFXClusters	=mElements;
					gg.GFXClusters		=new GFXCluster[gg.NumGFXClusters];
					gg.GFXClusters		=ReadChunkData(br, typeof(GFXCluster), true, mElements) as GFXCluster[];
					break;
				}
				case GBSP_CHUNK_AREAS:
				{
					gg.NumGFXAreas	=mElements;
					gg.GFXAreas		=new GFXArea[gg.NumGFXAreas];
					gg.GFXAreas		=ReadChunkData(br, typeof(GFXArea), true, mElements) as GFXArea[];
					break;
				}
				case GBSP_CHUNK_AREA_PORTALS:
				{
					gg.NumGFXAreaPortals	=mElements;
					gg.GFXAreaPortals		=new GFXAreaPortal[gg.NumGFXAreaPortals];
					gg.GFXAreaPortals		=ReadChunkData(br, typeof(GFXAreaPortal), true, mElements) as GFXAreaPortal[];
					break;
				}
				case GBSP_CHUNK_PORTALS:
				{
					gg.NumGFXPortals	=mElements;
					gg.GFXPortals		=new GFXPortal[gg.NumGFXPortals];
					gg.GFXPortals		=ReadChunkData(br, typeof(GFXPortal), true, mElements) as GFXPortal[];
					break;
				}
				case GBSP_CHUNK_PLANES:
				{
					gg.NumGFXPlanes	=mElements;
					gg.GFXPlanes	=new GFXPlane[gg.NumGFXPlanes];
					gg.GFXPlanes	=ReadChunkData(br, typeof(GFXPlane), true, mElements) as GFXPlane[];
					break;
				}
				case GBSP_CHUNK_FACES:
				{
					gg.NumGFXFaces	=mElements;
					gg.GFXFaces		=new GFXFace[gg.NumGFXFaces];
					gg.GFXFaces		=ReadChunkData(br, typeof(GFXFace), true, mElements) as GFXFace[];
					break;
				}
				case GBSP_CHUNK_LEAF_FACES:
				{
					gg.NumGFXLeafFaces	=mElements;
					gg.GFXLeafFaces		=new int[gg.NumGFXLeafFaces];
					gg.GFXLeafFaces		=ReadChunkData(br, typeof(int), true, mElements) as int[];
					break;
				}
				case GBSP_CHUNK_LEAF_SIDES:
				{
					gg.NumGFXLeafSides	=mElements;
					gg.GFXLeafSides		=new GFXLeafSide[gg.NumGFXLeafSides];
					gg.GFXLeafSides		=ReadChunkData(br, typeof(GFXLeafSide), true, mElements) as GFXLeafSide[];
					break;
				}
				case GBSP_CHUNK_VERTS:
				{
					gg.NumGFXVerts	=mElements;
					gg.GFXVerts		=new Vector3[gg.NumGFXVerts];
					gg.GFXVerts		=ReadChunkData(br, typeof(Vector3), true, mElements) as Vector3[];
					break;
				}
				case GBSP_CHUNK_VERT_INDEX:
				{
					gg.NumGFXVertIndexList	=mElements;
					gg.GFXVertIndexList		=new int[gg.NumGFXVertIndexList];
					gg.GFXVertIndexList		=ReadChunkData(br, typeof(int), true, mElements) as int[];
					break;
				}
				case GBSP_CHUNK_RGB_VERTS:
				{
					gg.NumGFXRGBVerts	=mElements;
					gg.GFXRGBVerts		=new Vector3[gg.NumGFXRGBVerts];
					gg.GFXRGBVerts		=ReadChunkData(br, typeof(Vector3), true, mElements) as Vector3[];
					break;
				}
				case GBSP_CHUNK_TEXINFO:
				{					
					gg.NumGFXTexInfo	=mElements;
					gg.GFXTexInfo		=new GFXTexInfo[gg.NumGFXTexInfo];
					gg.GFXTexInfo		=ReadChunkData(br, typeof(GFXTexInfo), true, mElements) as GFXTexInfo[];
					break;
				}
				case GBSP_CHUNK_ENTDATA:
				{
					Int32	numEnts		=br.ReadInt32();
					gg.NumGFXEntData	=mElements;
					gg.GFXEntData		=new MapEntity[gg.NumGFXEntData];
					gg.GFXEntData		=ReadChunkData(br, typeof(MapEntity), true, numEnts) as MapEntity[];
					break;
				}
				case GBSP_CHUNK_LIGHTDATA:
				{
					gg.NumGFXLightData	=mElements;
					gg.GFXLightData		=new byte[gg.NumGFXLightData];
					gg.GFXLightData		=ReadChunkData(br, typeof(byte), true, mElements) as byte[];
					break;
				}
				case GBSP_CHUNK_VISDATA:
				{
					gg.NumGFXVisData	=mElements;
					gg.GFXVisData		=new byte[gg.NumGFXVisData];
					gg.GFXVisData		=ReadChunkData(br, typeof(byte), true, mElements) as byte[];
					break;
				}
				case GBSP_CHUNK_SKYDATA:
				{
					gg.GFXSkyData   =ReadChunkData(br, typeof(GFXSkyData), false, 0) as GFXSkyData;
					break;
				}
				case GBSP_CHUNK_END:
				{
					break;
				}
				default:
					return	false;
			}
			return	true;
		}

		internal bool Read(BinaryReader br, GBSPGlobals gg)
		{
			mType		=br.ReadInt32();
			mElements	=br.ReadInt32();

			switch(mType)
			{
				case GBSP_CHUNK_HEADER:
				{
					gg.GBSPHeader	=ReadChunkData(br, typeof(GBSPHeader), false, 0) as GBSPHeader;
					if(gg.GBSPHeader.mTAG != "GBSP")
					{
						return	false;
					}
					if(gg.GBSPHeader.mVersion != GBSP_VERSION)
					{
						return	false;
					}
					break;
				}
				case GBSP_CHUNK_MODELS:
				{
					gg.NumGFXModels	=mElements;
					gg.GFXModels	=new GFXModel[gg.NumGFXModels];
                    gg.GFXModels    =ReadChunkData(br, typeof(GFXModel), true, mElements) as GFXModel[];
					break;
				}
				case GBSP_CHUNK_NODES:
				{
					gg.NumGFXNodes	=mElements;
					gg.GFXNodes		=new GFXNode[gg.NumGFXNodes];
					gg.GFXNodes		=ReadChunkData(br, typeof(GFXNode), true, mElements) as GFXNode[];
					break;
				}
				case GBSP_CHUNK_BNODES:
				{
					gg.NumGFXBNodes	=mElements;
					gg.GFXBNodes	=new GFXBNode[gg.NumGFXBNodes];
					gg.GFXBNodes    =ReadChunkData(br, typeof(GFXBNode), true, mElements) as GFXBNode[];
					break;
				}
				case GBSP_CHUNK_LEAFS:
				{
					gg.NumGFXLeafs	=mElements;
					gg.GFXLeafs		=new GFXLeaf[gg.NumGFXLeafs];
					gg.GFXLeafs		=ReadChunkData(br, typeof(GFXLeaf), true, mElements) as GFXLeaf[];
					break;
				}
				case GBSP_CHUNK_CLUSTERS:
				{
					gg.NumGFXClusters	=mElements;
					gg.GFXClusters		=new GFXCluster[gg.NumGFXClusters];
					gg.GFXClusters		=ReadChunkData(br, typeof(GFXCluster), true, mElements) as GFXCluster[];
					break;
				}
				case GBSP_CHUNK_AREAS:
				{
					gg.NumGFXAreas	=mElements;
					gg.GFXAreas		=new GFXArea[gg.NumGFXAreas];
					gg.GFXAreas		=ReadChunkData(br, typeof(GFXArea), true, mElements) as GFXArea[];
					break;
				}
				case GBSP_CHUNK_AREA_PORTALS:
				{
					gg.NumGFXAreaPortals	=mElements;
					gg.GFXAreaPortals		=new GFXAreaPortal[gg.NumGFXAreaPortals];
					gg.GFXAreaPortals		=ReadChunkData(br, typeof(GFXAreaPortal), true, mElements) as GFXAreaPortal[];
					break;
				}
				case GBSP_CHUNK_PORTALS:
				{
					gg.NumGFXPortals	=mElements;
					gg.GFXPortals		=new GFXPortal[gg.NumGFXPortals];
					gg.GFXPortals		=ReadChunkData(br, typeof(GFXPortal), true, mElements) as GFXPortal[];
					break;
				}
				case GBSP_CHUNK_PLANES:
				{
					gg.NumGFXPlanes	=mElements;
					gg.GFXPlanes	=new GFXPlane[gg.NumGFXPlanes];
					gg.GFXPlanes	=ReadChunkData(br, typeof(GFXPlane), true, mElements) as GFXPlane[];
					break;
				}
				case GBSP_CHUNK_FACES:
				{
					gg.NumGFXFaces	=mElements;
					gg.GFXFaces		=new GFXFace[gg.NumGFXFaces];
					gg.GFXFaces		=ReadChunkData(br, typeof(GFXFace), true, mElements) as GFXFace[];
					break;
				}
				case GBSP_CHUNK_LEAF_FACES:
				{
					gg.NumGFXLeafFaces	=mElements;
					gg.GFXLeafFaces		=new int[gg.NumGFXLeafFaces];
					gg.GFXLeafFaces		=ReadChunkData(br, typeof(int), true, mElements) as int[];
					break;
				}
				case GBSP_CHUNK_LEAF_SIDES:
				{
					gg.NumGFXLeafSides	=mElements;
					gg.GFXLeafSides		=new GFXLeafSide[gg.NumGFXLeafSides];
					gg.GFXLeafSides		=ReadChunkData(br, typeof(GFXLeafSide), true, mElements) as GFXLeafSide[];
					break;
				}
				case GBSP_CHUNK_VERTS:
				{
					gg.NumGFXVerts	=mElements;
					gg.GFXVerts		=new Vector3[gg.NumGFXVerts];
					gg.GFXVerts		=ReadChunkData(br, typeof(Vector3), true, mElements) as Vector3[];
					break;
				}
				case GBSP_CHUNK_VERT_INDEX:
				{
					gg.NumGFXVertIndexList	=mElements;
					gg.GFXVertIndexList		=new int[gg.NumGFXVertIndexList];
					gg.GFXVertIndexList		=ReadChunkData(br, typeof(int), true, mElements) as int[];
					break;
				}
				case GBSP_CHUNK_RGB_VERTS:
				{
					gg.NumGFXRGBVerts	=mElements;
					gg.GFXRGBVerts		=new Vector3[gg.NumGFXRGBVerts];
					gg.GFXRGBVerts		=ReadChunkData(br, typeof(Vector3), true, mElements) as Vector3[];
					break;
				}
				case GBSP_CHUNK_TEXINFO:
				{
					gg.NumGFXTexInfo	=mElements;
					gg.GFXTexInfo		=new GFXTexInfo[gg.NumGFXTexInfo];
					gg.GFXTexInfo		=ReadChunkData(br, typeof(GFXTexInfo), true, mElements) as GFXTexInfo[];
					break;
				}
				case GBSP_CHUNK_ENTDATA:
				{
					gg.NumGFXEntData	=mElements;
					gg.GFXEntData		=new MapEntity[gg.NumGFXEntData];
					gg.GFXEntData		=ReadChunkData(br, typeof(MapEntity), true, mElements) as MapEntity[];
					break;
				}
				case GBSP_CHUNK_LIGHTDATA:
				{
					gg.NumGFXLightData	=mElements;
					gg.GFXLightData		=new byte[gg.NumGFXLightData];
					gg.GFXLightData		=ReadChunkData(br, typeof(byte), true, mElements) as byte[];
					break;
				}
				case GBSP_CHUNK_VISDATA:
				{
					gg.NumGFXVisData	=mElements;
					gg.GFXVisData		=new byte[gg.NumGFXVisData];
					gg.GFXVisData		=ReadChunkData(br, typeof(byte), true, mElements) as byte[];
					break;
				}
				case GBSP_CHUNK_SKYDATA:
				{
					gg.GFXSkyData   =ReadChunkData(br, typeof(GFXSkyData), false, 0) as GFXSkyData;
					break;
				}
				case GBSP_CHUNK_END:
				{
					break;
				}
				default:
					return	false;
			}

			return	true;
		}

		internal object ReadChunkData(BinaryReader br, Type chunkType, bool bArray, int length)
		{
			//create an instance
			Assembly	ass	=Assembly.GetExecutingAssembly();
			object		ret	=null;

			if(bArray)
			{
				ret	=Array.CreateInstance(chunkType, length);
			}
			else
			{
				ret	=ass.CreateInstance(chunkType.ToString());
			}

			//check for a read method
			MethodInfo	mi	=chunkType.GetMethod("Read");

			//invoke
			if(bArray)
			{
				Array	arr	=ret as Array;

				for(int i=0;i < arr.Length;i++)
				{
					object	obj	=ass.CreateInstance(chunkType.ToString());

					if(mi == null)
					{
						//no read method, could be a primitive type
						if(chunkType == typeof(Int32))
						{
							Int32	num	=br.ReadInt32();

							obj	=num;
						}
						else if(chunkType == typeof(Vector3))
						{
							Vector3	vec;
							vec.X	=br.ReadSingle();
							vec.Y	=br.ReadSingle();
							vec.Z	=br.ReadSingle();

							obj	=vec;
						}
						else if(chunkType == typeof(byte))
						{
							byte	num	=br.ReadByte();

							obj	=num;
						}
						else
						{
							Debug.Assert(false);
						}
					}
					else
					{
						mi.Invoke(obj, new object[]{br});
					}
					arr.SetValue(obj, i);
				}
			}
			else
			{
				mi.Invoke(ret, new object[]{br});
			}

			return	ret;
		}
	}
}
