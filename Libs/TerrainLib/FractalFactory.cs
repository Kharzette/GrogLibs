using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TerrainLib
{
	public class FractalFactory
	{
		float	mVariance		=500.0f;
		float	mMedianHeight	=128.0f;
		int		mWidth			=1025;
		int		mHeight			=1025;

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
