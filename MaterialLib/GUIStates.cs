using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace MaterialLib
{
	//keeps track of state block changes
	//by editors
	public class GUIStates
	{
#if !XBOX
		//reference to the material that owns us
		Material	mMat;

		//reference to the state block pool
		StateBlockPool	mSBPool;

		//tool side shader parameters
		BindingList<ShaderParameters>	mGUIParameters	=new BindingList<ShaderParameters>();

		//blend state data
		BlendFunction	mAlphaBlendFunc;
		Blend			mAlphaDestBlend;
		Blend			mAlphaSrcBlend;
		Color			mBlendFactor;
		BlendFunction	mColorBlendFunc;
		Blend			mColorDestBlend;
		Blend			mColorSrcBlend;

		//depth stencil data
		bool			mbDepthEnable;
		CompareFunction	mDepthFunc;
		bool			mbDepthWriteEnable;

		//raster
		CullMode	mCullMode;


		internal GUIStates(Material parent, StateBlockPool sbp)
		{
			mMat	=parent;
			mSBPool	=sbp;

			//set up initial values from the parent states
			mAlphaBlendFunc		=mMat.BlendState.AlphaBlendFunction;
			mAlphaDestBlend		=mMat.BlendState.AlphaDestinationBlend;
			mAlphaSrcBlend		=mMat.BlendState.AlphaSourceBlend;
			mBlendFactor		=mMat.BlendState.BlendFactor;
			mColorBlendFunc		=mMat.BlendState.ColorBlendFunction;
			mColorDestBlend		=mMat.BlendState.ColorDestinationBlend;
			mColorSrcBlend		=mMat.BlendState.ColorSourceBlend;
			mbDepthEnable		=mMat.DepthState.DepthBufferEnable;
			mDepthFunc			=mMat.DepthState.DepthBufferFunction;
			mbDepthWriteEnable	=mMat.DepthState.DepthBufferWriteEnable;
		}


		public string Name
		{
			get { return mMat.Name; }
			set { mMat.Name = value; }
		}
		public string ShaderName
		{
			get { return mMat.ShaderName; }
			set { mMat.ShaderName = value; }
		}
		public string Technique
		{
			get { return mMat.Technique; }
			set { mMat.Technique = value; }
		}
#if !XBOX
		public BindingList<ShaderParameters> Parameters
		{
			get { return mGUIParameters; }
			set { mGUIParameters = value; }
		}
#endif
		public Color Emissive
		{
			get { return mMat.Emissive; }
			set { mMat.Emissive = value; }
		}

		//blend state crap
		public BlendFunction AlphaBlendFunc
		{
			get { return mAlphaBlendFunc; }
			set { mAlphaBlendFunc = value; }
		}
		public Blend AlphaDestBlend
		{
			get { return mAlphaDestBlend; }
			set { mAlphaDestBlend = value; }
		}
		public Blend AlphaSourceBlend
		{
			get { return mAlphaSrcBlend; }
			set { mAlphaSrcBlend = value; }
		}
		public Color BlendFactor
		{
			get { return mBlendFactor; }
			set { mBlendFactor = value; }
		}
		public BlendFunction ColorBlendFunc
		{
			get { return mColorBlendFunc; }
			set { mColorBlendFunc = value; }
		}
		public Blend ColorDestBlend
		{
			get { return mColorDestBlend; }
			set { mColorDestBlend = value; }
		}
		public Blend ColorSourceBlend
		{
			get { return mColorSrcBlend; }
			set { mColorSrcBlend = value; }
		}
		/*
		public ColorWriteChannels ColorWriteChans0
		{
			get { return mBlendState.ColorWriteChannels; }
			set { mBlendState.ColorWriteChannels = value; }
		}
		public ColorWriteChannels ColorWriteChans1
		{
			get { return mBlendState.ColorWriteChannels1; }
			set { mBlendState.ColorWriteChannels1 = value; }
		}
		public ColorWriteChannels ColorWriteChans2
		{
			get { return mBlendState.ColorWriteChannels2; }
			set { mBlendState.ColorWriteChannels2 = value; }
		}
		public ColorWriteChannels ColorWriteChans3
		{
			get { return mBlendState.ColorWriteChannels3; }
			set { mBlendState.ColorWriteChannels3 = value; }
		}
		public int MSampMask
		{
			get { return mBlendState.MultiSampleMask; }
			set { mBlendState.MultiSampleMask = value; }
		}*/

		//depthstencil stuff
		//leaving out most of it for now
		public bool DepthEnable
		{
			get { return mbDepthEnable; }
			set { mbDepthEnable = value; }
		}
		public CompareFunction DepthFunction
		{
			get { return mDepthFunc; }
			set { mDepthFunc = value; }
		}
		public bool DepthWriteEnable
		{
			get { return mbDepthWriteEnable; }
			set { mbDepthWriteEnable = value; }
		}

		//raster stuff
		public CullMode CullMode
		{
			get { return mCullMode; }
			set { mCullMode = value; }
		}


		//for setting the state directly
		internal void SetBlendState(BlendState bs)
		{
			mMat.SetBlendState(
				mSBPool.FindBlendState(bs.AlphaBlendFunction, bs.AlphaDestinationBlend,
				bs.AlphaSourceBlend, bs.BlendFactor, bs.ColorBlendFunction,
				bs.ColorDestinationBlend, bs.ColorSourceBlend));
		}


		//for setting the state directly
		internal void SetDepthState(DepthStencilState ds)
		{
			mMat.SetDepthState(
				mSBPool.FindDepthStencilState(ds.DepthBufferEnable,
				ds.DepthBufferFunction, ds.DepthBufferWriteEnable));
		}


		//for setting the state directly
		internal void SetRasterState(RasterizerState rs)
		{
			mMat.SetRasterState(
				mSBPool.FindRasterizerState(rs.CullMode));
		}
#endif
	}
}
