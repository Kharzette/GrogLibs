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

		internal Vector3 GetCenter()
		{
			return	((mA + mB) * 0.5f);
		}
	}
}
