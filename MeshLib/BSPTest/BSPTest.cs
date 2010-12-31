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

		Zone	mZone;

		MeshLib.IndoorMesh	mLevel;

		UtilityLib.GameCamera	mGameCam;

		MaterialLib.MaterialLib	mMatLib;


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

			base.Initialize();
		}

		protected override void LoadContent()
		{
			mSB	=new SpriteBatch(GraphicsDevice);

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

			Vector3	startPos	=mGameCam.CamPos;

			mGameCam.Update(msDelta, kbs, Mouse.GetState());

			Vector3	endPos	=mGameCam.CamPos;
			/*
			Line	ln;
			ln.mP1	=-startPos;
			ln.mP2	=-endPos;
			if(mMap.MoveLine(ref ln, 16.0f))
//			if(mMap.MoveLine(ref ln))
			{
				mGameCam.CamPos	=-ln.mP2;
			}*/
			mLevel.Update(msDelta);
			mMatLib.UpdateWVP(mGameCam.World, mGameCam.View, mGameCam.Projection, -mGameCam.CamPos);

			base.Update(gameTime);
		}


		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice	g	=mGDM.GraphicsDevice;

			g.Clear(Color.CornflowerBlue);

			mLevel.Draw(g, mGameCam, mZone.IsMaterialVisibleFromPos);

			base.Draw(gameTime);
		}
	}
}
