using System;
using System.IO;
using System.Numerics;


namespace MeshLib;

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


	public KeyFrame(KeyFrame copyMe)
	{
		mRotation	=copyMe.mRotation;
		mPosition	=copyMe.mPosition;
		mScale		=copyMe.mScale;
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


	internal void Transform(Matrix4x4 trans)
	{
		Matrix4x4	rotMat		=Matrix4x4.CreateFromQuaternion(mRotation);
		Matrix4x4	transMat	=Matrix4x4.CreateTranslation(mPosition);
		Matrix4x4	scaleMat	=Matrix4x4.CreateScale(mScale);

		Matrix4x4	final	=scaleMat * rotMat * transMat;

		final	*=trans;

		Matrix4x4.Decompose(final, out mScale, out mRotation, out mPosition);
	}


	internal void ConvertToLeftHanded()
	{
		Matrix4x4	rotMat		=Matrix4x4.CreateFromQuaternion(mRotation);
		Matrix4x4	transMat	=Matrix4x4.CreateTranslation(mPosition);
		Matrix4x4	scaleMat	=Matrix4x4.CreateScale(mScale);

		Matrix4x4	final	=scaleMat * rotMat * transMat;

		RightHandToLeft(ref final);

		Matrix4x4.Decompose(final, out mScale, out mRotation, out mPosition);
	}


	public static void RightHandToLeft(ref Matrix4x4 mat)
	{
		mat.M31	=-mat.M31;
		mat.M32	=-mat.M32;
		mat.M33	=-mat.M33;
		mat.M34	=-mat.M34;

		mat.M13	=-mat.M13;
		mat.M23	=-mat.M23;
		mat.M33	=-mat.M33;
		mat.M43	=-mat.M43;
	}


	internal static void Lerp(KeyFrame key0, KeyFrame key1,
		float percentage, KeyFrame result)
	{
		result.mPosition	=Vector3.Lerp(key0.mPosition, key1.mPosition, percentage);
		result.mScale		=Vector3.Lerp(key0.mScale, key1.mScale, percentage);
		result.mRotation	=Quaternion.Slerp(key0.mRotation, key1.mRotation, percentage);
	}
}