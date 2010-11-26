using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class GBSPBrush
	{
		Bounds		mBounds	=new Bounds();
		Int32		mSide, mTestSide;
		MapBrush	mOriginal;

		List<GBSPSide>	mSides	=new List<GBSPSide>();

		public const UInt32 BSP_CONTENTS_SOLID2			=(1<<0);		// Solid (Visible)
		public const UInt32 BSP_CONTENTS_WINDOW2		=(1<<1);		// Window (Visible)
		public const UInt32 BSP_CONTENTS_EMPTY2			=(1<<2);		// Empty but Visible (water, lava, etc...)

		public const UInt32 BSP_CONTENTS_TRANSLUCENT2	=(1<<3);		// Vis will see through it
		public const UInt32 BSP_CONTENTS_WAVY2			=(1<<4);		// Wavy (Visible)
		public const UInt32 BSP_CONTENTS_DETAIL2		=(1<<5);		// Won't be included in vis oclusion

		public const UInt32 BSP_CONTENTS_CLIP2			=(1<<6);		// Structural but not visible
		public const UInt32 BSP_CONTENTS_HINT2			=(1<<7);		// Primary splitter (Non-Visible)
		public const UInt32 BSP_CONTENTS_AREA2			=(1<<8);		// Area seperator leaf (Non-Visible)

		public const UInt32 BSP_CONTENTS_FLOCKING		=(1<<9);		// flocking flag.  Not really a contents type
		public const UInt32 BSP_CONTENTS_SHEET			=(1<<10);
		public const UInt32 RESERVED3					=(1<<11);
		public const UInt32 RESERVED4					=(1<<12);
		public const UInt32 RESERVED5					=(1<<13);
		public const UInt32 RESERVED6					=(1<<14);
		public const UInt32 RESERVED7					=(1<<15);

		//16-31 reserved for user contents
		public const UInt32 BSP_CONTENTS_USER1			=(1<<16);
		public const UInt32 BSP_CONTENTS_USER2			=(1<<17);
		public const UInt32 BSP_CONTENTS_USER3			=(1<<18);
		public const UInt32 BSP_CONTENTS_USER4			=(1<<19);
		public const UInt32 BSP_CONTENTS_USER5			=(1<<20);
		public const UInt32 BSP_CONTENTS_USER6			=(1<<21);
		public const UInt32 BSP_CONTENTS_USER7			=(1<<22);
		public const UInt32 BSP_CONTENTS_USER8			=(1<<23);
		public const UInt32 BSP_CONTENTS_USER9			=(1<<24);
		public const UInt32 BSP_CONTENTS_USER10			=(1<<25);
		public const UInt32 BSP_CONTENTS_USER11			=(1<<26);
		public const UInt32 BSP_CONTENTS_USER12			=(1<<27);
		public const UInt32 BSP_CONTENTS_USER13			=(1<<28);
		public const UInt32 BSP_CONTENTS_USER14			=(1<<29);
		public const UInt32 BSP_CONTENTS_USER15			=(1<<30);
		public const UInt32 BSP_CONTENTS_USER16			=(0x80000000);
		
		//These contents are all solid types
		public const UInt32 BSP_CONTENTS_SOLID_CLIP		=(BSP_CONTENTS_SOLID2 | BSP_CONTENTS_WINDOW2 | BSP_CONTENTS_CLIP2);
		
		//These contents are all visible types
		public const UInt32 BSP_VISIBLE_CONTENTS		=(BSP_CONTENTS_SOLID2 | BSP_CONTENTS_EMPTY2 | BSP_CONTENTS_WINDOW2 | BSP_CONTENTS_SHEET | BSP_CONTENTS_WAVY2);
		
		//These contents define where faces are NOT allowed to merge across
		public const UInt32 BSP_MERGE_SEP_CONTENTS		=(BSP_CONTENTS_WAVY2 | BSP_CONTENTS_HINT2 | BSP_CONTENTS_AREA2);


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
				if(mb.mOriginalSides[j].mPoly.mVerts.Count > 2)
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


		void BoundBrush()
		{
			mBounds.ClearBounds();

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

			if(((c1 & BSP_CONTENTS_DETAIL2) != 0) &&
				!((c2 & BSP_CONTENTS_DETAIL2) != 0))
			{
				return	false;
			}

			if(((c1|c2) & BSP_CONTENTS_FLOCKING) != 0)
			{
				return	false;
			}

			if((c1 & BSP_CONTENTS_SOLID2) != 0)
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
				if(mSides[i].mPoly.mVerts.Count < 3)
				{
					continue;
				}

				for(int j=0;j < mSides[i].mPoly.mVerts.Count;j++)
				{
					float	d	=Vector3.Dot(mSides[i].mPoly.mVerts[j], plane.mNormal) - plane.mDist;
					if(d > max)
					{
						max		=d;
						side	=GBSPPlane.PSIDE_FRONT;
					}
					if(-d > max)
					{
						max		=-d;
						side	=GBSPPlane.PSIDE_BACK;
					}
				}
			}

			return	side;
		}


		void Split(Int32 pNum, sbyte pSide, byte midFlags, bool bVisible, PlanePool pool, out GBSPBrush front, out GBSPBrush back)
		{
			GBSPPlane	plane1	=pool.mPlanes[pNum];

			plane1.mType	=GBSPPlane.PLANE_ANY;

			if(pSide != 0)
			{
				plane1.Inverse();
			}

			front	=back	=null;

			float	frontD	=0.0f;
			float	backD	=0.0f;

			for(int i=0;i < mSides.Count;i++)
			{
				if(mSides[i].mPoly.mVerts.Count < 3)
				{
					continue;
				}

				for(int j=0;j < mSides[i].mPoly.mVerts.Count;j++)
				{
					float	d	=pool.mPlanes[pNum].DistanceFast(mSides[i].mPoly.mVerts[j]);

					if(pSide != 0)
					{
						d	=-d;
					}

					if(d > frontD)
					{
						frontD	=d;
					}
					else if(d < backD)
					{
						backD	=d;
					}
				}
			}

			if(frontD < 0.1f)
			{
				back	=new GBSPBrush(this);
				front	=null;
				return;
			}

			if(backD > -0.1f)
			{
				front	=new GBSPBrush(this);
				back	=null;
				return;
			}

			GBSPPoly	p	=new GBSPPoly(plane1);

			for(int i=0;i < mSides.Count && p.mVerts.Count > 2;i++)
			{
				GBSPPlane	plane2	=pool.mPlanes[mSides[i].mPlaneNum];

				p.ClipPolyEpsilon(0.0f, plane2, mSides[i].mPlaneSide == 0);
			}

			if(p.IsTiny())
			{
				UInt32	side	=MostlyOnSide(plane1);

				if(side == GBSPPlane.PSIDE_FRONT)
				{
					front	=new GBSPBrush(this);
				}
				if(side == GBSPPlane.PSIDE_BACK)
				{
					back	=new GBSPBrush(this);
				}
				return;
			}

			GBSPPoly	midPoly	=p;

			front	=new GBSPBrush();
			front.mOriginal	=mOriginal;

			back	=new GBSPBrush();
			back.mOriginal	=mOriginal;

			for(int i=0;i < mSides.Count;i++)
			{
				if(mSides[i].mPoly.mVerts.Count < 3)
				{
					continue;
				}

				GBSPPoly	polyFront, polyBack;

				p	=new GBSPPoly(mSides[i].mPoly);

				if(!p.SplitEpsilon(0.0f, plane1, out polyFront, out polyBack, false))
				{
					Map.Print("Error splitting poly...\n");
				}

				if(polyFront != null)
				{
					GBSPSide	frontSide	=new GBSPSide(mSides[i]);

					frontSide.mPoly		=polyFront;
					frontSide.mFlags	&=~GBSPSide.SIDE_TESTED;

					front.mSides.Add(frontSide);
				}
				if(polyBack != null)
				{
					GBSPSide	backSide	=new GBSPSide(mSides[i]);

					backSide.mPoly	=polyBack;
					backSide.mFlags	&=~GBSPSide.SIDE_TESTED;

					back.mSides.Add(backSide);
				}
			}

			front.BoundBrush();
			if(!front.CheckBrush())
			{
				front	=null;
			}
			back.BoundBrush();
			if(!back.CheckBrush())
			{
				back	=null;
			}

			if(front == null || back == null)
			{
				if(front == null && back == null)
				{
					Map.Print("Split removed brush\n");
				}
				else
				{
					Map.Print("Split not on both sides\n");
				}

				if(front != null)
				{
					front	=new GBSPBrush(this);
				}
				if(back != null)
				{
					back	=new GBSPBrush(this);
				}
				return;
			}

			//add in the split plane
			GBSPSide	splitSide	=new GBSPSide();

			splitSide.mPlaneNum		=pNum;
			splitSide.mPlaneSide	=pSide;

			if(bVisible)
			{
				splitSide.mFlags	|=GBSPSide.SIDE_VISIBLE;
			}
			splitSide.mFlags	&=~GBSPSide.SIDE_TESTED;
			splitSide.mFlags	|=midFlags;

			splitSide.mPlaneSide	=(pSide == 0)? (sbyte)1 : (sbyte)0;

			splitSide.mPoly	=new GBSPPoly(midPoly);
			splitSide.mPoly.Reverse();

			front.mSides.Add(splitSide);

			//add back side
			splitSide	=new GBSPSide();

			splitSide.mPlaneNum		=pNum;
			splitSide.mPlaneSide	=pSide;

			if(bVisible)
			{
				splitSide.mFlags	|=GBSPSide.SIDE_VISIBLE;
			}
			splitSide.mFlags	&=~GBSPSide.SIDE_TESTED;
			splitSide.mFlags	|=midFlags;

			splitSide.mPoly	=new GBSPPoly(midPoly);

			back.mSides.Add(splitSide);

			float	v1	=front.Volume(pool);
			if(v1 < 1.0f)
			{
				front	=null;
			}
			v1	=back.Volume(pool);
			if(v1 < 1.0f)
			{
				back	=null;
			}

			if(front == null && back == null)
			{
				Map.Print("SplitBrush:  Brush was not split.\n");
			}
		}

		private float Volume(PlanePool pool)
		{
			GBSPPoly	p	=null;
			int			i	=0;

			for(i=0;i < mSides.Count;i++)
			{
				if(mSides[i].mPoly.mVerts.Count > 2)
				{
					p	=mSides[i].mPoly;
					break;
				}
			}
			if(p == null)
			{
				return	0.0f;
			}

			Vector3	corner	=p.mVerts[0];

			float	volume	=0.0f;
			for(;i < mSides.Count;i++)
			{
				p	=mSides[i].mPoly;
				if(p == null)
				{
					continue;
				}

				GBSPPlane	plane	=pool.mPlanes[mSides[i].mPlaneNum];

				if(mSides[i].mPlaneSide != 0)
				{
					plane.Inverse();
				}

				float	d		=-(Vector3.Dot(corner, plane.mNormal) - plane.mDist);
				float	area	=p.Area();

				volume	+=d * area;
			}

			volume	/=3.0f;
			return	volume;
		}


		void Subtract(GBSPBrush b, PlanePool pool, List<GBSPBrush> outList)
		{
			GBSPBrush	inside	=new GBSPBrush(this);

			for(int i=0;i < b.mSides.Count && inside != null;i++)
			{
				GBSPBrush	front, back;

				inside.Split(b.mSides[i].mPlaneNum, b.mSides[i].mPlaneSide, (byte)GBSPSide.SIDE_NODE, false, pool, out front, out back);

				if(front != null)
				{
					outList.Add(front);
				}
				inside	=back;
			}

			if(inside == null)
			{
				outList.Clear();
			}
			else
			{
				//make this == inside
				this.mBounds	=inside.mBounds;
				this.mOriginal	=inside.mOriginal;
				this.mSide		=inside.mSide;

				this.mSides.Clear();
				foreach(GBSPSide side in inside.mSides)
				{
					this.mSides.Add(new GBSPSide(side));
				}
				this.mTestSide	=inside.mTestSide;
			}
		}


		internal static void CSGBrushes(List<GBSPBrush> inList, PlanePool pool)
		{
			List<GBSPBrush>	outList	=new List<GBSPBrush>();

			Map.Print("---- CSGBrushes ----\n");
			Map.Print("Num brushes before CSG : " + inList.Count + "\n");

			NewList:

			for(int i=0;i < inList.Count;i++)
			{
				GBSPBrush	b1	=inList[i];
				GBSPBrush	b2	=null;

				for(int j=0;j < inList.Count;j++)
				{
					if(i == j)
					{
						continue;
					}

					b2	=inList[j];

					if(!b1.Overlaps(b2))
					{
						continue;
					}

					List<GBSPBrush>	outsides1	=new List<GBSPBrush>();
					List<GBSPBrush>	outsides2	=new List<GBSPBrush>();

					if(b2.BrushCanBite(b1))
					{
						GBSPBrush	dupe	=new GBSPBrush(b1);
						dupe.Subtract(b2, pool, outsides1);

						if(outsides1.Count == 0)
						{
							continue;
						}
					}

					if(b1.BrushCanBite(b2))
					{
						GBSPBrush	dupe	=new GBSPBrush(b2);
						dupe.Subtract(b1, pool, outsides2);

						if(outsides2.Count == 0)
						{
							continue;
						}
					}

					if(outsides1.Count == 0 && outsides2.Count == 0)
					{
						continue;
					}

					if(outsides1.Count > 4 && outsides2.Count > 4)
					{
						outsides1.Clear();
						outsides2.Clear();
						continue;
					}

					if(outsides1.Count < outsides2.Count)
					{
						outsides2.Clear();
						inList.Remove(b1);
						inList.AddRange(outsides1);
						goto NewList;
					}
					else
					{
						outsides1.Clear();
						inList.Remove(b2);
						inList.AddRange(outsides2);
						goto NewList;
					}
				}
			}
			Map.Print("Num brushes after CSG  : " + inList.Count + "\n");
		}


		internal void GetTriangles(List<Vector3> verts, List<uint> indexes, bool p)
		{
			foreach(GBSPSide s in mSides)
			{
				s.GetTriangles(verts, indexes);
			}
		}
	}
}
