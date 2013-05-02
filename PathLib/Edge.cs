using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using UtilityLib;


namespace PathLib
{
	public class Edge
	{
		internal Vector3	mA, mB;

		const float	OverlapEpsilon	=16f;	//need to overlap by this much

		internal bool IsColinear(Edge other)
		{
			Vector3	edgeVec1	=mA - mB;
			Vector3	edgeVec2	=other.mA - other.mB;

			Vector3	testVec	=Vector3.Cross(edgeVec1, edgeVec2);
			return	testVec.Equals(Vector3.Zero);
		}

		internal bool AlmostEqual(Edge other)
		{
			if(Mathery.CompareVector(mA, other.mA)
				&& Mathery.CompareVector(mB, other.mB))
			{
				return	true;
			}
			if(Mathery.CompareVector(mB, other.mA)
				&& Mathery.CompareVector(mA, other.mB))
			{
				return	true;
			}
			return	false;
		}

		internal bool AlmostEqualXZ(Edge other)
		{
			Vector2	ourAXZ, ourBXZ;
			Vector2	otherAXZ, otherBXZ;

			ourAXZ.X	=mA.X;
			ourAXZ.Y	=mA.Z;
			ourBXZ.X	=mB.X;
			ourBXZ.Y	=mB.Z;

			otherAXZ.X	=other.mA.X;
			otherAXZ.Y	=other.mA.Z;
			otherBXZ.X	=other.mB.X;
			otherBXZ.Y	=other.mB.Z;

			if(Mathery.CompareVector(ourAXZ, otherAXZ)
				&& Mathery.CompareVector(ourBXZ, otherBXZ))
			{
				return	true;
			}
			if(Mathery.CompareVector(ourBXZ, otherAXZ)
				&& Mathery.CompareVector(ourAXZ, otherBXZ))
			{
				return	true;
			}
			return	false;
		}

		internal Vector3 GetCenter()
		{
			return	((mA + mB) * 0.5f);
		}
	}
}
