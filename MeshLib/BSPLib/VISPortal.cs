using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework;


namespace BSPLib
{
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

			float	bestDist = 0.0f;

			foreach(Vector3	vert in mPoly.mVerts)
			{
				Vector3	toCent	=vert - mCenter;

				float	dist	=toCent.Length();
				if(dist > bestDist)
				{
					bestDist	=dist;
				}
			}
			mRadius	=bestDist;
		}


		internal bool CanSeePortal(VISPortal Portal2)
		{
			Int32	i;
			float	Dist;

			for(i=0;i < mPoly.mVerts.Count;i++)
			{
				Dist	=Portal2.mPlane.DistanceFast(mPoly.mVerts[i]);
				if(Dist < -UtilityLib.Mathery.ON_EPSILON)
				{
					break;
				}
			}

			if(i == mPoly.mVerts.Count)
			{
				//No points of Portal1 behind Portal2, can't possibly see
				return	false;
			}

			for(i=0;i < Portal2.mPoly.mVerts.Count;i++)
			{
				Dist	=mPlane.DistanceFast(Portal2.mPoly.mVerts[i]);
				if(Dist > UtilityLib.Mathery.ON_EPSILON)
				{
					break;
				}
			}

			if(i == Portal2.mPoly.mVerts.Count)
			{
				//No points of Portal2 in front of Portal1, can't possibly see
				return	false;
			}
			return	true;
		}


		internal void FloodPortalsFast_r(GBSPGlobals gg,
			VISPortal DestPortal, Dictionary<VISPortal, Int32> visIndexer)
		{
			VISLeaf		Leaf;
			VISPortal	Portal;
			Int32		LeafNum;
			Int32		PNum;

//			PNum	=Array.IndexOf(gg.VisPortals, DestPortal);
			Debug.Assert(visIndexer.ContainsKey(DestPortal));
			PNum	=visIndexer[DestPortal];
			
			if(gg.PortalSeen[PNum] != 0)
			{
				return;
			}

			gg.PortalSeen[PNum]	=1;

			//Add the portal that we are Flooding into, to the original portals visbits
			LeafNum	=DestPortal.mLeaf;
			
			Int32	Bit	=1 << (PNum & 7);
			if((mVisBits[PNum >> 3] & Bit) == 0)
			{
				Debug.Assert(Bit < 256);
				mVisBits[PNum>>3]	|=(byte)Bit;
				mMightSee++;
				gg.VisLeafs[gg.SrcLeaf].mMightSee++;
				gg.MightSee++;
			}

			Leaf	=gg.VisLeafs[LeafNum];

			//Now, try and Flood into the leafs that this portal touches
			for(Portal=Leaf.mPortals;Portal != null;Portal=Portal.mNext)
			{
				//If SrcPortal can see this Portal, flood into it...
				if(CanSeePortal(Portal))
				{
					FloodPortalsFast_r(gg, Portal, visIndexer);
				}
			}
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
