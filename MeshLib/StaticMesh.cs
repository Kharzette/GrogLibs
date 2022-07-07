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

	//transform
	Matrix4x4	mTransform;


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


	public void Draw(MatLib mlib)
	{
		mParts.Draw(mlib);
	}


	public void Draw(MatLib mlib, string altMaterial)
	{
		mParts.Draw(mlib, altMaterial);
	}


	public void DrawX(MatLib mlib, int numInst, string altMaterial)
	{
		mParts.DrawX(mlib, altMaterial, numInst);
	}


	public void DrawDMN(MatLib mlib)
	{
		mParts.DrawDMN(mlib);
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