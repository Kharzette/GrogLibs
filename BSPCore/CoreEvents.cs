using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BSPCore
{
	public static class CoreEvents
	{
		public static event EventHandler	eNumPlanesChanged;
		public static event EventHandler	eNumVertsChanged;
		public static event EventHandler	eNumClustersChanged;
		public static event EventHandler	eNumPortalsChanged;
		public static event EventHandler	eGBSPSaveDone;
		public static event EventHandler	eVisDone;
		public static event EventHandler	eLightDone;
		public static event EventHandler	eBuildDone;
		public static event EventHandler	ePrint;


		public static void Print(string str)
		{
			EventHandler	evt	=ePrint;
			if(evt != null)
			{
				evt(str, null);
			}
		}


		public static void FireBuildDoneEvent(object sender, EventArgs ea)
		{
			UtilityLib.Misc.SafeInvoke(eBuildDone, sender);
		}


		public static void FireVisDoneEvent(object sender, EventArgs ea)
		{
			UtilityLib.Misc.SafeInvoke(eVisDone, sender);
		}


		public static void FireGBSPSaveDoneEvent(object sender, EventArgs ea)
		{
			UtilityLib.Misc.SafeInvoke(eGBSPSaveDone, sender);
		}


		public static void FireLightDoneDoneEvent(object sender, EventArgs ea)
		{
			UtilityLib.Misc.SafeInvoke(eLightDone, sender);
		}


		public static void FireNumPortalsChangedEvent(object sender, EventArgs ea)
		{
			UtilityLib.Misc.SafeInvoke(eNumPortalsChanged, sender);
		}


		public static void FireNumVertsChangedEvent(object sender, EventArgs ea)
		{
			UtilityLib.Misc.SafeInvoke(eNumVertsChanged, sender);
		}


		public static void FireNumClustersChangedEvent(object sender, EventArgs ea)
		{
			UtilityLib.Misc.SafeInvoke(eNumClustersChanged, sender);
		}


		public static void FireNumPlanesChangedEvent(object sender, EventArgs ea)
		{
			UtilityLib.Misc.SafeInvoke(eNumPlanesChanged, sender);
		}
	}
}
