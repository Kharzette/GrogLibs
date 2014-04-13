using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
		string			mName;			//name of the material
		Effect			mEffect;		//ref of the shader
		EffectTechnique	mTechnique;		//technique to use with this material

		//all of the shader variables TODO: hide/ignore some
		Dictionary<string, EffectVariableValue>	mVars	=new Dictionary<string, EffectVariableValue>();

		//stuff the gui doesn't want to see
		List<EffectVariableValue>	mHidden	=new List<EffectVariableValue>();

		//skip these when assigning parameters to the shader
		List<EffectVariableValue>	mIgnored	=new List<EffectVariableValue>();

		//delegates for IO
		internal delegate string				NameForEffect(Effect e);
		internal delegate Effect				EffectForName(string name);
		internal delegate List<EffectVariable>	GrabVariables(string fx);


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


		internal Material Clone(string newName)
		{
			Material	ret	=new Material(newName);

			ret.Shader		=Shader;
			ret.Technique	=Technique;

			foreach(KeyValuePair<string, EffectVariableValue> evv in mVars)
			{
				EffectVariableValue	evv2	=new EffectVariableValue();

				//this should be a valuetype?  hope no ref problems
				evv2.mValue	=evv.Value.mValue;
				evv2.mVar	=evv.Value.mVar;

				ret.mVars.Add(evv.Key, evv2);
			}

			IEnumerable<string>	hide	=from evv in mHidden select evv.mVar.Description.Name;
			IEnumerable<string>	ignore	=from evv in mIgnored select evv.mVar.Description.Name;

			ret.Hide(hide.ToList());
			ret.Ignore(ignore.ToList());

			return	ret;
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


		internal BindingList<EffectVariableValue> GetGUIVariables()
		{
			BindingList<EffectVariableValue>	ret	=new BindingList<EffectVariableValue>();

			foreach(KeyValuePair<string, EffectVariableValue> var in mVars)
			{
				if(mIgnored.Contains(var.Value))
				{
					continue;
				}
				if(mHidden.Contains(var.Value))
				{
					continue;
				}
				ret.Add(var.Value);
			}
			return	ret;
		}


		internal void Ignore(List<string> toIgnore)
		{
			foreach(string vname in toIgnore)
			{
				if(!mVars.ContainsKey(vname))
				{
					continue;
				}
				if(mIgnored.Contains(mVars[vname]))
				{
					continue;
				}
				mIgnored.Add(mVars[vname]);
			}
		}


		internal void Hide(List<string> toHide)
		{
			foreach(string vname in toHide)
			{
				if(!mVars.ContainsKey(vname))
				{
					continue;
				}
				if(mHidden.Contains(mVars[vname]))
				{
					continue;
				}
				mHidden.Add(mVars[vname]);
			}
		}


		internal void UnHideAll()
		{
			mHidden.Clear();
			mIgnored.Clear();
		}


		#region IO
		internal void Write(BinaryWriter bw, NameForEffect nfe)
		{
			bw.Write(mName);
			bw.Write(nfe(mEffect));
			bw.Write(mTechnique.Description.Name);

			bw.Write(mHidden.Count);
			foreach(EffectVariableValue evv in mHidden)
			{
				bw.Write(evv.Name);
			}

			bw.Write(mIgnored.Count);
			foreach(EffectVariableValue evv in mIgnored)
			{
				bw.Write(evv.Name);
			}

			bw.Write(mVars.Count);
			foreach(KeyValuePair<string, EffectVariableValue> varVal in mVars)
			{
				varVal.Value.Write(bw);
			}
		}


		internal void Read(BinaryReader br, EffectForName efn, GrabVariables gv)
		{
			mName	=br.ReadString();

			string	fx	=br.ReadString();

			mEffect							=efn(fx);
			List<EffectVariable>	vars	=gv(fx);

			SetVariables(vars);

			string	tech	=br.ReadString();
			mTechnique		=mEffect.GetTechniqueByName(tech);

			int	count	=br.ReadInt32();

			List<string>	varNames	=new List<string>();
			for(int i=0;i < count;i++)
			{
				varNames.Add(br.ReadString());
			}
			Hide(varNames);

			count	=br.ReadInt32();
			varNames.Clear();
			for(int i=0;i < count;i++)
			{
				varNames.Add(br.ReadString());
			}
			Ignore(varNames);

			count	=br.ReadInt32();
			for(int i=0;i < count;i++)
			{
				string	varName	=br.ReadString();
				if(mVars.ContainsKey(varName))
				{
					mVars[varName].Read(br);
				}
				else
				{
					br.ReadString();	//consume
				}
			}
		}
		#endregion


		void ApplyVariables()
		{
			foreach(KeyValuePair<string, EffectVariableValue> varVal in mVars)
			{
				if(mIgnored.Contains(varVal.Value))
				{
					continue;
				}

				object			val	=varVal.Value.mValue;
				EffectVariable	var	=varVal.Value.mVar;

				//todo: need to support setting params to null
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
			mHidden.Clear();
			mIgnored.Clear();

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


		internal void GuessParameterVisibility(
			Dictionary<string, List<string>> ignoreData,
			Dictionary<string, List<string>> hiddenData)
		{
			//clear existing hide/ignore stuff
			mHidden.Clear();
			mIgnored.Clear();

			string	tech	=mTechnique.Description.Name;

			if(ignoreData.ContainsKey(tech))
			{
				Ignore(ignoreData[tech]);
			}

			if(hiddenData.ContainsKey(tech))
			{
				Hide(hiddenData[tech]);
			}
		}


		internal void ResetParameterVisibility()
		{
			mIgnored.Clear();
			mHidden.Clear();
		}
	}
}
