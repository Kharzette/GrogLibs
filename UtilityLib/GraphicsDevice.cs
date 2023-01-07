using System;
using System.Diagnostics;
using SharpDX.Windows;		//renderform
using Vortice.Mathematics;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.Direct3D11.Debug;
using Vortice.DXGI;
using SharpGen.Runtime;

using Cursor		=System.Windows.Forms.Cursor;
using ResultCode	=Vortice.DXGI.ResultCode;


namespace UtilityLib;

public class GraphicsDevice
{
	RenderForm	mRForm;

	ID3D11Device		mGD;
	ID3D11Debug			mGDD;
	ID3D11DeviceContext	mDC;

	IDXGISwapChain	mSChain;

	ID3D11Texture2D			mBackBuffer, mDepthBuffer;
	ID3D11RenderTargetView	mBBView;
	ID3D11DepthStencilView	mDSView;

	Viewport		mScreenPort, mShadowPort;
	FeatureLevel	?mFLevel;

	GameCamera	mGCam;

	//keep near and far clip distances
	//need for camera rebuild on device lost or resize
	float	mClipNear, mClipFar;
	bool	mbResized;

	//keep track of mouse pos during mouse look
	System.Drawing.Point		mStoredMousePos	=System.Drawing.Point.Empty;
	System.Drawing.Rectangle	mStoredClipRect	=Cursor.Clip;

	public event EventHandler	ePreResize;
	public event EventHandler	eResized;
	public event EventHandler	eDeviceLost;


	public ID3D11Device GD
	{
		get { return mGD; }
	}

	public ID3D11DeviceContext DC
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


	public GraphicsDevice(string formTitle, System.Drawing.Icon icon,
		FeatureLevel ?flevel, float near, float far)
	{			
		mRForm	=new RenderForm(formTitle, icon);

		//screen viewport
		mScreenPort	=new Viewport(0, 0,
			mRForm.ClientRectangle.Width,
			mRForm.ClientRectangle.Height,
			0f, 1f);

		//shadow viewport
		mShadowPort	=new Viewport(0, 0,
			512, 512, 0f, 1f);

		mRForm.UserResized	+=OnRenderFormResize;

		mClipNear	=near;
		mClipFar	=far;

		mGCam	=new UtilityLib.GameCamera(mRForm.ClientSize.Width,
			mRForm.ClientSize.Height, 16f/9f, near, far);

		Init(flevel);
	}


	void Init(FeatureLevel ?flevel)
	{
		//release any existing
		ReleaseAll();

		IDXGIFactory2	fact2	=DXGI.CreateDXGIFactory1<IDXGIFactory2>();

		IDXGIFactory6	fact6	=fact2.QueryInterfaceOrNull<IDXGIFactory6>();

		int	highPerf	=fact6.GetAdapterByGpuPreference(GpuPreference.HighPerformance);

		FeatureLevel	[]features	=null;
		if(flevel != null)
		{
			features	=new FeatureLevel[1];

			features[0]	=new FeatureLevel();
			features[0]	=flevel.Value;
		}

		FeatureLevel	?outFeatures;

		SwapChainDescription	scDesc	=new SwapChainDescription();

		scDesc.BufferCount			=1;
		scDesc.Flags				=SwapChainFlags.None;
		scDesc.Windowed				=true;
		scDesc.OutputWindow			=mRForm.Handle;
		scDesc.SampleDescription	=new SampleDescription(1, 0);
		scDesc.SwapEffect			=SwapEffect.Discard;
		scDesc.BufferUsage			=Usage.RenderTargetOutput;
		scDesc.BufferDescription	=new ModeDescription(
			0, 0,
//				mRForm.ClientSize.Width, mRForm.ClientSize.Height,
			new Rational(60, 1), Format.R8G8B8A8_UNorm_SRgb);

		bool	bDebug	=true;

		Result	res	=D3D11.D3D11CreateDeviceAndSwapChain(null, DriverType.Hardware,
			DeviceCreationFlags.Debug, features,
			scDesc, out mSChain, out mGD, out outFeatures, out mDC);
		
		if(res != Result.Ok)
		{
			//probably no debug stuff installed
			bDebug	=false;
			res	=D3D11.D3D11CreateDeviceAndSwapChain(null, DriverType.Hardware,
				DeviceCreationFlags.None, features,
				scDesc, out mSChain, out mGD, out outFeatures, out mDC);

			if(res != Result.Ok)
			{
				Debug.WriteLine("Device creation failed: " + res.ToString());
				fact6.Dispose();
				fact2.Dispose();
				return;
			}
		}

		if(bDebug)
		{
			mGDD	=mGD.QueryInterface<ID3D11Debug>();
		}

		fact6.Dispose();
		fact2.Dispose();

		if(res != Result.Ok)
		{
			return;
		}

		if(outFeatures != null)
		{
			mFLevel	=outFeatures.Value;
		}


		//I always use this, hope it doesn't change somehow
		DC.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);

		//Get the backbuffer from the swapchain
		//Renderview on the backbuffer
		mBackBuffer	=mSChain.GetBuffer<ID3D11Texture2D>(0);
		mBBView		=mGD.CreateRenderTargetView(mBackBuffer);
		
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
			CPUAccessFlags		=CpuAccessFlags.None,
			MiscFlags			=ResourceOptionFlags.None
		};

		mDepthBuffer	=mGD.CreateTexture2D(depthDesc);
		mDSView			=mGD.CreateDepthStencilView(mDepthBuffer);

		//screen viewport
		mScreenPort	=new Viewport(0, 0,
			mRForm.ClientRectangle.Width,
			mRForm.ClientRectangle.Height,
			0f, 1f);

		//shadow viewport
		mShadowPort	=new Viewport(0, 0,
			512, 512, 0f, 1f);

		//Setup targets and viewport for rendering
		mDC.RSSetViewport(mScreenPort);
		mDC.RSSetScissorRect(mRForm.ClientRectangle.Width,
			mRForm.ClientRectangle.Height);

		mDC.OMSetRenderTargets(mBBView, mDSView);

		mRForm.UserResized	+=OnRenderFormResize;

		mGCam	=new UtilityLib.GameCamera(mRForm.ClientSize.Width,
			mRForm.ClientSize.Height, 16f/9f, mClipNear, mClipFar);
	}


	void HandleResize()
	{
		int	width	=mRForm.ClientRectangle.Width;
		int	height	=mRForm.ClientRectangle.Height;

		if(width == 0 || height == 0)
		{
			return;	//minimize?
		}

		//fire this event, and hopefully other code will
		//use it to let go of references to device stuff
		Misc.SafeInvoke(ePreResize, this);

		mBBView.Dispose();
		mDSView.Dispose();
		mBackBuffer.Dispose();
		mDepthBuffer.Dispose();

		DC.ClearState();

		DC.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);

		mSChain.ResizeBuffers(1, width, height, Format.Unknown, SwapChainFlags.None);

		mBackBuffer	=mSChain.GetBuffer<ID3D11Texture2D>(0);
		mBBView		=mGD.CreateRenderTargetView(mBackBuffer);

		Texture2DDescription	depthDesc	=new Texture2DDescription()
		{
			//pick depth format based on feature level
			Format				=(mGD.FeatureLevel != FeatureLevel.Level_9_3)?
									Format.D32_Float_S8X24_UInt : Format.D24_UNorm_S8_UInt,
			ArraySize			=1,
			MipLevels			=1,
			Width				=width,
			Height				=height,
			SampleDescription	=new SampleDescription(1, 0),
			Usage				=ResourceUsage.Default,
			BindFlags			=BindFlags.DepthStencil,
			CPUAccessFlags		=CpuAccessFlags.None,
			MiscFlags			=ResourceOptionFlags.None
		};

		mDepthBuffer	=mGD.CreateTexture2D(depthDesc);
		mDSView			=mGD.CreateDepthStencilView(mDepthBuffer);

		mScreenPort	=new Viewport(0, 0, width, height, 0f, 1f);

		mDC.RSSetViewport(mScreenPort);
		mDC.OMSetRenderTargets(mBBView, mDSView);

		mGCam	=new UtilityLib.GameCamera(width, height, 16f/9f, mClipNear, mClipFar);

		//other stuff can now resize rendertargets and such
		Misc.SafeInvoke(eResized, this);
	}


	public void Present()
	{
		Result	r	=mSChain.Present(0);
		if(r.Failure)
		{
			if(r.Code == ResultCode.DeviceRemoved
				|| r.Code == ResultCode.DeviceReset
				|| r.Code == ResultCode.DeviceHung
				|| r.Code == ResultCode.DriverInternalError)
			{
				Debug.WriteLine("Device lost : " + r.Code.ToString());
				Init(mFLevel);
				Misc.SafeInvoke(eDeviceLost, null);
			}				
		}
	}


	public void ClearViews()
	{
		mDC.ClearDepthStencilView(mDSView, DepthStencilClearFlags.Depth, 1f, 0);
		mDC.ClearRenderTargetView(mBBView,
			Misc.SystemColorToDXColor(System.Drawing.Color.CornflowerBlue));
	}


	public void ClearDepth()
	{
		mDC.ClearDepthStencilView(mDSView, DepthStencilClearFlags.Depth, 1f, 0);
	}


	public void SetFullScreen(bool bFull)
	{
		mSChain.SetFullscreenState(bFull, null);
		mbResized	=true;
	}


	public void SetClip(float near, float far)
	{
		mClipNear	=near;
		mClipFar	=far;
	}


	public void ReleaseAll()
	{
		if(mDC != null)
		{
			mDC.ClearState();
			mDC.Flush();
			mDC.Dispose();
		}

		if(mBBView != null)			mBBView.Dispose();
		if(mBackBuffer != null)		mBackBuffer.Dispose();
		if(mDSView != null)			mDSView.Dispose();
		if(mDepthBuffer != null)	mDepthBuffer.Dispose();
		if(mSChain != null)			mSChain.Dispose();
		if(mGD != null)				mGD.Dispose();

/*
#if DEBUG
		if(mGDD != null)
		{
			mGDD.ReportLiveDeviceObjects(ReportingLevel.Detail);
			mGDD.Dispose();
		}
#endif*/
	}


	public void SetShadowViewPort()
	{
		mDC.RSSetViewport(mShadowPort);
	}


	public void SetScreenViewPort()
	{
		mDC.RSSetViewport(mScreenPort);
	}


	public Viewport GetScreenViewPort()
	{
		return	mScreenPort;
	}


	public void ResetCursorPos()
	{
		Cursor.Position	=mStoredMousePos;
	}


	public void SetCapture(bool bOn)
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