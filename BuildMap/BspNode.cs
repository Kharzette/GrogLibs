using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace BuildMap
{
	public class Portal
	{
		public	Face	mPoly;					//Convex poly that holds the shape of the portal
		public	BspNode	mFrontNode, mBackNode;	//Node on each side of the portal
		public	Portal	mFront, mBack;			//Next portal for each node
		public	BspNode	mOnNode;				//the node portal is sitting on

		public Portal(Plane plane)
		{
			mPoly		=new Face(plane);
			mFrontNode	=null;
			mBackNode	=null;
			mFront		=null;
			mBack		=null;
			mOnNode		=null;
		}

		public Portal()
		{
			mPoly		=null;
			mFrontNode	=null;
			mBackNode	=null;
			mFront		=null;
			mBack		=null;
			mOnNode		=null;
		}
	};

	public class BspNode
	{
		enum ContentsFlags
		{
			BSP_CONTENTS_SOLID2			=(1<<0),	// Solid (Visible)
			BSP_CONTENTS_WINDOW2		=(1<<1),	// Window (Visible)
			BSP_CONTENTS_EMPTY2			=(1<<2),	// Empty but Visible (water, lava, etc...)
			BSP_CONTENTS_TRANSLUCENT2	=(1<<3),	// Vis will see through it
			BSP_CONTENTS_WAVY2			=(1<<4),	// Wavy (Visible)
			BSP_CONTENTS_DETAIL2		=(1<<5),	// Won't be included in vis oclusion
			BSP_CONTENTS_CLIP2			=(1<<6),	// Structural but not visible
			BSP_CONTENTS_HINT2			=(1<<7),	// Primary splitter (Non-Visible)
			BSP_CONTENTS_AREA2			=(1<<8),	// Area seperator leaf (Non-Visible)
			BSP_CONTENTS_FLOCKING		=(1<<9),	// flocking flag.  Not really a contents type
			BSP_CONTENTS_SHEET			=(1<<10),

			//These contents are all solid types
			BSP_CONTENTS_SOLID_CLIP		=(BSP_CONTENTS_SOLID2 | BSP_CONTENTS_WINDOW2 | BSP_CONTENTS_CLIP2),

			//These contents are all visible types
			BSP_VISIBLE_CONTENTS		=(BSP_CONTENTS_SOLID2 | BSP_CONTENTS_EMPTY2 | 
										BSP_CONTENTS_WINDOW2 | BSP_CONTENTS_SHEET | 
										BSP_CONTENTS_WAVY2),

			//These contents define where faces are NOT allowed to merge across
			BSP_MERGE_SEP_CONTENTS		=(BSP_CONTENTS_WAVY2 | BSP_CONTENTS_HINT2 | 
										BSP_CONTENTS_AREA2)
		};

		private Face			mFace;
		private	ContentsFlags	mContents;
		private BspNode			mFront;
		private BspNode			mBack;
		private	Bounds			mBounds;
		private BspNode			mParent;
		private	bool			mbLeaf;
		private	Portal			mPortals;
		private	List<Brush>		mBrushes;
		private	bool			mbDetail;


		public BspNode(Face f)
		{
			mFace	=new Face(f);
		}


		public BspNode()
		{
		}


		private bool FindGoodSplitFace(List<Brush> brushList, out Face bestFace)
		{
			int		BestIndex	=-1;
			float	BestScore	=696969696.0f;

			foreach(Brush b in brushList)
			{
				float	score	=b.GetBestSplittingFaceScore(brushList);

				if(score < BestScore)
				{
					BestScore	=score;
					BestIndex	=brushList.IndexOf(b);
				}
			}

			if(BestIndex > 0)
			{
				bestFace	=brushList[BestIndex].GetBestSplittingFace(brushList);
				return	true;
			}
			else
			{	//this won't be used but gotta return something
				bestFace	=new Face(new Plane());
				return	false;
			}
		}


		public void BuildTree(List<Brush> brushList)
		{
			List<Brush>	frontList	=new List<Brush>();
			List<Brush> backList	=new List<Brush>();


			if(!FindGoodSplitFace(brushList, out mFace))
			{
				//this is a leaf node
				mbLeaf	=true;
				return;
			}
			//split the entire list into front and back
			foreach(Brush b in brushList)
			{
				Brush	bf, bb;

				b.SplitBrush(mFace.GetPlane(), out bf, out bb);

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


		//per face add, like in fusion
		private void AddToTree(BspNode n)
		{
			if(mFace == null)
			{
				mFace	=n.mFace;
				return;
			}

			int		pf, pb, po;
			Plane	tp	=mFace.GetPlane();

			mFace.GetSplitInfo(n.mFace, out pf, out pb, out po);

			if(pf == 0 && pb == 0)	//coplanar
			{
				Plane	np	=n.mFace.GetPlane();

				if(Vector3.Dot(tp.Normal, np.Normal) > 0)
				{
					if(mBack != null)
					{
						mBack.AddToTree(n);
					}
					else
					{
						mBack	=n;
					}
				}
				else
				{
					if(mFront != null)
					{
						mFront.AddToTree(n);
					}
					else
					{
						mFront	=n;
					}
				}
			}
			else if(pf == 0)	//back
			{
				if(mBack != null)
				{
					mBack.AddToTree(n);
				}
				else
				{
					mBack	=n;
				}
			}
			else if(pb == 0)	//front
			{
				if(mFront != null)
				{
					mFront.AddToTree(n);
				}
				else
				{
					mFront	=n;
				}
			}
			else	//split
			{
				Face	ff, bf;

				ff	=new Face(n.mFace);
				bf	=new Face(n.mFace);

				ff.ClipByFace(mFace, true);
				bf.ClipByFace(mFace, false);

				if(ff.IsValid() && !ff.IsTiny())
				{
					n.mFace	=ff;

					if(mFront != null)
					{
						mFront.AddToTree(n);
					}
					else
					{
						mFront	=n;
					}
				}

				if(bf.IsValid() && !bf.IsTiny())
				{
					if(mBack != null)
					{
						BspNode	bnode	=new BspNode();
						bnode.mFace		=bf;

						mBack.AddToTree(bnode);
					}
					else
					{
						mBack		=new BspNode();
						mBack.mFace	=bf;
					}
				}
			}
		}


		public void AddBrushToTree(Brush b)
		{
			List<BspNode>	nodes;

			b.GetNodesFromFaces(out nodes);

			foreach(BspNode n in nodes)
			{
				AddToTree(n);
			}
		}


		//something is hozed so just drawing all nodes
		public void Draw(GraphicsDevice g, Vector3 camPos)
		{
			Plane	p	=mFace.GetPlane();
			float	d	=Vector3.Dot(camPos, p.Normal) + p.Dist;

//			if(d < 0.01f)
			{
				if(mFront != null)
				{
					mFront.Draw(g, camPos);
				}
			}
//			else if(d > 0.01f)
			{
				if(mBack != null)
				{
					mBack.Draw(g, camPos);
				}
			}
/*			else
			{
				if(mFront != null)
				{
					mFront.Draw(g, camPos);
				}
				if(mBack != null)
				{
					mBack.Draw(g, camPos);
				}
			}*/

			mFace.Draw(g, Color.AntiqueWhite);
		}


		private bool CreateFaceOnNode(out Face outFace)
		{
			Plane	Plane;
			BspNode	Parent, curNode;

			outFace	=new Face(mFace.GetPlane());

			if(!outFace.IsValid())
			{
				return	false;
			}

			//Clip this portal by all the parents of this node
			for(Parent = mParent, curNode = this;Parent != null && outFace.IsValid();)
			{
				Plane	=Parent.mFace.GetPlane();

				//hope this works like pointers
				outFace.ClipByFace(Parent.mFace, (Parent.mFront == curNode));

				curNode	=Parent;
				Parent	=Parent.mParent;
			}

			return	true;
		}


		public static bool CreateAllOutsidePortals(BspNode node, BspNode outsideNode)
		{
			Portal			prt;
			List<Portal>	Portals	=new List<Portal>();

			outsideNode.mbLeaf		=true;
			outsideNode.mContents	=ContentsFlags.BSP_CONTENTS_SOLID2;

			//Create 6 portals, and point to the outside and the RootNode
			Plane	p;
			p.Normal	=Vector3.Backward;
			p.Dist		=Bounds.MIN_MAX_BOUNDS;
			prt			=new Portal(p);
//			AddPortalToNodes(prt, node, outsideNode);
			AddPortalToNodes(prt, outsideNode, node);
			Portals.Add(prt);

			p.Normal	=Vector3.Down;
			p.Dist		=Bounds.MIN_MAX_BOUNDS;
			prt			=new Portal(p);
//			AddPortalToNodes(prt, node, outsideNode);
			AddPortalToNodes(prt, outsideNode, node);
			Portals.Add(prt);

			p.Normal	=Vector3.Forward;
			p.Dist		=Bounds.MIN_MAX_BOUNDS;
			prt			=new Portal(p);
//			AddPortalToNodes(prt, node, outsideNode);
			AddPortalToNodes(prt, outsideNode, node);
			Portals.Add(prt);

			p.Normal	=Vector3.Left;
			p.Dist		=Bounds.MIN_MAX_BOUNDS;
			prt			=new Portal(p);
//			AddPortalToNodes(prt, node, outsideNode);
			AddPortalToNodes(prt, outsideNode, node);
			Portals.Add(prt);

			p.Normal	=Vector3.Right;
			p.Dist		=Bounds.MIN_MAX_BOUNDS;
			prt			=new Portal(p);
//			AddPortalToNodes(prt, node, outsideNode);
			AddPortalToNodes(prt, outsideNode, node);
			Portals.Add(prt);

			p.Normal	=Vector3.Up;
			p.Dist		=Bounds.MIN_MAX_BOUNDS;
			prt			=new Portal(p);
//			AddPortalToNodes(prt, node, outsideNode);
			AddPortalToNodes(prt, outsideNode, node);
			Portals.Add(prt);

			foreach(Portal pt in Portals)
			{
				foreach(Portal pt2 in Portals)
				{
					if(pt == pt2)
					{
						continue;
					}

					pt.mPoly.ClipByFace(pt2.mPoly, false);

					if(!pt.mPoly.IsValid())
					{
						return	false;
					}
				}
			}

			return	true;
		}


		void CalcNodeBoundsFromPortals()
		{
			Portal	p;

			mBounds.ClearBounds();

			for(p=mPortals; p != null;)
			{
				p.mPoly.AddToBounds(mBounds);

				if(p.mFrontNode == this)
				{
					p	=p.mFront;
				}
				else
				{
					p	=p.mBack;
				}
			}
		}


		public bool CreatePortals(BspNode outsideNode)
		{
			if(!CreateAllOutsidePortals(this, outsideNode))
			{
				Debug.WriteLine("CreatePortals:  Could not create bbox portals.");
				return	false;
			}

			if(!PartitionPortals(this))
			{
				Debug.WriteLine("CreatePortals:  Could not partition portals.");
				return	false;
			}

			return	true;
		}


		private static bool PartitionPortals(BspNode node)
		{
			Face		newPoly;
			Portal		prt, newPrt, nextPrt;
			BspNode		Front, Back, OtherNode;

			node.CalcNodeBoundsFromPortals();

			if(node.mbLeaf)
			{
				return	true;
			}

			if(node.mbDetail)	//We can stop at detail seperators for the vis tree
			{
				return	true;
			}

			Front	=node.mFront;
			Back	=node.mBack;

			//Create a new portal
			if(!node.CreateFaceOnNode(out newPoly))
			{
				Debug.WriteLine("PartitionPortals:  CreatePolyOnNode failed.");
				return	false;
			}

			//Clip it against all other portals attached to this node
			for(prt = node.mPortals;prt != null && newPoly.IsValid();)
			{
				if(prt.mFrontNode != node && prt.mBackNode != node)
				{
					Debug.WriteLine("PartitionPortals_r:  Portal does not look at either node.");
					return	false;
				}

				if(prt.mFrontNode == node)
				{
					newPoly.ClipByFace(prt.mPoly, true);
					prt	=prt.mFront;
				}
				else
				{
					newPoly.ClipByFace(prt.mPoly, false);
					prt	=prt.mBack;
				}

				if(!newPoly.IsValid())
				{
					Debug.WriteLine("PartitionPortals_r:  Portal was cut away.");
					break;
				}
			}
			
			if(newPoly.IsValid() && newPoly.IsTiny())
			{
				newPoly	=null;
			}

			if(newPoly != null)
			{
				newPrt	=new Portal();
				if(newPrt == null)
				{
					Debug.WriteLine("PartitionPortals_r:  Out of memory for portal.");
					return	false;
				}
				newPrt.mPoly		=newPoly;
				newPrt.mOnNode	=node;

				/*
				if (!CheckPortal(NewPortal))
				{
					GHook.Error("PartiionPortals_r:  Check Portal failed.\n");
					return GE_FALSE;
				}
				else*/
					AddPortalToNodes(newPrt, Front, Back);

			}
			
			//Partition all portals by this node
			for(prt = node.mPortals; prt != null;prt = nextPrt)
			{
				if(prt.mFrontNode != node && prt.mBackNode != node)
				{
					Debug.WriteLine("PartitionPortals_r:  Portal does not look at either node.");
					return	false;
				}

				//Remember the node on the back side
				if(prt.mFrontNode == node)
				{
					OtherNode	=prt.mBackNode;
					nextPrt		=prt.mFront;
				}
				else
				{
					OtherNode	=prt.mFrontNode;
					nextPrt		=prt.mBack;
				}

				prt.mFrontNode.RemovePortal(prt);
				prt.mBackNode.RemovePortal(prt);

				Face	ff	=new Face(prt.mPoly);
				Face	bf	=new Face(prt.mPoly);

				ff.ClipByFace(node.mFace, true);
				bf.ClipByFace(node.mFace, false);
				
				if(ff.IsValid() && ff.IsTiny())
				{
					ff	=null;
				}
				if(bf.IsValid() && bf.IsTiny())
				{
					bf	=null;
				}
				
				if(ff == null && bf == null)
				{
					continue;
				}
				
				if(ff == null)
				{
					prt.mPoly	=bf;
					if(prt.mFrontNode == node)
					{
						AddPortalToNodes(prt, Back, OtherNode);
					}
					else
					{
						AddPortalToNodes(prt, OtherNode, Back);
					}
					continue;
				}

				if(bf == null)
				{
					prt.mPoly	=ff;
					if(prt.mFrontNode == node)
					{
						AddPortalToNodes(prt, Back, OtherNode);
					}
					else
					{
						AddPortalToNodes(prt, OtherNode, Back);
					}
					continue;
				}

				//Portal was split
				newPrt	=new Portal();
				newPrt.mPoly		=bf;
				newPrt.mFrontNode	=prt.mFrontNode;
				newPrt.mBackNode	=prt.mBackNode;
				//newPrt.mFront		=prt.mFront;
				//newPrt.mBack		=prt.mBack;
				newPrt.mOnNode		=prt.mOnNode;

				prt.mPoly	=ff;

				if(prt.mFrontNode == node)
				{
					AddPortalToNodes(prt, Front, OtherNode);
					AddPortalToNodes(newPrt, Back, OtherNode);
				}
				else
				{
					AddPortalToNodes(prt, OtherNode, Front);
					AddPortalToNodes(newPrt, OtherNode, Back);
				}
			}

			if(node.mPortals != null)
			{
				Debug.WriteLine("*WARNING* PartitionPortals_r:  Portals still on node after distribution...");
			}
			
			if(!PartitionPortals(Front))
			{
				return	false;
			}

			if(!PartitionPortals(Back))
			{
				return	false;
			}

			return	true;
		}


		public static bool AddPortalToNodes(Portal prt, BspNode front, BspNode back)
		{
			if(prt.mFrontNode != null || prt.mBackNode != null)
			{
				Debug.WriteLine("LinkPortal:  Portal allready looks at one of the nodes.");
				return	false;
			}

			prt.mFrontNode	=front;
			prt.mFront		=front.mPortals;
			front.mPortals	=prt;

			prt.mBackNode	=back;
			prt.mBack		=back.mPortals;
			back.mPortals	=prt;

			return	true;
		}


		//this is hugely complicated by a lack of pointers
		public bool RemovePortal(Portal prt)
		{
			Portal	p, prev;

			for(p = mPortals, prev=null;p != null;)
			{
				if(p == prt)
				{
					break;
				}

				if(p.mFrontNode == this)
				{
					prev	=p;
					p		=p.mFront;
				}
				else
				{
					prev	=p;
					p		=p.mBack;
				}
			}

			if(p == null)
			{
				Debug.WriteLine("Assgoblinry in RemovePortal");
				return	false;
			}

			if(p.mFrontNode == this)
			{
				if(prev == null)
				{
					mPortals	=prt.mFront;
				}
				else
				{
					if(prev.mFrontNode == this)
					{
						prev.mFront	=prt.mFront;
					}
					else
					{
						prev.mBack	=prt.mFront;
					}
				}
				prt.mFrontNode	=null;
			}
			else
			{
				if(prev == null)
				{
					mPortals	=prt.mBack;
				}
				else
				{
					if(prev.mFrontNode == this)
					{
						prev.mFront	=prt.mBack;
					}
					else
					{
						prev.mBack	=prt.mBack;
					}
				}
				prt.mBackNode	=null;
			}
			return	true;
		}
	}
}
