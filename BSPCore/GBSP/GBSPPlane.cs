using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPCore
{
	internal struct GBSPPlane
	{
		internal Vector3	mNormal;
		internal float		mDist;
		internal UInt32		mType;

		internal const UInt32	PLANE_X		=0;
		internal const UInt32	PLANE_Y		=1;
		internal const UInt32	PLANE_Z		=2;
		internal const UInt32	PLANE_ANYX	=3;
		internal const UInt32	PLANE_ANYY	=4;
		internal const UInt32	PLANE_ANYZ	=5;
		internal const UInt32	PLANE_ANY	=6;

		internal const UInt32	PSIDE_FRONT		=1;
		internal const UInt32	PSIDE_BACK		=2;
		internal const UInt32	PSIDE_BOTH		=(PSIDE_FRONT | PSIDE_BACK);
		internal const UInt32	PSIDE_FACING	=4;

		internal const float PLANESIDE_EPSILON	=0.001f;


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


		internal GBSPPlane(List<Vector3> verts)
		{
			int	i;

			mNormal	=Vector3.Zero;

			//catches colinear points now
			for(i=0;i < verts.Count;i++)
			{
				//gen a plane normal from the cross of edge vectors
				Vector3	v1  =verts[i] - verts[(i + 1) % verts.Count];
				Vector3	v2  =verts[(i + 2) % verts.Count] - verts[(i + 1) % verts.Count];

				mNormal   =Vector3.Cross(v1, v2);

				if(!mNormal.Equals(Vector3.Zero))
				{
					break;
				}
				//try the next three if there are three
			}
			if(i >= verts.Count)
			{
				//need a talky flag
				//in some cases this isn't worthy of a warning
				Map.Print("Face with no normal!");
				mNormal	=Vector3.UnitX;
				mDist	=0.0f;
				mType	=GBSPPlane.PLANE_ANY;
				return;
			}

			mNormal.Normalize();
			mDist	=Vector3.Dot(verts[1], mNormal);
			mType	=GetPlaneType(mNormal);
		}


		internal GBSPPlane(GBSPPoly poly)
		{
			this	=poly.GenPlane();
		}


		internal void Snap()
		{
			Utility64.Mathery.SnapVector(ref mNormal);

			float	roundedDist	=(float)Math.Round((double)mDist);

			if(Math.Abs(mDist - roundedDist) < Utility64.Mathery.DIST_EPSILON)
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

			if(Utility64.Mathery.VecIdx(mNormal, type) < 0)
			{
				Inverse();
				side	=1;
			}
		}


		internal bool Compare(GBSPPlane other)
		{
			Vector3	norm	=mNormal - other.mNormal;
			float	dist	=mDist - other.mDist;

			if(Math.Abs(norm.X) < Utility64.Mathery.NORMAL_EPSILON &&
				Math.Abs(norm.Y) < Utility64.Mathery.NORMAL_EPSILON &&
				Math.Abs(norm.Z) < Utility64.Mathery.NORMAL_EPSILON &&
				Math.Abs(dist) < Utility64.Mathery.DIST_EPSILON)
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


		internal void Write(System.IO.BinaryWriter bw)
		{
			bw.Write(mNormal.X);
			bw.Write(mNormal.Y);
			bw.Write(mNormal.Z);
			bw.Write(mDist);
			bw.Write(mType);
		}


		internal void Read(System.IO.BinaryReader br)
		{
			mNormal.X	=br.ReadSingle();
			mNormal.Y	=br.ReadSingle();
			mNormal.Z	=br.ReadSingle();
			mDist		=br.ReadSingle();
			mType		=br.ReadUInt32();
		}
	}
}
