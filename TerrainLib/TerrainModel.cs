﻿//Escape from Cube Mountain - Copyright © 2011 Ken Baird
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


		public Plane GetGroundPlane(Vector3 coord)
		{
			Plane	ret	=new Plane();

			int	x	=(int)Math.Floor(coord.X / mPolySize);
			int	y	=(int)Math.Floor(coord.Z / mPolySize);

			float	xgrid	=x * mPolySize;
			float	ygrid	=y * mPolySize;

			if(x >= (mGridSize - 1) || x < 0 || y >= (mGridSize - 1) || y < 0)
			{				
				return	ret;
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
				Vector3	topLeftVert	=Vector3.Zero;
				topLeftVert.X		=xgrid;
				topLeftVert.Y		=topLeft;
				topLeftVert.Z		=ygrid;

				Vector3	topRightVert	=Vector3.Zero;
				topRightVert.X			=xgrid + mPolySize;
				topRightVert.Y			=topRight;
				topRightVert.Z			=ygrid;

				Vector3	botLeftVert	=Vector3.Zero;
				botLeftVert.X		=xgrid;
				botLeftVert.Y		=botLeft;
				botLeftVert.Z		=ygrid + mPolySize;

				ret.Normal	=Vector3.Cross(topLeftVert - botLeftVert, topLeftVert - topRightVert);
				ret.Normal.Normalize();
				ret.D		=-Vector3.Dot(topLeftVert, ret.Normal);
			}
			else
			{
				//bottom right tri
				Vector3	botRightVert	=Vector3.Zero;
				botRightVert.X			=xgrid;
				botRightVert.Y			=topLeft;
				botRightVert.Z			=ygrid;

				Vector3	topRightVert	=Vector3.Zero;
				topRightVert.X			=xgrid + mPolySize;
				topRightVert.Y			=topRight;
				topRightVert.Z			=ygrid;

				Vector3	botLeftVert	=Vector3.Zero;
				botLeftVert.X		=xgrid;
				botLeftVert.Y		=botLeft;
				botLeftVert.Z		=ygrid + mPolySize;

				ret.Normal	=Vector3.Cross(botRightVert - botLeftVert, botRightVert - topRightVert);
				ret.Normal.Normalize();
				ret.D		=-Vector3.Dot(botRightVert, ret.Normal);
			}

			return	ret;
		}


		public float GetGoodCloudHeight()
		{
			float	max	=float.MinValue;
			float	min	=float.MaxValue;

			//trace the outer edge of the map

			//top horiz
			for(int x=0;x < mGridSize;x++)
			{
				float	height	=mHeightGrid[0, x];

				if(height < min)
				{
					min	=height;
				}

				if(height > max)
				{
					max	=height;
				}
			}

			//bottom horiz
			for(int x=0;x < mGridSize;x++)
			{
				float	height	=mHeightGrid[mGridSize - 1, x];

				if(height < min)
				{
					min	=height;
				}

				if(height > max)
				{
					max	=height;
				}
			}

			//left vert
			for(int y=0;y < mGridSize;y++)
			{
				float	height	=mHeightGrid[y, 0];

				if(height < min)
				{
					min	=height;
				}

				if(height > max)
				{
					max	=height;
				}
			}

			//right vert
			for(int y=0;y < mGridSize;y++)
			{
				float	height	=mHeightGrid[y, mGridSize - 1];

				if(height < min)
				{
					min	=height;
				}

				if(height > max)
				{
					max	=height;
				}
			}

			return	max;
		}


		public Vector3 GetPeak()
		{
			float	max		=float.MinValue;
			float	min		=float.MaxValue;
			Vector3	peak	=Vector3.Zero;

			for(int y=0;y < mGridSize;y++)
			{
				for(int x=0;x < mGridSize;x++)
				{
					float	height	=mHeightGrid[y, x];

					if(height < min)
					{
						min	=height;
					}

					if(height > max)
					{
						max		=height;
						peak.Y	=height;
						peak.X	=x * mPolySize;
						peak.Z	=y * mPolySize;
					}
				}
			}

			return	peak;
		}
	}
}