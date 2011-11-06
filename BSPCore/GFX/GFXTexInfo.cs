using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework;


namespace BSPCore
{
	public class GFXTexInfo : UtilityLib.IReadWriteable
	{
		public Vector3	[]mVecs			=new Vector3[2];
		public float	[]mShift		=new float[2];
		public float	[]mDrawScale	=new float[2];
		public UInt32	mFlags;
		public float	mFaceLight;
		public float	mReflectiveScale;
		public float	mAlpha;
		public float	mMipMapBias;
		public string	mMaterial;	//index into MaterialLib


		public void Write(BinaryWriter bw)
		{
			bw.Write(mVecs[0].X);
			bw.Write(mVecs[0].Y);
			bw.Write(mVecs[0].Z);
			bw.Write(mVecs[1].X);
			bw.Write(mVecs[1].Y);
			bw.Write(mVecs[1].Z);
			bw.Write(mShift[0]);
			bw.Write(mShift[1]);
			bw.Write(mDrawScale[0]);
			bw.Write(mDrawScale[1]);
			bw.Write(mFlags);
			bw.Write(mFaceLight);
			bw.Write(mReflectiveScale);
			bw.Write(mAlpha);
			bw.Write(mMipMapBias);
			bw.Write(mMaterial);
		}


		public void Read(BinaryReader br)
		{
			mVecs[0].X			=br.ReadSingle();
			mVecs[0].Y			=br.ReadSingle();
			mVecs[0].Z			=br.ReadSingle();
			mVecs[1].X			=br.ReadSingle();
			mVecs[1].Y			=br.ReadSingle();
			mVecs[1].Z			=br.ReadSingle();
			mShift[0]			=br.ReadSingle();
			mShift[1]			=br.ReadSingle();
			mDrawScale[0]		=br.ReadSingle();
			mDrawScale[1]		=br.ReadSingle();
			mFlags				=br.ReadUInt32();
			mFaceLight			=br.ReadSingle();
			mReflectiveScale	=br.ReadSingle();
			mAlpha				=br.ReadSingle();
			mMipMapBias			=br.ReadSingle();
			mMaterial			=br.ReadString();
		}


		public bool IsLightMapped()
		{
			return	((mFlags & TexInfo.NO_LIGHTMAP) == 0);
		}


		public bool IsAlpha()
		{
			return	((mFlags & TexInfo.TRANS) != 0);
		}


		public bool IsSky()
		{
			return	((mFlags & TexInfo.SKY) != 0);
		}


		public bool IsMirror()
		{
			return	((mFlags & TexInfo.MIRROR) != 0);
		}


		public bool IsGouraud()
		{
			return	((mFlags & TexInfo.GOURAUD) != 0);
		}


		public bool IsFlat()
		{
			return	((mFlags & TexInfo.FLAT) != 0);
		}


		public bool IsFullBright()
		{
			return	((mFlags & TexInfo.FULLBRIGHT) != 0);
		}


		public bool IsLight()
		{
			return	((mFlags & TexInfo.LIGHT) != 0);
		}


		public Vector2 GetTexCoord(Vector3 vert)
		{
			Vector2	ret	=Vector2.Zero;

			ret.X	=Vector3.Dot(vert, mVecs[0]);
			ret.Y	=Vector3.Dot(vert, mVecs[1]);

			return	ret;
		}


		internal static string ScryTrueName(GFXFace f, GFXTexInfo tex)
		{
			string	matName	=tex.mMaterial;

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
			}
			else if(tex.IsMirror())
			{
				matName	+="*Mirror";
			}
			else if(tex.IsAlpha())
			{
				matName	+="*Alpha";
			}
			else if(tex.IsFlat() || tex.IsGouraud())
			{
				matName	+="*VertLit";
			}
			else if(tex.IsFullBright() || tex.IsLight())
			{
				matName	+="*FullBright";
			}
			else if(tex.IsSky())
			{
				matName	+="*Sky";
			}

			int	numStyles	=0;
			for(int i=0;i < 4;i++)
			{
				if(f.mLTypes[i] != 255)
				{
					numStyles++;
				}
			}

			//animated lights ?
			if(numStyles > 1 || (numStyles == 1 && f.mLTypes[0] != 0))
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

			return	matName;
		}
	}
}
