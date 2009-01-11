using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

namespace Collada
{
	/// <summary>
	/// This is the main type for your game
	/// </summary>
	public class Game1 : Microsoft.Xna.Framework.Game
	{
		SpriteBatch				mSpriteBatch;
		Collada					mCollada;
		GraphicsDeviceManager	mGDM;

		private Effect			mTestEffect;

		private Matrix	mWorldMatrix;
		private Matrix	mViewMatrix;
		private Matrix	mViewTranspose;
		private Matrix	mProjectionMatrix;

		private GamePadState	mCurrentGamePadState;
		private GamePadState	mLastGamePadState;
		private KeyboardState	mCurrentKeyboardState;
		private MouseState		mCurrentMouseState;
		private MouseState		mLastMouseState;

		//cam / player stuff will move later
		private Vector3	mCamPos, mDotPos;
		private float	mPitch, mYaw, mRoll;


		public Game1()
		{
			mGDM	=new GraphicsDeviceManager(this);
			Content.RootDirectory	="Content";
		}

		protected override void Initialize()
		{
			mPitch = mYaw = mRoll = 0;
			InitializeEffect();
			InitializeTransform();
			VertexTypes.AddType(typeof(VPosNormTexTexColor));

			base.Initialize();
		}


		private void InitializeTransform()
		{
			mWorldMatrix    =Matrix.CreateTranslation(new Vector3(0.0f, 0.0f, 0.0f));

			mViewMatrix =Matrix.CreateTranslation(new Vector3(0.0f, 0.0f, 0.0f));

			mProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(
				MathHelper.ToRadians(45),
				(float)mGDM.GraphicsDevice.Viewport.Width /
				(float)mGDM.GraphicsDevice.Viewport.Height,
				1.0f, 100.0f);
		}

		/// <summary>
		/// LoadContent will be called once per game and is the place to load
		/// all of your content.
		/// </summary>
		protected override void LoadContent()
		{
			mTestEffect	=Content.Load<Effect>("VPosNormTexTexColor");
			mCollada	=new Collada("content/hero.dae", GraphicsDevice);
//			mCollada = new Collada("content/WackyWalk.dae", GraphicsDevice);
		}

		/// <summary>
		/// UnloadContent will be called once per game and is the place to unload
		/// all content.
		/// </summary>
		protected override void UnloadContent()
		{
			// TODO: Unload any non ContentManager content here
		}


		private void CheckGamePadInput()
		{
			mLastMouseState         =mCurrentMouseState;
			mLastGamePadState       =mCurrentGamePadState;
			mCurrentGamePadState    =GamePad.GetState(PlayerIndex.One);
			mCurrentKeyboardState   =Keyboard.GetState();
			mCurrentMouseState      =Mouse.GetState();
			/*
			if(((mCurrentGamePadState.Buttons.A == ButtonState.Pressed)) &&
				(mLastGamePadState.Buttons.A == ButtonState.Released))
			{
				mbDrawBsp	=!mbDrawBsp;
			}
			if(((mCurrentGamePadState.Buttons.B == ButtonState.Pressed)) &&
				(mLastGamePadState.Buttons.B == ButtonState.Released))
			{
				mbDrawDot	=!mbDrawDot;
			}
			if(((mCurrentGamePadState.Buttons.Y == ButtonState.Pressed)) &&
				(mLastGamePadState.Buttons.Y == ButtonState.Released))
			{
				mbDrawBrushes	=!mbDrawBrushes;
			}
			if(((mCurrentGamePadState.Buttons.X == ButtonState.Pressed)) &&
				(mLastGamePadState.Buttons.X == ButtonState.Released))
			{
				mbDrawPortals	=!mbDrawPortals;
			}*/
		}


		private void UpdateMatrices()
		{
			// Compute camera matrices.

			mViewMatrix = Matrix.CreateTranslation(mCamPos) *
			Matrix.CreateRotationY(MathHelper.ToRadians(mYaw)) *
			Matrix.CreateRotationX(MathHelper.ToRadians(mPitch)) *
			Matrix.CreateRotationZ(MathHelper.ToRadians(mRoll));
			mProjectionMatrix   =Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4,
										GraphicsDevice.DisplayMode.AspectRatio,
										1,
										10000);

			Matrix.Transpose(ref mViewMatrix, out mViewTranspose);

			mDotPos	=-mCamPos + mViewTranspose.Forward * 10.0f;
		}


		private void InitializeEffect()
		{
		}

		/// <summary>
		/// Allows the game to run logic such as updating the world,
		/// checking for collisions, gathering input, and playing audio.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Update(GameTime gameTime)
		{
			// Allows the game to exit
			if(GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
				this.Exit();

			CheckGamePadInput();

			UpdateCamera(gameTime);

			UpdateMatrices();

			base.Update(gameTime);
		}

		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Draw(GameTime gameTime)
		{
			mGDM.GraphicsDevice.Clear(Color.CornflowerBlue);

			UpdateLightMapEffect();

			mCollada.Draw(mGDM.GraphicsDevice, mTestEffect);

			base.Draw(gameTime);
		}


		private void UpdateLightMapEffect()
		{
			mTestEffect.Parameters["mWorld"].SetValue(mWorldMatrix);
			mTestEffect.Parameters["mView"].SetValue(mViewMatrix);
			mTestEffect.Parameters["mProjection"].SetValue(mProjectionMatrix);
			mTestEffect.Parameters["mLocal"].SetValue(Matrix.Identity);	//TODO:fix
			//mZoneEffect.Parameters["TextureEnabled"].SetValue(mbTextureEnabled);
		}


		private void UpdateCamera(GameTime gameTime)
		{
			float time = (float)gameTime.ElapsedGameTime.TotalMilliseconds;

			Vector3 vup;
			Vector3 vleft;
			Vector3 vin;

			//grab view matrix in vector transpose
			vup.X   =mViewMatrix.M12;
			vup.Y   =mViewMatrix.M22;
			vup.Z   =mViewMatrix.M32;
			vleft.X =mViewMatrix.M11;
			vleft.Y =mViewMatrix.M21;
			vleft.Z =mViewMatrix.M31;
			vin.X   =mViewMatrix.M13;
			vin.Y   =mViewMatrix.M23;
			vin.Z   =mViewMatrix.M33;

			Matrix.Transpose(ref mViewMatrix, out mViewTranspose);

			if(mCurrentKeyboardState.IsKeyDown(Keys.Up) ||
				mCurrentKeyboardState.IsKeyDown(Keys.W))
			{
				mCamPos += vin * (time * 0.1f);
			}

			if(mCurrentKeyboardState.IsKeyDown(Keys.Down) ||
				mCurrentKeyboardState.IsKeyDown(Keys.S))
			{
				mCamPos -= vin * (time * 0.1f);
			}

			if(mCurrentKeyboardState.IsKeyDown(Keys.Left) ||
				mCurrentKeyboardState.IsKeyDown(Keys.A))
			{
				mCamPos += vleft * (time * 0.1f);
			}

			if(mCurrentKeyboardState.IsKeyDown(Keys.Right) ||
				mCurrentKeyboardState.IsKeyDown(Keys.D))
			{
				mCamPos -= vleft * (time * 0.1f);
			}

			//Note: apologies for hacking this shit in, I wanted the ability to turn to be able to see the map better -- Kyth
			if(mCurrentKeyboardState.IsKeyDown(Keys.Q))
			{
				mYaw -= time*0.1f;
			}

			if(mCurrentKeyboardState.IsKeyDown(Keys.E))
			{
				mYaw += time*0.1f;
			}

			if(mCurrentKeyboardState.IsKeyDown(Keys.Z))
			{
				mPitch -= time*0.1f;
			}

			if(mCurrentKeyboardState.IsKeyDown(Keys.C))
			{
				mPitch += time*0.1f;
			}

			if(mCurrentKeyboardState.IsKeyDown(Keys.Right) ||
				mCurrentKeyboardState.IsKeyDown(Keys.D))
			{
				mCamPos -= vleft * (time * 0.1f);
			}

			//Horrible mouselook hack so I can see where I'm going. Won't let me spin in circles, some kind of overflow issue?
			if(mCurrentMouseState.RightButton == ButtonState.Pressed)
			{
				mPitch += (mCurrentMouseState.Y - mLastMouseState.Y) * time * 0.03f;
				mYaw += (mCurrentMouseState.X - mLastMouseState.X) * time * 0.03f;
			}

			mPitch += mCurrentGamePadState.ThumbSticks.Right.Y * time * 0.25f;
			mYaw += mCurrentGamePadState.ThumbSticks.Right.X * time * 0.25f;

			mCamPos -= vleft * (mCurrentGamePadState.ThumbSticks.Left.X * time * 0.25f);
			mCamPos += vin * (mCurrentGamePadState.ThumbSticks.Left.Y * time * 0.25f);
		}
	}
}
