using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace BSPLib
{
	class BuildContext
	{
		internal	BspNode		This;
		internal	List<Brush>	Brushes;
		internal	bool		mbBevel;
	}

	public class BspNode
	{
		public Plane	mPlane;
		public BspNode	mFront, mBack, mParent;
		public Brush	mBrush;
		public Bounds	mBounds;
		public Portal	mPortals;

		public static List<Brush>	TroubleBrushes	=new List<Brush>();

		static float	fraction	=0.0f;


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
		static internal bool FindGoodSplitFace(List<Brush> brushList, out Face bestFace, object prog)
		{
			int		BestIndex	=-1;
			float	BestScore	=696969.0f;

			if(prog != null)
			{
				ProgressWatcher.UpdateProgress(prog, 0, brushList.Count, 0);
			}

			foreach(Brush b in brushList)
			{
				float	score	=b.GetBestSplittingFaceScore(brushList);

				ProgressWatcher.UpdateProgress(prog, 0, brushList.Count, brushList.IndexOf(b));

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


		internal void GetTriangles(List<Vector3> tris, List<UInt32> ind, bool bCheckFlags)
		{
			if(mBrush != null)
			{
				mBrush.GetTriangles(tris, ind, bCheckFlags);
				return;
			}
			mFront.GetTriangles(tris, ind, bCheckFlags);
			mBack.GetTriangles(tris, ind, bCheckFlags);
		}


		internal void GetFaces(List<Face> planes)
		{
			if(mBrush != null)
			{
				mBrush.GetFaces(planes);
				return;
			}
			planes.Add(new Face(mPlane, null));

			mFront.GetFaces(planes);
			mBack.GetFaces(planes);
		}
		#endregion


		#region IO
		public virtual void Write(BinaryWriter bw)
		{
			//write plane
			bw.Write(mPlane.mNormal.X);
			bw.Write(mPlane.mNormal.Y);
			bw.Write(mPlane.mNormal.Z);
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
			mPlane.mNormal.Z	=br.ReadSingle();
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


		void GatherFaces(List<Face> faces)
		{
			if(mBrush != null)
			{
				faces.AddRange(mBrush.GetFaces());
				return;
			}
			else
			{
				Face	f	=new Face(mPlane, null);
				f.mFlags	|=Face.SURF_SKIP;
				faces.Add(f);
			}
			mFront.GatherFaces(faces);
			mBack.GatherFaces(faces);
		}


		void MakeLeaf(List<Brush> brushList)
		{
			if(brushList.Count != 1)
			{
				Map.Print("Merging " + brushList.Count + " brushes in a leaf");
			}

			if(mParent != null)
			{
				mPlane	=mParent.mPlane;
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
				//tiny removal broke something
				Map.Print("Bad brush in leaf!");

				//remake without sealing
				mBrush	=new Brush();
				foreach(Brush b in brushList)
				{
					mBrush.AddFaces(b.GetFaces());
				}
				if(mBrush.IsValid())
				{
					Map.Print("Unsealing seems to have fixed it...");
				}
				else
				{
					Map.Print("Brush is hozed, compile will likely fail...");
					lock(TroubleBrushes)
					{
						TroubleBrushes.Add(mBrush);
					}
					//brush is messed up, donut add it
					mBrush	=null;
				}
			}
		}


		internal void AddToTree(List<Brush> brushList)
		{
			List<Brush>	frontList	=new List<Brush>();
			List<Brush>	backList	=new List<Brush>();

			Face	splitFace	=new Face(mPlane, null);

			//split the entire list into front and back
			foreach(Brush b in brushList)
			{
				Brush	bf, bb;

				b.SplitBrush(splitFace, out bf, out bb);

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

			//see if we are at a leaf
			bool	bLeaf	=false;
			if(mBrush != null)
			{
				backList.Add(mBrush);
				mBrush	=null;
				bLeaf	=true;
			}

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

			if(bLeaf)
			{
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
				else
				{
					mFront	=new BspNode();
					mFront.BuildTree(frontList, null);
					mBack	=new BspNode();
					mBack.BuildTree(backList, null);
				}
			}
			else
			{
				if(frontList.Count > 0)
				{
					AddToTree(frontList);
				}
				else
				{
					Debug.Assert(false);// && "Nonleaf node with no front side brushes!");
				}

				if(backList.Count > 0)
				{
					AddToTree(backList);
				}
				else
				{
					Debug.Assert(false);// && "Nonleaf node with no back side brushes!");
				}
			}
		}


		//builds a bsp good for collision info
		internal void BuildTree(List<Brush> brushList, object prog)
		{
			List<Brush>	frontList	=new List<Brush>();
			List<Brush>	backList	=new List<Brush>();

			Face	face;

			ProgressWatcher.UpdateProgress(prog, brushList.Count);

			if(!FindGoodSplitFace(brushList, out face, null))
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
				mFront	=new BspNode();
				mFront.mParent	=this;
				mFront.BuildTree(frontList, prog);
			}
			else
			{
				Debug.Assert(false);// && "Nonleaf node with no front side brushes!");
			}

			if(backList.Count > 0)
			{
				mBack	=new BspNode();
				mBack.mParent	=this;
				mBack.BuildTree(backList, prog);
			}
			else
			{
				Debug.Assert(false);// && "Nonleaf node with no back side brushes!");
			}
		}


		//grab all the brushes in the tree
		internal void GatherNodeBrushes(List<Brush> brushes)
		{
			if(mBrush != null)
			{
				brushes.Add(mBrush);
				return;
			}

			mFront.GatherNodeBrushes(brushes);
			mBack.GatherNodeBrushes(brushes);
		}


		//make the node bounds
		internal Bounds Bound()
		{
			if(mBrush != null)
			{
				mBrush.BoundBrush();
				mBounds	=new Bounds(mBrush.GetBounds());
				return	mBounds;
			}

			mBounds	=new Bounds();
			mBounds.Merge(null, mFront.Bound());
			mBounds.Merge(null, mBack.Bound());

			return	mBounds;
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
			else if(d >= Plane.EPSILON)
			{
				return	mFront.ClassifyPoint(pnt);
			}
			else
			{
				Debug.Assert(false);
				return	false;
			}
		}


		internal BspNode GetNodeLandedIn(Vector3 pnt)
		{
			float	d;

			if(mBrush != null)
			{
				return	this;
			}

			d	=Vector3.Dot(mPlane.mNormal, pnt) - mPlane.mDistance;

			if(d < 0.0f)
			{
				BspNode	landed	=mBack.GetNodeLandedIn(pnt);
				if(landed != null)
				{
					return	landed;
				}
			}
			else
			{
				BspNode	landed	=mFront.GetNodeLandedIn(pnt);
				if(landed != null)
				{
					return	landed;
				}
			}
			return	null;
		}


		internal bool ClassifyPoint(Vector3 pnt, float radius)
		{
			float	d;

			if(mBrush != null)
			{
				return	mBrush.IsPointInside(pnt, radius);
			}

			d	=Vector3.Dot(mPlane.mNormal, pnt) - mPlane.mDistance;

			if(d < radius)
			{
				if(mBack.ClassifyPoint(pnt, radius))
				{
					return	true;
				}
			}
			if(d >= -radius)
			{
				return	mFront.ClassifyPoint(pnt, radius);
			}
			return	false;
		}


		//used for gameish movement
		void ClipRay(Line ogLine, Vector3 ray, Plane hit, out Vector3 newStart, out Vector3 newEnd)
		{
			//back up along the ray to get out
			//of the plane
			float	len			=ray.Length();
			float	originalLen	=(ogLine.mP2 - ogLine.mP1).Length();

			Debug.Assert(!ClassifyPoint(ogLine.mP1));

			if(len < 0.001)
			{
				//the ray passed in is right on
				//the hit plane, use the OG
				ray	=ogLine.mP2 - ogLine.mP1;
				len	=ray.Length();

				Debug.Assert(len > 0.0f);
			}

			ray	/=len;

			//grab the dot product of the normalized
			//ray to the hit normal, to use for reflecting
			float	dot	=Vector3.Dot(hit.mNormal, ray);

			if(len <= 0.11f)
			{
				//not enough movement
				//to bother using the new point
				newStart	=ogLine.mP1;
			}
			else
			{
				//shorten
				len	-=0.1f;

				//this might shorten beyond the
				//original point
				if(len < 0.1f)
				{
					//not enough movement
					//to bother using the new point
					newStart	=ogLine.mP1;
				}
				else
				{
					ray	*=len;

					//new start point
					newStart	=ray + ogLine.mP1;

					//make sure out of the plane
					while(ClassifyPoint(newStart))
					{
						ray	*=0.9f;
						if(ray.Length() < 0.1f)
						{
							//not enough movement
							//to bother using the new point
							newStart	=ogLine.mP1;
							break;
						}
						newStart	=ray + ogLine.mP1;
					}
				}
			}

			//the new endpoint is the original end point
			newEnd	=ogLine.mP2;

			float	endDist	=Vector3.Dot(hit.mNormal, newEnd) - hit.mDistance;

			//push out of the plane a little
			endDist	-=0.1f;

			//pushed out of the impacted plane
			//and projected by the remaining veclength
			newEnd	-=hit.mNormal * endDist;
		}


		//used for gameish movement
		void ClipRay(Line ogLine, Vector3 ray, Plane hit, out Vector3 newStart, out Vector3 newEnd, float radius)
		{
			//back up along the ray to get out
			//of the plane
			float	len			=ray.Length();
			float	originalLen	=(ogLine.mP2 - ogLine.mP1).Length();

			Debug.Assert(!ClassifyPoint(ogLine.mP1, radius));

			if(len < 0.001f)
			{
				//the ray passed in is right on
				//the hit plane, use the OG
				ray	=ogLine.mP2 - ogLine.mP1;
				len	=ray.Length();

				Debug.Assert(len > 0.0f);
			}

			if(len > 0.0f)
			{
				ray	/=len;
			}

			if(len <= 0.11f)
			{
				//not enough movement
				//to bother using the new point
				newStart	=ogLine.mP1;
			}
			else
			{
				//shorten
				len	-=0.1f;

				//this might shorten beyond the
				//original point
				if(len < 0.1f)
				{
					//not enough movement
					//to bother using the new point
					newStart	=ogLine.mP1;
				}
				else
				{
					ray	*=len;

					//new start point
					newStart	=ray + ogLine.mP1;

					//make sure out of the plane
					while(ClassifyPoint(newStart, radius))
					{
						ray	*=0.9f;
						if(ray.Length() < 0.1f)
						{
							//not enough movement
							//to bother using the new point
							newStart	=ogLine.mP1;
							break;
						}
						newStart	=ray + ogLine.mP1;
					}
				}
			}

			//the new endpoint is the original end point
			newEnd	=ogLine.mP2;

			float	endDist	=Vector3.Dot(hit.mNormal, newEnd) - hit.mDistance;

			//adjust planes for radius
			endDist	-=radius;

			//push out of the plane a little
			endDist	-=0.1f;

			//pushed out of the impacted plane
			//and projected by the remaining veclength
			newEnd	-=hit.mNormal * endDist;
		}


		public bool MoveLine(ref Line ln)
		{
			List<ClipSegment>	segs	=new List<ClipSegment>();

			Debug.Assert(!ClassifyPoint(ln.mP1));

			RayCastBrushes(ln, ref segs);
			if(segs.Count <= 0)
			{
				return	false;
			}

			//check each segment for a valid motion
			float		longest		=-69.0f;
			ClipSegment	longClip	=null;
			foreach(ClipSegment seg in segs)
			{
				Vector3	newStart	=seg.mSeg.mP1;
				Vector3	newEnd		=seg.mSeg.mP2;

				if(ClassifyPoint(newStart))
				{
					continue;
				}

				//cast a new ray from the impact point
				//and the new end point
				Line	ln2;
				ln2.mP1	=newStart;
				ln2.mP2	=newEnd;

				List<ClipSegment>	bounce	=new List<ClipSegment>();
				RayCastBrushes(ln2, ref bounce);
				if(segs.Count <= 0)
				{
					//assign ray
					ln.mP2	=newEnd;
					return	true;
				}
				ClipSegment	segBounce	=ClipSegment.GetNearest(bounce, ln2.mP1);

				Vector3	ray		=segBounce.mSeg.mP1 - newStart;
				float	rayLen	=ray.LengthSquared();
				if(rayLen > longest)
				{
					longest		=rayLen;
					longClip	=seg;
				}
			}
			if(longClip != null)
			{
				ln.mP2	=longClip.mSeg.mP2;
			}

			return	true;
		}


		public bool MoveLine(ref Line ln, float radius)
		{
			List<ClipSegment>	segs	=new List<ClipSegment>();

			Debug.Assert(!ClassifyPoint(ln.mP1, radius));

			RayCastBrushes(ln, ref segs, radius);
			if(segs.Count <= 0)
			{
				return	false;
			}

			//check each segment for a valid motion
			float		longest		=-69.0f;
			ClipSegment	longClip	=null;
			foreach(ClipSegment seg in segs)
			{
				//grab the bounced off movement
				Line	bounced	=seg.mSplitPlane.BounceLine(ln, radius);

				Vector3	newStart	=bounced.mP1;
				Vector3	newEnd		=bounced.mP2;

				//make sure the start point is valid
				if(ClassifyPoint(newStart, radius))
				{
					continue;
				}

				//cast a new ray from the impact point
				//and the new end point
				Line	ln2;
				ln2.mP1	=newStart;
				ln2.mP2	=newEnd;

				List<ClipSegment>	bounce	=new List<ClipSegment>();
				RayCastBrushes(ln2, ref bounce, radius);
				if(bounce.Count <= 0)
				{
					//assign ray
					ln.mP2	=newEnd;
					return	true;
				}
				foreach(ClipSegment segBounce in bounce)
				{
					Line	bouncedBounce	=segBounce.mSplitPlane.BounceLine(ln2, radius);
					Vector3	ray		=bouncedBounce.mP2 - newStart;
					float	rayLen	=ray.LengthSquared();
					if(rayLen > longest)
					{
						longest		=rayLen;
						longClip	=segBounce;
					}
				}
			}
			if(longClip != null)
			{
				ln.mP2	=longClip.mSeg.mP2;
			}

			return	true;
		}


		public void RayCastBrushes(Line ln, ref List<ClipSegment> segs)
		{
			float	d	=Vector3.Dot(mPlane.mNormal, ln.mP1) - mPlane.mDistance;
			float	d2	=Vector3.Dot(mPlane.mNormal, ln.mP2) - mPlane.mDistance;

			if(mBrush != null)
			{
				Debug.Assert(mFront == null && mBack == null);
				mBrush.RayCast(ln, ref segs);
				return;
			}

			if(d < Plane.EPSILON && d2 < Plane.EPSILON)
			{
				if(mBack != null)
				{
					mBack.RayCastBrushes(ln, ref segs);
				}
			}
			else if(d >= Plane.EPSILON && d2 >= Plane.EPSILON)
			{
				if(mFront != null)
				{
					mFront.RayCastBrushes(ln, ref segs);
				}
			}
			else
			{
				float	splitRatio	=d / (d - d2);
				Vector3	mid			=ln.mP1 + (splitRatio * (ln.mP2 - ln.mP1));

				if(d >= 0.0)
				{
					if(mFront != null)
					{
						Line	ln2;
						ln2.mP1	=ln.mP1;
						ln2.mP2	=mid;
						mFront.RayCastBrushes(ln2, ref segs);
					}
					if(mBack != null)
					{
						Line	ln2;
						ln2.mP1	=mid;
						ln2.mP2	=ln.mP2;
						mBack.RayCastBrushes(ln2, ref segs);
					}
				}
				else
				{
					if(mFront != null)
					{
						Line	ln2;
						ln2.mP1	=mid;
						ln2.mP2	=ln.mP2;
						mFront.RayCastBrushes(ln2, ref segs);
					}
					if(mBack != null)
					{
						Line	ln2;
						ln2.mP1	=ln.mP1;
						ln2.mP2	=mid;
						mBack.RayCastBrushes(ln2, ref segs);
					}
				}
			}
		}


		public void RayCastBrushes(Line ln, ref List<ClipSegment> segs, float radius)
		{
			if(mBrush != null)
			{
				Debug.Assert(mFront == null && mBack == null);
				mBrush.RayCast2(ln, ref segs, radius);
				return;
			}

//			float	d1	=Vector3.Dot(mPlane.mNormal, ln.mP1) - mPlane.mDistance;
//			float	d2	=Vector3.Dot(mPlane.mNormal, ln.mP2) - mPlane.mDistance;

//			if(d1 > radius && d2 > radius)
			{
				mFront.RayCastBrushes(ln, ref segs, radius);
			}
//			else if(d1 < -radius && d2 < -radius)
			{
				mBack.RayCastBrushes(ln, ref segs, radius);
			}
/*			else
			{
				float	splitRatio	=d1 / (d1 - d2);
				Vector3	mid			=ln.mP1 + (splitRatio * (ln.mP2 - ln.mP1));
				if(d1 < d2)
				{
					Line	ln2;
					ln2.mP1	=ln.mP1;
					ln2.mP2	=mid;
					mFront.RayCastBrushes(ln2, ref segs);
					ln2.mP1	=mid;
					ln2.mP2	=ln.mP2;
					mBack.RayCastBrushes(ln2, ref segs);
				}
				else if(d1 > d2)
				{
					Line	ln2;
					ln2.mP1	=mid;
					ln2.mP2	=ln.mP2;
					mFront.RayCastBrushes(ln2, ref segs);
					ln2.mP1	=ln.mP1;
					ln2.mP2	=mid;
					mBack.RayCastBrushes(ln2, ref segs);
				}
			}*/
		}


		internal void RayCast3(Vector3 start, Vector3 end, List<Ray> rayParts)
		{
			float	d	=Vector3.Dot(mPlane.mNormal, start) - mPlane.mDistance;
			float	d2	=Vector3.Dot(mPlane.mNormal, end) - mPlane.mDistance;

			if(mBrush != null)
			{
				Debug.Assert(mFront == null && mBack == null);
				mBrush.RayCast3(start, end, rayParts);
				return;
			}

			if(d < Plane.EPSILON && d2 < Plane.EPSILON)
			{
				if(mBack != null)
				{
					mBack.RayCast3(start, end, rayParts);
				}
			}
			else if(d >= Plane.EPSILON && d2 >= Plane.EPSILON)
			{
				if(mFront != null)
				{
					mFront.RayCast3(start, end, rayParts);
				}
			}
			else
			{
				d	=-(Plane.EPSILON - d);
				d2	=-(Plane.EPSILON - d2);
				float	splitRatio	=d / (d - d2);
				Vector3	mid			=start + (splitRatio * (end - start));

				if(d >= 0.0)
				{
					if(mFront != null)
					{
						Vector3	newStart, newEnd;
						newStart	=start;
						newEnd		=mid;
						mFront.RayCast3(newStart, newEnd, rayParts);
					}
					if(mBack != null)
					{
						Vector3	newStart, newEnd;
						newStart	=mid;
						newEnd		=end;
						mBack.RayCast3(newStart, newEnd, rayParts);
					}
				}
				else
				{
					if(mFront != null)
					{
						Vector3	newStart, newEnd;
						newStart	=mid;
						newEnd		=end;
						mFront.RayCast3(newStart, newEnd, rayParts);
					}
					if(mBack != null)
					{
						Vector3	newStart, newEnd;
						newStart	=start;
						newEnd		=mid;
						mBack.RayCast3(newStart, newEnd, rayParts);
					}
				}
			}
		}

		internal bool MoveLine2(ref Line ln, float radius)
		{
			List<ClipSegment>	segs	=new List<ClipSegment>();

			Debug.Assert(!ClassifyPoint(ln.mP1, radius));

			RayCastBrushes(ln, ref segs, radius);
			if(segs.Count <= 0)
			{
				return	false;
			}
			ClipSegment	seg	=ClipSegment.GetNearest(segs, ln.mP1);

			Vector3	newStart	=Vector3.Zero;
			Vector3	newEnd		=Vector3.Zero;

			Vector3	ray	=seg.mSeg.mP1 - ln.mP1;

			ClipRay(ln, ray, seg.mSplitPlane, out newStart, out newEnd, radius);

			//cast a new ray from the impact point
			//and the new end point
			Line	ln2;
			ln2.mP1	=newStart;
			ln2.mP2	=newEnd;

			segs.Clear();
			RayCastBrushes(ln2, ref segs, radius);

			if(segs.Count <= 0)
			{
				//assign ray
				ln.mP2	=newEnd;
				return	true;
			}
			seg	=ClipSegment.GetNearest(segs, ln.mP1);

			ray	=seg.mSeg.mP1 - newStart;
			ClipRay(ln2, ray, seg.mSplitPlane, out newStart, out newEnd, radius);

			//one bounce is good enough for now
			ln.mP2	=newStart;
			return	true;
		}
	}
}
