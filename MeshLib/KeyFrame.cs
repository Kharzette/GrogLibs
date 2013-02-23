using System;
using System.IO;
using Microsoft.Xna.Framework;


namespace MeshLib
{
	public class KeyFrame
	{
		public Quaternion	mRotation;
		public Vector3		mPosition;
		public Vector3		mScale;


		public KeyFrame()
		{
			mScale		=Vector3.One;
			mRotation	=Quaternion.Identity;
		}


		internal void Write(BinaryWriter bw)
		{
			bw.Write(mRotation.X);
			bw.Write(mRotation.Y);
			bw.Write(mRotation.Z);
			bw.Write(mRotation.W);

			bw.Write(mPosition.X);
			bw.Write(mPosition.Y);
			bw.Write(mPosition.Z);

			bw.Write(mScale.X);
			bw.Write(mScale.Y);
			bw.Write(mScale.Z);
		}


		internal void Read(BinaryReader br)
		{
			mRotation.X	=br.ReadSingle();
			mRotation.Y	=br.ReadSingle();
			mRotation.Z	=br.ReadSingle();
			mRotation.W	=br.ReadSingle();

			mPosition.X	=br.ReadSingle();
			mPosition.Y	=br.ReadSingle();
			mPosition.Z	=br.ReadSingle();

			mScale.X	=br.ReadSingle();
			mScale.Y	=br.ReadSingle();
			mScale.Z	=br.ReadSingle();
		}


		internal static void Lerp(KeyFrame key0, KeyFrame key1,
			float percentage, KeyFrame result)
		{
			Vector3.Lerp(ref key0.mPosition, ref key1.mPosition, percentage, out result.mPosition);
			Vector3.Lerp(ref key0.mScale, ref key1.mScale, percentage, out result.mScale);
			Quaternion.Slerp(ref key0.mRotation, ref key1.mRotation, percentage, out result.mRotation);
		}
	}
}