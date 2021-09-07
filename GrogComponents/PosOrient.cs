using System;
using SharpDX;
using UtilityLib;


namespace EntityLib
{
	public class PosOrient : Component
	{
		internal Vector3	mPosition;
		internal float		mYaw, mPitch, mRoll;


		public PosOrient(Entity owner, Vector3 pos,
						 float yaw, float pitch, float roll) : base(owner)
		{
			mPosition	=pos;
			mYaw		=yaw;
			mPitch		=pitch;
			mRoll		=roll;
		}


		public Vector3	GetPosition()
		{
			return	mPosition;
		}


		public void SetYaw(float yaw)
		{
			mYaw	=yaw;
		}


		public Matrix	GetMatrix()
		{
			return	Matrix.RotationYawPitchRoll(
				MathUtil.DegreesToRadians(mYaw),
				MathUtil.DegreesToRadians(mPitch),
				MathUtil.DegreesToRadians(mRoll))
					* Matrix.Translation(mPosition);
		}
	}
}