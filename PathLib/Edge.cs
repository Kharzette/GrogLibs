using System;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using UtilityLib;


namespace PathLib;

internal class Edge
{
	internal Vector3	mA, mB;


	internal bool IsColinear(Edge other)
	{
		Vector3	edgeVec1	=mA - mB;
		Vector3	edgeVec2	=other.mA - other.mB;
		Vector3	edgeVec3	=other.mA - mA;
		Vector3	edgeVec4	=other.mB - mA;

		Vector3	testVec	=Vector3.Cross(edgeVec1, edgeVec2);
		if(!testVec.Equals(Vector3.Zero))
		{
			return	false;
		}

		testVec	=Vector3.Cross(edgeVec1, edgeVec3);
		if(!testVec.Equals(Vector3.Zero))
		{
			return	false;
		}

		testVec	=Vector3.Cross(edgeVec1, edgeVec4);
		if(!testVec.Equals(Vector3.Zero))
		{
			return	false;
		}
		return	true;
	}


	//compute the amount the two edges overlap
	internal float GetOverlap(Edge other)
	{
		if(!IsColinear(other))
		{
			return	0f;
		}

		Vector3	edgeVec	=mA - mB;
		Vector3	oVecA	=mA - other.mA;
		Vector3	oVecB	=mA - other.mB;

		float	len	=edgeVec.Length();

		edgeVec	/=len;

		float	aDot	=Vector3.Dot(edgeVec, oVecA);
		float	bDot	=Vector3.Dot(edgeVec, oVecB);

		float	ret	=0f;

		if(aDot >= 0f && aDot <= len)
		{
			//a inside the line segment
			if(bDot >= 0f && bDot <= len)
			{
				//b inside the line segment
				//so overlap is from b to a
				ret	=Math.Abs(bDot - aDot);
			}
			else
			{
				if(bDot <= 0f)
				{
					//overlap is from start to A
					ret	=aDot;
				}
				else
				{
					//overlap is from end to A
					ret	=len - aDot;
				}
			}
		}
		else if(bDot >= 0f && bDot <= len)
		{
			//b inside the line segment
			if(aDot <= 0f)
			{
				//overlap from start to b
				ret	=bDot;
			}
			else
			{
				//overlap from end to b
				ret	=len - bDot;
			}
		}
		return	ret;
	}


	internal float Length()
	{
		return	(mB - mA).Length();
	}


	internal Vector3 GetCenter()
	{
		return	((mA + mB) * 0.5f);
	}


	internal float Distance(Vector3 pos)
	{
		Vector3	line	=mB - mA;

		Vector3	lineToPos	=pos - mA;

		Vector3	perp	=Vector3.Cross(lineToPos, line);

		Vector3	norm	=Vector3.Cross(line, perp);

		norm	=Vector3.Normalize(norm);

		float	dist	=norm.dot(mA);

		float	posDist	=norm.dot(pos) - dist;

		Debug.Assert(posDist >= 0f);

		return	posDist;
	}
}