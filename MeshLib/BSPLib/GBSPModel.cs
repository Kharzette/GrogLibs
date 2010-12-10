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


		internal bool ProcessWorldModel(List<MapBrush> list,
			List<MapEntity> ents, PlanePool pool,
			TexInfoPool tip, bool bVerbose)
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

			mGBSPBrushes	=GBSPBrush.CSGBrushes(bVerbose, mGBSPBrushes, pool);

			DumpBrushListToFile(mGBSPBrushes);

			GBSPNode	root	=new GBSPNode();
			root.BuildBSP(mGBSPBrushes, pool, bVerbose);

			mBounds	=new Bounds(root.mBounds);

			mGBSPBrushes	=null;
			prev			=null;

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

			mGBSPBrushes	=GBSPBrush.CSGBrushes(bVerbose, mGBSPBrushes, pool);

			root.BuildBSP(mGBSPBrushes, pool, bVerbose);

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

			mGBSPBrushes	=GBSPBrush.CSGBrushes(bVerbose, mGBSPBrushes, pool);

			GBSPNode	root	=new GBSPNode();
			root.BuildBSP(mGBSPBrushes, pool, bVerbose);

			mBounds			=new Bounds(root.mBounds);

			mGBSPBrushes	=null;
			prev			=null;

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


		internal bool PrepGBSPModel(string VisFile, bool SaveVis,
			bool bVerbose, PlanePool pool, ref int numLeafClusters,
			List<GFXLeafSide> leafSides)
		{
			if(SaveVis)
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

				if(!SavePortalFile(VisFile, pool, numLeafClusters))
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


		bool SavePortalFile(string FileName, PlanePool pool, int numLeafClusters)
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

			int	NumPortals		=0;	//Number of portals
			int	NumPortalLeafs	=0;	//Current leaf number

			if(!mRootNode[0].PrepPortalFile_r(ref NumPortalLeafs, ref NumPortals))
			{
				bw.Close();
				fs.Close();
				Map.Print("SavePortalFile:  Could not PrepPortalFile.\n");
				return	false;
			}

			if(NumPortalLeafs != mNumClusters)
			{
				bw.Close();
				fs.Close();
				Map.Print("SavePortalFile:  Invalid number of clusters!!!\n");
				return	false;
			}

			bw.Write("GBSP_PRTFILE");
			bw.Write(NumPortals);
			bw.Write(mNumClusters);

			if(!mRootNode[0].SavePortalFile_r(bw, pool, numLeafClusters))
			{
				bw.Close();
				fs.Close();
				return	false;
			}

			bw.Close();
			fs.Close();

			Map.Print("Num Portals          : " + NumPortals + "\n");
			Map.Print("Num Portal Leafs     : " + NumPortalLeafs + "\n");

			return	true;
		}
	}
}
