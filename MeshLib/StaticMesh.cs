using System;
using System.IO;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using Vortice.DXGI;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.Mathematics;
using UtilityLib;
using MaterialLib;

using MatLib	=MaterialLib.MaterialLib;

namespace MeshLib;

public class StaticMesh
{
	MeshPartStuff	mParts;

	//bounds
	BoundingBox		mBoxBound;
	BoundingSphere	mSphereBound;

	//transform
	Matrix4x4	mTransform;

	//materials per part
	List<MeshMaterial>	mPartMats	=new List<MeshMaterial>();


	public StaticMesh(IArch statA)
	{
		mParts	=new MeshPartStuff(statA);

		SetTransform(Matrix4x4.Identity);
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


	public Matrix4x4 GetTransform()
	{
		return	mTransform;
	}


	//set only one part's material transform
	public void SetPartTransform(int idx, Matrix4x4 mat)
	{
		mParts.SetMatObjTransform(idx, mat);
	}


	public void SetTransform(Matrix4x4 mat)
	{
		mTransform		=mat;

		//set in the materials
		mParts.SetMatObjTransforms(mat);
	}


	public Dictionary<Mesh, BoundingBox> GetBoundData()
	{
		return	mParts.GetBoundData();
	}


	public BoundingBox GetBoxBound()
	{
		return	mParts.GetBoxBound();
	}


	public BoundingSphere GetSphereBound()
	{
		return	mParts.GetSphereBound();
	}


	public BoundingBox GetTransformedBoxBound()
	{
		BoundingBox	box	=mParts.GetBoxBound();

		box.Min	=Vector3.Transform(box.Min, mTransform);
		box.Max	=Vector3.Transform(box.Max, mTransform);

		return	box;
	}


	public BoundingSphere GetTransformedSphereBound()
	{
		BoundingSphere	ret	=mParts.GetSphereBound();

		return	Mathery.TransformSphere(mTransform, ret);
	}


	public void SetMatLib(MatLib mats, StuffKeeper sk)
	{
		mParts.SetMatLibs(mats, sk);
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


	public void SetPartMaterialName(int index, string matName,
									StuffKeeper sk)
	{
		mParts.SetPartMaterialName(index, matName, sk);
	}


	public string GetPartMaterialName(int index)
	{
		return	mParts.GetPartMaterialName(index);
	}


	public int GetNumParts()
	{
		return	mParts.GetNumParts();
	}


	public void SetPartVisible(int index, bool bVisible)
	{
		mParts.SetPartVisible(index, bVisible);
	}


	public void SetTriLightValues(
		Vector4 col0, Vector4 col1, Vector4 col2, Vector3 lightDir)
	{
		lightDir.Length();
		mParts.SetTriLightValues(col0, col1, col2, lightDir);
	}


	public void UpdateBounds()
	{
		mBoxBound		=mParts.GetBoxBound();
		mSphereBound	=mParts.GetSphereBound();
	}


	public void Draw(ID3D11DeviceContext dc, MatLib matLib)
	{
		mParts.Draw(dc);
	}


	public void Draw(ID3D11DeviceContext dc, MatLib matLib, string altMaterial)
	{
		mParts.Draw(dc, altMaterial);
	}


	public void DrawX(ID3D11DeviceContext dc, MatLib matLib, int numInst, string altMaterial)
	{
		mParts.DrawX(dc, altMaterial, numInst);
	}


	public void DrawDMN(ID3D11DeviceContext dc, MatLib matLib)
	{
		mParts.DrawDMN(dc);
	}


	public Vector3 GetForwardVector()
	{
		return	Vector3.TransformNormal(Vector3.UnitZ, mTransform);
	}


	public void SaveToFile(string fileName)
	{
		FileStream		file	=new FileStream(fileName, FileMode.Create, FileAccess.Write);
		BinaryWriter	bw		=new BinaryWriter(file);

		//write a magic number identifying mesh instances
		UInt32	magic	=0x57A71C15;

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
		if(magic != 0x57A71C15)
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


	//this if for the DMN renderererererer
	public void AssignMaterialIDs(MaterialLib.IDKeeper idk)
	{
		mParts.AssignMaterialIDs(idk);
	}
}