using System;
using System.IO;
using System.Numerics;
using System.Diagnostics;
using UtilityLib;


namespace BSPCore;

public class TexInfo
{
	public Vector3		mUVec, mVVec;
	public float		mShiftU, mShiftV;
	public float		mDrawScaleU, mDrawScaleV;
	public UInt32		mFlags;
	public float		mLightMapScale;
	public float		mAlpha	=1.0f;	//default
	public string		mTexture;
	public string		mMaterial;

	//genesis texinfo flags
//	public const UInt32	MIRROR		=(1<<0);
//	public const UInt32	FULLBRIGHT	=(1<<1);
//	public const UInt32	SKY			=(1<<2);
//	public const UInt32	EMITLIGHT	=(1<<3);
//	public const UInt32	TRANSPARENT	=(1<<4);
//	public const UInt32	GOURAUD		=(1<<5);
//	public const UInt32	FLAT		=(1<<6);
//	public const UInt32	CELSHADE	=(1<<7);
//	public const UInt32	NO_LIGHTMAP	=(1<<15);

	//Q2 texinfo flags
	public const UInt32	SURF_LIGHT		=0x1;	//value will hold the light strength
	public const UInt32	SURF_SLICK		=0x2;	//effects game physics
	public const UInt32	SURF_SKY		=0x4;	//don't draw, but add to skybox
	public const UInt32	SURF_WARP		=0x8;	//turbulent water warp
	public const UInt32	SURF_TRANS33	=0x10;
	public const UInt32	SURF_TRANS66	=0x20;
	public const UInt32	SURF_FLOWING	=0x40;	//scroll towards angle
	public const UInt32	SURF_NODRAW		=0x80;	//don't bother referencing the texture
	public const UInt32	SURF_HINT		=0x100;	//make a primary bsp splitter
	public const UInt32	SURF_SKIP		=0x200;	//completely ignore, allowing non-closed brushes


	internal bool Compare(TexInfo other)
	{
		if(mUVec != other.mUVec)
		{
			return	false;
		}
		if(mVVec != other.mVVec)
		{
			return	false;
		}
		if(mShiftU != other.mShiftU)
		{
			return	false;
		}
		if(mShiftV != other.mShiftV)
		{
			return	false;
		}
		if(mDrawScaleU != other.mDrawScaleU)
		{
			return	false;
		}
		if(mDrawScaleV != other.mDrawScaleV)
		{
			return	false;
		}
		if(mFlags != other.mFlags)
		{
			return	false;
		}
		if(mLightMapScale != other.mLightMapScale)
		{
			return	false;
		}
		if(mAlpha != other.mAlpha)
		{
			return	false;
		}
		if(mTexture != other.mTexture)
		{
			return	false;
		}
		return	true;
	}


	internal void	QRead(BinaryReader br)
	{
		Vector4	vecs0	=FileUtil.ReadVector4(br);
		Vector4	vecs1	=FileUtil.ReadVector4(br);

		mUVec	=vecs0.XYZ();
		mVVec	=vecs1.XYZ();

		mShiftU	=vecs0.W;
		mShiftV	=vecs1.W;

		mFlags	=br.ReadUInt32();

		mLightMapScale	=8f;	//guessing?

		//not sure what this is for yet
		uint	value	=br.ReadUInt32();

		char	[]texture	=br.ReadChars(32);
		mTexture	=new string(texture);

		uint	nextTexInfo	=br.ReadUInt32();
	}

	public bool IsLightMapped()
	{
		return	!(Misc.bFlagSet(mFlags, SURF_SKY)
			|| Misc.bFlagSet(mFlags, SURF_WARP)
			|| Misc.bFlagSet(mFlags, SURF_NODRAW)
			|| Misc.bFlagSet(mFlags, SURF_SKIP));			
	}

	public bool IsAlpha()
	{
		return	((mFlags & (SURF_TRANS33 | SURF_TRANS66)) != 0);
	}

	public bool IsSky()
	{
		return	((mFlags & SURF_SKY) != 0);
	}

	public bool IsLight()
	{
		return	((mFlags & SURF_LIGHT) != 0);
	}

	internal static string ScryTrueName(QFace f, TexInfo tex)
	{
		string	matName	=tex.mTexture;

		if(tex.IsLightMapped())
		{
			if(tex.IsAlpha() && f.mLightOfs != -1)
			{
				matName	+="*LitAlpha";
			}
			else if(tex.IsAlpha())
			{
				matName	+="*Alpha";
			}
			else if(f.mLightOfs == -1)
			{
				matName	+="*VertLit";
			}
			
			int	numStyles	=0;
			numStyles	+=(f.mStyles.R != 255)? 1 : 0;
			numStyles	+=(f.mStyles.G != 255)? 1 : 0;
			numStyles	+=(f.mStyles.B != 255)? 1 : 0;
			numStyles	+=(f.mStyles.A != 255)? 1 : 0;

			//animated lights ?
			if(numStyles > 1)
			{
				Debug.Assert(tex.IsLightMapped());

				//see if material is already alpha
				if(matName.Contains("*LitAlpha"))
				{
					matName	+="Anim";	//*LitAlphaAnim
				}
				else
				{
					matName	+="*Anim";
				}
			}
		}
		else if(tex.IsAlpha())
		{
			matName	+="*Alpha";
		}
		else if(tex.IsSky())
		{
			matName	+="*Sky";
		}
		else if(tex.IsLight())
		{
			matName	+="*FullBright";
		}

		return	matName;
	}
}