using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class GBSPPortal
	{
		public GBSPPoly		mPoly;							//Convex poly that holds the shape of the portal
		public GBSPNode		[]mNodes	=new GBSPNode[2];	//Node on each side of the portal
		public GBSPPortal	[]mNext		=new GBSPPortal[2];	//Next portal for each node
		public Int32		mPlaneNum;

		public GBSPNode	mOnNode;
		public GBSPFace	[]mFace	=new GBSPFace[2];
		public GBSPSide	mSide;
		public byte		mSideFound;


		internal GBSPPortal() { }
		internal GBSPPortal(GBSPPortal copyMe)
		{
			mPoly		=new GBSPPoly(copyMe.mPoly);
			mNodes[0]	=copyMe.mNodes[0];
			mNodes[1]	=copyMe.mNodes[1];
			mNext[0]	=copyMe.mNext[0];
			mNext[1]	=copyMe.mNext[1];
			mPlaneNum	=copyMe.mPlaneNum;
			mOnNode		=copyMe.mOnNode;
			mFace[0]	=copyMe.mFace[0];
			mFace[1]	=copyMe.mFace[1];
			mSide		=copyMe.mSide;
			mSideFound	=copyMe.mSideFound;
		}


		internal static GBSPPortal CreateOutsidePortal(GBSPPlane Plane, GBSPNode Node, GBSPNode outNode, PlanePool pool)
		{
			GBSPPortal	NewPortal;
			sbyte		Side;

			NewPortal	=new GBSPPortal();
			if(NewPortal == null)
			{
				return	null;
			}

			NewPortal.mPoly	=new GBSPPoly(Plane);
			if(NewPortal.mPoly == null || NewPortal.mPoly.IsTiny())
			{
				return	null;
			}
			NewPortal.mPlaneNum	=pool.FindPlane(Plane, out Side);

			if(NewPortal.mPlaneNum == -1)
			{
				Map.Print("CreateOutsidePortal:  Could not create plane.\n");
				return	null;
			}

			if(Side == 0)
			{
				if(!GBSPNode.AddPortalToNodes(NewPortal, Node, outNode))
				{
					return	null;
				}
			}
			else
			{
				if(!GBSPNode.AddPortalToNodes(NewPortal, outNode, Node))
				{
					return	null;
				}
			}
			return	NewPortal;
		}


		internal GBSPFace FaceFromPortal(Int32 PSide)
		{
			GBSPFace	f;
			GBSPSide	Side;

			Int32	NotPSide	=(PSide == 0)? 1 : 0;

			Side	=mSide;			
			if(Side == null)
			{
				return	null;	//Portal does not bridge different visible contents
			}

			if(((mNodes[PSide].mContents & GBSPBrush.BSP_CONTENTS_WINDOW2) != 0)
				&& VisibleContents(mNodes[NotPSide].mContents
				^ mNodes[PSide].mContents) == GBSPBrush.BSP_CONTENTS_WINDOW2)
			{
				return	null;
			}

			f	=new GBSPFace();

//			if(Side.mTexInfo >= NumTexInfo || Side.mTexInfo < 0)
//			{
//				Map.Print("*WARNING* FaceFromPortal:  Bad texinfo.\n");
//			}

			f.mTexInfo		=Side.mTexInfo;
			f.mPlaneNum		=Side.mPlaneNum;
			f.mPlaneSide	=PSide;
			f.mPortal		=this;
			f.mVisible		=1;

			if(PSide != 0)
			{
				f.mPoly	=new GBSPPoly(mPoly);
				f.mPoly.Reverse();
			}
			else
			{
				f.mPoly	=new GBSPPoly(mPoly);
			}

			return f;
		}


		static UInt32 VisibleContents(UInt32 Contents)
		{
			Int32	j;
			UInt32	MajorContents;

			if(Contents == 0)
			{
				return 0;
			}

			//Only check visible contents
			Contents	&=GBSPBrush.BSP_VISIBLE_CONTENTS;
			
			//Return the strongest one, return the first lsb
			for(j=0;j < 32;j++)
			{
				MajorContents	=(Contents & (UInt32)(1<<j));

				if(MajorContents != 0)
				{
					return	MajorContents;
				}
			}

			return 0;
		}	


		internal void FindPortalSide(Int32 PSide, PlanePool pool)
		{
			UInt32		VisContents, MajorContents;
			GBSPBrush	Brush;
			Int32		j;
			Int32		PlaneNum;
			GBSPSide	BestSide;
			float		Dot, BestDot;
			GBSPPlane	p1, p2;

			//First, check to see if the contents are intersecting sheets (special case)
			if(((mNodes[0].mContents & GBSPBrush.BSP_CONTENTS_SHEET) != 0)
				&& ((mNodes[1].mContents & GBSPBrush.BSP_CONTENTS_SHEET) != 0))
			{
				//The contents are intersecting sheets, so or them together
				VisContents	=mNodes[0].mContents | mNodes[1].mContents;
			}
			else
			{
				//Make sure the contents on both sides are not the same
				VisContents	=mNodes[0].mContents ^ mNodes[1].mContents;
			}

			//There must be a visible contents on at least one side of the portal...
			MajorContents	=VisibleContents(VisContents);

			if(MajorContents == 0)
			{
				return;
			}

			PlaneNum	=mOnNode.mPlaneNum;
			BestSide	=null;
			BestDot		=0.0f;
			for(j=0;j < 2;j++)
			{
				GBSPNode	Node	=mNodes[j];

				p1	=pool.mPlanes[mOnNode.mPlaneNum];

				for(Brush=Node.mBrushList;Brush != null;Brush=Brush.mNext)
				{
					MapBrush	MapBrush;

					MapBrush	=Brush.mOriginal;

					//Only use the brush that contains a major contents (solid)
					if((MapBrush.mContents & MajorContents) == 0)
					{
						continue;
					}

					foreach(GBSPSide pSide in MapBrush.mOriginalSides)
					{
						if((pSide.mFlags & GBSPSide.SIDE_NODE) != 0)
						{
							continue;		// Side not visible (result of a csg'd topbrush)
						}

						//First, Try an exact match
						if(pSide.mPlaneNum == PlaneNum)
						{	
							BestSide	=pSide;

							goto GotSide;
						}

						//In the mean time, try for the closest match
						p2	=pool.mPlanes[pSide.mPlaneNum];
						Dot	=Vector3.Dot(p1.mNormal, p2.mNormal);
						if(Dot > BestDot)
						{
							BestDot		=Dot;
							BestSide	=pSide;
						}
					}
				}
			}

			GotSide:

			if(BestSide == null)
			{
				Map.Print("WARNING: Could not map portal to original brush...\n");
			}

			mSideFound	=1;
			mSide		=BestSide;
		}


		internal bool Check()
		{
			if(mPoly.mVerts.Count < 3)
			{
				Map.Print("CheckPortal:  NumVerts < 3.\n");
				return	false;
			}

			for(int i=0;i < mPoly.mVerts.Count;i++)
			{
				for(int k=0;k < 3;k++)
				{
					float	val	=UtilityLib.Mathery.VecIdx(mPoly.mVerts[i], k);

					if(val == Brush.MIN_MAX_BOUNDS)
					{
						Map.Print("CheckPortal:  Portal was not clipped on all sides!!!\n");
						return	false;
					}
					if(val == -Brush.MIN_MAX_BOUNDS)
					{
						Map.Print("CheckPortal:  Portal was not clipped on all sides!!!\n");
						return	false;
					}
				}
			}
			return	true;
		}
	}
}
