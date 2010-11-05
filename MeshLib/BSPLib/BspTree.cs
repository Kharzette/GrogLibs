using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BSPLib
{
	public class BspTree
	{
		//tree root
		BspNode	mRoot;
		int		mThreadCount;

		event EventHandler	eThreadDone;


		#region Constructors
		public BspTree(List<Brush> brushList, bool bBevel)
		{
			eThreadDone	+=OnThreadDone;

			if(!BuildThreaded(brushList, bBevel))
			{
				//register a progress on the number of brushes left
				object	prog	=ProgressWatcher.RegisterProgress(0,
					brushList.Count, brushList.Count - 1);

				mRoot	=new BspNode();
				mRoot.BuildTree(brushList, prog);
			}
		}


		void OnThreadDone(object sender, EventArgs ea)
		{
			mThreadCount--;
			if(mThreadCount == 0)
			{
				Map.Print("Bounding node brushes\n");

				mRoot.BoundNodeBrushes();

				if((bool)sender)
				{
					Map.Print("Beveling node brushes\n");
					mRoot.BevelNodeBrushes();
				}
			}
		}


		void CBBuildTree(object context)
		{
			BuildContext	bc	=context as BuildContext;

			//register a progress on the number of brushes left
			object	prog	=ProgressWatcher.RegisterProgress(0, bc.Brushes.Count, bc.Brushes.Count - 1);

			bc.This.BuildTree(bc.Brushes, prog);

			Map.Print("Tree build thread complete\n");

			if(eThreadDone != null)
			{
				eThreadDone(bc.mbBevel, null);
			}
		}


		bool BuildThreaded(List<Brush> brushList, bool bBevel)
		{
			List<Brush>	frontList	=new List<Brush>();
			List<Brush>	backList	=new List<Brush>();

			Face	face;

			mRoot	=new BspNode();

			Map.Print("Searching for the best initial split plane");

			object	prog	=ProgressWatcher.RegisterProgress(0, brushList.Count, 0);

			if(!mRoot.FindGoodSplitFace(brushList, out face, prog))			
			{
				Map.Print("Failed to find a good initial split plane!");
				return	false;
			}

			ProgressWatcher.DestroyProgress(prog);

			Map.Print("Found a good initial split plane, splitting entire list");

			mThreadCount	=2;

			mRoot.mPlane	=face.GetPlane();

			//split the entire list into front and back
			foreach(Brush b in brushList)
			{
				Brush	bf, bb;

				b.SplitBrush(face, out bf, out bb);

				if(bb != null)
				{
					if(bb.IsValid())
					{
						backList.Add(bb);
					}
				}
				if(bf != null)
				{
					if(bf.IsValid())
					{
						frontList.Add(bf);
					}
				}
			}

			Map.Print("Finished splitting");

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
				mRoot.mFront	=new BspNode();

				BuildContext	bc	=new BuildContext();
				bc.Brushes	=frontList;
				bc.This		=mRoot.mFront;
				bc.mbBevel	=bBevel;

				Map.Print("Spinning a front side thread\n");

				ThreadPool.QueueUserWorkItem(CBBuildTree, bc);
			}
			else
			{
				Debug.Assert(false);// && "Nonleaf node with no front side brushes!");
			}

			if(backList.Count > 0)
			{
				mRoot.mBack	=new BspNode();
				BuildContext	bc	=new BuildContext();
				bc.Brushes	=backList;
				bc.This		=mRoot.mBack;
				bc.mbBevel	=bBevel;

				Map.Print("Spinning a back side thread\n");

				ThreadPool.QueueUserWorkItem(CBBuildTree, bc);
			}
			else
			{
				Debug.Assert(false);// && "Nonleaf node with no back side brushes!");
			}

			Map.Print("Initial node complete, threads building");

			return	true;
		}
		#endregion


		#region Queries
		public Bounds GetBounds()
		{
			Bounds	bnd	=new Bounds();

			mRoot.AddToBounds(bnd);

			return	bnd;
		}


		public bool ClassifyPoint(Vector3 pnt)
		{
			return	mRoot.ClassifyPoint(pnt);
		}


		public BspNode GetRoot()
		{
			return	mRoot;
		}


		public void GetTriangles(List<Vector3> verts, List<UInt16> indexes)
		{
			mRoot.GetTriangles(verts, indexes);		
		}


		void GetPlanes(List<Plane> planes)
		{
			mRoot.GetPlanes(planes);
		}
		#endregion


		#region IO
		public void Write(BinaryWriter bw)
		{
			mRoot.Write(bw);
		}


		public void Read(BinaryReader br)
		{
			mRoot.Read(br);
		}
		#endregion
	}
}
