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
using Microsoft.Xna.Framework.Storage;
using BSPZone;


namespace BSPTest
{
	public class BSPTest : Game
	{
		GraphicsDeviceManager	mGDM;
		SpriteBatch				mSB;
		ContentManager			mSharedCM;
		SpriteFont				mKoot;

		Zone					mZone;
		MeshLib.IndoorMesh		mLevel;
		UtilityLib.GameCamera	mGameCam;
		MaterialLib.MaterialLib	mMatLib;

		Vector3		mVelocity;
		BoundingBox	mCharBox;
		bool		mbOnGround;

		const float MidAirMoveScale	=0.4f;


		public BSPTest()
		{
			mGDM	=new GraphicsDeviceManager(this);

			Content.RootDirectory	="Content";
		}


		protected override void Initialize()
		{
			mGameCam	=new UtilityLib.GameCamera(
				mGDM.GraphicsDevice.Viewport.Width,
				mGDM.GraphicsDevice.Viewport.Height,
				mGDM.GraphicsDevice.Viewport.AspectRatio);

			//70, 32 is the general character size

			//bottom
			mCharBox.Min	=-Vector3.UnitX * 16;
			mCharBox.Min	+=-Vector3.UnitZ * 16;

			//top
			mCharBox.Max	=Vector3.UnitX * 16;
			mCharBox.Max	+=Vector3.UnitZ * 16;
			mCharBox.Max	+=Vector3.UnitY * 70;

			base.Initialize();
		}


		protected override void LoadContent()
		{
			mSB			=new SpriteBatch(GraphicsDevice);
			mSharedCM	=new ContentManager(Services, "SharedContent");
			mKoot		=mSharedCM.Load<SpriteFont>("Fonts/Koot20");

			mMatLib	=new MaterialLib.MaterialLib(GraphicsDevice,
				Content, mSharedCM, false);

			mMatLib.ReadFromFile("Content/dm4ish.MatLib", false);

			mZone	=new Zone();
			mLevel	=new MeshLib.IndoorMesh(GraphicsDevice, mMatLib);
			
			mZone.Read("Content/dm4ish.Zone");

			mLevel.Read(GraphicsDevice, "Content/dm4ish.ZoneDraw", false);

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

			//lower ray pos down to foot level
			Vector3	startPos	=-mGameCam.CamPos;
			startPos			-=Vector3.UnitY * 65.0f;

			//set for a movement update (will move this eventually)
			mGameCam.CamPos	=-startPos;

			//do a movement update
			mGameCam.Update(msDelta, kbs, Mouse.GetState());

			//use the result for the raycast
			Vector3	endPos		=-mGameCam.CamPos;
			Vector3	moveDelta	=endPos - startPos;

			//flatten movement
			moveDelta.Y	=0;
			mVelocity	+=moveDelta;

			//if not on the ground, limit midair movement
			if(!mbOnGround)
			{
				mVelocity.X	*=MidAirMoveScale;
				mVelocity.Z	*=MidAirMoveScale;
			}

			mVelocity.Y	-=((9.8f / 1000.0f) * msDelta);	//gravity

			//get ideal final position
			endPos	=startPos + mVelocity;

			//move it through the bsp
			if(mZone.BipedMoveBox(mCharBox, startPos, endPos, ref endPos))
			{
				//on ground, zero out velocity
				mVelocity	=Vector3.Zero;
				mbOnGround	=true;
			}
			else
			{
				mVelocity	=endPos - startPos;
				mbOnGround	=false;
			}

			//bump position back to eye height
			endPos			+=Vector3.UnitY * 65.0f;
			mGameCam.CamPos	=-endPos;

			mGameCam.UpdateMatrices();

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
//			g.RenderState.DepthBufferEnable	=true;

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
