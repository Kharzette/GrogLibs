using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;


namespace BSPCore
{
	internal class GBSPModel
	{
		GBSPNode	mRootNode;
		Vector3		mOrigin;
		GBSPNode	mOutsideNode	=new GBSPNode();
		Bounds		mBounds;

		//for saving, might delete
		internal int	mRootNodeID;
		internal int	mFirstFace, mNumFaces;
		internal int	mFirstLeaf, mNumLeafs;
		internal int	mFirstCluster, mNumClusters;

		//area portal stuff, probably won't use
		internal bool	mbAreaPortal;
		internal int	mAreaFront, mAreaBack;

		//blockery
		Array	mBlockNodes;


		internal bool ProcessWorldModel(List<MapBrush> list, List<MapEntity> ents,
			PlanePool pool, TexInfoPool tip, bool bVerbose)
		{
			Bounds	modelBounds	=MapBrush.GetListBounds(list);

			//find the min and max extents rounded out on K boundaries
			//sort of the way quake 2 does it
			int	kMinX	=(int)Math.Floor(modelBounds.mMins.X / 1024.0f);
			int	kMinZ	=(int)Math.Floor(modelBounds.mMins.Z / 1024.0f);
			int	kMaxX	=(int)Math.Ceiling(modelBounds.mMaxs.X / 1024.0f);
			int	kMaxZ	=(int)Math.Ceiling(modelBounds.mMaxs.Z / 1024.0f);

			mBlockNodes	=Array.CreateInstance(typeof(GBSPNode),
				(kMaxX - kMinX) + 1,
				(kMaxZ - kMinZ) + 1);

			list.Reverse();
			List<GBSPBrush>	glist	=GBSPBrush.ConvertMapBrushList(list);

			for(int z=kMinZ;z < kMaxZ;z++)
			{
				for(int x=kMinX;x < kMaxX;x++)
				{
					GBSPNode	blockNode	=ProcessBlock(glist, pool, x, z);
					mBlockNodes.SetValue(blockNode, x - kMinX, z - kMinZ);
				}
			}
			
			GBSPNode	root	=GBSPNode.BlockTree(mBlockNodes, pool,
				kMinX, kMinZ,
				kMinX, kMinZ, kMaxX, kMaxZ);

			mBounds	=new Bounds();

			//crank out the bounds to the blocks used
			mBounds.mMins.X	=kMinX * 1024.0f;
			mBounds.mMins.Y	=modelBounds.mMins.Y;
			mBounds.mMins.Z	=kMinZ * 1024.0f;

			mBounds.mMaxs.X	=(kMaxX + 1) * 1024.0f;
			mBounds.mMaxs.Y	=modelBounds.mMaxs.Y + 8.0f;
			mBounds.mMaxs.Z	=(kMaxZ + 1) * 1024.0f;

			glist	=null;

			if(!root.CreatePortals(mOutsideNode, false, bVerbose, pool, mBounds.mMins, mBounds.mMaxs))
			{
				CoreEvents.Print("Could not create the portals.\n");
				return	false;
			}

			if(root.RemoveHiddenLeafs(mOutsideNode, ents, pool, bVerbose) == -1)
			{
				CoreEvents.Print("Failed to remove hidden leafs.\n");
			}

			root.MarkVisibleSides(list, pool, bVerbose);

			if(!root.FreePortals())
			{
				CoreEvents.Print("BuildBSP:  Could not free portals.\n");
				return	false;
			}

			root.FreeBSP_r();

			glist	=GBSPBrush.ConvertMapBrushList(list);

			GBSPNode.bDumpery	=true;

			for(int z=kMinZ;z < kMaxZ;z++)
			{
				for(int x=kMinX;x < kMaxX;x++)
				{
					GBSPNode	blockNode	=ProcessBlock(glist, pool, x, z);
					mBlockNodes.SetValue(blockNode, x - kMinX, z - kMinZ);
				}
			}

			GBSPBrush.DumpBrushListToFile(GBSPNode.leafBrushes, pool, "LeafBrushes.map");

			root	=GBSPNode.BlockTree(mBlockNodes, pool,
				kMinX, kMinZ,
				kMinX, kMinZ, kMaxX, kMaxZ);

			CoreEvents.FireNumPlanesChangedEvent(pool.mPlanes.Count, null);

			if(!root.CreatePortals(mOutsideNode, false, bVerbose, pool, mBounds.mMins, mBounds.mMaxs))
			{
				CoreEvents.Print("Could not create the portals.\n");
				return	false;
			}

			if(root.RemoveHiddenLeafs(mOutsideNode, ents, pool, bVerbose) == -1)
			{
				CoreEvents.Print("Failed to remove hidden leafs.\n");
			}

			root.MarkVisibleSides(list, pool, bVerbose);

			root.MakeFaces(pool, bVerbose);

//			GBSPFace.DumpFaceList(GBSPNode.dumpFaces, pool, "DumpFaces.Debug");

			root.MakeLeafFaces();

			if(!root.FreePortals())
			{
				CoreEvents.Print("BuildBSP:  Could not free portals.\n");
				return	false;
			}

			root.MergeNodes(bVerbose);

			mRootNode	=root;

			return	true;
		}


//		static int dumps	=0;
		internal GBSPNode ProcessBlock(List<GBSPBrush> brushes, PlanePool pp, int xblock, int zblock)
		{
			CoreEvents.Print("############### block " + xblock + "," + zblock + " ###############\n");

			Bounds	blockBounds	=new Bounds();

			blockBounds.mMins.X	=xblock * 1024;
			blockBounds.mMins.Z	=zblock * 1024;
			blockBounds.mMins.Y	=-4096;
			blockBounds.mMaxs.X	=(xblock + 1) * 1024;
			blockBounds.mMaxs.Z	=(zblock + 1) * 1024;
			blockBounds.mMaxs.Y	=4096;

			CoreEvents.Print("" + blockBounds.mMins + ", " + blockBounds.mMaxs + "\n");

			List<GBSPBrush>	blocked	=GBSPBrush.BlockChopBrushes(brushes, blockBounds, pp);

			int	brushCount	=blocked.Count;

			GBSPBrush.TestBrushListValid(blocked);

			if(!GBSPBrush.TestListInBounds(blocked, blockBounds))
			{
				CoreEvents.Print("Brush out of bounds after choppery!\n");
				GBSPBrush.DumpBrushListToFile(blocked, pp, "Brush_x" + xblock + "_z" + zblock + ".map");
			}

			List<GBSPBrush>	csgList	=GBSPBrush.CSGBrushes(true, blocked, pp);			

			CoreEvents.FireNumPlanesChangedEvent(pp.mPlanes.Count, null);

//			GBSPBrush.DumpBrushListToFile(csgList, pp, "CSG" + dumps++ + ".map");

			//print out brushes that are still overlapping
			GBSPBrush.DumpOverlapping(csgList, pp);

			GBSPNode	root	=new GBSPNode();
			root.BuildBSP(csgList, pp, true);
			CoreEvents.FireNumPlanesChangedEvent(pp.mPlanes.Count, null);

			return	root;
		}


		internal bool ProcessSubModel(List<MapBrush> list,
			PlanePool pool, TexInfoPool tip, bool bVerbose)
		{
			list.Reverse();

			List<GBSPBrush>	glist	=GBSPBrush.ConvertMapBrushList(list);

			List<GBSPBrush>	csgList	=GBSPBrush.CSGBrushes(bVerbose, glist, pool);

			GBSPNode	root	=new GBSPNode();
			root.BuildBSP(csgList, pool, bVerbose);

			mBounds			=new Bounds(root.GetBounds());

			glist	=null;

			if(!root.CreatePortals(mOutsideNode, false, bVerbose, pool, mBounds.mMins, mBounds.mMaxs))
			{
				CoreEvents.Print("Could not create the portals.\n");
				return	false;
			}

			root.MarkVisibleSides(list, pool, bVerbose);

			root.MakeFaces(pool, bVerbose);

			if(!root.FreePortals())
			{
				CoreEvents.Print("BuildBSP:  Could not free portals.\n");
				return	false;
			}

			root.MergeNodes(bVerbose);

			mRootNode	=root;

			return	true;
		}


		internal void GetTriangles(List<Vector3> verts, List<uint> indexes, bool bCheck)
		{
			mRootNode.GetLeafTriangles(verts, indexes, bCheck);
		}


		internal bool PrepGBSPModel(string visFile, bool bSaveVis,
			bool bVerbose, PlanePool pool, ref int numLeafClusters,
			List<GFXLeafSide> leafSides)
		{
			if(bSaveVis)
			{
				if(!mRootNode.CreatePortals(mOutsideNode, true, false, pool, mBounds.mMins, mBounds.mMaxs))
				{
					CoreEvents.Print("Could not create VIS portals.\n");
					return	false;
				}

				mFirstCluster	=numLeafClusters;

				if(!mRootNode.CreateLeafClusters(bVerbose, ref numLeafClusters))
				{
					CoreEvents.Print("Could not create leaf clusters.\n");
					return	false;
				}

				mNumClusters	=numLeafClusters - mFirstCluster;

				if(!SavePortalFile(visFile, pool, numLeafClusters))
				{
					return	false;
				}

				if(!mRootNode.FreePortals())
				{
					CoreEvents.Print("PrepGBSPModel:  Could not free portals.\n");
					return	false;
				}
			}
			else
			{
				mFirstCluster	=-1;
				mNumClusters	=0;
			}

			if(!mRootNode.CreatePortals(mOutsideNode, false, false, pool, mBounds.mMins, mBounds.mMaxs))
			{
				CoreEvents.Print("Could not create REAL portals.\n");
				return	false;
			}

			if(!mRootNode.CreateLeafSides(pool, leafSides, mOutsideNode, bVerbose))
			{
				CoreEvents.Print("Could not create leaf sides.\n");
				return	false;
			}

			return	true;
		}


		internal void PrepNodes(NodeCounter nc)
		{
			mFirstFace	=nc.mNumGFXFaces;
			mFirstLeaf	=nc.mNumGFXLeafs;

			mRootNodeID	=mRootNode.PrepGFXNodes_r(mRootNodeID, nc);

			mNumFaces	=nc.mNumGFXFaces - mFirstFace;
			mNumLeafs	=nc.mNumGFXLeafs - mFirstLeaf;
		}


		bool SavePortalFile(string fileName, PlanePool pool, int numLeafClusters)
		{
			string	portalFile	=fileName;

			CoreEvents.Print(" --- Save Portal File --- \n");
			  
			int	dotPos	=portalFile.LastIndexOf('.');
			portalFile	=portalFile.Substring(0, dotPos);
			portalFile	+=".gpf";

			FileStream	fs	=new FileStream(portalFile,
				FileMode.OpenOrCreate, FileAccess.Write);

			if(fs == null)
			{
				CoreEvents.Print("SavePortalFile:  Error opening " + portalFile + " for writing.\n");
				return	false;
			}

			BinaryWriter	bw	=new BinaryWriter(fs);

			int	numPortals		=0;	//Number of portals
			int	numPortalLeafs	=0;	//Current leaf number

//			bool	bMergey	=mRootNode[0].MergePortals_r(pool);

//			mRootNode[0].NumberLeafs_r(ref numPortalLeafs, ref numPortals);

			if(!mRootNode.PrepPortalFile_r(ref numPortalLeafs, ref numPortals))
			{
				bw.Close();
				fs.Close();
				CoreEvents.Print("SavePortalFile:  Could not PrepPortalFile.\n");
				return	false;
			}

			if(numPortalLeafs != mNumClusters)
			{
				bw.Close();
				fs.Close();
				CoreEvents.Print("SavePortalFile:  Invalid number of clusters!!!\n");
				return	false;
			}

			bw.Write("GBSP_PRTFILE");
			bw.Write(numPortals);
			bw.Write(mNumClusters);

			if(!mRootNode.SavePortalFile_r(bw, pool, numLeafClusters))
			{
				bw.Close();
				fs.Close();
				return	false;
			}

			bw.Close();
			fs.Close();

			CoreEvents.Print("Num Portals          : " + numPortals + "\n");
			CoreEvents.Print("Num Portal Leafs     : " + numPortalLeafs + "\n");

			return	true;
		}


		internal void SetOrigin(Vector3 org)
		{
			mOrigin	=org;
		}


		internal bool GetFaceVertIndexNumbers(FaceFixer ff)
		{
			return	mRootNode.GetFaceVertIndexNumbers_r(ff);
		}


		internal bool FixTJunctions(FaceFixer ff, TexInfoPool tip)
		{
			return	mRootNode.FixTJunctions_r(ff, tip);
		}


		internal void ConvertToGFXAndSave(BinaryWriter bw)
		{
			GFXModel	GModel	=new GFXModel();

			GModel.mRootNode		=mRootNodeID;
			GModel.mOrigin			=mOrigin;
			GModel.mMins			=mBounds.mMins;
			GModel.mMaxs			=mBounds.mMaxs;
			GModel.mFirstFace		=mFirstFace;
			GModel.mNumFaces		=mNumFaces;
			GModel.mFirstLeaf		=mFirstLeaf;
			GModel.mNumLeafs		=mNumLeafs;
			GModel.mFirstCluster	=mFirstCluster;
			GModel.mNumClusters		=mNumClusters;
			GModel.mAreaFront		=mAreaFront;
			GModel.mAreaBack		=mAreaBack;

			GModel.Write(bw);
		}


		internal bool CreateAreas(ref int numAreas, CoreDelegates.ModelForLeafNode mod4leaf)
		{
			return	mRootNode.CreateAreas_r(ref numAreas, mod4leaf);
		}


		internal bool FinishAreaPortals(CoreDelegates.ModelForLeafNode mod4leaf)
		{
			return	mRootNode.FinishAreaPortals_r(mod4leaf);
		}


		internal bool SaveGFXNodes_r(BinaryWriter bw)
		{
			return	mRootNode.SaveGFXNodes_r(bw);
		}


		internal bool SaveGFXFaces_r(BinaryWriter bw)
		{
			return	mRootNode.SaveGFXFaces_r(bw);
		}


		internal bool SaveGFXLeafs_r(BinaryWriter bw, List<int> gfxLeafFaces, ref int TotalLeafSize)
		{
			return	mRootNode.SaveGFXLeafs_r(bw, gfxLeafFaces, ref TotalLeafSize);
		}
	}
}