﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MaterialLib;
using MeshLib;


namespace BSPLib
{
	//grind up a map into gpu friendly data
	public class MapGrinder
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

		//computed material stuff
		List<string>	mMaterialNames		=new List<string>();
		List<Material>	mMaterials			=new List<Material>();

		//lightmap material stuff
		List<Int32>		mLMMaterialOffsets	=new List<Int32>();
		List<Int32>		mLMMaterialNumVerts	=new List<Int32>();
		List<Int32>		mLMMaterialNumTris	=new List<Int32>();

		//lightmap alpha material stuff
		List<Int32>		mLMAMaterialOffsets		=new List<Int32>();
		List<Int32>		mLMAMaterialNumVerts	=new List<Int32>();
		List<Int32>		mLMAMaterialNumTris		=new List<Int32>();
		List<Vector3>	mLMAMaterialSortPoints	=new List<Vector3>();

		//vert lit material stuff
		List<Int32>		mVLitMaterialOffsets	=new List<Int32>();
		List<Int32>		mVLitMaterialNumVerts	=new List<Int32>();
		List<Int32>		mVLitMaterialNumTris	=new List<Int32>();

		//alpha material stuff
		List<Int32>		mAlphaMaterialOffsets		=new List<Int32>();
		List<Int32>		mAlphaMaterialNumVerts		=new List<Int32>();
		List<Int32>		mAlphaMaterialNumTris		=new List<Int32>();
		List<Vector3>	mAlphaMaterialSortPoints	=new List<Vector3>();

		//fullbright material stuff
		List<Int32>		mFBMaterialOffsets	=new List<Int32>();
		List<Int32>		mFBMaterialNumVerts	=new List<Int32>();
		List<Int32>		mFBMaterialNumTris	=new List<Int32>();

		//mirror material stuff
		List<Int32>		mMirrorMaterialOffsets		=new List<Int32>();
		List<Int32>		mMirrorMaterialNumVerts		=new List<Int32>();
		List<Int32>		mMirrorMaterialNumTris		=new List<Int32>();
		List<Vector3>	mMirrorMaterialSortPoints	=new List<Vector3>();

		//sky material stuff
		List<Int32>		mSkyMaterialOffsets		=new List<Int32>();
		List<Int32>		mSkyMaterialNumVerts	=new List<Int32>();
		List<Int32>		mSkyMaterialNumTris		=new List<Int32>();

		//animated lightmap material stuff
		List<Int32>		mLMAnimMaterialOffsets	=new List<Int32>();
		List<Int32>		mLMAnimMaterialNumVerts	=new List<Int32>();
		List<Int32>		mLMAnimMaterialNumTris	=new List<Int32>();

		//animated lightmap alpha material stuff
		List<Int32>		mLMAAnimMaterialOffsets		=new List<Int32>();
		List<Int32>		mLMAAnimMaterialNumVerts	=new List<Int32>();
		List<Int32>		mLMAAnimMaterialNumTris		=new List<Int32>();
		List<Vector3>	mLMAAnimMaterialSortPoints	=new List<Vector3>();

		//computed lightmap atlas
		TexAtlas	mLMAtlas;

		//passed in data
		int			mLightGridSize;
		GFXTexInfo	[]mTexInfos;
		GFXFace		[]mFaces;

		//constants
		const float	AlphaValue	=0.8f;	//vertex alpha hardcode (TODO Fix)


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
			mLMVD	=VertexTypes.GetVertexDeclarationForType(typeof(VPosNormTex0Tex1));

			//lightmapped alpha
			mLMAVD	=VertexTypes.GetVertexDeclarationForType(typeof(VPosNormTex0Tex1Col0));

			//vertex lit, alpha, and mirror
			mVLitVD		=VertexTypes.GetVertexDeclarationForType(typeof(VPosNormTex0Col0));
			mAlphaVD	=VertexTypes.GetVertexDeclarationForType(typeof(VPosNormTex0Col0));
			mMirrorVD	=VertexTypes.GetVertexDeclarationForType(typeof(VPosNormTex0Col0));

			//animated lightmapped, and alpha as well
			//alpha is stored in the style vector4
			mLMAnimVD	=VertexTypes.GetVertexDeclarationForType(typeof(VPosNormBlendTex0Tex1Tex2Tex3Tex4));
			mLMAAnimVD	=VertexTypes.GetVertexDeclarationForType(typeof(VPosNormBlendTex0Tex1Tex2Tex3Tex4));

			//FullBright and sky
			mFBVD	=VertexTypes.GetVertexDeclarationForType(typeof(VPosTex0));
			mSkyVD	=VertexTypes.GetVertexDeclarationForType(typeof(VPosTex0));
		}


		internal List<Material> GetMaterials()
		{
			return	mMaterials;
		}


		internal void GetLMMaterialData(out Int32 []matOffsets,	out Int32 []matNumVerts,
										out Int32 []matNumTris)
		{
			matOffsets	=mLMMaterialOffsets.ToArray();
			matNumVerts	=mLMMaterialNumVerts.ToArray();
			matNumTris	=mLMMaterialNumTris.ToArray();
		}


		internal void GetLMAMaterialData(out Int32 []matOffsets, out Int32 []matNumVerts,
										 out Int32 []matNumTris, out Vector3 []matSortPoints)
		{
			matOffsets		=mLMAMaterialOffsets.ToArray();
			matNumVerts		=mLMAMaterialNumVerts.ToArray();
			matNumTris		=mLMAMaterialNumTris.ToArray();
			matSortPoints	=mLMAMaterialSortPoints.ToArray();
		}


		internal void GetVLitMaterialData(out Int32 []matOffsets,	out Int32 []matNumVerts,
											out Int32 []matNumTris)
		{
			matOffsets	=mVLitMaterialOffsets.ToArray();
			matNumVerts	=mVLitMaterialNumVerts.ToArray();
			matNumTris	=mVLitMaterialNumTris.ToArray();
		}


		internal void GetFullBrightMaterialData(out Int32 []matOffsets,	out Int32 []matNumVerts,
												out Int32 []matNumTris)
		{
			matOffsets	=mFBMaterialOffsets.ToArray();
			matNumVerts	=mFBMaterialNumVerts.ToArray();
			matNumTris	=mFBMaterialNumTris.ToArray();
		}


		internal void GetAlphaMaterialData(out Int32 []matOffsets,	out Int32 []matNumVerts,
											out Int32 []matNumTris, out Vector3 []matSortPoints)
		{
			matOffsets		=mAlphaMaterialOffsets.ToArray();
			matNumVerts		=mAlphaMaterialNumVerts.ToArray();
			matNumTris		=mAlphaMaterialNumTris.ToArray();
			matSortPoints	=mAlphaMaterialSortPoints.ToArray();
		}


		internal void GetMirrorMaterialData(out Int32 []matOffsets,	out Int32 []matNumVerts,
											out Int32 []matNumTris, out Vector3 []matSortPoints,
											out List<List<Vector3>> matPolys)
		{
			matOffsets		=mMirrorMaterialOffsets.ToArray();
			matNumVerts		=mMirrorMaterialNumVerts.ToArray();
			matNumTris		=mMirrorMaterialNumTris.ToArray();
			matSortPoints	=mMirrorMaterialSortPoints.ToArray();
			matPolys		=mMirrorPolys;
		}


		internal void GetSkyMaterialData(out Int32 []matOffsets,	out Int32 []matNumVerts,
											out Int32 []matNumTris)
		{
			matOffsets	=mSkyMaterialOffsets.ToArray();
			matNumVerts	=mSkyMaterialNumVerts.ToArray();
			matNumTris	=mSkyMaterialNumTris.ToArray();
		}


		internal void GetLMAnimMaterialData(out Int32 []matOffsets,	out Int32 []matNumVerts,
											out Int32 []matNumTris)
		{
			matOffsets	=mLMAnimMaterialOffsets.ToArray();
			matNumVerts	=mLMAnimMaterialNumVerts.ToArray();
			matNumTris	=mLMAnimMaterialNumTris.ToArray();
		}


		internal void GetLMAAnimMaterialData(out Int32 []matOffsets, out Int32 []matNumVerts,
											 out Int32 []matNumTris, out Vector3 []matSortPoints)
		{
			matOffsets		=mLMAAnimMaterialOffsets.ToArray();
			matNumVerts		=mLMAAnimMaterialNumVerts.ToArray();
			matNumTris		=mLMAAnimMaterialNumTris.ToArray();
			matSortPoints	=mLMAAnimMaterialSortPoints.ToArray();
		}


		internal void GetLMBuffers(out VertexBuffer vb, out IndexBuffer ib)
		{
			if(mLMVerts.Count == 0)
			{
				vb	=null;
				ib	=null;
				return;
			}

			VPosNormTex0Tex1	[]varray	=new VPosNormTex0Tex1[mLMVerts.Count];
			for(int i=0;i < mLMVerts.Count;i++)
			{
				varray[i].Position	=mLMVerts[i];
				varray[i].TexCoord0	=mLMFaceTex0[i];
				varray[i].TexCoord1	=mLMFaceTex1[i];
				varray[i].Normal	=mLMNormals[i];
			}

			vb	=new VertexBuffer(mGD, mLMVD, varray.Length, BufferUsage.None);
			vb.SetData<VPosNormTex0Tex1>(varray);

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

			VPosNormTex0Tex1Col0	[]varray	=new VPosNormTex0Tex1Col0[mLMAVerts.Count];
			for(int i=0;i < mLMAVerts.Count;i++)
			{
				varray[i].Position	=mLMAVerts[i];
				varray[i].TexCoord0	=mLMAFaceTex0[i];
				varray[i].TexCoord1	=mLMAFaceTex1[i];
				varray[i].Normal	=mLMANormals[i];
				varray[i].Color0	=Vector4.One;
				varray[i].Color0.W	=AlphaValue;	//TODO: donut hardcode
			}

			vb	=new VertexBuffer(mGD, mLMAVD, varray.Length, BufferUsage.None);
			vb.SetData<VPosNormTex0Tex1Col0>(varray);

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
				varray[i].Color0.W	=AlphaValue;	//TODO: donut hardcode
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
				varray[i].Color0.W	=AlphaValue;	//TODO: donut hardcode
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

			VPosNormBlendTex0Tex1Tex2Tex3Tex4	[]varray
				=new VPosNormBlendTex0Tex1Tex2Tex3Tex4[mLMAnimVerts.Count];
			for(int i=0;i < mLMAnimVerts.Count;i++)
			{
				varray[i].Position	=mLMAnimVerts[i];
				varray[i].Normal	=mLMAnimNormals[i];
				varray[i].TexCoord0	=mLMAnimFaceTex0[i];
				varray[i].TexCoord1	=mLMAnimFaceTex1[i];
				varray[i].TexCoord2	=mLMAnimFaceTex2[i];
				varray[i].TexCoord3	=mLMAnimFaceTex3[i];
				varray[i].TexCoord4	=mLMAnimFaceTex4[i];
				varray[i].BoneIndex	=mLMAnimStyle[i];
			}

			vb	=new VertexBuffer(mGD, mLMAnimVD, varray.Length, BufferUsage.None);
			vb.SetData<VPosNormBlendTex0Tex1Tex2Tex3Tex4>(varray);

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

			VPosNormBlendTex0Tex1Tex2Tex3Tex4	[]varray
				=new VPosNormBlendTex0Tex1Tex2Tex3Tex4[mLMAAnimVerts.Count];
			for(int i=0;i < mLMAAnimVerts.Count;i++)
			{
				varray[i].Position	=mLMAAnimVerts[i];
				varray[i].Normal	=mLMAAnimNormals[i];
				varray[i].TexCoord0	=mLMAAnimFaceTex0[i];
				varray[i].TexCoord1	=mLMAAnimFaceTex1[i];
				varray[i].TexCoord2	=mLMAAnimFaceTex2[i];
				varray[i].TexCoord3	=mLMAAnimFaceTex3[i];
				varray[i].TexCoord4	=mLMAAnimFaceTex4[i];
				varray[i].BoneIndex	=mLMAAnimStyle[i];
			}

			vb	=new VertexBuffer(mGD, mLMAAnimVD, varray.Length, BufferUsage.None);
			vb.SetData<VPosNormBlendTex0Tex1Tex2Tex3Tex4>(varray);

			ib	=new IndexBuffer(mGD, IndexElementSize.ThirtyTwoBits, mLMAAnimIndexes.Count, BufferUsage.None);
			ib.SetData<Int32>(mLMAAnimIndexes.ToArray());
		}


		internal bool BuildLMAnimFaceData(Vector3 []verts, int[] indexes, byte []lightData)
		{
			List<Int32>	firstVert	=new List<Int32>();
			List<Int32>	numVert		=new List<Int32>();
			List<Int32>	numFace		=new List<Int32>();

			Map.Print("Handling animated lightmaps...\n");

			foreach(Material mat in mMaterials)
			{
				int	numFaceVerts	=mLMAnimVerts.Count;
				int	numFaces		=0;

				if(!mat.Name.EndsWith("*Anim"))
				{
					numFace.Add(numFaces);
					mLMAnimMaterialNumVerts.Add(mLMAnimVerts.Count - numFaceVerts);
					continue;
				}

				Map.Print("Animated light for material: " + mat.Name + ".\n");

				foreach(GFXFace f in mFaces)
				{
					if(f.mLightOfs == -1)
					{
						continue;	//only interested in lightmapped
					}

					GFXTexInfo	tex	=mTexInfos[f.mTexInfo];

					if(!mat.Name.StartsWith(tex.mMaterial))
					{
						continue;
					}

					numFaces++;

					//grab lightmap0
					double	scaleU, scaleV, offsetU, offsetV;
					scaleU	=scaleV	=offsetU	=offsetV	=0.0;
					Color	[]lmap	=new Color[f.mLHeight * f.mLWidth];

					for(int i=0;i < lmap.Length;i++)
					{
						lmap[i].R	=lightData[f.mLightOfs + (i * 3)];
						lmap[i].G	=lightData[f.mLightOfs + (i * 3) + 1];
						lmap[i].B	=lightData[f.mLightOfs + (i * 3) + 2];
						lmap[i].A	=0xFF;
					}
					if(!mLMAtlas.Insert(lmap, f.mLWidth, f.mLHeight,
						out scaleU, out scaleV, out offsetU, out offsetV))
					{
						Map.Print("Lightmap atlas out of space, try increasing it's size.\n");
						return	false;
					}

					List<Vector3>	fverts	=new List<Vector3>();

					int		nverts	=f.mNumVerts;
					int		fvert	=f.mFirstVert;
					int		k		=0;
					for(k=0;k < nverts;k++)
					{
						int		idx	=indexes[fvert + k];
						Vector3	pnt	=verts[idx];
						Vector2	crd;
						crd.X	=Vector3.Dot(tex.mVecs[0], pnt) + tex.mShift[0];
						crd.Y	=Vector3.Dot(tex.mVecs[1], pnt) + tex.mShift[1];

						mLMAnimFaceTex0.Add(crd);
						fverts.Add(pnt);

						mLMAnimVerts.Add(pnt);
					}

					//flat shaded normals for lightmapped surfaces
					ComputeNormals(fverts, mLMAnimNormals);

					List<Vector2>	coords	=new List<Vector2>();
					GetTexCoords1(fverts, f.mLWidth, f.mLHeight, tex, out coords);

					float	crunchX	=f.mLWidth / (float)(f.mLWidth + 1);
					float	crunchY	=f.mLHeight / (float)(f.mLHeight + 1);

					for(k=0;k < nverts;k++)
					{
						Vector2	tc	=coords[k];

						//stretch coords to +1 size
						tc.X	*=crunchX;
						tc.Y	*=crunchY;

						//scale to atlas space
						tc.X	/=mLMAtlas.Width;
						tc.Y	/=mLMAtlas.Height;

						//step half a pixel in atlas space
						tc.X	+=1.0f / (mLMAtlas.Width * 2.0f);
						tc.Y	+=1.0f / (mLMAtlas.Height * 2.0f);

						//move to atlas position
						tc.X	+=(float)offsetU;
						tc.Y	+=(float)offsetV;

						mLMAnimFaceTex1.Add(tc);
					}

					firstVert.Add(mLMAnimVerts.Count - f.mNumVerts);
					numVert.Add(f.mNumVerts);

					//now add the animated lights if need be
					for(int s=1;s < 4;s++)
					{
						if(f.mLTypes[s] == 255)
						{
							//fill with zeros for empty spots
							for(k=0;k < nverts;k++)
							{
								if(s == 1)
								{
									mLMAnimFaceTex2.Add(Vector2.Zero);
								}
								else if(s == 2)
								{
									mLMAnimFaceTex3.Add(Vector2.Zero);
								}
								else if(s == 3)
								{
									mLMAnimFaceTex4.Add(Vector2.Zero);
								}
							}
							continue;
						}

						//grab animated lightmaps
						scaleU	=scaleV	=offsetU	=offsetV	=0.0;
						lmap	=new Color[f.mLHeight * f.mLWidth];

						int	sizeOffset	=f.mLHeight * f.mLWidth * 3;

						sizeOffset	*=s;

						for(int i=0;i < lmap.Length;i++)
						{
							lmap[i].R	=lightData[sizeOffset + f.mLightOfs + (i * 3)];
							lmap[i].G	=lightData[sizeOffset + f.mLightOfs + (i * 3) + 1];
							lmap[i].B	=lightData[sizeOffset + f.mLightOfs + (i * 3) + 2];
							lmap[i].A	=0xFF;
						}

						//insert animated map
						if(!mLMAtlas.Insert(lmap, f.mLWidth, f.mLHeight,
							out scaleU, out scaleV, out offsetU, out offsetV))
						{
							Map.Print("Lightmap atlas out of space, try increasing it's size.\n");
							return	false;
						}

						//grab texcoords to animated map location
						coords	=new List<Vector2>();
						GetTexCoords1(fverts, f.mLWidth, f.mLHeight, tex, out coords);

						crunchX	=f.mLWidth / (float)(f.mLWidth + 1);
						crunchY	=f.mLHeight / (float)(f.mLHeight + 1);

						for(k=0;k < nverts;k++)
						{
							Vector2	tc	=coords[k];

							//stretch coords to +1 size
							tc.X	*=crunchX;
							tc.Y	*=crunchY;

							//scale to atlas space
							tc.X	/=mLMAtlas.Width;
							tc.Y	/=mLMAtlas.Height;

							//step half a pixel in atlas space
							tc.X	+=1.0f / (mLMAtlas.Width * 2.0f);
							tc.Y	+=1.0f / (mLMAtlas.Height * 2.0f);

							//move to atlas position
							tc.X	+=(float)offsetU;
							tc.Y	+=(float)offsetV;

							if(s == 1)
							{
								mLMAnimFaceTex2.Add(tc);
							}
							else if(s == 2)
							{
								mLMAnimFaceTex3.Add(tc);
							}
							else if(s == 3)
							{
								mLMAnimFaceTex4.Add(tc);
							}
						}
					}

					//style index
					for(k=0;k < nverts;k++)
					{
						Vector4	styleIndex	=Vector4.Zero;
						styleIndex.X	=f.mLTypes[0];
						styleIndex.Y	=f.mLTypes[1];
						styleIndex.Z	=f.mLTypes[2];
						mLMAnimStyle.Add(styleIndex);
					}
				}

				numFace.Add(numFaces);
				mLMAnimMaterialNumVerts.Add(mLMAnimVerts.Count - numFaceVerts);
			}

			//might not be any
			if(mLMAnimVerts.Count == 0)
			{
				return	true;
			}

			mLMAtlas.Finish();

			ComputeIndexes(mLMAnimIndexes, mLMAnimMaterialOffsets,
				mLMAnimMaterialNumTris, numFace, firstVert, numVert, null, null);

			return	true;
		}


		internal bool BuildLMAAnimFaceData(Vector3 []verts, int[] indexes, byte []lightData)
		{
			List<Int32>	firstVert	=new List<Int32>();
			List<Int32>	numVert		=new List<Int32>();
			List<Int32>	numFace		=new List<Int32>();

			Map.Print("Handling alpha animated lightmaps...\n");

			foreach(Material mat in mMaterials)
			{
				int	numFaceVerts	=mLMAAnimVerts.Count;
				int	numFaces		=0;

				if(!mat.Name.EndsWith("*LitAlphaAnim"))
				{
					numFace.Add(numFaces);
					mLMAAnimMaterialNumVerts.Add(mLMAAnimVerts.Count - numFaceVerts);
					continue;
				}

				Map.Print("Animated light for material: " + mat.Name + ".\n");

				foreach(GFXFace f in mFaces)
				{
					if(f.mLightOfs == -1)
					{
						continue;	//only interested in lightmapped
					}

					GFXTexInfo	tex	=mTexInfos[f.mTexInfo];

					if(!mat.Name.StartsWith(tex.mMaterial))
					{
						continue;
					}

					numFaces++;

					//grab lightmap0
					double	scaleU, scaleV, offsetU, offsetV;
					scaleU	=scaleV	=offsetU	=offsetV	=0.0;
					Color	[]lmap	=new Color[f.mLHeight * f.mLWidth];

					for(int i=0;i < lmap.Length;i++)
					{
						lmap[i].R	=lightData[f.mLightOfs + (i * 3)];
						lmap[i].G	=lightData[f.mLightOfs + (i * 3) + 1];
						lmap[i].B	=lightData[f.mLightOfs + (i * 3) + 2];
						lmap[i].A	=0xFF;
					}
					if(!mLMAtlas.Insert(lmap, f.mLWidth, f.mLHeight,
						out scaleU, out scaleV, out offsetU, out offsetV))
					{
						Map.Print("Lightmap atlas out of space, try increasing it's size.\n");
						return	false;
					}

					List<Vector3>	fverts	=new List<Vector3>();

					int		nverts	=f.mNumVerts;
					int		fvert	=f.mFirstVert;
					int		k		=0;
					for(k=0;k < nverts;k++)
					{
						int		idx	=indexes[fvert + k];
						Vector3	pnt	=verts[idx];
						Vector2	crd;
						crd.X	=Vector3.Dot(tex.mVecs[0], pnt) + tex.mShift[0];
						crd.Y	=Vector3.Dot(tex.mVecs[1], pnt) + tex.mShift[1];

						mLMAAnimFaceTex0.Add(crd);
						fverts.Add(pnt);

						mLMAAnimVerts.Add(pnt);
					}

					//flat shaded normals for lightmapped surfaces
					ComputeNormals(fverts, mLMAAnimNormals);

					List<Vector2>	coords	=new List<Vector2>();
					GetTexCoords1(fverts, f.mLWidth, f.mLHeight, tex, out coords);

					float	crunchX	=f.mLWidth / (float)(f.mLWidth + 1);
					float	crunchY	=f.mLHeight / (float)(f.mLHeight + 1);

					for(k=0;k < nverts;k++)
					{
						Vector2	tc	=coords[k];

						//stretch coords to +1 size
						tc.X	*=crunchX;
						tc.Y	*=crunchY;

						//scale to atlas space
						tc.X	/=mLMAtlas.Width;
						tc.Y	/=mLMAtlas.Height;

						//step half a pixel in atlas space
						tc.X	+=1.0f / (mLMAtlas.Width * 2.0f);
						tc.Y	+=1.0f / (mLMAtlas.Height * 2.0f);

						//move to atlas position
						tc.X	+=(float)offsetU;
						tc.Y	+=(float)offsetV;

						mLMAAnimFaceTex1.Add(tc);
					}

					firstVert.Add(mLMAAnimVerts.Count - f.mNumVerts);
					numVert.Add(f.mNumVerts);

					//now add the animated lights if need be
					for(int s=1;s < 4;s++)
					{
						if(f.mLTypes[s] == 255)
						{
							//fill with zeros for empty spots
							for(k=0;k < nverts;k++)
							{
								if(s == 1)
								{
									mLMAAnimFaceTex2.Add(Vector2.Zero);
								}
								else if(s == 2)
								{
									mLMAAnimFaceTex3.Add(Vector2.Zero);
								}
								else if(s == 3)
								{
									mLMAAnimFaceTex4.Add(Vector2.Zero);
								}
							}
							continue;
						}

						//grab animated lightmaps
						scaleU	=scaleV	=offsetU	=offsetV	=0.0;
						lmap	=new Color[f.mLHeight * f.mLWidth];

						int	sizeOffset	=f.mLHeight * f.mLWidth;

						sizeOffset	*=s;

						for(int i=0;i < lmap.Length;i++)
						{
							lmap[i].R	=lightData[sizeOffset + f.mLightOfs + (i * 3)];
							lmap[i].G	=lightData[sizeOffset + f.mLightOfs + (i * 3) + 1];
							lmap[i].B	=lightData[sizeOffset + f.mLightOfs + (i * 3) + 2];
							lmap[i].A	=0xFF;
						}

						//insert animated map
						if(!mLMAtlas.Insert(lmap, f.mLWidth, f.mLHeight,
							out scaleU, out scaleV, out offsetU, out offsetV))
						{
							Map.Print("Lightmap atlas out of space, try increasing it's size.\n");
							return	false;
						}

						//grab texcoords to animated map location
						coords	=new List<Vector2>();
						GetTexCoords1(fverts, f.mLWidth, f.mLHeight, tex, out coords);

						crunchX	=f.mLWidth / (float)(f.mLWidth + 1);
						crunchY	=f.mLHeight / (float)(f.mLHeight + 1);

						for(k=0;k < nverts;k++)
						{
							Vector2	tc	=coords[k];

							//stretch coords to +1 size
							tc.X	*=crunchX;
							tc.Y	*=crunchY;

							//scale to atlas space
							tc.X	/=mLMAtlas.Width;
							tc.Y	/=mLMAtlas.Height;

							//step half a pixel in atlas space
							tc.X	+=1.0f / (mLMAtlas.Width * 2.0f);
							tc.Y	+=1.0f / (mLMAtlas.Height * 2.0f);

							//move to atlas position
							tc.X	+=(float)offsetU;
							tc.Y	+=(float)offsetV;

							if(s == 1)
							{
								mLMAAnimFaceTex2.Add(tc);
							}
							else if(s == 2)
							{
								mLMAAnimFaceTex3.Add(tc);
							}
							else if(s == 3)
							{
								mLMAAnimFaceTex4.Add(tc);
							}
						}
					}

					//style index
					for(k=0;k < nverts;k++)
					{
						Vector4	styleIndex	=Vector4.Zero;
						styleIndex.X	=f.mLTypes[0];
						styleIndex.Y	=f.mLTypes[1];
						styleIndex.Z	=f.mLTypes[2];
						styleIndex.W	=AlphaValue;
						mLMAAnimStyle.Add(styleIndex);
					}
				}

				numFace.Add(numFaces);
				mLMAAnimMaterialNumVerts.Add(mLMAAnimVerts.Count - numFaceVerts);
			}

			//might not be any
			if(mLMAAnimVerts.Count == 0)
			{
				return	true;
			}

			mLMAtlas.Finish();

			ComputeIndexes(mLMAAnimIndexes, mLMAAnimMaterialOffsets,
				mLMAAnimMaterialNumTris, numFace, firstVert, numVert,
				mLMAAnimVerts, mLMAAnimMaterialSortPoints);

			return	true;
		}


		internal bool BuildLMFaceData(Vector3 []verts, int[] indexes, byte []lightData)
		{
			if(lightData == null)
			{
				return	false;
			}

			List<Int32>	firstVert	=new List<Int32>();
			List<Int32>	numVert		=new List<Int32>();
			List<Int32>	numFace		=new List<Int32>();

			Map.Print("Atlasing " + lightData.Length + " bytes of light data...\n");

			foreach(Material mat in mMaterials)
			{
				int	numFaceVerts	=mLMVerts.Count;
				int	numFaces		=0;

				//skip all special materials
				if(mat.Name.Contains("*"))
				{
					numFace.Add(numFaces);
					mLMMaterialNumVerts.Add(mLMVerts.Count - numFaceVerts);
					continue;
				}

				Map.Print("Light for material: " + mat.Name + ".\n");

				foreach(GFXFace f in mFaces)
				{
					if(f.mLightOfs == -1)
					{
						continue;	//only interested in lightmapped
					}

					if(f.mLTypes[1] != 255)
					{
						continue;	//only interested in non animated
					}

					if(f.mLTypes[0] != 0)
					{
						continue;
					}

					GFXTexInfo	tex	=mTexInfos[f.mTexInfo];

					if(tex.mMaterial != mat.Name)
					{
						continue;
					}

					numFaces++;

					//grab lightmap0
					double	scaleU, scaleV, offsetU, offsetV;
					scaleU	=scaleV	=offsetU	=offsetV	=0.0;
					Color	[]lmap	=new Color[f.mLHeight * f.mLWidth];

					for(int i=0;i < lmap.Length;i++)
					{
						lmap[i].R	=lightData[f.mLightOfs + (i * 3)];
						lmap[i].G	=lightData[f.mLightOfs + (i * 3) + 1];
						lmap[i].B	=lightData[f.mLightOfs + (i * 3) + 2];
						lmap[i].A	=0xFF;
					}

					if(!mLMAtlas.Insert(lmap, f.mLWidth, f.mLHeight,
						out scaleU, out scaleV, out offsetU, out offsetV))
					{
						Map.Print("Lightmap atlas out of space, try increasing it's size.\n");
						return	false;
					}

					List<Vector3>	fverts		=new List<Vector3>();

					int		nverts	=f.mNumVerts;
					int		fvert	=f.mFirstVert;
					int		k		=0;
					for(k=0;k < nverts;k++)
					{
						int		idx	=indexes[fvert + k];
						Vector3	pnt	=verts[idx];
						Vector2	crd;
						crd.X	=Vector3.Dot(tex.mVecs[0], pnt) + tex.mShift[0];
						crd.Y	=Vector3.Dot(tex.mVecs[1], pnt) + tex.mShift[1];

						mLMFaceTex0.Add(crd);
						fverts.Add(pnt);

						mLMVerts.Add(pnt);
					}

					//flat shaded normals for lightmapped surfaces
					ComputeNormals(fverts, mLMNormals);

					List<Vector2>	coords	=new List<Vector2>();
					GetTexCoords1(fverts, f.mLWidth, f.mLHeight, tex, out coords);

					float	crunchX	=f.mLWidth / (float)(f.mLWidth + 1);
					float	crunchY	=f.mLHeight / (float)(f.mLHeight + 1);

					for(k=0;k < nverts;k++)
					{
						Vector2	tc	=coords[k];

						//stretch coords to +1 size
						tc.X	*=crunchX;
						tc.Y	*=crunchY;

						//scale to atlas space
						tc.X	/=mLMAtlas.Width;
						tc.Y	/=mLMAtlas.Height;

						//step half a pixel in atlas space
						tc.X	+=1.0f / (mLMAtlas.Width * 2.0f);
						tc.Y	+=1.0f / (mLMAtlas.Height * 2.0f);

						//move to atlas position
						tc.X	+=(float)offsetU;
						tc.Y	+=(float)offsetV;

						mLMFaceTex1.Add(tc);
					}

					firstVert.Add(mLMVerts.Count - f.mNumVerts);
					numVert.Add(f.mNumVerts);
				}

				numFace.Add(numFaces);
				mLMMaterialNumVerts.Add(mLMVerts.Count - numFaceVerts);
			}

			mLMAtlas.Finish();

			ComputeIndexes(mLMIndexes, mLMMaterialOffsets, mLMMaterialNumTris,
				numFace, firstVert, numVert, null, null);

			return	true;
		}


		internal bool BuildLMAFaceData(Vector3 []verts, int[] indexes, byte []lightData)
		{
			List<Int32>	firstVert	=new List<Int32>();
			List<Int32>	numVert		=new List<Int32>();
			List<Int32>	numFace		=new List<Int32>();

			Map.Print("Handling lightmapped alpha materials\n");

			foreach(Material mat in mMaterials)
			{
				int	numFaceVerts	=mLMAVerts.Count;
				int	numFaces		=0;

				if(!mat.Name.EndsWith("*LitAlpha"))
				{
					numFace.Add(numFaces);
					mLMAMaterialNumVerts.Add(mLMAVerts.Count - numFaceVerts);
					continue;
				}

				Map.Print("Light for material: " + mat.Name + ".\n");

				foreach(GFXFace f in mFaces)
				{
					if(f.mLightOfs == -1)
					{
						continue;	//only interested in lightmapped
					}

					if(f.mLTypes[1] != 255)
					{
						continue;	//only interested in non animated
					}

					GFXTexInfo	tex	=mTexInfos[f.mTexInfo];

					if(!mat.Name.StartsWith(tex.mMaterial))
					{
						continue;
					}

					numFaces++;

					//grab lightmap0
					double	scaleU, scaleV, offsetU, offsetV;
					scaleU	=scaleV	=offsetU	=offsetV	=0.0;
					Color	[]lmap	=new Color[f.mLHeight * f.mLWidth];

					for(int i=0;i < lmap.Length;i++)
					{
						lmap[i].R	=lightData[f.mLightOfs + (i * 3)];
						lmap[i].G	=lightData[f.mLightOfs + (i * 3) + 1];
						lmap[i].B	=lightData[f.mLightOfs + (i * 3) + 2];
						lmap[i].A	=0xFF;
					}
					if(!mLMAtlas.Insert(lmap, f.mLWidth, f.mLHeight,
						out scaleU, out scaleV, out offsetU, out offsetV))
					{
						Map.Print("Lightmap atlas out of space, try increasing it's size.\n");
						return	false;
					}

					List<Vector3>	fverts	=new List<Vector3>();

					int		nverts	=f.mNumVerts;
					int		fvert	=f.mFirstVert;
					int		k		=0;
					for(k=0;k < nverts;k++)
					{
						int		idx	=indexes[fvert + k];
						Vector3	pnt	=verts[idx];
						Vector2	crd;
						crd.X	=Vector3.Dot(tex.mVecs[0], pnt) + tex.mShift[0];
						crd.Y	=Vector3.Dot(tex.mVecs[1], pnt) + tex.mShift[1];

						mLMAFaceTex0.Add(crd);
						fverts.Add(pnt);

						mLMAVerts.Add(pnt);
					}

					//flat shaded normals for lightmapped surfaces
					ComputeNormals(fverts, mLMANormals);

					List<Vector2>	coords	=new List<Vector2>();
					GetTexCoords1(fverts, f.mLWidth, f.mLHeight, tex, out coords);

					float	crunchX	=f.mLWidth / (float)(f.mLWidth + 1);
					float	crunchY	=f.mLHeight / (float)(f.mLHeight + 1);

					for(k=0;k < nverts;k++)
					{
						Vector2	tc	=coords[k];

						//stretch coords to +1 size
						tc.X	*=crunchX;
						tc.Y	*=crunchY;

						//scale to atlas space
						tc.X	/=mLMAtlas.Width;
						tc.Y	/=mLMAtlas.Height;

						//step half a pixel in atlas space
						tc.X	+=1.0f / (mLMAtlas.Width * 2.0f);
						tc.Y	+=1.0f / (mLMAtlas.Height * 2.0f);

						//move to atlas position
						tc.X	+=(float)offsetU;
						tc.Y	+=(float)offsetV;

						mLMAFaceTex1.Add(tc);
					}

					firstVert.Add(mLMAVerts.Count - f.mNumVerts);
					numVert.Add(f.mNumVerts);
				}

				numFace.Add(numFaces);
				mLMAMaterialNumVerts.Add(mLMAVerts.Count - numFaceVerts);
			}

			mLMAtlas.Finish();

			ComputeIndexes(mLMAIndexes, mLMAMaterialOffsets, mLMAMaterialNumTris,
				numFace, firstVert, numVert, mLMAVerts, mLMAMaterialSortPoints);

			return	true;
		}


		internal bool BuildVLitFaceData(Vector3 []verts,
			Vector3 []rgbVerts, Vector3 []vnorms, int[] indexes)
		{
			List<Int32>	firstVert	=new List<Int32>();
			List<Int32>	numVert		=new List<Int32>();
			List<Int32>	numFace		=new List<Int32>();

			Map.Print("Building vertex lit face data...\n");

			foreach(Material mat in mMaterials)
			{
				int	numFaceVerts	=mVLitVerts.Count;
				int	numFaces		=0;

				if(!mat.Name.EndsWith("*VertLit"))
				{
					numFace.Add(numFaces);
					mVLitMaterialNumVerts.Add(mVLitVerts.Count - numFaceVerts);
					continue;
				}

				Map.Print("Material: " + mat.Name + ".\n");

				foreach(GFXFace f in mFaces)
				{
					if(f.mLightOfs != -1)
					{
						continue;	//only interested in non lightmapped
					}

					//check anim lights for good measure
					Debug.Assert(f.mLTypes[0] == 255);
					Debug.Assert(f.mLTypes[1] == 255);
					Debug.Assert(f.mLTypes[2] == 255);
					Debug.Assert(f.mLTypes[3] == 255);

					GFXTexInfo	tex	=mTexInfos[f.mTexInfo];

					if(!mat.Name.StartsWith(tex.mMaterial))
					{
						continue;
					}

					numFaces++;

					List<Vector3>	fverts	=new List<Vector3>();

					int		nverts	=f.mNumVerts;
					int		fvert	=f.mFirstVert;
					int		k		=0;
					for(k=0;k < nverts;k++)
					{
						int		idx	=indexes[fvert + k];
						Vector3	pnt	=verts[idx];
						Vector2	crd;
						crd.X	=Vector3.Dot(tex.mVecs[0], pnt) + tex.mShift[0];
						crd.Y	=Vector3.Dot(tex.mVecs[1], pnt) + tex.mShift[1];

						mVLitTex0.Add(crd);
						fverts.Add(pnt);

						mVLitVerts.Add(pnt);

						if(tex.IsGouraud())						
						{
							mVLitNormals.Add(vnorms[idx]);
						}
						Vector4	col	=Vector4.One;
						col.X	=rgbVerts[fvert + k].X;
						col.Y	=rgbVerts[fvert + k].Y;
						col.Z	=rgbVerts[fvert + k].Z;
						mVLitColors.Add(col);
					}

					if(!tex.IsGouraud())
					{
						ComputeNormals(fverts, mVLitNormals);
					}

					firstVert.Add(mVLitVerts.Count - f.mNumVerts);
					numVert.Add(f.mNumVerts);
				}
				numFace.Add(numFaces);
				mVLitMaterialNumVerts.Add(mVLitVerts.Count - numFaceVerts);
			}

			ComputeIndexes(mVLitIndexes, mVLitMaterialOffsets,
				mVLitMaterialNumTris, numFace, firstVert, numVert, null, null);

			return	true;
		}


		internal bool BuildMirrorFaceData(Vector3 []verts,
			Vector3 []rgbVerts, Vector3 []vnorms, int[] indexes)
		{
			List<Int32>	firstVert	=new List<Int32>();
			List<Int32>	numVert		=new List<Int32>();
			List<Int32>	numFace		=new List<Int32>();

			Map.Print("Building mirror face data...\n");

			foreach(Material mat in mMaterials)
			{
				int	numFaceVerts	=mMirrorVerts.Count;
				int	numFaces		=0;

				if(!mat.Name.EndsWith("*Mirror"))
				{
					numFace.Add(numFaces);
					mMirrorMaterialNumVerts.Add(mMirrorVerts.Count - numFaceVerts);
					continue;
				}

				Map.Print("Material: " + mat.Name + ".\n");

				foreach(GFXFace f in mFaces)
				{
					if(f.mLightOfs != -1)
					{
						continue;	//only interested in non lightmapped
					}

					//check anim lights for good measure
					Debug.Assert(f.mLTypes[0] == 255);
					Debug.Assert(f.mLTypes[1] == 255);
					Debug.Assert(f.mLTypes[2] == 255);
					Debug.Assert(f.mLTypes[3] == 255);

					GFXTexInfo	tex	=mTexInfos[f.mTexInfo];

					if(!mat.Name.StartsWith(tex.mMaterial))
					{
						continue;
					}

					numFaces++;

					List<Vector3>	fverts	=new List<Vector3>();

					int		nverts	=f.mNumVerts;
					int		fvert	=f.mFirstVert;
					int		k		=0;
					for(k=0;k < nverts;k++)
					{
						int		idx	=indexes[fvert + k];
						Vector3	pnt	=verts[idx];
						Vector2	crd;
						crd.X	=Vector3.Dot(tex.mVecs[0], pnt) + tex.mShift[0];
						crd.Y	=Vector3.Dot(tex.mVecs[1], pnt) + tex.mShift[1];

						fverts.Add(pnt);
						mMirrorVerts.Add(pnt);

						if(tex.IsGouraud())						
						{
							mMirrorNormals.Add(vnorms[idx]);
						}
						Vector4	col	=Vector4.One;
						col.X	=rgbVerts[fvert + k].X;
						col.Y	=rgbVerts[fvert + k].Y;
						col.Z	=rgbVerts[fvert + k].Z;
						mMirrorColors.Add(col);
					}

					List<Vector2>	coords	=new List<Vector2>();
					GetMirrorTexCoords(fverts, 256, 256, tex, out coords);
					mMirrorTex0.AddRange(coords);

					mMirrorPolys.Add(fverts);

					if(!tex.IsGouraud())
					{
						ComputeNormals(fverts, mMirrorNormals);
					}

					firstVert.Add(mMirrorVerts.Count - f.mNumVerts);
					numVert.Add(f.mNumVerts);
				}
				numFace.Add(numFaces);
				mMirrorMaterialNumVerts.Add(mMirrorVerts.Count - numFaceVerts);
			}

			ComputeIndexes(mMirrorIndexes, mMirrorMaterialOffsets,
				mMirrorMaterialNumTris, numFace, firstVert,
				numVert, mMirrorVerts, mMirrorMaterialSortPoints);

			return	true;
		}


		internal bool BuildAlphaFaceData(Vector3 []verts,
			Vector3 []rgbVerts, Vector3 []vnorms, int[] indexes)
		{
			List<Int32>	firstVert	=new List<Int32>();
			List<Int32>	numVert		=new List<Int32>();
			List<Int32>	numFace		=new List<Int32>();

			Map.Print("Building alpha face data...\n");

			foreach(Material mat in mMaterials)
			{
				int	numFaceVerts	=mAlphaVerts.Count;
				int	numFaces		=0;

				if(!mat.Name.EndsWith("*Alpha"))
				{
					numFace.Add(numFaces);
					mAlphaMaterialNumVerts.Add(mAlphaVerts.Count - numFaceVerts);
					continue;
				}

				Map.Print("Material: " + mat.Name + ".\n");

				foreach(GFXFace f in mFaces)
				{
					if(f.mLightOfs != -1)
					{
						continue;	//only interested in non lightmapped
					}

					//check anim lights for good measure
					Debug.Assert(f.mLTypes[0] == 255);
					Debug.Assert(f.mLTypes[1] == 255);
					Debug.Assert(f.mLTypes[2] == 255);
					Debug.Assert(f.mLTypes[3] == 255);

					GFXTexInfo	tex	=mTexInfos[f.mTexInfo];

					if(!mat.Name.StartsWith(tex.mMaterial))
					{
						continue;
					}

					numFaces++;

					List<Vector3>	fverts	=new List<Vector3>();

					int		nverts	=f.mNumVerts;
					int		fvert	=f.mFirstVert;
					int		k		=0;
					for(k=0;k < nverts;k++)
					{
						int		idx	=indexes[fvert + k];
						Vector3	pnt	=verts[idx];
						Vector2	crd;
						crd.X	=Vector3.Dot(tex.mVecs[0], pnt) + tex.mShift[0];
						crd.Y	=Vector3.Dot(tex.mVecs[1], pnt) + tex.mShift[1];

						mAlphaTex0.Add(crd);
						fverts.Add(pnt);

						mAlphaVerts.Add(pnt);

						if(tex.IsGouraud())						
						{
							mAlphaNormals.Add(vnorms[idx]);
						}
						Vector4	col	=Vector4.One;
						col.X	=rgbVerts[fvert + k].X;
						col.Y	=rgbVerts[fvert + k].Y;
						col.Z	=rgbVerts[fvert + k].Z;
						mAlphaColors.Add(col);
					}

					if(!tex.IsGouraud())
					{
						ComputeNormals(fverts, mAlphaNormals);
					}

					firstVert.Add(mAlphaVerts.Count - f.mNumVerts);
					numVert.Add(f.mNumVerts);
				}
				numFace.Add(numFaces);
				mAlphaMaterialNumVerts.Add(mAlphaVerts.Count - numFaceVerts);
			}

			ComputeIndexes(mAlphaIndexes, mAlphaMaterialOffsets,
				mAlphaMaterialNumTris, numFace, firstVert,
				numVert, mAlphaVerts, mAlphaMaterialSortPoints);

			return	true;
		}


		internal bool BuildFullBrightFaceData(Vector3 []verts, int[] indexes)
		{
			List<Int32>	firstVert	=new List<Int32>();
			List<Int32>	numVert		=new List<Int32>();
			List<Int32>	numFace		=new List<Int32>();

			Map.Print("Building full bright face data...\n");

			foreach(Material mat in mMaterials)
			{
				int	numFaceVerts	=mFBVerts.Count;
				int	numFaces		=0;

				if(!mat.Name.EndsWith("*FullBright"))
				{
					numFace.Add(numFaces);
					mFBMaterialNumVerts.Add(mFBVerts.Count - numFaceVerts);
					continue;
				}

				Map.Print("Material: " + mat.Name + ".\n");

				foreach(GFXFace f in mFaces)
				{
					if(f.mLightOfs != -1)
					{
						continue;	//only interested in non lightmapped
					}

					//check anim lights for good measure
					Debug.Assert(f.mLTypes[0] == 255);
					Debug.Assert(f.mLTypes[1] == 255);
					Debug.Assert(f.mLTypes[2] == 255);
					Debug.Assert(f.mLTypes[3] == 255);

					GFXTexInfo	tex	=mTexInfos[f.mTexInfo];

					if(!mat.Name.StartsWith(tex.mMaterial))
					{
						continue;
					}

					numFaces++;

					List<Vector3>	fverts	=new List<Vector3>();

					int		nverts	=f.mNumVerts;
					int		fvert	=f.mFirstVert;
					int		k		=0;
					for(k=0;k < nverts;k++)
					{
						int		idx	=indexes[fvert + k];
						Vector3	pnt	=verts[idx];
						Vector2	crd;
						crd.X	=Vector3.Dot(tex.mVecs[0], pnt) + tex.mShift[0];
						crd.Y	=Vector3.Dot(tex.mVecs[1], pnt) + tex.mShift[1];

						mFBTex0.Add(crd);
						fverts.Add(pnt);

						mFBVerts.Add(pnt);
					}

					firstVert.Add(mFBVerts.Count - f.mNumVerts);
					numVert.Add(f.mNumVerts);
				}
				numFace.Add(numFaces);
				mFBMaterialNumVerts.Add(mFBVerts.Count - numFaceVerts);
			}

			ComputeIndexes(mFBIndexes, mFBMaterialOffsets,
				mFBMaterialNumTris, numFace, firstVert, numVert, null, null);

			return	true;
		}


		internal bool BuildSkyFaceData(Vector3 []verts, int[] indexes)
		{
			List<Int32>	firstVert	=new List<Int32>();
			List<Int32>	numVert		=new List<Int32>();
			List<Int32>	numFace		=new List<Int32>();

			Map.Print("Building sky face data...\n");

			foreach(Material mat in mMaterials)
			{
				int	numFaceVerts	=mSkyVerts.Count;
				int	numFaces		=0;

				if(!mat.Name.EndsWith("*Sky"))
				{
					numFace.Add(numFaces);
					mSkyMaterialNumVerts.Add(mSkyVerts.Count - numFaceVerts);
					continue;
				}

				Map.Print("Material: " + mat.Name + ".\n");

				foreach(GFXFace f in mFaces)
				{
					if(f.mLightOfs != -1)
					{
						continue;	//only interested in non lightmapped
					}

					//check anim lights for good measure
					Debug.Assert(f.mLTypes[0] == 255);
					Debug.Assert(f.mLTypes[1] == 255);
					Debug.Assert(f.mLTypes[2] == 255);
					Debug.Assert(f.mLTypes[3] == 255);

					GFXTexInfo	tex	=mTexInfos[f.mTexInfo];

					if(!mat.Name.StartsWith(tex.mMaterial))
					{
						continue;
					}

					numFaces++;

					List<Vector3>	fverts	=new List<Vector3>();

					int		nverts	=f.mNumVerts;
					int		fvert	=f.mFirstVert;
					int		k		=0;
					for(k=0;k < nverts;k++)
					{
						int		idx	=indexes[fvert + k];
						Vector3	pnt	=verts[idx];
						Vector2	crd;
						crd.X	=Vector3.Dot(tex.mVecs[0], pnt) + tex.mShift[0];
						crd.Y	=Vector3.Dot(tex.mVecs[1], pnt) + tex.mShift[1];

						mSkyTex0.Add(crd);
						fverts.Add(pnt);

						mSkyVerts.Add(pnt);
					}

					firstVert.Add(mSkyVerts.Count - f.mNumVerts);
					numVert.Add(f.mNumVerts);
				}
				numFace.Add(numFaces);
				mSkyMaterialNumVerts.Add(mSkyVerts.Count - numFaceVerts);
			}

			ComputeIndexes(mSkyIndexes, mSkyMaterialOffsets,
				mSkyMaterialNumTris, numFace, firstVert, numVert, null, null);

			return	true;
		}


		void ComputeIndexes(List<int> inds, List<int> matOffsets,
			List<int> matTris, List<int> numFace, List<int> firstVert,
			List<int> numVert, List<Vector3> verts, List<Vector3> sortPoints)
		{
			int	faceOfs	=0;
			for(int j=0;j < mMaterialNames.Count;j++)
			{
				int	cnt	=inds.Count;

				matOffsets.Add(cnt);

				if(sortPoints != null)
				{
					double	X	=0.0;
					double	Y	=0.0;
					double	Z	=0.0;

					//compute sort point
					int	numAvg	=0;
					for(int i=faceOfs;i < (numFace[j] + faceOfs);i++)
					{
						int		nverts	=numVert[i];
						int		fvert	=firstVert[i];
						for(int k=fvert;k < (fvert + nverts);k++)
						{
							X	+=verts[k].X;
							Y	+=verts[k].Y;
							Z	+=verts[k].Z;

							numAvg++;
						}
					}

					X	/=numAvg;
					Y	/=numAvg;
					Z	/=numAvg;

					sortPoints.Add(new Vector3((float)X, (float)Y, (float)Z));
				}

				for(int i=faceOfs;i < (numFace[j] + faceOfs);i++)
				{
					int		nverts	=numVert[i];
					int		fvert	=firstVert[i];
					int		k		=0;

					//triangulate
					for(k=1;k < nverts-1;k++)
					{
						inds.Add(fvert);
						inds.Add(fvert + k);
						inds.Add(fvert + ((k + 1) % nverts));
					}
				}

				faceOfs	+=numFace[j];

				int	numTris	=(inds.Count - cnt);

				numTris	/=3;

				matTris.Add(numTris);
			}
		}


		void ComputeNormals(List<Vector3> verts, List<Vector3> norms)
		{
			GBSPPlane	p	=new GBSPPlane(verts);

			foreach(Vector3 v in verts)
			{
				norms.Add(p.mNormal);
			}
		}


		void GetTexCoords1(List<Vector3> verts,
			int	lwidth, int lheight, GFXTexInfo tex,
			out List<Vector2> coords)
		{
			coords	=new List<Vector2>();

			float	minS, minT;

			minS	=Bounds.MIN_MAX_BOUNDS;
			minT	=Bounds.MIN_MAX_BOUNDS;

			GBSPPlane	pln;
			pln.mNormal	=Vector3.Cross(tex.mVecs[0], tex.mVecs[1]);

			pln.mNormal.Normalize();
			pln.mDist	=0;
			pln.mType	=GBSPPlane.PLANE_ANY;

			//get a proper set of texvecs for lighting
			Vector3	xv, yv;
			GBSPPoly.TextureAxisFromPlane(pln, out xv, out yv);

			//scale down to light space
			xv	/=mLightGridSize;
			yv	/=mLightGridSize;

			//calculate the min values for s and t
			foreach(Vector3 pnt in verts)
			{
				float	d	=Vector3.Dot(xv, pnt);
				if(d < minS)
				{
					minS	=d;
				}

				d	=Vector3.Dot(yv, pnt);
				if(d < minT)
				{
					minT	=d;
				}
			}

			float	shiftU	=-minS;
			float	shiftV	=-minT;

			foreach(Vector3 pnt in verts)
			{
				Vector2	crd;
				crd.X	=Vector3.Dot(xv, pnt);
				crd.Y	=Vector3.Dot(yv, pnt);

				crd.X	+=shiftU;
				crd.Y	+=shiftV;

				coords.Add(crd);
			}
		}


		void GetMirrorTexCoords(List<Vector3> verts,
			int	lwidth, int lheight, GFXTexInfo tex,
			out List<Vector2> coords)
		{
			coords	=new List<Vector2>();

			float	minS, minT;
			float	maxS, maxT;

			minS	=Bounds.MIN_MAX_BOUNDS;
			minT	=Bounds.MIN_MAX_BOUNDS;
			maxS	=-Bounds.MIN_MAX_BOUNDS;
			maxT	=-Bounds.MIN_MAX_BOUNDS;

			GBSPPlane	pln;
			pln.mNormal	=Vector3.Cross(tex.mVecs[0], tex.mVecs[1]);

			pln.mNormal.Normalize();
			pln.mDist	=0;
			pln.mType	=GBSPPlane.PLANE_ANY;

			//get a proper set of texvecs for lighting
			Vector3	xv, yv;
			GBSPPoly.TextureAxisFromPlane(pln, out xv, out yv);

			//calculate the min values for s and t
			foreach(Vector3 pnt in verts)
			{
				float	d	=Vector3.Dot(xv, pnt);
				if(d < minS)
				{
					minS	=d;
				}
				if(d > maxS)
				{
					maxS	=d;
				}

				d	=Vector3.Dot(yv, pnt);
				if(d < minT)
				{
					minT	=d;
				}
				if(d > maxT)
				{
					maxT	=d;
				}
			}

			float	shiftU	=-minS;
			float	shiftV	=-minT;

			Vector2	scale	=Vector2.Zero;
			scale.X	=maxS - minS;
			scale.Y	=maxT - minT;

			foreach(Vector3 pnt in verts)
			{
				Vector2	crd;
				crd.X	=Vector3.Dot(xv, pnt);
				crd.Y	=Vector3.Dot(yv, pnt);

				crd.X	+=shiftU;
				crd.Y	+=shiftV;

				crd	/=scale;

				coords.Add(crd);
			}
		}


		internal List<string> GetMaterialNames()
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

				mMaterials.Add(mat);
			}
		}


		internal static string ScryTrueName(GFXFace f, GFXTexInfo tex)
		{
			string	matName	=tex.mMaterial;

			if(tex.IsLightMapped())
			{
				if(tex.IsAlpha() && f.mLightOfs != -1)
				{
					matName	+="*LitAlpha";
				}
				else if(tex.IsAlpha())
				{
					matName	+="*Alpha";
				}
				else if(f.mLightOfs == -1)
				{
					matName	+="*VertLit";
				}
			}
			else if(tex.IsMirror())
			{
				matName	+="*Mirror";
			}
			else if(tex.IsAlpha())
			{
				matName	+="*Alpha";
			}
			else if(tex.IsFlat() || tex.IsGouraud())
			{
				matName	+="*VertLit";
			}
			else if(tex.IsFullBright() || tex.IsLight())
			{
				matName	+="*FullBright";
			}
			else if(tex.IsSky())
			{
				matName	+="*Sky";
			}

			int	numStyles	=0;
			for(int i=0;i < 4;i++)
			{
				if(f.mLTypes[i] != 255)
				{
					numStyles++;
				}
			}

			//animated lights ?
			if(numStyles > 1 || (numStyles == 1 && f.mLTypes[0] != 0))
			{
				Debug.Assert(tex.IsLightMapped());

				//see if material is already alpha
				if(matName.Contains("*LitAlpha"))
				{
					matName	+="Anim";	//*LitAlphaAnim
				}
				else
				{
					matName	+="*Anim";
				}
			}

			return	matName;
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
				string	matName	=ScryTrueName(f, mTexInfos[f.mTexInfo]);

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