using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Storage;


namespace BuildMap
{
	//based on this article
	//http://www.blackpawn.com/texts/lightmaps/default.html
	class TexNode
	{
		public const int TEXATLAS_WIDTH		=4096;
		public const int TEXATLAS_HEIGHT	=4096;

		TexNode		mFront;
		TexNode		mBack;
		Rectangle	mRect;
		bool		mbOccupied;


		public TexNode()
		{
			mRect.X			=0;
			mRect.Y			=0;
			mRect.Width		=TEXATLAS_WIDTH;
			mRect.Height	=TEXATLAS_HEIGHT;
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
			int	dw	=(mRect.Right - mRect.Left) - texW;
			int	dh	=(mRect.Bottom - mRect.Top) - texH;

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
	}


	public class TexAtlas
	{
		private	Texture2D	mAtlasTexture;
		private	TexNode		mRoot;


		public TexAtlas(GraphicsDevice g)
		{
			mRoot			=new TexNode();
			mAtlasTexture	=new Texture2D(g, TexNode.TEXATLAS_WIDTH,
								TexNode.TEXATLAS_HEIGHT, 1,
								TextureUsage.None,
								SurfaceFormat.Color);
		}


		public Texture2D GetAtlasTexture()
		{
			return	mAtlasTexture;
		}


		public bool	Insert(Color[] tex, int texW, int texH,
			out double scaleU, out double scaleV, out double uoffs, out double voffs)
		{
			//test code
			/*
			TexNode	n1	=mRoot.Insert(8, 8);
			TexNode	n2	=mRoot.Insert(8, 8);
			TexNode	n3	=mRoot.Insert(8, 8);
			TexNode	n4	=mRoot.Insert(8, 8);
			TexNode	n5	=mRoot.Insert(8, 8);
			TexNode	n6	=mRoot.Insert(8, 8);
			TexNode	n7	=mRoot.Insert(8, 8);
			TexNode	n8	=mRoot.Insert(8, 8);
			*/
			TexNode	n	=mRoot.Insert(texW, texH);

			scaleU = scaleV = uoffs = voffs = 0.0;

			if(n == null)
			{
				return	false;
			}

			Color[]	at	=new Color[TexNode.TEXATLAS_WIDTH * TexNode.TEXATLAS_HEIGHT];

			//copy pixels in
			mAtlasTexture.GetData<Color>(at);

			Rectangle	target	=n.GetRect();

			if(target.Top != 0)
			{
				int		j	=0;
				j++;
				j--;
			}

			int	c	=0;
			for(int y=target.Top;y < target.Bottom;y++)
			{
				for(int x=target.Left;x < target.Right;x++, c++)
				{
					at[(y * TexNode.TEXATLAS_WIDTH) + x]	=tex[c];
				}
			}

			mAtlasTexture.SetData<Color>(at);


			//maybe squeeze a little extra precision here
			scaleU	=(double)texW / (double)TexNode.TEXATLAS_WIDTH;
			scaleV	=(double)texH / (double)TexNode.TEXATLAS_HEIGHT;

			//get offsets in zero to one space
			uoffs	=((double)target.Left / (double)TexNode.TEXATLAS_WIDTH);
			voffs	=((double)target.Top / (double)TexNode.TEXATLAS_HEIGHT);

			return	true;
		}
	}
}
