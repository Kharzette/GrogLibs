using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class MapBrush
	{
		Int32		mEntityNum;
		Int32		mBrushNum;
		public UInt32		mContents;
		public Bounds		mBounds;
		Int32		mOrderID;

		public List<GBSPSide>	mOriginalSides	=new List<GBSPSide>();
		

		MapBrush AllocMapBrush(Int32 numSides)
		{
			return	new MapBrush();			
		}


		void FreeMapBrush()
		{
			foreach(GBSPSide side in mOriginalSides)
			{
				if(side.mPoly != null)
				{
					side.mPoly	=null;
				}
			}
			mOriginalSides.Clear();
		}


		#region IO
		internal static void SkipVMFEditorBlock(StreamReader sr)
		{
			string	s	="";
			while((s = sr.ReadLine()) != null)
			{
				s	=s.Trim();
				if(s.StartsWith("}"))
				{
					return;	//editor done
				}
			}
		}


		internal bool ReadVMFSolidBlock(StreamReader sr, PlanePool pool, int entityNum)
		{
			string	s	="";
			bool	ret	=true;
			while((s = sr.ReadLine()) != null)
			{
				s	=s.Trim();
				if(s == "side")
				{
					GBSPSide	side	=new GBSPSide();
					mContents	=side.ReadVMFSideBlock(sr, pool);

					if(mContents == Brush.CONTENTS_AUX)
					{
						ret	=false;
					}

					side.FixFlags();

					mOriginalSides.Add(side);
					mEntityNum	=entityNum;
				}
				else if(s.StartsWith("}"))
				{
					return	ret;	//entity done
				}
				else if(s == "editor")
				{
					//skip editor block
					SkipVMFEditorBlock(sr);
				}
			}
			return	ret;
		}
		#endregion


		//convert hammer contents to genesis style contents
		internal void FixContents()
		{
			UInt32	hammerContents	=mContents;

			mContents	=0;

			if(hammerContents == 0)
			{
				//set to solid by default?
				mContents	|=GBSPBrush.BSP_CONTENTS_SOLID2;
			}

			if((hammerContents & Brush.CONTENTS_LAVA) != 0)
			{
				mContents	|=GBSPBrush.BSP_CONTENTS_TRANSLUCENT2;
				mContents	|=GBSPBrush.BSP_CONTENTS_USER1;
			}
			if((hammerContents & Brush.CONTENTS_SLIME) != 0)
			{
				mContents	|=GBSPBrush.BSP_CONTENTS_TRANSLUCENT2;
				mContents	|=GBSPBrush.BSP_CONTENTS_WAVY2;
				mContents	|=GBSPBrush.BSP_CONTENTS_USER2;
			}
			if((hammerContents & Brush.CONTENTS_WATER) != 0)
			{
				mContents	|=GBSPBrush.BSP_CONTENTS_TRANSLUCENT2;
				mContents	|=GBSPBrush.BSP_CONTENTS_WAVY2;
				mContents	|=GBSPBrush.BSP_CONTENTS_USER3;
			}
			if((hammerContents & Brush.CONTENTS_MIST) != 0)
			{
				mContents	|=GBSPBrush.BSP_CONTENTS_TRANSLUCENT2;
				mContents	|=GBSPBrush.BSP_CONTENTS_USER4;
			}
			if((hammerContents & Brush.CONTENTS_AREAPORTAL) != 0)
			{
				mContents	|=GBSPBrush.BSP_CONTENTS_AREA2;
			}
			if((hammerContents & Brush.CONTENTS_PLAYERCLIP) != 0)
			{
				mContents	|=GBSPBrush.BSP_CONTENTS_CLIP2;
			}
			if((hammerContents & Brush.CONTENTS_MONSTERCLIP) != 0)
			{
				mContents	|=GBSPBrush.BSP_CONTENTS_CLIP2;
			}
			if((hammerContents & Brush.CONTENTS_CURRENT_0) != 0)
			{
				mContents	|=GBSPBrush.BSP_CONTENTS_USER5;
			}
			if((hammerContents & Brush.CONTENTS_CURRENT_90) != 0)
			{
				mContents	|=GBSPBrush.BSP_CONTENTS_USER6;
			}
			if((hammerContents & Brush.CONTENTS_CURRENT_180) != 0)
			{
				mContents	|=GBSPBrush.BSP_CONTENTS_USER7;
			}
			if((hammerContents & Brush.CONTENTS_CURRENT_270) != 0)
			{
				mContents	|=GBSPBrush.BSP_CONTENTS_USER8;
			}
			if((hammerContents & Brush.CONTENTS_CURRENT_UP) != 0)
			{
				mContents	|=GBSPBrush.BSP_CONTENTS_USER9;
			}
			if((hammerContents & Brush.CONTENTS_CURRENT_DOWN) != 0)
			{
				mContents	|=GBSPBrush.BSP_CONTENTS_USER10;
			}
			if((hammerContents & Brush.CONTENTS_DETAIL) != 0)
			{
				mContents	|=GBSPBrush.BSP_CONTENTS_DETAIL2;
			}
			if((hammerContents & Brush.CONTENTS_TRANSLUCENT) != 0)
			{
				mContents	|=GBSPBrush.BSP_CONTENTS_TRANSLUCENT2;
			}
			if((hammerContents & Brush.CONTENTS_LADDER) != 0)
			{
				mContents	|=GBSPBrush.BSP_CONTENTS_USER11;
			}
			if((hammerContents & Brush.CONTENTS_STRUCTURAL) != 0)
			{
				mContents	|=GBSPBrush.BSP_CONTENTS_SOLID2;
			}
			if((hammerContents & Brush.CONTENTS_TRIGGER) != 0)
			{
				mContents	|=GBSPBrush.BSP_CONTENTS_USER12;
				mContents	|=GBSPBrush.BSP_CONTENTS_EMPTY2;
			}
			if((hammerContents & Brush.CONTENTS_NODROP) != 0)
			{
				mContents	|=GBSPBrush.BSP_CONTENTS_USER13;
			}

			//HACK!  Convert solid sheets to clip...
			if(((mContents & GBSPBrush.BSP_CONTENTS_SHEET) !=0)
				&& ((mContents & GBSPBrush.BSP_CONTENTS_SOLID2) !=0))
			{
				mContents	&=~GBSPBrush.BSP_CONTENTS_SOLID2;
				mContents |= GBSPBrush.BSP_CONTENTS_CLIP2;
			}
			
			//Force clip to solid/detail, and mark faces as not visible (they will get put last in the tree)
			if((mContents & GBSPBrush.BSP_CONTENTS_CLIP2) != 0)
			{
				for(int k=0;k < mOriginalSides.Count;k++)
				{
					mOriginalSides[k].mFlags	&=~GBSPSide.SIDE_VISIBLE;	// Clips won't have faces
				}
				mContents	|=GBSPBrush.BSP_CONTENTS_DETAIL2;			// Clips are allways detail
			}
			
			//if empty hide sides?
			if((mContents & GBSPBrush.BSP_CONTENTS_EMPTY2) != 0)
			{
				for(int k=0;k < mOriginalSides.Count;k++)
				{
					mOriginalSides[k].mFlags	&=~GBSPSide.SIDE_VISIBLE;
				}
			}
			
			if((mContents & GBSPBrush.BSP_CONTENTS_SHEET) != 0)
			{
				//Only the first side is visible for sheets
				mOriginalSides[0].mFlags	|=GBSPSide.SIDE_SHEET;
				
				//Sheets are allways detail!!!
				mContents	|=GBSPBrush.BSP_CONTENTS_DETAIL2;
			}
			
			//Force non-solid/non-hint to detail
			//if (!(Brush->Contents & BSP_CONTENTS_SOLID2))
			//	Brush->Contents |= BSP_CONTENTS_DETAIL2;
			
			//Convert all sides to hint if need so...
			if((mContents & GBSPBrush.BSP_CONTENTS_HINT2) != 0)
			{
				for(int k=0;k < mOriginalSides.Count;k++)
				{
					mOriginalSides[k].mFlags	|=GBSPSide.SIDE_HINT;
					mOriginalSides[k].mFlags	|=GBSPSide.SIDE_VISIBLE;
				}
				
				if((mContents & GBSPBrush.BSP_CONTENTS_DETAIL2) != 0)
				{
					mContents	&=~GBSPBrush.BSP_CONTENTS_DETAIL2;
				}
			}
			
			if((mContents & GBSPBrush.BSP_CONTENTS_WINDOW2) != 0)
			{
				mContents	|=GBSPBrush.BSP_CONTENTS_TRANSLUCENT2;
				mContents	|=GBSPBrush.BSP_CONTENTS_DETAIL2;
			}
		}


		internal bool MakePolys(PlanePool pool)
		{
			mBounds	=new Bounds();

			for(int i=0;i < mOriginalSides.Count;i++)
			{
				GBSPPlane	plane	=pool.mPlanes[mOriginalSides[i].mPlaneNum];

				if(mOriginalSides[i].mPlaneSide != 0)
				{
					plane.Inverse();
				}
				GBSPPoly	p	=new GBSPPoly(plane);

				for(int j=0;j < mOriginalSides.Count && p.mVerts.Count != 0;j++)
				{
					if(i == j)
					{
						continue;
					}
					GBSPPlane	plane2	=pool.mPlanes[mOriginalSides[j].mPlaneNum];
					p.ClipPolyEpsilon(0.0f, plane2, mOriginalSides[j].mPlaneSide == 0);
				}

				GBSPSide	side	=mOriginalSides[i];
				side.mPoly	=p;

				if(p.mVerts.Count > 2)
				{
					side.mFlags	|=GBSPSide.SIDE_VISIBLE;
					for(int j=0;j < p.mVerts.Count;j++)
					{
						mBounds.AddPointToBounds(p.mVerts[j]);
					}
				}
			}

			for(int i=0;i < 3;i++)
			{
				if(UtilityLib.Mathery.VecIdx(mBounds.mMins, i) <= -Brush.MIN_MAX_BOUNDS
					|| UtilityLib.Mathery.VecIdx(mBounds.mMaxs, i) >= Brush.MIN_MAX_BOUNDS)
				{
					Map.Print("Entity " + mEntityNum + ", Brush " + mBrushNum + ": Bounds out of range\n");
				}
			}
			return	true;
		}


		internal void GetTriangles(List<Vector3> tris, List<UInt32> ind, bool bCheckFlags)
		{
			foreach(GBSPSide s in mOriginalSides)
			{
				s.GetTriangles(tris, ind, bCheckFlags);
			}
		}
	}
}
