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
using SharpDX.D3DCompiler;

//ambiguous stuff
using Device	=SharpDX.Direct3D11.Device;


namespace MaterialLib
{
	internal class Material
	{
		string			mName;			//name of the material
		Effect			mEffect;		//ref of the shader
		EffectTechnique	mTechnique;		//technique to use with this material
		int				mNumPasses;		//number of passes for the technique
		bool			mbFinalized;	//trimmed down ignored yet?

		//Effect passes are allocated / freed alot unless cached
		List<EffectPass>	mPasses		=new List<EffectPass>();

		//vert formats expected
		List<InputLayout>	mLayouts	=new List<InputLayout>();

		//all of the shader variables TODO: hide/ignore some
		Dictionary<string, EffectVariableValue>	mVars	=new Dictionary<string, EffectVariableValue>();

		//finalized list with the ignored stuff removed
		Dictionary<string, EffectVariableValue>	mFinalVars	=new Dictionary<string, EffectVariableValue>();

		//stuff the gui doesn't want to see
		List<EffectVariableValue>	mHidden	=new List<EffectVariableValue>();

		//skip these when assigning parameters to the shader
		List<EffectVariableValue>	mIgnored	=new List<EffectVariableValue>();

		//delegates for IO
		internal delegate string				NameForEffect(Effect e);
		internal delegate Effect				EffectForName(string name);
		internal delegate ShaderResourceView	ResourceForName(string name);
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
			set { mTechnique = value;  CalcLayouts(); }
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
				evv2.mVarAs	=evv.Value.mVarAs;

				ret.mVars.Add(evv.Key, evv2);
			}

			IEnumerable<string>	hide	=from evv in mHidden select evv.mVar.Description.Name;
			IEnumerable<string>	ignore	=from evv in mIgnored select evv.mVar.Description.Name;

			ret.Hide(hide.ToList());
			ret.Ignore(ignore.ToList());

			return	ret;
		}


		internal int GetNumPasses()
		{
			return	mNumPasses;
		}


		internal void Clear()
		{
			mEffect		=null;
			mTechnique	=null;

			foreach(InputLayout il in mLayouts)
			{
				il.Dispose();
			}
			mLayouts.Clear();
			
			foreach(EffectPass ep in mPasses)
			{
				ep.Dispose();
			}
			mPasses.Clear();

			foreach(KeyValuePair<string, EffectVariableValue> varVal in mVars)
			{
				varVal.Value.Dispose();
			}
			mVars.Clear();
		}


		//oops be careful using the name Finalize, it means something in the language!
		internal void FinalizeMat()
		{
			if(mbFinalized)
			{
				//redo?
				mFinalVars.Clear();
			}

			foreach(KeyValuePair<string, EffectVariableValue> varVal in mVars)
			{
				if(mIgnored.Contains(varVal.Value))
				{
					continue;
				}
				mFinalVars.Add(varVal.Key, varVal.Value);
			}

			mbFinalized	=true;
		}


		internal void ApplyPass(DeviceContext dc, int pass)
		{
			if(mTechnique == null)
			{
				return;
			}

			EffectPass	ep	=mPasses[pass];
			if(ep == null)
			{
				return;
			}

			Debug.Assert(ep.IsValid);

			if(!ep.IsValid)
			{
				return;
			}

			ApplyVariables();

			dc.InputAssembler.InputLayout	=mLayouts[pass];

			ep.Apply(dc);
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


		internal void UnIgnoreAll()
		{
			mIgnored.Clear();
		}


		internal void UnHideAll()
		{
			mHidden.Clear();
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

			if(mEffect == null || vars.Count == 0)
			{
				return;
			}

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

			CalcLayouts();
		}
		#endregion


		void ApplyVariables()
		{
			if(mbFinalized)
			{
				foreach(KeyValuePair<string, EffectVariableValue> varVal in mFinalVars)
				{
					varVal.Value.ApplyVar();
				}
				return;
			}

			foreach(KeyValuePair<string, EffectVariableValue> varVal in mVars)
			{
				if(mIgnored.Contains(varVal.Value))
				{
					continue;
				}

				varVal.Value.ApplyVar();
			}
		}


		//after loading, resources are just in string form, fix em
		internal void SetResources(ResourceForName resForName)
		{
			foreach(KeyValuePair<string, EffectVariableValue> varVal in mVars)
			{
				EffectVariable	var	=varVal.Value.mVar;
				object			val	=varVal.Value.mValue;

				if(var.TypeInfo.Description.Type == ShaderVariableType.Texture2D)
				{
					if(val != null && val is string)
					{
						varVal.Value.mValue	=resForName(val as string);
					}
				}
			}
		}


		internal void SetVariables(List<EffectVariable> vars)
		{
			foreach(KeyValuePair<string, EffectVariableValue> varVal in mVars)
			{
				varVal.Value.Dispose();
			}
			mVars.Clear();
			mHidden.Clear();
			mIgnored.Clear();

			foreach(EffectVariable var in vars)
			{
				EffectVariableValue	evv	=new EffectVariableValue();

				evv.mValue	=null;
				evv.mVar	=var;

				//one time grab of the reflectiony data
				evv.SetExtraData();

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


		Format	GetFormatFromParamDesc(ShaderParameterDescription spd)
		{
			//special cases here
			//the reflection stuff returns float4 for stuff I want half4
			if(spd.SemanticName == "BLENDWEIGHTS")
			{
				return	Format.R16G16B16A16_Float;
			}
			if(spd.SemanticName == "BLENDINDICES")
			{
				return	Format.R8G8B8A8_UInt;
			}
			if(spd.SemanticName == "COLOR")
			{
				return	Format.R8G8B8A8_UNorm;
			}
			else if(spd.SemanticName == "TANGENT")
			{
				return	Format.R16G16B16A16_Float;
			}

			if(spd.SemanticName == "TEXCOORD" || spd.SemanticName == "NORMAL")
			{
				if(((spd.UsageMask & RegisterComponentMaskFlags.ComponentW) != 0))
				{
					return	Format.R16G16B16A16_Float;
				}
				else if(((spd.UsageMask & RegisterComponentMaskFlags.ComponentZ) != 0))
				{
					return	Format.R16G16B16A16_Float;	//xyz not supported I suppose
				}
				else if(((spd.UsageMask & RegisterComponentMaskFlags.ComponentY) != 0))
				{
					return	Format.R16G16_Float;
				}
				else if(((spd.UsageMask & RegisterComponentMaskFlags.ComponentX) != 0))
				{
					return	Format.R16_Float;
				}
			}

			if(spd.ComponentType == RegisterComponentType.Float32)
			{
				if(((spd.UsageMask & RegisterComponentMaskFlags.ComponentW) != 0))
				{
					return	Format.R32G32B32A32_Float;
				}
				else if(((spd.UsageMask & RegisterComponentMaskFlags.ComponentZ) != 0))
				{
					return	Format.R32G32B32_Float;
				}
				else if(((spd.UsageMask & RegisterComponentMaskFlags.ComponentY) != 0))
				{
					return	Format.R32G32_Float;
				}
				else if(((spd.UsageMask & RegisterComponentMaskFlags.ComponentX) != 0))
				{
					return	Format.R32_Float;
				}
			}
			else if(spd.ComponentType == RegisterComponentType.SInt32)
			{
				if(((spd.UsageMask & RegisterComponentMaskFlags.ComponentW) != 0))
				{
					return	Format.R32G32B32A32_SInt;
				}
				else if(((spd.UsageMask & RegisterComponentMaskFlags.ComponentZ) != 0))
				{
					return	Format.R32G32B32_SInt;
				}
				else if(((spd.UsageMask & RegisterComponentMaskFlags.ComponentY) != 0))
				{
					return	Format.R32G32_SInt;
				}
				else if(((spd.UsageMask & RegisterComponentMaskFlags.ComponentX) != 0))
				{
					return	Format.R32_SInt;
				}
			}
			else if(spd.ComponentType == RegisterComponentType.UInt32)
			{
				if(((spd.UsageMask & RegisterComponentMaskFlags.ComponentW) != 0))
				{
					return	Format.R32G32B32A32_UInt;
				}
				else if(((spd.UsageMask & RegisterComponentMaskFlags.ComponentZ) != 0))
				{
					return	Format.R32G32B32_UInt;
				}
				else if(((spd.UsageMask & RegisterComponentMaskFlags.ComponentY) != 0))
				{
					return	Format.R32G32_UInt;
				}
				else if(((spd.UsageMask & RegisterComponentMaskFlags.ComponentX) != 0))
				{
					return	Format.R32_UInt;
				}
			}
			Debug.Assert(false);
			return	Format.Unknown;
		}


		void CalcLayouts()
		{
			//clear existing
			foreach(InputLayout il in mLayouts)
			{
				il.Dispose();
			}
			mLayouts.Clear();

			foreach(EffectPass ep in mPasses)
			{
				ep.Dispose();
			}
			mPasses.Clear();

			if(mEffect == null || mTechnique == null)
			{
				return;
			}

			Device	dev	=mEffect.Device;

			mNumPasses	=0;

			for(int i=0;i < mTechnique.Description.PassCount;i++)
			{
				EffectPass	ep	=mTechnique.GetPassByIndex(i);
				if(ep == null)
				{
					break;
				}

				//use to investigate layout of a particlar technique
//				if(mTechnique.Description.Name == "Particle")
//				{
//					int	j=0;
//					j++;
//				}

				Debug.Assert(ep.IsValid);

				if(!ep.IsValid)
				{
					//stick a null in the list for invalid passes
					mPasses.Add(null);
					continue;
				}

				mPasses.Add(ep);

				mNumPasses++;

				EffectShaderDescription	sd	=ep.VertexShaderDescription.
					Variable.GetShaderDescription(ep.VertexShaderDescription.Index);

				ShaderReflection	sr	=new ShaderReflection(sd.Bytecode);

				int	numParams	=sr.Description.InputParameters;

				InputElement	[]elements	=new InputElement[numParams];

				for(int j=0;j < numParams;j++)
				{
					ShaderParameterDescription	spd	=sr.GetInputParameterDescription(j);

					elements[j]	=new InputElement(spd.SemanticName, spd.SemanticIndex,
						GetFormatFromParamDesc(spd),
						InputElement.AppendAligned, 0);
				}
				InputLayout	il	=new InputLayout(dev,
					ep.Description.Signature, elements);
				mLayouts.Add(il);

				il.DebugName	=mTechnique.Description.Name + i;

				sr.Dispose();
			}
			dev.Dispose();
		}


		internal void ResetParameterVisibility()
		{
			mIgnored.Clear();
			mHidden.Clear();
		}
	}
}
