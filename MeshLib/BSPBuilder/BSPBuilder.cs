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
using BSPLib;


namespace BSPBuilder
{
	public class BSPBuilder : Game
	{
		GraphicsDeviceManager	mGDM;
		SpriteBatch				mSB;

		//forms
		MainForm	mMF;

		Map	mMap;

		//debug draw stuff
		BasicEffect				mMapEffect;
		VertexBuffer			mVB;
		IndexBuffer				mIB;
		VertexDeclaration		mVD;
		GameCamera				mGameCam;
		SpriteFont				mKoot;
		int						mNumVerts;
		int						mNumTris;
		Vector2					mTextPos;
		Random					mRnd	=new Random();


		public BSPBuilder()
		{
			mGDM	=new GraphicsDeviceManager(this);
			Content.RootDirectory	="Content";

			//set window position
			if(!mGDM.IsFullScreen)
			{
				System.Windows.Forms.Control	mainWindow
					=System.Windows.Forms.Form.FromHandle(this.Window.Handle);

				//add data binding so it will save
				mainWindow.DataBindings.Add(new System.Windows.Forms.Binding("Location",
					global::BSPBuilder.Properties.Settings.Default,
					"MainWindowPos", true,
					System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));

				mainWindow.Location	=
					global::BSPBuilder.Properties.Settings.Default.MainWindowPos;

				IsMouseVisible	=true;
			}
		}


		protected override void Initialize()
		{
			mTextPos	=Vector2.One * 20.0f;

			mGameCam	=new GameCamera(mGDM.GraphicsDevice.Viewport.Width,
				mGDM.GraphicsDevice.Viewport.Height,
				mGDM.GraphicsDevice.Viewport.AspectRatio);

			base.Initialize();
		}


		protected override void LoadContent()
		{
			mSB	=new SpriteBatch(GraphicsDevice);

			mMF	=new MainForm();
			mMF.Visible	=true;
			mMF.eOpenVMF			+=OnOpenVMF;
			mMF.eEntityIndChanged	+=OnEntityIndexChanged;

			mKoot	=Content.Load<SpriteFont>("Fonts/Kootenay");

			mVD	=new VertexDeclaration(mGDM.GraphicsDevice,
				VertexPositionColorTexture.VertexElements);

			mMapEffect	=new BasicEffect(mGDM.GraphicsDevice, null);

			mMapEffect.TextureEnabled		=false;
			mMapEffect.DiffuseColor			=Vector3.One;
			mMapEffect.VertexColorEnabled	=true;

			//tired of that gump
//			OnOpenVMF("C:\\Users\\kbaird\\Documents\\sdk_arena_lumberyard.vmf", null);
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

			mGameCam.Update(msDelta, kbs, Mouse.GetState());

			mMapEffect.World		=mGameCam.World;
			mMapEffect.View			=mGameCam.View;
			mMapEffect.Projection	=mGameCam.Projection;

			base.Update(gameTime);
		}


		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice	g	=mGDM.GraphicsDevice;

			g.Clear(Color.CornflowerBlue);

			if(mVB != null)
			{
				g.VertexDeclaration		=mVD;
				g.Vertices[0].SetSource(mVB, 0, VertexPositionColorTexture.SizeInBytes);
				g.Indices	=mIB;

				g.RenderState.DepthBufferEnable	=true;
//				g.RenderState.CullMode	=CullMode.CullClockwiseFace;

				mMapEffect.Begin();
				foreach(EffectPass pass in mMapEffect.CurrentTechnique.Passes)
				{
					pass.Begin();

					g.DrawIndexedPrimitives(PrimitiveType.TriangleList,
						0, 0, mNumVerts, 0, mNumTris);

					pass.End();
				}
				mMapEffect.End();
			}

			mSB.Begin();

			mSB.DrawString(mKoot, "Coordinates: " + mGameCam.CamPos, mTextPos, Color.Yellow);

			mSB.End();

			base.Draw(gameTime);
		}


		void MakeDrawData()
		{
			List<Vector3>	verts	=new List<Vector3>();
			List<UInt16>	indexes	=new List<UInt16>();

			mMap.GetTriangles(verts, indexes, mMF.EntityInd);
			if(verts.Count <= 0)
			{
				return;
			}

			mNumVerts	=verts.Count;
			mNumTris	=indexes.Count / 3;

			mVB	=new VertexBuffer(mGDM.GraphicsDevice,
				VertexPositionColorTexture.SizeInBytes * verts.Count,
				BufferUsage.WriteOnly);
			mIB	=new IndexBuffer(mGDM.GraphicsDevice,
				2 * indexes.Count, BufferUsage.WriteOnly,
				IndexElementSize.SixteenBits);

			VertexPositionColorTexture	[]vpnt
				=new VertexPositionColorTexture[mNumVerts];

			int		cnt	=0;
			Color	col	=Color.White;
			foreach(Vector3 vert in verts)
			{
				if((cnt % 6) == 0)
				{
					col	=UtilityLib.Mathery.RandomColor(mRnd);
				}
				vpnt[cnt].Position				=vert;
				vpnt[cnt].Color					=col;
				vpnt[cnt++].TextureCoordinate	=Vector2.Zero;
			}

			mVB.SetData<VertexPositionColorTexture>(vpnt);
			mIB.SetData<UInt16>(indexes.ToArray());
		}


		void OnEntityIndexChanged(object sender, EventArgs ea)
		{
			MakeDrawData();
			mMF.NumberOfPortals	="" + mMap.GetNumPortals(mMF.EntityInd);
		}


		void OnOpenVMF(object sender, EventArgs ea)
		{
			string	fileName	=sender as string;

			if(fileName != null)
			{
				mMap	=new Map(fileName);

//				mMap.RemoveOverlap();
				mMap.BuildTree((float)mMF.BevelHullSize, mMF.bBevels);

				MakeDrawData();

				mMF.NumberOfPortals	="" + mMap.GetNumPortals(mMF.EntityInd);
			}
		}


		void OnSaveVMF(object sender, EventArgs ea)
		{
			string	fileName	=sender as string;

			if(fileName != null)
			{
				mMap.Save(fileName);
			}
		}
	}
}