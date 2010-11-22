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
				if(parent.mPlane.IsCoPlanar(f.GetPlane()))
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


		internal void FilterPortalFront(Portal port, List<Portal> pieces)
		{			
			if(mbLeaf)
			{
				port.mFront	=this;
				pieces.Add(port);
				return;
			}

			//split portal
			Portal	front		=new Portal();
			front.mFace			=new Face(port.mFace);
			front.mBack			=port.mBack;
			front.mFront		=port.mFront;
			front.mOnNode		=port.mOnNode;
			front.mFace.ClipByPlane(mPlane, true, true);
			if(!front.mFace.IsTiny())
			{
				mFront.FilterPortalFront(front, pieces);
			}

			Portal	back		=new Portal();
			back.mFace			=new Face(port.mFace);
			back.mBack			=port.mBack;
			back.mFront			=port.mFront;
			back.mOnNode		=port.mOnNode;
			back.mFace.ClipByPlane(mPlane, false, true);
			if(!back.mFace.IsTiny())
			{
				mBack.FilterPortalFront(back, pieces);
			}
		}


		internal void FilterPortalBack(Portal port, List<Portal> pieces)
		{			
			if(mbLeaf)
			{
				port.mBack	=this;
				pieces.Add(port);
				return;
			}

			//split portal
			Portal	front		=new Portal();
			front.mFace			=new Face(port.mFace);
			front.mBack			=port.mBack;
			front.mFront		=port.mFront;
			front.mOnNode		=port.mOnNode;
			front.mFace.ClipByPlane(mPlane, true, true);
			if(!front.mFace.IsTiny())
			{
				mFront.FilterPortalFront(front, pieces);
			}

			Portal	back		=new Portal();
			back.mFace			=new Face(port.mFace);
			back.mBack			=port.mBack;
			back.mFront			=port.mFront;
			back.mOnNode		=port.mOnNode;
			back.mFace.ClipByPlane(mPlane, false, true);
			if(!back.mFace.IsTiny())
			{
				mBack.FilterPortalFront(back, pieces);
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


		//filter portal into it's OnNode
		internal void FilterPortal(Portal port, List<Portal> pieces)
		{
			Debug.Assert(!mbLeaf);
			Debug.Assert(this == port.mOnNode);

			//split portal
			Portal	front		=new Portal();
			front.mFace			=new Face(port.mFace);
			front.mBack			=port.mBack;
			front.mFront		=port.mFront;
			front.mOnNode		=port.mOnNode;
			front.mFace.ClipByPlane(mPlane, true, true);
			if(!front.mFace.IsTiny())
			{
				mFront.FilterPortalFront(front, pieces);
			}

			Portal	back		=new Portal();
			back.mFace			=new Face(port.mFace);
			back.mBack			=port.mBack;
			back.mFront			=port.mFront;
			back.mOnNode		=port.mOnNode;
			back.mFace.ClipByPlane(mPlane, false, true);
			if(!back.mFace.IsTiny())
			{
				mBack.FilterPortalBack(back, pieces);
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
	}
}
