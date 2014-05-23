using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.Direct3D;
using UtilityLib;


namespace MaterialLib
{
	public class EffectVariableValue
	{
		internal EffectVariable	mVar;
		internal object			mValue;

		public string Name
		{
			get { return mVar.Description.Name; }
		}

		public object Value
		{
			get { return ValueAsString(mValue); }
			set { mValue = ValueFromString(value); }
		}


		object ValueFromString(object val)
		{
			if(!(val is string))
			{
				return	val;
			}

			string	sz	=(string)val;

			if(mVar.TypeInfo.Description.Class == ShaderVariableClass.MatrixColumns)
			{
				return	Misc.StringToMatrix(sz);
			}
			else if(mVar.TypeInfo.Description.Class == ShaderVariableClass.Object)
			{
				return	sz;
			}
			else if(mVar.TypeInfo.Description.Class == ShaderVariableClass.Scalar)
			{
				if(mVar.TypeInfo.Description.Type == ShaderVariableType.Float)
				{
					if(mVar.TypeInfo.Description.Elements > 0)
					{
						return	ParseFloatArray(sz);
					}
					else
					{
						float	fval;
						Mathery.TryParse(sz, out fval);
						return	fval;
					}
				}
				else if(mVar.TypeInfo.Description.Type == ShaderVariableType.Bool)
				{
					bool	bVal;
					Mathery.TryParse(sz, out bVal);
					return	bVal;
				}
				else if(mVar.TypeInfo.Description.Type == ShaderVariableType.Int)
				{
					int	iVal;
					Mathery.TryParse(sz, out iVal);
					return	iVal;
				}
				else
				{
					Debug.Assert(false);
				}
			}
			else if(mVar.TypeInfo.Description.Class == ShaderVariableClass.Struct)
			{
				Debug.Assert(false);
			}
			else if(mVar.TypeInfo.Description.Class == ShaderVariableClass.Vector)
			{
				if(mVar.TypeInfo.Description.Columns == 2)
				{
					return	Misc.StringToVector2(sz);
				}
				else if(mVar.TypeInfo.Description.Columns == 3)
				{
					return	Misc.StringToVector3(sz);
				}
				else if(mVar.TypeInfo.Description.Columns == 4)
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


		string ValueAsString(object val)
		{
			if(val == null)
			{
				return	"";
			}

			if(mVar.TypeInfo.Description.Class == ShaderVariableClass.MatrixColumns)
			{
				if(mVar.TypeInfo.Description.Elements > 0)
				{
					return	"Big Ass MatArray";
				}
				else
				{
					return	Misc.MatrixToString((Matrix)val);
				}
			}
			else if(mVar.TypeInfo.Description.Class == ShaderVariableClass.Object)
			{
				if(val is string)	//still in texname form?
				{
					return	(string)val;
				}
				else if(val is ShaderResourceView)
				{
					ShaderResourceView	srv	=val as ShaderResourceView;
					return	srv.DebugName;
				}
				else
				{
					return	"SomeObject";
				}
			}
			else if(mVar.TypeInfo.Description.Class == ShaderVariableClass.Scalar)
			{
				if(mVar.TypeInfo.Description.Type == ShaderVariableType.Float)
				{
					if(mVar.TypeInfo.Description.Elements > 0)
					{
						return	Misc.FloatArrayToString((float [])val);
					}
					else
					{
						return	Misc.FloatToString((float)val);
					}
				}
				else if(mVar.TypeInfo.Description.Type == ShaderVariableType.Bool)
				{
					return	((bool)val).ToString(System.Globalization.CultureInfo.InvariantCulture);
				}
				else if(mVar.TypeInfo.Description.Type == ShaderVariableType.Int)
				{
					return	((int)val).ToString(System.Globalization.CultureInfo.InvariantCulture);
				}
				else
				{
					Debug.Assert(false);
				}
			}
			else if(mVar.TypeInfo.Description.Class == ShaderVariableClass.Struct)
			{
				Debug.Assert(false);
			}
			else if(mVar.TypeInfo.Description.Class == ShaderVariableClass.Vector)
			{
				if(mVar.TypeInfo.Description.Columns == 2)
				{
					return	Misc.VectorToString((Vector2)val);
				}
				else if(mVar.TypeInfo.Description.Columns == 3)
				{
					return	Misc.VectorToString((Vector3)val);
				}
				else if(mVar.TypeInfo.Description.Columns == 4)
				{
					return	Misc.VectorToString((Vector4)val);
				}
				else
				{
					Debug.Assert(false);
				}
			}
			return	null;
		}

		float[] ParseFloatArray(string floats)
		{
			string	[]toks	=floats.Split(' ');

			List<float>	ret	=new List<float>();

			foreach(string tok in toks)
			{
				float	f;
				if(Mathery.TryParse(tok, out f))
				{
					ret.Add(f);
				}
			}
			return	ret.ToArray();
		}


		internal void Write(BinaryWriter bw)
		{
			bw.Write(Name);
			bw.Write(ValueAsString(mValue));
		}


		internal void Read(BinaryReader br)
		{
			string	val	=br.ReadString();
			if(val == "")
			{
				mValue	=null;
				return;
			}

			mValue	=ValueFromString(val);
		}
	}
}
