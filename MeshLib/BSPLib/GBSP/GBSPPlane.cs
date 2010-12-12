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

		public const float PLANESIDE_EPSILON	=0.001f;


		internal GBSPPlane(GBSPPlane copyMe)
		{
			mNormal	=copyMe.mNormal;
			mDist	=copyMe.mDist;
			mType	=copyMe.mType;
		}


		internal GBSPPlane(GBSPPoly poly)
		{
			this	=poly.GenPlane();
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

			UInt32	type	=mType % PLANE_ANYX;

			if(UtilityLib.Mathery.VecIdx(mNormal, type) < 0)
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
