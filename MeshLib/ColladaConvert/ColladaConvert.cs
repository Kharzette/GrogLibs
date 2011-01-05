using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using MeshLib;


namespace ColladaConvert
{
	public class ColladaConvert : Microsoft.Xna.Framework.Game
	{
		GraphicsDeviceManager	mGDM;
		ContentManager			mSharedCM;
		VertexBuffer			mVB, mBoundsVB;
		IndexBuffer				mIB, mBoundsIB;
		Effect					mFX;

		MaterialLib.MaterialLib	mMatLib;
		AnimLib					mAnimLib;
		Character				mCharacter;
		StaticMeshObject		mStaticMesh;

		UtilityLib.GameCamera	mGameCam;

		//material gui
		SharedForms.MaterialForm	mMF;

		//animation gui
		AnimForm	mCF;
		string		mCurrentAnimName;
		float		mTimeScale;			//anim playback speed

		Texture2D	mDesu;
		Texture2D	mEureka;
		Vector3		mLightDir;

		//number of bounds drawing
		int	mNumBounds;


		public static event EventHandler	eAnimsUpdated;

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

				IsMouseVisible	=true;
			}
		}


		protected override void Initialize()
		{
			mGameCam	=new UtilityLib.GameCamera(mGDM.GraphicsDevice.Viewport.Width,
				mGDM.GraphicsDevice.Viewport.Height,
				mGDM.GraphicsDevice.Viewport.AspectRatio);

			//default cam pos off to one side
//			Vector3	camPos	=Vector3.Zero;
//			camPos.X	=102.0f;
//			camPos.Y	=-96.0f;
//			camPos.Z	=187.0f;

//			mGameCam.CamPos	=-camPos;

			base.Initialize();
		}


		protected override void LoadContent()
		{
			mSharedCM	=new ContentManager(Services, "SharedContent");
			mMatLib		=new MaterialLib.MaterialLib(mGDM.GraphicsDevice, Content, mSharedCM, true);
			mAnimLib	=new AnimLib();
			mCharacter	=new Character(mMatLib, mAnimLib);
			mStaticMesh	=new StaticMeshObject(mMatLib);

			//load debug shaders
			mFX	=mSharedCM.Load<Effect>("Shaders/Static");

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

			//create vertex and index buffers
			mIB	=new IndexBuffer(mGDM.GraphicsDevice, IndexElementSize.SixteenBits, 12, BufferUsage.WriteOnly);
			mVB	=new VertexBuffer(mGDM.GraphicsDevice, typeof(VertexPositionNormalTexture), 8, BufferUsage.WriteOnly);

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

			mMF	=new SharedForms.MaterialForm(mGDM.GraphicsDevice, mMatLib, true);
			mMF.Visible	=true;

			//bind matform window position
			mMF.DataBindings.Add(new System.Windows.Forms.Binding("Location",
				global::ColladaConvert.Properties.Settings.Default,
				"MaterialFormPos", true,
				System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));

//			mMF.eBoundsUpdated	+=OnBoundsChanged;
			mMF.eNukedMeshPart	+=OnNukedMeshPart;
//			mMF.eBoundMesh		+=OnBoundMesh;
		}


		protected override void UnloadContent()
		{
		}


		void InitializeEffect()
		{
			Vector4	[]lightColor	=new Vector4[3];
			lightColor[0]	=new Vector4(0.9f, 0.9f, 0.9f, 1.0f);
			lightColor[1]	=new Vector4(0.6f, 0.5f, 0.5f, 1.0f);
			lightColor[2]	=new Vector4(0.1f, 0.1f, 0.1f, 1.0f);

			Vector4	ambColor	=new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
			Vector3	lightDir	=new Vector3(1.0f, -1.0f, 0.1f);
			lightDir.Normalize();
		}


		void OnOpenAnim(object sender, EventArgs ea)
		{
			string	path	=(string)sender;

			if(ColladaFileUtils.LoadAnim(path, mAnimLib))
			{
				eAnimsUpdated(mAnimLib.GetAnims(), null);
			}
		}


		void OnOpenModel(object sender, EventArgs ea)
		{
			mCharacter	=ColladaFileUtils.LoadCharacter(sender as string, mGDM.GraphicsDevice, mMatLib, mAnimLib);

			mMF.UpdateMeshPartList(mCharacter.GetMeshPartList(), null);
			eAnimsUpdated(mAnimLib.GetAnims(), null);
		}


		void OnNukedMeshPart(object sender, EventArgs ea)
		{
			Mesh	msh	=sender as Mesh;

			mCharacter.NukeMesh(msh);
		}


		void OnBoundMesh(object sender, EventArgs ea)
		{
			mCharacter.Bound();
		}


		void OnBoundsChanged(object sender, EventArgs ea)
		{
			//clear existing
			mBoundsVB	=null;
			mBoundsIB	=null;

			List<IRayCastable>	bnds	=(List<IRayCastable>)sender;

			if(bnds.Count <= 0)
			{
				bnds.Add(mCharacter.GetBounds());
			}
			ReBuildBoundsDrawData(bnds);
		}


		void ReBuildBoundsDrawData(List<IRayCastable> bnds)
		{
			if(bnds.Count == 0)
			{
				return;
			}

			int	numCorners	=0;
			foreach(IRayCastable bnd in bnds)
			{
				numCorners	+=2;
			}
			mBoundsVB	=new VertexBuffer(mGDM.GraphicsDevice, typeof(VertexPositionNormalTexture), numCorners * 4, BufferUsage.WriteOnly);

			VertexPositionNormalTexture	[]vpnt	=new VertexPositionNormalTexture[numCorners * 4];

			int	idx	=0;
			foreach(IRayCastable bnd in bnds)
			{
				Vector3	min, max;

				bnd.GetMinMax(out min, out max);

				float	xDiff	=max.X - min.X;
				float	yDiff	=max.Y - min.Y;
				float	zDiff	=max.Z - min.Z;

				vpnt[idx++].Position	=min;
				vpnt[idx++].Position	=min + Vector3.UnitX * xDiff;
				vpnt[idx++].Position	=min + Vector3.UnitX * xDiff + Vector3.UnitY * yDiff;
				vpnt[idx++].Position	=min + Vector3.UnitY * yDiff;
				vpnt[idx++].Position	=min + Vector3.UnitZ * zDiff;
				vpnt[idx++].Position	=min + Vector3.UnitZ * zDiff + Vector3.UnitX * xDiff;
				vpnt[idx++].Position	=min + Vector3.UnitZ * zDiff + Vector3.UnitY * yDiff;
				vpnt[idx++].Position	=min + Vector3.UnitZ * zDiff + Vector3.UnitX * xDiff + Vector3.UnitY * yDiff;
			}
			mBoundsVB.SetData<VertexPositionNormalTexture>(vpnt);

			mBoundsIB	=new IndexBuffer(mGDM.GraphicsDevice, IndexElementSize.SixteenBits, (numCorners * 3) * 6, BufferUsage.WriteOnly);

			UInt16	[]inds	=new UInt16[(numCorners * 3) * 6];

			mNumBounds	=numCorners / 2;

			idx	=0;
			UInt16 bndIdx	=0;
			foreach(IRayCastable bnd in bnds)
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
		void OnOpenStaticModel(object sender, EventArgs ea)
		{
			string	path	=(string)sender;

			mStaticMesh	=ColladaFileUtils.LoadStatic(path, mGDM.GraphicsDevice, mMatLib);

			mMF.UpdateMeshPartList(null, mStaticMesh.GetMeshPartList());
		}


		void OnSaveLibrary(object sender, EventArgs ea)
		{
			string	path	=(string)sender;

			mAnimLib.SaveToFile(path);
		}


		void OnLoadLibrary(object sender, EventArgs ea)
		{
			string	path	=(string)sender;

			mAnimLib.ReadFromFile(path, true);
			eAnimsUpdated(mAnimLib.GetAnims(), null);
		}


		void OnSaveCharacter(object sender, EventArgs ea)
		{
			string	path	=(string)sender;

			mCharacter.SaveToFile(path);
		}


		void OnLoadCharacter(object sender, EventArgs ea)
		{
			string	path	=(string)sender;

			mCharacter	=new Character(mMatLib, mAnimLib);
			mCharacter.ReadFromFile(path, mGDM.GraphicsDevice, true);

			mMF.UpdateMeshPartList(mCharacter.GetMeshPartList(), null);
		}


		void OnLoadStatic(object sender, EventArgs ea)
		{
			string	path	=(string)sender;

			mStaticMesh.ReadFromFile(path, mGDM.GraphicsDevice, true);

			mMF.UpdateMeshPartList(null, mStaticMesh.GetMeshPartList());
		}


		void OnSaveStatic(object sender, EventArgs ea)
		{
			string	path	=(string)sender;

			mStaticMesh.SaveToFile(path);
		}


		void OnAnimSelChanged(object sender, EventArgs ea)
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


		void OnTimeScaleChanged(object sender, EventArgs ea)
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

			float	msDelta	=gameTime.ElapsedGameTime.Milliseconds;

			KeyboardState	kbs	=Keyboard.GetState();

			mGameCam.Update(msDelta, kbs, Mouse.GetState());

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

			mMatLib.UpdateWVP(mGameCam.World, mGameCam.View, mGameCam.Projection, -mGameCam.CamPos);

			//put in some keys for messing with bones
			float	time		=(float)gameTime.ElapsedGameTime.TotalMilliseconds;

			mCharacter.Animate(mCurrentAnimName, (float)(gameTime.TotalGameTime.TotalSeconds) * mTimeScale);

			base.Update(gameTime);
		}


		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice	g	=mGDM.GraphicsDevice;

			g.Clear(Color.CornflowerBlue);

			mCharacter.Draw(g);
			mStaticMesh.Draw(g);

			g.SetVertexBuffer(mVB);
			g.Indices	=mIB;

			//default light direction
			mLightDir.X	=-0.3f;
			mLightDir.Y	=-1.0f;
			mLightDir.Z	=-0.2f;
			mLightDir.Normalize();

			mFX.Parameters["mLightDirection"].SetValue(mLightDir);

			g.BlendState	=BlendState.AlphaBlend;

			mFX.CurrentTechnique	=mFX.Techniques[0];
			
			mFX.Parameters["mTexture"].SetValue(mEureka);

			mFX.CurrentTechnique.Passes[0].Apply();

			g.DrawIndexedPrimitives(PrimitiveType.TriangleList,
				4, 0, 4, 0, 2);


			mFX.Parameters["mTexture"].SetValue(mDesu);

			mFX.CurrentTechnique.Passes[0].Apply();

			g.DrawIndexedPrimitives(PrimitiveType.TriangleList,
				0, 0, 4, 0, 2);

			//draw bounds if any
			if(mBoundsVB != null)// && mMF.bDrawBounds())
			{
				g.SetVertexBuffer(mBoundsVB);
				g.Indices	=mBoundsIB;

				mFX.CurrentTechnique.Passes[0].Apply();

				g.DrawIndexedPrimitives(PrimitiveType.TriangleList,
					0, 0, 8, 0, 6 * 2 * mNumBounds);
			}

			base.Draw(gameTime);
		}
	}
}
