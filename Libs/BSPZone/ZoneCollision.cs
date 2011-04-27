using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPZone
{
	public partial class Zone
	{
		Vector3	GMins1, GMaxs1;
		Vector3	GMins2, GMaxs2;
		Vector3	GFront, GBack;
		bool	LeafHit;
		float	BestDist;

		//Globals returned in the bsp subdivision code
		Int32		GPlaneNum;
		ZonePlane	GlobalPlane;
		Int32		GlobalNode;
		Int32		GlobalSide;
		Vector3		GlobalI;
		float		GRatio;
		Int32		GlobalLeaf;
		bool		HitSet;


		//=====================================================================================
		//	Trace_WorldCollisionBBox
		//	Shoots a ray through the world, using the expandable leaf hull
		//  The hull is expanded by the input BBox to simulate the points having volume...
		//=====================================================================================
		public bool Trace_WorldCollisionBBox(Vector3 Mins, Vector3 Maxs, Vector3 Front,
			Vector3 Back, UInt32 Flags,	ref Vector3 I, ref ZonePlane P)
		{
			Vector3		NewFront, NewBack, OMins, OMaxs, BestI, Vect;
			Vector3		Impact;
			Int32		i, b;
			float		Dist, BestD;
			ZoneModel	BestModel;
			ZonePlane	BestPlane, Plane2;
			bool		Hit;

//			Model	=null;

			BestI	=Vector3.Zero;
			BestPlane	=new ZonePlane();
			BestD = 99999.0f;
			BestModel = null;
			Hit = false;				// Have not hit nothing yet...
			
			//GMins1/GMaxs1 is what is used to expand the plane out with
			GMins1 = Mins;
			GMaxs1 = Maxs;
			
			GFront = Front;
			GBack = Back;

//			BSPData = &World->CurrentBSP->BSPData;
//			Models = World->CurrentBSP->Models;

//			MiscNodes = mZoneNodes;
//			MiscPlanes = BSPData->GFXPlanes;
//			MiscLeafs = BSPData->GFXLeafs;
//			MiscSides = BSPData->GFXLeafSides;

//			assert(MiscNodes != NULL);
//			assert(MiscPlanes != NULL);
//			assert(MiscLeafs != NULL);
//			assert(MiscSides != NULL);

			Trace_GetMoveBox(Mins, Maxs, Front, Back, out OMins, out OMaxs);

			//Then test the world bsp(all models are the world bsp)
			//Go through each model, and find out what leafs we hit, keeping the closest intersection
//			for(i=0;i < mZoneModels.Length;i++)//, Models++)
			for(i=0;i < 1;i++)//, Models++)
			{				
				for(b=0;b < 3;b++)
				{
					if(UtilityLib.Mathery.VecIdx(OMaxs, b)
						< UtilityLib.Mathery.VecIdx(mZoneModels[i].mMins, b))
					{
						break;
					}
					if(UtilityLib.Mathery.VecIdx(OMins, b)
						> UtilityLib.Mathery.VecIdx(mZoneModels[i].mMaxs, b))
					{
						break;
					}
				}

				if(b != 3)
				{
					continue;
				}
				

				//Reset flags
				BestDist	=9999.0f;
				LeafHit		=false;

				//replacing below with
				GFront	=Front;
				GBack	=Back;

//				GFront	=Front - mZoneModels[i].mOrigin;
//				GBack	=Back - mZoneModels[i].mOrigin;
				
				// InverseTransform the point about models center of rotation
//				geXForm3d_TransposeTransform(&Models->XForm, &GFront, &NewFront);
//				geXForm3d_TransposeTransform(&Models->XForm, &GBack , &NewBack);

				// push back into world
//				Vector3_Add(&NewFront, &Models->Pivot, &GFront);
//				Vector3_Add(&NewBack , &Models->Pivot, &GBack);
				
				//Make out box out of this move so we only check the leafs it intersected with...
				Trace_GetMoveBox(Mins, Maxs, GFront, GBack, out GMins2, out GMaxs2);

				FindClosestLeafIntersection_r(mZoneModels[i].mRootNode[0]);

				if(LeafHit)
				{
					// Rotate the impact plane
//					geXForm3d_Rotate(&Models->XForm, &GlobalPlane.Normal, &GlobalPlane.Normal);
					
					// Rotate the impact point
//					Vector3_Subtract(&GlobalI, &Models->Pivot, &GlobalI);
//					geXForm3d_Transform(&Models->XForm, &GlobalI, &NewFront);
					//geXForm3d_Rotate(&Models->XForm, &GlobalI, &NewFront);
//					Vector3_Add(&NewFront, &Models->Pivot, &GlobalI);
					
					// Find the new plane distance based on the new impact point with the new plane
					GlobalPlane.mDist	=Vector3.Dot(GlobalPlane.mNormal, GlobalI);

					Vect	=GlobalI - Front;
					Dist	=Vect.Length();

					if(Dist < BestD)
					{
						BestD = Dist;
						BestI = GlobalI;
						BestPlane = GlobalPlane;
						BestModel = mZoneModels[i];
						Hit = true;
					}
				}

//				if ((Flags & GE_COLLIDE_NO_SUB_MODELS))
//					goto NoModels;
			}

			if(Hit)
			{
				I		=BestI;
				P		=BestPlane;
//				Model	=BestModel;
				return	true;
			}
			return	false;
		}


		//=====================================================================================
		//	FindClosestLeafIntersection_r
		//=====================================================================================
		void FindClosestLeafIntersection_r(Int32 Node)
		{
			Int32	Leaf;
			UInt32	Side, Contents;

			if(Node < 0)
			{
				Leaf = -(Node+1);
//				Contents = MiscLeafs[Leaf].Contents;
				Contents = mZoneLeafs[Leaf].mContents;

				//if (Contents != BSP_CONTENTS_SOLID && Contents != BSP_CONTENTS_WINDOW)
				if((Contents & BSPZone.Contents.BSP_CONTENTS_SOLID_CLIP) == 0)
				{
					return;		// Only solid leafs contain side info...
				}

				HitSet = false;
				
//				if (!MiscLeafs[Leaf].NumSides)
				if(mZoneLeafs[Leaf].mNumSides == 0)
				{
					return;
				}

				IntersectLeafSides_r(GFront, GBack, Leaf, 0, 1);
				return;
			}

			Side	=Trace_BoxOnPlaneSide(GMins2, GMaxs2, mZonePlanes[mZoneNodes[Node].mPlaneNum]);

			//Go down the sides that the box lands in
			if((Side & ZonePlane.PSIDE_FRONT) != 0)
			{
				FindClosestLeafIntersection_r(mZoneNodes[Node].mChildren[0]);
			}

			if((Side & ZonePlane.PSIDE_BACK) != 0)
			{
				FindClosestLeafIntersection_r(mZoneNodes[Node].mChildren[1]);
			}
		}


		//=====================================================================================
		//	Trace_ExpandPlaneForBox
		//	Pushes a plan out by the side of the box it is looking at
		//=====================================================================================
		void Trace_ExpandPlaneForBox(ref ZonePlane plane, Vector3 Mins, Vector3 Maxs)
		{
			Vector3	Normal;

			Normal = plane.mNormal;
			
			if(Normal.X > 0)
				plane.mDist -= Normal.X * Mins.X;
			else	 
				plane.mDist -= Normal.X * Maxs.X;
			
			if (Normal.Y > 0)
				plane.mDist -= Normal.Y * Mins.Y;
			else
				plane.mDist -= Normal.Y * Maxs.Y;

			if (Normal.Z > 0)
				plane.mDist -= Normal.Z * Mins.Z;
			else							 
				plane.mDist -= Normal.Z * Maxs.Z;
		}


		//=====================================================================================
		//	IntersectLeafSides
		//=====================================================================================
		bool IntersectLeafSides_r(Vector3 Front, Vector3 Back,
							Int32 Leaf,	Int32 Side, Int32 PSide)
		{
			float		Fd, Bd, Dist;
			ZonePlane	Plane;
			Int32		RSide, Side2;
			Vector3		I, Vec;

			if(PSide == 0)
				return false;

			if(Side >= mZoneLeafs[Leaf].mNumSides)
			{
				return true;		// if it lands behind all planes, it is inside
			}

			RSide = mZoneLeafs[Leaf].mFirstSide + Side;

			Plane = mZonePlanes[mZoneLeafSides[RSide].mPlaneNum];
			Plane.mType = ZonePlane.PLANE_ANY;
			
			if(mZoneLeafSides[RSide].mPlaneSide != 0)
			{
				Plane.Inverse();
			}
			
			//Simulate the point having a box, by pushing the plane out by the box size
			Trace_ExpandPlaneForBox(ref Plane, GMins1, GMaxs1);

			Fd	=Plane.DistanceFast(Front);
			Bd	=Plane.DistanceFast(Back);

			if(Fd >= 0 && Bd >= 0)	// Leaf sides are convex hulls, so front side is totally outside
			{
				return IntersectLeafSides_r(Front, Back, Leaf, Side+1, 0);
			}

			if(Fd < 0 && Bd < 0)
			{
				return IntersectLeafSides_r(Front, Back, Leaf, Side+1, 1);
			}

			//We have an intersection

			//Dist = Fd / (Fd - Bd);

			Side2	=(Fd < 0)? 1 : 0;
			
			if (Fd < 0)
				Dist = (Fd + UtilityLib.Mathery.ON_EPSILON)/(Fd-Bd);
			else
				Dist = (Fd - UtilityLib.Mathery.ON_EPSILON)/(Fd-Bd);

			if (Dist < 0.0f)
				Dist = 0.0f;
			
			if (Dist > 1.0f)
				Dist = 1.0f;

			I	=Front + Dist * (Back - Front);

			//Only go down the back side, since the front side is empty in a convex tree
			if(IntersectLeafSides_r(Front, I, Leaf, Side+1, Side2))
			{
				LeafHit = true;
				return true;
			}
			else if(IntersectLeafSides_r(I, Back, Leaf, Side+1, (Side2 == 0)? 1 : 0))
			{
				Vec	=I - GFront;
				Dist	=Vec.Length();

				//Record the intersection closest to the start of ray
				if(Dist < BestDist && !HitSet)
				{
					GlobalI = I;
					GlobalLeaf = Leaf;
					BestDist = Dist;
					GlobalPlane = Plane;
					GRatio = Dist;
					HitSet = true;
				}
				LeafHit = true;
				return true;
			}
			
			return false;	
		}


		//=====================================================================================
		//	IntersectLeafSides
		//=====================================================================================
		bool PointInLeafSides(Vector3 Pos, ZoneLeaf Leaf)
		{
			Int32		i, f;
			ZonePlane	Plane;
			float		Dist;

			f = Leaf.mFirstSide;

			for (i=0; i< Leaf.mNumSides; i++)
			{
				Plane = mZonePlanes[mZoneLeafSides[i+f].mPlaneNum];
				Plane.mType = ZonePlane.PLANE_ANY;
			
				if(mZoneLeafSides[i+f].mPlaneSide != 0)
				{
					Plane.Inverse();
				}

				//Simulate the point having a box, by pushing the plane out by the box size
				Trace_ExpandPlaneForBox(ref Plane, GMins1, GMaxs1);

				Dist	=Plane.DistanceFast(Pos);
				
				if(Dist >= 0.0f)
				{
					return false;	//Since leafs are convex, it must be outside...
				}
			}
			return	true;
		}


		//=====================================================================================
		//	Trace_BoxOnPlaneSide
		//	
		//	Returns PSIDE_FRONT, PSIDE_BACK, or PSIDE_BOTH
		//=====================================================================================
		UInt32 Trace_BoxOnPlaneSide(Vector3 Mins, Vector3 Maxs, ZonePlane Plane)
		{
			UInt32	Side;
			Int32	i;
			Vector3	[]Corners	=new Vector3[2];
			float	Dist1, Dist2;

			//Axial planes are easy
			if(Plane.mType < ZonePlane.PLANE_ANYX)
			{
				Side = 0;
				if(UtilityLib.Mathery.VecIdx(Maxs, Plane.mType) >= Plane.mDist)
				{
					Side	|=ZonePlane.PSIDE_FRONT;
				}
				if(UtilityLib.Mathery.VecIdx(Mins, Plane.mType) < Plane.mDist)
				{
					Side	|=ZonePlane.PSIDE_BACK;
				}
				return	Side;
			}

			//Create the proper leading and trailing verts for the box
			for(i=0;i < 3;i++)
			{
				if(UtilityLib.Mathery.VecIdx(Plane.mNormal, i) < 0)
				{
					UtilityLib.Mathery.VecIdxAssign(ref Corners[0], i,
						UtilityLib.Mathery.VecIdx(Mins, i));
					UtilityLib.Mathery.VecIdxAssign(ref Corners[1], i,
						UtilityLib.Mathery.VecIdx(Maxs, i));
				}
				else
				{
					UtilityLib.Mathery.VecIdxAssign(ref Corners[1], i,
						UtilityLib.Mathery.VecIdx(Mins, i));
					UtilityLib.Mathery.VecIdxAssign(ref Corners[0], i,
						UtilityLib.Mathery.VecIdx(Maxs, i));
				}
			}
			Dist1 = Vector3.Dot(Plane.mNormal, Corners[0]) - Plane.mDist;
			Dist2 = Vector3.Dot(Plane.mNormal, Corners[1]) - Plane.mDist;
			
			Side = 0;
			if(Dist1 >= 0)
			{
				Side	=ZonePlane.PSIDE_FRONT;
			}
			if(Dist2 < 0)
			{
				Side	|=ZonePlane.PSIDE_BACK;
			}
			return Side;
		}


		//=====================================================================================
		//	MoveBox
		//	Creates a box around the entire move
		//=====================================================================================
		void Trace_GetMoveBox(Vector3 Mins, Vector3 Maxs, Vector3 Front, Vector3 Back,
			out Vector3 OMins, out Vector3 OMaxs)
		{
//			assert(Mins);
//			assert(Maxs);
//			assert(Front);
//			assert(Back);

			OMins	=Vector3.Zero;
			OMaxs	=Vector3.Zero;
			
			for(int i=0;i < 3;i++)
			{
				if(UtilityLib.Mathery.VecIdx(Back, i)
					> UtilityLib.Mathery.VecIdx(Front, i))
				{
					UtilityLib.Mathery.VecIdxAssign(ref OMins, i,
						UtilityLib.Mathery.VecIdx(Front, i)
						+ UtilityLib.Mathery.VecIdx(Mins, i) - 1.0f);
					UtilityLib.Mathery.VecIdxAssign(ref OMaxs, i,
						UtilityLib.Mathery.VecIdx(Back, i)
						+ UtilityLib.Mathery.VecIdx(Maxs, i) + 1.0f);
				}
				else
				{
					UtilityLib.Mathery.VecIdxAssign(ref OMins, i,
						UtilityLib.Mathery.VecIdx(Back, i)
						+ UtilityLib.Mathery.VecIdx(Mins, i) - 1.0f);
					UtilityLib.Mathery.VecIdxAssign(ref OMaxs, i,
						UtilityLib.Mathery.VecIdx(Front, i)
						+ UtilityLib.Mathery.VecIdx(Maxs, i) + 1.0f);
				}
			}
		}
	}
}
