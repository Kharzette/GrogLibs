using System;
using System.Collections.Generic;
using System.IO;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;

//ambiguous stuff
using Color		=SharpDX.Color;
using Device	=SharpDX.Direct3D11.Device;
using MatLib	=MaterialLib.MaterialLib;


namespace MeshLib
{
	public class StaticMesh
	{
		StaticArch	mArch;

		//transform
		Matrix	mTransform;
		Matrix	mTransInverted;


		public StaticMesh(StaticArch statA)
		{
			mArch	=statA;

			SetTransform(Matrix.Identity);
		}


		public void FreeAll()
		{
			mArch		=null;
		}


		public Matrix GetTransform()
		{
			return	mTransform;
		}


		public void SetTransform(Matrix mat)
		{
			mTransform		=mat;
			mTransInverted	=mTransform;

			mTransInverted.Invert();
		}


		public BoundingBox GetBoxBound()
		{
			BoundingBox	box	=mArch.GetBoxBound();

			box.Minimum	=Vector3.TransformCoordinate(box.Minimum, mTransform);
			box.Maximum	=Vector3.TransformCoordinate(box.Maximum, mTransform);

			return	box;
		}


		public BoundingSphere GetSphereBound()
		{
			BoundingSphere	ret	=mArch.GetSphereBound();

			ret.Center	=Vector3.TransformCoordinate(ret.Center, mTransform);
			ret.Radius	*=mTransform.ScaleVector.Length();

			return	ret;
		}


		public void SetTriLightValues(MatLib mats,
			Vector4 col0, Vector4 col1, Vector4 col2, Vector3 lightDir)
		{
			mArch.SetTriLightValues(mats, col0, col1, col2, lightDir);
		}


		public void Draw(DeviceContext dc, MaterialLib.MaterialLib matLib)
		{
			mArch.Draw(dc, matLib, mTransform);
		}


		public void DrawDMN(DeviceContext dc,
			MaterialLib.MaterialLib matLib)
		{
			mArch.DrawDMN(dc, matLib, mTransform);
		}


		public void Draw(DeviceContext dc,
			MaterialLib.MaterialLib matLib,
			string altMatName)
		{
			mArch.Draw(dc, matLib, altMatName, mTransform);
		}


		//TODO: needs testing
		public float? RayIntersect(Vector3 start, Vector3 end, bool bBox, out Mesh partHit)
		{
			//backtransform the ray
			Vector3	backStart	=Vector3.TransformCoordinate(start, mTransInverted);
			Vector3	backEnd		=Vector3.TransformCoordinate(end, mTransInverted);

			return	mArch.RayIntersect(backStart, backEnd, bBox, out partHit);
		}
	}
}