using System;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using Vortice.Mathematics;
using Vortice.Direct3D11;
using UtilityLib;

using MatLib	=MaterialLib.MaterialLib;

namespace MeshLib;

//editor / tool related code
public partial class Character
{
	bool RenamePart(int index, string newName)
	{
		if(index < 0 || index >= mParts.Count)
		{
			return	false;
		}

		mParts[index].Name	=newName;

		return	true;
	}


	int	GetPartCount()
	{
		return	mParts.Count;
	}


	void GenTangents(ID3D11Device gd,
		List<int> parts, int texCoordSet)
	{
		SharedMeshStuff.GenTangents(gd, mParts, parts, texCoordSet);
	}


	void NukeVertexElements(ID3D11Device gd,
		List<int> indexes,
		List<int> vertElementIndexes)
	{
		SharedMeshStuff.NukeVertexElements(gd, mParts, indexes, vertElementIndexes);
	}


	Type GetPartVertexType(int index)
	{
		if(index < 0 || index >= mParts.Count)
		{
			return	null;
		}
		return	mParts[index].VertexType;
	}


	string GetPartName(int index)
	{
		if(index < 0 || index >= mParts.Count)
		{
			return	"";
		}
		return	mParts[index].Name;
	}


	void ReIndexVertWeights(ID3D11Device gd, Dictionary<int, int> idxMap)
	{
		foreach(Mesh m in mParts)
		{
			EditorMesh	em	=m.GetEditorMesh();
			if(em == null)
			{
				continue;
			}

			em.ReIndexVertWeights(gd, idxMap);
		}
	}


	void GetBoneNamesInUseByDraw(List<string> names)
	{
		Skeleton	skel	=mAnimLib.GetSkeleton();

		if(skel == null)
		{
			Debug.WriteLine("No skeleton in GetBoneNamesInUseByDraw()");
			return;
		}

		for(int i=0;i < mParts.Count;i++)
		{
			Mesh		m	=mParts[i];
			EditorMesh	em	=m.GetEditorMesh();
			if(em == null)
			{
				return;
			}

			List<int>	boneInds	=new List<int>();

			em.GetBonesInUseByDraw(boneInds);

			foreach(int idx in boneInds)
			{
				string	boneName	=skel.GetBoneName(idx);
				if(!names.Contains(boneName))
				{
					names.Add(boneName);
				}
			}
		}
	}


	//this can be used to rebuild the bones if the skeleton changed
	public void ReBuildBones(ID3D11Device gd)
	{
		//clear
		mBones	=null;

		Skeleton	sk	=mAnimLib.GetSkeleton();
		if(sk == null)
		{
			return;
		}

		Dictionary<int, int>	reMap	=new Dictionary<int, int>();
		sk.Compact(reMap);

		for(int i=0;i < mParts.Count;i++)
		{
			EditorMesh	em	=mParts[i].GetEditorMesh();
			if(em == null)
			{
				continue;
			}
			em.ReIndexVertWeights(gd, reMap);
		}
	}


	//this will be a starting point, user can edit the shape
	//in ColladaConvert
	void GenerateRoughBounds()
	{
		mBound.ComputeOverall(mParts, null);
	}


	public bool	GetRoughBoundChoice()
	{
		return	mBound.GetRoughBoundChoice();
	}
	
	
	public BoundingBox GetRoughBoxBound()
	{
		return	mBound.GetRoughBox();
	}


	BoundingSphere GetRoughSphereBound()
	{
		return	mBound.GetRoughSphere();
	}


	public void	SetRoughBoundChoice(bool bBox)
	{
		mBound.SetRoughChoice(bBox);
	}


	bool?	GetPartBoundChoice(int index)
	{
		if(index < 0 || index >= mParts.Count)
		{
			return	null;
		}
		return	mBound.GetPartChoice(index);
	}


	BoundingBox?	GetPartBoxBound(int index)
	{
		if(index < 0 || index >= mParts.Count)
		{
			return	null;
		}

		return	mBound.GetPartBox(index);
	}


	BoundingSphere?	GetPartSphereBound(int index)
	{
		if(index < 0 || index >= mParts.Count)
		{
			return	null;
		}

		return	mBound.GetPartSphere(index);
	}


	public Dictionary<int, Matrix4x4> GetBoneTransforms(Skeleton skel)
	{
		Dictionary<int, Matrix4x4>	ret	=new Dictionary<int, Matrix4x4>();

		int	numBones	=skel.GetNumIndexedBones();

		for(int i=0;i < numBones;i++)
		{
			Matrix4x4	bone	=mSkin.GetBoneByIndex(i, skel);

			ret.Add(i, bone);
		}
		return	ret;
	}


	public void BuildDebugBoundDrawData(CommonPrims cprims)
	{
		mSkin?.BuildDebugBoundDrawData(cprims);
	}

	public void BuildDebugBoundDrawData(int index, CommonPrims cprims)
	{
		mSkin?.BuildDebugBoundDrawData(index, cprims);
	}


	void ComputeBoneBounds(Skeleton skel)
	{
		int	numBones	=skel.GetNumIndexedBones();

		for(int i=0;i < numBones;i++)
		{
			List<Vector3>	boxPoints	=new List<Vector3>();

			foreach(Mesh m in mParts)
			{
				EditorMesh	em	=m.GetEditorMesh();
				if(em == null)
				{
					return;
				}

				BoundingBox		box;
				BoundingSphere	sphere;

				em.ComputeInfluencedBound(i, out box, out sphere);

				if(box.Min != Vector3.Zero
					&& box.Max != Vector3.Zero)
				{
					boxPoints.Add(box.Min);
					boxPoints.Add(box.Max);
				}
			}

			if(boxPoints.Count > 0)
			{
				//make a copy that is transformed by inverse bind pose
				List<Vector3>	boneyPoints	=new List<Vector3>(boxPoints);

				//this makes a better box bound
				mSkin.MulByIBP(i, boneyPoints);

				//sometimes the origin of the bone, where the joint is
				//doesn't get a point
				boneyPoints.Add(Vector3.Zero);

				BoundingBox	worldBox	=BoundingBox.CreateFromPoints(boxPoints.ToArray());
				BoundingBox	boneBox		=BoundingBox.CreateFromPoints(boneyPoints.ToArray());

				//but cyl and sphere get a better bound from the world box
				//so pass them both in
				mSkin.SetBoneBounds(i, worldBox, boneBox);
			}
		}
	}


	//find borders of shared verts between mesh parts
	List<EditorMesh.WeightSeam> Frankenstein()
	{
		return	SharedMeshStuff.Frankenstein(mParts);
	}


	void GetPartPlanes(int meshIndex, out List<Vector3> normals, out List<float> distances)
	{
		normals		=new List<Vector3>();
		distances	=new List<float>();
		return;
	}


	int GetPartColladaPolys(int meshIndex, out string polys, out string counts)
	{
		polys	=null;
		counts	=null;
		if(meshIndex < 0 || meshIndex >= mParts.Count)
		{
			return	0;
		}

		return	SharedMeshStuff.GetPartColladaPolys(mParts[meshIndex], out polys, out counts);
	}


	void GetPartPositions(int meshIndex, out List<Vector3> positions, out List<int> indexes)
	{
		positions	=null;
		indexes		=null;
		if(meshIndex < 0 || meshIndex >= mParts.Count)
		{
			return;
		}

		SharedMeshStuff.GetPartPositions(mParts[meshIndex], Matrix4x4.Identity, out positions, out indexes);
	}


	void GetPartColladaPositions(int meshIndex, out float []positions)
	{
		positions	=null;
		if(meshIndex < 0 || meshIndex >= mParts.Count)
		{
			return;
		}

		SharedMeshStuff.GetPartColladaPositions(mParts[meshIndex], out positions);
	}


	void GetPartColladaNormals(int meshIndex, out float []normals)
	{
		normals	=null;
		if(meshIndex < 0 || meshIndex >= mParts.Count)
		{
			return;
		}

		SharedMeshStuff.GetPartColladaNormals(mParts[meshIndex], out normals);
	}


	Matrix4x4 GetPartTransform(int meshIndex)
	{
		return	Matrix4x4.Identity;
	}
}
