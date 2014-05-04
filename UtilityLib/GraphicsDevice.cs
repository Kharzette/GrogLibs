using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UtilityLib;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;

using Device	=SharpDX.Direct3D11.Device;
using Cursor	=System.Windows.Forms.Cursor;


namespace UtilityLib
{
	public class GraphicsDevice
	{
		RenderForm	mRForm;

		Device			mGD;
		DeviceContext	mDC;

		SwapChain	mSChain;

		Texture2D			mBackBuffer, mDepthBuffer;
		RenderTargetView	mBBView;
		DepthStencilView	mDSView;

		GameCamera	mGCam;

		bool	mbResized;

		//keep track of mouse pos during mouse look
		System.Drawing.Point		mStoredMousePos	=System.Drawing.Point.Empty;
		System.Drawing.Rectangle	mStoredClipRect	=Cursor.Clip;


		public Device GD
		{
			get { return mGD; }
		}

		public DeviceContext DC
		{
			get { return mDC; }
		}

		public RenderForm RendForm
		{
			get { return mRForm; }
		}

		public GameCamera GCam
		{
			get { return mGCam; }
		}


		public GraphicsDevice(string formTitle, FeatureLevel flevel)
		{
			mRForm	=new RenderForm(formTitle);

			SwapChainDescription	scDesc	=new SwapChainDescription();

			scDesc.BufferCount			=1;
			scDesc.Flags				=SwapChainFlags.None;
			scDesc.IsWindowed			=true;
			scDesc.ModeDescription		=new ModeDescription(mRForm.ClientSize.Width, mRForm.ClientSize.Height, new Rational(60, 1), Format.R8G8B8A8_UNorm);
			scDesc.OutputHandle			=mRForm.Handle;
			scDesc.SampleDescription	=new SampleDescription(1, 0);
			scDesc.SwapEffect			=SwapEffect.Discard;
			scDesc.Usage				=Usage.RenderTargetOutput;
			
			SharpDX.DXGI.Factory	fact	=new Factory();

			Adapter	adpt	=fact.GetAdapter(0);

			FeatureLevel	[]features	=new FeatureLevel[1];

			features[0]	=new FeatureLevel();

			features[0]	=flevel;

			Device.CreateWithSwapChain(adpt, DeviceCreationFlags.Debug, features,
				scDesc, out mGD, out mSChain);

			adpt.Dispose();

			mDC	=mGD.ImmediateContext;

			//I always use this, hope it doesn't change somehow
			mDC.InputAssembler.PrimitiveTopology	=PrimitiveTopology.TriangleList;

			//Get the backbuffer from the swapchain
			mBackBuffer	=Texture2D.FromSwapChain<Texture2D>(mSChain, 0);

			//Renderview on the backbuffer
			mBBView	=new RenderTargetView(mGD, mBackBuffer);
			
			//Create the depth buffer
			Texture2DDescription	depthDesc	=new Texture2DDescription()
			{
				//pick depth format based on feature level
				Format				=(mGD.FeatureLevel != FeatureLevel.Level_9_3)?
										Format.D32_Float_S8X24_UInt : Format.D24_UNorm_S8_UInt,
				ArraySize			=1,
				MipLevels			=1,
				Width				=mRForm.ClientSize.Width,
				Height				=mRForm.ClientSize.Height,
				SampleDescription	=new SampleDescription(1, 0),
				Usage				=ResourceUsage.Default,
				BindFlags			=BindFlags.DepthStencil,
				CpuAccessFlags		=CpuAccessFlags.None,
				OptionFlags			=ResourceOptionFlags.None
			};

			mDepthBuffer	=new Texture2D(mGD, depthDesc);
			
			//Create the depth buffer view
			mDSView	=new DepthStencilView(mGD, mDepthBuffer);
			
			//Setup targets and viewport for rendering
			mDC.Rasterizer.SetViewport(new Viewport(0, 0,
				mRForm.ClientSize.Width, mRForm.ClientSize.Height, 0.0f, 1.0f));

			mDC.OutputMerger.SetTargets(mDSView, mBBView);

			mRForm.UserResized	+=OnRenderFormResize;

			mGCam	=new UtilityLib.GameCamera(mRForm.ClientSize.Width,
				mRForm.ClientSize.Height, 16f/9f, 0.1f, 3000f);
		}


		void HandleResize()
		{
			Utilities.Dispose(ref mBackBuffer);
			Utilities.Dispose(ref mBBView);
			Utilities.Dispose(ref mDepthBuffer);
			Utilities.Dispose(ref mDSView);

			mSChain.ResizeBuffers(1, mRForm.Width, mRForm.Height, Format.Unknown, SwapChainFlags.None);

			mBackBuffer	=Texture2D.FromSwapChain<Texture2D>(mSChain, 0);
			mBBView		=new RenderTargetView(mGD, mBackBuffer);

			Texture2DDescription	depthDesc	=new Texture2DDescription()
			{
				//pick depth format based on feature level
				Format				=(mGD.FeatureLevel != FeatureLevel.Level_9_3)?
										Format.D32_Float_S8X24_UInt : Format.D24_UNorm_S8_UInt,
				ArraySize			=1,
				MipLevels			=1,
				Width				=mRForm.Width,
				Height				=mRForm.Height,
				SampleDescription	=new SampleDescription(1, 0),
				Usage				=ResourceUsage.Default,
				BindFlags			=BindFlags.DepthStencil,
				CpuAccessFlags		=CpuAccessFlags.None,
				OptionFlags			=ResourceOptionFlags.None
			};

			mDepthBuffer	=new Texture2D(mGD, depthDesc);
			mDSView			=new DepthStencilView(mGD, mDepthBuffer);

			Viewport	vp	=new Viewport(0, 0, mRForm.Width, mRForm.Height, 0f, 1f);

			mDC.Rasterizer.SetViewport(vp);
			mDC.OutputMerger.SetTargets(mDSView, mBBView);

			mGCam	=new UtilityLib.GameCamera(mRForm.Width, mRForm.Height, 16f/9f, 0.1f, 3000f);
		}


		public void Present()
		{
			mSChain.Present(0, PresentFlags.None);
		}


		public void ClearViews()
		{
			mDC.ClearDepthStencilView(mDSView, DepthStencilClearFlags.Depth, 1f, 0);
			mDC.ClearRenderTargetView(mBBView, Color.CornflowerBlue);
		}


		public void ReleaseAll()
		{
			mBBView.Dispose();
			mBackBuffer.Dispose();
			mDSView.Dispose();
			mDepthBuffer.Dispose();
			mDC.Dispose();
			mSChain.Dispose();
			mGD.Dispose();
		}


		public void ResetCursorPos()
		{
			Cursor.Position	=mStoredMousePos;
		}


		public void ToggleCapture(bool bOn)
		{
			if(bOn)
			{
				mRForm.Capture	=true;

				Cursor.Hide();

				mStoredMousePos	=Cursor.Position;

				Cursor.Clip	=mRForm.RectangleToScreen(mRForm.ClientRectangle);
			}
			else
			{
				mRForm.Capture	=false;

				Cursor.Show();
				Cursor.Clip	=mStoredClipRect;
			}
		}


		public void CheckResize()
		{
			if(!mbResized)
			{
				return;
			}

			HandleResize();

			mbResized	=false;
		}


		void OnRenderFormResize(object sender, EventArgs ea)
		{
			mbResized	=true;
		}
	}
}
