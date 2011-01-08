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


	internal class VISPortal
	{
		internal VISPortal	mNext;
		internal GBSPPoly	mPoly;
		internal GBSPPlane	mPlane;
		internal Vector3	mCenter;
		internal float		mRadius;

		internal byte	[]mVisBits;
		internal byte	[]mFinalVisBits;
		internal Int32	mLeaf;
		internal Int32	mMightSee;
		internal Int32	mCanSee;
		internal bool	mbDone;


		internal void Read(BinaryReader br, List<int> indexes)
		{
			int	idx	=br.ReadInt32();
			indexes.Add(idx);

			mPoly	=new GBSPPoly();
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


		internal void Write(BinaryWriter bw, Dictionary<VISPortal, Int32> portIndexer)
		{
			if(mNext != null)
			{
				bw.Write(portIndexer[mNext]);
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
					Map.Print("No VisInfo for portal.\n");
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
			Dictionary<VISPortal, Int32> visIndexer,
			bool []portSeen, VISLeaf []visLeafs,
			int srcLeaf, ref int mightSee)
		{
			Debug.Assert(visIndexer.ContainsKey(destPortal));
			Int32	portNum	=visIndexer[destPortal];
			
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
					FloodPortalsFast_r(port, visIndexer, portSeen, visLeafs, srcLeaf, ref mightSee);
				}
			}
		}


		static bool ClipToSeperators(GBSPPoly src, GBSPPoly pass,
			GBSPPoly targ, bool bFlipClip, ref GBSPPoly dest)
		{
			return	targ.SeperatorClip(src, pass, bFlipClip, ref dest);
		}


		internal bool FloodPortalsSlow_r(VISPortal destPort, VISPStack prevStack,
			Dictionary<VISPortal, int> visIndexer, ref int canSee, VISLeaf []visLeafs)
		{
			VISPStack	stack	=new VISPStack();

			Int32	portNum	=visIndexer[destPort];

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
				portNum	=visIndexer[port];
				Bit		=(byte)(1<<(portNum&7));

				//GHook.Printf("PrevStack VisBits:  %i\n", PrevStack.mVisBits[PNum>>3]);

				//If might see could'nt see it, then don't worry about it
				if((mVisBits[portNum>>3] & Bit) == 0)
				{
					continue;
				}

				if((prevStack.mVisBits[portNum>>3] & Bit) == 0)
				{
					continue;	// Can't possibly see it
				}

				//If the portal can't see anything we haven't allready seen, skip it
				UInt32	more	=0;
				if(port.mbDone)
				{
					for(int j=0;j < mFinalVisBits.Length;j++)
					{
						//there is no & for bytes, can you believe that?
						uint	prevBit	=(uint)prevStack.mVisBits[j];
						uint	portBit		=(uint)port.mFinalVisBits[j];
						uint	bothBit		=prevBit & portBit;
						stack.mVisBits[j]	=(byte)bothBit;

						prevBit	=stack.mVisBits[j];
						portBit		=mFinalVisBits[j];

						more	|=prevBit &~ portBit;
					}
				}
				else
				{
					for(int j=0;j < mFinalVisBits.Length;j++)
					{
						//there is no & for bytes, can you believe that?
						uint	prevBit	=(uint)prevStack.mVisBits[j];
						uint	portBit		=(uint)port.mVisBits[j];
						uint	bothBit		=prevBit & portBit;
						stack.mVisBits[j]	=(byte)bothBit;

						prevBit	=stack.mVisBits[j];
						portBit		=mFinalVisBits[j];

						more	|=prevBit &~ portBit;
					}
				}
				
				if(more == 0 && ((mFinalVisBits[portNum>>3] & Bit) != 0))
				{
					//Can't see anything new
					continue;
				}
				
				//Setup Source/Pass
				stack.mPass	=new GBSPPoly(port.mPoly);

				//Cut away portion of pass portal we can't see through
				if(!stack.mPass.ClipPoly(mPlane, false))
				{
					return	false;
				}
				if(stack.mPass.VertCount() < 3)
				{
					continue;
				}

				stack.mSource	=new GBSPPoly(prevStack.mSource);

				if(!stack.mSource.ClipPoly(port.mPlane, true))
				{
					return	false;
				}
				if(stack.mSource.VertCount() < 3)
				{
					continue;
				}

				//If we don't have a PrevStack.mPass, then we don't have enough to look through.
				//This portal can only be blocked by VisBits (Above test)...
				if(prevStack.mPass == null)
				{
					if(!FloodPortalsSlow_r(port, stack, visIndexer, ref canSee, visLeafs))
					{
						return	false;
					}

					stack.mSource	=null;
					stack.mPass		=null;
					continue;
				}

				if(!ClipToSeperators(stack.mSource, prevStack.mPass, stack.mPass, false, ref stack.mPass))
				{
					return	false;
				}

				if(stack.mPass == null || stack.mPass.VertCount() < 3)
				{
					stack.mSource	=null;
					continue;
				}
				
				if(!ClipToSeperators(prevStack.mPass, stack.mSource, stack.mPass, true, ref stack.mPass))
				{
					return	false;
				}
				if(stack.mPass == null || stack.mPass.VertCount() < 3)
				{
					stack.mSource	=null;
					continue;
				}

				//Flood into it...
				if(!FloodPortalsSlow_r(port, stack, visIndexer, ref canSee, visLeafs))
				{
					return	false;
				}

				stack.mSource	=null;
				stack.mPass		=null;
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


		internal void Write(BinaryWriter bw, Dictionary<VISPortal, Int32> portIndexer)
		{
			bw.Write(portIndexer[mPortals]);
			bw.Write(mMightSee);
			bw.Write(mCanSee);
		}


		internal void Read(BinaryReader br, VISPortal[] ports)
		{
			Int32	idx	=br.ReadInt32();

			mPortals	=ports[idx];

			mMightSee	=br.ReadInt32();
			mCanSee		=br.ReadInt32();
		}
	}
}
