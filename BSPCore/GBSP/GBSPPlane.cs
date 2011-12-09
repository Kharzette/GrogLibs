using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPCore
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


		internal GBSPPlane(GFXPlane copyMe)
		{
			mNormal	=copyMe.mNormal;
			mDist	=copyMe.mDist;
			mType	=copyMe.mType;
		}


		public GBSPPlane(Vector3 []verts)
		{
			int	i;

			mNormal	=Vector3.Zero;

			//catches colinear points now
			for(i=0;i < verts.Length;i++)
			{
				//gen a plane normal from the cross of edge vectors
				Vector3	v1  =verts[i] - verts[(i + 1) % verts.Length];
				Vector3	v2  =verts[(i + 2) % verts.Length] - verts[(i + 1) % verts.Length];

				mNormal   =Vector3.Cross(v1, v2);

				if(!mNormal.Equals(Vector3.Zero))
				{
					break;
				}
				//try the next three if there are three
			}
			if(i >= verts.Length)
			{
				//need a talky flag
				//in some cases this isn't worthy of a warning
				CoreEvents.Print("Face with no normal!");
				mNormal	=Vector3.UnitX;
				mDist	=0.0f;
				mType	=GBSPPlane.PLANE_ANY;
				return;
			}

			mNormal.Normalize();
			mDist	=Vector3.Dot(verts[1], mNormal);
			mType	=GetPlaneType(mNormal);
		}


		public GBSPPlane(GBSPPoly poly)
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


		public void Inverse()
		{
			mNormal	=-mNormal;
			mDist	=-mDist;
		}


		//negative planes were messing up
		internal float DistanceFast(Vector3 pos)
		{
			return	Vector3.Dot(pos, mNormal) - mDist;
		}


		public void Write(System.IO.BinaryWriter bw)
		{
			bw.Write(mNormal.X);
			bw.Write(mNormal.Y);
			bw.Write(mNormal.Z);
			bw.Write(mDist);
			bw.Write(mType);
		}


		public void Read(System.IO.BinaryReader br)
		{
			mNormal.X	=br.ReadSingle();
			mNormal.Y	=br.ReadSingle();
			mNormal.Z	=br.ReadSingle();
			mDist		=br.ReadSingle();
			mType		=br.ReadUInt32();
		}
	}
}
