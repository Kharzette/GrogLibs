using System;
using System.Linq;
using System.Text;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using Vortice.Mathematics;


namespace BSPCore;

//this class mostly converts draw data into vertex
//buffer ready info of the right format
internal class MaterialFill
{
	static void GetMirrorTexCoords(List<Vector3> verts,
		TexInfo tex,	out List<Vector2> coords)
	{
		coords	=new List<Vector2>();

		float	minS, minT;
		float	maxS, maxT;

		minS	=Bounds.MIN_MAX_BOUNDS;
		minT	=Bounds.MIN_MAX_BOUNDS;
		maxS	=-Bounds.MIN_MAX_BOUNDS;
		maxT	=-Bounds.MIN_MAX_BOUNDS;

		GBSPPlane	pln;
		pln.mNormal	=Vector3.Cross(tex.mUVec, tex.mVVec);

		pln.mNormal	=Vector3.Normalize(pln.mNormal);
		pln.mDist	=0;
		pln.mType	=GBSPPlane.PLANE_ANY;

		//get a proper set of texvecs for lighting
		Vector3	xv, yv;
		GBSPPlane.TextureAxisFromPlaneGrog(pln.mNormal, out xv, out yv);

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


	static byte ClampLightIndex(int idx)
	{
		if(idx == 255)
		{
			return	255;	//not in use
		}
		else if(idx >= 32)	//switchable
		{
			return	(byte)(idx - 20);
		}
		else if(idx < 12)
		{
			return	(byte)idx;
		}

		Debug.Assert(false);	//light style in a strange place

		return	0;
	}


	static Color AssignLightStyleIndex(QFace f)
	{
		//switchable styles reference the same shader
		//array as animated, so need a - 20
		return	new Color(ClampLightIndex(f.mStyles.R),
			ClampLightIndex(f.mStyles.G),
			ClampLightIndex(f.mStyles.B),
			ClampLightIndex(f.mStyles.A));
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
		int	lwidth, int lheight, TexInfo tex,
		out List<double> sCoords, out List<double> tCoords)
	{
		sCoords	=new List<double>();
		tCoords	=new List<double>();

		LInfo	li	=new LInfo();
		FInfo	fi	=new FInfo();

		fi.SetPlane(pln);
		fi.CalcFaceLightInfo(li, verts, lightGridSize, tex);

		//offset to the start of the texture
		Int32	shiftU, shiftV;
		li.GetLMin(out shiftU, out shiftV);

		foreach(Vector3 pnt in verts)
		{
			double	crdX, crdY;

			//dot product
			crdX	=Vector3.Dot(pnt, tex.mUVec);
			crdY	=Vector3.Dot(pnt, tex.mVVec);

			//scale by light grid size
			crdX	/=lightGridSize;
			crdY	/=lightGridSize;

			//shift relative to start position
			crdX	-=shiftU;
			crdY	-=shiftV;

			sCoords.Add(crdX);
			tCoords.Add(crdY);
		}
	}


	static bool AtlasAnimated(MaterialLib.TexAtlas atlas, int lightGridSize,
		DrawDataChunk ddc, QFace f, byte []lightData,
		List<Vector3> faceVerts, GBSPPlane pln, TexInfo tex)
	{
		for(int s=0;s < 4;s++)
		{
			List<Vector2>	coordSet	=null;
			bool			bTuFittyFi	=false;

			if(s == 0)
			{
				if(f.mStyles.R == 255)
				{
					bTuFittyFi	=true;
				}
				coordSet	=ddc.mTex1;
			}
			else if(s == 1)
			{
				if(f.mStyles.G == 255)
				{
					bTuFittyFi	=true;
				}
				coordSet	=ddc.mTex2;
			}
			else if(s == 2)
			{
				if(f.mStyles.B == 255)
				{
					bTuFittyFi	=true;
				}
				coordSet	=ddc.mTex3;
			}
			else if(s == 3)
			{
				if(f.mStyles.A == 255)
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


	internal static bool AtlasLightMap(MaterialLib.TexAtlas atlas, int lightGridSize,
		QFace f, byte []lightData, int styleIndex, List<Vector3> faceVerts,
		GBSPPlane sidedPlane, TexInfo tex, List<Vector2> texCoords)
	{
		double	scaleU, scaleV, offsetU, offsetV;
		scaleU	=scaleV	=offsetU	=offsetV	=0.0;

		//calc light stuff
		FInfo	fi	=new FInfo();
		LInfo	li	=new LInfo();

		fi.SetPlane(sidedPlane);

		fi.CalcFaceLightInfo(li, faceVerts, lightGridSize, tex);

		int	lHeight	=li.GetLHeight();
		int	lWidth	=li.GetLWidth();

		Color	[]lmap	=new Color[lHeight * lWidth];

		int	sizeOffset	=lHeight * lWidth * 3;

		sizeOffset	*=styleIndex;

		for(int i=0;i < lmap.Length;i++)
		{
			lmap[i]	=new Color(lightData[sizeOffset + f.mLightOfs + (i * 3)],
				lightData[sizeOffset + f.mLightOfs + (i * 3) + 1],
				lightData[sizeOffset + f.mLightOfs + (i * 3) + 2],
				(byte)0xFF);
		}

		if(!atlas.Insert(lmap, lWidth, lHeight,
			out scaleU, out scaleV, out offsetU, out offsetV))
		{
			CoreEvents.Print("Lightmap atlas out of space, try increasing it's size.\n");
			return	false;
		}

		List<double>	coordsU	=new List<double>();
		List<double>	coordsV	=new List<double>();
		GetTexCoords1(faceVerts, sidedPlane, lightGridSize, lWidth, lHeight, tex, out coordsU, out coordsV);
		AddTexCoordsToList(atlas, texCoords, coordsU, coordsV, offsetU, offsetV);

		return	true;
	}
/*



	//sided plane should be pre flipped if side != 0
	static void ComputeFaceNormals(QFace f, Vector3 []verts, int []indexes,
		TexInfo tex, Vector3 []vnorms, GBSPPlane sidedPlane,
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


	static void ComputeFaceColors(QFace f, Vector3 []verts, int []indexes,
		TexInfo tex, Vector3 []rgbVerts,	List<Color> colors)
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
			colors.Add(new Color(col));
		}
	}


	internal static bool FillLightMapped(DrawDataChunk ddc, GBSPPlane plane,
				QFace f, TexInfo tex, int lightGridSize, byte []lightData,
				MaterialLib.TexAtlas atlas)
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
			CoreEvents.Print("Lightmap atlas out of space, try increasing it's size.\n");
			return	false;
		}
		ddc.mVerts.AddRange(faceVerts);

		return	true;
	}


	internal static bool FillLightMapAnimated(DrawDataChunk ddc, GFXPlane []pp,
				Vector3 []verts, int []indexes, QFace f,
				TexInfo tex, int lightGridSize, byte []lightData,
				MaterialLib.TexAtlas atlas)
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
			ddc.mColors.Add(new Color(1f, 1f, 1f, tex.mAlpha));
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
			Color	styleIndex	=AssignLightStyleIndex(f);
			ddc.mStyles.Add(styleIndex);
		}

		return	true;
	}


	internal static bool FillFullBright(DrawDataChunk ddc, GFXPlane []pp,
				Vector3 []verts, int []indexes, Vector3 []rgbVerts,
				Vector3 []vnorms, QFace f, TexInfo tex)
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

		ddc.mVerts.AddRange(faceVerts);

		return	true;
	}


	internal static bool FillSky(DrawDataChunk ddc, GFXPlane []pp,
				Vector3 []verts, int []indexes,
				QFace f, TexInfo tex)
	{
		ddc.mNumFaces++;
		ddc.mVCounts.Add(f.mNumVerts);

		List<Vector3>	faceVerts	=new List<Vector3>();
		ComputeFaceData(f, verts, indexes, tex, ddc.mTex0, faceVerts);

		ddc.mVerts.AddRange(faceVerts);

		return	true;
	}


	internal static bool FillAlpha(DrawDataChunk ddc, GFXPlane []pp,
				Vector3 []verts, int []indexes, Vector3 []rgbVerts,
				Vector3 []vnorms, QFace f, TexInfo tex)
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
				Vector3 []verts, int []indexes, Vector3 []rgbVerts, QFace f, TexInfo tex,
				int lightGridSize, byte []lightData, MaterialLib.TexAtlas atlas)
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

		foreach(Vector3 v in faceVerts)
		{
			ddc.mColors.Add(new Color(1f, 1f, 1f, tex.mAlpha));
		}

		if(!AtlasLightMap(atlas, lightGridSize, f, lightData, 0, faceVerts, pln, tex, ddc.mTex1))
		{
			CoreEvents.Print("Lightmap atlas out of space, try increasing it's size.\n");
			return	false;
		}

		ddc.mVerts.AddRange(faceVerts);

		return	true;
	}


	internal static bool FillLightMappedAlphaAnimated(DrawDataChunk ddc, GFXPlane []pp,
				Vector3 []verts, int []indexes, Vector3 []rgbVerts,
				QFace f, TexInfo tex, int lightGridSize,
				byte []lightData, MaterialLib.TexAtlas atlas)
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
			ddc.mColors.Add(new Color(1f, 1f, 1f, tex.mAlpha));
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
			Color	styleIndex	=AssignLightStyleIndex(f);
			ddc.mStyles.Add(styleIndex);
		}

		return	true;
	}*/
}