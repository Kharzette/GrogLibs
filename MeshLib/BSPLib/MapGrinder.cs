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

		//lightmap atlas
		TexAtlas	mLMAtlas;


		public MapGrinder(GraphicsDevice gd, List<string> matNames)
		{
			mGD				=gd;
			mMaterialNames	=matNames;

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
				varray[i].TexCoord1	=mFaceTex0[i];	//duping texcoord0!
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

			foreach(string mat in mMaterialNames)
			{
				int	numFaceVerts	=mFaceVerts.Count;
				int	numFaces		=0;

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

					List<Vector2>	coords	=new List<Vector2>();

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

						coords.Add(crd);

						mFaceVerts.Add(pnt);
					}

					Bounds	bnd	=new Bounds();
					foreach(Vector2 crd in coords)
					{
						bnd.AddPointToBounds(crd);
					}

					for(k=0;k < nverts;k++)
					{
						int	idx	=indexes[fvert + k];

						Vector2	tc	=Vector2.Zero;
						tc.X	=coords[k].X;// - bnd.mMins.X;
						tc.Y	=coords[k].Y;// - bnd.mMins.Y;
						mFaceTex0.Add(tc);

						//lightmap coords
						tc.X	-=bnd.mMins.X;
						tc.Y	-=bnd.mMins.Y;
//						tc		/=16;
//						tc.X	/=f.mLWidth;
//						tc.Y	/=f.mLHeight;
						tc.X	+=(float)offsetU;
						tc.Y	+=(float)offsetV;
						tc.X	*=(float)scaleU;
						tc.Y	*=(float)scaleV;
//						tc.X	-=bnd.mMins.X / 16;
//						tc.Y	-=bnd.mMins.Y / 16;

						//tex1 here for now
						mFaceTex1.Add(tc);
					}
					firstVert.Add(mFaceVerts.Count - f.mNumVerts);
					numVert.Add(f.mNumVerts);
				}

				numFace.Add(numFaces);
				mMaterialNumVerts.Add(mFaceVerts.Count - numFaceVerts);
			}

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


		internal TexAtlas GetLightMapAtlas()
		{
			return	mLMAtlas;
		}
	}
}
