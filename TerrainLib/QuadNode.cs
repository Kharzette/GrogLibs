using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using SharpDX;
using UtilityLib;


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
					neh[y, x - halfW + 1]	=data[y, x];
				}
			}

			//southwest
			for(int y=(halfH - 1);y < h;y++)
			{
				for(int x=0;x < halfW;x++)
				{
					swh[y - halfH + 1, x]	=data[y, x];
				}
			}

			//southeast
			for(int y=(halfH - 1);y < h;y++)
			{
				for(int x=(halfW - 1);x < w;x++)
				{
					seh[y - halfH + 1, x - halfW + 1]	=data[y, x];
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


		internal void FixBoxHeights(float[,] heightGrid)
		{
			if(mChildNorthEast == null)
			{
				int	startX	=(int)Math.Round(mBounds.Minimum.X);
				int	startZ	=(int)Math.Round(mBounds.Minimum.Z);
				int	endX	=(int)Math.Round(mBounds.Maximum.X);
				int	endZ	=(int)Math.Round(mBounds.Maximum.Z);

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

			mChildNorthWest.FixBoxHeights(heightGrid);
			mChildNorthEast.FixBoxHeights(heightGrid);
			mChildSouthWest.FixBoxHeights(heightGrid);
			mChildSouthEast.FixBoxHeights(heightGrid);
		}


		bool TryCollide(List<Vector3> tri, Vector3 start, Vector3 end, out Vector3 hit)
		{
			Vector3	norm;
			float	dist;

			hit	=Vector3.Zero;

			Vector3	direction	=end - start;

			Mathery.PlaneFromVerts(tri, out norm, out dist);

			//check facing
			if(norm.dot(direction) >= 0f)
			{
				return	false;
			}
			float	d1	=norm.dot(start) - dist;
			float	d2	=norm.dot(end) - dist;

			if(d1 > 0 && d2 > 0)
			{
				return	false;
			}

			if(d1 < 0 && d2 < 0)
			{
				return	false;
			}

			if(d1 == 0 && d2 == 0)
			{
				//exactly along the plane?  possible I guess
				//set a breakpoint here
				return	false;
			}

			float	ratio	=d1 / (d1 - d2);

			hit	=start + ratio * (end - start);

			//check inside
			float	closeToTwoPi	=6.2f;
			float	angSum			=Mathery.ComputeAngleSum(hit, tri);
			if(angSum >= closeToTwoPi)
			{
				return	true;
			}
			return	false;
		}


		internal bool Trace(Vector3 start, Vector3 end, out Vector3 hit)
		{
			float?	distHit	=Mathery.RayIntersectBox(start, end, mBounds);
			if(distHit == null)
			{
				hit	=Vector3.Zero;
				return	false;
			}

			if(mChildNorthWest == null)
			{
				//intersect heights
				List<Vector3>	tri1	=new List<Vector3>();
				tri1.Add(new Vector3(mBounds.Minimum.X, mHeights[0, 0], mBounds.Minimum.Z));
				tri1.Add(new Vector3(mBounds.Maximum.X, mHeights[0, 1], mBounds.Minimum.Z));
				tri1.Add(new Vector3(mBounds.Minimum.X, mHeights[1, 0], mBounds.Maximum.Z));

				if(TryCollide(tri1, start, end, out hit))
				{
					return	true;
				}

				tri1.Clear();
				tri1.Add(new Vector3(mBounds.Maximum.X, mHeights[0, 1], mBounds.Minimum.Z));
				tri1.Add(new Vector3(mBounds.Maximum.X, mHeights[1, 1], mBounds.Maximum.Z));
				tri1.Add(new Vector3(mBounds.Minimum.X, mHeights[1, 0], mBounds.Maximum.Z));
				if(TryCollide(tri1, start, end, out hit))
				{
					return	true;
				}
				return	false;
			}

			Vector3	nw, ne, sw, se;

			bool	nwHit	=mChildNorthWest.Trace(start, end, out nw);
			bool	neHit	=mChildNorthEast.Trace(start, end, out ne);
			bool	swHit	=mChildSouthWest.Trace(start, end, out sw);
			bool	seHit	=mChildSouthEast.Trace(start, end, out se);

			if(!(nwHit || neHit || swHit || seHit))
			{
				hit	=Vector3.Zero;
				return	false;
			}

			float	nwDist	=(nwHit)? Vector3.Distance(nw, start) : float.MaxValue;
			float	neDist	=(neHit)? Vector3.Distance(ne, start) : float.MaxValue;
			float	swDist	=(swHit)? Vector3.Distance(sw, start) : float.MaxValue;
			float	seDist	=(seHit)? Vector3.Distance(se, start) : float.MaxValue;

			float	bestDist	=float.MaxValue;
			Vector3	bestHit		=Vector3.Zero;

			if(nwDist < bestDist)
			{
				bestDist	=nwDist;
				bestHit		=nw;
			}
			if(neDist < bestDist)
			{
				bestDist	=neDist;
				bestHit		=ne;
			}
			if(swDist < bestDist)
			{
				bestDist	=swDist;
				bestHit		=sw;
			}
			if(seDist < bestDist)
			{
				bestDist	=seDist;
				bestHit		=se;
			}

			hit	=bestHit;

			return	true;
		}
	}
}
