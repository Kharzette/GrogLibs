using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MaterialLib;
using MeshLib;
using UtilityLib;


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
		internal List<Vector4>	mColors		=new List<Vector4>();
		internal List<Vector4>	mStyles		=new List<Vector4>();
	}


	//grind up a map into gpu friendly data
	public partial class MapGrinder
	{
		GraphicsDevice			mGD;
		MaterialLib.MaterialLib	mMatLib;

		//vertex declarations
		VertexDeclaration	mLMVD, mVLitVD, mFBVD, mAlphaVD;
		VertexDeclaration	mMirrorVD, mSkyVD, mLMAnimVD;
		VertexDeclaration	mLMAVD, mLMAAnimVD;

		//computed lightmapped geometry
		List<Vector3>	mLMVerts		=new List<Vector3>();
		List<Vector3>	mLMNormals		=new List<Vector3>();
		List<Vector2>	mLMFaceTex0		=new List<Vector2>();
		List<Vector2>	mLMFaceTex1		=new List<Vector2>();
		List<Int32>		mLMIndexes		=new List<Int32>();

		//computed lightmapped alpha geometry
		List<Vector3>	mLMAVerts		=new List<Vector3>();
		List<Vector3>	mLMANormals		=new List<Vector3>();
		List<Vector2>	mLMAFaceTex0	=new List<Vector2>();
		List<Vector2>	mLMAFaceTex1	=new List<Vector2>();
		List<Int32>		mLMAIndexes		=new List<Int32>();
		List<Vector4>	mLMAColors		=new List<Vector4>();

		//computed vertex lit geometry
		List<Vector3>	mVLitVerts		=new List<Vector3>();
		List<Vector2>	mVLitTex0		=new List<Vector2>();
		List<Vector3>	mVLitNormals	=new List<Vector3>();
		List<Vector4>	mVLitColors		=new List<Vector4>();
		List<Int32>		mVLitIndexes	=new List<Int32>();

		//computed fullbright geometry
		List<Vector3>	mFBVerts	=new List<Vector3>();
		List<Vector2>	mFBTex0		=new List<Vector2>();
		List<Int32>		mFBIndexes	=new List<Int32>();

		//computed alpha geometry
		List<Vector3>	mAlphaVerts		=new List<Vector3>();
		List<Vector2>	mAlphaTex0		=new List<Vector2>();
		List<Vector3>	mAlphaNormals	=new List<Vector3>();
		List<Vector4>	mAlphaColors	=new List<Vector4>();
		List<Int32>		mAlphaIndexes	=new List<Int32>();

		//computed mirror geometry
		List<Vector3>		mMirrorVerts	=new List<Vector3>();
		List<Vector3>		mMirrorNormals	=new List<Vector3>();
		List<Vector2>		mMirrorTex0		=new List<Vector2>();
		List<Vector4>		mMirrorColors	=new List<Vector4>();
		List<Int32>			mMirrorIndexes	=new List<Int32>();
		List<List<Vector3>>	mMirrorPolys	=new List<List<Vector3>>();

		//computed sky geometry
		List<Vector3>	mSkyVerts	=new List<Vector3>();
		List<Vector2>	mSkyTex0	=new List<Vector2>();
		List<Int32>		mSkyIndexes	=new List<Int32>();

		//animated lightmap geometry
		List<Vector3>	mLMAnimVerts	=new List<Vector3>();
		List<Vector3>	mLMAnimNormals	=new List<Vector3>();
		List<Vector2>	mLMAnimFaceTex0	=new List<Vector2>();
		List<Vector2>	mLMAnimFaceTex1	=new List<Vector2>();
		List<Vector2>	mLMAnimFaceTex2	=new List<Vector2>();
		List<Vector2>	mLMAnimFaceTex3	=new List<Vector2>();
		List<Vector2>	mLMAnimFaceTex4	=new List<Vector2>();
		List<Int32>		mLMAnimIndexes	=new List<Int32>();
		List<Vector4>	mLMAnimStyle	=new List<Vector4>();

		//animated lightmap alpha geometry
		List<Vector3>	mLMAAnimVerts		=new List<Vector3>();
		List<Vector3>	mLMAAnimNormals		=new List<Vector3>();
		List<Vector2>	mLMAAnimFaceTex0	=new List<Vector2>();
		List<Vector2>	mLMAAnimFaceTex1	=new List<Vector2>();
		List<Vector2>	mLMAAnimFaceTex2	=new List<Vector2>();
		List<Vector2>	mLMAAnimFaceTex3	=new List<Vector2>();
		List<Vector2>	mLMAAnimFaceTex4	=new List<Vector2>();
		List<Int32>		mLMAAnimIndexes		=new List<Int32>();
		List<Vector4>	mLMAAnimStyle		=new List<Vector4>();
		List<Vector4>	mLMAAnimColors		=new List<Vector4>();

		//computed material stuff
		List<string>	mMaterialNames		=new List<string>();
		List<Material>	mMaterials			=new List<Material>();

		//material draw call information
		//opaques
		//indexed by material and model number
		Dictionary<int, List<DrawCall>>	mLMDraws		=new Dictionary<int, List<DrawCall>>();
		Dictionary<int, List<DrawCall>>	mVLitDraws		=new Dictionary<int, List<DrawCall>>();
		Dictionary<int, List<DrawCall>>	mLMAnimDraws	=new Dictionary<int, List<DrawCall>>();
		Dictionary<int, List<DrawCall>>	mFBDraws		=new Dictionary<int, List<DrawCall>>();
		Dictionary<int, List<DrawCall>>	mSkyDraws		=new Dictionary<int, List<DrawCall>>();

		//alphas
		Dictionary<int, List<List<DrawCall>>>	mLMADraws		=new Dictionary<int, List<List<DrawCall>>>();
		Dictionary<int, List<List<DrawCall>>>	mAlphaDraws		=new Dictionary<int, List<List<DrawCall>>>();
		Dictionary<int, List<List<DrawCall>>>	mMirrorDraws	=new Dictionary<int, List<List<DrawCall>>>();
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
		internal delegate void FinishUp(int modelIndex, List<DrawDataChunk> matChunks, ref int vertOfs);
		internal delegate void FinishUpAlpha(int modelIndex, List<Dictionary<Int32, DrawDataChunk>> perPlaneChunk, ref int vertOfs);

		public MapGrinder(GraphicsDevice gd, GFXTexInfo []texs,
			GFXFace []faces, int lightGridSize, int atlasSize)
		{
			mGD				=gd;
			mTexInfos		=texs;
			mLightGridSize	=lightGridSize;
			mFaces			=faces;
			mMatLib			=new MaterialLib.MaterialLib();

			if(gd != null)
			{
				mLMAtlas	=new TexAtlas(gd, atlasSize, atlasSize);
			}

			CalcMaterialNames();
			CalcMaterials();
			InitVertexDeclarations(gd);
		}


		void InitVertexDeclarations(GraphicsDevice gd)
		{
			if(gd == null)
			{
				return;
			}

			//make vertex declarations
			//lightmapped
			mLMVD	=VertexTypes.GetVertexDeclarationForType(typeof(VPosNormTex04));

			//lightmapped alpha
			mLMAVD	=VertexTypes.GetVertexDeclarationForType(typeof(VPosNormTex04Col0));

			//vertex lit, alpha, and mirror
			mVLitVD		=VertexTypes.GetVertexDeclarationForType(typeof(VPosNormTex0Col0));
			mAlphaVD	=VertexTypes.GetVertexDeclarationForType(typeof(VPosNormTex0Col0));
			mMirrorVD	=VertexTypes.GetVertexDeclarationForType(typeof(VPosNormTex0Col0));

			//animated lightmapped, and alpha as well
			//alpha is stored in the style vector4
			mLMAnimVD	=VertexTypes.GetVertexDeclarationForType(typeof(VPosNormBlendTex04Tex14Tex24));
			mLMAAnimVD	=VertexTypes.GetVertexDeclarationForType(typeof(VPosNormBlendTex04Tex14Tex24));

			//FullBright and sky
			mFBVD	=VertexTypes.GetVertexDeclarationForType(typeof(VPosTex0));
			mSkyVD	=VertexTypes.GetVertexDeclarationForType(typeof(VPosTex0));
		}


		internal List<Material> GetMaterials()
		{
			return	mMaterials;
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


		internal void GetMirrorMaterialData(out Dictionary<int, List<List<DrawCall>>> draws, out List<List<Vector3>> polys)
		{
			draws	=mMirrorDraws;
			polys	=mMirrorPolys;
		}


		internal void GetLMAAnimMaterialData(out Dictionary<int, List<List<DrawCall>>> draws)
		{
			draws	=mLMAAnimDraws;
		}


		internal void GetLMBuffers(out VertexBuffer vb, out IndexBuffer ib)
		{
			if(mLMVerts.Count == 0)
			{
				vb	=null;
				ib	=null;
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
				varray[i].Normal		=mLMNormals[i];
			}

			vb	=new VertexBuffer(mGD, mLMVD, varray.Length, BufferUsage.None);
			vb.SetData<VPosNormTex04>(varray);

			ib	=new IndexBuffer(mGD, IndexElementSize.ThirtyTwoBits, mLMIndexes.Count, BufferUsage.None);
			ib.SetData<Int32>(mLMIndexes.ToArray());
		}


		internal void GetLMABuffers(out VertexBuffer vb, out IndexBuffer ib)
		{
			if(mLMAVerts.Count == 0)
			{
				vb	=null;
				ib	=null;
				return;
			}

			VPosNormTex04Col0	[]varray	=new VPosNormTex04Col0[mLMAVerts.Count];
			for(int i=0;i < mLMAVerts.Count;i++)
			{
				varray[i].Position		=mLMAVerts[i];
				varray[i].TexCoord0.X	=mLMAFaceTex0[i].X;
				varray[i].TexCoord0.Y	=mLMAFaceTex0[i].Y;
				varray[i].TexCoord0.Z	=mLMAFaceTex1[i].X;
				varray[i].TexCoord0.W	=mLMAFaceTex1[i].Y;
				varray[i].Normal		=mLMANormals[i];
				varray[i].Color0		=mLMAColors[i];
			}

			vb	=new VertexBuffer(mGD, mLMAVD, varray.Length, BufferUsage.None);
			vb.SetData<VPosNormTex04Col0>(varray);

			ib	=new IndexBuffer(mGD, IndexElementSize.ThirtyTwoBits, mLMAIndexes.Count, BufferUsage.None);
			ib.SetData<Int32>(mLMAIndexes.ToArray());
		}


		internal void GetVLitBuffers(out VertexBuffer vb, out IndexBuffer ib)
		{
			if(mVLitVerts.Count == 0)
			{
				vb	=null;
				ib	=null;
				return;
			}

			VPosNormTex0Col0	[]varray	=new VPosNormTex0Col0[mVLitVerts.Count];
			for(int i=0;i < mVLitVerts.Count;i++)
			{
				varray[i].Position	=mVLitVerts[i];
				varray[i].TexCoord0	=mVLitTex0[i];
				varray[i].Normal	=mVLitNormals[i];
				varray[i].Color0	=mVLitColors[i];
			}

			vb	=new VertexBuffer(mGD, mVLitVD, varray.Length, BufferUsage.None);
			vb.SetData<VPosNormTex0Col0>(varray);

			ib	=new IndexBuffer(mGD, IndexElementSize.ThirtyTwoBits, mVLitIndexes.Count, BufferUsage.None);
			ib.SetData<Int32>(mVLitIndexes.ToArray());
		}


		internal void GetAlphaBuffers(out VertexBuffer vb, out IndexBuffer ib)
		{
			if(mAlphaVerts.Count == 0)
			{
				vb	=null;
				ib	=null;
				return;
			}

			VPosNormTex0Col0	[]varray	=new VPosNormTex0Col0[mAlphaVerts.Count];
			for(int i=0;i < mAlphaVerts.Count;i++)
			{
				varray[i].Position	=mAlphaVerts[i];
				varray[i].TexCoord0	=mAlphaTex0[i];
				varray[i].Normal	=mAlphaNormals[i];
				varray[i].Color0	=mAlphaColors[i];
			}

			vb	=new VertexBuffer(mGD, mAlphaVD, varray.Length, BufferUsage.None);
			vb.SetData<VPosNormTex0Col0>(varray);

			ib	=new IndexBuffer(mGD, IndexElementSize.ThirtyTwoBits, mAlphaIndexes.Count, BufferUsage.None);
			ib.SetData<Int32>(mAlphaIndexes.ToArray());
		}


		internal void GetFullBrightBuffers(out VertexBuffer vb, out IndexBuffer ib)
		{
			if(mFBVerts.Count == 0)
			{
				vb	=null;
				ib	=null;
				return;
			}

			VPosTex0	[]varray	=new VPosTex0[mFBVerts.Count];
			for(int i=0;i < mFBVerts.Count;i++)
			{
				varray[i].Position	=mFBVerts[i];
				varray[i].TexCoord0	=mFBTex0[i];
			}

			vb	=new VertexBuffer(mGD, mFBVD, varray.Length, BufferUsage.None);
			vb.SetData<VPosTex0>(varray);

			ib	=new IndexBuffer(mGD, IndexElementSize.ThirtyTwoBits, mFBIndexes.Count, BufferUsage.None);
			ib.SetData<Int32>(mFBIndexes.ToArray());
		}


		internal void GetMirrorBuffers(out VertexBuffer vb, out IndexBuffer ib)
		{
			if(mMirrorVerts.Count == 0)
			{
				vb	=null;
				ib	=null;
				return;
			}

			VPosNormTex0Col0	[]varray	=new VPosNormTex0Col0[mMirrorVerts.Count];
			for(int i=0;i < mMirrorVerts.Count;i++)
			{
				varray[i].Position	=mMirrorVerts[i];
				varray[i].TexCoord0	=mMirrorTex0[i];
				varray[i].Normal	=mMirrorNormals[i];
				varray[i].Color0	=mMirrorColors[i];
			}

			vb	=new VertexBuffer(mGD, mMirrorVD, varray.Length, BufferUsage.None);
			vb.SetData<VPosNormTex0Col0>(varray);

			ib	=new IndexBuffer(mGD, IndexElementSize.ThirtyTwoBits, mMirrorIndexes.Count, BufferUsage.None);
			ib.SetData<Int32>(mMirrorIndexes.ToArray());
		}


		internal void GetSkyBuffers(out VertexBuffer vb, out IndexBuffer ib)
		{
			if(mSkyVerts.Count == 0)
			{
				vb	=null;
				ib	=null;
				return;
			}

			VPosTex0	[]varray	=new VPosTex0[mSkyVerts.Count];
			for(int i=0;i < mSkyVerts.Count;i++)
			{
				varray[i].Position	=mSkyVerts[i];
				varray[i].TexCoord0	=mSkyTex0[i];
			}

			vb	=new VertexBuffer(mGD, mSkyVD, varray.Length, BufferUsage.None);
			vb.SetData<VPosTex0>(varray);

			ib	=new IndexBuffer(mGD, IndexElementSize.ThirtyTwoBits, mSkyIndexes.Count, BufferUsage.None);
			ib.SetData<Int32>(mSkyIndexes.ToArray());
		}


		internal void GetLMAnimBuffers(out VertexBuffer vb, out IndexBuffer ib)
		{
			if(mLMAnimVerts.Count == 0)
			{
				vb	=null;
				ib	=null;
				return;
			}

			VPosNormBlendTex04Tex14Tex24	[]varray
				=new VPosNormBlendTex04Tex14Tex24[mLMAnimVerts.Count];
			for(int i=0;i < mLMAnimVerts.Count;i++)
			{
				varray[i].Position		=mLMAnimVerts[i];
				varray[i].Normal		=mLMAnimNormals[i];
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
				varray[i].BoneIndex		=mLMAnimStyle[i];
			}

			vb	=new VertexBuffer(mGD, mLMAnimVD, varray.Length, BufferUsage.None);
			vb.SetData<VPosNormBlendTex04Tex14Tex24>(varray);

			ib	=new IndexBuffer(mGD, IndexElementSize.ThirtyTwoBits, mLMAnimIndexes.Count, BufferUsage.None);
			ib.SetData<Int32>(mLMAnimIndexes.ToArray());
		}


		internal void GetLMAAnimBuffers(out VertexBuffer vb, out IndexBuffer ib)
		{
			if(mLMAAnimVerts.Count == 0)
			{
				vb	=null;
				ib	=null;
				return;
			}

			VPosNormBlendTex04Tex14Tex24	[]varray
				=new VPosNormBlendTex04Tex14Tex24[mLMAAnimVerts.Count];
			for(int i=0;i < mLMAAnimVerts.Count;i++)
			{
				varray[i].Position		=mLMAAnimVerts[i];
				varray[i].Normal		=mLMAAnimNormals[i];
				varray[i].TexCoord0.X	=mLMAAnimFaceTex0[i].X;
				varray[i].TexCoord0.Y	=mLMAAnimFaceTex0[i].Y;
				varray[i].TexCoord0.Z	=mLMAAnimFaceTex1[i].X;
				varray[i].TexCoord0.W	=mLMAAnimFaceTex1[i].Y;
				varray[i].TexCoord1.X	=mLMAAnimFaceTex2[i].X;
				varray[i].TexCoord1.Y	=mLMAAnimFaceTex2[i].Y;
				varray[i].TexCoord1.Z	=mLMAAnimFaceTex3[i].X;
				varray[i].TexCoord1.W	=mLMAAnimFaceTex3[i].Y;
				varray[i].TexCoord2.X	=mLMAAnimFaceTex4[i].X;
				varray[i].TexCoord2.Y	=mLMAAnimFaceTex4[i].Y;
				varray[i].TexCoord2.Z	=mLMAAnimColors[i].W;
				varray[i].BoneIndex		=mLMAAnimStyle[i];
			}

			vb	=new VertexBuffer(mGD, mLMAAnimVD, varray.Length, BufferUsage.None);
			vb.SetData<VPosNormBlendTex04Tex14Tex24>(varray);

			ib	=new IndexBuffer(mGD, IndexElementSize.ThirtyTwoBits, mLMAAnimIndexes.Count, BufferUsage.None);
			ib.SetData<Int32>(mLMAAnimIndexes.ToArray());
		}


		//experimental drawcall builder that uses lots of callbacks
		internal bool BuildFaceData(Vector3 []verts, int[] indexes,
			Vector3 []rgbVerts, Vector3 []vnorms,
			object pobj, GFXModel []models, byte []lightData,
			IsCorrectMaterial correct, FillDrawChunk fill, FinishUp fin)
		{
			GFXPlane	[]pp	=pobj as GFXPlane [];

			int	vertOfs	=0;	//model offsets
			for(int i=0;i < models.Length;i++)
			{
				//store faces per material
				List<DrawDataChunk>	matChunks	=new List<DrawDataChunk>();

				foreach(Material mat in mMaterials)
				{
					DrawDataChunk	ddc	=new DrawDataChunk();
					matChunks.Add(ddc);

					//skip on material name
					if(!correct(null, null, mat.Name))
					{
						continue;
					}

					int	firstFace	=models[i].mFirstFace;
					int	nFaces		=models[i].mNumFaces;

					for(int face=firstFace;face < (firstFace + nFaces);face++)
					{
						GFXFace		f	=mFaces[face];
						GFXTexInfo	tex	=mTexInfos[f.mTexInfo];

						if(!correct(f, tex, mat.Name))
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


		internal bool BuildAlphaFaceData(Vector3 []verts, int[] indexes,
			Vector3 []rgbVerts, Vector3 []vnorms,
			object pobj, GFXModel []models, byte []lightData,
			IsCorrectMaterial correct, FillDrawChunk fill, FinishUpAlpha fin)
		{
			GFXPlane	[]pp	=pobj as GFXPlane [];

			int	vertOfs	=0;	//model offsets
			for(int i=0;i < models.Length;i++)
			{
				//store each plane used, and how many faces per material
				List<Dictionary<Int32, DrawDataChunk>>	perPlaneChunks
					=new List<Dictionary<Int32, DrawDataChunk>>();

				foreach(Material mat in mMaterials)
				{
					Dictionary<Int32, DrawDataChunk>	ddcs
						=new Dictionary<Int32, DrawDataChunk>();
					perPlaneChunks.Add(ddcs);

					if(!correct(null, null, mat.Name))
					{
						continue;
					}

					int	firstFace	=models[i].mFirstFace;
					int	nFaces		=models[i].mNumFaces;

					for(int face=firstFace;face < (firstFace + nFaces);face++)
					{
						GFXFace		f	=mFaces[face];
						GFXTexInfo	tex	=mTexInfos[f.mTexInfo];

						if(!correct(f, tex, mat.Name))
						{
							continue;
						}

						DrawDataChunk	ddc	=null;

						if(ddcs.ContainsKey(f.mPlaneNum))
						{
							ddc	=ddcs[f.mPlaneNum];
						}
						else
						{
							ddc	=new DrawDataChunk();
						}

						if(!fill(ddc, pp, verts, indexes, rgbVerts, vnorms, f, tex, mLightGridSize, lightData, mLMAtlas, mMirrorPolys))
						{
							return	false;
						}

						if(!ddcs.ContainsKey(f.mPlaneNum))
						{
							ddcs.Add(f.mPlaneNum, ddc);
						}
					}
				}
				fin(i, perPlaneChunks, ref vertOfs);
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
			return	BuildAlphaFaceData(verts, indexes, rgbVerts, vnorms, pobj, models, null,
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


		List<DrawCall> ComputeIndexes(List<int> inds, List<DrawDataChunk> ddcs, ref int vertOfs)
		{
			List<DrawCall>	draws	=new List<DrawCall>();

			//index as if data is already in a big vbuffer
			for(int j=0;j < mMaterialNames.Count;j++)
			{
				int	cnt	=inds.Count;

				DrawCall	dc		=new DrawCall();				
				dc.mStartIndex		=cnt;
				dc.mSortPoint		=Vector3.Zero;	//unused for opaques
				dc.mMinVertIndex	=696969;

				for(int i=0;i < ddcs[j].mNumFaces;i++)
				{
					int	nverts	=ddcs[j].mVCounts[i];

					//triangulate
					for(int k=1;k < nverts-1;k++)
					{
						inds.Add(vertOfs);
						inds.Add(vertOfs + k);
						inds.Add(vertOfs + ((k + 1) % nverts));
					}

					if(vertOfs < dc.mMinVertIndex)
					{
						dc.mMinVertIndex	=vertOfs;
					}

					vertOfs	+=ddcs[j].mVCounts[i];
				}

				int	numTris	=(inds.Count - cnt);

				numTris	/=3;

				dc.mPrimCount	=numTris;
				dc.mNumVerts	=ddcs[j].mVerts.Count;

				draws.Add(dc);
			}
			return	draws;
		}


		Vector3 ComputeSortPoint(DrawDataChunk ddc)
		{
			double	X	=0.0;
			double	Y	=0.0;
			double	Z	=0.0;

			//compute sort point
			int	numAvg	=0;
			foreach(Vector3 v in ddc.mVerts)
			{
				X	+=v.X;
				Y	+=v.Y;
				Z	+=v.Z;

				numAvg++;
			}

			X	/=numAvg;
			Y	/=numAvg;
			Z	/=numAvg;

			Vector3	ret	=Vector3.Zero;

			ret.X	=(float)X;
			ret.Y	=(float)Y;
			ret.Z	=(float)Z;

			return	ret;
		}


		//for alphas
		static void StuffVBArrays(List<Dictionary<Int32, DrawDataChunk>> perPlaneChunks,
			List<Vector3> verts, List<Vector3> norms, List<Vector2> tex0,
			List<Vector2> tex1, List<Vector2> tex2, List<Vector2> tex3, 
			List<Vector2> tex4, List<Vector4> colors, List<Vector4> styles)
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
			List<Vector2> tex4, List<Vector4> colors, List<Vector4> styles)
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


		List<List<DrawCall>> ComputeAlphaIndexes(List<int> inds,
			List<Dictionary<Int32, DrawDataChunk>> perPlaneChunks, ref int vertOfs)
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
					dc.mMinVertIndex	=696969;

					for(int i=0;i < pf.Value.mNumFaces;i++)
					{
						int	nverts	=pf.Value.mVCounts[i];

						//triangulate
						for(int k=1;k < nverts-1;k++)
						{
							inds.Add(vertOfs);
							inds.Add(vertOfs + k);
							inds.Add(vertOfs + ((k + 1) % nverts));
						}

						if(vertOfs < dc.mMinVertIndex)
						{
							dc.mMinVertIndex	=vertOfs;
						}

						vertOfs	+=pf.Value.mVCounts[i];
					}

					int	numTris	=(inds.Count - cnt);

					numTris	/=3;

					dc.mPrimCount	=numTris;
					dc.mNumVerts	=pf.Value.mVerts.Count;

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
				MaterialLib.Material	mat	=mMatLib.CreateMaterial();
				mat.Name				=matName;
				mat.ShaderName			="Shaders\\LightMap";
				mat.Technique			="";
				mat.BlendState			=BlendState.Opaque;
				mat.DepthState			=DepthStencilState.Default;
				mat.RasterState			=RasterizerState.CullCounterClockwise;

				//set some parameter defaults
				if(mat.Name.EndsWith("*Alpha"))
				{
					mat.BlendState	=BlendState.AlphaBlend;
					mat.DepthState	=DepthStencilState.DepthRead;
					mat.Technique	="Alpha";
				}
				else if(mat.Name.EndsWith("*LitAlpha"))
				{
					mat.BlendState	=BlendState.AlphaBlend;
					mat.DepthState	=DepthStencilState.DepthRead;
					mat.Technique	="LightMapAlpha";
					mat.AddParameter("mLightMap",
						EffectParameterClass.Object,
						EffectParameterType.Texture,
						"LightMapAtlas");
				}
				else if(mat.Name.EndsWith("*LitAlphaAnim"))
				{
					mat.BlendState	=BlendState.AlphaBlend;
					mat.DepthState	=DepthStencilState.DepthRead;
					mat.Technique	="LightMapAnimAlpha";
					mat.AddParameter("mLightMap",
						EffectParameterClass.Object,
						EffectParameterType.Texture,
						"LightMapAtlas");
				}
				else if(mat.Name.EndsWith("*VertLit"))
				{
					mat.Technique	="VertexLighting";
				}
				else if(mat.Name.EndsWith("*VertLitToon"))
				{
					mat.Technique	="VLitToon";
				}
				else if(mat.Name.EndsWith("*FullBright"))
				{
					mat.Technique	="FullBright";
				}
				else if(mat.Name.EndsWith("*Mirror"))
				{
					mat.Technique	="Mirror";
					mat.AddParameter("mTexture",
						EffectParameterClass.Object,
						EffectParameterType.Texture,
						"MirrorTexture");
					mat.AddParameter("mTexSize",
						EffectParameterClass.Vector,
						EffectParameterType.Single,
						"1 1");
				}
				else if(mat.Name.EndsWith("*Sky"))
				{
					mat.Technique	="Sky";
				}
				else if(mat.Name.EndsWith("*Anim"))
				{
					mat.Technique	="LightMapAnim";
					mat.AddParameter("mLightMap",
						EffectParameterClass.Object,
						EffectParameterType.Texture,
						"LightMapAtlas");
				}
				else if(mat.Name.EndsWith("*AnimToon"))
				{
					mat.Technique	="LightMapAnimToon";
					mat.AddParameter("mLightMap",
						EffectParameterClass.Object,
						EffectParameterType.Texture,
						"LightMapAtlas");
				}
				else if(mat.Name.EndsWith("*Toon"))
				{
					mat.Technique	="LightMapToon";
					mat.AddParameter("mLightMap",
						EffectParameterClass.Object,
						EffectParameterType.Texture,
						"LightMapAtlas");
				}
				else
				{
					mat.Technique	="LightMap";
					mat.AddParameter("mLightMap",
						EffectParameterClass.Object,
						EffectParameterType.Texture,
						"LightMapAtlas");
				}

				//add stuff to ignore
				//this hides it in the gui
				mat.IgnoreParameter("mEyePos");
				mat.IgnoreParameter("mLight0Position");
//				mat.IgnoreParameter("mLight0Color");
//				mat.IgnoreParameter("mLightRange");
//				mat.IgnoreParameter("mLightFalloffRange");
				mat.IgnoreParameter("mAniIntensities");
				mat.IgnoreParameter("mWarpFactor");

				mMaterials.Add(mat);
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