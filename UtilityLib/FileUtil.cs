using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Storage;


namespace UtilityLib
{
	public class FileUtil
	{
		public static Stream OpenTitleFile(string fileName)
		{
			return	TitleContainer.OpenStream(fileName);
		}


		public static string StripExtension(string fileName)
		{
			int	dotPos	=fileName.LastIndexOf('.');
			if(dotPos != -1)
			{
				return	fileName.Substring(0, dotPos);
			}
			return	fileName;
		}

		
		public static bool FileExists(string fileName)
		{
#if !XBOX
			return	File.Exists(fileName);
#else
			return	false;
#endif
		}


		public static void WriteArray(BinaryWriter bw, Int32 []intArray)
		{
			if(intArray == null)
			{
				bw.Write(false);
				return;
			}
			else
			{
				bw.Write(true);
			}
			bw.Write(intArray.Length);
			for(int i=0;i < intArray.Length;i++)
			{
				bw.Write(intArray[i]);
			}
		}


		public static byte []ReadByteArray(BinaryReader br)
		{
			bool	bNull	=!br.ReadBoolean();
			if(bNull)
			{
				return	null;
			}
			int	count	=br.ReadInt32();
			return	br.ReadBytes(count);
		}


		public static Int32 []ReadIntArray(BinaryReader br)
		{
			bool	bNull	=!br.ReadBoolean();
			if(bNull)
			{
				return	null;
			}
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
			if(vecArray == null)
			{
				bw.Write(false);
				return;
			}
			else
			{
				bw.Write(true);
			}
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
			bool	bNull	=!br.ReadBoolean();
			if(bNull)
			{
				return	null;
			}
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
				bw.Write(ve.Offset);
				bw.Write((UInt32)ve.VertexElementFormat);
				bw.Write((UInt32)ve.VertexElementUsage);
				bw.Write(ve.UsageIndex);
			}
		}


		public static void ReadVertexDeclaration(BinaryReader br, out VertexDeclaration vd)
		{
			int	numElements	=br.ReadInt32();
			VertexElement	[]vels	=new VertexElement[numElements];
			for(int i=0;i < numElements;i++)
			{
				int	offset		=br.ReadInt32();

				VertexElementFormat	vef	=(VertexElementFormat)br.ReadUInt32();
				VertexElementUsage	veu	=(VertexElementUsage)br.ReadUInt32();

				int	usageIndex	=br.ReadInt32();

				vels[i]	=new VertexElement(offset, vef, veu, usageIndex);
			}
			vd	=new VertexDeclaration(vels);
		}


		public static void WriteIndexBuffer(BinaryWriter bw, IndexBuffer ib)
		{
			int	numIdx	=ib.IndexCount;

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
			ib	=new IndexBuffer(g, IndexElementSize.ThirtyTwoBits, numIdx,
							(bEditor)? BufferUsage.None : BufferUsage.WriteOnly);
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
			if(arr == null)
			{
				bw.Write(false);
				return;
			}
			else
			{
				bw.Write(true);
			}
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


		//saves out enough info to recreate
		//the object from reflection
		public static void SaveProps(BinaryWriter bw, object obj)
		{
			bw.Write(obj.GetType().AssemblyQualifiedName);

			PropertyInfo	[]props	=obj.GetType().GetProperties();
			foreach(PropertyInfo pi in props)
			{
				object	madProp	=pi.GetValue(obj, null);

				if(madProp is string)
				{
					bw.Write(madProp as string);
				}
				else if(madProp is UInt32)
				{
					UInt32	gack	=(UInt32)madProp;
					bw.Write(gack);
				}
				else if(madProp is Int32)
				{
					Int32	gack	=(Int32)madProp;
					bw.Write(gack);
				}
				else if(madProp is bool)
				{
					bool	gack	=(bool)madProp;
					bw.Write(gack);
				}
				else if(madProp is Int16)
				{
					Int16	gack	=(Int16)madProp;
					bw.Write(gack);
				}
				else if(madProp is UInt16)
				{
					UInt16	gack	=(UInt16)madProp;
					bw.Write(gack);
				}
				else if(madProp is Vector2)
				{
					Vector2	gack	=(Vector2)madProp;
					bw.Write(gack.X);
					bw.Write(gack.Y);
				}
				else if(madProp is Vector3)
				{
					Vector3	gack	=(Vector3)madProp;
					bw.Write(gack.X);
					bw.Write(gack.Y);
					bw.Write(gack.Z);
				}
				else if(madProp is Vector4)
				{
					Vector4	gack	=(Vector4)madProp;
					bw.Write(gack.X);
					bw.Write(gack.Y);
					bw.Write(gack.Z);
					bw.Write(gack.W);
				}
				else if(madProp is Quaternion)
				{
					Quaternion	gack	=(Quaternion)madProp;
					bw.Write(gack.X);
					bw.Write(gack.Y);
					bw.Write(gack.Z);
					bw.Write(gack.W);
				}
				else if(madProp is Matrix)
				{
					Matrix	gack	=(Matrix)madProp;
					bw.Write(gack.M11);
					bw.Write(gack.M12);
					bw.Write(gack.M13);
					bw.Write(gack.M14);
					bw.Write(gack.M21);
					bw.Write(gack.M22);
					bw.Write(gack.M23);
					bw.Write(gack.M24);
					bw.Write(gack.M31);
					bw.Write(gack.M32);
					bw.Write(gack.M33);
					bw.Write(gack.M34);
					bw.Write(gack.M41);
					bw.Write(gack.M42);
					bw.Write(gack.M43);
					bw.Write(gack.M44);
				}
			}
		}


		public static object ReadProps(BinaryReader br)
		{
			string	typeName	=br.ReadString();

			Type	t	=Type.GetType(typeName);
			object	ret	=Activator.CreateInstance(t);

			PropertyInfo	[]props	=ret.GetType().GetProperties();
			for(int i=0;i < props.Length;i++)
			{
				if(props[i].PropertyType == typeof(string))
				{
					string	val	=br.ReadString();
					props[i].SetValue(ret, val, null);
				}
				else if(props[i].PropertyType == typeof(UInt32))
				{
					UInt32	val	=br.ReadUInt32();
					props[i].SetValue(ret, val, null);
				}
				else if(props[i].PropertyType == typeof(Int32))
				{
					Int32	val	=br.ReadInt32();
					props[i].SetValue(ret, val, null);
				}
				else if(props[i].PropertyType == typeof(bool))
				{
					bool	val	=br.ReadBoolean();
					props[i].SetValue(ret, val, null);
				}
				else if(props[i].PropertyType == typeof(Int16))
				{
					Int16	val	=br.ReadInt16();
					props[i].SetValue(ret, val, null);
				}
				else if(props[i].PropertyType == typeof(UInt16))
				{
					UInt16	val	=br.ReadUInt16();
					props[i].SetValue(ret, val, null);
				}
				else if(props[i].PropertyType == typeof(Vector2))
				{
					Vector2	val	=Vector2.Zero;

					val.X	=br.ReadSingle();
					val.Y	=br.ReadSingle();

					props[i].SetValue(ret, val, null);
				}
				else if(props[i].PropertyType == typeof(Vector3))
				{
					Vector3	val	=Vector3.Zero;

					val.X	=br.ReadSingle();
					val.Y	=br.ReadSingle();
					val.Z	=br.ReadSingle();

					props[i].SetValue(ret, val, null);
				}
				else if(props[i].PropertyType == typeof(Vector4))
				{
					Vector4	val	=Vector4.Zero;

					val.X	=br.ReadSingle();
					val.Y	=br.ReadSingle();
					val.Z	=br.ReadSingle();
					val.W	=br.ReadSingle();

					props[i].SetValue(ret, val, null);
				}
				else if(props[i].PropertyType == typeof(Quaternion))
				{
					Quaternion	val	=Quaternion.Identity;

					val.X	=br.ReadSingle();
					val.Y	=br.ReadSingle();
					val.Z	=br.ReadSingle();
					val.W	=br.ReadSingle();

					props[i].SetValue(ret, val, null);
				}
				else if(props[i].PropertyType == typeof(Matrix))
				{
					Matrix	val	=Matrix.Identity;

					val.M11	=br.ReadSingle();
					val.M12	=br.ReadSingle();
					val.M13	=br.ReadSingle();
					val.M14	=br.ReadSingle();
					val.M21	=br.ReadSingle();
					val.M22	=br.ReadSingle();
					val.M23	=br.ReadSingle();
					val.M24	=br.ReadSingle();
					val.M31	=br.ReadSingle();
					val.M32	=br.ReadSingle();
					val.M33	=br.ReadSingle();
					val.M34	=br.ReadSingle();
					val.M41	=br.ReadSingle();
					val.M42	=br.ReadSingle();
					val.M43	=br.ReadSingle();
					val.M44	=br.ReadSingle();

					props[i].SetValue(ret, val, null);
				}
			}
			return	ret;
		}
	}
}
