using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class BspFlatNode
	{
		public Plane		mPlane;
		public BspFlatNode	mFront, mBack, mParent;
		public List<Face>	mFaces	=new List<Face>();
		public Bounds		mBounds;
		public List<Portal>	mPortals	=new List<Portal>();
		public bool			mbLeaf;
		public UInt32		mBrushContents;


		void MakeLeaf(List<Brush> brushList)
		{
			if(brushList.Count != 1)
			{
				Map.Print("Merging " + brushList.Count + " brushes in a leaf");
			}

			//copy in the brush faces
			//merge multiples
			Brush	merge	=new Brush();
			foreach(Brush b in brushList)
			{
				merge.AddFaces(b.GetFaces());
				merge.SetContents(b.GetContents());
			}
			merge.SealFaces();
			if(!merge.IsValid())
			{
				//tiny removal broke something
				Map.Print("Bad brush in leaf!");
				Map.Print("Brush is hozed, compile will likely fail...");
				lock(BspNode.TroubleBrushes)
				{
					foreach(Brush b in brushList)
					{
						BspNode.TroubleBrushes.Add(b);
					}
				}

				//brush is messed up, donut add it
				return;
			}

			List<Face>	brushFaces	=merge.GetFaces();
			BspFlatNode	parent		=this.mParent;
			BspFlatNode	node		=this;
			foreach(Face f in brushFaces)
			{
				//skip faces coplanar (even opposite)
				if(parent != null && parent.mPlane.IsCoPlanar(f.GetPlane()))
				{
					parent.mFaces.Add(new Face(f));
					continue;
				}
				node.mFaces.Add(new Face(f));
				node.mParent	=parent;
				node.mPlane		=f.GetPlane();

				//create front side leaf
//				node.mFront					=new BspFlatNode();
//				node.mFront.mbLeaf			=true;
//				node.mFront.mBrushContents	=0;

				//advance to next face / node
				parent		=node;
				node.mBack	=new BspFlatNode();
				node		=node.mBack;
			}
			node.mbLeaf			=true;
			node.mBrushContents	=merge.GetContents();
		}


		//builds a bsp good for portals
		internal void BuildTree(List<Brush> brushList)
		{
			List<Brush>	frontList	=new List<Brush>();
			List<Brush>	backList	=new List<Brush>();

			Face	face;

			if(!BspNode.FindGoodSplitFace(brushList, out face, null))
			{
				MakeLeaf(brushList);
				return;
			}

			mPlane	=face.GetPlane();

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

			//nuke original
			brushList.Clear();

			//check both lists to ensure
			//there's some valid space
			bool	bFrontValid	=false;
			bool	bBackValid	=false;
			if(frontList.Count > 0)
			{
				foreach(Brush b in frontList)
				{
					if(b.IsValid())
					{
						bFrontValid	=true;
					}
				}
			}
			if(backList.Count > 0)
			{
				foreach(Brush b in backList)
				{
					if(b.IsValid())
					{
						bBackValid	=true;
					}
				}
			}

			if(bFrontValid && !bBackValid)
			{
				//make a leaf from frontlist
				MakeLeaf(frontList);
				return;
			}
			else if(bBackValid && !bFrontValid)
			{
				MakeLeaf(backList);
				return;
			}
			else if(!bFrontValid && !bBackValid)
			{
				Debug.Assert(false);
			}

			if(frontList.Count > 0)
			{
				mFront	=new BspFlatNode();
				mFront.mParent	=this;
				mFront.BuildTree(frontList);
			}
			else
			{
				Debug.Assert(false);// && "Nonleaf node with no front side brushes!");
			}

			if(backList.Count > 0)
			{
				mBack	=new BspFlatNode();
				mBack.mParent	=this;
				mBack.BuildTree(backList);
			}
			else
			{
				Debug.Assert(false);// && "Nonleaf node with no back side brushes!");
			}
		}


		//do a filter that does no splits, just passes down
		//the sides until it hits a leaf
		internal void FilterPortal(Portal port)
		{
			Debug.Assert(!mbLeaf);

			int	pointsFront, pointsBack, pointsOn;
			port.mFace.GetSplitInfo(mPlane, out pointsFront, out pointsBack, out pointsOn);

			if(pointsFront > 0)
			{
				Debug.Assert(pointsBack == 0);
				mFront.FilterPortalFront(port);
			}
			else if(pointsBack > 0)
			{
				Debug.Assert(pointsFront == 0);
				mBack.FilterPortalBack(port);
			}
			else if(pointsOn > 0)
			{
				mFront.FilterPortalFront(port);
				mBack.FilterPortalBack(port);
			}
			else
			{
				Debug.Assert(false);
			}
		}


		internal void FilterPortalFront(Portal port)
		{
			if(mbLeaf)
			{
				Debug.Assert(port.mFront == null);
				port.mFront	=this;
				return;
			}

			int	pointsFront, pointsBack, pointsOn;
			port.mFace.GetSplitInfo(mPlane, out pointsFront, out pointsBack, out pointsOn);

			if(pointsFront > 0)
			{
				Debug.Assert(pointsBack == 0);
				if(mFront != null)
				{
					mFront.FilterPortalFront(port);
				}
				else
				{
					port.mFront	=this;
				}
			}
			else if(pointsBack > 0)
			{
				Debug.Assert(pointsFront == 0);
				mBack.FilterPortalFront(port);
			}
			else if(pointsOn > 0)
			{
				if(mFront != null)
				{
					mFront.FilterPortalFront(port);
				}
				else
				{
					port.mFront	=this;
				}
			}
		}


		internal void FilterPortalBack(Portal port)
		{
			if(mbLeaf)
			{
				Debug.Assert(port.mBack == null);
				port.mBack	=this;
				return;
			}

			int	pointsFront, pointsBack, pointsOn;
			port.mFace.GetSplitInfo(mPlane, out pointsFront, out pointsBack, out pointsOn);

			if(pointsFront > 0)
			{
				Debug.Assert(pointsBack == 0);
				if(mFront != null)
				{
					mFront.FilterPortalBack(port);
				}
				else
				{
					port.mBack	=this;
				}
			}
			else if(pointsBack > 0)
			{
				Debug.Assert(pointsFront == 0);
				mBack.FilterPortalBack(port);
			}
			else if(pointsOn > 0)
			{
				mBack.FilterPortalBack(port);
			}
		}


		internal void GetTriangles(List<Vector3> tris, List<UInt32> ind, bool bCheckFlags)
		{
			foreach(Face f in mFaces)
			{
				if(bCheckFlags)
				{
					if((f.mFlags & Face.SURF_SKIP) != 0)
					{
						continue;
					}
					if((f.mFlags & Face.SURF_NODRAW) != 0)
					{
						continue;
					}
					if((f.mFlags & Face.SURF_HINT) != 0)
					{
						continue;
					}
				}
				else
				{
					if((f.mFlags & Face.SURF_SKIP) != 0)
					{
						continue;
					}
				}
				f.GetTriangles(tris, ind);
			}

			if(mFront != null)
			{
				mFront.GetTriangles(tris, ind, bCheckFlags);
			}

			if(mBack != null)
			{
				mBack.GetTriangles(tris, ind, bCheckFlags);
			}
		}


		internal void GatherNonLeafNodes(List<BspFlatNode> nonLeafs)
		{
			if(mbLeaf)
			{
				return;
			}

			nonLeafs.Add(this);

			if(mFront != null)
			{
				mFront.GatherNonLeafNodes(nonLeafs);
			}
			if(mBack != null)
			{
				mBack.GatherNonLeafNodes(nonLeafs);
			}
		}


		internal void MergeBrush(List<Brush> inList, List<Brush> outList)
		{
			if(mbLeaf)
			{
				outList.AddRange(inList);
				return;
			}

			List<Brush>	frontList	=new List<Brush>();
			List<Brush>	backList	=new List<Brush>();
			Face		splitFace	=new Face(mPlane, null);
			foreach(Brush b in inList)
			{
				Brush	front, back;

				b.SplitBrush(splitFace, out front, out back);

				if(back != null)
				{
					if(back.IsValid())
					{
						backList.Add(back);
					}
				}
				if(front != null)
				{
					if(front.IsValid())
					{
						frontList.Add(front);
					}
				}
			}

			if(mFront != null)
			{
				mFront.MergeBrush(frontList, outList);
			}
			else
			{
				outList.AddRange(frontList);
			}
			if(mBack != null)
			{
				mBack.MergeBrush(backList, outList);
			}
		}


		internal void PartitionPortals()
		{
			if(mbLeaf)
			{
				return;
			}

			//create a portal on this node
			Portal	p	=new Portal();
			p.mFace		=new Face(mPlane, null);
			p.mOnNode	=this;

			//slice up the portals by all the other
			//portals attached to this node
			foreach(Portal slicer in mPortals)
			{
				Plane	split	=slicer.mFace.GetPlane();
				if(mPlane.IsCoPlanar(split))
				{
					continue;
				}

				p.mFace.ClipByPlane(split, this == slicer.mFront, true);
				if(p.mFace.IsTiny())
				{
					break;
				}
			}

			if(p.mFace.IsTiny())
			{
				Map.Print("Portal destroyed\n");
				return;
			}

			//add portal to front & back
			if(mFront != null)
			{
				p.mFront	=mFront;
				p.mFront.mPortals.Add(p);
			}
			if(mBack != null)
			{
				p.mBack		=mBack;
				p.mBack.mPortals.Add(p);
			}

			//split all the other portals
			//by the one we just created
			List<Portal>	nuke	=new List<Portal>();
			List<Portal>	adds	=new List<Portal>();

			BspFlatNode	node	=this;
			while(node != null)
			{
				foreach(Portal port in node.mPortals)
				{
					Portal	front, back;
					if(port.Split(mPlane, out front, out back))
					{
						nuke.Add(port);
						if(front != null)
						{
							adds.Add(front);
						}
						if(back != null)
						{
							adds.Add(back);
						}
					}
				}
				//blast split portals
				foreach(Portal port in nuke)
				{
					port.mBack.mPortals.Remove(port);
					if(port.mFront != null)
					{
						port.mFront.mPortals.Remove(port);
					}
				}

				//add split up portals to nodes
				foreach(Portal port in adds)
				{
					if(port.mFront != null)
					{
						port.mFront.mPortals.Add(port);
					}
					port.mBack.mPortals.Add(port);
				}

				nuke.Clear();
				adds.Clear();

				node	=node.mParent;
			}

			if(mFront != null)
			{
				mFront.PartitionPortals();
			}
			if(mBack != null)
			{
				mBack.PartitionPortals();
			}
		}


		internal void GatherPortals(List<Portal> portals)
		{
			if(mbLeaf)
			{
//				Debug.Assert(mPortals.Count == 0);
				portals.AddRange(mPortals);
				return;
			}

			portals.AddRange(mPortals);

			if(mFront != null)
			{
				mFront.GatherPortals(portals);
			}
			if(mBack != null)
			{
				mBack.GatherPortals(portals);
			}
		}


		internal void GatherNonLeafPlanes(List<Plane> planes)
		{
			if(mbLeaf)
			{
				return;
			}

			bool	bFound	=false;
			foreach(Plane p in planes)
			{
				if(p.IsCoPlanar(mPlane))
				{
					bFound	=true;
					break;
				}
			}

			if(!bFound)
			{
				planes.Add(mPlane);
			}

			if(mFront != null)
			{
				mFront.GatherNonLeafPlanes(planes);
			}
			if(mBack != null)
			{
				mBack.GatherNonLeafPlanes(planes);
			}
		}


		//make the node bounds
		internal Bounds Bound()
		{
			mBounds	=new Bounds();
			mBounds.AddPointToBounds(Vector3.Zero);

			if(mFaces.Count > 0)
			{
				foreach(Face f in mFaces)
				{
					f.AddToBounds(mBounds);
				}
			}

			if(mFront != null)
			{
				mBounds.MergeBounds(null, mFront.Bound());
			}

			if(mBack != null)
			{
				mBounds.MergeBounds(null, mBack.Bound());
			}

			return	mBounds;
		}


		internal BspFlatNode GetNodeLandedIn(Vector3 pnt)
		{
			float	d;

			if(mbLeaf)
			{
				return	this;
			}

			d	=Vector3.Dot(mPlane.mNormal, pnt) - mPlane.mDistance;

			if(d < 0.0f)
			{
				if(mBack != null)
				{
					BspFlatNode	landed	=mBack.GetNodeLandedIn(pnt);
					if(landed != null)
					{
						return	landed;
					}
				}
			}
			else
			{
				if(mFront != null)
				{
					BspFlatNode	landed	=mFront.GetNodeLandedIn(pnt);
					if(landed != null)
					{
						return	landed;
					}
				}
				else
				{
					return	this;
				}
			}
			return	null;
		}


		internal void AddPortal(Portal port)
		{
			mPortals.Add(port);
		}


		internal bool FloodToNode(BspFlatNode node, List<BspFlatNode> flooded)
		{
			if(node == this)
			{
				return	true;
			}

			if(flooded.Contains(this))
			{
				return	false;
			}
			flooded.Add(this);

			foreach(Portal port in mPortals)
			{
				if((port.mBack.mBrushContents & Brush.CONTENTS_SOLID) == 0)
				{
					if(port.mBack.FloodToNode(node, flooded))
					{
						return	true;
					}
				}
			}
			return	false;
		}


		internal void FloodFillEmpty(List<BspFlatNode> flooded)
		{
			if(flooded.Contains(this))
			{
				return;
			}
			flooded.Add(this);

			foreach(Portal port in mPortals)
			{
				if(port.mFront == this)
				{
					if((port.mBack.mBrushContents & Brush.CONTENTS_SOLID) == 0)
					{
						port.mBack.FloodFillEmpty(flooded);
					}
				}
				else
				{
					if(port.mFront != null &&
						(port.mFront.mBrushContents & Brush.CONTENTS_SOLID) == 0)
					{
						port.mFront.FloodFillEmpty(flooded);
					}
				}
			}
		}
	}
}
