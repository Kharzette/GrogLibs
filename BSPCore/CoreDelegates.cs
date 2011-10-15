using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace BSPCore
{
	public class CoreDelegates
	{
		public delegate bool IsPointInSolid(Vector3 pos);
		public delegate bool RayCollision(Vector3 front, Vector3 back, ref Vector3 Impacto);
		public delegate Int32 GetNodeLandedIn(Int32 node, Vector3 pos);
		public delegate Vector3 GetEmissiveForMaterial(string matName);
	}
}
