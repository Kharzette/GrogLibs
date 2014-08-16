using System;
using System.Diagnostics;
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

		//materials per part
		List<MeshMaterial>	mPartMats	=new List<MeshMaterial>();


		public StaticMesh(StaticArch statA)
		{
			mArch	=statA;

			SetTransform(Matrix.Identity);
		}


		public void FreeAll()
		{
			mArch		=null;
		}


		public bool SetPartName(int index, string name)
		{
			return	mArch.RenameMesh(index, name);
		}


		public void SetPartVisible(int index, bool bVisible)
		{
			Debug.Assert(index >= 0 && index < mPartMats.Count);

			if(index < 0 || index >= mPartMats.Count)
			{
				return;
			}

			mPartMats[index].mbVisible	=bVisible;
		}


		public void SetPartMaterialName(int index, string matName)
		{
			Debug.Assert(index >= 0 && index < mPartMats.Count);

			if(index < 0 || index >= mPartMats.Count)
			{
				return;
			}

			mPartMats[index].mMaterialName	=matName;
		}


		public void AddMeshPart(Mesh m, MatLib mats)
		{
			mArch.AddMeshPart(m);

			MeshMaterial	mm	=new MeshMaterial();

			mm.mMatLib			=mats;
			mm.mMaterialName	="NoMaterial";
			mm.mbVisible		=true;
			mm.mObjectTransform	=mTransform;

			mPartMats.Add(mm);
		}


		public void NukeMeshPart(List<int> indexes)
		{
			List<MeshMaterial>	toNuke	=new List<MeshMaterial>();
			foreach(int ind in indexes)
			{
				Debug.Assert(ind >= 0 && ind < mPartMats.Count);

				if(ind < 0 || ind >= mPartMats.Count)
				{
					continue;
				}

				toNuke.Add(mPartMats[ind]);
			}

			mPartMats.RemoveAll(mp => toNuke.Contains(mp));

			toNuke.Clear();

			mArch.NukeMesh(indexes);
		}


		public Mesh GetMeshPart(int index)
		{
			return	mArch.GetMeshPart(index);
		}


		public int GetMeshPartCount()
		{
			return	mArch.GetMeshPartList().Count;
		}


		public string GetMeshPartName(int index)
		{
			Mesh	mp	=mArch.GetMeshPart(index);

			if(mp == null)
			{
				return	"null part";
			}
			return	mp.Name;
		}


		public Type GetMeshPartVertexType(int index)
		{
			Mesh	mp	=mArch.GetMeshPart(index);

			if(mp == null)
			{
				return	null;	//does this work for Type?
			}
			return	mp.VertexType;
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
			foreach(MeshMaterial mm in mPartMats)
			{
				mm.mObjectTransform	=mat;
			}
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


		public void SetTriLightValues(
			Vector4 col0, Vector4 col1, Vector4 col2, Vector3 lightDir)
		{
			foreach(MeshMaterial mm in mPartMats)
			{
				mm.mMatLib.SetTriLightValues(
					mm.mMaterialName, col0, col1, col2, lightDir);
			}
		}


		public void Draw(DeviceContext dc)
		{
			mArch.Draw(dc, mPartMats);
		}


		public void DrawDMN(DeviceContext dc)
		{
			mArch.DrawDMN(dc, mPartMats);
		}


		public void Draw(DeviceContext dc,
			MaterialLib.MaterialLib matLib,
			string altMatName)
		{
			mArch.Draw(dc, mPartMats);
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