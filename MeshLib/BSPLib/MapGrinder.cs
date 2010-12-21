using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace BSPLib
{
	//grind up a map into gpu friendly data
	public class MapGrinder
	{
		GraphicsDevice	mGD;

		//geometry
		List<Vector3>	mFaceVerts	=new List<Vector3>();
		List<Vector2>	mFaceTex0	=new List<Vector2>();
		List<Vector2>	mFaceTex1	=new List<Vector2>();
		List<Int32>		mIndexes	=new List<Int32>();

		//material stuff
		List<string>	mMaterialNames		=new List<string>();
		List<Int32>		mMaterialOffsets	=new List<Int32>();
		List<Int32>		mMaterialNumVerts	=new List<Int32>();
		List<Int32>		mMaterialNumTris	=new List<Int32>();

		//light density
		int	mLightGridSize;

		//lightmap atlas
		TexAtlas	mLMAtlas;


		public MapGrinder(GraphicsDevice gd, List<string> matNames, int lightGridSize)
		{
			mGD				=gd;
			mMaterialNames	=matNames;
			mLightGridSize	=lightGridSize;

			mLMAtlas	=new TexAtlas(gd);
		}


		internal Int32 GetNumVerts()
		{
			return	mFaceVerts.Count;
		}


		internal Int32 GetNumTris()
		{
			return	mIndexes.Count / 3;
		}


		internal void GetMaterialData(out Int32 []matOffsets,
			out Int32 []matNumVerts, out Int32 []matNumTris)
		{
			matOffsets	=mMaterialOffsets.ToArray();
			matNumVerts	=mMaterialNumVerts.ToArray();
			matNumTris	=mMaterialNumTris.ToArray();
		}



		internal void GetBuffers(out VertexBuffer vb, out IndexBuffer ib)
		{
			VPosTex0Tex1	[]varray	=new VPosTex0Tex1[mFaceVerts.Count];
			for(int i=0;i < mFaceVerts.Count;i++)
			{
				varray[i].Position	=mFaceVerts[i];
				varray[i].TexCoord0	=mFaceTex0[i];
				varray[i].TexCoord1	=mFaceTex1[i];
			}

			vb	=new VertexBuffer(mGD, 28 * varray.Length, BufferUsage.WriteOnly);
			vb.SetData<VPosTex0Tex1>(varray);

			ib	=new IndexBuffer(mGD, 4 * mIndexes.Count, BufferUsage.WriteOnly,
					IndexElementSize.ThirtyTwoBits);
			ib.SetData<Int32>(mIndexes.ToArray());
		}


		internal void BuildFaceData(Vector3 []verts, int[] indexes,
			GFXTexInfo []texInfos, GFXFace []faces, byte []lightData)
		{
			List<Int32>	firstVert	=new List<Int32>();
			List<Int32>	numVert		=new List<Int32>();
			List<Int32>	numFace		=new List<Int32>();

			Map.Print("Atlasing " + lightData.Length + " bytes of light data...");

			foreach(string mat in mMaterialNames)
			{
				int	numFaceVerts	=mFaceVerts.Count;
				int	numFaces		=0;

				Map.Print("Light for material: " + mat + ".\n");

				foreach(GFXFace f in faces)
				{
					GFXTexInfo	tex	=texInfos[f.mTexInfo];

					if(tex.mMaterial != mat)
					{
						continue;
					}

					numFaces++;

					//grab lightmap
					double	scaleU, scaleV, offsetU, offsetV;
					scaleU	=scaleV	=offsetU	=offsetV	=0.0;
					if(f.mLightOfs != -1)
					{
						Color	[]lmap	=new Color[f.mLHeight * f.mLWidth];

						for(int i=0;i < lmap.Length;i++)
						{
							lmap[i].R	=lightData[f.mLightOfs + (i * 3)];
							lmap[i].G	=lightData[f.mLightOfs + (i * 3) + 1];
							lmap[i].B	=lightData[f.mLightOfs + (i * 3) + 2];
							lmap[i].A	=0xFF;
						}
						mLMAtlas.Insert(lmap, f.mLWidth, f.mLHeight,
							out scaleU, out scaleV, out offsetU, out offsetV);
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

						mFaceTex0.Add(crd);
						fverts.Add(pnt);

						mFaceVerts.Add(pnt);
					}

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
						tc.X	/=TexAtlas.TEXATLAS_WIDTH;
						tc.Y	/=TexAtlas.TEXATLAS_HEIGHT;

						//step half a pixel in atlas space
						tc.X	+=1.0f / (TexAtlas.TEXATLAS_WIDTH * 2.0f);
						tc.Y	+=1.0f / (TexAtlas.TEXATLAS_HEIGHT * 2.0f);

						//move to atlas position
						tc.X	+=(float)offsetU;
						tc.Y	+=(float)offsetV;

						mFaceTex1.Add(tc);
					}
					firstVert.Add(mFaceVerts.Count - f.mNumVerts);
					numVert.Add(f.mNumVerts);
				}

				numFace.Add(numFaces);
				mMaterialNumVerts.Add(mFaceVerts.Count - numFaceVerts);
			}

			mLMAtlas.Finish();

			int	faceOfs	=0;
			for(int j=0;j < mMaterialNames.Count;j++)
			{
				int	cnt	=mIndexes.Count;

				mMaterialOffsets.Add(cnt);

				for(int i=faceOfs;i < (numFace[j] + faceOfs);i++)
				{
					int		nverts	=numVert[i];
					int		fvert	=firstVert[i];
					int		k		=0;

					//triangulate
					for(k=1;k < nverts-1;k++)
					{
						mIndexes.Add(fvert);
						mIndexes.Add(fvert + k);
						mIndexes.Add(fvert + ((k + 1) % nverts));
					}
				}

				faceOfs	+=numFace[j];

				int	numTris	=(mIndexes.Count - cnt);

				numTris	/=3;

				mMaterialNumTris.Add(numTris);
			}
		}


		private void GetTexCoords1(List<Vector3> verts,
			int	lwidth, int lheight,
			GFXTexInfo tex, out List<Vector2> coords)
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

			//in light space at this point
			//no idea why I need this 1.5
			//the math makes no sense, should be
			//only 0.5
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


		internal TexAtlas GetLightMapAtlas()
		{
			return	mLMAtlas;
		}
	}
}