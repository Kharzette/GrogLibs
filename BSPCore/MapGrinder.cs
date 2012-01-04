using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MaterialLib;
using MeshLib;


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
		List<DrawCall>	mLMDraws		=new List<DrawCall>();
		List<DrawCall>	mVLitDraws		=new List<DrawCall>();
		List<DrawCall>	mFBDraws		=new List<DrawCall>();
		List<DrawCall>	mSkyDraws		=new List<DrawCall>();
		List<DrawCall>	mLMAnimDraws	=new List<DrawCall>();

		//alphas
		List<List<DrawCall>>	mAlphaDraws		=new List<List<DrawCall>>();
		List<List<DrawCall>>	mMirrorDraws	=new List<List<DrawCall>>();
		List<List<DrawCall>>	mLMAAnimDraws	=new List<List<DrawCall>>();
		List<List<DrawCall>>	mLMADraws		=new List<List<DrawCall>>();

		//computed lightmap atlas
		TexAtlas	mLMAtlas;

		//passed in data
		int			mLightGridSize;
		GFXTexInfo	[]mTexInfos;
		GFXFace		[]mFaces;


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


		internal void GetLMMaterialData(out DrawCall []dcs)
		{
			dcs	=mLMDraws.ToArray();
		}


		internal void GetLMAMaterialData(out List<DrawCall> []draws)
		{
			draws	=mLMADraws.ToArray();
		}


		internal void GetAlphaMaterialData(out List<DrawCall> []draws)
		{
			draws	=mAlphaDraws.ToArray();
		}


		internal void GetVLitMaterialData(out DrawCall []dcs)
		{
			dcs	=mVLitDraws.ToArray();
		}


		internal void GetFullBrightMaterialData(out DrawCall []dcs)
		{
			dcs	=mFBDraws.ToArray();
		}


		internal void GetMirrorMaterialData(out List<DrawCall> []draws, out List<List<Vector3>> polys)
		{
			draws	=mMirrorDraws.ToArray();
			polys	=mMirrorPolys;
		}


		internal void GetSkyMaterialData(out DrawCall []dcs)
		{
			dcs	=mSkyDraws.ToArray();
		}


		internal void GetLMAnimMaterialData(out DrawCall []dcs)
		{
			dcs	=mLMAnimDraws.ToArray();
		}


		internal void GetLMAAnimMaterialData(out List<DrawCall> []draws)
		{
			draws	=mLMAAnimDraws.ToArray();
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


		static List<Vector3> GetFaceVerts(GFXFace f, Vector3 []verts, int []indexes)
		{
			List<Vector3>	ret	=new List<Vector3>();
			for(int k=0;k < f.mNumVerts;k++)
			{
				int		idx	=indexes[f.mFirstVert + k];
				Vector3	pnt	=verts[idx];

				ret.Add(pnt);
			}
			return	ret;
		}


		//handles basic verts and texcoord 0
		static void ComputeFaceData(GFXFace f, Vector3 []verts, int []indexes, GFXTexInfo tex,
			List<Vector2> tex0, List<Vector3> outVerts)
		{
			outVerts.AddRange(GetFaceVerts(f, verts, indexes));
			foreach(Vector3 v in outVerts)
			{
				Vector2	crd;
				crd.X	=Vector3.Dot(tex.mVecU, v) + tex.mShiftU;
				crd.Y	=Vector3.Dot(tex.mVecV, v) + tex.mShiftV;

				tex0.Add(crd);
			}
		}


		//sided plane should be pre flipped if side != 0
		static void ComputeFaceNormals(GFXFace f, Vector3 []verts, int []indexes,
			GFXTexInfo tex, Vector3 []vnorms, GBSPPlane sidedPlane,
			List<Vector3> norms)
		{
			for(int k=0;k < f.mNumVerts;k++)
			{
				int		idx	=indexes[f.mFirstVert + k];

				if(tex.IsGouraud())						
				{
					norms.Add(vnorms[idx]);
				}
				else
				{
					norms.Add(sidedPlane.mNormal);
				}
			}
		}


		static void ComputeFaceColors(GFXFace f, Vector3 []verts, int []indexes,
			GFXTexInfo tex, Vector3 []rgbVerts,	List<Vector4> colors)
		{
			int	fvert	=f.mFirstVert;
			for(int k=0;k < f.mNumVerts;k++)
			{
				int		idx	=indexes[fvert + k];

				Vector4	col	=Vector4.One;
				if((tex.mFlags & TexInfo.FULLBRIGHT) == 0 && rgbVerts != null)
				{
					col.X	=rgbVerts[fvert + k].X / 255.0f;
					col.Y	=rgbVerts[fvert + k].Y / 255.0f;
					col.Z	=rgbVerts[fvert + k].Z / 255.0f;
				}

				if((tex.mFlags & TexInfo.TRANS) != 0)
				{
					col.W	=tex.mAlpha;
				}
				colors.Add(col);
			}
		}


		bool AtlasLightMap(GFXFace f, byte []lightData, int styleIndex, List<Vector3> faceVerts,
			GBSPPlane sidedPlane, GFXTexInfo tex, List<Vector2> texCoords)
		{
			double	scaleU, scaleV, offsetU, offsetV;
			scaleU	=scaleV	=offsetU	=offsetV	=0.0;
			Color	[]lmap	=new Color[f.mLHeight * f.mLWidth];

			int	sizeOffset	=f.mLHeight * f.mLWidth * 3;

			sizeOffset	*=styleIndex;

			for(int i=0;i < lmap.Length;i++)
			{
				lmap[i].R	=lightData[sizeOffset + f.mLightOfs + (i * 3)];
				lmap[i].G	=lightData[sizeOffset + f.mLightOfs + (i * 3) + 1];
				lmap[i].B	=lightData[sizeOffset + f.mLightOfs + (i * 3) + 2];
				lmap[i].A	=0xFF;
			}

			if(!mLMAtlas.Insert(lmap, f.mLWidth, f.mLHeight,
				out scaleU, out scaleV, out offsetU, out offsetV))
			{
				CoreEvents.Print("Lightmap atlas out of space, try increasing it's size.\n");
				return	false;
			}

			List<double>	coordsU	=new List<double>();
			List<double>	coordsV	=new List<double>();
			GetTexCoords1(faceVerts, sidedPlane, f.mLWidth, f.mLHeight, tex, out coordsU, out coordsV);
			AddTexCoordsToList(texCoords, coordsU, coordsV, offsetU, offsetV);

			return	true;
		}


		bool AtlasAnimated(DrawDataChunk ddc, GFXFace f, byte []lightData,
			List<Vector3> faceVerts, GBSPPlane pln, GFXTexInfo tex)
		{
			for(int s=0;s < 4;s++)
			{
				List<Vector2>	coordSet	=null;
				bool			bTuFittyFi	=false;

				if(s == 0)
				{
					if(f.mLType0 == 255)
					{
						bTuFittyFi	=true;
					}
					coordSet	=ddc.mTex1;
				}
				else if(s == 1)
				{
					if(f.mLType1 == 255)
					{
						bTuFittyFi	=true;
					}
					coordSet	=ddc.mTex2;
				}
				else if(s == 2)
				{
					if(f.mLType2 == 255)
					{
						bTuFittyFi	=true;
					}
					coordSet	=ddc.mTex3;
				}
				else if(s == 3)
				{
					if(f.mLType3 == 255)
					{
						bTuFittyFi	=true;
					}
					coordSet	=ddc.mTex4;
				}

				if(bTuFittyFi)
				{
					for(int i=0;i < faceVerts.Count;i++)
					{
						coordSet.Add(Vector2.Zero);
					}
					continue;
				}

				if(!AtlasLightMap(f, lightData, s, faceVerts, pln, tex, coordSet))
				{
					return	false;
				}
			}
			return	true;
		}


		internal bool BuildLMAnimFaceData(Vector3 []verts, int[] indexes,
			int firstFace, int nFaces, byte []lightData, object pobj)
		{
			GFXPlane	[]pp		=pobj as GFXPlane [];

			CoreEvents.Print("Handling animated lightmaps...\n");

			//store faces per material
			List<DrawDataChunk>	matChunks	=new List<DrawDataChunk>();

			foreach(Material mat in mMaterials)
			{
				DrawDataChunk	ddc	=new DrawDataChunk();
				matChunks.Add(ddc);

				if(!mat.Name.EndsWith("*Anim"))
				{
					continue;
				}

				CoreEvents.Print("Animated light for material: " + mat.Name + ".\n");

				for(int face=firstFace;face < (firstFace + nFaces);face++)
				{
					GFXFace	f	=mFaces[face];
					if(f.mLightOfs == -1)
					{
						continue;	//only interested in lightmapped
					}

					GFXTexInfo	tex	=mTexInfos[f.mTexInfo];

					if(tex.mAlpha < 1.0f)
					{
						continue;
					}

					if(!mat.Name.StartsWith(tex.mMaterial))
					{
						continue;
					}

					//make sure actually animating
					if(f.mLType0 ==0 || f.mLType0 == 255)
					{
						if(f.mLType1 ==0 || f.mLType1 == 255)
						{
							if(f.mLType2 ==0 || f.mLType2 == 255)
							{
								if(f.mLType3 ==0 || f.mLType3 == 255)
								{
									continue;
								}
							}
						}
					}

					ddc.mNumFaces++;
					ddc.mVCounts.Add(f.mNumVerts);

					GFXPlane	pl	=pp[f.mPlaneNum];
					GBSPPlane	pln	=new GBSPPlane(pl);
					if(f.mPlaneSide > 0)
					{
						pln.Inverse();
					}

					List<Vector3>	faceVerts	=new List<Vector3>();
					ComputeFaceData(f, verts, indexes, tex, ddc.mTex0, faceVerts);
					ComputeFaceNormals(f, verts, indexes, tex, null, pln, ddc.mNorms);

					foreach(Vector3 v in faceVerts)
					{
						ddc.mColors.Add(new Vector4(1, 1, 1, tex.mAlpha));
					}

					AtlasAnimated(ddc, f, lightData, faceVerts, pln, tex);

					ddc.mVerts.AddRange(faceVerts);

					//style index
					for(int k=0;k < f.mNumVerts;k++)
					{
						Vector4	styleIndex	=Vector4.Zero;
						styleIndex.X	=f.mLType0;
						styleIndex.Y	=f.mLType1;
						styleIndex.Z	=f.mLType2;
						styleIndex.W	=f.mLType3;
						ddc.mStyles.Add(styleIndex);
					}
				}
			}

			mLMAtlas.Finish();

			mLMAnimDraws	=ComputeIndexes(mLMAnimIndexes, matChunks);

			StuffVBArrays(matChunks, mLMAnimVerts, mLMAnimNormals,
				mLMAnimFaceTex0, mLMAnimFaceTex1, mLMAnimFaceTex2,
				mLMAnimFaceTex3, mLMAnimFaceTex4, null, mLMAnimStyle);

			return	true;
		}


		internal bool BuildLMAAnimFaceData(Vector3 []verts, int[] indexes,
			int firstFace, int nFaces, byte []lightData, object pobj)
		{
			GFXPlane	[]pp		=pobj as GFXPlane [];

			CoreEvents.Print("Handling alpha animated lightmaps...\n");

			//store each plane used, and how many faces per material
			List<Dictionary<Int32, DrawDataChunk>>	perPlaneChunks
				=new List<Dictionary<Int32, DrawDataChunk>>();

			foreach(Material mat in mMaterials)
			{
				Dictionary<Int32, DrawDataChunk>	ddcs
					=new Dictionary<Int32, DrawDataChunk>();
				perPlaneChunks.Add(ddcs);

				if(!mat.Name.EndsWith("*LitAlphaAnim"))
				{
					continue;
				}

				CoreEvents.Print("Animated light for material: " + mat.Name + ".\n");

				for(int face=firstFace;face < (firstFace + nFaces);face++)
				{
					GFXFace	f	=mFaces[face];
					if(f.mLightOfs == -1)
					{
						continue;	//only interested in lightmapped
					}

					GFXTexInfo	tex	=mTexInfos[f.mTexInfo];

					if(tex.mAlpha >= 1.0f)
					{
						continue;
					}
					if(!mat.Name.StartsWith(tex.mMaterial))
					{
						continue;
					}

					if(f.mLType0 ==0 || f.mLType0 == 255)
					{
						if(f.mLType1 ==0 || f.mLType1 == 255)
						{
							if(f.mLType2 ==0 || f.mLType2 == 255)
							{
								if(f.mLType3 ==0 || f.mLType3 == 255)
								{
									continue;
								}
							}
						}
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

					ddc.mNumFaces++;
					ddc.mVCounts.Add(f.mNumVerts);

					GFXPlane	pl	=pp[f.mPlaneNum];
					GBSPPlane	pln	=new GBSPPlane(pl);
					if(f.mPlaneSide > 0)
					{
						pln.Inverse();
					}

					List<Vector3>	faceVerts	=new List<Vector3>();
					ComputeFaceData(f, verts, indexes, tex, ddc.mTex0, faceVerts);
					ComputeFaceNormals(f, verts, indexes, tex, null, pln, ddc.mNorms);

					foreach(Vector3 v in faceVerts)
					{
						ddc.mColors.Add(new Vector4(1, 1, 1, tex.mAlpha));
					}

					AtlasAnimated(ddc, f, lightData, faceVerts, pln, tex);

					ddc.mVerts.AddRange(faceVerts);

					//style index
					for(int k=0;k < f.mNumVerts;k++)
					{
						Vector4	styleIndex	=Vector4.Zero;
						styleIndex.X	=f.mLType0;
						styleIndex.Y	=f.mLType1;
						styleIndex.Z	=f.mLType2;
						styleIndex.W	=f.mLType3;
						ddc.mStyles.Add(styleIndex);
					}

					if(!ddcs.ContainsKey(f.mPlaneNum))
					{
						ddcs.Add(f.mPlaneNum, ddc);
					}
				}
			}

			mLMAtlas.Finish();

			mLMAAnimDraws	=ComputeAlphaIndexes(mLMAAnimIndexes, perPlaneChunks);

			StuffVBArrays(perPlaneChunks, mLMAAnimVerts, mLMAAnimNormals,
				mLMAAnimFaceTex0, mLMAAnimFaceTex1, mLMAAnimFaceTex2, 
				mLMAAnimFaceTex3, mLMAAnimFaceTex4, mLMAAnimColors,
				mLMAAnimStyle);

			return	true;
		}


		internal bool BuildLMFaceData(Vector3 []verts, int[] indexes,
			int firstFace, int nFaces, byte []lightData, object pobj)
		{
			GFXPlane	[]pp	=pobj as GFXPlane [];
			if(lightData == null)
			{
				return	false;
			}

			CoreEvents.Print("Atlasing " + lightData.Length + " bytes of light data...\n");

			//store faces per material
			List<DrawDataChunk>	matChunks	=new List<DrawDataChunk>();

			foreach(Material mat in mMaterials)
			{
				DrawDataChunk	ddc	=new DrawDataChunk();
				matChunks.Add(ddc);

				//skip all special materials
				if(mat.Name.Contains("*"))
				{
					continue;
				}

				CoreEvents.Print("Light for material: " + mat.Name + ".\n");

				for(int face=firstFace;face < (firstFace + nFaces);face++)
				{
					GFXFace	f	=mFaces[face];
					if(f.mLightOfs == -1)
					{
						continue;	//only interested in lightmapped
					}

					//make sure not animating
					if(f.mLType1 != 255 || f.mLType2 != 255 || f.mLType3 != 255)
					{
						continue;
					}
					if(f.mLType0 != 0)
					{
						continue;
					}

					GFXTexInfo	tex	=mTexInfos[f.mTexInfo];

					if(tex.mAlpha < 1.0f)
					{
						continue;
					}
					if(tex.mMaterial != mat.Name)
					{
						continue;
					}

					ddc.mNumFaces++;
					ddc.mVCounts.Add(f.mNumVerts);

					//grab plane for dynamic lighting normals
					GFXPlane	pl	=pp[f.mPlaneNum];
					GBSPPlane	pln	=new GBSPPlane(pl);
					if(f.mPlaneSide > 0)
					{
						pln.Inverse();
					}

					List<Vector3>	faceVerts	=new List<Vector3>();
					ComputeFaceData(f, verts, indexes, tex, ddc.mTex0, faceVerts);
					ComputeFaceNormals(f, verts, indexes, tex, null, pln, ddc.mNorms);

					if(!AtlasLightMap(f, lightData, 0, faceVerts, pln, tex, ddc.mTex1))
					{
						return	false;
					}
					ddc.mVerts.AddRange(faceVerts);
				}
			}

			mLMAtlas.Finish();

			mLMDraws	=ComputeIndexes(mLMIndexes, matChunks);

			StuffVBArrays(matChunks, mLMVerts, mLMNormals,
				mLMFaceTex0, mLMFaceTex1, null, null,
				null, null, null);

			return	true;
		}


		internal bool BuildLMAFaceData(Vector3 []verts, int[] indexes,
			int firstFace, int nFaces, byte []lightData, object pobj)
		{
			GFXPlane	[]pp		=pobj as GFXPlane [];

			CoreEvents.Print("Handling lightmapped alpha materials\n");

			//store each plane used, and how many faces per material
			List<Dictionary<Int32, DrawDataChunk>>	perPlaneChunks
				=new List<Dictionary<Int32, DrawDataChunk>>();

			foreach(Material mat in mMaterials)
			{
				Dictionary<Int32, DrawDataChunk>	ddcs
					=new Dictionary<Int32, DrawDataChunk>();
				perPlaneChunks.Add(ddcs);

				if(!mat.Name.EndsWith("*LitAlpha"))
				{
					continue;
				}

				CoreEvents.Print("Light for material: " + mat.Name + ".\n");

				for(int face=firstFace;face < (firstFace + nFaces);face++)
				{
					GFXFace	f	=mFaces[face];
					if(f.mLightOfs == -1)
					{
						continue;	//only interested in lightmapped
					}

					//make sure not animating
					if(f.mLType1 != 255 || f.mLType2 != 255 || f.mLType3 != 255)
					{
						continue;
					}
					if(f.mLType0 != 0)
					{
						continue;
					}

					GFXTexInfo	tex	=mTexInfos[f.mTexInfo];

					if(tex.mAlpha >= 1.0f)
					{
						continue;
					}
					if(!mat.Name.StartsWith(tex.mMaterial))
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

					ddc.mNumFaces++;
					ddc.mVCounts.Add(f.mNumVerts);

					//grab plane for dynamic lighting normals
					GFXPlane	pl	=pp[f.mPlaneNum];
					GBSPPlane	pln	=new GBSPPlane(pl);
					if(f.mPlaneSide > 0)
					{
						pln.Inverse();
					}

					List<Vector3>	faceVerts	=new List<Vector3>();
					ComputeFaceData(f, verts, indexes, tex, ddc.mTex0, faceVerts);
					ComputeFaceNormals(f, verts, indexes, tex, null, pln, ddc.mNorms);

					foreach(Vector3 v in faceVerts)
					{
						ddc.mColors.Add(new Vector4(1, 1, 1, tex.mAlpha));
					}

					if(!AtlasLightMap(f, lightData, 0, faceVerts, pln, tex, ddc.mTex1))
					{
						return	false;
					}

					ddc.mVerts.AddRange(faceVerts);

					if(!ddcs.ContainsKey(f.mPlaneNum))
					{
						ddcs.Add(f.mPlaneNum, ddc);
					}
				}
			}

			mLMAtlas.Finish();

			mLMADraws	=ComputeAlphaIndexes(mLMAIndexes, perPlaneChunks);

			StuffVBArrays(perPlaneChunks, mLMAVerts, mLMANormals,
				mLMAFaceTex0, mLMAFaceTex1, null, null, null,
				mLMAColors, null);

			return	true;
		}


		internal bool BuildVLitFaceData(Vector3 []verts, Vector3 []rgbVerts, Vector3 []vnorms,
			int firstFace, int nFaces, int[] indexes, object pobj)
		{
			GFXPlane	[]pp	=pobj as GFXPlane [];

			CoreEvents.Print("Building vertex lit face data...\n");

			//store faces per material
			List<DrawDataChunk>	matChunks	=new List<DrawDataChunk>();

			foreach(Material mat in mMaterials)
			{
				DrawDataChunk	ddc	=new DrawDataChunk();
				matChunks.Add(ddc);

				if(!mat.Name.EndsWith("*VertLit"))
				{
					continue;
				}

				CoreEvents.Print("Material: " + mat.Name + ".\n");

				for(int face=firstFace;face < (firstFace + nFaces);face++)
				{
					GFXFace	f	=mFaces[face];
					if(f.mLightOfs != -1)
					{
						continue;	//only interested in non lightmapped
					}

					//check anim lights for good measure
					Debug.Assert(f.mLType0 == 255);
					Debug.Assert(f.mLType1 == 255);
					Debug.Assert(f.mLType2 == 255);
					Debug.Assert(f.mLType3 == 255);

					GFXTexInfo	tex	=mTexInfos[f.mTexInfo];

					if(tex.mAlpha < 1.0f)
					{
						continue;
					}

					if((tex.mFlags & 
						(TexInfo.FULLBRIGHT | TexInfo.MIRROR | TexInfo.SKY)) != 0)
					{
						continue;
					}

					if(!mat.Name.StartsWith(tex.mMaterial))
					{
						continue;
					}

					ddc.mNumFaces++;
					ddc.mVCounts.Add(f.mNumVerts);

					GFXPlane	pl	=pp[f.mPlaneNum];
					GBSPPlane	pln	=new GBSPPlane(pl);
					if(f.mPlaneSide > 0)
					{
						pln.Inverse();
					}

					List<Vector3>	faceVerts	=new List<Vector3>();
					ComputeFaceData(f, verts, indexes, tex, ddc.mTex0, faceVerts);
					ComputeFaceNormals(f, verts, indexes, tex, vnorms, pln, ddc.mNorms);
					ComputeFaceColors(f, verts, indexes, tex, rgbVerts, ddc.mColors);

					ddc.mVerts.AddRange(faceVerts);
				}
			}

			mVLitDraws	=ComputeIndexes(mVLitIndexes, matChunks);

			StuffVBArrays(matChunks, mVLitVerts, mVLitNormals,
				mVLitTex0, null, null, null,
				null, mVLitColors, null);

			return	true;
		}


		internal bool BuildMirrorFaceData(Vector3 []verts, Vector3 []rgbVerts, Vector3 []vnorms,
			int firstFace, int nFaces, int[] indexes, object pobj)
		{
			GFXPlane	[]pp		=pobj as GFXPlane [];

			CoreEvents.Print("Building mirror face data...\n");

			//store each plane used, and how many faces per material
			List<Dictionary<Int32, DrawDataChunk>>	perPlaneChunks
				=new List<Dictionary<Int32, DrawDataChunk>>();
			foreach(Material mat in mMaterials)
			{
				Dictionary<Int32, DrawDataChunk>	ddcs
					=new Dictionary<Int32, DrawDataChunk>();
				perPlaneChunks.Add(ddcs);

				if(!mat.Name.EndsWith("*Mirror"))
				{
					continue;
				}

				CoreEvents.Print("Material: " + mat.Name + ".\n");

				for(int face=firstFace;face < (firstFace + nFaces);face++)
				{
					GFXFace	f	=mFaces[face];
					if(f.mLightOfs != -1)
					{
						continue;	//only interested in non lightmapped
					}

					//check anim lights for good measure
					Debug.Assert(f.mLType0 == 255);
					Debug.Assert(f.mLType1 == 255);
					Debug.Assert(f.mLType2 == 255);
					Debug.Assert(f.mLType3 == 255);

					GFXTexInfo	tex	=mTexInfos[f.mTexInfo];

					if((tex.mFlags & TexInfo.MIRROR) == 0)
					{
						continue;
					}

					if(!mat.Name.StartsWith(tex.mMaterial))
					{
						continue;
					}
					if(!mat.Name.EndsWith("*Mirror"))
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

					ddc.mNumFaces++;
					ddc.mVCounts.Add(f.mNumVerts);

					GFXPlane	pl	=pp[f.mPlaneNum];
					GBSPPlane	pln	=new GBSPPlane(pl);
					if(f.mPlaneSide > 0)
					{
						pln.Inverse();
					}

					List<Vector3>	fverts	=new List<Vector3>();
					List<Vector2>	blah	=new List<Vector2>();
					ComputeFaceData(f, verts, indexes, tex, blah, fverts);
					ComputeFaceNormals(f, verts, indexes, tex, vnorms, pln, ddc.mNorms);
					ComputeFaceColors(f, verts, indexes, tex, rgbVerts, ddc.mColors);

					ddc.mVerts.AddRange(fverts);

					List<Vector2>	coords	=new List<Vector2>();
					GetMirrorTexCoords(fverts, 256, 256, tex, out coords);
					ddc.mTex0.AddRange(coords);

					mMirrorPolys.Add(fverts);

					if(!ddcs.ContainsKey(f.mPlaneNum))
					{
						ddcs.Add(f.mPlaneNum, ddc);
					}
				}
			}

			mMirrorDraws	=ComputeAlphaIndexes(mMirrorIndexes, perPlaneChunks);

			StuffVBArrays(perPlaneChunks, mMirrorVerts, mMirrorNormals,
				mMirrorTex0, null, null, null, null, mMirrorColors, null);

			return	true;
		}


		internal bool BuildAlphaFaceData(Vector3 []verts, Vector3 []rgbVerts, Vector3 []vnorms,
			int firstFace, int nFaces, int[] indexes, object pobj)
		{
			GFXPlane	[]pp		=pobj as GFXPlane [];

			CoreEvents.Print("Building alpha face data...\n");

			//store each plane used, and how many faces per material
			List<Dictionary<Int32, DrawDataChunk>>	perPlaneChunks
				=new List<Dictionary<Int32, DrawDataChunk>>();
			foreach(Material mat in mMaterials)
			{
				Dictionary<Int32, DrawDataChunk>	ddcs
					=new Dictionary<Int32, DrawDataChunk>();
				perPlaneChunks.Add(ddcs);

				if(!mat.Name.EndsWith("*Alpha"))
				{
					continue;
				}

				CoreEvents.Print("Material: " + mat.Name + ".\n");

				for(int face=firstFace;face < (firstFace + nFaces);face++)
				{
					GFXFace	f	=mFaces[face];
					if(f.mLightOfs != -1)
					{
						continue;	//only interested in non lightmapped
					}

					//check anim lights for good measure
					Debug.Assert(f.mLType0 == 255);
					Debug.Assert(f.mLType1 == 255);
					Debug.Assert(f.mLType2 == 255);
					Debug.Assert(f.mLType3 == 255);

					GFXTexInfo	tex	=mTexInfos[f.mTexInfo];

					if(tex.mAlpha >= 1.0f)
					{
						continue;
					}

					if((tex.mFlags & TexInfo.MIRROR) != 0)
					{
						continue;
					}

					if(!mat.Name.StartsWith(tex.mMaterial))
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

					ddc.mNumFaces++;
					ddc.mVCounts.Add(f.mNumVerts);

					GFXPlane	pl	=pp[f.mPlaneNum];
					GBSPPlane	pln	=new GBSPPlane(pl);
					if(f.mPlaneSide > 0)
					{
						pln.Inverse();
					}

					List<Vector3>	faceVerts	=new List<Vector3>();
					ComputeFaceData(f, verts, indexes, tex, ddc.mTex0, faceVerts);
					ComputeFaceNormals(f, verts, indexes, tex, vnorms, pln, ddc.mNorms);
					ComputeFaceColors(f, verts, indexes, tex, rgbVerts, ddc.mColors);

					ddc.mVerts.AddRange(faceVerts);

					if(!ddcs.ContainsKey(f.mPlaneNum))
					{
						ddcs.Add(f.mPlaneNum, ddc);
					}
				}
			}

			mAlphaDraws	=ComputeAlphaIndexes(mAlphaIndexes, perPlaneChunks);

			StuffVBArrays(perPlaneChunks, mAlphaVerts, mAlphaNormals,
				mAlphaTex0, null, null, null, null,	mAlphaColors, null);

			return	true;
		}


		internal bool BuildFullBrightFaceData(Vector3 []verts,
			int firstFace, int nFaces, int[] indexes, object pobj)
		{
			GFXPlane	[]pp		=pobj as GFXPlane [];

			CoreEvents.Print("Building full bright face data...\n");

			//store faces per material
			List<DrawDataChunk>	matChunks	=new List<DrawDataChunk>();

			foreach(Material mat in mMaterials)
			{
				DrawDataChunk	ddc	=new DrawDataChunk();
				matChunks.Add(ddc);

				if(!mat.Name.EndsWith("*FullBright"))
				{
					continue;
				}

				CoreEvents.Print("Material: " + mat.Name + ".\n");

				for(int face=firstFace;face < (firstFace + nFaces);face++)
				{
					GFXFace	f	=mFaces[face];
					if(f.mLightOfs != -1)
					{
						continue;	//only interested in non lightmapped
					}

					//check anim lights for good measure
					Debug.Assert(f.mLType0 == 255);
					Debug.Assert(f.mLType1 == 255);
					Debug.Assert(f.mLType2 == 255);
					Debug.Assert(f.mLType3 == 255);

					GFXTexInfo	tex	=mTexInfos[f.mTexInfo];

					if(tex.mAlpha < 1.0f)
					{
						continue;
					}
					if(!mat.Name.StartsWith(tex.mMaterial))
					{
						continue;
					}
					if(!mat.Name.EndsWith("*FullBright"))
					{
						continue;
					}

					ddc.mNumFaces++;
					ddc.mVCounts.Add(f.mNumVerts);

					List<Vector3>	faceVerts	=new List<Vector3>();
					ComputeFaceData(f, verts, indexes, tex, ddc.mTex0, faceVerts);

					ddc.mVerts.AddRange(faceVerts);
				}
			}

			mFBDraws	=ComputeIndexes(mFBIndexes, matChunks);

			StuffVBArrays(matChunks, mFBVerts, null,
				mVLitTex0, null, null, null,
				null, mVLitColors, null);

			return	true;
		}


		internal bool BuildSkyFaceData(Vector3 []verts,
			int firstFace, int nFaces, int[] indexes, object pobj)
		{
			GFXPlane	[]pp		=pobj as GFXPlane [];

			CoreEvents.Print("Building sky face data...\n");

			//store faces per material
			List<DrawDataChunk>	matChunks	=new List<DrawDataChunk>();

			foreach(Material mat in mMaterials)
			{
				DrawDataChunk	ddc	=new DrawDataChunk();
				matChunks.Add(ddc);

				if(!mat.Name.EndsWith("*Sky"))
				{
					continue;
				}

				CoreEvents.Print("Material: " + mat.Name + ".\n");

				for(int face=firstFace;face < (firstFace + nFaces);face++)
				{
					GFXFace	f	=mFaces[face];
					if(f.mLightOfs != -1)
					{
						continue;	//only interested in non lightmapped
					}

					//check anim lights for good measure
					Debug.Assert(f.mLType0 == 255);
					Debug.Assert(f.mLType1 == 255);
					Debug.Assert(f.mLType2 == 255);
					Debug.Assert(f.mLType3 == 255);

					GFXTexInfo	tex	=mTexInfos[f.mTexInfo];

					if(!tex.IsSky())
					{
						continue;
					}
					if(tex.mAlpha < 1.0f)
					{
						continue;
					}

					if(!mat.Name.StartsWith(tex.mMaterial))
					{
						continue;
					}
					if(!mat.Name.EndsWith("*Sky"))
					{
						continue;
					}

					ddc.mNumFaces++;
					ddc.mVCounts.Add(f.mNumVerts);

					List<Vector3>	faceVerts	=new List<Vector3>();
					ComputeFaceData(f, verts, indexes, tex, ddc.mTex0, faceVerts);

					ddc.mVerts.AddRange(faceVerts);
				}
			}

			mSkyDraws	=ComputeIndexes(mSkyIndexes, matChunks);

			StuffVBArrays(matChunks, mSkyVerts, null,
				mSkyTex0, null, null, null,
				null, null, null);

			return	true;
		}


		List<DrawCall> ComputeIndexes(List<int> inds, List<DrawDataChunk> ddcs)
		{
			List<DrawCall>	draws	=new List<DrawCall>();

			//index as if data is already in a big vbuffer
			int	vbVertOfs	=0;
			for(int j=0;j < mMaterialNames.Count;j++)
			{
				int	cnt	=inds.Count;

				DrawCall	dc	=new DrawCall();				
				dc.mStartIndex	=cnt;
				dc.mSortPoint	=Vector3.Zero;	//unused for opaques

				for(int i=0;i < ddcs[j].mNumFaces;i++)
				{
					int	nverts	=ddcs[j].mVCounts[i];

					//triangulate
					for(int k=1;k < nverts-1;k++)
					{
						inds.Add(vbVertOfs);
						inds.Add(vbVertOfs + k);
						inds.Add(vbVertOfs + ((k + 1) % nverts));
					}

					vbVertOfs	+=ddcs[j].mVCounts[i];
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
			List<Dictionary<Int32, DrawDataChunk>> perPlaneChunks)
		{
			List<List<DrawCall>>	draws	=new List<List<DrawCall>>();

			//index as if data is already in a big vbuffer
			int	vbVertOfs	=0;
			for(int j=0;j < mMaterialNames.Count;j++)
			{
				List<DrawCall>	dcs	=new List<DrawCall>();
				foreach(KeyValuePair<Int32, DrawDataChunk> pf in perPlaneChunks[j])
				{
					int	cnt	=inds.Count;

					DrawCall	dc	=new DrawCall();				
					dc.mStartIndex	=cnt;
					dc.mSortPoint	=ComputeSortPoint(pf.Value);

					for(int i=0;i < pf.Value.mNumFaces;i++)
					{
						int	nverts	=pf.Value.mVCounts[i];

						//triangulate
						for(int k=1;k < nverts-1;k++)
						{
							inds.Add(vbVertOfs);
							inds.Add(vbVertOfs + k);
							inds.Add(vbVertOfs + ((k + 1) % nverts));
						}

						vbVertOfs	+=pf.Value.mVCounts[i];
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


		void GetTexCoords1(List<Vector3> verts, GBSPPlane pln,
			int	lwidth, int lheight, GFXTexInfo tex,
			out List<double> sCoords, out List<double> tCoords)
		{
			sCoords	=new List<double>();
			tCoords	=new List<double>();

			//get a proper set of texvecs for lighting
			Vector3	xv, yv;
			GBSPPoly.TextureAxisFromPlane(pln, out xv, out yv);

			double	sX	=xv.X;
			double	sY	=xv.Y;
			double	sZ	=xv.Z;
			double	tX	=yv.X;
			double	tY	=yv.Y;
			double	tZ	=yv.Z;

			double	minS, minT;
			double	maxS, maxT;

			minS	=Bounds.MIN_MAX_BOUNDS;
			minT	=Bounds.MIN_MAX_BOUNDS;
			maxS	=-Bounds.MIN_MAX_BOUNDS;
			maxT	=-Bounds.MIN_MAX_BOUNDS;

			//calculate texture space extents
			foreach(Vector3 pnt in verts)
			{
				double	d	=(pnt.X * sX) + (pnt.Y * sY) + (pnt.Z * sZ);
				if(d < minS)
				{
					minS	=d;
				}
				if(d > maxS)
				{
					maxS	=d;
				}

				d	=(pnt.X * tX) + (pnt.Y * tY) + (pnt.Z * tZ);
				if(d < minT)
				{
					minT	=d;
				}
				if(d > maxT)
				{
					maxT	=d;
				}
			}

			//extent is the size of the surface in texels
			//note that these are texture texels not light
			double	extentS	=maxS - minS;
			double	extentT	=maxT - minT;

			//offset to the start of the texture
			double	shiftU	=-minS;
			double	shiftV	=-minT;

			foreach(Vector3 pnt in verts)
			{
				double	crdX, crdY;

				//dot product
				crdX	=(pnt.X * sX) + (pnt.Y * sY) + (pnt.Z * sZ);
				crdY	=(pnt.X * tX) + (pnt.Y * tY) + (pnt.Z * tZ);

				//shift relative to start position
				crdX	+=shiftU;
				crdY	+=shiftV;

				//now the coordinates are set for textures
				//scale by light grid size
				crdX	/=mLightGridSize;
				crdY	/=mLightGridSize;

				sCoords.Add(crdX);
				tCoords.Add(crdY);
			}
		}


		void AddTexCoordsToList(List<Vector2> tc, List<double> uList, List<double> vList, double offsetU, double offsetV)
		{
			for(int k=0;k < uList.Count;k++)
			{
				double	tcU	=uList[k];
				double	tcV	=vList[k];

				//scale to atlas space
				tcU	/=mLMAtlas.Width;
				tcV	/=mLMAtlas.Height;

				//step half a pixel in atlas space
				tcU	+=1.0 / (mLMAtlas.Width * 2.0);
				tcV	+=1.0 / (mLMAtlas.Height * 2.0);

				//move to atlas position
				tcU	+=offsetU;
				tcV	+=offsetV;

				tc.Add(new Vector2((float)tcU, (float)tcV));
			}
		}


		//unused attempt at using the texture vectors
		void GetTexCoords2(List<Vector3> verts, GFXTexInfo tex,
			out List<double> sCoords, out List<double> tCoords)
		{
			sCoords	=new List<double>();
			tCoords	=new List<double>();

			double	sX	=tex.mVecU.X;
			double	sY	=tex.mVecU.Y;
			double	sZ	=tex.mVecU.Z;
			double	tX	=tex.mVecV.X;
			double	tY	=tex.mVecV.Y;
			double	tZ	=tex.mVecV.Z;

			double	minS, minT;
			double	maxS, maxT;

			minS	=Bounds.MIN_MAX_BOUNDS;
			minT	=Bounds.MIN_MAX_BOUNDS;
			maxS	=-Bounds.MIN_MAX_BOUNDS;
			maxT	=-Bounds.MIN_MAX_BOUNDS;

			//calculate texture space extents
			foreach(Vector3 pnt in verts)
			{
				double	d	=(pnt.X * sX) + (pnt.Y * sY) + (pnt.Z * sZ);
				if(d < minS)
				{
					minS	=d;
				}
				if(d > maxS)
				{
					maxS	=d;
				}

				d	=(pnt.X * tX) + (pnt.Y * tY) + (pnt.Z * tZ);
				if(d < minT)
				{
					minT	=d;
				}
				if(d > maxT)
				{
					maxT	=d;
				}
			}

			//extent is the size of the surface in texels
			//note that these are texture texels not light
			double	extentS	=maxS - minS;
			double	extentT	=maxT - minT;

			//offset to the start of the texture
			double	shiftU	=-minS;
			double	shiftV	=-minT;

			foreach(Vector3 pnt in verts)
			{
				double	crdX, crdY;

				//dot product
				crdX	=(pnt.X * sX) + (pnt.Y * sY) + (pnt.Z * sZ);
				crdY	=(pnt.X * tX) + (pnt.Y * tY) + (pnt.Z * tZ);

				//shift relative to start position
				crdX	+=shiftU;
				crdY	+=shiftV;

				//now the coordinates are set for textures
				//scale by light grid size
				crdX	/=mLightGridSize;
				crdY	/=mLightGridSize;

				sCoords.Add(crdX);
				tCoords.Add(crdY);
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
			pln.mNormal	=Vector3.Cross(tex.mVecU, tex.mVecV);

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