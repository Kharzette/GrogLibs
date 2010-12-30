using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace MeshLib
{
	//draw bits of indoor scenery
	public class IndoorMesh
	{
		//GPU stuff
		VertexBuffer		mLMVB, mVLitVB, mLMAnimVB, mAlphaVB, mSkyVB;
		VertexBuffer		mFBVB, mMirrorVB, mLMAVB, mLMAAnimVB;
		VertexDeclaration	mLMVD, mVLitVD, mLMAnimVD, mAlphaVD, mSkyVD;
		VertexDeclaration	mFBVD, mMirrorVD, mLMAVD, mLMAAnimVD;
		IndexBuffer			mLMIB, mVLitIB, mLMAnimIB, mAlphaIB;
		IndexBuffer			mSkyIB, mFBIB, mMirrorIB, mLMAIB, mLMAAnimIB;
		RenderTarget2D		mMirrorRenderTarget;
		Texture2D			mMirrorTexture;

		//material library reference
		MaterialLib.MaterialLib	mMatLib;

		//light map atlas
		UtilityLib.TexAtlas	mLightMapAtlas;

		//lightmap animation stuff
		Dictionary<int, string>	mStyles			=new Dictionary<int, string>();
		Dictionary<int, float>	mCurStylePos	=new Dictionary<int, float>();

		//material draw stuff
		//offsets into the vbuffer per material
		Int32[]	mLMMatOffsets, mVLitMatOffsets, mLMAnimMatOffsets;
		Int32[]	mAlphaMatOffsets, mSkyMatOffsets, mFBMatOffsets, mMirrorMatOffsets;
		Int32[]	mLMAMatOffsets, mLMAAnimMatOffsets;

		//numverts for drawprim call per material
		Int32[]	mLMMatNumVerts, mVLitMatNumVerts, mLMAnimMatNumVerts;
		Int32[] mAlphaMatNumVerts, mSkyMatNumVerts, mFBMatNumVerts, mMirrorMatNumVerts;
		Int32[]	mLMAMatNumVerts, mLMAAnimMatNumVerts;

		//primcount per material
		Int32[]	mLMMatNumTris, mVLitMatNumTris, mLMAnimMatNumTris;
		Int32[]	mAlphaMatNumTris, mSkyMatNumTris, mFBMatNumTris, mMirrorMatNumTris;
		Int32[]	mLMAMatNumTris, mLMAAnimMatNumTris;

		//sort points for alphas
		Vector3[] mLMASortPoints, mAlphaSortPoints, mMirrorSortPoints, mLMAAnimSortPoints;

		//mirror polys for rendering through
		List<List<Vector3>>	mMirrorPolys	=new List<List<Vector3>>();

		//for sorting alphas
		MaterialLib.AlphaPool	mAlphaPool	=new MaterialLib.AlphaPool();

		//constants
		const float		ThirtyFPS	=(1000.0f / 30.0f);

		//delegates
		#region Delegates
		public delegate bool IsMaterialVisible(Vector3 eyePos, int matIdx);

		//tool side delegates for building the indoor mesh
		//from raw parts
		public delegate bool BuildLMRenderData(GraphicsDevice g,
			//lightmap stuff
			out VertexBuffer lmVB,
			out IndexBuffer lmIB,
			out VertexDeclaration lmVD,
			out Int32 []matOffsets,
			out Int32 []matNumVerts,
			out Int32 []matNumTris,

			//animated lightmap stuff
			out VertexBuffer lmAnimVB,
			out IndexBuffer lmAnimIB,
			out VertexDeclaration lmAnimVD,
			out Int32 []matAnimOffsets,
			out Int32 []matAnimNumVerts,
			out Int32 []matAnimNumTris,

			//lightmapped alpha stuff
			out VertexBuffer lmaVB,
			out IndexBuffer lmaIB,
			out VertexDeclaration lmaVD,
			out Int32 []amatOffsets,
			out Int32 []amatNumVerts,
			out Int32 []amatNumTris,
			out Vector3 []amatSortPoints,

			//animated alpha lightmap stuff
			out VertexBuffer lmaAnimVB,
			out IndexBuffer lmaAnimIB,
			out VertexDeclaration lmaAnimVD,
			out Int32 []amatAnimOffsets,
			out Int32 []amatAnimNumVerts,
			out Int32 []amatAnimNumTris,
			out Vector3 []amatAnimSortPoints,

			int lightAtlasSize,
			out UtilityLib.TexAtlas lightAtlas);

		public delegate void BuildVLitRenderData(GraphicsDevice g, out VertexBuffer vb,
			out IndexBuffer ib, out VertexDeclaration vd, out Int32 []matOffsets,
			out Int32 []matNumVerts, out Int32 []matNumTris);

		public delegate void BuildAlphaRenderData(GraphicsDevice g, out VertexBuffer vb,
			out IndexBuffer ib, out VertexDeclaration vd, out Int32 []matOffsets,
			out Int32 []matNumVerts, out Int32 []matNumTris, out Vector3 []matSortPoints);

		public delegate void BuildFullBrightRenderData(GraphicsDevice g, out VertexBuffer vb,
			out IndexBuffer ib, out VertexDeclaration vd, out Int32 []matOffsets,
			out Int32 []matNumVerts, out Int32 []matNumTris);

		public delegate void BuildMirrorRenderData(GraphicsDevice g, out VertexBuffer vb,
			out IndexBuffer ib, out VertexDeclaration vd, out Int32 []matOffsets,
			out Int32 []matNumVerts, out Int32 []matNumTris,
			out Vector3 []matSortPoints, out List<List<Vector3>> mirrorPolys);

		public delegate void BuildSkyRenderData(GraphicsDevice g, out VertexBuffer vb,
			out IndexBuffer ib, out VertexDeclaration vd, out Int32 []matOffsets,
			out Int32 []matNumVerts, out Int32 []matNumTris);
		#endregion


		public IndoorMesh(GraphicsDevice gd, MaterialLib.MaterialLib matLib)
		{
			mMatLib	=matLib;

			mMirrorRenderTarget	=new RenderTarget2D(gd, 256, 256, 1,
				gd.PresentationParameters.BackBufferFormat,
				RenderTargetUsage.DiscardContents);

			//Quake1 styled lights 'a' is total darkness, 'z' is maxbright.
			//0 normal
			mStyles.Add(0, "m");
				
			//1 FLICKER (first variety)
			mStyles.Add(1, "mmnmmommommnonmmonqnmmo");
			
			//2 SLOW STRONG PULSE
			mStyles.Add(2, "abcdefghijklmnopqrstuvwxyzyxwvutsrqponmlkjihgfedcba");
			
			//3 CANDLE (first variety)
			mStyles.Add(3, "mmmmmaaaaammmmmaaaaaabcdefgabcdefg");
			
			//4 FAST STROBE
			mStyles.Add(4, "mamamamamama");
			
			//5 GENTLE PULSE 1
			mStyles.Add(5,"jklmnopqrstuvwxyzyxwvutsrqponmlkj");
			
			//6 FLICKER (second variety)
			mStyles.Add(6, "nmonqnmomnmomomno");
			
			//7 CANDLE (second variety)
			mStyles.Add(7, "mmmaaaabcdefgmmmmaaaammmaamm");
			
			//8 CANDLE (third variety)
			mStyles.Add(8, "mmmaaammmaaammmabcdefaaaammmmabcdefmmmaaaa");
			
			//9 SLOW STROBE (fourth variety)
			mStyles.Add(9, "aaaaaaaazzzzzzzz");
			
			//10 FLUORESCENT FLICKER
			mStyles.Add(10, "mmamammmmammamamaaamammma");

			//11 SLOW PULSE NOT FADE TO BLACK
			mStyles.Add(11, "abcdefghijklmnopqrrqponmlkjihgfedcba");
			
			//12 UNDERWATER LIGHT MUTATION
			//this light only distorts the lightmap - no contribution
			//is made to the brightness of affected surfaces
			mStyles.Add(12, "mmnnmmnnnmmnn");

			mCurStylePos.Add(0, 0.0f);
			mCurStylePos.Add(1, 0.0f);
			mCurStylePos.Add(2, 0.0f);
			mCurStylePos.Add(3, 0.0f);
			mCurStylePos.Add(4, 0.0f);
			mCurStylePos.Add(5, 0.0f);
			mCurStylePos.Add(6, 0.0f);
			mCurStylePos.Add(7, 0.0f);
			mCurStylePos.Add(8, 0.0f);
			mCurStylePos.Add(9, 0.0f);
			mCurStylePos.Add(10, 0.0f);
			mCurStylePos.Add(11, 0.0f);
			mCurStylePos.Add(12, 0.0f);
		}


		public void BuildLM(GraphicsDevice g, int atlasSize, BuildLMRenderData brd)
		{
			brd(g, out mLMVB, out mLMIB, out mLMVD, out mLMMatOffsets, out mLMMatNumVerts,
				out mLMMatNumTris, out mLMAnimVB, out mLMAnimIB, out mLMAnimVD,
				out mLMAnimMatOffsets, out mLMAnimMatNumVerts, out mLMAnimMatNumTris,
				out mLMAVB, out mLMAIB, out mLMAVD, out mLMAMatOffsets, out mLMAMatNumVerts,
				out mLMAMatNumTris, out mLMASortPoints, out mLMAAnimVB, out mLMAAnimIB,
				out mLMAAnimVD, out mLMAAnimMatOffsets, out mLMAAnimMatNumVerts,
				out mLMAAnimMatNumTris, out mLMAAnimSortPoints, atlasSize, out mLightMapAtlas);

			mMatLib.AddMap("LightMapAtlas", mLightMapAtlas.GetAtlasTexture());
			mMatLib.RefreshShaderParameters();
		}


		public void BuildVLit(GraphicsDevice g, BuildVLitRenderData brd)
		{
			brd(g, out mVLitVB, out mVLitIB,
						out mVLitVD, out mVLitMatOffsets, out mVLitMatNumVerts,
						out mVLitMatNumTris);
		}


		public void BuildAlpha(GraphicsDevice g, BuildAlphaRenderData brd)
		{
			brd(g, out mAlphaVB, out mAlphaIB, out mAlphaVD,
						out mAlphaMatOffsets, out mAlphaMatNumVerts,
						out mAlphaMatNumTris, out mAlphaSortPoints);
		}


		public void BuildFullBright(GraphicsDevice g, BuildFullBrightRenderData brd)
		{
			brd(g, out mFBVB, out mFBIB, out mFBVD,
						out mFBMatOffsets, out mFBMatNumVerts, out mFBMatNumTris);
		}


		public void BuildMirror(GraphicsDevice g, BuildMirrorRenderData brd)
		{
			brd(g, out mMirrorVB, out mMirrorIB, out mMirrorVD,
						out mMirrorMatOffsets, out mMirrorMatNumVerts,
						out mMirrorMatNumTris, out mMirrorSortPoints, out mMirrorPolys);
		}


		public void BuildSky(GraphicsDevice g, BuildSkyRenderData brd)
		{
			brd(g, out mSkyVB, out mSkyIB, out mSkyVD,
						out mSkyMatOffsets, out mSkyMatNumVerts, out mSkyMatNumTris);
		}


		public void Update(float msDelta)
		{
			UpdateAnimatedLightMaps(msDelta);
		}


		public void Draw(GraphicsDevice gd,
			UtilityLib.GameCamera gameCam,
			IsMaterialVisible bMatVis)
		{
			//draw mirrored world if need be
			List<Matrix>	mirrorMats;
			List<Vector3>	mirrorCenters;
			List<Rectangle>	scissors	=GetMirrorRects(out mirrorMats, out mirrorCenters, gameCam);

			for(int i=0;i < scissors.Count;i++)
			{
				mMatLib.UpdateWVP(gameCam.World, mirrorMats[i], gameCam.Projection, mirrorCenters[i]);

				gd.SetRenderTarget(0, mMirrorRenderTarget);
//				g.Clear(Color.CornflowerBlue);

				//render world
				DrawMaterials(gd, -gameCam.CamPos, mFBVB, mFBIB, mFBVD, mFBMatOffsets, mFBMatNumVerts, mFBMatNumTris, false, null, bMatVis);
				DrawMaterials(gd, -gameCam.CamPos, mVLitVB, mVLitIB, mVLitVD, mVLitMatOffsets, mVLitMatNumVerts, mVLitMatNumTris, false, null, bMatVis);
				DrawMaterials(gd, -gameCam.CamPos, mSkyVB, mSkyIB, mSkyVD, mSkyMatOffsets, mSkyMatNumVerts, mSkyMatNumTris, false, null, bMatVis);
				DrawMaterials(gd, -gameCam.CamPos, mLMVB, mLMIB, mLMVD, mLMMatOffsets, mLMMatNumVerts, mLMMatNumTris, false, null, bMatVis);
				DrawMaterials(gd, -gameCam.CamPos, mLMAnimVB, mLMAnimIB, mLMAnimVD, mLMAnimMatOffsets, mLMAnimMatNumVerts, mLMAnimMatNumTris, false, null, bMatVis);
#if true
				DrawMaterials(gd, -gameCam.CamPos, mAlphaVB, mAlphaIB, mAlphaVD, mAlphaMatOffsets, mAlphaMatNumVerts, mAlphaMatNumTris, false, mAlphaSortPoints, bMatVis);
				DrawMaterials(gd, -gameCam.CamPos, mLMAVB, mLMAIB, mLMAVD, mLMAMatOffsets, mLMAMatNumVerts, mLMAMatNumTris, false, mLMASortPoints, bMatVis);
				DrawMaterials(gd, -gameCam.CamPos, mLMAAnimVB, mLMAAnimIB, mLMAAnimVD, mLMAAnimMatOffsets, mLMAAnimMatNumVerts, mLMAAnimMatNumTris, false, mLMAAnimSortPoints, bMatVis);
#else
				DrawMaterials(gd, -gameCam.CamPos, mAlphaVB, mAlphaIB, mAlphaVD, mAlphaMatOffsets, mAlphaMatNumVerts, mAlphaMatNumTris, true, mAlphaSortPoints, bMatVis);
				DrawMaterials(gd, -gameCam.CamPos, mLMAVB, mLMAIB, mLMAVD, mLMAMatOffsets, mLMAMatNumVerts, mLMAMatNumTris, true, mLMASortPoints, bMatVis);
				DrawMaterials(gd, -gameCam.CamPos, mLMAAnimVB, mLMAAnimIB, mLMAAnimVD, mLMAAnimMatOffsets, mLMAAnimMatNumVerts, mLMAAnimMatNumTris, true, mLMAAnimSortPoints, bMatVis);
				mAlphaPool.DrawAll(gd, mMatLib, -gameCam.CamPos);
#endif
			}

			if(scissors.Count > 0)
			{
				gd.SetRenderTarget(0, null);

				mMirrorTexture	=mMirrorRenderTarget.GetTexture();
				mMatLib.AddMap("MirrorTexture", mMirrorTexture);

				//reset matrices
				mMatLib.UpdateWVP(gameCam.World, gameCam.View, gameCam.Projection, -gameCam.CamPos);
			}

			gd.Clear(Color.CornflowerBlue);

			DrawMaterials(gd, -gameCam.CamPos, mFBVB, mFBIB, mFBVD, mFBMatOffsets, mFBMatNumVerts, mFBMatNumTris, false, null, bMatVis);
			DrawMaterials(gd, -gameCam.CamPos, mVLitVB, mVLitIB, mVLitVD, mVLitMatOffsets, mVLitMatNumVerts, mVLitMatNumTris, false, null, bMatVis);
			DrawMaterials(gd, -gameCam.CamPos, mSkyVB, mSkyIB, mSkyVD, mSkyMatOffsets, mSkyMatNumVerts, mSkyMatNumTris, false, null, bMatVis);
			DrawMaterials(gd, -gameCam.CamPos, mLMVB, mLMIB, mLMVD, mLMMatOffsets, mLMMatNumVerts, mLMMatNumTris, false, null, bMatVis);
			DrawMaterials(gd, -gameCam.CamPos, mLMAnimVB, mLMAnimIB, mLMAnimVD, mLMAnimMatOffsets, mLMAnimMatNumVerts, mLMAnimMatNumTris, false, null, bMatVis);

			//alphas
#if true
			//draw immediately for pix
			DrawMaterials(gd, -gameCam.CamPos, mAlphaVB, mAlphaIB, mAlphaVD, mAlphaMatOffsets, mAlphaMatNumVerts, mAlphaMatNumTris, false, mAlphaSortPoints, bMatVis);
			DrawMaterials(gd, -gameCam.CamPos, mLMAVB, mLMAIB, mLMAVD, mLMAMatOffsets, mLMAMatNumVerts, mLMAMatNumTris, false, mLMASortPoints, bMatVis);
			DrawMaterials(gd, -gameCam.CamPos, mLMAAnimVB, mLMAAnimIB, mLMAAnimVD, mLMAAnimMatOffsets, mLMAAnimMatNumVerts, mLMAAnimMatNumTris, false, mLMAAnimSortPoints, bMatVis);
#else
			//pix freaks out about the alpha sorting
			DrawMaterials(gd, -gameCam.CamPos, mAlphaVB, mAlphaIB, mAlphaVD, mAlphaMatOffsets, mAlphaMatNumVerts, mAlphaMatNumTris, true, mAlphaSortPoints, bMatVis);
			DrawMaterials(gd, -gameCam.CamPos, mLMAVB, mLMAIB, mLMAVD, mLMAMatOffsets, mLMAMatNumVerts, mLMAMatNumTris, true, mLMASortPoints, bMatVis);
			DrawMaterials(gd, -gameCam.CamPos, mLMAAnimVB, mLMAAnimIB, mLMAAnimVD, mLMAAnimMatOffsets, mLMAAnimMatNumVerts, mLMAAnimMatNumTris, true, mLMAAnimSortPoints, bMatVis);
			if(scissors.Count > 0)
			{
				//draw mirror surface itself
				DrawMaterials(gd, -gameCam.CamPos, mMirrorVB, mMirrorIB, mMirrorVD, mMirrorMatOffsets, mMirrorMatNumVerts, mMirrorMatNumTris, true, mMirrorSortPoints, bMatVis);
			}

			mAlphaPool.DrawAll(gd, mMatLib, -gameCam.CamPos);
#endif		
		}


		void DrawMaterials(GraphicsDevice g, Vector3 eyePos,
			VertexBuffer vb, IndexBuffer ib, VertexDeclaration vd,
			Int32 []offsets, Int32 []numVerts, Int32 []numTris,
			bool bAlpha, Vector3 []sortPoints, IsMaterialVisible bMatVis)
		{
			if(vb == null)
			{
				return;
			}

			Dictionary<string, MaterialLib.Material>	mats	=mMatLib.GetMaterials();

			g.VertexDeclaration	=vd;
			g.Vertices[0].SetSource(vb, 0, vd.GetVertexStrideSize(0));
			g.Indices	=ib;

			int	idx	=0;

			foreach(KeyValuePair<string, MaterialLib.Material> mat in mats)
			{
				Effect		fx	=mMatLib.GetShader(mat.Value.ShaderName);
				if(fx == null)
				{
					idx++;
					continue;
				}
				if(numVerts[idx] <= 0)
				{
					idx++;
					continue;
				}
				if(!bMatVis(eyePos, idx))
				{
					idx++;
					continue;
				}

				if(bAlpha)
				{
					mAlphaPool.StoreDraw(sortPoints[idx], mat.Value,
						vb, ib, vd, 0, 0, numVerts[idx],
						offsets[idx], numTris[idx]);
					idx++;
					continue;
				}

				//this might get slow
				mMatLib.ApplyParameters(mat.Key);

				//set renderstates from material
				//this could also get crushingly slow
				mat.Value.ApplyRenderStates(g);

				fx.CommitChanges();

				fx.Begin();
				foreach(EffectPass pass in fx.CurrentTechnique.Passes)
				{
					pass.Begin();

					g.DrawIndexedPrimitives(PrimitiveType.TriangleList,
						0, 0,
						numVerts[idx],
						offsets[idx],
						numTris[idx]);

					pass.End();
				}
				fx.End();

				idx++;
			}
		}


		float StyleVal(string szVal)
		{
			char	first	=szVal[0];
			char	topVal	='z';

			//get from zero to 25
			float	val	=topVal - first;

			//scale up to 0 to 255
			val	*=(255.0f / 25.0f);

			Debug.Assert(val >= 0.0f);
			Debug.Assert(val <= 255.0f);

			return	(255.0f - val) / 255.0f;
		}


		void UpdateAnimatedLightMaps(float msDelta)
		{
			Dictionary<string, MaterialLib.Material>	mats	=mMatLib.GetMaterials();

			string	intensities	="";

			for(int i=0;i < 12;i++)
			{
				mCurStylePos[i]	+=msDelta;

				float	endTime	=mStyles[i].Length * ThirtyFPS;

				while(mCurStylePos[i] >= endTime)
				{
					mCurStylePos[i]	-=endTime;
				}

				int	curPos	=(int)Math.Floor(mCurStylePos[i] / ThirtyFPS);

				float	val		=StyleVal(mStyles[i].Substring(curPos, 1));
				float	nextVal	=StyleVal(mStyles[i].Substring((curPos + 1) % mStyles[i].Length, 1));

				float	ratio	=mCurStylePos[i] - (curPos * ThirtyFPS);

				ratio	/=ThirtyFPS;

				if(i == 11)
				{
					intensities	+="" + MathHelper.Lerp(val, nextVal, ratio);
				}
				else
				{
					intensities	+="" + MathHelper.Lerp(val, nextVal, ratio) + " ";
				}
			}

			foreach(KeyValuePair<string, MaterialLib.Material> mat in mats)
			{
				if(mat.Key.EndsWith("Anim"))
				{
					mat.Value.AddParameter("mAniIntensities",
						EffectParameterClass.Scalar,
						EffectParameterType.Single,
						intensities);
				}
			}
		}


		List<Rectangle> GetMirrorRects(out List<Matrix> mirrorMats,
									   out List<Vector3> mirrorCenters,
										UtilityLib.GameCamera gameCam)
		{
			List<Rectangle>	scissorRects	=new List<Rectangle>();

			mirrorMats		=new List<Matrix>();
			mirrorCenters	=new List<Vector3>();

			foreach(List<Vector3> poly in mMirrorPolys)
			{
				//see if we are behind the mirror
				Vector3	norm;
				float	dist;
				UtilityLib.Mathery.PlaneFromVerts(poly, out norm, out dist);
				if(Vector3.Dot(-gameCam.CamPos, norm) - dist < 0)
				{
					continue;
				}

				BoundingBox	box	=GetExtents(poly);
				if(gameCam.IsBoxOnScreen(box))
				{
					scissorRects.Add(gameCam.GetScreenCoverage(poly));

					//calculate centerpoint
					Vector3	center	=Vector3.Zero;
					foreach(Vector3 vert in poly)
					{
						center	+=vert;
					}
					center	/=poly.Count;
					mirrorCenters.Add(center);

					Vector3	eyeVec	=center - -gameCam.CamPos;

					Vector3	reflect	=Vector3.Reflect(eyeVec, norm);

					reflect.Normalize();

					//get view matrix
					Vector3	side	=Vector3.Cross(reflect, Vector3.Up);
					if(side.LengthSquared() == 0.0f)
					{
						side	=Vector3.Cross(reflect, Vector3.Right);
					}
					Vector3	up	=Vector3.Cross(reflect, side);

					Matrix	mirrorView	=Matrix.CreateLookAt(center, center + reflect, up);
					mirrorMats.Add(mirrorView);
				}
			}
			return	scissorRects;
		}


		BoundingBox GetExtents(List<Vector3> poly)
		{
			Vector3	mins	=Vector3.One * 696969.0f;
			Vector3	maxs	=Vector3.One * -696969.0f;

			foreach(Vector3 pnt in poly)
			{
				if(pnt.X < mins.X)
				{
					mins.X	=pnt.X;
				}
				if(pnt.X > maxs.X)
				{
					maxs.X	=pnt.X;
				}
				if(pnt.Y < mins.Y)
				{
					mins.Y	=pnt.Y;
				}
				if(pnt.Y > maxs.Y)
				{
					maxs.Y	=pnt.Y;
				}
				if(pnt.Z < mins.Z)
				{
					mins.Z	=pnt.Z;
				}
				if(pnt.Z > maxs.Z)
				{
					maxs.Z	=pnt.Z;
				}
			}
			return	new BoundingBox(mins, maxs);
		}


		public void Write(string fileName)
		{
			FileStream	file	=UtilityLib.FileUtil.OpenTitleFile(fileName,
									FileMode.OpenOrCreate, FileAccess.Write);

			BinaryWriter	bw	=new BinaryWriter(file);

			bw.Write(0x57415244);	//DRAW

			mLightMapAtlas.Write(bw);

			int	numVerts	=mLMVB.SizeInBytes / mLMVD.GetVertexStrideSize(0);
			int	typeIdx		=MeshLib.VertexTypes.GetIndex(typeof(MeshLib.VPosNormTex0Tex1));

			bw.Write(numVerts);
			bw.Write(typeIdx);
			MeshLib.VertexTypes.WriteVerts(bw, mLMVB, numVerts, typeIdx);
			UtilityLib.FileUtil.WriteIndexBuffer(bw, mLMIB);
			UtilityLib.FileUtil.WriteVertexDeclaration(bw, mLMVD);

			numVerts	=mVLitVB.SizeInBytes / mVLitVD.GetVertexStrideSize(0);
			typeIdx		=MeshLib.VertexTypes.GetIndex(typeof(MeshLib.VPosNormTex0Col0));
			bw.Write(numVerts);
			bw.Write(typeIdx);
			MeshLib.VertexTypes.WriteVerts(bw, mVLitVB, numVerts, typeIdx);
			UtilityLib.FileUtil.WriteIndexBuffer(bw, mVLitIB);
			UtilityLib.FileUtil.WriteVertexDeclaration(bw, mVLitVD);

			numVerts	=mLMAnimVB.SizeInBytes / mLMAnimVD.GetVertexStrideSize(0);
			typeIdx		=MeshLib.VertexTypes.GetIndex(typeof(MeshLib.VPosNormTex0Col0));
			bw.Write(numVerts);
			bw.Write(typeIdx);
			MeshLib.VertexTypes.WriteVerts(bw, mLMAnimVB, numVerts, typeIdx);
			UtilityLib.FileUtil.WriteIndexBuffer(bw, mLMAnimIB);
			UtilityLib.FileUtil.WriteVertexDeclaration(bw, mLMAnimVD);

			numVerts	=mAlphaVB.SizeInBytes / mAlphaVD.GetVertexStrideSize(0);
			typeIdx		=MeshLib.VertexTypes.GetIndex(typeof(MeshLib.VPosNormTex0Col0));
			bw.Write(typeIdx);
			bw.Write(numVerts);
			MeshLib.VertexTypes.WriteVerts(bw, mAlphaVB, numVerts, typeIdx);
			UtilityLib.FileUtil.WriteIndexBuffer(bw, mAlphaIB);
			UtilityLib.FileUtil.WriteVertexDeclaration(bw, mAlphaVD);

			numVerts	=mSkyVB.SizeInBytes / mSkyVD.GetVertexStrideSize(0);
			typeIdx		=MeshLib.VertexTypes.GetIndex(typeof(MeshLib.VPosNormTex0Col0));
			bw.Write(numVerts);
			bw.Write(typeIdx);
			MeshLib.VertexTypes.WriteVerts(bw, mSkyVB, numVerts, typeIdx);
			UtilityLib.FileUtil.WriteIndexBuffer(bw, mSkyIB);
			UtilityLib.FileUtil.WriteVertexDeclaration(bw, mSkyVD);

			numVerts	=mFBVB.SizeInBytes / mFBVD.GetVertexStrideSize(0);
			typeIdx		=MeshLib.VertexTypes.GetIndex(typeof(MeshLib.VPosNormTex0Col0));
			bw.Write(numVerts);
			bw.Write(typeIdx);
			MeshLib.VertexTypes.WriteVerts(bw, mFBVB, numVerts, typeIdx);
			UtilityLib.FileUtil.WriteIndexBuffer(bw, mFBIB);
			UtilityLib.FileUtil.WriteVertexDeclaration(bw, mFBVD);

			numVerts	=mMirrorVB.SizeInBytes / mMirrorVD.GetVertexStrideSize(0);
			typeIdx		=MeshLib.VertexTypes.GetIndex(typeof(MeshLib.VPosNormTex0Col0));
			bw.Write(numVerts);
			bw.Write(typeIdx);
			MeshLib.VertexTypes.WriteVerts(bw, mMirrorVB, numVerts, typeIdx);
			UtilityLib.FileUtil.WriteIndexBuffer(bw, mMirrorIB);
			UtilityLib.FileUtil.WriteVertexDeclaration(bw, mMirrorVD);

			numVerts	=mLMAVB.SizeInBytes / mLMAVD.GetVertexStrideSize(0);
			typeIdx		=MeshLib.VertexTypes.GetIndex(typeof(MeshLib.VPosNormTex0Col0));
			bw.Write(numVerts);
			bw.Write(typeIdx);
			MeshLib.VertexTypes.WriteVerts(bw, mLMAVB, numVerts, typeIdx);
			UtilityLib.FileUtil.WriteIndexBuffer(bw, mLMAIB);
			UtilityLib.FileUtil.WriteVertexDeclaration(bw, mLMAVD);

			numVerts	=mLMAAnimVB.SizeInBytes / mLMAAnimVD.GetVertexStrideSize(0);
			typeIdx		=MeshLib.VertexTypes.GetIndex(typeof(MeshLib.VPosNormTex0Col0));
			bw.Write(numVerts);
			bw.Write(typeIdx);
			MeshLib.VertexTypes.WriteVerts(bw, mLMAAnimVB, numVerts, typeIdx);
			UtilityLib.FileUtil.WriteIndexBuffer(bw, mLMAAnimIB);
			UtilityLib.FileUtil.WriteVertexDeclaration(bw, mLMAAnimVD);

			//material offsets
			UtilityLib.FileUtil.WriteArray(bw, mLMMatOffsets);
			UtilityLib.FileUtil.WriteArray(bw, mVLitMatOffsets);
			UtilityLib.FileUtil.WriteArray(bw, mLMAnimMatOffsets);
			UtilityLib.FileUtil.WriteArray(bw, mAlphaMatOffsets);
			UtilityLib.FileUtil.WriteArray(bw, mSkyMatOffsets);
			UtilityLib.FileUtil.WriteArray(bw, mFBMatOffsets);
			UtilityLib.FileUtil.WriteArray(bw, mMirrorMatOffsets);
			UtilityLib.FileUtil.WriteArray(bw, mLMAMatOffsets);
			UtilityLib.FileUtil.WriteArray(bw, mLMAAnimMatOffsets);

			//numverts per material
			UtilityLib.FileUtil.WriteArray(bw, mLMMatNumVerts);
			UtilityLib.FileUtil.WriteArray(bw, mVLitMatNumVerts);
			UtilityLib.FileUtil.WriteArray(bw, mLMAnimMatNumVerts);
			UtilityLib.FileUtil.WriteArray(bw, mAlphaMatNumVerts);
			UtilityLib.FileUtil.WriteArray(bw, mSkyMatNumVerts);
			UtilityLib.FileUtil.WriteArray(bw, mFBMatNumVerts);
			UtilityLib.FileUtil.WriteArray(bw, mMirrorMatNumVerts);
			UtilityLib.FileUtil.WriteArray(bw, mLMAMatNumVerts);
			UtilityLib.FileUtil.WriteArray(bw, mLMAAnimMatNumVerts);

			//primcount per material
			UtilityLib.FileUtil.WriteArray(bw, mLMMatNumTris);
			UtilityLib.FileUtil.WriteArray(bw, mVLitMatNumTris);
			UtilityLib.FileUtil.WriteArray(bw, mLMAnimMatNumTris);
			UtilityLib.FileUtil.WriteArray(bw, mAlphaMatNumTris);
			UtilityLib.FileUtil.WriteArray(bw, mSkyMatNumTris);
			UtilityLib.FileUtil.WriteArray(bw, mFBMatNumTris);
			UtilityLib.FileUtil.WriteArray(bw, mMirrorMatNumTris);
			UtilityLib.FileUtil.WriteArray(bw, mLMAMatNumTris);
			UtilityLib.FileUtil.WriteArray(bw, mLMAAnimMatNumTris);

			//sort points
			UtilityLib.FileUtil.WriteArray(bw, mLMASortPoints);
			UtilityLib.FileUtil.WriteArray(bw, mAlphaSortPoints);
			UtilityLib.FileUtil.WriteArray(bw, mMirrorSortPoints);
			UtilityLib.FileUtil.WriteArray(bw, mLMAAnimSortPoints);

			//mirror polys
			bw.Write(mMirrorPolys.Count);
			for(int i=0;i < mMirrorPolys.Count;i++)
			{
				UtilityLib.FileUtil.WriteArray(bw, mMirrorPolys[i].ToArray());
			}

			bw.Close();
			file.Close();
		}
	}
}
