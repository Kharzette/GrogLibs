using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using SharpDX;
using UtilityLib;


namespace BSPCore
{
	internal partial class GBSPBrush
	{
		#region Stats & Gets & Tests
		internal static void TestBrushListValid(List<GBSPBrush> list, PlanePool pp)
		{
			foreach(GBSPBrush b in list)
			{
				if(!b.CheckBrush(pp))
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
				return	0;	//nothing, empty space but not the same as contents empty
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


		static internal List<GBSPBrush> ConvertMapBrushList(List<MapBrush> list, PlanePool pp)
		{
			List<GBSPBrush>	ret	=new List<GBSPBrush>();
			foreach(MapBrush b in list)
			{
				GBSPBrush	gb	=new GBSPBrush(b, pp);

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


		//this is a mix of the Q2 algo and the Genesis algo: finds
		//the best brush side to use as a bsp splitting plane
		internal static GBSPSide SelectSplitSide(BuildStats bs, List<GBSPBrush> list,
												 GBSPNode node, PlanePool pool, ClipPools cp)
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
						bool	planeSide	=side.mbFlipSide;

						Debug.Assert(node.CheckPlaneAgainstParents(planeNum) == true);

						if(!node.CheckPlaneAgainstVolume(planeNum, pool, cp))
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
		#endregion


		#region Carving Operations
		//takes a list of brushes and returns the brushes inside
		//the passed in bounds.  If some are on the border they
		//are lopped off at the boundary
		static internal List<GBSPBrush> BlockChopBrushes(List<GBSPBrush> list,
			Bounds block, PlanePool pp, ClipPools cp)
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

				GBSPBrush	boxedCopy	=b.ChopToBoxAndClone(blockPlanes, pp, cp);
				if(boxedCopy == null)
				{
					continue;
				}

				boxedCopy.BoundBrush();

				if(!boxedCopy.CheckBrush(pp))
				{
					CoreEvents.Print("Boxed copy failed checkbrush!\n");
				}

				ret.Add(boxedCopy);
			}
			return	ret;
		}


		internal static void SplitBrushList(List<GBSPBrush> list, Int32 nodePlaneNum,
			PlanePool pool,	out List<GBSPBrush> front, out List<GBSPBrush> back, ClipPools cp)
		{
			front	=new List<GBSPBrush>();
			back	=new List<GBSPBrush>();

			List<GBSPBrush>	bad	=new List<GBSPBrush>();

			foreach(GBSPBrush b in list)
			{
				UInt32	sideFlag	=b.mSide;
				if(sideFlag == GBSPPlane.PSIDE_BOTH)
				{
					GBSPBrush	newFront, newBack;
					b.Split(nodePlaneNum, false, (byte)GBSPSide.SIDE_NODE, false,
						pool, out newFront, out newBack, true, cp);
					if(newFront != null)
					{
						if(!newFront.CheckBrush(pool))
						{
							if(!bad.Contains(b))
							{
								bad.Add(b);
							}
						}
						else
						{
							front.Add(newFront);
						}
					}
					if(newBack != null)
					{
						if(!newBack.CheckBrush(pool))
						{
							if(!bad.Contains(b))
							{
								bad.Add(b);
							}
						}
						else
						{
							back.Add(newBack);
						}
					}
					continue;
				}

				GBSPBrush	newBrush	=new GBSPBrush(b);

				if(!newBrush.CheckBrush(pool))
				{
					if(!bad.Contains(b))
					{
						bad.Add(b);
					}
					continue;
				}

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

			if(bad.Count > 0)
			{
				CoreEvents.Print("" + bad.Count + " bad brushes encountered in SplitBrushList()\n");
				//uncomment the stuff below to narrow down the problem
//				DumpBrushListToFile(bad, pool, "badStuff.map");

//				GBSPBrush	testFront, testBack;
//				bad[0].Split(nodePlaneNum, false, (byte)GBSPSide.SIDE_NODE, false, pool, out testFront, out testBack, false, cp);
			}
		}


		//returns a list with no parts overlapping
		internal static List<GBSPBrush> GrabDetails(List<GBSPBrush> list)
		{
			List<GBSPBrush>	ret	=new List<GBSPBrush>();

			foreach(GBSPBrush b in list)
			{
				if(Misc.bFlagSet(b.mOriginal.mContents, Contents.BSP_CONTENTS_DETAIL2))
				{
					ret.Add(b);
				}
			}

			foreach(GBSPBrush b in ret)
			{
				list.Remove(b);
			}
			return	ret;
		}


		//returns a list with no parts overlapping
		internal static List<GBSPBrush> GankBrushOverlap(bool bVerbose,
			List<GBSPBrush> list, PlanePool pool, ClipPools cp)
		{
			List<GBSPBrush>	keep		=new List<GBSPBrush>();
			List<GBSPBrush>	subResult1	=new List<GBSPBrush>();
			List<GBSPBrush>	subResult2	=new List<GBSPBrush>();

			if(bVerbose)
			{
				CoreEvents.Print("---- GankBrushOverlap ----\n");
				CoreEvents.Print("Num brushes before gankery\t: " + list.Count + "\n");
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
						subResult1	=Subtract(b1, b2, pool, cp);

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
						subResult2	=Subtract(b2, b1, pool, cp);

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
							subResult1	=Subtract(b1, b2, pool, cp);

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
				CoreEvents.Print("Num brushes after gankery\t: " + keep.Count + "\n");
			}

			return	keep;
		}
		#endregion


		#region Debug Stuff
		//handy for debuggerizing
		static internal void DumpBrushListToFile(List<GBSPBrush> brushList, PlanePool pool, string fileName)
		{
			FileStream		fs	=new FileStream(fileName, FileMode.Create, FileAccess.Write);
			StreamWriter	sw	=new StreamWriter(fs);

			sw.WriteLine("{");
			sw.WriteLine("\"classname\"	\"worldspawn\"");
			foreach(GBSPBrush b in brushList)
			{
				sw.WriteLine("{");

				for(int i=0;i < b.mSides.Count;i++)
				{
					GBSPPlane	sidePlane	=pool.mPlanes[b.mSides[i].mPlaneNum];
					if(b.mSides[i].mbFlipSide)
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
						planePoly.mVerts[2].Y + " ) BOGUS 0 0 0 1 1 0 128 0");
				}
				sw.WriteLine("}");
			}
			sw.WriteLine("}");
			sw.Close();
			fs.Close();
		}


		//debug function to dump a brush file of stuff that is poking
		//into each other, good for tracking down problems
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
		#endregion
	}
}
