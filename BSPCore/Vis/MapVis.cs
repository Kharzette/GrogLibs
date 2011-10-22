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


namespace BSPCore
{
	public class WorldLeaf
	{
		public Int32	mVisFrame;
		public Int32	mParent;
	}

	class WorkDivided
	{
		public Int32	startPort, endPort;
		public bool		bAttempted;
		public bool		bFinished;
	}

	public class VisState
	{
		public byte	[]mVisData;
		public int	mStartPort;
		public int	mEndPort;
		public int	mTotalPorts;
		public bool	mbVisInProgress;
	}

	public partial class Map
	{
		//vis related stuff
		VISPortal	[]mVisPortals;
		VISPortal	[]mVisSortedPortals;
		VISLeaf		[]mVisLeafs;
		Int32		mNumVisLeafBytes, mNumVisPortalBytes;

		//area stuff
		List<GFXArea>		mAreas		=new List<GFXArea>();
		List<GFXAreaPortal>	mAreaPorts	=new List<GFXAreaPortal>();

		//threading
		TaskScheduler	mTaskSched	=TaskScheduler.Default;

		//events
		public static event EventHandler	eFloodSlowDone;
		public static event EventHandler	eSlowVisPieceDone;


		void ThreadVisCB(object threadContext)
		{
			VisParameters	vp	=threadContext as VisParameters;

			GFXHeader	header	=LoadGBSPFile(vp.mFileName);
			if(header == null)
			{
				Print("PvsGBSPFile:  Could not load GBSP file: " + vp.mFileName + "\n");
				CoreEvents.FireVisDoneEvent(false, null);
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

			CoreEvents.FireNumPortalsChangedEvent(mVisPortals.Length, null);

			Print("NumPortals           : " + mVisPortals.Length + "\n");
			
			//Vis'em
			if(!VisAllLeafs(vp.mVisParams.mbFullVis, vp.mClients))
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

			CoreEvents.FireVisDoneEvent(true, null);
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
					Print("Exception: " + e.Message + " for HasPortals.  Will requeue...\n");
				}

				return	false;
			}
			return	bHasSuccess;
		}


		bool FeedPortalsToRemote(Dictionary<VISPortal, Int32> portIndexer,
			MapVisClient amvc, out bool bRealFailure)
		{
			MemoryStream	ms	=new MemoryStream();
			BinaryWriter	bw	=new BinaryWriter(ms);

			bw.Write(mVisSortedPortals.Length);
			foreach(VISPortal vp in mVisPortals)
			{
				vp.Write(bw, portIndexer);
			}

			bw.Write(mVisLeafs.Length);
			foreach(VISLeaf vl in mVisLeafs)
			{
				vl.Write(bw, portIndexer);
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
					Print("Exception: " + e.Message + " for ReadPortals.  Will requeue...\n");
				}

				return	false;
			}

			return	bReadSuccess;
		}


		bool ProcessWork(WorkDivided wrk, MapVisClient amvc, out bool bRealFailure)
		{
			bRealFailure	=false;

			VisState	vs	=new VisState();

			vs.mVisData		=null;
			vs.mStartPort	=wrk.startPort;
			vs.mEndPort		=wrk.endPort;			

			byte	[]ports	=null;
			try
			{
				var task	=Task<byte []>.Factory.FromAsync(amvc.BeginFloodPortalsSlow,
					amvc.EndFloodPortalsSlow, (object)vs, (object)vs);

				//grab data
				ports	=task.Result;
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
					Print("Exception: " + e.Message + " for portals " + wrk.startPort + " to " + wrk.endPort + ".  Will requeue...\n");
				}

				return	false;
			}

			if(ports == null)
			{
				bRealFailure	=true;
				return	false;
			}

			BytesToVisBits(ports, wrk.startPort, wrk.endPort);

			return	true;
		}


		void BytesToVisBits(byte []ports, int startPort, int endPort)
		{
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
		}


		//see if this work unit has already been attempted
		void CheckForAbandonedData(ConcurrentQueue<MapVisClient> mvcq,
			ConcurrentQueue<WorkDivided> workq)
		{
			//sometimes pieces take so long to finish
			//that the connection times out, or a network
			//glitch or something else goes wrong
			foreach(WorkDivided wd in workq)
			{
				if(!wd.bAttempted || wd.bFinished)
				{
					continue;
				}

				foreach(MapVisClient mvc in mvcq)
				{
					VisState	vs	=mvc.GetAbandoned(wd.startPort, wd.endPort);
					if(vs != null)
					{
						BytesToVisBits(vs.mVisData, wd.startPort, wd.endPort);
						wd.bFinished	=true;
						return;
					}
				}
			}
		}


		void DistributedVis(ConcurrentQueue<MapVisClient> clients,
			Dictionary<VISPortal, Int32> portIndexer)
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
				granularity	=500;
			}
			else if(mVisPortals.Length > 10000)
			{
				granularity	=300;
			}
			else if(mVisPortals.Length > 5000)
			{
				granularity	=150;
			}
			else
			{
				granularity	=50;
			}

			//make a list of work to be done
			ConcurrentQueue<WorkDivided>	work	=new ConcurrentQueue<WorkDivided>();

			for(int i=0;i < (mVisPortals.Length / granularity);i++)
			{
				WorkDivided	wd	=new WorkDivided();
				wd.startPort	=i * granularity;
				wd.endPort		=(i + 1) * granularity;

				work.Enqueue(wd);
			}

			if(((mVisPortals.Length / granularity) * granularity) != mVisPortals.Length)
			{
				WorkDivided	remainder	=new WorkDivided();
				remainder.startPort	=(mVisPortals.Length / granularity) * granularity;
				remainder.endPort	=mVisPortals.Length;

				work.Enqueue(remainder);
			}

			List<MapVisClient>	working	=new List<MapVisClient>();

			Print("Beginning distributed visibility with " + clients.Count + " possible work machines\n");

			DateTime	startTime	=DateTime.Now;
			DateTime	trackTime	=DateTime.Now;

			object	prog	=ProgressWatcher.RegisterProgress(0, work.Count, 0);
			while(!work.IsEmpty || working.Count != 0)
			{
				MapVisClient	amvc	=null;

				if((DateTime.Now - trackTime).Minutes > 15)
				{
					trackTime	=DateTime.Now;
					CheckForAbandonedData(clients, work);
				}

				if(clients.TryDequeue(out amvc))
				{
					if(amvc != null)
					{
						//small potential for jumping out of the while
						//before getting here
						lock(working) {	working.Add(amvc); }

						bool	bRecreate;
						if(amvc.IsReadyOrTrashed(out bRecreate))
						{
							Task	task	=Task.Factory.StartNew(() =>
							{
								WorkDivided	wrk;
								if(work.TryDequeue(out wrk))
								{
									bool	bRealFailure;

									//see if this work unit has already been attempted
									if(wrk.bFinished)
									{
										//this work unit was grabbed in the check for
										//abandoned stuff
										ProgressWatcher.UpdateProgressIncremental(prog);
									}
									else
									{
										//see if client has portals
										bool	bHasPortals	=ClientHasPortals(amvc, mVisPortals.Length, out bRealFailure);

										if(!bRealFailure)
										{
											bool	bFed	=false;
											if(!bHasPortals)
											{
												bFed	=FeedPortalsToRemote(portIndexer, amvc, out bRealFailure);
											}

											if(!bRealFailure && (bFed || bHasPortals))
											{
												wrk.bAttempted	=true;

												if(!ProcessWork(wrk, amvc, out bRealFailure))
												{
													//failed, requeue
													work.Enqueue(wrk);
												}
												else
												{
													ProgressWatcher.UpdateProgressIncremental(prog);
												}
											}
										}
										if(bRealFailure)
										{
											Print("Build Farm Node : " + amvc.Endpoint.Address + " failed a work unit.  Requeueing it.\n");
											amvc.mNumFailures++;
										}
									}
								}
							});
							lock(working) { working.Remove(amvc); }
							clients.Enqueue(amvc);
						}
						else
						{
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
				}
				Thread.Sleep(1000);
			}

			Print("Finished vis\n");
			foreach(MapVisClient mvc in clients)
			{
				Print(mvc.Endpoint.Address.ToString() + " with " + mvc.mNumFailures + " failures.\n");
			}
		}


		bool VisAllLeafs(bool bFullVis, ConcurrentQueue<MapVisClient> clients)
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
			SortPortals();

			if(bFullVis)
			{
				DistributedVis(clients, portIndexer);
			}
			else
			{
				prog	=ProgressWatcher.RegisterProgress(0, mVisPortals.Length, 0);
				if(!FloodPortalsSlow(portIndexer, 0, mVisPortals.Length, false, prog))
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

			Parallel.For(startPort, endPort, (k) =>
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
					return;
				}

				portStack.mSource	=null;
				port.mbDone			=true;

				if(bVerbose)
				{
					Print("Portal: " + (k + 1) + " - Fast Vis: "
						+ port.mMightSee + ", Full Vis: "
						+ port.mCanSee + "\n");
				}
			});
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
				if(indexes[i] > 0)
				{
					visPortals[i].mNext	=visPortals[indexes[i]];
				}
			}

			//rebuild visindexer
			Dictionary<VISPortal, Int32>	portIndexer	=new Dictionary<VISPortal, Int32>();
			for(int i=0;i < visPortals.Length;i++)
			{
				portIndexer.Add(visPortals[i], i);
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

			Parallel.For(startPort, endPort, (k) =>
			{
				VISPortal	port	=visPortals[k];
				
				port.mFinalVisBits	=new byte[numVisPortalBytes];

				//This portal can't see anyone yet...
				for(int i=0;i < numVisPortalBytes;i++)
				{
					port.mFinalVisBits[i]	=0;
				}

				int	CanSee	=0;

				VISPStack	portStack	=new VISPStack();				
				for(int i=0;i < numVisPortalBytes;i++)
				{
					portStack.mVisBits[i]	=port.mVisBits[i];
				}

				//Setup Source/Pass
				portStack.mSource	=new GBSPPoly(port.mPoly);
				portStack.mPass		=null;

				if(!port.FloodPortalsSlow_r(port, portStack, portIndexer, ref CanSee, visLeafs))
				{
					return;
				}

				portStack.mSource	=null;
				port.mbDone			=true;

				Console.WriteLine("Portal: " + (k + 1) + " - Fast Vis: "
					+ port.mMightSee + ", Full Vis: "
					+ port.mCanSee);
			});

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

			DateTime	endVisTime	=DateTime.Now;

			TimeSpan	totalTime	=endVisTime - visTime;

			//if more than an hour has gone by, store the portals
			//as the connection might have been broken
			if(totalTime.Hours > 1)
			{
				VisState	vs	=new VisState();
				vs.mStartPort	=startPort;
				vs.mEndPort		=endPort;
				vs.mTotalPorts	=0;
				vs.mVisData		=returnBytes;

				Utility64.Misc.SafeInvoke(eSlowVisPieceDone, vs);
			}

			return	returnBytes;
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

				GBSPPoly	poly	=new GBSPPoly();
				poly.Read(br);

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
