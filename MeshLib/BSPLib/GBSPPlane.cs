using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public struct GBSPPlane
	{
		public Vector3	mNormal;
		public float	mDist;
		public UInt32	mType;

		public const UInt32	PLANE_X		=0;
		public const UInt32	PLANE_Y		=1;
		public const UInt32	PLANE_Z		=2;
		public const UInt32	PLANE_ANYX	=3;
		public const UInt32	PLANE_ANYY	=4;
		public const UInt32	PLANE_ANYZ	=5;
		public const UInt32	PLANE_ANY	=6;

		public const UInt32	PSIDE_FRONT		=1;
		public const UInt32	PSIDE_BACK		=2;
		public const UInt32	PSIDE_BOTH		=(PSIDE_FRONT | PSIDE_BACK);
		public const UInt32	PSIDE_FACING	=4;


		internal GBSPPlane(GBSPPlane copyMe)
		{
			mNormal	=copyMe.mNormal;
			mDist	=copyMe.mDist;
			mType	=copyMe.mType;
		}


		internal GBSPPlane(GBSPPoly poly)
		{
			int i;

			mNormal	=Vector3.Zero;

			//catches colinear points now
			for(i=0;i < poly.mVerts.Count;i++)
			{
				//gen a plane normal from the cross of edge vectors
				Vector3	v1  =poly.mVerts[i] - poly.mVerts[(i + 1) % poly.mVerts.Count];
				Vector3	v2  =poly.mVerts[(i + 2) % poly.mVerts.Count] - poly.mVerts[(i + 1) % poly.mVerts.Count];

				mNormal   =Vector3.Cross(v1, v2);

				if(!mNormal.Equals(Vector3.Zero))
				{
					break;
				}
				//try the next three if there are three
			}
			if(i >= poly.mVerts.Count)
			{
				//need a talky flag
				//in some cases this isn't worthy of a warning
				Map.Print("Face with no normal!");
				mNormal	=Vector3.UnitX;
				mDist	=0.0f;
				mType	=PLANE_ANY;
				return;
			}

			mNormal.Normalize();
			mDist	=Vector3.Dot(poly.mVerts[1], mNormal);
			mType	=GetPlaneType(mNormal);
		}


		internal void Snap()
		{
			UtilityLib.Mathery.SnapVector(ref mNormal);

			float	roundedDist	=(float)Math.Round((double)mDist);

			if(Math.Abs(mDist - roundedDist) < UtilityLib.Mathery.DIST_EPSILON)
			{
				mDist	=roundedDist;
			}
		}


		static internal UInt32 GetPlaneType(Vector3 normal)
		{
			float	X, Y, Z;
			
			X	=Math.Abs(normal.X);
			Y	=Math.Abs(normal.Y);
			Z	=Math.Abs(normal.Z);
			
			if(X == 1.0f)
			{
				return	PLANE_X;
			}
			else if(Y == 1.0f)
			{
				return	PLANE_Y;
			}
			else if(Z == 1.0f)
			{
				return	PLANE_Z;
			}

			if(X >= Y && X >= Z)
			{
				return	PLANE_ANYX;
			}
			else if(Y >= X && Y >= Z)
			{
				return	PLANE_ANYY;
			}
			else
			{
				return	PLANE_ANYZ;
			}
		}


		internal void Side(out sbyte side)
		{
			mType	=GetPlaneType(mNormal);

			side	=0;

			if(UtilityLib.Mathery.VecIdx(mNormal, mType) < 0)
			{
				Inverse();
				side	=1;
			}
		}


		internal bool Compare(GBSPPlane other)
		{
			Vector3	norm	=mNormal - other.mNormal;
			float	dist	=mDist - other.mDist;

			if(Math.Abs(norm.X) < UtilityLib.Mathery.NORMAL_EPSILON &&
				Math.Abs(norm.Y) < UtilityLib.Mathery.NORMAL_EPSILON &&
				Math.Abs(norm.Z) < UtilityLib.Mathery.NORMAL_EPSILON &&
				Math.Abs(dist) < UtilityLib.Mathery.DIST_EPSILON)
			{
				return	true;
			}
			return	false;
		}


		internal void Inverse()
		{
			mNormal	=-mNormal;
			mDist	=-mDist;
		}


		internal float DistanceFast(Vector3 pos)
		{
			switch(mType)
			{
				case PLANE_X:
					return	pos.X - mDist;
				case PLANE_Y:
					return	pos.Y - mDist;
				case PLANE_Z:
					return	pos.Z - mDist;

				default:
					return	Vector3.Dot(pos, mNormal) - mDist;
			}
		}
	}
}
