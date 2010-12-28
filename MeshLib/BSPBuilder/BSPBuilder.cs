using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using BSPLib;


namespace BSPBuilder
{
	public class BSPBuilder : Game
	{
		GraphicsDeviceManager	mGDM;
		SpriteBatch				mSB;

		//forms
		MainForm		mMainForm;
		CollisionForm	mCollForm;
		MaterialForm	mMatForm;

		//data
		Map						mMap;
		MaterialLib.MaterialLib	mMatLib;

		//debug draw stuff
		BasicEffect				mMapEffect;
		VertexBuffer			mVB, mLineVB;
		IndexBuffer				mIB, mLineIB;
		VertexDeclaration		mVD;
		GameCamera				mGameCam;
		SpriteFont				mKoot;
		int						mNumVerts, mNumLines;
		int						mNumTris;
		Vector2					mTextPos;
		Random					mRnd	=new Random();
		string					mDrawChoice;

		//new debug draw stuff
		VertexBuffer		mLMVB, mVLitVB, mLMAnimVB, mAlphaVB, mSkyVB, mFBVB, mMirrorVB, mLMAVB, mLMAAnimVB;
		VertexDeclaration	mLMVD, mVLitVD, mLMAnimVD, mAlphaVD, mSkyVD, mFBVD, mMirrorVD, mLMAVD, mLMAAnimVD;
		IndexBuffer			mLMIB, mVLitIB, mLMAnimIB, mAlphaIB, mSkyIB, mFBIB, mMirrorIB, mLMAIB, mLMAAnimIB;
		TexAtlas			mLMapAtlas;
		Int32				mDebugLeaf;

		//for sorting alphas
		MaterialLib.AlphaPool	mAlphaPool	=new MaterialLib.AlphaPool();

		//debug lightmap animation stuff
		Dictionary<int, string>	mStyles			=new Dictionary<int, string>();
		Dictionary<int, float>	mCurStylePos	=new Dictionary<int, float>();

		//material draw stuff
		//offsets into the vbuffer per material
		Int32[]	mLMMatOffsets, mVLitMatOffsets, mLMAnimMatOffsets;
		Int32[]	mAlphaMatOffsets, mSkyMatOffsets, mFBMatOffsets, mMirrorMatOffsets;
		Int32[]	mLMAMatOffsets, mLMAAnimMatOffsets;

		//numverts for drawprim call per material
		Int32[]	mLMMatNumVerts, mVLitMatNumVerts, mLMAnimMatNumVerts;
		Int32[] mAlphaNumVerts, mSkyNumVerts, mFBNumVerts, mMirrorNumVerts;
		Int32[]	mLMAMatNumVerts, mLMAAnimMatNumVerts;

		//primcount per material
		Int32[]	mLMMatNumTris, mVLitMatNumTris, mLMAnimMatNumTris;
		Int32[]	mAlphaNumTris, mSkyNumTris, mFBNumTris, mMirrorNumTris;
		Int32[]	mLMAMatNumTris, mLMAAnimMatNumTris;

		//sort points for alphas
		Vector3[] mLMASortPoints, mAlphaSortPoints, mMirrorSortPoints, mLMAAnimSortPoints;

		//collision debuggery
		Vector3				mStart, mEnd;
		BasicEffect			mBFX;
		VertexBuffer		mRayVB;
		IndexBuffer			mRayIB;

		//constants
		const float		ThirtyFPS	=(1000.0f / 30.0f);


		public BSPBuilder()
		{
			mGDM	=new GraphicsDeviceManager(this);
			Content.RootDirectory	="Content";

			//set window position
			if(!mGDM.IsFullScreen)
			{
				System.Windows.Forms.Control	mainWindow
					=System.Windows.Forms.Form.FromHandle(this.Window.Handle);

				//add data binding so it will save
				mainWindow.DataBindings.Add(new System.Windows.Forms.Binding("Location",
					global::BSPBuilder.Properties.Settings.Default,
					"MainWindowPos", true,
					System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));

				mainWindow.Location	=
					global::BSPBuilder.Properties.Settings.Default.MainWindowPos;

				IsMouseVisible	=true;
			}
		}


		protected override void Initialize()
		{
			mTextPos	=Vector2.One * 20.0f;

			mGameCam	=new GameCamera(mGDM.GraphicsDevice.Viewport.Width,
				mGDM.GraphicsDevice.Viewport.Height,
				mGDM.GraphicsDevice.Viewport.AspectRatio);

			//Quake1 styled lights 'a' is total darkness, 'z' is maxbright.
			// 0 normal
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

			base.Initialize();
		}


		protected override void LoadContent()
		{
			mSB		=new SpriteBatch(GraphicsDevice);
			mMatLib	=new MaterialLib.MaterialLib(mGDM.GraphicsDevice, Content);

			mCollForm				=new CollisionForm();
			mCollForm.Visible		=false;
			mCollForm.eStartRay		+=OnStartRay;
			mCollForm.eEndRay		+=OnEndRay;
			mCollForm.eRepeatRay	+=OnRepeatRay;

			mMatForm					=new MaterialForm(mGDM.GraphicsDevice, mMatLib);
			mMatForm.Visible			=true;
			mMatForm.eMaterialNuked		+=OnMaterialNuked;
			mMatForm.eLibraryCleared	+=OnMaterialsCleared;

			mMainForm						=new MainForm();
			mMainForm.Visible				=true;
			mMainForm.eOpenBrushFile		+=OnOpenBrushFile;
			mMainForm.eLightGBSP			+=OnLightGBSP;
			mMainForm.eVisGBSP				+=OnVisGBSP;
			mMainForm.eMaterialVisGBSP		+=OnMaterialVisGBSP;
			mMainForm.eBuildGBSP			+=OnBuildGBSP;
			mMainForm.eSaveGBSP				+=OnSaveGBSP;
			mMainForm.eLoadGBSP				+=OnLoadGBSP;
			mMainForm.eDrawChoiceChanged	+=OnDrawChoiceChanged;

			mBFX					=new BasicEffect(GraphicsDevice, null);
			mBFX.View				=mGameCam.View;
			mBFX.Projection			=mGameCam.Projection;
			mBFX.VertexColorEnabled	=true;

			mKoot	=Content.Load<SpriteFont>("Fonts/Kootenay");

			mVD	=new VertexDeclaration(mGDM.GraphicsDevice,
				VertexPositionColorTexture.VertexElements);

			mMapEffect	=new BasicEffect(mGDM.GraphicsDevice, null);

			mMapEffect.TextureEnabled		=false;
			mMapEffect.DiffuseColor			=Vector3.One;
			mMapEffect.VertexColorEnabled	=true;

			Map.ePrint	+=OnMapPrint;

			//tired of that gump
//			OnOpenVMF("C:\\Users\\kbaird\\Documents\\sdk_arena_lumberyard.vmf", null);
		}


		protected override void UnloadContent()
		{
		}


		protected override void Update(GameTime gameTime)
		{
			if(GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
			{
				this.Exit();
			}
			float	msDelta	=gameTime.ElapsedGameTime.Milliseconds;

			KeyboardState	kbs	=Keyboard.GetState();

			if(kbs.IsKeyDown(Keys.V))
			{
				mDebugLeaf	=mMap.FindNodeLandedIn(0, -mGameCam.CamPos);
				mDebugLeaf	=-(mDebugLeaf + 1);
			}

			UpdateAnimatedLightMaps(msDelta);

			mGameCam.Update(msDelta, kbs, Mouse.GetState());

			mMapEffect.World		=mGameCam.World;
			mMapEffect.View			=mGameCam.View;
			mMapEffect.Projection	=mGameCam.Projection;

			mBFX.World		=mGameCam.World;
			mBFX.View		=mGameCam.View;
			mBFX.Projection	=mGameCam.Projection;

			mMatLib.UpdateWVP(mGameCam.World, mGameCam.View, mGameCam.Projection);

			base.Update(gameTime);
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


		void DrawVLit()
		{
			if(mVLitVB == null)
			{
				return;
			}

			Dictionary<string, MaterialLib.Material>	mats	=mMatLib.GetMaterials();

			GraphicsDevice	g	=mGDM.GraphicsDevice;

			g.VertexDeclaration	=mVLitVD;
			g.Vertices[0].SetSource(mVLitVB, 0, 32);
			g.Indices	=mVLitIB;

			int	idx	=0;

			foreach(KeyValuePair<string, MaterialLib.Material> mat in mats)
			{
				Effect		fx	=mMatLib.GetShader(mat.Value.ShaderName);
				if(fx == null)
				{
					idx++;
					continue;
				}
				if(mVLitMatNumVerts[idx] <= 0)
				{
					idx++;
					continue;
				}
				if(!mMap.IsMaterialVisible(mDebugLeaf, idx))
				{
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
						mVLitMatNumVerts[idx],
						mVLitMatOffsets[idx],
						mVLitMatNumTris[idx]);

					pass.End();
				}
				fx.End();

				idx++;
			}
		}


		void DrawMaterials(VertexBuffer vb, IndexBuffer ib, VertexDeclaration vd,
			int vbStride, Int32 []offsets, Int32 []numVerts, Int32 []numTris,
			bool bAlpha, Vector3 []sortPoints)
		{
			if(vb == null)
			{
				return;
			}

			Dictionary<string, MaterialLib.Material>	mats	=mMatLib.GetMaterials();

			GraphicsDevice	g	=mGDM.GraphicsDevice;

			g.VertexDeclaration	=vd;
			g.Vertices[0].SetSource(vb, 0, vbStride);
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
				if(!mMap.IsMaterialVisible(mDebugLeaf, idx))
				{
					idx++;
					continue;
				}

				if(bAlpha)
				{
					mAlphaPool.StoreDraw(sortPoints[idx], mat.Value,
						vb, ib, vd, vbStride, 0, 0, numVerts[idx],
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


		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice	g	=mGDM.GraphicsDevice;

			g.Clear(Color.CornflowerBlue);

			GraphicsDevice.RenderState.DepthBufferEnable	=true;

			DrawMaterials(mFBVB, mFBIB, mFBVD, 20, mFBMatOffsets, mFBNumVerts, mFBNumTris, false, null);
			DrawMaterials(mVLitVB, mVLitIB, mVLitVD, 32, mVLitMatOffsets, mVLitMatNumVerts, mVLitMatNumTris, false, null);
			DrawMaterials(mSkyVB, mSkyIB, mSkyVD, 20, mSkyMatOffsets, mSkyNumVerts, mSkyNumTris, false, null);
			DrawMaterials(mLMVB, mLMIB, mLMVD, 28, mLMMatOffsets, mLMMatNumVerts, mLMMatNumTris, false, null);
			DrawMaterials(mLMAnimVB, mLMAnimIB, mLMAnimVD, 68, mLMAnimMatOffsets, mLMAnimMatNumVerts, mLMAnimMatNumTris, false, null);

			//alphas
			DrawMaterials(mAlphaVB, mAlphaIB, mAlphaVD, 36, mAlphaMatOffsets, mAlphaNumVerts, mAlphaNumTris, true, mAlphaSortPoints);
			DrawMaterials(mMirrorVB, mMirrorIB, mMirrorVD, 36, mMirrorMatOffsets, mMirrorNumVerts, mMirrorNumTris, true, mMirrorSortPoints);
			DrawMaterials(mLMAVB, mLMAIB, mLMAVD, 44, mLMAMatOffsets, mLMAMatNumVerts, mLMAMatNumTris, true, mLMASortPoints);
			DrawMaterials(mLMAAnimVB, mLMAAnimIB, mLMAAnimVD, 68, mLMAAnimMatOffsets, mLMAAnimMatNumVerts, mLMAAnimMatNumTris, true, mLMAAnimSortPoints);

			mAlphaPool.DrawAll(g, mMatLib, -mGameCam.CamPos);
			
			if(mVB != null)
			{
				g.VertexDeclaration		=mVD;
				g.Vertices[0].SetSource(mVB, 0, VertexPositionColorTexture.SizeInBytes);
				g.Indices	=mIB;

				g.RenderState.DepthBufferEnable	=true;

				if(mDrawChoice == "Portals")
				{
					g.RenderState.AlphaBlendEnable	=true;
				}

				mMapEffect.Begin();
				foreach(EffectPass pass in mMapEffect.CurrentTechnique.Passes)
				{
					pass.Begin();

					g.DrawIndexedPrimitives(PrimitiveType.TriangleList,
						0, 0, mNumVerts, 0, mNumTris);

					pass.End();
				}
				mMapEffect.End();

				if(mDrawChoice == "Portals")
				{
					g.RenderState.CullMode	=CullMode.CullClockwiseFace;
					mMapEffect.Begin();
					foreach(EffectPass pass in mMapEffect.CurrentTechnique.Passes)
					{
						pass.Begin();

						g.DrawIndexedPrimitives(PrimitiveType.TriangleList,
							0, 0, mNumVerts, 0, mNumTris);

						pass.End();
					}
					mMapEffect.End();

					g.RenderState.CullMode			=CullMode.CullCounterClockwiseFace;
					g.RenderState.AlphaBlendEnable	=false;
				}
			}

			//draw ray pieces if any
			/*
			if(mRayVB != null && mRayParts.Count > 0)
			{
				GraphicsDevice.Vertices[0].SetSource(mRayVB, 0, 16);
				GraphicsDevice.VertexDeclaration	=mVD;
				GraphicsDevice.Indices				=mRayIB;

				mBFX.Begin();
				foreach(EffectPass ep in mBFX.CurrentTechnique.Passes)
				{
					ep.Begin();

					GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.LineList,
						0, 0, mRayParts.Count * 2, 0, mRayParts.Count);

					ep.End();
				}
				mBFX.End();
			}*/

			//draw portal lines if any
			/*if(mLineVB != null)
			{
				GraphicsDevice.Vertices[0].SetSource(mLineVB, 0, VertexPositionColorTexture.SizeInBytes);
				GraphicsDevice.VertexDeclaration	=mVD;
				GraphicsDevice.Indices				=mLineIB;

				//draw over anything
				GraphicsDevice.RenderState.DepthBufferEnable	=false;

				mBFX.Begin();
				foreach(EffectPass ep in mBFX.CurrentTechnique.Passes)
				{
					ep.Begin();

					GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.LineList,
						0, 0, mNumLines * 2, 0, mNumLines);

					ep.End();
				}
				mBFX.End();

				//turn zbuffer back on
				GraphicsDevice.RenderState.DepthBufferEnable	=true;
			}
			*/

			KeyboardState	kbstate	=Keyboard.GetState();
			if(kbstate.IsKeyDown(Keys.L))
			{
				mMatLib.DrawMap("LightMapAtlas", mSB);
			}

			mSB.Begin();

			mSB.DrawString(mKoot, "Coordinates: " + -mGameCam.CamPos, mTextPos, Color.Yellow);

			mSB.End();

			base.Draw(gameTime);
		}


		void MakeDrawData()
		{
			if(mMap == null)
			{
				return;
			}

			List<Vector3>	verts		=new List<Vector3>();
			List<Vector3>	lineVerts	=new List<Vector3>();
			List<UInt32>	indexes		=new List<UInt32>();
			List<UInt32>	lineIndexes	=new List<UInt32>();

			mMap.GetTriangles(mGameCam.CamPos, verts, indexes, mDrawChoice);
			if(verts.Count <= 0)
			{
				return;
			}
			if(mDrawChoice.StartsWith("Portal"))
			{
//				mMap.GetPortalLines(lineVerts, lineIndexes);
			}

			mNumVerts	=verts.Count;
			mNumTris	=indexes.Count / 3;

			mVB	=new VertexBuffer(mGDM.GraphicsDevice,
				VertexPositionColorTexture.SizeInBytes * verts.Count,
				BufferUsage.WriteOnly);
			mIB	=new IndexBuffer(mGDM.GraphicsDevice,
				4 * indexes.Count, BufferUsage.WriteOnly,
				IndexElementSize.ThirtyTwoBits);

			if(lineVerts.Count > 0)
			{
				mNumLines	=lineVerts.Count / 2;

				mLineVB	=new VertexBuffer(mGDM.GraphicsDevice,
					VertexPositionColorTexture.SizeInBytes * lineVerts.Count,
					BufferUsage.WriteOnly);
				mLineIB	=new IndexBuffer(mGDM.GraphicsDevice,
					4 * lineIndexes.Count, BufferUsage.WriteOnly,
					IndexElementSize.ThirtyTwoBits);
			}
			else
			{
				mNumLines	=0;
				mLineVB		=null;
				mLineIB		=null;
			}

			VertexPositionColorTexture	[]vpnt
				=new VertexPositionColorTexture[mNumVerts];

			int		cnt	=0;
			Color	col	=Color.White;
			foreach(Vector3 vert in verts)
			{
				if((cnt % 6) == 0)
				{
					col	=UtilityLib.Mathery.RandomColor(mRnd);
				}
				vpnt[cnt].Position				=vert;
				vpnt[cnt].Color					=col;
				if(mDrawChoice == "Portals")
				{
					vpnt[cnt].Color.A	=50;
				}

				vpnt[cnt++].TextureCoordinate	=Vector2.Zero;

			}

			mVB.SetData<VertexPositionColorTexture>(vpnt);
			mIB.SetData<UInt32>(indexes.ToArray());

			if(lineVerts.Count <= 0)
			{
				return;
			}

			vpnt	=new VertexPositionColorTexture[lineVerts.Count];

			cnt	=0;
			foreach(Vector3 vert in lineVerts)
			{
				vpnt[cnt].Position			=vert;
				vpnt[cnt].TextureCoordinate	=Vector2.Zero;
				vpnt[cnt].Color				=Color.White;
				cnt++;
			}

			mLineVB.SetData<VertexPositionColorTexture>(vpnt);
			mLineIB.SetData<UInt32>(lineIndexes.ToArray());
		}


		void OnRepeatRay(object sender, EventArgs ea)
		{
			/*
			mCollForm.PrintToConsole("Casting ray: " +
				mStart.X + ", " + mStart.Y + ", " + mStart.Z +
				"  to  " + mEnd.X + ", " + mEnd.Y + ", " + mEnd.Z + "\n");

			Line	ln;
			ln.mP1	=mStart;
			ln.mP2	=mEnd;

			if(!mMap.MoveLine(ref ln, 8.0f))
			{
				ClipSegment	seg	=new ClipSegment();
				seg.mSeg.mP1	=mStart;
				seg.mSeg.mP2	=mEnd;

				mRayParts.Add(seg);

				mCollForm.PrintToConsole("No collision\n");
			}
			else
			{
				mCollForm.PrintToConsole("Collision!\n");
				ClipSegment	seg	=new ClipSegment();
				seg.mSeg.mP1	=mStart;
				seg.mSeg.mP2	=mEnd;
				mRayParts.Add(seg);

				seg	=new ClipSegment();
				seg.mSeg.mP1	=ln.mP1;
				seg.mSeg.mP2	=ln.mP2;
				mRayParts.Add(seg);
			}
			UpdateRayVerts();*/
		}


		void OnStartRay(object sender, EventArgs ea)
		{
			mStart	=mGameCam.CamPos;
		}


		void OnEndRay(object sender, EventArgs ea)
		{
			/*
			mEnd	=mGameCam.CamPos;

			mRayParts.Clear();

//			mStart	=-mStart;
//			mEnd	=-mEnd;

			mStart	=-mStart;
			mEnd	=-mEnd;

			OnRepeatRay(null, null);*/

//			Matrix	transpose	=Matrix.Transpose(mGameCam.View);

//			mStart	=Vector3.Transform(mStart, transpose);
//			mEnd	=Vector3.Transform(mEnd, transpose);

			//hard code from before to repro collision goblinry
			/*
			mStart.X	=-286.2419f;
			mStart.Y	=4.640844f;
			mStart.Z	=-0.1963519f;
			mEnd.X		=337.2641f;
			mEnd.Y		=5.518799f;
			mEnd.Z		=81.22206f;
			*/
			/*
			mMap.RayCast(mStart, mEnd, ref mRayParts);

			if(mRayParts.Count == 0)
			{
				ClipSegment	seg	=new ClipSegment();
				seg.mSeg.mP1	=mStart;
				seg.mSeg.mP2	=mEnd;

				mRayParts.Add(seg);

				mCF.PrintToConsole("No collision\n");
			}
			else
			{
				mCF.PrintToConsole("Ray returned in " + mRayParts.Count + " pieces\n");
			}*/
		}


		void OnOpenBrushFile(object sender, EventArgs ea)
		{
			string	fileName	=sender as string;

			if(fileName != null)
			{
				if(mMap != null)
				{
					//unregister old events
					mMap.eNumCollisionFacesChanged	-=OnNumCollisionFacesChanged;
					mMap.eNumDrawFacesChanged		-=OnNumDrawFacesChanged;
					mMap.eNumMapFacesChanged		-=OnNumMapFacesChanged;
					mMap.eProgressChanged			-=OnMapProgressChanged;
					mMap.eNumPortalsChanged			-=OnNumPortalsChanged;
				}
				mMap	=new Map(fileName);
				mMap.eNumCollisionFacesChanged	+=OnNumCollisionFacesChanged;
				mMap.eNumDrawFacesChanged		+=OnNumDrawFacesChanged;
				mMap.eNumMapFacesChanged		+=OnNumMapFacesChanged;
				mMap.eProgressChanged			+=OnMapProgressChanged;
				mMap.eNumPortalsChanged			+=OnNumPortalsChanged;
				mMainForm.SetBuildEnabled(true);
			}
		}


		void OnBuildGBSP(object sender, EventArgs ea)
		{
			if(mMap.BuildTree(mMainForm.BSPParameters))
			{
				mMainForm.SetSaveEnabled(true);
			}
		}


		void OnLightGBSP(object sender, EventArgs ea)
		{
			string	fileName	=sender as string;

			if(fileName != null)
			{
				if(mMap != null)
				{
					//unregister old events
					mMap.eNumCollisionFacesChanged	-=OnNumCollisionFacesChanged;
					mMap.eNumDrawFacesChanged		-=OnNumDrawFacesChanged;
					mMap.eNumMapFacesChanged		-=OnNumMapFacesChanged;
					mMap.eProgressChanged			-=OnMapProgressChanged;
					mMap.eNumPortalsChanged			-=OnNumPortalsChanged;
				}
				mMap	=new Map();
				mMap.LightGBSPFile(fileName,
					mMainForm.LightParameters,
					mMainForm.BSPParameters);
			}
		}


		void OnVisGBSP(object sender, EventArgs ea)
		{
			string	fileName	=sender as string;

			if(fileName != null)
			{
				if(mMap != null)
				{
					//unregister old events
					mMap.eNumCollisionFacesChanged	-=OnNumCollisionFacesChanged;
					mMap.eNumDrawFacesChanged		-=OnNumDrawFacesChanged;
					mMap.eNumMapFacesChanged		-=OnNumMapFacesChanged;
					mMap.eProgressChanged			-=OnMapProgressChanged;
					mMap.eNumPortalsChanged			-=OnNumPortalsChanged;
				}
				mMap	=new Map();
				mMap.VisGBSPFile(fileName, mMainForm.VisParameters, mMainForm.BSPParameters);
			}
		}


		void OnMaterialVisGBSP(object sender, EventArgs ea)
		{
			string	fileName	=sender as string;

			if(fileName != null)
			{
				if(mMap != null)
				{
					//unregister old events
					mMap.eNumCollisionFacesChanged	-=OnNumCollisionFacesChanged;
					mMap.eNumDrawFacesChanged		-=OnNumDrawFacesChanged;
					mMap.eNumMapFacesChanged		-=OnNumMapFacesChanged;
					mMap.eProgressChanged			-=OnMapProgressChanged;
					mMap.eNumPortalsChanged			-=OnNumPortalsChanged;
				}
				mMap	=new Map();
				mMap.MaterialVisGBSPFile(fileName, mMainForm.VisParameters, mMainForm.BSPParameters);
			}
		}


		void OnLoadGBSP(object sender, EventArgs ea)
		{
			string	fileName	=sender as string;

			if(fileName != null)
			{
				if(mMap != null)
				{
					//unregister old events
					mMap.eNumCollisionFacesChanged	-=OnNumCollisionFacesChanged;
					mMap.eNumDrawFacesChanged		-=OnNumDrawFacesChanged;
					mMap.eNumMapFacesChanged		-=OnNumMapFacesChanged;
					mMap.eProgressChanged			-=OnMapProgressChanged;
					mMap.eNumPortalsChanged			-=OnNumPortalsChanged;
				}
				mMap	=new Map();
				if(!mMap.LoadGBSPFile(fileName))
				{
					OnMapPrint("Load failed\n", null);
				}
				else
				{
					GraphicsDevice	g	=mGDM.GraphicsDevice;

					mMatLib.NukeAllMaterials();

					List<MaterialLib.Material>	mats	=mMap.GetMaterials();

					mMap.BuildLMRenderData(g,
						out mLMVB,
						out mLMIB,
						out mLMVD,
						out mLMMatOffsets,
						out mLMMatNumVerts,
						out mLMMatNumTris,
						out mLMAnimVB,
						out mLMAnimIB,
						out mLMAnimVD,
						out mLMAnimMatOffsets,
						out mLMAnimMatNumVerts,
						out mLMAnimMatNumTris,
						out mLMAVB,
						out mLMAIB,
						out mLMAVD,
						out mLMAMatOffsets,
						out mLMAMatNumVerts,
						out mLMAMatNumTris,
						out mLMASortPoints,
						out mLMAAnimVB,
						out mLMAAnimIB,
						out mLMAAnimVD,
						out mLMAAnimMatOffsets,
						out mLMAAnimMatNumVerts,
						out mLMAAnimMatNumTris,
						out mLMAAnimSortPoints,
						out mLMapAtlas);

					mMap.BuildVLitRenderData(g, out mVLitVB, out mVLitIB,
						out mVLitVD, out mVLitMatOffsets, out mVLitMatNumVerts,
						out mVLitMatNumTris);

					mMap.BuildAlphaRenderData(g, out mAlphaVB, out mAlphaIB, out mAlphaVD,
						out mAlphaMatOffsets, out mAlphaNumVerts, out mAlphaNumTris, out mAlphaSortPoints);

					mMap.BuildFullBrightRenderData(g, out mFBVB, out mFBIB, out mFBVD,
						out mFBMatOffsets, out mFBNumVerts, out mFBNumTris);

					mMap.BuildMirrorRenderData(g, out mMirrorVB, out mMirrorIB, out mMirrorVD,
						out mMirrorMatOffsets, out mMirrorNumVerts, out mMirrorNumTris, out mMirrorSortPoints);

					mMap.BuildSkyRenderData(g, out mSkyVB, out mSkyIB, out mSkyVD,
						out mSkyMatOffsets, out mSkyNumVerts, out mSkyNumTris);

					mMatLib.AddMap("LightMapAtlas", mLMapAtlas.GetAtlasTexture());

					foreach(MaterialLib.Material mat in mats)
					{
						mMatLib.AddMaterial(mat);
					}
					mMatLib.RefreshShaderParameters();
					mMatForm.UpdateMaterials();
				}
			}
		}


		void OnSaveGBSP(object sender, EventArgs ea)
		{
			string	fileName	=sender as string;

			if(fileName != null)
			{
				mMap.SaveGBSPFile(fileName,	mMainForm.BSPParameters);
			}
		}


		void OnMapPrint(object sender, EventArgs ea)
		{
			string	str	=sender as string;

			mMainForm.PrintToConsole(str);
		}


		void OnMaterialNuked(object sender, EventArgs ea)
		{
			//rebuild material vis
			mMap.VisMaterials();
		}


		void OnMaterialsCleared(object sender, EventArgs ea)
		{
			if(mLMapAtlas != null)
			{
				mMatLib.AddMap("LightMapAtlas", mLMapAtlas.GetAtlasTexture());
			}
		}


		void OnDrawChoiceChanged(object sender, EventArgs ea)
		{
			string	choice	=sender as string;

			mDrawChoice	=choice;

			MakeDrawData();
		}


		void OnNumCollisionFacesChanged(object sender, EventArgs ea)
		{
			int	num	=(int)sender;

			mMainForm.NumberOfAreas	="" + num;
		}


		void OnNumDrawFacesChanged(object sender, EventArgs ea)
		{
			int	num	=(int)sender;

			mMainForm.NumberOfNodes	="" + num;
		}


		void OnNumPortalsChanged(object sender, EventArgs ea)
		{
			int	num	=(int)sender;

			mMainForm.NumberOfPortals	="" + num;
		}


		void OnNumMapFacesChanged(object sender, EventArgs ea)
		{
			int	num	=(int)sender;

			mMainForm.NumberOfFaces	="" + num;
		}


		void OnMapProgressChanged(object sender, EventArgs ea)
		{
		}


		void UpdateRayVerts()
		{
			/*
			if(mRayParts == null || mRayParts.Count <= 0)
			{
				return;
			}
			mRayVB	=new VertexBuffer(GraphicsDevice, mRayParts.Count * 2 * 16, BufferUsage.WriteOnly);
			mRayIB	=new IndexBuffer(GraphicsDevice, mRayParts.Count * 2 * 2, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);

			VertexPositionColor	[]verts		=new VertexPositionColor[mRayParts.Count * 2];
			short				[]indexs	=new short[mRayParts.Count * 2];

			int	idx	=0;
			foreach(ClipSegment seg in mRayParts)
			{
				Microsoft.Xna.Framework.Graphics.Color	randColor;

				randColor	=new Microsoft.Xna.Framework.Graphics.Color(
						Convert.ToByte(255),
						Convert.ToByte(255 - idx * 20),
						Convert.ToByte(255 - idx * 20));

				indexs[idx]				=(short)idx;
				verts[idx].Position.X	=seg.mSeg.mP1.X;
				verts[idx].Position.Y	=seg.mSeg.mP1.Y;
				verts[idx].Position.Z	=seg.mSeg.mP1.Z;
				verts[idx++].Color		=randColor;

				indexs[idx]				=(short)idx;
				verts[idx].Position.X	=seg.mSeg.mP2.X;
				verts[idx].Position.Y	=seg.mSeg.mP2.Y;
				verts[idx].Position.Z	=seg.mSeg.mP2.Z;
				verts[idx++].Color		=randColor;

//				Line	ln	=r.mSplitPlane.CreateLine();

//				indexs[idx]				=(short)idx;
//				verts[idx].Position.X	=ln.mP1.X;
//				verts[idx].Position.Y	=ln.mP1.Y;
//				verts[idx].Position.Z	=0.0f;
//				verts[idx++].Color		=Color.Blue;
				
//				indexs[idx]				=(short)idx;
//				verts[idx].Position.X	=ln.mP2.X;
//				verts[idx].Position.Y	=ln.mP2.Y;
//				verts[idx].Position.Z	=0.0f;
//				verts[idx++].Color		=Color.Blue;
			}

			mRayVB.SetData<VertexPositionColor>(verts);
			mRayIB.SetData<short>(indexs);*/
		}
	}
}