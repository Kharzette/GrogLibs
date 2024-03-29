﻿using System;
using System.Text;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using Vortice.Mathematics;
using Vortice.Mathematics.PackedVector;
using MaterialLib;
using MeshLib;
using UtilityLib;

using MatLib	=MaterialLib.MaterialLib;


namespace BSPCore;

//This is all the stuff that goes into a single draw.
//For BSP this is a bunch of face triangles that are all the
//same texture and shader.
class DrawDataChunk
{
	internal int			mNumFaces;
	internal List<int>		mVCounts	=new List<int>();
	internal List<Vector3>	mVerts		=new List<Vector3>();
	internal List<Vector3>	mNorms		=new List<Vector3>();
	internal List<Vector2>	mTex0		=new List<Vector2>();
	internal List<Vector2>	mTex1		=new List<Vector2>();
	internal List<Vector2>	mTex2		=new List<Vector2>();
	internal List<Vector2>	mTex3		=new List<Vector2>();
	internal List<Vector2>	mTex4		=new List<Vector2>();
	internal List<Color>	mColors		=new List<Color>();
	internal List<Color>	mStyles		=new List<Color>();
}


//grind up a map into gpu friendly data
public partial class MapGrinder
{
	GraphicsDevice			mGD;
	MaterialLib.MaterialLib	mMatLib;

	//computed lightmapped geometry
	List<Vector3>	mLMVerts		=new List<Vector3>();
	List<Vector3>	mLMNormals		=new List<Vector3>();
	List<Vector2>	mLMFaceTex0		=new List<Vector2>();
	List<Vector2>	mLMFaceTex1		=new List<Vector2>();
	List<UInt16>	mLMIndexes		=new List<UInt16>();

	//computed lightmapped alpha geometry
	List<Vector3>	mLMAVerts		=new List<Vector3>();
	List<Vector3>	mLMANormals		=new List<Vector3>();
	List<Vector2>	mLMAFaceTex0	=new List<Vector2>();
	List<Vector2>	mLMAFaceTex1	=new List<Vector2>();
	List<UInt16>	mLMAIndexes		=new List<UInt16>();
	List<Color>		mLMAColors		=new List<Color>();

	//computed vertex lit geometry
	List<Vector3>	mVLitVerts		=new List<Vector3>();
	List<Vector2>	mVLitTex0		=new List<Vector2>();
	List<Vector3>	mVLitNormals	=new List<Vector3>();
	List<Color>		mVLitColors		=new List<Color>();
	List<UInt16>	mVLitIndexes	=new List<UInt16>();

	//computed fullbright geometry
	List<Vector3>	mFBVerts	=new List<Vector3>();
	List<Vector3>	mFBNormals	=new List<Vector3>();
	List<Vector2>	mFBTex0		=new List<Vector2>();
	List<UInt16>	mFBIndexes	=new List<UInt16>();

	//computed alpha geometry
	List<Vector3>	mAlphaVerts		=new List<Vector3>();
	List<Vector2>	mAlphaTex0		=new List<Vector2>();
	List<Vector3>	mAlphaNormals	=new List<Vector3>();
	List<Color>		mAlphaColors	=new List<Color>();
	List<UInt16>	mAlphaIndexes	=new List<UInt16>();

	//computed mirror geometry
	List<Vector3>		mMirrorVerts	=new List<Vector3>();
	List<Vector3>		mMirrorNormals	=new List<Vector3>();
	List<Vector2>		mMirrorTex0		=new List<Vector2>();
	List<Vector2>		mMirrorTex1		=new List<Vector2>();
	List<Color>			mMirrorColors	=new List<Color>();
	List<UInt16>		mMirrorIndexes	=new List<UInt16>();
	List<List<Vector3>>	mMirrorPolys	=new List<List<Vector3>>();

	//computed sky geometry
	List<Vector3>	mSkyVerts	=new List<Vector3>();
	List<Vector2>	mSkyTex0	=new List<Vector2>();
	List<UInt16>	mSkyIndexes	=new List<UInt16>();

	//animated lightmap geometry
	List<Vector3>	mLMAnimVerts	=new List<Vector3>();
	List<Vector3>	mLMAnimNormals	=new List<Vector3>();
	List<Vector2>	mLMAnimFaceTex0	=new List<Vector2>();
	List<Vector2>	mLMAnimFaceTex1	=new List<Vector2>();
	List<Vector2>	mLMAnimFaceTex2	=new List<Vector2>();
	List<Vector2>	mLMAnimFaceTex3	=new List<Vector2>();
	List<Vector2>	mLMAnimFaceTex4	=new List<Vector2>();
	List<UInt16>	mLMAnimIndexes	=new List<UInt16>();
	List<Color>		mLMAnimStyle	=new List<Color>();

	//animated lightmap alpha geometry
	List<Vector3>	mLMAAnimVerts		=new List<Vector3>();
	List<Vector3>	mLMAAnimNormals		=new List<Vector3>();
	List<Vector2>	mLMAAnimFaceTex0	=new List<Vector2>();
	List<Vector2>	mLMAAnimFaceTex1	=new List<Vector2>();
	List<Vector2>	mLMAAnimFaceTex2	=new List<Vector2>();
	List<Vector2>	mLMAAnimFaceTex3	=new List<Vector2>();
	List<Vector2>	mLMAAnimFaceTex4	=new List<Vector2>();
	List<UInt16>	mLMAAnimIndexes		=new List<UInt16>();
	List<Color>		mLMAAnimStyle		=new List<Color>();
	List<Color>		mLMAAnimColors		=new List<Color>();

	//material draw call information
	//opaques
	//indexed by model number
	Dictionary<int, List<DrawCall>>	mLMDraws		=new Dictionary<int, List<DrawCall>>();
	Dictionary<int, List<DrawCall>>	mLMAnimDraws	=new Dictionary<int, List<DrawCall>>();
	Dictionary<int, List<DrawCall>>	mVLitDraws		=new Dictionary<int, List<DrawCall>>();
	Dictionary<int, List<DrawCall>>	mSkyDraws		=new Dictionary<int, List<DrawCall>>();
	Dictionary<int, List<DrawCall>>	mFBDraws		=new Dictionary<int, List<DrawCall>>();
	
	//alphas
	Dictionary<int, List<DrawCall>>	mLMADraws		=new Dictionary<int, List<DrawCall>>();
	Dictionary<int, List<DrawCall>>	mAlphaDraws		=new Dictionary<int, List<DrawCall>>();
	Dictionary<int, List<DrawCall>>	mLMAAnimDraws	=new Dictionary<int, List<DrawCall>>();

	//computed material stuff
	List<string>	mMaterialNames		=new List<string>();

	//computed lightmap atlas
	TexAtlas	mLMAtlas;

	//passed in data
	int			mLightGridSize;
	TexInfo		[]mTexInfos;
	QFace		[]mFaces;
	QEdge		[]mEdges;
	int			[]mSurfEdges;
	GBSPPlane	[]mPlanes;
	Vector3		[]mVerts;
	QModel		[]mModels;
	byte		[]mLightData;


	public MapGrinder(GraphicsDevice gd,
		StuffKeeper sk, MatLib matLib,
		TexInfo []texs, Vector3 []verts,
		QEdge []edges, int []surfEdges,
		QFace []faces, GBSPPlane []planes,
		QModel []models, byte []lightData,
		int atlasSize)
	{
		mGD			=gd;
		mMatLib		=matLib;

		mTexInfos	=texs;
		mFaces		=faces;
		mEdges		=edges;
		mSurfEdges	=surfEdges;
		mPlanes		=planes;
		mVerts		=verts;
		mModels		=models;
		mLightData	=lightData;

		mLightGridSize	=16;		//maybe?

		if(mMatLib == null)
		{
			mMatLib	=new MatLib(sk);
		}

		if(gd != null)
		{
			mLMAtlas	=new TexAtlas(gd, atlasSize, atlasSize);
		}

		CalcMaterialNames();
		CalcMaterials();
	}


	public void FreeAll()
	{
		mMatLib.FreeAll();
	}


	public TexAtlas GetLMAtlas()
	{
		return	mLMAtlas;
	}


	public Dictionary<int, List<DrawCall>>	GetLMDrawCalls()
	{
		return	mLMDraws;
	}

	public Dictionary<int, List<DrawCall>>	GetLMADrawCalls()
	{
		return	mLMADraws;
	}

	public Dictionary<int, List<DrawCall>>	GetVLitDrawCalls()
	{
		return	mVLitDraws;
	}

	public Dictionary<int, List<DrawCall>>	GetFullBrightDrawCalls()
	{
		return	mFBDraws;
	}

	public Dictionary<int, List<DrawCall>>	GetLMAnimDrawCalls()
	{
		return	mLMAnimDraws;
	}

	public Dictionary<int, List<DrawCall>>	GetLMAAnimDrawCalls()
	{
		return	mLMAAnimDraws;
	}

	public Dictionary<int, List<DrawCall>>	GetVLitAlphaDrawCalls()
	{
		return	mAlphaDraws;
	}

	public Dictionary<int, List<DrawCall>>	GetSkyDrawCalls()
	{
		return	mSkyDraws;
	}


	//gets the face winding non triangulated
	List<Vector3> GetFaceVerts(QFace f)
	{
		List<Vector3>	ret	=new List<Vector3>();

		int	firstEdge	=f.mFirstEdge;

		for(int j=0;j < f.mNumEdges;j++)
		{
			int	edge	=mSurfEdges[firstEdge];

			UInt16	index	=0;
			if(edge < 0)
			{
				index	=mEdges[-edge].v1;
			}
			else
			{
				index	=mEdges[edge].v0;
			}

			ret.Add(Vector3.Transform(mVerts[index], Map.mGrogTransform));
		}		
		return	ret;
	}

	//triangulate
	List<Vector3> GetFaceTris(QFace f)
	{
		List<Vector3>	ret	=new List<Vector3>();

		int	firstEdge	=f.mFirstEdge;

		for(int j=1;j < f.mNumEdges - 1;j++)
		{
			int	edge	=mSurfEdges[firstEdge];

			UInt16	index	=0;
			if(edge < 0)
			{
				index	=mEdges[-edge].v1;
			}
			else
			{
				index	=mEdges[edge].v0;
			}

			ret.Add(Vector3.Transform(mVerts[index], Map.mGrogTransform));

			edge	=mSurfEdges[firstEdge + j];
			index	=0;
			if(edge < 0)
			{
				index	=mEdges[-edge].v1;
			}
			else
			{
				index	=mEdges[edge].v0;
			}

			ret.Add(Vector3.Transform(mVerts[index], Map.mGrogTransform));

			edge	=mSurfEdges[firstEdge + ((j + 1) % f.mNumEdges)];
			index	=0;
			if(edge < 0)
			{
				index	=mEdges[-edge].v1;
			}
			else
			{
				index	=mEdges[edge].v0;
			}

			ret.Add(Vector3.Transform(mVerts[index], Map.mGrogTransform));
		}
		return	ret;
	}

	//handles basic verts and texcoord 0
	void ComputeFaceData(QFace f, TexInfo tex,
		List<Vector2> tex0, List<Vector3> outVerts)
	{
		List<Vector3>	worldVerts	=GetFaceTris(f);

		foreach(Vector3 v in worldVerts)
		{
			Vector2	crd;
			crd.X	=Vector3.Dot(tex.mUVec, v);
			crd.Y	=Vector3.Dot(tex.mVVec, v);

			crd.X	/=tex.mDrawScaleU;
			crd.Y	/=tex.mDrawScaleV;

			crd.X	+=tex.mShiftU;
			crd.Y	+=tex.mShiftV;

			tex0.Add(crd);

			outVerts.Add(v);
		}
	}


	public bool BuildLMData()
	{
		UInt16	vertOfs	=0;	//model offset into the vertex buffer
		for(int i=0;i < mModels.Length;i++)
		{
			//store faces per material
			Dictionary<int,	DrawDataChunk>	matChunks	=new Dictionary<int, DrawDataChunk>();

			foreach(string mat in mMaterialNames)
			{
				int	firstFace	=mModels[i].mFirstFace;
				int	nFaces		=mModels[i].mNumFaces;

				for(int face=firstFace;face < (firstFace + nFaces);face++)
				{
					QFace	f	=mFaces[face];
					TexInfo	tex	=mTexInfos[f.mTexInfo];

					GBSPPlane	pln	=mPlanes[f.mPlaneNum];
					if(f.mSide != 0)
					{
						pln.Inverse();
					}

					if(!mat.StartsWith(tex.mTexture))
					{
						continue;
					}

					//skip non lightmap materials
					if(!MaterialCorrect.IsLightMapped(f, tex, mat))
					{
						continue;
					}

					int	matIndex	=mMaterialNames.IndexOf(mat);

					DrawDataChunk	ddc;
					if(matChunks.ContainsKey(matIndex))
					{
						ddc	=matChunks[matIndex];
					}
					else
					{
						ddc	=new DrawDataChunk();
						matChunks.Add(matIndex, ddc);
					}

					List<Vector3>	faceVerts	=new List<Vector3>();
					ComputeFaceData(f, tex, ddc.mTex0, faceVerts);

					ddc.mNumFaces++;
					ddc.mVCounts.Add(faceVerts.Count);

					//add face norm to verts
					for(int j=0;j < faceVerts.Count;j++)
					{
						ddc.mNorms.Add(pln.mNormal);
					}

					if(!MaterialFill.AtlasLightMap(mLMAtlas, mLightGridSize, f,
						mLightData, 0, faceVerts, pln, tex, ddc.mTex1))
					{
						CoreEvents.Print("Lightmap atlas out of space, try increasing it's size.\n");
						return	false;
					}

					ddc.mVerts.AddRange(faceVerts);
				}
			}
			FinishLightMapped(i, matChunks, ref vertOfs);
		}
		return	true;
	}
/*
	internal bool BuildLMAnimData(Vector3 []verts, int[] inds,
			GFXPlane []pp, GFXModel []models, byte []lightData)
	{
		UInt16	vertOfs	=0;	//model offset into the vertex buffer
		for(int i=0;i < models.Length;i++)
		{
			//store faces per material
			Dictionary<int,	DrawDataChunk>	matChunks	=new Dictionary<int, DrawDataChunk>();

			foreach(string mat in mMaterialNames)
			{
				int	firstFace	=models[i].mFirstFace;
				int	nFaces		=models[i].mNumFaces;

				for(int face=firstFace;face < (firstFace + nFaces);face++)
				{
					QFace	f	=mFaces[face];
					TexInfo	tex	=mTexInfos[f.mTexInfo];

					if(!mat.StartsWith(tex.mMaterial))
					{
						continue;
					}

					//skip unmatched materials
					if(!MaterialCorrect.IsLightMapAnimated(f, tex, mat))
					{
						continue;
					}

					int	matIndex	=mMaterialNames.IndexOf(mat);

					DrawDataChunk	ddc;
					if(matChunks.ContainsKey(matIndex))
					{
						ddc	=matChunks[matIndex];
					}
					else
					{
						ddc	=new DrawDataChunk();
						matChunks.Add(matIndex, ddc);
					}

					if(!MaterialFill.FillLightMapAnimated(ddc, pp, verts, inds,
						f, tex, mLightGridSize, lightData, mLMAtlas))
					{
						return	false;
					}
				}
			}
			FinishLightMapAnimated(i, matChunks, ref vertOfs);
		}
		return	true;
	}

	//alphas with lightmaps
	internal bool BuildLMAData(Vector3 []verts, int[] inds, Vector3 []rgbVerts,
			GFXPlane []pp, GFXModel []models, byte []lightData)
	{
		UInt16	vertOfs	=0;	//model offset into the vertex buffer
		for(int i=0;i < models.Length;i++)
		{
			//store faces per material
			Dictionary<int,	DrawDataChunk>	matChunks	=new Dictionary<int, DrawDataChunk>();

			foreach(string mat in mMaterialNames)
			{
				int	firstFace	=models[i].mFirstFace;
				int	nFaces		=models[i].mNumFaces;

				for(int face=firstFace;face < (firstFace + nFaces);face++)
				{
					QFace	f	=mFaces[face];
					TexInfo	tex	=mTexInfos[f.mTexInfo];

					if(!mat.StartsWith(tex.mMaterial))
					{
						continue;
					}

					//skip unmatched materials
					if(!MaterialCorrect.IsLightMappedAlpha(f, tex, mat))
					{
						continue;
					}

					int	matIndex	=mMaterialNames.IndexOf(mat);

					DrawDataChunk	ddc;
					if(matChunks.ContainsKey(matIndex))
					{
						ddc	=matChunks[matIndex];
					}
					else
					{
						ddc	=new DrawDataChunk();
						matChunks.Add(matIndex, ddc);
					}

					if(!MaterialFill.FillLightMappedAlpha(ddc, pp, verts, inds,
						rgbVerts, f, tex, mLightGridSize, lightData, mLMAtlas))
					{
						return	false;
					}
				}
			}
			FinishLightMappedAlpha(i, matChunks, ref vertOfs);
		}
		return	true;
	}

	//alphas with lightmaps
	internal bool BuildLMAAnimData(Vector3 []verts, int[] inds, Vector3 []rgbVerts,
			GFXPlane []pp, GFXModel []models, byte []lightData)
	{
		UInt16	vertOfs	=0;	//model offset into the vertex buffer
		for(int i=0;i < models.Length;i++)
		{
			//store faces per material
			Dictionary<int,	DrawDataChunk>	matChunks	=new Dictionary<int, DrawDataChunk>();

			foreach(string mat in mMaterialNames)
			{
				int	firstFace	=models[i].mFirstFace;
				int	nFaces		=models[i].mNumFaces;

				for(int face=firstFace;face < (firstFace + nFaces);face++)
				{
					QFace	f	=mFaces[face];
					TexInfo	tex	=mTexInfos[f.mTexInfo];

					if(!mat.StartsWith(tex.mMaterial))
					{
						continue;
					}

					//skip unmatched materials
					if(!MaterialCorrect.IsLightMappedAlphaAnimated(f, tex, mat))
					{
						continue;
					}

					int	matIndex	=mMaterialNames.IndexOf(mat);

					DrawDataChunk	ddc;
					if(matChunks.ContainsKey(matIndex))
					{
						ddc	=matChunks[matIndex];
					}
					else
					{
						ddc	=new DrawDataChunk();
						matChunks.Add(matIndex, ddc);
					}

					if(!MaterialFill.FillLightMappedAlphaAnimated(ddc, pp, verts, inds,
						rgbVerts, f, tex, mLightGridSize, lightData, mLMAtlas))
					{
						return	false;
					}
				}
			}
			FinishLightMappedAlphaAnimated(i, matChunks, ref vertOfs);
		}
		return	true;
	}

	internal bool BuildFullBrightData(Vector3 []verts, int[] inds, Vector3 []rgbVerts,
			Vector3 []vnorms, GFXPlane []pp, GFXModel []models)
	{
		UInt16	vertOfs	=0;	//model offset into the vertex buffer
		for(int i=0;i < models.Length;i++)
		{
			//store faces per material
			Dictionary<int,	DrawDataChunk>	matChunks	=new Dictionary<int, DrawDataChunk>();

			foreach(string mat in mMaterialNames)
			{
				int	firstFace	=models[i].mFirstFace;
				int	nFaces		=models[i].mNumFaces;

				for(int face=firstFace;face < (firstFace + nFaces);face++)
				{
					QFace	f	=mFaces[face];
					TexInfo	tex	=mTexInfos[f.mTexInfo];

					if(!mat.StartsWith(tex.mMaterial))
					{
						continue;
					}

					//skip unmatched materials
					if(!MaterialCorrect.IsFullBright(f, tex, mat))
					{
						continue;
					}

					int	matIndex	=mMaterialNames.IndexOf(mat);

					DrawDataChunk	ddc;
					if(matChunks.ContainsKey(matIndex))
					{
						ddc	=matChunks[matIndex];
					}
					else
					{
						ddc	=new DrawDataChunk();
						matChunks.Add(matIndex, ddc);
					}

					if(!MaterialFill.FillFullBright(ddc, pp, verts, inds,
						rgbVerts, vnorms, f, tex))
					{
						return	false;
					}
				}
			}
			FinishFullBright(i, matChunks, ref vertOfs);
		}
		return	true;
	}

	internal bool BuildSkyData(Vector3 []verts, int[] inds,
			GFXPlane []pp, GFXModel []models)
	{
		UInt16	vertOfs	=0;	//model offset into the vertex buffer
		for(int i=0;i < models.Length;i++)
		{
			//store faces per material
			Dictionary<int,	DrawDataChunk>	matChunks	=new Dictionary<int, DrawDataChunk>();

			foreach(string mat in mMaterialNames)
			{
				int	firstFace	=models[i].mFirstFace;
				int	nFaces		=models[i].mNumFaces;

				for(int face=firstFace;face < (firstFace + nFaces);face++)
				{
					QFace	f	=mFaces[face];
					TexInfo	tex	=mTexInfos[f.mTexInfo];

					if(!mat.StartsWith(tex.mMaterial))
					{
						continue;
					}

					//skip unmatched materials
					if(!MaterialCorrect.IsSky(f, tex, mat))
					{
						continue;
					}

					int	matIndex	=mMaterialNames.IndexOf(mat);

					DrawDataChunk	ddc;
					if(matChunks.ContainsKey(matIndex))
					{
						ddc	=matChunks[matIndex];
					}
					else
					{
						ddc	=new DrawDataChunk();
						matChunks.Add(matIndex, ddc);
					}

					if(!MaterialFill.FillSky(ddc, pp, verts, inds, f, tex))
					{
						return	false;
					}
				}
			}
			FinishSky(i, matChunks, ref vertOfs);
		}
		return	true;
	}

	internal bool BuildVLitAlphaData(Vector3 []verts, int[] inds, Vector3 []rgbVerts,
			Vector3 []vnorms, GFXPlane []pp, GFXModel []models)
	{
		UInt16	vertOfs	=0;	//model offset into the vertex buffer
		for(int i=0;i < models.Length;i++)
		{
			//store faces per material
			Dictionary<int,	DrawDataChunk>	matChunks	=new Dictionary<int, DrawDataChunk>();

			foreach(string mat in mMaterialNames)
			{
				int	firstFace	=models[i].mFirstFace;
				int	nFaces		=models[i].mNumFaces;

				for(int face=firstFace;face < (firstFace + nFaces);face++)
				{
					QFace	f	=mFaces[face];
					TexInfo	tex	=mTexInfos[f.mTexInfo];

					if(!mat.StartsWith(tex.mMaterial))
					{
						continue;
					}

					//skip unmatched materials
					if(!MaterialCorrect.IsAlpha(f, tex, mat))
					{
						continue;
					}

					int	matIndex	=mMaterialNames.IndexOf(mat);

					DrawDataChunk	ddc;
					if(matChunks.ContainsKey(matIndex))
					{
						ddc	=matChunks[matIndex];
					}
					else
					{
						ddc	=new DrawDataChunk();
						matChunks.Add(matIndex, ddc);
					}

					if(!MaterialFill.FillAlpha(ddc, pp, verts, inds,
						rgbVerts, vnorms, f, tex))
					{
						return	false;
					}
				}
			}
			FinishAlpha(i, matChunks, ref vertOfs);
		}
		return	true;
	}
*/
	public void GetLMGeometry(out int typeIndex, out Array verts, out UInt16 []inds)
	{
		if(mLMVerts.Count == 0)
		{
			typeIndex	=-1;
			verts		=null;
			inds		=null;
			return;
		}

		VPosNormTex04F	[]varray	=new VPosNormTex04F[mLMVerts.Count];
		for(int i=0;i < mLMVerts.Count;i++)
		{
			varray[i].Position		=mLMVerts[i];
			varray[i].TexCoord04.X	=mLMFaceTex0[i].X;
			varray[i].TexCoord04.Y	=mLMFaceTex0[i].Y;
			varray[i].TexCoord04.Z	=mLMFaceTex1[i].X;
			varray[i].TexCoord04.W	=mLMFaceTex1[i].Y;
			varray[i].Normal.X		=mLMNormals[i].X;
			varray[i].Normal.Y		=mLMNormals[i].Y;
			varray[i].Normal.Z		=mLMNormals[i].Z;
			varray[i].Normal.W		=1f;
		}

		typeIndex	=VertexTypes.GetIndex(varray[0].GetType());
		verts		=varray;
		inds		=mLMIndexes.ToArray();
	}

	public void GetLMAGeometry(out int typeIndex, out Array verts, out UInt16 []inds)
	{
		if(mLMAVerts.Count == 0)
		{
			typeIndex	=-1;
			verts		=null;
			inds		=null;
			return;
		}

		VPosNormTex04F	[]varray	=new VPosNormTex04F[mLMAVerts.Count];
		for(int i=0;i < mLMAVerts.Count;i++)
		{
			varray[i].Position		=mLMAVerts[i];
			varray[i].TexCoord04.X	=mLMAFaceTex0[i].X;
			varray[i].TexCoord04.Y	=mLMAFaceTex0[i].Y;
			varray[i].TexCoord04.Z	=mLMAFaceTex1[i].X;
			varray[i].TexCoord04.W	=mLMAFaceTex1[i].Y;
			varray[i].Normal.X		=mLMANormals[i].X;
			varray[i].Normal.Y		=mLMANormals[i].Y;
			varray[i].Normal.Z		=mLMANormals[i].Z;
			varray[i].Normal.W		=(float)mLMAColors[i].A / 255f;
		}

		typeIndex	=VertexTypes.GetIndex(varray[0].GetType());
		verts		=varray;
		inds		=mLMAIndexes.ToArray();
	}

	public void GetVLitGeometry(out int typeIndex, out Array verts, out UInt16 []inds)
	{
		if(mVLitVerts.Count == 0)
		{
			typeIndex	=-1;
			verts		=null;
			inds		=null;
			return;
		}

		VPosNormTex0Col0F	[]varray	=new VPosNormTex0Col0F[mVLitVerts.Count];
		for(int i=0;i < mVLitVerts.Count;i++)
		{
			varray[i].Position	=mVLitVerts[i];
			varray[i].TexCoord0	=mVLitTex0[i];
			varray[i].Color0	=mVLitColors[i];

			varray[i].Normal	=new Vector4(mVLitNormals[i].X,
				mVLitNormals[i].Y, mVLitNormals[i].Z, 1f);
		}

		typeIndex	=VertexTypes.GetIndex(varray[0].GetType());
		verts		=varray;
		inds		=mVLitIndexes.ToArray();
	}

	public void GetAlphaGeometry(out int typeIndex, out Array verts, out UInt16 []inds)
	{
		if(mAlphaVerts.Count == 0)
		{
			typeIndex	=-1;
			verts		=null;
			inds		=null;
			return;
		}

		VPosNormTex0Col0F	[]varray	=new VPosNormTex0Col0F[mAlphaVerts.Count];
		for(int i=0;i < mAlphaVerts.Count;i++)
		{
			varray[i].Position	=mAlphaVerts[i];
			varray[i].TexCoord0	=mAlphaTex0[i];
			varray[i].Color0	=mAlphaColors[i];
			varray[i].Normal	=new Vector4(mAlphaNormals[i].X,
				mAlphaNormals[i].Y, mAlphaNormals[i].Z, 1f);
		}

		typeIndex	=VertexTypes.GetIndex(varray[0].GetType());
		verts		=varray;
		inds		=mAlphaIndexes.ToArray();
	}

	public void GetFullBrightGeometry(out int typeIndex, out Array verts, out UInt16 []inds)
	{
		if(mFBVerts.Count == 0)
		{
			typeIndex	=-1;
			verts		=null;
			inds		=null;
			return;
		}

		VPosNormTex0F	[]varray	=new VPosNormTex0F[mFBVerts.Count];
		for(int i=0;i < mFBVerts.Count;i++)
		{
			varray[i].Position	=mFBVerts[i];
			varray[i].TexCoord0	=mFBTex0[i];
			varray[i].Normal	=new Vector4(mFBNormals[i].X,
				mFBNormals[i].Y, mFBNormals[i].Z, 1f);
		}

		typeIndex	=VertexTypes.GetIndex(varray[0].GetType());
		verts		=varray;
		inds		=mFBIndexes.ToArray();
	}

	internal void GetMirrorGeometry(out int typeIndex, out Array verts, out UInt16 []inds)
	{
		if(mMirrorVerts.Count == 0)
		{
			typeIndex	=-1;
			verts		=null;
			inds		=null;
			return;
		}

		VPosNormTex0Tex1Col0	[]varray	=new VPosNormTex0Tex1Col0[mMirrorVerts.Count];
		for(int i=0;i < mMirrorVerts.Count;i++)
		{
			varray[i].Position	=mMirrorVerts[i];
			varray[i].TexCoord0	=mMirrorTex0[i];
			varray[i].TexCoord1	=mMirrorTex1[i];
			varray[i].Color0	=mMirrorColors[i];
			varray[i].Normal	=new Half4(mMirrorNormals[i].X,
				mMirrorNormals[i].Y, mMirrorNormals[i].Z, 1f);
		}

		typeIndex	=VertexTypes.GetIndex(varray[0].GetType());
		verts		=varray;
		inds		=mMirrorIndexes.ToArray();
	}

	public void GetSkyGeometry(out int typeIndex, out Array verts, out UInt16 []inds)
	{
		if(mSkyVerts.Count == 0)
		{
			typeIndex	=-1;
			verts		=null;
			inds		=null;
			return;
		}

		VPosTex0	[]varray	=new VPosTex0[mSkyVerts.Count];
		for(int i=0;i < mSkyVerts.Count;i++)
		{
			varray[i].Position	=mSkyVerts[i];
			varray[i].TexCoord0	=mSkyTex0[i];
		}

		typeIndex	=VertexTypes.GetIndex(varray[0].GetType());
		verts		=varray;
		inds		=mSkyIndexes.ToArray();
	}

	public void GetLMAnimGeometry(out int typeIndex, out Array verts, out UInt16 []inds)
	{
		if(mLMAnimVerts.Count == 0)
		{
			typeIndex	=-1;
			verts		=null;
			inds		=null;
			return;
		}

		VPosNormTex04Tex14Tex24Color0F	[]varray
			=new VPosNormTex04Tex14Tex24Color0F[mLMAnimVerts.Count];
		for(int i=0;i < mLMAnimVerts.Count;i++)
		{
			varray[i].Position		=mLMAnimVerts[i];
			varray[i].Normal.X		=mLMAnimNormals[i].X;
			varray[i].Normal.Y		=mLMAnimNormals[i].Y;
			varray[i].Normal.Z		=mLMAnimNormals[i].Z;
			varray[i].Normal.W		=1f;
			varray[i].TexCoord04.X	=mLMAnimFaceTex0[i].X;
			varray[i].TexCoord04.Y	=mLMAnimFaceTex0[i].Y;
			varray[i].TexCoord04.Z	=mLMAnimFaceTex1[i].X;
			varray[i].TexCoord04.W	=mLMAnimFaceTex1[i].Y;
			varray[i].TexCoord14.X	=mLMAnimFaceTex2[i].X;
			varray[i].TexCoord14.Y	=mLMAnimFaceTex2[i].Y;
			varray[i].TexCoord14.Z	=mLMAnimFaceTex3[i].X;
			varray[i].TexCoord14.W	=mLMAnimFaceTex3[i].Y;
			varray[i].TexCoord24.X	=mLMAnimFaceTex4[i].X;
			varray[i].TexCoord24.Y	=mLMAnimFaceTex4[i].Y;
			varray[i].TexCoord24.Z	=1.0f;	//alpha
			varray[i].TexCoord24.W	=69.0f;	//nothin
			varray[i].Color0		=mLMAnimStyle[i];
		}

		typeIndex	=VertexTypes.GetIndex(varray[0].GetType());
		verts		=varray;
		inds		=mLMAnimIndexes.ToArray();
	}


	public void GetLMAAnimGeometry(out int typeIndex, out Array verts, out UInt16 []inds)
	{
		if(mLMAAnimVerts.Count == 0)
		{
			typeIndex	=-1;
			verts		=null;
			inds		=null;
			return;
		}

		VPosNormTex04Tex14Tex24Color0F	[]varray
			=new VPosNormTex04Tex14Tex24Color0F[mLMAAnimVerts.Count];
		for(int i=0;i < mLMAAnimVerts.Count;i++)
		{
			varray[i].Position		=mLMAAnimVerts[i];
			varray[i].Normal.X		=mLMAAnimNormals[i].X;
			varray[i].Normal.Y		=mLMAAnimNormals[i].Y;
			varray[i].Normal.Z		=mLMAAnimNormals[i].Z;
			varray[i].Normal.W		=1f;
			varray[i].TexCoord04.X	=mLMAAnimFaceTex0[i].X;
			varray[i].TexCoord04.Y	=mLMAAnimFaceTex0[i].Y;
			varray[i].TexCoord04.Z	=mLMAAnimFaceTex1[i].X;
			varray[i].TexCoord04.W	=mLMAAnimFaceTex1[i].Y;
			varray[i].TexCoord14.X	=mLMAAnimFaceTex2[i].X;
			varray[i].TexCoord14.Y	=mLMAAnimFaceTex2[i].Y;
			varray[i].TexCoord14.Z	=mLMAAnimFaceTex3[i].X;
			varray[i].TexCoord14.W	=mLMAAnimFaceTex3[i].Y;
			varray[i].TexCoord24.X	=mLMAAnimFaceTex4[i].X;
			varray[i].TexCoord24.Y	=mLMAAnimFaceTex4[i].Y;
			varray[i].TexCoord24.Z	=(float)mLMAAnimColors[i].A / 255f;	//alpha
			varray[i].TexCoord24.W	=69.0f;	//nothin
			varray[i].Color0		=mLMAAnimStyle[i];
		}

		typeIndex	=VertexTypes.GetIndex(varray[0].GetType());
		verts		=varray;
		inds		=mLMAAnimIndexes.ToArray();
	}


	public List<string> GetMaterialNames()
	{
		return	mMaterialNames;
	}


	void CalcMaterials()
	{
		//build material list
		foreach(string matName in mMaterialNames)
		{
			bool	bCel	=false;
			string	mn		=matName;
			if(mn.Contains("*Cel"))
			{
				bCel	=true;
			}

			//grab all the info that used to be contained
			//within effect techniques
			string	vs					="";
			string	ps					="";
			string	blendState			="";
			string	depthState			="";

			//set some parameter defaults
			if(mn.EndsWith("*Alpha"))
			{
				blendState			="AlphaBlending";
				depthState			="DisableDepthWrite";
				if(bCel)
				{
					//old effect tech VertexLightingAlphaCel
					vs	="VertexLitVS";
					ps	="VertexLitCelPS";
				}
				else
				{
					//old effect tech VertexLightingAlpha
					vs	="VertexLitVS";
					ps	="VertexLitPS";
				}
			}
			else if(mn.EndsWith("*LitAlpha"))
			{
				blendState			="AlphaBlending";
				depthState			="DisableDepthWrite";
				if(bCel)
				{
					//old effect tech LightMapAlphaCel
					vs	="LightMapVS";
					ps	="LightMapCelPS";
				}
				else
				{
					//old effect tech LightMapAlpha
					vs	="LightMapVS";
					ps	="LightMapPS";
				}
			}
			else if(mn.EndsWith("*LitAlphaAnim"))
			{
				blendState			="AlphaBlending";
				depthState			="DisableDepthWrite";
				if(bCel)
				{
					//old effect tech LightMapAnimAlphaCel
					vs	="LightMapAnimVS";
					ps	="LightMapAnimCelPS";
				}
				else
				{
					//old effect tech LightMapAnimAlpha
					vs	="LightMapAnimVS";
					ps	="LightMapAnimPS";
				}
			}
			else if(mn.EndsWith("*VertLit"))
			{
				blendState			="NoBlending";
				depthState			="EnableDepth";
				if(bCel)
				{
					//old effect tech VertexLightingCel
					vs	="VertexLitVS";
					ps	="VertexLitCelPS";
				}
				else
				{
					//old effect tech VertexLighting
					vs	="VertexLitVS";
					ps	="VertexLitPS";
				}
			}
			else if(mn.EndsWith("*FullBright"))
			{
				//old effect tech FullBright
				blendState			="NoBlending";
				depthState			="EnableDepth";
				vs					="FullBrightVS";
				ps					="VertexLitPS";
			}
			else if(mn.EndsWith("*Mirror"))
			{
				//old effect tech Mirror
				//but I will tackle this one later
				blendState			="NoBlending";
				depthState			="EnableDepth";
				vs					="VertexLitVS";
				ps					="VertexLitPS";
			}
			else if(mn.EndsWith("*Sky"))
			{
				//old effect tech Sky
				blendState			="NoBlending";
				depthState			="EnableDepth";
				vs					="SkyVS";
				ps					="SkyPS";
			}
			else if(mn.EndsWith("*Anim"))
			{
				blendState			="NoBlending";
				depthState			="EnableDepth";
				if(bCel)
				{
					//old effect tech LightMapAnimCel
					vs	="LightMapAnimVS";
					ps	="LightMapAnimCelPS";
				}
				else
				{
					//old effect tech LightMapAnim
					vs	="LightMapAnimVS";
					ps	="LightMapAnimPS";
				}
			}
			else
			{
				blendState			="NoBlending";
				depthState			="EnableDepth";
				if(bCel)
				{
					//old effect tech LightMapCel
					vs	="LightMapVS";
					ps	="LightMapCelPS";
				}
				else
				{
					//old effect tech LightMap
					vs	="LightMapVS";
					ps	="LightMapPS";
				}
			}

			//The main material only takes care of the 0 pass.
			//A single material will be used for all DMN passes
			//in the categories above, same for shadows
			mMatLib.CreateMaterial(matName, true, false);
			mMatLib.SetMaterialStates(matName, blendState, depthState);
			mMatLib.SetMaterialVShader(matName, vs);
			mMatLib.SetMaterialPShader(matName, ps);
		}

		//create generic DMN materials

		//vertlit covers vlit, *Alpha, *Mirror
		mMatLib.CreateMaterial("VertexLitDMN", true, false);
		mMatLib.SetMaterialStates("VertexLitDMN", "NoBlending", "EnableDepth");
		mMatLib.SetMaterialVShader("VertexLitDMN", "VertexLitVS");
		mMatLib.SetMaterialPShader("VertexLitDMN", "VertexLitDMNPS");
		mMatLib.CreateMaterial("VertexLitShadow", true, false);
		mMatLib.SetMaterialStates("VertexLitShadow", "ShadowBlending", "ShadowDepth");
		mMatLib.SetMaterialVShader("VertexLitShadow", "VertexLitVS");
		mMatLib.SetMaterialPShader("VertexLitShadow", "VertexLitShadowPS");

		//covers *LitAlpha, LightMap
		mMatLib.CreateMaterial("LightMapDMN", true, false);
		mMatLib.SetMaterialStates("LightMapDMN", "NoBlending", "EnableDepth");
		mMatLib.SetMaterialVShader("LightMapDMN", "LightMapVS");
		mMatLib.SetMaterialPShader("LightMapDMN", "LightMapDMNPS");
		mMatLib.CreateMaterial("LightMapShadow", true, false);
		mMatLib.SetMaterialStates("LightMapShadow", "ShadowBlending", "ShadowDepth");
		mMatLib.SetMaterialVShader("LightMapShadow", "LightMapVS");
		mMatLib.SetMaterialPShader("LightMapShadow", "LightMapShadowPS");

		//*LightAlphaAnim, *Anim
		mMatLib.CreateMaterial("LightMapAnimDMN", true, false);
		mMatLib.SetMaterialStates("LightMapAnimDMN", "NoBlending", "EnableDepth");
		mMatLib.SetMaterialVShader("LightMapAnimDMN", "LightMapAnimVS");
		mMatLib.SetMaterialPShader("LightMapAnimDMN", "LightMapAnimDMNPS");
		mMatLib.CreateMaterial("LightMapAnimShadow", true, false);
		mMatLib.SetMaterialStates("LightMapAnimShadow", "ShadowBlending", "ShadowDepth");
		mMatLib.SetMaterialVShader("LightMapAnimShadow", "LightMapAnimVS");
		mMatLib.SetMaterialPShader("LightMapAnimShadow", "LightMapAnimShadowPS");

		//fullbright
		mMatLib.CreateMaterial("FullBrightDMN", true, false);
		mMatLib.SetMaterialStates("FullBrightDMN", "NoBlending", "EnableDepth");
		mMatLib.SetMaterialVShader("FullBrightDMN", "FullBrightVS");
		mMatLib.SetMaterialPShader("FullBrightDMN", "VertexLitDMNPS");
		mMatLib.CreateMaterial("FullBrightShadow", true, false);
		mMatLib.SetMaterialStates("FullBrightShadow", "ShadowBlending", "ShadowDepth");
		mMatLib.SetMaterialVShader("FullBrightShadow", "FullBrightVS");
		mMatLib.SetMaterialPShader("FullBrightShadow", "VertexLitShadowPS");

		//no casting shadows on the sky
		mMatLib.CreateMaterial("SkyDMN", true, false);
		mMatLib.SetMaterialStates("SkyDMN", "NoBlending", "EnableDepth");
		mMatLib.SetMaterialVShader("SkyDMN", "SkyVS");
		mMatLib.SetMaterialPShader("SkyDMN", "SkyDMNPS");
	}


	void CalcMaterialNames()
	{
		mMaterialNames.Clear();

		if(mFaces == null)
		{
			return;
		}

		foreach(QFace f in mFaces)
		{
			string	matName	=TexInfo.ScryTrueName(f, mTexInfos[f.mTexInfo]);

			if(!mMaterialNames.Contains(matName))
			{
				mMaterialNames.Add(matName);
			}
		}
	}


	internal TexAtlas GetLightMapAtlas()
	{
		return	mLMAtlas;
	}


	//for opaques
	static void StuffVBArrays(Dictionary<int, DrawDataChunk> matChunks,
		List<Vector3> verts, List<Vector3> norms, List<Vector2> tex0,
		List<Vector2> tex1, List<Vector2> tex2, List<Vector2> tex3, 
		List<Vector2> tex4, List<Color> colors, List<Color> styles)
	{
		foreach(KeyValuePair<int, DrawDataChunk> matChunk in matChunks)
		{
			verts.AddRange(matChunk.Value.mVerts);

			if(norms != null)
			{
				norms.AddRange(matChunk.Value.mNorms);
			}
			if(tex0 != null)
			{
				tex0.AddRange(matChunk.Value.mTex0);
			}
			if(tex1 != null)
			{
				tex1.AddRange(matChunk.Value.mTex1);
			}
			if(tex2 != null)
			{
				tex2.AddRange(matChunk.Value.mTex2);
			}
			if(tex3 != null)
			{
				tex3.AddRange(matChunk.Value.mTex3);
			}
			if(tex4 != null)
			{
				tex4.AddRange(matChunk.Value.mTex4);
			}
			if(colors != null)
			{
				colors.AddRange(matChunk.Value.mColors);
			}
			if(styles != null)
			{
				styles.AddRange(matChunk.Value.mStyles);
			}
		}
	}

	List<DrawCall> ComputeIndexes(List<UInt16> inds, Dictionary<int, DrawDataChunk> matChunks, ref UInt16 vertOfs)
	{
		List<DrawCall>	draws	=new List<DrawCall>();

		foreach(KeyValuePair<int, DrawDataChunk> matChunk in matChunks)
		{
			int	cnt	=inds.Count;

			DrawCall	dc		=new DrawCall();				
			dc.mStartIndex		=cnt;
			dc.mMaterialID		=matChunk.Key;

			for(int i=0;i < matChunk.Value.mNumFaces;i++)
			{
				int	nverts	=matChunk.Value.mVCounts[i];

				//triangulate
				for(UInt16 k=1;k < nverts-1;k++)
				{
					inds.Add(vertOfs);
					inds.Add((UInt16)(vertOfs + k));
					inds.Add((UInt16)(vertOfs + ((k + 1) % nverts)));
				}

				vertOfs	+=(UInt16)nverts;
			}

			dc.mCount	=(inds.Count - cnt);

			draws.Add(dc);
		}
		return	draws;
	}
}