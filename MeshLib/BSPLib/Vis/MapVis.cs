using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class WorldLeaf
	{
		public Int32	mVisFrame;
		public Int32	mParent;
	}


	public partial class Map
	{
		//vis related stuff
		VISPortal	[]mVisPortals;
		VISPortal	[]mVisSortedPortals;
		VISLeaf		[]mVisLeafs;
		Int32		mNumVisLeafBytes, mNumVisPortalBytes;
		Int32		mNumVisMaterialBytes;

		//area stuff
		List<GFXArea>		mAreas		=new List<GFXArea>();
		List<GFXAreaPortal>	mAreaPorts	=new List<GFXAreaPortal>();


		void ThreadVisCB(object threadContext)
		{
			VisParameters	vp	=threadContext as VisParameters;

			GFXHeader	header	=LoadGBSPFile(vp.mFileName);
			if(header == null)
			{
				Print("PvsGBSPFile:  Could not load GBSP file: " + vp.mFileName + "\n");
				if(eVisDone != null)
				{
					eVisDone(false, null);
				}
				return;
			}
			string	PFile;

			//Clean out any old vis data
			FreeFileVisData();

			//Open the bsp file for writing
			FileStream	fs	=new FileStream(vp.mFileName,
				FileMode.OpenOrCreate, FileAccess.Write);

			BinaryWriter	bw	=null;

			if(fs == null)
			{
				Print("VisGBSPFile:  Could not open GBSP file for writing: " + vp.mFileName + "\n");
				goto	ExitWithError;
			}

			//Prepare the portal file name
			int	extPos	=vp.mFileName.LastIndexOf(".");
			PFile		=vp.mFileName.Substring(0, extPos);
			PFile		+=".gpf";
			
			//Load the portal file
			if(!LoadPortalFile(PFile))
			{
				goto	ExitWithError;
			}

			if(eNumPortalsChanged != null)
			{
				eNumPortalsChanged(mVisPortals.Length, null);
			}

			Print("NumPortals           : " + mVisPortals.Length + "\n");
			
			//Vis'em
			if(!VisAllLeafs(vp.mVisParams.mbSortPortals, vp.mVisParams.mbFullVis, vp.mBSPParams.mbVerbose))
			{
				goto	ExitWithError;
			}

			bw	=new BinaryWriter(fs);

			//Save the leafs, clusters, vis data, etc
			WriteVis(bw, header.mbHasLight, false);

			//Free all the vis stuff
			FreeAllVisData();

			//Free any remaining leftover bsp data
			FreeGBSPFile();

			bw.Close();
			fs.Close();
			bw	=null;
			fs	=null;
			
			if(eVisDone != null)
			{
				eVisDone(true, null);
			}
			return;

			// ==== ERROR ====
			ExitWithError:
			{
				Print("PvsGBSPFile:  Could not vis the file: " + vp.mFileName + "\n");

				if(bw != null)
				{
					bw.Close();
				}
				if(fs != null)
				{
					fs.Close();
				}

				FreeAllVisData();
				FreeGBSPFile();

				if(eVisDone != null)
				{
					eVisDone(false, null);
				}
				return;
			}
		}


		public void VisGBSPFile(string fileName, VisParams prms, BSPBuildParams prms2)
		{
			VisParameters	vp	=new VisParameters();
			vp.mBSPParams	=prms2;
			vp.mVisParams	=prms;
			vp.mFileName	=fileName;

			ThreadPool.QueueUserWorkItem(ThreadVisCB, vp);
		}


		bool MaterialVisGBSPFile(string fileName, VisParams prms, BSPBuildParams prms2)
		{
			Print(" --- Material Vis GBSP File --- \n");

			GFXHeader	header	=LoadGBSPFile(fileName);
			if(header == null)
			{
				Print("PvsGBSPFile:  Could not load GBSP file: " + fileName + "\n");
				return	false;
			}

			//make sure it is lit
			if(mGFXLightData == null)
			{
				Print("Map needs to be lit before material vis can work properly.\n");
				return	false;
			}

			//Open the bsp file for writing
			FileStream	fs	=new FileStream(fileName,
				FileMode.OpenOrCreate, FileAccess.Write);

			BinaryWriter	bw	=null;

			if(fs == null)
			{
				Print("MatVisGBSPFile:  Could not open GBSP file for writing: " + fileName + "\n");
				goto	ExitWithError;
			}

			//make a material vis, what materials
			//can be seen from each leaf
			VisMaterials();

			//Save the leafs, clusters, vis data, etc
			bw	=new BinaryWriter(fs);
			WriteVis(bw, header.mbHasLight, true);

			//Free all the vis stuff
			FreeAllVisData();

			//Free any remaining leftover bsp data
			FreeGBSPFile();

			bw.Close();
			fs.Close();
			bw	=null;
			fs	=null;
			
			return	true;

			// ==== ERROR ====
			ExitWithError:
			{
				Print("MatPvsGBSPFile:  Could not vis the file: " + fileName + "\n");

				if(bw != null)
				{
					bw.Close();
				}
				if(fs != null)
				{
					fs.Close();
				}

				FreeAllVisData();
				FreeGBSPFile();

				return	false;
			}
		}


		public bool IsMaterialVisibleFromPos(Vector3 pos, int matIndex)
		{
			if(mGFXNodes == null)
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


		public bool IsMaterialVisible(int leaf, int matIndex)
		{
			if(mGFXLeafs == null)
			{
				return	false;
			}

			int	clust	=mGFXLeafs[leaf].mCluster;

			if(clust == -1 || mGFXClusters[clust].mVisOfs == -1
				|| mGFXMaterialVisData == null)
			{
				return	true;	//this will make everything vis
								//when outside of the map
			}

			//plus one to avoid 0 problem
			matIndex++;

			int	ofs	=leaf * mNumVisMaterialBytes;
			
			return	((mGFXMaterialVisData[ofs + (matIndex >> 3)] & (1 << (matIndex & 7))) != 0);
		}


		public void VisMaterials()
		{
			Dictionary<Int32, List<string>>	visibleMaterials
				=new Dictionary<Int32, List<string>>();

			if(mGFXLeafs == null)
			{
				return;
			}

			Print("Computing visible materials from each leaf...\n");

			//make a temporary mapgrinder to help sync
			//up material names and indexes and such
			MapGrinder	mg	=new MapGrinder(null, mGFXTexInfos, mGFXFaces, mLightMapGridSize, 1);

			object	prog	=ProgressWatcher.RegisterProgress(0, mGFXLeafs.Length, 0);

			for(int leaf=0;leaf < mGFXLeafs.Length;leaf++)
			{
				ProgressWatcher.UpdateProgress(prog, leaf);

				int	clust	=mGFXLeafs[leaf].mCluster;
				if(clust == -1)
				{
					continue;
				}

				int	ofs		=mGFXClusters[clust].mVisOfs;
				if(ofs == -1)
				{
					continue;
				}

				visibleMaterials.Add(leaf, new List<string>());

				List<int>	visibleClusters	=new List<int>();

				//Mark all visible clusters
				for(int i=0;i < mGFXModels[0].mNumClusters;i++)
				{
					if((mGFXVisData[ofs + (i >> 3)] & (1 << (i & 7))) != 0)
					{
						visibleClusters.Add(i);
					}
				}

				for(int i=0;i < mGFXModels[0].mNumLeafs;i++)
				{
					GFXLeaf	checkLeaf	=mGFXLeafs[mGFXModels[0].mFirstLeaf + i];
					int		checkClust	=checkLeaf.mCluster;

					if(checkClust == -1 || !visibleClusters.Contains(checkClust))
					{
						continue;
					}
					for(int k=0;k < checkLeaf.mNumFaces;k++)
					{
						GFXFace	f	=mGFXFaces[mGFXLeafFaces[k + checkLeaf.mFirstFace]];

						string	matName	=MapGrinder.ScryTrueName(f, mGFXTexInfos[f.mTexInfo]);

						if(!visibleMaterials[leaf].Contains(matName))
						{
							visibleMaterials[leaf].Add(matName);
						}
					}
				}
			}

			ProgressWatcher.Clear();

			//grab list of material names
			List<string>	matNames	=mg.GetMaterialNames();

			//alloc compressed bytes
			mNumVisMaterialBytes	=((matNames.Count + 63) & ~63) >> 3;

			mGFXMaterialVisData	=new byte[mGFXLeafs.Length * mNumVisMaterialBytes];

			//compress
			foreach(KeyValuePair<Int32, List<string>> visMat in visibleMaterials)
			{
				foreach(string mname in visMat.Value)
				{
					//zero doesn't or very well, so + 1 here
					int	idx	=matNames.IndexOf(mname) + 1;
					mGFXMaterialVisData[visMat.Key * mNumVisMaterialBytes + (idx >> 3)]
						|=(byte)(1 << (idx & 7));
				}
			}
			Print("Material Vis Complete:  " + mGFXMaterialVisData.Length + " bytes.\n");
		}


		bool CreateAreas(GBSPModel worldModel, NodeCounter nc)
		{
			Print(" --- Create Area Leafs --- \n");

			//Clear all model area info
			foreach(GBSPModel mod in mModels)
			{
				mod.mAreas[0]		=mod.mAreas[1]	=0;
				mod.mbAreaPortal	=false;
			}

			int	numAreas	=1;

			if(!worldModel.CreateAreas(ref numAreas, GetModelForLeafNode))
			{
				Map.Print("Could not create model areas.\n");
				return	false;
			}

			if(!worldModel.FinishAreaPortals(GetModelForLeafNode))
			{
				Map.Print("CreateAreas: FinishAreaPortals_r failed.\n");
				return	false;
			}

			if(!FinishAreas(numAreas))
			{
				Map.Print("Could not finalize model areas.\n");
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
					Print("*WARNING* FinishAreas:  AreaPortal did not touch any areas!\n");
				}
				else if(mModels[i].mAreas[1] == 0)
				{
					Print("*WARNING* FinishAreas:  AreaPortal only touched one area.\n");
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
						Print("FinishAreas:  Max area portals.\n");
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


		bool VisAllLeafs(bool bSortPortals,	bool bFullVis, bool bVerbose)
		{
			//create a dictionary to map a vis portal back to an index
			Dictionary<VISPortal, Int32>	portIndexer	=new Dictionary<VISPortal, Int32>();
			for(int i=0;i < mVisPortals.Length;i++)
			{
				portIndexer.Add(mVisPortals[i], i);
			}

			Map.Print("Quick vis for " + mVisPortals.Length + " portals...\n");

			//Create PortalSeen array.  This is used by Vis flooding routines
			//This is deleted below...
			bool	[]portalSeen	=new bool[mVisPortals.Length];

			object	prog	=ProgressWatcher.RegisterProgress(0, mVisLeafs.Length, 0);

			//Flood all the leafs with the fast method first...
			for(int i=0;i < mVisLeafs.Length; i++)
			{
				FloodLeafPortalsFast(i, portalSeen, portIndexer);

				ProgressWatcher.UpdateProgress(prog, i);
			}
			ProgressWatcher.Clear();

			//Sort the portals with MightSee
			if(bSortPortals)
			{
				SortPortals();
			}

			if(bFullVis)
			{
				prog	=ProgressWatcher.RegisterProgress(0, mVisPortals.Length, 0);
				if(!FloodPortalsSlow(portIndexer, 0, mVisPortals.Length, bVerbose, prog))
				{
					return	false;
				}
			}

			ProgressWatcher.Clear();

			//Don't need this anymore...
			portalSeen	=null;

			mGFXVisData	=new byte[mVisLeafs.Length * mNumVisLeafBytes];
			if(mGFXVisData == null)
			{
				Print("VisAllLeafs:  Out of memory for LeafVisBits.\n");
				goto	ExitWithError;
			}

			int	TotalVisibleLeafs	=0;

			for(int i=0;i < mVisLeafs.Length;i++)
			{
				int	leafSee	=0;
				
				if(!CollectLeafVisBits(i, ref leafSee))
				{
					goto	ExitWithError;
				}
				TotalVisibleLeafs	+=leafSee;
			}

			Print("Total visible areas           : " + TotalVisibleLeafs + "\n");
			Print("Average visible from each area: " + TotalVisibleLeafs / mVisLeafs.Length + "\n");

			return	true;

			// ==== ERROR ====
			ExitWithError:
			{
				// Free all the global vis data
				FreeAllVisData();

				return	false;
			}
		}


		bool FloodPortalsSlow(Dictionary<VISPortal, Int32> visIndexer,
			int startPort, int endPort, bool bVerbose, object prog)
		{
			for(int k=startPort;k < endPort;k++)
			{
				mVisPortals[k].mbDone	=false;
			}

//			Parallel.For(startPort, endPort, (k) =>
			for(int k=startPort;k < endPort;k++)
			{
				if(prog != null)
				{
					ProgressWatcher.UpdateProgress(prog, k);
				}

				VISPortal	port	=mVisSortedPortals[k];
				
				port.mFinalVisBits	=new byte[mNumVisPortalBytes];

				//This portal can't see anyone yet...
				for(int i=0;i < mNumVisPortalBytes;i++)
				{
					port.mFinalVisBits[i]	=0;
				}

				int	CanSee	=0;

				VISPStack	portStack	=new VISPStack();				
				for(int i=0;i < mNumVisPortalBytes;i++)
				{
					portStack.mVisBits[i]	=port.mVisBits[i];
				}

				//Setup Source/Pass
				portStack.mSource	=new GBSPPoly(port.mPoly);
				portStack.mPass		=null;

				if(!port.FloodPortalsSlow_r(port, portStack, visIndexer, ref CanSee, mVisLeafs))
				{
					return	false;
				}

				portStack.mSource	=null;
				port.mbDone			=true;

				if(bVerbose)
				{
					Print("Portal: " + (k + 1) + " - Fast Vis: "
						+ port.mMightSee + ", Full Vis: "
						+ port.mCanSee + "\n");
				}
			}//);
			return	true;
		}


		bool CollectLeafVisBits(int leafNum, ref int leafSee)
		{
			VISPortal	sport;
			VISLeaf		Leaf;
			Int32		k, Bit, SLeaf, LeafBitsOfs;
			
			Leaf	=mVisLeafs[leafNum];

			LeafBitsOfs	=leafNum * mNumVisLeafBytes;

			byte	[]portalBits	=new byte[mNumVisPortalBytes];

			if(!VISPortal.CollectBits(Leaf.mPortals, portalBits))
			{
				return	false;
			}

			// Take this list, and or all leafs that each visible portal looks in to
			for(k=0;k < mVisPortals.Length;k++)
			{
				if((portalBits[k >> 3] & (1 << (k & 7))) != 0)
				{
					sport	=mVisPortals[k];
					SLeaf	=sport.mLeaf;
					Debug.Assert((1 << (SLeaf & 7)) < 256);
					mGFXVisData[LeafBitsOfs + (SLeaf >> 3)]	|=(byte)(1 << (SLeaf & 7));
				}
			}
					
			Bit	=1 << (leafNum & 7);

			Debug.Assert(Bit < 256);

			//He should not have seen himself (yet...)
			if((mGFXVisData[LeafBitsOfs + (leafNum >> 3)] & Bit) != 0)
			{
				Map.Print("*WARNING* CollectLeafVisBits:  Leaf:" + leafNum + " can see himself!\n");
			}
			mGFXVisData[LeafBitsOfs + (leafNum >> 3)]	|=(byte)Bit;

			for(k=0;k < mVisLeafs.Length;k++)
			{
				Bit	=(1 << (k & 7));

				if((mGFXVisData[LeafBitsOfs + (k>>3)] & Bit) != 0)
				{
					leafSee++;
				}
			}

			if(leafSee == 0)
			{
				Map.Print("CollectLeafVisBits:  Leaf can see nothing.\n");
				return	false;
			}

			mGFXClusters[leafNum].mVisOfs	=LeafBitsOfs;

			return	true;
		}


		void SortPortals()
		{
			List<VISPortal>	sortMe	=new List<VISPortal>(mVisPortals);

			sortMe.Sort(new VisPortalComparer());

			mVisSortedPortals	=sortMe.ToArray();
		}


		void FloodLeafPortalsFast(int leafNum, bool []portSeen,
			Dictionary<VISPortal, Int32> visIndexer)
		{
			VISLeaf	leaf	=mVisLeafs[leafNum];

			if(leaf.mPortals == null)
			{
				//GHook.Printf("*WARNING* FloodLeafPortalsFast:  Leaf with no portals.\n");
				return;
			}
			
			int	srcLeaf	=leafNum;

			for(VISPortal port=leaf.mPortals;port != null;port=port.mNext)
			{
				port.mVisBits	=new byte[mNumVisPortalBytes];

				//This portal can't see anyone yet...
				for(int i=0;i < mNumVisPortalBytes;i++)
				{
					port.mVisBits[i]	=0;
				}
				for(int i=0;i < mVisPortals.Length;i++)
				{
					portSeen[i]	=false;
				}

				int	mightSee	=0;
				
				port.FloodPortalsFast_r(port, visIndexer,
					portSeen, mVisLeafs, srcLeaf, ref mightSee);
			}
		}


		void FreeFileVisData()
		{
			mGFXVisData			=null;
			mGFXMaterialVisData	=null;
		}


		void FreeAllVisData()
		{
			mGFXVisData			=null;
			mGFXMaterialVisData	=null;

			if(mVisPortals != null)
			{
				for(int i=0;i < mVisPortals.Length;i++)
				{
					mVisPortals[i].mPoly			=null;
					mVisPortals[i].mFinalVisBits	=null;
					mVisPortals[i].mVisBits		=null;
				}

				mVisPortals	=null;
			}
			mVisPortals			=null;
			mVisSortedPortals	=null;
			mVisLeafs			=null;

			FreeGBSPFile();	//Free rest of GBSP GFX data
		}


		bool LoadPortalFile(string portFile)
		{
			FileStream	fs	=new FileStream(portFile,
				FileMode.Open, FileAccess.Read);

			BinaryReader	br	=null;

			if(fs == null)		// opps
			{
				Print("LoadPortalFile:  Could not open " + portFile + " for reading.\n");
				goto	ExitWithError;
			}

			br	=new BinaryReader(fs);
			
			// 
			//	Check the TAG
			//
			string	TAG	=br.ReadString();
			if(TAG != "GBSP_PRTFILE")
			{
				Print("LoadPortalFile:  " + portFile + " is not a GBSP Portal file.\n");
				goto	ExitWithError;
			}

			//
			//	Get the number of portals
			//
			int	NumVisPortals	=br.ReadInt32();
			if(NumVisPortals >= VISPStack.MAX_TEMP_PORTALS)
			{
				Print("LoadPortalFile:  Max portals for temp buffers.\n");
				goto	ExitWithError;
			}
			
			mVisPortals	=new VISPortal[NumVisPortals];
			if(mVisPortals == null)
			{
				Print("LoadPortalFile:  Out of memory for VisPortals.\n");
				goto	ExitWithError;
			}
			
			mVisSortedPortals	=new VISPortal[NumVisPortals];
			if(mVisSortedPortals == null)
			{
				Print("LoadPortalFile:  Out of memory for VisSortedPortals.\n");
				goto ExitWithError;
			}

			//
			//	Get the number of leafs
			//
			int	NumVisLeafs	=br.ReadInt32();
			if(NumVisLeafs > mGFXLeafs.Length)
			{
				goto	ExitWithError;
			}
			
			mVisLeafs	=new VISLeaf[NumVisLeafs];
			if(mVisLeafs == null)
			{
				Print("LoadPortalFile:  Out of memory for VisLeafs.\n");
				goto ExitWithError;
			}

			//fill arrays with blank objects
			for(int i=0;i < NumVisLeafs;i++)
			{
				mVisLeafs[i]	=new VISLeaf();
			}

			//
			//	Load in the portals
			//
			for(int i=0;i < NumVisPortals;i++)
			{
				//alloc blank portal
				mVisPortals[i]	=new VISPortal();

				int	numVerts	=br.ReadInt32();

				GBSPPoly	poly	=new GBSPPoly();

				for(int j=0;j < numVerts;j++)
				{
					Vector3	vert;
					vert.X	=br.ReadSingle();
					vert.Y	=br.ReadSingle();
					vert.Z	=br.ReadSingle();

					poly.AddVert(vert);
				}

				int	leafFrom	=br.ReadInt32();
				int	leafTo		=br.ReadInt32();
				
				if(leafFrom >= NumVisLeafs || leafFrom < 0)
				{
					Print("LoadPortalFile:  Invalid LeafFrom: " + leafFrom + "\n");
					goto	ExitWithError;
				}

				if(leafTo >= NumVisLeafs || leafTo < 0)
				{
					Print("LoadPortalFile:  Invalid LeafTo: " + leafTo + "\n");
					goto	ExitWithError;
				}

				VISLeaf		leaf	=mVisLeafs[leafFrom];
				VISPortal	port	=mVisPortals[i];

				port.mPoly	=poly;
				port.mLeaf	=leafTo;
				port.mPlane	=new GBSPPlane(poly);
				port.mNext	=leaf.mPortals;
				leaf.mPortals	=port;

				port.CalcPortalInfo();
			}
			
			mNumVisLeafBytes	=((NumVisLeafs+63)&~63) >> 3;
			mNumVisPortalBytes	=((NumVisPortals+63)&~63) >> 3;

			br.Close();
			fs.Close();
			br	=null;
			fs	=null;

			return	true;

			// ==== ERROR ===
			ExitWithError:
			{
				if(br != null)
				{
					br.Close();
				}
				if(fs != null)
				{
					fs.Close();
				}

				mVisPortals			=null;
				mVisSortedPortals	=null;
				mVisLeafs			=null;

				return	false;
			}
		}
	}
}
