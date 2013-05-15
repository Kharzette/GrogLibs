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
		//reference to the material that owns us
		Material	mMat;

		//reference to the state block pool
		StateBlockPool	mSBPool;

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
			SetBlendValues(mMat.BlendState);
			SetDepthValues(mMat.DepthState);
			SetRasterValues(mMat.RasterState);
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
		public Color Emissive
		{
			get { return mMat.Emissive; }
			set { mMat.Emissive = value; }
		}
		public BindingList<ShaderParameters> ShaderParameters
		{
			get { return mMat.ShaderParameters; }
		}

		//blend state crap
		public BlendFunction AlphaBlendFunc
		{
			get { return mAlphaBlendFunc; }
			set { mAlphaBlendFunc = value; UpdateMaterialBlendState(); }
		}
		public Blend AlphaDestBlend
		{
			get { return mAlphaDestBlend; }
			set { mAlphaDestBlend = value; UpdateMaterialBlendState(); }
		}
		public Blend AlphaSrcBlend
		{
			get { return mAlphaSrcBlend; }
			set { mAlphaSrcBlend = value; UpdateMaterialBlendState(); }
		}
		public Color BlendFactor
		{
			get { return mBlendFactor; }
			set { mBlendFactor = value; UpdateMaterialBlendState(); }
		}
		public BlendFunction ColorBlendFunc
		{
			get { return mColorBlendFunc; }
			set { mColorBlendFunc = value; UpdateMaterialBlendState(); }
		}
		public Blend ColorDestBlend
		{
			get { return mColorDestBlend; }
			set { mColorDestBlend = value; UpdateMaterialBlendState(); }
		}
		public Blend ColorSrcBlend
		{
			get { return mColorSrcBlend; }
			set { mColorSrcBlend = value; UpdateMaterialBlendState(); }
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
			set { mbDepthEnable = value; UpdateMaterialDepthState(); }
		}
		public CompareFunction DepthFunc
		{
			get { return mDepthFunc; }
			set { mDepthFunc = value; UpdateMaterialDepthState(); }
		}
		public bool DepthWriteEnable
		{
			get { return mbDepthWriteEnable; }
			set { mbDepthWriteEnable = value; UpdateMaterialDepthState(); }
		}

		//raster stuff
		public CullMode CullMode
		{
			get { return mCullMode; }
			set { mCullMode = value; UpdateMaterialRasterState(); }
		}

		public Material GetParentMaterial()
		{
			return	mMat;
		}


		//for setting the state directly
		internal void SetBlendState(BlendState bs)
		{
			BlendState	fbs	=mSBPool.FindBlendState(bs.AlphaBlendFunction, bs.AlphaDestinationBlend,
				bs.AlphaSourceBlend, bs.BlendFactor, bs.ColorBlendFunction,
				bs.ColorDestinationBlend, bs.ColorSourceBlend);

			SetBlendValues(fbs);

			mMat.SetBlendState(fbs);
		}


		//for setting the state directly
		internal void SetDepthState(DepthStencilState ds)
		{
			DepthStencilState	fds	=mSBPool.FindDepthStencilState(
				ds.DepthBufferEnable, ds.DepthBufferFunction,
				ds.DepthBufferWriteEnable);

			SetDepthValues(fds);

			mMat.SetDepthState(fds);
		}


		//for setting the state directly
		internal void SetRasterState(RasterizerState rs)
		{
			RasterizerState	frs	=mSBPool.FindRasterizerState(rs.CullMode);

			SetRasterValues(frs);

			mMat.SetRasterState(frs);
		}


		void SetBlendValues(BlendState bs)
		{
			mAlphaBlendFunc		=bs.AlphaBlendFunction;
			mAlphaDestBlend		=bs.AlphaDestinationBlend;
			mAlphaSrcBlend		=bs.AlphaSourceBlend;
			mBlendFactor		=bs.BlendFactor;
			mColorBlendFunc		=bs.ColorBlendFunction;
			mColorDestBlend		=bs.ColorDestinationBlend;
			mColorSrcBlend		=bs.ColorSourceBlend;
		}


		void SetDepthValues(DepthStencilState dss)
		{
			mbDepthEnable		=dss.DepthBufferEnable;
			mDepthFunc			=dss.DepthBufferFunction;
			mbDepthWriteEnable	=dss.DepthBufferWriteEnable;
		}


		void UpdateMaterialBlendState()
		{
			BlendState	bs	=mSBPool.FindBlendState(mAlphaBlendFunc,
				mAlphaDestBlend, mAlphaSrcBlend, mBlendFactor,
				mColorBlendFunc, mColorDestBlend, mColorSrcBlend);

			mMat.SetBlendState(bs);
		}


		void UpdateMaterialDepthState()
		{
			DepthStencilState	dss	=mSBPool.FindDepthStencilState(
				mbDepthEnable, mDepthFunc, mbDepthWriteEnable);

			mMat.SetDepthState(dss);
		}


		void UpdateMaterialRasterState()
		{
			RasterizerState	rs	=mSBPool.FindRasterizerState(mCullMode);

			mMat.SetRasterState(rs);
		}


		void SetRasterValues(RasterizerState rs)
		{
			mCullMode	=rs.CullMode;
		}
	}
}
