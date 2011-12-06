using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;


namespace BSPCore
{
	internal class GBSPModel
	{
		GBSPNode	[]mRootNode		=new GBSPNode[2];
		Vector3		mOrigin;
		GBSPNode	mOutsideNode	=new GBSPNode();
		Bounds		mBounds;

		//for saving, might delete
		internal int	[]mRootNodeID	=new int[2];
		internal int	mFirstFace, mNumFaces;
		internal int	mFirstLeaf, mNumLeafs;
		internal int	mFirstCluster, mNumClusters;

		//area portal stuff, probably won't use
		internal bool	mbAreaPortal;
		internal int	[]mAreas	=new int[2];

		//blockery
		Array	mBlockNodes	=Array.CreateInstance(typeof(GBSPNode), 10, 10);


		internal bool ProcessWorldModel(List<MapBrush> list, List<MapEntity> ents,
			PlanePool pool, TexInfoPool tip, bool bVerbose)
		{
			int	block_xl	=-8;
			int	block_xh	=7;
			int	block_zl	=-8;
			int	block_zh	=7;

			Bounds	modelBounds	=MapBrush.GetListBounds(list);

			list.Reverse();
			List<GBSPBrush>	glist	=GBSPBrush.ConvertMapBrushList(list);
			
			if(block_xh * 1024 > modelBounds.mMaxs.X)
			{
				block_xh	=(int)Math.Floor(modelBounds.mMaxs.X / 1024.0);
			}
			if((block_xl + 1) * 1024 < modelBounds.mMins.X)
			{
				block_xl	=(int)Math.Floor(modelBounds.mMins.X / 1024.0);
			}
			if(block_zh * 1024 > modelBounds.mMaxs.Z)
			{
				block_zh	=(int)Math.Floor(modelBounds.mMaxs.Z / 1024.0);
			}
			if((block_zl + 1) * 1024 < modelBounds.mMins.Z)
			{
				block_zl	=(int)Math.Floor(modelBounds.mMins.Z / 1024.0);
			}
			
			if(block_xl < -4)
			{
				block_xl	=-4;
			}
			if(block_zl <-4)
			{
				block_zl	=-4;
			}
			if(block_xh > 3)
			{
				block_xh	=3;
			}
			if(block_zh > 3)
			{
				block_zh	=3;
			}

			for(int i=0;i < 16;i++)
			{
				ProcessBlock(glist, pool, i, block_xl, block_xh, block_zl, block_zh);
			}

			GBSPNode	root	=GBSPNode.BlockTree(mBlockNodes, pool,
				block_xl - 1, block_zl -1, block_xh + 1, block_zh + 1);

			mBounds	=modelBounds;

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
			glist	=GBSPBrush.CSGBrushes(bVerbose, glist, pool);
			CoreEvents.FireNumPlanesChangedEvent(pool.mPlanes.Count, null);

			root.BuildBSP(glist, pool, bVerbose);
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

			root.MakeFaces(pool, tip, bVerbose);

			root.MakeLeafFaces();

			if(!root.FreePortals())
			{
				CoreEvents.Print("BuildBSP:  Could not free portals.\n");
				return	false;
			}

			root.MergeNodes(bVerbose);

			mRootNode[0]	=root;

			return	true;
		}


		internal void ProcessBlock(List<GBSPBrush> brushes, PlanePool pp, int blockNum, int block_xl, int block_xh, int block_zl, int block_zh)
		{
			int		xblock, zblock;

			zblock	=block_zl + blockNum / (block_xh - block_xl + 1);
			xblock	=block_xl + blockNum % (block_xh - block_xl + 1);

			CoreEvents.Print("############### block " + xblock + "," + zblock + " ###############\n");

			Bounds	blockBounds	=new Bounds();

			blockBounds.mMins.X	=xblock * 1024;
			blockBounds.mMins.Z	=zblock * 1024;
			blockBounds.mMins.Y	=-4096;
			blockBounds.mMaxs.X	=(xblock + 1) * 1024;
			blockBounds.mMaxs.Z	=(zblock + 1) * 1024;
			blockBounds.mMaxs.Y	=4096;

			List<GBSPBrush>	blocked	=GBSPBrush.BlockChopBrushes(brushes, blockBounds, pp);

			int	brushCount	=blocked.Count;

			if(!GBSPBrush.TestListInBounds(blocked, blockBounds))
			{
				CoreEvents.Print("Brush out of bounds after choppery!\n");
			}

			GBSPBrush.DumpBrushListToFile(blocked, "Brush_x" + xblock + "_z" + zblock + ".map");

			List<GBSPBrush>	csgList	=GBSPBrush.CSGBrushes(true, blocked, pp);			

			CoreEvents.FireNumPlanesChangedEvent(pp.mPlanes.Count, null);

			//print out brushes that are still overlapping
			GBSPBrush.DumpOverlapping(csgList, pp);

			GBSPNode	root	=new GBSPNode();
			root.BuildBSP(csgList, pp, true);
			CoreEvents.FireNumPlanesChangedEvent(pp.mPlanes.Count, null);

			mBlockNodes.SetValue(root, xblock + 5, zblock + 5);
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

			root.MakeFaces(pool, tip, bVerbose);

			if(!root.FreePortals())
			{
				CoreEvents.Print("BuildBSP:  Could not free portals.\n");
				return	false;
			}

			root.MergeNodes(bVerbose);

			mRootNode[0]	=root;

			return	true;
		}


		internal void GetTriangles(List<Vector3> verts, List<uint> indexes, bool bCheck)
		{
			mRootNode[0].GetLeafTriangles(verts, indexes, bCheck);
		}


		internal bool PrepGBSPModel(string visFile, bool bSaveVis,
			bool bVerbose, PlanePool pool, ref int numLeafClusters,
			List<GFXLeafSide> leafSides)
		{
			if(bSaveVis)
			{
				if(!mRootNode[0].CreatePortals(mOutsideNode, true, false, pool, mBounds.mMins, mBounds.mMaxs))
				{
					CoreEvents.Print("Could not create VIS portals.\n");
					return	false;
				}

				mFirstCluster	=numLeafClusters;

				if(!mRootNode[0].CreateLeafClusters(bVerbose, ref numLeafClusters))
				{
					CoreEvents.Print("Could not create leaf clusters.\n");
					return	false;
				}

				mNumClusters	=numLeafClusters - mFirstCluster;

				if(!SavePortalFile(visFile, pool, numLeafClusters))
				{
					return	false;
				}

				if(!mRootNode[0].FreePortals())
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

			if(!mRootNode[0].CreatePortals(mOutsideNode, false, false, pool, mBounds.mMins, mBounds.mMaxs))
			{
				CoreEvents.Print("Could not create REAL portals.\n");
				return	false;
			}

			if(!mRootNode[0].CreateLeafSides(pool, leafSides, bVerbose))
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

			mRootNodeID[0]	=mRootNode[0].PrepGFXNodes_r(mRootNodeID[0], nc);

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

			if(!mRootNode[0].PrepPortalFile_r(ref numPortalLeafs, ref numPortals))
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

			if(!mRootNode[0].SavePortalFile_r(bw, pool, numLeafClusters))
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
			return	mRootNode[0].GetFaceVertIndexNumbers_r(ff);
		}


		internal bool FixTJunctions(FaceFixer ff, TexInfoPool tip)
		{
			return	mRootNode[0].FixTJunctions_r(ff, tip);
		}


		internal void ConvertToGFXAndSave(BinaryWriter bw)
		{
			GFXModel	GModel	=new GFXModel();

			GModel.mRootNode[0]		=mRootNodeID[0];
			GModel.mOrigin			=mOrigin;
			GModel.mMins			=mBounds.mMins;
			GModel.mMaxs			=mBounds.mMaxs;
			GModel.mRootNode[1]		=mRootNodeID[1];
			GModel.mFirstFace		=mFirstFace;
			GModel.mNumFaces		=mNumFaces;
			GModel.mFirstLeaf		=mFirstLeaf;
			GModel.mNumLeafs		=mNumLeafs;
			GModel.mFirstCluster	=mFirstCluster;
			GModel.mNumClusters		=mNumClusters;
			GModel.mAreas[0]		=mAreas[0];
			GModel.mAreas[1]		=mAreas[1];

			GModel.Write(bw);
		}


		internal bool CreateAreas(ref int numAreas, CoreDelegates.ModelForLeafNode mod4leaf)
		{
			return	mRootNode[0].CreateAreas_r(ref numAreas, mod4leaf);
		}


		internal bool FinishAreaPortals(CoreDelegates.ModelForLeafNode mod4leaf)
		{
			return	mRootNode[0].FinishAreaPortals_r(mod4leaf);
		}


		internal bool SaveGFXNodes_r(BinaryWriter bw)
		{
			return	mRootNode[0].SaveGFXNodes_r(bw);
		}


		internal bool SaveGFXFaces_r(BinaryWriter bw)
		{
			return	mRootNode[0].SaveGFXFaces_r(bw);
		}


		internal bool SaveGFXLeafs_r(BinaryWriter bw, List<int> gfxLeafFaces, ref int TotalLeafSize)
		{
			return	mRootNode[0].SaveGFXLeafs_r(bw, gfxLeafFaces, ref TotalLeafSize);
		}
	}
}