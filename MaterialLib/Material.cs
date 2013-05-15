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
		string	mName;			//name of the overall material
		string	mShaderName;	//name of the shader
		string	mTechnique;		//technique to use with this material

		//emmisive color for radiosity
		Color	mEmissiveColor	=Color.White;	//default white

		//state objects, don't modify these directly
		BlendState			mBlendState			=BlendState.Opaque;
		DepthStencilState	mDepthStencilState	=DepthStencilState.Default;
		RasterizerState		mRasterizeState		=RasterizerState.CullCounterClockwise;

		//shader parameters
		ParameterKeeper	mPKeeper;

		//for datagrid and editing, but also
		//access to the state pool
		GUIStates	mGUIStates;

		//tool side constructor for editing
		internal Material(StateBlockPool sbp,
			Dictionary<string, Texture2D> texs,
			Dictionary<string, TextureCube> cubes)
		{
			mGUIStates	=new GUIStates(this, sbp);

			mPKeeper	=new ParameterKeeper(texs, cubes);
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
		public BindingList<ShaderParameters> ShaderParameters
		{
			get { return mPKeeper.GetParametersForGUI(); }
		}


		#region IO
		internal void Write(BinaryWriter bw)
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

			mPKeeper.Write(bw);
		}


		internal void Read(BinaryReader br)
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

			mPKeeper.Read(br);
		}
		#endregion


		public void AddParameter(string name, EffectParameterClass epc,
			EffectParameterType ept, int count, object val)
		{
			mPKeeper.AddParameter(name, epc, ept, count, val);
		}


		public void SetParameter(string name, object value)
		{
			mPKeeper.SetParameter(name, value);
		}


		public void HideShaderParameters(List<ShaderParameters> toHide)
		{
			mPKeeper.Hide(toHide);
		}


		public void UpdateShaderParameters(Effect fx)
		{
			mPKeeper.UpdateShaderParameters(fx);
		}


		public void ApplyRenderStates(GraphicsDevice g)
		{
			g.BlendState		=mBlendState;
			g.DepthStencilState	=mDepthStencilState;
			g.RasterizerState	=mRasterizeState;
		}


		public void SetTextureParameterToCube(string name)
		{
			mPKeeper.SetTextureParameterToCube(name);
		}


		internal void GetTexturesInUse(List<string> tex)
		{
			mPKeeper.GetTexturesInUse(tex);
		}


		internal void GetTextureCubesInUse(List<string> tex)
		{
			mPKeeper.GetTextureCubesInUse(tex);
		}


		public void ApplyShaderParameters(Effect fx)
		{
			//set technique
			if(mTechnique != "")
			{
				if(fx.Techniques[mTechnique] == null)
				{
					return;
				}
				fx.CurrentTechnique	=fx.Techniques[mTechnique];
			}

			mPKeeper.ApplyShaderParameters(fx);
		}


		internal object GetParameterValue(string name)
		{
			return	mPKeeper.GetParameterValue(name);
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


		internal void UpdateTexPointers(Dictionary<string, Texture2D> maps,
			Dictionary<string, TextureCube> cubes)
		{
			mPKeeper.UpdateTexPointers(maps, cubes);
		}


		internal void SetTriLightValues(Vector4 colorVal, Vector3 lightDir)
		{
			mPKeeper.SetTriLightValues(colorVal, lightDir);
		}
	}
}
