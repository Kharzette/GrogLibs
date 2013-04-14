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
		//allows for four different types of shading
		Texture3D	[]mCellTex;

		//constants for a world preset
		//looks good with bsp lightmapped levels
		const int	WorldLookupSize	=20;
		const float	WorldThreshold0	=0.6f;
		const float	WorldThreshold1	=0.4f;
		const float	WorldThreshold2	=0.25f;
		const float	WorldThreshold3	=0.1f;
		const float	WorldLevel0		=1.0f;
		const float	WorldLevel1		=0.7f;
		const float	WorldLevel2		=0.5f;
		const float	WorldLevel3		=0.2f;
		const float	WorldLevel4		=0.05f;

		//constants for a character preset
		//looks good for anime style characters
		const int	CharacterLookupSize	=20;
		const float	CharacterThreshold0	=0.6f;
		const float	CharacterThreshold1	=0.4f;
		const float	CharacterLevel0		=1.0f;
		const float	CharacterLevel1		=0.5f;
		const float	CharacterLevel2		=0.1f;


		public void InitCellShading(int numShadingVariations)
		{
			mCellTex	=new Texture3D[numShadingVariations];
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
			int		size	=20;

			if(bCharacter)
			{
				thresholds	=new float[2];
				levels		=new float[3];

				thresholds[0]	=CharacterThreshold0;
				thresholds[1]	=CharacterThreshold1;
				levels[0]		=CharacterLevel0;
				levels[1]		=CharacterLevel1;
				levels[2]		=CharacterLevel2;
			}
			else
			{
				//worldy preset
				thresholds	=new float[4];
				levels		=new float[5];

				thresholds[0]	=WorldThreshold0;
				thresholds[1]	=WorldThreshold1;
				thresholds[2]	=WorldThreshold2;
				thresholds[3]	=WorldThreshold3;
				levels[0]		=WorldLevel0;
				levels[1]		=WorldLevel1;
				levels[2]		=WorldLevel2;
				levels[3]		=WorldLevel3;
				levels[4]		=WorldLevel4;
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

			mCellTex[index]	=new Texture3D(gd, size,
				size, size, false, SurfaceFormat.Color);

			Color	[]data	=new Color[size * size * size];

			float	csize	=size;

			for(int z=0;z < size;z++)
			{
				for(int y=0;y < size;y++)
				{
					for(int x=0;x < size;x++)
					{
						float	xPercent	=(float)x / csize;
						float	yPercent	=(float)y / csize;
						float	zPercent	=(float)z / csize;

						Vector3	color	=Vector3.Zero;

						color.X	=CellMe(xPercent, thresholds, levels);
						color.Y	=CellMe(yPercent, thresholds, levels);
						color.Z	=CellMe(zPercent, thresholds, levels);

						data[x + (y * size) + (z * size * size)]
							=new Color(color);
					}
				}
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
