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

public class CharacterArch : IArch
{
	//parts
	List<Mesh>	mMeshParts	=new List<Mesh>();

	//skin info
	Skin	mSkin;


	void IArch.FreeAll()
	{
		foreach(Mesh m in mMeshParts)
		{
			m.FreeAll();
		}
	}


	void IArch.SetSkin(Skin s, Skeleton sk)
	{
		mSkin	=s;

		ComputeBoneBounds(sk);
	}


	Skin IArch.GetSkin()
	{
		return	mSkin;
	}


	bool IArch.RenamePart(int index, string newName)
	{
		if(index < 0 || index >= mMeshParts.Count)
		{
			return	false;
		}

		mMeshParts[index].Name	=newName;

		return	true;
	}


	void IArch.AddPart(Mesh m)
	{
		if(m != null)
		{
			mMeshParts.Add(m);
		}
	}


	int IArch.GetPartCount()
	{
		return	mMeshParts.Count;
	}


	void IArch.GenTangents(ID3D11Device gd,
		List<int> parts, int texCoordSet)
	{
		List<Mesh>	toChange	=new List<Mesh>();
		foreach(int ind in parts)
		{
			Debug.Assert(ind >= 0 && ind < mMeshParts.Count);

			if(ind < 0 || ind >= mMeshParts.Count)
			{
				continue;
			}

			toChange.Add(mMeshParts[ind]);
		}

		foreach(EditorMesh em in toChange)
		{
			if(em == null)
			{
				continue;
			}
			em.GenTangents(gd, texCoordSet);
		}
	}


	void IArch.NukeVertexElements(ID3D11Device gd,
		List<int> indexes,
		List<int> vertElementIndexes)
	{
		List<Mesh>	toChange	=new List<Mesh>();
		foreach(int ind in indexes)
		{
			Debug.Assert(ind >= 0 && ind < mMeshParts.Count);

			if(ind < 0 || ind >= mMeshParts.Count)
			{
				continue;
			}

			toChange.Add(mMeshParts[ind]);
		}

		foreach(EditorMesh em in toChange)
		{
			if(em == null)
			{
				continue;
			}
			em.NukeVertexElement(vertElementIndexes, gd);
		}
	}


	void IArch.NukePart(int index)
	{
		if(index < 0 || index >= mMeshParts.Count)
		{
			return;
		}

		mMeshParts.RemoveAt(index);
	}


	void IArch.NukeParts(List<int> indexes)
	{
		List<Mesh>	toNuke	=new List<Mesh>();
		foreach(int ind in indexes)
		{
			Debug.Assert(ind >= 0 && ind < mMeshParts.Count);

			if(ind < 0 || ind >= mMeshParts.Count)
			{
				continue;
			}

			toNuke.Add(mMeshParts[ind]);
		}

		mMeshParts.RemoveAll(mp => toNuke.Contains(mp));

		toNuke.Clear();
	}


	Type IArch.GetPartVertexType(int index)
	{
		if(index < 0 || index >= mMeshParts.Count)
		{
			return	null;
		}
		return	mMeshParts[index].VertexType;
	}


	string IArch.GetPartName(int index)
	{
		if(index < 0 || index >= mMeshParts.Count)
		{
			return	"";
		}
		return	mMeshParts[index].Name;
	}


	void IArch.ReIndexVertWeights(ID3D11Device gd, Dictionary<int, int> idxMap)
	{
		foreach(Mesh m in mMeshParts)
		{
			EditorMesh	em	=m as EditorMesh;
			if(em == null)
			{
				continue;
			}

			em.ReIndexVertWeights(gd, idxMap);
		}
	}


	void IArch.GetPartBoneNamesInUseByDraw(int index, List<string> names, Skeleton skel)
	{
		if(index < 0 || index >= mMeshParts.Count)
		{
			return;
		}

		if(skel == null)
		{
			Debug.WriteLine("No skeleton in GetPartBoneNamesInUseByDraw()");
			return;
		}

		Mesh	m	=mMeshParts[index];

		EditorMesh	em	=m as EditorMesh;
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


	void IArch.Draw(MatLib mlib, Matrix4x4 transform, List<MeshMaterial> meshMats)
	{
		Debug.Assert(meshMats.Count == mMeshParts.Count);

		for(int i=0;i < mMeshParts.Count;i++)
		{
			MeshMaterial	mm	=meshMats[i];

			if(!mm.mbVisible)
			{
				continue;
			}

			Mesh	m	=mMeshParts[i];

			m.Draw(mlib, transform, mm);
		}
	}


	void IArch.Draw(MatLib mlib, Matrix4x4 transform,
		List<MeshMaterial> meshMats, string altMaterial)
	{
		Debug.Assert(meshMats.Count == mMeshParts.Count);

		for(int i=0;i < mMeshParts.Count;i++)
		{
			MeshMaterial	mm	=meshMats[i];

			if(!mm.mbVisible)
			{
				continue;
			}

			Mesh	m	=mMeshParts[i];

			m.Draw(mlib, transform, mm, altMaterial);
		}
	}


	void IArch.DrawX(MatLib mlib, Matrix4x4 transform,
		List<MeshMaterial> meshMats, int numInst, string altMaterial)
	{
		Debug.Assert(meshMats.Count == mMeshParts.Count);

		for(int i=0;i < mMeshParts.Count;i++)
		{
			MeshMaterial	mm	=meshMats[i];

			if(!mm.mbVisible)
			{
				continue;
			}

			Mesh	m	=mMeshParts[i];

			m.DrawX(mlib, transform, mm, numInst, altMaterial);
		}
	}


	void IArch.DrawDMN(MatLib mlib, Matrix4x4 transform, List<MeshMaterial> meshMats)
	{
		Debug.Assert(meshMats.Count == mMeshParts.Count);

		for(int i=0;i < mMeshParts.Count;i++)
		{
			MeshMaterial	mm	=meshMats[i];

			if(!mm.mbVisible)
			{
				continue;
			}

			Mesh	m	=mMeshParts[i];

			m.DrawDMN(mlib, transform, mm);
		}
	}


	float? IArch.RayIntersect(Vector3 start, Vector3 end, bool bBox, out Mesh partHit)
	{
		partHit	=null;
		return	null;	//characters don't use this
	}


	void IArch.UpdateBounds()
	{
		//this is kind of meaningless for character archetypes
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


	void ComputeBoneBounds(Skeleton skel)
	{
		int	numBones	=skel.GetNumIndexedBones();

		for(int i=0;i < numBones;i++)
		{
			List<Vector3>	boxPoints	=new List<Vector3>();

			foreach(EditorMesh em in mMeshParts)
			{
				if(em == null)
				{
					return;
				}

				BoundingBox		box;
				BoundingSphere	sphere;

				em.GetInfluencedBound(i, out box, out sphere);

				if(box.Min != Vector3.Zero
					&& box.Max != Vector3.Zero)
				{
					boxPoints.Add(box.Min);
					boxPoints.Add(box.Max);
				}
			}

			if(boxPoints.Count > 0)
			{
				mSkin.SetBoneBounds(i, BoundingBox.CreateFromPoints(boxPoints.ToArray()));
			}
		}
	}


	BoundingBox IArch.GetBoxBound()
	{
		List<Vector3>	pnts	=new List<Vector3>();
		foreach(Mesh m in mMeshParts)
		{
			BoundingBox	b	=m.GetBoxBounds();

			//internal part transforms
			Vector3	transMin;
			Vector3	transMax;

			Mathery.TransformCoordinate(b.Min, m.GetTransform(), out transMin);
			Mathery.TransformCoordinate(b.Max, m.GetTransform(), out transMax);

			pnts.Add(transMin);
			pnts.Add(transMax);
		}

		return	BoundingBox.CreateFromPoints(pnts.ToArray());
	}


	BoundingSphere IArch.GetSphereBound()
	{
		//not sure what I want this to do yet
		BoundingSphere	merged	=new BoundingSphere(Vector3.Zero, 0f);

		return	merged;
	}


	//find borders of shared verts between mesh parts
	List<EditorMesh.WeightSeam> IArch.Frankenstein()
	{
		List<EditorMesh.WeightSeam>	seams	=new List<EditorMesh.WeightSeam>();

		//make a "compared against" dictionary to prevent
		//needless work
		Dictionary<Mesh, List<Mesh>>	comparedAgainst	=new Dictionary<Mesh, List<Mesh>>();
		foreach(Mesh m in mMeshParts)
		{
			comparedAgainst.Add(m, new List<Mesh>());
		}

		for(int i=0;i < mMeshParts.Count;i++)
		{
			EditorMesh	meshA	=mMeshParts[i] as EditorMesh;
			if(meshA == null)
			{
				continue;
			}

			for(int j=0;j < mMeshParts.Count;j++)
			{
				if(i == j)
				{
					continue;
				}

				EditorMesh	meshB	=mMeshParts[j] as EditorMesh;
				if(meshB == null)
				{
					continue;
				}

				if(comparedAgainst[meshA].Contains(meshB)
					|| comparedAgainst[meshB].Contains(meshA))
				{
					continue;
				}

				EditorMesh.WeightSeam	seam	=meshA.FindSeam(meshB);

				comparedAgainst[meshA].Add(meshB);

				if(seam.mSeam.Count == 0)
				{
					continue;
				}

				Debug.WriteLine("Seam between " + meshA.Name + ", and "
					+ meshB.Name + " :Verts: " + seam.mSeam.Count);

				seams.Add(seam);
			}
		}
		return	seams;
	}


	void IArch.GetPartPlanes(int meshIndex, out List<Vector3> normals, out List<float> distances)
	{
		normals		=new List<Vector3>();
		distances	=new List<float>();
		return;
	}


	int IArch.GetPartColladaPolys(int meshIndex, out string polys, out string counts)
	{
		polys	=null;
		counts	=null;
		if(meshIndex < 0 || meshIndex >= mMeshParts.Count)
		{
			return	0;
		}

		Mesh	m	=mMeshParts[meshIndex];
		if(!(m is EditorMesh))
		{
			return	0;
		}

		EditorMesh	em	=m as EditorMesh;

		return	em.GetColladaPolys(out polys, out counts);
	}


	void IArch.GetPartPositions(int meshIndex, out List<Vector3> positions, out List<int> indexes)
	{
		positions	=null;
		indexes		=null;
		if(meshIndex < 0 || meshIndex >= mMeshParts.Count)
		{
			return;
		}

		Mesh	m	=mMeshParts[meshIndex];
		if(!(m is EditorMesh))
		{
			return;
		}

		EditorMesh	em	=m as EditorMesh;

		em.GetPositions(out positions, out indexes);
	}


	void IArch.GetPartColladaPositions(int meshIndex, out float []positions)
	{
		positions	=null;
		if(meshIndex < 0 || meshIndex >= mMeshParts.Count)
		{
			return;
		}

		Mesh	m	=mMeshParts[meshIndex];
		if(!(m is EditorMesh))
		{
			return;
		}

		EditorMesh	em	=m as EditorMesh;

		em.GetColladaPositions(out positions);
	}


	void IArch.GetPartColladaNormals(int meshIndex, out float []normals)
	{
		normals	=null;
		if(meshIndex < 0 || meshIndex >= mMeshParts.Count)
		{
			return;
		}

		Mesh	m	=mMeshParts[meshIndex];
		if(!(m is EditorMesh))
		{
			return;
		}

		EditorMesh	em	=m as EditorMesh;

		em.GetColladaNormals(out normals);
	}


	Matrix4x4 IArch.GetPartTransform(int meshIndex)
	{
		if(meshIndex < 0 || meshIndex >= mMeshParts.Count)
		{
			return	Matrix4x4.Identity;
		}
		return	mMeshParts[meshIndex].GetTransform();
	}


	void IArch.SaveToFile(string fileName)
	{
		FileStream		file	=new FileStream(fileName, FileMode.Create, FileAccess.Write);
		BinaryWriter	bw		=new BinaryWriter(file);

		//write a magic number identifying characters
		UInt32	magic	=0xCA1EABC8;

		bw.Write(magic);

		//save mesh parts
		bw.Write(mMeshParts.Count);
		foreach(Mesh m in mMeshParts)
		{
			m.Write(bw);
		}

		//save skin
		mSkin.Write(bw);

		bw.Close();
		file.Close();
	}


	//set bEditor if you want the buffers set to readable
	//so they can be resaved if need be
	bool IArch.ReadFromFile(string fileName, ID3D11Device gd, bool bEditor)
	{
		Stream	file	=new FileStream(fileName, FileMode.Open, FileAccess.Read);
		if(file == null)
		{
			return	false;
		}
		BinaryReader	br	=new BinaryReader(file);

		//clear existing data
		mMeshParts.Clear();

		//read magic number
		UInt32	magic	=br.ReadUInt32();

		if(magic != 0xCA1EABC8)
		{
			br.Close();
			file.Close();
			return	false;
		}

		int	numMesh	=br.ReadInt32();

		for(int i=0;i < numMesh;i++)
		{
			Mesh	m;

			if(bEditor)
			{
				m	=new EditorMesh("temp");
			}
			else
			{
				m	=new Mesh();
			}

			m.Read(br, gd, bEditor);
			mMeshParts.Add(m);
		}

		mSkin	=new Skin();
		mSkin.Read(br);

		br.Close();
		file.Close();

		return	true;
	}
}
