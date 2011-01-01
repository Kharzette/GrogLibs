using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using BSPZone;


namespace BSPTest
{
	public class BSPTest : Game
	{
		GraphicsDeviceManager	mGDM;
		SpriteBatch				mSB;
		SpriteFont				mKoot;

		Zone					mZone;
		MeshLib.IndoorMesh		mLevel;
		UtilityLib.GameCamera	mGameCam;
		MaterialLib.MaterialLib	mMatLib;

		Vector3	mVelocity;
		Vector3	mBodyMins, mBodyMaxs;
		Vector3	mStart, mEnd, mImpacto;


		public BSPTest()
		{
			mGDM	=new GraphicsDeviceManager(this);

			Content.RootDirectory	="Content";
		}


		protected override void Initialize()
		{
			mGameCam	=new UtilityLib.GameCamera(mGDM.GraphicsDevice.Viewport.Width,
				mGDM.GraphicsDevice.Viewport.Height,
				mGDM.GraphicsDevice.Viewport.AspectRatio);

			//70, 32
			mBodyMins	=-Vector3.UnitX * 16;
			mBodyMins	+=-Vector3.UnitZ * 16;
			mBodyMins	+=-Vector3.UnitY * 35;

			mBodyMaxs	=Vector3.UnitX * 16;
			mBodyMaxs	+=Vector3.UnitZ * 16;
			mBodyMaxs	+=Vector3.UnitY * 35;

			base.Initialize();
		}

		protected override void LoadContent()
		{
			mSB		=new SpriteBatch(GraphicsDevice);
			mKoot	=Content.Load<SpriteFont>("Fonts/Kootenay");

			mMatLib	=new MaterialLib.MaterialLib(GraphicsDevice, Content, "Content/eels.MatLib", false);

			mZone	=new Zone();
			mLevel	=new MeshLib.IndoorMesh(GraphicsDevice, mMatLib);
			
			mZone.Read("Content/eels.Zone");

			mLevel.Read(GraphicsDevice, "Content/eels.ZoneDraw", false);

			mGameCam.CamPos	=-mZone.GetPlayerStartPos();
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

			Vector3	startPos	=-mGameCam.CamPos;

			//do a movement update
			mGameCam.Update(msDelta, kbs, Mouse.GetState());

			Vector3	endPos	=-mGameCam.CamPos;

			mVelocity	+=endPos - startPos;
			mVelocity.Y	-=((9.8f / 1000.0f) * msDelta);	//gravity

			endPos	+=mVelocity;

			Vector3		impacto		=Vector3.Zero;
			ZonePlane	planeHit	=new ZonePlane();
			if(mZone.Trace_WorldCollisionBBox(mBodyMins, mBodyMaxs,
				startPos, endPos, 0, ref impacto, ref planeHit))
			{
				//reset velocity
				mVelocity	=Vector3.Zero;

				//reflect the ray's energy
				float	dist	=planeHit.DistanceFast(endPos);

				//push out of the plane
				endPos	+=(planeHit.mNormal * dist);

				//ray cast again
				if(mZone.Trace_WorldCollisionBBox(mBodyMins, mBodyMaxs,
					startPos, endPos, 0, ref impacto, ref planeHit))
				{
					//just use second impact point
					endPos	=impacto;
				}
			}

			mGameCam.CamPos	=-endPos;
			/*
			if(kbs.IsKeyDown(Keys.O))
			{
				mStart	=-mGameCam.CamPos;
			}
			if(kbs.IsKeyDown(Keys.P))
			{
				mEnd	=-mGameCam.CamPos;
			}
			if(kbs.IsKeyDown(Keys.C))
			{
				Vector3		impacto		=Vector3.Zero;
				ZonePlane	planeHit	=new ZonePlane();
				if(mZone.Trace_WorldCollisionBBox(mBodyMins, mBodyMaxs,
					mStart, mEnd, 0, ref impacto, ref planeHit))
				{
					mImpacto	=impacto;
				}
			}*/

			mLevel.Update(msDelta);
			mMatLib.UpdateWVP(mGameCam.World, mGameCam.View, mGameCam.Projection, -mGameCam.CamPos);

			base.Update(gameTime);
		}


		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice	g	=mGDM.GraphicsDevice;

			g.Clear(Color.CornflowerBlue);

			//spritebatch turns this off
			g.RenderState.DepthBufferEnable	=true;

			mLevel.Draw(g, mGameCam, mZone.IsMaterialVisibleFromPos);

			mSB.Begin();
			mSB.DrawString(mKoot, "Coordinates: " + -mGameCam.CamPos,
				Vector2.One * 20.0f, Color.Yellow);
//			mSB.DrawString(mKoot, "Impacto: " + mImpacto,
//				Vector2.One * 20.0f + Vector2.UnitY * 80, Color.Yellow);
			mSB.End();

			base.Draw(gameTime);
		}
	}
}
