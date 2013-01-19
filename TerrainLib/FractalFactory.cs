using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using UtilityLib;


namespace TerrainLib
{
	//generates and operates on big two dimensional
	//height maps for use in terrain
	public class FractalFactory
	{
		float	mVariance;
		float	mMedianHeight;
		int		mWidth;
		int		mHeight;

		Random	mRnd;


		public FractalFactory(float variance, float medianHeight, int width, int height)
		{
			mVariance		=variance;
			mMedianHeight	=medianHeight;
			mWidth			=width;
			mHeight			=height;
		}


		public float[,] CreateFractal(int seed, int size)
		{
			mRnd	=new Random(seed);

			float	[,]ret	=new float[mHeight, mWidth];

			for(int y=0;y < mHeight;y++)
			{
				for(int x=0;x < mWidth;x++)
				{
					ret[y, x]	=mMedianHeight;
				}
			}

			Subdivide(ret, 0, 0, mWidth, mHeight, mVariance);

			return	ret;
		}


		public static int Erode(float [,]data, Random rnd, int iterations,
			float rain, float solubility, float evaporation)
		{
			int	w	=data.GetLength(1);
			int	h	=data.GetLength(0);

			//make a water map
			float	[,]water	=new float[h, w];
			float	halfRain	=rain / 2f;

			bool	bDry	=false;
			int		i		=0;
			for(i=0;i < iterations || !bDry;i++)
			{
				//movement
				bDry	=true;
				for(int y=0;y < h;y++)
				{
					for(int x=0;x < w;x++)
					{
						if(i < iterations)
						{
							float	rainFall	=Mathery.RandomFloatNext(rnd, halfRain, rain);

							water[y, x]	+=rainFall;
						}

						float	waterHeight	=water[y, x];

						if(waterHeight <= 0)
						{
							continue;
						}

						bDry	=false;

						float	land		=data[y, x];

						int		lowestX, lowestY;
						float	lowLand, lowWater;

						FindLowestNeighbor(data, water, x, y, out lowestX, out lowestY, out lowLand, out lowWater);

						float	lowHeight	=lowLand + lowWater;

						if(i < iterations && (land - lowLand) < -0.0001f && (land - lowLand) > 0.0001f)
						{
							data[y, x]	-=solubility * Math.Abs(land - lowLand);
						}

						if(land > lowHeight)
						{
							//move all water downhill
							water[lowestY, lowestX]	+=waterHeight;
							water[y, x]				=0f;
						}
						else if((waterHeight + land) < lowHeight)
						{
							//no transfer
						}
						else
						{
							//even out the water
							float	diff	=(land + waterHeight) - lowHeight;

							diff	/=2f;

							water[y, x]				-=diff;
							water[lowestY, lowestX]	+=diff;
						}
					}
				}

				//evaporation
				bDry	=true;
				for(int y=0;y < h;y++)
				{
					for(int x=0;x < w;x++)
					{
						float	wat			=water[y, x];

						wat	-=evaporation * i;
						if(wat < 0f)
						{
							wat	=0f;
						}
						else
						{
							bDry		=false;
							data[y, x]	+=evaporation * solubility;
						}

						water[y, x]	=wat;
					}
				}
			}

			return	i;
		}


		static void FindLowestNeighbor(float [,]data, float [,]water, int x, int y,
			out int lx, out int ly, out float lLand, out float lWater)
		{
			float	lowestWaterLand	=lLand	=lWater	=float.MaxValue;

			float	lowLand, lowWater;

			lx	=x;
			ly	=y;

			//upper left
			GetWrappedWaterAndLandHeight(data, water, x - 1, y - 1, out lowLand, out lowWater);
			float	waterAndLand	=lowLand + lowWater;
			if(waterAndLand < lowestWaterLand)
			{
				lowestWaterLand	=waterAndLand;
				lLand			=lowLand;
				lWater			=lowWater;
				GetWrappedCoords(data, x - 1, y - 1, out lx, out ly);
			}

			//upper center
			GetWrappedWaterAndLandHeight(data, water, x, y - 1, out lowLand, out lowWater);
			waterAndLand	=lowLand + lowWater;
			if(waterAndLand < lowestWaterLand)
			{
				lowestWaterLand	=waterAndLand;
				lLand			=lowLand;
				lWater			=lowWater;
				GetWrappedCoords(data, x, y - 1, out lx, out ly);
			}

			//upper right
			GetWrappedWaterAndLandHeight(data, water, x + 1, y - 1, out lowLand, out lowWater);
			waterAndLand	=lowLand + lowWater;
			if(waterAndLand < lowestWaterLand)
			{
				lowestWaterLand	=waterAndLand;
				lLand			=lowLand;
				lWater			=lowWater;
				GetWrappedCoords(data, x + 1, y - 1, out lx, out ly);
			}

			//left
			GetWrappedWaterAndLandHeight(data, water, x - 1, y, out lowLand, out lowWater);
			waterAndLand	=lowLand + lowWater;
			if(waterAndLand < lowestWaterLand)
			{
				lowestWaterLand	=waterAndLand;
				lLand			=lowLand;
				lWater			=lowWater;
				GetWrappedCoords(data, x - 1, y, out lx, out ly);
			}

			//right
			GetWrappedWaterAndLandHeight(data, water, x + 1, y, out lowLand, out lowWater);
			waterAndLand	=lowLand + lowWater;
			if(waterAndLand < lowestWaterLand)
			{
				lowestWaterLand	=waterAndLand;
				lLand			=lowLand;
				lWater			=lowWater;
				GetWrappedCoords(data, x + 1, y, out lx, out ly);
			}

			//lower left
			GetWrappedWaterAndLandHeight(data, water, x - 1, y + 1, out lowLand, out lowWater);
			waterAndLand	=lowLand + lowWater;
			if(waterAndLand < lowestWaterLand)
			{
				lowestWaterLand	=waterAndLand;
				lLand			=lowLand;
				lWater			=lowWater;
				GetWrappedCoords(data, x - 1, y + 1, out lx, out ly);
			}

			//lower center
			GetWrappedWaterAndLandHeight(data, water, x, y + 1, out lowLand, out lowWater);
			waterAndLand	=lowLand + lowWater;
			if(waterAndLand < lowestWaterLand)
			{
				lowestWaterLand	=waterAndLand;
				lLand			=lowLand;
				lWater			=lowWater;
				GetWrappedCoords(data, x, y + 1, out lx, out ly);
			}

			//lower right
			GetWrappedWaterAndLandHeight(data, water, x + 1, y + 1, out lowLand, out lowWater);
			waterAndLand	=lowLand + lowWater;
			if(waterAndLand < lowestWaterLand)
			{
				lowestWaterLand	=waterAndLand;
				lLand			=lowLand;
				lWater			=lowWater;
				GetWrappedCoords(data, x + 1, y + 1, out lx, out ly);
			}
		}


		static void GetWrappedWaterAndLandHeight(float [,]data, float [,]water, int x, int y,
			out float lowLand, out float lowWater)
		{
			int	wx, wy;

			GetWrappedCoords(data, x, y, out wx, out wy);

			lowWater	=water[wy, wx];
			lowLand		=data[wy, wx];
		}


		static void GetWrappedCoords(float [,]data, int x, int y, out int wrappedX, out int wrappedY)
		{
			wrappedX	=x;
			wrappedY	=y;

			int	w	=data.GetLength(1);
			int	h	=data.GetLength(0);

			while(wrappedX < 0)
			{
				wrappedX	+=w;
			}
			while(wrappedX >= w)
			{
				wrappedX	-=w;
			}

			while(wrappedY < 0)
			{
				wrappedY	+=h;
			}
			while(wrappedY >= h)
			{
				wrappedY	-=h;
			}
		}


		public static void MakeTiled(float [,]data, float depth)
		{
			int	w	=data.GetLength(1);
			int	h	=data.GetLength(0);

			//top edge
			for(int d=0;d < depth;d++)
			{
				for(int x=0;x < w;x++)
				{
					float	top		=data[d, x];
					float	bottom	=data[h - d - 1, x];

					float	average	=(top + bottom) * 0.5f;

					float	finalTop	=MathHelper.Lerp(average, top, d / depth);
					float	finalBottom	=MathHelper.Lerp(average, bottom, d / depth);

					data[d, x]			=finalTop;
					data[h - d - 1, x]	=finalBottom;
				}
			}

			//left edge
			for(int d=0;d < depth;d++)
			{
				for(int y=0;y < h;y++)
				{
					float	left	=data[y, d];
					float	right	=data[y, w - d - 1];

					float	average	=(left + right) * 0.5f;

					float	finalLeft	=MathHelper.Lerp(average, left, d / depth);
					float	finalRight	=MathHelper.Lerp(average, right, d / depth);

					data[y, d]			=finalLeft;
					data[y, w - d - 1]	=finalRight;
				}
			}
		}


		public static void SmoothPass(float [,]data)
		{
			int	w	=data.GetLength(1);
			int	h	=data.GetLength(0);
			for(int y=0;y < h;y++)
			{
				for(int x=0;x < w;x++)
				{
					float	upLeft, up, upRight;
					float	left, right;
					float	downLeft, down, downRight;

					if(y > 0)
					{
						if(x > 0)
						{
							upLeft	=data[y - 1, x - 1];
						}
						else
						{
							upLeft	=data[y - 1, x];
						}
						if(x < (w - 1))
						{
							upRight	=data[y - 1, x + 1];
						}
						else
						{
							upRight	=data[y - 1, x];
						}
						up	=data[y - 1, x];
					}
					else
					{
						if(x > 0)
						{
							upLeft	=data[y, x - 1];
						}
						else
						{
							upLeft	=data[y, x];
						}
						if(x < (w - 1))
						{
							upRight	=data[y, x + 1];
						}
						else
						{
							upRight	=data[y, x];
						}
						up	=data[y, x];
					}

					if(x > 0)
					{
						left	=data[y, x - 1];
					}
					else
					{
						left	=data[y, x];
					}

					if(x < (w - 1))
					{
						right	=data[y, x + 1];
					}
					else
					{
						right	=data[y, x];
					}

					if(y < (h - 1))
					{
						if(x > 0)
						{
							downLeft	=data[y + 1, x - 1];
						}
						else
						{
							downLeft	=data[y + 1, x];
						}

						if(x < (w - 1))
						{
							downRight	=data[y + 1, x + 1];
						}
						else
						{
							downRight	=data[y + 1, x];
						}

						down	=data[y + 1, x];
					}
					else
					{
						if(x > 0)
						{
							downLeft	=data[y, x - 1];
						}
						else
						{
							downLeft	=data[y, x];
						}

						if(x < (w - 1))
						{
							downRight	=data[y, x + 1];
						}
						else
						{
							downRight	=data[y, x];
						}

						down	=data[y, x];
					}

					float	sum	=upLeft + up + upRight + left
						+ right + downLeft + down + downRight;

					sum	/=8.0f;

					data[y, x]	=sum;
				}
			}
		}


		//This helps to create island shaped terrains.
		//Stuff inside the island range will be unaffected,
		//while distances greater will gradually fall off
		//into the sea / clouds / void
		public static void CircleBias(float [,]data, float islandRange)
		{
			int	w	=data.GetLength(1);
			int	h	=data.GetLength(0);

			Vector2	center	=Vector2.UnitX * (w / 2);
			center			+=Vector2.UnitY * (h / 2);

			for(int y=0;y < h;y++)
			{
				for(int x=0;x < w;x++)
				{
					float	heightBias	=0.0f;

					Vector2	xyVec	=Vector2.UnitX * x;
					xyVec			+=Vector2.UnitY * y;

					heightBias	=Vector2.Distance(center, xyVec);

					if(heightBias > islandRange)
					{
						heightBias	-=islandRange;
						heightBias	*=MathHelper.Log2E;
						data[y, x]	-=heightBias;
					}
				}
			}
		}


		void Subdivide(float[,] map, int xStart, int yStart, int width, int height, float variance)
		{
			if(width < 3)
			{
				return;
			}

			//calc halfway points
			int	halfWidth	=(width - 1) / 2;
			int	halfHeight	=(height - 1) / 2;

			//get center point
			float	topLeft		=map[yStart, xStart];
			float	topRight	=map[yStart, xStart + width - 1];
			float	bottomLeft	=map[yStart + height - 1, xStart];
			float	bottomRight	=map[yStart + height - 1, xStart + width - 1];

			float	center	=topLeft + topRight + bottomLeft + bottomRight;

			//average corners
			center	*=0.25f;

			//add some variance
			center	+=mRnd.Next(-4096, 4096) * (variance / 8192.0f);

			//set center point to center
			map[yStart + halfHeight, xStart + halfWidth]	=center;

			//set edge mids to center
			//unless they've already been changed
			if(map[yStart + halfHeight, xStart] == mMedianHeight)
			{
				map[yStart + halfHeight, xStart]
					=(topLeft + bottomLeft + center) / 3.0f;
			}
			if(map[yStart, xStart + halfWidth] == mMedianHeight)
			{
				map[yStart, xStart + halfWidth]
					=(topLeft + topRight + center) / 3.0f;
			}
			if(map[yStart + halfHeight, xStart + width - 1] == mMedianHeight)
			{
				map[yStart + halfHeight, xStart + width - 1]
					=(topRight + bottomRight + center) / 3.0f;
			}
			if(map[yStart + height - 1, xStart + halfWidth] == mMedianHeight)
			{
				map[yStart + height - 1, xStart + halfWidth]
					=(bottomLeft + bottomRight + center) / 3.0f;
			}

			Subdivide(map, xStart, yStart, halfWidth + 1, halfHeight + 1, variance / 2.0f);
			Subdivide(map, xStart + halfWidth, yStart, halfWidth + 1, halfHeight + 1, variance / 2.0f);
			Subdivide(map, xStart, yStart + halfHeight, halfWidth + 1, halfHeight + 1, variance / 2.0f);
			Subdivide(map, xStart + halfWidth, yStart + halfHeight, halfWidth + 1, halfHeight + 1, variance / 2.0f);
		}
	}
}
