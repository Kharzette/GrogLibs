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
	class ThreadCounter
	{
		public int mThreadCount;
	}


	public class BspTree
	{
		//tree root
		BspNode			mRoot, mOutsideNode;
		ThreadCounter	mThreadCount	=new ThreadCounter();

		//debug draw portal stuff
		List<Brush>			mEmptySpace		=new List<Brush>();
		List<Portal>		mPortals		=new List<Portal>();
		List<Face>			mEmptyToSolid	=new List<Face>();
		List<Face>			mEmptyToEmpty	=new List<Face>();
		public List<BrushPortal>	mBrushPortals	=new List<BrushPortal>();
		List<Brush>			mFlooded		=new List<Brush>();
		List<Face>			mTroubleFaces	=new List<Face>();
		List<Face>			mVisibleFaces	=new List<Face>();

		event EventHandler	eThreadDone;

		public event EventHandler	eBuildComplete;


		#region Constructors
		public BspTree() { }
		public BspTree(List<Brush> brushList, bool bBevel)
		{
			eThreadDone	+=OnThreadDone;

			mOutsideNode	=new BspNode();

			if(!BuildThreaded(brushList, bBevel))
			{
				//register a progress on the number of brushes left
				object	prog	=ProgressWatcher.RegisterProgress(0,
					brushList.Count, brushList.Count - 1);

				mRoot	=new BspNode();
				mRoot.BuildTree(brushList, prog);
			}
		}
		#endregion


		void OnThreadDone(object sender, EventArgs ea)
		{
			int	threadCount;
			lock(mThreadCount)
			{
				mThreadCount.mThreadCount--;
				threadCount	=mThreadCount.mThreadCount;
			}

			if(threadCount == 0)
			{
				Map.Print("Bounding node brushes\n");
				mRoot.Bound();
				Map.Print("Bounding complete\n");

				Map.Print("Not beveling node brushes because I ganked that\n");

				if(eBuildComplete != null)
				{
					eBuildComplete(null, null);
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

			if(!BspNode.FindGoodSplitFace(brushList, out face, prog))			
			{
				Map.Print("Failed to find a good initial split plane!");
				return	false;
			}

			ProgressWatcher.DestroyProgress(prog);

			Map.Print("Found a good initial split plane, splitting entire list");

			lock(mThreadCount)
			{
				mThreadCount.mThreadCount	=2;
			}

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
//					Debug.Assert(false);// && "Got a bestplane but no splits!");
					return	false;
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


		internal void Portalize()
		{
			Bounds	rootBounds	=mRoot.mBounds;

			//create outside node brush volume
			Brush	outsideBrush	=new Brush();

			List<Face>	obFaces	=new List<Face>();
			for(int i=0;i < 6;i++)
			{
				Plane	p;
				p.mNormal	=UtilityLib.Mathery.AxialNormals[i];

				if(i < 3)
				{
					p.mDistance	=Vector3.Dot(mRoot.mBounds.mMaxs, p.mNormal) + 128.0f;
				}
				else
				{
					p.mDistance	=Vector3.Dot(mRoot.mBounds.mMins, p.mNormal) + 128.0f;
				}

				Face	f		=new Face(p, null);
				obFaces.Add(f);
			}

			//clip brush faces against each other
			for(int i=0;i < 6;i++)
			{
				for(int j=0;j < 6;j++)
				{
					if(i==j)
					{
						continue;
					}
					obFaces[j].ClipByFace(obFaces[i], false, true);
				}
			}

			outsideBrush.AddFaces(obFaces);

			List<Brush>	inList	=new List<Brush>();

			inList.Add(outsideBrush);

			//merge the outside volume into the empty
			//space of the tree
			mRoot.MergeBrush(inList, mEmptySpace);

			//grab brush faces
			List<Face>	emptySpaceFaces	=new List<Face>();
			foreach(Brush b in mEmptySpace)
			{
				//set empty
				b.SetContents(Brush.CONTENTS_EMPTY);

				List<Face>	brushFaces	=b.GetFaces();

				//clone
				foreach(Face f in brushFaces)
				{
					emptySpaceFaces.Add(new Face(f));
				}
			}

			//get all faces that land on a single
			//brush face, these are the empty to
			//solid portals
			mEmptyToSolid	=new List<Face>();

			//faces that land in two brushes are
			//empty to empty portals
			mEmptyToEmpty	=new List<Face>();
			foreach(Face f in emptySpaceFaces)
			{
				BrushPortal	bp	=new BrushPortal();

				int	onCount			=0;
				int	onOppositeCount	=0;
				foreach(Brush b in mEmptySpace)
				{
					if(b.IsFaceOnBrush(f))
					{
						onCount++;
					}
					if(b.IsFaceOnBrushOpposite(f))
					{
						onOppositeCount++;
						bp.mConnections.Add(b);
					}
				}

				if(onCount > 0 && onOppositeCount == 0)
				{
					mEmptyToSolid.Add(f);
				}
				else if(onCount > 0 && onOppositeCount > 0)
				{
					mEmptyToEmpty.Add(f);

					mBrushPortals.Add(bp);

					foreach(Brush b in bp.mConnections)
					{
						b.mPortals.Add(bp);
					}
				}
				else
				{
					Map.Print("Adding a face to trouble faces...\n");
					mTroubleFaces.Add(f);
				}
			}

			//mark outside portals
			for(int i=0;i < 6;i++)
			{
				Plane	p;
				p.mNormal	=UtilityLib.Mathery.AxialNormals[i];

				if(i < 3)
				{
					p.mDistance	=Vector3.Dot(mRoot.mBounds.mMaxs, p.mNormal) + 128.0f;
				}
				else
				{
					p.mDistance	=Vector3.Dot(mRoot.mBounds.mMins, p.mNormal) + 128.0f;
				}

				foreach(Brush b in mEmptySpace)
				{
					if(b.ContainsPlane(p))
					{
						b.SetContents(Brush.CONTENTS_SOLID);
					}
				}
			}
		}


		internal Brush GetBrushLandedIn(Vector3 pos)
		{
			foreach(Brush b in mEmptySpace)
			{
				if(b.IsPointInside(pos))
				{
					return	b;
				}
			}
			return	null;
		}


		#region Queries
		public Bounds GetBounds()
		{
			return	mRoot.mBounds;
		}


		public bool ClassifyPoint(Vector3 pnt)
		{
			return	mRoot.ClassifyPoint(pnt);
		}


		public BspNode GetRoot()
		{
			return	mRoot;
		}


		public void GetTriangles(List<Vector3> verts, List<UInt32> indexes, bool bCheckFlags)
		{
//			foreach(Face f in mEmptyToEmpty)
//			{
//				f.GetTriangles(verts, indexes);
//			}
//			foreach(Brush b in mFlooded)
//			{
//				b.GetTriangles(verts, indexes, false);
//			}
//			foreach(Face f in mVisibleFaces)
//			{
//				f.GetTriangles(verts, indexes);
//			}
//			foreach(Face f in mTroubleFaces)
//			{
//				f.GetTriangles(verts, indexes);
//			}
			foreach(Face f in mEmptyToSolid)
			{
				f.GetTriangles(verts, indexes);
			}
//			foreach(Brush b in mEmptySpace)
//			{
//				b.GetTriangles(verts, indexes, bCheckFlags);
//			}
//			mRoot.GetTriangles(verts, indexes, bCheckFlags);
		}
		#endregion


		#region IO
		public void Write(BinaryWriter bw)
		{
			mRoot.Write(bw);
		}


		public void Read(BinaryReader br)
		{
			mRoot	=new BspNode();
			mRoot.Read(br);
		}
		#endregion


		public bool MoveLine(ref Line ln)
		{
			return	mRoot.MoveLine(ref ln);
		}


		public bool RayCast(Vector3 p1, Vector3 p2, ref List<ClipSegment> segs)
		{
			Line	ln	=new Line();
			ln.mP1	=p1;
			ln.mP2	=p2;

			mRoot.RayCastBrushes(ln, ref segs);
			return	(segs.Count <= 0);
		}


		internal void RayCast3(Vector3 mStart, Vector3 mEnd, List<Ray> rayParts)
		{
			mRoot.RayCast3(mStart, mEnd, rayParts);
		}


		internal bool MoveLine(ref Line ln, float radius)
		{
			return	mRoot.MoveLine(ref ln, radius);
		}


		internal BspNode GetNodeLandedIn(Vector3 org)
		{
			return	mRoot.GetNodeLandedIn(org);
		}


		internal bool CheckForLeak(Dictionary<Brush, List<Entity>> brushEnts)
		{
			bool	ret	=false;
			foreach(KeyValuePair<Brush, List<Entity>> brushEnt in brushEnts)
			{
				brushEnt.Key.Flood(mFlooded);
				foreach(Brush flood in mFlooded)
				{
					if((flood.GetContents() & Brush.CONTENTS_SOLID) != 0)
					{
						Vector3	org	=Vector3.Zero;
						brushEnt.Value[0].GetOrigin(out org);
						Map.Print("Leak found near: " + org + "!!!\n");
						ret	=true;
					}
				}
			}
			return	ret;
		}


		//use previously flooded info
		internal void RemoveHiddenFaces()
		{
			mVisibleFaces	=new List<Face>();
			foreach(Brush b in mFlooded)
			{
				List<Face>	floodFaces	=b.GetFaces();

				foreach(Face floodFace in floodFaces)
				{
					foreach(Face e2sFace in mEmptyToSolid)
					if(floodFace.CompareEpsilon(e2sFace, 0.001f))
					{
						mVisibleFaces.Add(new Face(e2sFace));
					}
				}
			}
		}
	}
}
