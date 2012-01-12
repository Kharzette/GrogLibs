//#define PIXGOBLINRY	//use this to remove alpha sorting, which somehow makes pix crash

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
		IndexBuffer			mLMIB, mVLitIB, mLMAnimIB, mAlphaIB;
		IndexBuffer			mSkyIB, mFBIB, mMirrorIB, mLMAIB, mLMAAnimIB;
		RenderTarget2D		mMirrorRenderTarget;

		int	mVLitTypeIdx;

		//material library reference
		MaterialLib.MaterialLib	mMatLib;

		//light map atlas
		MaterialLib.TexAtlas	mLightMapAtlas;

		//lightmap animation stuff
		Dictionary<int, string>	mStyles			=new Dictionary<int, string>();
		Dictionary<int, float>	mCurStylePos	=new Dictionary<int, float>();
		bool					[]mSwitches		=new bool[32];	//switchable on / off

		//material draw stuff
		//drawcalls for sorted alphas
		List<DrawCall>	[]mLMADrawCalls;
		List<DrawCall>	[]mAlphaDrawCalls;
		List<DrawCall>	[]mLMAAnimDrawCalls;
		List<DrawCall>	[]mMirrorDrawCalls;

		//drawcalls for non alphas (single per material)
		DrawCall	[]mLMDrawCalls;
		DrawCall	[]mLMAnimDrawCalls;
		DrawCall	[]mVLitDrawCalls;
		DrawCall	[]mSkyDrawCalls;
		DrawCall	[]mFBDrawCalls;

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
			out DrawCall []lmDC,

			//animated lightmap stuff
			out VertexBuffer lmAnimVB,
			out IndexBuffer lmAnimIB,
			out DrawCall []lmAnimDC,

			//lightmapped alpha stuff
			out VertexBuffer lmaVB,
			out IndexBuffer lmaIB,
			out List<DrawCall> []lmaDCalls,

			//animated alpha lightmap stuff
			out VertexBuffer lmaAnimVB,
			out IndexBuffer lmaAnimIB,
			out List<DrawCall> []lmaAnimDCalls,

			int lightAtlasSize,
			object pp,
			out MaterialLib.TexAtlas lightAtlas);

		public delegate void BuildVLitRenderData(GraphicsDevice g, out VertexBuffer vb,
			out IndexBuffer ib, out DrawCall []dcs, object pp);

		public delegate void BuildAlphaRenderData(GraphicsDevice g, out VertexBuffer vb,
			out IndexBuffer ib, out List<DrawCall> []adcs, object pp);

		public delegate void BuildFullBrightRenderData(GraphicsDevice g, out VertexBuffer vb,
			out IndexBuffer ib, out DrawCall []dcs, object pp);

		public delegate void BuildMirrorRenderData(GraphicsDevice g, out VertexBuffer vb,
			out IndexBuffer ib, out List<DrawCall> []mdcalls,
			out List<List<Vector3>> mirrorPolys, object pp);

		public delegate void BuildSkyRenderData(GraphicsDevice g, out VertexBuffer vb,
			out IndexBuffer ib, out DrawCall []dcs, object pp);
		#endregion


		public IndoorMesh(GraphicsDevice gd, MaterialLib.MaterialLib matLib)
		{
			mMatLib	=matLib;

			mMirrorRenderTarget	=new RenderTarget2D(gd, 256, 256, false,
				gd.PresentationParameters.BackBufferFormat,
				gd.PresentationParameters.DepthStencilFormat,
				0, RenderTargetUsage.DiscardContents);

			//Quake1 styled lights 'a' is total darkness, 'z' is maxbright.
			//0 normal
			mStyles.Add(0, "z");
				
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


		public void BuildLM(GraphicsDevice g, int atlasSize, BuildLMRenderData brd, object pp)
		{
			brd(g, out mLMVB, out mLMIB, out mLMDrawCalls, out mLMAnimVB, out mLMAnimIB,
				out mLMAnimDrawCalls, out mLMAVB, out mLMAIB, out mLMADrawCalls,
				out mLMAAnimVB, out mLMAAnimIB,	out mLMAAnimDrawCalls,
				atlasSize, pp, out mLightMapAtlas);

			if(mLightMapAtlas != null)
			{
				mMatLib.AddMap("LightMapAtlas", mLightMapAtlas.GetAtlasTexture());
				mMatLib.RefreshShaderParameters();
			}
		}


		public void BuildVLit(GraphicsDevice g, BuildVLitRenderData brd, object pp)
		{
			brd(g, out mVLitVB, out mVLitIB, out mVLitDrawCalls, pp);
		}


		public void BuildAlpha(GraphicsDevice g, BuildAlphaRenderData brd, object pp)
		{
			brd(g, out mAlphaVB, out mAlphaIB, out mAlphaDrawCalls, pp);
		}


		public void BuildFullBright(GraphicsDevice g, BuildFullBrightRenderData brd, object pp)
		{
			brd(g, out mFBVB, out mFBIB, out mFBDrawCalls, pp);
		}


		public void BuildMirror(GraphicsDevice g, BuildMirrorRenderData brd, object pp)
		{
			brd(g, out mMirrorVB, out mMirrorIB, out mMirrorDrawCalls, out mMirrorPolys, pp);
		}


		public void BuildSky(GraphicsDevice g, BuildSkyRenderData brd, object pp)
		{
			brd(g, out mSkyVB, out mSkyIB, out mSkyDrawCalls, pp);
		}


		public void Update(float msDelta)
		{
			UpdateAnimatedLightMaps(msDelta);
		}


		public void Draw(GraphicsDevice gd,
			UtilityLib.GameCamera gameCam,
			Vector3 position,
			IsMaterialVisible bMatVis)
		{
			//draw mirrored world if need be
			List<Matrix>	mirrorMats;
			List<Vector3>	mirrorCenters;
			List<Rectangle>	scissors	=GetMirrorRects(out mirrorMats, out mirrorCenters, -position, gameCam);

			for(int i=0;i < scissors.Count;i++)
			{
				mMatLib.UpdateWVP(gameCam.World, mirrorMats[i], gameCam.Projection, mirrorCenters[i]);

				gd.SetRenderTarget(mMirrorRenderTarget);
//				g.Clear(Color.CornflowerBlue);

				//render world
				DrawMaterialsDC(gd, position, mFBVB, mFBIB, mFBDrawCalls, bMatVis);
				DrawMaterialsDC(gd, position, mVLitVB, mVLitIB, mVLitDrawCalls, bMatVis);
				DrawMaterialsDC(gd, position, mSkyVB, mSkyIB, mSkyDrawCalls, bMatVis);
				DrawMaterialsDC(gd, position, mLMVB, mLMIB, mLMDrawCalls, bMatVis);
				DrawMaterialsDC(gd, position, mLMAnimVB, mLMAnimIB, mLMAnimDrawCalls, bMatVis);
				
				//alphas
				DrawMaterialsDC(gd, position, mAlphaVB, mAlphaIB, mAlphaDrawCalls, bMatVis);
				DrawMaterialsDC(gd, position, mLMAVB, mLMAIB, mLMADrawCalls, bMatVis);
				DrawMaterialsDC(gd, position, mLMAAnimVB, mLMAAnimIB, mLMAAnimDrawCalls, bMatVis);
				mAlphaPool.DrawAll(gd, mMatLib, position);
			}

			if(scissors.Count > 0)
			{
				gd.SetRenderTarget(null);
				mMatLib.AddMap("MirrorTexture", mMirrorRenderTarget);

				//reset matrices
				mMatLib.UpdateWVP(gameCam.World, gameCam.View, gameCam.Projection, -position);
			}

			gd.Clear(Color.CornflowerBlue);

			DrawMaterialsDC(gd, position, mFBVB, mFBIB, mFBDrawCalls, bMatVis);
			DrawMaterialsDC(gd, position, mVLitVB, mVLitIB, mVLitDrawCalls, bMatVis);
			DrawMaterialsDC(gd, position, mSkyVB, mSkyIB, mSkyDrawCalls, bMatVis);
			DrawMaterialsDC(gd, position, mLMVB, mLMIB, mLMDrawCalls, bMatVis);
			DrawMaterialsDC(gd, position, mLMAnimVB, mLMAnimIB, mLMAnimDrawCalls, bMatVis);

			//alphas
			DrawMaterialsDC(gd, position, mAlphaVB, mAlphaIB, mAlphaDrawCalls, bMatVis);
			DrawMaterialsDC(gd, position, mLMAVB, mLMAIB, mLMADrawCalls, bMatVis);
			DrawMaterialsDC(gd, position, mLMAAnimVB, mLMAAnimIB, mLMAAnimDrawCalls, bMatVis);
			if(scissors.Count > 0)
			{
				//draw mirror surface itself
				DrawMaterialsDC(gd, position, mMirrorVB, mMirrorIB, mMirrorDrawCalls, bMatVis);
			}

			mAlphaPool.DrawAll(gd, mMatLib, position);
		}


		//for opaques
		void DrawMaterialsDC(GraphicsDevice g, Vector3 eyePos,
			VertexBuffer vb, IndexBuffer ib, DrawCall []dcs,
			IsMaterialVisible bMatVis)
		{
			if(vb == null)
			{
				return;
			}

			Dictionary<string, MaterialLib.Material>	mats	=mMatLib.GetMaterials();

			g.SetVertexBuffer(vb);
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
				if(!bMatVis(eyePos, idx))
				{
					idx++;
					continue;
				}

				DrawCall	call	=dcs[idx];
				if(call.mPrimCount == 0)
				{
					idx++;
					continue;
				}

				mMatLib.ApplyParameters(mat.Key);

				//set renderstates from material
				mat.Value.ApplyRenderStates(g);

				fx.CurrentTechnique.Passes[0].Apply();

				g.DrawIndexedPrimitives(PrimitiveType.TriangleList,
					0, 0, call.mNumVerts, call.mStartIndex, call.mPrimCount);
				idx++;
			}
		}


		//this one is for alphas
		void DrawMaterialsDC(GraphicsDevice g, Vector3 eyePos,
			VertexBuffer vb, IndexBuffer ib, List<DrawCall> []dcs,
			IsMaterialVisible bMatVis)
		{
			if(vb == null)
			{
				return;
			}

			Dictionary<string, MaterialLib.Material>	mats	=mMatLib.GetMaterials();

			g.SetVertexBuffer(vb);
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
				if(!bMatVis(eyePos, idx))
				{
					idx++;
					continue;
				}

				List<DrawCall>	calls	=dcs[idx];
				if(calls.Count == 0)
				{
					idx++;
					continue;
				}

				foreach(DrawCall dc in calls)
				{
					if(dc.mPrimCount <= 0)
					{
						continue;
					}
					mAlphaPool.StoreDraw(dc.mSortPoint, mat.Value,
						vb, ib, 0, 0, dc.mNumVerts,
						dc.mStartIndex, dc.mPrimCount);
				}
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

				intensities	+="" + MathHelper.Lerp(val, nextVal, ratio) + " ";
			}

			//switchable lights
			for(int i=0;i < 32;i++)
			{
				intensities	+="" + ((mSwitches[i])? 1.0f : 0.0f) + " ";
			}

			intensities	=intensities.TrimEnd(' ');

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


		List<Rectangle> GetMirrorRects(out List<Matrix>			mirrorMats,
									   out List<Vector3>		mirrorCenters,
									   Vector3					position,
									   UtilityLib.GameCamera	gameCam)
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
				if(Vector3.Dot(-position, norm) - dist < 0)
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

					Vector3	eyeVec	=center - -position;

					Vector3	reflect	=Vector3.Reflect(eyeVec, norm);

					reflect.Normalize();

					//get view matrix
					//needs to be upside down
					Vector3	side	=Vector3.Cross(reflect, Vector3.Down);
					if(side.LengthSquared() == 0.0f)
					{
						side	=Vector3.Cross(reflect, Vector3.Left);
					}
					Vector3	up	=Vector3.Cross(reflect, side);

					Matrix	mirrorView	=Matrix.CreateLookAt(center, center + reflect, up);
					mirrorMats.Add(mirrorView);
				}
			}
			return	scissorRects;
		}


		public List<Vector3>	GetNormals()
		{
			return	VertexTypes.GetNormals(mVLitVB, mVLitTypeIdx);
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


		public Texture2D GetLightMapAtlas()
		{
			if(mLightMapAtlas == null)
			{
				return	null;
			}
			return	mLightMapAtlas.GetAtlasTexture();
		}


		public void SwitchLight(int lightIndex, bool bOn)
		{
			mSwitches[lightIndex - 32]	=bOn;
		}


		#region IO
		public void Read(GraphicsDevice g, string fileName, bool bEditor)
		{
			Stream			file	=null;
			if(bEditor)
			{
				file	=new FileStream(fileName, FileMode.Open, FileAccess.Read);
			}
			else
			{
				file	=UtilityLib.FileUtil.OpenTitleFile(fileName);
			}

			if(file == null)
			{
				return;
			}
			BinaryReader	br	=new BinaryReader(file);

			UInt32	tag	=br.ReadUInt32();

			if(tag != 0x57415244)
			{
				return;
			}

			mLightMapAtlas	=new MaterialLib.TexAtlas(g, 1, 1);

			bool	bLightMapNeeded	=br.ReadBoolean();

			if(bLightMapNeeded)
			{
				mLightMapAtlas.Read(g, br);
				mMatLib.AddMap("LightMapAtlas", mLightMapAtlas.GetAtlasTexture());
			}


			int	numVerts	=br.ReadInt32();
			if(numVerts != 0)
			{
				int	typeIdx	=br.ReadInt32();
				VertexTypes.ReadVerts(br, g, out mLMVB, numVerts, typeIdx, bEditor);
				UtilityLib.FileUtil.ReadIndexBuffer(br, out mLMIB, g, bEditor);
			}

			numVerts	=br.ReadInt32();
			if(numVerts != 0)
			{
				mVLitTypeIdx	=br.ReadInt32();
				VertexTypes.ReadVerts(br, g, out mVLitVB, numVerts, mVLitTypeIdx, bEditor);
				UtilityLib.FileUtil.ReadIndexBuffer(br, out mVLitIB, g, bEditor);
			}

			numVerts	=br.ReadInt32();
			if(numVerts != 0)
			{
				int	typeIdx		=br.ReadInt32();
				VertexTypes.ReadVerts(br, g, out mLMAnimVB, numVerts, typeIdx, bEditor);
				UtilityLib.FileUtil.ReadIndexBuffer(br, out mLMAnimIB, g, bEditor);
			}

			numVerts	=br.ReadInt32();
			if(numVerts != 0)
			{
				int	typeIdx		=br.ReadInt32();
				VertexTypes.ReadVerts(br, g, out mAlphaVB, numVerts, typeIdx, bEditor);
				UtilityLib.FileUtil.ReadIndexBuffer(br, out mAlphaIB, g, bEditor);
			}

			numVerts	=br.ReadInt32();
			if(numVerts != 0)
			{
				int	typeIdx		=br.ReadInt32();
				VertexTypes.ReadVerts(br, g, out mSkyVB, numVerts, typeIdx, bEditor);
				UtilityLib.FileUtil.ReadIndexBuffer(br, out mSkyIB, g, bEditor);
			}

			numVerts	=br.ReadInt32();
			if(numVerts != 0)
			{
				int	typeIdx		=br.ReadInt32();
				VertexTypes.ReadVerts(br, g, out mFBVB, numVerts, typeIdx, bEditor);
				UtilityLib.FileUtil.ReadIndexBuffer(br, out mFBIB, g, bEditor);
			}

			numVerts	=br.ReadInt32();
			if(numVerts != 0)
			{
				int	typeIdx		=br.ReadInt32();
				VertexTypes.ReadVerts(br, g, out mMirrorVB, numVerts, typeIdx, bEditor);
				UtilityLib.FileUtil.ReadIndexBuffer(br, out mMirrorIB, g, bEditor);
			}

			numVerts	=br.ReadInt32();
			if(numVerts != 0)
			{
				int	typeIdx		=br.ReadInt32();
				VertexTypes.ReadVerts(br, g, out mLMAVB, numVerts, typeIdx, bEditor);
				UtilityLib.FileUtil.ReadIndexBuffer(br, out mLMAIB, g, bEditor);
			}

			numVerts	=br.ReadInt32();
			if(numVerts != 0)
			{
				int	typeIdx		=br.ReadInt32();
				VertexTypes.ReadVerts(br, g, out mLMAAnimVB, numVerts, typeIdx, bEditor);
				UtilityLib.FileUtil.ReadIndexBuffer(br, out mLMAAnimIB, g, bEditor);
			}

			mLMDrawCalls		=DrawCall.ReadDrawCallArray(br);
			mVLitDrawCalls		=DrawCall.ReadDrawCallArray(br);
			mLMAnimDrawCalls	=DrawCall.ReadDrawCallArray(br);
			mSkyDrawCalls		=DrawCall.ReadDrawCallArray(br);
			mFBDrawCalls		=DrawCall.ReadDrawCallArray(br);

			mLMADrawCalls		=DrawCall.ReadDrawCallListArray(br);
			mAlphaDrawCalls		=DrawCall.ReadDrawCallListArray(br);
			mLMAAnimDrawCalls	=DrawCall.ReadDrawCallListArray(br);
			mMirrorDrawCalls	=DrawCall.ReadDrawCallListArray(br);
			
			int	mirrorCount	=br.ReadInt32();
			for(int i=0;i < mirrorCount;i++)
			{
				Vector3	[]verts	=UtilityLib.FileUtil.ReadVecArray(br);

				List<Vector3>	vlist	=new List<Vector3>(verts);

				mMirrorPolys.Add(vlist);
			}

			br.Close();
			file.Close();
		}


		void WriteMaterial(VertexBuffer vb, IndexBuffer ib, BinaryWriter bw)
		{
			if(vb == null)
			{
				bw.Write(0);
			}
			else
			{
				int	typeIdx		=MeshLib.VertexTypes.GetIndexForVertexDeclaration(vb.VertexDeclaration);

				if(typeIdx == -1)
				{
					//bogus type
					bw.Write(0);
					return;
				}

				bw.Write(vb.VertexCount);
				bw.Write(typeIdx);
				MeshLib.VertexTypes.WriteVerts(bw, vb, typeIdx);
				UtilityLib.FileUtil.WriteIndexBuffer(bw, ib);
			}
		}


		public void Write(string fileName)
		{
			FileStream	file	=new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write);

			BinaryWriter	bw	=new BinaryWriter(file);

			bw.Write(0x57415244);	//DRAW

			bw.Write(mLightMapAtlas != null);

			if(mLightMapAtlas != null)
			{
				mLightMapAtlas.Write(bw);
			}

			WriteMaterial(mLMVB, mLMIB, bw);
			WriteMaterial(mVLitVB, mVLitIB, bw);
			WriteMaterial(mLMAnimVB, mLMAnimIB, bw);
			WriteMaterial(mAlphaVB, mAlphaIB, bw);
			WriteMaterial(mSkyVB, mSkyIB, bw);
			WriteMaterial(mFBVB, mFBIB, bw);
			WriteMaterial(mMirrorVB, mMirrorIB, bw);
			WriteMaterial(mLMAVB, mLMAIB, bw);
			WriteMaterial(mLMAAnimVB, mLMAAnimIB, bw);

			//drawcall stuff
			//opaques
			DrawCall.WriteDrawCallArray(bw, mLMDrawCalls);
			DrawCall.WriteDrawCallArray(bw, mVLitDrawCalls);
			DrawCall.WriteDrawCallArray(bw, mLMAnimDrawCalls);
			DrawCall.WriteDrawCallArray(bw, mSkyDrawCalls);
			DrawCall.WriteDrawCallArray(bw, mFBDrawCalls);

			//alphas
			DrawCall.WriteDrawCallListArray(bw, mLMADrawCalls);
			DrawCall.WriteDrawCallListArray(bw, mAlphaDrawCalls);
			DrawCall.WriteDrawCallListArray(bw, mLMAAnimDrawCalls);
			DrawCall.WriteDrawCallListArray(bw, mMirrorDrawCalls);

			//mirror polys
			bw.Write(mMirrorPolys.Count);
			for(int i=0;i < mMirrorPolys.Count;i++)
			{
				UtilityLib.FileUtil.WriteArray(bw, mMirrorPolys[i].ToArray());
			}

			bw.Close();
			file.Close();
		}
		#endregion
	}
}