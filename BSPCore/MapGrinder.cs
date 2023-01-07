using System;
using System.Text;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using Vortice.Mathematics;
using MaterialLib;
using MeshLib;
using UtilityLib;

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


		internal int Area()
		{
			double	total	=0.0;
			int		vertOfs	=0;
			for(int i=0;i < mNumFaces;i++)
			{
				int	nverts	=mVCounts[i];
				for(int j=2;j < nverts;j++)
				{
					Vector3	vect1	=mVerts[j + vertOfs - 1] - mVerts[vertOfs];
					Vector3	vect2	=mVerts[j + vertOfs] - mVerts[vertOfs];

					//not sure if this ordering is correct, but since
					//only the length is used, should be ok
					Vector3	cross	=Vector3.Cross(vect1, vect2);

					total	+=0.5f * cross.Length();
				}
				vertOfs	+=nverts;
			}

			//cap
			if(total > int.MaxValue)
			{
				return	int.MaxValue;
			}
			else
			{
				return	(int)total;
			}
		}
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

		//computed lightmap atlas
		TexAtlas	mLMAtlas;

		//passed in data
		int			mLightGridSize;
		GFXTexInfo	[]mTexInfos;
		GFXFace		[]mFaces;


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


		internal void GetLMGeometry(out int typeIndex, out Array verts, out UInt16 []inds)
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

			VPosNormTex04F	[]varray	=new VPosNormTex04F[mLMAVerts.Count];
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

			VPosNormTex04Tex14Tex24Color0F	[]varray
				=new VPosNormTex04Tex14Tex24Color0F[mLMAnimVerts.Count];
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
				varray[i].Color0		=mLMAnimStyle[i];
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

			VPosNormTex04Tex14Tex24Color0F	[]varray
				=new VPosNormTex04Tex14Tex24Color0F[mLMAAnimVerts.Count];
			for(int i=0;i < mLMAAnimVerts.Count;i++)
			{
				varray[i].Position		=mLMAAnimVerts[i];
				varray[i].Normal.X		=mLMAAnimNormals[i].X;
				varray[i].Normal.Y		=mLMAAnimNormals[i].Y;
				varray[i].Normal.Z		=mLMAAnimNormals[i].Z;
				varray[i].Normal.W		=1f;
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
				varray[i].TexCoord2.Z	=mLMAAnimColors[i].A;	//alpha
				varray[i].TexCoord2.W	=69.0f;	//nothin
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
				if(bLightMap)
				{
					//lightmap atlases need 32 bit texcoords
					mMatLib.SetMaterialPrecision32(matName, true);
				}

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