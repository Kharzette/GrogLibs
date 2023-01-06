//#define EMPTY_BRUSHES
using System;
using System.Linq;
using System.Text;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;

using UtilityLib;


namespace BSPCore
{
	internal class TriBSP
	{
		GBSPPoly	mPoly;

		GBSPBrush	mVolume;

		int		mPlaneNum	=-1;
		bool	mbSide;

		TriBSP	mFront, mBack;

		const int	BlockSize	=2048;
		const float	fBlockSize	=2048f;


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


		internal void Free()
		{
			if(mFront != null)
			{
				mFront.Free();
			}
			if(mBack != null)
			{
				mBack.Free();
			}

			if(mPoly != null)
			{
				mPoly.Free();
			}
			if(mVolume != null)
			{
				mVolume.Free();
			}
		}


		internal void GetVolumes(List<GBSPBrush> brushes)
		{
			if(mVolume != null)
			{
				//clone these so the tree can be freed without freeing brushes
				brushes.Add(new GBSPBrush(mVolume));
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

			gp.Snap();

			mPlaneNum	=pp.FindPlane(gp, out mbSide);

			mPoly	=poly;
		}


		internal void Insert(List<Vector3> triangle, PlanePool pp)
		{
			GBSPPoly	gp	=new GBSPPoly(triangle[0], triangle[1], triangle[2]);

			Insert(gp, pp);
		}


		//get the tree ready with block planes
		//to sort of octree the top nodes
		internal void PrepareTree(Bounds bnd, PlanePool pp)
		{
			int	kMinX	=(int)Math.Floor(bnd.mMins.X / fBlockSize);
			int	kMinZ	=(int)Math.Floor(bnd.mMins.Z / fBlockSize);
			int	kMaxX	=(int)Math.Ceiling(bnd.mMaxs.X / fBlockSize);
			int	kMaxZ	=(int)Math.Ceiling(bnd.mMaxs.Z / fBlockSize);

			Array	blockNodes	=Array.CreateInstance(typeof(TriBSP),
				(kMaxX - kMinX) + 1,
				(kMaxZ - kMinZ) + 1);

			ClipPools	cp	=new ClipPools();
			for(int z=kMinZ;z < kMaxZ;z++)
			{
				for(int x=kMinX;x < kMaxX;x++)
				{
					ProcessBlock(pp, x, z, cp);
				}
			}
		}


		void ProcessBlock(PlanePool pp, int xblock, int zblock, ClipPools cp)
		{
			Bounds	blockBounds	=new Bounds();

			blockBounds.mMins.X	=xblock * BlockSize;
			blockBounds.mMins.Z	=zblock * BlockSize;
			blockBounds.mMins.Y	=-4096;
			blockBounds.mMaxs.X	=(xblock + 1) * BlockSize;
			blockBounds.mMaxs.Z	=(zblock + 1) * BlockSize;
			blockBounds.mMaxs.Y	=4096;

			MapBrush	mb	=new MapBrush(blockBounds, pp, cp);
			GBSPBrush	gb	=new GBSPBrush(mb, pp);

			gb.TriTreeInsert(this, pp);
		}


		//build a leafy non solidy facey tree
		//not sure what these are called exactly
		internal void Insert(GBSPPoly gp, PlanePool pp)
		{
			if(mPlaneNum == -1)
			{
				MakeNode(gp, pp);
				return;
			}

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
