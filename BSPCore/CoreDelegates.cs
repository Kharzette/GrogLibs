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
		public delegate bool RayCollision(Vector3 front, Vector3 back, int modelIndex, Matrix modelInv);
		public delegate Int32 GetNodeLandedIn(Int32 node, Vector3 pos);
		public delegate Vector3 GetEmissiveForMaterial(string matName);
		internal delegate GBSPModel ModelForLeafNode(GBSPNode n);
		public delegate void SaveVisZoneData(System.IO.BinaryWriter bw);
	}
}
