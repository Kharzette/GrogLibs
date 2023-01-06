using System;
using System.IO;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using UtilityLib;


namespace BSPZone
{
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
		List<ZoneEntity>	mEntities	=new List<ZoneEntity>();

		int	mLightMapGridSize;
		int	mNumVisLeafBytes;
		int	mNumVisMaterialBytes;

		//pathing uses these to make the graph connections too
		public const float	RampAngle		=0.7f;	//how steep can we climb?
		public const float	StepHeight		=18.0f;	//stair step height for bipeds
		public const float	StepDownHeight	=10.0f;	//how far down can a biped step
													//while in a sprint?

		const int	MaxMoveBoxIterations	=16;


		#region IO
		public void Write(string fileName, bool bDebug)
		{
			FileStream	file	=new FileStream(fileName, FileMode.Create, FileAccess.Write);

			BinaryWriter	bw	=new BinaryWriter(file);

			FileUtil.WriteArray(mZoneModels, bw);
			FileUtil.WriteArray(mZoneNodes, bw);
			FileUtil.WriteArray(mZoneLeafs, bw);
			FileUtil.WriteArray(mVisAreas, bw);
			FileUtil.WriteArray(mVisAreaPortals, bw);
			FileUtil.WriteArray(mZonePlanes, bw);
			FileUtil.WriteArray(mEntities.ToArray(), bw);
			FileUtil.WriteArray(mZoneLeafSides, bw);

			bw.Write(bDebug);
			if(bDebug)
			{
				FileUtil.WriteArray(bw, mDebugLeafFaces);
				FileUtil.WriteArray(mDebugFaces, bw);
				FileUtil.WriteArray(bw, mDebugVerts);
				FileUtil.WriteArray(bw, mDebugIndexes);
			}

			if(mVisData != null && mVisData.Length > 0)
			{
				bw.Write(true);
				FileUtil.WriteArray(mVisData, bw);
			}
			else
			{
				bw.Write(false);
			}

			if(mMaterialVisData != null && mMaterialVisData.Length > 0)
			{
				bw.Write(true);
				FileUtil.WriteArray(mMaterialVisData, bw);
			}
			else
			{
				bw.Write(false);
			}
			FileUtil.WriteArray(mVisClusters, bw);
			bw.Write(mLightMapGridSize);
			bw.Write(mNumVisLeafBytes);
			bw.Write(mNumVisMaterialBytes);

			bw.Close();
			file.Close();
		}


		public void Read(string fileName, bool bTool)
		{
			Stream	file	=new FileStream(fileName, FileMode.Open, FileAccess.Read);
			if(file == null)
			{
				return;
			}
			BinaryReader	br	=new BinaryReader(file);

			mZoneModels		=FileUtil.ReadArray<ZoneModel>(br);
			mZoneNodes		=FileUtil.ReadArray<ZoneNode>(br);
			mZoneLeafs		=FileUtil.ReadArray<ZoneLeaf>(br);
			mVisAreas		=FileUtil.ReadArray<VisArea>(br);
			mVisAreaPortals	=FileUtil.ReadArray<VisAreaPortal>(br);
			mZonePlanes		=FileUtil.ReadArray<ZonePlane>(br);

			ZoneEntity	[]ents	=FileUtil.ReadArray<ZoneEntity>(br);
			mEntities.AddRange(ents);

			mZoneLeafSides	=FileUtil.ReadArray<ZoneLeafSide>(br);

			bool	bDebug	=br.ReadBoolean();
			if(bDebug)
			{
				mDebugLeafFaces	=FileUtil.ReadIntArray(br);
				mDebugFaces		=FileUtil.ReadArray<DebugFace>(br);
				mDebugVerts		=FileUtil.ReadVecArray(br);
				mDebugIndexes	=FileUtil.ReadIntArray(br);
			}

			mVisData			=FileUtil.ReadByteArray(br);
			mMaterialVisData	=FileUtil.ReadByteArray(br);

			mVisClusters	=FileUtil.ReadArray<VisCluster>(br);

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

			BuildNonCollidingModelsList();
		}
		#endregion


		public void FreeAll()
		{
			mNonCollidingModels.Clear();
			mPushables.Clear();			
		}


		#region Model Related
		public void ClearPushableVelocities(float secDelta)//, Microsoft.Xna.Framework.Audio.AudioListener lis)
		{
			Debug.Assert(secDelta > 0f);	//zero deltatimes are not good for this stuff

			//clear pushable push velocities
			foreach(KeyValuePair<object, Pushable> push in mPushables)
			{
				push.Value.mMobile.ClearPushVelocity();
			}
		}


		public Matrix GetModelTransform(int modelIndex)
		{
			if(modelIndex >= mZoneModels.Length)
			{
				return	Matrix.Identity;
			}

			return	mZoneModels[modelIndex].mTransform;
		}


		public BoundingBox GetModelBounds(int modelNum)
		{
			if(mZoneModels != null && modelNum > 0 && modelNum < mZoneModels.Length)
			{
				return	mZoneModels[modelNum].mBounds;
			}
			return	new BoundingBox();
		}
		#endregion


		#region Pathfinding Support
		void GetWalkableFaces(int node,
			List<List<Vector3>> polys,	//will be clipped to the leaf
			List<int> leaves,
			List<int> debugFacesUsed)
		{
			if(node < 0)
			{
				Int32	leaf	=-(node + 1);

				ZoneLeaf	zLeaf	=mZoneLeafs[leaf];

				//I remember having trouble with details in the past
				//but I can't remember the specifics, and it seems like
				//they work fine now so removing this
//				if(Misc.bFlagSet(zLeaf.mContents, Contents.BSP_CONTENTS_DETAIL2))
//				{
//					return;
//				}

				for(int f=0;f < zLeaf.mNumFaces;f++)
				{
					int	leafFace	=mDebugLeafFaces[f + zLeaf.mFirstFace];

					//faces shouldn't be in multiple nodes anymore
					//since we stopped merging faces
					Debug.Assert(!debugFacesUsed.Contains(leafFace));

					DebugFace	df	=mDebugFaces[leafFace];

					//check flags
					if(Misc.bFlagSet(df.mFlags, Zone.SKY))
					{
						continue;
					}

					//get plane
					ZonePlane	zp	=mZonePlanes[df.mPlaneNum];
					if(df.mbFlipSide)
					{
						zp.Inverse();
					}

					if(!zp.IsGround())
					{
						continue;
					}

					List<Vector3>	poly	=new List<Vector3>();
					for(int v=0;v < df.mNumVerts;v++)
					{
						int	idx	=mDebugIndexes[v + df.mFirstVert];

						poly.Add(mDebugVerts[idx]);
					}

					polys.Add(poly);
					leaves.Add(node);
					debugFacesUsed.Add(leafFace);
				}
				return;
			}

			ZoneNode	n	=mZoneNodes[node];

			GetWalkableFaces(n.mFront, polys, leaves, debugFacesUsed);
			GetWalkableFaces(n.mBack, polys, leaves, debugFacesUsed);
		}


		public void GetWalkableFaces(out List<List<Vector3>> polys, out List<int> leaves)
		{
			polys	=new List<List<Vector3>>();
			leaves	=new List<int>();

			List<int>	debugFacesUsed	=new List<int>();

			GetWalkableFaces(mZoneModels[0].mRootNode, polys, leaves, debugFacesUsed);
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

			GetModelGeometry(verts, inds);

			return	leafsVisible;
		}


		public void GetNodeGeometry(int node, List<Vector3> verts, List<UInt32> inds)
		{
			if(node > 0)
			{
				return;
			}
			node	=-(node + 1);

			ZoneLeaf	zl	=mZoneLeafs[node];

			int	firstFace	=zl.mFirstFace;
			int	numFaces	=zl.mNumFaces;

			for(int j=firstFace;j < (firstFace + numFaces);j++)
			{
				int		vofs	=verts.Count;
				int		face	=mDebugLeafFaces[j];
				int		nverts	=mDebugFaces[face].mNumVerts;
				int		fvert	=mDebugFaces[face].mFirstVert;

				for(int k=fvert;k < (fvert + nverts);k++)
				{
					int	idx	=mDebugIndexes[k];

					Vector3	transd	=mDebugVerts[idx];

					verts.Add(transd);
				}

				for(int z=1;z < nverts-1;z++)
				{
					//initial vertex
					inds.Add((UInt32)vofs);
					inds.Add((UInt32)(vofs + z));
					inds.Add((UInt32)(vofs + ((z + 1) % nverts)));
				}
			}
		}


		//this doesn't look right at all
		public void GetModelGeometry(List<Vector3> verts, List<UInt32> inds)
		{
			if(mDebugFaces == null || mZoneModels.Length < 2)
			{
				return;	//no debug info saved
			}

			for(int i=1;i < mZoneModels.Length;i++)
			{
				int	firstFace	=mZoneModels[i].mFirstFace;
				int	numFaces	=mZoneModels[i].mNumFaces;

				for(int j=firstFace;j < (firstFace + numFaces);j++)
				{
					int		vofs	=verts.Count;
					int		face	=j;
					int		nverts	=mDebugFaces[face].mNumVerts;
					int		fvert	=mDebugFaces[face].mFirstVert;

					for(int k=fvert;k < (fvert + nverts);k++)
					{
						int	idx	=mDebugIndexes[k];

						Vector3	transd	=Vector3.TransformCoordinate(mDebugVerts[idx], mZoneModels[i].mTransform);

						verts.Add(transd);
					}

					for(int z=1;z < nverts-1;z++)
					{
						//initial vertex
						inds.Add((UInt32)vofs);
						inds.Add((UInt32)(vofs + z));
						inds.Add((UInt32)(vofs + ((z + 1) % nverts)));
					}
				}
			}
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


		//http://www.youtube.com/user/helpmefindparents
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
			mins	=mZoneModels[0].mBounds.Minimum;
			maxs	=mZoneModels[0].mBounds.Maximum;
		}
	}
}