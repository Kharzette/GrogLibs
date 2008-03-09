using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BuildMap
{
	public class Bounds
	{
		public	const float	MIN_MAX_BOUNDS	=15192.0f;
		public	const float	DIST_EPSILON	=0.01f;
		public	const float	ANGLE_EPSILON	=0.00001f;
		public	Vector3	mMins, mMaxs;


		public Bounds()
		{
			ClearBounds();
		}
		
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


		public void BuildPortals()
		{
			mRoot.BuildPortals();
		}


		public bool ClassifyPoint(Vector3 pnt)
		{
			return	mRoot.ClassifyPoint(pnt);
		}


		public void Draw(GraphicsDevice g, Effect fx, Vector3 camPos)
		{
			mRoot.Draw(g, fx, camPos);
		}


		public void DrawPortals(GraphicsDevice g, Effect fx, Vector3 camPos)
		{
			mRoot.DrawPortalsDumb(g, fx, camPos);
		}


		public BspNode GetRoot()
		{
			return	mRoot;
		}
	}
}
