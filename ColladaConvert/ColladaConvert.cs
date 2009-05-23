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

namespace ColladaConvert
{
	public class ColladaConvert : Microsoft.Xna.Framework.Game
	{
		SpriteBatch				mSpriteBatch;
		Collada					mCollada;
		GraphicsDeviceManager	mGDM;
		VertexBuffer			mVB;
		VertexDeclaration		mVDecl;
		IndexBuffer				mIB;
		Effect					mFX;

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

		private	Texture2D	mDesu;
		private	Vector3		mLightDir;


		public ColladaConvert()
		{
			mGDM	=new GraphicsDeviceManager(this);
			Content.RootDirectory	="Content";
		}

		protected override void Initialize()
		{
			mPitch = mYaw = mRoll = 0;
			InitializeTransform();

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
			//load shader
			mFX		=Content.Load<Effect>("Simple");

			mTestEffect	=Content.Load<Effect>("VPosNormTexAnim");
			mCollada	=new Collada("content/beach.dae", GraphicsDevice);
//			mCollada	=new Collada("content/hero.dae", GraphicsDevice);
//			mCollada = new Collada("content/WackyWalk.dae", GraphicsDevice);
			mDesu	=Content.Load<Texture2D>("desu");

			Point	topLeft, bottomRight;
			topLeft.X		=-500;
			topLeft.Y		=-500;
			bottomRight.X	=500;
			bottomRight.Y	=500;

			//fill in some verts, just a simple quad
			VertexPositionNormalTexture	[]verts	=new VertexPositionNormalTexture[4];
			verts[0].Position.X	=topLeft.X;
			verts[1].Position.X	=bottomRight.X;
			verts[2].Position.X	=topLeft.X;
			verts[3].Position.X	=bottomRight.X;

			verts[0].Position.Y	=topLeft.Y;
			verts[1].Position.Y	=topLeft.Y;
			verts[2].Position.Y	=bottomRight.Y;
			verts[3].Position.Y	=bottomRight.Y;

			verts[0].TextureCoordinate	=Vector2.UnitY;
			verts[1].TextureCoordinate	=Vector2.UnitX + Vector2.UnitY;
			verts[3].TextureCoordinate	=Vector2.UnitX;

			//set up a simple vertex element
			VertexElement	[]ve	=new VertexElement[3];

			ve[0]	=new VertexElement(0, 0, VertexElementFormat.Vector3,
						VertexElementMethod.Default, VertexElementUsage.Position, 0);
			ve[1]	=new VertexElement(0, 12, VertexElementFormat.Vector3,
						VertexElementMethod.Default, VertexElementUsage.Normal, 0);
			ve[2]	=new VertexElement(0, 24, VertexElementFormat.Vector2,
						VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 0);

			//create vertex declaration
			mVDecl	=new VertexDeclaration(mGDM.GraphicsDevice, ve);

			//create vertex and index buffers
			mVB	=new VertexBuffer(mGDM.GraphicsDevice, 32 * 4, BufferUsage.WriteOnly);
			mIB	=new IndexBuffer(mGDM.GraphicsDevice, 2 * 6, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);

			//put our data into the vertex buffer
			mVB.SetData<VertexPositionNormalTexture>(verts);

			//mark the indexes
			UInt16	[]ind	=new ushort[6];
			ind[0]	=0;
			ind[1]	=1;
			ind[2]	=2;
			ind[3]	=2;
			ind[4]	=1;
			ind[5]	=3;

			//fill in index buffer
			mIB.SetData<UInt16>(ind);

			InitializeEffect();
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
		}


		private void UpdateMatrices()
		{
			// Compute camera matrices.

			mViewMatrix	=Matrix.CreateTranslation(mCamPos) *
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
			Vector4	[]lightColor	=new Vector4[3];
			lightColor[0]	=new Vector4(0.9f, 0.9f, 0.9f, 1.0f);
			lightColor[1]	=new Vector4(0.6f, 0.5f, 0.5f, 1.0f);
			lightColor[2]	=new Vector4(0.1f, 0.1f, 0.1f, 1.0f);

			Vector4	ambColor	=new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
			Vector3	lightDir	=new Vector3(1.0f, -1.0f, 0.1f);
			lightDir.Normalize();

			mTestEffect.Parameters["mLightColor"].SetValue(lightColor);
			mTestEffect.Parameters["mLightDirection"].SetValue(lightDir);
			mTestEffect.Parameters["mAmbientColor"].SetValue(ambColor);
			mTestEffect.Parameters["mTexture0"].SetValue(mDesu);
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
			//rotate the light vector

			//grab a time value to use to spin the axii
			float spinAmount	=gameTime.TotalGameTime.Milliseconds;

			//scale it back a bit
			spinAmount	*=0.00001f;

			//build a matrix that spins over time
			Matrix	mat	=Matrix.CreateFromYawPitchRoll
				(spinAmount * 3.0f,
				spinAmount,
				spinAmount * 0.5f);

			//transform (rotate) the vector
			mLightDir	=Vector3.TransformNormal(mLightDir, mat);

			//update it in the shader
			mFX.Parameters["mLightDir"].SetValue(mLightDir);
			mFX.Parameters["mTexture"].SetValue(mDesu);

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

			mCollada.UpdateBones(mGDM.GraphicsDevice, mTestEffect);

			mCollada.Draw(mGDM.GraphicsDevice, mTestEffect);

			//set stream source, index, and decl
			mGDM.GraphicsDevice.Vertices[0].SetSource(mVB, 0, 32);
			mGDM.GraphicsDevice.Indices				=mIB;
			mGDM.GraphicsDevice.VertexDeclaration	=mVDecl;

			mLightDir.X	=0.5f;
			mLightDir.Y	=0.2f;
			mLightDir.Z	=0.7f;

			mLightDir.Normalize();

			mFX.Parameters["mLightDir"].SetValue(mLightDir);
			mFX.Begin();
			foreach(EffectPass pass in mFX.CurrentTechnique.Passes)
			{
				pass.Begin();
				
				//draw shizzle here
				mGDM.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList,
					0, 0, 4, 0, 2);

				pass.End();
			}
			mFX.End();

			base.Draw(gameTime);
		}


		private void UpdateLightMapEffect()
		{
			mTestEffect.Parameters["mWorld"].SetValue(mWorldMatrix);
			mTestEffect.Parameters["mView"].SetValue(mViewMatrix);
			mTestEffect.Parameters["mProjection"].SetValue(mProjectionMatrix);
			mTestEffect.Parameters["mLocal"].SetValue(Matrix.Identity);	//TODO:fix
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
