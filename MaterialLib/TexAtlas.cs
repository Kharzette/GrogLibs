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
	//hate readonly garbage
	public struct	Rect
	{
		public int	X, Y, W, H;
	};

	TexNode		mFront;
	TexNode		mBack;
	Rect		mRect;
	bool		mbOccupied;


	public TexNode() { }
	public TexNode(int w, int h)
	{
		mRect.X	=mRect.Y	=0;
		mRect.W	=w;
		mRect.H	=h;
	}


	public Rect GetRect()
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

		if(mRect.W < texW || mRect.H < texH)
		{
			return	null;	//too small
		}
		else if(mRect.W == texW	&& mRect.H == texH)
		{
			mbOccupied	=true;	//just right
			return	this;
		}

		//split
		int dw	=mRect.W - texW;
		int dh	=mRect.H - texH;

		mFront	=new TexNode();
		mBack	=new TexNode();

		if(dw > dh)
		{
			//split vertical
			mFront.mRect.X	=mRect.X;
			mFront.mRect.Y	=mRect.Y;
			mFront.mRect.W	=texW;
			mFront.mRect.H	=mRect.H;

			mBack.mRect.X	=mRect.X + texW;
			mBack.mRect.Y	=mRect.Y;
			mBack.mRect.W	=mRect.W - texW;
			mBack.mRect.H	=mRect.H;
		}
		else
		{
			mFront.mRect.X	=mRect.X;
			mFront.mRect.Y	=mRect.Y;
			mFront.mRect.W	=mRect.W;
			mFront.mRect.H	=texH;

			mBack.mRect.X	=mRect.X;
			mBack.mRect.Y	=mRect.Y + texH;
			mBack.mRect.W	=mRect.W;
			mBack.mRect.H	=mRect.H - texH;
		}

		return	mFront.Insert(texW, texH);
	}


	internal void Write(BinaryWriter bw)
	{
		bw.Write(mRect.X);
		bw.Write(mRect.Y);
		bw.Write(mRect.W);
		bw.Write(mRect.H);
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
		mRect.X		=br.ReadInt32();
		mRect.Y		=br.ReadInt32();
		mRect.W		=br.ReadInt32();
		mRect.H		=br.ReadInt32();
		mbOccupied	=br.ReadBoolean();

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

		TexNode.Rect	target	=n.GetRect();

		int c	=0;
		for(int y=target.Y;y < (target.Y + target.H);y++)
		{
			for(int x=target.X;x < (target.X + target.W);x++, c++)
			{
				mBuildArray[(y * mWidth) + x]	=tex[c];
			}
		}

		//maybe squeeze a little extra precision here
		scaleU	=(double)texW / (double)mWidth;
		scaleV	=(double)texH / (double)mHeight;

		//get offsets in zero to one space
		uoffs	=((double)target.X / (double)mWidth);
		voffs	=((double)target.Y / (double)mHeight);

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