using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BuildMap
{
	public struct Bounds
	{
		public	const float	MIN_MAX_BOUNDS	=15192.0f;
		public	const int	MAX_BSP_MODELS	=2048;
		public	const int	PSIDE_FRONT		=1;
		public	const int	PSIDE_BACK		=2;
		public	const int	PSIDE_BOTH		=(PSIDE_FRONT|PSIDE_BACK);
		public	const int	PSIDE_FACING	=4;
		public	const int	PLANENUM_LEAF	=-1;
		public	const int	MAX_BSP_PLANES	=32000;
		public	const float	DIST_EPSILON	=0.01f;
		public	const float	ANGLE_EPSILON	=0.00001f;
		public	const int	SIDE_HINT		=(1<<0);	//Side is a hint side
		public	const int	SIDE_SHEET		=(1<<1);	//Side is a sheet (only visible face in a sheet contents)
		public	const int	SIDE_VISIBLE	=(1<<2);	// 
		public	const int	SIDE_TESTED		=(1<<3);	// 
		public	const int	SIDE_NODE		=(1<<4);	// 
		public	const int	MAX_WELDED_VERTS=64000;
		Vector3	mMins, mMaxs;		
		
		public void ClearBounds()
		{
			mMins.X	=mMins.Y =	mMins.Z	=MIN_MAX_BOUNDS;
			mMaxs	=-mMins;
		}

		public void AddPointToBounds(Vector3 pnt)
		{
			if(pnt.X < mMins.X)
			{
				mMins.X	=pnt.X;
			}
			if(pnt.X > mMaxs.X)
			{
				mMaxs.X	=pnt.X;
			}
			if(pnt.Y < mMins.Y)
			{
				mMins.Y	=pnt.Y;
			}
			if(pnt.Y > mMaxs.Y)
			{
				mMaxs.Y	=pnt.Y;
			}
			if(pnt.Z < mMins.Z)
			{
				mMins.Z	=pnt.Z;
			}
			if(pnt.Z > mMaxs.Z)
			{
				mMaxs.Z	=pnt.Z;
			}
		}
	}

	class BspTree
	{
		private	BspNode	mRoot;


		public BspTree(List<Brush> brushList)
		{
			foreach(Brush b in brushList)
			{
				//check for null volumes
			}

			mRoot	=new BspNode();
			mRoot.BuildTree(brushList);
			/*
			foreach(Brush b in brushList)
			{
				mRoot.AddBrushToTree(b);
			}
			mRoot.MarkLeafs();
			*/
		}


		public bool ClassifyPoint(Vector3 pnt)
		{
			return	mRoot.ClassifyPoint(pnt);
		}


		public void Draw(GraphicsDevice g, Vector3 camPos)
		{
			mRoot.Draw(g, camPos);
		}
	}
}
