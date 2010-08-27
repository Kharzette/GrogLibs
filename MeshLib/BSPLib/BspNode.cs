using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace BSPLib
{
	public class BspNode
	{
		public	Plane	mPlane;
		public	BspNode	mFront, mBack;
		public	Brush	mBrush;


		#region Constructors
		public BspNode(Face f)
		{
			mPlane	=f.GetPlane();
		}


		public BspNode()
		{
		}
		#endregion


		#region Queries
		internal void AddToBounds(ref Bounds bnd)
		{
			if(mBrush != null)
			{
				mBrush.AddToBounds(ref bnd);
			}

			if(mFront != null)
			{
				mFront.AddToBounds(ref bnd);
			}
			if(mBack != null)
			{
				mBack.AddToBounds(ref bnd);
			}
		}


		bool FindGoodSplitFace(List<Brush> brushList, out Face bestFace)
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
				return true;
			}
			else
			{	//this won't be used but gotta return something
				bestFace	=new Face(new Plane(), null);
				return	false;
			}
		}


		internal void GetTriangles(List<Vector3> tris, List<UInt16> ind)
		{
			if(mBrush != null)
			{
				mBrush.GetTriangles(tris, ind);
				return;
			}
			mFront.GetTriangles(tris, ind);
			mBack.GetTriangles(tris, ind);
		}


		internal void GetPlanes(List<Plane> planes)
		{
			if(mBrush != null)
			{
				mBrush.GetPlanes(planes);
				return;
			}
			mFront.GetPlanes(planes);
			mBack.GetPlanes(planes);
		}
		#endregion


		#region IO
		public virtual void Write(BinaryWriter bw)
		{
			//write plane
			bw.Write(mPlane.mNormal.X);
			bw.Write(mPlane.mNormal.Y);
			bw.Write(mPlane.mDistance);

			bw.Write(mBrush != null);
			if(mBrush != null)
			{
				mBrush.Write(bw);
			}

			bw.Write(mFront != null);
			if(mFront != null)
			{
				mFront.Write(bw);
			}

			bw.Write(mBack != null);
			if(mBack != null)
			{
				mBack.Write(bw);
			}
		}


		public virtual void Read(BinaryReader br)
		{
			mPlane.mNormal.X	=br.ReadSingle();
			mPlane.mNormal.Y	=br.ReadSingle();
			mPlane.mDistance	=br.ReadSingle();

			bool	val	=br.ReadBoolean();
			if(val)
			{
				mBrush	=new Brush();
				mBrush.Read(br);
			}

			val	=br.ReadBoolean();
			if(val)
			{
				mFront	=new BspNode();
				mFront.Read(br);
			}

			val	=br.ReadBoolean();
			if(val)
			{
				mBack	=new BspNode();
				mBack.Read(br);
			}
		}
		#endregion


		//builds a bsp good for collision info
		internal void BuildTree(List<Brush> brushList)
		{
			List<Brush>	frontList	=new List<Brush>();
			List<Brush>	backList	=new List<Brush>();

			Face	face;

			if(!FindGoodSplitFace(brushList, out face))
			{
				if(brushList.Count != 1)
				{
					Debug.WriteLine("Merging " + brushList.Count + " brushes in a leaf");
				}

				//copy in the brush faces
				//merge multiples
				mBrush	=new Brush();
				foreach(Brush b in brushList)
				{
					mBrush.AddFaces(b.GetFaces());
				}
				mBrush.SealFaces();
				if(!mBrush.IsValid())
				{
					//brush is messed up, donut add it
					mBrush	=null;
					Debug.WriteLine("Bad brush in leaf!");
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
				mFront.BuildTree(frontList);
			}
			else
			{
				Debug.Assert(false);// && "Nonleaf node with no front side brushes!");
			}

			if(backList.Count > 0)
			{
				mBack			=new BspNode();
				mBack.BuildTree(backList);
			}
			else
			{
				Debug.Assert(false);// && "Nonleaf node with no back side brushes!");
			}
		}


		internal void GetPortals(List<Face> portals)
		{
			if(mBrush != null)
			{
				mBrush.GetPortals(portals);
				return;
			}

			mBack.GetPortals(portals);
			mFront.GetPortals(portals);
		}


		//see if this portal lands on the back
		internal void MergePortal(Face portal, List<Face> outPortals)
		{
			if(mBrush != null)
			{
				mBrush.MergePortal(portal, outPortals);
				return;
			}
			mBack.MergePortal(portal, outPortals);
			mFront.MergePortal(portal, outPortals);
		}


		internal void MarkPortal(Face portal)
		{
			if(mBrush != null)
			{
				mBrush.MarkPortal(portal);
				return;
			}

			Face	planeFace	=new Face(mPlane, null);
			Face	back		=new Face(portal);
			Face	front		=new Face(portal);

			if(back.ClipByFace(planeFace, false, true))
			{
				mBack.MarkPortal(back);
			}
			if(front.ClipByFace(planeFace, true, true))
			{
				mFront.MarkPortal(front);
			}
		}


		internal void BevelObtuse(float hullWidth)
		{
			if(mBrush != null)
			{
				mBrush.BevelObtuse(hullWidth);
				return;
			}

			mBack.BevelObtuse(hullWidth);
			mFront.BevelObtuse(hullWidth);
		}

		
		//grab all the brushes in the tree
		void GatherNodeBrushes(ref List<Brush> brushes)
		{
			if(mBrush != null)
			{
				brushes.Add(mBrush);
				return;
			}

			if(mFront != null)
			{
				mFront.GatherNodeBrushes(ref brushes);
			}
			mBack.GatherNodeBrushes(ref brushes);
		}


		internal bool ClassifyPoint(Vector3 pnt)
		{
			float	d;

			if(mBrush != null)
			{
				return	mBrush.IsPointInside(pnt);
			}

			d	=Vector3.Dot(mPlane.mNormal, pnt) - mPlane.mDistance;

			if(d < Plane.EPSILON)
			{
				return	mBack.ClassifyPoint(pnt);
			}
			if(d >= Plane.EPSILON)
			{
				return	mFront.ClassifyPoint(pnt);
			}
			else
			{
				return	mBack.ClassifyPoint(pnt);
			}
		}
	}
}
