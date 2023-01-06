using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using Vortice.Mathematics;
using System.Numerics;


namespace UtilityLib
{
	public partial class FileUtil
	{
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


		public static void WriteMatrixArray(BinaryWriter bw, Matrix4x4 []matArray)
		{
			if(matArray == null)
			{
				return;
			}

			bw.Write(matArray.Length);
			for(int i=0;i < matArray.Length;i++)
			{
				WriteMatrix(bw, matArray[i]);
			}
		}


		public static void WriteMatrix(BinaryWriter bw, Matrix4x4 mat)
		{
			bw.Write(mat.M11);
			bw.Write(mat.M12);
			bw.Write(mat.M13);
			bw.Write(mat.M14);
			bw.Write(mat.M21);
			bw.Write(mat.M22);
			bw.Write(mat.M23);
			bw.Write(mat.M24);
			bw.Write(mat.M31);
			bw.Write(mat.M32);
			bw.Write(mat.M33);
			bw.Write(mat.M34);
			bw.Write(mat.M41);
			bw.Write(mat.M42);
			bw.Write(mat.M43);
			bw.Write(mat.M44);
		}


		public static Matrix4x4 []ReadMatrixArray(BinaryReader br)
		{
			int	count	=br.ReadInt32();

			Debug.Assert(count > 0);

			Matrix4x4	[]ret	=new Matrix4x4[count];

			for(int i=0;i < count;i++)
			{
				ret[i]	=ReadMatrix(br);
			}
			return	ret;
		}


		public static Matrix4x4 ReadMatrix(BinaryReader br)
		{
			Matrix4x4	ret	=Matrix4x4.Identity;

			ret.M11	=br.ReadSingle();
			ret.M12	=br.ReadSingle();
			ret.M13	=br.ReadSingle();
			ret.M14	=br.ReadSingle();
			ret.M21	=br.ReadSingle();
			ret.M22	=br.ReadSingle();
			ret.M23	=br.ReadSingle();
			ret.M24	=br.ReadSingle();
			ret.M31	=br.ReadSingle();
			ret.M32	=br.ReadSingle();
			ret.M33	=br.ReadSingle();
			ret.M34	=br.ReadSingle();
			ret.M41	=br.ReadSingle();
			ret.M42	=br.ReadSingle();
			ret.M43	=br.ReadSingle();
			ret.M44	=br.ReadSingle();

			return	ret;
		}


		public static void WriteVector2(BinaryWriter bw, Vector2 vec)
		{
			bw.Write(vec.X);
			bw.Write(vec.Y);
		}


		public static Vector2 ReadVector2(BinaryReader br)
		{
			Vector2	ret	=Vector2.Zero;

			ret.X	=br.ReadSingle();
			ret.Y	=br.ReadSingle();

			return	ret;
		}


		public static void WriteVector3(BinaryWriter bw, Vector3 vec)
		{
			bw.Write(vec.X);
			bw.Write(vec.Y);
			bw.Write(vec.Z);
		}


		public static Vector3 ReadVector3(BinaryReader br)
		{
			Vector3	ret	=Vector3.Zero;

			ret.X	=br.ReadSingle();
			ret.Y	=br.ReadSingle();
			ret.Z	=br.ReadSingle();

			return	ret;
		}


		public static void WriteVector4(BinaryWriter bw, Vector4 vec)
		{
			bw.Write(vec.X);
			bw.Write(vec.Y);
			bw.Write(vec.Z);
			bw.Write(vec.W);
		}


		public static Vector4 ReadVector4(BinaryReader br)
		{
			Vector4	ret	=Vector4.Zero;

			ret.X	=br.ReadSingle();
			ret.Y	=br.ReadSingle();
			ret.Z	=br.ReadSingle();
			ret.W	=br.ReadSingle();

			return	ret;
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


		//loads up emissive colors from materials (for bouncing light or whatever)
		public static Dictionary<string, Color> LoadEmissives(string fileName)
		{
			string	emmName	=StripExtension(fileName);

			emmName	+=".Emissives";

			if(!File.Exists(emmName))
			{
				//not a big deal, just use white
				return	null;
			}

			FileStream		fs	=new FileStream(emmName, FileMode.Open, FileAccess.Read);
			BinaryReader	br	=new BinaryReader(fs);

			UInt32	magic	=br.ReadUInt32();
			if(magic != 0xED1551BE)
			{
				return	null;
			}

			Dictionary<string, Color>	ret	=new Dictionary<string, Color>();

			Color	tempColor;

			int	count	=br.ReadInt32();
			for(int i=0;i < count;i++)
			{
				string	matName	=br.ReadString();

				UInt32	val	=br.ReadUInt32();

				tempColor	=new Color(val);

				ret.Add(matName, tempColor);
			}

			br.Close();
			fs.Close();

			return	ret;
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
				else if(madProp is Matrix4x4)
				{
					Matrix4x4	gack	=(Matrix4x4)madProp;
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
				else if(props[i].PropertyType == typeof(Matrix4x4))
				{
					Matrix4x4	val	=Matrix4x4.Identity;

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
