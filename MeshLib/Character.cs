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
	BoundingSphere	mSphereBound;

	//transform
	Matrix4x4	mTransform;

	//raw bone transforms for shader
	Matrix4x4	[]mBones;

	//this must match the value in CommonFunctions.hlsli in the shader lib!
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
	}


	public BoundingBox GetBoxBound()
	{
		return	mBoxBound;
	}


	public BoundingSphere GetSphereBound()
	{
		return	mSphereBound;
	}


	public void GetBoneNamesInUseByDraw(List<string> names)
	{
		mParts.GetBoneNamesInUseByDraw(names, mAnimLib.GetSkeleton());
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

		//mParts.ComputeBoneBounds(null, mAnimLib.GetSkeleton());

		Vector3	[]corners	=new Vector3[8];

		Skin	sk	=mParts.GetSkin();

		Vector3	max	=Vector3.One * -float.MaxValue;
		Vector3	min	=Vector3.One * float.MaxValue;

		Vector3	center	=Vector3.Zero;

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

			Vector3	boxCenter	=Vector3.Zero;
			for(int j=0;j < 8;j++)
			{
				Vector3	transd	=Vector3.Transform(corners[j], mBones[i]);

				Mathery.AddPointToBoundingBox(ref min, ref max, transd);

				boxCenter	+=transd;
			}

			center	+=boxCenter / 8;
		}

		center	/=mBones.Length;

		mBoxBound.Min	=min;
		mBoxBound.Max	=max;

		float	distMin	=Vector3.Distance(min, center);
		float	distMax	=Vector3.Distance(max, center);

		mSphereBound.Center	=center;
		mSphereBound.Radius	=(distMin > distMax)? distMin : distMax;
	}


	public void Draw(MatLib mlib)
	{
		UpdateShaderBones(mlib.GetDC(), mlib.GetCBKeeper());

		mParts.Draw(mlib, mTransform);
	}


	public void Draw(MatLib mlib, string altMaterial)
	{
		UpdateShaderBones(mlib.GetDC(), mlib.GetCBKeeper());

		mParts.Draw(mlib, mTransform, altMaterial);
	}


	public void DrawDMN(MatLib mlib)
	{
		UpdateShaderBones(mlib.GetDC(), mlib.GetCBKeeper());

		mParts.DrawDMN(mlib, mTransform);
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