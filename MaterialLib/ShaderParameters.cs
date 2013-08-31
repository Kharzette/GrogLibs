using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using UtilityLib;


namespace MaterialLib
{
	public class ShaderParameters
	{
		string					mName;
		EffectParameterClass	mClass;
		EffectParameterType		mType;
		int						mCount;
		object					mValue;

		//pointer to the real parameter
		internal EffectParameter	mParam;


		public ShaderParameters()
		{
			mName	="";
			mValue	=null;
		}

		public string Name
		{
			get { return mName; }
			set { mName = UtilityLib.Misc.AssignValue(value); }
		}
		public EffectParameterClass Class
		{
			get { return mClass; }
			set { mClass = value; }
		}
		public EffectParameterType Type
		{
			get { return mType; }
			set { mType = value; }
		}

		//this one is used by the tools to fill in grids and such
		public string Value
		{
			get
			{
				return	ValueAsString();
			}
			set
			{
				mValue	=ValueFromString(value);
			}
		}


		internal void SetCount(int count)
		{
			mCount	=count;
		}


		internal object GetRealValue()
		{
			return	mValue;
		}


		internal void SetRealValue(object val)
		{
			mValue	=val;
		}


		internal void Write(BinaryWriter bw)
		{
			bw.Write(mName);
			bw.Write((UInt32)mClass);
			bw.Write((UInt32)mType);
			bw.Write(mCount);

			bw.Write(mValue != null);

			if(mValue == null)
			{
				return;
			}

			if(mType == EffectParameterType.Bool)
			{
				bw.Write((bool)mValue);
			}
			else if(mType == EffectParameterType.Int32)
			{
				bw.Write((Int32)mValue);
			}
			else if(mType == EffectParameterType.Single)
			{
				if(mClass == EffectParameterClass.Matrix)
				{
					if(mValue is Matrix)
					{
						FileUtil.WriteMatrix(bw, (Matrix)mValue);
					}
					else
					{
						FileUtil.WriteMatrixArray(bw, (Matrix [])mValue);
					}
				}
				else if(mClass == EffectParameterClass.Vector)
				{
					if(mCount == 1)
					{
						bw.Write((float)mValue);
					}
					else if(mCount == 2)
					{
						Vector2	val	=(Vector2)mValue;
						FileUtil.WriteVector2(bw, val);
					}
					else if(mCount == 3)
					{
						Vector3	val	=(Vector3)mValue;
						FileUtil.WriteVector3(bw, val);
					}
					else if(mCount == 4)
					{
						Vector4	val	=(Vector4)mValue;
						FileUtil.WriteVector4(bw, val);
					}
					else
					{
						Debug.Assert(false);
					}
				}
				else if(mClass == EffectParameterClass.Scalar)
				{
					if(mCount == 1)
					{
						bw.Write((float)mValue);
					}
					else
					{
						FileUtil.WriteArray(bw, (float [])mValue);
					}
				}
			}
			else if(mType == EffectParameterType.String)
			{
				bw.Write((string)mValue);
			}
			else if(mType == EffectParameterType.Void)
			{
				Debug.Assert(false);	//no idea
			}
			else
			{
				//textures
				if(mValue == null)
				{
					bw.Write("");
				}
				else if(mValue is string)
				{
					bw.Write(mValue as string);
				}
				else
				{
					bw.Write((string)((Texture)mValue).Name);
				}
			}
		}


		internal void Read(BinaryReader br)
		{
			mName	=br.ReadString();
			mClass	=(EffectParameterClass)br.ReadUInt32();
			mType	=(EffectParameterType)br.ReadUInt32();
			mCount	=br.ReadInt32();

			bool	bNotNull	=br.ReadBoolean();

			if(!bNotNull)
			{
				mValue	=null;
				return;
			}

			if(mType == EffectParameterType.Bool)
			{
				mValue	=br.ReadBoolean();
			}
			else if(mType == EffectParameterType.Int32)
			{
				mValue	=br.ReadInt32();
			}
			else if(mType == EffectParameterType.Single)
			{
				if(mClass == EffectParameterClass.Matrix)
				{
					if(mCount <= 1)
					{
						Matrix	val	=FileUtil.ReadMatrix(br);
						mValue		=val;
					}
					else
					{
						Matrix	[]mats	=FileUtil.ReadMatrixArray(br);
						mValue			=mats;
					}
				}
				else if(mClass == EffectParameterClass.Vector)
				{
					if(mCount == 1)
					{
						mValue	=br.ReadSingle();
					}
					else if(mCount == 2)
					{
						Vector2	val	=FileUtil.ReadVector2(br);
						mValue		=val;
					}
					else if(mCount == 3)
					{
						Vector3	val	=FileUtil.ReadVector3(br);
						mValue		=val;
					}
					else if(mCount == 4)
					{
						Vector4	val	=FileUtil.ReadVector4(br);
						mValue		=val;
					}
					else
					{
						Debug.Assert(false);
					}
				}
				else if(mClass == EffectParameterClass.Scalar)
				{
					if(mCount == 1)
					{
						mValue	=br.ReadSingle();
					}
					else
					{
						mValue	=FileUtil.ReadFloatArray(br);
					}
				}
			}
			else if(mType == EffectParameterType.String)
			{
				mValue	=br.ReadString();
			}
			else
			{
				//textures, just read the name
				mValue	=br.ReadString();
			}
		}


		float[] ParseFloatArray(string floats)
		{
			string	[]toks	=floats.Split(' ');

			List<float>	ret	=new List<float>();

			foreach(string tok in toks)
			{
				float	f;
				if(UtilityLib.Mathery.TryParse(tok, out f))
				{
					ret.Add(f);
				}
			}
			return	ret.ToArray();
		}


		object ValueFromString(string sz)
		{
			if(mClass == EffectParameterClass.Matrix)
			{
				return	Misc.StringToMatrix(sz);
			}
			else if(mClass == EffectParameterClass.Object)
			{
				return	sz;
			}
			else if(mClass == EffectParameterClass.Scalar)
			{
				if(mType == EffectParameterType.Single)
				{
					if(mCount == 1)
					{
						float	val;
						Mathery.TryParse(sz, out val);
						return	val;
					}
					else
					{
						return	ParseFloatArray(sz);
					}
				}
				else if(mType == EffectParameterType.Bool)
				{
					bool	bVal;
					Mathery.TryParse(sz, out bVal);
					return	bVal;
				}
				else
				{
					Debug.Assert(false);
				}
			}
			else if(mClass == EffectParameterClass.Struct)
			{
				Debug.Assert(false);
			}
			else if(mClass == EffectParameterClass.Vector)
			{
				if(mCount == 2)
				{
					return	Misc.StringToVector2(sz);
				}
				else if(mCount == 3)
				{
					return	Misc.StringToVector3(sz);
				}
				else if(mCount == 4)
				{
					return	Misc.StringToVector4(sz);
				}
				else
				{
					Debug.Assert(false);
				}
			}
			return	null;
		}


		string ValueAsString()
		{
			if(mValue == null)
			{
				return	"";
			}

			if(mClass == EffectParameterClass.Matrix)
			{
				if(mCount > 1)
				{
					return	"Big Ass Array";
				}
				else
				{
					return	Misc.MatrixToString((Matrix)mValue);
				}
			}
			else if(mClass == EffectParameterClass.Object)
			{
				if(mValue is string)	//still in texname form?
				{
					return	(string)mValue;
				}
				else
				{
					return	((Texture)mValue).Name;
				}
			}
			else if(mClass == EffectParameterClass.Scalar)
			{
				if(mType == EffectParameterType.Single)
				{
					if(mCount == 1)
					{
						return	Misc.FloatToString((float)mValue);
					}
					else
					{
						return	Misc.FloatArrayToString((float [])mValue);
					}
				}
				else if(mType == EffectParameterType.Bool)
				{
					return	((bool)mValue).ToString(System.Globalization.CultureInfo.InvariantCulture);
				}
				else
				{
					Debug.Assert(false);
				}
			}
			else if(mClass == EffectParameterClass.Struct)
			{
				Debug.Assert(false);
			}
			else if(mClass == EffectParameterClass.Vector)
			{
				if(mCount == 2)
				{
					return	Misc.VectorToString((Vector2)mValue);
				}
				else if(mCount == 3)
				{
					return	Misc.VectorToString((Vector3)mValue);
				}
				else if(mCount == 4)
				{
					return	Misc.VectorToString((Vector4)mValue);
				}
				else
				{
					Debug.Assert(false);
				}
			}
			return	null;
		}
	}
}
