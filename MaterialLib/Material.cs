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
	public class Material
	{
		public struct TriLight
		{
			internal Vector4	mColor0, mColor1, mColor2;
		}

		string	mShaderName;	//name of the shader
		string	mName;			//name of the overall material
		string	mTechnique;		//technique to use with this material

		//emmisive color for radiosity
		Color	mEmissiveColor	=Color.White;	//default white

		//state objects, don't modify these directly
		BlendState			mBlendState			=BlendState.Opaque;
		DepthStencilState	mDepthStencilState	=DepthStencilState.Default;
		RasterizerState		mRasterizeState		=RasterizerState.CullCounterClockwise;

		//parameters for the chosen shader
		List<ShaderParameters>			mParameters		=new List<ShaderParameters>();

		//for datagrid and editing, but also
		//access to the state pool
		GUIStates	mGUIStates;

		//list of parameters to ignore
		//these will be updated by code at runtime
		List<string>	mIgnoreParameters	=new List<string>();


		//tool side constructor for editing
		internal Material(StateBlockPool sbp)
		{
			mGUIStates	=new GUIStates(this, sbp);
		}


		public string Name
		{
			get { return mName; }
			set { mName = Misc.AssignValue(value); }
		}
		public string ShaderName
		{
			get { return mShaderName; }
			set { mShaderName = Misc.AssignValue(value); }
		}
		public string Technique
		{
			get { return mTechnique; }
			set { mTechnique = Misc.AssignValue(value); }
		}
#if !XBOX
		public BindingList<ShaderParameters> Parameters
		{
			get { return mGUIStates.Parameters; }
			set { mGUIStates.Parameters = value; }
		}
#endif
		public Color Emissive
		{
			get { return mEmissiveColor; }
			set { mEmissiveColor = value; }
		}
		public BlendState BlendState
		{
			get { return mBlendState; }
#if !XBOX
			set { mGUIStates.SetBlendState(value); }
#endif
		}
		public DepthStencilState DepthState
		{
			get { return mDepthStencilState; }
#if !XBOX
			set { mGUIStates.SetDepthState(value); }
#endif
		}
		public RasterizerState RasterState
		{
			get { return mRasterizeState; }
#if !XBOX
			set { mGUIStates.SetRasterState(value); }
#endif
		}


		public void Write(BinaryWriter bw)
		{
			bw.Write(mName);
			bw.Write(mShaderName);
			bw.Write(mTechnique);
			bw.Write(mEmissiveColor.PackedValue);

			//blend state
			bw.Write((UInt32)mBlendState.AlphaBlendFunction);
			bw.Write((UInt32)mBlendState.AlphaDestinationBlend);
			bw.Write((UInt32)mBlendState.AlphaSourceBlend);
			bw.Write(mBlendState.BlendFactor.PackedValue);
			bw.Write((UInt32)mBlendState.ColorBlendFunction);
			bw.Write((UInt32)mBlendState.ColorDestinationBlend);
			bw.Write((UInt32)mBlendState.ColorSourceBlend);
			bw.Write((UInt32)mBlendState.ColorWriteChannels);
			bw.Write((UInt32)mBlendState.ColorWriteChannels1);
			bw.Write((UInt32)mBlendState.ColorWriteChannels2);
			bw.Write((UInt32)mBlendState.ColorWriteChannels3);
			bw.Write(mBlendState.MultiSampleMask);

			//depth state
			bw.Write(mDepthStencilState.DepthBufferEnable);
			bw.Write((UInt32)mDepthStencilState.DepthBufferFunction);
			bw.Write(mDepthStencilState.DepthBufferWriteEnable);

			//cullmode
			bw.Write((UInt32)mRasterizeState.CullMode);

			bw.Write(mParameters.Count);
			foreach(ShaderParameters sp in mParameters)
			{
				sp.Write(bw);
			}

			bw.Write(mIgnoreParameters.Count);
			foreach(string ig in mIgnoreParameters)
			{
				bw.Write(ig);
			}
		}


		public void Read(BinaryReader br)
		{
			mName			=br.ReadString();
			mShaderName		=br.ReadString();
			mTechnique		=br.ReadString();

			UInt32	emCol	=br.ReadUInt32();

			//what a pain in the ass
			mEmissiveColor	=new Color(emCol & 0xff,
				emCol & 0xff00 >> 8,
				emCol & 0xff0000 >> 16,
				emCol & 0xff000000 >> 24);

			BlendState	bs	=new BlendState();
			bs.AlphaBlendFunction		=(BlendFunction)br.ReadUInt32();
			bs.AlphaDestinationBlend	=(Blend)br.ReadUInt32();
			bs.AlphaSourceBlend			=(Blend)br.ReadUInt32();

			emCol	=br.ReadUInt32();
			bs.BlendFactor	=new Color(emCol & 0xff,
				emCol & 0xff00 >> 8,
				emCol & 0xff0000 >> 16,
				emCol & 0xff000000 >> 24);

			bs.ColorBlendFunction		=(BlendFunction)br.ReadUInt32();
			bs.ColorDestinationBlend	=(Blend)br.ReadUInt32();
			bs.ColorSourceBlend			=(Blend)br.ReadUInt32();
			bs.ColorWriteChannels		=(ColorWriteChannels)br.ReadUInt32();
			bs.ColorWriteChannels1		=(ColorWriteChannels)br.ReadUInt32();
			bs.ColorWriteChannels2		=(ColorWriteChannels)br.ReadUInt32();
			bs.ColorWriteChannels3		=(ColorWriteChannels)br.ReadUInt32();
			bs.MultiSampleMask			=br.ReadInt32();
			mGUIStates.SetBlendState(bs);

			DepthStencilState	dss	=new DepthStencilState();
			dss.DepthBufferEnable		=br.ReadBoolean();
			dss.DepthBufferFunction		=(CompareFunction)br.ReadUInt32();
			dss.DepthBufferWriteEnable	=br.ReadBoolean();
			mGUIStates.SetDepthState(dss);

			RasterizerState	rs	=new RasterizerState();
			rs.CullMode	=(CullMode)br.ReadUInt32();
			mGUIStates.SetRasterState(rs);

			mParameters.Clear();
			int	numParameters	=br.ReadInt32();
			for(int i=0;i < numParameters;i++)
			{
				ShaderParameters	sp	=new ShaderParameters();
				sp.Read(br);

				mParameters.Add(sp);
			}

			mIgnoreParameters.Clear();
			int	numIgnores	=br.ReadInt32();
			for(int i=0;i < numIgnores;i++)
			{
				string	ig	=br.ReadString();
				mIgnoreParameters.Add(ig);
			}
			UpdateGUIParams();
		}


		internal bool GetTriLight(out TriLight tri)
		{
			bool	bZero	=false;
			bool	bOne	=false;
			bool	bTwo	=false;

			tri.mColor0	=Vector4.Zero;
			tri.mColor1	=Vector4.Zero;
			tri.mColor2	=Vector4.Zero;

			foreach(ShaderParameters sp in mParameters)
			{
				if(sp.Class != EffectParameterClass.Vector)
				{
					continue;
				}

				if(sp.Name == "mLightColor0")
				{
					tri.mColor0	=Misc.StringToVector4(sp.Value);
					bZero		=true;
				}
				else if(sp.Name == "mLightColor1")
				{
					tri.mColor1	=Misc.StringToVector4(sp.Value);
					bOne		=true;
				}
				else if(sp.Name == "mLightColor2")
				{
					tri.mColor2	=Misc.StringToVector4(sp.Value);
					bTwo		=true;
				}
			}
			return	(bZero && bOne && bTwo);
		}


		internal void SetTriLightValues(TriLight tri, Vector3 lightDir)
		{
			foreach(ShaderParameters sp in mParameters)
			{
				if(sp.Class != EffectParameterClass.Vector)
				{
					continue;
				}

				if(sp.Name == "mLightColor0")
				{
					sp.Value	=Misc.VectorToString(tri.mColor0);
				}
				else if(sp.Name == "mLightColor1")
				{
					sp.Value	=Misc.VectorToString(tri.mColor1);
				}
				else if(sp.Name == "mLightColor2")
				{
					sp.Value	=Misc.VectorToString(tri.mColor2);
				}
				else if(sp.Name == "mLightDirection")
				{
					sp.Value	=Misc.VectorToString(lightDir);
				}
			}
		}


		internal void StripTextureExtensions()
		{
			foreach(ShaderParameters sp in mParameters)
			{
				if(sp.Type == EffectParameterType.Texture)
				{
					if(sp.Value != null && sp.Value != "")
					{
						sp.Value	=FileUtil.StripExtension(sp.Value);
					}
				}
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


		public List<string>	GetReferencedCubeTextures()
		{
			List<string>	ret	=new List<string>();

			foreach(ShaderParameters sp in mParameters)
			{
				if(sp.Type == EffectParameterType.TextureCube)
				{
					if(sp.Value != null && sp.Value != "")
					{
						ret.Add(sp.Value);
					}
				}
			}
			return	ret;
		}


		public void SetParameter(string paramName, string value)
		{
			foreach(ShaderParameters sp in mParameters)
			{
				if(sp.Name == paramName)
				{
					sp.Value	=value;
				}
			}
		}


		public void SetTextureParameterToCube(string name)
		{
			foreach(ShaderParameters sp in mParameters)
			{
				if(sp.Name == name)
				{
					if(sp.Type == EffectParameterType.Texture)
					{
						sp.Type	=EffectParameterType.TextureCube;
					}
					return;
				}
			}
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
				UpdateGUIParams();
			}
		}


		public void IgnoreParameter(string paramName)
		{
			if(!mIgnoreParameters.Contains(paramName))
			{
				mIgnoreParameters.Add(paramName);
			}
			UpdateGUIParams();
		}


		public void StopIgnoringParameter(string paramName)
		{
			if(mIgnoreParameters.Contains(paramName))
			{
				mIgnoreParameters.Remove(paramName);

				UpdateGUIParams();
			}
		}


		public void ApplyRenderStates(GraphicsDevice g)
		{
			g.BlendState		=mBlendState;
			g.DepthStencilState	=mDepthStencilState;
			g.RasterizerState	=mRasterizeState;
		}


		public void UpdateShaderParameters(Effect fx)
		{
			List<ShaderParameters>	parms	=new List<ShaderParameters>();

			foreach(EffectParameter ep in fx.Parameters)
			{
				//skip matrices
				if(ep.ParameterClass == EffectParameterClass.Matrix)
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
					case EffectParameterClass.Matrix:
						sp.Value	=Convert.ToString(ep.GetValueMatrix());
						break;

					case EffectParameterClass.Vector:
						if(ep.ColumnCount == 2)
						{
							Vector2	vec	=ep.GetValueVector2();

							sp.Value	=Misc.VectorToString(vec);
						}
						else if(ep.ColumnCount == 3)
						{
							Vector3	vec	=ep.GetValueVector3();
							sp.Value	=Misc.VectorToString(vec);
						}
						else
						{
							Vector4	vec	=ep.GetValueVector4();
							sp.Value	=Misc.VectorToString(vec);
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
					UpdateGUIParams();
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
			if(gank.Count > 0)
			{
				UpdateGUIParams();
			}
		}


		void UpdateGUIParams()
		{
#if !XBOX
			mGUIStates.Parameters.Clear();

			foreach(ShaderParameters sp in mParameters)
			{
				if(!mIgnoreParameters.Contains(sp.Name))
				{
					mGUIStates.Parameters.Add(sp);
				}
			}
#endif
		}


		internal List<ShaderParameters> GetRealShaderParameters()
		{
			return	mParameters;
		}


		internal void SetEmissive(byte red, byte green, byte blue)
		{
			mEmissiveColor	=new Color(red, green, blue, 255);
		}


		//should only be set from the state pool
		internal void SetBlendState(BlendState bs)
		{
			mBlendState	=bs;
		}


		//should only be set from the state pool
		internal void SetDepthState(DepthStencilState ds)
		{
			mDepthStencilState	=ds;
		}


		//should only be set from the state pool
		internal void SetRasterState(RasterizerState rs)
		{
			mRasterizeState	=rs;
		}


		//expose gui stuff to gui
#if !XBOX
		public GUIStates GetGUIStates()
		{
			return	mGUIStates;
		}
#endif
	}
}
