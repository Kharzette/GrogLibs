//#define PIXGOBLINRY	//use this to remove alpha sorting, which somehow makes pix crash
using System;
using System.IO;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using Vortice.Direct3D11;
using Vortice.Mathematics;
using UtilityLib;
using MaterialLib;


using MatLib	=MaterialLib.MaterialLib;

namespace MeshLib;

//draw bits of indoor scenery
public class IndoorMesh
{
	//vertex
	ID3D11Buffer	mLMVB, mVLitVB, mLMAnimVB, mAlphaVB, mSkyVB;
	ID3D11Buffer	mFBVB, mMirrorVB, mLMAVB, mLMAAnimVB;

	//index
	ID3D11Buffer	mLMIB, mVLitIB, mLMAnimIB, mAlphaIB;
	ID3D11Buffer	mSkyIB, mFBIB, mMirrorIB, mLMAIB, mLMAAnimIB;

	//draw calls indexed by model
	Dictionary<int, List<DrawCall>>	mLMDraws;
	Dictionary<int, List<DrawCall>>	mLMADraws;
	Dictionary<int, List<DrawCall>>	mVLitDraws;

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
	Half					[]mAniIntensities	=new Half[44];

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


	public void SetLMData(ID3D11Device gd, int typeIndex, Array verts, UInt16 []inds,
		Dictionary<int, List<DrawCall>> lmDraws)
	{
		if(typeIndex == -1)
		{
			return;
		}
		mLMVB	=VertexTypes.BuildABuffer(gd, verts, typeIndex);
		mLMIB	=VertexTypes.BuildAnIndexBuffer(gd, inds);

		mLMVerts	=verts;
		mLMInds		=inds;
		mLMIndex	=typeIndex;
		mLMDraws	=lmDraws;
	}

	public void SetLMAData(ID3D11Device gd, int typeIndex, Array verts, UInt16 []inds,
		Dictionary<int, List<DrawCall>> draws)
	{
		if(typeIndex == -1)
		{
			return;
		}
		mLMAVB	=VertexTypes.BuildABuffer(gd, verts, typeIndex);
		mLMAIB	=VertexTypes.BuildAnIndexBuffer(gd, inds);

		mLMAVerts	=verts;
		mLMAInds	=inds;
		mLMAIndex	=typeIndex;
		mLMADraws	=draws;
	}

	public void SetVLitData(ID3D11Device gd, int typeIndex, Array verts, UInt16 []inds,
		Dictionary<int, List<DrawCall>> draws)
	{
		if(typeIndex == -1)
		{
			return;
		}
		mVLitVB	=VertexTypes.BuildABuffer(gd, verts, typeIndex);
		mVLitIB	=VertexTypes.BuildAnIndexBuffer(gd, inds);

		mVLitVerts	=verts;
		mVLitInds	=inds;
		mVLitIndex	=typeIndex;
		mVLitDraws	=draws;
	}


	public void SetLMAtlas(TexAtlas lma)
	{
		mLightMapAtlas	=lma;
	}


	public void FinishAtlas(GraphicsDevice gd, StuffKeeper sk)
	{
		if(mLightMapAtlas == null)
		{
			return;
		}

		mLightMapAtlas.Finish(gd, sk, "LightMapAtlas");
//		sk.AddMap("LightMapAtlas", mLightMapAtlas.GetAtlasSRV());
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
	}


	public void Draw(GraphicsDevice gd,
		IsMaterialVisible bMatVis,
		GetModelMatrix getModMatrix)
	{
		CBKeeper	cbk	=mMatLib.GetCBKeeper();
		cbk.SetAniIntensities(mAniIntensities);

		DrawStuff(gd, bMatVis, getModMatrix, mLMDraws, mLMIndex, mLMVB, mLMIB);
		DrawStuff(gd, bMatVis, getModMatrix, mVLitDraws, mVLitIndex, mVLitVB, mVLitIB);
		DrawStuff(gd, bMatVis, getModMatrix, mLMADraws, mLMAIndex, mLMAVB, mLMAIB);
	}


	internal void DrawStuff(GraphicsDevice gd, IsMaterialVisible bMatVis, GetModelMatrix getModMatrix,
		Dictionary<int, List<DrawCall>> draws, int typeIdx, ID3D11Buffer vb, ID3D11Buffer ib)
	{
		if(draws == null)
		{
			return;
		}
		if(draws.Count == 0)
		{
			return;
		}

		CBKeeper	cbk	=mMatLib.GetCBKeeper();

		gd.DC.IASetVertexBuffer(0, vb, VertexTypes.GetSizeForTypeIndex(typeIdx), 0);
		gd.DC.IASetIndexBuffer(ib, Vortice.DXGI.Format.R16_UInt, 0);

		List<string>	matNames	=mMatLib.GetMaterialNames();

		foreach(KeyValuePair<int, List<DrawCall>> modCall in draws)
		{
			Matrix4x4	worldMat	=getModMatrix(modCall.Key);

			cbk.SetTransposedWorldMat(worldMat);

			foreach(DrawCall dc in modCall.Value)
			{
				Debug.Assert(dc.mCount > 0);

				string	mat	=matNames[dc.mMaterialID];

				mMatLib.ApplyMaterial(mat, gd.DC);

				gd.DC.DrawIndexed(dc.mCount, dc.mStartIndex, 0);
			}
		}
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

		return	MathHelper.Lerp(val, nextVal, ratio);
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

			mAniIntensities[i]	=(Half)lerped;
		}

		//switchable lights
		for(int i=0;i < 32;i++)
		{
			mAniIntensities[12 + i]	=(Half)((mSwitches[i])? 1.0f : 0.0f);
		}

		foreach(string mat in mats)
		{
			if(mat.EndsWith("Anim"))
			{
				BSPMat	bm	=mMatLib.GetMaterialBSPMat(mat);

				bm.AniIntensities	=mAniIntensities;
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
			mLightMapAtlas.Read(g, br, sk);
			mLightMapAtlas.Finish(g, sk, "LightMapAtlas");
		}


		int	numVerts	=br.ReadInt32();
		if(numVerts != 0)
		{
			mLMIndex	=br.ReadInt32();
			VertexTypes.ReadVerts(br, g.GD, out mLMVerts);
			mLMVB	=VertexTypes.BuildABuffer(g.GD, mLMVerts, mLMIndex);
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
			if(bEditor)
			{
				ReadIndexBuffer(br, out mLMAAnimIB, out mLMAAnimInds, g);
			}
			else
			{
				ReadIndexBuffer(br, out mLMAAnimIB, g);
			}
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


	void ReadIndexBuffer(BinaryReader br, out ID3D11Buffer ib, GraphicsDevice g)
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
	void ReadIndexBuffer(BinaryReader br, out ID3D11Buffer ib, out UInt16 []ibArray, GraphicsDevice g)
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

		bw.Close();
		file.Close();
	}
	#endregion
}