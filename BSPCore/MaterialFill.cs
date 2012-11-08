using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework;


namespace BSPCore
{
	internal class MaterialFill
	{
		static void GetMirrorTexCoords(List<Vector3> verts,
			GFXTexInfo tex,	out List<Vector2> coords)
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
			GBSPPlane.TextureAxisFromPlane(pln, out xv, out yv);

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


		static float ClampLightIndex(int idx)
		{
			if(idx == 255)
			{
				return	255;	//not in use
			}
			else if(idx >= 32)	//switchable
			{
				return	idx - 20;
			}
			else if(idx < 12)
			{
				return	idx;
			}

			Debug.Assert(false);	//light style in a strange place

			return	0;
		}


		static Vector4 AssignLightStyleIndex(GFXFace f)
		{
			//switchable styles reference the same shader
			//array as animated, so need a - 20
			Vector4	ret	=Vector4.Zero;

			ret.X	=ClampLightIndex(f.mLType0);
			ret.Y	=ClampLightIndex(f.mLType1);
			ret.Z	=ClampLightIndex(f.mLType2);
			ret.W	=ClampLightIndex(f.mLType3);

			return	ret;
		}


		static void AddTexCoordsToList(MaterialLib.TexAtlas atlas,
			List<Vector2> tc, List<double> uList, List<double> vList, double offsetU, double offsetV)
		{
			for(int k=0;k < uList.Count;k++)
			{
				double	tcU	=uList[k];
				double	tcV	=vList[k];

				//scale to atlas space
				tcU	/=atlas.Width;
				tcV	/=atlas.Height;

				//step half a pixel in atlas space
				tcU	+=1.0 / (atlas.Width * 2.0);
				tcV	+=1.0 / (atlas.Height * 2.0);

				//move to atlas position
				tcU	+=offsetU;
				tcV	+=offsetV;

				tc.Add(new Vector2((float)tcU, (float)tcV));
			}
		}


		static void GetTexCoords1(List<Vector3> verts, GBSPPlane pln, int lightGridSize,
			int	lwidth, int lheight, GFXTexInfo tex,
			out List<double> sCoords, out List<double> tCoords)
		{
			sCoords	=new List<double>();
			tCoords	=new List<double>();

			//get a proper set of texvecs for lighting
			Vector3	xv, yv;
			GBSPPlane.TextureAxisFromPlane(pln, out xv, out yv);

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
				crdX	/=lightGridSize;
				crdY	/=lightGridSize;

				sCoords.Add(crdX);
				tCoords.Add(crdY);
			}
		}


		static bool AtlasAnimated(MaterialLib.TexAtlas atlas, int lightGridSize,
			DrawDataChunk ddc, GFXFace f, byte []lightData,
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

				if(!AtlasLightMap(atlas, lightGridSize, f, lightData, s, faceVerts, pln, tex, coordSet))
				{
					return	false;
				}
			}
			return	true;
		}


		static bool AtlasLightMap(MaterialLib.TexAtlas atlas, int lightGridSize,
			GFXFace f, byte []lightData, int styleIndex, List<Vector3> faceVerts,
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

			if(!atlas.Insert(lmap, f.mLWidth, f.mLHeight,
				out scaleU, out scaleV, out offsetU, out offsetV))
			{
				CoreEvents.Print("Lightmap atlas out of space, try increasing it's size.\n");
				return	false;
			}

			List<double>	coordsU	=new List<double>();
			List<double>	coordsV	=new List<double>();
			GetTexCoords1(faceVerts, sidedPlane, lightGridSize, f.mLWidth, f.mLHeight, tex, out coordsU, out coordsV);
			AddTexCoordsToList(atlas, texCoords, coordsU, coordsV, offsetU, offsetV);

			return	true;
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


		//handles basic verts and texcoord 0 with model matrix
		static void ComputeFaceData(GFXFace f, Vector3 []verts, int []indexes,
			GFXTexInfo tex,	List<Vector2> tex0, List<Vector3> outVerts)
		{
			List<Vector3>	worldVerts	=GetFaceVerts(f, verts, indexes);

			foreach(Vector3 v in worldVerts)
			{
				Vector2	crd;
				crd.X	=Vector3.Dot(tex.mVecU, v);
				crd.Y	=Vector3.Dot(tex.mVecV, v);

				crd.X	/=tex.mDrawScaleU;
				crd.Y	/=tex.mDrawScaleV;

				crd.X	+=tex.mShiftU;
				crd.Y	+=tex.mShiftV;

				tex0.Add(crd);

				outVerts.Add(v);
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

				if(UtilityLib.Misc.bFlagSet(tex.mFlags, TexInfo.MIRROR | TexInfo.TRANSPARENT))
				{
					col.W	=tex.mAlpha;
				}
				colors.Add(col);
			}
		}


		internal static bool FillLightMapped(DrawDataChunk ddc, GFXPlane []pp,
					Vector3 []verts, int []indexes, Vector3 []rgbVerts, Vector3 []vnorms,
					GFXFace f, GFXTexInfo tex, int lightGridSize,
					byte []lightData, MaterialLib.TexAtlas atlas,
					List<List<Vector3>> mirrorPolys)
		{
			ddc.mNumFaces++;
			ddc.mVCounts.Add(f.mNumVerts);

			//grab plane for dynamic lighting normals
			GFXPlane	pl	=pp[f.mPlaneNum];
			GBSPPlane	pln	=new GBSPPlane(pl);
			if(f.mbFlipSide)
			{
				pln.Inverse();
			}

			List<Vector3>	faceVerts	=new List<Vector3>();
			ComputeFaceData(f, verts, indexes, tex, ddc.mTex0, faceVerts);
			ComputeFaceNormals(f, verts, indexes, tex, null, pln, ddc.mNorms);

			if(!AtlasLightMap(atlas, lightGridSize, f, lightData, 0, faceVerts, pln, tex, ddc.mTex1))
			{
				return	false;
			}
			ddc.mVerts.AddRange(faceVerts);

			return	true;
		}


		internal static bool FillLightMapAnimated(DrawDataChunk ddc, GFXPlane []pp,
					Vector3 []verts, int []indexes, Vector3 []rgbVerts, Vector3 []vnorms,
					GFXFace f, GFXTexInfo tex, int lightGridSize,
					byte []lightData, MaterialLib.TexAtlas atlas,
					List<List<Vector3>> mirrorPolys)
		{
			ddc.mNumFaces++;
			ddc.mVCounts.Add(f.mNumVerts);

			GFXPlane	pl	=pp[f.mPlaneNum];
			GBSPPlane	pln	=new GBSPPlane(pl);
			if(f.mbFlipSide)
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

			if(!AtlasAnimated(atlas, lightGridSize, ddc, f, lightData, faceVerts, pln, tex))
			{
				CoreEvents.Print("Lightmap atlas out of space, try increasing it's size.\n");
				return	false;
			}

			ddc.mVerts.AddRange(faceVerts);

			//style index
			for(int k=0;k < f.mNumVerts;k++)
			{
				Vector4	styleIndex	=AssignLightStyleIndex(f);
				ddc.mStyles.Add(styleIndex);
			}

			return	true;
		}


		internal static bool FillVLit(DrawDataChunk ddc, GFXPlane []pp,
					Vector3 []verts, int []indexes, Vector3 []rgbVerts, Vector3 []vnorms,
					GFXFace f, GFXTexInfo tex, int lightGridSize,
					byte []lightData, MaterialLib.TexAtlas atlas,
					List<List<Vector3>> mirrorPolys)
		{
			ddc.mNumFaces++;
			ddc.mVCounts.Add(f.mNumVerts);

			//grab plane for dynamic lighting normals
			GFXPlane	pl	=pp[f.mPlaneNum];
			GBSPPlane	pln	=new GBSPPlane(pl);
			if(f.mbFlipSide)
			{
				pln.Inverse();
			}

			List<Vector3>	faceVerts	=new List<Vector3>();
			ComputeFaceData(f, verts, indexes, tex, ddc.mTex0, faceVerts);
			ComputeFaceNormals(f, verts, indexes, tex, vnorms, pln, ddc.mNorms);
			ComputeFaceColors(f, verts, indexes, tex, rgbVerts, ddc.mColors);

			ddc.mVerts.AddRange(faceVerts);

			return	true;
		}


		internal static bool FillFullBright(DrawDataChunk ddc, GFXPlane []pp,
					Vector3 []verts, int []indexes, Vector3 []rgbVerts, Vector3 []vnorms,
					GFXFace f, GFXTexInfo tex, int lightGridSize,
					byte []lightData, MaterialLib.TexAtlas atlas,
					List<List<Vector3>> mirrorPolys)
		{
			ddc.mNumFaces++;
			ddc.mVCounts.Add(f.mNumVerts);

			List<Vector3>	faceVerts	=new List<Vector3>();
			ComputeFaceData(f, verts, indexes, tex, ddc.mTex0, faceVerts);

			ddc.mVerts.AddRange(faceVerts);

			return	true;
		}


		internal static bool FillSky(DrawDataChunk ddc, GFXPlane []pp,
					Vector3 []verts, int []indexes, Vector3 []rgbVerts, Vector3 []vnorms,
					GFXFace f, GFXTexInfo tex, int lightGridSize,
					byte []lightData, MaterialLib.TexAtlas atlas,
					List<List<Vector3>> mirrorPolys)
		{
			ddc.mNumFaces++;
			ddc.mVCounts.Add(f.mNumVerts);

			List<Vector3>	faceVerts	=new List<Vector3>();
			ComputeFaceData(f, verts, indexes, tex, ddc.mTex0, faceVerts);

			ddc.mVerts.AddRange(faceVerts);

			return	true;
		}


		internal static bool FillMirror(DrawDataChunk ddc, GFXPlane []pp,
					Vector3 []verts, int []indexes, Vector3 []rgbVerts, Vector3 []vnorms,
					GFXFace f, GFXTexInfo tex, int lightGridSize,
					byte []lightData, MaterialLib.TexAtlas atlas,
					List<List<Vector3>> mirrorPolys)
		{
			ddc.mNumFaces++;
			ddc.mVCounts.Add(f.mNumVerts);

			GFXPlane	pl	=pp[f.mPlaneNum];
			GBSPPlane	pln	=new GBSPPlane(pl);
			if(f.mbFlipSide)
			{
				pln.Inverse();
			}

			List<Vector3>	fverts	=new List<Vector3>();
			ComputeFaceData(f, verts, indexes, tex, ddc.mTex0, fverts);
			ComputeFaceNormals(f, verts, indexes, tex, vnorms, pln, ddc.mNorms);
			ComputeFaceColors(f, verts, indexes, tex, rgbVerts, ddc.mColors);

			ddc.mVerts.AddRange(fverts);

			List<Vector2>	coords	=new List<Vector2>();
			GetMirrorTexCoords(fverts, tex, out coords);
			ddc.mTex1.AddRange(coords);

			mirrorPolys.Add(fverts);

			return	true;
		}


		internal static bool FillAlpha(DrawDataChunk ddc, GFXPlane []pp,
					Vector3 []verts, int []indexes, Vector3 []rgbVerts, Vector3 []vnorms,
					GFXFace f, GFXTexInfo tex, int lightGridSize,
					byte []lightData, MaterialLib.TexAtlas atlas,
					List<List<Vector3>> mirrorPolys)
		{
			ddc.mNumFaces++;
			ddc.mVCounts.Add(f.mNumVerts);

			GFXPlane	pl	=pp[f.mPlaneNum];
			GBSPPlane	pln	=new GBSPPlane(pl);
			if(f.mbFlipSide)
			{
				pln.Inverse();
			}

			List<Vector3>	faceVerts	=new List<Vector3>();
			ComputeFaceData(f, verts, indexes, tex, ddc.mTex0, faceVerts);
			ComputeFaceNormals(f, verts, indexes, tex, vnorms, pln, ddc.mNorms);
			ComputeFaceColors(f, verts, indexes, tex, rgbVerts, ddc.mColors);

			ddc.mVerts.AddRange(faceVerts);

			return	true;
		}


		internal static bool FillLightMappedAlpha(DrawDataChunk ddc, GFXPlane []pp,
					Vector3 []verts, int []indexes, Vector3 []rgbVerts, Vector3 []vnorms,
					GFXFace f, GFXTexInfo tex, int lightGridSize,
					byte []lightData, MaterialLib.TexAtlas atlas,
					List<List<Vector3>> mirrorPolys)
		{
			ddc.mNumFaces++;
			ddc.mVCounts.Add(f.mNumVerts);

			//grab plane for dynamic lighting normals
			GFXPlane	pl	=pp[f.mPlaneNum];
			GBSPPlane	pln	=new GBSPPlane(pl);
			if(f.mbFlipSide)
			{
				pln.Inverse();
			}

			List<Vector3>	faceVerts	=new List<Vector3>();
			ComputeFaceData(f, verts, indexes, tex, ddc.mTex0, faceVerts);
			ComputeFaceNormals(f, verts, indexes, tex, vnorms, pln, ddc.mNorms);

			foreach(Vector3 v in faceVerts)
			{
				ddc.mColors.Add(new Vector4(1, 1, 1, tex.mAlpha));
			}

			if(!AtlasLightMap(atlas, lightGridSize, f, lightData, 0, faceVerts, pln, tex, ddc.mTex1))
			{
				return	false;
			}

			ddc.mVerts.AddRange(faceVerts);

			return	true;
		}


		internal static bool FillLightMappedAlphaAnimated(DrawDataChunk ddc, GFXPlane []pp,
					Vector3 []verts, int []indexes, Vector3 []rgbVerts, Vector3 []vnorms,
					GFXFace f, GFXTexInfo tex, int lightGridSize,
					byte []lightData, MaterialLib.TexAtlas atlas,
					List<List<Vector3>> mirrorPolys)
		{
			ddc.mNumFaces++;
			ddc.mVCounts.Add(f.mNumVerts);

			GFXPlane	pl	=pp[f.mPlaneNum];
			GBSPPlane	pln	=new GBSPPlane(pl);
			if(f.mbFlipSide)
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

			if(!AtlasAnimated(atlas, lightGridSize, ddc, f, lightData, faceVerts, pln, tex))
			{
				CoreEvents.Print("Lightmap atlas out of space, try increasing it's size.\n");
				return	false;
			}
			ddc.mVerts.AddRange(faceVerts);

			//style index
			for(int k=0;k < f.mNumVerts;k++)
			{
				Vector4	styleIndex	=AssignLightStyleIndex(f);
				ddc.mStyles.Add(styleIndex);
			}

			return	true;
		}
	}
}
