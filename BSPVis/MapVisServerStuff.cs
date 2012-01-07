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
	public partial class VisMap
	{
		//done event for slow flood
		public static event EventHandler	eFlowChunkComplete;


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


		//used by the external distributed vis
		public static byte []PortalFlow(byte []visData, int startPort, int endPort)
		{
			DateTime	visTime	=DateTime.Now;

			//convert the bytes to something usable
			MemoryStream	ms	=new MemoryStream();
			BinaryWriter	bw	=new BinaryWriter(ms);

			bw.Write(visData, 0, visData.Length);

			BinaryReader	br	=new BinaryReader(ms);
			br.BaseStream.Seek(0, SeekOrigin.Begin);

			int	portCount	=br.ReadInt32();

			VISPortal	[]visPortals	=new VISPortal[portCount];
			for(int i=0;i < portCount;i++)
			{
				visPortals[i]	=new VISPortal();
				visPortals[i].Read(br);
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
			Parallel.For(startPort, endPort, (k) =>
				{
					VisLoop(visPortals, visLeafs, numVisPortalBytes, k, endPort, ref count);
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

			VisState	vs	=new VisState();
			vs.mStartPort	=startPort;
			vs.mEndPort		=endPort;
			vs.mTotalPorts	=0;
			vs.mVisData		=returnBytes;

			UtilityLib.Misc.SafeInvoke(eFlowChunkComplete, vs);

			return	returnBytes;
		}


		static void VisLoop(VISPortal []visPortals, VISLeaf []visLeafs,
			int numVisPortalBytes, int k, int endPort, ref int count)
		{
			VISPortal	port	=visPortals[k];

			//This portal can't see anyone yet...
			for(int i=0;i < numVisPortalBytes;i++)
			{
				port.mPortalVis[i]	=0;
			}
			PortalFlow(k, visPortals, visLeafs, numVisPortalBytes);

			port.mbDone			=true;
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
				bool	bStarted	=amvc.PortalFlow(vs);
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
			FileInfo[]		fis	=di.GetFiles(baseName + "bits*.VisBits", SearchOption.TopDirectoryOnly);

			foreach(FileInfo fi in fis)
			{
				int	postBits	=fi.Name.IndexOf("bits") + 4;

				string	justNums	=fi.Name.Substring(postBits);

				int	underPos	=justNums.IndexOf('_');
				int	dotPos		=justNums.LastIndexOf('.');

				string	leftNum		=justNums.Substring(0, underPos);
				string	rightNum	=justNums.Substring(underPos + 1, dotPos - underPos - 1);

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

				br.BaseStream.Seek(0, SeekOrigin.Begin);

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


		void NukeVisBits(string fileName)
		{
			int		dirPos		=fileName.LastIndexOf('\\');
			string	baseName	=UtilityLib.FileUtil.StripExtension(fileName);
			string	dirName		=baseName.Substring(0, dirPos);

			baseName	=baseName.Substring(dirPos + 1);

			DirectoryInfo	di	=new DirectoryInfo(dirName);
			FileInfo[]		fis	=di.GetFiles(baseName + "bits*.VisBits", SearchOption.TopDirectoryOnly);

			foreach(FileInfo fi in fis)
			{
				CoreEvents.Print("Blasting " + fi.Name + "...\n");
				fi.Delete();
			}
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
			FileStream	fs	=new FileStream(saveChunk + ".VisBits", FileMode.Create, FileAccess.Write);
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
						CoreEvents.Print("Something wrong with work unit at " +
							vs.mStartPort + " to " + vs.mEndPort + "...  Restarting.\n");
						lock(workingOn)
						{
							workingOn.Remove(wrk);
						}
						work.Enqueue(wrk);
						lock(working)
						{
							working.Remove(wrk.mCruncher);
						}
						clients.Enqueue(wrk.mCruncher);
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

			CoreEvents.Print("Deleting vis bits...\n");
			NukeVisBits(fileName);
			CoreEvents.Print("Complete.\n");
		}
	}
}
