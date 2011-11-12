using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using BSPCore;


namespace BSPVis
{
	class ReturnCondition
	{
		internal VISPortal	mListSpot;
		internal VISPortal	mDPort;
		internal VISPStack	mPrevStack;
		internal VISPStack	mCurStack;
	}

	class VISPStack
	{
		public byte		[]mVisBits	=new byte[MAX_TEMP_PORTALS/8];
		public GBSPPoly	mSource;
		public GBSPPoly	mPass;

		public const int	MAX_TEMP_PORTALS	=25000;


		internal void FreeAll(VisPools vp, ClipPools cp)
		{
			Free(vp, cp);

			vp.mStacks.FlagFreeItem(this);
		}

		internal void Free(VisPools vp, ClipPools cp)
		{
			if(mSource != null)
			{
				if(mSource.mVerts != null)
				{
					cp.FreeVerts(mSource.mVerts);
				}
				vp.mPolys.FlagFreeItem(mSource);
				mSource	=null;
			}

			if(mPass != null)
			{
				if(mPass.mVerts != null)
				{
					cp.FreeVerts(mPass.mVerts);
				}
				vp.mPolys.FlagFreeItem(mPass);
				mPass	=null;
			}
		}
	}


	internal class VISPortal
	{
		internal VISPortal	mNext;
		internal GBSPPoly	mPoly;
		internal GBSPPlane	mPlane;
		internal Vector3	mCenter;
		internal float		mRadius;

		internal byte	[]mVisBits;
		internal byte	[]mFinalVisBits;
		internal Int32	mPortNum;		//index into portal array or portal num for vis
		internal Int32	mLeaf;
		internal Int32	mMightSee;
		internal Int32	mCanSee;
		internal bool	mbDone;


		internal void Read(BinaryReader br, List<int> indexes)
		{
			int	idx	=br.ReadInt32();
			indexes.Add(idx);

			mPoly	=new GBSPPoly(0);
			mPoly.Read(br);
			mPlane.Read(br);
			mCenter.X	=br.ReadSingle();
			mCenter.Y	=br.ReadSingle();
			mCenter.Z	=br.ReadSingle();
			mRadius		=br.ReadSingle();

			int	vblen	=br.ReadInt32();
			if(vblen > 0)
			{
				mVisBits	=br.ReadBytes(vblen);
			}

			int	fvblen	=br.ReadInt32();
			if(fvblen > 0)
			{
				mFinalVisBits	=br.ReadBytes(fvblen);
			}

			mPortNum	=br.ReadInt32();
			mLeaf		=br.ReadInt32();
			mMightSee	=br.ReadInt32();
			mCanSee		=br.ReadInt32();
			mbDone		=br.ReadBoolean();
		}


		internal void WriteVisBits(BinaryWriter bw)
		{
			if(mFinalVisBits == null)
			{
				bw.Write(-1);
			}
			else
			{
				bw.Write(mFinalVisBits.Length);
				if(mFinalVisBits.Length > 0)
				{
					bw.Write(mFinalVisBits, 0, mFinalVisBits.Length);
				}
			}
		}


		internal void ReadVisBits(BinaryReader br)
		{
			int	fvblen	=br.ReadInt32();
			if(fvblen > 0)
			{
				mFinalVisBits	=br.ReadBytes(fvblen);
			}
		}


		internal void Write(BinaryWriter bw)
		{
			if(mNext != null)
			{
				bw.Write(mNext.mPortNum);
			}
			else
			{
				bw.Write(-1);
			}
			mPoly.Write(bw);
			mPlane.Write(bw);
			bw.Write(mCenter.X);
			bw.Write(mCenter.Y);
			bw.Write(mCenter.Z);
			bw.Write(mRadius);

			if(mVisBits == null)
			{
				bw.Write(-1);
			}
			else
			{
				bw.Write(mVisBits.Length);
				if(mVisBits.Length > 0)
				{
					bw.Write(mVisBits, 0, mVisBits.Length);
				}
			}

			if(mFinalVisBits == null)
			{
				bw.Write(-1);
			}
			else
			{
				bw.Write(mFinalVisBits.Length);
				if(mFinalVisBits.Length > 0)
				{
					bw.Write(mFinalVisBits, 0, mFinalVisBits.Length);
				}
			}

			bw.Write(mPortNum);
			bw.Write(mLeaf);
			bw.Write(mMightSee);
			bw.Write(mCanSee);
			bw.Write(mbDone);
		}


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
						portBits[k]	|=p.mFinalVisBits[k];
					}
				}
				else if(p.mVisBits != null)
				{
					for(int k=0;k < portBits.Length;k++)
					{
						portBits[k]	|=p.mVisBits[k];
					}
				}
				else
				{
					CoreEvents.Print("No VisInfo for portal.\n");
					return	false;
				}

				p.mVisBits		=null;
				p.mFinalVisBits	=null;
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


		internal void FloodPortalsFast_r(VISPortal destPortal,
			bool []portSeen, VISLeaf []visLeafs,
			int srcLeaf, ref int mightSee)
		{
			Int32	portNum	=destPortal.mPortNum;
			
			if(portSeen[portNum])
			{
				return;
			}

			portSeen[portNum]	=true;

			//Add the portal that we are Flooding into, to the original portals visbits
			Int32	leafNum	=destPortal.mLeaf;
			
			byte	Bit	=(byte)(portNum & 7);
			Bit	=(byte)(1 << Bit);

			if((mVisBits[portNum >> 3] & Bit) == 0)
			{
				mVisBits[portNum>>3]	|=(byte)Bit;
				mMightSee++;
				visLeafs[srcLeaf].mMightSee++;
				mightSee++;
			}

			VISLeaf	leaf	=visLeafs[leafNum];

			//Now, try and Flood into the leafs that this portal touches
			for(VISPortal port=leaf.mPortals;port != null;port=port.mNext)
			{
				//If SrcPortal can see this Portal, flood into it...
				if(CanSeePortal(port))
				{
					FloodPortalsFast_r(port, portSeen, visLeafs, srcLeaf, ref mightSee);
				}
			}
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

					*((UInt32 *)pD)	=anded;

					ret	|=anded &~ *((UInt32 *)pS3);

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


		static unsafe UInt64 AndTogetherQWords(byte []src1, byte []src2, byte []src3, byte []dest)
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
					UInt64	anded=*((UInt64 *)pS1) & *((UInt64 *)pS2);

					*((UInt64 *)pD)	=anded;

					ret	|=anded &~ *((UInt64 *)pS3);

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


		internal bool FloodPortalsSlowNoR(VISPortal destPort, VISPStack prevStack,
			ref int canSee, VISLeaf []visLeafs, VisPools vPools, ClipPools cPools)
		{
			Stack<VISPortal>	destPorts	=new Stack<VISPortal>();
			Stack<VISPStack>	prevStacks	=new Stack<VISPStack>();

			Stack<ReturnCondition>	rets	=new Stack<ReturnCondition>();

			VISPStack	stack	=null;

			int	maxStackDepth	=0;
			int	maxRetDepth		=0;

			destPorts.Push(destPort);
			prevStacks.Push(prevStack);

			while(destPorts.Count > 0 || rets.Count > 0)
			{
				VISPortal	port	=null;
				VISPortal	dPort	=null;
				VISPStack	prStack	=null;

				if(rets.Count > maxRetDepth)
				{
					maxRetDepth	=rets.Count;
				}

				if(rets.Count > 0)
				{
					ReturnCondition	ret	=rets.Pop();
					dPort		=ret.mDPort;
					prStack		=ret.mPrevStack;
					port		=ret.mListSpot;
					stack		=ret.mCurStack;
					stack.Free(vPools, cPools);
				}

startLoop:
				if(destPorts.Count > maxStackDepth)
				{
					maxStackDepth	=destPorts.Count;
				}
				bool	nullStuff	=(dPort == null);
				if(nullStuff)
				{
					dPort	=destPorts.Pop();
					prStack	=prevStacks.Pop();
					stack	=vPools.mStacks.GetFreeItem();
				}

				Int32	portNum	=dPort.mPortNum;
				byte	Bit		=(byte)(portNum & 7);
				Bit				=(byte)(1 << Bit);
				bool	bNoFree	=false;

				if(nullStuff)
				{
					//Add the portal that we are Flooding into, to the original portals visbits
					if((mFinalVisBits[portNum >> 3] & Bit) == 0)
					{
						mFinalVisBits[portNum >> 3]	|=Bit;
						mCanSee++;
						visLeafs[mLeaf].mCanSee++;
						canSee++;
					}
				}

				//Get the leaf that this portal looks into, and flood from there
				Int32	leafNum	=dPort.mLeaf;
				VISLeaf	leaf	=visLeafs[leafNum];

				bool	retFalse	=false;

				// Now, try and Flood into the leafs that this portal touches
				for(port=(port == null)? leaf.mPortals : port;port != null;port=port.mNext)
				{
					//note this reuse of portNum
					portNum	=port.mPortNum;
					Bit		=(byte)(1 << (portNum & 7));

					//If might see could'nt see it, then don't worry about it
					if((prStack.mVisBits[portNum >> 3] & Bit) == 0)
					{
						continue;	//Can't possibly see it
					}

					//If the portal can't see anything we haven't allready seen, skip it
					UInt64	more	=0;
					if(port.mbDone)
					{
						for(int j=0;j < mFinalVisBits.Length;j++)
						{
							//there is no & for bytes, can you believe that?
							uint	prevBit		=(uint)prStack.mVisBits[j];
							uint	portBit		=(uint)port.mFinalVisBits[j];
							uint	bothBit		=prevBit & portBit;
							stack.mVisBits[j]	=(byte)bothBit;

							prevBit	=stack.mVisBits[j];
							portBit	=mFinalVisBits[j];

							more	|=prevBit &~ portBit;
						}
					}
					else
					{
						//qwords and dwords seem about the same performancewise
						more	=AndTogetherQWords(prStack.mVisBits, port.mVisBits, mFinalVisBits, stack.mVisBits);
					}
				
					if(more == 0 && ((mFinalVisBits[portNum>>3] & Bit) != 0))
					{
						//Can't see anything new
						continue;
					}

					//Setup Source/Pass
					stack.mPass			=vPools.mPolys.GetFreeItem();
					stack.mPass.mVerts	=cPools.DupeVerts(port.mPoly.mVerts);

					//Cut away portion of pass portal we can't see through
					if(!stack.mPass.ClipPoly(mPlane, false, cPools))
					{
						stack.FreeAll(vPools, cPools);
						retFalse	=true;
						break;
					}
					if(stack.mPass.VertCount() < 3)
					{
						stack.Free(vPools, cPools);
						continue;
					}

					stack.mSource			=vPools.mPolys.GetFreeItem();
					stack.mSource.mVerts	=cPools.DupeVerts(prStack.mSource.mVerts);

					if(!stack.mSource.ClipPoly(port.mPlane, true, cPools))
					{
						stack.FreeAll(vPools, cPools);
						retFalse	=true;
						break;
					}
					if(stack.mSource.VertCount() < 3)
					{
						stack.Free(vPools, cPools);
						continue;
					}

					//If we don't have a PrevStack.mPass, then we don't have enough to look through.
					//This portal can only be blocked by VisBits (Above test)...
					if(prStack.mPass == null)
					{
						destPorts.Push(port);
						prevStacks.Push(stack);

						Debug.Assert(port != dPort);

						if(port.mNext != null)
						{
							ReturnCondition	ret	=new ReturnCondition();
							ret.mListSpot	=port.mNext;
							ret.mDPort		=dPort;
							ret.mPrevStack	=prStack;
							ret.mCurStack	=stack;
							rets.Push(ret);
						}
						dPort	=null;
						port	=null;
						goto	startLoop;
					}

					if(!stack.mPass.SeperatorClip(stack.mSource, prStack.mPass, false, cPools))
					{
						stack.FreeAll(vPools, cPools);
						retFalse	=true;
						break;
					}
					if(stack.mPass == null || stack.mPass.VertCount() < 3)
					{
						stack.Free(vPools, cPools);
						continue;
					}

					if(!stack.mPass.SeperatorClip(prStack.mPass, stack.mSource, true, cPools))
					{
						stack.FreeAll(vPools, cPools);
						retFalse	=true;
						break;
					}
					if(stack.mPass == null || stack.mPass.VertCount() < 3)
					{
						stack.Free(vPools, cPools);
						continue;
					}

					//Flood into it...
					destPorts.Push(port);
					prevStacks.Push(stack);

					if(port.mNext != null)
					{
						ReturnCondition	ret	=new ReturnCondition();
						ret.mListSpot	=port.mNext;
						ret.mDPort		=dPort;
						ret.mPrevStack	=prStack;
						ret.mCurStack	=stack;
						rets.Push(ret);
					}
					dPort	=null;
					port	=null;
					bNoFree	=true;
					goto	startLoop;
				}

				if(bNoFree)
				{
					bNoFree	=false;
				}
				else
				{
					stack.FreeAll(vPools, cPools);
				}

				if(retFalse)
				{
					CoreEvents.Print("Max Depth: " + maxStackDepth + " , Max Ret Depth: " + maxRetDepth + "\n");
					return	false;
				}
			}
			CoreEvents.Print("Max Depth: " + maxStackDepth + " , Max Ret Depth: " + maxRetDepth + "\n");
			return	true;
		}


		internal bool FloodPortalsSlow_r(VISPortal destPort, VISPStack prevStack,
			ref int canSee, VISLeaf []visLeafs, VisPools vPools, ClipPools cPools)
		{
			VISPStack	stack	=vPools.mStacks.GetFreeItem();

			Int32	portNum	=destPort.mPortNum;

			//Add the portal that we are Flooding into, to the original portals visbits
			byte	Bit	=(byte)(portNum & 7);
			Bit	=(byte)(1 << Bit);

			if((mFinalVisBits[portNum >> 3] & Bit) == 0)
			{
				mFinalVisBits[portNum>>3] |= Bit;
				mCanSee++;
				visLeafs[mLeaf].mCanSee++;
				canSee++;
			}

			//Get the leaf that this portal looks into, and flood from there
			Int32	leafNum	=destPort.mLeaf;
			VISLeaf	leaf	=visLeafs[leafNum];

			// Now, try and Flood into the leafs that this portal touches
			for(VISPortal port=leaf.mPortals;port != null;port=port.mNext)
			{
				portNum	=port.mPortNum;
				Bit		=(byte)(1<<(portNum&7));

				//GHook.Printf("PrevStack VisBits:  %i\n", PrevStack.mVisBits[PNum>>3]);

				//If might see could'nt see it, then don't worry about it
				if((prevStack.mVisBits[portNum>>3] & Bit) == 0)
				{
					continue;	// Can't possibly see it
				}

				//If the portal can't see anything we haven't allready seen, skip it
				UInt64	more	=0;
				if(port.mbDone)
				{
					for(int j=0;j < mFinalVisBits.Length;j++)
					{
						//there is no & for bytes, can you believe that?
						uint	prevBit		=(uint)prevStack.mVisBits[j];
						uint	portBit		=(uint)port.mFinalVisBits[j];
						uint	bothBit		=prevBit & portBit;
						stack.mVisBits[j]	=(byte)bothBit;

						prevBit	=stack.mVisBits[j];
						portBit	=mFinalVisBits[j];

						more	|=prevBit &~ portBit;
					}
				}
				else
				{
					//qwords and dwords seem about the same performancewise
					more	=AndTogetherDWords(prevStack.mVisBits, port.mVisBits, mFinalVisBits, stack.mVisBits);
				}
				
				if(more == 0 && ((mFinalVisBits[portNum>>3] & Bit) != 0))
				{
					//Can't see anything new
					continue;
				}

				//Setup Source/Pass
				stack.mPass			=vPools.mPolys.GetFreeItem();
				stack.mPass.mVerts	=cPools.DupeVerts(port.mPoly.mVerts);

				//Cut away portion of pass portal we can't see through
				if(!stack.mPass.ClipPoly(mPlane, false, cPools))
				{
					stack.FreeAll(vPools, cPools);
					return	false;
				}
				if(stack.mPass.VertCount() < 3)
				{
					stack.Free(vPools, cPools);
					continue;
				}

				stack.mSource			=vPools.mPolys.GetFreeItem();
				stack.mSource.mVerts	=cPools.DupeVerts(prevStack.mSource.mVerts);

				if(!stack.mSource.ClipPoly(port.mPlane, true, cPools))
				{
					stack.FreeAll(vPools, cPools);
					return	false;
				}
				if(stack.mSource.VertCount() < 3)
				{
					stack.Free(vPools, cPools);
					continue;
				}

				//If we don't have a PrevStack.mPass, then we don't have enough to look through.
				//This portal can only be blocked by VisBits (Above test)...
				if(prevStack.mPass == null)
				{
					if(!FloodPortalsSlow_r(port, stack, ref canSee, visLeafs, vPools, cPools))
					{
						stack.FreeAll(vPools, cPools);
						return	false;
					}
					stack.Free(vPools, cPools);
					continue;
				}

				if(!stack.mPass.SeperatorClip(stack.mSource, prevStack.mPass, false, cPools))
				{
					stack.FreeAll(vPools, cPools);
					return	false;
				}
				if(stack.mPass == null || stack.mPass.VertCount() < 3)
				{
					stack.Free(vPools, cPools);
					continue;
				}

				if(!stack.mPass.SeperatorClip(prevStack.mPass, stack.mSource, true, cPools))
				{
					stack.FreeAll(vPools, cPools);
					return	false;
				}
				if(stack.mPass == null || stack.mPass.VertCount() < 3)
				{
					stack.Free(vPools, cPools);
					continue;
				}

				//Flood into it...
				if(!FloodPortalsSlow_r(port, stack, ref canSee, visLeafs, vPools, cPools))
				{
					stack.FreeAll(vPools, cPools);
					return	false;
				}

				stack.Free(vPools, cPools);
			}

			stack.FreeAll(vPools, cPools);

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
		internal VISPortal	mPortals;
		internal Int32		mMightSee;
		internal Int32		mCanSee;


		internal void Write(BinaryWriter bw)
		{
			if(mPortals == null)
			{
				bw.Write(-1);
			}
			else
			{
				bw.Write(mPortals.mPortNum);
			}
			bw.Write(mMightSee);
			bw.Write(mCanSee);
		}


		internal void Read(BinaryReader br, VISPortal[] ports)
		{
			Int32	idx	=br.ReadInt32();

			if(idx >= 0)
			{
				mPortals	=ports[idx];
			}
			mMightSee	=br.ReadInt32();
			mCanSee		=br.ReadInt32();
		}
	}
}
