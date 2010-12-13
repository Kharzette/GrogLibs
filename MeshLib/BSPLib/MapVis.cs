using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public partial class Map
	{
		//vis related stuff
		VISPortal	[]VisPortals;
		VISPortal	[]VisSortedPortals;
		VISLeaf		[]VisLeafs;
		Int32		NumVisLeafBytes, NumVisPortalBytes;

		//area stuff
		List<GFXArea>		mAreas		=new List<GFXArea>();
		List<GFXAreaPortal>	mAreaPorts	=new List<GFXAreaPortal>();


		public bool VisGBSPFile(string fileName, VisParams prms, BSPBuildParams prms2)
		{
			Print(" --- Vis GBSP File --- \n");

			mVisParams	=prms;
			mBSPParms	=prms2;

			// Fill in the global bsp data
			if(!LoadGBSPFile(fileName))
			{
				Print("PvsGBSPFile:  Could not load GBSP file: " + fileName + "\n");
				return	false;
			}
			string	PFile;

			//Clean out any old vis data
			FreeFileVisData();

			//Open the bsp file for writing
			FileStream	fs	=UtilityLib.FileUtil.OpenTitleFile(fileName,
				FileMode.OpenOrCreate, FileAccess.Write);

			BinaryWriter	bw	=null;

			if(fs == null)
			{
				Print("VisGBSPFile:  Could not open GBSP file for writing: " + fileName + "\n");
				goto	ExitWithError;
			}

			bw	=new BinaryWriter(fs);

			// Prepare the portal file name
			int	extPos	=fileName.LastIndexOf(".");
			PFile		=fileName.Substring(0, extPos);
			PFile		+=".gpf";
			
			//Load the portal file
			if(!LoadPortalFile(PFile))
			{
				goto	ExitWithError;
			}

			Print("NumPortals           : " + VisPortals.Length + "\n");
			
			//Write out everything but vis info
			if(!StartWritingVis(bw))
			{
				goto	ExitWithError;
			}

			//Vis'em
			if(!VisAllLeafs())
			{
				goto	ExitWithError;
			}

			//Save the leafs, clusters, vis data, etc
			if(!FinishWritingVis(bw))
			{
				goto	ExitWithError;
			}

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
				Print("PvsGBSPFile:  Could not vis the file: " + fileName + "\n");

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
			GFXArea	areaZero	=new GFXArea();
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
		bool FinishWritingVis(BinaryWriter bw)
		{
			if(!SaveVisdGFXLeafs(bw))
			{
				return	false;
			}
			if(!SaveVisdGFXClusters(bw))
			{
				return	false;
			}
			if(!SaveGFXVisData(bw))
			{
				return	false;
			}

			GBSPChunk	Chunk	=new GBSPChunk();

			Chunk.mType		=GBSPChunk.GBSP_CHUNK_END;
			Chunk.mElements	=0;
			Chunk.Write(bw);

			return	true;
		}


		bool VisAllLeafs()
		{
			Int32	i;

			//Create PortalSeen array.  This is used by Vis flooding routines
			//This is deleted below...
			bool	[]PortalSeen	=new bool[VisPortals.Length];

			//create a dictionary to map a vis portal back to an index
			Dictionary<VISPortal, Int32>	portIndexer	=new Dictionary<VISPortal, Int32>();
			for(i=0;i < VisPortals.Length;i++)
			{
				portIndexer.Add(VisPortals[i], i);
			}

			//Flood all the leafs with the fast method first...
			for(i=0;i < VisLeafs.Length; i++)
			{
				FloodLeafPortalsFast(i, PortalSeen, portIndexer);
			}

			//Sort the portals with MightSee
			if(mVisParams.mbSortPortals)
			{
				SortPortals();
			}

			if(mVisParams.mbFullVis)
			{
				if(!FloodPortalsSlow(portIndexer, PortalSeen))
				{
					return	false;
				}
			}

			//Don't need this anymore...
			PortalSeen	=null;

			mGFXVisData	=new byte[VisLeafs.Length * NumVisLeafBytes];
			if(mGFXVisData == null)
			{
				Print("VisAllLeafs:  Out of memory for LeafVisBits.\n");
				goto	ExitWithError;
			}

			int	TotalVisibleLeafs	=0;

			for(i=0;i < VisLeafs.Length;i++)
			{
				int	leafSee	=0;
				
				if(!CollectLeafVisBits(i, ref leafSee))
				{
					goto	ExitWithError;
				}
				TotalVisibleLeafs	+=leafSee;
			}

			Print("Total visible areas           : " + TotalVisibleLeafs + "\n");
			Print("Average visible from each area: " + TotalVisibleLeafs / VisLeafs.Length + "\n");

			return	true;

			// ==== ERROR ====
			ExitWithError:
			{
				// Free all the global vis data
				FreeAllVisData();

				return	false;
			}
		}


		bool FloodPortalsSlow(Dictionary<VISPortal, Int32> visIndexer, bool []PortalSeen)
		{
			VISPortal	Portal;
			VISPStack	PStack	=new VISPStack();
			Int32		i, k;

			for(k=0;k < VisPortals.Length;k++)
			{
				VisPortals[k].mDone	=false;
			}

			for(k=0;k < VisPortals.Length;k++)
			{
				Portal	=VisSortedPortals[k];
				
				Portal.mFinalVisBits	=new byte[NumVisPortalBytes];

				//This portal can't see anyone yet...
				for(i=0;i < NumVisPortalBytes;i++)
				{
					Portal.mFinalVisBits[i]	=0;
				}
				for(i=0;i < VisPortals.Length;i++)
				{
					PortalSeen[i]	=false;
				}

				int	CanSee	=0;
				
				for(i=0;i < NumVisPortalBytes;i++)
				{
					PStack.mVisBits[i]	=Portal.mVisBits[i];
				}

				//Setup Source/Pass
				PStack.mSource	=new GBSPPoly(Portal.mPoly);
				PStack.mPass	=null;

				if(!Portal.FloodPortalsSlow_r(Portal, PStack, visIndexer, ref CanSee, VisLeafs))
				{
					return	false;
				}

				PStack.mSource	=null;
				Portal.mDone	=true;

				if(mBSPParms.mbVerbose)
				{
					Print("Portal: " + (k + 1) + " - Fast Vis: "
						+ Portal.mMightSee + ", Full Vis: "
						+ Portal.mCanSee + "\n");
				}
			}			
			return	true;
		}


		bool CollectLeafVisBits(int LeafNum, ref int leafSee)
		{
			VISPortal	Portal, SPortal;
			VISLeaf		Leaf;
			Int32		k, Bit, SLeaf, LeafBitsOfs;
			
			Leaf	=VisLeafs[LeafNum];

			LeafBitsOfs	=LeafNum * NumVisLeafBytes;

			byte	[]PortalBits	=new byte[NumVisPortalBytes];

			//'OR' all portals that this portal can see into one list
			for(Portal=Leaf.mPortals;Portal != null;Portal=Portal.mNext)
			{
				if(Portal.mFinalVisBits != null)
				{
					//Try to use final vis info first
					for(k=0;k < NumVisPortalBytes;k++)
					{
						PortalBits[k]	|=Portal.mFinalVisBits[k];
					}
				}
				else if(Portal.mVisBits != null)
				{
					for(k=0;k < NumVisPortalBytes;k++)
					{
						PortalBits[k]	|=Portal.mVisBits[k];
					}
				}
				else
				{
					Map.Print("No VisInfo for portal.\n");
					return	false;
				}

				Portal.mVisBits			=null;
				Portal.mFinalVisBits	=null;
			}

			// Take this list, and or all leafs that each visible portal looks in to
			for(k=0;k < VisPortals.Length;k++)
			{
				if((PortalBits[k >> 3] & (1 << (k & 7))) != 0)
				{
					SPortal	=VisPortals[k];
					SLeaf	=SPortal.mLeaf;
					Debug.Assert((1 << (SLeaf & 7)) < 256);
					mGFXVisData[LeafBitsOfs + (SLeaf >> 3)]	|=(byte)(1 << (SLeaf & 7));
				}
			}
					
			Bit	=1 << (LeafNum & 7);

			Debug.Assert(Bit < 256);

			//He should not have seen himself (yet...)
			if((mGFXVisData[LeafBitsOfs + (LeafNum >> 3)] & Bit) != 0)
			{
				Map.Print("*WARNING* CollectLeafVisBits:  Leaf:" + LeafNum + " can see himself!\n");
			}
			mGFXVisData[LeafBitsOfs + (LeafNum >> 3)]	|=(byte)Bit;

			for(k=0;k < VisLeafs.Length;k++)
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

			mGFXClusters[LeafNum].mVisOfs	=LeafBitsOfs;

			return	true;
		}


		void SortPortals()
		{
			List<VISPortal>	sortMe	=new List<VISPortal>(VisPortals);

			sortMe.Sort(new VisPortalComparer());

			VisSortedPortals	=sortMe.ToArray();
		}


		void FloodLeafPortalsFast(int leafNum, bool []portSeen,
			Dictionary<VISPortal, Int32> visIndexer)
		{
			VISLeaf		Leaf;
			VISPortal	Portal;

			Leaf	=VisLeafs[leafNum];

			if(Leaf.mPortals == null)
			{
				//GHook.Printf("*WARNING* FloodLeafPortalsFast:  Leaf with no portals.\n");
				return;
			}
			
			int	srcLeaf	=leafNum;

			for(Portal=Leaf.mPortals;Portal != null;Portal=Portal.mNext)
			{
				Portal.mVisBits	=new byte[NumVisPortalBytes];

				//This portal can't see anyone yet...
				for(int i=0;i < NumVisPortalBytes;i++)
				{
					Portal.mVisBits[i]	=0;
				}
				for(int i=0;i < VisPortals.Length;i++)
				{
					portSeen[i]	=false;
				}

				int	mightSee	=0;
				
				Portal.FloodPortalsFast_r(Portal, visIndexer,
					portSeen, VisLeafs, srcLeaf, ref mightSee);
			}
		}


		void FreeFileVisData()
		{
			mGFXVisData		=null;
		}


		bool StartWritingVis(BinaryWriter bw)
		{
			GBSPHeader	header	=new GBSPHeader();
			header.mTAG			="GBSP";
			header.mVersion		=GBSPChunk.GBSP_VERSION;
			header.mBSPTime		=DateTime.Now;

			GBSPChunk	chunk	=new GBSPChunk();
			chunk.mType			=GBSPChunk.GBSP_CHUNK_HEADER;
			chunk.mElements		=1;
			chunk.Write(bw, header);

			if(!SaveGFXModelData(bw))
			{
				return	false;
			}
			if(!SaveVisdGFXNodes(bw))
			{
				return	false;
			}
			if(!SaveGFXPortals(bw))
			{
				return	false;
			}
			if(!SaveGFXBNodes(bw))
			{
				return	false;
			}
			if(!SaveVisdGFXPlanes(bw))
			{
				return	false;
			}
			if(!SaveVisdGFXFaces(bw))
			{
				return	false;
			}
			if(!SaveGFXAreasAndPortals(bw))
			{
				return	false;
			}
			if(!SaveVisdGFXLeafFacesAndSides(bw))
			{
				return	false;
			}
			if(!SaveGFXVerts(bw))
			{
				return	false;
			}
			if(!SaveGFXVertIndexList(bw))
			{
				return	false;
			}
			if(!SaveGFXRGBVerts(bw))
			{
				return	false;
			}
			if(!SaveGFXEntData(bw))
			{
				return	false;
			}
			if(!SaveVisdGFXTexInfos(bw))
			{
				return	false;
			}
			if(!SaveGFXLightData(bw))
			{
				return	false;
			}
			return	true;
		}


		void FreeAllVisData()
		{
			mGFXVisData		=null;

			if(VisPortals != null)
			{
				for(int i=0;i < VisPortals.Length;i++)
				{
					VisPortals[i].mPoly			=null;
					VisPortals[i].mFinalVisBits	=null;
					VisPortals[i].mVisBits		=null;
				}

				VisPortals	=null;
			}
			VisPortals			=null;
			VisSortedPortals	=null;
			VisLeafs			=null;

			FreeGBSPFile();	//Free rest of GBSP GFX data
		}


		bool LoadPortalFile(string PFile)
		{
			Int32		LeafFrom, LeafTo;
			VISPortal	pPortal;
			VISLeaf		pLeaf;
			GBSPPoly	pPoly;
			Int32		i, NumVerts;
			string		TAG;

			pPoly	=null;

			FileStream	fs	=UtilityLib.FileUtil.OpenTitleFile(PFile,
				FileMode.Open, FileAccess.Read);

			BinaryReader	br	=null;

			if(fs == null)		// opps
			{
				Print("LoadPortalFile:  Could not open " + PFile + " for reading.\n");
				goto	ExitWithError;
			}

			br	=new BinaryReader(fs);
			
			// 
			//	Check the TAG
			//
			TAG	=br.ReadString();
			if(TAG != "GBSP_PRTFILE")
			{
				Print("LoadPortalFile:  " + PFile + " is not a GBSP Portal file.\n");
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
			
			VisPortals	=new VISPortal[NumVisPortals];
			if(VisPortals == null)
			{
				Print("LoadPortalFile:  Out of memory for VisPortals.\n");
				goto	ExitWithError;
			}
			
			VisSortedPortals	=new VISPortal[NumVisPortals];
			if(VisSortedPortals == null)
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
			
			VisLeafs	=new VISLeaf[NumVisLeafs];
			if(VisLeafs == null)
			{
				Print("LoadPortalFile:  Out of memory for VisLeafs.\n");
				goto ExitWithError;
			}

			//fill arrays with blank objects
			for(i=0;i < NumVisLeafs;i++)
			{
				VisLeafs[i]	=new VISLeaf();
			}

			//
			//	Load in the portals
			//
			for(i=0;i < NumVisPortals;i++)
			{
				//alloc blank portal
				VisPortals[i]	=new VISPortal();

				NumVerts	=br.ReadInt32();

				pPoly	=new GBSPPoly();

				for(int j=0;j < NumVerts;j++)
				{
					Vector3	vert;
					vert.X	=br.ReadSingle();
					vert.Y	=br.ReadSingle();
					vert.Z	=br.ReadSingle();

					pPoly.AddVert(vert);
				}

				LeafFrom	=br.ReadInt32();
				LeafTo		=br.ReadInt32();
				
				if(LeafFrom >= NumVisLeafs || LeafFrom < 0)
				{
					Print("LoadPortalFile:  Invalid LeafFrom: " + LeafFrom + "\n");
					goto	ExitWithError;
				}

				if(LeafTo >= NumVisLeafs || LeafTo < 0)
				{
					Print("LoadPortalFile:  Invalid LeafTo: " + LeafTo + "\n");
					goto	ExitWithError;
				}

				pLeaf	=VisLeafs[LeafFrom];
				pPortal	=VisPortals[i];

				pPortal.mPoly	=pPoly;
				pPortal.mLeaf	=LeafTo;
				pPortal.mPlane	=new GBSPPlane(pPoly);
				pPortal.mNext	=pLeaf.mPortals;
				pLeaf.mPortals	=pPortal;

				pPortal.CalcPortalInfo();
			}
			
			NumVisLeafBytes	=((NumVisLeafs+63)&~63) >> 3;
			NumVisPortalBytes	=((NumVisPortals+63)&~63) >> 3;

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

				VisPortals			=null;
				VisSortedPortals	=null;
				VisLeafs			=null;
				pPoly				=null;

				return	false;
			}
		}
	}
}
