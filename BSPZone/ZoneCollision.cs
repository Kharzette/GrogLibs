using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using UtilityLib;


namespace BSPZone
{
	class RayTrace
	{
		internal Vector3		mOriginalStart, mOriginalEnd;
		internal Vector3		mIntersection;
		internal Int32			mLeaf;
		internal float			mBestDist;
		internal ZonePlane		mBestPlane;
		internal float			mRatio;
		internal bool			mbHitSet, mbLeafHit;
		internal BoundingBox	mRayBox, mMoveBox;
		internal float			mRadius;

		internal RayTrace()
		{
			mIntersection	=Vector3.Zero;
			mBestPlane		=new ZonePlane();
			mBestDist		=99999.0f;
			mbHitSet		=false;
		}
	}


	public partial class Zone
	{
		Vector3	[]mTestBoxCorners		=new Vector3[8];
		Vector3	[]mTestTransBoxCorners	=new Vector3[8];

		BoundingBox	mTinyBox;	//for doing raycasts

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


		//No need to use leafs for this, as bevels are only
		//there for plane expansion
		bool RayIntersect(Vector3 start, Vector3 end, Int32 node,
			ref Vector3 intersectionPoint, ref bool hitLeaf,
			ref Int32 leafHit, ref Int32 nodeHit)
		{
			float	Fd, Bd, dist;
			Vector3	I;

			if(node < 0)						
			{
				Int32	leaf	=-(node+1);

				leafHit	=leaf;

				if((mZoneLeafs[leaf].mContents
					& Contents.BSP_CONTENTS_SOLID2) != 0)
				{
					return	true;	//Ray collided with solid space
				}
				else 
				{
					return	false;	//Ray collided with empty space
				}
			}
			ZoneNode	n	=mZoneNodes[node];
			ZonePlane	p	=mZonePlanes[n.mPlaneNum];

			Fd	=p.DistanceFast(start);
			Bd	=p.DistanceFast(end);

			if(Fd >= 0 && Bd >= 0)
			{
				return(RayIntersect(start, end, n.mFront,
					ref intersectionPoint, ref hitLeaf, ref leafHit, ref nodeHit));
			}
			if(Fd < 0 && Bd < 0)
			{
				return(RayIntersect(start, end, n.mBack,
					ref intersectionPoint, ref hitLeaf, ref leafHit, ref nodeHit));
			}

			dist	=Fd / (Fd - Bd);

			I	=start + dist * (end - start);

			//Work our way to the front, from the back side.  As soon as there
			//are no more collisions, we can assume that we have the front portion of the
			//ray that is in empty space.  Once we find this, and see that the back half is in
			//solid space, then we found the front intersection point...
			if(RayIntersect(start, I,
				(Fd < 0)? n.mBack : n.mFront,
				ref intersectionPoint, ref hitLeaf, ref leafHit, ref nodeHit))
			{
				return	true;
			}
			else if(RayIntersect(I, end,
				(Fd < 0)? n.mFront : n.mBack,
				ref intersectionPoint, ref hitLeaf, ref leafHit, ref nodeHit))
			{
				if(!hitLeaf)
				{
					intersectionPoint	=I;
					hitLeaf				=true;
					nodeHit				=node;
				}
				return	true;
			}
			return	false;
		}


		//returns the closest impact, checks all models
		public bool TraceAllBox(BoundingBox boxBounds, Vector3 start, Vector3 end,
			 ref int modelHit, ref Vector3 I, ref ZonePlane P)
		{
			List<int>		modelsHit	=new List<int>();
			List<Vector3>	impacts		=new List<Vector3>();
			List<ZonePlane>	planes		=new List<ZonePlane>();

			RayTrace	rt	=new RayTrace();

			rt.mOriginalStart	=start;
			rt.mOriginalEnd		=end;
			rt.mMoveBox			=boxBounds;
			rt.mRatio			=float.MaxValue;

			for(int i=0;i < mZoneModels.Length;i++)
			{
				Vector3		impacto	=Vector3.Zero;
				ZonePlane	hp		=ZonePlane.Blank;

				if(i != 0)
				{
					if(Trace_FakeOBBoxModel(boxBounds, i,
						start, end, ref impacto, ref hp))
					{
						modelsHit.Add(i);
						impacts.Add(impacto);
						planes.Add(hp);
					}
				}
				else
				{
					if(Trace_BoxModel(boxBounds, i,
						start, end, ref impacto, ref hp))
					{
						modelsHit.Add(i);
						impacts.Add(impacto);
						planes.Add(hp);
					}
				}
			}

			if(modelsHit.Count == 0)
			{
				return	false;
			}

			int		bestIdx		=0;
			float	bestDist	=float.MaxValue;
			for(int i=0;i < modelsHit.Count;i++)
			{
				float	dist	=Vector3.DistanceSquared(impacts[i], start);
				if(dist < bestDist)
				{
					bestDist	=dist;
					bestIdx		=i;
				}
			}

			modelHit	=modelsHit[bestIdx];
			if(modelHit != 0)
			{
				//adjust these back to worldspace
				I	=Vector3.Transform(impacts[bestIdx], mZoneModels[modelHit].mTransform);
				P	=ZonePlane.Transform(planes[bestIdx], mZoneModels[modelHit].mTransform);
			}
			else
			{
				I	=impacts[bestIdx];
				P	=planes[bestIdx];
			}

			return	true;
		}


		//returns the closest impact, checks all models
		public bool TraceAllSphere(float radius, Vector3 start, Vector3 end,
			 ref int modelHit, ref Vector3 I, ref ZonePlane P)
		{
			List<int>		modelsHit	=new List<int>();
			List<Vector3>	impacts		=new List<Vector3>();
			List<ZonePlane>	planes		=new List<ZonePlane>();

			RayTrace	rt	=new RayTrace();

			rt.mOriginalStart	=start;
			rt.mOriginalEnd		=end;
			rt.mRadius			=radius;
			rt.mRatio			=float.MaxValue;

			for(int i=0;i < mZoneModels.Length;i++)
			{
				Vector3		impacto	=Vector3.Zero;
				ZonePlane	hp		=ZonePlane.Blank;

				if(i == 0)
				{
					if(TraceSphereNode(rt, start, end, mZoneModels[i].mRootNode))
					{
						modelsHit.Add(i);
						impacts.Add(rt.mIntersection);
						planes.Add(rt.mBestPlane);
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
						modelsHit.Add(i);
						impacts.Add(rt.mIntersection);
						planes.Add(rt.mBestPlane);
					}
				}
			}

			if(modelsHit.Count == 0)
			{
				return	false;
			}

			int		bestIdx		=0;
			float	bestDist	=float.MaxValue;
			for(int i=0;i < modelsHit.Count;i++)
			{
				if(i != 0)
				{
					impacts[i]	=Vector3.Transform(impacts[i], mZoneModels[modelsHit[i]].mTransform);
					planes[i]	=ZonePlane.Transform(planes[i], mZoneModels[modelsHit[i]].mTransform);
				}

				float	dist	=Vector3.DistanceSquared(impacts[i], start);
				if(dist < bestDist)
				{
					bestDist	=dist;
					bestIdx		=i;
				}
			}

			modelHit	=modelsHit[bestIdx];
			I			=impacts[bestIdx];
			P			=planes[bestIdx];

			return	true;
		}


		//returns the closest impact, checks all models
		public bool TraceAllRay(Vector3 start, Vector3 end,
			 ref int modelHit, ref Vector3 I, ref ZonePlane P)
		{
			List<int>		modelsHit	=new List<int>();
			List<Vector3>	impacts		=new List<Vector3>();
			List<ZonePlane>	planes		=new List<ZonePlane>();

			RayTrace	rt	=new RayTrace();

			rt.mOriginalStart	=start;
			rt.mOriginalEnd		=end;
			rt.mRatio			=float.MaxValue;

			for(int i=0;i < mZoneModels.Length;i++)
			{
				Vector3		impacto	=Vector3.Zero;
				ZonePlane	hp		=ZonePlane.Blank;

				if(i == 0)
				{
					if(TraceRayNode(rt, start, end, mZoneModels[i].mRootNode))
					{
						modelsHit.Add(i);
						impacts.Add(rt.mIntersection);
						planes.Add(rt.mBestPlane);
					}
				}
				else
				{
					ZoneModel	zm			=mZoneModels[i];
					Vector3		modelStart	=Vector3.Transform(start, zm.mInvertedTransform);
					Vector3		modelEnd	=Vector3.Transform(end, zm.mInvertedTransform);
					rt.mOriginalStart		=modelStart;
					rt.mOriginalEnd			=modelEnd;
					if(TraceRayNode(rt, modelStart, modelEnd, mZoneModels[i].mRootNode))
					{
						modelsHit.Add(i);
						impacts.Add(rt.mIntersection);
						planes.Add(rt.mBestPlane);
					}
				}
			}

			if(modelsHit.Count == 0)
			{
				return	false;
			}

			int		bestIdx		=0;
			float	bestDist	=float.MaxValue;
			for(int i=0;i < modelsHit.Count;i++)
			{
				if(i != 0)
				{
					impacts[i]	=Vector3.Transform(impacts[i], mZoneModels[modelsHit[i]].mTransform);
					planes[i]	=ZonePlane.Transform(planes[i], mZoneModels[modelsHit[i]].mTransform);
				}

				float	dist	=Vector3.DistanceSquared(impacts[i], start);
				if(dist < bestDist)
				{
					bestDist	=dist;
					bestIdx		=i;
				}
			}

			modelHit	=modelsHit[bestIdx];
			I			=impacts[bestIdx];
			P			=planes[bestIdx];

			return	true;
		}


		//ported from genesis, don't really fully understand how it works
		bool Trace_BoxModel(BoundingBox boxBounds, int modelIndex,
			Vector3 start, Vector3 end, ref Vector3 I, ref ZonePlane P)
		{
			RayTrace	trace		=new RayTrace();

			//set boxes
			trace.mRayBox	=boxBounds;
			trace.mMoveBox	=Trace_GetMoveBox(boxBounds, start, end);

			ZoneModel	worldModel	=mZoneModels[modelIndex];

			if(!trace.mMoveBox.Intersects(worldModel.mBounds))
			{
				return	false;
			}

			trace.mOriginalStart	=start;
			trace.mOriginalEnd		=end;
			FindClosestLeafIntersection_r(trace, worldModel.mRootNode);

			if(trace.mbLeafHit)
			{
				I	=trace.mIntersection;
				P	=trace.mBestPlane;
				return	true;
			}
			return	false;
		}


		//warning, this only works with centered bounding boxes!
		//simulate an oriented collision box and collide vs a model
		bool Trace_FakeOBBoxModel(BoundingBox boxBounds, int modelIndex,
			Vector3 start, Vector3 end, ref Vector3 I, ref ZonePlane P)
		{
			RayTrace	trace		=new RayTrace();
			ZoneModel	testModel	=mZoneModels[modelIndex];

#if DEBUG
			System.Diagnostics.Debug.Assert(Mathery.IsBoundingBoxCentered(boxBounds));
#endif

			//get box bound corners
			boxBounds.GetCorners(mTestBoxCorners);

			//transform into model space
			Vector3.Transform(mTestBoxCorners, ref testModel.mInvertedTransform, mTestTransBoxCorners);

			//transform ray
			Vector3	modelStart	=Vector3.Transform(start, testModel.mInvertedTransform);
			Vector3	modelEnd	=Vector3.Transform(end, testModel.mInvertedTransform);

			//bound the modelSpace corners
			trace.mRayBox	=BoundingBox.CreateFromPoints(mTestTransBoxCorners);

			Mathery.CenterBoundingBoxAtOrigin(ref trace.mRayBox);

			trace.mMoveBox			=Trace_GetMoveBox(trace.mRayBox, modelStart, modelEnd);
			trace.mOriginalStart	=modelStart;
			trace.mOriginalEnd		=modelEnd;

			//test for basic box overlap
			if(!trace.mMoveBox.Intersects(testModel.mBounds))
			{
				return	false;
			}
			FindClosestLeafIntersection_r(trace, testModel.mRootNode);

			if(trace.mbLeafHit)
			{
				I	=trace.mIntersection;
				P	=trace.mBestPlane;
				return	true;
			}
			return	false;
		}


		//warning, this only works with centered bounding boxes!
		//intersect against trigger models
		bool Trace_FakeOBBoxTrigger(BoundingBox boxBounds,
			int modelIndex, Vector3 start, Vector3 end)
		{
			RayTrace	trace		=new RayTrace();
			ZoneModel	testModel	=mZoneModels[modelIndex];

#if DEBUG
			System.Diagnostics.Debug.Assert(Mathery.IsBoundingBoxCentered(boxBounds));
#endif

			//get box bound corners
			boxBounds.GetCorners(mTestBoxCorners);

			//transform into model space
			Vector3.Transform(mTestBoxCorners, ref testModel.mInvertedTransform, mTestTransBoxCorners);

			//transform ray
			Vector3	modelStart	=Vector3.Transform(start, testModel.mInvertedTransform);
			Vector3	modelEnd	=Vector3.Transform(end, testModel.mInvertedTransform);

			//bound the modelSpace corners
			trace.mRayBox	=BoundingBox.CreateFromPoints(mTestTransBoxCorners);

			Mathery.CenterBoundingBoxAtOrigin(ref trace.mRayBox);

			trace.mMoveBox			=Trace_GetMoveBox(trace.mRayBox, modelStart, modelEnd);
			trace.mOriginalStart	=modelStart;
			trace.mOriginalEnd		=modelEnd;

			//test for basic box overlap
			if(!trace.mMoveBox.Intersects(testModel.mBounds))
			{
				return	false;
			}
			TestTriggerIntersection_r(trace, testModel.mRootNode);

			if(trace.mbLeafHit)
			{
				return	true;
			}
			return	false;
		}


		//drop a position to the "floor"
		public Vector3 DropToGround(Vector3 pos, bool bUseModels)
		{
			int			modelHit	=0;
			Vector3		impacto		=Vector3.Zero;
			ZonePlane	planeHit	=ZonePlane.Blank;

			bool	bHit	=false;
			if(bUseModels)
			{
				bHit	=TraceAllRay(pos, pos + (Vector3.UnitY * -300f),
							ref modelHit, ref impacto, ref planeHit);
			}
			else
			{
				RayTrace	trace	=new RayTrace();
				bHit	=TraceRayNode(trace, pos, pos + (Vector3.UnitY * - 300f),
							mZoneModels[0].mRootNode);
				if(bHit)
				{
					impacto		=trace.mIntersection;
					planeHit	=trace.mBestPlane;
				}
			}

			if(bHit && planeHit != ZonePlane.Blank && planeHit.IsGround())
			{
				impacto	+=(planeHit.mNormal * Mathery.ON_EPSILON);
				return	impacto;
			}
			return	pos;
		}


		void TestTriggerIntersection_r(RayTrace trace, Int32 node)
		{
			if(node < 0)
			{
				Int32	leaf		=-(node + 1);
				UInt32	contents	=mZoneLeafs[leaf].mContents;

				if((contents & BSPZone.Contents.BSP_CONTENTS_TRIGGER) == 0)
				{
					return;
				}

				trace.mbLeafHit	=true;
				return;
			}

			UInt32	side	=Trace_BoxOnPlaneSide(trace.mMoveBox, mZonePlanes[mZoneNodes[node].mPlaneNum]);

			//Go down the sides that the box lands in
			if((side & ZonePlane.PSIDE_FRONT) != 0)
			{
				TestTriggerIntersection_r(trace, mZoneNodes[node].mFront);
			}

			if((side & ZonePlane.PSIDE_BACK) != 0)
			{
				TestTriggerIntersection_r(trace, mZoneNodes[node].mBack);
			}
		}


		void FindClosestLeafIntersection_r(RayTrace trace, Int32 node)
		{
			if(node < 0)
			{
				Int32	leaf		=-(node + 1);
				UInt32	contents	=mZoneLeafs[leaf].mContents;

				if((contents & BSPZone.Contents.BSP_CONTENTS_SOLID_CLIP) == 0)
				{
					return;		// Only solid leafs contain side info...
				}

				trace.mbHitSet	=false;
				
				if(mZoneLeafs[leaf].mNumSides == 0)
				{
					return;
				}

				IntersectLeafSides_r(trace, trace.mOriginalStart, trace.mOriginalEnd, leaf, 0, 1);
				return;
			}

			UInt32	side	=Trace_BoxOnPlaneSide(trace.mMoveBox, mZonePlanes[mZoneNodes[node].mPlaneNum]);

			//Go down the sides that the box lands in
			if((side & ZonePlane.PSIDE_FRONT) != 0)
			{
				FindClosestLeafIntersection_r(trace, mZoneNodes[node].mFront);
			}

			if((side & ZonePlane.PSIDE_BACK) != 0)
			{
				FindClosestLeafIntersection_r(trace, mZoneNodes[node].mBack);
			}
		}


		void Trace_ExpandPlaneForBox(ref ZonePlane p, BoundingBox box)
		{
			Vector3	norm	=p.mNormal;
			
			if(norm.X > 0)
			{
				p.mDist	-=norm.X * box.Min.X;
			}
			else
			{
				p.mDist	-=norm.X * box.Max.X;
			}
			
			if(norm.Y > 0)
			{
				p.mDist	-=norm.Y * box.Min.Y;
			}
			else
			{
				p.mDist	-=norm.Y * box.Max.Y;
			}

			if(norm.Z > 0)
			{
				p.mDist	-=norm.Z * box.Min.Z;
			}
			else
			{
				p.mDist	-=norm.Z * box.Max.Z;
			}
		}


		//This is a port from Genesis, and I really cannot follow the code.
		//It appears to have been written to work with non convex leafs, as
		//there doesn't seem to be any preference for finding the front side
		//intersections as opposed to the back side.  I think this can cause
		//false collisions with node split planes that aren't actually solid.
		//At any rate, this is happening, but the collisions never leave you
		//in an invalid location so I'm still using it for now.
		bool IntersectLeafSides_r(RayTrace trace, Vector3 start, Vector3 end,
			Int32 leaf, Int32 side, Int32 pSide)
		{
			if(pSide == 0)
			{
				return	false;
			}

			if(side >= mZoneLeafs[leaf].mNumSides)
			{
				return	true;	//if it lands behind all planes, it is inside
			}

			int	RSide	=mZoneLeafs[leaf].mFirstSide + side;

			ZonePlane	p	=mZonePlanes[mZoneLeafSides[RSide].mPlaneNum];

			p.mType	=ZonePlane.PLANE_ANY;
			
			if(mZoneLeafSides[RSide].mbFlipSide)
			{
				p.Inverse();
			}
			
			//Simulate the point having a box, by pushing the plane out by the box size
			Trace_ExpandPlaneForBox(ref p, trace.mRayBox);

			float	frontDist	=p.DistanceFast(start);
			float	backDist	=p.DistanceFast(end);

			if(frontDist >= 0 && backDist >= 0)
			{
				//Leaf sides are convex hulls, so front side is totally outside
				return	IntersectLeafSides_r(trace, start, end, leaf, side + 1, 0);
			}

			if(frontDist < 0 && backDist < 0)
			{
				return	IntersectLeafSides_r(trace, start, end, leaf, side + 1, 1);
			}

			Int32	splitSide	=(frontDist < 0)? 1 : 0;
			float	splitDist	=0.0f;
			
			if(frontDist < 0)
			{
				splitDist	=(frontDist + UtilityLib.Mathery.ON_EPSILON)
								/ (frontDist - backDist);
			}
			else
			{
				splitDist	=(frontDist - UtilityLib.Mathery.ON_EPSILON)
								/ (frontDist - backDist);
			}

			if(splitDist < 0.0f)
			{
				splitDist	=0.0f;
			}
			
			if(splitDist > 1.0f)
			{
				splitDist	=1.0f;
			}

			Vector3	intersect	=start + splitDist * (end - start);

			//Only go down the back side, since the front side is empty in a convex tree
			if(IntersectLeafSides_r(trace, start, intersect, leaf, side + 1, splitSide))
			{
				trace.mbLeafHit	=true;
				return	true;
			}
			else if(IntersectLeafSides_r(trace, intersect, end, leaf, side + 1, (splitSide == 0)? 1 : 0))
			{
				splitDist	=(intersect - trace.mOriginalStart).Length();

				//Record the intersection closest to the start of ray
				if(splitDist < trace.mBestDist && !trace.mbHitSet)
				{
					trace.mIntersection	=intersect;
					trace.mLeaf			=leaf;
					trace.mBestDist		=splitDist;
					trace.mBestPlane	=p;
					trace.mRatio		=splitDist;
					trace.mbHitSet		=true;
				}
				trace.mbLeafHit	=true;
				return	true;
			}			
			return	false;	
		}


		UInt32 GetContents(int node)
		{
			if(node < 0)
			{
				Int32		leafIdx		=-(node + 1);
				ZoneLeaf	zl			=mZoneLeafs[leafIdx];

				return	zl.mContents;
			}
			return	Contents.BSP_CONTENTS_EMPTY2;
		}
		
		
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


		//might still be a bit dodgy for movement, needs more testing
		bool TraceSphereNode(RayTrace trace, Vector3 start, Vector3 end, Int32 node)
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
					end	=trace.mIntersection;
				}
			}
			if(PartFront(p, -trace.mRadius, start, end, out clipStart, out clipEnd))
			{
				bHit	|=TraceSphereNode(trace, clipStart, clipEnd, zn.mFront);
				return	bHit;
			}
			return	bHit;
		}


		public bool TraceRayNode(Vector3 start, Vector3 end)
		{
			RayTrace	rt	=new RayTrace();
			rt.mRadius	=5;

			BoundingBox	testBox	=Misc.MakeBox(16, 50);

			Vector3		impacto		=Vector3.Zero;
			ZonePlane	planeHit	=ZonePlane.Blank;
			return	Trace_BoxModel(testBox, 0, start, end, ref impacto, ref planeHit);
		}


		//might still be a bit dodgy for movement, needs more testing
		bool TraceRayNode(RayTrace trace, Vector3 start, Vector3 end, Int32 node)
		{
			bool	bHit	=false;

			if(node < 0)
			{
				Int32		leafIdx		=-(node + 1);
				ZoneLeaf	zl			=mZoneLeafs[leafIdx];

				if(Misc.bFlagSet(zl.mContents, Contents.BSP_CONTENTS_SOLID_CLIP))
				{
					trace.mbHitSet		=true;
					trace.mIntersection	=start;
					return	true;
				}
				return	false;
			}

			ZoneNode	zn	=mZoneNodes[node];
			ZonePlane	p	=mZonePlanes[zn.mPlaneNum];

			Vector3	clipStart, clipEnd;
			if(PartBehind(p, 0, start, end, out clipStart, out clipEnd))
			{
				bHit	=TraceRayNode(trace, clipStart, clipEnd, zn.mBack);
				if(bHit)
				{
					end	=trace.mIntersection;
				}
			}
			if(PartFront(p, 0, start, end, out clipStart, out clipEnd))
			{
				bHit	|=TraceRayNode(trace, clipStart, clipEnd, zn.mFront);
				if(bHit)
				{
					if(trace.mbHitSet)
					{
						trace.mBestPlane	=p;
						trace.mbHitSet		=false;
					}
				}
				return	bHit;
			}
			return	bHit;
		}


		//this almost works but needs more fiddling
		bool TraceBoxWorld(RayTrace trace, Vector3 start, Vector3 end, Int32 node)
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

			Trace_ExpandPlaneForBox(ref p, trace.mMoveBox);

			Vector3	clipStart, clipEnd;
			if(PartBehind(p, 0, start, end, out clipStart, out clipEnd))
			{
				bHit	=TraceBoxWorld(trace, clipStart, clipEnd, zn.mBack);
				if(bHit)
				{
					end	=trace.mIntersection;
				}
			}
			if(PartFront(p, 0, start, end, out clipStart, out clipEnd))
			{
				bHit	|=TraceBoxWorld(trace, clipStart, clipEnd, zn.mFront);
				return	bHit;
			}
			return	bHit;
		}


		bool ClipRayToLeaf(RayTrace trace, ZoneLeaf zl)
		{
			return	ClipSphereToLeaf(trace, zl, 0f);
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

				//split
				float	ratio			=frontDist / (frontDist - backDist);
				Vector3	intersection	=start + ratio * (end - start);

				if(frontDist > 0)
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

			if(bClipped)
			{
				trace.mIntersection	=start;
				trace.mBestPlane	=clipPlane;
			}
			return	bClipped;
		}


		bool ClipBoxToLeaf(RayTrace trace, ZoneLeaf zl)
		{
			if(zl.mNumSides <= 0)
			{
				return	false;
			}

			Vector3	start	=trace.mOriginalStart;
			Vector3	end		=trace.mOriginalEnd;

			bool		bClipped	=false;
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

				Trace_ExpandPlaneForBox(ref p, trace.mMoveBox);

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

				//split
				float	ratio			=frontDist / (frontDist - backDist);
				Vector3	intersection	=start + ratio * (end - start);

				if(frontDist > 0)
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

			if(bClipped)
			{
				trace.mIntersection	=start;
				trace.mBestPlane	=clipPlane;
			}
			return	bClipped;
		}


		bool PointInLeafSides(Vector3 pnt, ZoneLeaf leaf, BoundingBox box)
		{
			Int32	f	=leaf.mFirstSide;

			for(int i=0;i < leaf.mNumSides;i++)
			{
				ZonePlane	p	=mZonePlanes[mZoneLeafSides[i + f].mPlaneNum];
				p.mType			=ZonePlane.PLANE_ANY;
			
				if(mZoneLeafSides[i + f].mbFlipSide)
				{
					p.Inverse();
				}

				//Simulate the point having a box, by pushing the plane out by the box size
				Trace_ExpandPlaneForBox(ref p, box);

				float	dist	=p.DistanceFast(pnt);

				if(dist >= 0.0f)
				{
					return false;	//Since leafs are convex, it must be outside...
				}
			}
			return	true;
		}


		BoundingBox Trace_GetMoveBox(BoundingBox box, Vector3 start, Vector3 end)
		{
			BoundingBox	ret	=new BoundingBox();

			Mathery.ClearBoundingBox(ref ret);
			Mathery.AddPointToBoundingBox(ref ret, start);
			Mathery.AddPointToBoundingBox(ref ret, end);

			ret.Min	+=box.Min - Vector3.One;
			ret.Max	+=box.Max + Vector3.One;

			return	ret;
		}


		//about 3 times faster than original genesis on xbox
		UInt32 Trace_BoxOnPlaneSide(BoundingBox box, ZonePlane p)
		{
			UInt32	side	=0;
			Vector3	corner0	=Vector3.Zero;
			Vector3	corner1	=Vector3.Zero;
			float	dist1, dist2;

			//Axial planes are easy
			if(p.mType < ZonePlane.PLANE_ANYX)
			{
				if(UtilityLib.Mathery.VecIdx(box.Max, p.mType) >= p.mDist)
				{
					side	|=ZonePlane.PSIDE_FRONT;
				}
				if(UtilityLib.Mathery.VecIdx(box.Min, p.mType) < p.mDist)
				{
					side	|=ZonePlane.PSIDE_BACK;
				}
				return	side;
			}

			//Create the proper leading and trailing verts for the box
			if(p.mNormal.X < 0)
			{
				corner0.X	=box.Min.X;
				corner1.X	=box.Max.X;
			}
			else
			{
				corner1.X	=box.Min.X;
				corner0.X	=box.Max.X;
			}
			if(p.mNormal.Y < 0)
			{
				corner0.Y	=box.Min.Y;
				corner1.Y	=box.Max.Y;
			}
			else
			{
				corner1.Y	=box.Min.Y;
				corner0.Y	=box.Max.Y;
			}
			if(p.mNormal.Z < 0)
			{
				corner0.Z	=box.Min.Z;
				corner1.Z	=box.Max.Z;
			}
			else
			{
				corner1.Z	=box.Min.Z;
				corner0.Z	=box.Max.Z;
			}

			dist1	=Vector3.Dot(p.mNormal, corner0) - p.mDist;
			dist2	=Vector3.Dot(p.mNormal, corner1) - p.mDist;
			
			if(dist1 >= 0)
			{
				side	=ZonePlane.PSIDE_FRONT;
			}
			if(dist2 < 0)
			{
				side	|=ZonePlane.PSIDE_BACK;
			}
			return	side;
		}
	}
}