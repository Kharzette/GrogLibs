using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace BuildMap
{
	public class BspNode
	{
		public const UInt32	CONTENTS_SOLID	=1;

		public	Plane		mPlane;
		public	Face		mFace;
		public	BspNode		mFront,	mBack;
		private	Bounds		mBounds;
		public	bool		mbLeaf;
		private	Face		mPortal;
		public	BspNode		mParent;
		private	UInt32		mFillKey;	//for flooding
		private	UInt32		mContents;


		public BspNode(Face f)
		{
			mFace	=new Face(f);
			mPlane	=f.GetPlane();
		}


		public BspNode()
		{
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


		//builds a bsp good for collision info
		public void BuildTree(List<Brush> brushList)
		{
			List<Brush>	frontList	=new List<Brush>();
			List<Brush> backList	=new List<Brush>();

			Face	face;

			if(!FindGoodSplitFace(brushList, out face))
			{
				//copy in the brush faces
				foreach(Brush b in brushList)
				{
					b.AddFacesToLeaf(this);
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


		#region DebugStuff
		public void BuildVertexInfo(GraphicsDevice g)
		{
			if(mFront != null)
			{
				mFront.BuildVertexInfo(g);
			}

			if(mFace != null)
			{
				mFace.BuildVertexInfo();
			}

			if(mBack != null)
			{
				mBack.BuildVertexInfo(g);
			}
		}

		public static int	gCount;
		public static int	gCheck;


		public int GetFirstBSPSurface(out Vector3[] surfPoints)
		{
			int	np;
			if(mBack != null)
			{
				np	=mBack.GetFirstBSPSurface(out surfPoints);
				if(np > 0)
				{
					return	np;
				}
			}
			if(mPortal != null)
			{
				if(gCount > gCheck)
				{
					np	=mPortal.GetSurfPoints(out surfPoints);
					if(np > 0)
					{
						return	np;
					}
				}
				gCount++;
			}
			if(mFront != null)
			{
				np	=mFront.GetFirstBSPSurface(out surfPoints);
				if(np > 0)
				{
					return	np;
				}
			}
			surfPoints	=null;
			return	0;
		}


		public void Light(GraphicsDevice g, BspNode root, Vector3 lightPos, float lightVal, Vector3 clr)
		{
			if(mFront != null)
			{
				mFront.Light(g, root, lightPos, lightVal, clr);
			}

			if(mFace != null)
			{
				mFace.LightFace(g, root, lightPos, lightVal, clr);
			}

			if(mBack != null)
			{
				mBack.Light(g, root, lightPos, lightVal, clr);
			}
		}
		#endregion


		//something is hozed so just drawing all nodes
		public void Draw(GraphicsDevice g, Effect fx, Vector3 camPos)
		{
			if(mFace != null)
			{
				mFace.Draw(g, fx);
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


		public Bounds BoundNodes()
		{
			Bounds	b, b2;

			b	=b2	=null;

			if(mbLeaf)
			{
				mBounds	=new Bounds();

				mFace.AddToBounds(ref mBounds);
				return	mBounds;
			}

			if(mFront != null)
			{
				b	=mFront.BoundNodes();
			}
			if(mBack != null)
			{
				b2	=mBack.BoundNodes();
			}
			mBounds	=new Bounds();
			mBounds.MergeBounds(b, b2);

			if(mFace != null)
			{
				mFace.AddToBounds(ref mBounds);
			}

			return	mBounds;
		}


		public void WriteToFile(BinaryWriter bw)
		{
			//write plane
			bw.Write(mPlane.Normal.X);
			bw.Write(mPlane.Normal.Y);
			bw.Write(mPlane.Normal.Z);
			bw.Write(mPlane.Dist);

			//write bounds
			bw.Write(mBounds.mMins.X);
			bw.Write(mBounds.mMins.Y);
			bw.Write(mBounds.mMins.Z);
			bw.Write(mBounds.mMaxs.X);
			bw.Write(mBounds.mMaxs.Y);
			bw.Write(mBounds.mMaxs.Z);

			bw.Write(mbLeaf);

			bw.Write(mFront != null);
			if(mFront != null)
			{
				mFront.WriteToFile(bw);
			}

			bw.Write(mBack != null);
			if(mBack != null)
			{
				mBack.WriteToFile(bw);
			}
		}


		#region RayCastsAndContents
		public bool ClassifyPoint(Vector3 pnt)
		{
			float	d;

			if(mbLeaf)
			{
				d	=Vector3.Dot(mPlane.Normal, pnt) - mPlane.Dist;
				if(d > -Face.ON_EPSILON)
				{
					return	false;
				}
				else
				{
					return	true;
				}
			}

			d	=Vector3.Dot(mPlane.Normal, pnt) - mPlane.Dist;

			if(d > -Face.ON_EPSILON)
			{
				if(mFront == null)
				{
					return	false;	//landed in empty
				}
				return	mFront.ClassifyPoint(pnt);
			}
			else if(d < Face.ON_EPSILON)
			{
				return	mBack.ClassifyPoint(pnt);
			}
			else
			{
				if(mFront == null)
				{
					return	false;	//landed in empty
				}
				return	mFront.ClassifyPoint(pnt);
			}
		}


		//returns impact point
		public Vector3 RayCast(Vector3 pntA, Vector3 pntB)
		{
			float	d, d2;

			if(mbLeaf)
			{
				d	=Vector3.Dot(mPlane.Normal, pntA) - mPlane.Dist;
				d2	=Vector3.Dot(mPlane.Normal, pntB) - mPlane.Dist;

				if(d > -Face.ON_EPSILON && d2 > -Face.ON_EPSILON)
				{
					return	Vector3.Zero;	//no impact
				}
				else if(d < Face.ON_EPSILON && d2 < Face.ON_EPSILON)
				{
					return	pntA;	//crossover point is impact
				}
				else	//split up segment
				{
					float	splitRatio	=d / (d - d2);
					Vector3	mid	=pntA + (splitRatio * (pntB - pntA));

					return	mid;
				}
			}

			d	=Vector3.Dot(mPlane.Normal, pntA) - mPlane.Dist;
			d2	=Vector3.Dot(mPlane.Normal, pntB) - mPlane.Dist;

			if(d > -Face.ON_EPSILON && d2 > -Face.ON_EPSILON)
			{
				return	mFront.RayCast(pntA, pntB);
			}
			else if(d < Face.ON_EPSILON && d2 < Face.ON_EPSILON)
			{
				return	mBack.RayCast(pntA, pntB);
			}
			else	//split up segment
			{
				float	splitRatio	=d / (d - d2);
				Vector3	mid	=pntA + (splitRatio * (pntB - pntA));

				if(d > 0.0f)
				{
					return	mFront.RayCast(pntA, mid);
				}
				else
				{
					return	mBack.RayCast(mid, pntB);
				}
			}
		}


		//returns true or false
		public bool RayCastBool(Vector3 pntA, Vector3 pntB)
		{
			float	d, d2;

			if(mbLeaf)
			{
				d	=Vector3.Dot(mPlane.Normal, pntA) - mPlane.Dist;
				d2	=Vector3.Dot(mPlane.Normal, pntB) - mPlane.Dist;

				if(d > -Face.ON_EPSILON && d2 > -Face.ON_EPSILON)
				{
					return	true;	//no impact
				}
				else if(d < Face.ON_EPSILON && d2 < Face.ON_EPSILON)
				{
					return	false;	//crossover point is impact
				}
				else	//split up segment
				{
					return	false;
				}
			}

			d	=Vector3.Dot(mPlane.Normal, pntA) - mPlane.Dist;
			d2	=Vector3.Dot(mPlane.Normal, pntB) - mPlane.Dist;

			if(d > -Face.ON_EPSILON && d2 > -Face.ON_EPSILON)
			{
				if(mFront == null)
				{
					return	true;	//landed in empty
				}
				return	mFront.RayCastBool(pntA, pntB);
			}
			else if(d < Face.ON_EPSILON && d2 < Face.ON_EPSILON)
			{
				return	mBack.RayCastBool(pntA, pntB);
			}
			else	//split up segment
			{
				float	splitRatio	=d / (d - d2);
				Vector3	mid	=pntA + (splitRatio * (pntB - pntA));

				bool	bHit;

				if(mFront != null)
				{
					if(d > 0.0f)
					{
						bHit	=mFront.RayCastBool(pntA, mid);
					}
					else
					{
						bHit	=mFront.RayCastBool(mid, pntB);
					}
					if(!bHit)
					{
						return	bHit;
					}
				}
				if(d2 > 0.0f)
				{
					return	mBack.RayCastBool(pntA, mid);
				}
				else
				{
					return	mBack.RayCastBool(mid, pntB);
				}
			}
		}


		//returns true for solid
		public bool GetLeafLandedIn(Vector3 pnt, out BspNode bn)
		{
			float	d;

			bn	=this;
			if(mbLeaf)
			{
				d	=Vector3.Dot(mPlane.Normal, pnt) - mPlane.Dist;
				if(d < Face.ON_EPSILON)
				{
					return	true;
				}
				else
				{
					return	true;
				}
			}

			d	=Vector3.Dot(mPlane.Normal, pnt) - mPlane.Dist;

			if(d > -Face.ON_EPSILON)
			{
				return	mFront.ClassifyPoint(pnt);
			}
			else if(d < Face.ON_EPSILON)
			{
				return	mBack.ClassifyPoint(pnt);
			}
			else if(d > 0.0f)
			{
				return	mFront.ClassifyPoint(pnt);
			}
			else
			{
				return	mBack.ClassifyPoint(pnt);
			}
		}
		#endregion


		#region PortalStuff
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
			if((mContents & CONTENTS_SOLID) != 0)
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


		private void FillUnTouchedLeafs(UInt32 curFill)
		{
			if(!mbLeaf)
			{
				mFront.FillUnTouchedLeafs(curFill);
				mBack.FillUnTouchedLeafs(curFill);
				return;
			}

			if((mContents & CONTENTS_SOLID) != 0)
			{
				return;		//allready solid or removed...
			}

			if(mFillKey != curFill)
			{
				// Fill er in with solid so it does not show up...(Preserve user contents)
				mContents	&=(0xffff0000);
				mContents	|=CONTENTS_SOLID;
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
				mPortal.Draw(g, fx);
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
					mPortal.Draw(g, fx);
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
					mPortal.Draw(g, fx);
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
					mPortal.Draw(g, fx);
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
					mPortal	=new Face(mPlane, mFace);
					mParent.ClipToParents(mPortal);
					mPortal.mFlags	=0;

					if(mFront != null && (mFront.mContents & CONTENTS_SOLID) != 0)
					{
						mPortal.mFlags	|=1;
					}

					if((mBack.mContents & CONTENTS_SOLID) != 0)
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
		#endregion
	}
}
