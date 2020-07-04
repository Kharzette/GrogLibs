using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using SharpDX;
using UtilityLib;


namespace BSPZone
{
	//just the facts on an impact
	public class Collision
	{
		public Vector3		mIntersection;
		public ZonePlane	mPlaneHit;
		public bool			mbStartInside;
		public int			mModelHit;
		public DebugFace	mFaceHit;
	}

	//passed down into the recursive routines
	class RayTrace
	{
		internal Collision	mCollision	=new Collision();

		internal Vector3		mOriginalStart, mOriginalEnd;
		internal BoundingBox?	mBounds;
		internal float?			mRadius;

		internal RayTrace(Vector3 start, Vector3 end)
		{
			mOriginalStart	=start;
			mOriginalEnd	=end;
		}
	}


	public partial class Zone
	{
		Vector3	[]mTestBoxCorners		=new Vector3[8];
		Vector3	[]mTestTransBoxCorners	=new Vector3[8];


		#region Public Collision Interface
		//returns the closest impact, checks all models
		//everything returned in worldspace
		public bool TraceAll(float? radius, BoundingBox? box,
			Vector3 start, Vector3 end,
			out Collision col)
		{
			List<Collision>	collisions	=new List<Collision>();

			for(int i=0;i < mZoneModels.Length;i++)
			{
				if(mNonCollidingModels.Contains(i))
				{
					continue;	//don't bother vs triggers etc
				}

				RayTrace	rt	=new RayTrace(start, end);

				if(radius != null)
				{
					rt.mRadius		=radius.Value;
				}
				if(box != null)
				{
					rt.mBounds	=box.Value;
				}

				rt.mCollision.mModelHit	=i;

				if(i == 0)
				{
					if(TraceNode(rt, start, end, mZoneModels[i].mRootNode))
					{
						collisions.Add(rt.mCollision);
					}
				}
				else
				{
					ZoneModel	zm	=mZoneModels[i];

					if(box == null)
					{
						//the sphere routine doesn't automagically transform
						Vector3		modelStart	=Vector3.TransformCoordinate(start, zm.mInvertedTransform);
						Vector3		modelEnd	=Vector3.TransformCoordinate(end, zm.mInvertedTransform);
						rt.mOriginalStart		=modelStart;
						rt.mOriginalEnd			=modelEnd;

						if(TraceNode(rt, modelStart, modelEnd, mZoneModels[i].mRootNode))
						{
							Collision	c	=rt.mCollision;

							//transform results back to worldspace
							c.mIntersection	=Vector3.TransformCoordinate(c.mIntersection,
								mZoneModels[c.mModelHit].mTransform);

							c.mPlaneHit	=ZonePlane.Transform(c.mPlaneHit,
								mZoneModels[c.mModelHit].mTransform);

							collisions.Add(c);
						}
					}
					else
					{
						if(TraceFakeOrientedBoxModel(rt, start, end, zm))
						{
							collisions.Add(rt.mCollision);
						}
					}
				}
			}

			if(collisions.Count == 0)
			{
				col		=null;
				return	false;
			}

			int		bestIdx		=0;
			float	bestDist	=float.MaxValue;
			for(int i=0;i < collisions.Count;i++)
			{
				float	dist	=Vector3.DistanceSquared(collisions[i].mIntersection, start);
				if(dist < bestDist)
				{
					bestDist	=dist;
					bestIdx		=i;
				}
			}

			col	=collisions[bestIdx];

			//fill in face info
			//TODO: make it optional?
			Vector3	facePos	=col.mIntersection;
			col.mPlaneHit.ReflectPosition(ref facePos);

			col.mFaceHit	=GetFaceForPoint(facePos, col.mModelHit);

			return	true;
		}


		//drop a box to the ground
		public Vector3 DropToGround(BoundingBox box, Vector3 pos, bool bUseModels)
		{
			Collision	col;

			RayTrace	trace	=new RayTrace(pos, pos + (Vector3.UnitY * - 300f));

			trace.mBounds	=box;

			bool	bHit	=false;
			if(bUseModels)
			{
				bHit	=TraceAll(null, null, trace.mOriginalStart,
					trace.mOriginalEnd, out col);
			}
			else
			{
				bHit	=TraceNode(trace, trace.mOriginalStart,
					trace.mOriginalEnd, mZoneModels[0].mRootNode);
			}

			if(bHit && trace.mCollision.mbStartInside)
			{
				trace.mCollision.mPlaneHit.ReflectPosition(ref pos);
			}
			else if(bHit)
			{
				pos	=trace.mCollision.mIntersection + (trace.mCollision.mPlaneHit.mNormal * Mathery.ON_EPSILON);
			}
			return	pos;
		}


		//drop a worldspace position to the "floor"
		public Vector3 DropToGround(Vector3 pos, bool bUseModels)
		{
			Collision	col;

			RayTrace	trace	=new RayTrace(pos, pos + (Vector3.UnitY * - 300f));

			bool	bHit	=false;
			if(bUseModels)
			{
				bHit	=TraceAll(null, null, trace.mOriginalStart,
					trace.mOriginalEnd, out col);
			}
			else
			{
				bHit	=TraceNode(trace, trace.mOriginalStart,
					trace.mOriginalEnd, mZoneModels[0].mRootNode);
			}

			if(bHit && trace.mCollision.mbStartInside)
			{
				trace.mCollision.mPlaneHit.ReflectPosition(ref pos);
			}
			else if(bHit)
			{
				pos	=trace.mCollision.mIntersection + (trace.mCollision.mPlaneHit.mNormal * Mathery.ON_EPSILON);
			}
			return	pos;
		}


		//get the floor normal underneath pos
		public ZonePlane GetGroundNormal(Vector3 pos, bool bUseModels)
		{
			Collision	col;

			RayTrace	trace	=new RayTrace(pos, pos + (Vector3.UnitY * - 300f));

			bool	bHit	=false;
			if(bUseModels)
			{
				bHit	=TraceAll(null, null, trace.mOriginalStart,
					trace.mOriginalEnd, out col);
			}
			else
			{
				bHit	=TraceNode(trace, trace.mOriginalStart,
					trace.mOriginalEnd, mZoneModels[0].mRootNode);
			}
			return	trace.mCollision.mPlaneHit;
		}


		public UInt32	GetWorldContents(Vector3 pos)
		{
			Int32	node	=FindWorldNodeLandedIn(pos);
			if(node > 0)
			{
				return	0;
			}

			Int32	leafIdx	=-(node + 1);
			
			return	mZoneLeafs[leafIdx].mContents;
		}


		public Int32 FindWorldNodeLandedIn(Vector3 pos)
		{
			return	FindNodeLandedIn(mZoneModels[0].mRootNode, pos);
		}


		public Int32 FindNodeLandedIn(Int32 node, Vector3 pos)
		{
			float		dist;
			ZoneNode	pNode;

			if(node < 0)		// At leaf, no more recursing
			{
				return	node;
			}

			pNode	=mZoneNodes[node];
			
			//Get the distance that the eye is from this plane
			dist	=mZonePlanes[pNode.mPlaneNum].Distance(pos);

			//Go down the side we are on first, then the other side
			Int32	ret	=0;
			ret	=FindNodeLandedIn((dist < 0)? pNode.mBack : pNode.mFront, pos);
			if(ret < 0)
			{
				return	ret;
			}
			ret	=FindNodeLandedIn((dist < 0)? pNode.mFront : pNode.mBack, pos);
			return	ret;
		}


		//trace a ray from the screen point through the camera
		//to intersect the world at the closest location.
		//Starting outside the world is fine
		public Vector3 TraceScreenPointRay(GameCamera cam,
			Viewport vp, Vector2 screenPos,
			float rayDistance, out int modelOn)
		{
			Vector3	camForward	=cam.Forward;
			Vector3	start		=new Vector3(screenPos.X, screenPos.Y, 0f);
			Vector3	end			=new Vector3(screenPos.X, screenPos.Y, 1f);

			start	=vp.Unproject(start, cam.Projection, cam.View, Matrix.Identity);
			end		=vp.Unproject(end, cam.Projection, cam.View, Matrix.Identity);

			Vector3	ray	=end - start;

			ray.Normalize();

			start	=cam.Position;
			end		=start + ray * rayDistance;

			List<Zone.TraceSegment>	segz	=new List<Zone.TraceSegment>();

			TraceSegWorld(start, end, 0, segz);

			float	bestDist	=float.MaxValue;
			Vector3	bestStart	=Vector3.Zero;
			foreach(Zone.TraceSegment seg in segz)
			{
				if(Misc.bFlagSet(seg.mContents, BSPZone.Contents.BSP_CONTENTS_SOLID2))
				{
					continue;
				}

				float	dist	=Vector3.Distance(seg.mStart, start);
				if(dist < bestDist)
				{
					//bump slightly inside
					bestStart	=seg.mStart + (ray * 0.1f);
					bestDist	=dist;
				}
			}

			Collision	col	=new Collision();

			Vector3	hitPos	=Vector3.Zero;
			if(TraceAll(null, null, bestStart, end, out col))
			{
				hitPos	=col.mIntersection + Vector3.UnitY;
				modelOn	=col.mModelHit;
			}
			else
			{
				hitPos	=end;
				modelOn	=-1;
			}
			return	hitPos;
		}


		//returns a list of models intersecting a nonmoving box
		public bool TraceStaticBoxVsModels(BoundingBox boxBounds, Vector3 pos, List<int> intersecting)
		{
			bool	bRet	=false;

			ZonePlane	zp	=ZonePlane.BlankX;

			for(int i=1;i < mZoneModels.Length;i++)
			{
				if(mNonCollidingModels.Contains(i))
				{
					continue;	//don't bother vs triggers etc
				}

				if(IntersectBoxModel(boxBounds, pos, i, ref zp))
				{
					bRet	=true;
					intersecting.Add(i);
				}
			}
			return	bRet;
		}
		#endregion


		#region Movement Specific Collision Routines
		//expects and returns worldspace values
		bool TraceFakeOrientedBoxModel(RayTrace rt, Vector3 start, Vector3 end, ZoneModel mod)
		{
			//make a copy of the bounds
			BoundingBox	boxCopy	=rt.mBounds.Value;
			BoundingBox	box		=boxCopy;

			FakeOrientBoxCollisionToModel(mod, ref box, ref start, ref end);

			rt.mOriginalStart	=start;
			rt.mOriginalEnd		=end;
			rt.mBounds			=box;

			bool	bHit	=TraceNode(rt, start, end, mod.mRootNode);

			//copy bounds back
			rt.mBounds	=boxCopy;

			Collision	col	=rt.mCollision;

			if(bHit && !col.mbStartInside)
			{
				col.mIntersection	=Vector3.TransformCoordinate(col.mIntersection, mod.mTransform);
				col.mPlaneHit		=ZonePlane.Transform(col.mPlaneHit, mod.mTransform);
			}
			return	bHit;
		}


		//expects and returns worldspace values
		internal bool IntersectBoxModel(BoundingBox box, Vector3 pos, int modelIndex, ref ZonePlane zp)
		{
			return	IntersectBoxModel(box, pos, mZoneModels[modelIndex], ref zp);
		}


		//expects and returns worldspace values
		bool IntersectBoxModel(BoundingBox box, Vector3 pos, ZoneModel mod, ref ZonePlane zp)
		{
#if DEBUG
			Debug.Assert(Mathery.IsBoundingBoxCentered(box));
#endif

			//get box bound corners
			box.GetCorners(mTestBoxCorners);

			//transform into model space
			Vector3.TransformCoordinate(mTestBoxCorners, ref mod.mInvertedTransform, mTestTransBoxCorners);

			//transform position
			pos	=Vector3.TransformCoordinate(pos, mod.mInvertedTransform);

			//bound the modelSpace corners
			box	=BoundingBox.FromPoints(mTestTransBoxCorners);

			Mathery.CenterBoundingBoxAtOrigin(ref box);

			bool	bHit	=IntersectBoxNode(box, pos, mod.mRootNode, ref zp);
			if(bHit)
			{
				zp	=ZonePlane.Transform(zp, mod.mTransform);
			}
			return	bHit;
		}


		//returns the closest impact, checks all models
		//everything returned in worldspace
		//this is used as a second pass for movement, as you want
		//the world stuff resolved first
		bool TraceModelsBox(BoundingBox boxBounds, Vector3 start, Vector3 end, out Collision col)
		{
			List<Collision>	collisions	=new List<Collision>();

			for(int i=1;i < mZoneModels.Length;i++)
			{
				if(mNonCollidingModels.Contains(i))
				{
					continue;	//don't bother vs triggers etc
				}

				RayTrace	rt	=new RayTrace(start, end);
				ZoneModel	zm	=mZoneModels[i];
				rt.mBounds		=boxBounds;

				rt.mCollision.mModelHit	=i;

				if(TraceFakeOrientedBoxModel(rt, start, end, zm))
				{
					if(rt.mCollision.mbStartInside)
					{
						col	=rt.mCollision;
						return	true;
					}
					collisions.Add(rt.mCollision);
				}
			}

			if(collisions.Count == 0)
			{
				col		=null;
				return	false;
			}

			int		bestIdx		=0;
			float	bestDist	=float.MaxValue;
			for(int i=0;i < collisions.Count;i++)
			{
				float	dist	=Vector3.DistanceSquared(collisions[i].mIntersection, start);
				if(dist < bestDist)
				{
					bestDist	=dist;
					bestIdx		=i;
				}
			}

			col	=collisions[bestIdx];

			return	true;
		}
		#endregion


		#region Fake OBB Adjust
		//warning, this only works with centered bounding boxes!
		//build a good box for collision vs a model
		void FakeOrientBoxCollisionToModel(ZoneModel mod,
			ref BoundingBox box, ref Vector3 start, ref Vector3 end)
		{
#if DEBUG
			Debug.Assert(Mathery.IsBoundingBoxCentered(box));
#endif

			//get box bound corners
			box.GetCorners(mTestBoxCorners);

			//transform into model space
			Vector3.TransformCoordinate(mTestBoxCorners, ref mod.mInvertedTransform, mTestTransBoxCorners);

			//transform ray
			start	=Vector3.TransformCoordinate(start, mod.mInvertedTransform);
			end		=Vector3.TransformCoordinate(end, mod.mInvertedTransform);

			//bound the modelSpace corners
			box	=BoundingBox.FromPoints(mTestTransBoxCorners);

			Mathery.CenterBoundingBoxAtOrigin(ref box);
		}
		#endregion


		#region Trigger Stuff
		//expects start and end relative to whatever model owns the nodes
		bool TraceNodeTrigger(RayTrace trace, Vector3 start, Vector3 end, Int32 node)
		{
			bool	bHit	=false;

			if(node < 0)
			{
				Int32		leafIdx		=-(node + 1);
				ZoneLeaf	zl			=mZoneLeafs[leafIdx];

				if(Misc.bFlagSet(zl.mContents, Contents.BSP_CONTENTS_TRIGGER))
				{
					return	true;
				}
				return	false;
			}

			ZoneNode	zn	=mZoneNodes[node];
			ZonePlane	p	=mZonePlanes[zn.mPlaneNum];

			float	dist	=0.0f;
			if(trace.mBounds != null)
			{
				dist	=trace.mBounds.Value.BoxPlaneDistance(p.mNormal);
			}
			else if(trace.mRadius != null)
			{
				dist	=trace.mRadius.Value;
			}

			Vector3	clipStart, clipEnd;
			if(PartBehind(p, -dist, start, end, out clipStart, out clipEnd))
			{
				bHit	=TraceNodeTrigger(trace, clipStart, clipEnd, zn.mBack);
				if(bHit)
				{
					end	=trace.mCollision.mIntersection;
				}
			}
			if(PartFront(p, -dist, start, end, out clipStart, out clipEnd))
			{
				bHit	|=TraceNodeTrigger(trace, clipStart, clipEnd, zn.mFront);
				return	bHit;
			}
			return	bHit;
		}


		//expects worldspace values
		bool TraceFakeOrientedBoxTrigger(RayTrace rt, Vector3 start, Vector3 end, ZoneModel mod)
		{
			//make a copy of the bounds
			BoundingBox	boxCopy	=rt.mBounds.Value;
			BoundingBox	box		=boxCopy;

			FakeOrientBoxCollisionToModel(mod, ref box, ref start, ref end);

			rt.mOriginalStart	=start;
			rt.mOriginalEnd		=end;
			rt.mBounds			=box;

			bool	bHit	=TraceNodeTrigger(rt, start, end, mod.mRootNode);

			rt.mBounds	=boxCopy;

			return	bHit;
		}
		#endregion


		#region Helper Routines
		//clip a move segment to the front side
		bool PartFront(ZonePlane p, float distAdjust, Vector3 start, Vector3 end,
			out Vector3 clipStart, out Vector3 clipEnd)
		{
			clipStart	=start;
			clipEnd		=end;

			float	startDist	=p.Distance(start) - distAdjust;
			float	endDist		=p.Distance(end) - distAdjust;

			if(startDist > 0 && endDist > 0)
			{
				return	true;
			}

			if(startDist <= 0 && endDist <= 0)
			{
				return	false;
			}

			if(startDist > 0)
			{
				float	ratio	=startDist / (startDist - endDist);
				clipEnd			=start + ratio * (end - start);
			}
			else
			{
				float	ratio	=startDist / (startDist - endDist);
				clipStart		=start + ratio * (end - start);
			}
			return	true;
		}


		//clip a move segment to the back side
		bool PartBehind(ZonePlane p, float distAdjust, Vector3 start, Vector3 end,
			out Vector3 clipStart, out Vector3 clipEnd)
		{
			clipStart	=start;
			clipEnd		=end;

			float	startDist	=p.Distance(start) + distAdjust;
			float	endDist		=p.Distance(end) + distAdjust;

			if(startDist <= 0 && endDist <= 0)
			{
				return	true;
			}

			if(startDist > 0 && endDist > 0)
			{
				return	false;
			}

			if(startDist <= 0)
			{
				float	ratio	=startDist / (startDist - endDist);
				clipEnd			=start + ratio * (end - start);
			}
			else
			{
				float	ratio	=startDist / (startDist - endDist);
				clipStart		=start + ratio * (end - start);
			}
			return	true;
		}
		#endregion


		#region Core Recursive Collision Routines
		//model relative values
		internal bool TraceNode(RayTrace trace, Vector3 start, Vector3 end, Int32 node)
		{
			bool	bHit	=false;

			if(node < 0)
			{
				Int32		leafIdx		=-(node + 1);
				ZoneLeaf	zl			=mZoneLeafs[leafIdx];

				if(Misc.bFlagSet(zl.mContents, Contents.BSP_CONTENTS_SOLID_CLIP))
				{
					if(ClipToLeaf(trace, zl))
					{
						return	true;
					}
				}
				return	false;
			}

			ZoneNode	zn		=mZoneNodes[node];
			ZonePlane	p		=mZonePlanes[zn.mPlaneNum];

			Vector3	ray	=end - start;

			//get plane distance to boundBox side
			float	dist	=0.0f;
			if(trace.mBounds != null)
			{
				dist	=trace.mBounds.Value.BoxPlaneDistance(p.mNormal);
			}
			else if(trace.mRadius != null)
			{
				dist	=trace.mRadius.Value;
			}

			Vector3	clipStart, clipEnd;
			if(PartBehind(p, -dist, start, end, out clipStart, out clipEnd))
			{
				bHit	=TraceNode(trace, clipStart, clipEnd, zn.mBack);
				if(bHit)
				{
					end	=trace.mCollision.mIntersection;
				}
			}
			if(PartFront(p, -dist, start, end, out clipStart, out clipEnd))
			{
				bHit	|=TraceNode(trace, clipStart, clipEnd, zn.mFront);
				return	bHit;
			}
			return	bHit;
		}


		//expects pos relative to whatever model owns the nodes
		//plane returned in model space
		bool IntersectBoxNode(BoundingBox box, Vector3 pos, Int32 node, ref ZonePlane planeHit)
		{
			bool	bHit	=false;

			if(node < 0)
			{
				Int32		leafIdx		=-(node + 1);
				ZoneLeaf	zl			=mZoneLeafs[leafIdx];

				if(Misc.bFlagSet(zl.mContents, Contents.BSP_CONTENTS_SOLID_CLIP))
				{
					if(IntersectBoxLeaf(box, pos, zl, ref planeHit))
					{
						return	true;
					}
				}
				return	false;
			}

			ZoneNode	zn	=mZoneNodes[node];
			ZonePlane	p	=mZonePlanes[zn.mPlaneNum];

			float	dist	=box.BoxPlaneDistance(p.mNormal);

			Vector3	clipPos;
			if(PartBehind(p, -dist, pos, pos, out clipPos, out clipPos))
			{
				bHit	=IntersectBoxNode(box, clipPos, zn.mBack, ref planeHit);
			}
			if(PartFront(p, -dist, pos, pos, out clipPos, out clipPos))
			{
				bHit	|=IntersectBoxNode(box, clipPos, zn.mFront, ref planeHit);
				return	bHit;
			}
			return	bHit;
		}
		#endregion


		#region Leaf Clips
		bool ClipToLeaf(RayTrace trace, ZoneLeaf zl)
		{
			if(zl.mNumSides <= 0)
			{
				return	false;
			}

			Vector3	start	=trace.mOriginalStart;
			Vector3	end		=trace.mOriginalEnd;

			bool		bClipped	=false;
			bool		bAnyInFront	=false;
			ZonePlane	clipPlane	=ZonePlane.Blank;

			//clip the ray inside the leaf
			float		nearestFront	=float.MaxValue;
			ZonePlane	closeFront		=ZonePlane.Blank;
			for(int i=0;i < zl.mNumSides;i++)
			{
				ZoneLeafSide	side	=mZoneLeafSides[i + zl.mFirstSide];
				ZonePlane		p		=mZonePlanes[side.mPlaneNum];

				if(side.mbFlipSide)
				{
					p.Inverse();
				}

				float	distAdjust	=0f;

				if(trace.mBounds != null)
				{
					distAdjust	+=trace.mBounds.Value.BoxPlaneDistance(p.mNormal);
				}
				else if(trace.mRadius != null)
				{
					distAdjust	+=trace.mRadius.Value;
				}

				float	frontDist	=p.Distance(start) - distAdjust;
				float	backDist	=p.Distance(end) - distAdjust;

				//track the plane nearest to the front
				//just in case the startpoint is in solid
				float	absDist	=Math.Abs(frontDist);
				if(absDist < nearestFront)
				{
					nearestFront		=absDist;
					closeFront			=p;
					closeFront.mDist	+=distAdjust;
				}

				if(frontDist > 0 && backDist >= 0)
				{
					return	false;	//not intersecting
				}

				if(frontDist < 0 && backDist < 0)
				{
					continue;
				}

				//split
				float	ratio;
				Vector3	intersection;

				if(frontDist == 0 && backDist == 0)
				{
					intersection	=start;
				}
				else
				{
					ratio			=frontDist / (frontDist - backDist);
					intersection	=start + ratio * (end - start);
				}

				if(frontDist > 0)
				{
					bAnyInFront	=true;
					start		=intersection;
					clipPlane	=p;
					bClipped	=true;

					clipPlane.mDist	+=distAdjust;
				}
				else
				{
					end		=intersection;
				}
			}

			if(bClipped)
			{
				trace.mCollision.mIntersection	=start;
				trace.mCollision.mPlaneHit		=clipPlane;
			}
			else if(!bAnyInFront)
			{
				//started inside!
				trace.mCollision.mbStartInside	=true;

				//use the plane nearest to the front
				//might not be completely correct, but should be close
				trace.mCollision.mPlaneHit	=closeFront;

				return	true;
			}

			return	bClipped;
		}


		//all model relative
		bool IntersectBoxLeaf(BoundingBox box, Vector3 pos, ZoneLeaf zl, ref ZonePlane planeHit)
		{
			if(zl.mNumSides <= 0)
			{
				return	false;
			}

			bool		bIntersecting	=false;
			float		bestDist		=float.MinValue;
			ZonePlane	bestPlane		=ZonePlane.Blank;
			for(int i=0;i < zl.mNumSides;i++)
			{
				ZoneLeafSide	side	=mZoneLeafSides[i + zl.mFirstSide];
				ZonePlane		p		=mZonePlanes[side.mPlaneNum];

				if(side.mbFlipSide)
				{
					p.Inverse();
				}

				p.mDist	+=box.BoxPlaneDistance(p.mNormal);

				float	dist	=p.Distance(pos);
				if(dist > 0)
				{
					return	false;	//not intersecting
				}

				if(dist > bestDist)
				{
					bestDist	=dist;
					bestPlane	=p;
				}

				if(dist < 0)
				{
					bIntersecting	=true;
				}
			}

			planeHit	=bestPlane;

			return	bIntersecting;
		}
		#endregion


		#region Face Collisions
		//uses the add up the angles trick to determine point in poly
		float ComputeAngleSum(DebugFace df, Vector3 point)
		{
			float	dotSum	=0f;
			for(int i=0;i < df.mNumVerts;i++)
			{
				int	vIdx0	=i + df.mFirstVert;
				int	vIdx1	=((i + 1) % df.mNumVerts) + df.mFirstVert;

				vIdx0	=mDebugIndexes[vIdx0];
				vIdx1	=mDebugIndexes[vIdx1];

				Vector3	v1	=mDebugVerts[vIdx0] - point;
				Vector3	v2	=mDebugVerts[vIdx1] - point;

				float	len1	=v1.Length();
				float	len2	=v2.Length();

				if((len1 * len2) < 0.0001f)
				{
					return	MathUtil.TwoPi;
				}

				v1	/=len1;
				v2	/=len2;

				float	dot	=Vector3.Dot(v1, v2);

				//clamp these values or acos can NaN
				if(dot > 1f)
				{
					dot	=1f;
				}
				else if(dot < -1f)
				{
					dot	=-1f;
				}
				dotSum	+=(float)Math.Acos(dot);
			}
			return	dotSum;
		}


		//should pass in a point just off the front side of a struck plane
		DebugFace GetFaceForPoint(Vector3 pos, int modelIdx)
		{
			int		numFaces, firstFace;
			bool	bLeaf;

			int	node	=FindNodeLandedIn(mZoneModels[modelIdx].mRootNode, pos);
			if(node < 0)
			{
				node	=-(node + 1);

				ZoneLeaf	zl	=mZoneLeafs[node];
				numFaces		=zl.mNumFaces;
				firstFace		=zl.mFirstFace;
				bLeaf			=true;
			}
			else
			{
				ZoneNode	zn	=mZoneNodes[node];
				numFaces		=zn.mNumFaces;
				firstFace		=zn.mFirstFace;
				bLeaf			=false;
			}

			if(numFaces == 0)
			{
			}
			else if(numFaces == 1)
			{
				if(bLeaf)
				{
					return	mDebugFaces[mDebugLeafFaces[firstFace]];
				}
				else
				{
					return	mDebugFaces[firstFace];
				}
			}

			float		bestSum		=float.MinValue;
			DebugFace	bestFace	=null;
			for(int f=0;f < numFaces;f++)
			{
				int	face;
				if(bLeaf)
				{
					face	=mDebugLeafFaces[f + firstFace];
				}
				else
				{
					face	=f + firstFace;
				}

				DebugFace	df	=mDebugFaces[face];

				float	sum	=ComputeAngleSum(df, pos);
				if(sum > bestSum)
				{
					bestSum		=sum;
					bestFace	=df;
				}
			}
			return	bestFace;
		}
		#endregion
	}
}