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
using Microsoft.Xna.Framework.Storage;
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


		public Map() { }


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
				return(RayIntersect(Front, Back, n.mChildren[0], ref intersectionPoint, ref hitLeaf));
			}
			if(Fd < 1 && Bd < 1)
			{
				return(RayIntersect(Front, Back, n.mChildren[1], ref intersectionPoint, ref hitLeaf));
			}

			Side	=(Fd < 0)? 1 : 0;
			Dist	=Fd / (Fd - Bd);

			I	=Front + Dist * (Back - Front);

			//Work our way to the front, from the back side.  As soon as there
			//is no more collisions, we can assume that we have the front portion of the
			//ray that is in empty space.  Once we find this, and see that the back half is in
			//solid space, then we found the front intersection point...
			if(RayIntersect(Front, I, n.mChildren[Side], ref intersectionPoint, ref hitLeaf))
			{
				return	true;
			}
			else if(RayIntersect(I, Back, n.mChildren[(Side == 0)? 1 : 0], ref intersectionPoint, ref hitLeaf))
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


		bool RayCollide(Vector3 Front, Vector3 Back, ref Vector3 I)
		{
			bool	hitLeaf	=false;
			if(RayIntersect(Front, Back, mGFXModels[0].mRootNode, ref I, ref hitLeaf))
			{
				return	true;
			}
			return	false;
		}


		#region Queries
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
					if(gs.mPlaneSide != 0)
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
		public void Write(string fileName, int matCount, CoreDelegates.SaveVisZoneData saveVis)
		{
			FileStream	file	=new FileStream(fileName,
									FileMode.OpenOrCreate, FileAccess.Write);

			BinaryWriter	bw	=new BinaryWriter(file);

			UtilityLib.FileUtil.WriteArray(mGFXModels, bw);
			UtilityLib.FileUtil.WriteArray(mGFXNodes, bw);
			UtilityLib.FileUtil.WriteArray(mGFXLeafs, bw);
			UtilityLib.FileUtil.WriteArray(mGFXAreas, bw);
			UtilityLib.FileUtil.WriteArray(mGFXAreaPortals, bw);
			UtilityLib.FileUtil.WriteArray(mGFXPlanes, bw);
			UtilityLib.FileUtil.WriteArray(mGFXEntities, bw);
			UtilityLib.FileUtil.WriteArray(mGFXLeafSides, bw);

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

			mLightMapGridSize		=br.ReadInt32();

			br.Close();
			file.Close();
		}
		#endregion


		public void LoadBrushFile(string mapFileName)
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

					//see if this is a .map or a .vmf
					if(mapFileName.EndsWith(".map") || mapFileName.EndsWith(".MAP"))
					{
						while((s = sr.ReadLine()) != null)
						{
							s	=s.Trim();
							if(s == "{")
							{
								MapEntity	e	=new MapEntity();
								e.ReadFromMap(sr, mPlanePool, mTIPool, mEntities.Count);
								mEntities.Add(e);

								CoreEvents.FireNumPlanesChangedEvent(mPlanePool.mPlanes.Count, null);
							}
						}
					}
					else
					{
						while((s = sr.ReadLine()) != null)
						{
							s	=s.Trim();
							if(s == "entity")
							{
								MapEntity	e	=new MapEntity();
								e.ReadVMFEntBlock(sr, mEntities.Count, mPlanePool, mTIPool);
								mEntities.Add(e);
								CoreEvents.FireNumPlanesChangedEvent(mPlanePool.mPlanes.Count, null);
							}
							else if(s == "world")
							{
								MapEntity	e	=new MapEntity();
								e.ReadVMFWorldBlock(sr, mEntities.Count, mPlanePool, mTIPool);
								mEntities.Add(e);
								CoreEvents.FireNumPlanesChangedEvent(mPlanePool.mPlanes.Count, null);
							}
							else if(s == "cameras")
							{
								MapEntity.SkipVMFEditorBlock(sr);
							}
							else if(s == "cordon")
							{
								MapEntity.SkipVMFEditorBlock(sr);
							}
						}
					}
				}
			}

			foreach(MapEntity e in mEntities)
			{
				e.CountBrushes(ref numDetails, ref numSolids, ref numTotal);
			}

			InsertModelNumbers();

			CoreEvents.Print("Brush file load complete\n");
			CoreEvents.Print("" + numSolids + " solid brushes\n");
			CoreEvents.Print("" + numDetails + " detail brushes\n");
			CoreEvents.Print("" + numTotal + " total brushes\n");
		}


		//mainly used from the 2D editor
		public void AddSingleBrush(List<GFXPlane> planes)
		{
			List<int>	planeNums	=new List<int>();
			List<sbyte>	sides		=new List<sbyte>();

			foreach(GFXPlane p in planes)
			{
				int		planeNum	=0;
				sbyte	side		=0;
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

			MapBrush	mb	=new MapBrush(mPlanePool, planeNums, sides);

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


		bool ProcessEntities(bool bVerbose, bool bEntityVerbose)
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

				mod.SetOrigin(org);

				if(index == 0)
				{
					List<MapBrush>	brushes	=new List<MapBrush>(me.GetBrushes());
					if(!mod.ProcessWorldModel(brushes, mEntities,
						mPlanePool, mTIPool, bVerbose))
					{
						return	false;
					}
				}
				else
				{
					List<MapBrush>	brushes	=new List<MapBrush>(me.GetBrushes());
					if(!mod.ProcessSubModel(brushes, mPlanePool,
						mTIPool, bEntityVerbose))
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

				if(i != 0)
				{
					mEntities[i].mData.Add("Model", "" + NumModels);
				}
				NumModels++;
			}
			return	true;
		}


		void BuildTreeCB(object threadContext)
		{
			BSPBuildParams	bp	=threadContext as BSPBuildParams;

			mTIPool.AssignMaterials();

			if(ProcessEntities(bp.mbVerbose, bp.mbEntityVerbose))
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

			for(int i=0;i < mModels.Count;i++)
			{
				if(!mModels[i].GetFaceVertIndexNumbers(ff))
				{
					return	false;
				}
			}

			//Skip if asked to do so...
			if(!bFixTJunctions)
			{
				return	true;
			}


			CoreEvents.Print(" --- Fix Model TJunctions --- \n");

			for(int i=0;i < mModels.Count;i++)
			{
				if(!mModels[i].FixTJunctions(ff, mTIPool))
				{
					return false;
				}
			}

			if(bVerbose)
			{
				CoreEvents.Print(" Num TJunctions        : " + ff.NumTJunctions + "\n");
				CoreEvents.Print(" Num Fixed Faces       : " + ff.NumFixedFaces + "\n");
			}
			return true;
		}


		bool PrepAllGBSPModels(string visFile, NodeCounter nc, bool bVerbose, bool bEntityVerbose)
		{
			Int32	i;

			List<GFXLeafSide>	leafSides	=new List<GFXLeafSide>();
			for(i=0;i < mModels.Count;i++)
			{
				if(!mModels[i].PrepGBSPModel(visFile, i == 0,
					(i == 0)? bVerbose : bEntityVerbose,
					mPlanePool,
					ref nc.mNumLeafClusters,
					leafSides))
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
									FileMode.OpenOrCreate, FileAccess.Write);

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

			if(!PrepAllGBSPModels(VisFile, nc, sp.mBSPParams.mbVerbose, sp.mBSPParams.mbEntityVerbose))
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

			SaveGFXEntDataList(bw);
			
			bw.Close();
			file.Close();

			CoreEvents.Print(" --- Save GBSP File --- \n");
		 	
			CoreEvents.Print("Num Models           : " + mModels.Count + "\n");
			CoreEvents.Print("Num Nodes            : " + nc.mNumGFXNodes + "\n");
			CoreEvents.Print("Num Solid Leafs      : " + nc.mNumSolidLeafs + "\n");
			CoreEvents.Print("Num Total Leafs      : " + nc.mNumGFXLeafs + "\n");
			CoreEvents.Print("Num Clusters         : " + nc.mNumLeafClusters + "\n");
			CoreEvents.Print("Num Areas            : " + (mGFXAreas.Length - 1) + "\n");
			CoreEvents.Print("Num Area Portals     : " + mGFXAreaPortals.Length + "\n");
			CoreEvents.Print("Num Leafs Sides      : " + mGFXLeafSides.Length + "\n");
			CoreEvents.Print("Num Planes           : " + mPlanePool.mPlanes.Count + "\n");
			CoreEvents.Print("Num Faces            : " + nc.mNumGFXFaces + "\n");
			CoreEvents.Print("Num Leaf Faces       : " + nc.mNumGFXLeafFaces + "\n");
			CoreEvents.Print("Num Vert Index       : " + nc.VertIndexListCount + "\n");
			CoreEvents.Print("Num Verts            : " + mGFXVerts.Length + "\n");
			CoreEvents.Print("Num FaceInfo         : " + mTIPool.mTexInfos.Count + "\n");

			FreeGBSPFile();

			CoreEvents.FireGBSPSaveDoneEvent(true, null);
		}


		bool CreateAreas(GBSPModel worldModel, NodeCounter nc)
		{
			CoreEvents.Print(" --- Create Area Leafs --- \n");

			//Clear all model area info
			foreach(GBSPModel mod in mModels)
			{
				mod.mAreas[0]		=mod.mAreas[1]	=0;
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

				if(mModels[i].mAreas[0] == 0)
				{
					CoreEvents.Print("*WARNING* FinishAreas:  AreaPortal did not touch any areas!\n");
				}
				else if(mModels[i].mAreas[1] == 0)
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
					int	a0	=mModels[m].mAreas[0];
					int	a1	=mModels[m].mAreas[1];

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
			float		Dist1;
			GFXNode		pNode;
			Int32		Side;

			if(node < 0)		// At leaf, no more recursing
			{
				return	node;
			}

			pNode	=mGFXNodes[node];
			
			//Get the distance that the eye is from this plane
			Dist1	=mGFXPlanes[pNode.mPlaneNum].DistanceFast(pos);

			if(Dist1 < 0)
			{
				Side	=1;
			}
			else
			{
				Side	=0;
			}
			
			//Go down the side we are on first, then the other side
			Int32	ret	=0;
			ret	=FindNodeLandedIn(pNode.mChildren[Side], pos);
			if(ret < 0)
			{
				return	ret;
			}
			ret	=FindNodeLandedIn(pNode.mChildren[(Side == 0)? 1 : 0], pos);
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
				if(UtilityLib.Mathery.CompareVectorEpsilon(center,
					new Vector3(64, -32, -27), 1.1f))
				{
					int	gack	=0;
					gack++;
				}

				ret.Add(center);

				GFXPlane	p	=mGFXPlanes[f.mPlaneNum];

				if(f.mPlaneSide > 0)
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


		//todo: remove when done testing
		public GFXPlane []GetPlanes()
		{
			return	mGFXPlanes;
		}


		public bool BuildLMRenderData(GraphicsDevice g,
			//lightmap stuff
			out VertexBuffer lmVB,
			out IndexBuffer lmIB,
			out Int32 []matOffsets,
			out Int32 []matNumVerts,
			out Int32 []matNumTris,

			//animated lightmap stuff
			out VertexBuffer lmAnimVB,
			out IndexBuffer lmAnimIB,
			out Int32 []matAnimOffsets,
			out Int32 []matAnimNumVerts,
			out Int32 []matAnimNumTris,

			//lightmapped alpha stuff
			out VertexBuffer lmaVB,
			out IndexBuffer lmaIB,
			out Int32 []amatOffsets,
			out Int32 []amatNumVerts,
			out Int32 []amatNumTris,
			out Vector3 []amatSortPoints,

			//animated alpha lightmap stuff
			out VertexBuffer lmaAnimVB,
			out IndexBuffer lmaAnimIB,
			out Int32 []amatAnimOffsets,
			out Int32 []amatAnimNumVerts,
			out Int32 []amatAnimNumTris,
			out Vector3 []amatAnimSortPoints,

			int lightAtlasSize,
			object	pp,
			out MaterialLib.TexAtlas lightAtlas)
		{
			MapGrinder	mg	=new MapGrinder(g, mGFXTexInfos, mGFXFaces, mLightMapGridSize, lightAtlasSize);

			if(!mg.BuildLMFaceData(mGFXVerts, mGFXVertIndexes, mGFXModels[0].mFirstFace, mGFXModels[0].mNumFaces, mGFXLightData, pp))
			{
				lmVB	=null;	lmIB	=null;	matOffsets	=null;
				matNumVerts	=null;	matNumTris	=null;	lmAnimVB	=null;
				lmAnimIB	=null;	lmAnimVB	=null;
				matAnimOffsets	=null;	matAnimNumVerts	=null;	matAnimNumTris	=null;
				lmaVB	=null;	lmaIB	=null;	amatOffsets	=null;
				amatNumVerts	=null;	amatNumTris	=null;	lmaAnimVB	=null;
				lmaAnimIB	=null;	lmaAnimVB	=null;
				amatAnimOffsets	=null;	amatAnimNumVerts	=null;	amatAnimNumTris	=null;
				amatSortPoints	=null;	amatAnimSortPoints	=null;	lightAtlas	=null;
				return	false;
			}
			mg.GetLMBuffers(out lmVB, out lmIB);

			if(!mg.BuildLMAnimFaceData(mGFXVerts, mGFXVertIndexes, mGFXModels[0].mFirstFace, mGFXModels[0].mNumFaces, mGFXLightData, pp))
			{
				lmVB	=null;	lmIB	=null;	matOffsets	=null;
				matNumVerts	=null;	matNumTris	=null;	lmAnimVB	=null;
				lmAnimIB	=null;	lmAnimVB	=null;
				matAnimOffsets	=null;	matAnimNumVerts	=null;	matAnimNumTris	=null;
				lmaVB	=null;	lmaIB	=null;	amatOffsets	=null;
				amatNumVerts	=null;	amatNumTris	=null;	lmaAnimVB	=null;
				lmaAnimIB	=null;	lmaAnimVB	=null;
				amatAnimOffsets	=null;	amatAnimNumVerts	=null;	amatAnimNumTris	=null;
				amatSortPoints	=null;	amatAnimSortPoints	=null;	lightAtlas	=null;
				return	false;
			}
			mg.GetLMAnimBuffers(out lmAnimVB, out lmAnimIB);

			if(!mg.BuildLMAFaceData(mGFXVerts, mGFXVertIndexes, mGFXModels[0].mFirstFace, mGFXModels[0].mNumFaces, mGFXLightData, pp))
			{
				lmVB	=null;	lmIB	=null;	matOffsets	=null;
				matNumVerts	=null;	matNumTris	=null;	lmAnimVB	=null;
				lmAnimIB	=null;	lmAnimVB	=null;
				matAnimOffsets	=null;	matAnimNumVerts	=null;	matAnimNumTris	=null;
				lmaVB	=null;	lmaIB	=null;	amatOffsets	=null;
				amatNumVerts	=null;	amatNumTris	=null;	lmaAnimVB	=null;
				lmaAnimIB	=null;	lmaAnimVB	=null;
				amatAnimOffsets	=null;	amatAnimNumVerts	=null;	amatAnimNumTris	=null;
				amatSortPoints	=null;	amatAnimSortPoints	=null;	lightAtlas	=null;
				return	false;
			}
			mg.GetLMABuffers(out lmaVB, out lmaIB);

			if(!mg.BuildLMAAnimFaceData(mGFXVerts, mGFXVertIndexes, mGFXModels[0].mFirstFace, mGFXModels[0].mNumFaces, mGFXLightData, pp))
			{
				lmVB	=null;	lmIB	=null;	matOffsets	=null;
				matNumVerts	=null;	matNumTris	=null;	lmAnimVB	=null;
				lmAnimIB	=null;	lmAnimVB	=null;
				matAnimOffsets	=null;	matAnimNumVerts	=null;	matAnimNumTris	=null;
				lmaVB	=null;	lmaIB	=null;	amatOffsets	=null;
				amatNumVerts	=null;	amatNumTris	=null;	lmaAnimVB	=null;
				lmaAnimIB	=null;	lmaAnimVB	=null;
				amatAnimOffsets	=null;	amatAnimNumVerts	=null;	amatAnimNumTris	=null;
				amatSortPoints	=null;	amatAnimSortPoints	=null;	lightAtlas	=null;
				return	false;
			}
			mg.GetLMAAnimBuffers(out lmaAnimVB, out lmaAnimIB);

			lightAtlas	=mg.GetLightMapAtlas();

			mg.GetLMMaterialData(out matOffsets, out matNumVerts, out matNumTris);
			mg.GetLMAnimMaterialData(out matAnimOffsets, out matAnimNumVerts, out matAnimNumTris);
			mg.GetLMAMaterialData(out amatOffsets, out amatNumVerts, out amatNumTris, out amatSortPoints);
			mg.GetLMAAnimMaterialData(out amatAnimOffsets, out amatAnimNumVerts, out amatAnimNumTris, out amatAnimSortPoints);

			return	true;
		}


		public void BuildVLitRenderData(GraphicsDevice g, out VertexBuffer vb,
			out IndexBuffer ib, out Int32 []matOffsets,
			out Int32 []matNumVerts, out Int32 []matNumTris, object pp)
		{
			MapGrinder	mg	=new MapGrinder(g, mGFXTexInfos, mGFXFaces, mLightMapGridSize, 1);

			Vector3	[]vnorms	=MakeVertNormals();

			mg.BuildVLitFaceData(mGFXVerts, mGFXRGBVerts, vnorms, mGFXModels[0].mFirstFace, mGFXModels[0].mNumFaces, mGFXVertIndexes, pp);

			mg.GetVLitBuffers(out vb, out ib);

			mg.GetVLitMaterialData(out matOffsets, out matNumVerts, out matNumTris);
		}


		public void BuildAlphaRenderData(GraphicsDevice g, out VertexBuffer vb,
			out IndexBuffer ib, out Int32 []matOffsets,	out Int32 []matNumVerts,
			out Int32 []matNumTris, out Vector3 []matSortPoints, object pp)
		{
			MapGrinder	mg	=new MapGrinder(g, mGFXTexInfos, mGFXFaces, mLightMapGridSize, 1);

			Vector3	[]vnorms	=MakeVertNormals();

			mg.BuildAlphaFaceData(mGFXVerts, mGFXRGBVerts, vnorms, mGFXModels[0].mFirstFace, mGFXModels[0].mNumFaces, mGFXVertIndexes, pp);

			mg.GetAlphaBuffers(out vb, out ib);

			mg.GetAlphaMaterialData(out matOffsets, out matNumVerts, out matNumTris, out matSortPoints);
		}


		public void BuildFullBrightRenderData(GraphicsDevice g, out VertexBuffer vb,
			out IndexBuffer ib, out Int32 []matOffsets, out Int32 []matNumVerts,
			out Int32 []matNumTris, object pp)
		{
			MapGrinder	mg	=new MapGrinder(g, mGFXTexInfos, mGFXFaces, mLightMapGridSize, 1);

			mg.BuildFullBrightFaceData(mGFXVerts, mGFXModels[0].mFirstFace, mGFXModels[0].mNumFaces, mGFXVertIndexes, pp);

			mg.GetFullBrightBuffers(out vb, out ib);

			mg.GetFullBrightMaterialData(out matOffsets, out matNumVerts, out matNumTris);
		}


		public void BuildMirrorRenderData(GraphicsDevice g, out VertexBuffer vb,
			out IndexBuffer ib, out Int32 []matOffsets,	out Int32 []matNumVerts,
			out Int32 []matNumTris,	out Vector3 []matSortPoints,
			out List<List<Vector3>> mirrorPolys, object pp)
		{
			MapGrinder	mg	=new MapGrinder(g, mGFXTexInfos, mGFXFaces, mLightMapGridSize, 1);

			Vector3	[]vnorms	=MakeVertNormals();

			mg.BuildMirrorFaceData(mGFXVerts, mGFXRGBVerts, vnorms, mGFXModels[0].mFirstFace, mGFXModels[0].mNumFaces, mGFXVertIndexes, pp);

			mg.GetMirrorBuffers(out vb, out ib);

			mg.GetMirrorMaterialData(out matOffsets, out matNumVerts,
				out matNumTris, out matSortPoints, out mirrorPolys);
		}


		public void BuildSkyRenderData(GraphicsDevice g, out VertexBuffer vb,
			out IndexBuffer ib, out Int32 []matOffsets,	out Int32 []matNumVerts,
			out Int32 []matNumTris, object pp)
		{
			MapGrinder	mg	=new MapGrinder(g, mGFXTexInfos, mGFXFaces, mLightMapGridSize, 1);

			mg.BuildSkyFaceData(mGFXVerts, mGFXModels[0].mFirstFace, mGFXModels[0].mNumFaces, mGFXVertIndexes, pp);

			mg.GetSkyBuffers(out vb, out ib);

			mg.GetSkyMaterialData(out matOffsets, out matNumVerts, out matNumTris);
		}
#endif
	}
}
