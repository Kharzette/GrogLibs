using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using Microsoft.Xna.Framework;


namespace BSPCore
{
	internal partial class GBSPBrush
	{
		Bounds		mBounds	=new Bounds();
		UInt32		mSide;		//marked during the bsp split heuristic, and used in list split
		UInt32		mTestSide;	//used as a temp during the bsp split heuristic
		MapBrush	mOriginal;	//original brush from the map file

		List<GBSPSide>	mSides	=new List<GBSPSide>();


		#region Construction
		internal GBSPBrush() { }
		internal GBSPBrush(GBSPBrush copyMe)
		{
			mBounds		=new Bounds(copyMe.mBounds);
			mOriginal	=copyMe.mOriginal;
			mSide		=copyMe.mSide;
			mTestSide	=copyMe.mTestSide;

			mSides.Clear();

			foreach(GBSPSide side in copyMe.mSides)
			{
				mSides.Add(new GBSPSide(side));
			}
		}


		internal GBSPBrush(MapBrush mb, PlanePool pp)
		{
			Int32	Vis	=0;

			for(int j=0;j < mb.mOriginalSides.Count;j++)
			{
				if(mb.mOriginalSides[j].mPoly.VertCount() > 2)
				{
					Vis++;
				}
			}

			if(Vis == 0)
			{
				return;
			}

			mOriginal	=mb;

			for(int j=0;j < mb.mOriginalSides.Count;j++)
			{
				mSides.Add(new GBSPSide(mb.mOriginalSides[j]));
				if((mb.mOriginalSides[j].mFlags & GBSPSide.SIDE_HINT) != 0)
				{
					mSides[j].mFlags	|=GBSPSide.SIDE_VISIBLE;
				}
			}

			mBounds	=mb.mBounds;

			BoundBrush();

			if(!CheckBrush(pp))
			{
				CoreEvents.Print("GBSPBrush CTOR:  Bad brush.\n");
				return;
			}
		}
		#endregion


		#region Checks & Gets & Tests
		void BoundBrush()
		{
			mBounds.Clear();

			for(int i=0;i < mSides.Count;i++)
			{
				mSides[i].AddToBounds(mBounds);
			}
		}


		internal Vector3 GetCenter()
		{
			Vector3	ret	=Vector3.Zero;

			for(int i=0;i < mSides.Count;i++)
			{
				ret	+=mSides[i].GetCenter();			
			}
			ret	/=mSides.Count;

			return	ret;
		}


		bool Overlaps(GBSPBrush otherBrush)
		{
			//check bounds first
			if(!mBounds.Overlaps(otherBrush.mBounds))
			{
				return	false;
			}

			for(int i=0;i < mSides.Count;i++)
			{
				for(int j=0;j < otherBrush.mSides.Count;j++)
				{
					if(mSides[i].mPlaneNum == otherBrush.mSides[j].mPlaneNum &&
						mSides[i].mbFlipSide != otherBrush.mSides[j].mbFlipSide)
					{
						return	false;
					}
				}
			}
			return	true;
		}


		bool PointInside(Vector3 point, PlanePool pp, float epsilon)
		{
			foreach(GBSPSide s in mSides)
			{
				GBSPPlane	p	=pp.mPlanes[s.mPlaneNum];

				if(s.mbFlipSide)
				{
					p.Inverse();
				}

				float	dist	=p.Distance(point);
				if(dist > epsilon)
				{
					return	false;
				}
			}
			return	true;
		}


		bool ReallyOverlaps(GBSPBrush otherBrush, PlanePool pp)
		{
			//check bounds first
			if(!mBounds.Overlaps(otherBrush.mBounds))
			{
				return	false;
			}

			for(int j=0;j < otherBrush.mSides.Count;j++)
			{
				GBSPPoly	p	=otherBrush.mSides[j].mPoly;

				if(p.mVerts == null)
				{
					continue;
				}

				for(int i=0;i < p.mVerts.Length;i++)
				{
					if(PointInside(p.mVerts[i], pp, -0.1f))
					{
						return	true;
					}
				}
			}

			return	false;
		}


		internal static bool SameContents(GBSPBrush b1, GBSPBrush b2)
		{
			return	(b1.mOriginal.mContents == b2.mOriginal.mContents);
		}


		//determines via contents if it is ok for otherbrush to carve
		//into thisbrush (water can't chop stone etc)
		bool BrushCanBite(GBSPBrush otherBrush)
		{
			UInt32	c1, c2;

			c1	=mOriginal.mContents;
			c2	=otherBrush.mOriginal.mContents;

			if(((c1 & Contents.BSP_CONTENTS_DETAIL2) != 0) &&
				!((c2 & Contents.BSP_CONTENTS_DETAIL2) != 0))
			{
				return	false;
			}

			if(((c1|c2) & Contents.BSP_CONTENTS_FLOCKING) != 0)
			{
				return	false;
			}

			if((c1 & Contents.BSP_CONTENTS_SOLID2) != 0)
			{
				return	true;
			}

			return	false;
		}


		UInt32 MostlyOnSide(GBSPPlane plane)
		{
			UInt32	side	=GBSPPlane.PSIDE_FRONT;
			float	max		=0.0f;

			for(int i=0;i < mSides.Count;i++)
			{
				if(mSides[i].mPoly.VertCount() < 3)
				{
					continue;
				}
				side	=mSides[i].mPoly.GetMaxDistance(plane, ref max);
			}

			return	side;
		}


		internal bool CheckBrush(PlanePool pool)
		{
			if(mSides.Count < 3)
			{
				return	false;
			}

			if(pool != null)
			{
				ClipPools	cp	=new ClipPools();

				//check side planes
				for(int i=0;i < mSides.Count;i++)
				{
					GBSPSide	iSide	=mSides[i];
					GBSPPlane	iPlane	=pool.mPlanes[iSide.mPlaneNum];

					if(mSides[i].mbFlipSide)
					{
						iPlane.Inverse();
					}

					GBSPPoly	iPoly	=new GBSPPoly(iPlane);

					for(int j=0;j < mSides.Count;j++)
					{
						if(i == j)
						{
							continue;
						}

						GBSPPlane	jPlane	=pool.mPlanes[mSides[j].mPlaneNum];

						if(mSides[j].mbFlipSide)
						{
							jPlane.Inverse();
						}

						if(!iPoly.ClipPoly(jPlane, true, cp))
						{
							return	false;	//something nonconvex in here
						}
						if(iPoly.mVerts == null)
						{
							return	false;	//something nonconvex
						}
					}
				}
			}

			if(mBounds.IsMaxExtents())
			{
				return	false;
			}
			return	true;
		}


		internal float Volume(PlanePool pool)
		{
			GBSPPoly	cornerPoly	=null;

			foreach(GBSPSide s in mSides)
			{
				if(s.mPoly.VertCount() > 2)
				{
					cornerPoly	=s.mPoly;
					break;
				}
			}
			if(cornerPoly == null)
			{
				return	0.0f;
			}

			float	volume	=0.0f;
			foreach(GBSPSide s in mSides)
			{
				GBSPPoly	p	=s.mPoly;
				if(p == null)
				{
					continue;
				}

				GBSPPlane	plane	=pool.mPlanes[s.mPlaneNum];

				if(s.mbFlipSide)
				{
					plane.Inverse();
				}

				float	d		=-cornerPoly.GetCornerDistance(plane);
				float	area	=p.Area();

				volume	+=d * area;
			}

			volume	/=3.0f;
			return	volume;
		}


		//used in the bsp split plane choosing algorithm
		UInt32 TestBrushToPlane(int planeNum, bool planeSide, PlanePool pool,
			out int numSplits, out bool bHintSplit, ref int EpsilonBrush)
		{
			GBSPPlane	plane;
			UInt32		sideFlag;
			float		frontDist, backDist;
			Int32		frontCount, backCount;

			numSplits	=0;
			bHintSplit	=false;

			foreach(GBSPSide s in mSides)
			{
				int	Num	=s.mPlaneNum;
				
				if(Num == planeNum && !s.mbFlipSide)
				{
					return	GBSPPlane.PSIDE_BACK | GBSPPlane.PSIDE_FACING;
				}

				if(Num == planeNum && s.mbFlipSide)
				{
					return	GBSPPlane.PSIDE_FRONT | GBSPPlane.PSIDE_FACING;
				}
			}
			
			//See if it's totally on one side or the other
			plane	=pool.mPlanes[planeNum];

			sideFlag	=mBounds.BoxOnPlaneSide(plane);

			if(sideFlag != GBSPPlane.PSIDE_BOTH)
			{
				return	sideFlag;
			}
			
			//The brush is split, count the number of splits 
			frontDist	=backDist	=0.0f;

			foreach(GBSPSide s in mSides)
			{
				if((s.mFlags & GBSPSide.SIDE_NODE) != 0)
				{
					continue;
				}
				if((s.mFlags & GBSPSide.SIDE_VISIBLE) == 0)
				{
					continue;
				}
				if(s.mPoly.VertCount() < 3)
				{
					continue;
				}

				frontCount	=backCount	=0;
				s.mPoly.SplitSideTest(plane, out frontCount, out backCount, ref frontDist, ref backDist);

				if(frontCount != 0 && backCount != 0)
				{
					numSplits++;
					if((s.mFlags & GBSPSide.SIDE_HINT) != 0)
					{
						bHintSplit	=true;
					}
				}
			}

			//Check to see if this split would produce a tiny brush (would result in tiny leafs, bad for vising)
			if((frontDist > 0.0f && frontDist < 1.0f) || (backDist < 0.0f && backDist > -1.0f))
			{
				EpsilonBrush++;
			}

			return	sideFlag;
		}


		internal void GetTriangles(List<Vector3> verts, List<uint> indexes, bool bCheckFlags)
		{
			foreach(GBSPSide s in mSides)
			{
				s.GetTriangles(verts, indexes, bCheckFlags);
			}
		}
		#endregion


		#region Carving Operations
		//cut a clone of this brush down to the box planes passed in
		GBSPBrush	ChopToBoxAndClone(int []planes, PlanePool pp, ClipPools cp)
		{
			GBSPBrush	ret	=new GBSPBrush(this);
			for(int i=0;i < 6;i++)
			{
				GBSPBrush	front, back;
				if(i < 3)
				{
					ret.Split(planes[i], false, 0, false, pp, out front, out back, false, cp);
				}
				else
				{
					ret.Split(planes[i], true, 0, false, pp, out front, out back, false, cp);
				}
				if(back == null)
				{
					return	null;
				}
				ret	=back;
			}

			//the reason this works is that the box plane numbers
			//are not shared with everything else
			for(int i=0;i < ret.mSides.Count;i++)
			{
				int	pnum	=ret.mSides[i].mPlaneNum;
				if(pnum == planes[0] || pnum == planes[2]
					|| pnum == planes[3] || pnum == planes[5])
				{
					ret.mSides[i].mTexInfo	=-1;
					ret.mSides[i].mFlags	|=GBSPSide.SIDE_NODE;
				}
			}

			return	ret;
		}


		//split this brush by the plane passed in, assigning the newly
		//created split face the flags (splitFaceFlags and bVisible),
		//returning the results in front & back
		internal void Split(Int32 planeNum, bool bFlipSide, byte splitFaceFlags, bool bVisible,
					PlanePool pool, out GBSPBrush front, out GBSPBrush back, bool bVerbose, ClipPools cp)
		{
			GBSPPlane	poolPlane, sidedPlane;
			float		frontDist, backDist;
			GBSPBrush	[]resultBrushes	=new GBSPBrush[2];

			poolPlane		=pool.mPlanes[planeNum];
			poolPlane.mType	=GBSPPlane.PLANE_ANY;

			sidedPlane	=poolPlane;
			if(bFlipSide)
			{
				sidedPlane.Inverse();
			}

			front	=back	=null;

			// Check all points
			frontDist = backDist = 0.0f;

			foreach(GBSPSide s in mSides)
			{
				if(s.mPoly == null)
				{
					continue;
				}
				s.mPoly.GetSplitMaxDist(poolPlane, bFlipSide, ref frontDist, ref backDist);
			}
			
			if(frontDist < 0.1f)
			{
				back	=new GBSPBrush(this);
				return;
			}

			if(backDist > -0.1f)
			{
				front	=new GBSPBrush(this);
				return;
			}

			//create a new poly from the split plane
			GBSPPoly	midPoly	=new GBSPPoly(sidedPlane);
			if(midPoly == null && bVerbose)
			{
				CoreEvents.Print("SplitBrush:  Could not create poly.\n");
			}
			
			//Clip the poly by all the planes of the brush being split
			foreach(GBSPSide s in mSides)
			{
				if(midPoly.IsTiny())
				{
					break;
				}
				GBSPPlane	plane2	=pool.mPlanes[s.mPlaneNum];
				
				midPoly.ClipPolyEpsilon(0.0f, plane2, !s.mbFlipSide, cp);
			}

			if(midPoly.IsTiny())
			{	
				UInt32	Side	=MostlyOnSide(sidedPlane);
				
				if(Side == GBSPPlane.PSIDE_FRONT)
				{
					front	=new GBSPBrush(this);
				}
				if(Side == GBSPPlane.PSIDE_BACK)
				{
					back	=new GBSPBrush(this);
				}
				return;
			}

			//Create 2 brushes
			for(int i=0;i < 2;i++)
			{
				resultBrushes[i]	=new GBSPBrush();
				
				if(resultBrushes[i] == null && bVerbose)
				{
					CoreEvents.Print("SplitBrush:  Out of memory for brush.\n");
				}
				
				resultBrushes[i].mOriginal	=mOriginal;
			}

			//Split all the current polys of the brush being
			//split, and distribute it to the other 2 brushes
			foreach(GBSPSide s in mSides)
			{
				GBSPPoly	[]poly	=new GBSPPoly[2];
				
				if(s.mPoly == null || s.mPoly.mVerts == null)
				{
					continue;
				}

				GBSPPoly	sidePoly	=new GBSPPoly(s.mPoly);
				if(!sidePoly.SplitEpsilon(0.0f, sidedPlane, out poly[0], out poly[1], false) && bVerbose)
				{
					CoreEvents.Print("SplitBrush:  Error splitting poly...\n");
				}

				for(int j=0;j < 2;j++)
				{
					GBSPSide	destSide;

					if(poly[j] == null)
					{
						continue;
					}

					if(poly[j].IsTiny())
					{
						continue;
					}

					if(poly[j].IsMaxExtents())
					{
						continue;
					}

					destSide	=new GBSPSide(s);

					resultBrushes[j].mSides.Add(destSide);
					
					destSide.mPoly	=poly[j];
					destSide.mFlags	&=~GBSPSide.SIDE_TESTED;
				}
			}

			for(int i=0;i < 2;i++)
			{
				resultBrushes[i].BoundBrush();

				if(!resultBrushes[i].CheckBrush(null))
				{
					CoreEvents.Print("SplitBrush:  Result brush failed CheckBrush()\n");
					resultBrushes[i]	=null;
				}			
			}

			if(resultBrushes[0] == null || resultBrushes[1] == null)
			{
				if(bVerbose)
				{
					if(resultBrushes[0] == null && resultBrushes[1] == null)
					{
						CoreEvents.Print("SplitBrush:  Split removed brush\n");
					}
					else
					{
						CoreEvents.Print("SplitBrush:  Split not on both sides\n");
					}
				}
				
				if(resultBrushes[0] != null)
				{
					front	=new GBSPBrush(this);
				}
				if(resultBrushes[1] != null)
				{
					back	=new GBSPBrush(this);
				}
				return;
			}

			for(int i=0;i < 2;i++)
			{
				GBSPSide	newSide	=new GBSPSide();

				GBSPBrush	dupe	=new GBSPBrush(resultBrushes[i]);

				newSide.mPlaneNum	=planeNum;
				newSide.mbFlipSide	=bFlipSide;

				if(bVisible)
				{
					newSide.mFlags	|=GBSPSide.SIDE_VISIBLE;
				}

				newSide.mFlags	&=~GBSPSide.SIDE_TESTED;
				newSide.mFlags	|=splitFaceFlags;
			
				if(i == 0)
				{
					newSide.mbFlipSide	=!newSide.mbFlipSide;
					newSide.mPoly		=new GBSPPoly(midPoly);
					newSide.mPoly.Reverse();
				}
				else
				{
					newSide.mPoly		=new GBSPPoly(midPoly);
				}

				resultBrushes[i].mSides.Add(newSide);
				if(!resultBrushes[i].CheckBrush(pool))
				{
					CoreEvents.Print("SplitBrush:  Result brush failed CheckBrush() after inserting new side\n");
					resultBrushes[i]	=null;
				}
			}

			{
				float	v1;
				for(int z=0;z < 2;z++)
				{
					if(resultBrushes[z] == null)
					{
						continue;
					}

					v1	=resultBrushes[z].Volume(pool);
					if(v1 < 1.0f)
					{
						resultBrushes[z]	=null;
					}
				}
			}

			if(bVerbose && (resultBrushes[0] == null || resultBrushes[1] == null))
			{
				CoreEvents.Print("SplitBrush:  Brush was not split for plane " + planeNum + "\n");
			}
			
			front	=resultBrushes[0];
			back	=resultBrushes[1];
		}


		//result list == a - b
		static List<GBSPBrush> Subtract(GBSPBrush a, GBSPBrush b, PlanePool pool, ClipPools cp)
		{
			List<GBSPBrush>	outside	=new List<GBSPBrush>();

			GBSPBrush	inside	=a;	//Default a being inside b

			//Splitting the inside list against each plane of brush b,
			//only keeping pieces that fall on the outside
			for(int i=0;i < b.mSides.Count && inside != null;i++)
			{
				GBSPBrush	front, back;

				inside.Split(b.mSides[i].mPlaneNum,	b.mSides[i].mbFlipSide, 0,
					false, pool, out front, out back, true, cp);

				//Make sure we don't free a, but free all other fragments
				if(inside != a)
				{
					inside	=null;
				}

				//Keep all front sides, and put them in the Outside list
				if(front != null)
				{
					outside.Add(front);
				}

				inside	=back;
			}

			if(inside == null)
			{
				outside.Clear();
				outside.Add(a);
				return	outside;	//Nothing on inside list, so cancel all cuts, and return original
			}
			
			inside	=null;	//Free all inside fragments

			return	outside;	//Return what was on the outside
		}
		#endregion
	}
}
