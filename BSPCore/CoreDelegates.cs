using System;
using System.Numerics;
using System.Collections.Generic;


namespace BSPCore;

public class CoreDelegates
{
	public delegate int CorrectLightPoint(ref Vector3 pos, Vector3 t2wU, Vector3 t2wV);
	public delegate bool IsPointInSolid(Vector3 pos);
	public delegate bool RayCollision(Vector3 front, Vector3 back, int modelIndex, Matrix4x4 modelInv, out Vector3 impact);
	public delegate Int32 GetNodeLandedIn(Int32 node, Vector3 pos);
	internal delegate GBSPModel ModelForLeafNode(GBSPNode n);
	public delegate void SaveVisZoneData(System.IO.BinaryWriter bw);
}