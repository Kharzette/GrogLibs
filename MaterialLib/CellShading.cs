using System;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace MaterialLib
{
	public partial class MaterialLib
	{
		//cell shading lookup textures
		//allows for many different types of shading
		Texture2D	[]mCellTex;

		//constants for a world preset
		//looks good with bsp lightmapped levels
		const int	WorldLookupSize	=256;
		const float	WorldThreshold0	=0.7f;
		const float	WorldThreshold1	=0.3f;
		const float	WorldLevel0		=1.0f;
		const float	WorldLevel1		=0.5f;
		const float	WorldLevel2		=0.08f;

		//constants for a character preset
		//looks good for anime style characters
		const int	CharacterLookupSize	=256;
		const float	CharacterThreshold0	=0.6f;
		const float	CharacterThreshold1	=0.35f;
		const float	CharacterThreshold2	=0.1f;
		const float	CharacterLevel0		=1.0f;
		const float	CharacterLevel1		=0.6f;
		const float	CharacterLevel2		=0.3f;
		const float	CharacterLevel3		=0.1f;


		public void InitCellShading(int numShadingVariations)
		{
			mCellTex	=new Texture2D[numShadingVariations];
		}


		public void SetCellTexture(int index)
		{
			foreach(KeyValuePair<string, Material> mat in mMats)
			{
				foreach(ShaderParameters sp in mat.Value.Parameters)
				{
					if(sp.Name == "mCellTable")
					{
						sp.Value	="::" + index;	//hax
					}
				}
			}
		}


		public void GenerateCellTexturePreset(GraphicsDevice gd, bool bCharacter, int index)
		{
			float	[]thresholds;
			float	[]levels;
			int		size;

			if(bCharacter)
			{
				thresholds	=new float[3];
				levels		=new float[4];

				thresholds[0]	=CharacterThreshold0;
				thresholds[1]	=CharacterThreshold1;
				thresholds[2]	=CharacterThreshold2;
				levels[0]		=CharacterLevel0;
				levels[1]		=CharacterLevel1;
				levels[2]		=CharacterLevel2;
				levels[3]		=CharacterLevel3;
				size			=CharacterLookupSize;
			}
			else
			{
				//worldy preset
				thresholds	=new float[2];
				levels		=new float[3];

				thresholds[0]	=WorldThreshold0;
				thresholds[1]	=WorldThreshold1;
				levels[0]		=WorldLevel0;
				levels[1]		=WorldLevel1;
				levels[2]		=WorldLevel2;
				size			=WorldLookupSize;
			}

			GenerateCellTexture(gd, index, size, thresholds, levels);
		}


		//generate a lookup texture for cell shading
		//this allows a game to specify exactly instead of using a preset
		public void GenerateCellTexture(GraphicsDevice gd,
			int index, int size, float []thresholds, float []levels)
		{
			if(mCellTex == null)
			{
				return;	//need to init with a size first
			}

			mCellTex[index]	=new Texture2D(gd,
				size, size, false, SurfaceFormat.Color);

			Color	[]data	=new Color[size * size];

			float	csize	=size * size;

			for(int x=0;x < (size * size);x++)
			{
				float	xPercent	=(float)x / csize;

				Vector3	color	=Vector3.Zero;

				color.X	=CellMe(xPercent, thresholds, levels);
				color.Y	=color.X;
				color.Z	=color.X;

				data[x]	=new Color(color);
			}

			mCellTex[index].SetData<Color>(data);
		}


		float	CellMe(float val, float []thresholds, float []levels)
		{
			float	ret	=-69f;

			Debug.Assert(thresholds.Length == (levels.Length - 1));

			for(int i=0;i < thresholds.Length;i++)
			{
				if(val > thresholds[i])
				{
					ret	=levels[i];
					break;
				}
			}

			if(ret < -68f)
			{
				ret	=levels[levels.Length - 1];
			}
			return	ret;
		}
	}
}
