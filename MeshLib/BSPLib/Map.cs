using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.Diagnostics;
using Microsoft.Xna.Framework.Storage;


namespace BSPLib
{
	public class Map
	{
		List<Entity>	mEntities;
		BspTree			mDrawTree;
		BspTree			mCollisionTree;
		BspFlatTree		mPortalTree;

		bool		mbBuilding;
		int			mMaxCPUCores;
		bool		mbBevel;

		List<Brush>	mDebugBrushes;		//debuggery remove
		List<Brush>	mDrawBrushes;		//visible geometry
		List<Brush>	mCollisionBrushes;	//collision hull

		GBSPNode	mRoot;

		List<MapBrush>	mMapBrushes		=new List<MapBrush>();
		List<GBSPBrush>	mGBSPBrushes	=new List<GBSPBrush>();

		//planes
		PlanePool	mPlanePool	=new PlanePool();

		//portals
		Dictionary<BspNode, List<Portal>>	mPortals;

		public event EventHandler	eCPUCoresInUseChanged;
		public event EventHandler	eNumMapFacesChanged;
		public event EventHandler	eNumDrawFacesChanged;
		public event EventHandler	eNumCollisionFacesChanged;
		public event EventHandler	eNumPortalsChanged;
		public event EventHandler	eProgressChanged;

		static public event EventHandler	ePrint;

		event EventHandler	eDrawCSGDone;
		event EventHandler	eCollisionCSGDone;


		#region Constructors
		public Map() { }


		//reads a text brush file
		public Map(string mapFileName)
		{
			mEntities	=new List<Entity>();

			if(File.Exists(mapFileName))
			{
				using(StreamReader sr = File.OpenText(mapFileName))
				{
					string	s	="";

					//see if this is a .map or a .vmf
					if(mapFileName.EndsWith(".map"))
					{
						Print(".map files not supported right now\n");
						return;
					}
					else
					{
						while((s = sr.ReadLine()) != null)
						{
							s	=s.Trim();
							if(s == "entity")
							{
								Entity	e	=new Entity();
								e.ReadVMFEntBlock(sr, mEntities.Count, mPlanePool);
								mEntities.Add(e);
							}
							else if(s == "world")
							{
								Entity	e	=new Entity();
								e.ReadVMFWorldBlock(sr, mEntities.Count, mPlanePool);
								mEntities.Add(e);
							}
							else if(s == "cameras")
							{
								Brush.SkipVMFEditorBlock(sr);
							}
							else if(s == "cordon")
							{
								Brush.SkipVMFEditorBlock(sr);
							}
						}
					}
				}
			}
		}
		#endregion


		#region Queries
		public void GetPortalLines(List<Vector3> verts, List<UInt32> indexes)
		{
			mPortalTree.GetPortalLines(verts, indexes);

			int	ofs		=verts.Count;

			UInt32	offset	=(UInt32)ofs;

			//add a few lines for node landed in
			foreach(Entity e in mEntities)
			{
				Vector3	org	=Vector3.Zero;
				if(!e.GetOrigin(out org))
				{
					continue;
				}
				BspFlatNode	landed	=mPortalTree.GetNodeLandedIn(org);
				foreach(Face f in landed.mFaces)
				{
					Vector3	centroid	=f.GetCentroid();

					verts.Add(centroid);
					verts.Add(centroid + Vector3.UnitX * 10.0f);
					verts.Add(centroid);
					verts.Add(centroid + Vector3.UnitY * 10.0f);
					verts.Add(centroid);
					verts.Add(centroid + Vector3.UnitZ * 10.0f);
					verts.Add(centroid);
					verts.Add(centroid - Vector3.UnitX * 10.0f);
					verts.Add(centroid);
					verts.Add(centroid - Vector3.UnitY * 10.0f);
					verts.Add(centroid);
					verts.Add(centroid - Vector3.UnitZ * 10.0f);

					indexes.Add(offset++);
					indexes.Add(offset++);
					indexes.Add(offset++);
					indexes.Add(offset++);
					indexes.Add(offset++);
					indexes.Add(offset++);
					indexes.Add(offset++);
					indexes.Add(offset++);
					indexes.Add(offset++);
					indexes.Add(offset++);
					indexes.Add(offset++);
					indexes.Add(offset++);
				}
			}
		}


		public void GetTriangles(List<Vector3> verts, List<UInt32> indexes, string drawChoice)
		{
			if(drawChoice == "Map Brushes")
			{
				foreach(MapBrush b in mMapBrushes)
				{
					b.GetTriangles(verts, indexes, true);
				}
			}
			else if(drawChoice == "Trouble Brushes")
			{
				lock(BspNode.TroubleBrushes)
				{
					foreach(Brush b in BspNode.TroubleBrushes)
					{
						b.SealFaces();
						b.GetTriangles(verts, indexes, false);
					}
				}
			}
			else if(drawChoice == "Draw Brushes")
			{
				foreach(GBSPBrush b in mGBSPBrushes)
				{
					b.GetTriangles(verts, indexes, true);
				}
			}
			else if(drawChoice == "Collision Brushes")
			{
				foreach(Brush b in mCollisionBrushes)
				{
					b.GetTriangles(verts, indexes, false);
				}
			}
			else if(drawChoice == "Draw Tree")
			{
				mRoot.GetTriangles(verts, indexes, true);
			}
			else if(drawChoice == "Collision Tree")
			{
				mCollisionTree.GetTriangles(verts, indexes, false);
			}
			else if(drawChoice == "Portals")
			{
				mPortalTree.GetPortalTriangles(verts, indexes);
			}
			else if(drawChoice == "Portal Tree")
			{
				mPortalTree.GetTriangles(verts, indexes, true);
			}
		}


		public bool ClassifyPoint(Vector3 pnt)
		{
			return	mCollisionTree.ClassifyPoint(pnt);
		}


		public Vector3 GetPlayerStartPos()
		{
			foreach(Entity e in mEntities)
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
			foreach(Entity e in mEntities)
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


		public Entity GetWorldSpawnEntity()
		{
			foreach(Entity e in mEntities)
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
		#endregion


		#region IO
		public void Write(string fileName)
		{
			FileStream	file	=UtilityLib.FileUtil.OpenTitleFile(fileName,
									FileMode.OpenOrCreate, FileAccess.Write);

			BinaryWriter	bw	=new BinaryWriter(file);

			bw.Write(mEntities.Count);

			//write all entities
			foreach(Entity e in mEntities)
			{
				e.Write(bw);
			}

			//write bsps
			mDrawTree.Write(bw);
			mCollisionTree.Write(bw);

			//write brush lists
			bw.Write(mDrawBrushes.Count);
			foreach(Brush b in mDrawBrushes)
			{
				b.Write(bw);
			}
			bw.Write(mCollisionBrushes.Count);
			foreach(Brush b in mCollisionBrushes)
			{
				b.Write(bw);
			}
		}


		public void Read(string fileName)
		{
			FileStream	file	=UtilityLib.FileUtil.OpenTitleFile(fileName,
									FileMode.Open, FileAccess.Read);

			BinaryReader	br	=new BinaryReader(file);

			int	numEnts	=br.ReadInt32();

			mEntities	=new List<Entity>();
			for(int i=0;i < numEnts;i++)
			{
				Entity	e	=new Entity();
				e.Read(br);

				mEntities.Add(e);
			}

			mDrawTree		=new BspTree();
			mCollisionTree	=new BspTree();

			mDrawTree.Read(br);
			mCollisionTree.Read(br);

			mDrawBrushes		=new List<Brush>();
			mCollisionBrushes	=new List<Brush>();
			mDebugBrushes		=new List<Brush>();

			int	numBrushes	=br.ReadInt32();
			for(int i=0;i < numBrushes;i++)
			{
				Brush	b	=new Brush();
				b.Read(br);

				mDrawBrushes.Add(b);
			}

			numBrushes	=br.ReadInt32();
			for(int i=0;i < numBrushes;i++)
			{
				Brush	b	=new Brush();
				b.Read(br);

				mCollisionBrushes.Add(b);
			}
		}
		#endregion


		//makes sure that only one volume
		//occupies any space
		void RemoveOverlap(List<Brush> brushes, object prog)
		{
			int	iterationCount	=0;
			for(int i=0;i < brushes.Count;i++)
			{
				for(int j=0;j < brushes.Count;j++, iterationCount++)
				{
					if(i == j)
					{
						continue;
					}

					if(iterationCount > 100000)
					{
						ProgressWatcher.UpdateProgress(prog, 0, brushes.Count, i);
						iterationCount	=0;
					}

					if(!brushes[i].Intersects(brushes[j]))
					{
						continue;
					}

					List<Brush>	cutup	=new List<Brush>();
					List<Brush>	cutup2	=new List<Brush>();

					if(brushes[i].SubtractBrush(brushes[j], out cutup))
					{
						//make sure the brush returned is
						//not the one passed in
						if(cutup.Count == 1)
						{
							Debug.Assert(!brushes[i].Equals(cutup[0]));
						}
					}
					else
					{
						cutup.Clear();
					}

					if(brushes[j].SubtractBrush(brushes[i], out cutup2))
					{
						//make sure the brush returned is
						//not the one passed in
						if(cutup2.Count == 1)
						{
							Debug.Assert(!brushes[j].Equals(cutup2[0]));
						}
					}
					else
					{
						cutup2.Clear();
					}

					if(cutup.Count==0 && cutup2.Count==0)
					{
						continue;
					}

					if(cutup.Count > 4 && cutup2.Count > 4)
					{
						continue;
					}

					if(cutup.Count < cutup2.Count)
					{
						cutup2.Clear();

						foreach(Brush b in cutup)
						{
							if(b.IsValid())
							{
								brushes.Add(b);
							}
						}
						cutup.Clear();
						brushes.RemoveAt(i);
						i--;
						break;
					}
					else
					{
						cutup.Clear();

						foreach(Brush b in cutup2)
						{
							if(b.IsValid())
							{
								brushes.Add(b);
							}
						}
						cutup2.Clear();
						brushes.RemoveAt(j);
						j--;
						continue;
					}
				}
			}

			//nuke thins and invalids
			for(int i=0;i < brushes.Count;i++)
			{
				Brush	b	=brushes[i];

				if(!b.IsValid())
				{
					Print("Invalid brush removed");
					brushes.RemoveAt(i);
					i--;
					continue;
				}

				if(b.IsVeryThin())
				{
					b.RemoveVeryThinSides();
					if(!b.IsValid())
					{
//						lock(BspNode.TroubleBrushes)
//						{
//							BspNode.TroubleBrushes.Add(b);
//						}
						Print("Thin brush removed");
						brushes.RemoveAt(i);
						i--;
					}
				}
			}
		}


		void CopyDetailBrushes(List<Brush> details)
		{
			foreach(Entity e in mEntities)
			{
				if(e.mBrushes != null)
				{
					if(e.mData.ContainsKey("classname"))
					{
						if(e.mData["classname"] == "func_detail")
						{
/*							foreach(Brush db in e.mBrushes)
							{
								Brush	b	=new Brush(db);

								details.Add(b);
							}*/
						}
					}
				}
			}
		}


		void PromoteClips(List<Brush> brushes)
		{
			foreach(Brush b in brushes)
			{
				b.PromoteClips();
			}
		}


		internal void UpdateNumPortals(int numPortals)
		{
			if(eNumPortalsChanged != null)
			{
				eNumPortalsChanged(numPortals, null);
			}
		}


		void CBRemoveDrawOverlap(object context)
		{
			Print("Draw overlap removal thread starting\n");

			//copy map brushes
			mDrawBrushes	=new List<Brush>();
			foreach(Brush b in mDebugBrushes)
			{
				Brush	db	=new Brush(b);
				mDrawBrushes.Add(db);
			}

			if(eNumDrawFacesChanged != null)
			{
				eNumDrawFacesChanged(mDrawBrushes.Count, null);
			}

			object	prog	=ProgressWatcher.RegisterProgress(0, mDrawBrushes.Count, 0);

			RemoveOverlap(mDrawBrushes, prog);

			ProgressWatcher.DestroyProgress(prog);

			Print("" + mDrawBrushes.Count + " draw brushes after overlap removal\n");

			if(eNumDrawFacesChanged != null)
			{
				eNumDrawFacesChanged(mDrawBrushes.Count, null);
			}
			if(eDrawCSGDone != null)
			{
				eDrawCSGDone(null, null);
			}
		}


		void CBRemoveCollisionOverlap(object context)
		{
			Print("Collision overlap removal thread starting\n");

			//copy map brushes
			mCollisionBrushes	=new List<Brush>();
			foreach(Brush b in mDebugBrushes)
			{
				Brush	cb	=new Brush(b);
				mCollisionBrushes.Add(cb);
			}

			//promote clips to solid and make the collision hull
			PromoteClips(mCollisionBrushes);
			if(eNumCollisionFacesChanged != null)
			{
				eNumCollisionFacesChanged(mCollisionBrushes.Count, null);
			}

			object	prog	=ProgressWatcher.RegisterProgress(0, mCollisionBrushes.Count, 0);

			RemoveOverlap(mCollisionBrushes, prog);

			ProgressWatcher.DestroyProgress(prog);

			Print("" + mCollisionBrushes.Count + " collision brushes after overlap removal\n");

			if(eNumCollisionFacesChanged != null)
			{
				eNumCollisionFacesChanged(mCollisionBrushes.Count, null);
			}

			if(eCollisionCSGDone != null)
			{
				eCollisionCSGDone(context, null);
			}
		}


		void OnCollisionCSGDone(object sender, EventArgs ea)
		{
			eCollisionCSGDone	-=OnCollisionCSGDone;

			Print("Starting collision tree build\n");

			//dupe collision list
			List<Brush>	colls	=new List<Brush>();
			foreach(Brush b in mCollisionBrushes)
			{
				colls.Add(new Brush(b));
			}

			//dupe draw list
			List<Brush>	ports	=new List<Brush>();
			foreach(Brush b in mDrawBrushes)
			{
				ports.Add(new Brush(b));
			}

			mCollisionTree	=new BspTree(colls, sender != null);
			mCollisionTree.eBuildComplete	+=OnCollisionTreeBuildComplete;
		}


		void OnCollisionTreeBuildComplete(object sender, EventArgs ea)
		{
		}


		void OnPortalTreeBuildComplete(object sender, EventArgs ea)
		{
			Print("Portalizing...\n");
			mPortalTree.Portalize(UpdateNumPortals);
			Print("Portalization complete\n");
		}


		void OnPortalizationComplete(object sender, EventArgs ea)
		{
			Dictionary<BspFlatNode, List<Entity>>	nodeEnts
				=new Dictionary<BspFlatNode, List<Entity>>();

			foreach(Entity e in mEntities)
			{
				Vector3	org	=Vector3.Zero;
				if(!e.GetOrigin(out org))
				{
					continue;
				}
				BspFlatNode	landed	=mPortalTree.GetNodeLandedIn(org);
				if(!nodeEnts.ContainsKey(landed))
				{
					nodeEnts.Add(landed, new List<Entity>());
				}
				nodeEnts[landed].Add(e);
			}

			Print("Checking for leaks...\n");

			if(mPortalTree.CheckForLeak(nodeEnts))
			{
				Print("Leak found, stopping here...\n");
			}
			else
			{
				Print("No leaks found.\n");
			}
		}


		void OnDrawCSGDone(object sender, EventArgs ea)
		{
			eDrawCSGDone	-=OnDrawCSGDone;

			Print("Starting draw tree build\n");

			//dupe draw list twice
			List<Brush>	draws	=new List<Brush>();
			List<Brush>	ports	=new List<Brush>();
			foreach(Brush b in mDrawBrushes)
			{
				draws.Add(new Brush(b));
				ports.Add(new Brush(b));
			}

			mDrawTree	=new BspTree(draws, false);
			mPortalTree	=new BspFlatTree(ports);

//			mPortalTree.eBuildComplete			+=OnPortalTreeBuildComplete;
			mPortalTree.ePortalizationComplete	+=OnPortalizationComplete;

			//not threaded yet
			OnPortalTreeBuildComplete(null, null);
		}


		static internal void Print(string str)
		{
			EventHandler	evt	=ePrint;
			if(evt != null)
			{
				if(str.EndsWith("\n"))
				{
					evt(str, null);
				}
				else
				{
					evt(str + "\n", null);
				}
			}
		}


		public void BuildTree(bool bBevel, int maxCPUCores)
		{
			mbBuilding		=true;
			mbBevel			=bBevel;
			mMaxCPUCores	=maxCPUCores;

			//look for the worldspawn
			Entity	wse	=GetWorldSpawnEntity();

			foreach(MapBrush b in wse.mBrushes)
			{
				mMapBrushes.Add(b);
			}

			foreach(MapBrush b in mMapBrushes)
			{
				GBSPBrush	gb	=new GBSPBrush(b);

				mGBSPBrushes.Add(gb);
			}

			GBSPBrush.CSGBrushes(mGBSPBrushes, mPlanePool);

			//copy all starting brushes to debug
			mDebugBrushes	=new List<Brush>();

			mRoot	=new GBSPNode();
			mRoot.BuildBSP(mGBSPBrushes, mPlanePool);
			/*
			foreach(Brush b in wse.mBrushes)
			{
				Brush	cb	=new Brush(b);
				mDebugBrushes.Add(cb);
			}
//			CopyDetailBrushes(mDebugBrushes);
			if(eNumMapFacesChanged != null)
			{
				eNumMapFacesChanged(mDebugBrushes.Count, null);
			}

			Print("" + mDebugBrushes.Count + " map brushes\n");

			Print("Starting two threads for overlap removal\n");

			eCollisionCSGDone	+=OnCollisionCSGDone;
			eDrawCSGDone		+=OnDrawCSGDone;

			//start two threads for removing overlap
			//for the two datasets
			ThreadPool.QueueUserWorkItem(CBRemoveDrawOverlap);
			ThreadPool.QueueUserWorkItem(CBRemoveCollisionOverlap, bBevel ? new object() : null);
			*/
		}


		public bool MoveLine(ref Line ln)
		{
			return	mCollisionTree.MoveLine(ref ln);
		}


		public bool MoveLine(ref Line ln, float radius)
		{
			return	mCollisionTree.MoveLine(ref ln, radius);
		}


		public bool RayCast(Vector3 p1, Vector3 p2, ref List<ClipSegment> segs)
		{
			return	mCollisionTree.RayCast(p1, p2, ref segs);
		}


		public void RayCast3(Vector3 mStart, Vector3 mEnd, List<Ray> rayParts)
		{
			mCollisionTree.RayCast3(mStart, mEnd, rayParts);
		}
	}
}
