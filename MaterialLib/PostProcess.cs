using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;


namespace MaterialLib
{
	public class PostProcess
	{
		//targets for doing postery
		Dictionary<string, RenderTarget2D>	mPostTargets	=new Dictionary<string, RenderTarget2D>();

		//for a fullscreen quad
		VertexBuffer	mQuadVB;
		IndexBuffer		mQuadIB;

		//effect file that has most of the post stuff in it
		Effect	mPostFX;

		//stuff
		int		mResX, mResY;
		bool	mbReach;

		//gaussian blur stuff
		float	[]mSampleWeightsX;
		float	[]mSampleWeightsY;
		Vector2	[]mSampleOffsetsX;
		Vector2	[]mSampleOffsetsY;

		//constants
		const float	BlurAmount	=4f;


		public PostProcess(GraphicsDeviceManager gdm, ContentManager slib, int resx, int resy)
		{
			mbReach	=(gdm.GraphicsProfile == GraphicsProfile.Reach);

			if(mbReach)
			{
				mPostFX	=slib.Load<Effect>("Shaders/PostReach");
			}
			else
			{
				mPostFX	=slib.Load<Effect>("Shaders/Post");
			}

			mResX	=resx;
			mResY	=resy;

			MakeQuad(gdm.GraphicsDevice);

			InitPostParams();
		}


		public void MakePostTarget(GraphicsDevice gd, string name,
			int resx, int resy, SurfaceFormat surf, DepthFormat depth)
		{
			RenderTarget2D	rend	=new RenderTarget2D(gd, resx, resy, false, surf, depth);

			mPostTargets.Add(name, rend);
		}


		//hax for ludum dare
		public void SetIntensity(float val, float val2)
		{
			mPostFX.Parameters["mBloomIntensity"].SetValue(val);
			mPostFX.Parameters["mOpacity"].SetValue(val2);
		}


		//set up parameters with known values
		void InitPostParams()
		{
			mPostFX.Parameters["mTexelSteps"].SetValue(1.0f);
			mPostFX.Parameters["mThreshold"].SetValue(0.2f);
			mPostFX.Parameters["mScreenSize"].SetValue(new Vector2(mResX, mResY));
			mPostFX.Parameters["mOpacity"].SetValue(0.75f);

			//bloom settings
			mPostFX.Parameters["mBloomThreshold"].SetValue(0.25f);
			mPostFX.Parameters["mBloomIntensity"].SetValue(1.25f);
			mPostFX.Parameters["mBloomSaturation"].SetValue(1f);
			mPostFX.Parameters["mBaseIntensity"].SetValue(1f);
			mPostFX.Parameters["mBaseSaturation"].SetValue(1f);

			InitBlurParams(1.0f / (mResX / 2), 0, 0, 1.0f / (mResY / 2));

			//hidef can afford to store these once
			if(!mbReach)
			{
				SetBlurParams(true);
				SetBlurParams(false);
			}
		}


		void SetBlurParams(bool bX)
		{
			EffectParameter weightsParameterX, offsetsParameterX;
			EffectParameter weightsParameterY, offsetsParameterY;

			if(mbReach)
			{
				weightsParameterX	=weightsParameterY	=mPostFX.Parameters["mWeights"];
				offsetsParameterX	=offsetsParameterY	=mPostFX.Parameters["mOffsets"];
			}
			else
			{
				weightsParameterX	=mPostFX.Parameters["mWeightsX"];
				offsetsParameterX	=mPostFX.Parameters["mOffsetsX"];
				weightsParameterY	=mPostFX.Parameters["mWeightsY"];
				offsetsParameterY	=mPostFX.Parameters["mOffsetsY"];
			}

			if(bX)
			{
				weightsParameterX.SetValue(mSampleWeightsX);
				offsetsParameterX.SetValue(mSampleOffsetsX);
			}
			else
			{
				weightsParameterY.SetValue(mSampleWeightsY);
				offsetsParameterY.SetValue(mSampleOffsetsY);
			}
		}


		//from the xna bloom sample
		void InitBlurParams(float dxX, float dyX, float dxY, float dyY)
		{
			EffectParameter weightsParameterX, offsetsParameterX;
			EffectParameter weightsParameterY, offsetsParameterY;

			if(mbReach)
			{
				weightsParameterX	=weightsParameterY	=mPostFX.Parameters["mWeights"];
				offsetsParameterX	=offsetsParameterY	=mPostFX.Parameters["mOffsets"];
			}
			else
			{
				weightsParameterX	=mPostFX.Parameters["mWeightsX"];
				offsetsParameterX	=mPostFX.Parameters["mOffsetsX"];
				weightsParameterY	=mPostFX.Parameters["mWeightsY"];
				offsetsParameterY	=mPostFX.Parameters["mOffsetsY"];
			}
			
			//Look up how many samples our gaussian blur effect supports.
			int	sampleCountX	=weightsParameterX.Elements.Count;
			int	sampleCountY	=weightsParameterY.Elements.Count;
			
			//Create temporary arrays for computing our filter settings.
			mSampleWeightsX	=new float[sampleCountX];
			mSampleWeightsY	=new float[sampleCountY];
			mSampleOffsetsX	=new Vector2[sampleCountX];
			mSampleOffsetsY	=new Vector2[sampleCountY];
			
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

			mQuadVB	=new VertexBuffer(gd, typeof(VertexPositionTexture), 4, BufferUsage.WriteOnly);
			mQuadVB.SetData(verts);

			mQuadIB	=new IndexBuffer(gd, IndexElementSize.SixteenBits, 6, BufferUsage.WriteOnly);

			UInt16	[]inds	=new UInt16[6];
			inds[0]	=0;
			inds[1]	=1;
			inds[2]	=2;
			inds[3]	=1;
			inds[4]	=3;
			inds[5]	=2;

			mQuadIB.SetData(inds);
		}


		public void SetTarget(GraphicsDevice gd, string targName, bool bClear)
		{
			if(targName == "null")
			{
				gd.SetRenderTarget(null);
			}
			else if(!mPostTargets.ContainsKey(targName))
			{
				return;
			}
			else
			{
				gd.SetRenderTarget(mPostTargets[targName]);
			}

			if(bClear)
			{
				gd.Clear(Color.CornflowerBlue);
			}
		}


		public Texture2D GetTargetTexture(string targName)
		{
			return	mPostTargets[targName];
		}


		public void SetParameter(string paramName, string targName)
		{
			mPostFX.Parameters[paramName].SetValue(mPostTargets[targName]);
		}


		public void DrawStage(GraphicsDevice gd, string technique)
		{
			gd.SetVertexBuffer(mQuadVB);
			gd.Indices	=mQuadIB;

			if(mbReach && technique.StartsWith("GaussianBlur"))
			{
				mPostFX.CurrentTechnique	=mPostFX.Techniques["GaussianBlur"];

				SetBlurParams(technique.EndsWith("X"));
			}
			else
			{
				mPostFX.CurrentTechnique	=mPostFX.Techniques[technique];
			}

			mPostFX.CurrentTechnique.Passes[0].Apply();

			gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 4, 0, 2);
		}
	}
}
