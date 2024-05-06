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

namespace MeshLib;

//an instance of a non animating boneless style mesh (or meshes)
public partial class StaticMesh
{
	//Overall transform of the whole thing
	Matrix4x4	mTransform;

	//bounding information
	MeshBound	mBound;

	//mesh parts and their relative xforms
	//should always have the same count
	List<Mesh>			mParts		=new List<Mesh>();
	List<Matrix4x4>		mTransforms	=new List<Matrix4x4>();
	List<MeshMaterial>	mPartMats	=new List<MeshMaterial>();


	public StaticMesh()
	{
		SetTransform(Matrix4x4.Identity);
	}


	//construct from file + a dictionary of possible part meshes
	public StaticMesh(string fileName, Dictionary<string, Mesh> meshes)
	{
		if(!File.Exists(fileName))
		{
			return;
		}

		Stream	file	=new FileStream(fileName, FileMode.Open, FileAccess.Read);
		if(file == null)
		{
			return;
		}

		BinaryReader	br	=new BinaryReader(file);

		UInt32	magic	=br.ReadUInt32();
		if(magic != 0x57A71C15)
		{
			br.Close();
			file.Close();
			return;
		}

		mTransform	=FileUtil.ReadMatrix(br);

		mBound	=new MeshBound();
		mBound.Read(br);

		int	numParts	=br.ReadInt32();

		for(int i=0;i < numParts;i++)
		{
			string	name	=br.ReadString();

			Matrix4x4		mat	=FileUtil.ReadMatrix(br);
			MeshMaterial	mm	=new MeshMaterial(br);

			if(!meshes.ContainsKey(name))
			{
				continue;
			}

			mParts.Add(meshes[name]);
			mTransforms.Add(mat);
			mPartMats.Add(mm);
		}

		br.Close();
		file.Close();
	}


	public void FreeAll()
	{
		mParts.Clear();	//don't free Mesh parts, this is an instance

		mParts	=null;

		mBound.FreeAll();
	}


	public void AddPart(Mesh part, Matrix4x4 mat, string matName)
	{
		if(matName == null || matName == "")
		{
			matName	="default";
		}

		mParts.Add(part);
		mTransforms.Add(mat);

		MeshMaterial	mm	=new MeshMaterial();

		mm.mbVisible		=true;
		mm.mMaterialID		=0;
		mm.mMaterialName	=matName;

		mPartMats.Add(mm);
	}


	public int GetPartCount()
	{
		if(mParts == null)
		{
			return	0;
		}
		return	mParts.Count;
	}


	public bool IsEmpty()
	{
		return	(mParts.Count == 0);
	}


	public Matrix4x4 GetTransform()
	{
		return	mTransform;
	}


	public void GetRoughBounds(out BoundingBox ?box, out BoundingSphere ?sph)
	{
		if(mBound == null)
		{
			box	=null;
			sph	=null;
			return;
		}
		
		box	=mBound.GetRoughBox();
		sph	=mBound.GetRoughSphere();
	}


	public void SetTransform(Matrix4x4 mat)
	{
		mTransform		=mat;
	}


	//Checks first against a bound encompassing all parts
	public bool RayIntersect(Vector3 startPos, Vector3 endPos, float rayRadius,
							out Vector3 hitPos, out Vector3 hitNorm)
	{
		hitPos	=hitNorm	=Vector3.Zero;

		if(!mBound.RayIntersectRough(ref mTransform, startPos, endPos, rayRadius))
		{
			return	false;
		}

		return	mBound.RayIntersectParts(ref mTransform, mTransforms,
			startPos, endPos, rayRadius,
			out hitPos, out hitNorm);
	}


	//I think in all cases where this is used the part meshes go too
	public void NukePart(int index)
	{
		if(index < 0 || index >= mParts.Count)
		{
			return;
		}

		Mesh	m	=mParts[index];

		m.FreeAll();

		mParts.RemoveAt(index);
		mTransforms.RemoveAt(index);
	}


	public void NukeParts(List<int> indexes)
	{
		List<Mesh>		toNuke	=new List<Mesh>();
		List<Matrix4x4>	toNukeT	=new List<Matrix4x4>();
		foreach(int ind in indexes)
		{
			Debug.Assert(ind >= 0 && ind < mParts.Count);

			if(ind < 0 || ind >= mParts.Count)
			{
				continue;
			}

			toNuke.Add(mParts[ind]);
			toNukeT.Add(mTransforms[ind]);
		}

		mParts.RemoveAll(mp => toNuke.Contains(mp));
		mTransforms.RemoveAll(mp => toNukeT.Contains(mp));

		foreach(Mesh m in toNuke)
		{
			m.FreeAll();
		}

		toNuke.Clear();
		toNukeT.Clear();
	}


	public void SetPartVisible(int index, bool bVisible)
	{
		if(index < 0 || index >= mParts.Count)
		{
			return;
		}
		mPartMats[index].mbVisible	=bVisible;
	}


	public Vector3 GetForwardVector()
	{
		return	Vector3.TransformNormal(Vector3.UnitZ, mTransform);
	}


	public void SaveParts(string pathDir)
	{
		foreach(Mesh m in mParts)
		{
			m.Write(pathDir + "/" + m.Name + ".mesh");
		}
	}

	
	public void SaveToFile(string fileName)
	{
		if(mBound == null)
		{
			Debug.WriteLine("Bound not set up yet!");
			Debug.Assert(false);
			return;
		}

		Debug.Assert(mParts.Count == mTransforms.Count);
		Debug.Assert(mParts.Count == mPartMats.Count);

		FileStream		file	=new FileStream(fileName, FileMode.Create, FileAccess.Write);
		BinaryWriter	bw		=new BinaryWriter(file);

		//write a magic number identifying mesh instances
		UInt32	magic	=0x57A71C15;

		bw.Write(magic);

		FileUtil.WriteMatrix(bw, mTransform);

		mBound.Write(bw);

		bw.Write(mParts.Count);

		for(int i=0;i < mParts.Count;i++)
		{
			FileUtil.WriteMatrix(bw, mTransforms[i]);
			mPartMats[i].Write(bw);
		}

		bw.Close();
		file.Close();
	}


	public bool GetSubBoundChoice(int idx)
	{
		return	mBound.GetSubBoundChoice(idx);
	}


	public int GetNumSubBounds()
	{
		return	mBound.GetNumSubBounds();
	}
}