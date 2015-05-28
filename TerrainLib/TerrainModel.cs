using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX;


namespace TerrainLib
{
	public class TerrainModel
	{
		float	[,]mHeightGrid;
		float	mPolySize;
		int		mGridSize;

		BoundingBox	mBox;

		QuadTree	mTree;


		public TerrainModel(float [,]grid, float polySize, int gridSize)
		{
			mHeightGrid	=grid;
			mPolySize	=polySize;
			mGridSize	=gridSize;

			//calc box
			CalcBounds();

			//build tree
			mTree	=new QuadTree();

			mTree.Build(grid, mBox);
		}


		public List<BoundingBox> GetAllBoxes()
		{
			return	mTree.GetAllBoxes();
		}


		public void FixBoxHeights()
		{
			mTree.FixBoxHeights(mHeightGrid, mPolySize);
		}


		public void FreeAll()
		{
			mHeightGrid	=null;
		}


		public int GetMapSize()
		{
			return	(int)(mGridSize * mPolySize);
		}


		public Vector3 GetCenter()
		{
			int	halfGrid	=mGridSize / 2;

			Vector3	ret	=Vector3.UnitX * (halfGrid * mPolySize);
			ret			+=Vector3.UnitZ * (halfGrid * mPolySize);

			ret.Y	=GetHeight(ret);

			return	ret;
		}


		public Vector3 GetRandomPositionOnGround(Random rand, float islandSize, float minDistance)
		{
			int	mapSize	=GetMapSize();

			Vector3	center	=Vector3.One * (mapSize / 2.0f);

			center.Y	=0.0f;

			Vector3	pos	=Vector3.Zero;

			while(true)
			{
				pos.X	=rand.Next(2, mapSize - 2);
				pos.Z	=rand.Next(2, mapSize - 2);

				float	dist	=Vector3.Distance(pos, center);

				if(dist < islandSize && dist > minDistance)
				{
					break;
				}
			}

			pos.Y	=GetHeight(pos);

			return	pos;
		}


		unsafe public float GetHeight(Vector3 coord)
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

			float	topLeft, topRight, botLeft, botRight;
			fixed(float *pHG = mHeightGrid)
			{
				int	yOfs	=y * mGridSize;

				topLeft		=*(pHG + yOfs + x);
				topRight	=*(pHG + yOfs + x + 1);
				botLeft		=*(pHG + yOfs + mGridSize + x);
				botRight	=*(pHG + yOfs + mGridSize + x + 1);
			}

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


		public float GetHeightSafe(Vector3 coord)
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


		//get the max height in a sampled radius
		public float GetMaxHeightRadius(Vector3 coord, float rad)
		{
			float	ret	=GetHeight(coord);

			ret	=Math.Max(ret, GetHeight(coord + Vector3.UnitX * rad));
			ret	=Math.Max(ret, GetHeight(coord + Vector3.UnitX * -rad));
			ret	=Math.Max(ret, GetHeight(coord + Vector3.UnitZ * rad));
			ret	=Math.Max(ret, GetHeight(coord + Vector3.UnitZ * -rad));

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


		//this doesn't really work, was something I was fooling around with
		bool RayCast(Vector3 startPos, Vector3 endPos, float dist, out Vector3 impacto)
		{
			Vector3	testPoint	=endPos - startPos;

			testPoint.Normalize();

			testPoint	*=dist;
			testPoint	+=startPos;

			float	height	=GetHeight(testPoint);

			if(height == -1.0f)
			{
				impacto	=endPos;
				return	false;
			}
			else if(height >= (testPoint.Y - 1.0f) && height <= (testPoint.Y + 1.0f))
			{
				impacto	=testPoint;
				return	true;
			}
			else if(height < testPoint.Y)
			{
				return	RayCast(startPos, endPos, dist + 10.0f, out impacto);
			}
			else
			{
				impacto	=testPoint;
				return	true;
			}
		}


		public bool RayCast(Vector3 startPos, Vector3 endPos, out Vector3 impacto)
		{
			return	RayCast(startPos, endPos, 2.0f, out impacto);
		}


		public float GetGoodCloudHeight(float islandDist)
		{
			float	max	=float.MinValue;
			float	min	=float.MaxValue;

			int	w	=mHeightGrid.GetLength(1);
			int	h	=mHeightGrid.GetLength(0);

			Vector3	center	=Vector3.UnitX * (w / 2);
			center			+=Vector3.UnitZ * (h / 2);
			center			+=Vector3.UnitY * mHeightGrid[(h/2), (w/2)];

			for(int y=0;y < mGridSize;y++)
			{
				for(int x=0;x < mGridSize;x++)
				{
					Vector3	pos	=Vector3.UnitX * x;
					pos		+=Vector3.UnitZ * y;
					pos.Y	=mHeightGrid[y, x];

					if(Vector3.Distance(center, pos) < islandDist)
					{
						if(pos.Y < min)
						{
							min	=pos.Y;
						}

						if(pos.Y > max)
						{
							max	=pos.Y;
						}
					}
				}
			}
			return	min;
		}


		public float[,] GetCloudyFactor(float islandDist, int gran, float cloudHeight)
		{
			float	[,]cloudyFactor	=new float[mGridSize / gran, mGridSize / gran];

			int	w	=mHeightGrid.GetLength(1);
			int	h	=mHeightGrid.GetLength(0);

			Vector3	center	=Vector3.UnitX * (w / 2);
			center			+=Vector3.UnitZ * (h / 2);
			center			+=Vector3.UnitY * mHeightGrid[(h/2), (w/2)];

			for(int y=0;y < mGridSize - 1;y++)
			{
				for(int x=0;x < mGridSize - 1;x++)
				{
					Vector3	pos	=Vector3.UnitX * x;
					pos			+=Vector3.UnitZ * y;
					pos.Y		=mHeightGrid[y, x];

					int	granx	=x / gran;
					int	grany	=y / gran;

					if(Vector3.Distance(center, pos) < islandDist)
					{
						if(pos.Y < cloudHeight)
						{
							cloudyFactor[grany, granx]	-=0.001f;
						}
						else
						{
							cloudyFactor[grany, granx]	+=0.01f;
						}
					}
					else
					{
						cloudyFactor[grany, granx]	-=0.01f;
					}
				}
			}

			return	cloudyFactor;
		}


		//this doesn't work
		public BoundingBox GetFrustumIntersection(BoundingFrustum frust)
		{
			Vector3	[]corners	=frust.GetCorners();

			//clip against box planes
			Vector3	ray0	=corners[4] - corners[0];
			Vector3	ray1	=corners[5] - corners[1];
			Vector3	ray2	=corners[6] - corners[2];
			Vector3	ray3	=corners[7] - corners[3];

//			ray0	=ClipToBox(mBox, ray0);
//			ray1	=ClipToBox(mBox, ray1);
//			ray2	=ClipToBox(mBox, ray2);
//			ray3	=ClipToBox(mBox, ray3);

			//stuff clipped back into corners
			corners[4]	=ray0;
			corners[5]	=ray1;
			corners[6]	=ray2;
			corners[7]	=ray3;

			//find the bounds of the clipped corners
			Vector3	max	=Vector3.One * float.MinValue;
			Vector3	min	=Vector3.One * float.MaxValue;
			foreach(Vector3 pos in corners)
			{
				if(pos.X < min.X)
				{
					min.X	=pos.X;
				}
				if(pos.X > max.X)
				{
					max.X	=pos.X;
				}
				if(pos.Y < min.Y)
				{
					min.Y	=pos.Y;
				}
				if(pos.Y > max.Y)
				{
					max.Y	=pos.Y;
				}
				if(pos.Z < min.Z)
				{
					min.Z	=pos.Z;
				}
				if(pos.Z > max.Z)
				{
					max.Z	=pos.Z;
				}
			}
			return	new BoundingBox(min, max);
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


		void CalcBounds()
		{
			Vector3	max	=Vector3.One * float.MinValue;
			Vector3	min	=Vector3.One * float.MaxValue;

			for(int y=0;y < mGridSize;y++)
			{
				for(int x=0;x < mGridSize;x++)
				{
					Vector3	pos	=Vector3.Zero;

					float	height	=mHeightGrid[y, x];

					pos.Y	=height;
					pos.X	=x * mPolySize;
					pos.Z	=y * mPolySize;

					if(pos.X < min.X)
					{
						min.X	=pos.X;
					}
					if(pos.X > max.X)
					{
						max.X	=pos.X;
					}
					if(pos.Y < min.Y)
					{
						min.Y	=pos.Y;
					}
					if(pos.Y > max.Y)
					{
						max.Y	=pos.Y;
					}
					if(pos.Z < min.Z)
					{
						min.Z	=pos.Z;
					}
					if(pos.Z > max.Z)
					{
						max.Z	=pos.Z;
					}
				}
			}

			mBox	=new BoundingBox(min, max);
		}
	}
}
