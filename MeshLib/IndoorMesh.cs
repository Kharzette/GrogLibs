//#define PIXGOBLINRY	//use this to remove alpha sorting, which somehow makes pix crash
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using UtilityLib;


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

		int	mVLitTypeIdx;

		//material library reference
		MaterialLib.MaterialLib	mMatLib;

		//light map atlas
		MaterialLib.TexAtlas	mLightMapAtlas;

		//lightmap animation stuff
		Dictionary<int, string>	mStyles				=new Dictionary<int, string>();
		Dictionary<int, float>	mCurStylePos		=new Dictionary<int, float>();
		bool					[]mSwitches			=new bool[32];	//switchable on / off
		float					[]mAniIntensities	=new float[44];

		//material draw stuff
		//indexed by model in the dictionary
		//drawcalls for sorted alphas
		Dictionary<int, List<List<DrawCall>>>	mLMADrawCalls;
		Dictionary<int, List<List<DrawCall>>>	mLMAAnimDrawCalls;
		Dictionary<int, List<List<DrawCall>>>	mAlphaDrawCalls;

		//drawcalls for non alphas (single per material)
		Dictionary<int, List<DrawCall>>	mLMDrawCalls;
		Dictionary<int, List<DrawCall>>	mVLitDrawCalls;
		Dictionary<int, List<DrawCall>>	mLMAnimDrawCalls;
		Dictionary<int, List<DrawCall>>	mSkyDrawCalls;
		Dictionary<int, List<DrawCall>>	mFBDrawCalls;
		Dictionary<int, List<DrawCall>>	mMirrorDrawCalls;

		//mirror polys for rendering through
		List<List<Vector3>>	mMirrorPolys	=new List<List<Vector3>>();

		//for sorting alphas
		MaterialLib.AlphaPool	mAlphaPool	=new MaterialLib.AlphaPool();

		//constants
		const float		ThirtyFPS	=(1000.0f / 30.0f);

		//delegates
		#region Delegates
		public delegate bool IsMaterialVisible(Vector3 eyePos, int matIdx);
		public delegate Matrix GetModelMatrix(int modelIndex);

		//render external stuff
		public delegate void RenderExternal(MaterialLib.AlphaPool ap,
			Vector3 camPos, Matrix view, Matrix proj);
		public delegate void RenderExternalDMN(Vector3 camPos,
			Matrix view, Matrix proj);

		//tool side delegates for building the indoor mesh
		//from raw parts
		public delegate bool BuildLMRenderData(GraphicsDevice g,
			//lightmap stuff
			out VertexBuffer lmVB,
			out IndexBuffer lmIB,
			out Dictionary<int, List<DrawCall>> lmDC,

			//animated lightmap stuff
			out VertexBuffer lmAnimVB,
			out IndexBuffer lmAnimIB,
			out Dictionary<int, List<DrawCall>> lmAnimDC,

			//lightmapped alpha stuff
			out VertexBuffer lmaVB,
			out IndexBuffer lmaIB,
			out Dictionary<int, List<List<DrawCall>>> lmaDCalls,

			//animated alpha lightmap stuff
			out VertexBuffer lmaAnimVB,
			out IndexBuffer lmaAnimIB,
			out Dictionary<int, List<List<MeshLib.DrawCall>>> lmaAnimDCalls,

			int lightAtlasSize,
			object pp,
			out MaterialLib.TexAtlas lightAtlas, bool bDynamicLights);

		public delegate void BuildVLitRenderData(GraphicsDevice g, out VertexBuffer vb,
			out IndexBuffer ib, out Dictionary<int, List<DrawCall>> dcs, object pp, bool bDynamicLights);

		public delegate void BuildAlphaRenderData(GraphicsDevice g, out VertexBuffer vb,
			out IndexBuffer ib, out Dictionary<int, List<List<MeshLib.DrawCall>>> adcs, object pp, bool bDynamicLights);

		public delegate void BuildFullBrightRenderData(GraphicsDevice g, out VertexBuffer vb,
			out IndexBuffer ib, out Dictionary<int, List<DrawCall>> dcs, object pp);

		public delegate void BuildMirrorRenderData(GraphicsDevice g, out VertexBuffer vb,
			out IndexBuffer ib, out Dictionary<int, List<MeshLib.DrawCall>> mdcalls,
			out List<List<Vector3>> mirrorPolys, object pp, bool bDynamicLights);

		public delegate void BuildSkyRenderData(GraphicsDevice g, out VertexBuffer vb,
			out IndexBuffer ib, out Dictionary<int, List<DrawCall>> dcs, object pp);
		#endregion


		public IndoorMesh(GraphicsDevice gd, MaterialLib.MaterialLib matLib)
		{
			mMatLib	=matLib;

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


		public void BuildLM(GraphicsDevice g, int atlasSize, BuildLMRenderData brd, object pp, bool bDyn)
		{
			brd(g, out mLMVB, out mLMIB, out mLMDrawCalls, out mLMAnimVB, out mLMAnimIB,
				out mLMAnimDrawCalls, out mLMAVB, out mLMAIB, out mLMADrawCalls,
				out mLMAAnimVB, out mLMAAnimIB,	out mLMAAnimDrawCalls,
				atlasSize, pp, out mLightMapAtlas, bDyn);

			if(mLightMapAtlas == null)
			{
				return;
			}

			Texture2D	lma	=mLightMapAtlas.GetAtlasTexture();
			if(lma != null)
			{
				lma.Name	="LightMapAtlas";
				mMatLib.AddMap("LightMapAtlas", lma);
				mMatLib.RefreshShaderParameters();
			}
		}


		public void BuildVLit(GraphicsDevice g, BuildVLitRenderData brd, object pp, bool bDyn)
		{
			brd(g, out mVLitVB, out mVLitIB, out mVLitDrawCalls, pp, bDyn);
		}


		public void BuildAlpha(GraphicsDevice g, BuildAlphaRenderData brd, object pp, bool bDyn)
		{
			brd(g, out mAlphaVB, out mAlphaIB, out mAlphaDrawCalls, pp, bDyn);
		}


		public void BuildFullBright(GraphicsDevice g, BuildFullBrightRenderData brd, object pp)
		{
			brd(g, out mFBVB, out mFBIB, out mFBDrawCalls, pp);
		}


		public void BuildMirror(GraphicsDevice g, BuildMirrorRenderData brd, object pp, bool bDyn)
		{
			brd(g, out mMirrorVB, out mMirrorIB, out mMirrorDrawCalls, out mMirrorPolys, pp, bDyn);
		}


		public void BuildSky(GraphicsDevice g, BuildSkyRenderData brd, object pp)
		{
			brd(g, out mSkyVB, out mSkyIB, out mSkyDrawCalls, pp);
		}


		public void Update(float msDelta)
		{
			UpdateAnimatedLightMaps(msDelta);
		}


		//draw depth, material id, and normal
		//assumes the proper target is set
		public void DrawDMN(GraphicsDevice gd, Vector3 viewPos,
			GameCamera gameCam,
			IsMaterialVisible bMatVis,
			GetModelMatrix getModMatrix,
			RenderExternalDMN rendExternalDMN)
		{
			//update materiallib wvp
			mMatLib.UpdateWVP(Matrix.Identity, gameCam.View, gameCam.Projection, viewPos);

			//draw solids first
//			DrawMaterialsDC(gd, viewPos, 2, getModMatrix, mFBVB, mFBIB, mFBDrawCalls, bMatVis);
//			DrawMaterialsDC(gd, viewPos, 2, getModMatrix, mVLitVB, mVLitIB, mVLitDrawCalls, bMatVis);
//			DrawMaterialsDC(gd, viewPos, 2, getModMatrix, mSkyVB, mSkyIB, mSkyDrawCalls, bMatVis);
			DrawMaterialsDC(gd, viewPos, 2, getModMatrix, mLMVB, mLMIB, mLMDrawCalls, bMatVis);
//			DrawMaterialsDC(gd, viewPos, 2, getModMatrix, mLMAnimVB, mLMAnimIB, mLMAnimDrawCalls, bMatVis);

			//draw alphas
//			DrawMaterialsDC(gd, viewPos, 2, getModMatrix, mAlphaVB, mAlphaIB, mAlphaDrawCalls, bMatVis);
//			DrawMaterialsDC(gd, viewPos, 2, getModMatrix, mLMAVB, mLMAIB, mLMADrawCalls, bMatVis);
//			DrawMaterialsDC(gd, viewPos, 2, getModMatrix, mLMAAnimVB, mLMAAnimIB, mLMAAnimDrawCalls, bMatVis);

			//draw outside stuff
			rendExternalDMN(viewPos, gameCam.View, gameCam.Projection);
		}


		//helpful overload for doing vis testing
		public void Draw(GraphicsDevice gd, Vector3 viewPos,
			GameCamera gameCam, int numShadows,
			IsMaterialVisible bMatVis,
			GetModelMatrix getModMatrix,
			RenderExternal rendExternal,
			MaterialLib.AlphaPool.RenderShadows renderShadows)
		{
			//update materiallib wvp
			mMatLib.UpdateWVP(Matrix.Identity, gameCam.View, gameCam.Projection, viewPos);

//			gd.Clear(Color.CornflowerBlue);

			//draw solids first
//			DrawMaterialsDC(gd, viewPos, 0, getModMatrix, mFBVB, mFBIB, mFBDrawCalls, bMatVis);
//			DrawMaterialsDC(gd, viewPos, 0, getModMatrix, mVLitVB, mVLitIB, mVLitDrawCalls, bMatVis);
//			DrawMaterialsDC(gd, viewPos, 0, getModMatrix, mSkyVB, mSkyIB, mSkyDrawCalls, bMatVis);
			DrawMaterialsDC(gd, viewPos, 0, getModMatrix, mLMVB, mLMIB, mLMDrawCalls, bMatVis);
//			DrawMaterialsDC(gd, viewPos, 0, getModMatrix, mLMAnimVB, mLMAnimIB, mLMAnimDrawCalls, bMatVis);

			//draw shadows
			for(int i=0;i < numShadows;i++)
			{
				//draw shad and set up materials for second pass
				renderShadows(i);

				//draw second pass with shadowing
//				DrawMaterialsDC(gd, viewPos, 1, getModMatrix, mFBVB, mFBIB, mFBDrawCalls, bMatVis);
//				DrawMaterialsDC(gd, viewPos, 1, getModMatrix, mVLitVB, mVLitIB, mVLitDrawCalls, bMatVis);
				DrawMaterialsDC(gd, viewPos, 1, getModMatrix, mLMVB, mLMIB, mLMDrawCalls, bMatVis);
//				DrawMaterialsDC(gd, viewPos, 1, getModMatrix, mLMAnimVB, mLMAnimIB, mLMAnimDrawCalls, bMatVis);
			}

			//draw alphas
//			DrawMaterialsDC(gd, viewPos, 0, getModMatrix, mAlphaVB, mAlphaIB, mAlphaDrawCalls, bMatVis);
//			DrawMaterialsDC(gd, viewPos, 0, getModMatrix, mLMAVB, mLMAIB, mLMADrawCalls, bMatVis);
//			DrawMaterialsDC(gd, viewPos, 0, getModMatrix, mLMAAnimVB, mLMAAnimIB, mLMAAnimDrawCalls, bMatVis);

			//draw outside stuff
			rendExternal(mAlphaPool, viewPos, gameCam.View, gameCam.Projection);
			mAlphaPool.DrawAll(gd, mMatLib, viewPos, numShadows, renderShadows);
		}


		public void Draw(GraphicsDevice gd,
			GameCamera gameCam, int numShadows,
			IsMaterialVisible bMatVis,
			GetModelMatrix getModMatrix,
			RenderExternal rendExternal,
			MaterialLib.AlphaPool.RenderShadows rendShads)
		{
			Draw(gd, gameCam.Position, gameCam, numShadows, bMatVis, getModMatrix, rendExternal, rendShads);
		}


		public float GetStyleStrength(int styleIndex)
		{
			if(styleIndex <= 0)
			{
				return	1f;	//0 is always fully on
			}

			if(styleIndex > 12)
			{
				return	1f;	//only 12 animated, switchable handled elsewhere
			}

			return	ComputeStyleStrength(mCurStylePos[styleIndex], mStyles[styleIndex]);
		}


		float ComputeStyleStrength(float stylePos, string styleString)
		{
			int	curPos	=(int)Math.Floor(stylePos / ThirtyFPS);

			float	val		=StyleVal(styleString.Substring(curPos, 1));
			float	nextVal	=StyleVal(styleString.Substring((curPos + 1) % styleString.Length, 1));

			float	ratio	=stylePos - (curPos * ThirtyFPS);

			ratio	/=ThirtyFPS;

			return	MathHelper.Lerp(val, nextVal, ratio);
		}


		//for opaques with models
		void DrawMaterialsDC(GraphicsDevice g, Vector3 eyePos,
			int pass, GetModelMatrix getModMatrix,
			VertexBuffer vb, IndexBuffer ib, Dictionary<int, List<DrawCall>> dcs,
			IsMaterialVisible bMatVis)
		{
			if(vb == null)
			{
				return;
			}

			Dictionary<string, MaterialLib.Material>	mats	=mMatLib.GetMaterials();

			g.SetVertexBuffer(vb);
			g.Indices	=ib;

			//cycle through models
			foreach(KeyValuePair<int, List<DrawCall>> modCall in dcs)
			{
				int	idx	=0;

				foreach(KeyValuePair<string, MaterialLib.Material> mat in mats)
				{
					DrawCall	call	=modCall.Value[idx];
					if(call.mPrimCount == 0)
					{
						idx++;
						continue;
					}

					Effect		fx	=mMatLib.GetShader(mat.Value.ShaderName);
					if(fx == null)
					{
						idx++;
						continue;
					}
					if(modCall.Key == 0)
					{
						if(!bMatVis(eyePos, idx))
						{
							idx++;
							continue;
						}
					}

					if(fx.CurrentTechnique.Passes.Count <= pass)
					{
						idx++;
						continue;
					}

					mat.Value.ApplyShaderParameters(fx);

					//set renderstates from material
					if(pass == 0)
					{
						mat.Value.ApplyRenderStates(g);
					}

					//set world mat from model transforms
					if(getModMatrix != null)
					{
						fx.Parameters["mWorld"].SetValue(getModMatrix(modCall.Key));
					}

					fx.CurrentTechnique.Passes[pass].Apply();

					g.DrawIndexedPrimitives(PrimitiveType.TriangleList,
						0, call.mMinVertIndex, call.mNumVerts, call.mStartIndex, call.mPrimCount);
					idx++;
				}
			}
		}


		//this one is for alphas with models
		void DrawMaterialsDC(GraphicsDevice g, Vector3 eyePos,
			int pass, GetModelMatrix getModMatrix,
			VertexBuffer vb, IndexBuffer ib, Dictionary<int, List<List<DrawCall>>> dcs,
			IsMaterialVisible bMatVis)
		{
			if(vb == null)
			{
				return;
			}

			if(pass == 3)
			{
				g.SetVertexBuffer(vb);
				g.Indices	=ib;
			}

			Dictionary<string, MaterialLib.Material>	mats	=mMatLib.GetMaterials();

			//cycle through models
			foreach(KeyValuePair<int, List<List<DrawCall>>> modCall in dcs)
			{
				int	idx	=0;

				foreach(KeyValuePair<string, MaterialLib.Material> mat in mats)
				{
					if(modCall.Value.Count == 0)
					{
						idx++;
						continue;
					}
					if(modCall.Value[idx].Count == 0)
					{
						idx++;
						continue;
					}

					Effect		fx	=mMatLib.GetShader(mat.Value.ShaderName);
					if(fx == null)
					{
						idx++;
						continue;
					}
					if(modCall.Key == 0)
					{
						if(!bMatVis(eyePos, idx))
						{
							idx++;
							continue;
						}
					}

					Matrix	modMat	=getModMatrix(modCall.Key);
					if(pass == 3)
					{
						fx.Parameters["mWorld"].SetValue(modMat);
					}

					foreach(DrawCall dc in modCall.Value[idx])
					{
						if(dc.mPrimCount <= 0)
						{
							continue;
						}

						if(pass != 3)
						{
							mAlphaPool.StoreDraw(dc.mSortPoint, mat.Value,
								vb, ib, modMat, 0, dc.mMinVertIndex, dc.mNumVerts,
								dc.mStartIndex, dc.mPrimCount);
						}
						else
						{
							//material depth normal pass draws directly
							g.DrawIndexedPrimitives(PrimitiveType.TriangleList,
								0, dc.mMinVertIndex, dc.mNumVerts,
								dc.mStartIndex, dc.mPrimCount);
						}
					}
					idx++;
				}
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

			for(int i=0;i < 12;i++)
			{
				mCurStylePos[i]	+=msDelta;

				float	endTime	=mStyles[i].Length * ThirtyFPS;

				while(mCurStylePos[i] >= endTime)
				{
					mCurStylePos[i]	-=endTime;
				}

				float	lerped	=ComputeStyleStrength(mCurStylePos[i], mStyles[i]);

				mAniIntensities[i]	=lerped;
			}

			//switchable lights
			for(int i=0;i < 32;i++)
			{
				mAniIntensities[12 + i]	=((mSwitches[i])? 1.0f : 0.0f);
			}

			foreach(KeyValuePair<string, MaterialLib.Material> mat in mats)
			{
				if(mat.Key.EndsWith("Anim"))
				{
					mat.Value.AddParameter("mAniIntensities",
						EffectParameterClass.Scalar,
						EffectParameterType.Single, 44,
						mAniIntensities);
				}
			}
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
		public void Read(GraphicsDevice g, string fileName, bool bEditor, bool bReach)
		{
			Stream			file	=null;
			if(bEditor)
			{
				file	=new FileStream(fileName, FileMode.Open, FileAccess.Read);
			}
			else
			{
				file	=FileUtil.OpenTitleFile(fileName);
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

				Texture2D	lma	=mLightMapAtlas.GetAtlasTexture();

				lma.Name	="LightMapAtlas";
				mMatLib.AddMap("LightMapAtlas", lma);
			}


			int	numVerts	=br.ReadInt32();
			if(numVerts != 0)
			{
				int	typeIdx	=br.ReadInt32();
				VertexTypes.ReadVerts(br, g, out mLMVB, numVerts, typeIdx, bEditor);
				FileUtil.ReadIndexBuffer(br, out mLMIB, g, bEditor, bReach);
			}

			numVerts	=br.ReadInt32();
			if(numVerts != 0)
			{
				mVLitTypeIdx	=br.ReadInt32();
				VertexTypes.ReadVerts(br, g, out mVLitVB, numVerts, mVLitTypeIdx, bEditor);
				FileUtil.ReadIndexBuffer(br, out mVLitIB, g, bEditor, bReach);
			}

			numVerts	=br.ReadInt32();
			if(numVerts != 0)
			{
				int	typeIdx		=br.ReadInt32();
				VertexTypes.ReadVerts(br, g, out mLMAnimVB, numVerts, typeIdx, bEditor);
				FileUtil.ReadIndexBuffer(br, out mLMAnimIB, g, bEditor, bReach);
			}

			numVerts	=br.ReadInt32();
			if(numVerts != 0)
			{
				int	typeIdx		=br.ReadInt32();
				VertexTypes.ReadVerts(br, g, out mAlphaVB, numVerts, typeIdx, bEditor);
				FileUtil.ReadIndexBuffer(br, out mAlphaIB, g, bEditor, bReach);
			}

			numVerts	=br.ReadInt32();
			if(numVerts != 0)
			{
				int	typeIdx		=br.ReadInt32();
				VertexTypes.ReadVerts(br, g, out mSkyVB, numVerts, typeIdx, bEditor);
				FileUtil.ReadIndexBuffer(br, out mSkyIB, g, bEditor, bReach);
			}

			numVerts	=br.ReadInt32();
			if(numVerts != 0)
			{
				int	typeIdx		=br.ReadInt32();
				VertexTypes.ReadVerts(br, g, out mFBVB, numVerts, typeIdx, bEditor);
				FileUtil.ReadIndexBuffer(br, out mFBIB, g, bEditor, bReach);
			}

			numVerts	=br.ReadInt32();
			if(numVerts != 0)
			{
				int	typeIdx		=br.ReadInt32();
				VertexTypes.ReadVerts(br, g, out mMirrorVB, numVerts, typeIdx, bEditor);
				FileUtil.ReadIndexBuffer(br, out mMirrorIB, g, bEditor, bReach);
			}

			numVerts	=br.ReadInt32();
			if(numVerts != 0)
			{
				int	typeIdx		=br.ReadInt32();
				VertexTypes.ReadVerts(br, g, out mLMAVB, numVerts, typeIdx, bEditor);
				FileUtil.ReadIndexBuffer(br, out mLMAIB, g, bEditor, bReach);
			}

			numVerts	=br.ReadInt32();
			if(numVerts != 0)
			{
				int	typeIdx		=br.ReadInt32();
				VertexTypes.ReadVerts(br, g, out mLMAAnimVB, numVerts, typeIdx, bEditor);
				FileUtil.ReadIndexBuffer(br, out mLMAAnimIB, g, bEditor, bReach);
			}

			mLMDrawCalls		=DrawCall.ReadDrawCallDict(br);
			mVLitDrawCalls		=DrawCall.ReadDrawCallDict(br);
			mLMAnimDrawCalls	=DrawCall.ReadDrawCallDict(br);
			mSkyDrawCalls		=DrawCall.ReadDrawCallDict(br);
			mFBDrawCalls		=DrawCall.ReadDrawCallDict(br);
			mMirrorDrawCalls	=DrawCall.ReadDrawCallDict(br);

			mLMADrawCalls		=DrawCall.ReadDrawCallAlphaDict(br);
			mAlphaDrawCalls		=DrawCall.ReadDrawCallAlphaDict(br);
			mLMAAnimDrawCalls	=DrawCall.ReadDrawCallAlphaDict(br);
			
			int	mirrorCount	=br.ReadInt32();
			for(int i=0;i < mirrorCount;i++)
			{
				Vector3	[]verts	=FileUtil.ReadVecArray(br);

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
				FileUtil.WriteIndexBuffer(bw, ib);
			}
		}


		public void Write(string fileName)
		{
			FileStream	file	=new FileStream(fileName, FileMode.Create, FileAccess.Write);

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
			DrawCall.WriteDrawCallDict(bw, mLMDrawCalls);
			DrawCall.WriteDrawCallDict(bw, mVLitDrawCalls);
			DrawCall.WriteDrawCallDict(bw, mLMAnimDrawCalls);
			DrawCall.WriteDrawCallDict(bw, mSkyDrawCalls);
			DrawCall.WriteDrawCallDict(bw, mFBDrawCalls);
			DrawCall.WriteDrawCallDict(bw, mMirrorDrawCalls);

			//alphas
			DrawCall.WriteDrawCallAlphaDict(bw, mLMADrawCalls);
			DrawCall.WriteDrawCallAlphaDict(bw, mAlphaDrawCalls);
			DrawCall.WriteDrawCallAlphaDict(bw, mLMAAnimDrawCalls);

			//mirror polys
			bw.Write(mMirrorPolys.Count);
			for(int i=0;i < mMirrorPolys.Count;i++)
			{
				FileUtil.WriteArray(bw, mMirrorPolys[i].ToArray());
			}

			bw.Close();
			file.Close();
		}
		#endregion
	}
}