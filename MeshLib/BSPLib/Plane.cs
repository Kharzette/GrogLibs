using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public struct Plane
	{
		public Vector3	mNormal;
		public float	mDistance;

		public const float	EPSILON		=0.03125f;


		public void Move(Vector3 worldPos)
		{
			mDistance	+=Vector3.Dot(mNormal, worldPos);
		}


		public void Invert()
		{
			mNormal		=-mNormal;
			mDistance	=-mDistance;
		}


		public bool IsOn(Vector3 pos)
		{
			float	d	=Vector3.Dot(mNormal, pos) - mDistance;
			return	(d >= -Plane.EPSILON && d < Plane.EPSILON);
		}


		internal void Read(BinaryReader br)
		{
			mNormal.X	=br.ReadSingle();
			mNormal.Y	=br.ReadSingle();
			mNormal.Z	=br.ReadSingle();
			mDistance	=br.ReadSingle();
		}


		internal void Write(BinaryWriter bw)
		{
			bw.Write(mNormal.X);
			bw.Write(mNormal.Y);
			bw.Write(mNormal.Z);
			bw.Write(mDistance);
		}


		//are the planes within tolerance of each other?
		public bool CompareEpsilon(Plane p2, float epsi)
		{
			if(p2.mNormal.X < (mNormal.X - epsi) || p2.mNormal.X > (mNormal.X + epsi))
			{
				return	false;
			}
			if(p2.mNormal.Y < (mNormal.Y - epsi) || p2.mNormal.Y > (mNormal.Y + epsi))
			{
				return	false;
			}
			if(p2.mNormal.Z < (mNormal.Z - epsi) || p2.mNormal.Z > (mNormal.Z + epsi))
			{
				return	false;
			}
			if(p2.mDistance < (mDistance - epsi) || p2.mDistance > (mDistance + epsi))
			{
				return	false;
			}
			return	true;
		}


		internal float DistanceFrom(Vector3 pos)
		{
			return	Vector3.Dot(pos, mNormal) - mDistance;
		}
	}
}
