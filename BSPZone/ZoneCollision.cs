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
		internal ZonePlane		mBestPlane;
		internal bool			mbHitSet;
		internal BoundingBox	mMoveBox;
		internal float			mRadius;
		internal bool			mbStartInside, mbEndInside;

		internal RayTrace()
		{
			mIntersection	=Vector3.Zero;
			mBestPlane		=ZonePlane.Blank;
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
				if(modelsHit[i] == -1)
				{
					continue;
				}
				impacts[i]	=Vector3.Transform(impacts[i], mZoneModels[modelsHit[i]].mTransform);
				planes[i]	=ZonePlane.Transform(planes[i], mZoneModels[modelsHit[i]].mTransform);

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
		public bool TraceModelsBox(BoundingBox boxBounds, Vector3 start, Vector3 end,
			 ref int modelHit, ref Vector3 I, ref ZonePlane P, ref bool bStartInSolid)
		{
			List<int>		modelsHit	=new List<int>();
			List<Vector3>	impacts		=new List<Vector3>();
			List<ZonePlane>	planes		=new List<ZonePlane>();

			RayTrace	rt	=new RayTrace();

			rt.mOriginalStart	=start;
			rt.mOriginalEnd		=end;
			rt.mMoveBox			=boxBounds;

			for(int i=1;i < mZoneModels.Length;i++)
			{
				Vector3		impacto	=Vector3.Zero;
				ZonePlane	hp		=ZonePlane.Blank;
				ZoneModel	zm		=mZoneModels[i];
				if(TraceFakeOrientedBoxModel(rt, start, end, zm))
				{
					if(rt.mbStartInside)
					{
						bStartInSolid	=true;
						return	true;
					}
					modelsHit.Add(i);
					impacts.Add(rt.mIntersection);
					planes.Add(rt.mBestPlane);
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
				//backtransform to worldspace to check dists
//				impacts[i]	=Vector3.Transform(impacts[i], mZoneModels[modelsHit[i]].mTransform);
//				planes[i]	=ZonePlane.Transform(planes[i], mZoneModels[modelsHit[i]].mTransform);

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
				if(modelsHit[i] == -1)
				{
					continue;
				}
				impacts[i]	=Vector3.Transform(impacts[i], mZoneModels[modelsHit[i]].mTransform);
				planes[i]	=ZonePlane.Transform(planes[i], mZoneModels[modelsHit[i]].mTransform);

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


		//should be quite solid now
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


		bool TraceFakeOrientedBoxModel(RayTrace rt, Vector3 start, Vector3 end, ZoneModel mod)
		{
			FakeOrientBoxCollisionToModel(mod, ref rt.mMoveBox, ref start, ref end);

			rt.mOriginalStart	=start;
			rt.mOriginalEnd		=end;

			bool	bHit	=TraceBoxNode(rt, start, end, mod.mRootNode);

			if(bHit && !rt.mbStartInside)
			{
				rt.mIntersection	=Vector3.Transform(rt.mIntersection, mod.mTransform);
				rt.mBestPlane		=ZonePlane.Transform(rt.mBestPlane, mod.mTransform);
			}
			return	bHit;
		}


		bool TraceFakeOrientedBoxTrigger(RayTrace rt, Vector3 start, Vector3 end, ZoneModel mod)
		{
			FakeOrientBoxCollisionToModel(mod, ref rt.mMoveBox, ref start, ref end);

			rt.mOriginalStart	=start;
			rt.mOriginalEnd		=end;

			return	TraceBoxNodeTrigger(rt, start, end, mod.mRootNode);
		}


		bool TraceBoxNodeTrigger(RayTrace trace, Vector3 start, Vector3 end, Int32 node)
		{
			bool	bHit	=false;

			if(node < 0)
			{
				Int32		leafIdx		=-(node + 1);
				ZoneLeaf	zl			=mZoneLeafs[leafIdx];

				if(Misc.bFlagSet(zl.mContents, Contents.BSP_CONTENTS_TRIGGER))
				{
					if(IsPointInLeaf(trace, end, zl))
					{
						trace.mbEndInside	=true;
					}
					return	true;
				}
				return	false;
			}

			ZoneNode	zn	=mZoneNodes[node];
			ZonePlane	p	=mZonePlanes[zn.mPlaneNum];

			float	dist	=Math.Abs(Vector3.Dot(trace.mMoveBox.Max, p.mNormal));

			Vector3	clipStart, clipEnd;
			if(PartBehind(p, -dist, start, end, out clipStart, out clipEnd))
			{
				bHit	=TraceBoxNodeTrigger(trace, clipStart, clipEnd, zn.mBack);
				if(bHit)
				{
					end	=trace.mIntersection;
				}
			}
			if(PartFront(p, -dist, start, end, out clipStart, out clipEnd))
			{
				bHit	|=TraceBoxNodeTrigger(trace, clipStart, clipEnd, zn.mFront);
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

			float	dist	=Math.Abs(Vector3.Dot(trace.mMoveBox.Max, p.mNormal));

			Vector3	clipStart, clipEnd;
			if(PartBehind(p, -dist, start, end, out clipStart, out clipEnd))
			{
				bHit	=TraceBoxNode(trace, clipStart, clipEnd, zn.mBack);
				if(bHit)
				{
					end	=trace.mIntersection;
				}
			}
			if(PartFront(p, -dist, start, end, out clipStart, out clipEnd))
			{
				bHit	|=TraceBoxNode(trace, clipStart, clipEnd, zn.mFront);
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
			else if(!bAnyInFront)
			{
				//started inside!
				trace.mbStartInside	=true;
				return	true;
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

				p.mDist	+=Math.Abs(Vector3.Dot(trace.mMoveBox.Max, p.mNormal));

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
			else if(!bAnyInFront)
			{
				//started inside!
				trace.mIntersection	=start;
				trace.mbStartInside	=true;
				return	true;
			}
			return	bClipped;
		}


		bool IsPointInLeaf(RayTrace trace, Vector3 point, ZoneLeaf zl)
		{
			if(zl.mNumSides <= 0)
			{
				return	false;
			}

			for(int i=0;i < zl.mNumSides;i++)
			{
				ZoneLeafSide	side	=mZoneLeafSides[i + zl.mFirstSide];
				ZonePlane		p		=mZonePlanes[side.mPlaneNum];

				if(side.mbFlipSide)
				{
					p.Inverse();
				}

				p.mDist	+=Math.Abs(Vector3.Dot(trace.mMoveBox.Max, p.mNormal));

				float	dist	=p.DistanceFast(point);
				if(dist > 0)
				{
					return	false;	//not intersecting
				}
			}
			return	true;
		}
	}
}