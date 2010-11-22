using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;


namespace BSPLib
{
	public class BspFlatNode
	{
		public Plane		mPlane;
		public BspFlatNode	mFront, mBack, mParent;
		public List<Face>	mFaces	=new List<Face>();
		public Bounds		mBounds;
		public List<Portal>	mPortals;
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
				node.mFront					=new BspFlatNode();
				node.mFront.mbLeaf			=true;
				node.mFront.mBrushContents	=0;

				//advance to next face / node
				parent		=node;
				node.mBack	=new BspFlatNode();
				node		=node.mBack;
			}
			node.mbLeaf			=true;
			node.mBrushContents	=merge.GetContents();
		}


		//builds a bsp good for portals
		internal void BuildTree(List<Brush> brushList, object prog)
		{
			List<Brush>	frontList	=new List<Brush>();
			List<Brush>	backList	=new List<Brush>();

			Face	face;

			ProgressWatcher.UpdateProgress(prog, brushList.Count);

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
				mFront.BuildTree(frontList, prog);
			}
			else
			{
				Debug.Assert(false);// && "Nonleaf node with no front side brushes!");
			}

			if(backList.Count > 0)
			{
				mBack	=new BspFlatNode();
				mBack.mParent	=this;
				mBack.BuildTree(backList, prog);
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
	}
}
