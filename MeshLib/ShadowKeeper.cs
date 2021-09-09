using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UtilityLib;
using MaterialLib;

using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using SharpDX.Direct3D;

//ambiguous stuff
using Color		=SharpDX.Color;
using Device	=SharpDX.Direct3D11.Device;
using MatLib	=MaterialLib.MaterialLib;
using Resource	=SharpDX.Direct3D11.Resource;


namespace MeshLib
{
	public class ShadowKeeper
	{
		class ShadowCastingLight
		{
			internal Vector3	mPosition;
			internal float		mIntensity;
		}

		//all registered lights in the map
		List<ShadowCastingLight>	mCastingLights	=new List<ShadowCastingLight>();

		//this tracks lights processed or being processed this frame
		List<int>	mLightsBatched	=new List<int>();

		//active batch being used right now
		List<int>	mCurrentBatch	=new List<int>();

		List<StaticMesh>	mStaticShadowers	=new List<StaticMesh>();

		Texture2D			[]mBatchCubes;
		ShaderResourceView	[]mBatchSRVs;
		RenderTargetView	[]mBatchViews;

		//arrays to feed to shaders
		Matrix	[]mLightViews;
		Matrix	[]mLightProjs;
		Vector3	[]mLightPositions;

		//clear color, distant depth
		SharpDX.Mathematics.Interop.RawColor4	mClearColour
			=new SharpDX.Mathematics.Interop.RawColor4(
				float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue);

		const int	BatchSize		=24;
		const int	NumCubes		=BatchSize / 3;	//use 3 colors, alpha needed
		const float	MinIntensity	=10f;


		public void Initialize(GraphicsDevice gd, int bufferSizeXY)
		{
			//cubemap face matrixs
			mLightViews	=new Matrix[BatchSize * 6];
			mLightProjs	=new Matrix[BatchSize * 6];

			mLightPositions	=new Vector3[BatchSize];

			mBatchCubes	=new Texture2D[NumCubes];
			mBatchSRVs	=new ShaderResourceView[NumCubes];
			mBatchViews	=new RenderTargetView[NumCubes];

			mCastingLights.Clear();

			for(int i=0;i < NumCubes;i++)
			{
				SampleDescription	sampDesc	=new SampleDescription();
				sampDesc.Count		=1;
				sampDesc.Quality	=0;

				Texture2DDescription	texDesc	=new Texture2DDescription();
				texDesc.ArraySize			=6;
				texDesc.BindFlags			=BindFlags.RenderTarget | BindFlags.ShaderResource;
				texDesc.CpuAccessFlags		=CpuAccessFlags.None;
				texDesc.MipLevels			=1;
				texDesc.OptionFlags			=ResourceOptionFlags.TextureCube;
				texDesc.Usage				=ResourceUsage.Default;
				texDesc.Width				=bufferSizeXY;
				texDesc.Height				=bufferSizeXY;
				texDesc.Format				=Format.R16G16B16A16_Float;
				texDesc.SampleDescription	=sampDesc;

				mBatchCubes[i]	=new Texture2D(gd.GD, texDesc);
				mBatchSRVs[i]	=new ShaderResourceView(gd.GD, mBatchCubes[i]);

				RenderTargetViewDescription	rtvDesc	=new RenderTargetViewDescription();

				rtvDesc.Dimension		=RenderTargetViewDimension.Texture2DArray;
				rtvDesc.Format			=Format.R16G16B16A16_Float;

				rtvDesc.Texture2DArray.MipSlice		=0;
				rtvDesc.Texture2DArray.ArraySize	=6;

				mBatchViews[i]	=new RenderTargetView(gd.GD, mBatchCubes[i], rtvDesc);
			}
		}


		public void StartNewFrame()
		{
			mLightsBatched.Clear();
		}


		public void RegisterShadower(StaticMesh sm)
		{
			mStaticShadowers.Add(sm);
		}


		public int AddShadowCastingLight(Vector3 pos, float intensity)
		{
			ShadowCastingLight	scl	=new ShadowCastingLight();

			scl.mPosition	=pos;
			scl.mIntensity	=intensity;

			mCastingLights.Add(scl);

			return	mCastingLights.IndexOf(scl);
		}


		public void BatchShadowRender(GraphicsDevice			gd,
									  MaterialLib.MaterialLib	mats)
		{
			gd.DC.OutputMerger.SetTargets(mBatchViews);

			for(int i=0;i < NumCubes;i++)
			{
				gd.DC.ClearRenderTargetView(mBatchViews[i], mClearColour);
			}

			for(int i=0;i < BatchSize;i++)
			{
				int	batchIdx	=mCurrentBatch[i];
				if(batchIdx < 0)
				{
					continue;
				}
				if(batchIdx >= mCastingLights.Count)
				{
					continue;
				}

				ShadowCastingLight	scl	=mCastingLights[batchIdx];

				mLightPositions[i]	=scl.mPosition;

				//set cube face render mats
				Mathery.CreateCubeMapViewProjMatrix(TextureCubeFace.NegativeX,
					scl.mPosition, 500f, out mLightViews[i * 6], out mLightProjs[i * 6]);
				Mathery.CreateCubeMapViewProjMatrix(TextureCubeFace.NegativeY,
					scl.mPosition, 500f, out mLightViews[i * 6 + 1], out mLightProjs[i * 6 + 1]);
				Mathery.CreateCubeMapViewProjMatrix(TextureCubeFace.NegativeZ,
					scl.mPosition, 500f, out mLightViews[i * 6 + 2], out mLightProjs[i * 6 + 2]);
				Mathery.CreateCubeMapViewProjMatrix(TextureCubeFace.PositiveX,
					scl.mPosition, 500f, out mLightViews[i * 6 + 3], out mLightProjs[i * 6 + 3]);
				Mathery.CreateCubeMapViewProjMatrix(TextureCubeFace.PositiveY,
					scl.mPosition, 500f, out mLightViews[i * 6 + 4], out mLightProjs[i * 6 + 4]);
				Mathery.CreateCubeMapViewProjMatrix(TextureCubeFace.PositiveZ,
					scl.mPosition, 500f, out mLightViews[i * 6 + 5], out mLightProjs[i * 6 + 5]);
			}

			//set batch and cubmap face projs
			mats.SetMaterialParameter("ShadowBatch", "mLightViews", mLightViews);
			mats.SetMaterialParameter("ShadowBatch", "mLightProjs", mLightProjs);

			//set batch light positions
			mats.SetMaterialParameter("ShadowBatch", "mLightPosition", mLightPositions);

			for(int j=0;j < mStaticShadowers.Count;j++)
			{
				StaticMesh	shd	=mStaticShadowers[j];

				shd.DrawX(gd.DC, mats, 6 * mCurrentBatch.Count, "ShadowBatch");
			}
		}


		//this might be called multiple times in a frame
		//returns false if out of lights to process
		//figure out which 8 lights (or less) to gather into a batch
		public bool ComputeBatch(Vector3 playerPos)
		{
			mCurrentBatch.Clear();

			for(int i=0;i < BatchSize;i++)
			{
				float	bestDist	=float.MaxValue;
				int		bestIdx		=-1;

				for(int j=0;j < mCastingLights.Count;j++)
				{
					if(mCurrentBatch.Contains(j))
					{
						continue;
					}
					if(mLightsBatched.Contains(j))
					{
						continue;
					}

					if(mCastingLights[j].mIntensity <= MinIntensity)
					{
						continue;
					}

					float	dist	=Vector3.Distance(playerPos, mCastingLights[j].mPosition);

					dist	-=mCastingLights[j].mIntensity;

					if(dist < bestDist)
					{
						bestDist	=dist;
						bestIdx		=j;
					}
				}

				if(bestIdx >= 0)
				{
					mCurrentBatch.Add(bestIdx);
				}
			}

			if(mCurrentBatch.Count <= 0)
			{
				return	false;
			}

			//add to processing list
			mLightsBatched.AddRange(mCurrentBatch);

			return	true;
		}
	}
}