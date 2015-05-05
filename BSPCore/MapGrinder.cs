using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using MaterialLib;
using MeshLib;
using UtilityLib;

using SharpDX;

using MatLib	=MaterialLib.MaterialLib;


namespace BSPCore
{
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

		//computed material stuff
		List<string>	mMaterialNames		=new List<string>();

		//material draw call information
		//opaques
		//indexed by material and model number
		Dictionary<int, List<DrawCall>>	mLMDraws		=new Dictionary<int, List<DrawCall>>();
		Dictionary<int, List<DrawCall>>	mVLitDraws		=new Dictionary<int, List<DrawCall>>();
		Dictionary<int, List<DrawCall>>	mLMAnimDraws	=new Dictionary<int, List<DrawCall>>();
		Dictionary<int, List<DrawCall>>	mFBDraws		=new Dictionary<int, List<DrawCall>>();
		Dictionary<int, List<DrawCall>>	mSkyDraws		=new Dictionary<int, List<DrawCall>>();
		Dictionary<int, List<DrawCall>>	mMirrorDraws	=new Dictionary<int, List<DrawCall>>();

		//alphas
		Dictionary<int, List<List<DrawCall>>>	mLMADraws		=new Dictionary<int, List<List<DrawCall>>>();
		Dictionary<int, List<List<DrawCall>>>	mAlphaDraws		=new Dictionary<int, List<List<DrawCall>>>();
		Dictionary<int, List<List<DrawCall>>>	mLMAAnimDraws	=new Dictionary<int, List<List<DrawCall>>>();

		//computed lightmap atlas
		TexAtlas	mLMAtlas;

		//passed in data
		int			mLightGridSize;
		GFXTexInfo	[]mTexInfos;
		GFXFace		[]mFaces;

		//delegates
		internal delegate bool IsCorrectMaterial(GFXFace f, GFXTexInfo tex, string matName);
		internal delegate bool FillDrawChunk(DrawDataChunk ddc, GFXPlane []pp,
											Vector3 []verts, int []indexes, Vector3 []rgbVerts, Vector3 []vnorms,
											GFXFace f, GFXTexInfo tex, int lightGridSize,
											byte []lightData, TexAtlas atlas,
											List<List<Vector3>> mirrorPolys);
		internal delegate void FinishUp(int modelIndex, List<DrawDataChunk> matChunks, ref UInt16 vertOfs);
		internal delegate void FinishUpAlpha(int modelIndex, List<Dictionary<Int32, DrawDataChunk>> perPlaneChunk, ref UInt16 vertOfs);

		public MapGrinder(GraphicsDevice gd,
			StuffKeeper sk, MatLib matLib,
			GFXTexInfo []texs, GFXFace []faces,
			int lightGridSize, int atlasSize)
		{
			mGD				=gd;
			mMatLib			=matLib;
			mTexInfos		=texs;
			mLightGridSize	=lightGridSize;
			mFaces			=faces;

			if(mMatLib == null)
			{
				mMatLib	=new MatLib(gd, sk);
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


		internal void GetLMMaterialData(out Dictionary<int, List<DrawCall>> draws)
		{
			draws	=mLMDraws;
		}


		internal void GetLMAnimMaterialData(out Dictionary<int, List<DrawCall>> draws)
		{
			draws	=mLMAnimDraws;
		}


		internal void GetLMAMaterialData(out Dictionary<int, List<List<DrawCall>>> draws)
		{
			draws	=mLMADraws;
		}


		internal void GetAlphaMaterialData(out Dictionary<int, List<List<DrawCall>>> draws)
		{
			draws	=mAlphaDraws;
		}


		internal void GetVLitMaterialData(out Dictionary<int, List<DrawCall>> draws)
		{
			draws	=mVLitDraws;
		}


		internal void GetFullBrightMaterialData(out Dictionary<int, List<DrawCall>> draws)
		{
			draws	=mFBDraws;
		}


		internal void GetSkyMaterialData(out Dictionary<int, List<DrawCall>> draws)
		{
			draws	=mSkyDraws;
		}


		internal void GetMirrorMaterialData(out Dictionary<int, List<DrawCall>> draws, out List<List<Vector3>> polys)
		{
			draws	=mMirrorDraws;
			polys	=mMirrorPolys;
		}


		internal void GetLMAAnimMaterialData(out Dictionary<int, List<List<DrawCall>>> draws)
		{
			draws	=mLMAAnimDraws;
		}


		internal void GetLMGeometry(out int typeIndex, out Array verts, out UInt16 []inds)
		{
			if(mLMVerts.Count == 0)
			{
				typeIndex	=-1;
				verts		=null;
				inds		=null;
				return;
			}

			VPosNormTex04	[]varray	=new VPosNormTex04[mLMVerts.Count];
			for(int i=0;i < mLMVerts.Count;i++)
			{
				varray[i].Position		=mLMVerts[i];
				varray[i].TexCoord0.X	=mLMFaceTex0[i].X;
				varray[i].TexCoord0.Y	=mLMFaceTex0[i].Y;
				varray[i].TexCoord0.Z	=mLMFaceTex1[i].X;
				varray[i].TexCoord0.W	=mLMFaceTex1[i].Y;
				varray[i].Normal.X		=mLMNormals[i].X;
				varray[i].Normal.Y		=mLMNormals[i].Y;
				varray[i].Normal.Z		=mLMNormals[i].Z;
				varray[i].Normal.W		=1f;
			}

			typeIndex	=VertexTypes.GetIndex(varray[0].GetType());
			verts		=varray;
			inds		=mLMIndexes.ToArray();
		}


		internal void GetLMAGeometry(out int typeIndex, out Array verts, out UInt16 []inds)
		{
			if(mLMAVerts.Count == 0)
			{
				typeIndex	=-1;
				verts		=null;
				inds		=null;
				return;
			}

			VPosNormTex04	[]varray	=new VPosNormTex04[mLMAVerts.Count];
			for(int i=0;i < mLMAVerts.Count;i++)
			{
				varray[i].Position		=mLMAVerts[i];
				varray[i].TexCoord0.X	=mLMAFaceTex0[i].X;
				varray[i].TexCoord0.Y	=mLMAFaceTex0[i].Y;
				varray[i].TexCoord0.Z	=mLMAFaceTex1[i].X;
				varray[i].TexCoord0.W	=mLMAFaceTex1[i].Y;
				varray[i].Normal.X		=mLMANormals[i].X;
				varray[i].Normal.Y		=mLMANormals[i].Y;
				varray[i].Normal.Z		=mLMANormals[i].Z;
				varray[i].Normal.W		=(float)mLMAColors[i].A / 255f;
			}

			typeIndex	=VertexTypes.GetIndex(varray[0].GetType());
			verts		=varray;
			inds		=mLMAIndexes.ToArray();
		}


		internal void GetVLitGeometry(out int typeIndex, out Array verts, out UInt16 []inds)
		{
			if(mVLitVerts.Count == 0)
			{
				typeIndex	=-1;
				verts		=null;
				inds		=null;
				return;
			}

			VPosNormTex0Col0	[]varray	=new VPosNormTex0Col0[mVLitVerts.Count];
			for(int i=0;i < mVLitVerts.Count;i++)
			{
				varray[i].Position	=mVLitVerts[i];
				varray[i].TexCoord0	=mVLitTex0[i];
				varray[i].Color0	=mVLitColors[i];
				varray[i].Normal.X	=mVLitNormals[i].X;
				varray[i].Normal.Y	=mVLitNormals[i].Y;
				varray[i].Normal.Z	=mVLitNormals[i].Z;
				varray[i].Normal.W	=1f;
			}

			typeIndex	=VertexTypes.GetIndex(varray[0].GetType());
			verts		=varray;
			inds		=mVLitIndexes.ToArray();
		}


		internal void GetAlphaGeometry(out int typeIndex, out Array verts, out UInt16 []inds)
		{
			if(mAlphaVerts.Count == 0)
			{
				typeIndex	=-1;
				verts		=null;
				inds		=null;
				return;
			}

			VPosNormTex0Col0	[]varray	=new VPosNormTex0Col0[mAlphaVerts.Count];
			for(int i=0;i < mAlphaVerts.Count;i++)
			{
				varray[i].Position	=mAlphaVerts[i];
				varray[i].TexCoord0	=mAlphaTex0[i];
				varray[i].Color0	=mAlphaColors[i];
				varray[i].Normal.X	=mAlphaNormals[i].X;
				varray[i].Normal.Y	=mAlphaNormals[i].Y;
				varray[i].Normal.Z	=mAlphaNormals[i].Z;
				varray[i].Normal.W	=1f;
			}

			typeIndex	=VertexTypes.GetIndex(varray[0].GetType());
			verts		=varray;
			inds		=mAlphaIndexes.ToArray();
		}


		internal void GetFullBrightGeometry(out int typeIndex, out Array verts, out UInt16 []inds)
		{
			if(mFBVerts.Count == 0)
			{
				typeIndex	=-1;
				verts		=null;
				inds		=null;
				return;
			}

			VPosNormTex0	[]varray	=new VPosNormTex0[mFBVerts.Count];
			for(int i=0;i < mFBVerts.Count;i++)
			{
				varray[i].Position	=mFBVerts[i];
				varray[i].TexCoord0	=mFBTex0[i];
				varray[i].Normal.X	=mFBNormals[i].X;
				varray[i].Normal.Y	=mFBNormals[i].Y;
				varray[i].Normal.Z	=mFBNormals[i].Z;
				varray[i].Normal.W	=1f;
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
				varray[i].Normal.X	=mMirrorNormals[i].X;
				varray[i].Normal.Y	=mMirrorNormals[i].Y;
				varray[i].Normal.Z	=mMirrorNormals[i].Z;
				varray[i].Normal.W	=1f;
			}

			typeIndex	=VertexTypes.GetIndex(varray[0].GetType());
			verts		=varray;
			inds		=mMirrorIndexes.ToArray();
		}


		internal void GetSkyGeometry(out int typeIndex, out Array verts, out UInt16 []inds)
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


		internal void GetLMAnimGeometry(out int typeIndex, out Array verts, out UInt16 []inds)
		{
			if(mLMAnimVerts.Count == 0)
			{
				typeIndex	=-1;
				verts		=null;
				inds		=null;
				return;
			}

			VPosNormTex04Tex14Tex24Color0	[]varray
				=new VPosNormTex04Tex14Tex24Color0[mLMAnimVerts.Count];
			for(int i=0;i < mLMAnimVerts.Count;i++)
			{
				varray[i].Position		=mLMAnimVerts[i];
				varray[i].Normal.X		=mLMAnimNormals[i].X;
				varray[i].Normal.Y		=mLMAnimNormals[i].Y;
				varray[i].Normal.Z		=mLMAnimNormals[i].Z;
				varray[i].Normal.W		=1f;
				varray[i].TexCoord0.X	=mLMAnimFaceTex0[i].X;
				varray[i].TexCoord0.Y	=mLMAnimFaceTex0[i].Y;
				varray[i].TexCoord0.Z	=mLMAnimFaceTex1[i].X;
				varray[i].TexCoord0.W	=mLMAnimFaceTex1[i].Y;
				varray[i].TexCoord1.X	=mLMAnimFaceTex2[i].X;
				varray[i].TexCoord1.Y	=mLMAnimFaceTex2[i].Y;
				varray[i].TexCoord1.Z	=mLMAnimFaceTex3[i].X;
				varray[i].TexCoord1.W	=mLMAnimFaceTex3[i].Y;
				varray[i].TexCoord2.X	=mLMAnimFaceTex4[i].X;
				varray[i].TexCoord2.Y	=mLMAnimFaceTex4[i].Y;
				varray[i].TexCoord2.Z	=1.0f;	//alpha
				varray[i].TexCoord2.W	=69.0f;	//nothin
				varray[i].TexCoord3		=mLMAnimStyle[i];
			}

			typeIndex	=VertexTypes.GetIndex(varray[0].GetType());
			verts		=varray;
			inds		=mLMAnimIndexes.ToArray();
		}


		internal void GetLMAAnimGeometry(out int typeIndex, out Array verts, out UInt16 []inds)
		{
			if(mLMAAnimVerts.Count == 0)
			{
				typeIndex	=-1;
				verts		=null;
				inds		=null;
				return;
			}

			VPosNormTex04Tex14Tex24Color0	[]varray
				=new VPosNormTex04Tex14Tex24Color0[mLMAAnimVerts.Count];
			for(int i=0;i < mLMAAnimVerts.Count;i++)
			{
				varray[i].Position		=mLMAnimVerts[i];
				varray[i].Normal.X		=mLMAnimNormals[i].X;
				varray[i].Normal.Y		=mLMAnimNormals[i].Y;
				varray[i].Normal.Z		=mLMAnimNormals[i].Z;
				varray[i].Normal.W		=1f;
				varray[i].TexCoord0.X	=mLMAnimFaceTex0[i].X;
				varray[i].TexCoord0.Y	=mLMAnimFaceTex0[i].Y;
				varray[i].TexCoord0.Z	=mLMAnimFaceTex1[i].X;
				varray[i].TexCoord0.W	=mLMAnimFaceTex1[i].Y;
				varray[i].TexCoord1.X	=mLMAnimFaceTex2[i].X;
				varray[i].TexCoord1.Y	=mLMAnimFaceTex2[i].Y;
				varray[i].TexCoord1.Z	=mLMAnimFaceTex3[i].X;
				varray[i].TexCoord1.W	=mLMAnimFaceTex3[i].Y;
				varray[i].TexCoord2.X	=mLMAnimFaceTex4[i].X;
				varray[i].TexCoord2.Y	=mLMAnimFaceTex4[i].Y;
				varray[i].TexCoord2.Z	=mLMAAnimColors[i].A;	//alpha
				varray[i].TexCoord2.W	=69.0f;	//nothin
				varray[i].TexCoord3.A	=mLMAAnimStyle[i].R;
			}

			typeIndex	=VertexTypes.GetIndex(varray[0].GetType());
			verts		=varray;
			inds		=mLMAAnimIndexes.ToArray();
		}


		//drawcall builder that uses material specific callbacks
		//to do the internal grinding
		internal bool BuildFaceData(Vector3 []verts, int[] indexes,
			Vector3 []rgbVerts, Vector3 []vnorms,
			object pobj, GFXModel []models, byte []lightData,
			IsCorrectMaterial correct, FillDrawChunk fill, FinishUp fin)
		{
			GFXPlane	[]pp	=pobj as GFXPlane [];

			UInt16	vertOfs	=0;	//model offsets
			for(int i=0;i < models.Length;i++)
			{
				//store faces per material
				List<DrawDataChunk>	matChunks	=new List<DrawDataChunk>();

				foreach(string mat in mMaterialNames)
				{
					DrawDataChunk	ddc	=new DrawDataChunk();
					matChunks.Add(ddc);

					//skip on material name
					if(!correct(null, null, mat))
					{
						continue;
					}

					int	firstFace	=models[i].mFirstFace;
					int	nFaces		=models[i].mNumFaces;

					for(int face=firstFace;face < (firstFace + nFaces);face++)
					{
						GFXFace		f	=mFaces[face];
						GFXTexInfo	tex	=mTexInfos[f.mTexInfo];

						if(!correct(f, tex, mat))
						{
							continue;
						}
						
						if(!fill(ddc, pp, verts, indexes, rgbVerts, vnorms, f, tex, mLightGridSize, lightData, mLMAtlas, mMirrorPolys))
						{
							return	false;
						}
					}
				}
				fin(i, matChunks, ref vertOfs);
			}

			return	true;
		}


		//alphas come in per face chunks with a sort point
		internal bool BuildAlphaFaceData(Vector3 []verts, int[] indexes,
			Vector3 []rgbVerts, Vector3 []vnorms,
			object pobj, GFXModel []models, byte []lightData,
			IsCorrectMaterial correct, FillDrawChunk fill, FinishUpAlpha fin)
		{
			GFXPlane	[]pp	=pobj as GFXPlane [];

			UInt16	vertOfs	=0;	//model offsets
			for(int i=0;i < models.Length;i++)
			{
				//store each face used, and how many faces per material
				List<Dictionary<Int32, DrawDataChunk>>	perFaceChunks
					=new List<Dictionary<Int32, DrawDataChunk>>();

				foreach(string mat in mMaterialNames)
				{
					Dictionary<Int32, DrawDataChunk>	ddcs
						=new Dictionary<Int32, DrawDataChunk>();
					perFaceChunks.Add(ddcs);

					if(!correct(null, null, mat))
					{
						continue;
					}

					int	firstFace	=models[i].mFirstFace;
					int	nFaces		=models[i].mNumFaces;

					for(int face=firstFace;face < (firstFace + nFaces);face++)
					{
						GFXFace		f	=mFaces[face];
						GFXTexInfo	tex	=mTexInfos[f.mTexInfo];

						if(!correct(f, tex, mat))
						{
							continue;
						}

						DrawDataChunk	ddc	=null;

						if(ddcs.ContainsKey(face))
						{
							Debug.Assert(false);
						}
						else
						{
							ddc	=new DrawDataChunk();
						}

						if(!fill(ddc, pp, verts, indexes, rgbVerts, vnorms, f, tex, mLightGridSize, lightData, mLMAtlas, mMirrorPolys))
						{
							return	false;
						}

						if(!ddcs.ContainsKey(face))
						{
							ddcs.Add(face, ddc);
						}
					}
				}
				fin(i, perFaceChunks, ref vertOfs);
			}
			return	true;
		}


		internal bool BuildLMAnimFaceData(Vector3 []verts, int[] indexes,
			byte []lightData, object pobj, GFXModel []models)
		{
			return	BuildFaceData(verts, indexes, null, null, pobj, models, lightData,
				MaterialCorrect.IsLightMapAnimated,
				MaterialFill.FillLightMapAnimated,
				FinishLightMapAnimated);
		}


		internal bool BuildLMAAnimFaceData(Vector3 []verts, int[] indexes,
			byte []lightData, object pobj, GFXModel []models)
		{
			return	BuildAlphaFaceData(verts, indexes, null, null, pobj, models, lightData,
				MaterialCorrect.IsLightMappedAlphaAnimated,
				MaterialFill.FillLightMappedAlphaAnimated,
				FinishLightMappedAlphaAnimated);
		}


		internal bool BuildLMFaceData(Vector3 []verts, int[] indexes,
			byte []lightData, object pobj, GFXModel []models)
		{
			return	BuildFaceData(verts, indexes, null, null, pobj, models, lightData,
				MaterialCorrect.IsLightMapped,
				MaterialFill.FillLightMapped,
				FinishLightMapped);
		}


		internal bool BuildLMAFaceData(Vector3 []verts, int[] indexes,
			byte []lightData, object pobj, GFXModel []models)
		{
			return	BuildAlphaFaceData(verts, indexes, null, null, pobj, models, lightData,
				MaterialCorrect.IsLightMappedAlpha,
				MaterialFill.FillLightMappedAlpha,
				FinishLightMappedAlpha);
		}


		internal bool BuildVLitFaceData(Vector3 []verts, int[] indexes,	Vector3 []rgbVerts,
			Vector3 []vnorms, object pobj, GFXModel []models)
		{
			return	BuildFaceData(verts, indexes, rgbVerts, vnorms, pobj, models, null,
				MaterialCorrect.IsVLit,
				MaterialFill.FillVLit,
				FinishVLit);
		}


		internal bool BuildMirrorFaceData(Vector3 []verts, int[] indexes,	Vector3 []rgbVerts,
			Vector3 []vnorms, object pobj, GFXModel []models)
		{
			return	BuildFaceData(verts, indexes, rgbVerts, vnorms, pobj, models, null,
				MaterialCorrect.IsMirror,
				MaterialFill.FillMirror,
				FinishMirror);
		}


		internal bool BuildAlphaFaceData(Vector3 []verts, int[] indexes, Vector3 []rgbVerts,
			Vector3 []vnorms, object pobj, GFXModel []models)
		{
			return	BuildAlphaFaceData(verts, indexes, rgbVerts, vnorms, pobj, models, null,
				MaterialCorrect.IsAlpha,
				MaterialFill.FillAlpha,
				FinishAlpha);
		}


		internal bool BuildFullBrightFaceData(Vector3 []verts, int[] indexes,
			object pobj, GFXModel []models)
		{
			return	BuildFaceData(verts, indexes, null, null, pobj, models, null,
				MaterialCorrect.IsFullBright,
				MaterialFill.FillFullBright,
				FinishFullBright);
		}


		internal bool BuildSkyFaceData(Vector3 []verts, int[] indexes,
			object pobj, GFXModel []models)
		{
			return	BuildFaceData(verts, indexes, null, null, pobj, models, null,
				MaterialCorrect.IsSky,
				MaterialFill.FillSky,
				FinishSky);
		}


		List<DrawCall> ComputeIndexes(List<UInt16> inds, List<DrawDataChunk> ddcs, ref UInt16 vertOfs)
		{
			List<DrawCall>	draws	=new List<DrawCall>();

			//index as if data is already in a big vbuffer
			for(int j=0;j < mMaterialNames.Count;j++)
			{
				int	cnt	=inds.Count;

				DrawCall	dc		=new DrawCall();				
				dc.mStartIndex		=cnt;
				dc.mSortPoint		=Vector3.Zero;	//unused for opaques

				for(int i=0;i < ddcs[j].mNumFaces;i++)
				{
					int	nverts	=ddcs[j].mVCounts[i];

					//triangulate
					//reverse with sharpdx coord change
					for(UInt16 k=1;k < nverts-1;k++)
					{
						inds.Add((UInt16)(vertOfs + ((k + 1) % nverts)));
						inds.Add((UInt16)(vertOfs + k));
						inds.Add(vertOfs);
					}

					vertOfs	+=(UInt16)nverts;
				}

				dc.mCount	=(inds.Count - cnt);

				draws.Add(dc);
			}
			return	draws;
		}


		Vector3 ComputeSortPoint(DrawDataChunk ddc)
		{
			Bounds	bnd	=new Bounds();

			//compute sort point
			foreach(Vector3 v in ddc.mVerts)
			{
				bnd.AddPointToBounds(v);
			}

			Vector3	ret	=bnd.GetCenter();

			return	ret;
		}


		//for alphas
		static void StuffVBArrays(List<Dictionary<Int32, DrawDataChunk>> perPlaneChunks,
			List<Vector3> verts, List<Vector3> norms, List<Vector2> tex0,
			List<Vector2> tex1, List<Vector2> tex2, List<Vector2> tex3, 
			List<Vector2> tex4, List<Color> colors, List<Color> styles)
		{
			foreach(Dictionary<Int32, DrawDataChunk> ppChunk in perPlaneChunks)
			{
				foreach(KeyValuePair<Int32, DrawDataChunk> ppddc in ppChunk)
				{
					verts.AddRange(ppddc.Value.mVerts);
					norms.AddRange(ppddc.Value.mNorms);

					if(tex0 != null)
					{
						tex0.AddRange(ppddc.Value.mTex0);
					}
					if(tex1 != null)
					{
						tex1.AddRange(ppddc.Value.mTex1);
					}
					if(tex2 != null)
					{
						tex2.AddRange(ppddc.Value.mTex2);
					}
					if(tex3 != null)
					{
						tex3.AddRange(ppddc.Value.mTex3);
					}
					if(tex4 != null)
					{
						tex4.AddRange(ppddc.Value.mTex4);
					}
					if(colors != null)
					{
						colors.AddRange(ppddc.Value.mColors);
					}
					if(styles != null)
					{
						styles.AddRange(ppddc.Value.mStyles);
					}
				}
			}
		}


		//for opaques
		static void StuffVBArrays(List<DrawDataChunk> matChunks,
			List<Vector3> verts, List<Vector3> norms, List<Vector2> tex0,
			List<Vector2> tex1, List<Vector2> tex2, List<Vector2> tex3, 
			List<Vector2> tex4, List<Color> colors, List<Color> styles)
		{
			foreach(DrawDataChunk ddc in matChunks)
			{
				verts.AddRange(ddc.mVerts);

				if(norms != null)
				{
					norms.AddRange(ddc.mNorms);
				}
				if(tex0 != null)
				{
					tex0.AddRange(ddc.mTex0);
				}
				if(tex1 != null)
				{
					tex1.AddRange(ddc.mTex1);
				}
				if(tex2 != null)
				{
					tex2.AddRange(ddc.mTex2);
				}
				if(tex3 != null)
				{
					tex3.AddRange(ddc.mTex3);
				}
				if(tex4 != null)
				{
					tex4.AddRange(ddc.mTex4);
				}
				if(colors != null)
				{
					colors.AddRange(ddc.mColors);
				}
				if(styles != null)
				{
					styles.AddRange(ddc.mStyles);
				}
			}
		}


		List<List<DrawCall>> ComputeAlphaIndexes(List<UInt16> inds,
			List<Dictionary<Int32, DrawDataChunk>> perPlaneChunks, ref UInt16 vertOfs)
		{
			List<List<DrawCall>>	draws	=new List<List<DrawCall>>();

			//index as if data is already in a big vbuffer
			for(int j=0;j < mMaterialNames.Count;j++)
			{
				List<DrawCall>	dcs	=new List<DrawCall>();
				foreach(KeyValuePair<Int32, DrawDataChunk> pf in perPlaneChunks[j])
				{
					int	cnt	=inds.Count;

					DrawCall	dc		=new DrawCall();				
					dc.mStartIndex		=cnt;
					dc.mSortPoint		=ComputeSortPoint(pf.Value);

					for(int i=0;i < pf.Value.mNumFaces;i++)
					{
						int	nverts	=pf.Value.mVCounts[i];

						//triangulate
						//reversing from xna to sharpdx
						for(int k=1;k < nverts-1;k++)
						{
							inds.Add((UInt16)(vertOfs + ((k + 1) % nverts)));
							inds.Add((UInt16)(vertOfs + k));
							inds.Add(vertOfs);
						}

						vertOfs	+=(UInt16)pf.Value.mVCounts[i];
					}

					dc.mCount	=(inds.Count - cnt);

					dcs.Add(dc);
				}
				draws.Add(dcs);
			}
			return	draws;
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

				string	tech		="";
				bool	bLightMap	=false;

				//set some parameter defaults
				if(mn.EndsWith("*Alpha"))
				{
					if(bCel)
					{
						tech	="VertexLightingAlphaCel";
					}
					else
					{
						tech	="VertexLightingAlpha";
					}
				}
				else if(mn.EndsWith("*LitAlpha"))
				{
					bLightMap	=true;
					if(bCel)
					{
						tech	="LightMapAlphaCel";
					}
					else
					{
						tech	="LightMapAlpha";
					}
				}
				else if(mn.EndsWith("*LitAlphaAnim"))
				{
					bLightMap	=true;
					if(bCel)
					{
						tech	="LightMapAnimAlphaCel";
					}
					else
					{
						tech	="LightMapAnimAlpha";
					}
				}
				else if(mn.EndsWith("*VertLit"))
				{
					if(bCel)
					{
						tech	="VertexLightingCel";
					}
					else
					{
						tech	="VertexLighting";
					}
				}
				else if(mn.EndsWith("*FullBright"))
				{
					tech	="FullBright";
				}
				else if(mn.EndsWith("*Mirror"))
				{
					bLightMap	=true;
					tech		="Mirror";
				}
				else if(mn.EndsWith("*Sky"))
				{
					tech	="Sky";
				}
				else if(mn.EndsWith("*Anim"))
				{
					if(bCel)
					{
						tech	="LightMapAnimCel";
					}
					else
					{
						tech	="LightMapAnim";
					}
					bLightMap	=true;
				}
				else
				{
					if(bCel)
					{
						tech	="LightMapCel";
					}
					else
					{
						tech	="LightMap";
					}
					bLightMap	=true;
				}

				mMatLib.CreateMaterial(matName);

				mMatLib.SetMaterialEffect(matName, "BSP.fx");
				mMatLib.SetMaterialTechnique(matName, tech);
				if(bLightMap)
				{
					mMatLib.SetMaterialParameter(matName, "mLightMap", null);
				}
			}
		}


		void CalcMaterialNames()
		{
			mMaterialNames.Clear();

			if(mFaces == null)
			{
				return;
			}

			foreach(GFXFace f in mFaces)
			{
				string	matName	=GFXTexInfo.ScryTrueName(f, mTexInfos[f.mTexInfo]);

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
	}
}