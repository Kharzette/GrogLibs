using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;


namespace BuildMap
{
	class BspNode
	{
		private bool		mbLeaf;
		private List<Face>	mFaces;
		private BspNode		mFront;
		private BspNode		mBack;
		private Vector3		mMins, mMaxs;
		private	Plane		mPlane;


		private bool FindGoodSplitPlane(List<Brush> brushList, out Plane bestPlane)
		{
			int		BestIndex	=-1;
			float	BestScore	=696969696.0f;

			foreach(Brush b in brushList)
			{
				float	score	=b.GetBestSplittingPlaneScore(brushList);

				if(score < BestScore)
				{
					BestScore	=score;
					BestIndex	=brushList.IndexOf(b);
				}
			}

			if(BestIndex > 0)
			{
				bestPlane	=brushList[BestIndex].GetBestSplittingPlane(brushList);
				return	true;
			}
			else
			{
				bestPlane	=new Plane();
				return	false;
			}
		}


		public void BuildTree(List<Brush> brushList)
		{
			List<Brush>	frontList	=new List<Brush>();
			List<Brush> backList	=new List<Brush>();


			if(!FindGoodSplitPlane(brushList, out mPlane))
			{
				//this is a leaf node
			}
			else
			{
				//split the entire list into front and back
				foreach(Brush b in brushList)
				{
					Brush	bf, bb;

					b.SplitBrush(mPlane, out bf, out bb);

					if(bb != null)
					{
						backList.Add(bb);
					}
					if(bf != null)
					{
						frontList.Add(bf);
					}
				}

				//make sure we actually split something here
				if(brushList.Count == (backList.Count + frontList.Count))
				{
					if(backList.Count == 0 || frontList.Count == 0)
					{
						Debug.Assert(false);// && "Got a bestplane but no splits!");
					}
				}


				//nuke original
				brushList.Clear();

				if(frontList.Count > 0)
				{
					mFront	=new BspNode();
					mFront.BuildTree(frontList);
				}
				else
				{
					Debug.Assert(false);// && "Nonleaf node with no front side brushes!");
				}

				if(backList.Count > 0)
				{
					mBack	=new BspNode();
					mBack.BuildTree(backList);
				}
				else
				{
					Debug.Assert(false);// && "Nonleaf node with no back side brushes!");
				}
			}
		}
	}
}
