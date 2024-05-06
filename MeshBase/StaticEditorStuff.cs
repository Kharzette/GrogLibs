using System;
using System.IO;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using Vortice.DXGI;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.Mathematics;

namespace MeshLib;

//editor / tool related code
public partial class StaticMesh
{
	public void GenTangents(ID3D11Device gd,
		List<int> parts, int texCoordSet)
	{
		SharedMeshStuff.GenTangents(gd, mParts, parts, texCoordSet);
	}


	public void	NukeVertexElements(ID3D11Device gd,
		List<int> indexes,
		List<int> vertElementIndexes)
	{
		SharedMeshStuff.NukeVertexElements(gd, mParts, indexes, vertElementIndexes);
	}


	public void GenerateRoughBounds()
	{
		mBound	=new MeshBound();
		mBound.ComputeOverall(mParts, mTransforms);
	}


	//find borders of shared verts between mesh parts
	public List<EditorMesh.WeightSeam> Frankenstein()
	{
		return	SharedMeshStuff.Frankenstein(mParts);
	}


	//for conversion to BSP, probably won't work very well
	public void GetPartPlanes(int meshIndex, out List<Vector3> normals, out List<float> distances)
	{
		if(meshIndex < 0 || meshIndex >= mParts.Count)
		{
			normals		=new List<Vector3>();
			distances	=new List<float>();
			return;
		}

		Mesh		m			=mParts[meshIndex];
		Matrix4x4	partTrans	=mTransforms[meshIndex];

		SharedMeshStuff.GetPartPlanes(m, partTrans, out normals, out distances);
	}


	public int GetPartColladaPolys(int meshIndex, out string polys, out string counts)
	{
		polys	=null;
		counts	=null;
		if(meshIndex < 0 || meshIndex >= mParts.Count)
		{
			return	0;
		}

		Mesh	m	=mParts[meshIndex];

		return	SharedMeshStuff.GetPartColladaPolys(m, out polys, out counts);
	}


	public void GetPartPositions(int meshIndex, out List<Vector3> positions, out List<int> indexes)
	{
		positions	=null;
		indexes		=null;
		if(meshIndex < 0 || meshIndex >= mParts.Count)
		{
			return;
		}

		Mesh		m			=mParts[meshIndex];
		Matrix4x4	partTrans	=mTransforms[meshIndex];

		SharedMeshStuff.GetPartPositions(m, partTrans, out positions, out indexes);
	}


	public void GetPartColladaPositions(int meshIndex, out float []positions)
	{
		positions	=null;
		if(meshIndex < 0 || meshIndex >= mParts.Count)
		{
			return;
		}

		Mesh	m	=mParts[meshIndex];

		SharedMeshStuff.GetPartColladaPositions(m, out positions);
	}


	public void GetPartColladaNormals(int meshIndex, out float []normals)
	{
		normals	=null;
		if(meshIndex < 0 || meshIndex >= mParts.Count)
		{
			return;
		}

		Mesh	m	=mParts[meshIndex];

		SharedMeshStuff.GetPartColladaNormals(m, out normals);
	}


	public Matrix4x4 GetPartTransform(int meshIndex)
	{
		if(meshIndex < 0 || meshIndex >= mParts.Count)
		{
			return	Matrix4x4.Identity;
		}
		return	mTransforms[meshIndex];
	}


	public Type GetPartVertexType(int index)
	{
		if(index < 0 || index >= mParts.Count)
		{
			return	null;
		}
		return	mParts[index].VertexType;
	}


	public string	GetPartName(int index)
	{
		if(index < 0 || index >= mParts.Count)
		{
			return	"";
		}
		return	mParts[index].Name;
	}


	public bool RenamePart(int index, string newName)
	{
		if(index < 0 || index >= mParts.Count)
		{
			return	false;
		}

		mParts[index].Name	=newName;

		return	true;
	}
}