using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace BuildMap
{
	public class BspNode
	{
		public const UInt32	BSP_CONTENTS_SOLID2	=1;

		private	Plane		mPlane;
		private List<Face>	mFaces;
		private BspNode		mFront,	mBack;
		private	Bounds		mBounds;
		private	bool		mbLeaf;
		private	Face		mPortal;
		private	BspNode		mParent;
		private	UInt32		mFillKey;	//for flooding
		private	UInt32		mContents;


		public BspNode(Face f)
		{
			mFaces		=new  List<Face>();
			Face	cp	=new Face(f);
			mFaces.Add(cp);

			mPlane	=f.GetPlane();
		}


		public BspNode()
		{
			mFaces	=new  List<Face>();
		}


		private bool FindGoodSplitFace(List<Brush> brushList, out Face bestFace)
		{
			int		BestIndex	=-1;
			float	BestScore	=696969.0f;

			foreach(Brush b in brushList)
			{
				float	score	=b.GetBestSplittingFaceScore(brushList);

				if(score < BestScore)
				{
					BestScore	=score;
					BestIndex	=brushList.IndexOf(b);
				}
			}

			if(BestIndex >= 0)
			{
				bestFace	=brushList[BestIndex].GetBestSplittingFace(brushList);
				return	true;
			}
			else
			{	//this won't be used but gotta return something
				bestFace	=new Face(new Plane(), null);
				return	false;
			}
		}


		private	bool IsPointBehindAllNodeFaces(Vector3 pnt)
		{
			Debug.Assert(mbLeaf);

			foreach(Face f in mFaces)
			{
				Plane	p	=f.GetPlane();

				float	d	=Vector3.Dot(p.Normal, pnt) - p.Dist;
				if(d > 0.0f)
				{
					return	false;
				}
			}
			return	true;
		}


		public void BuildTree(List<Brush> brushList)
		{
			List<Brush>	frontList	=new List<Brush>();
			List<Brush> backList	=new List<Brush>();

			Face	face;

			if(!FindGoodSplitFace(brushList, out face))
			{
				mbLeaf	=true;

				//copy in the brush faces
				foreach(Brush b in brushList)
				{
					b.AddFacesToList(ref mFaces);
				}
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
				mFront			=new BspNode();
				mFront.mParent	=this;
				mFront.BuildTree(frontList);
			}
			else
			{
				Debug.Assert(false);// && "Nonleaf node with no front side brushes!");
			}

			if(backList.Count > 0)
			{
				mBack			=new BspNode();
				mBack.mParent	=this;
				mBack.BuildTree(backList);
			}
			else
			{
				Debug.Assert(false);// && "Nonleaf node with no back side brushes!");
			}
		}


		private	void NukeAllButThisPlane(Plane p)
		{
			int	foundIdx	=-1;

			foreach(Face f in mFaces)
			{
				Plane	p2	=f.GetPlane();

				if(Math.Abs(p2.Normal.X - p.Normal.X) > Bounds.DIST_EPSILON)
				{
					continue;
				}
				if(Math.Abs(p2.Normal.Y - p.Normal.Y) > Bounds.DIST_EPSILON)
				{
					continue;
				}
				if(Math.Abs(p2.Normal.Z - p.Normal.Z) > Bounds.DIST_EPSILON)
				{
					continue;
				}
				foundIdx	=mFaces.IndexOf(f);
				break;
			}

			if(foundIdx == -1)
			{
				Debug.WriteLine("Couldn't find plane in NukeAll");
				return;
			}

			Face	keep	=new Face(mFaces[foundIdx]);

			mFaces.Clear();

			mFaces.Add(keep);
		}


		private	void ClipToParents(Face prt)
		{
			Face clippy	=new Face(mPlane, null);

			prt.ClipByFace(clippy, true);

			if(mParent != null)
			{
				mParent.ClipToParents(prt);
			}
		}


		public void DrawPortalsDumb(GraphicsDevice g, Effect fx, Vector3 camPos)
		{
			if(mFront != null)
			{
				mFront.Draw(g, fx, camPos);
			}

			if(mPortal != null)
			{
				mPortal.Draw(g, fx, Color.CadetBlue);
			}

			if(mBack != null)
			{
				mBack.Draw(g, fx, camPos);
			}
		}


		public	void DrawPortals(GraphicsDevice g, Effect fx, Vector3 camPos)
		{
			if(mbLeaf)
			{
				return;
			}

			float	d	=Vector3.Dot(mPlane.Normal, camPos) - mPlane.Dist;
			if(d > 0.0f)
			{
				if(mFront != null)
				{
					mFront.Draw(g, fx, camPos);
				}

				if(mPortal != null)
				{
					mPortal.Draw(g, fx, Color.CadetBlue);
				}

				if(mBack != null)
				{
					mBack.Draw(g, fx, camPos);
				}
			}
			else if(d < 0.0f)
			{
				if(mBack != null)
				{
					mBack.Draw(g, fx, camPos);
				}

				if(mPortal != null)
				{
					mPortal.Draw(g, fx, Color.CadetBlue);
				}

				if(mFront != null)
				{
					mFront.Draw(g, fx, camPos);
				}
			}
			else
			{
				if(mFront != null)
				{
					mFront.Draw(g, fx, camPos);
				}

				if(mPortal != null)
				{
					mPortal.Draw(g, fx, Color.CadetBlue);
				}

				if(mBack != null)
				{
					mBack.Draw(g, fx, camPos);
				}
			}
		}


		public	void BuildPortals()
		{
			//recurse to leaves
			if(mFront != null)
			{
				mFront.BuildPortals();
			}

			if(!mbLeaf)
			{
				if(mParent != null)
				{
					mPortal	=new Face(mPlane, null);
					mParent.ClipToParents(mPortal);

					mPortal.mFlags	=0;
					if((mFront.mContents & BSP_CONTENTS_SOLID2) != 0)
					{
						mPortal.mFlags	|=1;
					}

					if((mBack.mContents & BSP_CONTENTS_SOLID2) != 0)
					{
						mPortal.mFlags	|=4;
					}
					else
					{
						mPortal.mFlags	|=2;
					}
				}
			}

			if(mBack != null)
			{
				mBack.BuildPortals();
			}
		}


		public	void MarkLeafs()
		{
			if(mFront == null && mBack == null)
			{
				mbLeaf		=true;
				return;
			}

			if(mFront != null)
			{
				mFront.MarkLeafs();
			}

			if(mBack != null)
			{
				mBack.MarkLeafs();
			}
		}


		//per face add, like in fusion
		/*
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
		}*/


		public void AddBrushToTree(Brush b)
		{
			List<BspNode>	nodes;

			b.GetNodesFromFaces(out nodes);

			foreach(BspNode n in nodes)
			{
				//AddToTree(n);
			}
		}


		//something is hozed so just drawing all nodes
		public void Draw(GraphicsDevice g, Effect fx, Vector3 camPos)
		{
			if(mbLeaf)
			{
				if(mFaces != null)
				{
					foreach(Face f in mFaces)
					{
						f.Draw(g, fx, Color.AntiqueWhite);
					}
				}
			}

			float	d	=Vector3.Dot(mPlane.Normal, camPos) - mPlane.Dist;
			if(d > 0.0f)
			{
				if(mFront != null)
				{
					mFront.Draw(g, fx, camPos);
				}
				if(mBack != null)
				{
					mBack.Draw(g, fx, camPos);
				}
			}
			else if(d < 0.0f)
			{
				if(mBack != null)
				{
					mBack.Draw(g, fx, camPos);
				}
				if(mFront != null)
				{
					mFront.Draw(g, fx, camPos);
				}
			}
			else
			{
				if(mFront != null)
				{
					mFront.Draw(g, fx, camPos);
				}
				if(mBack != null)
				{
					mBack.Draw(g, fx, camPos);
				}
			}
		}

		

		private void FillUnTouchedLeafs(UInt32 curFill)
		{
			if(!mbLeaf)
			{
				mFront.FillUnTouchedLeafs(curFill);
				mBack.FillUnTouchedLeafs(curFill);
				return;
			}

			if((mContents & BSP_CONTENTS_SOLID2) != 0)
			{
				return;		//allready solid or removed...
			}

			if(mFillKey != curFill)
			{
				// Fill er in with solid so it does not show up...(Preserve user contents)
				mContents	&=(0xffff0000);
				mContents	|=BSP_CONTENTS_SOLID2;
			}
		}


		private bool FillFromEntities(List<Entity> ents, UInt32 curFill)
		{
			bool	bEmpty	=false;

			foreach(Entity e in ents)
			{
				if(ents.IndexOf(e) == 0)
				{
					continue;	//skip world
				}

				Vector3	org;

				if(!e.GetOrigin(out org))
				{
					continue;
				}

				BspNode	bn;

				if(GetLeafLandedIn(org, out bn))
				{
					continue;
				}
				
				//There is at least one entity in empty space...
				bEmpty	=true;

				if(!bn.FillLeafs2r(curFill))
				{
					return	false;
				}
			}

			if(!bEmpty)
			{
				Debug.WriteLine("FillFromEntities:  No valid entities for operation.");
				return	false;
			}

			return	true;
		}


		bool FillLeafs2r(UInt32 curFill)
		{
			if((mContents & BSP_CONTENTS_SOLID2) != 0)
			{
				return	true;
			}

			if(mFillKey == curFill)
			{
				return	true;
			}

			mFillKey	=curFill;

			return	true;
		}


		public bool ClassifyPoint(Vector3 pnt)
		{
			if(mbLeaf)
			{
				if(IsPointBehindAllNodeFaces(pnt))
				{
					return	true;
				}
				else
				{
					return	false;
				}
			}

			float	d	=Vector3.Dot(mPlane.Normal, pnt) - mPlane.Dist;

			if(d > 0.0f)
			{
				return	mFront.ClassifyPoint(pnt);
			}
			else if(d < 0.0f)
			{
				return	mBack.ClassifyPoint(pnt);
			}
			else
			{
				return	mFront.ClassifyPoint(pnt);
			}
		}


		//returns impact point
		public Vector3 RayCast(Vector3 pntA, Vector3 pntB)
		{
			if(mbLeaf)
			{
				if(IsPointBehindAllNodeFaces(pntA))
				{
					return	pntA;
				}
				else
				{
					return	Vector3.Zero;
				}
			}

			float	d	=Vector3.Dot(mPlane.Normal, pntA) - mPlane.Dist;
			float	d2	=Vector3.Dot(mPlane.Normal, pntB) - mPlane.Dist;

			if(d > 0.0f && d2 > 0.0f)
			{
				return	mFront.RayCast(pntA, pntB);
			}
			else if(d < 0.0f && d2 < 0.0f)
			{
				return	mBack.RayCast(pntA, pntB);
			}
			else if(d == 0.0f && d2 == 0.0f)
			{
				return	mFront.RayCast(pntA, pntB);
			}
			else	//split up segment
			{
				float	splitRatio	=d / (d - d2);
				Vector3	mid	=pntA + (splitRatio * (pntB - pntA));
				Vector3	ret;

				ret	=mFront.RayCast(pntA, mid);

				if(ret != Vector3.Zero)
				{
					return	ret;
				}

				return	mBack.RayCast(mid, pntB);
			}
		}


		//returns true or false
		public bool RayCastBool(Vector3 pntA, Vector3 pntB)
		{
			if(mbLeaf)
			{
				if(IsPointBehindAllNodeFaces(pntA))
				{
					return	false;
				}
				else
				{
					return	true;
				}
			}

			float	d	=Vector3.Dot(mPlane.Normal, pntA) - mPlane.Dist;
			float	d2	=Vector3.Dot(mPlane.Normal, pntB) - mPlane.Dist;

			if(d > 0.0f && d2 > 0.0f)
			{
				return	mFront.RayCastBool(pntA, pntB);
			}
			else if(d < 0.0f && d2 < 0.0f)
			{
				return	mBack.RayCastBool(pntA, pntB);
			}
			else if(d == 0.0f && d2 == 0.0f)
			{
				return	mFront.RayCastBool(pntA, pntB);
			}
			else	//split up segment
			{
				float	splitRatio	=d / (d - d2);
				Vector3	mid	=pntA + (splitRatio * (pntB - pntA));

				//is this correct?
				return	mFront.RayCastBool(pntA, mid);
				//return	mBack.RayCast(mid, pntB);
			}
		}


		//returns true for solid
		public bool GetLeafLandedIn(Vector3 pnt, out BspNode bn)
		{
			bn	=this;
			if(mbLeaf)
			{
				if(IsPointBehindAllNodeFaces(pnt))
				{
					return	true;
				}
				else
				{
					return	false;
				}
			}

			float	d	=Vector3.Dot(mPlane.Normal, pnt) - mPlane.Dist;

			if(d > 0.0f)
			{
				return	mFront.ClassifyPoint(pnt);
			}
			else if(d < 0.0f)
			{
				return	mBack.ClassifyPoint(pnt);
			}
			else
			{
				return	mFront.ClassifyPoint(pnt);
			}
		}
	}
}
