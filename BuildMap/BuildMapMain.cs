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
using System.Diagnostics;

namespace BuildMap
{
	/// <summary>
	/// This is the main type for your game
	/// </summary>
	public class BuildMapMain : Microsoft.Xna.Framework.Game
	{
		private GraphicsDeviceManager   graphics;
		private SpriteBatch             spriteBatch;
		private Map                     mMap;
		private Matrix                  mWorldMatrix;
		private Matrix                  mViewMatrix;
		private Matrix                  mViewTranspose;
		private Matrix                  mProjectionMatrix;
		private Effect					mMapEffect;
		private BasicEffect             mDotEffect;
		private BasicEffect             mPortalEffect;
		private VertexDeclaration       mVertexDeclaration;
		private GamePadState            mCurrentGamePadState;
		private GamePadState            mLastGamePadState;
		private KeyboardState           mCurrentKeyboardState;
		private MouseState              mCurrentMouseState;
		private MouseState              mLastMouseState;
		private	bool					mbDrawDot;
		private	bool					mbDrawBsp;
		private	bool					mbDrawBrushes;
		private	bool					mbDrawPortals;
		private	bool					mbTextureEnabled;
		private	string					mMapFileName;
		
		//cam / player stuff will move later
		private Vector3 mCamPos, mDotPos;
		private float   mPitch, mYaw, mRoll;
		

		public BuildMapMain(string[] args)
		{
			graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
			
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
			
			//arg zero should be the map name
			mMapFileName   =args[0];
			for (int i = 0; i < args.GetLength(0); i++)
			{
				Debug.WriteLine(args[i]);
			}
		}

		/// <summary>
		/// Allows the game to perform any initialization it needs to before starting to run.
		/// This is where it can query for any required services and load any non-graphic
		/// related content.  Calling base.Initialize will enumerate through any components
		/// and initialize them as well.
		/// </summary>
		protected override void Initialize()
		{
			// TODO: Add your initialization logic here
			mPitch = mYaw = mRoll = 0;
			InitializeEffect();
			InitializeTransform();

			mbDrawBsp		=false;
			mbDrawDot		=false;
			mbDrawBrushes	=true;
			mbDrawPortals	=false;
			mbTextureEnabled=false;

			base.Initialize();
		}

		/// <summary>
		/// LoadContent will be called once per game and is the place to load
		/// all of your content.
		/// </summary>
		protected override void LoadContent()
		{
			// Create a new SpriteBatch, which can be used to draw textures.
			spriteBatch = new SpriteBatch(GraphicsDevice);

			mMapEffect	=Content.Load<Effect>("LightMap");

			mMap	=new Map(mMapFileName);
			mMap.RemoveOverlap();
			mMap.BuildTree();
			mMap.BuildPortals();
			mMap.LightAllBrushes(graphics.GraphicsDevice);
			// TODO: use this.Content to load your game content here
		}

		/// <summary>
		/// UnloadContent will be called once per game and is the place to unload
		/// all content.
		/// </summary>
		protected override void UnloadContent()
		{
			// TODO: Unload any non ContentManager content here
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
			{
				this.Exit();
			}

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
			graphics.GraphicsDevice.Clear(Color.CornflowerBlue);
			graphics.GraphicsDevice.VertexDeclaration = mVertexDeclaration;

			mDotEffect.World = mWorldMatrix;
			mDotEffect.View = mViewMatrix;
			mDotEffect.Projection = mProjectionMatrix;
			mPortalEffect.World = mWorldMatrix;
			mPortalEffect.View = mViewMatrix;
			mPortalEffect.Projection = mProjectionMatrix;

			UpdateLightMapEffect();

			mMapEffect.Begin();
			foreach(EffectPass pass in mMapEffect.CurrentTechnique.Passes)
			{
				pass.Begin();

				if(mbDrawBrushes)
				{
					mMap.Draw(graphics.GraphicsDevice, mMapEffect);
				}

				pass.End();
			}
			mMapEffect.End();

			if(mbDrawDot)
			{
				mDotEffect.Begin();
				foreach(EffectPass pass in mDotEffect.CurrentTechnique.Passes)
				{
					pass.Begin();

					VertexPositionNormalTexture[]	vpc	=new VertexPositionNormalTexture[1];
					bool cp	=mMap.ClassifyPoint(mDotPos);
					if(cp == false)
					{
						mDotEffect.DiffuseColor	=Vector3.UnitX;
					}
					else if(cp == true)
					{
						mDotEffect.DiffuseColor	=Vector3.UnitY;
					}
					else
					{
						mDotEffect.DiffuseColor	=Vector3.UnitZ;
					}
					mDotEffect.CommitChanges();

					vpc[0].Position				=mDotPos;
					vpc[0].Normal				=Vector3.Forward;
					vpc[0].TextureCoordinate	=Vector2.One;

					graphics.GraphicsDevice.RenderState.PointSize	=10;

					graphics.GraphicsDevice.DrawUserPrimitives<VertexPositionNormalTexture>
						(PrimitiveType.PointList, vpc, 0, 1);

					mDotEffect.DiffuseColor	=Vector3.One;
					mDotEffect.CommitChanges();

					pass.End();
				}
				mDotEffect.End();
			}

			if(mbDrawPortals)
			{
				mPortalEffect.Begin();

				if(mbDrawBsp)
				{
					mMap.Draw(graphics.GraphicsDevice, mPortalEffect, mCamPos);
				}
				mMap.DrawPortals(graphics.GraphicsDevice, mPortalEffect, mCamPos);

				mPortalEffect.End();
			}

			base.Draw(gameTime);
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
			mVertexDeclaration = new VertexDeclaration(
				graphics.GraphicsDevice,
				VertexPositionNormalTexture.VertexElements);

			mDotEffect = new BasicEffect(graphics.GraphicsDevice, null);
			mDotEffect.DiffuseColor = new Vector3(1.0f, 1.0f, 1.0f);

			mDotEffect.World      =mWorldMatrix;
			mDotEffect.View       =mViewMatrix;
			mDotEffect.Projection =mProjectionMatrix;

			mPortalEffect = new BasicEffect(graphics.GraphicsDevice, null);
			mPortalEffect.DiffuseColor = new Vector3(0.0f, 0.0f, 1.0f);

			mPortalEffect.World			=mWorldMatrix;
			mPortalEffect.View			=mViewMatrix;
			mPortalEffect.Projection	=mProjectionMatrix;
			mPortalEffect.Alpha			=0.5f;
		}


		private void InitializeTransform()
		{
			mWorldMatrix    =Matrix.CreateTranslation(new Vector3(0.0f, 0.0f, 0.0f));

			mViewMatrix =Matrix.CreateTranslation(new Vector3(0.0f, 0.0f, 0.0f));

			mProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(
				MathHelper.ToRadians(45),
				(float)graphics.GraphicsDevice.Viewport.Width /
				(float)graphics.GraphicsDevice.Viewport.Height,
				1.0f, 100.0f);
		}


		private void CheckGamePadInput()
		{
			mLastMouseState         =mCurrentMouseState;
			mLastGamePadState       =mCurrentGamePadState;
			mCurrentGamePadState    =GamePad.GetState(PlayerIndex.One);
			mCurrentKeyboardState   =Keyboard.GetState();
			mCurrentMouseState      =Mouse.GetState();

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
			}
		}


		private void UpdateLightMapEffect()
		{
			//set all parameters
			/*
			mMapEffect.Parameters["colorMap"].SetValue(colorRT.GetTexture());
			mMapEffect.Parameters["normalMap"].SetValue(normalRT.GetTexture());
			mMapEffect.Parameters["depthMap"].SetValue(depthRT.GetTexture());
			mMapEffect.Parameters["lightDirection"].SetValue(lightDirection);
			mMapEffect.Parameters["Color"].SetValue(color.ToVector3());
			mMapEffect.Parameters["cameraPosition"].SetValue(camera.Position);
			mMapEffect.Parameters["InvertViewProjection"].SetValue(
				Matrix.Invert(camera.View * camera.Projection));
			mMapEffect.Parameters["halfPixel"].SetValue(halfPixel);
			*/

			mMapEffect.Parameters["World"].SetValue(mWorldMatrix);
			mMapEffect.Parameters["View"].SetValue(mViewMatrix);
			mMapEffect.Parameters["Projection"].SetValue(mProjectionMatrix);
			mMapEffect.Parameters["TextureEnabled"].SetValue(mbTextureEnabled);
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
