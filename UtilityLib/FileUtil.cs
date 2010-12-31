using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Storage;


namespace UtilityLib
{
	public class FileUtil
	{
		public static FileStream OpenTitleFile(string fileName,
			FileMode mode, FileAccess access)
		{
			string	fullPath	=Path.Combine(
									StorageContainer.TitleLocation,
									fileName);

			if(!File.Exists(fullPath) &&
				(access == FileAccess.Write ||
				access == FileAccess.ReadWrite))
			{
				return	File.Create(fullPath);
			}
			else
			{
				return	File.Open(fullPath, mode, access);
			}
		}


		public static bool FileExists(string fileName)
		{
			string	fullPath	=Path.Combine(
									StorageContainer.TitleLocation,
									fileName);

			return	File.Exists(fullPath);
		}


		public static void WriteArray(BinaryWriter bw, Int32 []intArray)
		{
			bw.Write(intArray.Length);
			for(int i=0;i < intArray.Length;i++)
			{
				bw.Write(intArray[i]);
			}
		}


		public static byte []ReadByteArray(BinaryReader br)
		{
			int	count	=br.ReadInt32();
			return	br.ReadBytes(count);
		}


		public static Int32 []ReadIntArray(BinaryReader br)
		{
			int	len	=br.ReadInt32();

			Int32	[]ret	=new Int32[len];
			for(int i=0;i < len;i++)
			{
				ret[i]	=br.ReadInt32();
			}
			return	ret;
		}


		public static void WriteArray(BinaryWriter bw, Vector3 []vecArray)
		{
			bw.Write(vecArray.Length);
			for(int i=0;i < vecArray.Length;i++)
			{
				bw.Write(vecArray[i].X);
				bw.Write(vecArray[i].Y);
				bw.Write(vecArray[i].Z);
			}
		}


		public static Vector3 []ReadVecArray(BinaryReader br)
		{
			int	len	=br.ReadInt32();

			Vector3	[]ret	=new Vector3[len];
			for(int i=0;i < len;i++)
			{
				ret[i].X	=br.ReadSingle();
				ret[i].Y	=br.ReadSingle();
				ret[i].Z	=br.ReadSingle();
			}
			return	ret;
		}


		public static void WriteVertexDeclaration(BinaryWriter bw, VertexDeclaration vd)
		{
			VertexElement	[]elms	=vd.GetVertexElements();

			bw.Write(elms.Length);
			foreach(VertexElement ve in elms)
			{
				bw.Write(ve.Stream);
				bw.Write(ve.Offset);
				bw.Write((UInt32)ve.VertexElementFormat);
				bw.Write((UInt32)ve.VertexElementMethod);
				bw.Write((UInt32)ve.VertexElementUsage);
				bw.Write(ve.UsageIndex);
			}
		}


		public static void ReadVertexDeclaration(BinaryReader br, out VertexDeclaration vd, GraphicsDevice g)
		{
			int	numElements	=br.ReadInt32();
			VertexElement	[]vels	=new VertexElement[numElements];
			for(int i=0;i < numElements;i++)
			{
				short	streamIdx	=br.ReadInt16();
				short	offset		=br.ReadInt16();

				VertexElementFormat	vef	=(VertexElementFormat)br.ReadUInt32();
				VertexElementMethod	vem	=(VertexElementMethod)br.ReadUInt32();
				VertexElementUsage	veu	=(VertexElementUsage)br.ReadUInt32();

				byte	usageIndex	=br.ReadByte();

				vels[i]	=new VertexElement(streamIdx, offset, vef, vem, veu, usageIndex);
			}
			vd	=new VertexDeclaration(g, vels);
		}


		public static void WriteIndexBuffer(BinaryWriter bw, IndexBuffer ib)
		{
			int	numIdx	=ib.SizeInBytes / 4;

			Int32	[]idxArray	=new Int32[numIdx];

			ib.GetData<Int32>(idxArray);

			bw.Write(numIdx);
			for(int i=0;i < numIdx;i++)
			{
				bw.Write(idxArray[i]);
			}
		}


		public static void ReadIndexBuffer(BinaryReader br, out IndexBuffer ib, GraphicsDevice g, bool bEditor)
		{
			int	numIdx	=br.ReadInt32();

			Int32	[]idxArray	=new Int32[numIdx];

			for(int i=0;i < numIdx;i++)
			{
				idxArray[i]	=br.ReadInt32();
			}
			ib	=new IndexBuffer(g, numIdx * 4,
				(bEditor)? BufferUsage.None : BufferUsage.WriteOnly,
				IndexElementSize.ThirtyTwoBits);
			ib.SetData<Int32>(idxArray);
		}


		public delegate IReadWriteable[] CreateRWArray(Int32 count);


		public static ArrayType []InitArray<ArrayType>(int count) where ArrayType : new()
		{
			ArrayType	[]ret	=new ArrayType[count];
			for(int i=0;i < count;i++)
			{
				ret[i]	=new ArrayType();
			}
			return	ret;
		}


		public static void WriteArray(IReadWriteable []arr, BinaryWriter bw)
		{
			bw.Write(arr.Length);
			foreach(IReadWriteable obj in arr)
			{
				obj.Write(bw);
			}
		}


		public static void WriteArray(byte []arr, BinaryWriter bw)
		{
			bw.Write(arr.Length);
			bw.Write(arr, 0, arr.Length);
		}


		public static object[] ReadArray(BinaryReader br, CreateRWArray crwa)
		{
			int	count	=br.ReadInt32();

			IReadWriteable	[]arr	=crwa(count);

			for(int i=0;i < count;i++)
			{
				arr[i].Read(br);
			}
			return	arr;
		}
	}
}
