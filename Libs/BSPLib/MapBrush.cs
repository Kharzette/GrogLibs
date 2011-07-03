using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class MapBrush
	{
		internal Int32		mEntityNum;
		internal UInt32		mContents;
		internal Bounds		mBounds;

		internal List<GBSPSide>	mOriginalSides	=new List<GBSPSide>();


		public int NumSides
		{
			get { return mOriginalSides.Count; }
			set { }
		}

		public Vector3 Center
		{
			get { return mBounds.GetCenter(); }
			set { }
		}


		public MapBrush()
		{
		}


		//this constructor will create a box
		//brush to fit the passed in bounds
		internal MapBrush(Bounds bnd, PlanePool pp)
		{
			for(int i=0;i < 3;i++)
			{
				GBSPPlane	p	=new GBSPPlane();

				p.mNormal	=Vector3.Zero;

				UtilityLib.Mathery.VecIdxAssign(ref p.mNormal, i, 1.0f);
				p.mDist	=UtilityLib.Mathery.VecIdx(bnd.mMaxs, i);

				GBSPSide	side	=new GBSPSide();
				side.mPlaneNum		=pp.FindPlane(p, out side.mPlaneSide);

				UtilityLib.Mathery.VecIdxAssign(ref p.mNormal, i, -1.0f);
				p.mDist	=-(UtilityLib.Mathery.VecIdx(bnd.mMins, i));

				GBSPSide	side2	=new GBSPSide();
				side2.mPlaneNum		=pp.FindPlane(p, out side2.mPlaneSide);

				side.FixFlags();
				side2.FixFlags();

				mOriginalSides.Add(side);
				mOriginalSides.Add(side2);
			}

			MakePolys(pp);
			FixContents();
		}


		internal MapBrush(PlanePool pp, List<int> planeNums, List<sbyte> sides)
		{
			for(int i=0;i < planeNums.Count;i++)
			{
				GBSPSide	side	=new GBSPSide();

				side.mPlaneNum	=planeNums[i];
				side.mPlaneSide	=sides[i];

				mOriginalSides.Add(side);
			}
			MakePolys(pp);
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


		internal bool ReadVMFSolidBlock(StreamReader sr, PlanePool pool, TexInfoPool tiPool, int entityNum)
		{
			string	s	="";
			bool	ret	=true;
			while((s = sr.ReadLine()) != null)
			{
				s	=s.Trim();
				if(s == "side")
				{
					GBSPSide	side	=new GBSPSide();
					mContents	=side.ReadVMFSideBlock(sr, pool, tiPool);

					if(mContents == Contents.CONTENTS_AUX)
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


		internal bool ReadFromMap(StreamReader sr, PlanePool pool, TexInfoPool tiPool, int entityNum)
		{
			string	s	="";
			bool	ret	=true;
			while((s = sr.ReadLine()) != null)
			{
				s	=s.Trim();
				if(s.StartsWith("("))
				{
					GBSPSide	side	=new GBSPSide();
					mContents	=side.ReadMapLine(s, pool, tiPool);

					if(mContents == Contents.CONTENTS_AUX)
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
			}
			return	ret;
		}


		public void Read(BinaryReader br)
		{
			mEntityNum	=br.ReadInt32();
			mContents	=br.ReadUInt32();

			mBounds	=new Bounds();

			mBounds.mMins.X	=br.ReadSingle();
			mBounds.mMins.Y	=br.ReadSingle();
			mBounds.mMins.Z	=br.ReadSingle();
			mBounds.mMaxs.X	=br.ReadSingle();
			mBounds.mMaxs.Y	=br.ReadSingle();
			mBounds.mMaxs.Z	=br.ReadSingle();

			int	numSides	=br.ReadInt32();
			for(int i=0;i < numSides;i++)
			{
				GBSPSide	s	=new GBSPSide();
				s.Read(br);

				mOriginalSides.Add(s);
			}
		}


		public void Write(BinaryWriter bw)
		{
			bw.Write(mEntityNum);
			bw.Write(mContents);
			
			bw.Write(mBounds.mMins.X);
			bw.Write(mBounds.mMins.Y);
			bw.Write(mBounds.mMins.Z);
			bw.Write(mBounds.mMaxs.X);
			bw.Write(mBounds.mMaxs.Y);
			bw.Write(mBounds.mMaxs.Z);

			bw.Write(mOriginalSides.Count);

			foreach(GBSPSide s in mOriginalSides)
			{
				s.Write(bw);
			}
		}
		#endregion


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

				for(int j=0;j < mOriginalSides.Count && p.VertCount() != 0;j++)
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

				if(p.VertCount() > 2)
				{
					side.mFlags	|=GBSPSide.SIDE_VISIBLE;
					p.AddToBounds(mBounds);
				}
			}

			for(int i=0;i < 3;i++)
			{
				if(UtilityLib.Mathery.VecIdx(mBounds.mMins, i) <= -Bounds.MIN_MAX_BOUNDS
					|| UtilityLib.Mathery.VecIdx(mBounds.mMaxs, i) >= Bounds.MIN_MAX_BOUNDS)
				{
					Map.Print("Entity " + mEntityNum + ", Brush bounds out of range\n");
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


		internal void GetLines(List<Vector3> tris, List<UInt32> ind, bool bCheckFlags)
		{
			foreach(GBSPSide s in mOriginalSides)
			{
				s.GetLines(tris, ind, bCheckFlags);
			}
		}


		internal void FixContents()
		{
			mContents	=Contents.FixContents(mContents);

			//fix faces as well
			//Force clip to solid/detail, and mark faces as not visible (they will get put last in the tree)
			if((mContents & Contents.BSP_CONTENTS_CLIP2) != 0)
			{
				for(int k=0;k < mOriginalSides.Count;k++)
				{
					mOriginalSides[k].mFlags	&=~GBSPSide.SIDE_VISIBLE;	// Clips won't have faces
				}
				mContents	|=Contents.BSP_CONTENTS_DETAIL2;			// Clips are allways detail
			}
			
			//if empty hide sides?
			if((mContents & Contents.BSP_CONTENTS_EMPTY2) != 0)
			{
				for(int k=0;k < mOriginalSides.Count;k++)
				{
					mOriginalSides[k].mFlags	&=~GBSPSide.SIDE_VISIBLE;
				}
			}
			
			if((mContents & Contents.BSP_CONTENTS_SHEET) != 0)
			{
				//Only the first side is visible for sheets
				mOriginalSides[0].mFlags	|=GBSPSide.SIDE_SHEET;
				
				//Sheets are allways detail!!!
				mContents	|=Contents.BSP_CONTENTS_DETAIL2;
			}
			
			//Force non-solid/non-hint to detail
			//if (!(Brush->Contents & BSP_CONTENTS_SOLID2))
			//	Brush->Contents |= BSP_CONTENTS_DETAIL2;
			
			//Convert all sides to hint if need so...
			if((mContents & Contents.BSP_CONTENTS_HINT2) != 0)
			{
				for(int k=0;k < mOriginalSides.Count;k++)
				{
					mOriginalSides[k].mFlags	|=GBSPSide.SIDE_HINT;
					mOriginalSides[k].mFlags	|=GBSPSide.SIDE_VISIBLE;
				}
				
				if((mContents & Contents.BSP_CONTENTS_DETAIL2) != 0)
				{
					mContents	&=~Contents.BSP_CONTENTS_DETAIL2;
				}
			}			
		}


		internal Bounds GetBounds()
		{
			return	mBounds;
		}
	}
}
