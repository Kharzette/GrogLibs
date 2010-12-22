using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MaterialLib;


namespace BSPLib
{
	//grind up a map into gpu friendly data
	public class MapGrinder
	{
		GraphicsDevice	mGD;

		//computed lightmapped geometry
		List<Vector3>	mLMVerts		=new List<Vector3>();
		List<Vector2>	mLMFaceTex0		=new List<Vector2>();
		List<Vector2>	mLMFaceTex1		=new List<Vector2>();
		List<Int32>		mLMIndexes		=new List<Int32>();

		//computed non lightmapped geometry
		List<Vector3>	mNonLMVerts		=new List<Vector3>();
		List<Vector2>	mNonLMFaceTex0	=new List<Vector2>();
		List<Int32>		mNonLMIndexes	=new List<Int32>();

		//animated lightmap geometry
		List<Vector3>	mLMAnimVerts	=new List<Vector3>();
		List<Vector2>	mLMAnimFaceTex0	=new List<Vector2>();
		List<Vector2>	mLMAnimFaceTex1	=new List<Vector2>();
		List<Vector2>	mLMAnimFaceTex2	=new List<Vector2>();
		List<Vector2>	mLMAnimFaceTex3	=new List<Vector2>();
		List<Vector2>	mLMAnimFaceTex4	=new List<Vector2>();
		List<Int32>		mLMAnimIndexes	=new List<Int32>();
		List<Vector4>	mLMAnimStyle	=new List<Vector4>();

		//computed material stuff
		List<string>	mMaterialNames		=new List<string>();
		List<Material>	mMaterials			=new List<Material>();

		//lightmap material stuff
		List<Int32>		mLMMaterialOffsets	=new List<Int32>();
		List<Int32>		mLMMaterialNumVerts	=new List<Int32>();
		List<Int32>		mLMMaterialNumTris	=new List<Int32>();

		//non lightmap material stuff
		List<Int32>		mNonLMMaterialOffsets	=new List<Int32>();
		List<Int32>		mNonLMMaterialNumVerts	=new List<Int32>();
		List<Int32>		mNonLMMaterialNumTris	=new List<Int32>();

		//animated lightmap material stuff
		List<Int32>		mLMAnimMaterialOffsets	=new List<Int32>();
		List<Int32>		mLMAnimMaterialNumVerts	=new List<Int32>();
		List<Int32>		mLMAnimMaterialNumTris	=new List<Int32>();

		//computed lightmap atlas
		TexAtlas	mLMAtlas;

		//passed in data
		int			mLightGridSize;
		GFXTexInfo	[]mTexInfos;
		GFXFace		[]mFaces;


		public MapGrinder(GraphicsDevice gd, GFXTexInfo []texs,
			GFXFace []faces, int lightGridSize)
		{
			mGD				=gd;
			mTexInfos		=texs;
			mLightGridSize	=lightGridSize;
			mFaces			=faces;

			if(gd != null)
			{
				mLMAtlas	=new TexAtlas(gd);
			}

			CalcMaterialNames();
			CalcMaterials();
		}


		internal Int32 GetNumLMVerts()
		{
			return	mLMVerts.Count;
		}


		internal Int32 GetNumLMTris()
		{
			return	mLMIndexes.Count / 3;
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


		internal void GetNonLMMaterialData(out Int32 []matOffsets,	out Int32 []matNumVerts,
											out Int32 []matNumTris)
		{
			matOffsets	=mNonLMMaterialOffsets.ToArray();
			matNumVerts	=mNonLMMaterialNumVerts.ToArray();
			matNumTris	=mNonLMMaterialNumTris.ToArray();
		}


		internal void GetLMAnimMaterialData(out Int32 []matOffsets,	out Int32 []matNumVerts,
											out Int32 []matNumTris)
		{
			matOffsets	=mLMAnimMaterialOffsets.ToArray();
			matNumVerts	=mLMAnimMaterialNumVerts.ToArray();
			matNumTris	=mLMAnimMaterialNumTris.ToArray();
		}


		internal void GetLMBuffers(out VertexBuffer vb, out IndexBuffer ib)
		{
			VPosTex0Tex1	[]varray	=new VPosTex0Tex1[mLMVerts.Count];
			for(int i=0;i < mLMVerts.Count;i++)
			{
				varray[i].Position	=mLMVerts[i];
				varray[i].TexCoord0	=mLMFaceTex0[i];
				varray[i].TexCoord1	=mLMFaceTex1[i];
			}

			vb	=new VertexBuffer(mGD, 28 * varray.Length, BufferUsage.WriteOnly);
			vb.SetData<VPosTex0Tex1>(varray);

			ib	=new IndexBuffer(mGD, 4 * mLMIndexes.Count, BufferUsage.WriteOnly,
					IndexElementSize.ThirtyTwoBits);
			ib.SetData<Int32>(mLMIndexes.ToArray());
		}


		internal void GetNonLMBuffers(out VertexBuffer vb, out IndexBuffer ib)
		{
			VPosTex0	[]varray	=new VPosTex0[mNonLMVerts.Count];
			for(int i=0;i < mNonLMVerts.Count;i++)
			{
				varray[i].Position	=mNonLMVerts[i];
				varray[i].TexCoord0	=mNonLMFaceTex0[i];
			}

			vb	=new VertexBuffer(mGD, 20 * varray.Length, BufferUsage.WriteOnly);
			vb.SetData<VPosTex0>(varray);

			ib	=new IndexBuffer(mGD, 4 * mNonLMIndexes.Count, BufferUsage.WriteOnly,
					IndexElementSize.ThirtyTwoBits);
			ib.SetData<Int32>(mNonLMIndexes.ToArray());
		}


		internal void GetLMAnimBuffers(out VertexBuffer vb, out IndexBuffer ib)
		{
			if(mLMAnimVerts.Count == 0)
			{
				vb	=null;
				ib	=null;
				return;
			}

			VPosTex0Tex1Tex2Tex3Tex4Style4	[]varray	=new VPosTex0Tex1Tex2Tex3Tex4Style4[mLMAnimVerts.Count];
			for(int i=0;i < mLMAnimVerts.Count;i++)
			{
				varray[i].Position		=mLMAnimVerts[i];
				varray[i].TexCoord0		=mLMAnimFaceTex0[i];
				varray[i].TexCoord1		=mLMAnimFaceTex1[i];
				varray[i].TexCoord2		=mLMAnimFaceTex2[i];
				varray[i].TexCoord3		=mLMAnimFaceTex3[i];
				varray[i].TexCoord4		=mLMAnimFaceTex4[i];
				varray[i].StyleIndex	=mLMAnimStyle[i];
			}

			vb	=new VertexBuffer(mGD, 68 * varray.Length, BufferUsage.WriteOnly);
			vb.SetData<VPosTex0Tex1Tex2Tex3Tex4Style4>(varray);

			ib	=new IndexBuffer(mGD, 4 * mLMAnimIndexes.Count, BufferUsage.WriteOnly,
					IndexElementSize.ThirtyTwoBits);
			ib.SetData<Int32>(mLMAnimIndexes.ToArray());
		}


		internal void BuildLMAnimFaceData(Vector3 []verts, int[] indexes, byte []lightData)
		{
			List<Int32>	firstVert	=new List<Int32>();
			List<Int32>	numVert		=new List<Int32>();
			List<Int32>	numFace		=new List<Int32>();

			Map.Print("Handling animated lightmaps...\n");

			foreach(Material mat in mMaterials)
			{
				int	numFaceVerts	=mLMAnimVerts.Count;
				int	numFaces		=0;

				if(!mat.Name.EndsWith("Anim"))
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
					if(f.mLTypes[1] == 255)
					{
						continue;	//only interested in animated
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
					mLMAtlas.Insert(lmap, f.mLWidth, f.mLHeight,
						out scaleU, out scaleV, out offsetU, out offsetV);

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
						mLMAtlas.Insert(lmap, f.mLWidth, f.mLHeight,
							out scaleU, out scaleV, out offsetU, out offsetV);

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
							tc.X	/=TexAtlas.TEXATLAS_WIDTH;
							tc.Y	/=TexAtlas.TEXATLAS_HEIGHT;

							//step half a pixel in atlas space
							tc.X	+=1.0f / (TexAtlas.TEXATLAS_WIDTH * 2.0f);
							tc.Y	+=1.0f / (TexAtlas.TEXATLAS_HEIGHT * 2.0f);

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
						styleIndex.X	=f.mLTypes[1];
						styleIndex.Y	=f.mLTypes[2];
						styleIndex.Z	=f.mLTypes[3];
						mLMAnimStyle.Add(styleIndex);
					}
				}

				numFace.Add(numFaces);
				mLMAnimMaterialNumVerts.Add(mLMAnimVerts.Count - numFaceVerts);
			}

			//might not be any
			if(mLMAnimVerts.Count == 0)
			{
				return;
			}

			mLMAtlas.Finish();

			int	faceOfs	=0;
			for(int j=0;j < mMaterialNames.Count;j++)
			{
				int	cnt	=mLMAnimIndexes.Count;

				mLMAnimMaterialOffsets.Add(cnt);

				for(int i=faceOfs;i < (numFace[j] + faceOfs);i++)
				{
					int		nverts	=numVert[i];
					int		fvert	=firstVert[i];
					int		k		=0;

					//triangulate
					for(k=1;k < nverts-1;k++)
					{
						mLMAnimIndexes.Add(fvert);
						mLMAnimIndexes.Add(fvert + k);
						mLMAnimIndexes.Add(fvert + ((k + 1) % nverts));
					}
				}

				faceOfs	+=numFace[j];

				int	numTris	=(mLMAnimIndexes.Count - cnt);

				numTris	/=3;

				mLMAnimMaterialNumTris.Add(numTris);
			}
		}


		internal void BuildLMFaceData(Vector3 []verts, int[] indexes, byte []lightData)
		{
			List<Int32>	firstVert	=new List<Int32>();
			List<Int32>	numVert		=new List<Int32>();
			List<Int32>	numFace		=new List<Int32>();

			Map.Print("Atlasing " + lightData.Length + " bytes of light data...");

			foreach(Material mat in mMaterials)
			{
				int	numFaceVerts	=mLMVerts.Count;
				int	numFaces		=0;

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
					mLMAtlas.Insert(lmap, f.mLWidth, f.mLHeight,
						out scaleU, out scaleV, out offsetU, out offsetV);

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

						mLMFaceTex0.Add(crd);
						fverts.Add(pnt);

						mLMVerts.Add(pnt);
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

						mLMFaceTex1.Add(tc);
					}

					firstVert.Add(mLMVerts.Count - f.mNumVerts);
					numVert.Add(f.mNumVerts);
				}

				numFace.Add(numFaces);
				mLMMaterialNumVerts.Add(mLMVerts.Count - numFaceVerts);
			}

			mLMAtlas.Finish();

			int	faceOfs	=0;
			for(int j=0;j < mMaterialNames.Count;j++)
			{
				int	cnt	=mLMIndexes.Count;

				mLMMaterialOffsets.Add(cnt);

				for(int i=faceOfs;i < (numFace[j] + faceOfs);i++)
				{
					int		nverts	=numVert[i];
					int		fvert	=firstVert[i];
					int		k		=0;

					//triangulate
					for(k=1;k < nverts-1;k++)
					{
						mLMIndexes.Add(fvert);
						mLMIndexes.Add(fvert + k);
						mLMIndexes.Add(fvert + ((k + 1) % nverts));
					}
				}

				faceOfs	+=numFace[j];

				int	numTris	=(mLMIndexes.Count - cnt);

				numTris	/=3;

				mLMMaterialNumTris.Add(numTris);
			}
		}


		internal void BuildNonLMFaceData(Vector3 []verts, int[] indexes)
		{
			List<Int32>	firstVert	=new List<Int32>();
			List<Int32>	numVert		=new List<Int32>();
			List<Int32>	numFace		=new List<Int32>();

			Map.Print("Building non light mapped face data...");

			foreach(Material mat in mMaterials)
			{
				int	numFaceVerts	=mNonLMVerts.Count;
				int	numFaces		=0;

				if(!mat.Name.EndsWith("NonLM"))
				{
					numFace.Add(numFaces);
					mNonLMMaterialNumVerts.Add(mNonLMVerts.Count - numFaceVerts);
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

						mNonLMFaceTex0.Add(crd);
						fverts.Add(pnt);

						mNonLMVerts.Add(pnt);
					}
					firstVert.Add(mNonLMVerts.Count - f.mNumVerts);
					numVert.Add(f.mNumVerts);
				}
				numFace.Add(numFaces);
				mNonLMMaterialNumVerts.Add(mNonLMVerts.Count - numFaceVerts);
			}

			int	faceOfs	=0;
			for(int j=0;j < mMaterialNames.Count;j++)
			{
				int	cnt	=mNonLMIndexes.Count;

				mNonLMMaterialOffsets.Add(cnt);

				for(int i=faceOfs;i < (numFace[j] + faceOfs);i++)
				{
					int		nverts	=numVert[i];
					int		fvert	=firstVert[i];
					int		k		=0;

					//triangulate
					for(k=1;k < nverts-1;k++)
					{
						mNonLMIndexes.Add(fvert);
						mNonLMIndexes.Add(fvert + k);
						mNonLMIndexes.Add(fvert + ((k + 1) % nverts));
					}
				}

				faceOfs	+=numFace[j];

				int	numTris	=(mNonLMIndexes.Count - cnt);

				numTris	/=3;

				mNonLMMaterialNumTris.Add(numTris);
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


		internal List<string> GetMaterialNames()
		{
			return	mMaterialNames;
		}


		void CalcMaterials()
		{
			//build material list
			foreach(string matName in mMaterialNames)
			{
				MaterialLib.Material	mat	=new MaterialLib.Material();
				mat.Name			=matName;
				mat.ShaderName		="";
				mat.Technique		="";
				mat.BlendFunction	=BlendFunction.Add;
				mat.SourceBlend		=Blend.SourceAlpha;
				mat.DestBlend		=Blend.InverseSourceAlpha;
				mat.DepthWrite		=true;
				mat.CullMode		=CullMode.CullCounterClockwiseFace;
				mat.ZFunction		=CompareFunction.Less;

				mMaterials.Add(mat);
			}
		}


		void CalcMaterialNames()
		{
			mMaterialNames.Clear();

			foreach(GFXFace f in mFaces)
			{
				GFXTexInfo	tex	=mTexInfos[f.mTexInfo];

				if(f.mLightOfs == -1)
				{
					if(!mMaterialNames.Contains(tex.mMaterial + "NonLM"))
					{
						mMaterialNames.Add(tex.mMaterial + "NonLM");
					}
				}

				int	numStyles	=0;
				for(int i=0;i < 4;i++)
				{
					if(f.mLTypes[i] != 255)
					{
						numStyles++;
					}
				}

				if(numStyles == 1)
				{
					//standard static light
					if(!mMaterialNames.Contains(tex.mMaterial))
					{
						mMaterialNames.Add(tex.mMaterial);
					}
				}
				else if(numStyles > 1)
				{
					//animated lights
					if(!mMaterialNames.Contains(tex.mMaterial + "Anim"))
					{
						mMaterialNames.Add(tex.mMaterial + "Anim");
					}
				}
			}
		}


		internal TexAtlas GetLightMapAtlas()
		{
			return	mLMAtlas;
		}
	}
}