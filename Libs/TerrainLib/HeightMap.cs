using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;


namespace Terrain
{
	public struct VPNTT
	{
		public Vector3	Position;
		public Vector3	Normal;
		public Vector2	TexCoord0;
		public Vector2	TexCoord1;
	};


	public class HeightMap
	{
		private	VertexDeclaration	mVDTerrain;
		private	VertexBuffer		mVBTerrain;
		private	IndexBuffer			mIBTerrain;
		private	Texture2D			mTEXTerrain0;
		private	Texture2D			mTEXTerrain1;
		private	Effect				mFX2TexGourad;

		private	int		mNumIndex, mNumVerts, mNumTris;
		private	int		mWidth, mHeight;
		private	float	mTileFactor0, mTileFactor1;
		private	Vector3	mPosition, mDirection;
		private	Matrix	mMat;


		//this version loads raw images
		public HeightMap(string			hmFileName,
						 int			width,
						 int			height,
						 string			tex0FileName,
						 string			tex1FileName,
						 float			tileFactor0,
						 float			tileFactor1,
						 GraphicsDevice	gd,
						 ContentManager	cm)
		{
			FileStream	file	=Terrain.OpenTitleFile(hmFileName,
									FileMode.Open, FileAccess.Read);

			BinaryReader	br	=new BinaryReader(file);

			//convert height map to verts
			int	w	=width;
			int	h	=height;

			mNumVerts	=w * h;
			mNumTris	=((w - 1) * (h - 1)) * 2;
			mNumIndex	=mNumTris * 3;

			//alloc some space for verts and indexs
			VPNTT	[]verts		=new VPNTT[mNumVerts];
			ushort	[]indexs	=new ushort[mNumIndex];

			//load the height map
			for(int y=0;y < h;y++)
			{
				for(int x=0;x < w;x++)
				{
					int	dex	=x + (y * w);
					verts[dex].Position.X	=(float)x * 10.0f;
					verts[dex].Position.Z	=(float)y * 10.0f;
					verts[dex].Position.Z	=(float)br.ReadByte();

					//texcoords
					verts[dex].TexCoord0.X	=(float)x * (4.0f / w);
					verts[dex].TexCoord0.Y	=(float)y * (4.0f / h);

					verts[dex].TexCoord1.X	=(float)x * (7.0f / w);
					verts[dex].TexCoord1.Y	=(float)y * (7.0f / h);
				}
			}

			BuildNormals(ref verts, w, h);
			IndexTris(w, h, gd);

			mVBTerrain	=new VertexBuffer(gd, mNumVerts * 40, BufferUsage.WriteOnly);
			mVBTerrain.SetData<VPNTT>(verts);

			mTEXTerrain0	=Texture2D.FromFile(gd, tex0FileName);
			mTEXTerrain1	=Texture2D.FromFile(gd, tex1FileName);

			InitVertexDeclaration(gd);
			InitEffect(cm);
		}


		//this one will load any texture type file
		public HeightMap(string			hmFileName,
						 string			tex0FileName,
						 string			tex1FileName,
						 float			tileFactor0,
						 float			tileFactor1,
						 GraphicsDevice	gd,
						 ContentManager	cm)
		{
			Texture2D	heightTex	=Texture2D.FromFile(gd, hmFileName);

			mWidth		=heightTex.Width;
			mHeight		=heightTex.Height;

			int	w	=mWidth;
			int	h	=mHeight;

			//center the map
			mPosition	=-Vector3.UnitX * (w / 2) * 10.0f;
			mPosition	+=Vector3.UnitY * (h / 2) * 10.0f;
			mMat		=Matrix.CreateTranslation(mPosition);

			mNumVerts		=w * h;
			mNumTris		=((w - 1) * (h - 1)) * 2;
			mNumIndex		=mNumTris * 3;
			mTileFactor0	=tileFactor0;
			mTileFactor1	=tileFactor1;

			Color	[]col	=new Color[w * h];

			heightTex.GetData<Color>(col);

			//alloc some space for verts
			VPNTT	[]verts		=new VPNTT[mNumVerts];

			//build from the texture pixels
			ushort	idx	=0;
			for(int y=0;y < h;y++)
			{
				for(int x=0;x < w;x++)
				{
					int	dex	=x + (y * w);
					verts[dex].Position.X	=(float)x * 10.0f;
					verts[dex].Position.Z	=(float)y * 10.0f;
					verts[dex].Position.Y	=((float)col[idx++].R) / 5.0f;

					//texcoords
					verts[dex].TexCoord0.X	=(float)x * (mTileFactor0 / w);
					verts[dex].TexCoord0.Y	=(float)y * (mTileFactor0 / h);

					verts[dex].TexCoord1.X	=(float)x * (mTileFactor1 / w);
					verts[dex].TexCoord1.Y	=(float)y * (mTileFactor1 / h);
				}
			}

			BuildNormals(ref verts, w, h);
			IndexTris(w, h, gd);

			mVBTerrain	=new VertexBuffer(gd, mNumVerts * 40, BufferUsage.WriteOnly);
			mVBTerrain.SetData<VPNTT>(verts);

			mTEXTerrain0	=Texture2D.FromFile(gd, tex0FileName);
			mTEXTerrain1	=Texture2D.FromFile(gd, tex1FileName);

			InitVertexDeclaration(gd);
			InitEffect(cm);
		}


		private	void IndexTris(int w, int h, GraphicsDevice gd)
		{
			ushort	[]indexs	=new ushort[mNumIndex];

			//index the tris
			ushort	idx	=0;
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
			mIBTerrain	=new IndexBuffer(gd, mNumIndex * 2, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
			mIBTerrain.SetData<ushort>(indexs);
		}


		private void BuildNormals(ref VPNTT []v, int w, int h)
		{
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
							adjacent[0]	=v[(x - 1) + ((y - 1) * w)].Position;
							valid[0]	=true;
						}
						else
						{
							valid[0]	=false;
						}

						adjacent[1]	=v[x + ((y - 1) * w)].Position;
						valid[1]	=true;

						if(x < (w - 1))
						{
							adjacent[2]	=v[(x + 1) + ((y - 1) * w)].Position;
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
						adjacent[7]	=v[(x - 1) + (y * w)].Position;
						valid[7]	=true;
					}
					else
					{
						valid[7]	=false;
					}

					if(x < (w - 1))
					{
						adjacent[3]	=v[(x + 1) + (y * w)].Position;
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
							adjacent[6]	=v[(x - 1) + ((y + 1) * w)].Position;
							valid[6]	=true;
						}
						else
						{
							valid[6]	=false;
						}

						adjacent[5]	=v[x + ((y + 1) * w)].Position;
						valid[5]	=true;

						if(x < (w - 1))
						{
							adjacent[4]	=v[(x + 1) + ((y + 1) * w)].Position;
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
						edge1	=adjacent[i++] - v[x + (y * w)].Position;

						//find next valid adjacent
						while(i < 8 && !valid[i])
						{
							i++;
						}
						if(i >= 8)
						{
							break;
						}
						edge2	=adjacent[i] - v[x + (y * w)].Position;

						norm	+=Vector3.Cross(edge2, edge1);
					}

					//average
					norm.Normalize();

					v[x + (y * w)].Normal	=norm;
				}
			}
		}

		private	void InitEffect(ContentManager cm)
		{
			mFX2TexGourad	=cm.Load<Effect>("Shaders/2TexGourad");

			//set up shader stuff that won't change for now
			Vector4	colr;
			colr.X	=1.0f;
			colr.Y	=0.9f;
			colr.Z	=0.9f;
			colr.W	=1.0f;
			mFX2TexGourad.Parameters["mLightColor"].SetValue(colr);

			colr.X	=0.1f;
			colr.Y	=0.1f;
			colr.Z	=0.1f;
			colr.W	=1.0f;
			mFX2TexGourad.Parameters["mAmbientColor"].SetValue(colr);

			Vector3	dir;
			dir.X	=0.4f;
			dir.Z	=-0.1f;
			dir.Y	=-0.6f;
			dir.Normalize();

			mFX2TexGourad.Parameters["mLightDirection"].SetValue(dir);

			mFX2TexGourad.Parameters["mTerTexture0"].SetValue(mTEXTerrain0);
			mFX2TexGourad.Parameters["mTerTexture1"].SetValue(mTEXTerrain1);
			mFX2TexGourad.CommitChanges();

			//init direction
			mDirection	=-Vector3.UnitY;
		}


		public void Draw(GraphicsDevice gd)
		{
			//set stream source
			gd.Vertices[0].SetSource(mVBTerrain, 0, 40);
			gd.Indices				=mIBTerrain;
			gd.VertexDeclaration	=mVDTerrain;

			//set local matrix
			mFX2TexGourad.Parameters["mLocal"].SetValue(mMat);

			mFX2TexGourad.Begin();
			foreach(EffectPass pass in mFX2TexGourad.CurrentTechnique.Passes)
			{
				pass.Begin();
				
				//draw shizzle here
				gd.DrawIndexedPrimitives(PrimitiveType.TriangleList,
					0, 0, mNumVerts, 0, mNumTris);

				pass.End();
			}
			mFX2TexGourad.End();
		}


		private	void InitVertexDeclaration(GraphicsDevice gd)
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

			mVDTerrain	=new VertexDeclaration(gd, ve);
		}


		//matrices, matrii, matrixs?
		public void UpdateMatrices(Matrix world, Matrix view, Matrix proj)
		{
			mFX2TexGourad.Parameters["mWorld"].SetValue(world);
			mFX2TexGourad.Parameters["mView"].SetValue(view);
			mFX2TexGourad.Parameters["mProjection"].SetValue(proj);

			mFX2TexGourad.CommitChanges();
		}


		public void UpdateLightColor(Vector4 lightColor)
		{
			mFX2TexGourad.Parameters["mLightColor"].SetValue(lightColor);
			mFX2TexGourad.CommitChanges();
		}


		public void UpdateLightDirection(Vector3 lightDir)
		{
			mFX2TexGourad.Parameters["mLightDirection"].SetValue(lightDir);
			mFX2TexGourad.CommitChanges();
		}


		public void UpdateAmbientColor(Vector4 ambient)
		{
			mFX2TexGourad.Parameters["mAmbientColor"].SetValue(ambient);
			mFX2TexGourad.CommitChanges();
		}


		public void SetPos(Vector3 pos)
		{
			mPosition	=pos;

			//update matrix
			mMat	=Matrix.CreateTranslation(mPosition);
		}
	}
}
