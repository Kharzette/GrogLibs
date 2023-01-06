using System;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using BSPCore;
using UtilityLib;


namespace BSPVis
{
	internal struct FlowParams
	{
		internal VISPortal	mDestPort;
		internal int		mLeafNum;
		internal VISPStack	mPrevStack;
		internal int		mNumVisPortalBytes;
	}

	public class VisParameters
	{
		public BSPBuildParams	mBSPParams;
		public VisParams		mVisParams;
		public string			mFileName;
		public BinaryWriter		mBSPFile;
	}

	public class WorldLeaf
	{
		public Int32	mVisFrame;
		public Int32	mParent;
	}

	public class VisState
	{
		public byte	[]mVisData;
		public int	mStartPort;
		public int	mEndPort;
		public int	mTotalPorts;
	}


	public partial class VisMap
	{
		//bspmap
		Map	mMap	=new Map();

		//vis related stuff
		GFXLeaf		[]mGFXLeafs;
		GFXCluster	[]mGFXClusters;
		VISPortal	[]mVisPortals;
		VISPortal	[]mVisSortedPortals;
		VISLeaf		[]mVisLeafs;
		Int32		mNumVisLeafBytes, mNumVisPortalBytes;
#if !X64
		Int32		mNumVisMaterialBytes;
#endif

		//compiled vis data
		byte	[]mGFXVisData;
		byte	[]mGFXMaterialVisData;

		//area stuff
		List<GFXArea>		mAreas		=new List<GFXArea>();
		List<GFXAreaPortal>	mAreaPorts	=new List<GFXAreaPortal>();

		//threading
		TaskScheduler	mTaskSched	=TaskScheduler.Default;


		void ThreadVisCB(object threadContext)
		{
			VisParameters	vp	=threadContext as VisParameters;

			GFXHeader	header	=mMap.LoadGBSPFile(vp.mFileName);
			if(header == null)
			{
				CoreEvents.Print("PvsGBSPFile:  Could not load GBSP file: " + vp.mFileName + "\n");
				CoreEvents.FireVisDoneEvent(false, null);
				return;
			}

			//copy out vis related stuff
			mGFXLeafs		=mMap.GetGFXLeafs();
			mGFXClusters	=mMap.GetGFXClusters();

			//Clean out any old vis data
			FreeFileVisData();

			string	visExt	=FileUtil.StripExtension(vp.mFileName);

			visExt	+=".VisData";

			//Open the vis file for writing
			FileStream	fs	=new FileStream(visExt, FileMode.Create, FileAccess.Write);

			BinaryWriter	bw	=null;

			if(fs == null)
			{
				CoreEvents.Print("VisGBSPFile:  Could not open VisData file for writing: " + visExt + "\n");
				goto	ExitWithError;
			}

			//Prepare the portal file name
			string	PFile;
			int	extPos	=vp.mFileName.LastIndexOf(".");
			PFile		=vp.mFileName.Substring(0, extPos);
			PFile		+=".gpf";
			
			//Load the portal file
			if(!LoadPortalFile(PFile, true))
			{
				goto	ExitWithError;
			}

			CoreEvents.FireNumPortalsChangedEvent(mVisPortals.Length, null);

			CoreEvents.Print("NumPortals\t: " + mVisPortals.Length + "\n");

			DateTime	startTime	=DateTime.Now;

			CoreEvents.Print("Starting vis at " + startTime + "\n");
			
			//Vis'em
			if(!VisAllLeafs(vp.mFileName, vp))
			{
				goto	ExitWithError;
			}

			bw	=new BinaryWriter(fs);

			WriteVis(bw);

			//Free all the vis stuff
			FreeAllVisData();

			//Free any remaining leftover bsp data
			mMap.FreeGBSPFile();

			bw.Close();
			fs.Close();
			bw	=null;
			fs	=null;

			DateTime	done	=DateTime.Now;

			CoreEvents.Print("Finished vis at " + done + "\n");
			CoreEvents.Print(done - startTime + " elapsed\n");

			CoreEvents.FireVisDoneEvent(true, null);
			return;

			// ==== ERROR ====
			ExitWithError:
			{
				CoreEvents.Print("PvsGBSPFile:  Could not vis the file: " + vp.mFileName + "\n");

				if(bw != null)
				{
					bw.Close();
				}
				if(fs != null)
				{
					fs.Close();
				}

				FreeAllVisData();
				mMap.FreeGBSPFile();

				CoreEvents.FireVisDoneEvent(false, null);
				return;
			}
		}


		public void VisGBSPFile(string fileName,
			VisParams prms, BSPBuildParams prms2)
		{
			VisParameters	vp	=new VisParameters();
			vp.mBSPParams	=prms2;
			vp.mVisParams	=prms;
			vp.mFileName	=fileName;

			ThreadPool.QueueUserWorkItem(ThreadVisCB, vp);
		}


		public void SetMap(Map map)
		{
			mMap	=map;

			//copy out vis related stuff
			mGFXLeafs		=mMap.GetGFXLeafs();
			mGFXClusters	=mMap.GetGFXClusters();
		}


		public void SaveVisZoneData(BinaryWriter bw)
		{
			FileUtil.WriteArray(mGFXVisData, bw);
			FileUtil.WriteArray(mGFXMaterialVisData, bw);
			FileUtil.WriteArray(mGFXClusters, bw); 
		}


		//good for testing, compares 2 vis files
		//returns true if they are exactly the same
		//note that if you use threading, they won't be, as
		//the order of portal completion is semi random
		public int CompareVisData(string fn1, string fn2, bool bMaterial)
		{
			FileStream		fs	=new FileStream(fn1, FileMode.Open, FileAccess.Read);
			BinaryReader	br	=new BinaryReader(fs);

			int	differences	=0;

			UInt32	magic	=br.ReadUInt32();

			if(magic != 0x715da7aa)
			{
				return	++differences;
			}

			byte	[]visData1	=FileUtil.ReadByteArray(br);
			byte	[]matData1	=FileUtil.ReadByteArray(br);

			br.Close();
			fs.Close();

			fs	=new FileStream(fn2, FileMode.Open, FileAccess.Read);
			br	=new BinaryReader(fs);

			magic	=br.ReadUInt32();
			if(magic != 0x715da7aa)
			{
				return	++differences;
			}

			byte	[]visData2	=FileUtil.ReadByteArray(br);
			byte	[]matData2	=FileUtil.ReadByteArray(br);

			br.Close();
			fs.Close();

			if(!bMaterial)
			{
				if(visData1.Length != visData2.Length)
				{
					differences++;
				}
				for(int i=0;i < visData1.Length;i++)
				{
					if(visData1[i] != visData2[i])
					{
						differences++;
					}
				}
			}
			else
			{
				if(matData1.Length != matData2.Length)
				{
					differences++;
				}
				for(int i=0;i < matData1.Length;i++)
				{
					if(matData1[i] != matData2[i])
					{
						differences++;
					}
				}
			}
			return	differences;
		}


		public bool LoadVisData(string fileName)
		{
			string	visExt	=FileUtil.StripExtension(fileName);

			visExt	+=".VisData";

			if(!File.Exists(visExt))
			{
				return	false;
			}

			FileStream		fs	=new FileStream(visExt, FileMode.Open, FileAccess.Read);
			BinaryReader	br	=new BinaryReader(fs);

			UInt32	magic	=br.ReadUInt32();

			if(magic != 0x715da7aa)
			{
				return	false;
			}

			mGFXVisData			=FileUtil.ReadByteArray(br);
			mGFXMaterialVisData	=FileUtil.ReadByteArray(br);

			//load clusters
			mGFXClusters	=FileUtil.ReadArray<GFXCluster>(br);

			br.Close();
			fs.Close();

			return	true;
		}


		public int GetDebugClusterGeometry(int clust, Random rnd,
			List<Vector3> verts, List<Vector3> norms,
			List<Color> colors, List<UInt16> inds,
			List<Int32> portNums)
		{
			if(clust >= mVisLeafs.Length || clust < 0)
			{
				return	0;
			}

			Color	clustColor	=Mathery.RandomColor(rnd);

			foreach(VISPortal vp in mVisLeafs[clust].mPortals)
			{
				vp.mPoly.GetTriangles(vp.mPlane, clustColor, verts, norms, colors, inds, false);

				norms.Add(vp.mCenter);
				norms.Add(vp.mCenter + (vp.mPlane.mNormal * 25.0f));

				portNums.Add(vp.mPortNum);
			}

			return	mVisLeafs[clust].mPortals.Count;
		}


#if !X64
		public void SetMaterialVisBytes(Int32 matCount)
		{
			mNumVisMaterialBytes	=((matCount + 63) & ~63) >> 3;
		}


		public bool MaterialVisGBSPFile(string fileName, GraphicsDevice gd)
		{
			CoreEvents.Print(" --- Material Vis GBSP File --- \n");

			GFXHeader	header	=mMap.LoadGBSPFile(fileName);
			if(header == null)
			{
				CoreEvents.Print("PvsGBSPFile:  Could not load GBSP file: " + fileName + "\n");
				return	false;
			}

			if(!header.mbHasVis)
			{
				CoreEvents.Print("PvsGBSPFile:  No vis data for: " + fileName + "\n");
				return	false;
			}

			//make sure it is lit
			if(!mMap.HasLightData())
			{
				CoreEvents.Print("Map needs to be lit before material vis can work properly.\n");
				return	false;
			}

			//copy out vis related stuff
			mGFXLeafs		=mMap.GetGFXLeafs();
			mGFXClusters	=mMap.GetGFXClusters();

			if(!LoadVisData(fileName))
			{
				CoreEvents.Print("Map needs vis data for material vis to work.\n");
				return	false;
			}

			string	visExt	=FileUtil.StripExtension(fileName);

			visExt	+=".VisData";

			//Open the bsp file for writing
			FileStream	fs	=new FileStream(visExt, FileMode.Open, FileAccess.Write);

			BinaryWriter	bw	=null;

			if(fs == null)
			{
				CoreEvents.Print("MatVisGBSPFile:  Could not open VisData file for writing: " + fileName + "\n");
				goto	ExitWithError;
			}

			//make a material vis, what materials
			//can be seen from each leaf
			VisMaterials(gd, mMap.CalcMaterialNames());

			//Save the leafs, clusters, vis data, etc
			bw	=new BinaryWriter(fs);
			WriteVis(bw);

			//Free all the vis stuff
			FreeAllVisData();

			//Free any remaining leftover bsp data
			mMap.FreeGBSPFile();

			bw.Close();
			fs.Close();
			bw	=null;
			fs	=null;
			
			return	true;

			// ==== ERROR ====
			ExitWithError:
			{
				CoreEvents.Print("MatPvsGBSPFile:  Could not vis the file: " + fileName + "\n");

				if(bw != null)
				{
					bw.Close();
				}
				if(fs != null)
				{
					fs.Close();
				}

				FreeAllVisData();
				mMap.FreeGBSPFile();

				return	false;
			}
		}


		public bool IsMaterialVisibleFromPos(Vector3 pos, int matIndex)
		{
			Int32	node	=mMap.FindNodeLandedIn(0, pos);
			if(node >= 0)
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

			//this can happen if old vis data is left around
			if(clust >= mGFXClusters.Length)
			{
				return	true;	//generally just want everything to draw if testing without vis
			}

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


		public void VisMaterials(GraphicsDevice gd, List<string> matNames)
		{
			Dictionary<Int32, List<string>>	visibleMaterials
				=new Dictionary<Int32, List<string>>();

			if(mGFXLeafs == null)
			{
				return;
			}

			CoreEvents.Print("Computing visible materials from each leaf...\n");

			//Grab map stuff needed to compute this
			int			firstLeaf	=mMap.GetWorldFirstLeaf();

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
				for(int i=0;i < mGFXClusters.Length;i++)
				{
					if((mGFXVisData[ofs + (i >> 3)] & (1 << (i & 7))) != 0)
					{
						visibleClusters.Add(i);
					}
				}

				for(int i=0;i < mGFXLeafs.Length;i++)
				{
					GFXLeaf	checkLeaf	=mGFXLeafs[firstLeaf + i];
					int		checkClust	=checkLeaf.mCluster;

					if(checkClust == -1 || !visibleClusters.Contains(checkClust))
					{
						continue;
					}
					for(int k=0;k < checkLeaf.mNumFaces;k++)
					{
						string	matName	=mMap.GetMaterialNameForLeafFace(k + checkLeaf.mFirstFace);

						if(!visibleMaterials[leaf].Contains(matName))
						{
							visibleMaterials[leaf].Add(matName);
						}
					}
				}
			}

			ProgressWatcher.Clear();

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

			CoreEvents.Print("Material Vis Complete:  " + mGFXMaterialVisData.Length + " bytes.\n");
		}
#endif


		void WriteVis(BinaryWriter bw)
		{
			//write out vis file tag
			UInt32	magic	=0x715da7aa;
			bw.Write(magic);

			SaveVisZoneData(bw);
		}
		

		void PortalFlow(int portalnum, VisPools vp)
		{
			VISPortal	p	=mVisSortedPortals[portalnum];

			p.mbDone	=false;

			p.mMightSee	=CountBits(p.mPortalFlood, mVisSortedPortals.Length);

			VISPStack	vps	=vp.mStacks.GetFreeItem();
			vps.mSource		=p.mPoly;
			p.mPortalFlood.CopyTo(vps.mVisBits, 0);

			FlowParams	fp;
			fp.mDestPort			=p;
			fp.mLeafNum				=p.mClusterTo;
			fp.mPrevStack			=vps;
			fp.mNumVisPortalBytes	=mNumVisPortalBytes;
			VISPortal.RecursiveLeafFlow(fp, vp);

			p.mbDone	=true;
			p.mCanSee	=CountBits(p.mPortalVis, mVisSortedPortals.Length);

			vp.mStacks.FlagFreeItem(vps);
		}


		static void	PortalFlow(int portalnum, VISPortal []visPortals,
			VISLeaf []visLeafs, int numVisPortalBytes)
		{
			VISPortal	p	=visPortals[portalnum];

			p.mbDone	=false;

			p.mMightSee	=CountBits(p.mPortalFlood, visPortals.Length);

			VisPools	vp	=new VisPools(visLeafs, new ClipPools());
			VISPStack	vps	=vp.mStacks.GetFreeItem();
			vps.mSource		=p.mPoly;
			p.mPortalFlood.CopyTo(vps.mVisBits, 0);


			FlowParams	fp;
			fp.mDestPort			=p;
			fp.mLeafNum				=p.mClusterTo;
			fp.mPrevStack			=vps;
			fp.mNumVisPortalBytes	=numVisPortalBytes;
			VISPortal.RecursiveLeafFlow(fp, vp);

			p.mbDone	=true;
			p.mCanSee	=CountBits(p.mPortalVis, visPortals.Length);

			vp.mStacks.FlagFreeItem(vps);

			Console.WriteLine("Portal: " + p.mPortNum +
				"\tRoughVis: " + p.mMightSee
				+ "\tFullVis: " + p.mCanSee
				+ "\tIterations: " + vp.mIterations);
		}


		bool VisAllLeafs(string fileName, VisParameters vp)
		{
			CoreEvents.Print("Rough vis for " + mVisPortals.Length + " portals...\n");

			object	prog	=ProgressWatcher.RegisterProgress(0, mVisPortals.Length, 0);

			for(int i=0;i < mVisPortals.Length; i++)
			{
				PortalFacingVis(i);
				ProgressWatcher.UpdateProgress(prog, i);
			}
			ProgressWatcher.Clear();

			//Sort the portals with MightSee
			if(vp.mVisParams.mbSortPortals)
			{
				SortPortals();
			}
			else
			{
				mVisSortedPortals	=mVisPortals;
			}

			if(vp.mVisParams.mbFullVis)
			{
				CoreEvents.Print("Full vis for " + mVisPortals.Length + " portals...\n");
				prog	=ProgressWatcher.RegisterProgress(0, mVisPortals.Length, 0);
				if(!PortalFlow(0, mVisPortals.Length, vp.mBSPParams, prog))
				{
					return	false;
				}
			}
			ProgressWatcher.Clear();

			mGFXVisData	=new byte[mVisLeafs.Length * mNumVisLeafBytes];
			if(mGFXVisData == null)
			{
				CoreEvents.Print("VisAllLeafs:  Out of memory for LeafVisBits.\n");
				goto	ExitWithError;
			}

			//null out full vis arrays if no full vis
			if(!vp.mVisParams.mbFullVis)
			{
				foreach(VISPortal vsp in mVisPortals)
				{
					vsp.mPortalVis	=null;
				}
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

			CoreEvents.Print("Total visible areas\t\t: " + TotalVisibleLeafs + "\n");

			if(mVisLeafs.Length > 0)
			{
				CoreEvents.Print("Average visible from each area\t: " + TotalVisibleLeafs / mVisLeafs.Length + "\n");
			}

			return	true;

			// ==== ERROR ====
			ExitWithError:
			{
				// Free all the global vis data
				FreeAllVisData();

				return	false;
			}
		}


		bool PortalFlow(int startPort, int endPort, BSPBuildParams bp, object prog)
		{
			for(int k=startPort;k < endPort;k++)
			{
				mVisPortals[k].mbDone	=false;
			}

			int	count	=startPort;

			ParallelOptions	po			=new ParallelOptions();
			po.MaxDegreeOfParallelism	=bp.mMaxThreads;

			Parallel.For(startPort, endPort, po, (k) =>
			{
				VISPortal	port	=mVisSortedPortals[k];
				ClipPools	cp		=new ClipPools();
				VisPools	vp		=new VisPools(mVisLeafs, cp);
				
				//This portal can't see anyone yet...
				for(int i=0;i < mNumVisPortalBytes;i++)
				{
					port.mPortalVis[i]	=0;
				}
				PortalFlow(k, vp);

				port.mbDone			=true;

				Interlocked.Increment(ref count);
				if(count % 10 == 0 && prog != null)
				{
					ProgressWatcher.UpdateProgress(prog, count);
				}

				if(bp.mbVerbose)
				{
					CoreEvents.Print("Portal: " + (k + 1) + "\tRough Vis: "
						+ port.mMightSee + "\tFull Vis: "
						+ port.mCanSee + "\tRemaining: "
						+ (endPort - count)
						+ "\tIterations: " + vp.mIterations + "\n");
				}
			});
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
					SLeaf	=sport.mClusterTo;
					Debug.Assert((1 << (SLeaf & 7)) < 256);
					mGFXVisData[LeafBitsOfs + (SLeaf >> 3)]	|=(byte)(1 << (SLeaf & 7));

					//also mark the leaf the portal lives in
					SLeaf	=sport.mClusterFrom;
					Debug.Assert((1 << (SLeaf & 7)) < 256);
					mGFXVisData[LeafBitsOfs + (SLeaf >> 3)]	|=(byte)(1 << (SLeaf & 7));
				}
			}

			Bit	=1 << (leafNum & 7);

			Debug.Assert(Bit < 256);

			//He should not have seen himself (yet...)
			if((mGFXVisData[LeafBitsOfs + (leafNum >> 3)] & Bit) != 0)
			{
				CoreEvents.Print("*WARNING* CollectLeafVisBits:  Leaf:" + leafNum + " can see itself!\n");
			}

			//mark own leaf as visible
			mGFXVisData[LeafBitsOfs + (leafNum >> 3)]	|=(byte)Bit;

			//mark immediate neighbors as visible
			//this is needed in clusters bordering a cluster
			//with a single portal.  I think it is a flaw only
			//my own vis stuff has
			foreach(VISPortal p in Leaf.mPortals)
			{
				Debug.Assert(p.mClusterFrom == leafNum);

				int	looksInto	=p.mClusterTo;

				mGFXVisData[LeafBitsOfs + (looksInto >> 3)]	|=(byte)(1 << (looksInto & 7));
			}

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
				CoreEvents.Print("CollectLeafVisBits:  Leaf can see nothing.\n");
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


		//wrote this one myself
		//couldn't get the gen or q2 vers to work
		//it is not very fast, but it works and is easy to read
		void FacingFlood(VISPortal p, VISLeaf flooding)
		{
			foreach(VISPortal port in flooding.mPortals)
			{
				if((p.mPortalFlood[port.mPortNum >> 3] & (1 << (port.mPortNum & 7))) != 0)
				{
					continue;
				}

				if(port.mPoly.AnyPartInFront(p.mPlane))
				{
					if(p.mPoly.AnyPartBehind(port.mPlane))
					{
						p.mPortalFlood[port.mPortNum >> 3]	|=(byte)(1 << (port.mPortNum & 7));
						FacingFlood(p, mVisLeafs[port.mClusterTo]);
					}
				}
			}
		}
		
		
		void PortalFacingVis(int portNum)
		{
			VISPortal	p	=mVisPortals[portNum];

			p.mPortalFlood	=new byte[mNumVisPortalBytes];
			p.mPortalVis	=new byte[mNumVisPortalBytes];

			VISLeaf	myLeaf	=mVisLeafs[p.mClusterFrom];
			VISLeaf	leafTo	=mVisLeafs[p.mClusterTo];

			FacingFlood(p, leafTo);

			p.mMightSee	=CountBits(p.mPortalFlood, mVisPortals.Length);
		}
		
		
		static int CountBits(byte []bits, int numbits)
		{
			int		i;
			int		c = 0;
			
			for(i=0 ; i<numbits ; i++)
			{
				if((bits[i>>3] & (1<<(i&7))) != 0)
				{
					c++;
				}
			}
			return	c;
		}


		void SaveGFXVisData(BinaryWriter bw)
		{
			bw.Write(mGFXVisData.Length);
			bw.Write(mGFXVisData, 0, mGFXVisData.Length);
		}


		void LoadGFXVisData(BinaryReader br)
		{
			int	count	=br.ReadInt32();
			mGFXVisData	=br.ReadBytes(count);
		}


		void SaveGFXMaterialVisData(BinaryWriter bw)
		{
			bw.Write(mGFXMaterialVisData.Length);
			bw.Write(mGFXMaterialVisData, 0, mGFXMaterialVisData.Length);
		}


		void LoadGFXMaterialVisData(BinaryReader br)
		{
			int	count			=br.ReadInt32();
			mGFXMaterialVisData	=br.ReadBytes(count);
		}


		public void FreeFileVisData()
		{
			mGFXVisData			=null;
			mGFXMaterialVisData	=null;
		}


		void FreeAllVisData()
		{
			FreeFileVisData();

			if(mVisPortals != null)
			{
				for(int i=0;i < mVisPortals.Length;i++)
				{
					mVisPortals[i].mPoly		=null;
					mVisPortals[i].mPortalFlood	=null;
					mVisPortals[i].mPortalVis	=null;
				}

				mVisPortals	=null;
			}
			mVisPortals			=null;
			mVisSortedPortals	=null;
			mVisLeafs			=null;

			mMap.FreeGBSPFile();	//Free rest of GBSP GFX data
		}


		public bool LoadPortalFile(string portFile, bool bCheckLeafs)
		{
			FileStream	fs	=new FileStream(portFile,
				FileMode.Open, FileAccess.Read);

			BinaryReader	br	=null;

			if(fs == null)		// opps
			{
				CoreEvents.Print("LoadPortalFile:  Could not open " + portFile + " for reading.\n");
				goto	ExitWithError;
			}

			br	=new BinaryReader(fs);
			
			// 
			//	Check the TAG
			//
			string	TAG	=br.ReadString();
			if(TAG != "GBSP_PRTFILE")
			{
				CoreEvents.Print("LoadPortalFile:  " + portFile + " is not a GBSP Portal file.\n");
				goto	ExitWithError;
			}

			//
			//	Get the number of portals
			//
			int	NumVisPortals	=br.ReadInt32();
			if(NumVisPortals >= VISPStack.MAX_TEMP_PORTALS)
			{
				CoreEvents.Print("LoadPortalFile:  Max portals for temp buffers.\n");
				goto	ExitWithError;
			}
			
			mVisPortals	=new VISPortal[NumVisPortals * 2];
			if(mVisPortals == null)
			{
				CoreEvents.Print("LoadPortalFile:  Out of memory for VisPortals.\n");
				goto	ExitWithError;
			}
			
			mVisSortedPortals	=new VISPortal[NumVisPortals * 2];
			if(mVisSortedPortals == null)
			{
				CoreEvents.Print("LoadPortalFile:  Out of memory for VisSortedPortals.\n");
				goto ExitWithError;
			}

			//
			//	Get the number of leafs
			//
			int	NumVisLeafs	=br.ReadInt32();

			if(bCheckLeafs)
			{
				if(NumVisLeafs > mGFXLeafs.Length)
				{
					goto	ExitWithError;
				}
			}			
			mVisLeafs	=new VISLeaf[NumVisLeafs];
			if(mVisLeafs == null)
			{
				CoreEvents.Print("LoadPortalFile:  Out of memory for VisLeafs.\n");
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
			for(int i=0;i < NumVisPortals * 2;i+=2)
			{
				//alloc blank portals
				mVisPortals[i]		=new VISPortal();
				mVisPortals[i + 1]	=new VISPortal();

				GBSPPoly	poly	=new GBSPPoly(0);
				poly.Read(br);

				int	leafFrom	=br.ReadInt32();
				int	leafTo		=br.ReadInt32();
				
				if(leafFrom >= NumVisLeafs || leafFrom < 0)
				{
					CoreEvents.Print("LoadPortalFile:  Invalid LeafFrom: " + leafFrom + "\n");
					goto	ExitWithError;
				}

				if(leafTo >= NumVisLeafs || leafTo < 0)
				{
					CoreEvents.Print("LoadPortalFile:  Invalid LeafTo: " + leafTo + "\n");
					goto	ExitWithError;
				}

				GBSPPlane	pln	=new GBSPPlane(poly);

				VISLeaf		fleaf	=mVisLeafs[leafFrom];	//leaves on either side of
				VISLeaf		bleaf	=mVisLeafs[leafTo];		//the portal
				VISPortal	fport	=mVisPortals[i];
				VISPortal	bport	=mVisPortals[i + 1];

				fport.mPortNum		=i;			//port index, needed post sort
				fport.mPoly			=poly;		//actual portal geometry
				fport.mClusterTo	=leafTo;	//leaf the portal leads into
				fport.mClusterFrom	=leafFrom;	//leaf the portal lives in
				fport.mPlane		=pln;
				fleaf.mPortals.Add(fport);
				fport.CalcPortalInfo();

				bport.mPortNum		=i + 1;
				bport.mPoly			=new GBSPPoly(poly);
				bport.mClusterTo	=leafFrom;
				bport.mClusterFrom	=leafTo;
				bport.mPlane		=pln;
				bleaf.mPortals.Add(bport);

				bport.mPoly.Reverse();	//backside portal is flipped
				bport.mPlane.Inverse();	//to point toward leafFrom

				bport.CalcPortalInfo();
			}
			
			mNumVisLeafBytes	=((NumVisLeafs + 63) & ~63) >> 3;
			mNumVisPortalBytes	=(((NumVisPortals * 2) + 63) &~ 63) >> 3;

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