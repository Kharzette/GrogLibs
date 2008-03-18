using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
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


		public void MergeBounds(Bounds b1, Bounds b2)
		{
			if(b1 != null)
			{
				AddPointToBounds(b1.mMins);
				AddPointToBounds(b1.mMaxs);
			}
			if(b2 != null)
			{
				AddPointToBounds(b2.mMins);
				AddPointToBounds(b2.mMaxs);
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

			mRoot.BoundNodes();
			/*
			foreach(Brush b in brushList)
			{
				mRoot.AddBrushToTree(b);
			}
			mRoot.MarkLeafs();
			*/
		}


		public void WriteToFile(BinaryWriter bw)
		{
			mRoot.WriteToFile(bw);
		}


		public void BuildPortals()
		{
			mRoot.BuildPortals();
		}


		public bool ClassifyPoint(Vector3 pnt)
		{
			return	mRoot.ClassifyPoint(pnt);
		}


		public int GetFirstBSPSurface(out Vector3[] surfPoints)
		{
			BspNode.gCount	=0;
			return	mRoot.GetFirstBSPSurface(out surfPoints);
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


		public void BuildVertexInfo(GraphicsDevice g)
		{
			mRoot.BuildVertexInfo(g);
		}


		public void Light(GraphicsDevice g, Vector3 lightPos, float lightVal, Vector3 clr)
		{
			mRoot.Light(g, mRoot, lightPos, lightVal, clr);
		}
	}
}
