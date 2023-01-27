using System;
using System.Linq;
using System.Text;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using UtilityLib;


namespace BSPCore;

//routines that check material types based on face, texinfo, and material name
internal class MaterialCorrect
{
	internal static bool IsLightMapped(QFace f, TexInfo tex, string matName)
	{
		if(matName != null)
		{
			if(matName.Contains('*'))
			{
				//only allow cel, all other special
				//materials will have their own category
				if(!matName.EndsWith("*Cel"))
				{
					return	false;
				}
			}
		}

		if(f.mLightOfs == -1)
		{
			return	false;	//only interested in lightmapped
		}

		//make sure not animating
		if(f.mStyles.G != 255 || f.mStyles.B != 255 || f.mStyles.A != 255)
		{
			return	false;
		}
		if(f.mStyles.R != 0)
		{
			return	false;
		}

		if(tex != null)
		{
			if(tex.mAlpha < 1.0f)
			{
				return	false;
			}
			if(!matName.StartsWith(tex.mTexture))
			{
				return	false;
			}
			if(!tex.IsLightMapped())
			{
				return	false;
			}
		}
		return	true;
	}


	internal static bool IsLightMapAnimated(QFace f, TexInfo tex, string matName)
	{
		if(matName != null)
		{
			if(!matName.EndsWith("*Anim"))
			{
				return	false;
			}
		}

		{
			if(f.mLightOfs == -1)
			{
				return	false;	//only interested in lightmapped
			}

			//make sure actually animating
			if(f.mStyles.R ==0 || f.mStyles.R == 255)
			{
				if(f.mStyles.G ==0 || f.mStyles.G == 255)
				{
					if(f.mStyles.B ==0 || f.mStyles.B == 255)
					{
						if(f.mStyles.A ==0 || f.mStyles.A == 255)
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


	internal static bool IsAlpha(QFace f, TexInfo tex, string matName)
	{
		if(matName != null)
		{
			if(!matName.EndsWith("*Alpha"))
			{
				return	false;
			}
		}

		if(f.mLightOfs != -1)
		{
			return	false;	//only interested in non lightmapped
		}

		//check anim lights for good measure
		Debug.Assert(f.mStyles.R == 255);
		Debug.Assert(f.mStyles.G == 255);
		Debug.Assert(f.mStyles.B == 255);
		Debug.Assert(f.mStyles.A == 255);

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


	internal static bool IsFullBright(QFace f, TexInfo tex, string matName)
	{
		if(matName != null)
		{
			if(!matName.EndsWith("*FullBright"))
			{
				return	false;
			}
		}

		{
			if(f.mLightOfs != -1)
			{
				return	false;	//only interested in non lightmapped
			}

			//check anim lights for good measure
			Debug.Assert(f.mStyles.R == 255);
			Debug.Assert(f.mStyles.G == 255);
			Debug.Assert(f.mStyles.B == 255);
			Debug.Assert(f.mStyles.A == 255);
		}

		if(tex != null)
		{
			if(tex.mAlpha < 1.0f)
			{
				return	false;
			}
			if(!tex.IsLight())
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


	internal static bool IsSky(QFace f, TexInfo tex, string matName)
	{
		if(matName != null)
		{
			if(!matName.EndsWith("*Sky"))
			{
				return	false;
			}
		}

		{
			if(f.mLightOfs != -1)
			{
				return	false;	//only interested in non lightmapped
			}

			//check anim lights for good measure
			Debug.Assert(f.mStyles.R == 255);
			Debug.Assert(f.mStyles.G == 255);
			Debug.Assert(f.mStyles.B == 255);
			Debug.Assert(f.mStyles.A == 255);
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


	internal static bool IsLightMappedAlpha(QFace f, TexInfo tex, string matName)
	{
		if(matName != null)
		{
			if(!matName.EndsWith("*LitAlpha"))
			{
				return	false;
			}
		}

		{
			if(f.mLightOfs == -1)
			{
				return	false;	//only interested in lightmapped
			}

			//make sure not animating
			if(f.mStyles.G != 255 || f.mStyles.B != 255 || f.mStyles.A != 255)
			{
				return	false;
			}
			if(f.mStyles.R != 0)
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


	internal static bool IsLightMappedAlphaAnimated(QFace f, TexInfo tex, string matName)
	{
		if(matName != null)
		{
			if(!matName.EndsWith("*LitAlphaAnim"))
			{
				return	false;
			}
		}

		{
			if(f.mLightOfs == -1)
			{
				return	false;	//only interested in lightmapped
			}

			if(f.mStyles.R ==0 || f.mStyles.R == 255)
			{
				if(f.mStyles.G ==0 || f.mStyles.G == 255)
				{
					if(f.mStyles.B ==0 || f.mStyles.B == 255)
					{
						if(f.mStyles.A ==0 || f.mStyles.A == 255)
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