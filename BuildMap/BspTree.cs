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
		private	BspNode	mOutsideNode;


		public BspTree(List<Brush> brushList)
		{
			foreach(Brush b in brushList)
			{
				//check for null volumes
			}

			mRoot	=new BspNode();
			mRoot.BuildTree(brushList);

			mOutsideNode	=new BspNode();
			mRoot.CreatePortals(mOutsideNode);
			/*
			foreach(Brush b in brushList)
			{
				mRoot.AddBrushToTree(b);
			}*/
		}


		public void Draw(GraphicsDevice g, Vector3 camPos)
		{
			mRoot.Draw(g, camPos);
		}
	}
}
