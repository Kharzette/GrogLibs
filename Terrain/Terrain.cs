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
	public struct VPNTT
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Vector2	TexCoord0;
		public Vector2	TexCoord1;
	};


	public class Terrain : Microsoft.Xna.Framework.Game
	{
		//drawing
		private	GraphicsDeviceManager	mGDM;
		private	SpriteBatch				mSpriteBatch;

		//maps & mats
		private	Matrix	mMATWorld;
		private	Matrix	mMATView;
		private	Matrix	mMATViewTranspose;
		private	Matrix	mMATProjection;

		//verts & effects
		private	Effect				mFXTerrain;
		private	VertexDeclaration	mVDTerrain;
		private	VertexBuffer		mVBTerrain;
		private	IndexBuffer			mIBTerrain;
		private	Texture2D			mTEXTerrain0;
		private	Texture2D			mTEXTerrain1;

		//control
		private	GamePadState	mGPStateCurrent;
		private	GamePadState	mGPStateLast;
		private	KeyboardState	mKBStateCurrent;
		private	MouseState		mMStateCurrent;
		private	MouseState		mMStateLast;

		//crap
		private	string	mHeightMapFileName;
		private	int		mNumIndex, mNumVerts, mNumTris;

		//cam / player stuff will move later
		private Vector3	mCamPos, mDotPos;
		private float	mPitch, mYaw, mRoll;


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

			//arg zero should be the map name
			mHeightMapFileName   =args[0];
			for(int i = 0;i < args.GetLength(0);i++)
			{
				Debug.WriteLine(args[i]);
			}
			mCamPos.X	=-192.0f;
			mCamPos.Y	=-64.0f;
			mCamPos.Z	=-64.0f;
		}


		protected override void Initialize()
		{
			mPitch	=mYaw	=mRoll	=0;
			InitializeEffect();
			InitializeTransform();

			base.Initialize();
		}


		protected override void LoadContent()
		{
			int			w, h;
			Texture2D	heightTex;

			mSpriteBatch	=new SpriteBatch(GraphicsDevice);

			if(false)
			{
				FileStream	file	=OpenTitleFile(mHeightMapFileName,
										FileMode.Open, FileAccess.Read);

				BinaryReader	br	=new BinaryReader(file);

				//convert height map to verts
				w	=1024;
				h	=1024;
			}
			else
			{
				heightTex	=Texture2D.FromFile(mGDM.GraphicsDevice, "content/height.jpg");
				w			=heightTex.Width;
				h			=heightTex.Height;
			}

			mNumVerts	=w * h;
			mNumTris	=((w - 1) * (h - 1)) * 2;
			mNumIndex	=mNumTris * 3;

			Color	[]col	=new Color[w * h];

			heightTex.GetData<Color>(col);

			//alloc some space for verts and indexs
			VPNTT	[]verts		=new VPNTT[mNumVerts];
			ushort	[]indexs	=new ushort[mNumIndex];

			//load the height map
			ushort	idx	=0;
			for(int y=0;y < h;y++)
			{
				for(int x=0;x < w;x++)
				{
					int	dex	=x + (y * w);
					verts[dex].Position.X	=(float)x * 10.0f;
					verts[dex].Position.Z	=(float)y * 10.0f;
//					verts[dex].Position.Z	=(float)br.ReadByte();
					verts[dex].Position.Y	=((float)col[idx++].R) / 5.0f;

					//texcoords
					verts[dex].TexCoord0.X	=(float)x * (4.0f / w);
					verts[dex].TexCoord0.Y	=(float)y * (4.0f / h);

					verts[dex].TexCoord1.X	=(float)x * (7.0f / w);
					verts[dex].TexCoord1.Y	=(float)y * (7.0f / h);
				}
			}

			Vector3	[]adjacent	=new Vector3[8];
			bool	[]valid		=new bool[8];

			//generate normals
			for(int y=0;y < h;y++)
			{
				for(int x=0;x < w;x++)
				{
					//get the positions of the 8
					//adjacent verts, numbered clockwise
					//from upper right on a grid

					//grab first 3 spots which
					//are negative in Y
					if(y > 0)
					{
						if(x > 0)
						{
							adjacent[0]	=verts[(x - 1) + ((y - 1) * w)].Position;
							valid[0]	=true;
						}
						else
						{
							valid[0]	=false;
						}

						adjacent[1]	=verts[x + ((y - 1) * w)].Position;
						valid[1]	=true;

						if(x < (w - 1))
						{
							adjacent[2]	=verts[(x + 1) + ((y - 1) * w)].Position;
							valid[2]	=true;
						}
						else
						{
							valid[2]	=false;
						}
					}
					else
					{
						valid[0]	=false;
						valid[1]	=false;
						valid[2]	=false;
					}

					//next two are to the sides of
					//the calcing vert in X
					if(x > 0)
					{
						adjacent[7]	=verts[(x - 1) + (y * w)].Position;
						valid[7]	=true;
					}
					else
					{
						valid[7]	=false;
					}

					if(x < (w - 1))
					{
						adjacent[3]	=verts[(x + 1) + (y * w)].Position;
						valid[3]	=true;
					}
					else
					{
						valid[3]	=false;
					}

					//next three are positive in Y
					if(y < (h - 1))
					{
						if(x > 0)
						{
							adjacent[6]	=verts[(x - 1) + ((y + 1) * w)].Position;
							valid[6]	=true;
						}
						else
						{
							valid[6]	=false;
						}

						adjacent[5]	=verts[x + ((y + 1) * w)].Position;
						valid[5]	=true;

						if(x < (w - 1))
						{
							adjacent[4]	=verts[(x + 1) + ((y + 1) * w)].Position;
							valid[4]	=true;
						}
						else
						{
							valid[4]	=false;
						}
					}
					else
					{
						valid[5]	=false;
						valid[6]	=false;
						valid[4]	=false;
					}

					//use the edges between adjacents
					//to determine a good normal
					Vector3	norm, edge1, edge2;

					norm	=Vector3.Zero;

					for(int i=0;i < 8;i++)
					{
						//find next valid adjacent
						while(i < 8 && !valid[i])
						{
							i++;
						}
						if(i >= 8)
						{
							break;
						}

						//note the i++
						edge1	=adjacent[i++] - verts[x + (y * w)].Position;

						//find next valid adjacent
						while(i < 8 && !valid[i])
						{
							i++;
						}
						if(i >= 8)
						{
							break;
						}
						edge2	=adjacent[i] - verts[x + (y * w)].Position;

						norm	+=Vector3.Cross(edge2, edge1);
					}

					//average
					norm.Normalize();

					verts[x + (y * w)].Normal	=norm;
				}
			}


			//index the tris
			idx	=0;

			for(int j=0;j < (h - 1);j++)
			{
				for(int i=(j * w);i < ((j * w) + (w - 1));i++)
				{
					indexs[idx++]	=(ushort)i;
					indexs[idx++]	=(ushort)(i + 1);
					indexs[idx++]	=(ushort)(i + w);

					indexs[idx++]	=(ushort)(i + 1);
					indexs[idx++]	=(ushort)((i + 1) + w);
					indexs[idx++]	=(ushort)(i + w);
				}
			}

			mIBTerrain	=new IndexBuffer(mGDM.GraphicsDevice, mNumIndex * 2, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
			mVBTerrain	=new VertexBuffer(mGDM.GraphicsDevice, mNumVerts * 40, BufferUsage.WriteOnly);

			mIBTerrain.SetData<ushort>(indexs);
			mVBTerrain.SetData<VPNTT>(verts);

			mTEXTerrain0	=Texture2D.FromFile(mGDM.GraphicsDevice, "content/dirt_simple_df_.dds");
			mTEXTerrain1	=Texture2D.FromFile(mGDM.GraphicsDevice, "content/dirt_mottledsand_df_.dds");

			mFXTerrain	=Content.Load<Effect>("Terrain");

			//set up shader stuff that won't change for now
			Vector4	colr;
			colr.X	=1.0f;
			colr.Y	=0.9f;
			colr.Z	=0.9f;
			colr.W	=1.0f;
			mFXTerrain.Parameters["lightColor"].SetValue(colr);

			colr.X	=0.1f;
			colr.Y	=0.1f;
			colr.Z	=0.1f;
			colr.W	=1.0f;
			mFXTerrain.Parameters["ambientColor"].SetValue(colr);

			Vector3	dir;
			dir.X	=0.2f;
			dir.Z	=-0.1f;
			dir.Y	=-1.0f;
			dir.Normalize();

			mFXTerrain.Parameters["lightDirection"].SetValue(dir);

			mFXTerrain.Parameters["TerTexture0"].SetValue(mTEXTerrain0);
			mFXTerrain.Parameters["TerTexture1"].SetValue(mTEXTerrain1);
			mFXTerrain.CommitChanges();

			//set stream source
			mGDM.GraphicsDevice.Vertices[0].SetSource(mVBTerrain, 0, 40);
			mGDM.GraphicsDevice.Indices	=mIBTerrain;
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

			base.Update(gameTime);
		}


		protected override void Draw(GameTime gameTime)
		{
			mGDM.GraphicsDevice.Clear(Color.CornflowerBlue);

			UpdateTerrainEffect();

			mGDM.GraphicsDevice.VertexDeclaration	=mVDTerrain;
			mFXTerrain.Begin();
			foreach(EffectPass pass in mFXTerrain.CurrentTechnique.Passes)
			{
				pass.Begin();
				
				//draw shizzle here
				mGDM.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList,
					0, 0, mNumVerts, 0, mNumTris);

				pass.End();
			}
			mFXTerrain.End();

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
			mDotPos	=-mCamPos + mMATViewTranspose.Forward * 10.0f;
		}


		private void InitializeEffect()
		{
			//set up a 2 texcoord vert element
			VertexElement	[]ve	=new VertexElement[4];

			ve[0]	=new VertexElement(0, 0, VertexElementFormat.Vector3,
						VertexElementMethod.Default, VertexElementUsage.Position, 0);
			ve[1]	=new VertexElement(0, 12, VertexElementFormat.Vector3,
						VertexElementMethod.Default, VertexElementUsage.Normal, 0);
			ve[2]	=new VertexElement(0, 24, VertexElementFormat.Vector2,
						VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 0);
			ve[3]	=new VertexElement(0, 32, VertexElementFormat.Vector2,
						VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 1);

			mVDTerrain	=new VertexDeclaration(mGDM.GraphicsDevice,	ve);
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


		private void UpdateTerrainEffect()
		{
			mFXTerrain.Parameters["World"].SetValue(mMATWorld);
			mFXTerrain.Parameters["View"].SetValue(mMATView);
			mFXTerrain.Parameters["Projection"].SetValue(mMATProjection);

			Vector3	dir;
			dir.X	=-0.7f;
			dir.Z	=-0.1f;
			dir.Y	=-0.5f;
			dir.Normalize();

			mFXTerrain.Parameters["lightDirection"].SetValue(dir);

			mFXTerrain.CommitChanges();
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

			if(mKBStateCurrent.IsKeyDown(Keys.Up) ||
				mKBStateCurrent.IsKeyDown(Keys.W))
			{
				mCamPos	+=vin * (time * 0.1f);
			}

			if(mKBStateCurrent.IsKeyDown(Keys.Down) ||
				mKBStateCurrent.IsKeyDown(Keys.S))
			{
				mCamPos	-=vin * (time * 0.1f);
			}

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

			if(mKBStateCurrent.IsKeyDown(Keys.Right) ||
				mKBStateCurrent.IsKeyDown(Keys.D))
			{
				mCamPos	-=vleft * (time * 0.1f);
			}

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
