using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using UtilityLib;


namespace BSPZone
{
	public struct ZonePlane : UtilityLib.IReadWriteable
	{
		public Vector3	mNormal;
		public float	mDist;
		public UInt32	mType;	//PLANE_X, PLANE_Y, etc...

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

		//default blank planes
		static ZonePlane	mBlank	=new ZonePlane(Vector3.Zero, 0.0f, PLANE_ANY);
		static ZonePlane	mBlankX	=new ZonePlane(Vector3.UnitX, 0.0f, PLANE_ANY);

		//constants
		public const float	GroundAngle	=0.8f;	//how sloped can you be to be considered ground


		public ZonePlane(Vector3 norm, float dist, UInt32 type)
		{
			mNormal	=norm;
			mDist	=dist;
			mType	=type;
		}


		public static ZonePlane Blank
		{
			get { return mBlank; }
		}

		public static ZonePlane BlankX
		{
			get { return mBlankX; }
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			return	base.Equals(obj);
		}

		public static bool operator ==(ZonePlane p1, ZonePlane p2)
		{
			return p1.mNormal.X == p2.mNormal.X
				&& p1.mNormal.Y == p2.mNormal.Y
				&& p1.mNormal.Z == p2.mNormal.Z
				&& p1.mDist == p2.mDist;
		}

		public static bool operator !=(ZonePlane p1, ZonePlane p2)
		{
			return !(p1 == p2);
		}

		public void Write(BinaryWriter bw)
		{
			bw.Write(mNormal.X);
			bw.Write(mNormal.Y);
			bw.Write(mNormal.Z);
			bw.Write(mDist);
			bw.Write(mType);
		}


		public void Read(BinaryReader br)
		{
			mNormal.X	=br.ReadSingle();
			mNormal.Y	=br.ReadSingle();
			mNormal.Z	=br.ReadSingle();
			mDist		=br.ReadSingle();
			mType		=br.ReadUInt32();
		}


		//there's something badly wrong with this
		//TODO: fix
		public float DistanceFast(Vector3 pos)
		{
			return	Vector3.Dot(pos, mNormal) - mDist;
			/*
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
			}*/
		}


		public bool IsGround()
		{
			return	(Vector3.Dot(mNormal, Vector3.UnitY) > GroundAngle);
		}


		//push slightly to the front side
		internal Vector3 ReflectPosition(Vector3 start, Vector3 end)
		{
			float	startDist	=DistanceFast(start);
			float	dist		=DistanceFast(end);

			//is the direction vector valid to find a collision response?
			if(startDist <= 0f || dist >= Mathery.VCompareEpsilon)
			{
				//place end directly on the plane
				end	-=(mNormal * dist);

				//adjust it to the front side
				end	+=(mNormal * Mathery.VCompareEpsilon);
			}
			else
			{
				end	-=(mNormal * (dist - Mathery.VCompareEpsilon));
			}
			return	end;
		}


		//adjust a position just off the front side
		internal void ReflectPosition(ref Vector3 pos)
		{
			float	dist	=DistanceFast(pos);

			//directly on or off a bit?
			if(dist >= Mathery.VCompareEpsilon)
			{
				//place end directly on the plane
				pos	-=(mNormal * dist);

				//adjust it to the front side
				pos	+=(mNormal * Mathery.VCompareEpsilon);
			}
			else
			{
				pos	-=(mNormal * (dist - Mathery.VCompareEpsilon));
			}
		}


		internal void Inverse()
		{
			mNormal	=-mNormal;
			mDist	=-mDist;
		}
	

		internal static ZonePlane Transform(ZonePlane plane, Matrix mat)
		{
			Vector3	p0, p1, p2;

			if(plane == ZonePlane.Blank)
			{
				return	plane;
			}

			UtilityLib.Mathery.PointsFromPlane(plane.mNormal, plane.mDist, out p0, out p1, out p2);

			p0	=Vector3.Transform(p0, mat);
			p1	=Vector3.Transform(p1, mat);
			p2	=Vector3.Transform(p2, mat);

			ZonePlane	ret	=ZonePlane.Blank;

			UtilityLib.Mathery.PlaneFromVerts(p0, p1, p2, out ret.mNormal, out ret.mDist);

			return	ret;
		}


		//the xna transform expects an inverted matrix
		//which is quite odd
		//note this method doesn't work very well at all
		//getting very off the wall results with rotations
		internal static ZonePlane XNATransform(ZonePlane zonePlane, Matrix matrix)
		{
			Plane	XNAPlane;
			XNAPlane.D		=zonePlane.mDist;
			XNAPlane.Normal	=zonePlane.mNormal;

			XNAPlane	=Plane.Transform(XNAPlane, matrix);

			ZonePlane	ret	=ZonePlane.Blank;

			ret.mNormal	=XNAPlane.Normal;
			ret.mDist	=XNAPlane.D;

			return	ret;
		}
	}
}
