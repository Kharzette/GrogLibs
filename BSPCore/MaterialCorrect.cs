using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using UtilityLib;


namespace BSPCore
{
	//routines that check material types based on face, texinfo, and material name
	internal class MaterialCorrect
	{
		internal static bool IsLightMapped(GFXFace f, GFXTexInfo tex, string matName)
		{
			if(matName != null)
			{
				//skip all special materials
				if(matName.Contains("*"))
				{
					return	false;
				}
			}

			if(f != null)
			{
				if(f.mLightOfs == -1)
				{
					return	false;	//only interested in lightmapped
				}

				//make sure not animating
				if(f.mLType1 != 255 || f.mLType2 != 255 || f.mLType3 != 255)
				{
					return	false;
				}
				if(f.mLType0 != 0)
				{
					return	false;
				}
			}

			if(tex != null)
			{
				if(tex.mAlpha < 1.0f)
				{
					return	false;
				}
				if(tex.mMaterial != matName)
				{
					return	false;
				}
			}
			return	true;
		}


		internal static bool IsLightMapAnimated(GFXFace f, GFXTexInfo tex, string matName)
		{
			if(matName != null)
			{
				if(!matName.EndsWith("*Anim"))
				{
					return	false;
				}
			}

			if(f != null)
			{
				if(f.mLightOfs == -1)
				{
					return	false;	//only interested in lightmapped
				}

				//make sure actually animating
				if(f.mLType0 ==0 || f.mLType0 == 255)
				{
					if(f.mLType1 ==0 || f.mLType1 == 255)
					{
						if(f.mLType2 ==0 || f.mLType2 == 255)
						{
							if(f.mLType3 ==0 || f.mLType3 == 255)
							{
								return	false;
							}
						}
					}
				}
			}

			if(tex != null)
			{
				if(tex.mAlpha < 1.0f)
				{
					return	false;
				}
				if(!matName.StartsWith(tex.mMaterial))
				{
					return	false;
				}
			}
			return	true;
		}


		internal static bool IsVLit(GFXFace f, GFXTexInfo tex, string matName)
		{
			if(matName != null)
			{
				if(!matName.EndsWith("*VertLit"))
				{
					return	false;
				}
			}

			if(f != null)
			{
				if(f.mLightOfs != -1)
				{
					return	false;	//only interested in non lightmapped
				}

				//check anim lights for good measure
				Debug.Assert(f.mLType0 == 255);
				Debug.Assert(f.mLType1 == 255);
				Debug.Assert(f.mLType2 == 255);
				Debug.Assert(f.mLType3 == 255);
			}

			if(tex != null)
			{
				if(tex.mAlpha < 1.0f)
				{
					return	false;
				}
				if((tex.mFlags & 
					(TexInfo.FULLBRIGHT | TexInfo.MIRROR | TexInfo.SKY)) != 0)
				{
					return	false;
				}

				if(!matName.StartsWith(tex.mMaterial))
				{
					return	false;
				}
			}
			return	true;
		}


		internal static bool IsMirror(GFXFace f, GFXTexInfo tex, string matName)
		{
			if(matName != null)
			{
				if(!matName.EndsWith("*Mirror"))
				{
					return	false;
				}
			}

			if(f != null)
			{
				if(f.mLightOfs != -1)
				{
					return	false;	//only interested in non lightmapped
				}

				//check anim lights for good measure
				Debug.Assert(f.mLType0 == 255);
				Debug.Assert(f.mLType1 == 255);
				Debug.Assert(f.mLType2 == 255);
				Debug.Assert(f.mLType3 == 255);
			}

			if(tex != null)
			{
				if((tex.mFlags & TexInfo.MIRROR) == 0)
				{
					return	false;
				}

				if(!matName.StartsWith(tex.mMaterial))
				{
					return	false;
				}
			}
			return	true;
		}


		internal static bool IsAlpha(GFXFace f, GFXTexInfo tex, string matName)
		{
			if(matName != null)
			{
				if(!matName.EndsWith("*Alpha"))
				{
					return	false;
				}
			}

			if(f != null)
			{
				if(f.mLightOfs != -1)
				{
					return	false;	//only interested in non lightmapped
				}

				//check anim lights for good measure
				Debug.Assert(f.mLType0 == 255);
				Debug.Assert(f.mLType1 == 255);
				Debug.Assert(f.mLType2 == 255);
				Debug.Assert(f.mLType3 == 255);
			}

			if(tex != null)
			{
				if(tex.mAlpha >= 1.0f)
				{
					return	false;
				}

				if((tex.mFlags & TexInfo.MIRROR) != 0)
				{
					return	false;
				}

				if(!matName.StartsWith(tex.mMaterial))
				{
					return	false;
				}
			}
			return	true;
		}


		internal static bool IsFullBright(GFXFace f, GFXTexInfo tex, string matName)
		{
			if(matName != null)
			{
				if(!matName.EndsWith("*FullBright"))
				{
					return	false;
				}
			}

			if(f != null)
			{
				if(f.mLightOfs != -1)
				{
					return	false;	//only interested in non lightmapped
				}

				//check anim lights for good measure
				Debug.Assert(f.mLType0 == 255);
				Debug.Assert(f.mLType1 == 255);
				Debug.Assert(f.mLType2 == 255);
				Debug.Assert(f.mLType3 == 255);
			}

			if(tex != null)
			{
				if(tex.mAlpha < 1.0f)
				{
					return	false;
				}
				if(Misc.bFlagSet(tex.mFlags, TexInfo.MIRROR))
				{
					return	false;
				}
				if(Misc.bFlagSet(tex.mFlags, TexInfo.GOURAUD))
				{
					return	false;
				}
				if(Misc.bFlagSet(tex.mFlags, TexInfo.FLAT))
				{
					return	false;
				}
				if(!Misc.bFlagSet(tex.mFlags, TexInfo.FULLBRIGHT))
				{
					return	false;
				}

				if(!matName.StartsWith(tex.mMaterial))
				{
					return	false;
				}
			}
			return	true;
		}


		internal static bool IsSky(GFXFace f, GFXTexInfo tex, string matName)
		{
			if(matName != null)
			{
				if(!matName.EndsWith("*Sky"))
				{
					return	false;
				}
			}

			if(f != null)
			{
				if(f.mLightOfs != -1)
				{
					return	false;	//only interested in non lightmapped
				}

				//check anim lights for good measure
				Debug.Assert(f.mLType0 == 255);
				Debug.Assert(f.mLType1 == 255);
				Debug.Assert(f.mLType2 == 255);
				Debug.Assert(f.mLType3 == 255);
			}

			if(tex != null)
			{
				if(!tex.IsSky())
				{
					return	false;
				}
				if(tex.mAlpha < 1.0f)
				{
					return	false;
				}

				if(!matName.StartsWith(tex.mMaterial))
				{
					return	false;
				}
			}
			return	true;
		}


		internal static bool IsLightMappedAlpha(GFXFace f, GFXTexInfo tex, string matName)
		{
			if(matName != null)
			{
				if(!matName.EndsWith("*LitAlpha"))
				{
					return	false;
				}
			}

			if(f != null)
			{
				if(f.mLightOfs == -1)
				{
					return	false;	//only interested in lightmapped
				}

				//make sure not animating
				if(f.mLType1 != 255 || f.mLType2 != 255 || f.mLType3 != 255)
				{
					return	false;
				}
				if(f.mLType0 != 0)
				{
					return	false;
				}
			}

			if(tex != null)
			{
				if(tex.mAlpha >= 1.0f)
				{
					return	false;
				}
				if(!matName.StartsWith(tex.mMaterial))
				{
					return	false;
				}
			}
			return	true;
		}


		internal static bool IsLightMappedAlphaAnimated(GFXFace f, GFXTexInfo tex, string matName)
		{
			if(matName != null)
			{
				if(!matName.EndsWith("*LitAlphaAnim"))
				{
					return	false;
				}
			}

			if(f != null)
			{
				if(f.mLightOfs == -1)
				{
					return	false;	//only interested in lightmapped
				}

				if(f.mLType0 ==0 || f.mLType0 == 255)
				{
					if(f.mLType1 ==0 || f.mLType1 == 255)
					{
						if(f.mLType2 ==0 || f.mLType2 == 255)
						{
							if(f.mLType3 ==0 || f.mLType3 == 255)
							{
								return	false;
							}
						}
					}
				}
			}

			if(tex != null)
			{
				if(tex.mAlpha >= 1.0f)
				{
					return	false;
				}
				if(!matName.StartsWith(tex.mMaterial))
				{
					return	false;
				}
			}
			return	true;
		}
	}
}
