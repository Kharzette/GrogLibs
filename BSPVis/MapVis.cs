using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.Xna.Framework;
using BSPCore;


namespace BSPVis
{
	public class VisParameters
	{
		public BSPBuildParams	mBSPParams;
		public VisParams		mVisParams;
		public string			mFileName;
		public BinaryWriter		mBSPFile;

		public ConcurrentQueue<MapVisClient>	mClients;
	}

	public class WorldLeaf
	{
		public Int32	mVisFrame;
		public Int32	mParent;
	}

	class WorkDivided
	{
		public Int32		mStartPort, mEndPort;
		public bool			mbAttempted;
		public bool			mbFinished;
		public DateTime		mTicTime;
		public int			mPollSeconds;
		public MapVisClient	mCruncher;
	}

	public class VisState
	{
		public byte	[]mVisData;
		public int	mStartPort;
		public int	mEndPort;
		public int	mTotalPorts;
	}

	public class VisMap
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
		Int32		mNumVisMaterialBytes;

		//compiled vis data
		byte	[]mGFXVisData;
		byte	[]mGFXMaterialVisData;

		//area stuff
		List<GFXArea>		mAreas		=new List<GFXArea>();
		List<GFXAreaPortal>	mAreaPorts	=new List<GFXAreaPortal>();

		//threading
		TaskScheduler	mTaskSched	=TaskScheduler.Default;

		//done event for slow flood
		public static event EventHandler	eSlowFloodPartDone;


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

			string	visExt	=UtilityLib.FileUtil.StripExtension(vp.mFileName);

			visExt	+=".VisData";

			//Open the vis file for writing
			FileStream	fs	=new FileStream(visExt,
				FileMode.OpenOrCreate, FileAccess.Write);

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
			if(!LoadPortalFile(PFile))
			{
				goto	ExitWithError;
			}

			CoreEvents.FireNumPortalsChangedEvent(mVisPortals.Length, null);

			CoreEvents.Print("NumPortals           : " + mVisPortals.Length + "\n");

			DateTime	startTime	=DateTime.Now;

			CoreEvents.Print("Starting vis at " + startTime + "\n");
			
			//Vis'em
			if(!VisAllLeafs(vp.mClients, vp.mFileName, vp))
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


		public void VisGBSPFile(string fileName, VisParams prms, BSPBuildParams prms2)
		{
			VisParameters	vp	=new VisParameters();
			vp.mBSPParams	=prms2;
			vp.mVisParams	=prms;
			vp.mFileName	=fileName;

			ThreadPool.QueueUserWorkItem(ThreadVisCB, vp);
		}


		public void VisGBSPFile(string fileName, VisParams prms,
			BSPBuildParams prms2, ConcurrentQueue<MapVisClient> clients)
		{
			VisParameters	vp	=new VisParameters();
			vp.mBSPParams	=prms2;
			vp.mVisParams	=prms;
			vp.mFileName	=fileName;
			vp.mClients		=clients;

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
			UtilityLib.FileUtil.WriteArray(mGFXVisData, bw);
			UtilityLib.FileUtil.WriteArray(mGFXMaterialVisData, bw);
			UtilityLib.FileUtil.WriteArray(mGFXClusters, bw); 
		}


		public void LoadVisData(string fileName)
		{
			string	visExt	=UtilityLib.FileUtil.StripExtension(fileName);

			visExt	+=".VisData";

			FileStream		fs	=new FileStream(visExt, FileMode.Open, FileAccess.Read);
			BinaryReader	br	=new BinaryReader(fs);

			UInt32	magic	=br.ReadUInt32();

			if(magic != 0x715da7aa)
			{
				return;
			}

			mGFXVisData	=UtilityLib.FileUtil.ReadByteArray(br);
			mGFXMaterialVisData	=UtilityLib.FileUtil.ReadByteArray(br);

			//load clusters
			mGFXClusters	=UtilityLib.FileUtil.ReadArray(br, delegate(Int32 count)
							{ return UtilityLib.FileUtil.InitArray<GFXCluster>(count); }) as GFXCluster[];

			br.Close();
			fs.Close();
		}


#if !X64
		public bool MaterialVisGBSPFile(string fileName)
		{
			CoreEvents.Print(" --- Material Vis GBSP File --- \n");

			GFXHeader	header	=mMap.LoadGBSPFile(fileName);
			if(header == null)
			{
				CoreEvents.Print("PvsGBSPFile:  Could not load GBSP file: " + fileName + "\n");
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

			LoadVisData(fileName);

			string	visExt	=UtilityLib.FileUtil.StripExtension(fileName);

			visExt	+=".VisData";

			//Open the bsp file for writing
			FileStream	fs	=new FileStream(visExt,
				FileMode.OpenOrCreate, FileAccess.Write);

			BinaryWriter	bw	=null;

			if(fs == null)
			{
				CoreEvents.Print("MatVisGBSPFile:  Could not open VisData file for writing: " + fileName + "\n");
				goto	ExitWithError;
			}

			//make a material vis, what materials
			//can be seen from each leaf
			VisMaterials();

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

			CoreEvents.Print("Computing visible materials from each leaf...\n");

			//Grab map stuff needed to compute this
			int			firstLeaf	=mMap.GetWorldFirstLeaf();

			//make a temporary mapgrinder to help sync
			//up material names and indexes and such
			MapGrinder	mg	=mMap.MakeMapGrinder();

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


		bool ClientHasPortals(MapVisClient amvc, int numPorts, out bool bRealFailure)
		{
			bRealFailure	=false;

			VisState	vs	=new VisState();

			vs.mVisData		=null;
			vs.mStartPort	=0;
			vs.mEndPort		=0;
			vs.mTotalPorts	=numPorts;

			bool	bHasSuccess	=false;

			try
			{
				bHasSuccess	=amvc.HasPortals(vs);
			}
			catch(Exception e)
			{
				//check for normal no worries exceptions
				if(e is System.AggregateException)
				{
					System.AggregateException	ae	=e as System.AggregateException;
					foreach(Exception ee in ae.InnerExceptions)
					{
						if(ee is System.ServiceModel.EndpointNotFoundException)
						{
						}
						else
						{
							bRealFailure	=true;
						}
					}
				}
				else if(e is System.ServiceModel.EndpointNotFoundException)
				{
				}
				else
				{
					bRealFailure	=true;
				}

				if(bRealFailure)
				{
					CoreEvents.Print("Exception: " + e.Message + " for HasPortals.  Will requeue...\n");
				}

				return	false;
			}
			return	bHasSuccess;
		}


		bool FeedPortalsToRemote(MapVisClient amvc, out bool bRealFailure)
		{
			MemoryStream	ms	=new MemoryStream();
			BinaryWriter	bw	=new BinaryWriter(ms);

			bw.Write(mVisSortedPortals.Length);
			foreach(VISPortal vp in mVisPortals)
			{
				vp.Write(bw);
			}

			bw.Write(mVisLeafs.Length);
			foreach(VISLeaf vl in mVisLeafs)
			{
				vl.Write(bw);
			}

			bw.Write(mNumVisPortalBytes);

			BinaryReader	br	=new BinaryReader(ms);
			br.BaseStream.Seek(0, SeekOrigin.Begin);

			byte	[]visDat	=br.ReadBytes((int)ms.Length);

			bw.Close();
			br.Close();
			ms.Close();

			bRealFailure	=false;

			VisState	vs	=new VisState();

			vs.mVisData		=visDat;
			vs.mStartPort	=0;
			vs.mEndPort		=0;
			vs.mTotalPorts	=mVisPortals.Length;

			bool	bReadSuccess	=false;

			try
			{
				bReadSuccess	=amvc.ReadPortals(vs);
			}
			catch(Exception e)
			{
				//check for normal no worries exceptions
				if(e is System.AggregateException)
				{
					System.AggregateException	ae	=e as System.AggregateException;
					foreach(Exception ee in ae.InnerExceptions)
					{
						if(ee is System.ServiceModel.EndpointNotFoundException)
						{
						}
						else
						{
							bRealFailure	=true;
						}
					}
				}
				else if(e is System.ServiceModel.EndpointNotFoundException)
				{
				}
				else
				{
					bRealFailure	=true;
				}

				if(bRealFailure)
				{
					CoreEvents.Print("Exception: " + e.Message + " for ReadPortals.  Will requeue...\n");
				}

				return	false;
			}

			return	bReadSuccess;
		}


		bool ProcessWork(WorkDivided wrk, MapVisClient amvc, out bool bRealFailure, string fileName)
		{
			bRealFailure	=false;

			VisState	vs	=new VisState();

			vs.mVisData		=null;
			vs.mStartPort	=wrk.mStartPort;
			vs.mEndPort		=wrk.mEndPort;			

			try
			{
				bool	bStarted	=amvc.BeginFloodPortalsSlow(vs);
			}
			catch(Exception e)
			{
				//check for normal no worries exceptions
				if(e is System.AggregateException)
				{
					System.AggregateException	ae	=e as System.AggregateException;
					foreach(Exception ee in ae.InnerExceptions)
					{
						if(ee is System.ServiceModel.EndpointNotFoundException)
						{
						}
						else
						{
							bRealFailure	=true;
						}
					}
				}
				else if(e is System.ServiceModel.EndpointNotFoundException)
				{
				}
				else
				{
					bRealFailure	=true;
				}

				if(bRealFailure)
				{
					CoreEvents.Print("Exception: " + e.Message + " for portals " + wrk.mStartPort + " to " + wrk.mEndPort + ".  Will requeue...\n");
				}

				return	false;
			}

			return	true;
		}


		List<WorkDivided> ResumeFromBitsFiles(string fileName)
		{
			int		dirPos		=fileName.LastIndexOf('\\');
			string	baseName	=UtilityLib.FileUtil.StripExtension(fileName);
			string	dirName		=baseName.Substring(0, dirPos);

			baseName	=baseName.Substring(dirPos + 1);

			DirectoryInfo	di	=new DirectoryInfo(dirName);
			FileInfo[]		fis	=di.GetFiles(baseName + "bits*", SearchOption.TopDirectoryOnly);

			foreach(FileInfo fi in fis)
			{
				int	postBits	=fi.Name.IndexOf("bits") + 4;

				string	justNums	=fi.Name.Substring(postBits);

				int	underPos	=justNums.IndexOf('_');

				string	leftNum		=justNums.Substring(0, underPos);
				string	rightNum	=justNums.Substring(underPos + 1);

				int	left, right;
				if(!int.TryParse(leftNum, out left))
				{
					continue;
				}
				if(!int.TryParse(rightNum, out right))
				{
					continue;
				}

				FileStream		fs	=fi.OpenRead();
				BinaryReader	br	=new BinaryReader(fs);

				for(int i=left;i < right;i++)
				{
					mVisPortals[i].ReadVisBits(br);
					mVisPortals[i].mbDone	=true;	//use to mark
				}

				br.Close();
				fs.Close();
			}

			//figure out what is left to do
			List<WorkDivided>	workRemaining	=new List<WorkDivided>();
			WorkDivided			current			=null;
			for(int i=0;i < mVisPortals.Length;i++)
			{
				if(!mVisPortals[i].mbDone && current == null)
				{
					current	=new WorkDivided();
					current.mStartPort	=i;
				}
				else if(mVisPortals[i].mbDone && current != null)
				{
					current.mEndPort	=i;
					workRemaining.Add(current);
					current	=null;
				}
			}

			if(current != null)
			{
				//never reached the end
				current.mEndPort	=mVisPortals.Length - 1;
				workRemaining.Add(current);
			}

			//reset mbDone
			for(int i=0;i < mVisPortals.Length;i++)
			{
				mVisPortals[i].mbDone	=false;
			}

			return	workRemaining;
		}


		void BytesToVisBits(byte []ports, int startPort, int endPort, string fileName)
		{
			string	saveChunk	=UtilityLib.FileUtil.StripExtension(fileName);

			saveChunk	+="bits" + startPort + "_" + endPort;

			MemoryStream	ms	=new MemoryStream();
			BinaryWriter	bw	=new BinaryWriter(ms);

			bw.Write(ports, 0, ports.Length);

			BinaryReader	br	=new BinaryReader(ms);
			br.BaseStream.Seek(0, SeekOrigin.Begin);

			for(int j=startPort;j < endPort;j++)
			{
				mVisPortals[j].ReadVisBits(br);
			}

			bw.Close();
			br.Close();
			ms.Close();

			//save to file too
			FileStream	fs	=new FileStream(saveChunk, FileMode.Create, FileAccess.Write);
			bw	=new BinaryWriter(fs);

			bw.Write(ports, 0, ports.Length);

			bw.Close();
			fs.Close();
		}


		void DistributedVis(ConcurrentQueue<MapVisClient> clients, string fileName, bool bResume, bool bVerbose)
		{
			if(clients == null || clients.Count == 0)
			{
				return;
			}

			int	granularity	=1;
			//choose a granularity, which is a size of how much
			//to split up the work
			if(mVisPortals.Length > 20000)
			{
				granularity	=50;
			}
			else if(mVisPortals.Length > 10000)
			{
				granularity	=100;
			}
			else
			{
				granularity	=500;
			}

			//make a list of work to be done
			ConcurrentQueue<WorkDivided>	work	=new ConcurrentQueue<WorkDivided>();

			if(bResume)
			{
				//load in the previously calculated stuff
				List<WorkDivided>	remainingWork	=ResumeFromBitsFiles(fileName);

				foreach(WorkDivided wd in remainingWork)
				{
					if(((wd.mEndPort - wd.mStartPort) / granularity) == 0)
					{
						WorkDivided	wdd		=new WorkDivided();
						wdd.mStartPort		=wd.mStartPort;
						wdd.mEndPort		=wd.mEndPort;
						wdd.mPollSeconds	=5;

						work.Enqueue(wdd);
						continue;
					}

					for(int i=0;i < ((wd.mEndPort - wd.mStartPort) / granularity);i++)
					{
						WorkDivided	wdd		=new WorkDivided();
						wdd.mStartPort		=i * granularity + wd.mStartPort;
						wdd.mEndPort		=wd.mStartPort + ((i + 1) * granularity);
						wdd.mPollSeconds	=5;

						work.Enqueue(wdd);
					}
				}
			}
			else
			{
				for(int i=0;i < (mVisPortals.Length / granularity);i++)
				{
					WorkDivided	wd	=new WorkDivided();
					wd.mStartPort	=i * granularity;
					wd.mEndPort		=(i + 1) * granularity;
					wd.mPollSeconds	=5;

					work.Enqueue(wd);
				}

				if(((mVisPortals.Length / granularity) * granularity) != mVisPortals.Length)
				{
					WorkDivided	remainder	=new WorkDivided();
					remainder.mStartPort	=(mVisPortals.Length / granularity) * granularity;
					remainder.mEndPort	=mVisPortals.Length;

					work.Enqueue(remainder);
				}
			}

			List<MapVisClient>	working		=new List<MapVisClient>();
			List<WorkDivided>	workingOn	=new List<WorkDivided>();

			CoreEvents.Print("Beginning distributed visibility with " + clients.Count + " possible work machines\n");

			DateTime	startTime	=DateTime.Now;

			object	prog	=ProgressWatcher.RegisterProgress(0, work.Count, 0);
			while(!work.IsEmpty || working.Count != 0)
			{
				MapVisClient	amvc	=null;

				//see if any clients are unbusy
				if(clients.TryDequeue(out amvc))
				{
					if(amvc == null)	//shouldn't happen
					{
						CoreEvents.Print("Null client in client queue!\n");
						continue;
					}

					if(bVerbose)
					{
						CoreEvents.Print("DeQueue of " + amvc.Endpoint.Address.ToString() + "\n");
					}

					bool	bRecreate;
					if(amvc.IsReadyOrTrashed(out bRecreate))
					{
						if(bVerbose)
						{
							CoreEvents.Print(amvc.Endpoint.Address.ToString() + " shows to be ready to go\n");
						}

						//add this client to the working list
						lock(working) {	working.Add(amvc); }

						Task	task	=Task.Factory.StartNew(() =>
						{
							bool	bWorking	=false;

							WorkDivided	wrk;
							if(work.TryDequeue(out wrk))
							{
								bool	bRealFailure;

								//see if client has portals
								bool	bHasPortals	=ClientHasPortals(amvc, mVisPortals.Length, out bRealFailure);

								if(bVerbose)
								{
									CoreEvents.Print(amvc.Endpoint.Address.ToString() + " HasPortals " + bHasPortals + " and RealFailure " + bRealFailure + "\n");
								}

								if(!bRealFailure)
								{
									bool	bFed	=false;
									if(!bHasPortals)
									{
										bFed	=FeedPortalsToRemote(amvc, out bRealFailure);
									}

									if(bVerbose)
									{
										CoreEvents.Print(amvc.Endpoint.Address.ToString() + " FedPortals " + bFed + " and RealFailure " + bRealFailure + "\n");
									}

									if(!bRealFailure && (bFed || bHasPortals))
									{
										wrk.mbAttempted	=true;

										if(!ProcessWork(wrk, amvc, out bRealFailure, fileName))
										{
											//failed, requeue
											work.Enqueue(wrk);
										}
										else
										{
											//client has begun work
											if(bVerbose)
											{
												CoreEvents.Print(amvc.Endpoint.Address.ToString() + " beginning work\n");
											}
											bWorking		=true;
											wrk.mTicTime	=DateTime.Now;
											wrk.mCruncher	=amvc;
											lock(workingOn) { workingOn.Add(wrk); }
										}
									}

									if(!bFed && !bHasPortals && !bRealFailure)
									{
										//something went wrong in the portal send stage
										if(bVerbose)
										{
											CoreEvents.Print(amvc.Endpoint.Address.ToString() + " had a problem\n");
										}
										work.Enqueue(wrk);
									}
								}

								if(bRealFailure)
								{
									if(bVerbose)
									{
										CoreEvents.Print("Build Farm Node : " + amvc.Endpoint.Address + " failed a work unit.  Requeueing it.\n");
									}
									amvc.mNumFailures++;
									work.Enqueue(wrk);
								}
							}
							if(!bWorking)
							{
								if(bVerbose)
								{
									CoreEvents.Print(amvc.Endpoint.Address.ToString() + " notworking, going back in client queue\n");
								}
								lock(working) { working.Remove(amvc); }
								clients.Enqueue(amvc);
							}
						});
					}
					else
					{
						if(bVerbose)
						{
							CoreEvents.Print(amvc.Endpoint.Address.ToString() + " not ready, bRecreate is " + bRecreate + "\n");
						}
						if(bRecreate)
						{
							//existing client hozed, make a new one
							//this will probably go on a lot if the endpoint is down
							MapVisClient	newMVC	=new MapVisClient("WSHttpBinding_IMapVis", amvc.mEndPointURI);
							clients.Enqueue(newMVC);
						}
						else
						{
							clients.Enqueue(amvc);
						}
					}
				}

				Thread.Sleep(1000);

				//check on the work in progress
				List<WorkDivided>	toCheck	=new List<WorkDivided>();
				lock(workingOn)
				{
					foreach(WorkDivided wrk in workingOn)
					{
						TimeSpan	elapsed	=DateTime.Now - wrk.mTicTime;

						if(elapsed.Seconds >= wrk.mPollSeconds)
						{
							toCheck.Add(wrk);
						}
					}
				}

				foreach(WorkDivided wrk in toCheck)
				{
					VisState	vs	=new VisState();
					vs.mStartPort	=wrk.mStartPort;
					vs.mEndPort		=wrk.mEndPort;

					byte	[]ports	=null;

					try
					{
						ports	=wrk.mCruncher.IsFinished(vs);
					}
					catch
					{
					}

					if(ports != null)
					{
						//finished a work unit!
						CoreEvents.Print(wrk.mCruncher.Endpoint.Address.ToString() + " finished a work unit\n");
						BytesToVisBits(ports, wrk.mStartPort, wrk.mEndPort, fileName);
						wrk.mbFinished	=true;
						ProgressWatcher.UpdateProgressIncremental(prog);
						lock(workingOn) { workingOn.Remove(wrk); }
						lock(working) { working.Remove(wrk.mCruncher); }
						clients.Enqueue(wrk.mCruncher);
					}

					wrk.mTicTime	=DateTime.Now;
				}
			}

			CoreEvents.Print("Finished vis in " + (DateTime.Now - startTime) + "\n");
			foreach(MapVisClient mvc in clients)
			{
				CoreEvents.Print("Freeing client portals\n");

				try
				{
					mvc.FreePortals();
				}
				catch {	}

				CoreEvents.Print(mvc.Endpoint.Address.ToString() + " with " + mvc.mNumFailures + " failures.\n");
			}
		}


		bool VisAllLeafs(ConcurrentQueue<MapVisClient> clients, string fileName, VisParameters vp)
		{
			CoreEvents.Print("Quick vis for " + mVisPortals.Length + " portals...\n");

			//Create PortalSeen array.  This is used by Vis flooding routines
			//This is deleted below...
			bool	[]portalSeen	=new bool[mVisPortals.Length];

			object	prog	=ProgressWatcher.RegisterProgress(0, mVisLeafs.Length, 0);

			//Flood all the leafs with the fast method first...
			for(int i=0;i < mVisLeafs.Length; i++)
			{
				FloodLeafPortalsFast(i, portalSeen);

				ProgressWatcher.UpdateProgress(prog, i);
			}
			ProgressWatcher.Clear();

			//Sort the portals with MightSee
			SortPortals();

			if(vp.mVisParams.mbDistribute)
			{
				DistributedVis(clients, fileName, vp.mVisParams.mbResume, vp.mBSPParams.mbVerbose);
			}
			else
			{
				prog	=ProgressWatcher.RegisterProgress(0, mVisPortals.Length, 0);
				if(!FloodPortalsSlow(0, mVisPortals.Length, vp.mBSPParams.mbVerbose, prog))
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
				CoreEvents.Print("VisAllLeafs:  Out of memory for LeafVisBits.\n");
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

			CoreEvents.Print("Total visible areas           : " + TotalVisibleLeafs + "\n");
			CoreEvents.Print("Average visible from each area: " + TotalVisibleLeafs / mVisLeafs.Length + "\n");

			return	true;

			// ==== ERROR ====
			ExitWithError:
			{
				// Free all the global vis data
				FreeAllVisData();

				return	false;
			}
		}


		bool FloodPortalsSlow(int startPort, int endPort, bool bVerbose, object prog)
		{
			for(int k=startPort;k < endPort;k++)
			{
				mVisPortals[k].mbDone	=false;
			}

			int	count	=startPort;
			for(int k=startPort;k < endPort;k++)
//			Parallel.For(startPort, endPort, (k) =>
			{
				VISPortal	port	=mVisSortedPortals[k];
				
				port.mFinalVisBits	=new byte[mNumVisPortalBytes];

				//This portal can't see anyone yet...
				for(int i=0;i < mNumVisPortalBytes;i++)
				{
					port.mFinalVisBits[i]	=0;
				}

				int	CanSee	=0;

				VisPools	vPools		=new VisPools();
				ClipPools	cPools		=new ClipPools();
				VISPStack	portStack	=vPools.mStacks.GetFreeItem();
				for(int i=0;i < mNumVisPortalBytes;i++)
				{
					portStack.mVisBits[i]	=port.mVisBits[i];
				}

				//Setup Source/Pass
				portStack.mSource			=vPools.mPolys.GetFreeItem();
				portStack.mSource.mVerts	=cPools.DupeVerts(port.mPoly.mVerts);
				portStack.mPass				=null;
//				if(!port.FloodPortalsSlow_r(port, portStack, ref CanSee, mVisLeafs, vPools, cPools))
				if(!port.FloodPortalsSlowNoR(port, portStack, ref CanSee, mVisLeafs, vPools, cPools))
				{
					return	false;
				}

				portStack.mSource	=null;
				port.mbDone			=true;

				Interlocked.Increment(ref count);
				if(count % 10 == 0 && prog != null)
				{
					ProgressWatcher.UpdateProgress(prog, count);
				}

				if(bVerbose)
				{
					CoreEvents.Print("Portal: " + (k + 1) + " - Fast Vis: "
						+ port.mMightSee + ", Full Vis: "
						+ port.mCanSee + ", remaining: "
						+ (endPort - count) + "\n");
				}
			}//);
			return	true;
		}


		//used by the external distributed vis
		public static byte []FloodPortalsSlow(byte []visData, int startPort, int endPort)
		{
			DateTime	visTime	=DateTime.Now;

			//convert the bytes to something usable
			MemoryStream	ms	=new MemoryStream();
			BinaryWriter	bw	=new BinaryWriter(ms);

			bw.Write(visData, 0, visData.Length);

			BinaryReader	br	=new BinaryReader(ms);
			br.BaseStream.Seek(0, SeekOrigin.Begin);

			int	portCount	=br.ReadInt32();

			List<Int32>	indexes			=new List<Int32>();
			VISPortal	[]visPortals	=new VISPortal[portCount];
			for(int i=0;i < portCount;i++)
			{
				visPortals[i]	=new VISPortal();
				visPortals[i].Read(br, indexes);
			}

			//rebuild indexes
			for(int i=0;i < portCount;i++)
			{
				if(indexes[i] >= 0)
				{
					visPortals[i].mNext	=visPortals[indexes[i]];
				}
			}

			//read visleafs
			int	leafCount	=br.ReadInt32();
			VISLeaf	[]visLeafs	=new VISLeaf[leafCount];
			for(int i=0;i < leafCount;i++)
			{
				visLeafs[i]	=new VISLeaf();
				visLeafs[i].Read(br, visPortals);
			}

			//read numbytes
			int	numVisPortalBytes	=br.ReadInt32();

			for(int k=startPort;k < endPort;k++)
			{
				visPortals[k].mbDone	=false;
			}

			bw.Close();
			br.Close();
			ms.Close();

			int	count	=startPort;

//			for(int i=0;i < startPort;i++)
//			{
//				VisLoop(visPortals, visLeafs, numVisPortalBytes, i, endPort, ref count);
//			}

			Parallel.For(startPort, endPort, (k) =>
				{
					VisLoop(visPortals, visLeafs, numVisPortalBytes, k, endPort, ref count);
				});
/*
			List<Task>	tasks	=new List<Task>();

			for(int i=startPort;i < endPort;i++)
			{
				int	temp	=i;

				Task	task	=new Task(() => VisLoop(visPortals, visLeafs, numVisPortalBytes, temp, endPort, ref count));

				tasks.Add(task);
				task.Start();
			}

			Task.WaitAll(tasks.ToArray());*/

			//put vis bits in return data
			ms	=new MemoryStream();
			bw	=new BinaryWriter(ms);

			for(int i=startPort;i < endPort;i++)
			{
				visPortals[i].WriteVisBits(bw);
			}

			//read into memory
			br	=new BinaryReader(ms);
			br.BaseStream.Seek(0, SeekOrigin.Begin);

			byte	[]returnBytes	=br.ReadBytes((int)ms.Length);

			bw.Close();
			br.Close();
			ms.Close();

			VisState	vs	=new VisState();
			vs.mStartPort	=startPort;
			vs.mEndPort		=endPort;
			vs.mTotalPorts	=0;
			vs.mVisData		=returnBytes;

			UtilityLib.Misc.SafeInvoke(eSlowFloodPartDone, vs);

			return	returnBytes;
		}


		static void VisLoop(VISPortal []visPortals, VISLeaf []visLeafs,
			int numVisPortalBytes, int k, int endPort, ref int count)
		{
			VISPortal	port	=visPortals[k];
				
			port.mFinalVisBits	=new byte[numVisPortalBytes];

			//This portal can't see anyone yet...
			for(int i=0;i < numVisPortalBytes;i++)
			{
				port.mFinalVisBits[i]	=0;
			}

			int	CanSee	=0;

			VisPools	vPools		=new VisPools();
			ClipPools	cPools		=new ClipPools();
			VISPStack	portStack	=vPools.mStacks.GetFreeItem();
			for(int i=0;i < numVisPortalBytes;i++)
			{
				portStack.mVisBits[i]	=port.mVisBits[i];
			}

			//Setup Source/Pass
			portStack.mSource			=vPools.mPolys.GetFreeItem();
			portStack.mSource.mVerts	=cPools.DupeVerts(port.mPoly.mVerts);
			portStack.mPass				=null;
			if(!port.FloodPortalsSlow_r(port, portStack, ref CanSee, visLeafs, vPools, cPools))
			{
				return;
			}

			portStack.mSource	=null;
			port.mbDone			=true;

			Interlocked.Increment(ref count);

			Console.WriteLine("Portal: " + (k + 1) + " - Fast Vis: "
				+ port.mMightSee + ", Full Vis: "
				+ port.mCanSee + ", remaining: " + (endPort - count));
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
				CoreEvents.Print("*WARNING* CollectLeafVisBits:  Leaf:" + leafNum + " can see himself!\n");
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


		void FloodLeafPortalsFast(int leafNum, bool []portSeen)
		{
			VISLeaf	leaf	=mVisLeafs[leafNum];

			if(leaf.mPortals == null)
			{
				CoreEvents.Print("*WARNING* FloodLeafPortalsFast:  Leaf with no portals.\n");
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
				
				port.FloodPortalsFast_r(port, portSeen, mVisLeafs, srcLeaf, ref mightSee);
			}
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
					mVisPortals[i].mPoly			=null;
					mVisPortals[i].mFinalVisBits	=null;
					mVisPortals[i].mVisBits			=null;
				}

				mVisPortals	=null;
			}
			mVisPortals			=null;
			mVisSortedPortals	=null;
			mVisLeafs			=null;

			mMap.FreeGBSPFile();	//Free rest of GBSP GFX data
		}


		bool LoadPortalFile(string portFile)
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
			if(NumVisLeafs > mGFXLeafs.Length)
			{
				goto	ExitWithError;
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

				VISLeaf		fleaf	=mVisLeafs[leafFrom];
				VISLeaf		bleaf	=mVisLeafs[leafTo];
				VISPortal	fport	=mVisPortals[i];
				VISPortal	bport	=mVisPortals[i + 1];

				fport.mPortNum	=i;
				fport.mPoly		=poly;
				fport.mLeaf		=leafTo;
				fport.mPlane	=pln;
				fport.mNext		=fleaf.mPortals;
				fleaf.mPortals	=fport;
				fport.CalcPortalInfo();

				bport.mPortNum	=i + 1;
				bport.mPoly		=new GBSPPoly(poly);
				bport.mLeaf		=leafFrom;
				bport.mNext		=bleaf.mPortals;
				bport.mPlane	=pln;
				bleaf.mPortals	=bport;

				bport.mPoly.Reverse();
				bport.mPlane.Inverse();

				bport.CalcPortalInfo();
			}
			
			mNumVisLeafBytes	=((NumVisLeafs+63)&~63) >> 3;
			mNumVisPortalBytes	=(((NumVisPortals * 2)+63)&~63) >> 3;

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
