using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class VISPStack
	{
		public byte		[]mVisBits	=new byte[MAX_TEMP_PORTALS/8];
		public GBSPPoly	mSource;
		public GBSPPoly	mPass;

		public const int	MAX_TEMP_PORTALS	=25000;
	}

	public class VISPortal
	{
		public VISPortal	mNext;
		public GBSPPoly		mPoly;
		public GBSPPlane	mPlane;
		public Vector3		mCenter;
		public float		mRadius;

		public byte		[]mVisBits;
		public byte		[]mFinalVisBits;
		public Int32	mLeaf;
		public Int32	mMightSee;
		public Int32	mCanSee;
		public bool		mDone;


		internal void CalcPortalInfo()
		{
			mCenter	=mPoly.Center();
			mRadius	=mPoly.Radius();
		}


		internal bool CanSeePortal(VISPortal port2)
		{
			if(!mPoly.AnyPartBehind(port2.mPlane))
			{
				//No points of Portal1 behind Portal2, can't possibly see
				return	false;
			}
			if(!port2.mPoly.AnyPartInFront(mPlane))
			{
				//No points of Portal2 in front of Portal1, can't possibly see
				return	false;
			}
			return	true;
		}


		internal void FloodPortalsFastNoGlobals_r(VISPortal DestPortal,
			Dictionary<VISPortal, Int32> visIndexer,
			bool []portSeen, VISLeaf []visLeafs,
			int srcLeaf, ref int mightSee)
		{
			VISLeaf		Leaf;
			VISPortal	Portal;
			Int32		LeafNum;
			Int32		PNum;

//			PNum	=Array.IndexOf(gg.VisPortals, DestPortal);
			Debug.Assert(visIndexer.ContainsKey(DestPortal));
			PNum	=visIndexer[DestPortal];
			
			if(portSeen[PNum])
			{
				return;
			}

			portSeen[PNum]	=true;

			//Add the portal that we are Flooding into, to the original portals visbits
			LeafNum	=DestPortal.mLeaf;

			
			byte	Bit	=(byte)(PNum & 7);
			Bit	=(byte)(1 << Bit);

			if((mVisBits[PNum >> 3] & Bit) == 0)
			{
				mVisBits[PNum>>3]	|=(byte)Bit;
				mMightSee++;
				visLeafs[srcLeaf].mMightSee++;
				mightSee++;
			}

			Leaf	=visLeafs[LeafNum];

			//Now, try and Flood into the leafs that this portal touches
			for(Portal=Leaf.mPortals;Portal != null;Portal=Portal.mNext)
			{
				//If SrcPortal can see this Portal, flood into it...
				if(CanSeePortal(Portal))
				{
					FloodPortalsFastNoGlobals_r(Portal, visIndexer, portSeen, visLeafs, srcLeaf, ref mightSee);
				}
			}
		}


		static bool ClipToSeperators(GBSPPoly Source, GBSPPoly Pass,
			GBSPPoly Target, bool FlipClip, ref GBSPPoly Dest)
		{
			return	Target.SeperatorClip(Source, Pass, FlipClip, ref Dest);
		}


		internal bool FloodPortalsSlow_r(VISPortal DestPortal, VISPStack PrevStack,
			Dictionary<VISPortal, int> visIndexer, ref int canSee,
			VISLeaf []visLeafs)
		{
			VISLeaf		Leaf;
			VISPortal	Portal;
			Int32		LeafNum, j;
			Int32		PNum;
			UInt32		More;
			VISPStack	Stack	=new VISPStack();

			PNum	=visIndexer[DestPortal];

			//Add the portal that we are Flooding into, to the original portals visbits
			byte	Bit	=(byte)(PNum & 7);
			Bit	=(byte)(1 << Bit);

			if((mFinalVisBits[PNum >> 3] & Bit) == 0)
			{
				mFinalVisBits[PNum>>3] |= Bit;
				mCanSee++;
				visLeafs[mLeaf].mCanSee++;
				canSee++;
			}

			//Get the leaf that this portal looks into, and flood from there
			LeafNum	=DestPortal.mLeaf;
			Leaf	=visLeafs[LeafNum];

//			Might	=(uint32*)Stack.VisBits;
//			Vis		=(uint32*)mFinalVisBits;

			// Now, try and Flood into the leafs that this portal touches
			for(Portal=Leaf.mPortals;Portal != null;Portal=Portal.mNext)
			{
				PNum	=visIndexer[Portal];
				Bit		=(byte)(1<<(PNum&7));

				//GHook.Printf("PrevStack VisBits:  %i\n", PrevStack.mVisBits[PNum>>3]);

				//If might see could'nt see it, then don't worry about it
				if((mVisBits[PNum>>3] & Bit) == 0)
				{
					continue;
				}

				if((PrevStack.mVisBits[PNum>>3] & Bit) == 0)
				{
					continue;	// Can't possibly see it
				}

				//If the portal can't see anything we haven't allready seen, skip it
				More	=0;
				if(Portal.mDone)
				{
//					Test = (uint32*)Portal.mFinalVisBits;

					for(j=0;j < mFinalVisBits.Length;j++)
					{
						//there is no & for bytes, can you believe that shit?
						uint	worthless	=(uint)PrevStack.mVisBits[j];
						uint	pieceof		=(uint)Portal.mFinalVisBits[j];
						uint	shit		=worthless & pieceof;
						Stack.mVisBits[j]	=(byte)shit;

						worthless	=Stack.mVisBits[j];
						pieceof		=mFinalVisBits[j];

						More	|=worthless &~ pieceof;
					}
				}
				else
				{
//					Test = (uint32*)Portal.mVisBits;
					for(j=0;j < mFinalVisBits.Length;j++)
					{
						//there is no & for bytes, can you believe that shit?
						uint	worthless	=(uint)PrevStack.mVisBits[j];
						uint	pieceof		=(uint)Portal.mVisBits[j];
						uint	shit		=worthless & pieceof;
						Stack.mVisBits[j]	=(byte)shit;

						worthless	=Stack.mVisBits[j];
						pieceof		=mFinalVisBits[j];

						More	|=worthless &~ pieceof;
					}
				}
				
				if(More == 0 && ((mFinalVisBits[PNum>>3] & Bit) != 0))
				{
					//Can't see anything new
					continue;
				}
				
				//Setup Source/Pass
				Stack.mPass	=new GBSPPoly(Portal.mPoly);

				//Cut away portion of pass portal we can't see through
				if(!Stack.mPass.ClipPoly(mPlane, false))
				{
					return	false;
				}
				if(Stack.mPass.VertCount() < 3)
				{
					continue;
				}

				Stack.mSource	=new GBSPPoly(PrevStack.mSource);

				if(!Stack.mSource.ClipPoly(Portal.mPlane, true))
				{
					return	false;
				}
				if(Stack.mSource.VertCount() < 3)
				{
					continue;
				}

				//If we don't have a PrevStack.mPass, then we don't have enough to look through.
				//This portal can only be blocked by VisBits (Above test)...
				if(PrevStack.mPass == null)
				{
					if(!FloodPortalsSlow_r(Portal, Stack, visIndexer, ref canSee, visLeafs))
					{
						return	false;
					}

					Stack.mSource	=null;
					Stack.mPass		=null;
					continue;
				}

				if(!ClipToSeperators(Stack.mSource, PrevStack.mPass, Stack.mPass, false, ref Stack.mPass))
				{
					return	false;
				}

				if(Stack.mPass == null || Stack.mPass.VertCount() < 3)
				{
					Stack.mSource	=null;
					continue;
				}
				
				if(!ClipToSeperators(PrevStack.mPass, Stack.mSource, Stack.mPass, true, ref Stack.mPass))
				{
					return	false;
				}
				if(Stack.mPass == null || Stack.mPass.VertCount() < 3)
				{
					Stack.mSource	=null;
					continue;
				}

				//Flood into it...
				if(!FloodPortalsSlow_r(Portal, Stack, visIndexer, ref canSee, visLeafs))
				{
					return	false;
				}

				Stack.mSource	=null;
				Stack.mPass		=null;
			}
			return	true;
		}
	}


	class VisLeafComparer : IComparer<VISLeaf>
	{
		public int Compare(VISLeaf x, VISLeaf y)
		{
			if(x.mMightSee == y.mMightSee)
			{
				return	0;
			}
			if(x.mMightSee < y.mMightSee)
			{
				return	-1;
			}
			return	1;
		}
	}


	public class VisPortalComparer : IComparer<VISPortal>
	{
		public int Compare(VISPortal x, VISPortal y)
		{
			if(x.mMightSee == y.mMightSee)
			{
				return	0;
			}
			if(x.mMightSee < y.mMightSee)
			{
				return	-1;
			}
			return	1;
		}
	}


	public class VISLeaf
	{
		public VISPortal	mPortals;
		public Int32		mMightSee;
		public Int32		mCanSee;


		internal void FloodPortalsFast(Int32 leafNum, Int32 numVisPortalBytes,
			Dictionary<Int32, VISLeaf> visLeafs, List<VISPortal> visPortals)
		{
			if(mPortals == null)
			{
				//GHook.Printf("*WARNING* FloodLeafPortalsFast:  Leaf with no portals.\n");
				return;
			}
			
			Int32	SrcLeaf	=leafNum;

			for(VISPortal Portal = mPortals;Portal != null;Portal = Portal.mNext)
			{
				Portal.mVisBits	=new byte[numVisPortalBytes];

				//This portal can't see anyone yet...
				int	mightSee	=0;

				Dictionary<Int32, byte>	portalSeen	=new Dictionary<Int32, byte>();
				
				FloodPortalsFast_r(Portal, Portal, SrcLeaf, visLeafs, visPortals, portalSeen, ref mightSee);
			}
		}


		void FloodPortalsFast_r(VISPortal SrcPortal, VISPortal DestPortal,
			Int32 SrcLeaf, Dictionary<Int32, VISLeaf> visLeafs, List<VISPortal> visPortals,
			Dictionary<Int32, byte>	portalSeen, ref Int32 mightSee)
		{
			VISLeaf		Leaf;
			VISPortal	Portal;
			Int32		LeafNum;
			Int32		PNum	=-1;

			//this is gonna be slow
			PNum	=visPortals.IndexOf(DestPortal);

			if(portalSeen.ContainsKey(PNum))
			{
				if(portalSeen[PNum] != 0)
				{
					return;
				}
				portalSeen[PNum]	=1;
			}
			else
			{
				portalSeen.Add(PNum, 1);
			}

			//Add the portal that we are Flooding into, to the original portals visbits
			LeafNum	=DestPortal.mLeaf;
			
			Int32	Bit	=1 << (PNum & 7);
			if((SrcPortal.mVisBits[PNum>>3] & Bit) == 0)
			{
				SrcPortal.mVisBits[PNum>>3]	|=(byte)Bit;
				SrcPortal.mMightSee++;
				visLeafs[SrcLeaf].mMightSee++;
				mightSee++;
			}

			Leaf	=visLeafs[LeafNum];

			//Now, try and Flood into the leafs that this portal touches
			for(Portal = mPortals;Portal != null;Portal = Portal.mNext)
			{
				//If SrcPortal can see this Portal, flood into it...
				if(SrcPortal.CanSeePortal(Portal))
				{
					FloodPortalsFast_r(SrcPortal, Portal, SrcLeaf, visLeafs, visPortals, portalSeen, ref mightSee);
				}
			}
		}


		internal bool CollectLeafVisBits(int LeafNum, ref int LeafSee,
			byte[] leafVisBits, byte[] portalBits,
			int NumVisPortalBytes, int NumVisLeafBytes,
			List<GFXCluster> gfxClusters, Dictionary<Int32, VISLeaf> visLeafs,
			List<VISPortal> visPortals)
		{
			VISPortal	Portal, SPortal;
			VISLeaf		Leaf;
			Int32		k, Bit, SLeaf;
			Int32		LeafBitsOfs;
			
			Leaf	=visLeafs[LeafNum];

			LeafBitsOfs	=LeafNum * NumVisLeafBytes;

			for(int i=0;i < NumVisPortalBytes;i++)
			{
				portalBits[i]	=0;
			}

			//'OR' all portals that this portal can see into one list
			for(Portal = mPortals;Portal != null;Portal = Portal.mNext)
			{
				if(Portal.mFinalVisBits != null)
				{
					//Try to use final vis info first
					for(k=0;k < NumVisPortalBytes;k++)
					{
						portalBits[k]	|=Portal.mFinalVisBits[k];
					}
				}
				else if(Portal.mVisBits != null)
				{
					for(k=0;k < NumVisPortalBytes;k++)
					{
						portalBits[k]	|=Portal.mVisBits[k];
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

			//Take this list, and or all leafs that each visible portal looks in to
			for(k=0;k < visPortals.Count;k++)
			{
				if((portalBits[k>>3] & (1<<(k&7))) != 0)
				{
					SPortal	=visPortals[k];
					SLeaf	=SPortal.mLeaf;
					leafVisBits[LeafBitsOfs + (SLeaf >> 3)]	|=(byte)(1 << (SLeaf & 7));
				}
			}
					
			Bit	=1 << (LeafNum & 7);

			//He should not have seen himself (yet...)
			if((leafVisBits[LeafBitsOfs + (LeafNum >> 3)] & Bit) != 0)
			{
				Map.Print("*WARNING* CollectLeafVisBits:  Leaf:" + LeafNum + " can see himself!\n");
			}

			// Make sure he can see himself!!!
			leafVisBits[LeafBitsOfs + (LeafNum >> 3)]	|=(byte)Bit;

			for(k=0;k < visLeafs.Count;k++)
			{
				Bit	=1 << (k & 7);
				if((leafVisBits[LeafBitsOfs + (k >> 3)] & Bit) != 0)
				{
					LeafSee++;
				}
			}

			if(LeafSee == 0)
			{
				Map.Print("CollectLeafVisBits:  Leaf can't see nothing.\n");
				return	false;
			}

			GFXCluster	clust	=new GFXCluster();

			clust.mVisOfs	=LeafBitsOfs;

			gfxClusters.Add(clust);

			Debug.Assert(gfxClusters.Count == LeafNum + 1);

			return	true;
		}
	}
}
