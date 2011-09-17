//Escape from Cube Mountain - Copyright © 2011 Ken Baird
//For Ludum Dare 21
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;


namespace TerrainLib
{
	public class TerrainModel
	{
		float	[,]mHeightGrid;
		float	mPolySize;
		int		mGridSize;

		public TerrainModel(float [,]grid, float polySize, int gridSize)
		{
			mHeightGrid	=grid;
			mPolySize	=polySize;
			mGridSize	=gridSize;
		}


		public int GetMapSize()
		{
			return	(int)(mGridSize * mPolySize);
		}


		public float GetHeight(Vector3 coord)
		{
			float	ret	=0.0f;

			int	x	=(int)Math.Floor(coord.X / mPolySize);
			int	y	=(int)Math.Floor(coord.Z / mPolySize);

			float	xgrid	=x * mPolySize;
			float	ygrid	=y * mPolySize;

			if(x >= (mGridSize - 1) || x < 0 || y >= (mGridSize - 1) || y < 0)
			{
				return	-1.0f;
			}


			float	topLeft		=mHeightGrid[y, x];
			float	topRight	=mHeightGrid[y, x + 1];
			float	botLeft		=mHeightGrid[y + 1, x];
			float	botRight	=mHeightGrid[y + 1, x + 1];

			//see if over the upper left or lower right triangle
			Vector2	upLeft	=Vector2.Zero;
			Vector2	lwRight	=Vector2.Zero;

			upLeft.X	=x * mPolySize;
			upLeft.Y	=y * mPolySize;
			lwRight.X	=(x + 1) * mPolySize;
			lwRight.Y	=(y + 1) * mPolySize;

			Vector2	inPos	=Vector2.Zero;
			inPos.X	=coord.X;
			inPos.Y	=coord.Z;

			if(Vector2.DistanceSquared(inPos, upLeft) <= Vector2.DistanceSquared(inPos, lwRight))
			{
				//top left tri

				//find gradient... reminds me of software rendering
				float	deltaX	=(topRight - topLeft) / mPolySize;
				float	deltaY	=(botLeft - topLeft) / mPolySize;

				ret	=topLeft + (deltaX * (coord.X - xgrid)) + (deltaY * (coord.Z - ygrid));
			}
			else
			{
				//lower right tri
				float	deltaX	=(botLeft - botRight) / mPolySize;
				float	deltaY	=(topRight - botRight) / mPolySize;

				ret	=botRight + (deltaX * ((xgrid + mPolySize) - coord.X)) + (deltaY * ((ygrid + mPolySize) - coord.Z));
			}

			return	ret;
		}
	}
}