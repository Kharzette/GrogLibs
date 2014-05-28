using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using UtilityLib;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

using Buffer	=SharpDX.Direct3D11.Buffer;


namespace MaterialLib
{
	public class PostProcess
	{
		internal struct VertexPositionTexture
		{
			internal Vector3	Position;
			internal Vector2	TextureCoordinate;
		}

		//targets for doing postery
		Dictionary<string, RenderTargetView>	mPostTargets	=new Dictionary<string, RenderTargetView>();
		Dictionary<string, DepthStencilView>	mPostDepths		=new Dictionary<string, DepthStencilView>();
		Dictionary<string, ShaderResourceView>	mPostTargSRVs	=new Dictionary<string, ShaderResourceView>();

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
		Vector2	[]mSampleOffsetsX;
		Vector2	[]mSampleOffsetsY;

		//datastream for setting above arrays
		DataStream	mSampleOffsetsXDS, mSampleOffsetsYDS;

		//constants
		const float	BlurAmount	=4f;


		public PostProcess(GraphicsDevice gd, Effect fx, int resx, int resy,
			RenderTargetView backBuffer, DepthStencilView backDepth)
		{
			mPostFX	=fx;

			mResX		=resx;
			mResY		=resy;
			mClearColor	=Color.CornflowerBlue;

			mPostTargets.Add("BackColor", backBuffer);
			mPostDepths.Add("BackDepth", backDepth);

			MakeQuad(gd);

			InitPostParams(gd.GD.FeatureLevel == FeatureLevel.Level_9_3);
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

			mPostDepths.Add(name, targView);
		}


		//set up parameters with known values
		void InitPostParams(bool bNineThree)
		{
			mPostFX.GetVariableByName("mTexelSteps").AsScalar().Set(1f);
			mPostFX.GetVariableByName("mThreshold").AsScalar().Set(0.01f);
			mPostFX.GetVariableByName("mScreenSize").AsVector().Set(new Vector2(mResX, mResY));
			mPostFX.GetVariableByName("mInvViewPort").AsVector().Set(new Vector2(1f / mResX, 1f / mResY));
			mPostFX.GetVariableByName("mOpacity").AsScalar().Set(0.75f);

			//bloomstuffs
			mPostFX.GetVariableByName("mBloomThreshold").AsScalar().Set(0.25f);
			mPostFX.GetVariableByName("mBloomIntensity").AsScalar().Set(1.25f);
			mPostFX.GetVariableByName("mBloomSaturation").AsScalar().Set(1f);
			mPostFX.GetVariableByName("mBaseIntensity").AsScalar().Set(1f);
			mPostFX.GetVariableByName("mBaseSaturation").AsScalar().Set(1f);

			InitBlurParams(1.0f / (mResX / 2), 0, 0, 1.0f / (mResY / 2), bNineThree);

			//hidef can afford to store these once
			SetBlurParams(true);
			SetBlurParams(false);
		}


		void SetBlurParams(bool bX)
		{
			EffectScalarVariable	weightsX	=mPostFX.GetVariableByName("mWeightsX").AsScalar();
			EffectScalarVariable	weightsY	=mPostFX.GetVariableByName("mWeightsX").AsScalar();
			EffectVectorVariable	offsetsX	=mPostFX.GetVariableByName("mOffsetsX").AsVector();
			EffectVectorVariable	offsetsY	=mPostFX.GetVariableByName("mOffsetsY").AsVector();

			if(bX)
			{
				weightsX.Set(mSampleWeightsX);
				offsetsX.SetRawValue(mSampleOffsetsXDS, mSampleOffsetsX.Length * 8);
			}
			else
			{
				weightsY.Set(mSampleWeightsY);
				offsetsY.SetRawValue(mSampleOffsetsYDS, mSampleOffsetsY.Length * 8);
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
			mSampleOffsetsX	=new Vector2[sampleCountX];
			mSampleOffsetsY	=new Vector2[sampleCountY];
			
			//stupid effect stuff has no array capability for vector2
			//pain in the ass
			mSampleOffsetsXDS	=new DataStream(sampleCountX * 8, true, true);
			mSampleOffsetsYDS	=new DataStream(sampleCountY * 8, true, true);

			//The first sample always has a zero offset.
			mSampleWeightsX[0]	=ComputeGaussian(0);
			mSampleOffsetsX[0]	=new Vector2(0);
			mSampleWeightsY[0]	=ComputeGaussian(0);
			mSampleOffsetsY[0]	=new Vector2(0);
			
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
				mSampleOffsetsX[i * 2 + 1]	=deltaX;
				mSampleOffsetsX[i * 2 + 2]	=-deltaX;
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
				mSampleOffsetsY[i * 2 + 1]	=deltaY;
				mSampleOffsetsY[i * 2 + 2]	=-deltaY;
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

			//write to a datastream
			for(int i=0;i < sampleCountX;i++)
			{
				mSampleOffsetsXDS.Write(mSampleOffsetsX[i]);
				mSampleOffsetsYDS.Write(mSampleOffsetsY[i]);
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
			if(targName == "null")
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


//		public Texture2D GetTargetTexture(string targName)
//		{
//			return	mPostTargets[targName];
//		}


		public void SetParameter(string paramName, string targName)
		{
			mPostFX.GetVariableByName(paramName).AsShaderResource().SetResource(mPostTargSRVs[targName]);
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
		}
	}
}
