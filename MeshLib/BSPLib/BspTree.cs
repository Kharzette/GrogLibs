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


		internal Dictionary<BspNode, List<Portal>> Portalize()
		{
			Dictionary<BspNode, List<Portal>>	portals	=new Dictionary<BspNode, List<Portal>>();

			portals.Add(mOutsideNode, new List<Portal>());

			Bounds	rootBounds	=mRoot.mBounds;

			//create outside node portals
			List<Portal>	outPorts	=new List<Portal>();
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
					p.mDistance	=Vector3.Dot(mRoot.mBounds.mMins, p.mNormal) - 128.0f;
				}

				Face	f		=new Face(p, null);
				Portal	port	=new Portal();

				port.mFace			=f;
				port.mFront			=mOutsideNode;
				port.mbFrontFront	=false;

				outPorts.Add(port);
			}

			//clip these against each other
			for(int i=0;i < 6;i++)
			{
				for(int j=0;j < 6;j++)
				{
					if(i==j)
					{
						continue;
					}
					outPorts[j].mFace.ClipByFace(outPorts[i].mFace, false, true);
				}
			}

			//filter outside portals
			for(int i=0;i < 6;i++)
			{
				Portal	port	=outPorts[i];

				List<Portal>	pieces	=new List<Portal>();

				mRoot.FilterPortalOutside(port, pieces);

				foreach(Portal piece in pieces)
				{
					portals[mOutsideNode].Add(piece);

					if(!portals.ContainsKey(piece.mBack))
					{
						portals.Add(piece.mBack, new List<Portal>());
					}
					portals[piece.mBack].Add(piece);
				}
			}

			//grab list of splitting planes
			//
			//make portal and filter each one
			/*
			List<Face>	bspFaces	=new List<Face>();
			mRoot.GetFaces(bspFaces);

			foreach(Face f in bspFaces)
			{
				Face	portFace	=new Face(f, false);
				Portal	port		=new Portal();

				port.mPortalFace	=portFace;

				List<Portal>	pieces	=new List<Portal>();

				mRoot.FilterPortalFront(port, pieces);

				List<Portal>	backPieces	=new List<Portal>();
				foreach(Portal piece in pieces)
				{
					mRoot.FilterPortalBack(piece, backPieces, false);
				}

				foreach(Portal piece in backPieces)
				{
					if(!portals.ContainsKey(piece.mFront))
					{
						portals.Add(piece.mFront, new List<Portal>());
					}
					portals[piece.mFront].Add(piece);

					if(!portals.ContainsKey(piece.mBack))
					{
						portals.Add(piece.mBack, new List<Portal>());
					}
					portals[piece.mBack].Add(piece);
				}
			}*/

			return	portals;
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
			mRoot.GetTriangles(verts, indexes, bCheckFlags);
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

		internal void CheckForLeak(Dictionary<BspNode, List<Portal>> portals,
			Dictionary<BspNode, List<Entity>> nodeEnts)
		{
			/*
			BspNode	node	=mOutsideNode;
			while(node != null)
			{
				foreach(Portal port in portals[node])
				{
					Portal	next	=port;

					while(next != null)
					{
						if(port.mBackContents != 0)
						{
							continue;
						}

						//this portal looks into the empty part of the node
						if(nodeEnts.ContainsKey(port.mBack))
						{
							Vector3	org	=Vector3.Zero;

							foreach(Entity e in nodeEnts[port.mBack])
							{
								if(!e.GetOrigin(out org))
								{
									continue;
								}
								//see if portal looks to the side
								//where the entity lies
								Face	side	=port.mBack.mBrush.GetSideInFrontOf(org);
								if(side == port.mBrushFace)
								{
									Map.Print("Leak detected near " + port.GetCentroid() + "\n");
									BspNode.TroubleBrushes.Add(port.mBack.mBrush);
									return;
								}
							}
						}
					}
				}
			}*/
		}
	}
}
