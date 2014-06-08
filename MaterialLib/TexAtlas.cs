using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using UtilityLib;

using Buffer	=SharpDX.Direct3D11.Buffer;


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
		Texture2D			mAtlasTexture;
		ShaderResourceView	mSRV;

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

			mBuildArray	=new Color[mWidth * mHeight];

			mRoot	=new TexNode(width, height);
		}


		public void FreeAll()
		{
			mSRV.Dispose();
			mAtlasTexture.Dispose();
		}


		public ShaderResourceView GetAtlasSRV()
		{
			return	mSRV;
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


		public void Finish(GraphicsDevice gd)
		{
			if(mBuildArray == null)
			{
				return;
			}

			DataStream	ds	=new DataStream(mWidth * mHeight * 4, true, true);

			foreach(Color c in mBuildArray)
			{
				ds.Write(c);
			}

			InitTex(gd, ds);
		}


		public void Write(BinaryWriter bw)
		{
			bw.Write(mWidth);
			bw.Write(mHeight);

			foreach(Color c in mBuildArray)
			{
				bw.Write(c.ToRgba());
			}

			mRoot.Write(bw);
		}


		public void Read(GraphicsDevice g, BinaryReader br)
		{
			mWidth	=br.ReadInt32();
			mHeight	=br.ReadInt32();

			DataStream	ds	=new DataStream(mWidth * mHeight * 4, true, true);

			byte	[]atlas	=br.ReadBytes(mWidth * mHeight * 4);

			ds.Write(atlas, 0, mWidth * mHeight * 4);

			InitTex(g, ds);

			mRoot	=new TexNode(mWidth, mHeight);
			mRoot.Read(br);
		}


		void InitTex(GraphicsDevice gd, DataStream ds)
		{
			SampleDescription	sampDesc	=new SampleDescription();
			sampDesc.Count		=1;
			sampDesc.Quality	=0;

			Texture2DDescription	texDesc	=new Texture2DDescription();
			texDesc.ArraySize			=1;
			texDesc.BindFlags			=BindFlags.ShaderResource;
			texDesc.CpuAccessFlags		=CpuAccessFlags.None;
			texDesc.MipLevels			=1;
			texDesc.OptionFlags			=ResourceOptionFlags.None;
			texDesc.Usage				=ResourceUsage.Immutable;
			texDesc.Width				=mWidth;
			texDesc.Height				=mHeight;
			texDesc.Format				=Format.R8G8B8A8_UNorm;
			texDesc.SampleDescription	=sampDesc;
			
			DataBox	[]dbs	=new DataBox[1];

			dbs[0]	=new DataBox(ds.DataPointer,
				texDesc.Width *
				(int)FormatHelper.SizeOfInBytes(texDesc.Format),
				texDesc.Width * texDesc.Height *
				(int)FormatHelper.SizeOfInBytes(texDesc.Format));

			mAtlasTexture	=new Texture2D(gd.GD, texDesc, dbs);

			mSRV	=new ShaderResourceView(gd.GD, mAtlasTexture);
		}
	}
}