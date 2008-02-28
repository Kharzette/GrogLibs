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
        private string                  szMapFileName;
        private Map                     mMap;
        private Matrix                  mWorldMatrix;
        private Matrix                  mViewMatrix;
        private Matrix                  mProjectionMatrix;
        private BasicEffect             mBasicEffect;
        private VertexDeclaration       mVertexDeclaration;
        private GamePadState            mCurrentGamePadState;
        private GamePadState            mLastGamePadState;
        private KeyboardState           mCurrentKeyboardState;
        private MouseState              mCurrentMouseState;
        private MouseState              mLastMouseState;

        //cam / player stuff will move later
        private Vector3 mCamPos;
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
            szMapFileName   =args[0];
            for (int i = 0; i < args.GetLength(0); i++)
            {
                Debug.WriteLine(args[i]);
            }

            mMap = new Map(szMapFileName);

            mMap.RemoveOverlap();

			mMap.BuildTree();
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
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
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
            graphics.GraphicsDevice.Clear(Color.CornflowerBlue);
            graphics.GraphicsDevice.VertexDeclaration = mVertexDeclaration;

            mBasicEffect.World = mWorldMatrix;
            mBasicEffect.View = mViewMatrix;
            mBasicEffect.Projection = mProjectionMatrix;

            // The effect is a compiled effect created and compiled elsewhere
            // in the application.
            mBasicEffect.Begin();

            foreach(EffectPass pass in mBasicEffect.CurrentTechnique.Passes)
            {
                pass.Begin();

                mMap.Draw(graphics.GraphicsDevice);

                pass.End();
            }
            mBasicEffect.End();

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
        }


        private void InitializeEffect()
        {

            mVertexDeclaration = new VertexDeclaration(
                graphics.GraphicsDevice,
                VertexPositionNormalTexture.VertexElements);

            mBasicEffect = new BasicEffect(graphics.GraphicsDevice, null);
            mBasicEffect.DiffuseColor = new Vector3(1.0f, 1.0f, 1.0f);

            mBasicEffect.World      =mWorldMatrix;
            mBasicEffect.View       =mViewMatrix;
            mBasicEffect.Projection =mProjectionMatrix;
            mBasicEffect.VertexColorEnabled = false;
            mBasicEffect.DirectionalLight0.DiffuseColor = new Vector3(1.0f, 1.0f, 1.0f);
            mBasicEffect.DirectionalLight0.Direction = Vector3.Down;
            mBasicEffect.DirectionalLight0.Enabled = true;
            mBasicEffect.DirectionalLight0.SpecularColor = new Vector3(1.0f, 1.0f, 1.0f);
            mBasicEffect.AmbientLightColor = new Vector3(0.1f, 0.1f, 0.1f);
            mBasicEffect.EnableDefaultLighting();
        }


        private void InitializeTransform()
        {
            mWorldMatrix    =Matrix.CreateTranslation(new Vector3(-1.5f, -0.5f, 0.0f));

            mViewMatrix =Matrix.CreateLookAt(
                new Vector3(0.0f, 0.0f, 7.0f),
                new Vector3(0.0f, 0.0f, 0.0f),
                Vector3.Up
                );

            mProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.ToRadians(45),
                (float)graphics.GraphicsDevice.Viewport.Width /
                (float)graphics.GraphicsDevice.Viewport.Height,
                1.0f, 100.0f
                );
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
            }
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
