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

					if(mContents == 0)
					{
						//set to solid by default?
						mContents	=Brush.CONTENTS_SOLID;
					}
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
				s.GetTriangles(tris, ind);
			}
		}
	}
}
