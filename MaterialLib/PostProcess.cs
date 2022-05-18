using System;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using Vortice.DXGI;
using Vortice.Direct3D11;
using Vortice.Mathematics;
using UtilityLib;


namespace MaterialLib;

public class PostProcess
{
	internal struct VertexPositionTexture
	{
		internal Vector3	Position;
		internal Vector2	TextureCoordinate;
	}

	//data for doing postery
	Dictionary<string, ID3D11Texture2D>				mPostTex2Ds		=new Dictionary<string, ID3D11Texture2D>();
	Dictionary<string, ID3D11RenderTargetView>		mPostTargets	=new Dictionary<string, ID3D11RenderTargetView>();
	Dictionary<string, ID3D11DepthStencilView>		mPostDepths		=new Dictionary<string, ID3D11DepthStencilView>();
	Dictionary<string, ID3D11ShaderResourceView>	mPostTargSRVs	=new Dictionary<string, ID3D11ShaderResourceView>();

	//keep track of lower res rendertargets
	//this info needed when a resize happens
	List<string>	mHalfResTargets		=new List<string>();
	List<string>	mQuarterResTargets	=new List<string>();
	List<string>	mFixedResTargets	=new List<string>();	//don't resize

	//data for looking up outline colours
	ID3D11Texture1D				mOutlineLookupTex;
	ID3D11ShaderResourceView	mOutlineLookupSRV;
	Color						[]mOutLineColors	=new Color[MaxOutlineColours];

	//for a fullscreen quad
	ID3D11Buffer	mQuadVB;
	ID3D11Buffer	mQuadIB;

	//stuff
	StuffKeeper	mSK;
	int			mResX, mResY;
	Color		mClearColor;

	//gaussian blur stuff
	float	[]mSampleWeightsX;
	float	[]mSampleWeightsY;
	float	[]mSampleOffsetsX;
	float	[]mSampleOffsetsY;

	//constants
	const float	BlurAmount			=4f;
	const int	MaxOutlineColours	=1024;


	public PostProcess(GraphicsDevice gd, StuffKeeper sk)
	{
		mSK	=sk;
		
		mResX		=gd.RendForm.ClientRectangle.Width;
		mResY		=gd.RendForm.ClientRectangle.Height;
		mClearColor	=Misc.SystemColorToDXColor(System.Drawing.Color.CornflowerBlue);

		ID3D11RenderTargetView	[]backBuf	=new ID3D11RenderTargetView[1];
		ID3D11DepthStencilView	backDepth;

		gd.DC.OMGetRenderTargets(1, backBuf, out backDepth);

		mPostTargets.Add("BackColor", backBuf[0]);
		mPostDepths.Add("BackDepth", backDepth);

		MakeQuad(gd);

		if(gd.GD.FeatureLevel != Vortice.Direct3D.FeatureLevel.Level_9_3)
		{
			MakeOutlineLookUp(gd);
		}

		InitPostParams(gd.GD.FeatureLevel == Vortice.Direct3D.FeatureLevel.Level_9_3);

		gd.ePreResize	+=OnPreResize;
		gd.eResized		+=OnResized;
	}


	public void MakePostTargetHalfRes(GraphicsDevice gd, string name,
		int resx, int resy, Format surf)
	{
		mHalfResTargets.Add(name);
		MakePostTarget(gd, name, resx, resy, surf);
	}

	public void MakePostTargetQuarterRes(GraphicsDevice gd, string name,
		int resx, int resy, Format surf)
	{
		mQuarterResTargets.Add(name);
		MakePostTarget(gd, name, resx, resy, surf);
	}

	public void MakePostTargetFixedRes(GraphicsDevice gd, string name,
		int resx, int resy, Format surf)
	{
		mFixedResTargets.Add(name);
		MakePostTarget(gd, name, resx, resy, surf);
	}


	public void MakePostTarget(GraphicsDevice gd, string name,
		int resx, int resy, Format surf)
	{
		Texture2DDescription	targDesc	=new Texture2DDescription()
		{
			//pick depth format based on feature level
			Format				=surf,
			ArraySize			=1,
			MipLevels			=1,
			Width				=resx,
			Height				=resy,
			SampleDescription	=new SampleDescription(1, 0),
			Usage				=ResourceUsage.Default,
			BindFlags			=BindFlags.RenderTarget | BindFlags.ShaderResource,
			CPUAccessFlags		=CpuAccessFlags.None,
			MiscFlags			=ResourceOptionFlags.None
		};

		ID3D11Texture2D	targ	=gd.GD.CreateTexture2D(targDesc);

		ID3D11RenderTargetView	targView	=gd.GD.CreateRenderTargetView(targ);
		
		ID3D11ShaderResourceView	targSRV	=gd.GD.CreateShaderResourceView(targ);

		mPostTex2Ds.Add(name, targ);
		mPostTargets.Add(name, targView);
		mPostTargSRVs.Add(name, targSRV);
	}


	public void MakePostDepth(GraphicsDevice gd, string name,
		int resx, int resy, Format surf)
	{
		Texture2DDescription	targDesc	=new Texture2DDescription()
		{
			//pick depth format based on feature level
			Format				=surf,
			ArraySize			=1,
			MipLevels			=1,
			Width				=resx,
			Height				=resy,
			SampleDescription	=new SampleDescription(1, 0),
			Usage				=ResourceUsage.Default,
			BindFlags			=BindFlags.DepthStencil,
			CPUAccessFlags		=CpuAccessFlags.None,
			MiscFlags			=ResourceOptionFlags.None
		};

		ID3D11Texture2D	targ	=gd.GD.CreateTexture2D(targDesc);

		ID3D11DepthStencilView	targView	=gd.GD.CreateDepthStencilView(targ);

		mPostTex2Ds.Add(name, targ);
		mPostDepths.Add(name, targView);
	}


	//set up parameters with known values
	void InitPostParams(bool bNineThree)
	{
		CBKeeper	cbk	=mSK.GetCBKeeper();

		cbk.SetOutlinerVars(new Vector2(mResX, mResY), 1f, 5f);

		cbk.SetInvViewPort(new Vector2(1f / mResX, 1f / mResY));

		cbk.SetBilateralBlurVars(1f, 1f, 0.75f);

		cbk.SetBloomVars(0.25f, 1.25f, 1f, 1f, 1f);

		InitBlurParams(1.0f / (mResX / 2), 0, 0, 1.0f / (mResY / 2), bNineThree);

		cbk.SetWeightsOffsets(mSampleWeightsX, mSampleOffsetsY,
			mSampleOffsetsX, mSampleOffsetsY);
	}


	//from the xna bloom sample
	void InitBlurParams(float dxX, float dyX, float dxY, float dyY, bool bNineThree)
	{
		int	sampleCountX;
		int	sampleCountY;

		//Doesn't seem to be a way to get array sizes from shaders any more
		//these values need to match KERNEL_SIZE in post.fx
		if(bNineThree)
		{
			sampleCountX	=15;
			sampleCountY	=15;
		}
		else
		{
			sampleCountX	=61;
			sampleCountY	=61;
		}
		
		//Create temporary arrays for computing our filter settings.
		mSampleWeightsX	=new float[sampleCountX];
		mSampleWeightsY	=new float[sampleCountY];
		mSampleOffsetsX	=new float[sampleCountX];
		mSampleOffsetsY	=new float[sampleCountY];
		
		//The first sample always has a zero offset.
		mSampleWeightsX[0]	=ComputeGaussian(0);
		mSampleOffsetsX[0]	=0f;
		mSampleWeightsY[0]	=ComputeGaussian(0);
		mSampleOffsetsY[0]	=0f;
		
		//Maintain a sum of all the weighting values.
		float	totalWeightsX	=mSampleWeightsX[0];
		float	totalWeightsY	=mSampleWeightsY[0];
		
		//Add pairs of additional sample taps, positioned
		//along a line in both directions from the center.
		for(int i=0;i < sampleCountX / 2; i++)
		{
			//Store weights for the positive and negative taps.
			float	weight				=ComputeGaussian(i + 1);
			mSampleWeightsX[i * 2 + 1]	=weight;
			mSampleWeightsX[i * 2 + 2]	=weight;				
			totalWeightsX				+=weight * 2;

			//To get the maximum amount of blurring from a limited number of
			//pixel shader samples, we take advantage of the bilinear filtering
			//hardware inside the texture fetch unit. If we position our texture
			//coordinates exactly halfway between two texels, the filtering unit
			//will average them for us, giving two samples for the price of one.
			//This allows us to step in units of two texels per sample, rather
			//than just one at a time. The 1.5 offset kicks things off by
			//positioning us nicely in between two texels.
			float	sampleOffset	=i * 2 + 1.5f;
			
			Vector2	deltaX	=new Vector2(dxX, dyX) * sampleOffset;

			//Store texture coordinate offsets for the positive and negative taps.
			mSampleOffsetsX[i * 2 + 1]	=deltaX.X;
			mSampleOffsetsX[i * 2 + 2]	=-deltaX.X;
		}

		//Add pairs of additional sample taps, positioned
		//along a line in both directions from the center.
		for(int i=0;i < sampleCountY / 2; i++)
		{
			//Store weights for the positive and negative taps.
			float	weight				=ComputeGaussian(i + 1);
			mSampleWeightsY[i * 2 + 1]	=weight;
			mSampleWeightsY[i * 2 + 2]	=weight;				
			totalWeightsY				+=weight * 2;

			//To get the maximum amount of blurring from a limited number of
			//pixel shader samples, we take advantage of the bilinear filtering
			//hardware inside the texture fetch unit. If we position our texture
			//coordinates exactly halfway between two texels, the filtering unit
			//will average them for us, giving two samples for the price of one.
			//This allows us to step in units of two texels per sample, rather
			//than just one at a time. The 1.5 offset kicks things off by
			//positioning us nicely in between two texels.
			float	sampleOffset	=i * 2 + 1.5f;
			
			Vector2	deltaY	=new Vector2(dxY, dyY) * sampleOffset;

			//Store texture coordinate offsets for the positive and negative taps.
			mSampleOffsetsY[i * 2 + 1]	=deltaY.Y;
			mSampleOffsetsY[i * 2 + 2]	=-deltaY.Y;
		}

		//Normalize the list of sample weightings, so they will always sum to one.
		for(int i=0;i < mSampleWeightsX.Length;i++)
		{
			mSampleWeightsX[i]	/=totalWeightsX;
		}
		for(int i=0;i < mSampleWeightsY.Length;i++)
		{
			mSampleWeightsY[i]	/=totalWeightsY;
		}
	}
	

	float ComputeGaussian(float n)
	{
		float	theta	=BlurAmount;
		
		return	(float)((1.0 / Math.Sqrt(2 * Math.PI * theta)) *
			Math.Exp(-(n * n) / (2 * theta * theta)));
	}


	void MakeQuad(GraphicsDevice gd)
	{
		VertexPositionTexture	[]verts	=new VertexPositionTexture[4];

		verts[0].Position = new Vector3(-1, 1, 1);
		verts[1].Position = new Vector3(1, 1, 1);
		verts[2].Position = new Vector3(-1, -1, 1);
		verts[3].Position = new Vector3(1, -1, 1);
		
		verts[0].TextureCoordinate = new Vector2(0, 0);
		verts[1].TextureCoordinate = new Vector2(1, 0);
		verts[2].TextureCoordinate = new Vector2(0, 1);
		verts[3].TextureCoordinate = new Vector2(1, 1);

		UInt16	[]inds	=new UInt16[6];
		inds[0]	=0;
		inds[1]	=1;
		inds[2]	=2;
		inds[3]	=1;
		inds[4]	=3;
		inds[5]	=2;

		BufferDescription	bd	=new BufferDescription(
			20 * verts.Length, BindFlags.VertexBuffer);

		mQuadVB	=gd.GD.CreateBuffer<VertexPositionTexture>(verts, bd);

		BufferDescription	id	=new BufferDescription(
			inds.Length * 2, BindFlags.IndexBuffer);

		mQuadIB	=gd.GD.CreateBuffer<UInt16>(inds, id);
	}


	public Color []GetOutlineColors()
	{
		return	mOutLineColors;
	}


	unsafe void MakeOutlineLookUp(GraphicsDevice gd)
	{
		Texture1DDescription	texDesc	=new Texture1DDescription();
		texDesc.ArraySize		=1;
		texDesc.BindFlags		=BindFlags.ShaderResource;
		texDesc.CPUAccessFlags	=CpuAccessFlags.Write;
		texDesc.MipLevels		=1;
		texDesc.MiscFlags		=ResourceOptionFlags.None;
		texDesc.Usage			=ResourceUsage.Dynamic;
		texDesc.Width			=MaxOutlineColours;
		texDesc.Format			=Format.R8G8B8A8_UNorm;

		mOutlineLookupTex	=gd.GD.CreateTexture1D(texDesc);
		mOutlineLookupSRV	=gd.GD.CreateShaderResourceView(mOutlineLookupTex);
	}


	public void UpdateOutlineColours(GraphicsDevice gd, int numMaterials)
	{
		gd.DC.UpdateSubresource(mOutLineColors, mOutlineLookupTex);
	}


	public void ClearTarget(GraphicsDevice gd, string targ, Color clearColor)
	{
		gd.DC.ClearRenderTargetView(mPostTargets[targ], clearColor);
	}


	public void ClearDepth(GraphicsDevice gd, string depth)
	{
		gd.DC.ClearDepthStencilView(mPostDepths[depth], DepthStencilClearFlags.Depth, 1f, 0);
	}


	public void SetClearColor(Color col)
	{
		mClearColor	=col;
	}


	public void SetTargets(GraphicsDevice gd, string targName, string depthName)
	{
		if(targName == null && depthName == null)
		{
			gd.DC.OMSetRenderTargets((ID3D11RenderTargetView)null, null);
		}
		else if(targName == "null")
		{
			gd.DC.OMSetRenderTargets((ID3D11RenderTargetView)null, null);
		}
		else if(mPostTargets.ContainsKey(targName)
			&& mPostDepths.ContainsKey(depthName))
		{
			gd.DC.OMSetRenderTargets(mPostTargets[targName], mPostDepths[depthName]);
		}
		else if(mPostTargets.ContainsKey(targName)
			&& depthName == "null")
		{
			gd.DC.OMSetRenderTargets(mPostTargets[targName]);
		}
		else
		{
			//need some sort of error here
			Debug.Assert(false);
			return;
		}
	}


	public void SetTwoTargets(GraphicsDevice gd,
		string targName1, string targName2,
		string depthName)
	{
		if(targName1 == null && targName2 == null && depthName == null)
		{
			gd.DC.OMSetRenderTargets((ID3D11RenderTargetView)null, null);
		}
		else if(targName1 == "null")
		{
			gd.DC.OMSetRenderTargets((ID3D11RenderTargetView)null, null);
		}
		else if(mPostTargets.ContainsKey(targName1)
			&& mPostTargets.ContainsKey(targName2)
			&& mPostDepths.ContainsKey(depthName))
		{
			ID3D11RenderTargetView	[]targs	=new ID3D11RenderTargetView[2];

			targs[0]	=mPostTargets[targName1];
			targs[1]	=mPostTargets[targName2];

			gd.DC.OMSetRenderTargets(2, targs, mPostDepths[depthName]);
		}
		else if(mPostTargets.ContainsKey(targName1)
			&& mPostTargets.ContainsKey(targName2)
			&& depthName == "null")
		{
			ID3D11RenderTargetView	[]targs	=new ID3D11RenderTargetView[2];

			targs[0]	=mPostTargets[targName1];
			targs[1]	=mPostTargets[targName2];

			gd.DC.OMSetRenderTargets(2, targs);
		}
		else
		{
			//need some sort of error here
			Debug.Assert(false);
			return;
		}
	}


//		public Texture2D GetTargetTexture(string targName)
//		{
//			return	mPostTargets[targName];
//		}


	public void DrawStage(GraphicsDevice gd, string vs, string ps)
	{
		gd.DC.IASetVertexBuffer(0, mQuadVB, 20);
		gd.DC.IASetIndexBuffer(mQuadIB, Format.R16_UInt, 0);

		CBKeeper	cbk	=mSK.GetCBKeeper();

		cbk.UpdatePost(gd.DC);

		gd.DC.DrawIndexed(6, 0, 0);
	}


	public void FreeAll(GraphicsDevice gd)
	{
		//unwire from device resize stuff
		gd.ePreResize	-=OnPreResize;
		gd.eResized		-=OnResized;

		//dispose all views
		foreach(KeyValuePair<string, ID3D11RenderTargetView> view in mPostTargets)
		{
			view.Value.Dispose();
		}
		foreach(KeyValuePair<string, ID3D11DepthStencilView> view in mPostDepths)
		{
			view.Value.Dispose();
		}

		//dispose all srvs
		foreach(KeyValuePair<string, ID3D11ShaderResourceView> srv in mPostTargSRVs)
		{
			srv.Value.Dispose();
		}

		//dispose all tex2ds
		foreach(KeyValuePair<string, ID3D11Texture2D> tex in mPostTex2Ds)
		{
			tex.Value.Dispose();
		}

		mPostTargets.Clear();
		mPostDepths.Clear();
		mPostTargSRVs.Clear();
		mPostTex2Ds.Clear();

		//buffers
		mQuadIB.Dispose();
		mQuadVB.Dispose();

		if(mOutlineLookupSRV != null)
		{
			mOutlineLookupSRV.Dispose();
		}
		if(mOutlineLookupTex != null)
		{
			mOutlineLookupTex.Dispose();
		}
	}


	void OnPreResize(object sender, EventArgs ea)
	{
		GraphicsDevice	gd	=sender as GraphicsDevice;
		if(gd == null)
		{
			return;
		}

		ID3D11RenderTargetView	backRTV	=mPostTargets["BackColor"];
		ID3D11DepthStencilView	backDSV	=mPostDepths["BackDepth"];

		//release these so the device can resize the swapchain
		mPostTargets.Remove("BackColor");
		mPostDepths.Remove("BackDepth");
		backRTV.Dispose();
		backDSV.Dispose();
	}


	void OnResized(object sender, EventArgs ea)
	{
		GraphicsDevice	gd	=sender as GraphicsDevice;
		if(gd == null)
		{
			return;
		}

		mResX	=gd.RendForm.ClientRectangle.Width;
		mResY	=gd.RendForm.ClientRectangle.Height;

		InitPostParams(gd.GD.FeatureLevel == Vortice.Direct3D.FeatureLevel.Level_9_3);

		//back buffer will already be resized
		ID3D11RenderTargetView	[]backBuf	=new ID3D11RenderTargetView[1];
		ID3D11DepthStencilView	backDepth;

		gd.DC.OMGetRenderTargets(1, backBuf, out backDepth);

		//dispose all views
		foreach(KeyValuePair<string, ID3D11RenderTargetView> view in mPostTargets)
		{
			view.Value.Dispose();
		}
		foreach(KeyValuePair<string, ID3D11DepthStencilView> view in mPostDepths)
		{
			view.Value.Dispose();
		}

		//dispose all srvs
		foreach(KeyValuePair<string, ID3D11ShaderResourceView> srv in mPostTargSRVs)
		{
			srv.Value.Dispose();
		}

		mPostTargets.Clear();
		mPostDepths.Clear();
		mPostTargSRVs.Clear();

		//copy the descriptions
		Dictionary<string, Texture2DDescription>	descs	=new Dictionary<string, Texture2DDescription>();

		foreach(KeyValuePair<string, ID3D11Texture2D> tex in mPostTex2Ds)
		{
			Texture2DDescription	resizeDesc	=new Texture2DDescription()
			{
				Format				=tex.Value.Description.Format,
				ArraySize			=tex.Value.Description.ArraySize,
				MipLevels			=tex.Value.Description.MipLevels,
				SampleDescription	=new SampleDescription(1, 0),
				Usage				=tex.Value.Description.Usage,
				BindFlags			=tex.Value.Description.BindFlags,
				CPUAccessFlags		=tex.Value.Description.CPUAccessFlags,
				MiscFlags			=tex.Value.Description.MiscFlags
			};

			if(mHalfResTargets.Contains(tex.Key))
			{
				resizeDesc.Width	=mResX / 2;
				resizeDesc.Height	=mResY / 2;
			}
			else if(mQuarterResTargets.Contains(tex.Key))
			{
				resizeDesc.Width	=mResX / 2;
				resizeDesc.Height	=mResY / 2;
			}
			else if(mFixedResTargets.Contains(tex.Key))
			{
				resizeDesc.Width	=tex.Value.Description.Width;
				resizeDesc.Height	=tex.Value.Description.Height;
			}
			else
			{
				resizeDesc.Width	=mResX;
				resizeDesc.Height	=mResY;
			}

			descs.Add(tex.Key, resizeDesc);
		}

		//blast all textures
		foreach(KeyValuePair<string, ID3D11Texture2D> tex in mPostTex2Ds)
		{
			tex.Value.Dispose();
		}
		mPostTex2Ds.Clear();
		
		//make resized versions
		foreach(KeyValuePair<string, Texture2DDescription> texDesc in descs)
		{
			ID3D11Texture2D	newTex	=gd.GD.CreateTexture2D(texDesc.Value);
			mPostTex2Ds.Add(texDesc.Key, newTex);

			if(Misc.bFlagSet((uint)newTex.Description.BindFlags, (uint)BindFlags.DepthStencil))
			{
				ID3D11DepthStencilView	dsv	=gd.GD.CreateDepthStencilView(newTex);
				mPostDepths.Add(texDesc.Key, dsv);
			}
			else
			{
				ID3D11RenderTargetView	rtv	=gd.GD.CreateRenderTargetView(newTex);

				ID3D11ShaderResourceView	srv	=gd.GD.CreateShaderResourceView(newTex);

				mPostTargets.Add(texDesc.Key, rtv);
				mPostTargSRVs.Add(texDesc.Key, srv);
			}
		}

		//readd the device backbuffer
		mPostTargets.Add("BackColor", backBuf[0]);
		mPostDepths.Add("BackDepth", backDepth);
	}
}