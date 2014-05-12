using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UtilityLib;
using BSPCore;


namespace BSPVis
{
	internal class VisPools
	{
		public Pool<GBSPPoly>	mPolys;
		public Pool<VISPStack>	mStacks;
		public ClipPools		mClipPools;
		public VISLeaf			[]mVisLeafs;
		public int				mIterations;


		internal VisPools(VISLeaf []leafs, ClipPools cp)
		{
			mPolys		=new Pool<GBSPPoly>(() => new GBSPPoly(0));
			mStacks		=new Pool<VISPStack>(() => new VISPStack());
			mVisLeafs	=leafs;
			mClipPools	=cp;
		}
	}
}