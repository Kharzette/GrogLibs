using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using UtilityLib;

using SharpDX;
using SharpDX.Direct3D11;

using Buffer	=SharpDX.Direct3D11.Buffer;
using MatLib	=MaterialLib.MaterialLib;


namespace BSPCore
{
	public partial class Map
	{
		public enum DebugDrawChoice
		{
			MapBrushes, CollisionBrushes, GFXFaces
		}

		List<MapEntity>	mEntities;

		//models
		internal List<GBSPModel>	mModels	=new List<GBSPModel>();

		//planes
		PlanePool	mPlanePool	=new PlanePool();

		//texinfos
		TexInfoPool	mTIPool	=new TexInfoPool();

		//area stuff
		List<GFXArea>		mAreas		=new List<GFXArea>();
		List<GFXAreaPortal>	mAreaPorts	=new List<GFXAreaPortal>();

		//gfx data
		GFXModel		[]mGFXModels;
		GFXNode			[]mGFXNodes;
		GFXLeaf			[]mGFXLeafs;
		GFXCluster		[]mGFXClusters;
		GFXArea			[]mGFXAreas;
		GFXAreaPortal	[]mGFXAreaPortals;
		GFXPlane		[]mGFXPlanes;
		GFXFace			[]mGFXFaces;
		Int32			[]mGFXLeafFaces;
		GFXLeafSide		[]mGFXLeafSides;
		Vector3			[]mGFXVerts;
		Int32			[]mGFXVertIndexes;
		Vector3			[]mGFXRGBVerts;
		GFXTexInfo		[]mGFXTexInfos;
		MapEntity		[]mGFXEntities;
		byte			[]mGFXLightData;
		int				mLightMapGridSize;


		public Map()
		{
			MakeSampleOffsets();
		}


		bool IsPointInSolidSpace(Vector3 pos)
		{
			Int32	node	=FindNodeLandedIn(0, pos);

			Int32	Leaf	=-(node + 1);

			return	((mGFXLeafs[Leaf].mContents & Contents.BSP_CONTENTS_SOLID2) != 0);
		}


		bool RayIntersect(Vector3 Front, Vector3 Back, Int32 Node, ref Vector3 intersectionPoint, ref bool hitLeaf)
		{
			float	Fd, Bd, Dist;
			Int32	Side;
			Vector3	I;

			if(Node < 0)						
			{
				Int32	Leaf	=-(Node+1);

				if((mGFXLeafs[Leaf].mContents
					& Contents.BSP_CONTENTS_SOLID2) != 0)
				{
					return	true;	//Ray collided with solid space
				}
				else 
				{
					return	false;	//Ray collided with empty space
				}
			}
			GFXNode		n	=mGFXNodes[Node];
			GFXPlane	p	=mGFXPlanes[n.mPlaneNum];

			Fd	=p.DistanceFast(Front);
			Bd	=p.DistanceFast(Back);

			if(Fd >= -1 && Bd >= -1)
			{
				return(RayIntersect(Front, Back, n.mFront, ref intersectionPoint, ref hitLeaf));
			}
			if(Fd < 1 && Bd < 1)
			{
				return(RayIntersect(Front, Back, n.mBack, ref intersectionPoint, ref hitLeaf));
			}

			Side	=(Fd < 0)? 1 : 0;
			Dist	=Fd / (Fd - Bd);

			I	=Front + Dist * (Back - Front);

			//Work our way to the front, from the back side.  As soon as there
			//is no more collisions, we can assume that we have the front portion of the
			//ray that is in empty space.  Once we find this, and see that the back half is in
			//solid space, then we found the front intersection point...
			if(RayIntersect(Front, I,
				(Fd < 0)? n.mBack : n.mFront,
				ref intersectionPoint, ref hitLeaf))
			{
				return	true;
			}
			else if(RayIntersect(I, Back,
				(Fd < 0)? n.mFront : n.mBack,
				ref intersectionPoint, ref hitLeaf))
			{
				if(!hitLeaf)
				{
					intersectionPoint	=I;
					hitLeaf				=true;
				}
				return	true;
			}
			return	false;
		}


		//scores based on the distances inside the edge vectors
		bool FaceContainsPointScore(GFXFace f, Vector3 point, out float offBy)
		{
			//make some edges
			List<Vector3>	edges	=new List<Vector3>();
			for(int i=0;i < f.mNumVerts;i++)
			{
				int	idx0	=mGFXVertIndexes[i + f.mFirstVert];
				int	idx1	=mGFXVertIndexes[((i + 1) % f.mNumVerts) + f.mFirstVert];

				Vector3	vert		=mGFXVerts[idx0];
				Vector3	vertPlusOne	=mGFXVerts[idx1];

				Vector3	edge	=vertPlusOne - vert;
				edge.Normalize();

				edges.Add(edge);
			}

			GFXPlane	p	=mGFXPlanes[f.mPlaneNum];

			//make edge planes
			List<GBSPPlane>	edgePlanes	=new List<GBSPPlane>();
			for(int i=0;i < f.mNumVerts;i++)
			{
				int		idx0	=mGFXVertIndexes[i + f.mFirstVert];
				Vector3	vert	=mGFXVerts[idx0];

				GBSPPlane	edgePlane	=new GBSPPlane();
				edgePlane.mNormal		=Vector3.Cross(p.mNormal, edges[i]);

				edgePlane.mNormal.Normalize();

				edgePlane.mDist			=Vector3.Dot(vert, edgePlane.mNormal);

				edgePlanes.Add(edgePlane);
			}

			offBy	=0f;
			foreach(GBSPPlane pl in edgePlanes)
			{
				float	dist	=pl.Distance(point);
				if(f.mbFlipSide)
				{
					dist	=-dist;
				}

				if(dist > 0f)
				{
					offBy	=dist;
					return	false;
				}
			}
			return	true;
		}


		//note that the front and back checks are 0 instead of 1!
		bool RayIntersectFace(Vector3 Front, Vector3 Back, Int32 Node,
			ref Vector3 intersectionPoint, ref bool hitLeaf, ref GFXFace hit)
		{
			float	Fd, Bd, Dist;
			Int32	Side;
			Vector3	I;

			if(Node < 0)						
			{
				Int32	Leaf	=-(Node+1);
				GFXLeaf	theLeaf	=mGFXLeafs[Leaf];

				if((theLeaf.mContents & Contents.BSP_CONTENTS_SOLID2) != 0)
				{
					return	true;	//Ray collided with solid space
				}
				else 
				{
					return	false;	//Ray collided with empty space
				}
			}
			GFXNode		n	=mGFXNodes[Node];
			GFXPlane	p	=mGFXPlanes[n.mPlaneNum];

			Fd	=p.DistanceFast(Front);
			Bd	=p.DistanceFast(Back);

			Side	=(Fd < 0)? 1 : 0;
			Dist	=Fd / (Fd - Bd);

			I	=Front + Dist * (Back - Front);

			if(Fd >= 0 && Bd >= 0)
			{
				return	RayIntersectFace(Front, Back, n.mFront,
							ref intersectionPoint, ref hitLeaf, ref hit);
			}
			if(Fd < 0 && Bd < 0)
			{
				return	RayIntersectFace(Front, Back, n.mBack,
							ref intersectionPoint, ref hitLeaf, ref hit);
			}

			//Work our way to the front, from the back side.  As soon as there
			//is no more collisions, we can assume that we have the front portion of the
			//ray that is in empty space.  Once we find this, and see that the back half is in
			//solid space, then we found the front intersection point...
			if(RayIntersectFace(Front, I,
				(Fd < 0)? n.mBack : n.mFront,
				ref intersectionPoint, ref hitLeaf, ref hit))
			{
				return	true;
			}
			else
			{
				bool	bSolid	=RayIntersectFace(I, Back,
					(Fd < 0)? n.mFront : n.mBack,
					ref intersectionPoint, ref hitLeaf, ref hit);

				if(bSolid)
				{
					if(!hitLeaf)
					{
						intersectionPoint	=I;
						hitLeaf				=true;
						if(hit == null)
						{
							if(n.mNumFaces == 1)
							{
								hit	=mGFXFaces[n.mFirstFace];
							}
							else
							{
								//find the best face
								float	bestScore	=float.MaxValue;
								for(int i=0;i < n.mNumFaces;i++)
								{
									GFXFace		f	=mGFXFaces[i + n.mFirstFace];
									GFXPlane	pl	=mGFXPlanes[f.mPlaneNum];

									float	dist	=pl.DistanceFast(intersectionPoint);
									if(dist < -0.001f || dist > 0.001f)
									{
										CoreEvents.Print("Face " + dist + " units off its plane!\n");
									}

									float	score;
									if(FaceContainsPointScore(f, intersectionPoint, out score))
									{
										hit	=f;
										break;
									}

									if(score < bestScore)
									{
										bestScore	=score;
										hit			=f;
									}
								}
							}
						}
					}
					return	true;
				}
			}
			return	false;
		}


		//tests vs world (model 0) as well as modelIndex if not zero
		//modelInv should be an inverted model matrix
		bool RayCollide(Vector3 Front, Vector3 Back, int modelIndex, Matrix modelInv)
		{
			bool	hitLeaf			=false;
			Vector3	worldImpacto	=Vector3.Zero;

			if(RayIntersect(Front, Back, mGFXModels[0].mRootNode, ref worldImpacto, ref hitLeaf))
			{
				return	true;
			}

			if(modelIndex > 0)
			{
				Vector3	modelImpacto	=Vector3.Zero;

				Vector3	frontInv	=Vector3.TransformCoordinate(Front, modelInv);
				Vector3	backInv		=Vector3.TransformCoordinate(Back, modelInv);

				if(RayIntersect(frontInv, backInv, mGFXModels[modelIndex].mRootNode, ref modelImpacto, ref hitLeaf))
				{
					return	true;
				}
			}
			return	false;
		}


		//tests vs world (model 0) as well as modelIndex if not zero
		//modelInv should be an inverted model matrix
		bool RayCollideToFace(Vector3 Front, Vector3 Back, int modelIndex, Matrix modelInv, ref GFXFace hit)
		{
			bool	hitLeaf			=false;
			Vector3	worldImpacto	=Vector3.Zero;

			if(RayIntersectFace(Front, Back, mGFXModels[0].mRootNode,
				ref worldImpacto, ref hitLeaf, ref hit))
			{
				return	true;
			}

			if(modelIndex > 0)
			{
				Vector3	modelImpacto	=Vector3.Zero;

				Vector3	frontInv	=Vector3.TransformCoordinate(Front, modelInv);
				Vector3	backInv		=Vector3.TransformCoordinate(Back, modelInv);

				if(RayIntersectFace(frontInv, backInv, mGFXModels[modelIndex].mRootNode,
					ref modelImpacto, ref hitLeaf, ref hit))
				{
					return	true;
				}
			}
			return	false;
		}


		#region Queries
		public Dictionary<int, Matrix> GetModelTransforms()
		{
			if(mGFXModels == null)
			{
				return	null;
			}

			Dictionary<int, Matrix>	ret	=new Dictionary<int,Matrix>();

			for(int i=0;i < mGFXModels.Length;i++)
			{
				Matrix	mat	=Matrix.Translation(mGFXModels[i].mOrigin);

				ret.Add(i, mat);
			}
			return	ret;
		}


		bool IsGround(GFXPlane p)
		{
			return	(Vector3.Dot(p.mNormal, Vector3.UnitY) > 0.8f);
		}


		public bool HasLightData()
		{
			return	(mGFXLightData != null && mGFXLightData.Length > 0);
		}


		public void GetTriangles(List<Vector3> verts,
			List<Vector3> normals,
			List<Color> colors,
			List<UInt16> indexes,
			DebugDrawChoice drawChoice)
		{
			if(drawChoice == DebugDrawChoice.MapBrushes)
			{
				if(mEntities != null)
				{
					mEntities[0].GetTriangles(mPlanePool, verts, normals, colors, indexes, false);
//					foreach(MapEntity ent in mEntities)
//					{
//						ent.GetTriangles(mPlanePool, verts, normals, colors, indexes, false);
//					}
				}
				else
				{
					CoreEvents.Print("This is intended for use pre build.\n");
				}
			}
			else if(drawChoice == DebugDrawChoice.CollisionBrushes)
			{
				if(mEntities != null)
				{
					foreach(MapEntity ent in mEntities)
					{
						ent.GetTriangles(mPlanePool, verts, normals, colors, indexes, true);
					}
				}
				else
				{
					CoreEvents.Print("This is intended for use pre build.\n");
				}
			}
			else if(drawChoice == DebugDrawChoice.GFXFaces)
			{
				if(mGFXFaces == null)
				{
					return;
				}

				Random	rnd	=new Random();

				foreach(GFXFace face in mGFXFaces)
				{
					UInt16	vofs	=(UInt16)verts.Count;
					Color	fColor	=Mathery.RandomColor(rnd);

					for(int i=0;i < face.mNumVerts;i++)
					{
						int	idx	=mGFXVertIndexes[i + face.mFirstVert];

						if(face.mbFlipSide)
						{
							normals.Add(-mGFXPlanes[face.mPlaneNum].mNormal);
						}
						else
						{
							normals.Add(mGFXPlanes[face.mPlaneNum].mNormal);
						}

						verts.Add(mGFXVerts[idx]);
						colors.Add(fColor);
					}

					for(UInt16 i=1;i < (face.mNumVerts - 1);i++)
					{
						indexes.Add((UInt16)(vofs + ((i + 1) % (UInt16)face.mNumVerts)));
						indexes.Add((UInt16)(vofs + i));
						indexes.Add(vofs);
					}
				}
			}
		}


		public Vector3 GetPlayerStartPos()
		{
			foreach(MapEntity e in mGFXEntities)
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


		public Vector3 GetFirstLightPos()
		{
			foreach(MapEntity e in mEntities)
			{
				if(e == GetWorldSpawnEntity())
				{
					continue;
				}
				float dist;
				if(e.GetLightValue(out dist))
				{
					Vector3	ret;
					e.GetOrigin(out ret);
					return	ret;
				}
			}
			return	Vector3.Zero;
		}


		public MapEntity GetWorldSpawnEntity()
		{
			foreach(MapEntity e in mEntities)
			{
				if(e.mData.ContainsKey("classname"))
				{
					if(e.mData["classname"] == "worldspawn")
					{
						return	e;
					}
				}
			}
			return	null;
		}


		public int GetNumWorldBrushes()
		{
			MapEntity	me	=GetWorldSpawnEntity();
			if(me == null)
			{
				return	0;
			}

			return	me.GetBrushes().Count;
		}


		public void GetWorldBrushTrianglesByIndex(int brushIndex,
			List<Vector3> verts,
			List<Vector3> norms,
			List<Color> colors,
			List<UInt16> indexes)
		{
			MapEntity	me	=GetWorldSpawnEntity();
			if(me == null)
			{
				return;
			}
			if(me.GetBrushes().Count > brushIndex)
			{
				me.GetBrushes()[brushIndex].GetTriangles(new Random(),
					mPlanePool, verts, norms, colors, indexes, false);
			}
			else
			{
				CoreEvents.Print("Brush index out of range!\n");
			}
		}


		public Bounds GetWorldBrushBoundsByIndex(int brushIndex)
		{
			MapEntity	me	=GetWorldSpawnEntity();
			if(me == null)
			{
				return	null;
			}

			if(me.GetBrushes().Count > brushIndex)
			{
				return	me.GetBrushes()[brushIndex].GetBounds();
			}
			else
			{
				CoreEvents.Print("Brush index out of range!\n");
				return	null;
			}
		}


		public List<GFXPlane> GetWorldBrushPlanesByIndex(int brushIndex)
		{
			List<GFXPlane>	ret	=new List<GFXPlane>();

			MapEntity	me	=GetWorldSpawnEntity();
			if(me == null)
			{
				return	null;
			}

			if(me.GetBrushes().Count > brushIndex)
			{
				foreach(GBSPSide gs in me.GetBrushes()[brushIndex].mOriginalSides)
				{
					GBSPPlane	p	=mPlanePool.mPlanes[gs.mPlaneNum];
					if(gs.mbFlipSide)
					{
						p.Inverse();
					}

					GFXPlane	gfxp	=new GFXPlane(p);

					ret.Add(gfxp);
				}
			}
			else
			{
				CoreEvents.Print("Brush index out of range!\n");
			}
			return	ret;
		}
		#endregion


		#region IO
		//this writes out something to be loaded by bspzone
		public void Write(string fileName, bool bDebug, int matCount,
			CoreDelegates.SaveVisZoneData saveVis)
		{
			FileStream	file	=new FileStream(fileName, FileMode.Create, FileAccess.Write);

			BinaryWriter	bw	=new BinaryWriter(file);

			FileUtil.WriteArray(mGFXModels, bw);
			FileUtil.WriteArray(mGFXNodes, bw);
			FileUtil.WriteArray(mGFXLeafs, bw);
			FileUtil.WriteArray(mGFXAreas, bw);
			FileUtil.WriteArray(mGFXAreaPortals, bw);

			//planes get rid of the type variable
			bw.Write(mGFXPlanes.Length);
			for(int i=0;i < mGFXPlanes.Length;i++)
			{
				mGFXPlanes[i].WriteZone(bw);
			}

			FileUtil.WriteArray(mGFXEntities, bw);
			FileUtil.WriteArray(mGFXLeafSides, bw);

			bw.Write(bDebug);
			if(bDebug)
			{
				FileUtil.WriteArray(bw, mGFXLeafFaces);	//for debuggery, not used normally

				//write debugfaces
				bw.Write(mGFXFaces.Length);
				foreach(GFXFace f in mGFXFaces)
				{
					GFXTexInfo	tex	=mGFXTexInfos[f.mTexInfo];

					f.WriteDebug(bw, tex);
				}

				FileUtil.WriteArray(bw, mGFXVerts);
				FileUtil.WriteArray(bw, mGFXVertIndexes);
			}

			saveVis(bw);
			
			bw.Write(mLightMapGridSize);
			bw.Write(((mGFXLeafs.Length + 63) & ~63) >> 3);
			bw.Write(((matCount + 63) & ~63) >> 3);

			bw.Close();
			file.Close();
		}


		public void Read(string fileName)
		{
			FileStream	file	=new FileStream(fileName,
									FileMode.Open, FileAccess.Read);

			BinaryReader	br	=new BinaryReader(file);

			mGFXModels		=FileUtil.ReadArray<GFXModel>(br);
			mGFXNodes		=FileUtil.ReadArray<GFXNode>(br);
			mGFXLeafs		=FileUtil.ReadArray<GFXLeaf>(br);
			mGFXClusters	=FileUtil.ReadArray<GFXCluster>(br);
			mGFXAreas		=FileUtil.ReadArray<GFXArea>(br);
			mGFXAreaPortals	=FileUtil.ReadArray<GFXAreaPortal>(br);
			mGFXPlanes		=FileUtil.ReadArray<GFXPlane>(br);
			mGFXEntities	=FileUtil.ReadArray<MapEntity>(br);
			mGFXLeafSides	=FileUtil.ReadArray<GFXLeafSide>(br);

			bool	bDebug	=br.ReadBoolean();
			if(bDebug)
			{
				mGFXLeafFaces	=FileUtil.ReadIntArray(br);
				mGFXFaces		=FileUtil.ReadArray<GFXFace>(br);
				mGFXVerts		=FileUtil.ReadVecArray(br);
				mGFXVertIndexes	=FileUtil.ReadIntArray(br);
			}

			mLightMapGridSize		=br.ReadInt32();

			br.Close();
			file.Close();
		}
		#endregion


		public void LoadBrushFile(string mapFileName, BSPBuildParams prms)
		{
			mEntities	=new List<MapEntity>();

			int	numSolids	=0;
			int	numDetails	=0;
			int	numTotal	=0;

			if(File.Exists(mapFileName))
			{
				using(StreamReader sr = File.OpenText(mapFileName))
				{
					string	s	="";

					string	ext	=FileUtil.GetExtension(mapFileName);

					ext	=ext.ToUpper();

					//see if this is a .map
					if(ext != "MAP")
					{
						CoreEvents.Print("Extension " + ext + " not a map file?\n");
						return;
					}

					while((s = sr.ReadLine()) != null)
					{
						s	=s.Trim();
						if(s == "{")
						{
							MapEntity	e	=new MapEntity();
							e.ReadFromMap(sr, mTIPool, mEntities.Count,	prms);
							mEntities.Add(e);

							CoreEvents.FireNumPlanesChangedEvent(mPlanePool.mPlanes.Count, null);
						}
					}
				}
			}

			InsertModelNumbers();

			//set origins
			foreach(MapEntity e in mEntities)
			{
				e.SetModelOrigin();
			}

			//move entity brushes to origins
			foreach(MapEntity e in mEntities)
			{
				e.MoveBrushesToOrigin();
			}

			//make brush faces and pool stuff
			ClipPools	cp	=new ClipPools();
			foreach(MapEntity e in mEntities)
			{
				e.MakeBrushPolys(mPlanePool, cp);
			}

			foreach(MapEntity e in mEntities)
			{
				e.CountBrushes(ref numDetails, ref numSolids, ref numTotal);
			}

			CoreEvents.Print("Brush file load complete\n");
			CoreEvents.Print("" + numSolids + " solid brushes\n");
			CoreEvents.Print("" + numDetails + " detail brushes\n");
			CoreEvents.Print("" + numTotal + " total brushes\n");
		}


		//mainly used from the 2D editor
		public void AddSingleBrush(List<GFXPlane> planes)
		{
			List<int>	planeNums	=new List<int>();
			List<bool>	sides		=new List<bool>();

			ClipPools	cp	=new ClipPools();
			foreach(GFXPlane p in planes)
			{
				int		planeNum	=0;
				bool	side;
				planeNum	=mPlanePool.FindPlane(p, out side);

				//make sure unique
				if(planeNums.Contains(planeNum))
				{
					int	ind	=planeNums.IndexOf(planeNum);

					if(side == sides[ind])
					{
						Debug.Assert(false);
						continue;
					}
				}

				planeNums.Add(planeNum);
				sides.Add(side);
			}

			MapBrush	mb	=new MapBrush(mPlanePool, planeNums, sides, cp);

			//set to solid
			mb.mContents	=Contents.BSP_CONTENTS_SOLID2;

			AddSingleBrush(mb);
		}


		public void AddSingleBrush(MapBrush mb)
		{
			//make sure entity list exists
			if(mEntities == null)
			{
				mEntities	=new List<MapEntity>();
			}

			MapEntity	me	=GetWorldSpawnEntity();
			if(me == null)
			{
				me	=new MapEntity();

				me.mData.Add("classname", "worldspawn");

				mEntities.Add(me);
			}

			me.GetBrushes().Add(mb);
		}


		bool ProcessEntities(BSPBuildParams prms, ClipPools cp)
		{
			int	index	=0;

			foreach(MapEntity me in mEntities)
			{
				if(me.GetBrushes().Count == 0)
				{
					index++;
					continue;
				}

				GBSPModel	mod	=new GBSPModel();

				Vector3	org;
				me.GetOrigin(out org);

				me.GetVectorNoConversion("ModelOrigin", out org);

				mod.SetOrigin(org);

				if(index == 0 && !prms.mbBuildAsBModel)
				{
					List<MapBrush>	brushes	=new List<MapBrush>(me.GetBrushes());
					if(!mod.ProcessWorldModel(brushes, mEntities,
						mPlanePool, mTIPool, prms.mbVerbose))
					{
						return	false;
					}
				}
				else
				{
					List<MapBrush>	brushes	=new List<MapBrush>(me.GetBrushes());
					if(!mod.ProcessSubModel(brushes, mPlanePool, mTIPool, cp))
					{
						return	false;
					}
				}
				mModels.Add(mod);
				index++;
			}
			return	true;
		}


		internal GBSPModel GetModelForLeafNode(GBSPNode Node)
		{
			if(Node.IsLeaf())
			{
				CoreEvents.Print("ModelForLeafNode:  Node not a leaf!\n");
				return	null;
			}

			int	entNum	=Node.GetOriginalEntityNum();
			if(entNum == -1)
			{
				return	null;
			}

			return	mModels[mEntities[entNum].mModelNum];
		}


		bool InsertModelNumbers()
		{
			Int32	i, NumModels	=0;

			for(i=0;i < mEntities.Count;i++)
			{
				if(mEntities[i].GetBrushCount() == 0)	//No model if no brushes
				{
					continue;
				}
				
				mEntities[i].mModelNum	=NumModels;

				mEntities[i].mData.Add("Model", "" + NumModels);

				NumModels++;
			}
			return	true;
		}


		void BuildTreeCB(object threadContext)
		{
			BSPBuildParams	bp	=threadContext as BSPBuildParams;

			mTIPool.AssignMaterials();

			ClipPools	cp	=new ClipPools();

			if(ProcessEntities(bp, cp))
			{
				CoreEvents.Print("Build GBSP Complete\n");
				CoreEvents.FireBuildDoneEvent(true, null);
			}
			else
			{
				CoreEvents.Print("Compilation failed\n");
				CoreEvents.FireBuildDoneEvent(false, null);
			}
		}


		public void BuildTree(BSPBuildParams prms)
		{
			ThreadPool.QueueUserWorkItem(BuildTreeCB, prms);
		}


		public void LoadEditorBrushFile(BinaryReader br)
		{
			//load planepool
			mPlanePool.Read(br);

			//load brushes
			int	cnt	=br.ReadInt32();
			for(int i=0;i < cnt;i++)
			{
				MapBrush	mb	=new MapBrush();
				mb.Read(br);

				AddSingleBrush(mb);
			}
		}


		public void SaveEditorBrushFile(BinaryWriter bw)
		{
			MapEntity	me	=GetWorldSpawnEntity();
			if(me == null)
			{
				CoreEvents.Print("No worldspawn entity!\n");
				return;
			}
			List<MapBrush>	brushes	=new List<MapBrush>(me.GetBrushes());
			if(brushes == null || brushes.Count == 0)
			{
				CoreEvents.Print("Worldspawn entity contains no brushes!\n");
				return;
			}
			mPlanePool.Write(bw);

			bw.Write(brushes.Count);
			foreach(MapBrush mb in brushes)
			{
				mb.Write(bw);
			}
		}


		public void SaveUpdatedEntities(string fileName)
		{
			//save entities
			string	entName	=FileUtil.StripExtension(fileName);
			entName			+=".EntData";

			FileStream		file	=new FileStream(entName, FileMode.Create, FileAccess.Write);
			BinaryWriter	bw		=new BinaryWriter(file);
			SaveGFXEntDataList(bw);

			bw.Close();
			file.Close();
		}


		public void LoadBuggeryBrushes(string path)
		{
			FileStream		fs	=new FileStream(path, FileMode.Open, FileAccess.Read);
			BinaryReader	br	=new BinaryReader(fs);

			mEntities	=new List<MapEntity>();

			//load texinfos
			mTIPool.Read(br);

			//load planepool
			mPlanePool.Read(br);

			int	numEnts	=br.ReadInt32();
			for(int i=0;i < numEnts;i++)
			{
				MapEntity	me	=new MapEntity();
				me.Read2(br);

				mEntities.Add(me);
			}

			int	numBrushes	=br.ReadInt32();
			for(int i=0;i < numBrushes;i++)
			{
				MapBrush	mb	=new MapBrush();
				mb.Read(br);

				AddSingleBrush(mb);
			}
		}


		bool FixModelTJunctions(FaceFixer ff, bool bFixTJunctions, bool bVerbose)
		{
			CoreEvents.Print(" --- Weld Model Verts --- \n");

			object	prog	=ProgressWatcher.RegisterProgress(0, mModels.Count, 0);

			for(int i=0;i < mModels.Count;i++)
			{
				if(!mModels[i].GetFaceVertIndexNumbers(ff))
				{
					return	false;
				}
				ProgressWatcher.UpdateProgressIncremental(prog);
			}

			ProgressWatcher.UpdateProgress(prog, 0);
			ProgressWatcher.DestroyProgress(prog);

			//Skip if asked to do so...
			if(!bFixTJunctions)
			{
				return	true;
			}

			CoreEvents.Print(" --- Fix Model TJunctions --- \n");

			for(int i=0;i < mModels.Count;i++)
			{
				object	prog2	=null;
				if(i == 0)
				{
					prog2	=ProgressWatcher.RegisterProgress(0, ff.IterationCount, 0);
				}

				if(!mModels[i].FixTJunctions(ff, mTIPool, prog2))
				{
					return false;
				}
			}

			ProgressWatcher.DestroyProgress(prog);

			if(bVerbose)
			{
				CoreEvents.Print(" Num TJunctions\t\t: " + ff.NumTJunctions + "\n");
				CoreEvents.Print(" Num Fixed Faces\t: " + ff.NumFixedFaces + "\n");
			}
			return true;
		}


		bool PrepAllGBSPModels(string visFile, NodeCounter nc,
			bool bVerbose, ClipPools cp)
		{
			Int32	i;

			List<GFXLeafSide>	leafSides	=new List<GFXLeafSide>();
			for(i=0;i < mModels.Count;i++)
			{
				if(!mModels[i].PrepGBSPModel(visFile, i == 0,
					false,
					mPlanePool,
					ref nc.mNumLeafClusters,
					leafSides, cp))
				{
					CoreEvents.Print("PrepAllGBSPModels:  Could not prep model " + i + "\n");
					return	false;
				}

				//create areas
				if(i == 0)
				{
					CreateAreas(mModels[i], nc);
				}
			}

			mGFXLeafSides	=leafSides.ToArray();

			return	true;
		}


		void ConvertGBSPToFileCB(object threadContext)
		{
			GBSPSaveParameters sp	=threadContext as GBSPSaveParameters;

			FileStream	file	=new FileStream(sp.mFileName,
									FileMode.Create, FileAccess.Write);

			if(file == null)
			{
				CoreEvents.Print("ConvertGBSPToFile:  geVFile_OpenNewSystem failed.\n");
				CoreEvents.FireGBSPSaveDoneEvent(false, null);
				return;
			}

			string	VisFile	=sp.mFileName;

			FaceFixer	ff	=new FaceFixer();

			if(!FixModelTJunctions(ff, sp.mBSPParams.mbFixTJunctions, sp.mBSPParams.mbVerbose))
			{
				CoreEvents.Print("ConvertGBSPToFile:  FixModelTJunctions failed.\n");
				CoreEvents.FireGBSPSaveDoneEvent(false, null);
				return;
			}

			mGFXVerts		=ff.GetWeldedVertArray();

			CoreEvents.FireNumVertsChangedEvent(mGFXVerts.Length, null);

			NodeCounter	nc	=new NodeCounter();
			ClipPools	cp	=new ClipPools();

			if(!PrepAllGBSPModels(VisFile, nc, sp.mBSPParams.mbVerbose, cp))
			{
				CoreEvents.Print("ConvertGBSPToFile:  Could not prep models.\n");
				CoreEvents.FireGBSPSaveDoneEvent(false, null);
				return;
			}

			CoreEvents.FireNumClustersChangedEvent(nc.mNumLeafClusters, null);

			mGFXVertIndexes	=nc.GetIndexArray();

			BinaryWriter	bw	=new BinaryWriter(file);

			GFXHeader	header	=new GFXHeader();

			header.mTag				=0x47425350;	//"GBSP"
			header.mbHasLight		=false;
			header.mbHasVis			=false;
			header.mbHasMaterialVis	=false;
			header.Write(bw);

			SaveGFXModelDataFromList(bw);
			SaveGFXNodes(bw, nc);
			SaveGFXLeafs(bw, nc);
			SaveEmptyGFXClusters(bw, nc);

			//set gfx area stuff from lists
			mGFXAreas		=mAreas.ToArray();
			mGFXAreaPortals	=mAreaPorts.ToArray();

			SaveGFXAreasAndPortals(bw);
			SaveGFXLeafSides(bw);
			SaveGFXFaces(bw, nc);

			mGFXPlanes	=mPlanePool.GetGFXArray();

			SaveGFXPlanes(bw);
			SaveGFXVerts(bw);
			SaveGFXVertIndexes(bw);
			mTIPool.Write(bw);

			bw.Close();
			file.Close();

			//save entities
			string	entName	=FileUtil.StripExtension(sp.mFileName);
			entName			+=".EntData";

			file	=new FileStream(entName, FileMode.Create, FileAccess.Write);
			bw		=new BinaryWriter(file);
			SaveGFXEntDataList(bw);

			bw.Close();
			file.Close();

			CoreEvents.Print(" --- Save GBSP File --- \n");
		 	
			CoreEvents.Print("Num Models\t\t: " + mModels.Count + "\n");
			CoreEvents.Print("Num Nodes\t\t: " + nc.mNumGFXNodes + "\n");
			CoreEvents.Print("Num Solid Leafs\t\t: " + nc.mNumSolidLeafs + "\n");
			CoreEvents.Print("Num Total Leafs\t\t: " + nc.mNumGFXLeafs + "\n");
			CoreEvents.Print("Num Clusters\t\t: " + nc.mNumLeafClusters + "\n");
			CoreEvents.Print("Num Areas\t\t: " + (mGFXAreas.Length - 1) + "\n");
			CoreEvents.Print("Num Area Portals\t: " + mGFXAreaPortals.Length + "\n");
			CoreEvents.Print("Num Leafs Sides\t\t: " + mGFXLeafSides.Length + "\n");
			CoreEvents.Print("Num Planes\t\t: " + mPlanePool.mPlanes.Count + "\n");
			CoreEvents.Print("Num Faces\t\t: " + nc.mNumGFXFaces + "\n");
			CoreEvents.Print("Num Leaf Faces\t\t: " + nc.mNumGFXLeafFaces + "\n");
			CoreEvents.Print("Num Vert Index\t\t: " + nc.VertIndexListCount + "\n");
			CoreEvents.Print("Num Verts\t\t: " + mGFXVerts.Length + "\n");
			CoreEvents.Print("Num FaceInfo\t\t: " + mTIPool.mTexInfos.Count + "\n");

			FreeGBSPFile();

			CoreEvents.FireGBSPSaveDoneEvent(true, null);
		}


		bool CreateAreas(GBSPModel worldModel, NodeCounter nc)
		{
			CoreEvents.Print(" --- Create Area Leafs --- \n");

			//Clear all model area info
			foreach(GBSPModel mod in mModels)
			{
				mod.mAreaFront		=mod.mAreaBack	=0;
				mod.mbAreaPortal	=false;
			}

			int	numAreas	=1;

			if(!worldModel.CreateAreas(ref numAreas, GetModelForLeafNode))
			{
				CoreEvents.Print("Could not create model areas.\n");
				return	false;
			}

			if(!worldModel.FinishAreaPortals(GetModelForLeafNode))
			{
				CoreEvents.Print("CreateAreas: FinishAreaPortals_r failed.\n");
				return	false;
			}

			if(!FinishAreas(numAreas))
			{
				CoreEvents.Print("Could not finalize model areas.\n");
				return	false;
			}

			foreach(GBSPModel mod in mModels)
			{
				mod.PrepNodes(nc);
			}

			return	true;
		}
		
		
		internal bool FinishAreas(int numAreas)
		{
			//First, go through and print out all errors pertaining to model areas
			for(int i=1;i < mModels.Count;i++)
			{
				if(!mModels[i].mbAreaPortal)
				{
					continue;
				}

				if(mModels[i].mAreaFront == 0)
				{
					CoreEvents.Print("*WARNING* FinishAreas:  AreaPortal did not touch any areas!\n");
				}
				else if(mModels[i].mAreaBack == 0)
				{
					CoreEvents.Print("*WARNING* FinishAreas:  AreaPortal only touched one area.\n");
				}
			}

			//Area 0 is the invalid area, set it here, and skip it in the loop below
			GFXArea	areaZero			=new GFXArea();
			areaZero.FirstAreaPortal	=0;
			areaZero.NumAreaPortals		=0;
			mAreas.Add(areaZero);
			
			for(int i=1;i < numAreas;i++)
			{
				GFXArea	area			=new GFXArea();
				area.FirstAreaPortal	=mAreas.Count;

				for(int m=1;m < mModels.Count;m++)
				{
					int	a0	=mModels[m].mAreaFront;
					int	a1	=mModels[m].mAreaBack;

					if(a0 == 0 || a1 == 0)
					{
						continue;
					}

					if(a0 == a1)
					{
						continue;
					}

					if(a0 != i && a1 != i)
					{
						continue;
					}

					if(mAreaPorts.Count >= GFXAreaPortal.MAX_AREA_PORTALS)
					{
						CoreEvents.Print("FinishAreas:  Max area portals.\n");
						return	false;
					}

					GFXAreaPortal	p	=new GFXAreaPortal();

					//Grab the area on the opposite side of the portal
					if(a0 == i)
					{
						p.mArea =a1;
					}
					else if(a1 == i)
					{
						p.mArea	=a0;
					}
					p.mModelNum	=m;	//Set the portals model number

					mAreaPorts.Add(p);
				}

				area.NumAreaPortals	=mAreaPorts.Count - area.FirstAreaPortal;
			}
			return	true;
		}


		public GFXLeaf[] GetGFXLeafs()
		{
			return	mGFXLeafs;
		}


		public GFXCluster[] GetGFXClusters()
		{
			return	mGFXClusters;
		}


		public Int32 FindNodeLandedIn(Int32 node, Vector3 pos)
		{
			float		dist;
			GFXNode		pNode;

			if(node < 0 || mGFXNodes == null)	// At leaf, no more recursing
			{
				return	node;
			}

			pNode	=mGFXNodes[node];
			
			//Get the distance that the eye is from this plane
			dist	=mGFXPlanes[pNode.mPlaneNum].DistanceFast(pos);

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


#if !X64
		public MapGrinder	MakeMapGrinder(GraphicsDevice gd)
		{
			return	new MapGrinder(gd, null, mGFXTexInfos, mGFXFaces, 4, 1);
		}


		public Int32	GetWorldFirstLeaf()
		{
			return	mGFXModels[0].mFirstLeaf;
		}


		public string	GetMaterialNameForLeafFace(Int32 faceIndex)
		{
			GFXFace	f	=mGFXFaces[mGFXLeafFaces[faceIndex]];

			return	GFXTexInfo.ScryTrueName(f, mGFXTexInfos[f.mTexInfo]);
		}


		public void MakeMaterials(GraphicsDevice gd, MatLib matLib, string fileName)
		{
			LoadGBSPFile(fileName);

			MapGrinder	mg	=new MapGrinder(gd, matLib, mGFXTexInfos, mGFXFaces, mLightMapGridSize, 1);
		}


		//get normals for rendering them
		public List<Vector3> GetFaceNormals()
		{
			List<Vector3>	ret	=new List<Vector3>();

			for(int i=0;i < mGFXFaces.Length;i++)
			{
				GFXFace	f	=mGFXFaces[i];

				Vector3	center	=Vector3.Zero;

				for(int j=0;j < f.mNumVerts;j++)
				{
					int	idx	=mGFXVertIndexes[f.mFirstVert + j];

					center	+=mGFXVerts[idx];
				}

				center	/=f.mNumVerts;

				ret.Add(center);

				GFXPlane	p	=mGFXPlanes[f.mPlaneNum];

				if(f.mbFlipSide)
				{
					ret.Add(center + (-p.mNormal * 5));
				}
				else
				{
					ret.Add(center + (p.mNormal * 5));
				}
			}
			return	ret;
		}


		List<Vector3> ComputeModelOrigins()
		{
			List<Vector3>	modelOrgs	=new List<Vector3>();

			//world model is at the origin
			modelOrgs.Add(Vector3.Zero);

			//grab model origins
			int	modelIndex	=1;
			foreach(MapEntity me in mGFXEntities)
			{
				if(!me.mData.ContainsKey("Model"))
				{
					continue;
				}
				if(me.mData["Model"] == "0")
				{
					continue;
				}

				int	modIdx	=0;
				
				if(!Int32.TryParse(me.mData["Model"], out modIdx))
				{
					CoreEvents.Print("Some manner of goblinry with model indexes");
					continue;
				}

				Debug.Assert(modIdx == modelIndex);

				modelIndex++;

				Vector3	org	=Vector3.Zero;

				if(!me.GetVectorNoConversion("ModelOrigin", out org))
				{
					continue;
				}

				modelOrgs.Add(org);
			}
			return	modelOrgs;
		}


		public GFXPlane[] GetPlanes()
		{
			return	mGFXPlanes;
		}


		//todo: remove when done testing
		public Color[] GetLightMapForFace(int fIdx, out int width, out int height)
		{
			GFXFace	face	=mGFXFaces[fIdx];

			if(face.mLightOfs == -1)
			{
				width	=-1;
				height	=-1;
				return	null;
			}

			Color	[]lmap	=new Color[face.mLHeight * face.mLWidth];

			int	sizeOffset	=face.mLHeight * face.mLWidth * 3;

			for(int i=0;i < lmap.Length;i++)
			{
				lmap[i].R	=mGFXLightData[face.mLightOfs + (i * 3)];
				lmap[i].G	=mGFXLightData[face.mLightOfs + (i * 3) + 1];
				lmap[i].B	=mGFXLightData[face.mLightOfs + (i * 3) + 2];
				lmap[i].A	=0xFF;
			}

			width	=face.mLWidth;
			height	=face.mLHeight;

			return	lmap;
		}


		public bool BuildLMRenderData(GraphicsDevice g,
			//lightmap stuff
			out int lmIndex,
			out Array lmVerts,
			out UInt16 []lmInds,
			out Dictionary<int, List<MeshLib.DrawCall>> lmDCs,

			//animated lightmap stuff
			out int lmAnimIndex,
			out Array lmAnimVerts,
			out UInt16 []lmAnimInds,
			out Dictionary<int, List<MeshLib.DrawCall>> lmAnimDCs,

			//lightmapped alpha stuff
			out int lmaIndex,
			out Array lmaVerts,
			out UInt16 []lmaInds,
			out Dictionary<int, List<List<MeshLib.DrawCall>>> lmaDCalls,

			//animated alpha lightmap stuff
			out int lmaAnimIndex,
			out Array lmaAnimVerts,
			out UInt16 []lmaAnimInds,
			out Dictionary<int, List<List<MeshLib.DrawCall>>> lmaAnimDCalls,

			int lightAtlasSize,
			object	pp,
			out MaterialLib.TexAtlas lightAtlas)
		{
			MapGrinder	mg	=new MapGrinder(g, null, mGFXTexInfos, mGFXFaces, mLightMapGridSize, lightAtlasSize);

			if(!mg.BuildLMFaceData(mGFXVerts, mGFXVertIndexes, mGFXLightData, pp, mGFXModels))
			{
				lmDCs		=null;	lmAnimDCs		=null;	lmaDCalls	=null;
				lightAtlas	=null;	lmaAnimDCalls	=null;
				lmVerts	=lmAnimVerts	=lmaVerts	=lmaAnimVerts	=null;
				lmInds	=lmAnimInds		=lmaInds	=lmaAnimInds	=null;
				lmIndex	=lmAnimIndex	=lmaIndex	=lmaAnimIndex	=-1;
				return	false;
			}
			mg.GetLMGeometry(out lmIndex, out lmVerts, out lmInds);

			if(!mg.BuildLMAnimFaceData(mGFXVerts, mGFXVertIndexes, mGFXLightData, pp, mGFXModels))
			{
				lmDCs		=null;	lmAnimDCs		=null;	lmaDCalls	=null;
				lightAtlas	=null;	lmaAnimDCalls	=null;
				lmVerts	=lmAnimVerts	=lmaVerts	=lmaAnimVerts	=null;
				lmInds	=lmAnimInds		=lmaInds	=lmaAnimInds	=null;
				lmIndex	=lmAnimIndex	=lmaIndex	=lmaAnimIndex	=-1;
				return	false;
			}
			mg.GetLMAnimGeometry(out lmAnimIndex, out lmAnimVerts, out lmAnimInds);

			if(!mg.BuildLMAFaceData(mGFXVerts, mGFXVertIndexes, mGFXLightData, pp, mGFXModels))
			{
				lmDCs		=null;	lmAnimDCs		=null;	lmaDCalls	=null;
				lightAtlas	=null;	lmaAnimDCalls	=null;
				lmVerts	=lmAnimVerts	=lmaVerts	=lmaAnimVerts	=null;
				lmInds	=lmAnimInds		=lmaInds	=lmaAnimInds	=null;
				lmIndex	=lmAnimIndex	=lmaIndex	=lmaAnimIndex	=-1;
				return	false;
			}
			mg.GetLMAGeometry(out lmaIndex, out lmaVerts, out lmaInds);

			if(!mg.BuildLMAAnimFaceData(mGFXVerts, mGFXVertIndexes, mGFXLightData, pp, mGFXModels))
			{
				lmDCs		=null;	lmAnimDCs		=null;	lmaDCalls	=null;
				lightAtlas	=null;	lmaAnimDCalls	=null;
				lmVerts	=lmAnimVerts	=lmaVerts	=lmaAnimVerts	=null;
				lmInds	=lmAnimInds		=lmaInds	=lmaAnimInds	=null;
				lmIndex	=lmAnimIndex	=lmaIndex	=lmaAnimIndex	=-1;
				return	false;
			}
			mg.GetLMAAnimGeometry(out lmaAnimIndex, out lmaAnimVerts, out lmaAnimInds);

			lightAtlas	=mg.GetLightMapAtlas();

			mg.GetLMMaterialData(out lmDCs);
			mg.GetLMAnimMaterialData(out lmAnimDCs);
			mg.GetLMAMaterialData(out lmaDCalls);
			mg.GetLMAAnimMaterialData(out lmaAnimDCalls);

			mg.FreeAll();

			return	true;
		}


		public void BuildVLitRenderData(GraphicsDevice g,
			out int index, out Array verts, out UInt16 []inds,
			out Dictionary<int, List<MeshLib.DrawCall>> dcs, object pp)
		{
			MapGrinder	mg	=new MapGrinder(g, null, mGFXTexInfos, mGFXFaces, mLightMapGridSize, 1);

			Vector3	[]vnorms	=MakeSmoothVertNormals();

			mg.BuildVLitFaceData(mGFXVerts, mGFXVertIndexes, mGFXRGBVerts, vnorms, pp, mGFXModels);

			mg.GetVLitGeometry(out index, out verts, out inds);

			mg.GetVLitMaterialData(out dcs);

			mg.FreeAll();
		}


		public void BuildAlphaRenderData(GraphicsDevice g,
			out int index, out Array verts, out UInt16 []inds,
			out Dictionary<int, List<List<MeshLib.DrawCall>>> alphaDrawCalls,
			object pp)
		{
			MapGrinder	mg	=new MapGrinder(g, null, mGFXTexInfos, mGFXFaces, mLightMapGridSize, 1);

			Vector3	[]vnorms	=MakeSmoothVertNormals();

			mg.BuildAlphaFaceData(mGFXVerts, mGFXVertIndexes, mGFXRGBVerts, vnorms, pp, mGFXModels);

			mg.GetAlphaGeometry(out index, out verts, out inds);

			mg.GetAlphaMaterialData(out alphaDrawCalls);

			mg.FreeAll();
		}


		public void BuildFullBrightRenderData(GraphicsDevice g,
			out int index, out Array verts, out UInt16 []inds,
			out Dictionary<int, List<MeshLib.DrawCall>> dcs, object pp)
		{
			MapGrinder	mg	=new MapGrinder(g, null, mGFXTexInfos, mGFXFaces, mLightMapGridSize, 1);

			Vector3	[]vnorms	=MakeSmoothVertNormals();

			mg.BuildFullBrightFaceData(mGFXVerts, mGFXVertIndexes, pp, mGFXModels);

			mg.GetFullBrightGeometry(out index, out verts, out inds);

			mg.GetFullBrightMaterialData(out dcs);

			mg.FreeAll();
		}


		public void BuildMirrorRenderData(GraphicsDevice g,
			out int index, out Array verts, out UInt16 []inds,
			out Dictionary<int, List<MeshLib.DrawCall>> dcs,
			out List<List<Vector3>> mirrorPolys, object pp)
		{
			MapGrinder	mg	=new MapGrinder(g, null, mGFXTexInfos, mGFXFaces, mLightMapGridSize, 1);

			Vector3	[]vnorms	=MakeSmoothVertNormals();

			mg.BuildMirrorFaceData(mGFXVerts, mGFXVertIndexes, mGFXRGBVerts, vnorms, pp, mGFXModels);

			mg.GetMirrorGeometry(out index, out verts, out inds);

			mg.GetMirrorMaterialData(out dcs, out mirrorPolys);

			mg.FreeAll();
		}


		public void BuildSkyRenderData(GraphicsDevice g,
			out int index, out Array verts, out UInt16 []inds,
			out Dictionary<int, List<MeshLib.DrawCall>> dcs, object pp)
		{
			MapGrinder	mg	=new MapGrinder(g, null, mGFXTexInfos, mGFXFaces, mLightMapGridSize, 1);

			mg.BuildSkyFaceData(mGFXVerts, mGFXVertIndexes, pp, mGFXModels);

			mg.GetSkyGeometry(out index, out verts, out inds);

			mg.GetSkyMaterialData(out dcs);

			mg.FreeAll();
		}
#endif
	}
}
