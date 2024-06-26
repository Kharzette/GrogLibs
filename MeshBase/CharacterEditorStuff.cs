﻿using System;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using Vortice.Mathematics;
using Vortice.Direct3D11;
using UtilityLib;

namespace MeshLib;

//editor / tool related code
public partial class Character
{
	const float	MinVolume	=0.05f;	//in meters


	public bool RenamePart(int index, string newName)
	{
		if(index < 0 || index >= mParts.Count)
		{
			return	false;
		}

		mParts[index].Name	=newName;

		return	true;
	}


	public void AdjustBoundLength(float lenDelta, bool bChoice)
	{
		mBound.SetRoughChoice(bChoice);
		mBound?.AdjustLength(lenDelta);
	}

	public void AdjustBoundRadius(float lenDelta, bool bChoice)
	{
		mBound.SetRoughChoice(bChoice);
		mBound?.AdjustRadius(lenDelta);
	}

	public void AdjustBoundDepth(float lenDelta, bool bChoice)
	{
		mBound.SetRoughChoice(bChoice);
		mBound?.AdjustDepth(lenDelta);
	}


	public void GenTangents(ID3D11Device gd,
		List<int> parts, int texCoordSet)
	{
		SharedMeshStuff.GenTangents(gd, mParts, parts, texCoordSet);
	}


	public void NukeVertexElements(ID3D11Device gd,
		List<int> indexes,
		List<int> vertElementIndexes)
	{
		SharedMeshStuff.NukeVertexElements(gd, mParts, indexes, vertElementIndexes);
	}


	public Type GetPartVertexType(int index)
	{
		if(index < 0 || index >= mParts.Count)
		{
			return	null;
		}
		return	mParts[index].VertexType;
	}


	public string GetPartName(int index)
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


	public void GetBoneNamesInUseByDraw(List<string> names)
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
	public void GenerateRoughBounds()
	{
		Skeleton	skel	=mAnimLib.GetSkeleton();
		if(skel == null)
		{
			return;
		}

		int	boneCount	=skel.GetBoneCount();
		if(boneCount <= 0)
		{
			return;
		}

		//see if a guess of bone bounds have been created
		if(mSkin.GetBoneBoundBox(0) == null)
		{
			ComputeBoneBounds(skel);
		}

		Vector3	[]corners	=new Vector3[8];

		Vector3	max	=Vector3.One * -float.MaxValue;
		Vector3	min	=Vector3.One * float.MaxValue;

		Vector3	center	=Vector3.Zero;

		for(int i=0;i < boneCount;i++)
		{
			int	choice	=mSkin.GetBoundChoice(i);
			if(choice == Skin.Invalid)
			{
				continue;
			}

			//rattling the
			BoundingBox	?boneBox;

			//this is the most likely
			if(choice == Skin.Capsule)
			{
				BoundingCapsule	?bc	=mSkin.GetBoneBoundCapsule(i);
				if(bc == null)
				{
					continue;
				}

				boneBox	=BoundingCapsule.BoxFromCapsule(bc.Value);
			}
			else if(choice == Skin.Sphere)
			{
				BoundingSphere	?bs	=mSkin.GetBoneBoundSphere(i);				
				if(bs == null)
				{
					continue;
				}

				boneBox	=new BoundingBox(bs.Value.Center - (Vector3.One * bs.Value.Radius),
					bs.Value.Center + (Vector3.One * bs.Value.Radius));
			}
			else if(choice == Skin.Box)
			{
				boneBox	=mSkin.GetBoneBoundBox(i);
				if(boneBox == null)
				{
					continue;
				}
			}
			else
			{
				continue;
			}
			
			Vector3	size	=boneBox.Value.Max - boneBox.Value.Min;
			float	vol		=size.X + size.Y + size.Z;

			//skip bones without much influence?
			if(vol < MinVolume)	//should be around an inch cubed
			{
				continue;
			}

			boneBox?.GetCorners(corners);

			Matrix4x4	bone	=mSkin.GetBoneByIndexNoBind(i, skel);

			Vector3	boxCenter	=Vector3.Zero;
			for(int j=0;j < 8;j++)
			{
				Vector3	transd	=Vector3.Transform(corners[j], bone);

				Mathery.AddPointToBoundingBox(ref min, ref max, transd);

				boxCenter	+=transd;
			}

			center	+=boxCenter / 8;
		}

		center	/=boneCount;

		mBound	=new MeshBound();
		mBound.ComputeRoughFromBox(center, min, max);
	}


	public bool	GetRoughBoundChoice()
	{
		return	mBound.GetRoughBoundChoice();
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

				BoundingBox	boneBox		=BoundingBox.CreateFromPoints(boneyPoints.ToArray());

				//but cyl and sphere get a better bound from the world box
				//so pass them both in
				mSkin.SetBoneBounds(i, boneBox);
			}
		}
	}


	//find borders of shared verts between mesh parts
	public List<EditorMesh.WeightSeam> Frankenstein()
	{
		return	SharedMeshStuff.Frankenstein(mParts);
	}


	void GetPartPlanes(int meshIndex, out List<Vector3> normals, out List<float> distances)
	{
		normals		=new List<Vector3>();
		distances	=new List<float>();
		return;
	}


	public int GetPartColladaPolys(int meshIndex, out string polys, out string counts)
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


	public void GetPartColladaPositions(int meshIndex, out float []positions)
	{
		positions	=null;
		if(meshIndex < 0 || meshIndex >= mParts.Count)
		{
			return;
		}

		SharedMeshStuff.GetPartColladaPositions(mParts[meshIndex], out positions);
	}


	public void GetPartColladaNormals(int meshIndex, out float []normals)
	{
		normals	=null;
		if(meshIndex < 0 || meshIndex >= mParts.Count)
		{
			return;
		}

		SharedMeshStuff.GetPartColladaNormals(mParts[meshIndex], out normals);
	}


	public Matrix4x4 GetPartTransform(int meshIndex)
	{
		return	Matrix4x4.Identity;
	}
}
