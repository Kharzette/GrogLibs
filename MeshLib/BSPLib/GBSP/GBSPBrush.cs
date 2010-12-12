using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class GBSPBrush
	{
		GBSPBrush	mNext;
		Bounds		mBounds	=new Bounds();
		UInt32		mSide, mTestSide;
		MapBrush		mOriginal;

		List<GBSPSide>	mSides	=new List<GBSPSide>();


		public GBSPBrush() { }
		public GBSPBrush(GBSPBrush copyMe)
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


		public GBSPBrush(MapBrush mb)
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
				Map.Print("MakeBSPBrushes:  Bad brush.\n");
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
					Map.Print("**WARNING** BuildBSP: Brush with NULL volume\n");
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


		static void Split(GBSPBrush Brush, Int32 PNum, sbyte PSide, byte MidFlags, bool Visible, PlanePool pool, out GBSPBrush Front, out GBSPBrush Back)
		{
			Int32		i, j;
			GBSPPoly	p, MidPoly;
			GBSPPlane	Plane, Plane2;
			float		FrontD, BackD;
			GBSPPlane	pPlane1;
			GBSPBrush	[]Brushes	=new GBSPBrush[2];

			pPlane1	=pool.mPlanes[PNum];

			Plane		=pPlane1;
			Plane.mType	=GBSPPlane.PLANE_ANY;

			if(PSide != 0)
			{
				Plane.Inverse();
			}

			Front	=Back	=null;

			// Check all points
			FrontD = BackD = 0.0f;

			for(i=0;i < Brush.mSides.Count;i++)
			{
				p	=Brush.mSides[i].mPoly;

				if(p == null)
				{
					continue;
				}

				Brush.mSides[i].mPoly.GetSplitMaxDist(pPlane1, PSide, ref FrontD, ref BackD);
			}
			
			if(FrontD < 0.1f)
			{
				Back	=new GBSPBrush(Brush);
				return;
			}

			if(BackD > -0.1f)
			{
				Front	=new GBSPBrush(Brush);
				return;
			}

			//create a new poly from the split plane
			p	=new GBSPPoly(Plane);
			if(p == null)
			{
				Map.Print("Could not create poly.\n");
			}
			
			//Clip the poly by all the planes of the brush being split
			for(i=0;i < Brush.mSides.Count && !p.IsTiny();i++)
			{
				Plane2	=pool.mPlanes[Brush.mSides[i].mPlaneNum];
				
				p.ClipPolyEpsilon(0.0f, Plane2, Brush.mSides[i].mPlaneSide == 0);
			}

			if(p.IsTiny())
			{	
				UInt32	Side	=Brush.MostlyOnSide(Plane);
				
				if(Side == GBSPPlane.PSIDE_FRONT)
				{
					Front	=new GBSPBrush(Brush);
				}
				if(Side == GBSPPlane.PSIDE_BACK)
				{
					Back	=new GBSPBrush(Brush);
				}
				return;
			}

			//Store the mid poly
			MidPoly	=p;					

			//Create 2 brushes
			for(i=0;i < 2;i++)
			{
				Brushes[i]	=new GBSPBrush();
				
				if(Brushes[i] == null)
				{
					Map.Print("SplitBrush:  Out of memory for brush.\n");
				}
				
				Brushes[i].mOriginal	=Brush.mOriginal;
			}

			//Split all the current polys of the brush being split, and distribute it to the other 2 brushes
			foreach(GBSPSide pSide in Brush.mSides)
			{
				GBSPPoly	[]Poly	=new GBSPPoly[2];
				
				if(pSide.mPoly == null)
				{
					continue;
				}

				p	=new GBSPPoly(pSide.mPoly);
				if(!p.SplitEpsilon(0.0f, Plane, out Poly[0], out Poly[1], false))
				{
					Map.Print("Error splitting poly...\n");
				}

				for(j=0;j < 2;j++)
				{
					GBSPSide	pDestSide;

					if(Poly[j] == null)
					{
						continue;
					}

					pDestSide	=new GBSPSide(pSide);

					Brushes[j].mSides.Add(pDestSide);
					
					pDestSide.mPoly		= Poly[j];
					pDestSide.mFlags	&=~GBSPSide.SIDE_TESTED;
				}
			}

			for(i=0;i < 2;i++)
			{
				Brushes[i].BoundBrush();

				if(!Brushes[i].CheckBrush())
				{
					Brushes[i]	=null;
				}			
			}

			if(Brushes[0] == null || Brushes[1] == null)
			{				
				if(Brushes[0] == null && Brushes[1] == null)
				{
					Map.Print("Split removed brush\n");
				}
				else
				{
					Map.Print("Split not on both sides\n");
				}
				
				if(Brushes[0] != null)
				{
					Front	=new GBSPBrush(Brush);
				}
				if(Brushes[1] != null)
				{
					Back	=new GBSPBrush(Brush);
				}
				return;
			}

			for(i=0;i < 2;i++)
			{
				GBSPSide	pSide	=new GBSPSide();

				Brushes[i].mSides.Add(pSide);

				pSide.mPlaneNum		=PNum;
				pSide.mPlaneSide	=(sbyte)PSide;

				if(Visible)
				{
					pSide.mFlags	|=GBSPSide.SIDE_VISIBLE;
				}

				pSide.mFlags	&=~GBSPSide.SIDE_TESTED;
				pSide.mFlags	|=MidFlags;
			
				if(i == 0)
				{
					pSide.mPlaneSide	=(pSide.mPlaneSide == 0)? (sbyte)1 : (sbyte)0;

					pSide.mPoly	=new GBSPPoly(MidPoly);
					pSide.mPoly.Reverse();
				}
				else
				{
					//might not need to copy this
					pSide.mPoly	=new GBSPPoly(MidPoly);
				}
			}

			{
				float	v1;
				for(int z=0;z < 2;z++)
				{
					v1	=Brushes[z].Volume(pool);
					if(v1 < 1.0f)
					{
						Brushes[z]	=null;
						//GHook.Printf("Tiny volume after clip\n");
					}
				}
			}

			if(Brushes[0] == null || Brushes[1] == null)
			{
				Map.Print("SplitBrush:  Brush was not split.\n");
			}
			
			Front	=Brushes[0];
			Back	=Brushes[1];
		}


		internal float Volume(PlanePool pool)
		{
			GBSPPoly	cornerPoly	=null;
			int			i	=0;

			for(i=0;i < mSides.Count;i++)
			{
				if(mSides[i].mPoly.VertCount() > 2)
				{
					cornerPoly	=mSides[i].mPoly;
					break;
				}
			}
			if(cornerPoly == null)
			{
				return	0.0f;
			}

			float	volume	=0.0f;
			for(;i < mSides.Count;i++)
			{
				GBSPPoly	p	=mSides[i].mPoly;
				if(p == null)
				{
					continue;
				}

				GBSPPlane	plane	=pool.mPlanes[mSides[i].mPlaneNum];

				if(mSides[i].mPlaneSide != 0)
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
			GBSPBrush	Outside, Inside;
			GBSPBrush	Front, Back;
			Int32		i;

			Inside	=a;	// Default a being inside b
			Outside	=null;

			//Splitting the inside list against each plane of brush b, only keeping peices that fall on the
			//outside
			for(i=0;i < b.mSides.Count && Inside != null;i++)
			{
				Split(Inside, b.mSides[i].mPlaneNum, b.mSides[i].mPlaneSide, (byte)GBSPSide.SIDE_NODE, false, pool, out Front, out Back);

				//Make sure we don't free a, but free all other fragments
				if(Inside != a)
				{
					Inside	=null;
				}

				//Keep all front sides, and put them in the Outside list
				if(Front != null)
				{	
					Front.mNext	=Outside;
					Outside		=Front;
				}

				Inside	=Back;
			}

			if(Inside == null)
			{
				FreeBrushList(Outside);		
				return	a;	//Nothing on inside list, so cancel all cuts, and return original
			}
			
			Inside	=null;	//Free all inside fragments

			return	Outside;	//Return what was on the outside
		}


		internal static GBSPBrush CSGBrushes(bool bVerbose, GBSPBrush Head, PlanePool pool)
		{
			GBSPBrush	b1, b2, Next;
			GBSPBrush	Tail;
			GBSPBrush	Keep;
			GBSPBrush	Sub, Sub2;
			Int32		c1, c2;

			if(bVerbose)
			{
				Map.Print("---- CSGBrushes ----\n");
				Map.Print("Num brushes before CSG : " + CountBrushList(Head) + "\n");
			}

			Keep	=null;

		NewList:

			if(Head == null)
			{
				return null;
			}

			for(Tail=Head;Tail.mNext != null;Tail=Tail.mNext);

			for(b1=Head;b1 != null;b1=Next)
			{
				Next = b1.mNext;
				
				for(b2=b1.mNext;b2 != null;b2 = b2.mNext)
				{
					if(!b1.Overlaps(b2))
					{
						continue;
					}

					Sub		=null;
					Sub2	=null;
					c1		=999999;
					c2		=999999;

					if(b2.BrushCanBite(b1))
					{
						Sub	=Subtract(b1, b2, pool);

						if(Sub == b1)
						{
							continue;
						}

						if(Sub == null)
						{
							Head = RemoveBrushList(b1, b1);
							goto NewList;
						}
						c1 = CountBrushList (Sub);
					}

					if(b1.BrushCanBite(b2))
					{
						Sub2	=Subtract(b2, b1, pool);

						if(Sub2 == b2)
						{
							continue;
						}

						if(Sub2 == null)
						{	
							FreeBrushList(Sub);
							Head	=RemoveBrushList(b1, b2);
							goto NewList;
						}
						c2	=CountBrushList(Sub2);
					}

					if(Sub == null && Sub2 == null)
					{
						continue;
					}

					if(c1 > 4 && c2 > 4)
					{
						if(Sub2 != null)
						{
							FreeBrushList(Sub2);
						}
						if(Sub != null)
						{
							FreeBrushList(Sub);
						}
						continue;
					}					

					if(c1 < c2)
					{
						if(Sub2 != null)
						{
							FreeBrushList(Sub2);
						}
						Tail	=AddBrushListToTail(Sub, Tail);
						Head	=RemoveBrushList(b1, b1);
						goto NewList;
					}
					else
					{
						if(Sub != null)
						{
							FreeBrushList(Sub);
						}
						Tail	=AddBrushListToTail(Sub2, Tail);
						Head	=RemoveBrushList(b1, b2);
						goto NewList;
					}
				}

				if(b2 == null)
				{	
					b1.mNext	=Keep;
					Keep		=b1;
				}
			}

			if(bVerbose)
			{
				Map.Print("Num brushes after CSG  : " + CountBrushList(Keep) + "\n");
			}

			return	Keep;
		}


		static GBSPBrush AddBrushListToTail(GBSPBrush List, GBSPBrush Tail)
		{
			GBSPBrush	Walk, Next;

			for (Walk=List;Walk != null;Walk=Next)
			{	// add to end of list
				Next		=Walk.mNext;
				Walk.mNext	=null;
				Tail.mNext	=Walk;
				Tail		=Walk;
			}
			return	Tail;
		}


		static Int32 CountBrushList(GBSPBrush Brushes)
		{
			Int32	c	=0;
			for(;Brushes != null;Brushes=Brushes.mNext)
			{
				c++;
			}
			return	c;
		}


		static GBSPBrush RemoveBrushList(GBSPBrush List, GBSPBrush Remove)
		{
			GBSPBrush	NewList;
			GBSPBrush	Next;

			NewList	=null;

			for(;List != null;List = Next)
			{
				Next	=List.mNext;

				if(List == Remove)
				{
					List	=null;
					continue;
				}

				List.mNext	=NewList;
				NewList		=List;
			}
			return	NewList;
		}


		internal static GBSPSide SelectSplitSide(BuildStats bs, GBSPBrush Brushes,
			GBSPNode Node, PlanePool pool)
		{
			Int32		Value, BestValue;
			GBSPBrush	Brush, Test;
			GBSPSide	Side, BestSide;
			Int32		i, j, Pass, NumPasses;
			Int32		PNum, PSide;
			UInt32		s;
			Int32		Front, Back, Both, Facing, Splits;
			Int32		BSplits;
			Int32		BestSplits;
			Int32		EpsilonBrush;
			bool		HintSplit	=false;

			BestSide	=null;
			BestValue	=-999999;
			BestSplits	=0;
			NumPasses	=4;
			for(Pass = 0;Pass < NumPasses;Pass++)
			{
				for(Brush = Brushes;Brush != null;Brush=Brush.mNext)
				{
					if(((Pass & 1) != 0)
						&& ((Brush.mOriginal.mContents & Contents.BSP_CONTENTS_DETAIL2) == 0))
					{
						continue;
					}
					if(((Pass & 1) == 0)
						&& ((Brush.mOriginal.mContents & Contents.BSP_CONTENTS_DETAIL2) != 0))
					{
						continue;
					}
					
					for(i=0;i < Brush.mSides.Count;i++)
					{
						Side	=Brush.mSides[i];

						if(Side.mPoly == null)
						{
							continue;
						}
						if((Side.mFlags & (GBSPSide.SIDE_TESTED | GBSPSide.SIDE_NODE)) != 0)
						{
							continue;
						}
 						if(((Side.mFlags & GBSPSide.SIDE_VISIBLE) == 0) && Pass < 2)
						{
							continue;
						}

						PNum	=Side.mPlaneNum;
						PSide	=Side.mPlaneSide;
						
						Debug.Assert(Node.CheckPlaneAgainstParents(PNum) == true);
												
						Front			=0;
						Back			=0;
						Both			=0;
						Facing			=0;
						Splits			=0;
						EpsilonBrush	=0;

						for(Test=Brushes;Test != null;Test=Test.mNext)
						{
							s	=Test.TestBrushToPlane(PNum, PSide, pool, out BSplits, out HintSplit, ref EpsilonBrush);

							Splits	+=BSplits;

							if(BSplits != 0 && ((s & GBSPPlane.PSIDE_FACING) != 0))
							{
								Map.Print("PSIDE_FACING with splits\n");
							}

							Test.mTestSide	=s;

							if((s & GBSPPlane.PSIDE_FACING) != 0)
							{
								Facing++;
								for(j=0;j < Test.mSides.Count;j++)
								{
									if(Test.mSides[j].mPlaneNum == PNum)
									{
										Test.mSides[j].mFlags	|=GBSPSide.SIDE_TESTED;
									}
								}
							}
							if((s & GBSPPlane.PSIDE_FRONT) != 0)
							{
								Front++;
							}
							if((s & GBSPPlane.PSIDE_BACK) != 0)
							{
								Back++;
							}
							if (s == GBSPPlane.PSIDE_BOTH)
							{
								Both++;
							}
						}

						Value	=5 * Facing - 5 * Splits - Math.Abs(Front - Back);
						
						if(pool.mPlanes[PNum].mType < 3)
						{
							Value	+=5;
						}
						
						Value	-=EpsilonBrush * 1000;	

						if(HintSplit && ((Side.mFlags & GBSPSide.SIDE_HINT) == 0))
						{
							Value	=-999999;
						}

						if(Value > BestValue)
						{
							BestValue	=Value;
							BestSide	=Side;
							BestSplits	=Splits;
							for(Test=Brushes;Test != null;Test=Test.mNext)
							{
								Test.mSide	=Test.mTestSide;
							}
						}
					}
				}

				if(BestSide != null)
				{
					if(Pass > 1)
					{
						bs.NumNonVisNodes++;
					}
					
					if(Pass > 0)
					{
						Node.SetDetail(true);	//Not needed for vis
						if((BestSide.mFlags & GBSPSide.SIDE_HINT) != 0)
						{
							Map.Print("*** Hint as Detail!!! ***\n");
						}
					}					
					break;
				}
			}

			for(Brush = Brushes;Brush != null;Brush=Brush.mNext)
			{
				for(i=0;i < Brush.mSides.Count;i++)
				{
					Brush.mSides[i].mFlags	&=~GBSPSide.SIDE_TESTED;
				}
			}

			return	BestSide;
		}


		UInt32 TestBrushToPlane(int PlaneNum, int PSide, PlanePool pool, out int NumSplits, out bool HintSplit, ref int EpsilonBrush)
		{
			GBSPPlane	Plane;
			UInt32		s;
			float		FrontD, BackD;
			Int32		Front, Back;

			NumSplits	=0;
			HintSplit	=false;

			for(int i=0;i < mSides.Count;i++)
			{
				int	Num	=mSides[i].mPlaneNum;
				
				if(Num == PlaneNum && mSides[i].mPlaneSide == 0)
				{
					return	GBSPPlane.PSIDE_BACK | GBSPPlane.PSIDE_FACING;
				}

				if(Num == PlaneNum && mSides[i].mPlaneSide != 0)
				{
					return	GBSPPlane.PSIDE_FRONT | GBSPPlane.PSIDE_FACING;
				}
			}
			
			//See if it's totally on one side or the other
			Plane	=pool.mPlanes[PlaneNum];

			s	=mBounds.BoxOnPlaneSide(Plane);

			if(s != GBSPPlane.PSIDE_BOTH)
			{
				return	s;
			}
			
			//The brush is split, count the number of splits 
			FrontD	=BackD	=0.0f;

			foreach(GBSPSide pSide in mSides)
			{
				if((pSide.mFlags & GBSPSide.SIDE_NODE) != 0)
				{
					continue;
				}
				if((pSide.mFlags & GBSPSide.SIDE_VISIBLE) == 0)
				{
					continue;
				}
				if(pSide.mPoly.VertCount() < 3)
				{
					continue;
				}

				Front	=Back	=0;
				pSide.mPoly.SplitSideTest(Plane, out Front, out Back, ref FrontD, ref BackD);

				if(Front != 0 && Back != 0)
				{
					NumSplits++;
					if((pSide.mFlags & GBSPSide.SIDE_HINT) != 0)
					{
						HintSplit	=true;
					}
				}
			}

			//Check to see if this split would produce a tiny brush (would result in tiny leafs, bad for vising)
			if((FrontD > 0.0f && FrontD < 1.0f) || (BackD < 0.0f && BackD > -1.0f))
			{
				EpsilonBrush++;
			}

			return	s;
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


		internal static void FreeBrushList(GBSPBrush brushes)
		{
			GBSPBrush	next;

			for(;brushes != null;brushes = next)
			{
				next	=brushes.mNext;

				brushes.mSides.Clear();
				brushes.mBounds	=null;
				brushes			=null;
			}
		}


		internal static void SplitBrushList(GBSPBrush Brushes, Int32 NodePlaneNum, PlanePool pool,
			out GBSPBrush Front, out GBSPBrush Back)
		{
			GBSPBrush	Brush, NewBrush, NewBrush2, Next;
			GBSPSide	Side;
			UInt32		Sides;
			Int32		i;

			Front = Back = null;

			for(Brush = Brushes;Brush != null;Brush = Next)
			{
				Next	=Brush.mNext;
				Sides	=Brush.mSide;

				if(Sides == GBSPPlane.PSIDE_BOTH)
				{
					Split(Brush, NodePlaneNum, 0, (byte)GBSPSide.SIDE_NODE, false, pool, out NewBrush, out NewBrush2);
					if(NewBrush != null)
					{
						NewBrush.mNext	=Front;
						Front			=NewBrush;
					}
					if(NewBrush2 != null)
					{
						NewBrush2.mNext	=Back;
						Back			=NewBrush2;
					}
					continue;
				}

				NewBrush	=new GBSPBrush(Brush);

				if((Sides & GBSPPlane.PSIDE_FACING) != 0)
				{
					for(i=0;i < NewBrush.mSides.Count;i++)
					{
						Side	=NewBrush.mSides[i];
						if(Side.mPlaneNum == NodePlaneNum)
						{
							Side.mFlags	|=GBSPSide.SIDE_NODE;
						}
					}
				}

				if((Sides & GBSPPlane.PSIDE_FRONT) != 0)
				{
					NewBrush.mNext	=Front;
					Front			=NewBrush;
					continue;
				}
				if((Sides & GBSPPlane.PSIDE_BACK) != 0)
				{
					NewBrush.mNext	=Back;
					Back			=NewBrush;
					continue;
				}
			}
		}


		static internal void DumpBrushListToFile(GBSPBrush brushList)
		{
			FileStream		fs	=new FileStream("BrushSides.txt", FileMode.Create, FileAccess.Write);
			StreamWriter	sw	=new StreamWriter(fs);			

			for(GBSPBrush b=brushList;b != null;b=b.mNext)
			{
				for(int i=0;i < b.mSides.Count;i++)
				{
					sw.Write("" + b.mSides[i].mPlaneNum + ", " +
						b.mSides[i].mPlaneSide + ", " +
						b.mSides[i].mFlags + "\n");
				}
			}
			sw.Close();
			fs.Close();
		}
	}
}
