using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public struct Edge
	{
		public Vector3	mP0, mP1;

		internal bool Overlaps(Edge e)
		{
			//check the directions
			Vector3	myDir	=mP1 - mP0;
			Vector3	eDir	=e.mP1 - e.mP0;

			myDir.Normalize();
			eDir.Normalize();

			int	blah=0;

			if(UtilityLib.Mathery.CompareVectorEpsilon(mP0, e.mP0, 0.001f))
			{
				blah++;
			}
			if(UtilityLib.Mathery.CompareVectorEpsilon(mP0, e.mP1, 0.001f))
			{
				blah++;
			}
			if(UtilityLib.Mathery.CompareVectorEpsilon(mP1, e.mP0, 0.001f))
			{
				blah++;
			}
			if(UtilityLib.Mathery.CompareVectorEpsilon(mP1, e.mP1, 0.001f))
			{
				blah++;
			}

			if(UtilityLib.Mathery.CompareVectorABS(myDir, eDir, 0.001f))
			{
				//create perpendicular end cap planes
				Plane	end0, end1;
				end1.mNormal	=myDir;
				end1.mDistance	=Vector3.Dot(mP1, myDir);

				end0	=end1;
				end0.Invert();
				end0.mDistance	=Vector3.Dot(mP0, end0.mNormal);

				//make sure at least one vert is inside the plane caps
				float	dist00	=end0.DistanceFrom(e.mP0);
				float	dist10	=end0.DistanceFrom(e.mP1);
				float	dist01	=end1.DistanceFrom(e.mP0);
				float	dist11	=end1.DistanceFrom(e.mP1);

				if(dist00 < Plane.EPSILON && dist01 < Plane.EPSILON)
				{
					//project dist00 onto plane
					Vector3	pos	=end0.mNormal * dist00;

					pos	=mP0 + pos;

					//compare
					if(UtilityLib.Mathery.CompareVectorEpsilon(pos, e.mP0, 0.001f))
					{
						return	true;
					}
				}
				else if(dist10 < Plane.EPSILON && dist11 < Plane.EPSILON)
				{
					//project dist10 onto plane
					Vector3	pos	=end0.mNormal * dist10;

					pos	=mP0 + pos;

					//compare
					if(UtilityLib.Mathery.CompareVectorEpsilon(pos, e.mP1, 0.001f))
					{
						return	true;
					}
				}
			}
			return	false;
		}
	}


	public class PortalLink
	{
		public Edge				mEdge;
		public List<PortalFace>	mConnected	=new List<PortalFace>();
	}
}
