using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using UtilityLib;

//ambiguous stuff
using Color		=SharpDX.Color;
using Device	=SharpDX.Direct3D11.Device;
using MatLib	=MaterialLib.MaterialLib;


namespace MeshLib
{
	public class StaticMesh
	{
		MeshPartStuff	mParts;

		//bounds
		BoundingBox		mBoxBound;
		BoundingSphere	mSphereBound;

		//transform
		Matrix	mTransform;
		Matrix	mTransInverted;

		//materials per part
		List<MeshMaterial>	mPartMats	=new List<MeshMaterial>();


		public StaticMesh(IArch statA)
		{
			mParts	=new MeshPartStuff(statA);

			SetTransform(Matrix.Identity);
		}


		public void FreeAll()
		{
			mParts.FreeAll();

			mParts	=null;
		}


		public bool IsEmpty()
		{
			return	mParts.IsEmpty();
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

			//set in the materials
			mParts.SetMatObjTransforms(mat);
		}


		public BoundingBox GetBoxBound()
		{
			BoundingBox	box	=mParts.GetBoxBound();

			box.Minimum	=Vector3.TransformCoordinate(box.Minimum, mTransform);
			box.Maximum	=Vector3.TransformCoordinate(box.Maximum, mTransform);

			return	box;
		}


		public BoundingSphere GetSphereBound()
		{
			BoundingSphere	ret	=mParts.GetSphereBound();

			return	Mathery.TransformSphere(mTransform, ret);
		}


		public void SetMatLib(MatLib mats)
		{
			mParts.SetMatLibs(mats);
		}


		//these index the same as the mesh part list in the archetype
		public void AddPart(MatLib mats)
		{
			mParts.AddPart(mats, mTransform);
		}


		public void NukePart(int index)
		{
			mParts.NukePart(index);
		}


		public void NukeParts(List<int> indexes)
		{
			mParts.NukeParts(indexes);
		}


		public void SetPartMaterialName(int index, string matName)
		{
			mParts.SetPartMaterialName(index, matName);
		}


		public string GetPartMaterialName(int index)
		{
			return	mParts.GetPartMaterialName(index);
		}


		public void SetPartVisible(int index, bool bVisible)
		{
			mParts.SetPartVisible(index, bVisible);
		}


		public void SetTriLightValues(
			Vector4 col0, Vector4 col1, Vector4 col2, Vector3 lightDir)
		{
			mParts.SetTriLightValues(col0, col1, col2, lightDir);
		}


		//TODO: needs testing
		public float? RayIntersect(Vector3 start, Vector3 end, bool bBox, out Mesh partHit)
		{
			//backtransform the ray
			Vector3	backStart	=Vector3.TransformCoordinate(start, mTransInverted);
			Vector3	backEnd		=Vector3.TransformCoordinate(end, mTransInverted);

			return	mParts.RayIntersect(backStart, backEnd, bBox, out partHit);
		}

	
		public float? RayIntersect(Vector3 start, Vector3 end, bool bBox)
		{
			//backtransform the ray
			Vector3	backStart	=Vector3.TransformCoordinate(start, mTransInverted);
			Vector3	backEnd		=Vector3.TransformCoordinate(end, mTransInverted);

			if(bBox)
			{
				return	Mathery.RayIntersectBox(backStart, backEnd, mBoxBound);
			}
			else
			{
				return	Mathery.RayIntersectSphere(backStart, backEnd, mSphereBound);
			}
		}


		public void UpdateBounds()
		{
			mBoxBound		=mParts.GetBoxBound();
			mSphereBound	=mParts.GetSphereBound();
		}


		public void Draw(DeviceContext dc, MatLib matLib)
		{
			mParts.Draw(dc);
		}


		public void Draw(DeviceContext dc, MatLib matLib, string altMaterial)
		{
			mParts.Draw(dc, altMaterial);
		}


		public void DrawDMN(DeviceContext dc, MatLib matLib)
		{
			mParts.DrawDMN(dc);
		}


		public Vector3 GetForwardVector()
		{
			return	mTransform.Forward;
		}


		public void SaveToFile(string fileName)
		{
			FileStream		file	=new FileStream(fileName, FileMode.Create, FileAccess.Write);
			BinaryWriter	bw		=new BinaryWriter(file);

			//write a magic number identifying characters
			UInt32	magic	=0xCA1EC7BE;

			bw.Write(magic);

			//save mesh parts
			mParts.Write(bw);

			bw.Close();
			file.Close();
		}


		public bool ReadFromFile(string fileName)
		{
			if(!File.Exists(fileName))
			{
				return	false;
			}

			Stream	file	=new FileStream(fileName, FileMode.Open, FileAccess.Read);
			if(file == null)
			{
				return	false;
			}
			BinaryReader	br	=new BinaryReader(file);

			UInt32	magic	=br.ReadUInt32();
			if(magic != 0xCA1EC7BE)
			{
				br.Close();
				file.Close();
				return	false;
			}

			mParts.Read(br);

			br.Close();
			file.Close();

			return	true;
		}
	}
}