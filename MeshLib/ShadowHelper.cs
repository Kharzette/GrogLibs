using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UtilityLib;

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

		GraphicsDevice			mGD;
		Texture2D				mPShad;
		Texture2D				mPShadCube;
		RenderTargetView		mPShadView;
		RenderTargetView		mPShadCubeView;
		MaterialLib.PostProcess	mPost;

		//game data
		MatLib			mZoneMats;
		List<Shadower>	mShadowers		=new List<Shadower>();
		List<MatLib>	mShadowerMats	=new List<MaterialLib.MaterialLib>();
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
			MaterialLib.PostProcess post,
			GetCurrentShadowInfoFromLights gcsifl,
			GetTransformedBound gtb)
		{
			mShadowers.Clear();
			mShadowerMats.Clear();

			SampleDescription	sampDesc	=new SampleDescription();
			sampDesc.Count		=1;
			sampDesc.Quality	=0;

			Resource	res	=null;

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

			mPShadCubeView	=new RenderTargetView(gd.GD, mPShadCube);
			mPShadView		=new RenderTargetView(gd.GD, mPShad);

			mZoneMats				=zoneMats;
			mPost					=post;
			mGetShadInfo			=gcsifl;
			mGetTransformedBound	=gtb;
			mGD						=gd;
			mDirectionalAttenuation	=dirAtten;
		}


		public void RegisterShadower(Shadower mesh, MaterialLib.MaterialLib meshMats)
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
			mPost.SetTarget(mGD, "SceneColor", false);

			//draw shadow pass
			if(si.mbShadowNeeded)
			{
				mZoneMats.SetParameterOnAll("mShadowAtten", si.mShadowAtten);
				mZoneMats.SetParameterOnAll("mShadowLightPos", si.mShadowLightPos);
				mZoneMats.SetParameterOnAll("mShadowTexture", si.mShadowTexture);
				mZoneMats.SetParameterOnAll("mbDirectional", si.mbDirectional);

				if(si.mbDirectional)
				{
					mZoneMats.SetParameterOnAll("mLightViewProj", si.mLightViewProj);
				}
			}
		}


		ShadowInfo DrawShadow(int idx)
		{
			Vector3	lightPos, lightDir;
			Matrix	shadowerTransform;
			bool	bDirectional;
			float	intensity;

			Shadower				shadower	=mShadowers[idx];
			MaterialLib.MaterialLib	shadMats	=mShadowerMats[idx];

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

				RenderShadowCubeFace(shadower, shadMats, CubeMapFace.PositiveX, lightPos);
				RenderShadowCubeFace(shadower, shadMats, CubeMapFace.PositiveY, lightPos);
				RenderShadowCubeFace(shadower, shadMats, CubeMapFace.PositiveZ, lightPos);
				RenderShadowCubeFace(shadower, shadMats, CubeMapFace.NegativeX, lightPos);
				RenderShadowCubeFace(shadower, shadMats, CubeMapFace.NegativeY, lightPos);
				RenderShadowCubeFace(shadower, shadMats, CubeMapFace.NegativeZ, lightPos);

				si.mShadowLightPos	=lightPos;
				si.mShadowTexture	=mPShadCube;
				si.mShadowAtten		=intensity;
			}
			else
			{
				mGD.SetRenderTarget(mPShad);
				mGD.Clear(Color.White);

				Matrix	lightView, lightProj;
				Vector3	fakeOrigin;

				Mathery.CreateBoundedDirectionalOrthoViewProj(
					mGetTransformedBound(shadower), -lightDir,
					out lightView, out lightProj, out fakeOrigin);

				shadMats.SetMaterialParameter("Shadow", "mLightViewProj", lightView * lightProj);
				shadMats.SetMaterialParameter("Shadow", "mShadowLightPos", fakeOrigin);

				if(shadower.mChar != null)
				{
					shadower.mChar.Draw(mGD, "Shadow");
				}
				else
				{
					shadower.mStatic.Draw(mGD, "Shadow");
				}

				si.mLightViewProj	=lightView * lightProj;
				si.mShadowAtten		=mDirectionalAttenuation;
				si.mShadowTexture	=mPShad;
				si.mShadowLightPos	=fakeOrigin;
			}

			return	si;
		}


		void RenderShadowCubeFace(Shadower shadower,
			MaterialLib.MaterialLib shadMats,
			CubeMapFace face, Vector3 lightPos)
		{
			Matrix	lightView, lightProj;

			mGD.SetRenderTarget(mPShadCube, face);
			mGD.Clear(Color.White);

			Mathery.CreateCubeMapViewProjMatrix(face,
				lightPos, 500f, out lightView, out lightProj);

			shadMats.SetMaterialParameter("Shadow", "mLightViewProj", lightView * lightProj);

			if(shadower.mChar != null)
			{
				shadower.mChar.Draw(mGD, "Shadow");
			}
			else
			{
				shadower.mStatic.Draw(mGD, "Shadow");
			}
		}
	}
}
