using System;
using System.IO;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using Vortice.Direct3D11;
using Vortice.Mathematics;
using UtilityLib;
using MaterialLib;

using MatLib	=MaterialLib.MaterialLib;


namespace MeshLib;

//an instance of a character
public class Character
{
	MeshPartStuff	mParts;

	//refs to anim lib
	AnimLib	mAnimLib;

	//bounds
	BoundingBox		mBoxBound;

	//transform
	Matrix4x4	mTransform;
	Matrix4x4	mTransInverted;

	//raw bone transforms for shader
	Matrix4x4	[]mBones;

	//this must match the value in Character.fx in the shader lib!
	const int	MAX_BONES	=55;


	public Character(IArch ca, AnimLib al)
	{
		mParts		=new MeshPartStuff(ca);
		mAnimLib	=al;
		mTransform	=Matrix4x4.Identity;
	}


	public void FreeAll()
	{
		mParts.FreeAll();

		mBones	=null;
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


	public void SetTransform(Matrix4x4 mat)
	{
		mTransform		=mat;
		Matrix4x4.Invert(mat, out mTransInverted);

		//set in the materials
		mParts.SetMatObjTransforms(mat);
	}


	public BoundingBox GetBoxBound()
	{
		return	mBoxBound;
	}


	public BoundingSphere GetSphereBound()
	{
		BoundingSphere	ret	=mParts.GetSphereBound();

		return	Mathery.TransformSphere(mTransform, ret);
	}


	public void GetBoneNamesInUseByDraw(List<string> names)
	{
		mParts.GetBoneNamesInUseByDraw(names, mAnimLib.GetSkeleton());
	}


	//these index the same as the mesh part list in the archetype
	public void AddPart(MatLib mats)
	{
		mParts.AddPart(mats, mTransform);
	}


	public void SetMatLib(MatLib mats, StuffKeeper sk)
	{
		mParts.SetMatLibs(mats, sk);
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


	public void SetPartVisible(int index, bool bVisible)
	{
		mParts.SetPartVisible(index, bVisible);
	}


	public void SetTriLightValues(
		Vector4 col0, Vector4 col1, Vector4 col2, Vector3 lightDir)
	{
		mParts.SetTriLightValues(col0, col1, col2, lightDir);
	}


	//this can be used to rebuild the bones if the skeleton changed
	public void ReBuildBones(ID3D11Device gd)
	{
		//clear
		mBones	=null;

		Skeleton	sk	=mAnimLib.GetSkeleton();

		Dictionary<int, int>	reMap	=new Dictionary<int, int>();
		sk.Compact(reMap);

		mParts.ReIndexVertWeights(gd, reMap);
	}


	//copies bones into the shader
	//materials should be set up to ignore
	//the mBones parameter
	void UpdateShaderBones(ID3D11DeviceContext dc, CBKeeper cbk)
	{
		if(mBones != null)
		{
			if(mBones.Length <= MAX_BONES)
			{
				cbk.SetBonesWithTranspose(mBones);
				cbk.UpdateCharacter(dc);
				cbk.SetCharacterToShaders(dc);
			}
			else
			{
				//Too many bones will stomp gpu memory!
				Debug.Assert(false);
			}
		}
	}


	void UpdateBones(Skeleton sk, Skin skn)
	{
		//no need for this if not skinned
		if(skn == null || sk == null)
		{
			return;
		}

		if(mBones == null)
		{
			mBones	=new Matrix4x4[sk.GetNumIndexedBones()];
		}
		for(int i=0;i < mBones.Length;i++)
		{
			mBones[i]	=skn.GetBoneByIndex(i, sk);
		}

		//should be cheap to do this whenever bones change
		UpdateBounds();
	}


	public void Update(float secDelta)
	{
	}


	public void Blend(string anim1, float anim1Time,
		string anim2, float anim2Time,
		float percentage)
	{
		mAnimLib.Blend(anim1, anim1Time, anim2, anim2Time, percentage);

		UpdateBones(mAnimLib.GetSkeleton(), mParts.GetSkin());
	}


	public void Animate(string anim, float time)
	{
		mAnimLib.Animate(anim, time);

		UpdateBones(mAnimLib.GetSkeleton(), mParts.GetSkin());
	}


	public void UpdateBounds()
	{
		if(mBones == null)
		{
			return;
		}

		Vector3	[]corners	=new Vector3[8];

		Skin	sk	=mParts.GetSkin();

		//clear bounds
		mBoxBound.Max	=Vector3.One * -float.MaxValue;
		mBoxBound.Min	=Vector3.One * float.MaxValue;

		for(int i=0;i < mBones.Length;i++)
		{
			BoundingBox	box	=sk.GetBoneBoundBox(i);
			
			Vector3	size	=box.Max - box.Min;
			float	vol		=size.X + size.Y + size.Z;

			//skip bones without much influence
			if(vol < 1f)
			{
				continue;
			}

			box.GetCorners(corners);

			for(int j=0;j < 8;j++)
			{
				Vector3	transd	=Vector3.Transform(corners[j], mBones[i]);

				Mathery.AddPointToBoundingBox(ref mBoxBound, transd);
			}
		}
	}


	public void Draw(ID3D11DeviceContext dc, CBKeeper cbk)
	{
		UpdateShaderBones(dc, cbk);

		mParts.Draw(dc);
	}


	public void Draw(ID3D11DeviceContext dc, CBKeeper cbk, string altMaterial)
	{
		UpdateShaderBones(dc, cbk);

		mParts.Draw(dc, altMaterial);
	}


	public void DrawDMN(ID3D11DeviceContext dc, CBKeeper cbk)
	{
		UpdateShaderBones(dc, cbk);

		mParts.DrawDMN(dc);
	}


	//this if for the DMN renderererererer
	public void AssignMaterialIDs(MaterialLib.IDKeeper idk)
	{
		mParts.AssignMaterialIDs(idk);
	}


	public Vector3 GetForwardVector()
	{
		Vector3	ret	=Vector3.UnitZ;

		return	Vector3.TransformNormal(ret, mTransform);
	}


	public void SaveToFile(string fileName)
	{
		FileStream		file	=new FileStream(fileName, FileMode.Create, FileAccess.Write);
		BinaryWriter	bw		=new BinaryWriter(file);

		//write a magic number identifying character instances
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