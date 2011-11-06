using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UtilityLib;
using Microsoft.Xna.Framework;
using BSPCore;


namespace BSPVis
{
	internal class VisPools
	{
		public Pool<GBSPPoly>	mPolys;
		public Pool<VISPStack>	mStacks;


		internal VisPools()
		{
			mPolys		=new Pool<GBSPPoly>(() => new GBSPPoly(0));
			mStacks		=new Pool<VISPStack>(() => new VISPStack());
		}
	}
}