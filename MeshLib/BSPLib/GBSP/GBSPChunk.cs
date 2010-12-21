using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class GBSPChunk
	{
		public Int32	mType;
		public Int32	mElements;

		public const int	VERSION			=15;
		public const int	HEADER			=0;
		public const int	MODELS			=1;
		public const int	NODES			=2;
		public const int	BNODES			=3;
		public const int	LEAFS			=4;
		public const int	CLUSTERS		=5;	
		public const int	AREAS			=6;	
		public const int	AREA_PORTALS	=7;	
		public const int	LEAF_SIDES		=8;
		public const int	PORTALS			=9;
		public const int	PLANES			=10;
		public const int	FACES			=11;
		public const int	LEAF_FACES		=12;
		public const int	VERT_INDEX		=13;
		public const int	VERTS			=14;
		public const int	RGB_VERTS		=15;
		public const int	ENTDATA			=16;
		public const int	TEXINFO			=17;
		public const int	TEXTURES		=18;
		public const int	TEXDATA			=19;
		public const int	LIGHTDATA		=20;
		public const int	VISDATA			=21;
		public const int	SKYDATA			=22;
		public const int	MATERIALVISDATA	=23;
		public const int	END				=0xffff;


		internal bool Write(BinaryWriter bw, GBSPHeader hdr)
		{
			bw.Write(mType);
			bw.Write(mElements);

			hdr.Write(bw);

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
				case HEADER:
				{
					return	ReadChunkData(br, typeof(GBSPHeader), false, 0) as GBSPHeader;
				}
				case MODELS:
				{
                    return	ReadChunkData(br, typeof(GFXModel), true, mElements) as GFXModel[];
				}
				case NODES:
				{
					return	ReadChunkData(br, typeof(GFXNode), true, mElements) as GFXNode[];
				}
				case BNODES:
				{
					return	ReadChunkData(br, typeof(GFXBNode), true, mElements) as GFXBNode[];
				}
				case LEAFS:
				{
					return	ReadChunkData(br, typeof(GFXLeaf), true, mElements) as GFXLeaf[];
				}
				case CLUSTERS:
				{
					return	ReadChunkData(br, typeof(GFXCluster), true, mElements) as GFXCluster[];
				}
				case AREAS:
				{
					return	ReadChunkData(br, typeof(GFXArea), true, mElements) as GFXArea[];
				}
				case AREA_PORTALS:
				{
					return	ReadChunkData(br, typeof(GFXAreaPortal), true, mElements) as GFXAreaPortal[];
				}
				case PORTALS:
				{
					return	ReadChunkData(br, typeof(GFXPortal), true, mElements) as GFXPortal[];
				}
				case PLANES:
				{
					return	ReadChunkData(br, typeof(GFXPlane), true, mElements) as GFXPlane[];
				}
				case FACES:
				{
					return	ReadChunkData(br, typeof(GFXFace), true, mElements) as GFXFace[];
				}
				case LEAF_FACES:
				{
					return	ReadChunkData(br, typeof(int), true, mElements) as int[];
				}
				case LEAF_SIDES:
				{
					return	ReadChunkData(br, typeof(GFXLeafSide), true, mElements) as GFXLeafSide[];
				}
				case VERTS:
				{
					return	ReadChunkData(br, typeof(Vector3), true, mElements) as Vector3[];
				}
				case VERT_INDEX:
				{
					return	ReadChunkData(br, typeof(int), true, mElements) as int[];
				}
				case RGB_VERTS:
				{
					return	ReadChunkData(br, typeof(Vector3), true, mElements) as Vector3[];
				}
				case TEXINFO:
				{
					return	ReadChunkData(br, typeof(GFXTexInfo), true, mElements) as GFXTexInfo[];
				}
				case ENTDATA:
				{
					return	ReadChunkData(br, typeof(MapEntity), true, mElements) as MapEntity[];
				}
				case LIGHTDATA:
				{
					return	ReadChunkData(br, typeof(byte), true, mElements) as byte[];
				}
				case VISDATA:
				{
					return	ReadChunkData(br, typeof(byte), true, mElements) as byte[];
				}
				case SKYDATA:
				{
					return	ReadChunkData(br, typeof(GFXSkyData), false, 0) as GFXSkyData;
				}
				case MATERIALVISDATA:
				{
					return	ReadChunkData(br, typeof(byte), true, mElements) as byte[];
				}
				case END:
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
