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
		public Vector3	mVecU;
		public Vector3	mVecV;
		public float	mShiftU;
		public float	mShiftV;
		public float	mDrawScaleU;
		public float	mDrawScaleV;
		public UInt32	mFlags;
		public float	mAlpha;
		public string	mMaterial;	//index into MaterialLib


		public void Write(BinaryWriter bw)
		{
			bw.Write(mVecU.X);
			bw.Write(mVecU.Y);
			bw.Write(mVecU.Z);
			bw.Write(mVecV.X);
			bw.Write(mVecV.Y);
			bw.Write(mVecV.Z);
			bw.Write(mShiftU);
			bw.Write(mShiftV);
			bw.Write(mDrawScaleU);
			bw.Write(mDrawScaleV);
			bw.Write(mFlags);
			bw.Write(mAlpha);
			bw.Write(mMaterial);
		}


		public void Read(BinaryReader br)
		{
			mVecU.X				=br.ReadSingle();
			mVecU.Y				=br.ReadSingle();
			mVecU.Z				=br.ReadSingle();
			mVecV.X				=br.ReadSingle();
			mVecV.Y				=br.ReadSingle();
			mVecV.Z				=br.ReadSingle();
			mShiftU				=br.ReadSingle();
			mShiftV				=br.ReadSingle();
			mDrawScaleU			=br.ReadSingle();
			mDrawScaleV			=br.ReadSingle();
			mFlags				=br.ReadUInt32();
			mAlpha				=br.ReadSingle();
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

			ret.X	=Vector3.Dot(vert, mVecU);
			ret.Y	=Vector3.Dot(vert, mVecV);

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
			else if(tex.IsSky())
			{
				matName	+="*Sky";
			}
			else if(tex.IsFullBright() || tex.IsLight())
			{
				matName	+="*FullBright";
			}

			int	numStyles	=0;
			numStyles	+=(f.mLType0 != 255)? 1 : 0;
			numStyles	+=(f.mLType1 != 255)? 1 : 0;
			numStyles	+=(f.mLType2 != 255)? 1 : 0;
			numStyles	+=(f.mLType3 != 255)? 1 : 0;

			//animated lights ?
			if(numStyles > 1 || (numStyles == 1 && f.mLType0 != 0))
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
