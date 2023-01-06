//#define PIXGOBLINRY	//use this to remove alpha sorting, which somehow makes pix crash
using System;
using System.IO;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using Vortice.Direct3D11;
using UtilityLib;
using MaterialLib;


using MatLib	=MaterialLib.MaterialLib;

namespace MeshLib;

//draw bits of indoor scenery
/*
public class IndoorMesh
{
	//vertex
	ID3D11Buffer	mLMVB, mVLitVB, mLMAnimVB, mAlphaVB, mSkyVB;
	ID3D11Buffer	mFBVB, mMirrorVB, mLMAVB, mLMAAnimVB;

	//index
	ID3D11Buffer	mLMIB, mVLitIB, mLMAnimIB, mAlphaIB;
	ID3D11Buffer	mSkyIB, mFBIB, mMirrorIB, mLMAIB, mLMAAnimIB;

	//vert copies saved for writing (for editor)
	Array	mLMVerts, mVLitVerts, mLMAnimVerts;
	Array	mAlphaVerts, mSkyVerts, mFBVerts;
	Array	mMirrorVerts, mLMAVerts, mLMAAnimVerts;

	//index copies saved for writing (for editor)
	UInt16[]	mLMInds, mVLitInds, mLMAnimInds;
	UInt16[]	mAlphaInds, mSkyInds, mFBInds;
	UInt16[]	mMirrorInds, mLMAInds, mLMAAnimInds;

	//vert type index for writing
	int	mLMIndex, mVLitIndex, mLMAnimIndex, mAlphaIndex, mSkyIndex;
	int	mFBIndex, mMirrorIndex, mLMAIndex, mLMAAnimIndex;

	//material library reference
	MatLib	mMatLib;

	//light map atlas
	TexAtlas	mLightMapAtlas;

	//lightmap animation stuff
	Dictionary<int, string>	mStyles				=new Dictionary<int, string>();
	Dictionary<int, float>	mCurStylePos		=new Dictionary<int, float>();
	bool					[]mSwitches			=new bool[32];	//switchable on / off
	float					[]mAniIntensities	=new float[44];

	//constants
	const float		ThirtyFPS	=(1000.0f / 30.0f);

	//delegates
	public delegate bool IsMaterialVisible(Vector3 eyePos, int matIdx);
	public delegate Matrix4x4 GetModelMatrix(int modelIndex);

	//render external stuff
	public delegate void RenderExternal(GameCamera gcam);
	public delegate void RenderExternalDMN(GameCamera gcam);



	public IndoorMesh(GraphicsDevice gd, MatLib matLib)
	{
		mMatLib	=matLib;

		//Quake1 styled lights 'a' is total darkness, 'z' is maxbright.
		//0 normal
		mStyles.Add(0, "z");
			
		//1 FLICKER (first variety)
		mStyles.Add(1, "mmnmmommommnonmmonqnmmo");
		
		//2 SLOW STRONG PULSE
		mStyles.Add(2, "abcdefghijklmnopqrstuvwxyzyxwvutsrqponmlkjihgfedcba");
		
		//3 CANDLE (first variety)
		mStyles.Add(3, "mmmmmaaaaammmmmaaaaaabcdefgabcdefg");
		
		//4 FAST STROBE
		mStyles.Add(4, "mamamamamama");
		
		//5 GENTLE PULSE 1
		mStyles.Add(5,"jklmnopqrstuvwxyzyxwvutsrqponmlkj");
		
		//6 FLICKER (second variety)
		mStyles.Add(6, "nmonqnmomnmomomno");
		
		//7 CANDLE (second variety)
		mStyles.Add(7, "mmmaaaabcdefgmmmmaaaammmaamm");
		
		//8 CANDLE (third variety)
		mStyles.Add(8, "mmmaaammmaaammmabcdefaaaammmmabcdefmmmaaaa");
		
		//9 SLOW STROBE (fourth variety)
		mStyles.Add(9, "aaaaaaaazzzzzzzz");
		
		//10 FLUORESCENT FLICKER
		mStyles.Add(10, "mmamammmmammamamaaamammma");

		//11 SLOW PULSE NOT FADE TO BLACK
		mStyles.Add(11, "abcdefghijklmnopqrrqponmlkjihgfedcba");
		
		//12 UNDERWATER LIGHT MUTATION
		//this light only distorts the lightmap - no contribution
		//is made to the brightness of affected surfaces
		mStyles.Add(12, "mmnnmmnnnmmnn");

		mCurStylePos.Add(0, 0.0f);
		mCurStylePos.Add(1, 0.0f);
		mCurStylePos.Add(2, 0.0f);
		mCurStylePos.Add(3, 0.0f);
		mCurStylePos.Add(4, 0.0f);
		mCurStylePos.Add(5, 0.0f);
		mCurStylePos.Add(6, 0.0f);
		mCurStylePos.Add(7, 0.0f);
		mCurStylePos.Add(8, 0.0f);
		mCurStylePos.Add(9, 0.0f);
		mCurStylePos.Add(10, 0.0f);
		mCurStylePos.Add(11, 0.0f);
		mCurStylePos.Add(12, 0.0f);
	}


	public void FinishAtlas(GraphicsDevice gd, StuffKeeper sk)
	{
		if(mLightMapAtlas == null)
		{
			return;
		}

		mLightMapAtlas.Finish(gd, sk, "LightMapAtlas");
		sk.AddMap("LightMapAtlas", mLightMapAtlas.GetAtlasSRV());
	}


	public void FreeAll()
	{
		if(mLMVB != null)		mLMVB.Dispose();
		if(mVLitVB != null)		mVLitVB.Dispose();
		if(mLMAnimVB != null)	mLMAnimVB.Dispose();
		if(mAlphaVB != null)	mAlphaVB.Dispose();
		if(mSkyVB != null)		mSkyVB.Dispose();
		if(mFBVB != null)		mFBVB.Dispose();
		if(mMirrorVB != null)	mMirrorVB.Dispose();
		if(mLMAVB != null)		mLMAVB.Dispose();
		if(mLMAAnimVB != null)	mLMAAnimVB.Dispose();
		
		if(mLMIB != null)		mLMIB.Dispose();
		if(mVLitIB != null)		mVLitIB.Dispose();
		if(mLMAnimIB != null)	mLMAnimIB.Dispose();
		if(mAlphaIB != null)	mAlphaIB.Dispose();
		if(mSkyIB != null)		mSkyIB.Dispose();
		if(mFBIB != null)		mFBIB.Dispose();
		if(mMirrorIB != null)	mMirrorIB.Dispose();
		if(mLMAIB != null)		mLMAIB.Dispose();
		if(mLMAAnimIB != null)	mLMAAnimIB.Dispose();

		if(mLightMapAtlas != null)	mLightMapAtlas.FreeAll();
	}


	public void Update(float msDelta)
	{
		Debug.Assert(msDelta > 0f);

		UpdateAnimatedLightMaps(msDelta);
	}


	//draw depth, material id, and normal
	//assumes the proper target is set
	public void DrawDMN(GraphicsDevice gd,
		IsMaterialVisible bMatVis,
		GetModelMatrix getModMatrix,
		RenderExternalDMN rendExternalDMN)
	{
		//update materiallib wvp
//		mMatLib.UpdateWVP(Matrix4x4.Identity, gd.GCam.View, gd.GCam.Projection, gd.GCam.Position);

		//draw solids first
		DrawMaterialsDC(gd, 2, getModMatrix, mFBVBB, mFBIB, mFBDrawCalls, bMatVis);
		DrawMaterialsDC(gd, 2, getModMatrix, mVLitVBB, mVLitIB, mVLitDrawCalls, bMatVis);
		DrawMaterialsDC(gd, 2, getModMatrix, mLMVBB, mLMIB, mLMDrawCalls, bMatVis);
		DrawMaterialsDC(gd, 2, getModMatrix, mLMAnimVBB, mLMAnimIB, mLMAnimDrawCalls, bMatVis);

		//draw alphas
		DrawMaterialsDC(gd, 2, getModMatrix, mAlphaVBB, mAlphaIB, mAlphaDrawCalls, bMatVis);
		DrawMaterialsDC(gd, 2, getModMatrix, mLMAVBB, mLMAIB, mLMADrawCalls, bMatVis);
		DrawMaterialsDC(gd, 2, getModMatrix, mLMAAnimVBB, mLMAAnimIB, mLMAAnimDrawCalls, bMatVis);

		//draw outside stuff
		rendExternalDMN(gd.GCam);

		//sky doesn't have a shadow draw pass
		DrawMaterialsDC(gd, 1, getModMatrix, mSkyVBB, mSkyIB, mSkyDrawCalls, bMatVis);
	}


	//helpful overload for doing vis testing
	public void Draw(GraphicsDevice gd,
		int numShadows,
		IsMaterialVisible bMatVis,
		GetModelMatrix getModMatrix,
		RenderExternal rendExternal,
		ShadowHelper.RenderShadows renderShadows,
		SetUpAlphaRenderTargets setUpAlphaTargets)
	{
		//update materiallib wvp
		mMatLib.UpdateWVP(Matrix.Identity, gd.GCam.View, gd.GCam.Projection, gd.GCam.Position);

//			gd.Clear(Color.CornflowerBlue);

		//draw solids first
		DrawMaterialsDC(gd, 0, getModMatrix, mFBVBB, mFBIB, mFBDrawCalls, bMatVis);
		DrawMaterialsDC(gd, 0, getModMatrix, mVLitVBB, mVLitIB, mVLitDrawCalls, bMatVis);
		DrawMaterialsDC(gd, 0, getModMatrix, mSkyVBB, mSkyIB, mSkyDrawCalls, bMatVis);
		DrawMaterialsDC(gd, 0, getModMatrix, mLMVBB, mLMIB, mLMDrawCalls, bMatVis);
		DrawMaterialsDC(gd, 0, getModMatrix, mLMAnimVBB, mLMAnimIB, mLMAnimDrawCalls, bMatVis);

		//draw shadows
		for(int i=0;i < numShadows;i++)
		{
			gd.SetShadowViewPort();

			//draw shad and set up materials for second pass
			bool	bShadDrawn	=renderShadows(i);

			gd.SetScreenViewPort();

			if(!bShadDrawn)
			{
				continue;
			}

			//draw second pass with shadowing
			DrawMaterialsDC(gd, 1, getModMatrix, mFBVBB, mFBIB, mFBDrawCalls, bMatVis);
			DrawMaterialsDC(gd, 1, getModMatrix, mVLitVBB, mVLitIB, mVLitDrawCalls, bMatVis);
			DrawMaterialsDC(gd, 1, getModMatrix, mLMVBB, mLMIB, mLMDrawCalls, bMatVis);
			DrawMaterialsDC(gd, 1, getModMatrix, mLMAnimVBB, mLMAnimIB, mLMAnimDrawCalls, bMatVis);

			//without this, you get a bunch of that annoying spam about
			//rendertarget still set to a resource etc etc
			mMatLib.ClearResourceParameter(gd.DC, "BSP.fx", "FullBright", "mShadowTexture");
			mMatLib.ClearResourceParameter(gd.DC, "BSP.fx", "FullBright", "mShadowCube");
		}

		//reset targets back in case shadow pass changed them
		setUpAlphaTargets();

		//draw alphas
		DrawMaterialsDC(gd, 0, getModMatrix, mAlphaVBB, mAlphaIB, mAlphaDrawCalls, bMatVis);
		DrawMaterialsDC(gd, 0, getModMatrix, mLMAVBB, mLMAIB, mLMADrawCalls, bMatVis);
		DrawMaterialsDC(gd, 0, getModMatrix, mLMAAnimVBB, mLMAAnimIB, mLMAAnimDrawCalls, bMatVis);

		//draw outside stuff
		rendExternal(mAlphaPool, gd.GCam);
		mAlphaPool.DrawAll(gd);
	}


	public float GetStyleStrength(int styleIndex)
	{
		if(styleIndex <= 0)
		{
			return	1f;	//0 is always fully on
		}

		if(styleIndex > 12)
		{
			return	1f;	//only 12 animated, switchable handled elsewhere
		}

		return	ComputeStyleStrength(mCurStylePos[styleIndex], mStyles[styleIndex]);
	}


	float ComputeStyleStrength(float stylePos, string styleString)
	{
		int	curPos	=(int)Math.Floor(stylePos / ThirtyFPS);

		float	val		=StyleVal(styleString.Substring(curPos, 1));
		float	nextVal	=StyleVal(styleString.Substring((curPos + 1) % styleString.Length, 1));

		float	ratio	=stylePos - (curPos * ThirtyFPS);

		ratio	/=ThirtyFPS;

		return	MathUtil.Lerp(val, nextVal, ratio);
	}


	//for opaques with models
	void DrawMaterialsDC(GraphicsDevice g,
		int pass, GetModelMatrix getModMatrix, Buffer vb,
		Buffer ib, Dictionary<int, List<DrawCall>> dcs,
		IsMaterialVisible bMatVis)
	{
		if(dcs == null)
		{
			return;
		}

		List<string>	mats	=mMatLib.GetMaterialNames();

		g.DC.InputAssembler.SetVertexBuffers(0, vbb);
		g.DC.InputAssembler.SetIndexBuffer(ib, SharpDX.DXGI.Format.R16_UInt, 0);

		g.DC.IASetVertexBuffer(0, vb,
		
		//cycle through models
		foreach(KeyValuePair<int, List<DrawCall>> modCall in dcs)
		{
			foreach(DrawCall call in modCall.Value)
			{
				Debug.Assert(call.mCount > 0);

				string	mat	=mats[call.mMaterialID];

				int	 numPasses	=mMatLib.GetNumMaterialPasses(mat);
				if(numPasses <= pass)
				{
					continue;
				}

				string	fx	=mMatLib.GetMaterialEffect(mat);
				if(fx == null || fx == "")
				{
					continue;
				}

				//modcall key is the model index
				//zero is always the big world model
				//zero can check material vis
				if(modCall.Key == 0)
				{
					if(!bMatVis(g.GCam.Position, call.mMaterialID))
					{
						continue;
					}
				}

				//set world mat from model transforms
				if(getModMatrix != null)
				{
					mMatLib.SetMaterialParameter(mat, "mWorld", getModMatrix(modCall.Key));
				}

				mMatLib.ApplyMaterialPass(mat, g.DC, pass);

				g.DC.DrawIndexed(call.mCount, call.mStartIndex, 0);
			}
		}
	}


	//this one is for alphas with models
	void DrawMaterialsDC(GraphicsDevice g,
		int pass, GetModelMatrix getModMatrix,
		VertexBufferBinding vbb, Buffer ib,
		Dictionary<int, List<List<DrawCall>>> dcs,
		IsMaterialVisible bMatVis)
	{
		if(dcs == null)
		{
			return;
		}

		List<string>	mats	=mMatLib.GetMaterialNames();

		//only pass 2 actually draws stuff here, others store draws for sorting
		if(pass == 2)
		{
			g.DC.InputAssembler.SetVertexBuffers(0, vbb);
			g.DC.InputAssembler.SetIndexBuffer(ib, SharpDX.DXGI.Format.R16_UInt, 0);
		}

		//cycle through models
		foreach(KeyValuePair<int, List<List<DrawCall>>> modCall in dcs)
		{
			foreach(List<DrawCall> planeCalls in modCall.Value)
			{
				Debug.Assert(planeCalls.Count > 0);

				//do some sanity checks based on call 0
				int		objMatId	=planeCalls[0].mMaterialID;
				string	mat			=mats[objMatId];

				int	 numPasses	=mMatLib.GetNumMaterialPasses(mat);
				if(numPasses <= pass)
				{
					continue;
				}

				string	fx	=mMatLib.GetMaterialEffect(mat);
				if(fx == null || fx == "")
				{
					continue;
				}

				//modcall key is the model index
				//zero is always the big world model
				//zero can check material vis
				if(modCall.Key == 0)
				{
					if(!bMatVis(g.GCam.Position, objMatId))
					{
						continue;
					}
				}

				foreach(DrawCall call in planeCalls)
				{
					Debug.Assert(call.mCount > 0);
					Debug.Assert(call.mMaterialID == objMatId);

					Matrix	modMat	=getModMatrix(modCall.Key);
					if(pass == 2)
					{
						//I guess we do this because if it isn't pass 2
						//nothing is actually drawn right now
						mMatLib.SetMaterialParameter(mat, "mWorld", modMat);
					}

					if(pass != 2)
					{
						if(call.mAreaScore > 0)
						{
							mAlphaPool.StoreDraw(mMatLib, call.mSortPoint, call.mAreaScore,
								call.mSortPlaneNormal, call.mSortPlaneDistance,
								mat, vbb, ib, modMat, call.mStartIndex, call.mCount);
						}
						else
						{
							mAlphaPool.StoreDraw(mMatLib, call.mSortPoint,
								mat, vbb, ib, modMat, call.mStartIndex, call.mCount);
						}
					}
					else
					{
						mMatLib.ApplyMaterial(mat, g.DC);//, pass);

						//material depth normal pass draws directly
						g.DC.DrawIndexed(call.mCount, call.mStartIndex, 0);
					}
				}
			}
		}
	}


	float StyleVal(string szVal)
	{
		char	first	=szVal[0];
		char	topVal	='z';

		//get from zero to 25
		float	val	=topVal - first;

		//scale up to 0 to 255
		val	*=(255.0f / 25.0f);

		Debug.Assert(val >= 0.0f);
		Debug.Assert(val <= 255.0f);

		return	(255.0f - val) / 255.0f;
	}


	void UpdateAnimatedLightMaps(float msDelta)
	{
		List<string>	mats	=mMatLib.GetMaterialNames();

		for(int i=0;i < 12;i++)
		{
			mCurStylePos[i]	+=msDelta;

			float	endTime	=mStyles[i].Length * ThirtyFPS;

			while(mCurStylePos[i] >= endTime)
			{
				mCurStylePos[i]	-=endTime;
			}

			float	lerped	=ComputeStyleStrength(mCurStylePos[i], mStyles[i]);

			mAniIntensities[i]	=lerped;
		}

		//switchable lights
		for(int i=0;i < 32;i++)
		{
			mAniIntensities[12 + i]	=((mSwitches[i])? 1.0f : 0.0f);
		}

		foreach(string mat in mats)
		{
			if(mat.EndsWith("Anim"))
			{
				mMatLib.SetMaterialParameter(mat,
					"mAniIntensities", mAniIntensities);
			}
		}
	}


	public void SwitchLight(int lightIndex, bool bOn)
	{
		mSwitches[lightIndex - 32]	=bOn;
	}


	#region IO
	public void Read(GraphicsDevice g, StuffKeeper sk,
		string fileName, bool bEditor)
	{
		Stream	file	=new FileStream(fileName, FileMode.Open, FileAccess.Read);
		if(file == null)
		{
			return;
		}
		BinaryReader	br	=new BinaryReader(file);

		UInt32	tag	=br.ReadUInt32();

		if(tag != 0x57415244)
		{
			return;
		}

		mLightMapAtlas	=new TexAtlas(g, 1, 1);

		bool	bLightMapNeeded	=br.ReadBoolean();

		if(bLightMapNeeded)
		{
			mLightMapAtlas.Read(g, br);

			ShaderResourceView	lma	=mLightMapAtlas.GetAtlasSRV();

			lma.DebugName	="LightMapAtlas";
			sk.AddMap("LightMapAtlas", lma);
		}


		int	numVerts	=br.ReadInt32();
		if(numVerts != 0)
		{
			mLMIndex	=br.ReadInt32();
			VertexTypes.ReadVerts(br, g.GD, out mLMVerts);
			mLMVB	=VertexTypes.BuildABuffer(g.GD, mLMVerts, mLMIndex);
			mLMVBB	=new VertexBufferBinding(mLMVB, VertexTypes.GetSizeForTypeIndex(mLMIndex), 0);
			if(bEditor)
			{
				ReadIndexBuffer(br, out mLMIB, out mLMInds, g);
			}
			else
			{
				ReadIndexBuffer(br, out mLMIB, g);
			}
		}

		numVerts	=br.ReadInt32();
		if(numVerts != 0)
		{
			mVLitIndex	=br.ReadInt32();
			VertexTypes.ReadVerts(br, g.GD, out mVLitVerts);
			mVLitVB		=VertexTypes.BuildABuffer(g.GD, mVLitVerts, mVLitIndex);
			mVLitVBB	=new VertexBufferBinding(mVLitVB, VertexTypes.GetSizeForTypeIndex(mVLitIndex), 0);
			if(bEditor)
			{
				ReadIndexBuffer(br, out mVLitIB, out mVLitInds, g);
			}
			else
			{
				ReadIndexBuffer(br, out mVLitIB, g);
			}
		}

		numVerts	=br.ReadInt32();
		if(numVerts != 0)
		{
			mLMAnimIndex	=br.ReadInt32();
			VertexTypes.ReadVerts(br, g.GD, out mLMAnimVerts);
			mLMAnimVB	=VertexTypes.BuildABuffer(g.GD, mLMAnimVerts, mLMAnimIndex);
			mLMAnimVBB	=new VertexBufferBinding(mLMAnimVB, VertexTypes.GetSizeForTypeIndex(mLMAnimIndex), 0);
			if(bEditor)
			{
				ReadIndexBuffer(br, out mLMAnimIB, out mLMAnimInds, g);
			}
			else
			{
				ReadIndexBuffer(br, out mLMAnimIB, g);
			}
		}

		numVerts	=br.ReadInt32();
		if(numVerts != 0)
		{
			mAlphaIndex	=br.ReadInt32();
			VertexTypes.ReadVerts(br, g.GD, out mAlphaVerts);
			mAlphaVB	=VertexTypes.BuildABuffer(g.GD, mAlphaVerts, mAlphaIndex);
			mAlphaVBB	=new VertexBufferBinding(mAlphaVB, VertexTypes.GetSizeForTypeIndex(mAlphaIndex), 0);
			if(bEditor)
			{
				ReadIndexBuffer(br, out mAlphaIB, out mAlphaInds, g);
			}
			else
			{
				ReadIndexBuffer(br, out mAlphaIB, g);
			}
		}

		numVerts	=br.ReadInt32();
		if(numVerts != 0)
		{
			mSkyIndex	=br.ReadInt32();
			VertexTypes.ReadVerts(br, g.GD, out mSkyVerts);
			mSkyVB	=VertexTypes.BuildABuffer(g.GD, mSkyVerts, mSkyIndex);
			mSkyVBB	=new VertexBufferBinding(mSkyVB, VertexTypes.GetSizeForTypeIndex(mSkyIndex), 0);
			if(bEditor)
			{
				ReadIndexBuffer(br, out mSkyIB, out mSkyInds, g);
			}
			else
			{
				ReadIndexBuffer(br, out mSkyIB, g);
			}
		}

		numVerts	=br.ReadInt32();
		if(numVerts != 0)
		{
			mFBIndex	=br.ReadInt32();
			VertexTypes.ReadVerts(br, g.GD, out mFBVerts);
			mFBVB	=VertexTypes.BuildABuffer(g.GD, mFBVerts, mFBIndex);
			mFBVBB	=new VertexBufferBinding(mFBVB, VertexTypes.GetSizeForTypeIndex(mFBIndex), 0);
			if(bEditor)
			{
				ReadIndexBuffer(br, out mFBIB, out mFBInds, g);
			}
			else
			{
				ReadIndexBuffer(br, out mFBIB, g);
			}
		}

		numVerts	=br.ReadInt32();
		if(numVerts != 0)
		{
			mMirrorIndex	=br.ReadInt32();
			VertexTypes.ReadVerts(br, g.GD, out mMirrorVerts);
			mMirrorVB	=VertexTypes.BuildABuffer(g.GD, mMirrorVerts, mMirrorIndex);
			mMirrorVBB	=new VertexBufferBinding(mMirrorVB, VertexTypes.GetSizeForTypeIndex(mMirrorIndex), 0);
			if(bEditor)
			{
				ReadIndexBuffer(br, out mMirrorIB, out mMirrorInds, g);
			}
			else
			{
				ReadIndexBuffer(br, out mMirrorIB, g);
			}
		}

		numVerts	=br.ReadInt32();
		if(numVerts != 0)
		{
			mLMAIndex	=br.ReadInt32();
			VertexTypes.ReadVerts(br, g.GD, out mLMAVerts);
			mLMAVB	=VertexTypes.BuildABuffer(g.GD, mLMAVerts, mLMAIndex);
			mLMAVBB	=new VertexBufferBinding(mLMAVB, VertexTypes.GetSizeForTypeIndex(mLMAIndex), 0);
			if(bEditor)
			{
				ReadIndexBuffer(br, out mLMAIB, out mLMAInds, g);
			}
			else
			{
				ReadIndexBuffer(br, out mLMAIB, g);
			}
		}

		numVerts	=br.ReadInt32();
		if(numVerts != 0)
		{
			mLMAAnimIndex	=br.ReadInt32();
			VertexTypes.ReadVerts(br, g.GD, out mLMAAnimVerts);
			mLMAAnimVB	=VertexTypes.BuildABuffer(g.GD, mLMAAnimVerts, mLMAAnimIndex);
			mLMAAnimVBB	=new VertexBufferBinding(mLMAAnimVB, VertexTypes.GetSizeForTypeIndex(mLMAAnimIndex), 0);
			if(bEditor)
			{
				ReadIndexBuffer(br, out mLMAAnimIB, out mLMAAnimInds, g);
			}
			else
			{
				ReadIndexBuffer(br, out mLMAAnimIB, g);
			}
		}

		mLMDrawCalls		=DrawCall.ReadDrawCallDict(br);
		mVLitDrawCalls		=DrawCall.ReadDrawCallDict(br);
		mLMAnimDrawCalls	=DrawCall.ReadDrawCallDict(br);
		mSkyDrawCalls		=DrawCall.ReadDrawCallDict(br);
		mFBDrawCalls		=DrawCall.ReadDrawCallDict(br);
		mMirrorDrawCalls	=DrawCall.ReadDrawCallDict(br);

		mLMADrawCalls		=DrawCall.ReadDrawCallAlphaDict(br);
		mAlphaDrawCalls		=DrawCall.ReadDrawCallAlphaDict(br);
		mLMAAnimDrawCalls	=DrawCall.ReadDrawCallAlphaDict(br);
		
		int	mirrorCount	=br.ReadInt32();
		for(int i=0;i < mirrorCount;i++)
		{
			Vector3	[]verts	=FileUtil.ReadVecArray(br);

			List<Vector3>	vlist	=new List<Vector3>(verts);

			mMirrorPolys.Add(vlist);
		}

		br.Close();
		file.Close();

		if(!bEditor)
		{
			//free stuff
			mLMVerts		=mVLitVerts	=mLMAnimVerts	=null;
			mAlphaVerts		=mSkyVerts	=mFBVerts		=null;
			mMirrorVerts	=mLMAVerts	=mLMAAnimVerts	=null;
		}
	}


	void WriteMaterial(int index, Array verts, UInt16 []ib, BinaryWriter bw)
	{
		if(verts == null)
		{
			bw.Write(0);
		}
		else
		{
			if(index == -1)
			{
				//bogus type
				bw.Write(0);
				return;
			}

			bw.Write(verts.Length);
			bw.Write(index);
			MeshLib.VertexTypes.WriteVerts(bw, verts, index);

			Debug.Assert(ib.Length < UInt16.MaxValue);

			bw.Write(ib.Length);
			foreach(UInt16 idx in ib)
			{
				bw.Write(idx);
			}
		}
	}


	void ReadIndexBuffer(BinaryReader br, out Buffer ib, GraphicsDevice g)
	{
		int	numIdx	=br.ReadInt32();

		Debug.Assert(numIdx < UInt16.MaxValue);

		if(numIdx >= UInt16.MaxValue)
		{
			Debug.WriteLine("Index buffer too big!");
			ib	=null;
			return;
		}

		UInt16	[]idxArray	=new UInt16[numIdx];

		for(int i=0;i < numIdx;i++)
		{
			idxArray[i]	=br.ReadUInt16();
		}

		ib	=VertexTypes.BuildAnIndexBuffer(g.GD, idxArray);
	}


	//keeps the array
	void ReadIndexBuffer(BinaryReader br, out Buffer ib, out UInt16 []ibArray, GraphicsDevice g)
	{
		int	numIdx	=br.ReadInt32();

		if(numIdx >= UInt16.MaxValue)
		{
			Debug.WriteLine("Index buffer too big!");
			ib		=null;
			ibArray	=null;
			return;
		}

		ibArray	=new UInt16[numIdx];

		for(int i=0;i < numIdx;i++)
		{
			ibArray[i]	=br.ReadUInt16();
		}
		ib	=VertexTypes.BuildAnIndexBuffer(g.GD, ibArray);
	}


	public void Write(string fileName)
	{
		FileStream	file	=new FileStream(fileName, FileMode.Create, FileAccess.Write);

		BinaryWriter	bw	=new BinaryWriter(file);

		bw.Write(0x57415244);	//DRAW

		bw.Write(mLightMapAtlas != null);

		if(mLightMapAtlas != null)
		{
			mLightMapAtlas.Write(bw);
		}

		WriteMaterial(mLMIndex, mLMVerts, mLMInds, bw);
		WriteMaterial(mVLitIndex, mVLitVerts, mVLitInds, bw);
		WriteMaterial(mLMAnimIndex, mLMAnimVerts, mLMAnimInds, bw);
		WriteMaterial(mAlphaIndex, mAlphaVerts, mAlphaInds, bw);
		WriteMaterial(mSkyIndex, mSkyVerts, mSkyInds, bw);
		WriteMaterial(mFBIndex, mFBVerts, mFBInds, bw);
		WriteMaterial(mMirrorIndex, mMirrorVerts, mMirrorInds, bw);
		WriteMaterial(mLMAIndex, mLMAVerts, mLMAInds, bw);
		WriteMaterial(mLMAAnimIndex, mLMAAnimVerts, mLMAAnimInds, bw);

		//drawcall stuff
		//opaques
		DrawCall.WriteDrawCallDict(bw, mLMDrawCalls);
		DrawCall.WriteDrawCallDict(bw, mVLitDrawCalls);
		DrawCall.WriteDrawCallDict(bw, mLMAnimDrawCalls);
		DrawCall.WriteDrawCallDict(bw, mSkyDrawCalls);
		DrawCall.WriteDrawCallDict(bw, mFBDrawCalls);
		DrawCall.WriteDrawCallDict(bw, mMirrorDrawCalls);

		//alphas
		DrawCall.WriteDrawCallAlphaDict(bw, mLMADrawCalls);
		DrawCall.WriteDrawCallAlphaDict(bw, mAlphaDrawCalls);
		DrawCall.WriteDrawCallAlphaDict(bw, mLMAAnimDrawCalls);

		//mirror polys
		bw.Write(mMirrorPolys.Count);
		for(int i=0;i < mMirrorPolys.Count;i++)
		{
			FileUtil.WriteArray(bw, mMirrorPolys[i].ToArray());
		}

		bw.Close();
		file.Close();
	}
	#endregion
}*/