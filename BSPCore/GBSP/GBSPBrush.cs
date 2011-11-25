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
		GBSPBrush	mNext;
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


		static internal GBSPBrush BlockChopBrushes(GBSPBrush listHead, Bounds block, PlanePool pp)
		{
			GBSPBrush	ret	=null;

			int		[]blockPlanes;
			block.GetPlanes(pp, out blockPlanes);

			for(GBSPBrush b = listHead;b != null;b = b.mNext)
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

				if(ret == null)
				{
					ret	=boxedCopy;
				}
				else
				{
					boxedCopy.mNext	=ret.mNext;
					ret.mNext		=boxedCopy;
				}
			}
			return	ret;
		}


		internal static bool TestListInBounds(GBSPBrush listHead, Bounds bound)
		{
			for(GBSPBrush b=listHead;b != null;b=b.mNext)
			{
				foreach(GBSPSide s in b.mSides)
				{
					foreach(Vector3 v in s.mPoly.mVerts)
					{
						if(!bound.IsPointInbounds(v))
						{
							return	false;
						}
					}
				}
			}
			return	true;
		}


		internal static int GetOriginalEntityNum(GBSPBrush listHead)
		{
			for(GBSPBrush b=listHead;b != null;b=b.mNext)
			{
				if(b.mOriginal.mEntityNum != 0)
				{
					return	b.mOriginal.mEntityNum;
				}
			}
			return	-1;
		}


		static internal void GetOriginalSidesByContents(GBSPBrush listHead, UInt32 contents, List<GBSPSide> sides)
		{
			for(GBSPBrush b=listHead;b != null;b=b.mNext)
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


		static internal void FreeSidePolys(GBSPBrush listHead)
		{
			for(GBSPBrush b=listHead;b != null;b=b.mNext)
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
		static internal UInt32 GetLeafContents(GBSPBrush listHead)
		{
			UInt32	ret	=0;

			for(GBSPBrush b=listHead;b != null;b=b.mNext)
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


		static internal void BrushListStats(GBSPBrush listHead, BuildStats bs, Bounds bounds, PlanePool pool)
		{
			for(GBSPBrush b=listHead;b != null;b=b.mNext)
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


		static internal GBSPBrush ConvertMapBrushList(List<MapBrush> list)
		{
			GBSPBrush	ret		=null;
			GBSPBrush	prev	=null;
			foreach(MapBrush b in list)
			{
				GBSPBrush	gb	=new GBSPBrush(b);

				//if brush is being dropped, the mOriginal
				//reference will be null
				if(gb.mOriginal == null)
				{
					continue;
				}

				if(prev != null)
				{
					prev.mNext	=gb;
				}

				if(ret == null)
				{
					ret	=gb;
				}
				prev	=gb;
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
					if(back == null)
					{
						return	null;
					}
					ret	=back;
				}
				else
				{
					ret.Split(planes[i], 1, 0, false, pp, out front, out back, false);
					if(front == null)
					{
						return	null;
					}
					ret	=front;
				}
			}

			for(int i=0;i < ret.mSides.Count;i++)
			{
				int	pnum	=ret.mSides[i].mPlaneNum;
				if(pnum == planes[0] || pnum == planes[2]
					|| pnum == planes[3] || pnum == planes[5])
				{
					ret.mSides[i].mTexInfo	=-1;
					ret.mSides[i].mFlags	=GBSPSide.SIDE_NODE;
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


		bool PointInside(Vector3 point, PlanePool pp)
		{
			foreach(GBSPSide s in mSides)
			{
				GBSPPlane	p	=pp.mPlanes[s.mPlaneNum];

				if(s.mPlaneSide != 0)
				{
					p.Inverse();
				}

				float	dist	=p.DistanceFast(point);
				if(dist < 0.0f)
				{
					return	true;
				}
			}
			return	false;
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
				for(int i=0;i < otherBrush.mSides[i].mPoly.mVerts.Length;i++)
				{
					if(PointInside(otherBrush.mSides[i].mPoly.mVerts[i], pp))
					{
						return	true;
					}
				}
			}

			return	true;
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
					PlanePool pool, out GBSPBrush front, out GBSPBrush back, bool bVerbose, out bool bTinySplit)
		{
			GBSPPlane	plane, plane2;
			float		frontDist, backDist;
			GBSPBrush	[]resultBrushes	=new GBSPBrush[2];

			bTinySplit	=false;

			plane		=pool.mPlanes[planeNum];
			plane.mType	=GBSPPlane.PLANE_ANY;

			if(planeSide != 0)
			{
				plane.Inverse();
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
				s.mPoly.GetSplitMaxDist(plane, planeSide, ref frontDist, ref backDist);
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
			GBSPPoly	midPoly	=new GBSPPoly(plane);
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
				plane2	=pool.mPlanes[s.mPlaneNum];
				
				midPoly.ClipPolyEpsilon(0.0f, plane2, s.mPlaneSide == 0);
			}

			if(midPoly.IsTiny())
			{	
				UInt32	Side	=MostlyOnSide(plane);
				
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
				if(!sidePoly.SplitEpsilon(0.0f, plane, out poly[0], out poly[1], false) && bVerbose)
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
						bTinySplit			=true;
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


		static GBSPBrush Subtract(GBSPBrush a, GBSPBrush b, PlanePool pool)
		{
			GBSPBrush	outside, inside;
			GBSPBrush	front, back;

			inside	=a;	// Default a being inside b
			outside	=null;

			//Splitting the inside list against each plane of brush b, only keeping peices that fall on the
			//outside
			for(int i=0;i < b.mSides.Count && inside != null;i++)
			{
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
					front.mNext	=outside;
					outside		=front;
				}

				inside	=back;
			}

			if(inside == null)
			{
				FreeBrushList(outside);		
				return	a;	//Nothing on inside list, so cancel all cuts, and return original
			}
			
			inside	=null;	//Free all inside fragments

			return	outside;	//Return what was on the outside
		}


		internal static void DumpOverlapping(GBSPBrush listHead, PlanePool pp)
		{
			List<GBSPBrush>	list	=BrushListToList(listHead);
			DumpOverlapping(list, pp);
		}


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
				DumpBrushListToFile(overlapping, "Overlapped" + gack++ + ".map");
			}
		}


		static int	gack	=0;
		internal static GBSPBrush CSGBrushes(bool bVerbose, GBSPBrush listHead, PlanePool pool)
		{
			GBSPBrush	listTail;
			GBSPBrush	listKeep;

			if(bVerbose)
			{
				CoreEvents.Print("---- CSGBrushes ----\n");
				CoreEvents.Print("Num brushes before CSG : " + CountBrushList(listHead) + "\n");
			}

			listKeep	=null;

		NewList:

			if(listHead == null)
			{
				return null;
			}

			//advance tail to end
			for(listTail=listHead;listTail.mNext != null;listTail=listTail.mNext);

			GBSPBrush	next;
			for(GBSPBrush b1=listHead;b1 != null;b1=next)
			{
				next	=b1.mNext;
				GBSPBrush	b2	=null;

				for(b2=b1.mNext;b2 != null;b2 = b2.mNext)
				{
					if(!b1.Overlaps(b2))
					{
						continue;
					}
/*
					if(UtilityLib.Mathery.CompareVector(b1.mOriginal.Center, new Vector3(-1004, -40, 40)))
					{
						gack++;

						GBSPBrush	b1Copy	=new GBSPBrush(b1);
						GBSPBrush	b2Copy	=new GBSPBrush(b2);

						b1Copy.mNext	=b2Copy;
						b2Copy.mNext	=null;

						DumpBrushListToFile(b1Copy, "Overlapping" + gack + ".map");
					}*/

					GBSPBrush	subResult	=null;
					GBSPBrush	subResult2	=null;
					Int32		c1			=999999;
					Int32		c2			=999999;

					if(b2.BrushCanBite(b1))
					{
						subResult	=Subtract(b1, b2, pool);

						if(subResult == b1)
						{
							continue;
						}

						if(subResult == null)
						{
							listHead	=RemoveBrushList(b1, b1);
							goto	NewList;
						}
						c1	=CountBrushList(subResult);
					}

					if(b1.BrushCanBite(b2))
					{
						subResult2	=Subtract(b2, b1, pool);

						if(subResult2 == b2)
						{
							continue;
						}

						if(subResult2 == null)
						{	
							FreeBrushList(subResult);
							listHead	=RemoveBrushList(b1, b2);
							goto	NewList;
						}
						c2	=CountBrushList(subResult2);
					}

					if(subResult == null && subResult2 == null)
					{
						continue;
					}

					if(false && c1 > 4 && c2 > 4)
					{
						if(subResult2 != null)
						{
							FreeBrushList(subResult2);
						}
						if(subResult != null)
						{
							FreeBrushList(subResult);
						}
						continue;
					}					

					if(c1 < c2)
					{
						if(subResult2 != null)
						{
							FreeBrushList(subResult2);
						}
						{
							DumpBrushListToFile(subResult, "Chopped" + gack++ + ".map");
						}
						listTail	=AddBrushListToTail(subResult, listTail);
						listHead	=RemoveBrushList(b1, b1);
						goto NewList;
					}
					else
					{
						if(subResult != null)
						{
							FreeBrushList(subResult);
						}
						{
							DumpBrushListToFile(subResult2, "Chopped" + gack++ + ".map");
						}
						listTail	=AddBrushListToTail(subResult2, listTail);
						listHead	=RemoveBrushList(b1, b2);
						goto NewList;
					}
				}

				if(b2 == null)
				{	
					b1.mNext	=listKeep;
					listKeep	=b1;
				}
			}

			if(bVerbose)
			{
				CoreEvents.Print("Num brushes after CSG  : " + CountBrushList(listKeep) + "\n");
			}

			return	listKeep;
		}


		internal static List<GBSPBrush> CSGBrushes(bool bVerbose, List<GBSPBrush> list, PlanePool pool)
		{
			List<GBSPBrush>	keep	=new List<GBSPBrush>();

			if(bVerbose)
			{
				CoreEvents.Print("---- CSGBrushes ----\n");
				CoreEvents.Print("Num brushes before CSG : " + list.Count + "\n");
			}

		startOver:

			foreach(GBSPBrush b1 in list)
			{
				foreach(GBSPBrush b2 in list)
				{
					if(!b1.Overlaps(b2))
					{
						continue;
					}

					GBSPBrush	subResult	=null;
					GBSPBrush	subResult2	=null;
					Int32		c1			=999999;
					Int32		c2			=999999;

					if(b2.BrushCanBite(b1))
					{
						subResult	=Subtract(b1, b2, pool);

						if(subResult == b1)
						{
							continue;
						}

						if(subResult == null)
						{
							list.Remove(b1);
							goto	startOver;
						}
						c1	=CountBrushList(subResult);
					}

					if(b1.BrushCanBite(b2))
					{
						subResult2	=Subtract(b2, b1, pool);

						if(subResult2 == b2)
						{
							continue;
						}

						if(subResult2 == null)
						{	
							FreeBrushList(subResult);
							list.Remove(b2);
							goto	startOver;
						}
						c2	=CountBrushList(subResult2);
					}

					if(subResult == null && subResult2 == null)
					{
						continue;
					}

					if(c1 < c2)
					{
						if(subResult2 != null)
						{
							FreeBrushList(subResult2);
						}
						for(GBSPBrush piece = subResult;piece != null;piece = piece.mNext)
						{
							list.Add(piece);
						}
						list.Remove(b1);
						goto	startOver;
					}
					else
					{
						if(subResult != null)
						{
							FreeBrushList(subResult);
						}
						for(GBSPBrush piece = subResult2;piece != null;piece = piece.mNext)
						{
							list.Add(piece);
						}
						list.Remove(b2);
						goto	startOver;
					}

				}
			}

			if(bVerbose)
			{
				CoreEvents.Print("Num brushes after CSG  : " + keep.Count + "\n");
			}

			return	keep;
		}


		static GBSPBrush AddBrushListToTail(GBSPBrush list, GBSPBrush tail)
		{
			GBSPBrush	walk, next;

			for (walk=list;walk != null;walk=next)
			{	// add to end of list
				next		=walk.mNext;
				walk.mNext	=null;
				tail.mNext	=walk;
				tail		=walk;
			}
			return	tail;
		}


		static internal Int32 CountBrushList(GBSPBrush listHead)
		{
			Int32	c	=0;
			for(;listHead != null;listHead=listHead.mNext)
			{
				c++;
			}
			return	c;
		}


		static GBSPBrush RemoveBrushList(GBSPBrush listHead, GBSPBrush toRemove)
		{
			GBSPBrush	newList;
			GBSPBrush	next;

			newList	=null;

			for(;listHead != null;listHead = next)
			{
				next	=listHead.mNext;

				if(listHead == toRemove)
				{
					listHead	=null;
					continue;
				}

				listHead.mNext	=newList;
				newList			=listHead;
			}
			return	newList;
		}


		internal static GBSPSide SelectSplitSide(BuildStats bs, GBSPBrush listHead,
												 GBSPNode node, PlanePool pool)
		{
			GBSPSide	bestSide	=null;
			Int32		bestValue	=-999999;
			Int32		bestSplits	=0;
			Int32		numPasses	=4;
			for(Int32 pass = 0;pass < numPasses;pass++)
			{
				for(GBSPBrush b = listHead;b != null;b=b.mNext)
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
						double	frontVol		=0.0;
						double	backVol			=0.0;

						for(GBSPBrush test=listHead;test != null;test=test.mNext)
						{
							Int32	brushSplits;
							double	fVol, bVol;
							UInt32	sideFlag	=test.TestBrushToPlane(planeNum, planeSide, pool,
								out brushSplits, out bHintSplit, ref EpsilonBrush);
//								out fVol, out bVol);

							splits		+=brushSplits;
//							frontVol	+=fVol;
//							backVol		+=bVol;

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

//						value	-=(int)((frontVol / backVol) * 30.0);
						
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
							for(GBSPBrush t=listHead;t != null;t=t.mNext)
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
				else
				{
					int	gack	=0;
					gack++;
				}
			}

			for(GBSPBrush b = listHead;b != null;b=b.mNext)
			{
				foreach(GBSPSide s in b.mSides)
				{
					s.mFlags	&=~GBSPSide.SIDE_TESTED;
				}
			}

			return	bestSide;
		}


		UInt32	ThoroughTestBrushToPlane(int planeNum, int planeSide, PlanePool pool,
			out int numSplits, out bool bHintSplit, ref int EpsilonBrush,
			out double frontVol, out double backVol)
		{
			UInt32	ret	=TestBrushToPlane(planeNum, planeSide, pool, out numSplits, out bHintSplit, ref EpsilonBrush);

			GBSPBrush	copy	=new GBSPBrush(this);

			GBSPBrush	front, back;

			bool	bTinyVol;

			copy.Split(planeNum, (sbyte)planeSide, 0, true, pool, out front, out back, false, out bTinyVol);

			if(bTinyVol)
			{
				CoreEvents.Print("Tiny volume test plane " + planeNum + "\n");
				EpsilonBrush++;
			}

			frontVol	=0.0;
			backVol		=0.0;

			if(front != null)
			{
				frontVol	=front.Volume(pool);
			}
			if(back != null)
			{
				backVol	=back.Volume(pool);
			}

			return	ret;
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


		internal static void MergeLists(GBSPBrush mergeFrom, GBSPBrush mergeTo)
		{
			GBSPBrush	next;
			for(GBSPBrush b = mergeFrom;b != null;b=next)
			{
				next	=b.mNext;
				b.mNext	=mergeTo;
				mergeTo	=b;
			}
		}


		internal static void FreeBrushList(GBSPBrush listHead)
		{
			GBSPBrush	next;

			for(;listHead != null;listHead = next)
			{
				next	=listHead.mNext;

				listHead.mSides.Clear();
				listHead.mBounds	=null;
				listHead			=null;
			}
		}


		internal static void SplitBrushList(GBSPBrush listHead, Int32 nodePlaneNum,
			PlanePool pool,	out GBSPBrush front, out GBSPBrush back)
		{
			GBSPBrush	next;

			front = back = null;

			for(GBSPBrush b = listHead;b != null;b = next)
			{
				next	=b.mNext;

				UInt32	sideFlag	=b.mSide;
				if(sideFlag == GBSPPlane.PSIDE_BOTH)
				{
					GBSPBrush	newFront, newBack;
					bool		bt;
					b.Split(nodePlaneNum, 0, (byte)GBSPSide.SIDE_NODE,
						false, pool, out newFront, out newBack, true, out bt);
					if(newFront != null)
					{
						newFront.mNext	=front;
						front			=newFront;
					}
					if(newBack != null)
					{
						newBack.mNext	=back;
						back			=newBack;
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
					newBrush.mNext	=front;
					front			=newBrush;
					continue;
				}
				if((sideFlag & GBSPPlane.PSIDE_BACK) != 0)
				{
					newBrush.mNext	=back;
					back			=newBrush;
					continue;
				}
			}
		}


		static internal List<GBSPBrush> BrushListToList(GBSPBrush listHead)
		{
			List<GBSPBrush>	brushes	=new List<GBSPBrush>();

			for(GBSPBrush b=listHead;b != null;b=b.mNext)
			{
				brushes.Add(b);
			}
			return	brushes;
		}


		//unused, but handy for debuggerizing
		static internal void DumpBrushListToFile(GBSPBrush brushList, string fileName)
		{
			List<GBSPBrush>	brushes	=BrushListToList(brushList);
			DumpBrushListToFile(brushes, fileName);
		}


		//unused, but handy for debuggerizing
		static internal void DumpBrushListToFile(List<GBSPBrush> brushList, string fileName)
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
					sw.WriteLine("( " +
						-b.mSides[i].mPoly.mVerts[0].X + " " +
						b.mSides[i].mPoly.mVerts[0].Z + " " +
						b.mSides[i].mPoly.mVerts[0].Y + " ) ( " +
						-b.mSides[i].mPoly.mVerts[1].X + " " +
						b.mSides[i].mPoly.mVerts[1].Z + " " +
						b.mSides[i].mPoly.mVerts[1].Y + " ) ( " +
						-b.mSides[i].mPoly.mVerts[2].X + " " +
						b.mSides[i].mPoly.mVerts[2].Z + " " +
						b.mSides[i].mPoly.mVerts[2].Y + " ) BOGUS 0 0 0 1.0 1.0");
				}
				sw.WriteLine("}");
			}
			sw.WriteLine("}");
			sw.Close();
			fs.Close();
		}
	}
}
