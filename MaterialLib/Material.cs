using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;
using UtilityLib;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;

//ambiguous stuff
using Buffer = SharpDX.Direct3D11.Buffer;
using Color = SharpDX.Color;
using Device = SharpDX.Direct3D11.Device;


namespace MaterialLib
{
	internal class Material
	{
		string			mName;			//name of the overall material
		Effect			mEffect;		//ref of the shader
		EffectTechnique	mTechnique;		//technique to use with this material

		//all of the shader variables TODO: hide/ignore some
		Dictionary<string, EffectVariableValue>	mVars	=new Dictionary<string, EffectVariableValue>();


		internal Material(string name)
		{
			mName	=name;
		}


		public string Name
		{
			get { return mName; }
			set { mName = Misc.AssignValue(value); }
		}
		public Effect Shader
		{
			get { return mEffect; }
			set { mEffect = value; }
		}
		public EffectTechnique Technique
		{
			get { return mTechnique; }
			set { mTechnique = value; }
		}


		internal void Clear()
		{
			mEffect		=null;
			mTechnique	=null;
			mVars.Clear();
		}


		internal void ApplyPass(DeviceContext dc, int pass)
		{
			EffectPass	ep	=mTechnique.GetPassByIndex(pass);
			if(ep == null)
			{
				return;
			}

			if(!ep.IsValid)
			{
				return;
			}

			ApplyVariables();

			ep.Apply(dc);
		}


		internal SharpDX.D3DCompiler.ShaderBytecode GetPassSignature(int pass)
		{
			if(mTechnique == null)
			{
				return	null;
			}
			EffectPass	ep	=mTechnique.GetPassByIndex(pass);
			if(ep == null)
			{
				return	null;
			}
			return	ep.Description.Signature;
		}


		internal BindingList<EffectVariableValue> GetVariables()
		{
			BindingList<EffectVariableValue>	ret	=new BindingList<EffectVariableValue>();

			foreach(KeyValuePair<string, EffectVariableValue> var in mVars)
			{
				ret.Add(var.Value);
			}
			return	ret;
		}


		#region IO
		internal void Write(BinaryWriter bw)
		{
			bw.Write(mName);

		}


		internal void Read(BinaryReader br)
		{
			mName			=br.ReadString();
		}
		#endregion


		void ApplyVariables()
		{
			foreach(KeyValuePair<string, EffectVariableValue> varVal in mVars)
			{
				object			val	=varVal.Value.mValue;
				EffectVariable	var	=varVal.Value.mVar;

				if(val == null)
				{
					continue;
				}

				if(val.GetType().IsArray)
				{
					if(val is Matrix [])
					{
						var.AsMatrix().SetMatrix((Matrix [])val);
					}
					else if(val is bool [])
					{
						var.AsScalar().Set((bool [])val);
					}
					else if(val is float [])
					{
						var.AsScalar().Set((float [])val);
					}
					else if(val is int [])
					{
						var.AsScalar().Set((int [])val);
					}
					else if(val is uint [])
					{
						var.AsScalar().Set((uint [])val);
					}
					else if(val is Color4 [])
					{
						var.AsVector().Set((Color4 [])val);
					}
					else if(val is Int4 [])
					{
						var.AsVector().Set((Int4 [])val);
					}
					else if(val is Vector4 [])
					{
						var.AsVector().Set((Vector4 [])val);
					}
					else if(val is Bool4 [])
					{
						var.AsVector().Set((Bool4 [])val);
					}
					else
					{
						Debug.Assert(false);
					}
				}
				else
				{
					if(val is Matrix)
					{
						var.AsMatrix().SetMatrix((Matrix)val);
					}
					else if(val is bool)
					{
						var.AsScalar().Set((bool)val);
					}
					else if(val is float)
					{
						var.AsScalar().Set((float)val);
					}
					else if(val is int)
					{
						var.AsScalar().Set((int)val);
					}
					else if(val is uint)
					{
						var.AsScalar().Set((uint)val);
					}
					else if(val is Bool4)
					{
						var.AsVector().Set((Bool4)val);
					}
					else if(val is Color4)
					{
						var.AsVector().Set((Color4)val);
					}
					else if(val is Int4)
					{
						var.AsVector().Set((Int4)val);
					}
					else if(val is Vector2)
					{
						var.AsVector().Set((Vector2)val);
					}
					else if(val is Vector3)
					{
						var.AsVector().Set((Vector3)val);
					}
					else if(val is Vector4)
					{
						var.AsVector().Set((Vector4)val);
					}
					else
					{
						Debug.Assert(false);
					}
				}
			}
		}


		internal void SetVariables(List<EffectVariable> vars)
		{
			mVars.Clear();

			foreach(EffectVariable var in vars)
			{
				EffectVariableValue	evv	=new EffectVariableValue();

				evv.mValue	=null;
				evv.mVar	=var;

				mVars.Add(var.Description.Name, evv);
			}
		}


		internal void SetEffectParameter(string name, object value)
		{
			if(!mVars.ContainsKey(name))
			{
				return;
			}

			EffectVariableValue	evv	=mVars[name];

			evv.mValue	=value;
		}
	}
}
