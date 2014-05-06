using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UtilityLib;
using MaterialLib;

using SharpDX;
using SharpDX.DXGI;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.Direct3D;

//ambiguous stuff
using Buffer	=SharpDX.Direct3D11.Buffer;
using Color		=SharpDX.Color;
using Device	=SharpDX.Direct3D11.Device;
using MatLib	=MaterialLib.MaterialLib;
using Resource	=SharpDX.Direct3D11.Resource;


namespace MeshLib
{
	public class ShadowHelper
	{
		//need a struct to help game keep track of instances
		public class Shadower
		{
			//will be one of these, other will be null
			public StaticMesh	mStatic;
			public Character	mChar;

			//game specific info for differentiating instances and such
			public object	mContext;
		}

		//for keeping track of shadows
		class ShadowInfo
		{
			internal bool		mbShadowNeeded;
			internal bool		mbDirectional;
			internal Vector3	mShadowLightPos;
			internal Texture2D	mShadowTexture;
			internal float		mShadowAtten;
			internal Matrix		mLightViewProj;

			internal ShadowInfo()
			{
				mbShadowNeeded	=false;
				mbDirectional	=false;
				mShadowLightPos	=Vector3.Zero;
				mShadowTexture	=null;
				mShadowAtten	=0f;
				mLightViewProj	=Matrix.Identity;
			}
		}

		GraphicsDevice		mGD;
		Texture2D			mPShad;
		Texture2D			mPShadCube;
		RenderTargetView	mPShadView;
		RenderTargetView	[]mPShadCubeViews;
		ShaderResourceView	mShad2D, mShadCube;
		PostProcess			mPost;

		//game data
		MatLib			mZoneMats;
		List<Shadower>	mShadowers		=new List<Shadower>();
		List<MatLib>	mShadowerMats	=new List<MatLib>();
		float			mDirectionalAttenuation;

		//delegates back to the game
		GetCurrentShadowInfoFromLights	mGetShadInfo;
		GetTransformedBound				mGetTransformedBound;

		public delegate bool GetCurrentShadowInfoFromLights(Shadower shadower,
			out Matrix shadowerTransform,
			out float intensity, out Vector3 lightPos,
			out Vector3 lightDir, out bool bDirectional);

		public delegate BoundingBox GetTransformedBound(Shadower shadower);



		public ShadowHelper()
		{
		}


		public void Initialize(GraphicsDevice gd,
			int bufferSizeXY, float dirAtten,
			MatLib zoneMats,
			PostProcess post,
			GetCurrentShadowInfoFromLights gcsifl,
			GetTransformedBound gtb)
		{
			mShadowers.Clear();
			mShadowerMats.Clear();

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
			texDesc.Format				=Format.R16_UNorm;
			texDesc.SampleDescription	=sampDesc;

			//for point lights
			mPShadCube	=new Texture2D(gd.GD, texDesc);

			texDesc.ArraySize			=1;
			texDesc.OptionFlags			=ResourceOptionFlags.None;

			//directional
			mPShad	=new Texture2D(gd.GD, texDesc);

			mPShadCubeViews	=new RenderTargetView[6];

			RenderTargetViewDescription	rtvDesc	=new RenderTargetViewDescription();

			rtvDesc.Dimension		=RenderTargetViewDimension.Texture2DArray;
			rtvDesc.Format			=Format.R16_UNorm;

			rtvDesc.Texture2DArray.MipSlice		=0;
			rtvDesc.Texture2DArray.ArraySize	=1;

			for(int i=0;i < 6;i++)
			{
				rtvDesc.Texture2DArray.FirstArraySlice	=i;
				mPShadCubeViews[i]	=new RenderTargetView(gd.GD, mPShadCube, rtvDesc);
			}

			mPShadView		=new RenderTargetView(gd.GD, mPShad);

			mZoneMats				=zoneMats;
			mPost					=post;
			mGetShadInfo			=gcsifl;
			mGetTransformedBound	=gtb;
			mGD						=gd;
			mDirectionalAttenuation	=dirAtten;
		}


		public void RegisterShadower(Shadower mesh, MatLib meshMats)
		{
			mShadowers.Add(mesh);
			mShadowerMats.Add(meshMats);
		}


		public void RenderShadows(int shadIndex)
		{
			if(shadIndex < 0 || shadIndex >= mShadowers.Count)
			{
				return;
			}

			ShadowInfo	si	=DrawShadow(shadIndex);

			//set only the color portion for the shadow render
			mPost.SetTargets(mGD, "SceneColor", "SceneDepth");

			//draw shadow pass
			if(si.mbShadowNeeded)
			{
				mZoneMats.SetParameterForAll("mShadowAtten", si.mShadowAtten);
				mZoneMats.SetParameterForAll("mShadowLightPos", si.mShadowLightPos);
				mZoneMats.SetParameterForAll("mShadowTexture", si.mShadowTexture);
				mZoneMats.SetParameterForAll("mbDirectional", si.mbDirectional);

				if(si.mbDirectional)
				{
					mZoneMats.SetParameterForAll("mLightViewProj", si.mLightViewProj);
				}
			}
		}


		ShadowInfo DrawShadow(int idx)
		{
			Vector3	lightPos, lightDir;
			Matrix	shadowerTransform;
			bool	bDirectional;
			float	intensity;

			Shadower	shadower	=mShadowers[idx];
			MatLib		shadMats	=mShadowerMats[idx];

			bool	bShad	=mGetShadInfo(shadower, out shadowerTransform,
				out intensity, out lightPos, out lightDir, out bDirectional);

			ShadowInfo	si	=new ShadowInfo();

			si.mbDirectional	=bDirectional;
			si.mbShadowNeeded	=bShad;

			if(!bShad)
			{
				return	si;
			}

			//set instance transform
			if(shadower.mStatic != null)
			{
				shadower.mStatic.SetTransform(shadowerTransform);
			}
			else
			{
				shadower.mChar.SetTransform(shadowerTransform);
			}

			shadMats.SetMaterialParameter("Shadow", "mbDirectional", bDirectional);

			if(!bDirectional)
			{
				shadMats.SetMaterialParameter("Shadow", "mShadowLightPos", lightPos);

				RenderShadowCubeFace(shadower, shadMats, 0, TextureCubeFace.PositiveX, lightPos);
				RenderShadowCubeFace(shadower, shadMats, 1, TextureCubeFace.NegativeX, lightPos);
				RenderShadowCubeFace(shadower, shadMats, 2, TextureCubeFace.PositiveY, lightPos);
				RenderShadowCubeFace(shadower, shadMats, 3, TextureCubeFace.NegativeY, lightPos);
				RenderShadowCubeFace(shadower, shadMats, 4, TextureCubeFace.PositiveZ, lightPos);
				RenderShadowCubeFace(shadower, shadMats, 5, TextureCubeFace.NegativeZ, lightPos);

				si.mShadowLightPos	=lightPos;
				si.mShadowTexture	=mPShadCube;
				si.mShadowAtten		=intensity;
			}
			else
			{
				mGD.DC.OutputMerger.SetRenderTargets(mPShadView);
				mGD.DC.ClearRenderTargetView(mPShadView, Color.White);

				Matrix	lightView, lightProj;
				Vector3	fakeOrigin;

				Mathery.CreateBoundedDirectionalOrthoViewProj(
					mGetTransformedBound(shadower), -lightDir,
					out lightView, out lightProj, out fakeOrigin);

				shadMats.SetMaterialParameter("Shadow", "mLightViewProj", lightView * lightProj);
				shadMats.SetMaterialParameter("Shadow", "mShadowLightPos", fakeOrigin);

				if(shadower.mChar != null)
				{
					shadower.mChar.Draw(mGD.DC, shadMats, "Shadow");
				}
				else
				{
					shadower.mStatic.Draw(mGD.DC, shadMats, "Shadow");
				}

				si.mLightViewProj	=lightView * lightProj;
				si.mShadowAtten		=mDirectionalAttenuation;
				si.mShadowTexture	=mPShad;
				si.mShadowLightPos	=fakeOrigin;
			}

			return	si;
		}


		void RenderShadowCubeFace(Shadower shadower,
			MatLib shadMats, int index,
			TextureCubeFace face, Vector3 lightPos)
		{
			Matrix	lightView, lightProj;

			mGD.DC.OutputMerger.SetTargets(mPShadCubeViews[index]);
			mGD.DC.ClearRenderTargetView(mPShadCubeViews[index], Color.White);

			Mathery.CreateCubeMapViewProjMatrix(face,
				lightPos, 500f, out lightView, out lightProj);

			shadMats.SetMaterialParameter("Shadow", "mLightViewProj", lightView * lightProj);

			if(shadower.mChar != null)
			{
				shadower.mChar.Draw(mGD.DC, shadMats, "Shadow");
			}
			else
			{
				shadower.mStatic.Draw(mGD.DC, shadMats, "Shadow");
			}
		}
	}
}
