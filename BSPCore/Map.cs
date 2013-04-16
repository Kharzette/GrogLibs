using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using System.Diagnostics;
#if !X64
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
#endif


namespace BSPCore
{
	public partial class Map
	{
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

			if(Fd >= -1 && Bd >= -1)
			{
				return	RayIntersectFace(Front, Back, n.mFront,
							ref intersectionPoint, ref hitLeaf, ref hit);
			}
			if(Fd < 1 && Bd < 1)
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
							//figure out which face was hit
							for(int i=0;i < n.mNumFaces;i++)
							{
								GFXFace		f	=mGFXFaces[i + n.mFirstFace];
								GFXPlane	pl	=mGFXPlanes[f.mPlaneNum];

								float	dist	=pl.DistanceFast(intersectionPoint);
								if(dist > -0.001f && dist < 0.001f)
								{
									hit	=f;
									break;
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

				Vector3	frontInv	=Vector3.Transform(Front, modelInv);
				Vector3	backInv		=Vector3.Transform(Back, modelInv);

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

				Vector3	frontInv	=Vector3.Transform(Front, modelInv);
				Vector3	backInv		=Vector3.Transform(Back, modelInv);

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
				Matrix	mat	=Matrix.CreateTranslation(mGFXModels[i].mOrigin);

				ret.Add(i, mat);
			}
			return	ret;
		}


		//pathing on a single flat face
		void AddPathPoints(List<Vector3> verts,
			List<Vector3> pathPoints, GFXPlane p, GFXTexInfo tex)
		{
			//make some edges
			List<Vector3>	edges	=new List<Vector3>();
			for(int i=0;i < verts.Count;i++)
			{
				Vector3	edge	=verts[(i + 1) % verts.Count] - verts[i];
				edge.Normalize();

				edges.Add(edge);
			}

			//make edge planes
			List<GBSPPlane>	edgePlanes	=new List<GBSPPlane>();
			for(int i=0;i < verts.Count;i++)
			{
				GBSPPlane	edgePlane	=new GBSPPlane();
				edgePlane.mNormal		=Vector3.Cross(p.mNormal, edges[i]);
				edgePlane.mDist			=Vector3.Dot(verts[i], edgePlane.mNormal);

				edgePlanes.Add(edgePlane);
			}

			//get bounds
			Bounds	bnd	=new Bounds();
			foreach(Vector3 vert in verts)
			{
				bnd.AddPointToBounds(vert);
			}

			Vector3	uvec	=tex.mVecU;
			Vector3	vvec	=tex.mVecV;

			uvec.Normalize();
			vvec.Normalize();

			float	gridSizeU	=Vector3.Dot(uvec, bnd.mMaxs);
			float	gridSizeV	=Vector3.Dot(vvec, bnd.mMaxs);

			gridSizeU	-=Vector3.Dot(uvec, bnd.mMins);
			gridSizeV	-=Vector3.Dot(vvec, bnd.mMins);

			int	nodeDensity	=12;

			gridSizeU	/=nodeDensity;
			gridSizeV	/=nodeDensity;

			uvec	*=nodeDensity;
			vvec	*=nodeDensity;

			if(gridSizeU < 0.0f)
			{
				gridSizeU	=-gridSizeU;
				uvec		=-uvec;
			}
			if(gridSizeV < 0.0f)
			{
				gridSizeV	=-gridSizeV;
				vvec		=-vvec;
			}

			Debug.Assert(gridSizeU > 0.0f && gridSizeV > 0.0f);

			//make a grid of lights through the bounds
			for(int v=0;v < gridSizeV;v++)
			{
				for(int u=0;u < gridSizeU;u++)
				{
					Vector3	org	=bnd.mMins + (uvec * u) + (vvec * v);

					//bump out a tad
					org	+=p.mNormal;

					bool	bOutside	=false;
					foreach(GBSPPlane ep in edgePlanes)
					{
						if(ep.Distance(org) > -5.0f)
						{
							bOutside	=true;
							break;	//outside the poly
						}
					}
					if(bOutside)
					{
						continue;
					}

					pathPoints.Add(org);
				}
			}
		}


		//pathing nodes
		public List<Vector3> GetGoodPathPoints()
		{
			List<Vector3>	ret	=new List<Vector3>();
			if(mGFXFaces == null)
			{
				return	ret;	//no debug info saved
			}

			List<Vector3>	verts	=new List<Vector3>();

			foreach(GFXLeaf gl in mGFXLeafs)
			{
				for(int i=0;i < gl.mNumFaces;i++)
				{
					int		face	=mGFXLeafFaces[gl.mFirstFace + i];

					GFXFace	f	=mGFXFaces[face];

					int		nverts	=f.mNumVerts;
					int		fvert	=f.mFirstVert;

					//grab the face plane
					int	pNum	=f.mPlaneNum;

					GFXPlane	p	=mGFXPlanes[pNum];

					if(!IsGround(p))
					{
						continue;
					}

					verts.Clear();
					for(int j=fvert;j < (fvert + nverts);j++)
					{
						int	idx	=mGFXVertIndexes[j];
						verts.Add(mGFXVerts[idx]);
					}
					
					AddPathPoints(verts, ret, p, mGFXTexInfos[f.mTexInfo]);
				}
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


		public void GetTriangles(Vector3 pos, List<Vector3> verts, List<UInt32> indexes, string drawChoice)
		{
			if(drawChoice == "Map Brushes")
			{
				if(mEntities != null)
				{
					foreach(MapEntity ent in mEntities)
					{
						ent.GetTriangles(verts, indexes, false);
					}
				}
				else
				{
					CoreEvents.Print("This is intended for use pre build.\n");
				}
			}
			else if(drawChoice == "Collision Brushes")
			{
				if(mEntities != null)
				{
					foreach(MapEntity ent in mEntities)
					{
						ent.GetTriangles(verts, indexes, true);
					}
				}
				else
				{
					CoreEvents.Print("This is intended for use pre build.\n");
				}
			}
				/*
			else if(drawChoice == "Vis Tree")
			{
				if(mGFXModels != null && mGFXModels.Length > 0)
				{
					int	root	=mGFXModels[0].mRootNode[0];

					VisWorld(root, pos);

					RenderBSPFrontBack_r2(root, pos, verts, indexes, true);

					for(int i=1;i < mGFXModels.Length;i++)
					{
						RenderModelBSPFrontBack_r2(mGFXModels[i].mRootNode[0], pos, verts, indexes);
					}
				}
				else
				{
					CoreEvents.Print("No GFXModels to draw!\n");
				}
			}*/
		}


		public bool ClassifyPoint(Vector3 pnt)
		{
			return	false;
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


		public void GetWorldBrushTrianglesByIndex(int brushIndex, List<Vector3> verts, List<UInt32> indexes)
		{
			MapEntity	me	=GetWorldSpawnEntity();
			if(me == null)
			{
				return;
			}
			if(me.GetBrushes().Count > brushIndex)
			{
				me.GetBrushes()[brushIndex].GetTriangles(verts, indexes, false);
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

			UtilityLib.FileUtil.WriteArray(mGFXModels, bw);
			UtilityLib.FileUtil.WriteArray(mGFXNodes, bw);
			UtilityLib.FileUtil.WriteArray(mGFXLeafs, bw);
			UtilityLib.FileUtil.WriteArray(mGFXAreas, bw);
			UtilityLib.FileUtil.WriteArray(mGFXAreaPortals, bw);
			UtilityLib.FileUtil.WriteArray(mGFXPlanes, bw);
			UtilityLib.FileUtil.WriteArray(mGFXEntities, bw);
			UtilityLib.FileUtil.WriteArray(mGFXLeafSides, bw);

			bw.Write(bDebug);
			if(bDebug)
			{
				UtilityLib.FileUtil.WriteArray(bw, mGFXLeafFaces);	//for debuggery, not used normally
				UtilityLib.FileUtil.WriteArray(mGFXFaces, bw);		//for debuggery
				UtilityLib.FileUtil.WriteArray(bw, mGFXVerts);
				UtilityLib.FileUtil.WriteArray(bw, mGFXVertIndexes);
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

			mGFXModels		=UtilityLib.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return UtilityLib.FileUtil.InitArray<GFXModel>(count); }) as GFXModel[];
			mGFXNodes		=UtilityLib.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return UtilityLib.FileUtil.InitArray<GFXNode>(count); }) as GFXNode[];
			mGFXLeafs		=UtilityLib.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return UtilityLib.FileUtil.InitArray<GFXLeaf>(count); }) as GFXLeaf[];
			mGFXClusters	=UtilityLib.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return UtilityLib.FileUtil.InitArray<GFXCluster>(count); }) as GFXCluster[];
			mGFXAreas		=UtilityLib.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return UtilityLib.FileUtil.InitArray<GFXArea>(count); }) as GFXArea[];
			mGFXAreaPortals	=UtilityLib.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return UtilityLib.FileUtil.InitArray<GFXAreaPortal>(count); }) as GFXAreaPortal[];
			mGFXPlanes		=UtilityLib.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return UtilityLib.FileUtil.InitArray<GFXPlane>(count); }) as GFXPlane[];
			mGFXEntities	=UtilityLib.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return UtilityLib.FileUtil.InitArray<MapEntity>(count); }) as MapEntity[];
			mGFXLeafSides	=UtilityLib.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return UtilityLib.FileUtil.InitArray<GFXLeafSide>(count); }) as GFXLeafSide[];

			bool	bDebug	=br.ReadBoolean();
			if(bDebug)
			{
				mGFXLeafFaces	=UtilityLib.FileUtil.ReadIntArray(br);
				mGFXFaces		=UtilityLib.FileUtil.ReadArray(br, delegate(Int32 count)
								{ return UtilityLib.FileUtil.InitArray<GFXFace>(count); }) as GFXFace[];
				mGFXVerts		=UtilityLib.FileUtil.ReadVecArray(br);
				mGFXVertIndexes	=UtilityLib.FileUtil.ReadIntArray(br);
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

					string	ext	=UtilityLib.FileUtil.GetExtension(mapFileName);

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


		//This is for 2D maps.
		//Creates a box around the extents
		//only builds world brushes (no models)
		/*
		public void BuildTree2D(BSPBuildParams prms,
			EntityLib.EntitySystem es,
			List<EntityLib.Entity> ents)
		{
			mEntities.RemoveRange(1, mEntities.Count - 1);

			foreach(EntityLib.Entity e in ents)
			{
				//extract location
				List<EntityLib.Component>	comps	=
					es.GetComponents(e, typeof(EntityLib.Components.Physical.Position3D));

				if(comps == null || comps.Count == 0)
				{
					continue;
				}

				EntityLib.Components.Physical.Position3D	pos
					=comps[0] as EntityLib.Components.Physical.Position3D;

				if(pos == null)
				{
					continue;
				}

				MapEntity	me	=new MapEntity();

				//negate x and swap Y and Z
				me.mData.Add("origin", "" + -pos.Position.X
					+ " " + pos.Position.Z + " " + pos.Position.Y);

				mEntities.Add(me);
			}

			MapEntity	wse	=GetWorldSpawnEntity();
			if(wse == null)
			{
				Print("No world spawn entity!");
			}

			if(wse.GetBrushes().Count <= 0)
			{
				Print("No brushes!");
			}

			Bounds	bnd	=new Bounds();
			foreach(MapBrush mb in wse.GetBrushes())
			{
				bnd.Merge(null, mb.GetBounds());
			}

			//create 6 sides around the extents
			Bounds			sideBounds		=new Bounds(bnd);
			float			hullThickness	=16.0f;
			List<MapBrush>	sideBrushes		=new List<MapBrush>();

			//Y pos, expand the y plane xz to overlap also
			sideBounds.mMins.Y	=bnd.mMaxs.Y;
			sideBounds.mMaxs.Y	=bnd.mMaxs.Y + hullThickness;
			sideBounds.mMins.X	-=hullThickness;
			sideBounds.mMaxs.X	+=hullThickness;
			sideBounds.mMins.Z	-=hullThickness;
			sideBounds.mMaxs.Z	+=hullThickness;
			sideBrushes.Add(new MapBrush(sideBounds, mPlanePool));

			//Y neg
			sideBounds.mMaxs.Y	=bnd.mMins.Y;
			sideBounds.mMins.Y	=bnd.mMins.Y - hullThickness;
			sideBrushes.Add(new MapBrush(sideBounds, mPlanePool));

			//reset
			sideBounds.mMins	=bnd.mMins;
			sideBounds.mMaxs	=bnd.mMaxs;

			//X pos, expand z for overlap
			sideBounds.mMins.X	=bnd.mMaxs.X;
			sideBounds.mMaxs.X	=bnd.mMaxs.X + hullThickness;
			sideBounds.mMins.Z	-=hullThickness;
			sideBounds.mMaxs.Z	+=hullThickness;
			sideBrushes.Add(new MapBrush(sideBounds, mPlanePool));

			//X neg
			sideBounds.mMaxs.X	=bnd.mMins.X;
			sideBounds.mMins.X	=bnd.mMins.X - hullThickness;
			sideBrushes.Add(new MapBrush(sideBounds, mPlanePool));

			//reset
			sideBounds.mMins	=bnd.mMins;
			sideBounds.mMaxs	=bnd.mMaxs;

			//Z pos, expand x for overlap
			sideBounds.mMins.Z	=bnd.mMaxs.Z;
			sideBounds.mMaxs.Z	=bnd.mMaxs.Z + hullThickness;
			sideBounds.mMins.X	-=hullThickness;
			sideBounds.mMaxs.X	+=hullThickness;
			sideBrushes.Add(new MapBrush(sideBounds, mPlanePool));

			//Z neg
			sideBounds.mMaxs.Z	=bnd.mMins.Z;
			sideBounds.mMins.Z	=bnd.mMins.Z - hullThickness;
			sideBrushes.Add(new MapBrush(sideBounds, mPlanePool));

			//reset
			sideBounds.mMins	=bnd.mMins;
			sideBounds.mMaxs	=bnd.mMaxs;

			//add in the encircling brushery
			foreach(MapBrush sideBrush in sideBrushes)
			{
				wse.GetBrushes().Add(sideBrush);
			}

			//add a bogus texinfo to the brushes
			foreach(MapBrush mb in wse.GetBrushes())
			{
				foreach(GBSPSide s in mb.mOriginalSides)
				{
					TexInfo	ti		=new TexInfo();
					ti.mMaterial	="Bogus";
					ti.mTexture		="Bogus";
					ti.mFlags		|=TexInfo.NO_LIGHTMAP;
					ti.mFlags		|=TexInfo.FULLBRIGHT;
					ti.mUVec		=Vector3.UnitX;
					ti.mVVec		=Vector3.UnitY;
					s.mTexInfo		=mTIPool.Add(ti);
				}
				mb.mContents	=Contents.BSP_CONTENTS_SOLID2;
			}*/

/*				
			FileStream		fs	=new FileStream("buggery.MapBrushes", FileMode.Create, FileAccess.Write);
			BinaryWriter	bw	=new BinaryWriter(fs);


			//write texinfos
			mTIPool.Write(bw);

			//write planes
			mPlanePool.Write(bw);

			//write entities
			bw.Write(mEntities.Count);
			foreach(MapEntity e in mEntities)
			{
				e.Write(bw);
			}

			//write brushes
			bw.Write(me.GetBrushes().Count);
			foreach(MapBrush mb in wse.GetBrushes())
			{
				mb.Write(bw);
			}

			bw.Close();
			fs.Close();*/

				//remove encircling
//				foreach(MapBrush sideBrush in sideBrushes)
//				{
//					me.GetBrushes().Remove(sideBrush);
//				}

//			ThreadPool.QueueUserWorkItem(BuildTreeCB, prms);
//		}


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
			string	entName	=UtilityLib.FileUtil.StripExtension(fileName);
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
			string	entName	=UtilityLib.FileUtil.StripExtension(sp.mFileName);
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
		public MapGrinder	MakeMapGrinder()
		{
			return	new MapGrinder(null, mGFXTexInfos, mGFXFaces, 4, 1);
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


		public List<MaterialLib.Material> GetMaterials()
		{
			MapGrinder	mg	=new MapGrinder(null, mGFXTexInfos, mGFXFaces, mLightMapGridSize, 1);

			return	mg.GetMaterials();
		}


		//an intermediate step to generate a set of materials
		//so the user can set up emissives for radiosity
		public List<MaterialLib.Material> GenerateMaterials(string fileName)
		{
			LoadGBSPFile(fileName);

			List<MaterialLib.Material>	ret	=GetMaterials();

			FreeGBSPFile();

			return	ret;
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


		//todo: remove when done testing
		public GFXPlane []GetPlanes()
		{
			return	mGFXPlanes;
		}


		public bool BuildLMRenderData(GraphicsDevice g,
			//lightmap stuff
			out VertexBuffer lmVB,
			out IndexBuffer lmIB,
			out Dictionary<int, List<MeshLib.DrawCall>> lmDCs,

			//animated lightmap stuff
			out VertexBuffer lmAnimVB,
			out IndexBuffer lmAnimIB,
			out Dictionary<int, List<MeshLib.DrawCall>> lmAnimDCs,

			//lightmapped alpha stuff
			out VertexBuffer lmaVB,
			out IndexBuffer lmaIB,
			out Dictionary<int, List<List<MeshLib.DrawCall>>> lmaDCalls,

			//animated alpha lightmap stuff
			out VertexBuffer lmaAnimVB,
			out IndexBuffer lmaAnimIB,
			out Dictionary<int, List<List<MeshLib.DrawCall>>> lmaAnimDCalls,

			int lightAtlasSize,
			object	pp,
			out MaterialLib.TexAtlas lightAtlas)
		{
			MapGrinder	mg	=new MapGrinder(g, mGFXTexInfos, mGFXFaces, mLightMapGridSize, lightAtlasSize);

			if(!mg.BuildLMFaceData(mGFXVerts, mGFXVertIndexes, mGFXLightData, pp, mGFXModels))
			{
				lmVB	=null;	lmIB	=null;	lmDCs	=null;
				lmAnimVB	=null;	lmAnimDCs	=null;
				lmAnimIB	=null;	lmAnimVB	=null;
				lmaVB	=null;	lmaIB	=null;	lmaAnimVB	=null;
				lmaAnimIB	=null;	lmaAnimVB	=null;	lmaDCalls	=null;
				lightAtlas	=null;	lmaAnimDCalls	=null;
				return	false;
			}
			mg.GetLMBuffers(out lmVB, out lmIB);

			if(!mg.BuildLMAnimFaceData(mGFXVerts, mGFXVertIndexes, mGFXLightData, pp, mGFXModels))
			{
				lmVB	=null;	lmIB	=null;	lmDCs	=null;
				lmAnimVB	=null;	lmAnimDCs	=null;
				lmAnimIB	=null;	lmAnimVB	=null;
				lmaVB	=null;	lmaIB	=null;	lmaAnimVB	=null;
				lmaAnimIB	=null;	lmaAnimVB	=null;	lmaDCalls	=null;
				lightAtlas	=null;	lmaAnimDCalls	=null;
				return	false;
			}
			mg.GetLMAnimBuffers(out lmAnimVB, out lmAnimIB);

			if(!mg.BuildLMAFaceData(mGFXVerts, mGFXVertIndexes, mGFXLightData, pp, mGFXModels))
			{
				lmVB	=null;	lmIB	=null;	lmDCs	=null;
				lmAnimVB	=null;	lmAnimDCs	=null;
				lmAnimIB	=null;	lmAnimVB	=null;
				lmaVB	=null;	lmaIB	=null;	lmaAnimVB	=null;
				lmaAnimIB	=null;	lmaAnimVB	=null;	lmaDCalls	=null;
				lightAtlas	=null;	lmaAnimDCalls	=null;
				return	false;
			}
			mg.GetLMABuffers(out lmaVB, out lmaIB);

			if(!mg.BuildLMAAnimFaceData(mGFXVerts, mGFXVertIndexes, mGFXLightData, pp, mGFXModels))
			{
				lmVB	=null;	lmIB	=null;	lmDCs	=null;
				lmAnimVB	=null;	lmAnimDCs	=null;
				lmAnimIB	=null;	lmAnimVB	=null;
				lmaVB	=null;	lmaIB	=null;	lmaAnimVB	=null;
				lmaAnimIB	=null;	lmaAnimVB	=null;	lmaDCalls	=null;
				lightAtlas	=null;	lmaAnimDCalls	=null;
				return	false;
			}
			mg.GetLMAAnimBuffers(out lmaAnimVB, out lmaAnimIB);

			lightAtlas	=mg.GetLightMapAtlas();

			mg.GetLMMaterialData(out lmDCs);
			mg.GetLMAnimMaterialData(out lmAnimDCs);
			mg.GetLMAMaterialData(out lmaDCalls);
			mg.GetLMAAnimMaterialData(out lmaAnimDCalls);

			return	true;
		}


		public void BuildVLitRenderData(GraphicsDevice g, out VertexBuffer vb,
			out IndexBuffer ib,
			out Dictionary<int, List<MeshLib.DrawCall>> dcs, object pp)
		{
			MapGrinder	mg	=new MapGrinder(g, mGFXTexInfos, mGFXFaces, mLightMapGridSize, 1);

			Vector3	[]vnorms	=MakeSmoothVertNormals();

			mg.BuildVLitFaceData(mGFXVerts, mGFXVertIndexes, mGFXRGBVerts, vnorms, pp, mGFXModels);

			mg.GetVLitBuffers(out vb, out ib);

			mg.GetVLitMaterialData(out dcs);
		}


		public void BuildAlphaRenderData(GraphicsDevice g, out VertexBuffer vb,
			out IndexBuffer ib,	out Dictionary<int, List<List<MeshLib.DrawCall>>> alphaDrawCalls, object pp)
		{
			MapGrinder	mg	=new MapGrinder(g, mGFXTexInfos, mGFXFaces, mLightMapGridSize, 1);

			Vector3	[]vnorms	=MakeSmoothVertNormals();

			mg.BuildAlphaFaceData(mGFXVerts, mGFXVertIndexes, mGFXRGBVerts, vnorms, pp, mGFXModels);

			mg.GetAlphaBuffers(out vb, out ib);

			mg.GetAlphaMaterialData(out alphaDrawCalls);
		}


		public void BuildFullBrightRenderData(GraphicsDevice g, out VertexBuffer vb,
			out IndexBuffer ib,
			out Dictionary<int, List<MeshLib.DrawCall>> dcs, object pp)
		{
			MapGrinder	mg	=new MapGrinder(g, mGFXTexInfos, mGFXFaces, mLightMapGridSize, 1);

			Vector3	[]vnorms	=MakeSmoothVertNormals();

			mg.BuildFullBrightFaceData(mGFXVerts, mGFXVertIndexes, pp, mGFXModels);

			mg.GetFullBrightBuffers(out vb, out ib);

			mg.GetFullBrightMaterialData(out dcs);
		}


		public void BuildMirrorRenderData(GraphicsDevice g, out VertexBuffer vb,
			out IndexBuffer ib, out Dictionary<int, List<MeshLib.DrawCall>> dcs,
			out List<List<Vector3>> mirrorPolys, object pp)
		{
			MapGrinder	mg	=new MapGrinder(g, mGFXTexInfos, mGFXFaces, mLightMapGridSize, 1);

			Vector3	[]vnorms	=MakeSmoothVertNormals();

			mg.BuildMirrorFaceData(mGFXVerts, mGFXVertIndexes, mGFXRGBVerts, vnorms, pp, mGFXModels);

			mg.GetMirrorBuffers(out vb, out ib);

			mg.GetMirrorMaterialData(out dcs, out mirrorPolys);
		}


		public void BuildSkyRenderData(GraphicsDevice g, out VertexBuffer vb,
			out IndexBuffer ib,
			out Dictionary<int, List<MeshLib.DrawCall>> dcs, object pp)
		{
			MapGrinder	mg	=new MapGrinder(g, mGFXTexInfos, mGFXFaces, mLightMapGridSize, 1);

			mg.BuildSkyFaceData(mGFXVerts, mGFXVertIndexes, pp, mGFXModels);

			mg.GetSkyBuffers(out vb, out ib);

			mg.GetSkyMaterialData(out dcs);
		}
#endif
	}
}
