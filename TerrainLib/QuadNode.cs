using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using SharpDX;


namespace TerrainLib
{
	internal class QuadNode
	{
		BoundingBox	mBounds;

		QuadNode	mChildNorthWest;
		QuadNode	mChildNorthEast;
		QuadNode	mChildSouthWest;
		QuadNode	mChildSouthEast;

		//null unless leaf?
		float	[,]mHeights;


		internal void Build(float [,]data, BoundingBox bound)
		{
			int	w	=data.GetLength(1);
			int	h	=data.GetLength(0);

			if((w * h) <= QuadTree.LeafPoints)
			{
				//leaf node
				mHeights	=data;
				mBounds		=bound;
				return;
			}

			mBounds	=bound;

			BoundingBox	nw, ne, sw, se;
			SplitBound(bound, out nw, out ne, out sw, out se);

			float	[,]nwh, neh, swh, seh;
			SplitHeights(data, out nwh, out neh, out swh, out seh);

			mChildNorthWest	=new QuadNode();
			mChildNorthEast	=new QuadNode();
			mChildSouthEast	=new QuadNode();
			mChildSouthWest	=new QuadNode();

			mChildNorthWest.Build(nwh, nw);
			mChildNorthEast.Build(neh, ne);
			mChildSouthWest.Build(swh, sw);
			mChildSouthEast.Build(seh, se);

			data	=null;
		}


		internal void GetAllBoxes(List<BoundingBox> boxes)
		{
			if(mChildNorthEast == null)
			{
				boxes.Add(mBounds);
				return;
			}

			mChildNorthWest.GetAllBoxes(boxes);
			mChildNorthEast.GetAllBoxes(boxes);
			mChildSouthWest.GetAllBoxes(boxes);
			mChildSouthEast.GetAllBoxes(boxes);
		}


		void SplitHeights(float [,]data, out float[,]nwh,
			out float[,]neh, out float[,]swh, out float[,]seh)
		{
			int	w	=data.GetLength(1);
			int	h	=data.GetLength(0);

			int	halfW	=w / 2;
			int	halfH	=h / 2;

			//one height of overlap because
			//all data is power of two plus one
			halfW	+=1;
			halfH	+=1;

			nwh	=new float[halfH, halfW];
			neh	=new float[halfH, halfW];
			swh	=new float[halfH, halfW];
			seh	=new float[halfH, halfW];

			//northwest
			for(int y=0;y < halfH;y++)
			{
				for(int x=0;x < halfW;x++)
				{
					nwh[y, x]	=data[y, x];
				}
			}

			//northeast
			for(int y=0;y < halfH;y++)
			{
				for(int x=(halfW - 1);x < w;x++)
				{
					nwh[y, x - halfW + 1]	=data[y, x];
				}
			}

			//southwest
			for(int y=(halfH - 1);y < h;y++)
			{
				for(int x=0;x < halfW;x++)
				{
					nwh[y - halfH + 1, x]	=data[y, x];
				}
			}

			//southeast
			for(int y=(halfH - 1);y < h;y++)
			{
				for(int x=(halfW - 1);x < w;x++)
				{
					nwh[y - halfH + 1, x - halfW + 1]	=data[y, x];
				}
			}
		}


		void SplitBound(BoundingBox bound, out BoundingBox nw,
			out BoundingBox ne, out BoundingBox sw, out BoundingBox se)
		{
			Vector3	middle	=(bound.Minimum + bound.Maximum) / 2f;

			nw.Minimum		=bound.Minimum;
			nw.Maximum		=bound.Maximum;
			nw.Maximum.X	=middle.X;
			nw.Maximum.Z	=middle.Z;

			se.Minimum		=bound.Minimum;
			se.Maximum		=bound.Maximum;
			se.Minimum.X	=middle.X;
			se.Minimum.Z	=middle.Z;

			sw.Minimum.X	=bound.Minimum.X;
			sw.Minimum.Z	=middle.Z;
			sw.Minimum.Y	=bound.Minimum.Y;
			sw.Maximum.X	=middle.X;
			sw.Maximum.Z	=bound.Maximum.Z;
			sw.Maximum.Y	=bound.Maximum.Y;

			ne.Minimum.X	=middle.X;
			ne.Minimum.Z	=bound.Minimum.Z;
			ne.Minimum.Y	=bound.Minimum.Y;
			ne.Maximum.X	=bound.Maximum.X;
			ne.Maximum.Z	=middle.Z;
			ne.Maximum.Y	=bound.Maximum.Y;
		}


		internal void FixBoxHeights(float[,] heightGrid, float polySize)
		{
			if(mChildNorthEast == null)
			{
				int	startX	=(int)Math.Round(mBounds.Minimum.X / polySize);
				int	startZ	=(int)Math.Round(mBounds.Minimum.Z / polySize);
				int	endX	=(int)Math.Round(mBounds.Maximum.X / polySize);
				int	endZ	=(int)Math.Round(mBounds.Maximum.Z / polySize);

				float	minHeight	=float.MaxValue;
				float	maxHeight	=float.MinValue;
				for(int z=startZ;z <= endZ;z++)
				{
					for(int x=startX;x <= endX;x++)
					{
						float	height	=heightGrid[z, x];

						if(height < minHeight)
						{
							minHeight	=height;
						}
						if(height > maxHeight)
						{
							maxHeight	=height;
						}
					}
				}

				mBounds.Minimum.Y	=minHeight;
				mBounds.Maximum.Y	=maxHeight;

				return;
			}

			mChildNorthWest.FixBoxHeights(heightGrid, polySize);
			mChildNorthEast.FixBoxHeights(heightGrid, polySize);
			mChildSouthWest.FixBoxHeights(heightGrid, polySize);
			mChildSouthEast.FixBoxHeights(heightGrid, polySize);
		}
	}
}
