using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;


namespace ColladaConvert
{
	public class ColladaConvert : Microsoft.Xna.Framework.Game
	{
		GraphicsDeviceManager	mGDM;
		VertexBuffer			mVB, mBoundsVB;
		VertexDeclaration		mVDecl;
		IndexBuffer				mIB, mBoundsIB;
		Effect					mFX;

		MaterialLib.MaterialLib		mMatLib;
		MeshLib.AnimLib				mAnimLib;
		MeshLib.Character			mCharacter;
		MeshLib.StaticMeshObject	mStaticMesh;

		//material gui
		MaterialForm	mMF;

		//animation gui
		AnimForm	mCF;
		string		mCurrentAnimName;
		float		mTimeScale;			//anim playback speed

		Matrix	mWorldMatrix;
		Matrix	mViewMatrix;
		Matrix	mViewTranspose;
		Matrix	mProjectionMatrix;

		GamePadState	mCurrentGamePadState;
		GamePadState	mLastGamePadState;
		KeyboardState	mCurrentKeyboardState;
		MouseState		mCurrentMouseState;
		MouseState		mLastMouseState;

		//cam / player stuff will move later
		Vector3	mCamPos, mDotPos;
		float	mPitch, mYaw, mRoll;

		Texture2D	mDesu;
		Texture2D	mEureka;
		Vector3		mLightDir;

		//number of bounds drawing
		int	mNumBounds;


		public static event EventHandler	eAnimsUpdated;
		public static event EventHandler	eMeshPartListUpdated;

		public ColladaConvert()
		{
			mGDM	=new GraphicsDeviceManager(this);
			Content.RootDirectory	="Content";

			mCurrentAnimName	="";
			mTimeScale			=1.0f;

			//set window position
			if(!mGDM.IsFullScreen)
			{
				System.Windows.Forms.Control	mainWindow
					=System.Windows.Forms.Form.FromHandle(this.Window.Handle);

				//add data binding so it will save
				mainWindow.DataBindings.Add(new System.Windows.Forms.Binding("Location",
					global::ColladaConvert.Properties.Settings.Default,
					"MainWindowPos", true,
					System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));

				mainWindow.Location	=
					global::ColladaConvert.Properties.Settings.Default.MainWindowPos;
			}
		}


		protected override void Initialize()
		{
			mPitch = mYaw = mRoll = 0;
			InitializeTransform();

			//default cam pos off to one side
			mCamPos.X	=102.0f;
			mCamPos.Y	=-96.0f;
			mCamPos.Z	=187.0f;
			mYaw		=155.0f;
			mPitch		=4.0f;

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


		protected override void LoadContent()
		{
			mMatLib		=new MaterialLib.MaterialLib(mGDM.GraphicsDevice, Content);
			mAnimLib	=new MeshLib.AnimLib();
			mCharacter	=new MeshLib.Character(mMatLib, mAnimLib);
			mStaticMesh	=new MeshLib.StaticMeshObject(mMatLib);

			//load debug shaders
			mFX			=Content.Load<Effect>("Shaders/Static");

			mDesu	=Content.Load<Texture2D>("Textures/desu");
			mEureka	=Content.Load<Texture2D>("Textures/Eureka");

			Point	topLeft, bottomRight;
			topLeft.X		=0;
			topLeft.Y		=0;
			bottomRight.X	=5;
			bottomRight.Y	=10;

			//fill in some verts two quads
			VertexPositionNormalTexture	[]verts	=new VertexPositionNormalTexture[8];
			verts[0].Position.X	=topLeft.X;
			verts[1].Position.X	=bottomRight.X;
			verts[2].Position.X	=topLeft.X;
			verts[3].Position.X	=bottomRight.X;

			verts[4].Position.Z	=topLeft.X;
			verts[5].Position.Z	=bottomRight.X;
			verts[6].Position.Z	=topLeft.X;
			verts[7].Position.Z	=bottomRight.X;

			verts[0].Position.Y	=topLeft.Y;
			verts[1].Position.Y	=topLeft.Y;
			verts[2].Position.Y	=bottomRight.Y;
			verts[3].Position.Y	=bottomRight.Y;

			verts[4].Position.Y	=topLeft.Y;
			verts[5].Position.Y	=topLeft.Y;
			verts[6].Position.Y	=bottomRight.Y;
			verts[7].Position.Y	=bottomRight.Y;

			verts[0].TextureCoordinate	=Vector2.UnitY;
			verts[1].TextureCoordinate	=Vector2.UnitX + Vector2.UnitY;
			verts[3].TextureCoordinate	=Vector2.UnitX;
			verts[4].TextureCoordinate	=Vector2.UnitY;
			verts[5].TextureCoordinate	=Vector2.UnitX + Vector2.UnitY;
			verts[7].TextureCoordinate	=Vector2.UnitX;

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
			mVB	=new VertexBuffer(mGDM.GraphicsDevice, 32 * 8, BufferUsage.WriteOnly);
			mIB	=new IndexBuffer(mGDM.GraphicsDevice, 2 * 12, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);

			//put our data into the vertex buffer
			mVB.SetData<VertexPositionNormalTexture>(verts);

			//mark the indexes
			ushort	[]ind	=new ushort[12];
			ind[0]	=0;
			ind[1]	=1;
			ind[2]	=2;
			ind[3]	=2;
			ind[4]	=1;
			ind[5]	=3;
			ind[6]	=4;
			ind[7]	=5;
			ind[8]	=6;
			ind[9]	=6;
			ind[10]	=5;
			ind[11]	=7;

			//fill in index buffer
			mIB.SetData<ushort>(ind);

			InitializeEffect();

			mCF	=new AnimForm(mAnimLib);
			mCF.Visible	=true;

			mCF.eLoadAnim				+=OnOpenAnim;
			mCF.eLoadModel				+=OnOpenModel;
			mCF.eLoadStaticModel		+=OnOpenStaticModel;
			mCF.eAnimSelectionChanged	+=OnAnimSelChanged;
			mCF.eTimeScaleChanged		+=OnTimeScaleChanged;
			mCF.eSaveLibrary			+=OnSaveLibrary;
			mCF.eSaveCharacter			+=OnSaveCharacter;
			mCF.eLoadCharacter			+=OnLoadCharacter;
			mCF.eLoadLibrary			+=OnLoadLibrary;
			mCF.eLoadStatic				+=OnLoadStatic;
			mCF.eSaveStatic				+=OnSaveStatic;

			mMF	=new MaterialForm(mGDM.GraphicsDevice, mMatLib);
			mMF.Visible	=true;

			mMF.eBoundsUpdated	+=OnBoundsChanged;
			mMF.eNukedMeshPart	+=OnNukedMeshPart;
		}


		protected override void UnloadContent()
		{
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
		}


		private void OnOpenAnim(object sender, EventArgs ea)
		{
			string	path	=(string)sender;
/*
			if(mCollada != null)
			{
//				mCollada.LoadAnim(path);
			}
			else
			{
//				mCollada	=new Collada(path, GraphicsDevice, Content, mMatLib, mAnimLib, mCharacter);
				eMeshPartListUpdated(null, null);
			}*/
			eAnimsUpdated(mAnimLib.GetAnims(), null);
		}


		private void OnOpenModel(object sender, EventArgs ea)
		{
			mCharacter	=ColladaFileUtils.LoadCharacter(sender as string, mGDM.GraphicsDevice, mMatLib, mAnimLib);

			eMeshPartListUpdated(mCharacter.GetMeshPartList(), null);
			eAnimsUpdated(mAnimLib.GetAnims(), null);
		}


		void OnNukedMeshPart(object sender, EventArgs ea)
		{
			MeshLib.Mesh	msh	=sender as MeshLib.Mesh;

			mCharacter.NukeMesh(msh);
		}


		void OnBoundsChanged(object sender, EventArgs ea)
		{
			//clear existing
			mBoundsVB	=null;
			mBoundsIB	=null;

			List<MeshLib.Bounds>	bnds	=(List<MeshLib.Bounds>)sender;

			if(bnds.Count <= 0)
			{
				return;
			}

			int	numCorners	=0;
			foreach(MeshLib.Bounds bnd in bnds)
			{
				numCorners	+=2;
			}
			mBoundsVB	=new VertexBuffer(mGDM.GraphicsDevice, numCorners * 4 * 32, BufferUsage.WriteOnly);

			VertexPositionNormalTexture	[]vpnt	=new VertexPositionNormalTexture[numCorners * 4];

			int	idx	=0;
			foreach(MeshLib.Bounds bnd in bnds)
			{
				float	xDiff	=bnd.mMaxs.X - bnd.mMins.X;
				float	yDiff	=bnd.mMaxs.Y - bnd.mMins.Y;
				float	zDiff	=bnd.mMaxs.Z - bnd.mMins.Z;

				vpnt[idx++].Position	=bnd.mMins;
				vpnt[idx++].Position	=bnd.mMins + Vector3.UnitX * xDiff;
				vpnt[idx++].Position	=bnd.mMins + Vector3.UnitX * xDiff + Vector3.UnitY * yDiff;
				vpnt[idx++].Position	=bnd.mMins + Vector3.UnitY * yDiff;
				vpnt[idx++].Position	=bnd.mMins + Vector3.UnitZ * zDiff;
				vpnt[idx++].Position	=bnd.mMins + Vector3.UnitZ * zDiff + Vector3.UnitX * xDiff;
				vpnt[idx++].Position	=bnd.mMins + Vector3.UnitZ * zDiff + Vector3.UnitY * yDiff;
				vpnt[idx++].Position	=bnd.mMins + Vector3.UnitZ * zDiff + Vector3.UnitX * xDiff + Vector3.UnitY * yDiff;
			}

			mBoundsVB.SetData<VertexPositionNormalTexture>(vpnt);

			mBoundsIB	=new IndexBuffer(mGDM.GraphicsDevice, (numCorners * 3) * 6 * 2, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);

			UInt16	[]inds	=new UInt16[(numCorners * 3) * 6];

			mNumBounds	=numCorners / 2;

			idx	=0;
			UInt16 bndIdx	=0;
			foreach(MeshLib.Bounds bnd in bnds)
			{
				UInt16	idxOffset	=bndIdx;
				//awesome compiler bug here, have to do this in 2 steps
				idxOffset	*=8;

				//max coordinates here
				//-z facing 
				//same compiler bug, two lines instead of 1
				inds[idx]	=0;
				inds[idx++]	+=idxOffset;
				inds[idx]	=1;
				inds[idx++]	+=idxOffset;
				inds[idx]	=2;
				inds[idx++]	+=idxOffset;
				inds[idx]	=0;
				inds[idx++]	+=idxOffset;
				inds[idx]	=2;
				inds[idx++]	+=idxOffset;
				inds[idx]	=3;
				inds[idx++]	+=idxOffset;

				//-y facing
				inds[idx]	=0;
				inds[idx++]	+=idxOffset;
				inds[idx]	=4;
				inds[idx++]	+=idxOffset;
				inds[idx]	=5;
				inds[idx++]	+=idxOffset;
				inds[idx]	=0;
				inds[idx++]	+=idxOffset;
				inds[idx]	=5;
				inds[idx++]	+=idxOffset;
				inds[idx]	=1;
				inds[idx++]	+=idxOffset;

				//-x facing
				inds[idx]	=0;
				inds[idx++]	+=idxOffset;
				inds[idx]	=3;
				inds[idx++]	+=idxOffset;
				inds[idx]	=6;
				inds[idx++]	+=idxOffset;
				inds[idx]	=0;
				inds[idx++]	+=idxOffset;
				inds[idx]	=6;
				inds[idx++]	+=idxOffset;
				inds[idx]	=4;
				inds[idx++]	+=idxOffset;

				//x facing
				inds[idx]	=1;
				inds[idx++]	+=idxOffset;
				inds[idx]	=5;
				inds[idx++]	+=idxOffset;
				inds[idx]	=7;
				inds[idx++]	+=idxOffset;
				inds[idx]	=1;
				inds[idx++]	+=idxOffset;
				inds[idx]	=7;
				inds[idx++]	+=idxOffset;
				inds[idx]	=2;
				inds[idx++]	+=idxOffset;

				//y facing
				inds[idx]	=7;
				inds[idx++]	+=idxOffset;
				inds[idx]	=6;
				inds[idx++]	+=idxOffset;
				inds[idx]	=3;
				inds[idx++]	+=idxOffset;
				inds[idx]	=7;
				inds[idx++]	+=idxOffset;
				inds[idx]	=3;
				inds[idx++]	+=idxOffset;
				inds[idx]	=2;
				inds[idx++]	+=idxOffset;

				//z facing
				inds[idx]	=4;
				inds[idx++]	+=idxOffset;
				inds[idx]	=6;
				inds[idx++]	+=idxOffset;
				inds[idx]	=7;
				inds[idx++]	+=idxOffset;
				inds[idx]	=4;
				inds[idx++]	+=idxOffset;
				inds[idx]	=7;
				inds[idx++]	+=idxOffset;
				inds[idx]	=5;
				inds[idx++]	+=idxOffset;

				bndIdx++;
			}
			mBoundsIB.SetData<UInt16>(inds);
		}


		//non skinned collada model
		private void OnOpenStaticModel(object sender, EventArgs ea)
		{
			string	path	=(string)sender;

//			mCollada	=new Collada(path, GraphicsDevice, Content, mMatLib, mStaticMesh);

			eMeshPartListUpdated(null, null);
		}


		private void OnSaveLibrary(object sender, EventArgs ea)
		{
			string	path	=(string)sender;

			mAnimLib.SaveToFile(path);
		}


		private void OnLoadLibrary(object sender, EventArgs ea)
		{
			string	path	=(string)sender;

			mAnimLib.ReadFromFile(path);
			eAnimsUpdated(mAnimLib.GetAnims(), null);
		}


		private void OnSaveCharacter(object sender, EventArgs ea)
		{
			string	path	=(string)sender;

			mCharacter.SaveToFile(path);
		}


		private void OnLoadCharacter(object sender, EventArgs ea)
		{
			string	path	=(string)sender;

			mCharacter.ReadFromFile(path, mGDM.GraphicsDevice, true);

			eMeshPartListUpdated(mCharacter.GetMeshPartList(), null);
		}


		private void OnLoadStatic(object sender, EventArgs ea)
		{
			string	path	=(string)sender;

			mStaticMesh.ReadFromFile(path, mGDM.GraphicsDevice, true);

			eMeshPartListUpdated(mStaticMesh.GetMeshPartList(), null);
		}


		private void OnSaveStatic(object sender, EventArgs ea)
		{
			string	path	=(string)sender;

			mStaticMesh.SaveToFile(path);
		}


		private void OnAnimSelChanged(object sender, EventArgs ea)
		{
			System.Windows.Forms.DataGridViewSelectedRowCollection
				src	=(System.Windows.Forms.DataGridViewSelectedRowCollection)sender;

			foreach(System.Windows.Forms.DataGridViewRow dgvr in src)
			{
				//eventually we'll blend these animations
				//but for now play the first
				mCurrentAnimName	=(string)dgvr.Cells[0].FormattedValue;
			}
		}


		private void OnTimeScaleChanged(object sender, EventArgs ea)
		{
			Decimal	val	=(Decimal)sender;

			mTimeScale	=Convert.ToSingle(val);
		}


		protected override void Update(GameTime gameTime)
		{
			if(GamePad.GetState(PlayerIndex.One).Buttons.Back
				== ButtonState.Pressed)
			{
				Exit();
			}

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
			mFX.Parameters["mLightDirection"].SetValue(mLightDir);
			mFX.Parameters["mTexture"].SetValue(mDesu);

			//put in some keys for messing with bones
			float	time		=(float)gameTime.ElapsedGameTime.TotalMilliseconds;

			//mCollada.DebugBoneModify(mBoneMatrix);

			mCharacter.Animate(mCurrentAnimName, (float)(gameTime.TotalGameTime.TotalSeconds) * mTimeScale);

			base.Update(gameTime);
		}


		protected override void Draw(GameTime gameTime)
		{
			mGDM.GraphicsDevice.Clear(Color.CornflowerBlue);

			UpdateWVP();

			mCharacter.Draw(mGDM.GraphicsDevice);
			mStaticMesh.Draw(mGDM.GraphicsDevice);

			//set stream source, index, and decl
			mGDM.GraphicsDevice.Vertices[0].SetSource(mVB, 0, 32);
			mGDM.GraphicsDevice.Indices				=mIB;
			mGDM.GraphicsDevice.VertexDeclaration	=mVDecl;

			//default light direction
			mLightDir.X	=-0.3f;
			mLightDir.Y	=-1.0f;
			mLightDir.Z	=-0.2f;
			mLightDir.Normalize();

			mFX.Parameters["mLightDirection"].SetValue(mLightDir);

			mGDM.GraphicsDevice.RenderState.AlphaBlendEnable	=true;
			mGDM.GraphicsDevice.RenderState.SourceBlend			=Blend.SourceAlpha;
			mGDM.GraphicsDevice.RenderState.DestinationBlend	=Blend.InverseSourceAlpha;

			mFX.CurrentTechnique	=mFX.Techniques[0];
			
			mFX.Parameters["mTexture"].SetValue(mEureka);
			mFX.Begin();
			foreach(EffectPass pass in mFX.CurrentTechnique.Passes)
			{
				pass.Begin();
				
				mGDM.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList,
					4, 0, 4, 0, 2);

				pass.End();
			}
			mFX.End();

			mFX.Parameters["mTexture"].SetValue(mDesu);
			mFX.Begin();
			foreach(EffectPass pass in mFX.CurrentTechnique.Passes)
			{
				pass.Begin();
				
				mGDM.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList,
					0, 0, 4, 0, 2);

				pass.End();
			}
			mFX.End();

			//draw bounds if any
			if(mBoundsVB != null && mMF.bDrawBounds())
			{
				mFX.Begin();

				mGDM.GraphicsDevice.Vertices[0].SetSource(mBoundsVB, 0, 32);
				mGDM.GraphicsDevice.Indices		=mBoundsIB;

				foreach(EffectPass pass in mFX.CurrentTechnique.Passes)
				{
					pass.Begin();

					mGDM.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList,
						0, 0, 8, 0, 6 * 2 * mNumBounds);

					pass.End();
				}

				mFX.End();
			}

			mGDM.GraphicsDevice.RenderState.AlphaBlendEnable	=false;

			base.Draw(gameTime);
		}


		private void UpdateWVP()
		{
			mMatLib.UpdateWVP(mWorldMatrix, mViewMatrix, mProjectionMatrix);
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
