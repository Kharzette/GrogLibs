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
		internal object			mVarAs;	//AsVector AsMatrix etc...

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
			else
			{
				Debug.Assert(false);
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


		#region SetVars
		void SetVar(Matrix []val)
		{
			EffectMatrixVariable	emv	=mVarAs as EffectMatrixVariable;
			if(emv == null)
			{
				return;
			}
			emv.SetMatrix(val);
		}


		void SetVar(bool []val)
		{
			EffectScalarVariable	esv	=mVarAs as EffectScalarVariable;
			if(esv == null)
			{
				return;
			}
			esv.Set(val);
		}


		void SetVar(float []val)
		{
			EffectScalarVariable	esv	=mVarAs as EffectScalarVariable;
			if(esv == null)
			{
				return;
			}
			esv.Set(val);
		}


		void SetVar(int []val)
		{
			EffectScalarVariable	esv	=mVarAs as EffectScalarVariable;
			if(esv == null)
			{
				return;
			}
			esv.Set(val);
		}


		void SetVar(uint []val)
		{
			EffectScalarVariable	esv	=mVarAs as EffectScalarVariable;
			if(esv == null)
			{
				return;
			}
			esv.Set(val);
		}


		void SetVar(Color4 []val)
		{
			EffectVectorVariable	evv	=mVarAs as EffectVectorVariable;
			if(evv == null)
			{
				return;
			}
			evv.Set(val);
		}


		void SetVar(Int4 []val)
		{
			EffectVectorVariable	evv	=mVarAs as EffectVectorVariable;
			if(evv == null)
			{
				return;
			}
			evv.Set(val);
		}


		void SetVar(Vector4 []val)
		{
			EffectVectorVariable	evv	=mVarAs as EffectVectorVariable;
			if(evv == null)
			{
				return;
			}
			evv.Set(val);
		}


		void SetVar(Bool4 []val)
		{
			EffectVectorVariable	evv	=mVarAs as EffectVectorVariable;
			if(evv == null)
			{
				return;
			}
			evv.Set(val);
		}


		void SetVar(Matrix val)
		{
			EffectMatrixVariable	emv	=mVarAs as EffectMatrixVariable;
			if(emv == null)
			{
				return;
			}
			emv.SetMatrix(val);
		}


		void SetVar(bool val)
		{
			EffectScalarVariable	esv	=mVarAs as EffectScalarVariable;
			if(esv == null)
			{
				return;
			}
			esv.Set(val);
		}


		void SetVar(float val)
		{
			EffectScalarVariable	esv	=mVarAs as EffectScalarVariable;
			if(esv == null)
			{
				return;
			}
			esv.Set(val);
		}


		void SetVar(int val)
		{
			EffectScalarVariable	esv	=mVarAs as EffectScalarVariable;
			if(esv == null)
			{
				return;
			}
			esv.Set(val);
		}


		void SetVar(uint val)
		{
			EffectScalarVariable	esv	=mVarAs as EffectScalarVariable;
			if(esv == null)
			{
				return;
			}
			esv.Set(val);
		}


		void SetVar(Color4 val)
		{
			EffectVectorVariable	evv	=mVarAs as EffectVectorVariable;
			if(evv == null)
			{
				return;
			}
			evv.Set(val);
		}


		void SetVar(Int4 val)
		{
			EffectVectorVariable	evv	=mVarAs as EffectVectorVariable;
			if(evv == null)
			{
				return;
			}
			evv.Set(val);
		}


		void SetVar(Vector2 val)
		{
			EffectVectorVariable	evv	=mVarAs as EffectVectorVariable;
			if(evv == null)
			{
				return;
			}
			evv.Set(val);
		}


		void SetVar(Vector3 val)
		{
			EffectVectorVariable	evv	=mVarAs as EffectVectorVariable;
			if(evv == null)
			{
				return;
			}
			evv.Set(val);
		}


		void SetVar(Vector4 val)
		{
			EffectVectorVariable	evv	=mVarAs as EffectVectorVariable;
			if(evv == null)
			{
				return;
			}
			evv.Set(val);
		}


		void SetVar(Bool4 val)
		{
			EffectVectorVariable	evv	=mVarAs as EffectVectorVariable;
			if(evv == null)
			{
				return;
			}
			evv.Set(val);
		}


		void SetVar(ShaderResourceView srv)
		{
			EffectShaderResourceVariable	esrv	=mVarAs as EffectShaderResourceVariable;
			if(esrv == null)
			{
				return;
			}
			esrv.SetResource(srv);
		}
		#endregion


		internal void ApplyVar()
		{
			//todo: need to support setting params to null
			if(mValue == null)
			{
				return;
			}

			if(mValue.GetType().IsArray)
			{
				if(mValue is Matrix [])
				{
					SetVar((Matrix [])mValue);
				}
				else if(mValue is bool [])
				{
					SetVar((bool [])mValue);
				}
				else if(mValue is float [])
				{
					SetVar((float [])mValue);
				}
				else if(mValue is int [])
				{
					SetVar((int [])mValue);
				}
				else if(mValue is uint [])
				{
					SetVar((uint [])mValue);
				}
				else if(mValue is Color4 [])
				{
					SetVar((Color4 [])mValue);
				}
				else if(mValue is Int4 [])
				{
					SetVar((Int4 [])mValue);
				}
				else if(mValue is Vector4 [])
				{
					SetVar((Vector4 [])mValue);
				}
				else if(mValue is Bool4 [])
				{
					SetVar((Bool4 [])mValue);
				}
				else
				{
					Debug.Assert(false);
				}
			}
			else
			{
				if(mValue is Matrix)
				{
					SetVar((Matrix)mValue);
				}
				else if(mValue is bool)
				{
					SetVar((bool)mValue);
				}
				else if(mValue is float)
				{
					SetVar((float)mValue);
				}
				else if(mValue is int)
				{
					SetVar((int)mValue);
				}
				else if(mValue is uint)
				{
					SetVar((uint)mValue);
				}
				else if(mValue is Bool4)
				{
					SetVar((Bool4)mValue);
				}
				else if(mValue is Color4)
				{
					SetVar((Color4)mValue);
				}
				else if(mValue is Int4)
				{
					SetVar((Int4)mValue);
				}
				else if(mValue is Vector2)
				{
					SetVar((Vector2)mValue);
				}
				else if(mValue is Vector3)
				{
					SetVar((Vector3)mValue);
				}
				else if(mValue is Vector4)
				{
					SetVar((Vector4)mValue);
				}
				else if(mValue is ShaderResourceView)
				{
					SetVar(mValue as ShaderResourceView);
				}
				else
				{
					Debug.Assert(false);
				}
			}
		}


		internal void SetCastThing()
		{
			if(mVar.TypeInfo.Description.Class == ShaderVariableClass.MatrixColumns)
			{
				mVarAs	=mVar.AsMatrix();
			}
			else if(mVar.TypeInfo.Description.Class == ShaderVariableClass.Object)
			{
				mVarAs	=mVar.AsShaderResource();
			}
			else if(mVar.TypeInfo.Description.Class == ShaderVariableClass.Scalar)
			{
				mVarAs	=mVar.AsScalar();
			}
			else if(mVar.TypeInfo.Description.Class == ShaderVariableClass.Struct)
			{
				Debug.Assert(false);
			}
			else if(mVar.TypeInfo.Description.Class == ShaderVariableClass.Vector)
			{
				mVarAs	=mVar.AsVector();
			}
			else
			{
				Debug.Assert(false);
			}
		}


		internal void Dispose()
		{
			if(mVarAs == null)
			{
				return;
			}

			if(mVar.TypeInfo.Description.Class == ShaderVariableClass.MatrixColumns)
			{
				(mVarAs as EffectMatrixVariable).Dispose();
			}
			else if(mVar.TypeInfo.Description.Class == ShaderVariableClass.Object)
			{
				(mVarAs as EffectShaderResourceVariable).Dispose();
			}
			else if(mVar.TypeInfo.Description.Class == ShaderVariableClass.Scalar)
			{
				(mVarAs as EffectScalarVariable).Dispose();
			}
			else if(mVar.TypeInfo.Description.Class == ShaderVariableClass.Struct)
			{
				Debug.Assert(false);
			}
			else if(mVar.TypeInfo.Description.Class == ShaderVariableClass.Vector)
			{
				(mVarAs as EffectVectorVariable).Dispose();
			}
			else
			{
				Debug.Assert(false);
			}
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
