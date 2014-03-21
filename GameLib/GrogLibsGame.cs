using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using UtilityLib;
using BSPZone;
using MeshLib;
using UILib;
using ParticleLib;
using PathLib;
using SpriteMapLib;


namespace GameLib
{
	public class GrogLibsGame : Game
	{
		protected GraphicsDeviceManager		mGDM;
		protected SpriteBatch				mSB;
		protected ContentManager			mSLib;
		protected MaterialLib.PostProcess	mPost;
		protected BasicEffect				mBFX;
		protected DynamicLights				mDynLights;
		protected MaterialLib.IDKeeper		mIDKeeper	=new MaterialLib.IDKeeper();

		ParticleBoss	mPB;
		Audio			mAudio	=new Audio();

		//fonts
		protected Dictionary<string, SpriteFont>	mFonts	=new Dictionary<string, SpriteFont>();

		//UI textures
		Dictionary<string, TextureElement>	mUITex	=new Dictionary<string, TextureElement>();

		//menu stuff
		MenuBuilder		mMB;
		QuickOptions	mQOpt;
		Vector2			mScreenCenter;
		string			mMenuFont;

		//camera
		protected GameCamera	mCam;
		int						mResX, mResY;

		//control stuff
		protected Input				mInput		=new Input();
		protected PlayerSteering	mPSteering;
		Mobile						mPathMobile;

		//level stuff
		List<string>			mLevels	=new List<string>();
		protected Zone			mZone;
		protected IndoorMesh	mZoneDraw;
		MaterialLib.MaterialLib	mZoneMats;
		Effect					mBSPFX;

		//pathing stuff
		protected PathGraph	mGraph	=PathGraph.CreatePathGrid();
		
		//statics
		MaterialLib.MaterialLib					mStaticMats;
		Dictionary<string, StaticMeshObject>	mStatics	=new Dictionary<string, StaticMeshObject>();
		Dictionary<ZoneEntity, LightHelper>		mSLHelpers	=new Dictionary<ZoneEntity, LightHelper>();

		//state
		protected GameState	mState	=new GameState();
		Enum				mMenuEnum;

		//helpers
		TriggerHelper		mTHelper	=new TriggerHelper();
		ParticleHelper		mPHelper	=new ParticleHelper();
		AudioHelper			mAHelper	=new AudioHelper();
		StaticHelper		mSHelper	=new StaticHelper();
		IntermissionHelper	mIMHelper	=new IntermissionHelper();
		ShadowHelper		mSHDHelper	=new ShadowHelper();

		//delegates and event handler prototypes
		public delegate void	StateTransitionedTo(object sender, EventArgs ea);
		public delegate void	StateTransitioningTo(object sender, EventArgs ea);
		public delegate void	StateTransitioningFrom(object sender, EventArgs ea);
		public delegate void	MenuInvoke(object sender, EventArgs ea);
		public delegate void	MenuNavigating(object sender, EventArgs ea);
		public delegate int		GetNumShadowsToDraw();

		//delegate instances for groglibs stuff
		GetNumShadowsToDraw							mGetNumShadows;
		ShadowHelper.GetCurrentShadowInfoFromLights	mGetCurShadowInfo;
		ShadowHelper.GetTransformedBound			mGetTransformedBound;
		TriggerHelper.OkToFireFunc					mOkToFire;


		public GrogLibsGame(int resX, int resY, float farClip,
			float unitWidth, float unitHeight, float unitEyeHeight,
			string menuFont, Enum menuEnum,
			PlayerSteering.SteeringMethod method, Enum startState) : base()
		{
			mGDM	=new GraphicsDeviceManager(this);

			mResX		=resX;
			mResY		=resY;
			mMenuFont	=menuFont;
			mMenuEnum	=menuEnum;

			mGDM.PreferredBackBufferWidth	=resX;
			mGDM.PreferredBackBufferHeight	=resY;

			mState.Transition(startState);

			Content.RootDirectory	="GameContent";

			mCam			=new GameCamera(resX, resY, 16f / 9f, 1f, farClip);
			mScreenCenter	=new Vector2(resX / 2, resY / 2);
			mPSteering		=new PlayerSteering(resX, resY);
			mPathMobile		=new Mobile(this, unitWidth / 2f, unitHeight,
								unitEyeHeight, false, mTHelper);

			mPSteering.Method	=method;
		}


		protected override void Initialize()
		{
			base.Initialize();
		}


		protected override void LoadContent()
		{
			GraphicsDevice	gd	=GraphicsDevice;

			mSB			=new SpriteBatch(gd);
			mSLib		=new ContentManager(Services, "ShaderLib");
			mPost		=new MaterialLib.PostProcess(mGDM, mSLib, mResX, mResY);
			mBFX		=new BasicEffect(gd);

			mBFX.LightingEnabled	=false;
			mBFX.VertexColorEnabled	=true;

			Effect	stat	=mSLib.Load<Effect>("Shaders/2D");
			Effect	post	=mSLib.Load<Effect>("Shaders/Post");
			Effect	lm		=mSLib.Load<Effect>("Shaders/BSP");
			if(mGDM.GraphicsProfile == GraphicsProfile.HiDef)
			{
				mDynLights	=new DynamicLights(gd, lm);
			}

			mBSPFX	=lm;

			mFonts	=FileUtil.LoadAllFonts(Content);

			TextureElement.LoadTexLib(Content.RootDirectory + "/TexLibs/UI.TexLib", Content, mUITex);

			//static stuff
			mStaticMats		=new MaterialLib.MaterialLib(gd, Content, mSLib, false);
			mStaticMats.ReadFromFile(Content.RootDirectory + "/Statics/Statics.MatLib", false, gd);

			mStatics	=StaticMeshObject.LoadAllMeshes("Statics", gd, Content, mStaticMats);

			//init cel shading
			mStaticMats.InitCelShading(1);
			mStaticMats.GenerateCelTexturePreset(gd, true, 0);
			mStaticMats.SetCelTexture(0);

			//make a bunch of post processing rendertargets
			mPost.MakePostTarget(gd, "SceneColor", true, mResX, mResY, SurfaceFormat.Color, DepthFormat.Depth24);
			mPost.MakePostTarget(gd, "SceneDepthMatNorm", true, mResX, mResY, SurfaceFormat.HalfVector4, DepthFormat.Depth24);
			mPost.MakePostTarget(gd, "Bleach", false, mResX, mResY, SurfaceFormat.Color, DepthFormat.None);
			mPost.MakePostTarget(gd, "Outline", false, mResX, mResY, SurfaceFormat.Color, DepthFormat.None);
			mPost.MakePostTarget(gd, "Bloom1", false, mResX/2, mResY/2, SurfaceFormat.Color, DepthFormat.None);
			mPost.MakePostTarget(gd, "Bloom2", false, mResX/2, mResY/2, SurfaceFormat.Color, DepthFormat.None);

			//particle thing
			mPB	=new ParticleBoss(gd, Content, mSLib, Content.RootDirectory + "/TexLibs/Particles.TexLib");

			//level stuff
			mZone		=new Zone();
			mZoneMats	=new MaterialLib.MaterialLib(gd, Content, mSLib, false);
			mZoneDraw	=new IndoorMesh(gd, mZoneMats);

			//set up menu stuff, stick to the libdocs way
			mMB		=new MenuBuilder(mFonts, mUITex);
			mQOpt	=new QuickOptions(mGDM, mMB, mPSteering,
				mScreenCenter, mMenuFont, "Textures\\UI\\HighLight.png");

			mMB.AddScreen("MainMenu", MenuBuilder.ScreenTypes.VerticalMenu,
				mScreenCenter, "Textures\\UI\\HighLight.png");

			mMB.AddMenuStop("MainMenu", "Controls", "Controls", mMenuFont);
			mMB.AddMenuStop("MainMenu", "Video", "Video", mMenuFont);
			mMB.AddMenuStop("MainMenu", "Exit", "Exit", mMenuFont);

			mMB.SetScreenTextColor("MainMenu", Color.White);

			mMB.SetUpNav("MainMenu", "Controls");

			mMB.Link("MainMenu", "Controls", "ControlsMenu");
			mMB.Link("MainMenu", "Video", "VideoMenu");

			mMB.ActivateScreen("MainMenu");
		}


		protected override void UnloadContent()
		{
		}


		protected override void Update(GameTime gameTime)
		{
			int	msDelta	=gameTime.ElapsedGameTime.Milliseconds;

			if(!IsActive)
			{
				base.Update(gameTime);
				return;
			}

			mInput.Update();

			if(mState.CurStateIs(mMenuEnum))
			{
				UpdateMenu(msDelta);
			}

			mZone.UpdateModels(msDelta, mAudio.mListener);

			Input.PlayerInput	pi	=mInput.Player1;

			if(mDynLights != null)
			{
				mDynLights.Update(msDelta, mGDM.GraphicsDevice);
			}

			mGraph.Update();
			mZoneDraw.Update(msDelta);
			mPB.Update(msDelta);
			mAudio.Update(mCam);
			mAHelper.Update();
			mSHelper.Update(msDelta);

			foreach(KeyValuePair<ZoneEntity, LightHelper> shelp in mSLHelpers)
			{
				Vector3	pos;

				shelp.Key.GetOrigin(out pos);

				shelp.Value.Update(msDelta, pos, mDynLights);
			}

			base.Update(gameTime);
		}


		protected override void Draw(GameTime gameTime)
		{
			int	msDelta	=gameTime.ElapsedGameTime.Milliseconds;

			GraphicsDevice	gd	=GraphicsDevice;

			gd.Clear(Color.CornflowerBlue);

			mDynLights.Update(msDelta, GraphicsDevice);

			mBFX.View		=mCam.View;
			mBFX.Projection	=mCam.Projection;

			mStaticMats.UpdateWVP(Matrix.Identity, mCam.View, mCam.Projection, mCam.Position);
			mZoneMats.UpdateWVP(Matrix.Identity, mCam.View, mCam.Projection, mCam.Position);

			mPost.SetTarget(gd, "SceneDepthMatNorm", true);

			gd.BlendState			=BlendState.Opaque;
			gd.DepthStencilState	=DepthStencilState.Default;

			mZoneDraw.DrawDMN(gd, mCam.Position, mCam, mZone.IsMaterialVisibleFromPos,
				mZone.GetModelTransform, RenderExternalDMN);

			mPost.SetTarget(gd, "SceneColor", true);

			if(mDynLights != null)
			{
				mDynLights.SetParameter();
			}

			mZoneDraw.Draw(gd, mCam, mGetNumShadows(), mZone.IsMaterialVisibleFromPos,
				mZone.GetModelTransform, RenderExternal, mSHDHelper.RenderShadows);

			gd.BlendState	=BlendState.Opaque;
//			mGraph.Render(gd, mBFX);

			mPost.SetTarget(gd, "Outline", true);
			mPost.SetParameter("mNormalTex", "SceneDepthMatNorm");
			mPost.DrawStage(gd, "Outline");

/*			mPost.SetTarget(gd, "Bleach", true);
			mPost.SetParameter("mBlurTargetTex", "Outline");
			mPost.DrawStage(gd, "GaussianBlurX");

			mPost.SetTarget(gd, "Outline", true);
			mPost.SetParameter("mBlurTargetTex", "Bleach");
			mPost.DrawStage(gd, "GaussianBlurY");
*/
//			mPost.SetTarget(gd, "Bleach", true);
//			mPost.SetParameter("mColorTex", "SceneColor");
//			mPost.DrawStage(gd, "BleachBypass");

//			mPost.SetTarget(gd, "Bloom1", true);
//			mPost.SetParameter("mBlurTargetTex", "Bleach");
//			mPost.DrawStage(gd, "BloomExtract");

//			mPost.SetTarget(gd, "Bloom2", true);
//			mPost.SetParameter("mBlurTargetTex", "Bloom1");
//			mPost.DrawStage(gd, "GaussianBlurX");

//			mPost.SetTarget(gd, "Bloom1", true);
//			mPost.SetParameter("mBlurTargetTex", "Bloom2");
//			mPost.DrawStage(gd, "GaussianBlurY");

//			mPost.SetTarget(gd, "SceneColor", true);
//			mPost.SetParameter("mBlurTargetTex", "Bloom1");
//			mPost.SetParameter("mColorTex", "Bleach");
//			mPost.DrawStage(gd, "BloomCombine");

			mPost.SetTarget(gd, "null", true);
			mPost.SetParameter("mBlurTargetTex", "Outline");
			mPost.SetParameter("mColorTex", "SceneColor");
			mPost.DrawStage(gd, "Modulate");

			UIDraw();

			base.Draw(gameTime);
		}


		protected virtual void UIDraw()
		{
			if(mState.CurStateIs(mMenuEnum))
			{
				mMB.Draw(mSB);
			}
		}


		protected void PickUpHitCheck(object context, Vector3 pos)
		{
			mSHelper.HitCheck(context, pos);
		}


		protected Mobile MakeMobile(object parent, float width,
			float height, float eyeHeight, bool bPushable)
		{
			return	new Mobile(parent, width, height, eyeHeight, bPushable, mTHelper);
		}


		public void SetCallBacks(GetNumShadowsToDraw gnstd,
			ShadowHelper.GetCurrentShadowInfoFromLights gcsifl,
			ShadowHelper.GetTransformedBound gtb,
			TriggerHelper.OkToFireFunc oktff,
			EventHandler transdTo,
			EventHandler transingTo,
			EventHandler transingFrom,
			EventHandler menuInvoke,
			EventHandler menuNav,
			EventHandler misc,
			EventHandler pickUp)
		{
			mGetNumShadows			=gnstd;
			mGetCurShadowInfo		=gcsifl;
			mGetTransformedBound	=gtb;
			mOkToFire				=oktff;

			mState.eTransitionedTo		+=transdTo;
			mState.eTransitioningFrom	+=transingFrom;
			mState.eTransitioningTo		+=transingTo;

			mMB.eMenuStopInvoke	+=menuInvoke;
			mMB.eNavigating		+=menuNav;

			mTHelper.eMisc		+=misc;
			mSHelper.ePickUp	+=pickUp;
		}


		protected void AddLevel(string lev)
		{
			mLevels.Add(lev);
		}


		protected void RegisterShadower(ShadowHelper.Shadower shad, MaterialLib.MaterialLib mats)
		{
			mSHDHelper.RegisterShadower(shad, mats);
		}


		void UpdateMenu(int msDelta)
		{
			if(!mMB.Active())
			{
				return;
			}

			Input.PlayerInput	pi	=mInput.Player1;

			if(pi.WasKeyPressed(Keys.Escape) || pi.WasButtonPressed(Buttons.Back))
			{
				if(mMB.GetActiveScreen() == "MainMenu")
				{
					mState.TransitionBack();
				}
				else
				{
					mMB.ActivateScreen("MainMenu");
				}
			}

			mMB.Update(msDelta, mInput);
		}


		protected virtual void RenderExternal(MaterialLib.AlphaPool ap,
			Vector3 camPos, Matrix view, Matrix proj)
		{
			GraphicsDevice	gd	=GraphicsDevice;

			mSHelper.Draw(DrawStatic);

			mPB.Draw(ap, view, proj);
		}


		protected virtual void RenderExternalDMN(Vector3 camPos,
			Matrix view, Matrix proj)
		{
			GraphicsDevice	gd	=GraphicsDevice;

			mSHelper.Draw(DrawStaticDMN);

			mPB.DrawDMN(view, proj, camPos);
		}


		void DrawStatic(Matrix local, ZoneEntity ze, Vector3 pos)
		{
			GraphicsDevice	gd	=mGDM.GraphicsDevice;

			Vector4	lightCol;
			Vector3	lightPos, lightDir;
			bool	bDir;
			float	intensity;
			mSLHelpers[ze].GetCurrentValues(out lightCol,
				out intensity, out lightPos, out lightDir, out bDir);

			string	meshName	=ze.GetValue("meshname");

			//kind of a waste unless the light changes
			mStaticMats.SetTriLightValues(lightCol, lightDir);

			mStatics[meshName].SetTransform(local);

			mStatics[meshName].Draw(gd);
		}


		void DrawStaticDMN(Matrix local, ZoneEntity ze, Vector3 pos)
		{
			GraphicsDevice	gd	=mGDM.GraphicsDevice;

			string	meshName	=ze.GetValue("meshname");

			mStatics[meshName].SetTransform(local);

			mStatics[meshName].Draw(gd, "DMN");
		}


		protected virtual void ChangeLevel(int index)
		{
			GraphicsDevice	gd	=GraphicsDevice;

			string	lev	=Content.RootDirectory + "/Levels/" + mLevels[index];

			mZoneMats.ReadFromFile(lev + ".MatLib", false, gd);
			mZone.Read(lev + ".Zone", false);
			mZoneDraw.Read(gd, lev + ".ZoneDraw", false);

			//init cel shading
			mZoneMats.InitCelShading(1);
			mZoneMats.GenerateCelTexturePreset(gd, false, 0);
			mZoneMats.SetCelTexture(0);

			mTHelper.Initialize(mZone, mAudio, mAudio.mListener, mZoneDraw.SwitchLight, mOkToFire);
			mPHelper.Initialize(mZone, mTHelper, mPB, "Textures\\Particles\\");
			mAHelper.Initialize(mZone, mTHelper, mAudio);
			mSHelper.Initialize(mZone);
			mIMHelper.Initlialize(mZone);

			List<ZoneEntity>	wEnt	=mZone.GetEntities("worldspawn");
			Debug.Assert(wEnt.Count == 1);

			float	mDirShadowAtten;
			string	ssa	=wEnt[0].GetValue("SunShadowAtten");
			if(!Single.TryParse(ssa, out mDirShadowAtten))
			{
				mDirShadowAtten	=200f;	//default
			}

			mSHDHelper.Initialize(gd, 512, mDirShadowAtten, mZoneMats, mPost, mGetCurShadowInfo, mGetTransformedBound);

			//make lighthelpers for statics
			mSLHelpers	=mSHelper.MakeLightHelpers(mZone, mZoneDraw.GetStyleStrength);

			mGraph.Load(lev + ".Pathing");
//			mGraph.GenerateGraph(mZone.GetWalkableFaces, 32, 18f, CanPathReach);
//			mGraph.Save(mLevels[index] + ".Pathing");
			mGraph.BuildDrawInfo(gd);

			mPathMobile.SetZone(mZone);

			mIDKeeper.AddLib(mZoneMats);
			mIDKeeper.AddLib(mStaticMats);
			mIDKeeper.Scan();
			mIDKeeper.AssignIDsToMaterials(mBSPFX);
		}


		bool CanPathReach(Vector3 start, Vector3 end)
		{
			ZonePlane	startPlane	=mZone.GetGroundNormal(start, false);
			ZonePlane	endPlane	=mZone.GetGroundNormal(end, false);

			BoundingBox	box	=mPathMobile.GetBounds();

			float	startDot	=Vector3.Dot(box.Max, startPlane.mNormal);
			float	endDot		=Vector3.Dot(box.Max, endPlane.mNormal);

			//adjust up out of the plane
			start	+=startDot * startPlane.mNormal;
			end		+=endDot * endPlane.mNormal;

			Collision	col			=new Collision();
			int			iterations	=0;

			//adjust start and end points if they are in solid
			//with the supplied radius, but don't let them
			//drift too far
			while(mZone.TraceAll(null, box, start, start + Vector3.Up, out col))
			{
				start	+=Vector3.UnitY;
				iterations++;

				if(iterations > 5)
				{
					return	false;
				}
			}

			while(mZone.TraceAll(null, box, end, end + Vector3.Up, out col))
			{
				end	+=Vector3.UnitY;
				iterations++;

				if(iterations > 5)
				{
					return	false;
				}
			}

			return	!mZone.TraceAll(null, box, start, end, out col);
		}
	}
}
