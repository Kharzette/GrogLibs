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
		private	Plane			mPlane;
		private List<Face>		mFaces;
		private BspNode			mFront,	mBack;
		private	Bounds			mBounds;
		private	bool			mbLeaf;


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
				bestFace	=new Face(new Plane());
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
				foreach(Brush b in brushList)
				{
					b.AddFacesToList(ref mFaces);
				}
				mbLeaf	=true;
				return;
			}

			mPlane	=face.GetPlane();

			//split the entire list into front and back
			foreach(Brush b in brushList)
			{
				Brush	bf, bb;

				b.SplitBrush(face.GetPlane(), out bf, out bb);

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
		public void Draw(GraphicsDevice g, Vector3 camPos)
		{
			if(mbLeaf)
			{
				if(mFaces != null)
				{
					foreach(Face f in mFaces)
					{
						f.Draw(g, Color.AntiqueWhite);
					}
				}
			}

			float	d	=Vector3.Dot(mPlane.Normal, camPos) - mPlane.Dist;
			if(d > 0.0f)
			{
				if(mFront != null)
				{
					mFront.Draw(g, camPos);
				}
				if(mBack != null)
				{
					mBack.Draw(g, camPos);
				}
			}
			else if(d < 0.0f)
			{
				if(mBack != null)
				{
					mBack.Draw(g, camPos);
				}
				if(mFront != null)
				{
					mFront.Draw(g, camPos);
				}
			}
			else
			{
				if(mFront != null)
				{
					mFront.Draw(g, camPos);
				}
				if(mBack != null)
				{
					mBack.Draw(g, camPos);
				}
			}
		}
		

		public bool ClassifyPoint(Vector3 pnt)
		{
			if(mbLeaf)
			{
				return	IsPointBehindAllNodeFaces(pnt);
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
