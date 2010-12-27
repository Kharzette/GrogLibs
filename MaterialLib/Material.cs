using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Storage;


namespace MaterialLib
{
	public class Material
	{
		string	mShaderName;	//name of the shader
		string	mName;			//name of the overall material
		string	mTechnique;		//technique to use with this material
		bool	mbAlpha;		//alpha blending

		//renderstate flags
		Blend			mSourceBlend;
		Blend			mDestBlend;
		BlendFunction	mBlendFunction;
		bool			mbDepthWrite;
		bool			mbAlphaTest;
		CullMode		mCullMode;
		CompareFunction	mZFunction;


		//parameters for the chosen shader
		BindingList<ShaderParameters>	mParameters	=new BindingList<ShaderParameters>();


		public string Name
		{
			get { return mName; }
			set { mName = value; }
		}
		public string ShaderName
		{
			get { return mShaderName; }
			set { mShaderName = value; }
		}
		public string Technique
		{
			get { return mTechnique; }
			set { mTechnique = value; }
		}
		public BindingList<ShaderParameters> Parameters
		{
			get { return mParameters; }
			set { mParameters = value; }
		}
		public bool Alpha
		{
			get { return mbAlpha; }
			set { mbAlpha = value; }
		}
		public Blend SourceBlend
		{
			get { return mSourceBlend; }
			set { mSourceBlend = value; }
		}
		public Blend DestBlend
		{
			get { return mDestBlend; }
			set { mDestBlend = value; }
		}
		public BlendFunction BlendFunction
		{
			get { return mBlendFunction; }
			set { mBlendFunction = value; }
		}
		public bool DepthWrite
		{
			get { return mbDepthWrite; }
			set { mbDepthWrite = value; }
		}
		public bool AlphaTest
		{
			get { return mbAlphaTest; }
			set { mbAlphaTest = value; }
		}
		public CullMode CullMode
		{
			get { return mCullMode; }
			set { mCullMode = value; }
		}
		public CompareFunction ZFunction
		{
			get { return mZFunction; }
			set { mZFunction = value; }
		}


		public void Write(BinaryWriter bw)
		{
			bw.Write(mName);
			bw.Write(mShaderName);
			bw.Write(mTechnique);
			bw.Write(mbAlpha);
			bw.Write((UInt32)mSourceBlend);
			bw.Write((UInt32)mDestBlend);
			bw.Write((UInt32)mBlendFunction);
			bw.Write(mbDepthWrite);
			bw.Write(mbAlphaTest);
			bw.Write((UInt32)mCullMode);
			bw.Write((UInt32)mZFunction);

			bw.Write(mParameters.Count);
			foreach(ShaderParameters sp in mParameters)
			{
				sp.Write(bw);
			}
		}


		public void Read(BinaryReader br)
		{
			mName			=br.ReadString();
			mShaderName		=br.ReadString();
			mTechnique		=br.ReadString();
			mbAlpha			=br.ReadBoolean();
			mSourceBlend	=(Blend)br.ReadUInt32();
			mDestBlend		=(Blend)br.ReadUInt32();
			mBlendFunction	=(BlendFunction)br.ReadUInt32();
			mbDepthWrite	=br.ReadBoolean();
			mbAlphaTest		=br.ReadBoolean();
			mCullMode		=(CullMode)br.ReadUInt32();
			mZFunction		=(CompareFunction)br.ReadUInt32();

			int	numParameters	=br.ReadInt32();
			for(int i=0;i < numParameters;i++)
			{
				ShaderParameters	sp	=new ShaderParameters();
				sp.Read(br);

				mParameters.Add(sp);
			}
		}


		public List<string>	GetReferencedTextures()
		{
			List<string>	ret	=new List<string>();

			foreach(ShaderParameters sp in mParameters)
			{
				if(sp.Type == EffectParameterType.Texture)
				{
					if(sp.Value != null && sp.Value != "")
					{
						ret.Add(sp.Value);
					}
				}
			}
			return	ret;
		}


		public string GetParameterValue(string name)
		{
			foreach(ShaderParameters sp in mParameters)
			{
				if(sp.Name == name)
				{
					return	sp.Value;
				}
			}
			return	"";
		}


		public void AddParameter(string name, EffectParameterClass epc,
								EffectParameterType ept, string value)
		{
			ShaderParameters	parm	=null;

			//see if the parameter already exists
			foreach(ShaderParameters sp in mParameters)
			{
				if(sp.Name == name)
				{
					parm	=sp;
					break;
				}
			}

			bool	bNew	=false;

			if(parm == null)
			{
				bNew	=true;
				parm	=new ShaderParameters();
			}

			parm.Name	=name;
			parm.Class	=epc;
			parm.Type	=ept;
			parm.Value	=value;

			if(bNew)
			{
				mParameters.Add(parm);
			}
		}


		public void ApplyRenderStates(GraphicsDevice g)
		{
			g.RenderState.AlphaBlendEnable			=Alpha;
			g.RenderState.AlphaTestEnable			=AlphaTest;
			g.RenderState.BlendFunction				=BlendFunction;
			g.RenderState.SourceBlend				=SourceBlend;
			g.RenderState.DestinationBlend			=DestBlend;
			g.RenderState.DepthBufferWriteEnable	=DepthWrite;
			g.RenderState.CullMode					=CullMode;
			g.RenderState.DepthBufferFunction		=ZFunction;
		}


		public void UpdateShaderParameters(Effect fx)
		{
			List<ShaderParameters>	parms	=new List<ShaderParameters>();

			foreach(EffectParameter ep in fx.Parameters)
			{
				//skip matrices
				if(ep.ParameterClass == EffectParameterClass.MatrixColumns
					|| ep.ParameterClass == EffectParameterClass.MatrixRows)
				{
					continue;
				}

				//skip samplers
				if(ep.ParameterType == EffectParameterType.Sampler)
				{
					continue;
				}

				//skip stuff with lots of elements
				//such as lists of bones
				if(ep.Elements.Count > 0)
				{
					continue;
				}

				ShaderParameters	sp	=new ShaderParameters();

				sp.Name		=ep.Name;
				sp.Class	=ep.ParameterClass;
				sp.Type		=ep.ParameterType;

				switch(sp.Class)
				{
					case EffectParameterClass.MatrixColumns:
						sp.Value	=Convert.ToString(ep.GetValueMatrix());
						break;

					case EffectParameterClass.MatrixRows:
						sp.Value	=Convert.ToString(ep.GetValueMatrix());
						break;

					case EffectParameterClass.Vector:
						if(ep.ColumnCount == 2)
						{
							Vector2	vec	=ep.GetValueVector2();
							sp.Value	=Convert.ToString(vec.X)
								+ " " + Convert.ToString(vec.Y);
						}
						else if(ep.ColumnCount == 3)
						{
							Vector3	vec	=ep.GetValueVector3();
							sp.Value	=Convert.ToString(vec.X)
								+ " " + Convert.ToString(vec.Y)
								+ " " + Convert.ToString(vec.Z);
						}
						else
						{
							Vector4	vec	=ep.GetValueVector4();
							sp.Value	=Convert.ToString(vec.X)
								+ " " + Convert.ToString(vec.Y)
								+ " " + Convert.ToString(vec.Z)
								+ " " + Convert.ToString(vec.W);
						}
						break;
				}
				parms.Add(sp);
			}

			//merge results
			//add any new parameters
			foreach(ShaderParameters newSp in parms)
			{
				bool	bFound	=false;
				foreach(ShaderParameters sp in mParameters)
				{
					if(sp.Name == newSp.Name)
					{
						bFound	=true;
					}
				}

				if(!bFound)
				{
					mParameters.Add(newSp);
				}
			}

			//gank any parameters that no longer exist
			//within the shader
			List<ShaderParameters>	gank	=new List<ShaderParameters>();
			foreach(ShaderParameters sp in mParameters)
			{
				bool	bFound	=false;
				{
					foreach(ShaderParameters newSp in parms)
					if(sp.Name == newSp.Name)
					{
						bFound	=true;
						break;
					}
				}

				if(!bFound)
				{
					gank.Add(sp);
				}
			}

			//gankery
			foreach(ShaderParameters sp in gank)
			{
				mParameters.Remove(sp);
			}
		}
	}
}
