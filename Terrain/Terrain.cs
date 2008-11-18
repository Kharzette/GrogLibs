using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

namespace Terrain
{
	public class Terrain : Microsoft.Xna.Framework.Game
	{
		//drawing
		private	GraphicsDeviceManager	mGDM;
		private	SpriteBatch				mSpriteBatch;
		private	Effect					mFXFlyer;

		//mats
		private	Matrix	mMATWorld;
		private	Matrix	mMATView;
		private	Matrix	mMATViewTranspose;
		private	Matrix	mMATProjection;

		//control
		private	GamePadState	mGPStateCurrent;
		private	GamePadState	mGPStateLast;
		private	KeyboardState	mKBStateCurrent;
		private	MouseState		mMStateCurrent;
		private	MouseState		mMStateLast;

		//cam / player stuff will move later
		private Vector3	mCamPos;
		private float	mPitch, mYaw, mRoll;

		//height maps
		private	HeightMap	mMap0, mMap1;
		private	Vector3		mMap0Position, mMap1Position;
		private	Vector3		mMap0Direction, mMap1Direction;

		//player ship
		private	Flyer	mPlayerShip;
		private	Vector3	mPlayerPos;
		private	float	mPlayerSpeed;	//expressed mainly in terrain movement


		public Terrain(string[] args)
		{
			mGDM	=new GraphicsDeviceManager(this);
			Content.RootDirectory	="Content";

			if(args.Length <= 0)
			{
				this.Exit();
				return;
			}

			//check the command line args
			if(args.GetLength(0) < 1)
			{
				this.Exit();
				return;
			}

			//not using arguments yet
			/*blah   =args[0];
			for(int i = 0;i < args.GetLength(0);i++)
			{
				Debug.WriteLine(args[i]);
			}*/
		}


		protected override void Initialize()
		{
			mYaw	=mRoll	=0.0f;
			mPitch	=90.0f;

			//initial camera position
			mCamPos	=Vector3.UnitY * -450.0f;

			//center the maps
			mMap0Position	=-Vector3.UnitX * (64 / 2) * 10.0f;
			mMap0Position	+=Vector3.UnitY * (64 / 2) * 10.0f;

			mMap1Position	=mMap0Position + (Vector3.UnitY * 630.0f);

			mMap0Direction	=-Vector3.UnitY;
			mMap1Direction	=-Vector3.UnitY;

			mPlayerPos	=Vector3.UnitY * 30000.0f;
			mPlayerPos	+=Vector3.UnitZ * 3000.0f;

			InitializeEffect();
			InitializeTransform();

			base.Initialize();
		}


		protected override void LoadContent()
		{
			mSpriteBatch	=new SpriteBatch(GraphicsDevice);
			mFXFlyer		=Content.Load<Effect>("Shaders/Flyer");

			mMap0	=new HeightMap("content/height.jpg",
				"content/dirt_simple_df_.dds",
				"content/dirt_mottledsand_df_.dds",
				3.0f, 7.0f,
				mGDM.GraphicsDevice, Content);

			mMap1	=new HeightMap("content/height.jpg",
				"content/dirt_simple_df_.dds",
				"content/dirt_mottledsand_df_.dds",
				3.0f, 7.0f,
				mGDM.GraphicsDevice, Content);

			mPlayerShip	=new Flyer("Models/p1_saucer", Content, mFXFlyer);
		}


		protected override void UnloadContent()
		{
			// TODO: Unload any non ContentManager content here
		}


		protected override void Update(GameTime gameTime)
		{
			//Allows the game to exit
			if(GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
			{
				this.Exit();
			}

			CheckGamePadInput();

			UpdateCamera(gameTime);

			UpdateMatrices();

			mPlayerShip.Update(gameTime, mKBStateCurrent);

			//update the height map positions
			float	time	=(float)gameTime.ElapsedGameTime.TotalMilliseconds;
			mMap0Position	+=mMap0Direction * mPlayerSpeed * (time * 0.1f);
			mMap1Position	+=mMap1Direction * mPlayerSpeed * (time * 0.1f);

			//wrap
			if(mMap0Position.Y < -(64 * 3))
			{
				mMap0Position	=mMap1Position + (Vector3.UnitY * 630.0f);
			}
			if(mMap1Position.Y < -(64 * 3))
			{
				mMap1Position	=mMap0Position + (Vector3.UnitY * 630.0f);
			}

			mMap0.SetPos(mMap0Position);
			mMap1.SetPos(mMap1Position);

			UpdatePlayer(gameTime);

			base.Update(gameTime);
		}


		protected override void Draw(GameTime gameTime)
		{
			mGDM.GraphicsDevice.Clear(Color.CornflowerBlue);

			mMap0.Draw(mGDM.GraphicsDevice);
			mMap1.Draw(mGDM.GraphicsDevice);
			mPlayerShip.Draw(mGDM.GraphicsDevice);

			base.Draw(gameTime);
		}


		private void UpdateMatrices()
		{
			mMATView	=Matrix.CreateTranslation(mCamPos) *
				Matrix.CreateRotationY(MathHelper.ToRadians(mYaw)) *
				Matrix.CreateRotationX(MathHelper.ToRadians(mPitch)) *
				Matrix.CreateRotationZ(MathHelper.ToRadians(mRoll));
			
			mMATProjection	=Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4,
				GraphicsDevice.DisplayMode.AspectRatio, 1, 10000);
			
			Matrix.Transpose(ref mMATView, out mMATViewTranspose);
			//Vector3	shipPos	=-mCamPos + mMATViewTranspose.Forward * 10.0f;

			//mPlayerShip.UpdatePosition(shipPos);

			//update height map
			mMap0.UpdateMatrices(mMATWorld, mMATView, mMATProjection);
			mPlayerShip.UpdateMatrices(mMATWorld, mMATView, mMATProjection);
		}


		private void InitializeEffect()
		{
		}


		private void InitializeTransform()
		{
			mMATWorld	=Matrix.CreateTranslation(new Vector3(0.0f, 0.0f, 0.0f));
			mMATView	=Matrix.CreateTranslation(new Vector3(0.0f, 0.0f, 0.0f));

			mMATProjection	=Matrix.CreatePerspectiveFieldOfView(
				MathHelper.ToRadians(45),
				(float)mGDM.GraphicsDevice.Viewport.Width /
				(float)mGDM.GraphicsDevice.Viewport.Height,
				1.0f, 100.0f);
		}
		
		
		private void CheckGamePadInput()
		{
			mMStateLast		=mMStateCurrent;
			mGPStateLast	=mGPStateCurrent;
			mGPStateCurrent	=GamePad.GetState(PlayerIndex.One);
			mKBStateCurrent	=Keyboard.GetState();
			mMStateCurrent	=Mouse.GetState();
		}


		private void UpdateCamera(GameTime gameTime)
		{
			float	time	=(float)gameTime.ElapsedGameTime.TotalMilliseconds;

			Vector3	vup;
			Vector3	vleft;
			Vector3	vin;

			//grab view matrix in vector transpose
			vup.X	=mMATView.M12;
			vup.Y	=mMATView.M22;
			vup.Z	=mMATView.M32;
			vleft.X	=mMATView.M11;
			vleft.Y	=mMATView.M21;
			vleft.Z	=mMATView.M31;
			vin.X	=mMATView.M13;
			vin.Y	=mMATView.M23;
			vin.Z	=mMATView.M33;

			Matrix.Transpose(ref mMATView, out mMATViewTranspose);

			/*
			if(mKBStateCurrent.IsKeyDown(Keys.Left) ||
				mKBStateCurrent.IsKeyDown(Keys.A))
			{
				mCamPos	+=vleft * (time * 0.1f);
			}

			if(mKBStateCurrent.IsKeyDown(Keys.Right) ||
				mKBStateCurrent.IsKeyDown(Keys.D))
			{
				mCamPos	-=vleft * (time * 0.1f);
			}

			//Note: apologies for hacking this shit in, I wanted the ability to turn to be able to see the map better -- Kyth
			if(mKBStateCurrent.IsKeyDown(Keys.Q))
			{
				mYaw	-=time*0.1f;
			}

			if(mKBStateCurrent.IsKeyDown(Keys.E))
			{
				mYaw	+=time*0.1f;
			}

			if(mKBStateCurrent.IsKeyDown(Keys.Z))
			{
				mPitch	-=time*0.1f;
			}

			if(mKBStateCurrent.IsKeyDown(Keys.C))
			{
				mPitch	+=time*0.1f;
			}
			*/
			//Horrible mouselook hack so I can see where I'm going. Won't let me spin in circles, some kind of overflow issue?
			if(mMStateCurrent.RightButton == ButtonState.Pressed)
			{
				mPitch	+=(mMStateCurrent.Y - mMStateLast.Y) * time * 0.03f;
				mYaw	+=(mMStateCurrent.X - mMStateLast.X) * time * 0.03f;
			}

			mPitch	+=mGPStateCurrent.ThumbSticks.Right.Y * time * 0.25f;
			mYaw	+=mGPStateCurrent.ThumbSticks.Right.X * time * 0.25f;

			mCamPos	-=vleft * (mGPStateCurrent.ThumbSticks.Left.X * time * 0.25f);
			mCamPos	+=vin * (mGPStateCurrent.ThumbSticks.Left.Y * time * 0.25f);
		}


		private void UpdatePlayer(GameTime gameTime)
		{
			float	time	=(float)gameTime.ElapsedGameTime.TotalMilliseconds;

			if(mKBStateCurrent.IsKeyDown(Keys.Left) ||
				mKBStateCurrent.IsKeyDown(Keys.A))
			{
				mPlayerPos	-=Vector3.UnitX * mPlayerSpeed * time;
			}

			if(mKBStateCurrent.IsKeyDown(Keys.Right) ||
				mKBStateCurrent.IsKeyDown(Keys.D))
			{
				mPlayerPos	+=Vector3.UnitX * mPlayerSpeed * time;
			}

			//keep player confined
			//his power level must not go over 9000!
			if(mPlayerPos.X > 8000.0f)
			{
				mPlayerPos.X	=8000.0f;
			}
			else if(mPlayerPos.X < -8000.0f)
			{
				mPlayerPos.X	=-8000.0f;
			}

			if(mKBStateCurrent.IsKeyDown(Keys.Up) ||
				mKBStateCurrent.IsKeyDown(Keys.W))
			{
				mPlayerSpeed	=10.0f;
			}
			else if(mKBStateCurrent.IsKeyDown(Keys.Down) ||
				mKBStateCurrent.IsKeyDown(Keys.S))
			{
				mPlayerSpeed	=3.0f;
			}
			else
			{
				mPlayerSpeed	=5.0f;
			}


			mPlayerShip.UpdatePosition(mPlayerPos);
		}


		public static FileStream OpenTitleFile(string fileName,
			FileMode mode, FileAccess access)
		{
			string	fullPath	=Path.Combine(
									StorageContainer.TitleLocation,
									fileName);

			if(!File.Exists(fullPath) &&
				(access == FileAccess.Write ||
				access == FileAccess.ReadWrite))
			{
				return	File.Create(fullPath);
			}
			else
			{
				return	File.Open(fullPath, mode, access);
			}
		}
	}
}
