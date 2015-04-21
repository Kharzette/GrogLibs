using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpDX;

using UtilityLib;


namespace BSPCore
{
	internal class TriBSP
	{
		GBSPPoly	mPoly;

		GBSPBrush	mVolume;

		int		mPlaneNum;
		bool	mbSide;

		TriBSP	mFront, mBack;


		internal void InsertVolume(GBSPBrush b, PlanePool pp, ClipPools cp)
		{
			GBSPBrush	front, back;
			b.Split(mPlaneNum, mbSide, 0, true, pp, out front, out back, false, cp);

			if(front != null)
			{
				if(mFront == null)
				{
#if EMPTY_BRUSHES
					mVolume	=front;
#endif
				}
				else
				{
					mFront.InsertVolume(front, pp, cp);
				}
			}

			if(back !=null)
			{
				if(mBack == null)
				{
					mVolume	=back;
				}
				else
				{
					mBack.InsertVolume(back, pp, cp);
				}
			}
		}


		internal void GetVolumes(List<GBSPBrush> brushes)
		{
			if(mVolume != null)
			{
				brushes.Add(mVolume);
			}

			if(mFront != null)
			{
				mFront.GetVolumes(brushes);
			}
			if(mBack != null)
			{
				mBack.GetVolumes(brushes);
			}
		}


		internal void MakeNode(List<Vector3> triangle, PlanePool pp)
		{
			GBSPPoly	pol	=new GBSPPoly(triangle[0], triangle[1], triangle[2]);

			MakeNode(pol, pp);
		}


		internal void MakeNode(GBSPPoly poly, PlanePool pp)
		{
			GBSPPlane	gp	=poly.GenPlane();

			mPlaneNum	=pp.FindPlane(gp, out mbSide);

			mPoly	=poly;
		}


		internal void Insert(List<Vector3> triangle, PlanePool pp)
		{
			GBSPPoly	gp	=new GBSPPoly(triangle[0], triangle[1], triangle[2]);

			Insert(gp, pp);
		}


		//build a leafy non solidy facey tree
		//not sure what these are called exactly
		void Insert(GBSPPoly gp, PlanePool pp)
		{
			GBSPPlane	plane	=pp.mPlanes[mPlaneNum];
			if(mbSide)
			{
				plane.Inverse();
			}

			int	frontCount, backCount;
			float	frontDist	=0f;
			float	backDist	=0f;
			gp.SplitSideTest(plane, out frontCount, out backCount,
				ref frontDist, ref backDist);

			if(frontCount == 0 && backCount == 0)
			{
				//coplanar
				GBSPPlane	polyPlane	=gp.GenPlane();

				float	dot	=polyPlane.mNormal.dot(plane.mNormal);
				if(dot > 0f)
				{
					if(mFront == null)
					{
						mFront	=new TriBSP();
						mFront.MakeNode(gp, pp);
					}
					else
					{
						mFront.Insert(gp, pp);
					}
				}
				else
				{
					if(mBack == null)
					{
						mBack	=new TriBSP();
						mBack.MakeNode(gp, pp);
					}
					else
					{
						mBack.Insert(gp, pp);
					}
				}
			}
			else if(frontCount > 0 && backCount > 0)
			{
				GBSPPoly	front	=null;
				GBSPPoly	back	=null;
				if(!gp.SplitEpsilon(0f, plane, out front, out back, false))
				{
					CoreEvents.Print("TriBSP::Insert():  Error splitting poly...\n");
				}

				if(mFront == null)
				{
					mFront	=new TriBSP();
					mFront.MakeNode(front, pp);
				}
				else
				{
					mFront.Insert(front, pp);
				}
				if(mBack == null)
				{
					mBack	=new TriBSP();
					mBack.MakeNode(back, pp);
				}
				else
				{
					mBack.Insert(back, pp);
				}
			}
			else if(frontCount > 0)
			{
				if(mFront == null)
				{
					mFront	=new TriBSP();
					mFront.MakeNode(gp, pp);
				}
				else
				{
					mFront.Insert(gp, pp);
				}
			}
			else if(backCount > 0)
			{
				if(mBack == null)
				{
					mBack	=new TriBSP();
					mBack.MakeNode(gp, pp);
				}
				else
				{
					mBack.Insert(gp, pp);
				}
			}
			else
			{
				//is this even possibru?
				Debug.Assert(false);
			}
		}
	}
}
