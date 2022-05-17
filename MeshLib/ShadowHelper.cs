using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using Vortice.DXGI;
using Vortice.Mathematics;
using Vortice.Direct3D11;
using UtilityLib;
using MaterialLib;

using MatLib	=MaterialLib.MaterialLib;

namespace MeshLib;

public class ShadowHelper
{
	//render shadowing objects
	public delegate bool RenderShadows(int shadIndex);

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
		internal bool						mbShadowNeeded;
		internal bool						mbDirectional;
		internal Vector3					mShadowLightPos;
		internal ID3D11ShaderResourceView	mShadowTexture;
		internal float						mShadowAtten;
		internal Matrix4x4					mLightViewProj;

		internal ShadowInfo()
		{
			mbShadowNeeded	=false;
			mbDirectional	=false;
			mShadowLightPos	=Vector3.Zero;
			mShadowTexture	=null;
			mShadowAtten	=0f;
			mLightViewProj	=Matrix4x4.Identity;
		}
	}

	GraphicsDevice				mGD;
	ID3D11Texture2D				mPShad;
	ID3D11Texture2D				mPShadCube;
	ID3D11RenderTargetView		mPShadView;
	ID3D11RenderTargetView		[]mPShadCubeViews;
	ID3D11ShaderResourceView	mShad2D, mShadCube;
	PostProcess					mPost;

	//game data
	MatLib			mZoneMats;
	List<Shadower>	mShadowers		=new List<Shadower>();
	List<MatLib>	mShadowerMats	=new List<MatLib>();
	float			mDirectionalAttenuation;

	//delegates back to the game
	GetCurrentShadowInfoFromLights	mGetShadInfo;
	GetTransformedBound				mGetTransformedBound;

	public delegate bool GetCurrentShadowInfoFromLights(Shadower shadower,
		out Matrix4x4 shadowerTransform,
		out float intensity, out Vector3 lightPos,
		out Vector3 lightDir, out bool bDirectional);

	public delegate BoundingBox GetTransformedBound(Shadower shadower);

	//should match CommonFunctions.hlsl
	const int 	ShadowTextureRegisterIndex	=4;
	const int 	ShadowCubeRegisterIndex		=5;



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
		texDesc.CPUAccessFlags		=CpuAccessFlags.None;
		texDesc.MipLevels			=1;
		texDesc.MiscFlags			=ResourceOptionFlags.TextureCube;
		texDesc.Usage				=ResourceUsage.Default;
		texDesc.Width				=bufferSizeXY;
		texDesc.Height				=bufferSizeXY;
		texDesc.Format				=Format.R32_Float;
		texDesc.SampleDescription	=sampDesc;

		//for point lights
		mPShadCube	=gd.GD.CreateTexture2D(texDesc);
		mShadCube	=gd.GD.CreateShaderResourceView(mPShadCube);

		texDesc.ArraySize			=1;
		texDesc.MiscFlags			=ResourceOptionFlags.None;

		//directional
		mPShad	=gd.GD.CreateTexture2D(texDesc);
		mShad2D	=gd.GD.CreateShaderResourceView(mPShad);

		mPShadCubeViews	=new ID3D11RenderTargetView[6];

		RenderTargetViewDescription	rtvDesc	=new RenderTargetViewDescription();

		rtvDesc.ViewDimension	=RenderTargetViewDimension.Texture2DArray;
		rtvDesc.Format			=Format.R32_Float;

		rtvDesc.Texture2DArray.MipSlice		=0;
		rtvDesc.Texture2DArray.ArraySize	=1;

		for(int i=0;i < 6;i++)
		{
			rtvDesc.Texture2DArray.FirstArraySlice	=i;
			mPShadCubeViews[i]	=gd.GD.CreateRenderTargetView(mPShadCube, rtvDesc);
		}

		mPShadView		=gd.GD.CreateRenderTargetView(mPShad);

		mPShad.DebugName		="2DShadowBuf";
		mPShadCube.DebugName	="CubeShadowBuf";

		mZoneMats				=zoneMats;
		mPost					=post;
		mGetShadInfo			=gcsifl;
		mGetTransformedBound	=gtb;
		mGD						=gd;
		mDirectionalAttenuation	=dirAtten;
	}


	public void FreeAll()
	{
		mShad2D.Dispose();
		mShadCube.Dispose();

		mPShadView.Dispose();
		for(int i=0;i < 6;i++)
		{
			mPShadCubeViews[i].Dispose();
		}

		mPShad.Dispose();
		mPShadCube.Dispose();
	}


	public void RegisterShadower(Shadower mesh, MatLib meshMats)
	{
		mShadowers.Add(mesh);
		mShadowerMats.Add(meshMats);
	}


	public void UnRegisterShadower(Shadower shad)
	{
		int	idx	=mShadowers.IndexOf(shad);
		if(idx < 0)
		{
			return;
		}

		mShadowers.RemoveAt(idx);
		mShadowerMats.RemoveAt(idx);
	}


	public int GetShadowCount()
	{
		return	mShadowers.Count();
	}


	public bool DrawShadows(int shadIndex, StuffKeeper sk)
	{
		if(shadIndex < 0 || shadIndex >= mShadowers.Count)
		{
			return	false;
		}

		CBKeeper	cbk	=sk.GetCBKeeper();
		ShadowInfo	si	=DrawShadow(shadIndex, cbk);

		//set only the color portion for the shadow render
		mPost.SetTargets(mGD, "SceneColor", "SceneDepth");

		//draw shadow pass
		if(si.mbShadowNeeded)
		{
			cbk.SetPerShadow(si.mShadowLightPos, si.mbDirectional, si.mShadowAtten);

			if(si.mbDirectional)
			{
				cbk.SetLightViewProj(si.mLightViewProj);
				mGD.DC.PSSetShaderResource(ShadowTextureRegisterIndex,
					si.mShadowTexture);
			}
			else
			{
				mGD.DC.PSSetShaderResource(ShadowCubeRegisterIndex, si.mShadowTexture);
			}
		}
		return	si.mbShadowNeeded;
	}


	ShadowInfo DrawShadow(int idx, CBKeeper cbk)
	{
		Vector3	lightPos, lightDir;
		Matrix4x4	shadowerTransform;
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

		cbk.SetPerShadowDirectional(bDirectional);

		if(!bDirectional)
		{
			cbk.SetPerShadowLightPos(lightPos);

			cbk.UpdatePerShadow(mGD.DC);
			cbk.SetPerShadowToShaders(mGD.DC);

			RenderShadowCubeFace(shadower, cbk, shadMats, 0, TextureCubeFace.PositiveX, lightPos);
			RenderShadowCubeFace(shadower, cbk, shadMats, 1, TextureCubeFace.NegativeX, lightPos);
			RenderShadowCubeFace(shadower, cbk, shadMats, 2, TextureCubeFace.PositiveY, lightPos);
			RenderShadowCubeFace(shadower, cbk, shadMats, 3, TextureCubeFace.NegativeY, lightPos);
			RenderShadowCubeFace(shadower, cbk, shadMats, 4, TextureCubeFace.PositiveZ, lightPos);
			RenderShadowCubeFace(shadower, cbk, shadMats, 5, TextureCubeFace.NegativeZ, lightPos);

			si.mShadowLightPos	=lightPos;
			si.mShadowTexture	=mShadCube;
			si.mShadowAtten		=intensity;
		}
		else
		{
			mGD.DC.OMSetRenderTargets(mPShadView);
			mGD.DC.ClearRenderTargetView(mPShadView,
				Misc.SystemColorToDXColor(System.Drawing.Color.White));

			Matrix4x4	lightView, lightProj;
			Vector3	fakeOrigin;

			Mathery.CreateBoundedDirectionalOrthoViewProj(
				mGetTransformedBound(shadower), lightDir,
				out lightView, out lightProj, out fakeOrigin);

			cbk.SetLightViewProj(lightView * lightProj);
			cbk.SetPerShadowLightPos(fakeOrigin);

			cbk.UpdatePerShadow(mGD.DC);
			cbk.SetPerShadowToShaders(mGD.DC);

			if(shadower.mChar != null)
			{
				shadower.mChar.Draw(mGD.DC, cbk, "Shadow");
			}
			else
			{
				shadower.mStatic.Draw(mGD.DC, shadMats, "Shadow");
			}

			si.mLightViewProj	=lightView * lightProj;
			si.mShadowAtten		=mDirectionalAttenuation;
			si.mShadowTexture	=mShad2D;
			si.mShadowLightPos	=fakeOrigin;
		}

		return	si;
	}


	void RenderShadowCubeFace(Shadower shadower, CBKeeper cbk,
		MatLib shadMats, int index,	TextureCubeFace face, Vector3 lightPos)
	{
		Matrix4x4	lightView, lightProj;

		mGD.DC.OMSetRenderTargets(mPShadCubeViews[index]);
		mGD.DC.ClearRenderTargetView(mPShadCubeViews[index],
			Misc.SystemColorToDXColor(System.Drawing.Color.White));

		Mathery.CreateCubeMapViewProjMatrix(face,
			lightPos, 500f, out lightView, out lightProj);

		cbk.SetLightViewProj(lightView * lightProj);

		cbk.UpdateFrame(mGD.DC);
		cbk.SetCommonCBToShaders(mGD.DC);

		if(shadower.mChar != null)
		{
			shadower.mChar.Draw(mGD.DC, cbk, "Shadow");
		}
		else
		{
			shadower.mStatic.Draw(mGD.DC, shadMats, "Shadow");
		}
	}
}