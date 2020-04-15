using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using UtilityLib;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

using Buffer		=SharpDX.Direct3D11.Buffer;
using RenderForm	=SharpDX.Windows.RenderForm;


namespace MaterialLib
{
	public class PostProcess
	{
		internal struct VertexPositionTexture
		{
			internal Vector3	Position;
			internal Vector2	TextureCoordinate;
		}

		//data for doing postery
		Dictionary<string, Texture2D>			mPostTex2Ds		=new Dictionary<string, Texture2D>();
		Dictionary<string, RenderTargetView>	mPostTargets	=new Dictionary<string, RenderTargetView>();
		Dictionary<string, DepthStencilView>	mPostDepths		=new Dictionary<string, DepthStencilView>();
		Dictionary<string, ShaderResourceView>	mPostTargSRVs	=new Dictionary<string, ShaderResourceView>();

		//keep track of lower res rendertargets
		//this info needed when a resize happens
		List<string>	mHalfResTargets		=new List<string>();
		List<string>	mQuarterResTargets	=new List<string>();
		List<string>	mFixedResTargets	=new List<string>();	//don't resize

		//data for looking up outline colours
		Texture1D			mOutlineLookupTex;
		ShaderResourceView	mOutlineLookupSRV;
		Color				[]mOutLineColors	=new Color[MaxOutlineColours];

		//for a fullscreen quad
		Buffer				mQuadVB;
		Buffer				mQuadIB;
		VertexBufferBinding	mQuadBinding;

		//effect file that has most of the post stuff in it
		Effect	mPostFX;

		//stuff
		int		mResX, mResY;
		Color	mClearColor;

		//gaussian blur stuff
		float	[]mSampleWeightsX;
		float	[]mSampleWeightsY;
		float	[]mSampleOffsetsX;
		float	[]mSampleOffsetsY;

		//constants
		const float	BlurAmount			=4f;
		const int	MaxOutlineColours	=1024;


		public PostProcess(GraphicsDevice gd, Effect fx)
		{
			Init(gd, fx);
		}


		public PostProcess(GraphicsDevice gd, MaterialLib mlib, string fxName)
		{
			if(mlib == null)
			{
				return;
			}

			Effect	fx	=mlib.GetEffect(fxName);
			if(fx == null)
			{
				return;
			}
			Init(gd, fx);
		}


		void Init(GraphicsDevice gd, Effect fx)
		{
			if(fx == null)
			{
				return;
			}

			mPostFX	=fx;

			mResX		=gd.RendForm.ClientRectangle.Width;
			mResY		=gd.RendForm.ClientRectangle.Height;
			mClearColor	=Color.CornflowerBlue;

			RenderTargetView	[]backBuf	=new RenderTargetView[1];
			DepthStencilView	backDepth;

			backBuf	=gd.DC.OutputMerger.GetRenderTargets(1, out backDepth);

			mPostTargets.Add("BackColor", backBuf[0]);
			mPostDepths.Add("BackDepth", backDepth);

			MakeQuad(gd);

			if(gd.GD.FeatureLevel != FeatureLevel.Level_9_3)
			{
				MakeOutlineLookUp(gd);
			}

			InitPostParams(gd.GD.FeatureLevel == FeatureLevel.Level_9_3);

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
				CpuAccessFlags		=CpuAccessFlags.None,
				OptionFlags			=ResourceOptionFlags.None
			};

			Texture2D	targ	=new Texture2D(gd.GD, targDesc);

			RenderTargetView	targView	=new RenderTargetView(gd.GD, targ);

			ShaderResourceView	targSRV	=new ShaderResourceView(gd.GD, targ);

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
				CpuAccessFlags		=CpuAccessFlags.None,
				OptionFlags			=ResourceOptionFlags.None
			};

			Texture2D	targ	=new Texture2D(gd.GD, targDesc);

			DepthStencilView	targView	=new DepthStencilView(gd.GD, targ);

			mPostTex2Ds.Add(name, targ);
			mPostDepths.Add(name, targView);
		}


		void SetVar(string varName, float val)
		{
			EffectVariable	ev	=mPostFX.GetVariableByName(varName);
			if(ev == null)
			{
				return;
			}

			EffectScalarVariable	esv	=ev.AsScalar();
			if(esv == null)
			{
				ev.Dispose();
				return;
			}

			esv.Set(val);

			esv.Dispose();
			ev.Dispose();
		}


		void SetVar(string varName, float []val)
		{
			EffectVariable	ev	=mPostFX.GetVariableByName(varName);
			if(ev == null)
			{
				return;
			}

			EffectScalarVariable	esv	=ev.AsScalar();
			if(esv == null)
			{
				ev.Dispose();
				return;
			}

			esv.Set(val);

			esv.Dispose();
			ev.Dispose();
		}


		void SetVar(string varName, Vector2 val)
		{
			EffectVariable	ev	=mPostFX.GetVariableByName(varName);
			if(ev == null)
			{
				return;
			}

			EffectVectorVariable	esv	=ev.AsVector();
			if(esv == null)
			{
				ev.Dispose();
				return;
			}

			esv.Set(val);

			esv.Dispose();
			ev.Dispose();
		}


		//set up parameters with known values
		void InitPostParams(bool bNineThree)
		{
			SetVar("mTexelSteps", 1f);
			SetVar("mThreshold", 5f);
			SetVar("mScreenSize", new Vector2(mResX, mResY));
			SetVar("mInvViewPort", new Vector2(1f / mResX, 1f / mResY));
			SetVar("mOpacity", 0.75f);

			//bloomstuffs
			SetVar("mBloomThreshold", 0.25f);
			SetVar("mBloomIntensity", 1.25f);
			SetVar("mBloomSaturation", 1f);
			SetVar("mBaseIntensity", 1f);
			SetVar("mBaseSaturation", 1f);

			InitBlurParams(1.0f / (mResX / 2), 0, 0, 1.0f / (mResY / 2), bNineThree);

			//hidef can afford to store these once
			SetBlurParams(true);
			SetBlurParams(false);
		}


		void SetBlurParams(bool bX)
		{
			if(bX)
			{
				SetVar("mWeightsX", mSampleWeightsX);
				SetVar("mOffsetsX", mSampleOffsetsX);
			}
			else
			{
				SetVar("mWeightsY", mSampleWeightsY);
				SetVar("mOffsetsY", mSampleOffsetsY);
			}
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
				20 * verts.Length,
				ResourceUsage.Immutable, BindFlags.VertexBuffer,
				CpuAccessFlags.None, ResourceOptionFlags.None, 0);

			mQuadVB	=Buffer.Create(gd.GD, verts, bd);

			BufferDescription	id	=new BufferDescription(inds.Length * 2,
				ResourceUsage.Immutable, BindFlags.IndexBuffer,
				CpuAccessFlags.None, ResourceOptionFlags.None, 0);

			mQuadIB	=Buffer.Create<UInt16>(gd.GD, inds, id);

			mQuadBinding	=new VertexBufferBinding(mQuadVB, 20, 0);
		}


		public Color []GetOutlineColors()
		{
			return	mOutLineColors;
		}


		void MakeOutlineLookUp(GraphicsDevice gd)
		{
			SampleDescription	sampDesc	=new SampleDescription();
			sampDesc.Count		=1;
			sampDesc.Quality	=0;

			DataStream	ds	=new DataStream(MaxOutlineColours * 4, false, true);
			for(int x=0;x < MaxOutlineColours;x++)
			{
				ds.Write(Color.White);
			}

			Texture1DDescription	texDesc	=new Texture1DDescription();
			texDesc.ArraySize		=1;
			texDesc.BindFlags		=BindFlags.ShaderResource;
			texDesc.CpuAccessFlags	=CpuAccessFlags.Write;
			texDesc.MipLevels		=1;
			texDesc.OptionFlags		=ResourceOptionFlags.None;
			texDesc.Usage			=ResourceUsage.Dynamic;
			texDesc.Width			=MaxOutlineColours;
			texDesc.Format			=Format.R8G8B8A8_UNorm;

			mOutlineLookupTex	=new Texture1D(gd.GD, texDesc, ds);
			mOutlineLookupSRV	=new ShaderResourceView(gd.GD, mOutlineLookupTex);
		}


		public void UpdateOutlineColours(GraphicsDevice gd, int numMaterials)
		{
			EffectVariable	ev	=mPostFX.GetVariableByName("mOutlineTex");
			if(ev == null)
			{
				return;
			}

			EffectShaderResourceVariable	esrv	=ev.AsShaderResource();
			if(esrv == null)
			{
				ev.Dispose();
				return;
			}

			DataStream	ds;

			gd.DC.MapSubresource(mOutlineLookupTex, 0, MapMode.WriteDiscard,
				SharpDX.Direct3D11.MapFlags.None, out ds);

			for(int i=0;i < numMaterials;i++)
			{
				Color	col	=mOutLineColors[i];

				col.R	/=2;
				col.G	/=2;
				col.B	/=2;

				ds.Write<Color>(col);
			}

			gd.DC.UnmapSubresource(mOutlineLookupTex, 0);

			esrv.SetResource(mOutlineLookupSRV);

			esrv.Dispose();
			ev.Dispose();
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
				gd.DC.OutputMerger.SetRenderTargets(null, (RenderTargetView)null);
			}
			else if(targName == "null")
			{
				gd.DC.OutputMerger.SetRenderTargets(null, (RenderTargetView)null);
			}
			else if(mPostTargets.ContainsKey(targName)
				&& mPostDepths.ContainsKey(depthName))
			{
				gd.DC.OutputMerger.SetRenderTargets(mPostDepths[depthName], mPostTargets[targName]);
			}
			else if(mPostTargets.ContainsKey(targName)
				&& depthName == "null")
			{
				gd.DC.OutputMerger.SetRenderTargets(null, mPostTargets[targName]);
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
				gd.DC.OutputMerger.SetRenderTargets(null, (RenderTargetView)null);
			}
			else if(targName1 == "null")
			{
				gd.DC.OutputMerger.SetRenderTargets(null, (RenderTargetView)null);
			}
			else if(mPostTargets.ContainsKey(targName1)
				&& mPostTargets.ContainsKey(targName2)
				&& mPostDepths.ContainsKey(depthName))
			{
				gd.DC.OutputMerger.SetTargets(mPostDepths[depthName],
					mPostTargets[targName1], mPostTargets[targName2]);
			}
			else if(mPostTargets.ContainsKey(targName1)
				&& mPostTargets.ContainsKey(targName2)
				&& depthName == "null")
			{
				gd.DC.OutputMerger.SetTargets(mPostTargets[targName1], mPostTargets[targName2]);
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


		public void SetParameter(string paramName, string targName)
		{
			EffectVariable	ev	=mPostFX.GetVariableByName(paramName);
			if(ev == null)
			{
				return;
			}

			EffectShaderResourceVariable	esrv	=ev.AsShaderResource();
			if(esrv == null)
			{
				ev.Dispose();
				return;
			}

			if(targName == null || targName == "null" || targName == "")
			{
				esrv.SetResource(null);
			}
			else
			{
				esrv.SetResource(mPostTargSRVs[targName]);
			}

			esrv.Dispose();
			ev.Dispose();
		}


		public void DrawStage(GraphicsDevice gd, string technique)
		{
			gd.DC.InputAssembler.SetVertexBuffers(0, mQuadBinding);
			gd.DC.InputAssembler.SetIndexBuffer(mQuadIB, Format.R16_UInt, 0);

			EffectTechnique	et	=mPostFX.GetTechniqueByName(technique);

			if(!et.IsValid)
			{
				return;
			}

			EffectPass	ep	=et.GetPassByIndex(0);

			ep.Apply(gd.DC);

			gd.DC.DrawIndexed(6, 0, 0);

			ep.Dispose();
			et.Dispose();
		}


		public void FreeAll(GraphicsDevice gd)
		{
			//unwire from device resize stuff
			gd.ePreResize	-=OnPreResize;
			gd.eResized		-=OnResized;

			//dispose all views
			foreach(KeyValuePair<string, RenderTargetView> view in mPostTargets)
			{
				view.Value.Dispose();
			}
			foreach(KeyValuePair<string, DepthStencilView> view in mPostDepths)
			{
				view.Value.Dispose();
			}

			//dispose all srvs
			foreach(KeyValuePair<string, ShaderResourceView> srv in mPostTargSRVs)
			{
				srv.Value.Dispose();
			}

			//dispose all tex2ds
			foreach(KeyValuePair<string, Texture2D> tex in mPostTex2Ds)
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

			mPostFX.Dispose();

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

			RenderTargetView	backRTV	=mPostTargets["BackColor"];
			DepthStencilView	backDSV	=mPostDepths["BackDepth"];

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

			InitPostParams(gd.GD.FeatureLevel == FeatureLevel.Level_9_3);

			//back buffer will already be resized
			RenderTargetView	[]backBuf	=new RenderTargetView[1];
			DepthStencilView	backDepth;

			backBuf	=gd.DC.OutputMerger.GetRenderTargets(1, out backDepth);

			//dispose all views
			foreach(KeyValuePair<string, RenderTargetView> view in mPostTargets)
			{
				view.Value.Dispose();
			}
			foreach(KeyValuePair<string, DepthStencilView> view in mPostDepths)
			{
				view.Value.Dispose();
			}

			//dispose all srvs
			foreach(KeyValuePair<string, ShaderResourceView> srv in mPostTargSRVs)
			{
				srv.Value.Dispose();
			}

			mPostTargets.Clear();
			mPostDepths.Clear();
			mPostTargSRVs.Clear();

			//copy the descriptions
			Dictionary<string, Texture2DDescription>	descs	=new Dictionary<string, Texture2DDescription>();

			foreach(KeyValuePair<string, Texture2D> tex in mPostTex2Ds)
			{
				Texture2DDescription	resizeDesc	=new Texture2DDescription()
				{
					Format				=tex.Value.Description.Format,
					ArraySize			=tex.Value.Description.ArraySize,
					MipLevels			=tex.Value.Description.MipLevels,
					SampleDescription	=new SampleDescription(1, 0),
					Usage				=tex.Value.Description.Usage,
					BindFlags			=tex.Value.Description.BindFlags,
					CpuAccessFlags		=tex.Value.Description.CpuAccessFlags,
					OptionFlags			=tex.Value.Description.OptionFlags
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
			foreach(KeyValuePair<string, Texture2D> tex in mPostTex2Ds)
			{
				tex.Value.Dispose();
			}
			mPostTex2Ds.Clear();
			
			//make resized versions
			foreach(KeyValuePair<string, Texture2DDescription> texDesc in descs)
			{
				Texture2D	newTex	=new Texture2D(gd.GD, texDesc.Value);

				mPostTex2Ds.Add(texDesc.Key, newTex);

				if(Misc.bFlagSet((uint)newTex.Description.BindFlags, (uint)BindFlags.DepthStencil))
				{
					DepthStencilView	dsv	=new DepthStencilView(gd.GD, newTex);

					mPostDepths.Add(texDesc.Key, dsv);
				}
				else
				{
					RenderTargetView	rtv	=new RenderTargetView(gd.GD, newTex);
					ShaderResourceView	srv	=new ShaderResourceView(gd.GD, newTex);

					mPostTargets.Add(texDesc.Key, rtv);
					mPostTargSRVs.Add(texDesc.Key, srv);
				}
			}

			//readd the device backbuffer
			mPostTargets.Add("BackColor", backBuf[0]);
			mPostDepths.Add("BackDepth", backDepth);
		}
	}
}
