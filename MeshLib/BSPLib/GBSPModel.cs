using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class GBSPModel
	{
		public GBSPNode	[]mRootNode	=new GBSPNode[2];
		public Vector3	mOrigin;
		public GBSPNode	mOutsideNode	=new GBSPNode();
		public Bounds	mBounds;

		//for saving, might delete
		public int		[]mRootNodeID	=new int[2];
		public int		mFirstFace, mNumFaces;
		public int		mFirstLeaf, mNumLeafs;
		public int		mFirstCluster, mNumClusters;
		public int		mNumSolidLeafs;

		//area portal stuff, probably won't use
		public bool		mbAreaPortal;
		public int		[]mAreas	=new int[2];

		//temporary
		GBSPBrush	mGBSPBrushes;


		static internal void DumpBrushListToFile(GBSPBrush brushList)
		{
			FileStream		fs	=new FileStream("BrushSides.txt", FileMode.Create, FileAccess.Write);
			StreamWriter	sw	=new StreamWriter(fs);			

			for(GBSPBrush b=brushList;b != null;b=b.mNext)
			{
				for(int i=0;i < b.mSides.Count;i++)
				{
					sw.Write("" + b.mSides[i].mPlaneNum + ", " +
						b.mSides[i].mPlaneSide + ", " +
						b.mSides[i].mFlags + "\n");
				}
			}
			sw.Close();
			fs.Close();
		}


		internal bool ProcessWorldModel(GBSPGlobals gbs, List<MapBrush> list,
			List<MapEntity> ents, PlanePool pool, TexInfoPool tip)
		{
			GBSPBrush	prev	=null;

			list.Reverse();
			foreach(MapBrush b in list)
			{
				GBSPBrush	gb	=new GBSPBrush(b);

				//if brush is being dropped, the mOriginal
				//reference will be null
				if(gb.mOriginal == null)
				{
					continue;
				}

				if(prev != null)
				{
					prev.mNext	=gb;
				}

				if(mGBSPBrushes == null)
				{
					mGBSPBrushes	=gb;
				}
				prev	=gb;
			}

			mGBSPBrushes	=GBSPBrush.CSGBrushes(gbs.Verbose, mGBSPBrushes, pool);

			DumpBrushListToFile(mGBSPBrushes);

			GBSPNode	root	=new GBSPNode();
			root.BuildBSP(gbs, mGBSPBrushes, pool);

			mBounds	=new Bounds();
			mBounds.mMins	=gbs.TreeMins;
			mBounds.mMaxs	=gbs.TreeMaxs;

			mGBSPBrushes	=null;
			prev			=null;

			if(!root.CreatePortals(mOutsideNode, false, gbs.Verbose, pool, mBounds.mMins, mBounds.mMaxs))
			{
				Map.Print("Could not create the portals.\n");
				return	false;
			}

			if(root.RemoveHiddenLeafs(gbs, mOutsideNode, ents, pool) == -1)
			{
				Map.Print("Failed to remove hidden leafs.\n");
			}

			root.MarkVisibleSides(gbs, list, pool);

			if(!root.FreePortals())
			{
				Map.Print("BuildBSP:  Could not free portals.\n");
				return	false;
			}

			root.FreeBSP_r();

			foreach(MapBrush b in list)
			{
				GBSPBrush	gb	=new GBSPBrush(b);

				//if brush is being dropped, the mOriginal
				//reference will be null
				if(gb.mOriginal == null)
				{
					continue;
				}

				if(prev != null)
				{
					prev.mNext	=gb;
				}

				if(mGBSPBrushes == null)
				{
					mGBSPBrushes	=gb;
				}
				prev	=gb;
			}

			mGBSPBrushes	=GBSPBrush.CSGBrushes(gbs.Verbose, mGBSPBrushes, pool);

			root.BuildBSP(gbs, mGBSPBrushes, pool);

			if(!root.CreatePortals(mOutsideNode, false, gbs.Verbose, pool, mBounds.mMins, mBounds.mMaxs))
			{
				Map.Print("Could not create the portals.\n");
				return	false;
			}

			if(root.RemoveHiddenLeafs(gbs, mOutsideNode, ents, pool) == -1)
			{
				Map.Print("Failed to remove hidden leafs.\n");
			}

			root.MarkVisibleSides(gbs, list, pool);

			root.MakeFaces(gbs, pool, tip);

			root.MakeLeafFaces();

			if(!root.FreePortals())
			{
				Map.Print("BuildBSP:  Could not free portals.\n");
				return	false;
			}

			root.MergeNodes(gbs);

			mRootNode[0]	=root;

			return	true;
		}


		internal bool ProcessSubModel(GBSPGlobals gbs, List<MapBrush> list,
			PlanePool pool, TexInfoPool tip)
		{
			GBSPBrush	prev	=null;

			list.Reverse();
			foreach(MapBrush b in list)
			{
				GBSPBrush	gb	=new GBSPBrush(b);

				if(prev != null)
				{
					prev.mNext	=gb;
				}

				if(mGBSPBrushes == null)
				{
					mGBSPBrushes	=gb;
				}
				prev	=gb;
			}

			mGBSPBrushes	=GBSPBrush.CSGBrushes(gbs.Verbose, mGBSPBrushes, pool);

			GBSPNode	root	=new GBSPNode();
			root.BuildBSP(gbs, mGBSPBrushes, pool);

			mBounds			=new Bounds();
			mBounds.mMins	=gbs.TreeMins;
			mBounds.mMaxs	=gbs.TreeMaxs;

			mGBSPBrushes	=null;
			prev			=null;

			if(!root.CreatePortals(mOutsideNode, false, gbs.Verbose, pool, mBounds.mMins, mBounds.mMaxs))
			{
				Map.Print("Could not create the portals.\n");
				return	false;
			}

			root.MarkVisibleSides(gbs, list, pool);

			root.MakeFaces(gbs, pool, tip);

			if(!root.FreePortals())
			{
				Map.Print("BuildBSP:  Could not free portals.\n");
				return	false;
			}

			root.MergeNodes(gbs);

			mRootNode[0]	=root;

			return	true;
		}


		internal bool PrepareSubModelForVis()
		{
			mFirstCluster	=-1;
			mNumClusters	=0;

			return	true;
		}


		internal void GetTriangles(List<Vector3> verts, List<uint> indexes, bool bCheck)
		{
//			for(GBSPBrush b = mGBSPBrushes;b != null;b=b.mNext)
//			{
//				b.GetTriangles(verts, indexes, bCheck);
//			}
			mRootNode[0].GetLeafTriangles(verts, indexes, bCheck);
		}


		internal bool VisAllLeafs(int numLeafClusters, PlanePool pool)
		{/*
			Int32	NumVisLeafBytes		=((visLeafs.Count + 63)&~63) >> 3;
			Int32	NumVisPortalBytes	=((visPortals.Count + 63)&~63) >> 3;
			Int32	NumVisPortalLongs	=NumVisPortalBytes / sizeof(UInt32);
			Int32	NumVisLeafLongs		=NumVisLeafBytes / sizeof(UInt32);

			foreach(KeyValuePair<Int32, VISLeaf> leafs in visLeafs)
			{
				leafs.Value.FloodPortalsFast(leafs.Key, NumVisPortalBytes, visLeafs, visPortals);
			}

			VisLeafComparer	comp	=new VisLeafComparer();

			byte	[]leafVisBits	=new byte[visLeafs.Count * NumVisLeafBytes];
			byte	[]portalBits	=new byte[NumVisPortalBytes];

			int	totalVisibleLeafs	=0;

			List<GFXCluster>	gfxClusters	=new List<GFXCluster>();

			foreach(KeyValuePair<Int32, VISLeaf> leafs in visLeafs)
			{
				int	LeafSee	=0;
				if(!leafs.Value.CollectLeafVisBits(leafs.Key, ref LeafSee, leafVisBits,
					portalBits, NumVisPortalBytes, NumVisLeafBytes, gfxClusters,
					visLeafs, visPortals))
				{
					return	false;
				}
				totalVisibleLeafs	+=LeafSee;
			}

			Map.Print("Total visible areas           : " + totalVisibleLeafs + "\n");
			Map.Print("Average visible from each area: " + (totalVisibleLeafs / visLeafs.Count) + "\n");
			*/
			return	true;
		}


		internal bool PrepGBSPModel(string VisFile, bool SaveVis, PlanePool pool, Map map)
		{
			if(SaveVis)
			{
				if(!mRootNode[0].CreatePortals(mOutsideNode, true, false, pool, mBounds.mMins, mBounds.mMaxs))
				{
					Map.Print("Could not create VIS portals.\n");
					return	false;
				}

				mFirstCluster	=map.mGlobals.NumLeafClusters;

				if(!mRootNode[0].CreateLeafClusters(map.mGlobals))
				{
					Map.Print("Could not create leaf clusters.\n");
					return	false;
				}

				mNumClusters	=map.mGlobals.NumLeafClusters - mFirstCluster;

				if(!SavePortalFile(map.mGlobals, VisFile, pool))
				{
					return	false;
				}

				if(!mRootNode[0].FreePortals())
				{
					Map.Print("PrepGBSPModel:  Could not free portals.\n");
					return	false;
				}
			}
			else
			{
				mFirstCluster	=-1;
				mNumClusters	=0;
			}

			if(!mRootNode[0].CreatePortals(mOutsideNode, false, false, pool, mBounds.mMins, mBounds.mMaxs))
			{
				Map.Print("Could not create REAL portals.\n");
				return	false;
			}

			if(!mRootNode[0].CreateLeafSides(map.mGlobals, pool))
			{
				Map.Print("Could not create leaf sides.\n");
				return	false;
			}

			//create area leafs
			if(this == map.mModels[0])
			{
				if(!mRootNode[0].CreateAreas(map))
				{
					Map.Print("Could not create Areas.\n");
					return	false;
				}
			}

			mFirstFace	=map.mGlobals.NumGFXFaces;
			mFirstLeaf	=map.mGlobals.NumGFXLeafs;

			mRootNodeID[0]	=mRootNode[0].PrepGFXNodes_r(map.mGlobals, mRootNodeID[0]);

			mNumFaces	=map.mGlobals.NumGFXFaces - mFirstFace;
			mNumLeafs	=map.mGlobals.NumGFXLeafs - mFirstLeaf;

			return	true;
		}


		bool SavePortalFile(GBSPGlobals gg, string FileName, PlanePool pool)
		{
			string	PortalFile;

			Map.Print(" --- Save Portal File --- \n");
			  
			PortalFile	=FileName;

			int	dotPos	=PortalFile.LastIndexOf('.');
			PortalFile	=PortalFile.Substring(0, dotPos);
			PortalFile	+=".gpf";

			FileStream	fs	=UtilityLib.FileUtil.OpenTitleFile(PortalFile,
				FileMode.OpenOrCreate, FileAccess.Write);

			if(fs == null)
			{
				Map.Print("SavePortalFile:  Error opening " + PortalFile + " for writing.\n");
				return	false;
			}

			BinaryWriter	bw	=new BinaryWriter(fs);

			gg.NumPortals		=0;	//Number of portals
			gg.NumPortalLeafs	=0;	//Current leaf number

			if(!mRootNode[0].PrepPortalFile_r(gg))
			{
				bw.Close();
				fs.Close();
				Map.Print("SavePortalFile:  Could not PrepPortalFile.\n");
				return	false;
			}

			if(gg.NumPortalLeafs != mNumClusters)
			{
				bw.Close();
				fs.Close();
				Map.Print("SavePortalFile:  Invalid number of clusters!!!\n");
				return	false;
			}

			bw.Write("GBSP_PRTFILE");
			bw.Write(gg.NumPortals);
			bw.Write(mNumClusters);

			if(!mRootNode[0].SavePortalFile_r(gg, bw, pool))
			{
				bw.Close();
				fs.Close();
				return	false;
			}

			bw.Close();
			fs.Close();

			Map.Print("Num Portals          : " + gg.NumPortals + "\n");
			Map.Print("Num Portal Leafs     : " + gg.NumPortalLeafs + "\n");

			return	true;
		}
	}
}
