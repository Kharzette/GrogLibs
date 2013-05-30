using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using UtilityLib;


namespace BSPZone
{
	public class Collision
	{
		internal Vector3		mIntersection;
		internal ZonePlane		mPlaneHit;
		internal bool			mbStartInside;
		internal int			mModelHit;
		internal DebugFace		mFaceHit;
	}

	class RayTrace
	{
		internal Collision	mCollision	=new Collision();

		internal Vector3		mOriginalStart, mOriginalEnd;
		internal BoundingBox	mBounds;
		internal float			mRadius;
		internal ZoneLeaf		mLeafHit;

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
					return	MathHelper.TwoPi;
				}

				v1	/=len1;
				v2	/=len2;

				float	dot	=Vector3.Dot(v1, v2);

				dotSum	+=(float)Math.Acos(dot);
			}
			return	dotSum;
		}


		DebugFace GetFaceForPoint(ZoneLeaf zl, Vector3 pos)
		{
			if(zl.mNumFaces == 0)
			{
				return	null;
			}
			else if(zl.mNumFaces == 1)
			{
				return	mDebugFaces[mDebugLeafFaces[zl.mFirstFace]];
			}

			float		bestSum		=float.MinValue;
			DebugFace	bestFace	=null;
			for(int f=0;f < zl.mNumFaces;f++)
			{
				int	leafFace	=mDebugLeafFaces[f + zl.mFirstFace];

				DebugFace	df	=mDebugFaces[leafFace];

				float	sum	=ComputeAngleSum(df, pos);
				if(sum > bestSum)
				{
					bestSum		=sum;
					bestFace	=df;
				}
			}
			return	bestFace;
		}


		//returns the closest impact, checks all models
		//everything returned in worldspace
		public bool TraceAllSphere(float radius, Vector3 start, Vector3 end,
			out Collision col)
		{
			List<Collision>	collisions	=new List<Collision>();

			for(int i=0;i < mZoneModels.Length;i++)
			{
				RayTrace	rt	=new RayTrace(start, end);
				rt.mRadius		=radius;

				rt.mCollision.mModelHit	=i;

				if(i == 0)
				{
					if(TraceSphereNode(rt, start, end, mZoneModels[i].mRootNode))
					{
						collisions.Add(rt.mCollision);
					}
				}
				else
				{
					ZoneModel	zm			=mZoneModels[i];
					Vector3		modelStart	=Vector3.Transform(start, zm.mInvertedTransform);
					Vector3		modelEnd	=Vector3.Transform(end, zm.mInvertedTransform);
					rt.mOriginalStart		=modelStart;
					rt.mOriginalEnd			=modelEnd;
					if(TraceSphereNode(rt, modelStart, modelEnd, mZoneModels[i].mRootNode))
					{
						collisions.Add(rt.mCollision);
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
				Collision	c	=collisions[i];

				c.mIntersection	=Vector3.Transform(c.mIntersection,
					mZoneModels[c.mModelHit].mTransform);

				c.mPlaneHit	=ZonePlane.Transform(c.mPlaneHit,
					mZoneModels[c.mModelHit].mTransform);

				float	dist	=Vector3.DistanceSquared(c.mIntersection, start);
				if(dist < bestDist)
				{
					bestDist	=dist;
					bestIdx		=i;
				}
			}

			col	=collisions[bestIdx];

			return	true;
		}


		//returns the closest impact, checks all models
		//everything returned in worldspace
		public bool TraceModelsBox(BoundingBox boxBounds, Vector3 start, Vector3 end, out Collision col)
		{
			List<Collision>	collisions	=new List<Collision>();

			for(int i=1;i < mZoneModels.Length;i++)
			{
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
			Vector3.Transform(mTestBoxCorners, ref mod.mInvertedTransform, mTestTransBoxCorners);

			//transform ray
			start	=Vector3.Transform(start, mod.mInvertedTransform);
			end		=Vector3.Transform(end, mod.mInvertedTransform);

			//bound the modelSpace corners
			box	=BoundingBox.CreateFromPoints(mTestTransBoxCorners);

			Mathery.CenterBoundingBoxAtOrigin(ref box);
		}


		//drop a worldspace position to the "floor"
		public Vector3 DropToGround(Vector3 pos, bool bUseModels)
		{
			Collision	col;

			RayTrace	trace	=new RayTrace(pos, pos + (Vector3.UnitY * - 300f));

			bool	bHit	=false;
			if(bUseModels)
			{
				bHit	=TraceAllSphere(0f, trace.mOriginalStart,
					trace.mOriginalEnd, out col);
			}
			else
			{
				bHit	=TraceSphereNode(trace, trace.mOriginalStart,
					trace.mOriginalEnd, mZoneModels[0].mRootNode);
			}

			if(bHit && trace.mCollision.mPlaneHit != ZonePlane.Blank
				&& trace.mCollision.mPlaneHit.IsGround())
			{
				trace.mCollision.mIntersection	+=(trace.mCollision.mPlaneHit.mNormal * Mathery.ON_EPSILON);
				return	trace.mCollision.mIntersection;
			}
			return	pos;
		}


		//clip a move segment to the front side
		bool PartFront(ZonePlane p, float distAdjust, Vector3 start, Vector3 end,
			out Vector3 clipStart, out Vector3 clipEnd)
		{
			clipStart	=start;
			clipEnd		=end;

			float	startDist	=p.DistanceFast(start) - distAdjust;
			float	endDist		=p.DistanceFast(end) - distAdjust;

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

			float	startDist	=p.DistanceFast(start) + distAdjust;
			float	endDist		=p.DistanceFast(end) + distAdjust;

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


		//model relative values
		internal bool TraceSphereNode(RayTrace trace, Vector3 start, Vector3 end, Int32 node)
		{
			bool	bHit	=false;

			if(node < 0)
			{
				Int32		leafIdx		=-(node + 1);
				ZoneLeaf	zl			=mZoneLeafs[leafIdx];

				if(Misc.bFlagSet(zl.mContents, Contents.BSP_CONTENTS_SOLID_CLIP))
				{
					if(ClipSphereToLeaf(trace, zl, trace.mRadius))
					{
						return	true;
					}
				}
				return	false;
			}

			ZoneNode	zn	=mZoneNodes[node];
			ZonePlane	p	=mZonePlanes[zn.mPlaneNum];

			Vector3	ray	=end - start;

			Vector3	clipStart, clipEnd;
			if(PartBehind(p, -trace.mRadius, start, end, out clipStart, out clipEnd))
			{
				bHit	=TraceSphereNode(trace, clipStart, clipEnd, zn.mBack);
				if(bHit)
				{
					end	=trace.mCollision.mIntersection;
				}
			}
			if(PartFront(p, -trace.mRadius, start, end, out clipStart, out clipEnd))
			{
				bHit	|=TraceSphereNode(trace, clipStart, clipEnd, zn.mFront);
				return	bHit;
			}
			return	bHit;
		}


		//expects and returns worldspace values
		bool TraceFakeOrientedBoxModel(RayTrace rt, Vector3 start, Vector3 end, ZoneModel mod)
		{
			//make a copy of the bounds
			BoundingBox	box	=rt.mBounds;

			FakeOrientBoxCollisionToModel(mod, ref rt.mBounds, ref start, ref end);

			rt.mOriginalStart	=start;
			rt.mOriginalEnd		=end;

			bool	bHit	=TraceBoxNode(rt, start, end, mod.mRootNode);

			//copy bounds back
			rt.mBounds	=box;

			Collision	col	=rt.mCollision;

			if(bHit && !col.mbStartInside)
			{
				col.mIntersection	=Vector3.Transform(col.mIntersection, mod.mTransform);
				col.mPlaneHit		=ZonePlane.Transform(col.mPlaneHit, mod.mTransform);
			}
			return	bHit;
		}


		//expects and returns worldspace values
		public bool IntersectBoxModel(BoundingBox box, Vector3 pos, int modelIndex, ref ZonePlane zp)
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
			Vector3.Transform(mTestBoxCorners, ref mod.mInvertedTransform, mTestTransBoxCorners);

			//transform position
			pos	=Vector3.Transform(pos, mod.mInvertedTransform);

			//bound the modelSpace corners
			box	=BoundingBox.CreateFromPoints(mTestTransBoxCorners);

			Mathery.CenterBoundingBoxAtOrigin(ref box);

			bool	bHit	=IntersectBoxNode(box, pos, mod.mRootNode, ref zp);
			if(bHit)
			{
				zp	=ZonePlane.Transform(zp, mod.mTransform);
			}
			return	bHit;
		}


		//expects worldspace values
		bool TraceFakeOrientedBoxTrigger(RayTrace rt, Vector3 start, Vector3 end, ZoneModel mod)
		{
			FakeOrientBoxCollisionToModel(mod, ref rt.mBounds, ref start, ref end);

			rt.mOriginalStart	=start;
			rt.mOriginalEnd		=end;

			return	TraceBoxNodeTrigger(rt, start, end, mod.mRootNode);
		}


		//expects start and end relative to whatever model owns the nodes
		bool TraceBoxNodeTrigger(RayTrace trace, Vector3 start, Vector3 end, Int32 node)
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

			float	dist	=Math.Abs(Vector3.Dot(trace.mBounds.Max, p.mNormal));

			Vector3	clipStart, clipEnd;
			if(PartBehind(p, -dist, start, end, out clipStart, out clipEnd))
			{
				bHit	=TraceBoxNodeTrigger(trace, clipStart, clipEnd, zn.mBack);
				if(bHit)
				{
					end	=trace.mCollision.mIntersection;
				}
			}
			if(PartFront(p, -dist, start, end, out clipStart, out clipEnd))
			{
				bHit	|=TraceBoxNodeTrigger(trace, clipStart, clipEnd, zn.mFront);
				return	bHit;
			}
			return	bHit;
		}


		//expects start and end relative to whatever model owns the nodes
		bool TraceBoxNode(RayTrace trace, Vector3 start, Vector3 end, Int32 node)
		{
			bool	bHit	=false;

			if(node < 0)
			{
				Int32		leafIdx		=-(node + 1);
				ZoneLeaf	zl			=mZoneLeafs[leafIdx];

				if(Misc.bFlagSet(zl.mContents, Contents.BSP_CONTENTS_SOLID_CLIP))
				{
					if(ClipBoxToLeaf(trace, zl))
					{
						return	true;
					}
				}
				return	false;
			}

			ZoneNode	zn	=mZoneNodes[node];
			ZonePlane	p	=mZonePlanes[zn.mPlaneNum];

			float	dist	=Math.Abs(Vector3.Dot(trace.mBounds.Max, p.mNormal));

			Vector3	clipStart, clipEnd;
			if(PartBehind(p, -dist, start, end, out clipStart, out clipEnd))
			{
				bHit	=TraceBoxNode(trace, clipStart, clipEnd, zn.mBack);
				if(bHit)
				{
					end	=trace.mCollision.mIntersection;
				}
			}
			if(PartFront(p, -dist, start, end, out clipStart, out clipEnd))
			{
				bHit	|=TraceBoxNode(trace, clipStart, clipEnd, zn.mFront);
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

			float	dist	=Math.Abs(Vector3.Dot(box.Max, p.mNormal));

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


		bool ClipSphereToLeaf(RayTrace trace, ZoneLeaf zl, float radius)
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
			for(int i=0;i < zl.mNumSides;i++)
			{
				ZoneLeafSide	side	=mZoneLeafSides[i + zl.mFirstSide];
				ZonePlane		p		=mZonePlanes[side.mPlaneNum];

				if(side.mbFlipSide)
				{
					p.Inverse();
				}

				p.mDist	+=radius;

				float	frontDist	=p.DistanceFast(start);
				float	backDist	=p.DistanceFast(end);
				if(frontDist > 0 && backDist >= 0)
				{
					return	false;	//not intersecting
				}

				if(frontDist < 0 && backDist < 0)
				{
					continue;
				}

				bAnyInFront	=true;

				//split
				float	ratio			=frontDist / (frontDist - backDist);
				Vector3	intersection	=start + ratio * (end - start);

				if(frontDist >= 0)
				{
					start		=intersection;
					clipPlane	=p;
					bClipped	=true;
				}
				else
				{
					end		=intersection;
				}
			}

			trace.mLeafHit	=zl;

			if(bClipped)
			{
				trace.mCollision.mIntersection	=start;
				trace.mCollision.mPlaneHit		=clipPlane;
			}
			else if(!bAnyInFront)
			{
				//started inside!
				trace.mCollision.mbStartInside	=true;
				return	true;
			}

			return	bClipped;
		}


		//positions and planes are model relative
		bool ClipBoxToLeaf(RayTrace trace, ZoneLeaf zl)
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
			for(int i=0;i < zl.mNumSides;i++)
			{
				ZoneLeafSide	side	=mZoneLeafSides[i + zl.mFirstSide];
				ZonePlane		p		=mZonePlanes[side.mPlaneNum];

				if(side.mbFlipSide)
				{
					p.Inverse();
				}

				p.mDist	+=Math.Abs(Vector3.Dot(trace.mBounds.Max, p.mNormal));

				float	frontDist	=p.DistanceFast(start);
				float	backDist	=p.DistanceFast(end);
				if(frontDist > 0 && backDist >= 0)
				{
					return	false;	//not intersecting
				}

				if(frontDist < 0 && backDist < 0)
				{
					continue;
				}

				bAnyInFront	=true;

				if(frontDist == 0 && backDist == 0)
				{
					clipPlane	=p;
					bClipped	=true;
					break;
				}

				//split
				float	ratio			=frontDist / (frontDist - backDist);
				Vector3	intersection	=start + ratio * (end - start);

				if(frontDist >= 0)
				{
					start		=intersection;
					clipPlane	=p;
					bClipped	=true;
				}
				else
				{
					end		=intersection;
				}
			}

			trace.mLeafHit	=zl;

			if(bClipped)
			{
				trace.mCollision.mIntersection	=start;
				trace.mCollision.mPlaneHit		=clipPlane;
			}
			else if(!bAnyInFront)
			{
				//started inside!
				trace.mCollision.mIntersection	=start;
				trace.mCollision.mbStartInside	=true;
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

				p.mDist	+=Math.Abs(Vector3.Dot(box.Max, p.mNormal));

				float	dist	=p.DistanceFast(pos);
				if(dist >= 0)
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
	}
}