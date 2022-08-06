using System;
using System.IO;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using Vortice.Mathematics;
using Vortice.Direct3D11;
using MaterialLib;

using MatLib	=MaterialLib.MaterialLib;

namespace MeshLib;

//handles collections of meshes
//used by characters and static meshes
internal class MeshPartStuff
{
	IArch	mArch;

	//materials per part
	List<MeshMaterial>	mPartMats	=new List<MeshMaterial>();


	internal MeshPartStuff(IArch arch)
	{
		mArch	=arch;

		for(int i=0;i < mArch.GetPartCount();i++)
		{
			AddPart();
		}
	}


	internal void FreeAll()
	{
		mPartMats.Clear();

		//arch is likely being used elsewhere
		//don't free it here
		mArch	=null;
	}


	internal bool IsEmpty()
	{
		return	(mPartMats.Count <= 0);
	}


	internal Skin GetSkin()
	{
		return	mArch.GetSkin();
	}


	internal IArch GetArch()
	{
		return	mArch;
	}


	internal int GetNumParts()
	{
		return	mPartMats.Count;
	}


	internal void SetPartVisible(int index, bool bVisible)
	{
		Debug.Assert(index >= 0 && index < mPartMats.Count);

		if(index < 0 || index >= mPartMats.Count)
		{
			return;
		}

		mPartMats[index].mbVisible	=bVisible;
	}


	internal void SetPartMaterialName(int index, string matName, StuffKeeper sk)
	{
		Debug.Assert(index >= 0 && index < mPartMats.Count);

		if(index < 0 || index >= mPartMats.Count)
		{
			return;
		}

		MeshMaterial	mm	=mPartMats[index];

		mm.mMaterialName	=matName;
	}


	internal string GetPartMaterialName(int index)
	{
		Debug.Assert(index >= 0 && index < mPartMats.Count);

		if(index < 0 || index >= mPartMats.Count)
		{
			return	"Nothing";
		}

		return	mPartMats[index].mMaterialName;
	}


	//these need to be kept in sync with the arch's mesh parts
	void AddPart()
	{
		MeshMaterial	mm	=new MeshMaterial();

		mm.mMaterialName	="NoMaterial";
		mm.mbVisible		=true;

		mPartMats.Add(mm);
	}


	internal void NukePart(int index)
	{
		Debug.Assert(index >= 0 && index < mPartMats.Count);

		if(index < 0 || index >= mPartMats.Count)
		{
			return;
		}

		mPartMats.RemoveAt(index);
	}


	internal void NukeParts(List<int> indexes)
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
	}


	internal void ReIndexVertWeights(ID3D11Device gd, Dictionary<int, int> idxMap)
	{
		mArch.ReIndexVertWeights(gd, idxMap);
	}


	internal void GetBoneNamesInUseByDraw(List<string> names, Skeleton skel)
	{
		int	partCount	=mArch.GetPartCount();
		for(int i=0;i < partCount;i++)
		{
			mArch.GetPartBoneNamesInUseByDraw(i, names, skel);
		}
	}


	internal void Draw(MatLib mlib, Matrix4x4 transform)
	{
		mArch.Draw(mlib, transform, mPartMats);
	}


	internal void Draw(MatLib mlib, Matrix4x4 transform, string altMaterial)
	{
		mArch.Draw(mlib, transform, mPartMats, altMaterial);
	}


	internal void DrawX(MatLib mlib, Matrix4x4 transform, string altMaterial, int numInst)
	{
		mArch.DrawX(mlib, transform, mPartMats, numInst, altMaterial);
	}


	internal void DrawDMN(MatLib mlib, Matrix4x4 transform)
	{
		mArch.DrawDMN(mlib, transform, mPartMats);
	}


	internal void Write(BinaryWriter bw)
	{
		bw.Write(mPartMats.Count);

		foreach(MeshMaterial mm in mPartMats)
		{
			mm.Write(bw);
		}
	}


	internal void Read(BinaryReader br)
	{
		mPartMats.Clear();

		int	cnt	=br.ReadInt32();

		for(int i=0;i < cnt;i++)
		{
			MeshMaterial	mm	=new MeshMaterial();

			mm.Read(br);

			mPartMats.Add(mm);
		}
	}


	internal void AssignMaterialIDs(MaterialLib.IDKeeper idk)
	{
		foreach(MeshMaterial mm in mPartMats)
		{
			mm.mMaterialID	=idk.GetID(mm.mMaterialName);
		}
	}


	internal void ComputeBoneBounds(List<string> skipMaterials, Skeleton skeleton)
	{
		CharacterArch	ca	=mArch as CharacterArch;
		if(ca == null)
		{
			return;
		}

		List<int>	skipParts	=new List<int>();
		for(int i=0;i < mPartMats.Count;i++)
		{
			if(!mPartMats[i].mbVisible)
			{
				skipParts.Add(i);
				continue;
			}

			if(skipMaterials != null && skipMaterials.Contains(mPartMats[i].mMaterialName))
			{
				skipParts.Add(i);
				continue;
			}
		}

		//ca.ComputeBoneBounds(skeleton, skipParts);
	}
}