using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
		MeshLib.IndoorMesh		mIndoorMesh;

		//debug draw stuff
		BasicEffect				mMapEffect;
		VertexBuffer			mVB, mLineVB;
		IndexBuffer				mIB, mLineIB;
		VertexDeclaration		mVD;
		UtilityLib.GameCamera	mGameCam;
		SpriteFont				mKoot;
		int						mNumVerts, mNumLines;
		int						mNumTris;
		Vector2					mTextPos;
		Random					mRnd	=new Random();
		string					mDrawChoice;
		Vector3					mDynamicLightPos;

		//working?
		bool	mbWorking;

		//collision debuggery
		Vector3				mStart, mEnd;
		BasicEffect			mBFX;
		VertexBuffer		mRayVB;
		IndexBuffer			mRayIB;


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

			mGameCam	=new UtilityLib.GameCamera(mGDM.GraphicsDevice.Viewport.Width,
				mGDM.GraphicsDevice.Viewport.Height,
				mGDM.GraphicsDevice.Viewport.AspectRatio);

			base.Initialize();
		}


		protected override void LoadContent()
		{
			mSB		=new SpriteBatch(GraphicsDevice);
			mMatLib	=new MaterialLib.MaterialLib(mGDM.GraphicsDevice, Content);

			mIndoorMesh	=new MeshLib.IndoorMesh(GraphicsDevice, mMatLib);

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
			mMainForm.eGenerateMaterials	+=OnGenerateMaterials;
			mMainForm.eBuildGBSP			+=OnBuildGBSP;
			mMainForm.eSaveGBSP				+=OnSaveGBSP;
			mMainForm.eSaveZone				+=OnSaveZone;
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
			if(mbWorking && !mMainForm.DrawNWork)
			{
				base.Update(gameTime);
				return;
			}

			float	msDelta	=gameTime.ElapsedGameTime.Milliseconds;

			KeyboardState	kbs	=Keyboard.GetState();

			if(kbs.IsKeyDown(Keys.L))
			{
				mDynamicLightPos	=-mGameCam.CamPos;
				mMatLib.SetParameterOnAll("mLight0Position", mDynamicLightPos);
			}

			mIndoorMesh.Update(msDelta);

			mGameCam.Update(msDelta, kbs, Mouse.GetState());

			mMapEffect.World		=mGameCam.World;
			mMapEffect.View			=mGameCam.View;
			mMapEffect.Projection	=mGameCam.Projection;

			mBFX.World		=mGameCam.World;
			mBFX.View		=mGameCam.View;
			mBFX.Projection	=mGameCam.Projection;

			mMatLib.UpdateWVP(mGameCam.World, mGameCam.View, mGameCam.Projection, -mGameCam.CamPos);

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


		protected override void Draw(GameTime gameTime)
		{
			if(mbWorking && !mMainForm.DrawNWork)
			{
				return;
			}

			GraphicsDevice	g	=mGDM.GraphicsDevice;

			GraphicsDevice.RenderState.DepthBufferEnable	=true;

			if(mMap != null)
			{
				mIndoorMesh.Draw(g, mGameCam, mMap.IsMaterialVisibleFromPos);
			}
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
					RegisterMapEvents(false);
				}
				mMap	=new Map();
				RegisterMapEvents(true);
				mMap.LoadBrushFile(fileName);
				mMainForm.SetBuildEnabled(true);
				mMainForm.SetZoneSaveEnabled(false);
				mMainForm.SetSaveEnabled(false);
			}
		}


		void OnBuildGBSP(object sender, EventArgs ea)
		{
			mbWorking	=true;
			mMainForm.EnableFileIO(false);
			mMap.BuildTree(mMainForm.BSPParameters);
		}


		Vector3 EmissiveForMaterial(string matName)
		{
			MaterialLib.Material	mat	=mMatLib.GetMaterial(matName);
			if(mat == null)
			{
				return	Vector3.One;
			}
			return	mat.Emissive.ToVector3();
		}


		void RegisterMapEvents(bool bReg)
		{
			if(bReg)
			{
				mMap.eNumPortalsChanged			+=OnNumPortalsChanged;
				mMap.eNumClustersChanged		+=OnNumClustersChanged;
				mMap.eNumPlanesChanged			+=OnNumPlanesChanged;
				mMap.eNumVertsChanged			+=OnNumVertsChanged;
				mMap.eBuildDone					+=OnBuildDone;
				mMap.eLightDone					+=OnLightDone;
				mMap.eVisDone					+=OnVisDone;
				mMap.eGBSPSaveDone				+=OnGBSPSaveDone;
			}
			else
			{
				mMap.eNumPortalsChanged			-=OnNumPortalsChanged;
				mMap.eNumClustersChanged		-=OnNumClustersChanged;
				mMap.eNumPlanesChanged			-=OnNumPlanesChanged;
				mMap.eNumVertsChanged			-=OnNumVertsChanged;
				mMap.eBuildDone					-=OnBuildDone;
				mMap.eLightDone					-=OnLightDone;
				mMap.eVisDone					-=OnVisDone;
				mMap.eGBSPSaveDone				-=OnGBSPSaveDone;
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
					RegisterMapEvents(false);
				}
				mMainForm.SetSaveEnabled(false);
				mMainForm.SetBuildEnabled(false);
				mMainForm.SetZoneSaveEnabled(false);
				mbWorking	=true;
				mMainForm.EnableFileIO(false);
				mMap	=new Map();
				RegisterMapEvents(true);

				ProgressWatcher.eProgressUpdated	+=OnProgressUpdated;

				mMap.LightGBSPFile(fileName, EmissiveForMaterial,
					mMainForm.LightParameters,
					mMainForm.BSPParameters,
					mMainForm.VisParameters);
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
					RegisterMapEvents(false);
				}
				mMainForm.SetSaveEnabled(false);
				mMainForm.SetBuildEnabled(false);
				mMainForm.SetZoneSaveEnabled(false);
				mbWorking	=true;
				mMainForm.EnableFileIO(false);
				mMap	=new Map();
				RegisterMapEvents(true);

				ProgressWatcher.eProgressUpdated	+=OnProgressUpdated;

				mMap.VisGBSPFile(fileName, mMainForm.VisParameters, mMainForm.BSPParameters);
			}
		}


		void OnGenerateMaterials(object sender, EventArgs ea)
		{
			string	fileName	=sender as string;

			if(fileName != null)
			{
				if(mMap != null)
				{
					//unregister old events
					RegisterMapEvents(false);
				}
				mMainForm.SetSaveEnabled(false);
				mMainForm.SetBuildEnabled(false);
				mMainForm.SetZoneSaveEnabled(false);
				mbWorking	=true;
				mMainForm.EnableFileIO(false);
				mMap	=new Map();
				RegisterMapEvents(true);

				mMatLib.NukeAllMaterials();
				List<MaterialLib.Material>	mats	=mMap.GenerateMaterials(fileName);

				foreach(MaterialLib.Material mat in mats)
				{
					mMatLib.AddMaterial(mat);
				}
				mMatLib.RefreshShaderParameters();
				mMatForm.UpdateMaterials();

				mbWorking	=false;
				mMainForm.EnableFileIO(true);	//not threaded
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
					RegisterMapEvents(false);
				}
				mMainForm.SetSaveEnabled(false);
				mMainForm.SetBuildEnabled(false);
				mMainForm.SetZoneSaveEnabled(false);
				mbWorking	=true;
				mMainForm.EnableFileIO(false);
				mMap	=new Map();

				RegisterMapEvents(true);

				GFXHeader	hdr	=mMap.LoadGBSPFile(fileName);

				if(hdr == null)
				{
					OnMapPrint("Load failed\n", null);
				}
				else
				{
					GraphicsDevice	g	=mGDM.GraphicsDevice;

					mMatLib.NukeAllMaterials();

					List<MaterialLib.Material>	mats	=mMap.GetMaterials();

					mIndoorMesh.BuildLM(g, mMainForm.LightParameters.mAtlasSize, mMap.BuildLMRenderData);
					mIndoorMesh.BuildVLit(g, mMap.BuildVLitRenderData);
					mIndoorMesh.BuildAlpha(g, mMap.BuildAlphaRenderData);
					mIndoorMesh.BuildFullBright(g, mMap.BuildFullBrightRenderData);
					mIndoorMesh.BuildMirror(g, mMap.BuildMirrorRenderData);
					mIndoorMesh.BuildSky(g, mMap.BuildSkyRenderData);

					foreach(MaterialLib.Material mat in mats)
					{
						mMatLib.AddMaterial(mat);
					}
					mMatLib.RefreshShaderParameters();
					mMatForm.UpdateMaterials();
					mMainForm.SetZoneSaveEnabled(true);
				}
				mbWorking	=false;
				mMainForm.EnableFileIO(true);
			}
		}


		void OnSaveGBSP(object sender, EventArgs ea)
		{
			string	fileName	=sender as string;

			if(fileName != null)
			{
				mbWorking	=true;
				mMainForm.EnableFileIO(false);
				mMap.SaveGBSPFile(fileName,	mMainForm.BSPParameters);
			}
		}


		void OnSaveZone(object sender, EventArgs ea)
		{
			string	fileName	=sender as string;

			if(fileName != null)
			{
				mMap.Write(fileName);

				//write out the zoneDraw
				mIndoorMesh.Write(fileName + "Draw");
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
			//might need to readd lightmap tex
		}


		void OnDrawChoiceChanged(object sender, EventArgs ea)
		{
			string	choice	=sender as string;

			mDrawChoice	=choice;

			MakeDrawData();
		}


		void OnNumClustersChanged(object sender, EventArgs ea)
		{
			int	num	=(int)sender;

			mMainForm.NumberOfClusters	="" + num;
		}


		void OnNumVertsChanged(object sender, EventArgs ea)
		{
			int	num	=(int)sender;

			mMainForm.NumberOfVerts	="" + num;
		}


		void OnNumPortalsChanged(object sender, EventArgs ea)
		{
			int	num	=(int)sender;

			mMainForm.NumberOfPortals	="" + num;
		}


		void OnNumPlanesChanged(object sender, EventArgs ea)
		{
			int	num	=(int)sender;

			mMainForm.NumberOfPlanes	="" + num;
		}


		void OnBuildDone(object sender, EventArgs ea)
		{
			bool	bSuccess	=(bool)sender;

			mbWorking	=false;
			mMainForm.EnableFileIO(true);
			mMainForm.SetSaveEnabled(true);
			mMainForm.SetBuildEnabled(false);
		}


		void OnLightDone(object sender, EventArgs ea)
		{
			bool	bSuccess	=(bool)sender;

			ProgressWatcher.eProgressUpdated	-=OnProgressUpdated;
			mMainForm.ClearProgress();
			mbWorking	=false;
			mMainForm.EnableFileIO(true);
		}


		void OnVisDone(object sender, EventArgs ea)
		{
			bool	bSuccess	=(bool)sender;

			ProgressWatcher.eProgressUpdated	-=OnProgressUpdated;
			mMainForm.ClearProgress();
			mbWorking	=false;
			mMainForm.EnableFileIO(true);
		}


		void OnGBSPSaveDone(object sender, EventArgs ea)
		{
			bool	bSuccess	=(bool)sender;

			mbWorking	=false;
			mMainForm.EnableFileIO(true);
		}


		void OnProgressUpdated(object sender, EventArgs ea)
		{
			ProgressEventArgs	pea	=ea as ProgressEventArgs;

			mMainForm.UpdateProgress(pea);
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