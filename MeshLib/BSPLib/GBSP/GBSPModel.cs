using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	internal class GBSPModel
	{
		GBSPNode	[]mRootNode		=new GBSPNode[2];
		Vector3		mOrigin;
		GBSPNode	mOutsideNode	=new GBSPNode();
		Bounds		mBounds;

		//for saving, might delete
		internal int		[]mRootNodeID	=new int[2];
		internal int		mFirstFace, mNumFaces;
		internal int		mFirstLeaf, mNumLeafs;
		internal int		mFirstCluster, mNumClusters;
		internal int		mNumSolidLeafs;

		//area portal stuff, probably won't use
		internal bool	mbAreaPortal;
		internal int	[]mAreas	=new int[2];


		internal bool ProcessWorldModel(List<MapBrush> list,
			List<MapEntity> ents, PlanePool pool,
			TexInfoPool tip, bool bVerbose, EventHandler planesChanged)
		{
			list.Reverse();
			GBSPBrush	glist	=GBSPBrush.ConvertMapBrushList(list);

			glist	=GBSPBrush.CSGBrushes(bVerbose, glist, pool);
			if(planesChanged != null)
			{
				planesChanged(pool.mPlanes.Count, null);
			}

			GBSPNode	root	=new GBSPNode();
			root.BuildBSP(glist, pool, bVerbose);
			if(planesChanged != null)
			{
				planesChanged(pool.mPlanes.Count, null);
			}

			mBounds	=new Bounds(root.GetBounds());

			glist	=null;

			if(!root.CreatePortals(mOutsideNode, false, bVerbose, pool, mBounds.mMins, mBounds.mMaxs))
			{
				Map.Print("Could not create the portals.\n");
				return	false;
			}

			if(root.RemoveHiddenLeafs(mOutsideNode, ents, pool, bVerbose) == -1)
			{
				Map.Print("Failed to remove hidden leafs.\n");
			}

			root.MarkVisibleSides(list, pool, bVerbose);

			if(!root.FreePortals())
			{
				Map.Print("BuildBSP:  Could not free portals.\n");
				return	false;
			}

			root.FreeBSP_r();

			glist	=GBSPBrush.ConvertMapBrushList(list);
			glist	=GBSPBrush.CSGBrushes(bVerbose, glist, pool);
			if(planesChanged != null)
			{
				planesChanged(pool.mPlanes.Count, null);
			}

			root.BuildBSP(glist, pool, bVerbose);
			if(planesChanged != null)
			{
				planesChanged(pool.mPlanes.Count, null);
			}

			if(!root.CreatePortals(mOutsideNode, false, bVerbose, pool, mBounds.mMins, mBounds.mMaxs))
			{
				Map.Print("Could not create the portals.\n");
				return	false;
			}

			if(root.RemoveHiddenLeafs(mOutsideNode, ents, pool, bVerbose) == -1)
			{
				Map.Print("Failed to remove hidden leafs.\n");
			}

			root.MarkVisibleSides(list, pool, bVerbose);

			root.MakeFaces(pool, tip, bVerbose);

			root.MakeLeafFaces();

			if(!root.FreePortals())
			{
				Map.Print("BuildBSP:  Could not free portals.\n");
				return	false;
			}

			root.MergeNodes(bVerbose);

			mRootNode[0]	=root;

			return	true;
		}


		internal bool ProcessSubModel(List<MapBrush> list,
			PlanePool pool, TexInfoPool tip, bool bVerbose)
		{
			list.Reverse();
			GBSPBrush	glist	=GBSPBrush.ConvertMapBrushList(list);
			glist	=GBSPBrush.CSGBrushes(bVerbose, glist, pool);

			GBSPNode	root	=new GBSPNode();
			root.BuildBSP(glist, pool, bVerbose);

			mBounds			=new Bounds(root.GetBounds());

			glist	=null;

			if(!root.CreatePortals(mOutsideNode, false, bVerbose, pool, mBounds.mMins, mBounds.mMaxs))
			{
				Map.Print("Could not create the portals.\n");
				return	false;
			}

			root.MarkVisibleSides(list, pool, bVerbose);

			root.MakeFaces(pool, tip, bVerbose);

			if(!root.FreePortals())
			{
				Map.Print("BuildBSP:  Could not free portals.\n");
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
					Map.Print("Could not create VIS portals.\n");
					return	false;
				}

				mFirstCluster	=numLeafClusters;

				if(!mRootNode[0].CreateLeafClusters(bVerbose, ref numLeafClusters))
				{
					Map.Print("Could not create leaf clusters.\n");
					return	false;
				}

				mNumClusters	=numLeafClusters - mFirstCluster;

				if(!SavePortalFile(visFile, pool, numLeafClusters))
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

			if(!mRootNode[0].CreateLeafSides(pool, leafSides, bVerbose))
			{
				Map.Print("Could not create leaf sides.\n");
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

			Map.Print(" --- Save Portal File --- \n");
			  
			int	dotPos	=portalFile.LastIndexOf('.');
			portalFile	=portalFile.Substring(0, dotPos);
			portalFile	+=".gpf";

			FileStream	fs	=UtilityLib.FileUtil.OpenTitleFile(portalFile,
				FileMode.OpenOrCreate, FileAccess.Write);

			if(fs == null)
			{
				Map.Print("SavePortalFile:  Error opening " + portalFile + " for writing.\n");
				return	false;
			}

			BinaryWriter	bw	=new BinaryWriter(fs);

			int	numPortals		=0;	//Number of portals
			int	numPortalLeafs	=0;	//Current leaf number

			if(!mRootNode[0].PrepPortalFile_r(ref numPortalLeafs, ref numPortals))
			{
				bw.Close();
				fs.Close();
				Map.Print("SavePortalFile:  Could not PrepPortalFile.\n");
				return	false;
			}

			if(numPortalLeafs != mNumClusters)
			{
				bw.Close();
				fs.Close();
				Map.Print("SavePortalFile:  Invalid number of clusters!!!\n");
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

			Map.Print("Num Portals          : " + numPortals + "\n");
			Map.Print("Num Portal Leafs     : " + numPortalLeafs + "\n");

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


		internal bool CreateAreas(ref int numAreas, Map.ModelForLeafNode mod4leaf)
		{
			return	mRootNode[0].CreateAreas_r(ref numAreas, mod4leaf);
		}


		internal bool FinishAreaPortals(Map.ModelForLeafNode mod4leaf)
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
