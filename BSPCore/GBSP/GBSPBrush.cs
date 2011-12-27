using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using Microsoft.Xna.Framework;


namespace BSPCore
{
	internal class GBSPBrush
	{
		Bounds		mBounds	=new Bounds();
		UInt32		mSide, mTestSide;
		MapBrush	mOriginal;

		List<GBSPSide>	mSides	=new List<GBSPSide>();


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


		internal GBSPBrush(MapBrush mb)
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

			if(!CheckBrush())
			{
				CoreEvents.Print("MakeBSPBrushes:  Bad brush.\n");
				return;
			}
		}


		bool CheckBrush()
		{
			if(mSides.Count < 3)
			{
				return	false;
			}

			if(mBounds.IsMaxExtents())
			{
				return	false;
			}
			return	true;
		}


		static internal List<GBSPBrush> BlockChopBrushes(List<GBSPBrush> list, Bounds block, PlanePool pp)
		{
			List<GBSPBrush>	ret	=new List<GBSPBrush>();

			int		[]blockPlanes;
			block.GetPlanes(pp, out blockPlanes);

			foreach(GBSPBrush b in list)
			{
				b.BoundBrush();

				if(!b.mBounds.Overlaps(block))
				{
					continue;
				}

				GBSPBrush	boxedCopy	=b.ChopToBoxAndClone(blockPlanes, pp);
				if(boxedCopy == null)
				{
					continue;
				}

				boxedCopy.BoundBrush();

				if(!boxedCopy.CheckBrush())
				{
					int	gack	=0;
					gack++;
				}

				ret.Add(boxedCopy);
			}
			return	ret;
		}


		internal static void TestBrushListValid(List<GBSPBrush> list)
		{
			foreach(GBSPBrush b in list)
			{
				if(!b.CheckBrush())
				{
					CoreEvents.Print("Bad brush in list!\n");
				}
			}
		}


		internal static bool TestListInBounds(List<GBSPBrush> list, Bounds bound)
		{
			foreach(GBSPBrush b in list)
			{
				foreach(GBSPSide s in b.mSides)
				{
					if(s.mPoly.mVerts == null)
					{
						continue;
					}
					foreach(Vector3 v in s.mPoly.mVerts)
					{
						if(!bound.IsPointInbounds(v, UtilityLib.Mathery.ON_EPSILON))
						{
							return	false;
						}
					}
				}
			}
			return	true;
		}


		internal static int GetOriginalEntityNum(List<GBSPBrush> list)
		{
			foreach(GBSPBrush b in list)
			{
				if(b.mOriginal.mEntityNum != 0)
				{
					return	b.mOriginal.mEntityNum;
				}
			}
			return	-1;
		}


		static internal void GetOriginalSidesByContents(List<GBSPBrush> list, UInt32 contents, List<GBSPSide> sides)
		{
			foreach(GBSPBrush b in list)
			{
				MapBrush	mb	=b.mOriginal;

				//filter by contents
				if((mb.mContents & contents) == 0)
				{
					continue;
				}

				foreach(GBSPSide side in mb.mOriginalSides)
				{
					sides.Add(side);
				}
			}
		}


		static internal void FreeSidePolys(List<GBSPBrush> list)
		{
			if(list == null)
			{
				return;
			}

			foreach(GBSPBrush b in list)
			{
				foreach(GBSPSide side in b.mSides)
				{
					if(side.mPoly != null)
					{
						side.mPoly.Free();
						side.mPoly	=null;
					}
				}
			}
		}


		//Get the contents of this leaf, by examining all the brushes that made this leaf
		static internal UInt32 GetLeafContents(List<GBSPBrush> list)
		{
			if(list == null)
			{
				return	Contents.BSP_CONTENTS_SOLID2;
			}

			if(list.Count == 0)
			{
				return	Contents.BSP_CONTENTS_EMPTY2;
			}

			UInt32	ret	=0;

			foreach(GBSPBrush b in list)
			{
				if((b.mOriginal.mContents & Contents.BSP_CONTENTS_SOLID2) != 0)
				{
					int	i=0;
					for(i=0;i < b.mSides.Count;i++)
					{
						if((b.mSides[i].mFlags & GBSPSide.SIDE_NODE) == 0)
						{
							break;
						}
					}
				
					//If all the planes in this leaf where caused by splits, then
					//we can force this leaf to be solid...
					if(i == b.mSides.Count)
					{
						ret	|=Contents.BSP_CONTENTS_SOLID2;
					}					
				}
				
				ret	|=b.mOriginal.mContents;
			}
			return	ret;
		}


		static internal void BrushListStats(List<GBSPBrush> list, BuildStats bs, Bounds bounds, PlanePool pool)
		{
			foreach(GBSPBrush b in list)
			{
				bs.NumVisBrushes++;
				
				if(b.Volume(pool) < 0.1f)
				{
					CoreEvents.Print("**WARNING** BuildBSP: Brush with NULL volume\n");
				}
				
				for(int i=0;i < b.mSides.Count;i++)
				{
					if(b.mSides[i].mPoly.VertCount() < 3)
					{
						continue;
					}
					if((b.mSides[i].mFlags & GBSPSide.SIDE_NODE) != 0)
					{
						continue;
					}
					if((b.mSides[i].mFlags & GBSPSide.SIDE_VISIBLE) != 0)
					{
						bs.NumVisFaces++;
					}
					else
					{
						bs.NumNonVisFaces++;
					}
				}
				bounds.Merge(b.mBounds, null);
			}
		}


		static internal List<GBSPBrush> ConvertMapBrushList(List<MapBrush> list)
		{
			List<GBSPBrush>	ret	=new List<GBSPBrush>();
			foreach(MapBrush b in list)
			{
				GBSPBrush	gb	=new GBSPBrush(b);

				//if brush is being dropped, the mOriginal
				//reference will be null
				if(gb.mOriginal == null)
				{
					continue;
				}
				ret.Add(gb);
			}
			return	ret;
		}


		GBSPBrush	ChopToBoxAndClone(int []planes, PlanePool pp)
		{
			GBSPBrush	ret	=new GBSPBrush(this);
			for(int i=0;i < 6;i++)
			{
				GBSPBrush	front, back;
				if(i < 3)
				{
					ret.Split(planes[i], 0, 0, false, pp, out front, out back, false);
				}
				else
				{
					ret.Split(planes[i], 1, 0, false, pp, out front, out back, false);
				}
				if(back == null)
				{
					return	null;
				}
				ret	=back;
			}

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


		void BoundBrush()
		{
			mBounds.Clear();

			for(int i=0;i < mSides.Count;i++)
			{
				mSides[i].AddToBounds(mBounds);
			}
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
						mSides[i].mPlaneSide != otherBrush.mSides[j].mPlaneSide)
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

				if(s.mPlaneSide != 0)
				{
					p.Inverse();
				}

				float	dist	=p.DistanceFast(point);
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


		internal void Split(Int32 planeNum, sbyte planeSide, byte splitFaceFlags, bool bVisible,
					PlanePool pool, out GBSPBrush front, out GBSPBrush back, bool bVerbose)
		{
			GBSPPlane	poolPlane, sidedPlane;
			float		frontDist, backDist;
			GBSPBrush	[]resultBrushes	=new GBSPBrush[2];

			poolPlane		=pool.mPlanes[planeNum];
			poolPlane.mType	=GBSPPlane.PLANE_ANY;

			sidedPlane	=poolPlane;
			if(planeSide != 0)
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
				s.mPoly.GetSplitMaxDist(poolPlane, planeSide, ref frontDist, ref backDist);
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
				CoreEvents.Print("Could not create poly.\n");
			}
			
			//Clip the poly by all the planes of the brush being split
			foreach(GBSPSide s in mSides)
			{
				if(midPoly.IsTiny())
				{
					break;
				}
				GBSPPlane	plane2	=pool.mPlanes[s.mPlaneNum];
				
				midPoly.ClipPolyEpsilon(0.0f, plane2, s.mPlaneSide == 0);
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
				
				if(s.mPoly == null)
				{
					continue;
				}

				GBSPPoly	sidePoly	=new GBSPPoly(s.mPoly);
				if(!sidePoly.SplitEpsilon(0.0f, sidedPlane, out poly[0], out poly[1], false) && bVerbose)
				{
					CoreEvents.Print("Error splitting poly...\n");
				}

				for(int j=0;j < 2;j++)
				{
					GBSPSide	destSide;

					if(poly[j] == null)
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

				if(!resultBrushes[i].CheckBrush())
				{
					resultBrushes[i]	=null;
				}			
			}

			if(resultBrushes[0] == null || resultBrushes[1] == null)
			{
				if(bVerbose)
				{
					if(resultBrushes[0] == null && resultBrushes[1] == null)
					{
						CoreEvents.Print("Split removed brush\n");
					}
					else
					{
						CoreEvents.Print("Split not on both sides\n");
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

				resultBrushes[i].mSides.Add(newSide);

				newSide.mPlaneNum	=planeNum;
				newSide.mPlaneSide	=(sbyte)planeSide;

				if(bVisible)
				{
					newSide.mFlags	|=GBSPSide.SIDE_VISIBLE;
				}

				newSide.mFlags	&=~GBSPSide.SIDE_TESTED;
				newSide.mFlags	|=splitFaceFlags;
			
				if(i == 0)
				{
					newSide.mPlaneSide	=(newSide.mPlaneSide == 0)? (sbyte)1 : (sbyte)0;
					newSide.mPoly		=new GBSPPoly(midPoly);
					newSide.mPoly.Reverse();
				}
				else
				{
					newSide.mPoly		=new GBSPPoly(midPoly);
				}
			}

			{
				float	v1;
				for(int z=0;z < 2;z++)
				{
					v1	=resultBrushes[z].Volume(pool);
					if(v1 < 1.0f)
					{
						resultBrushes[z]	=null;
						//GHook.Printf("Tiny volume after clip\n");
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

				if(s.mPlaneSide != 0)
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


		static List<GBSPBrush> Subtract(GBSPBrush a, GBSPBrush b, PlanePool pool)
		{
			List<GBSPBrush>	outside	=new List<GBSPBrush>();

			GBSPBrush	inside	=a;	// Default a being inside b

			//Splitting the inside list against each plane of brush b, only keeping peices that fall on the
			//outside
			for(int i=0;i < b.mSides.Count && inside != null;i++)
			{
				GBSPBrush	front, back;

				inside.Split(b.mSides[i].mPlaneNum,	b.mSides[i].mPlaneSide, (byte)GBSPSide.SIDE_NODE,
					false, pool, out front, out back, true);

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


		static int oCount;
		internal static void DumpOverlapping(List<GBSPBrush> list, PlanePool pp)
		{
			List<GBSPBrush>	overlapping	=new List<GBSPBrush>();

			foreach(GBSPBrush b1 in list)
			{
				foreach(GBSPBrush b2 in list)
				{
					if(b1 == b2)
					{
						continue;
					}
					if(!b1.Overlaps(b2))
					{
						continue;
					}

					if(!b1.ReallyOverlaps(b2, pp))
					{
						continue;
					}

					if(!overlapping.Contains(b1))
					{
						overlapping.Add(b1);
					}
					if(!overlapping.Contains(b2))
					{
						overlapping.Add(b2);
					}
				}
			}
			if(overlapping.Count > 0)
			{				
				DumpBrushListToFile(overlapping, pp, "Overlapped" + oCount++ + ".map");
			}
		}


		internal static List<GBSPBrush> CSGBrushes(bool bVerbose, List<GBSPBrush> list, PlanePool pool)
		{
			List<GBSPBrush>	keep		=new List<GBSPBrush>();
			List<GBSPBrush>	subResult1	=new List<GBSPBrush>();
			List<GBSPBrush>	subResult2	=new List<GBSPBrush>();

			if(bVerbose)
			{
				CoreEvents.Print("---- CSGBrushes ----\n");
				CoreEvents.Print("Num brushes before CSG : " + list.Count + "\n");
			}

		startOver:
			keep.Clear();

			foreach(GBSPBrush b1 in list)
			{
				foreach(GBSPBrush b2 in list)
				{
					if(b1 == b2)
					{
						continue;
					}

					if(!b1.Overlaps(b2))
					{
						continue;
					}

					subResult1.Clear();
					subResult2.Clear();
					Int32	c1	=999999;
					Int32	c2	=999999;

					if(b2.BrushCanBite(b1))
					{
						subResult1	=Subtract(b1, b2, pool);

						if(subResult1.Contains(b1))
						{
							continue;
						}

						if(subResult1.Count == 0)
						{
							list.Remove(b1);
							goto	startOver;
						}
						c1	=subResult1.Count;
					}

					if(b1.BrushCanBite(b2))
					{
						subResult2	=Subtract(b2, b1, pool);

						if(subResult2.Contains(b2))
						{
							continue;
						}

						if(subResult2.Count == 0)
						{	
							subResult1.Clear();
							list.Remove(b2);
							goto	startOver;
						}
						c2	=subResult2.Count;
					}

					//check for non solid stuff poking into each other
					//like water or something
					if(!b1.BrushCanBite(b2) && !b2.BrushCanBite(b1))
					{
						if(GBSPBrush.SameContents(b1, b2))
						{
							subResult1	=Subtract(b1, b2, pool);

							if(subResult1.Contains(b1))
							{
								continue;
							}

							if(subResult1.Count == 0)
							{
								list.Remove(b1);
								goto	startOver;
							}
							c1	=subResult1.Count;
						}
					}

					if(subResult1.Count == 0 && subResult2.Count == 0)
					{
						continue;
					}

					if(c1 < c2)
					{
						if(subResult2.Count > 0)
						{
							subResult2.Clear();
						}
						list.AddRange(subResult1);
						list.Remove(b1);
						goto	startOver;
					}
					else
					{
						if(subResult1.Count == 0)
						{
							subResult1.Clear();
						}
						list.AddRange(subResult2);
						list.Remove(b2);
						goto	startOver;
					}
				}
				keep.Add(b1);
			}

			if(bVerbose)
			{
				CoreEvents.Print("Num brushes after CSG  : " + keep.Count + "\n");
			}

			return	keep;
		}


		internal static GBSPSide SelectSplitSide(BuildStats bs, List<GBSPBrush> list,
												 GBSPNode node, PlanePool pool)
		{
			GBSPSide	bestSide	=null;
			Int32		bestValue	=-999999;
			Int32		bestSplits	=0;
			Int32		numPasses	=4;
			for(Int32 pass = 0;pass < numPasses;pass++)
			{
				foreach(GBSPBrush b in list)
				{
					if(((pass & 1) != 0)
						&& ((b.mOriginal.mContents & Contents.BSP_CONTENTS_DETAIL2) == 0))
					{
						continue;
					}
					if(((pass & 1) == 0)
						&& ((b.mOriginal.mContents & Contents.BSP_CONTENTS_DETAIL2) != 0))
					{
						continue;
					}
					
					bool	bHintSplit	=false;
					foreach(GBSPSide side in b.mSides)
					{
						if(side.mPoly == null)
						{
							continue;
						}
						if((side.mFlags & (GBSPSide.SIDE_TESTED)) != 0)
						{
							continue;
						}
						if((side.mFlags & (GBSPSide.SIDE_NODE)) != 0)
						{
							continue;
						}
 						if(((side.mFlags & GBSPSide.SIDE_VISIBLE) == 0) && pass < 2)
						{
							continue;
						}
 						if(((side.mFlags & GBSPSide.SIDE_VISIBLE) != 0) && pass >= 2)
						{
							continue;
						}

						Int32	planeNum	=side.mPlaneNum;
						Int32	planeSide	=side.mPlaneSide;

						Debug.Assert(node.CheckPlaneAgainstParents(planeNum) == true);

						if(!node.CheckPlaneAgainstVolume(planeNum, pool))
						{
							continue;	//borrowing a volume check from Q2
						}
												
						Int32	frontCount		=0;
						Int32	backCount		=0;
						Int32	bothCount		=0;
						Int32	facing			=0;
						Int32	splits			=0;
						Int32	EpsilonBrush	=0;

						foreach(GBSPBrush test in list)
						{
							Int32	brushSplits;
							UInt32	sideFlag	=test.TestBrushToPlane(planeNum, planeSide, pool,
								out brushSplits, out bHintSplit, ref EpsilonBrush);

							splits		+=brushSplits;

							if(brushSplits != 0 && ((sideFlag & GBSPPlane.PSIDE_FACING) != 0))
							{
								CoreEvents.Print("PSIDE_FACING with splits\n");
							}

							test.mTestSide	=sideFlag;

							if((sideFlag & GBSPPlane.PSIDE_FACING) != 0)
							{
								facing++;
								foreach(GBSPSide testSide in test.mSides)
								{
									if(testSide.mPlaneNum == planeNum)
									{
										testSide.mFlags	|=GBSPSide.SIDE_TESTED;
									}
								}
							}
							if((sideFlag & GBSPPlane.PSIDE_FRONT) != 0)
							{
								frontCount++;
							}
							if((sideFlag & GBSPPlane.PSIDE_BACK) != 0)
							{
								backCount++;
							}
							if (sideFlag == GBSPPlane.PSIDE_BOTH)
							{
								bothCount++;
							}
						}

						Int32	value	=5 * facing - 5 * splits - Math.Abs(frontCount - backCount);

						if(pool.mPlanes[planeNum].mType < 3)
						{
							value	+=5;
						}
						
						value	-=EpsilonBrush * 1000;

						if(bHintSplit && ((side.mFlags & GBSPSide.SIDE_HINT) == 0))
						{
							value	=-999999;
						}

						if(value > bestValue)
						{
							bestValue	=value;
							bestSide	=side;
							bestSplits	=splits;
							foreach(GBSPBrush t in list)
							{
								t.mSide	=t.mTestSide;
							}
						}
					}
				}

				if(bestSide != null)
				{
					if(pass > 1)
					{
						bs.NumNonVisNodes++;
					}
					
					if(pass > 0)
					{
						node.SetDetail(true);	//Not needed for vis
						if((bestSide.mFlags & GBSPSide.SIDE_HINT) != 0)
						{
							CoreEvents.Print("*** Hint as Detail!!! ***\n");
						}
					}
					break;
				}
			}

			foreach(GBSPBrush b in list)
			{
				foreach(GBSPSide s in b.mSides)
				{
					s.mFlags	&=~GBSPSide.SIDE_TESTED;
				}
			}

			return	bestSide;
		}


		UInt32 TestBrushToPlane(int planeNum, int planeSide, PlanePool pool,
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
				
				if(Num == planeNum && s.mPlaneSide == 0)
				{
					return	GBSPPlane.PSIDE_BACK | GBSPPlane.PSIDE_FACING;
				}

				if(Num == planeNum && s.mPlaneSide != 0)
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


		internal static void SplitBrushList(List<GBSPBrush> list, Int32 nodePlaneNum,
			PlanePool pool,	out List<GBSPBrush> front, out List<GBSPBrush> back)
		{
			front	=new List<GBSPBrush>();
			back	=new List<GBSPBrush>();

			foreach(GBSPBrush b in list)
			{
				UInt32	sideFlag	=b.mSide;
				if(sideFlag == GBSPPlane.PSIDE_BOTH)
				{
					GBSPBrush	newFront, newBack;
					b.Split(nodePlaneNum, 0, (byte)GBSPSide.SIDE_NODE,
						false, pool, out newFront, out newBack, true);
					if(newFront != null)
					{
						front.Add(newFront);
					}
					if(newBack != null)
					{
						back.Add(newBack);
					}
					continue;
				}

				GBSPBrush	newBrush	=new GBSPBrush(b);

				if((sideFlag & GBSPPlane.PSIDE_FACING) != 0)
				{
					foreach(GBSPSide newSide in newBrush.mSides)
					{
						if(newSide.mPlaneNum == nodePlaneNum)
						{
							newSide.mFlags	|=GBSPSide.SIDE_NODE;
						}
					}
				}

				if((sideFlag & GBSPPlane.PSIDE_FRONT) != 0)
				{
					front.Add(newBrush);
					continue;
				}
				if((sideFlag & GBSPPlane.PSIDE_BACK) != 0)
				{
					back.Add(newBrush);
					continue;
				}
			}
		}


		//handy for debuggerizing
		static internal void DumpBrushListToFile(List<GBSPBrush> brushList, PlanePool pool, string fileName)
		{
			FileStream		fs	=new FileStream(fileName, FileMode.Create, FileAccess.Write);
			StreamWriter	sw	=new StreamWriter(fs);

			sw.WriteLine("{");
			sw.WriteLine("\"sounds\"	\"10\"");
			sw.WriteLine("\"wad\"	\"gfx/metal.wad\"");
			sw.WriteLine("\"classname\"	\"worldspawn\"");
			sw.WriteLine("\"message\"	\"the Wind Tunnels\"");
			sw.WriteLine("\"worldtype\"	\"1\"");
			foreach(GBSPBrush b in brushList)
			{
				sw.WriteLine("{");

				for(int i=0;i < b.mSides.Count;i++)
				{
					GBSPPlane	sidePlane	=pool.mPlanes[b.mSides[i].mPlaneNum];
					if(b.mSides[i].mPlaneSide != 0)
					{
						sidePlane.Inverse();
					}
					GBSPPoly	planePoly	=new GBSPPoly(sidePlane);

					sw.WriteLine("( " +
						-planePoly.mVerts[0].X + " " +
						planePoly.mVerts[0].Z + " " +
						planePoly.mVerts[0].Y + " ) ( " +
						-planePoly.mVerts[1].X + " " +
						planePoly.mVerts[1].Z + " " +
						planePoly.mVerts[1].Y + " ) ( " +
						-planePoly.mVerts[2].X + " " +
						planePoly.mVerts[2].Z + " " +
						planePoly.mVerts[2].Y + " ) BOGUS 0 0 0 1.0 1.0");
				}
				sw.WriteLine("}");
			}
			sw.WriteLine("}");
			sw.Close();
			fs.Close();
		}
	}
}
