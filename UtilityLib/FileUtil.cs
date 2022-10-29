using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Reflection;


namespace UtilityLib
{
	public partial class FileUtil
	{
		public static string GetExtension(string fileName)
		{
			int	dotPos	=fileName.LastIndexOf('.');
			if(dotPos != -1)
			{
				return	fileName.Substring(dotPos + 1);
			}
			return	"";
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


		//returns only the path bit
		public static string StripFileName(string fileName)
		{
			string	conv	=ConvertPathSlashes(fileName);

			int	slashPos	=conv.LastIndexOf('/');
			if(slashPos != -1)
			{
				return	conv.Substring(0, slashPos);
			}
			return	".";
		}


		//returns just the filename part
		public static string StripPath(string fileName)
		{
			string	conv	=ConvertPathSlashes(fileName);

			int	slashPos	=conv.LastIndexOf('/');
			if(slashPos != -1)
			{
				return	conv.Substring(slashPos + 1);
			}
			return	fileName;
		}


		//converts \\ to /
		public static string ConvertPathSlashes(string path)
		{
			return	path.Replace('\\', '/');
		}


		//converts / to \\
		public static string ConvertPathBackSlashes(string path)
		{
			return	path.Replace('/', '\\');
		}


		public static bool FileExists(string fileName)
		{
#if !XBOX
			return	File.Exists(fileName);
#else
			return	false;
#endif
		}


		public static void WriteArray<T>(T []zms, BinaryWriter bw)
		{
			MethodInfo	write	=typeof(T).GetMethods().Where(
				x => x.Name == "Write").FirstOrDefault();

			object	[]parms	=new object[1];
			parms[0]		=bw;

			bw.Write(zms.Length);
			for(int i=0;i < zms.Length;i++)
			{
				write.Invoke(zms[i], parms);
			}
		}


		public static T []ReadArray<T>(BinaryReader br)
		{
			int	count	=br.ReadInt32();

			T	[]ret	=new T[count];

			MethodInfo	read	=typeof(T).GetMethods().Where(
				x => x.Name == "Read").FirstOrDefault();

			ConstructorInfo	ci	=typeof(T).GetConstructor(Type.EmptyTypes);

			object	[]parms;
			if(ci != null)
			{
				parms		=new object[1];
				parms[0]	=br;
			}
			else
			{
				parms		=new object[2];
				parms[0]	=br;
				parms[1]	=null;
			}

			for(int i=0;i < count;i++)
			{
				if(ci != null)	//class?
				{
					ret[i]	=(T)ci.Invoke(null);
					read.Invoke(ret[i], parms);
				}
				else
				{
					read.Invoke(ret[i], parms);
					ret[i]	=(T)parms[1];	//out parameter
				}
			}

			return	ret;
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


		public static void WriteArray(BinaryWriter bw, float []fArray)
		{
			bw.Write(fArray.Length);
			for(int i=0;i < fArray.Length;i++)
			{
				bw.Write(fArray[i]);
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


		public static float []ReadFloatArray(BinaryReader br)
		{
			int	count	=br.ReadInt32();

			Debug.Assert(count > 0);

			float	[]ret	=new float[count];

			for(int i=0;i < count;i++)
			{
				ret[i]	=br.ReadSingle();
			}
			return	ret;
		}


		public static ArrayType []InitArray<ArrayType>(int count) where ArrayType : new()
		{
			ArrayType	[]ret	=new ArrayType[count];
			for(int i=0;i < count;i++)
			{
				ret[i]	=new ArrayType();
			}
			return	ret;
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
	}
}
