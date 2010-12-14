using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	class VISPStack
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


		static internal bool CollectBits(VISPortal port, byte []portBits)
		{
			//'OR' all portals that this portal can see into one list
			for(VISPortal p=port;p != null;p=p.mNext)
			{
				if(p.mFinalVisBits != null)
				{
					//Try to use final vis info first
					for(int k=0;k < portBits.Length;k++)
					{
						portBits[k]	|=port.mFinalVisBits[k];
					}
				}
				else if(port.mVisBits != null)
				{
					for(int k=0;k < portBits.Length;k++)
					{
						portBits[k]	|=port.mVisBits[k];
					}
				}
				else
				{
					Map.Print("No VisInfo for portal.\n");
					return	false;
				}

				port.mVisBits		=null;
				port.mFinalVisBits	=null;
			}
			return	true;
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


		internal void FloodPortalsFast_r(VISPortal DestPortal,
			Dictionary<VISPortal, Int32> visIndexer,
			bool []portSeen, VISLeaf []visLeafs,
			int srcLeaf, ref int mightSee)
		{
			VISLeaf		Leaf;
			VISPortal	Portal;
			Int32		LeafNum;
			Int32		PNum;

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
					FloodPortalsFast_r(Portal, visIndexer, portSeen, visLeafs, srcLeaf, ref mightSee);
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
	}
}
