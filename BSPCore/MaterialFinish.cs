using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using MeshLib;


namespace BSPCore;

public partial class MapGrinder
{
	void FinishLightMapped(int modelIndex, Dictionary<int, DrawDataChunk> matChunks, ref UInt16 vertOfs)
	{
		if(matChunks.Count == 0)
		{
			return;
		}
		
		List<DrawCall>	modCalls	=ComputeIndexes(mLMIndexes, matChunks, ref vertOfs);

		StuffVBArrays(matChunks, mLMVerts, mLMNormals,
			mLMFaceTex0, mLMFaceTex1, null, null,
			null, null, null);

		mLMDraws.Add(modelIndex, modCalls);
	}

/*
	void FinishLightMapAnimated(int modelIndex, List<DrawDataChunk> matChunks, ref UInt16 vertOfs)
	{
		List<DrawCall>	modCalls	=ComputeIndexes(mLMAnimIndexes, matChunks, ref vertOfs);

		StuffVBArrays(matChunks, mLMAnimVerts, mLMAnimNormals,
			mLMAnimFaceTex0, mLMAnimFaceTex1, mLMAnimFaceTex2,
			mLMAnimFaceTex3, mLMAnimFaceTex4, null, mLMAnimStyle);

		mLMAnimDraws.Add(modelIndex, modCalls);
	}


	void FinishVLit(int modelIndex, List<DrawDataChunk> matChunks, ref UInt16 vertOfs)
	{
		List<DrawCall>	modCalls	=ComputeIndexes(mVLitIndexes, matChunks, ref vertOfs);

		StuffVBArrays(matChunks, mVLitVerts, mVLitNormals,
			mVLitTex0, null, null, null,
			null, mVLitColors, null);

		mVLitDraws.Add(modelIndex, modCalls);
	}


	void FinishSky(int modelIndex, List<DrawDataChunk> matChunks, ref UInt16 vertOfs)
	{
		List<DrawCall>	modCalls	=ComputeIndexes(mSkyIndexes, matChunks, ref vertOfs);

		StuffVBArrays(matChunks, mSkyVerts, null,
			mSkyTex0, null, null, null,
			null, null, null);

		mSkyDraws.Add(modelIndex, modCalls);
	}


	void FinishFullBright(int modelIndex, List<DrawDataChunk> matChunks, ref UInt16 vertOfs)
	{
		List<DrawCall>	modCalls	=ComputeIndexes(mFBIndexes, matChunks, ref vertOfs);

		StuffVBArrays(matChunks, mFBVerts, mFBNormals,
			mFBTex0, null, null, null,
			null, null, null);

		mFBDraws.Add(modelIndex, modCalls);
	}


	void FinishLightMappedAlpha(int modelIndex,
		List<Dictionary<Int32, DrawDataChunk>> perPlaneChunks,
		GFXPlane []pp, ref UInt16 vertOfs)
	{
		List<List<DrawCall>>	modCalls	=ComputeAlphaIndexes(mLMAIndexes, perPlaneChunks, pp, ref vertOfs);

		StuffVBArrays(perPlaneChunks, mLMAVerts, mLMANormals,
			mLMAFaceTex0, mLMAFaceTex1, null, null,	null,
			mLMAColors, null);

		mLMADraws.Add(modelIndex, modCalls);
	}


	void FinishMirror(int modelIndex, List<DrawDataChunk> matChunks, ref UInt16 vertOfs)
	{
		List<DrawCall>	modCalls	=ComputeIndexes(mMirrorIndexes, matChunks, ref vertOfs);

		StuffVBArrays(matChunks, mMirrorVerts, mMirrorNormals,
			mMirrorTex0, mMirrorTex1, null, null, null,
			mMirrorColors, null);

		mMirrorDraws.Add(modelIndex, modCalls);
	}


	void FinishAlpha(int modelIndex,
		List<Dictionary<Int32, DrawDataChunk>> perPlaneChunks,
		GFXPlane []pp, ref UInt16 vertOfs)
	{
		List<List<DrawCall>>	modCalls	=ComputeAlphaIndexes(mAlphaIndexes, perPlaneChunks, pp, ref vertOfs);

		StuffVBArrays(perPlaneChunks, mAlphaVerts, mAlphaNormals,
			mAlphaTex0, null, null, null, null,
			mAlphaColors, null);

		mAlphaDraws.Add(modelIndex, modCalls);
	}


	void FinishLightMappedAlphaAnimated(int modelIndex,
		List<Dictionary<Int32, DrawDataChunk>> perPlaneChunks,
		GFXPlane []pp, ref UInt16 vertOfs)
	{
		List<List<DrawCall>>	modCalls	=ComputeAlphaIndexes(mLMAAnimIndexes, perPlaneChunks, pp, ref vertOfs);

		StuffVBArrays(perPlaneChunks, mLMAAnimVerts, mLMAAnimNormals,
			mLMAAnimFaceTex0, mLMAAnimFaceTex1, mLMAAnimFaceTex2, 
			mLMAAnimFaceTex3, mLMAAnimFaceTex4, mLMAAnimColors,
			mLMAAnimStyle);

		mLMAAnimDraws.Add(modelIndex, modCalls);
	}*/
}