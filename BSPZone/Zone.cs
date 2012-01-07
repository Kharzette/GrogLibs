using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPZone
{
	internal class ZoneTrigger
	{
		internal ZoneEntity		mEntity;
		internal BoundingBox	mBox;
		internal Int32			mModelNum;
		internal bool			mbTriggered;
		internal bool			mbTriggerOnce;
	}

	public partial class Zone
	{
		class WorldLeaf
		{
//			public Int32	mVisFrame;
			public Int32	mParent;
		}

		//structural
		ZoneModel		[]mZoneModels;
		ZoneNode		[]mZoneNodes;
		ZoneLeaf		[]mZoneLeafs;
		ZoneLeafSide	[]mZoneLeafSides;
		ZonePlane		[]mZonePlanes;
		ZoneEntity		[]mZoneEntities;

		//debug vis stuff
		Int32			[]mDebugLeafFaces;
		DebugFace		[]mDebugFaces;
		Vector3			[]mDebugVerts;
		Int32			[]mDebugIndexes;
		VisCluster		[]mVisClusters;
		VisArea			[]mVisAreas;
		VisAreaPortal	[]mVisAreaPortals;

		//vis stuff
		Int32		[]mClusterVisFrame;
		WorldLeaf	[]mLeafData;
		Int32		[]mNodeParents;
		Int32		[]mNodeVisFrame;
		byte		[]mVisData;
		byte		[]mMaterialVisData;

		//gameplay stuff
		List<ZoneTrigger>	mTriggers	=new List<ZoneTrigger>();

		int	mLightMapGridSize;
		int	mNumVisLeafBytes;
		int	mNumVisMaterialBytes;

		public event EventHandler	eTriggerHit;

		const float	GroundAngle	=0.8f;	//how sloped can you be to be considered ground
		const float StepHeight	=18.0f;	//stair step height for bipeds


		#region IO
		void WritePlaneArray(BinaryWriter bw)
		{
			bw.Write(mZonePlanes.Length);
			for(int i=0;i < mZonePlanes.Length;i++)
			{
				mZonePlanes[i].Write(bw);
			}
		}


		void ReadPlaneArray(BinaryReader br)
		{
			int	count	=br.ReadInt32();
			mZonePlanes	=new ZonePlane[count];
			for(int i=0;i < count;i++)
			{
				mZonePlanes[i].Read(br);
			}
		}


		public void Write(string fileName, bool bDebug)
		{
			FileStream	file	=new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write);

			BinaryWriter	bw	=new BinaryWriter(file);

			UtilityLib.FileUtil.WriteArray(mZoneModels, bw);
			UtilityLib.FileUtil.WriteArray(mZoneNodes, bw);
			UtilityLib.FileUtil.WriteArray(mZoneLeafs, bw);
			UtilityLib.FileUtil.WriteArray(mVisAreas, bw);
			UtilityLib.FileUtil.WriteArray(mVisAreaPortals, bw);
			WritePlaneArray(bw);
			UtilityLib.FileUtil.WriteArray(mZoneEntities, bw);
			UtilityLib.FileUtil.WriteArray(mZoneLeafSides, bw);

			bw.Write(bDebug);
			if(bDebug)
			{
				UtilityLib.FileUtil.WriteArray(bw, mDebugLeafFaces);
				UtilityLib.FileUtil.WriteArray(mDebugFaces, bw);
				UtilityLib.FileUtil.WriteArray(bw, mDebugVerts);
				UtilityLib.FileUtil.WriteArray(bw, mDebugIndexes);
			}

			if(mVisData != null && mVisData.Length > 0)
			{
				bw.Write(true);
				UtilityLib.FileUtil.WriteArray(mVisData, bw);
			}
			else
			{
				bw.Write(false);
			}

			if(mMaterialVisData != null && mMaterialVisData.Length > 0)
			{
				bw.Write(true);
				UtilityLib.FileUtil.WriteArray(mMaterialVisData, bw);
			}
			else
			{
				bw.Write(false);
			}
			UtilityLib.FileUtil.WriteArray(mVisClusters, bw);
			bw.Write(mLightMapGridSize);
			bw.Write(mNumVisLeafBytes);
			bw.Write(mNumVisMaterialBytes);

			bw.Close();
			file.Close();
		}


		public void Read(string fileName, bool bTool)
		{
			Stream			file	=null;
			if(bTool)
			{
				file	=new FileStream(fileName, FileMode.Open, FileAccess.Read);
			}
			else
			{
				file	=UtilityLib.FileUtil.OpenTitleFile(fileName);
			}

			if(file == null)
			{
				return;
			}
			BinaryReader	br	=new BinaryReader(file);

			mZoneModels		=UtilityLib.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return UtilityLib.FileUtil.InitArray<ZoneModel>(count); }) as ZoneModel[];
			mZoneNodes		=UtilityLib.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return UtilityLib.FileUtil.InitArray<ZoneNode>(count); }) as ZoneNode[];
			mZoneLeafs		=UtilityLib.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return UtilityLib.FileUtil.InitArray<ZoneLeaf>(count); }) as ZoneLeaf[];
			mVisAreas		=UtilityLib.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return UtilityLib.FileUtil.InitArray<VisArea>(count); }) as VisArea[];
			mVisAreaPortals	=UtilityLib.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return UtilityLib.FileUtil.InitArray<VisAreaPortal>(count); }) as VisAreaPortal[];
			ReadPlaneArray(br);
			mZoneEntities	=UtilityLib.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return UtilityLib.FileUtil.InitArray<ZoneEntity>(count); }) as ZoneEntity[];
			mZoneLeafSides	=UtilityLib.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return UtilityLib.FileUtil.InitArray<ZoneLeafSide>(count); }) as ZoneLeafSide[];

			bool	bDebug	=br.ReadBoolean();
			if(bDebug)
			{
				mDebugLeafFaces	=UtilityLib.FileUtil.ReadIntArray(br);
				mDebugFaces		=UtilityLib.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return UtilityLib.FileUtil.InitArray<DebugFace>(count); }) as DebugFace[];
				mDebugVerts		=UtilityLib.FileUtil.ReadVecArray(br);
				mDebugIndexes	=UtilityLib.FileUtil.ReadIntArray(br);
			}

			mVisData			=UtilityLib.FileUtil.ReadByteArray(br);
			mMaterialVisData	=UtilityLib.FileUtil.ReadByteArray(br);

			mVisClusters	=UtilityLib.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return UtilityLib.FileUtil.InitArray<VisCluster>(count); }) as VisCluster[];

			mLightMapGridSize		=br.ReadInt32();
			mNumVisLeafBytes		=br.ReadInt32();
			mNumVisMaterialBytes	=br.ReadInt32();

			br.Close();
			file.Close();

			//make clustervisframe
			mClusterVisFrame	=new int[mVisClusters.Length];
			mNodeParents		=new int[mZoneNodes.Length];
			mNodeVisFrame		=new int[mZoneNodes.Length];
			mLeafData			=new WorldLeaf[mZoneLeafs.Length];

			//fill in leafdata with blank worldleafs
			for(int i=0;i < mZoneLeafs.Length;i++)
			{
				mLeafData[i]	=new WorldLeaf();
			}

			FindParents_r(mZoneModels[0].mRootNode, -1);

			//grab out triggers
			List<ZoneEntity>	trigs	=GetEntitiesStartsWith("trigger");
			foreach(ZoneEntity ze in trigs)
			{
				if(ze.mData.ContainsKey("Model"))
				{
					ZoneTrigger	zt		=new ZoneTrigger();
					string	modelNum	=ze.mData["Model"];
					zt.mModelNum		=Convert.ToInt32(modelNum);
					zt.mBox				=GetModelBounds(zt.mModelNum);
					zt.mEntity			=ze;
					zt.mbTriggered		=false;
					zt.mbTriggerOnce	=(ze.mData["classname"] == "trigger_once");

					mTriggers.Add(zt);
				}
			}
		}
		#endregion


		#region Entity Related
		public List<ZoneEntity> GetSwitchedOnLights()
		{
			List<ZoneEntity>	ret	=new List<ZoneEntity>();
			foreach(ZoneEntity e in mZoneEntities)
			{
				int	switchNum;
				if(!e.GetInt("LightSwitchNum", out switchNum))
				{
					continue;
				}

				int	spawnFlags;
				if(e.GetInt("spawnflags", out spawnFlags))
				{
					if(UtilityLib.Misc.bFlagSet(spawnFlags, 1))
					{
						continue;
					}
				}

				//no flags at all means start on
				ret.Add(e);
			}
			return	ret;
		}


		public Vector3 GetPlayerStartPos()
		{
			foreach(ZoneEntity e in mZoneEntities)
			{
				if(e.mData.ContainsKey("classname"))
				{
					if(e.mData["classname"] != "info_player_start")
					{
						continue;
					}
				}
				else
				{
					continue;
				}

				Vector3	ret	=Vector3.Zero;
				if(e.GetOrigin(out ret))
				{
					return	ret;
				}
			}
			return	Vector3.Zero;
		}


		public BoundingBox GetModelBounds(int modelNum)
		{
			if(modelNum > 0 && modelNum < mZoneModels.Length)
			{
				return	mZoneModels[modelNum].mBounds;
			}
			return	new BoundingBox();
		}


		public List<ZoneEntity> GetEntitiesByTargetName(string targName)
		{
			List<ZoneEntity>	ret	=new List<ZoneEntity>();
			foreach(ZoneEntity ze in mZoneEntities)
			{
				if(ze.mData.ContainsKey("targetname"))
				{
					if(ze.mData["targetname"] == targName)
					{
						ret.Add(ze);
					}
				}
			}
			return	ret;
		}


		public List<ZoneEntity> GetEntities(string className)
		{
			List<ZoneEntity>	ret	=new List<ZoneEntity>();
			foreach(ZoneEntity ze in mZoneEntities)
			{
				if(ze.mData.ContainsKey("classname"))
				{
					if(ze.mData["classname"] == className)
					{
						ret.Add(ze);
					}
				}
			}
			return	ret;
		}


		public List<ZoneEntity> GetEntitiesStartsWith(string startText)
		{
			List<ZoneEntity>	ret	=new List<ZoneEntity>();
			foreach(ZoneEntity ze in mZoneEntities)
			{
				if(ze.mData.ContainsKey("classname"))
				{
					if(ze.mData["classname"].StartsWith(startText))
					{
						ret.Add(ze);
					}
				}
			}
			return	ret;
		}


		//for assigning character lights
		public List<Vector3> GetNearestThreeLightsInLOS(Vector3 pos)
		{
			List<ZoneEntity>	lightsInVis	=new List<ZoneEntity>();

			foreach(ZoneEntity ent in mZoneEntities)
			{
				if(ent.IsLight())
				{
					Vector3	lightPos;
					if(ent.GetOrigin(out lightPos))
					{
						if(IsVisibleFrom(pos, lightPos))
						{
							Vector3	intersection	=Vector3.Zero;
							bool	bHitLeaf		=false;
							Int32	leafHit			=0;
							Int32	nodeHit			=0;
							if(!RayIntersect(pos, lightPos, 0, ref intersection,
								ref bHitLeaf, ref leafHit, ref nodeHit))
							{
								lightsInVis.Add(ent);
							}
						}
					}
				}
			}

			List<Vector3>	positions	=new List<Vector3>();

			foreach(ZoneEntity ent in mZoneEntities)
			{
				Vector3	lightPos;
				if(ent.GetOrigin(out lightPos))
				{
					positions.Add(lightPos);
				}
			}

			positions.Sort();

			if(positions.Count > 3)
			{
				positions.RemoveRange(3, positions.Count - 3);
			}
			return	positions;
		}
		#endregion


		#region Ray Casts and Movement
		public bool IsSphereInSolid(Vector3 pnt, float dist)
		{
			bool	bHitLeaf	=false;
			int		leafHit		=0;
			int		nodeHit		=0;
			return	SphereIntersect(pnt, dist, 0, ref bHitLeaf, ref leafHit, ref nodeHit);
		}


		//this is faked with a box for now
		//real capsule cast is broken
		public bool CapsuleCollide(Vector3 start, Vector3 end, float radius,
			ref Vector3 intersect, ref ZonePlane hitPlane)
		{
			return	Trace_WorldCollisionCapsule(start, end, radius, ref intersect, ref hitPlane);
		}


		public bool IsPointInSolid(Vector3 pnt)
		{
			int	node	=FindNodeLandedIn(0, pnt);

			if(node > 0)
			{
				return	true;	//is that right?  Can't remember
			}
			Int32	leaf	=-(node + 1);

			return	((mZoneLeafs[leaf].mContents & Contents.BSP_CONTENTS_SOLID2) != 0);
		}


		public bool RayCollide(Vector3 Front, Vector3 Back,
			ref Vector3 I, ref Int32 leafHit, ref Int32 nodeHit)
		{
			bool	hitLeaf	=false;
			if(RayIntersect(Front, Back, mZoneModels[0].mRootNode,
				ref I, ref hitLeaf, ref leafHit, ref nodeHit))
			{
				return	true;
			}
			return	false;
		}


		bool IsGround(ZonePlane p)
		{
			return	(Vector3.Dot(p.mNormal, Vector3.UnitY) > GroundAngle);
		}


		//returns true if on ground
		//this one assumes 2 legs, so navigates stairs
		public bool BipedMoveBox(BoundingBox box, Vector3 start,
								Vector3 end, ref Vector3 finalPos)
		{
			//try the original movement with no sliding
			Vector3		firstPos		=Vector3.Zero;
			Vector3		originalDelta	=end - start;
			ZonePlane	impactPlane		=new ZonePlane();

			//slide forward
			bool	bOn	=MoveBox(box, start, end, ref firstPos);
			if(!bOn)
			{
				//not on the ground
				finalPos	=firstPos;
				return	false;
			}

			//check the delta
			Vector3	firstDelta	=firstPos - start;
			float	delta		=originalDelta.LengthSquared() - firstDelta.LengthSquared();

			if(delta < UtilityLib.Mathery.ANGLE_EPSILON &&
				delta > -UtilityLib.Mathery.ANGLE_EPSILON)
			{
				//close enough to original
				finalPos	=start;
				return	true;
			}

			//try a step height up with the regular
			//box raycast (no sliding)
			Vector3		secondPos	=Vector3.Zero;
			Vector3		stepPos		=Vector3.Zero;
			if(Trace_WorldCollisionBBox(box, start, start + Vector3.Up * StepHeight, ref stepPos, ref impactPlane))
			{
				//hit something trying to step up
				//see if it's enough to bother with
				if((stepPos - start).LengthSquared() < 4)
				{
					stepPos	=start;
				}
			}
			else
			{
				stepPos	=start + Vector3.Up * StepHeight;
			}

			//try movement stepped
			bOn	=MoveBox(box, stepPos, end + Vector3.Up * StepHeight, ref stepPos);

			//raycast down a step height
			if(Trace_WorldCollisionBBox(box, stepPos, stepPos - Vector3.Up * StepHeight, ref stepPos, ref impactPlane))
			{
				if(!IsGround(impactPlane))
				{
					//use the non stepped movement
					finalPos	=firstPos;
					return	true;
				}
			}
			else			
			{
				//no impact, stepped off into midair
				//use the non stepped movement in this case
				finalPos	=firstPos;
				return	true;
			}

			Vector3	stepDelta	=stepPos - start;
			
			//see which went farther
			if(firstDelta.LengthSquared() > stepDelta.LengthSquared())
			{
				finalPos	=firstPos;
				return	true;
			}
			else
			{
				finalPos	=stepPos;
				return	true;
			}
		}


		//positions should be in the middle base of the box
		//returns true if on the ground
		public bool MoveBox(BoundingBox box, Vector3 start,
							Vector3 end, ref Vector3 finalPos)
		{
			Vector3		impacto		=Vector3.Zero;
			ZonePlane	firstPHit	=new ZonePlane();
			ZonePlane	secondPHit	=new ZonePlane();
			ZonePlane	thirdPHit	=new ZonePlane();

			if(Trace_WorldCollisionBBox(box, start, end, ref impacto, ref firstPHit))
			{
				//collisions from inside out will leave
				//an empty plane and impact
				if(!(impacto == Vector3.Zero && firstPHit.mNormal == Vector3.Zero))
				{
					//reflect the ray's energy
					float	dist	=firstPHit.DistanceFast(end);

					//push out of the plane
					end	-=(firstPHit.mNormal * dist);

					//ray cast again
					if(Trace_WorldCollisionBBox(box, start, end, ref impacto, ref secondPHit))
					{
						if(!(impacto == Vector3.Zero && secondPHit.mNormal == Vector3.Zero))
						{
							//push out of second plane
							dist	=secondPHit.DistanceFast(end);
							end		-=(secondPHit.mNormal * dist);

							//ray cast again
							if(Trace_WorldCollisionBBox(box, start, end, ref impacto, ref thirdPHit))
							{
								if(!(impacto == Vector3.Zero && thirdPHit.mNormal == Vector3.Zero))
								{
									//just use impact point
									end	=impacto;
								}
							}
						}
					}
				}
			}

			finalPos	=end;

			//check for a floorish plane
			if(IsGround(firstPHit) || IsGround(secondPHit) || IsGround(thirdPHit))
			{
				return	true;
			}
			return	false;
		}


		public void BoxTriggerCheck(BoundingBox box, Vector3 start, Vector3 end)
		{
			//TODO: full traced solution, just check contains for now
			List<Vector3>	bothCorners	=new List<Vector3>();

			Vector3	[]corners	=box.GetCorners();
			for(int i=0;i < corners.Length;i++)
			{
				Vector3	sCorn	=corners[i] + start;
				Vector3	eCorn	=corners[i] + end;

				bothCorners.Add(sCorn);
				bothCorners.Add(eCorn);
			}

			corners	=null;

			foreach(ZoneTrigger zt in mTriggers)
			{
				if(zt.mbTriggerOnce && zt.mbTriggered)
				{
					continue;
				}

				foreach(Vector3 pos in bothCorners)
				{
					ContainmentType	ct	=zt.mBox.Contains(pos);
					if(ct == ContainmentType.Disjoint)
					{
						continue;
					}

					zt.mbTriggered	=true;
					UtilityLib.Misc.SafeInvoke(eTriggerHit, zt.mEntity);
				}
			}
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
			dist	=mZonePlanes[pNode.mPlaneNum].DistanceFast(pos);

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
		#endregion


		#region Vis Stuff
		public bool IsMaterialVisibleFromPos(Vector3 pos, int matIndex)
		{
			if(mZoneNodes == null)
			{
				return	true;	//no map data
			}
			Int32	node	=FindNodeLandedIn(0, pos);
			if(node > 0)
			{
				return	true;	//in solid space
			}

			Int32	leaf	=-(node + 1);
			return	IsMaterialVisible(leaf, matIndex);
		}


		//only used for debugging vis
		public int GetVisibleGeometry(Vector3 pos, List<Vector3> verts, List<UInt32> inds)
		{
			if(mDebugFaces == null)
			{
				return	-1;	//no debug info saved
			}

			Int32	posNode	=FindNodeLandedIn(0, pos);
			if(posNode > 0)
			{
				return	0;	//solid
			}

			Int32	leaf	=-(posNode + 1);
			Int32	clust	=mZoneLeafs[leaf].mCluster;

			if(clust == -1 || mVisClusters[clust].mVisOfs == -1)
			{
				return	-69;	//no info for position
			}

			Int32	ofs	=mVisClusters[clust].mVisOfs;

			int	leafsVisible	=0;
			foreach(ZoneLeaf zl in mZoneLeafs)
			{
				Int32	c	=zl.mCluster;

				if(c < 0)
				{
					continue;
				}

				if((mVisData[ofs + (c >> 3)] & (1 << (c & 7))) == 0)
				{
					continue;
				}

				leafsVisible++;

				for(int i=0;i < zl.mNumFaces;i++)
				{
					int		vofs	=verts.Count;
					int		face	=mDebugLeafFaces[zl.mFirstFace + i];
					int		nverts	=mDebugFaces[face].mNumVerts;
					int		fvert	=mDebugFaces[face].mFirstVert;

					for(int j=fvert;j < (fvert + nverts);j++)
					{
						int	idx	=mDebugIndexes[j];
						verts.Add(mDebugVerts[idx]);
					}

					for(int k=1;k < nverts-1;k++)
					{
						//initial vertex
						inds.Add((UInt32)vofs);
						inds.Add((UInt32)(vofs + k));
						inds.Add((UInt32)(vofs + ((k + 1) % nverts)));
					}
				}
			}
			return	leafsVisible;
		}


		public Vector3 GetClusterCenter(int clust)
		{
			Vector3	ret	=Vector3.Zero;
			foreach(ZoneLeaf zl in mZoneLeafs)
			{
				if(zl.mCluster != clust)
				{
					continue;
				}

				ret	+=((zl.mMaxs + zl.mMins) / 2.0f);
			}
			return	ret;
		}


		public bool IsVisibleFrom(Vector3 posA, Vector3 posB)
		{
			Int32	posANode	=FindNodeLandedIn(0, posA);
			if(posANode > 0)
			{
				return	false;	//position in solid
			}

			Int32	posBNode	=FindNodeLandedIn(0, posB);
			if(posBNode > 0)
			{
				return	false;	//position in solid
			}

			Int32	leafA	=-(posANode + 1);
			Int32	leafB	=-(posBNode + 1);

			Int32	clusterA	=mZoneLeafs[leafA].mCluster;
			Int32	clusterB	=mZoneLeafs[leafB].mCluster;

			if(clusterA == -1 || mVisClusters[clusterA].mVisOfs == -1)
			{
				return	false;	//no vis info for position
			}
			if(clusterB == -1 || mVisClusters[clusterB].mVisOfs == -1)
			{
				return	false;	//no vis info for position
			}

			int	ofs	=mVisClusters[clusterA].mVisOfs;

			if((mVisData[ofs + (clusterB >> 3)] & (1 << (clusterB & 7))) != 0)
			{
				return	true;	//A can see B
			}
			return	false;
		}


		bool IsMaterialVisible(int leaf, int matIndex)
		{
			if(mZoneLeafs == null)
			{
				return	false;
			}

			int	clust	=mZoneLeafs[leaf].mCluster;

			if(clust == -1 || mVisClusters[clust].mVisOfs == -1
				|| mMaterialVisData == null)
			{
				return	true;	//this will make everything vis
								//when outside of the map
			}

			//plus one to avoid 0 problem
			matIndex++;

			int	ofs	=leaf * mNumVisMaterialBytes;
			
			return	((mMaterialVisData[ofs + (matIndex >> 3)] & (1 << (matIndex & 7))) != 0);
		}


		void FindParents_r(Int32 Node, Int32 Parent)
		{
			if(Node < 0)		// At a leaf, mark leaf parent and return
			{
				mLeafData[-(Node + 1)].mParent	=Parent;
				return;
			}

			//At a node, mark node parent, and keep going till hitting a leaf
			mNodeParents[Node]	=Parent;

			// Go down front and back markinf parents on the way down...
			FindParents_r(mZoneNodes[Node].mFront, Node);
			FindParents_r(mZoneNodes[Node].mBack, Node);
		}
		#endregion


		public void GetBounds(out Vector3 mins, out Vector3 maxs)
		{
			if(mZoneModels.Length <= 0)
			{
				mins	=Vector3.Zero;
				maxs	=Vector3.Zero;
				return;
			}
			mins	=mZoneModels[0].mBounds.Min;
			maxs	=mZoneModels[0].mBounds.Max;
		}
	}
}