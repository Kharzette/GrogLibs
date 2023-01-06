using System;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using BSPCore;


namespace BSPVis
{
	class VISPStack
	{
		internal byte		[]mVisBits	=new byte[MAX_TEMP_PORTALS/8];
		internal GBSPPoly	mSource;
		internal GBSPPoly	mPass;
		internal VISPortal	mPortal;
		internal VISLeaf	mLeaf;
		internal VISPStack	mNext;
		internal GBSPPlane	mPortalPlane;

		public const int	MAX_TEMP_PORTALS	=64000;


		internal void FreeAll(VisPools vp)
		{
			Free(vp);

			vp.mStacks.FlagFreeItem(this);
		}

		internal void Free(VisPools vp)
		{
			if(mSource != null)
			{
				if(mSource.mVerts != null)
				{
					vp.mClipPools.FreeVerts(mSource.mVerts);
				}
				vp.mPolys.FlagFreeItem(mSource);
				mSource	=null;
			}

			if(mPass != null)
			{
				if(mPass.mVerts != null)
				{
					vp.mClipPools.FreeVerts(mPass.mVerts);
				}
				vp.mPolys.FlagFreeItem(mPass);
				mPass	=null;
			}
		}
	}


	internal class VISPortal
	{
		internal GBSPPlane	mPlane;
		internal Vector3	mCenter;
		internal float		mRadius;
		internal GBSPPoly	mPoly;

		internal byte	[]mPortalFlood;
		internal byte	[]mPortalVis;

		internal Int32	mPortNum;		//index into portal array or portal num for vis
		internal Int32	mClusterTo;		//cluster portal aims into
		internal Int32	mClusterFrom;	//cluster portal lives in
		internal Int32	mMightSee;
		internal Int32	mCanSee;
		internal bool	mbDone;


		internal void Read(BinaryReader br)
		{
			mPlane.Read(br);
			mCenter.X	=br.ReadSingle();
			mCenter.Y	=br.ReadSingle();
			mCenter.Z	=br.ReadSingle();
			mRadius		=br.ReadSingle();
			mPoly		=new GBSPPoly(0);
			mPoly.Read(br);

			mPortalFlood	=UtilityLib.FileUtil.ReadByteArray(br);
			mPortalVis		=UtilityLib.FileUtil.ReadByteArray(br);

			mPortNum		=br.ReadInt32();
			mClusterTo		=br.ReadInt32();
			mClusterFrom	=br.ReadInt32();
			mMightSee		=br.ReadInt32();
			mCanSee			=br.ReadInt32();
			mbDone			=br.ReadBoolean();
		}


		internal void Write(BinaryWriter bw)
		{
			mPlane.Write(bw);
			bw.Write(mCenter.X);
			bw.Write(mCenter.Y);
			bw.Write(mCenter.Z);
			bw.Write(mRadius);
			mPoly.Write(bw);

			UtilityLib.FileUtil.WriteArray(mPortalFlood, bw);
			UtilityLib.FileUtil.WriteArray(mPortalVis, bw);

			bw.Write(mPortNum);
			bw.Write(mClusterTo);
			bw.Write(mClusterFrom);
			bw.Write(mMightSee);
			bw.Write(mCanSee);
			bw.Write(mbDone);
		}


		internal void WriteVisBits(BinaryWriter bw)
		{
			UtilityLib.FileUtil.WriteArray(mPortalVis, bw);
		}


		internal void ReadVisBits(BinaryReader br)
		{
			mPortalVis	=UtilityLib.FileUtil.ReadByteArray(br);
		}


		internal void CalcPortalInfo()
		{
			mCenter	=mPoly.Center();
			mRadius	=mPoly.Radius();
		}


		static internal bool CollectBits(List<VISPortal> ports, byte []portBits)
		{
			//'OR' all portals that this portal can see into one list
			foreach(VISPortal p in ports)
			{
				if(p.mPortalVis != null)
				{
					//Try to use final vis info first
					for(int k=0;k < portBits.Length;k++)
					{
						portBits[k]	|=p.mPortalVis[k];
					}
				}
				else if(p.mPortalFlood != null)
				{
					for(int k=0;k < portBits.Length;k++)
					{
						portBits[k]	|=p.mPortalFlood[k];
					}
				}
				else
				{
					CoreEvents.Print("No VisInfo for portal.\n");
					return	false;
				}

				p.mPortalFlood	=null;
				p.mPortalVis	=null;
			}
			return	true;
		}


		static unsafe UInt32 AndTogetherDWords(byte []src1, byte []src2, byte []src3, byte []dest)
		{
			int		len	=src2.Length;
			UInt32	ret	=0;

			fixed(byte *pSrc1 = src1, pSrc2 = src2, pSrc3 = src3, pDest = dest)
			{
				byte	*pS1	=pSrc1;
				byte	*pS2	=pSrc2;
				byte	*pS3	=pSrc3;
				byte	*pD		=pDest;

				for(int i=0;i < len / 4;i++)
				{
					UInt32	anded=*((UInt32 *)pS1) & *((UInt32 *)pS2);
					UInt32	val3	=*((UInt32 *)pS3);

					*((UInt32 *)pD)	=anded;

					//and the bytes one at a time
					ret	|=((anded & 0xFF) &~ (val3 & 0xFF));
					ret	|=(((anded >> 8) & 0xFF) &~ ((val3 >> 8) & 0xFF));
					ret	|=(((anded >> 16) & 0xFF) &~ ((val3 >> 16) & 0xFF));
					ret	|=(((anded >> 24) & 0xFF) &~ ((val3 >> 24) & 0xFF));

					pS1	+=4;
					pS2	+=4;
					pS3	+=4;
					pD	+=4;
				}

				//get leftover
				for(int i=0;i < len % 4;i++)
				{
					UInt32	anded	=*((UInt32 *)pSrc1) & *((UInt32 *)pSrc2);

					*pD	=(byte)anded;

					ret	|=(byte)(anded &~ *((UInt32 *)pS3));

					pS1++;
					pS2++;
					pS3++;
					pD++;
				}
			}
			return	ret;
		}


		internal static unsafe UInt64 AndTogetherQWords(byte []src1, byte []src2, byte []src3, byte []dest)
		{
			int		len	=src2.Length;
			UInt64	ret	=0;

			fixed(byte *pSrc1 = src1, pSrc2 = src2, pSrc3 = src3, pDest = dest)
			{
				byte	*pS1	=pSrc1;
				byte	*pS2	=pSrc2;
				byte	*pS3	=pSrc3;
				byte	*pD		=pDest;

				for(int i=0;i < len / 8;i++)
				{
					UInt64	anded	=*((UInt64 *)pS1) & *((UInt64 *)pS2);
					UInt64	val3	=*((UInt64 *)pS3);

					*((UInt64 *)pD)	=anded;

					//and the bytes one at a time
					ret	|=((anded & 0xFF) &~ (val3 & 0xFF));
					ret	|=(((anded >> 8) & 0xFF) &~ ((val3 >> 8) & 0xFF));
					ret	|=(((anded >> 16) & 0xFF) &~ ((val3 >> 16) & 0xFF));
					ret	|=(((anded >> 24) & 0xFF) &~ ((val3 >> 24) & 0xFF));
					ret	|=(((anded >> 32) & 0xFF) &~ ((val3 >> 32) & 0xFF));
					ret	|=(((anded >> 40) & 0xFF) &~ ((val3 >> 40) & 0xFF));
					ret	|=(((anded >> 48) & 0xFF) &~ ((val3 >> 48) & 0xFF));
					ret	|=(((anded >> 56) & 0xFF) &~ ((val3 >> 56) & 0xFF));

					pS1	+=8;
					pS2	+=8;
					pS3	+=8;
					pD	+=8;
				}

				//get leftover
				for(int i=0;i < len % 8;i++)
				{
					UInt32	anded	=*((UInt32 *)pSrc1) & *((UInt32 *)pSrc2);

					*pD	=(byte)anded;

					ret	|=(byte)(anded &~ *((UInt32 *)pS3));

					pS1++;
					pS2++;
					pS3++;
					pD++;
				}
			}
			return	ret;
		}


		//taken from quake 2's vis compiler
		internal static void RecursiveLeafFlow(FlowParams fp, VisPools vp)
		{
			VISLeaf	leaf	=vp.mVisLeafs[fp.mLeafNum];
			
			VISPStack	stack	=vp.mStacks.GetFreeItem();

			vp.mIterations++;

			fp.mPrevStack.mNext	=stack;
			
			stack.mNext		=null;
			stack.mLeaf		=leaf;
			stack.mPortal	=null;
			
			byte	[]might	=stack.mVisBits;
			byte	[]vis	=fp.mDestPort.mPortalVis;
			
			//check all portals for flowing into other leafs
			for(int i=0;i < leaf.mPortals.Count;i++)
			{
				VISPortal	p	=leaf.mPortals[i];

				int	pnum	=p.mPortNum;

				if((fp.mPrevStack.mVisBits[pnum >> 3] & (1 << (pnum & 7))) == 0)
				{
					continue;	// can't possibly see it
				}
				
				//if the portal can't see anything we haven't allready seen, skip it
				byte	[]test	=null;
				if(p.mbDone)
				{
					test	=p.mPortalVis;
				}
				else
				{
					test	=p.mPortalFlood;
				}

				UInt64	more	=AndTogetherQWords(fp.mPrevStack.mVisBits, test, vis, might);
				if((more == 0) && (vis[pnum >> 3] & (1 << (pnum & 7))) != 0)
				{	//can't see anything new
					continue;
				}

				//get plane of portal, point normal into the neighbor leaf
				stack.mPortalPlane	=p.mPlane;
				stack.mPortal		=p;
				stack.mNext			=null;

				stack.mPass			=vp.mPolys.GetFreeItem();
				stack.mPass.mVerts	=vp.mClipPools.DupeVerts(p.mPoly.mVerts);
				if(!stack.mPass.ClipPoly(fp.mDestPort.mPlane, false, vp.mClipPools))
				{
					stack.Free(vp);
					continue;
				}
				if(stack.mPass.mVerts == null)
				{
					stack.Free(vp);
					continue;
				}

				stack.mSource			=vp.mPolys.GetFreeItem();
				stack.mSource.mVerts	=vp.mClipPools.DupeVerts(fp.mPrevStack.mSource.mVerts);
				if(!stack.mSource.ClipPoly(p.mPlane, true, vp.mClipPools))
				{
					stack.Free(vp);
					continue;
				}
				if(stack.mSource.mVerts == null)
				{
					stack.Free(vp);
					continue;
				}

				if(fp.mPrevStack.mPass == null)
				{	//the second leaf can only be blocked if coplanar

					//mark the portal as visible
					vis[pnum >> 3]	|=(byte)(1 << (pnum & 7));

					FlowParams	fp2	=fp;
					fp2.mLeafNum	=p.mClusterTo;
					fp2.mPrevStack	=stack;
					RecursiveLeafFlow(fp2, vp);
					stack.Free(vp);
					continue;
				}

				if(!stack.mPass.SeperatorClip(stack.mSource, fp.mPrevStack.mPass, false, vp.mClipPools))
				{
					stack.Free(vp);
					continue;
				}
				if(stack.mPass.mVerts == null)
				{
					stack.Free(vp);
					continue;
				}
				if(!stack.mPass.SeperatorClip(fp.mPrevStack.mPass, stack.mSource, true, vp.mClipPools))
				{
					stack.Free(vp);
					continue;
				}
				if(stack.mPass.mVerts == null)
				{
					stack.Free(vp);
					continue;
				}

				//mark the portal as visible
				vis[pnum >> 3]	|=(byte)(1 << (pnum & 7));

				//flow through it for real
				FlowParams	fp3	=fp;
				fp3.mLeafNum	=p.mClusterTo;
				fp3.mPrevStack	=stack;
				RecursiveLeafFlow(fp3, vp);

				stack.Free(vp);
			}
			stack.FreeAll(vp);
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


	internal class VisPortalComparer : IComparer<VISPortal>
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


	internal class VISLeaf
	{
		internal List<VISPortal>	mPortals	=new List<VISPortal>();
		internal Int32				mMightSee;
		internal Int32				mCanSee;


		internal void Write(BinaryWriter bw)
		{
			bw.Write(mPortals.Count);
			foreach(VISPortal vp in mPortals)
			{
				bw.Write(vp.mPortNum);
			}
			bw.Write(mMightSee);
			bw.Write(mCanSee);
		}


		internal void Read(BinaryReader br, VISPortal[] ports)
		{
			Int32	numPorts	=br.ReadInt32();
			for(int i=0;i < numPorts;i++)
			{
				int	idx	=br.ReadInt32();
				mPortals.Add(ports[idx]);
			}
			mMightSee	=br.ReadInt32();
			mCanSee		=br.ReadInt32();
		}
	}
}
