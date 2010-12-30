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
	}
}
