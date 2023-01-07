using System;
using System.IO;
using System.Numerics;
using UtilityLib;
using Vortice;
using Vortice.Direct3D11;
using Vortice.Mathematics;


namespace MaterialLib;

//based on this article
//http://www.blackpawn.com/texts/lightmaps/default.html
class TexNode
{
	TexNode		mFront;
	TexNode		mBack;
	RawRect		mRect;
	bool		mbOccupied;


	public TexNode() { }
	public TexNode(int w, int h)
	{
		mRect	=new RawRect(0, 0, w, h);
	}


	public RawRect GetRect()
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
			mFront.mRect	=new RawRect(mRect.Left, mRect.Top,
								texW,
								(mRect.Bottom - mRect.Top));

			mBack.mRect	=new RawRect((mRect.Left + texW),
							mRect.Top,
							(mRect.Right - mRect.Left - texW),
							(mRect.Bottom - mRect.Top));
		}
		else
		{
			mFront.mRect	=new RawRect(mRect.Left, mRect.Top,
								(mRect.Right - mRect.Left),
								texH);

			mBack.mRect	=new RawRect(mRect.Left,
							(mRect.Top + texH),
							(mRect.Right - mRect.Left),
							(mRect.Bottom - mRect.Top - texH));
		}

		return	mFront.Insert(texW, texH);
	}


	internal void Write(BinaryWriter bw)
	{
		bw.Write(mRect.Left);
		bw.Write(mRect.Top);
		bw.Write(mRect.Right);
		bw.Write(mRect.Bottom);
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
		int	X		=br.ReadInt32();
		int	Y		=br.ReadInt32();
		int	Width	=br.ReadInt32();
		int	Height	=br.ReadInt32();
		mbOccupied	=br.ReadBoolean();

		mRect	=new RawRect(X, Y, Width, Height);

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
	ID3D11Texture2D				mAtlasTexture;
	ID3D11ShaderResourceView	mSRV;

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
		if(mSRV != null)
		{
			mSRV.Dispose();
		}
		if(mAtlasTexture != null)
		{
			mAtlasTexture.Dispose();
		}
	}


	public System.Drawing.Bitmap GetAtlasImage(ID3D11DeviceContext dc)
	{
		System.Drawing.Bitmap	bm	=new System.Drawing.Bitmap(mWidth, mHeight);

		System.Drawing.Imaging.BitmapData	bmd	=new System.Drawing.Imaging.BitmapData();

		System.Drawing.Rectangle	bmRect	=new System.Drawing.Rectangle(0, 0, mWidth, mHeight);

		bmd	=bm.LockBits(bmRect,
			System.Drawing.Imaging.ImageLockMode.WriteOnly,
			System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

		IntPtr	ptr	=bmd.Scan0;

		int	colSize	=(mWidth * mHeight * 4);

		byte	[]copyOf	=new byte[colSize];			

		for(int i=0;i < mBuildArray.Length;i++)
		{
			Color	c	=mBuildArray[i];

			copyOf[i * 4]		=c.B;
			copyOf[i * 4 + 1]	=c.G;
			copyOf[i * 4 + 2]	=c.R;
			copyOf[i * 4 + 3]	=c.A;
		}

		System.Runtime.InteropServices.Marshal.Copy(copyOf, 0, ptr, colSize);

		bm.UnlockBits(bmd);

		return	bm;
	}


	public ID3D11ShaderResourceView GetAtlasSRV()
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

		RawRect	target	=n.GetRect();

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


	public void Finish(GraphicsDevice gd, StuffKeeper sk, string texName)
	{
		if(mBuildArray == null)
		{
			return;
		}

		sk.AddTex(gd.GD, texName, mBuildArray, mWidth, mHeight);
	}


	public void Write(BinaryWriter bw)
	{
		bw.Write(mWidth);
		bw.Write(mHeight);

		foreach(Color c in mBuildArray)
		{
			bw.Write(c.PackedValue);
		}

		mRoot.Write(bw);
	}


	public void Read(GraphicsDevice gd, BinaryReader br, StuffKeeper sk)
	{
		mWidth	=br.ReadInt32();
		mHeight	=br.ReadInt32();

		string	texName	=br.ReadString();

		byte	[]atlas	=br.ReadBytes(mWidth * mHeight * 4);

		//this kind of ties it to lightmaps
		//want to use this eventually to atlas all the textures
		//as well for bsp meshes
		sk.AddTex(gd.GD, texName, mBuildArray, mWidth, mHeight);

		mRoot	=new TexNode(mWidth, mHeight);
		mRoot.Read(br);
	}
}