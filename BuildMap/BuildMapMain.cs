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
		private GraphicsDeviceManager   mGraphics;
		private SpriteBatch             mSpriteBatch;
		private Map                     mMap;
		private Matrix                  mWorldMatrix;
		private Matrix                  mViewMatrix;
		private Matrix                  mViewTranspose;
		private Matrix                  mProjectionMatrix;
		private Effect					mMapEffect;
		private BasicEffect             mDotEffect;
		private BasicEffect             mPortalEffect;
		private VertexDeclaration       mBasicVertexDeclaration;
		private VertexDeclaration       mMapVertexDeclaration;
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
			mGraphics = new GraphicsDeviceManager(this);
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
			mCamPos.X	=-192.0f;
			mCamPos.Y	=-64.0f;
			mCamPos.Z	=-64.0f;
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
			mSpriteBatch = new SpriteBatch(GraphicsDevice);

			string	outputFileName	=mMapFileName;

			//strip extension
			int	idx	=outputFileName.LastIndexOf('.');

			outputFileName	=outputFileName.Remove(idx);

			outputFileName += ".zone";

			//strip path stuff off
			idx	=outputFileName.LastIndexOf('/');
			if(idx != -1)
			{
				outputFileName	=outputFileName.Substring(idx + 1);
			}

			mMapEffect	=Content.Load<Effect>("LightMap");

			mMap	=new Map(mMapFileName);
			//mMap.LoadTextures(Content);
			mMap.RemoveOverlap();
			mMap.BuildTree();
			//mMap.BuildPortals();
			mMap.LightAllBrushes(mGraphics.GraphicsDevice);
			//mMap.LightAllBspFaces(mGraphics.GraphicsDevice);

			mMap.BuildVertexInfo(mGraphics.GraphicsDevice);

			mMap.Save(outputFileName);
			
			//mMap.SetTexturePointers();
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
			mGraphics.GraphicsDevice.Clear(Color.CornflowerBlue);

			mDotEffect.World = mWorldMatrix;
			mDotEffect.View = mViewMatrix;
			mDotEffect.Projection = mProjectionMatrix;
			mPortalEffect.World = mWorldMatrix;
			mPortalEffect.View = mViewMatrix;
			mPortalEffect.Projection = mProjectionMatrix;

			UpdateLightMapEffect();

			mGraphics.GraphicsDevice.VertexDeclaration	=mMapVertexDeclaration;
			mMapEffect.Begin();
			foreach(EffectPass pass in mMapEffect.CurrentTechnique.Passes)
			{
				pass.Begin();

				if(mbDrawBrushes)
				{
					mMap.Draw(mGraphics.GraphicsDevice, mMapEffect);
				}

				if(mbDrawBsp)
				{
					mMap.Draw(mGraphics.GraphicsDevice, mMapEffect, mCamPos);
				}

				pass.End();
			}
			mMapEffect.End();

			mGraphics.GraphicsDevice.VertexDeclaration = mBasicVertexDeclaration;
			if(mbDrawDot)
			{
				mDotEffect.Begin();
				foreach(EffectPass pass in mDotEffect.CurrentTechnique.Passes)
				{
					pass.Begin();

					Vector3[]	surfPnts;
					int np	=mMap.GetFirstSurface(out surfPnts);

					VertexPositionNormalTexture[]	vpc	=new VertexPositionNormalTexture[np + 1];
					for(int i=0;i < np;i++)
					{
						vpc[i].Position	=surfPnts[i];
						vpc[i].Normal				=Vector3.Forward;
						vpc[i].TextureCoordinate	=Vector2.One;
					}
					
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
					
					vpc[np].Position	=mMap.GetFirstLightPos();

					mGraphics.GraphicsDevice.RenderState.PointSize	=10;

					mGraphics.GraphicsDevice.DrawUserPrimitives<VertexPositionNormalTexture>
						(PrimitiveType.PointList, vpc, 0, np + 1);

					mDotEffect.DiffuseColor	=Vector3.One;
					mDotEffect.CommitChanges();

					pass.End();
				}
				mDotEffect.End();
			}
			if(mbDrawPortals)
			{
				mPortalEffect.Begin();
			}

			if(mbDrawPortals)
			{
				mMap.DrawPortals(mGraphics.GraphicsDevice, mPortalEffect, mCamPos);
			}

			if(mbDrawPortals || mbDrawBsp)
			{
				mPortalEffect.End();
			}
			/*
			//draw a few lightmaps
			int halfWidth = GraphicsDevice.Viewport.Width / 2;
			int halfHeight = GraphicsDevice.Viewport.Height / 2;
			
			mSpriteBatch.Begin();

			List<Texture2D>	thrice;

			mMap.GetThriceLightmaps(out thrice);

			if(thrice[0] != null)
			{
				mSpriteBatch.Draw(thrice[0],
					new Rectangle(0, 0, halfWidth, halfHeight), Color.White);
			}			
			if(thrice[1] != null)
			{
				mSpriteBatch.Draw(thrice[1], 
					new Rectangle(0, halfHeight, halfWidth, halfHeight), Color.White);
			}
			if(thrice[2] != null)
			{
				mSpriteBatch.Draw(thrice[2], 
					new Rectangle(halfWidth, 0, halfWidth, halfHeight), Color.White);
			}			
			mSpriteBatch.End();*/
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
			mBasicVertexDeclaration	=new VertexDeclaration(
				mGraphics.GraphicsDevice,
				VertexPositionNormalTexture.VertexElements);

			//set up a 2 texcoord vert element
			VertexElement[]	ve	=new VertexElement[3];

			ve[0]	=new VertexElement(0, 0, VertexElementFormat.Vector3,
				VertexElementMethod.Default, VertexElementUsage.Position, 0);
			ve[1]	=new VertexElement(0, 12, VertexElementFormat.Vector2,
				VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 0);
			ve[2]	=new VertexElement(0, 20, VertexElementFormat.Vector2,
				VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 1);
			
			mMapVertexDeclaration	=new VertexDeclaration(
				mGraphics.GraphicsDevice,
				ve);

			mDotEffect = new BasicEffect(mGraphics.GraphicsDevice, null);
			mDotEffect.DiffuseColor = new Vector3(1.0f, 1.0f, 1.0f);

			mDotEffect.World      =mWorldMatrix;
			mDotEffect.View       =mViewMatrix;
			mDotEffect.Projection =mProjectionMatrix;

			mPortalEffect = new BasicEffect(mGraphics.GraphicsDevice, null);
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
				(float)mGraphics.GraphicsDevice.Viewport.Width /
				(float)mGraphics.GraphicsDevice.Viewport.Height,
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
			if(((mCurrentGamePadState.Buttons.RightShoulder == ButtonState.Pressed)) &&
				(mLastGamePadState.Buttons.RightShoulder == ButtonState.Released))
			{
				BspNode.gCheck++;
			}
			if(((mCurrentGamePadState.Buttons.LeftShoulder == ButtonState.Pressed)) &&
				(mLastGamePadState.Buttons.LeftShoulder == ButtonState.Released))
			{
				BspNode.gCheck--;
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
