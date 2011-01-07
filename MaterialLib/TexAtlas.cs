using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Storage;


namespace MaterialLib
{
	//based on this article
	//http://www.blackpawn.com/texts/lightmaps/default.html
	class TexNode
	{
		TexNode		mFront;
		TexNode		mBack;
		Rectangle	mRect;
		bool		mbOccupied;


		public TexNode() { }
		public TexNode(int w, int h)
		{
			mRect.X			=0;
			mRect.Y			=0;
			mRect.Width		=w;
			mRect.Height	=h;
		}


		public Rectangle GetRect()
		{
			return	mRect;
		}


		public TexNode Insert(int texW, int texH)
		{
			TexNode	ret	=null;

			if(mFront != null || mBack != null)
			{
				if(mFront != null)
				{
					ret	=mFront.Insert(texW, texH);
				}
				if(ret != null)
				{
					return	ret;
				}
				if(mBack != null)
				{
					ret	=mBack.Insert(texW, texH);
				}
				if(ret != null)
				{
					return	ret;
				}
				return	ret;
			}

			if(mbOccupied)
			{
				return	null;
			}

			if(((mRect.Right - mRect.Left) < texW)
				|| ((mRect.Bottom - mRect.Top) < texH))
			{
				return	null;	//too small
			}
			else if(((mRect.Right - mRect.Left) == texW)
				&& ((mRect.Bottom - mRect.Top) == texH))
			{
				mbOccupied	=true;	//just right
				return	this;
			}

			//split
			int dw	=(mRect.Right - mRect.Left) - texW;
			int dh	=(mRect.Bottom - mRect.Top) - texH;

			mFront	=new TexNode();
			mBack	=new TexNode();

			if(dw > dh)
			{
				mFront.mRect	=new Rectangle(mRect.Left, mRect.Top,
									texW,
									(mRect.Bottom - mRect.Top));

				mBack.mRect	=new Rectangle((mRect.Left + texW),
								mRect.Top,
								(mRect.Right - mRect.Left - texW),
								(mRect.Bottom - mRect.Top));
			}
			else
			{
				mFront.mRect	=new Rectangle(mRect.Left, mRect.Top,
									(mRect.Right - mRect.Left),
									texH);

				mBack.mRect	=new Rectangle(mRect.Left,
								(mRect.Top + texH),
								(mRect.Right - mRect.Left),
								(mRect.Bottom - mRect.Top - texH));
			}

			return	mFront.Insert(texW, texH);
		}


		internal void Write(BinaryWriter bw)
		{
			bw.Write(mRect.X);
			bw.Write(mRect.Y);
			bw.Write(mRect.Width);
			bw.Write(mRect.Height);
			bw.Write(mbOccupied);

			bw.Write(mFront != null);
			bw.Write(mBack != null);

			if(mFront != null)
			{
				mFront.Write(bw);
			}
			if(mBack != null)
			{
				mBack.Write(bw);
			}
		}


		internal void Read(BinaryReader br)
		{
			mRect.X			=br.ReadInt32();
			mRect.Y			=br.ReadInt32();
			mRect.Width		=br.ReadInt32();
			mRect.Height	=br.ReadInt32();
			mbOccupied		=br.ReadBoolean();

			bool	bFront	=br.ReadBoolean();
			bool	bBack	=br.ReadBoolean();

			if(bFront)
			{
				mFront	=new TexNode();
				mFront.Read(br);
			}
			if(bBack)
			{
				mBack	=new TexNode();
				mBack.Read(br);
			}
		}
	}


	public class TexAtlas
	{
		Texture2D	mAtlasTexture;
		TexNode		mRoot;
		Color		[]mBuildArray;
		int			mWidth, mHeight;


		public int Width
		{
			get { return mWidth; }
		}
		public int Height
		{
			get { return mHeight; }
		}


		public TexAtlas(GraphicsDevice g, int width, int height)
		{
			mWidth	=width;
			mHeight	=height;

			mRoot			=new TexNode(width, height);
			mAtlasTexture	=new Texture2D(g, width, height,
								false, SurfaceFormat.Color);
		}


		public Texture2D GetAtlasTexture()
		{
			return	mAtlasTexture;
		}


		public void Finish()
		{
			if(mBuildArray != null)
			{
				mAtlasTexture.SetData<Color>(mBuildArray);
			}

			mBuildArray	=null;
		}


		public bool Insert(Color[] tex, int texW, int texH,
			out double scaleU, out double scaleV, out double uoffs, out double voffs)
		{
			TexNode	n	=mRoot.Insert(texW, texH);

			scaleU = scaleV = uoffs = voffs = 0.0;

			if(n == null)
			{
				return false;
			}

			//copy pixels in
			if(mBuildArray == null)
			{
				mBuildArray	=new Color[mWidth * mHeight];
				mAtlasTexture.GetData<Color>(mBuildArray);
			}

			Rectangle	target	=n.GetRect();

			int c	=0;
			for(int y=target.Top;y < target.Bottom;y++)
			{
				for(int x=target.Left;x < target.Right;x++, c++)
				{
					mBuildArray[(y * mWidth) + x]	=tex[c];
				}
			}

			//maybe squeeze a little extra precision here
			scaleU	=(double)texW / (double)mWidth;
			scaleV	=(double)texH / (double)mHeight;

			//get offsets in zero to one space
			uoffs	=((double)target.Left / (double)mWidth);
			voffs	=((double)target.Top / (double)mHeight);

			return	true;
		}


		public void Write(BinaryWriter bw)
		{
			//grab color bits
			byte	[]atlasTex	=new byte[mWidth * mHeight * 4];
			mAtlasTexture.GetData<byte>(atlasTex);

			bw.Write(mWidth);
			bw.Write(mHeight);
			bw.Write(atlasTex, 0, atlasTex.Length);

			mRoot.Write(bw);
		}


		public void Read(GraphicsDevice g, BinaryReader br)
		{
			mWidth	=br.ReadInt32();
			mHeight	=br.ReadInt32();

			mAtlasTexture	=new Texture2D(g, mWidth, mHeight,
								false, SurfaceFormat.Color);
#if XBOX
			Color	[]atlas	=new Color[mWidth * mHeight];
			for(int i=0;i < (mWidth * mHeight);i++)
			{
				byte	R	=br.ReadByte();
				byte	G	=br.ReadByte();
				byte	B	=br.ReadByte();
				byte	A	=br.ReadByte();

				int	ir	=R;
				int	ig	=G;
				int	ib	=B;

				ir	*=2;
				ig	*=2;
				ib	*=2;

				atlas[i]	=new Color(ir, ig, ib, A);
			}
			mAtlasTexture.SetData<Color>(atlas);
#else
			byte	[]atlas	=br.ReadBytes(mWidth * mHeight * 4);

			mAtlasTexture.SetData<byte>(atlas);
#endif
			mRoot	=new TexNode(mWidth, mHeight);
			mRoot.Read(br);
		}
	}
}