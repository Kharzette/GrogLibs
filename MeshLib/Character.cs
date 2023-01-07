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
public partial class Character
{
	//these should always have the same Count
	List<Mesh>			mParts		=new List<Mesh>();
	List<MeshMaterial>	mPartMats	=new List<MeshMaterial>();

	//unlike statics, there is no per part transform...
	//ColladaConvert should ensure all parts are origined properly

	//ref to anim lib
	AnimLib	mAnimLib;

	Matrix4x4	mTransform;
	MeshBound	mBound;

	//raw bone transforms for shader
	Matrix4x4	[]mBones;

	Skin	mSkin;

	//this must match the value in CommonFunctions.hlsli in the shader lib!
	const int	MAX_BONES	=55;


	public Character(List<Mesh> parts, Skin sk, AnimLib al)
	{
		mParts.AddRange(parts);

		//create material dummy values
		int	count	=0;
		foreach(Mesh m in parts)
		{
			MeshMaterial	mm	=new MeshMaterial();

			mm.mbVisible		=true;
			mm.mMaterialID		=count++;
			mm.mMaterialName	=m.Name + "Mat";

			mPartMats.Add(mm);
		}		

		mSkin		=sk;
		mAnimLib	=al;
		mTransform	=Matrix4x4.Identity;
	}


	//construct from file + a dictionary of possible part meshes
	public Character(string fileName, Dictionary<string, Mesh> meshes, AnimLib al)
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
		if(magic != 0xCA1EC7BE)
		{
			br.Close();
			file.Close();
			return;
		}

		mTransform	=FileUtil.ReadMatrix(br);

		mBound	=new MeshBound();
		mBound.Read(br);

		mSkin	=new Skin(1f);
		mSkin.Read(br);

		int	numParts	=br.ReadInt32();

		for(int i=0;i < numParts;i++)
		{
			string	name	=br.ReadString();

			MeshMaterial	mm	=new MeshMaterial(br);

			if(!meshes.ContainsKey(name))
			{
				continue;
			}

			mParts.Add(meshes[name]);
			mPartMats.Add(mm);
		}

		mAnimLib	=al;
	}


	//clear only instance data
	public void FreeAll()
	{
		mParts.Clear();
		mPartMats.Clear();

		mBones	=null;
	}


	public Skin GetSkin()
	{
		return	mSkin;
	}


	public void AddPart(Mesh part, string matName)
	{
		if(matName == null || matName == "")
		{
			matName	="default";
		}

		mParts.Add(part);

		MeshMaterial	mm	=new MeshMaterial();

		mm.mbVisible		=true;
		mm.mMaterialID		=0;
		mm.mMaterialName	=matName;

		mPartMats.Add(mm);
	}


	public int GetPartCount()
	{
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


	public void SetTransform(Matrix4x4 mat)
	{
		mTransform		=mat;
	}


	//I think in all cases where this is used the part meshes go too
	public void NukePart(int index)
	{
		if(index < 0 || index >= mParts.Count)
		{
			return;
		}

		Mesh			m	=mParts[index];
		MeshMaterial	mm	=mPartMats[index];

		m.FreeAll();

		mParts.RemoveAt(index);
		mPartMats.RemoveAt(index);
	}


	public void NukeParts(List<int> indexes)
	{
		List<Mesh>			toNuke		=new List<Mesh>();
		List<MeshMaterial>	toNukeMM	=new List<MeshMaterial>();
		foreach(int ind in indexes)
		{
			Debug.Assert(ind >= 0 && ind < mParts.Count);

			if(ind < 0 || ind >= mParts.Count)
			{
				continue;
			}

			toNuke.Add(mParts[ind]);
			toNukeMM.Add(mPartMats[ind]);
		}

		mParts.RemoveAll(mp => toNuke.Contains(mp));
		mPartMats.RemoveAll(mp => toNukeMM.Contains(mp));

		foreach(Mesh m in toNuke)
		{
			m.FreeAll();
		}

		toNuke.Clear();
	}


	public void SetPartMaterialName(int index, string matName,
									StuffKeeper sk)
	{
		if(index < 0 || index >= mParts.Count)
		{
			return;
		}
		mPartMats[index].mMaterialName	=matName;
	}


	public string GetPartMaterialName(int index)
	{
		if(index < 0 || index >= mParts.Count)
		{
			return	"";
		}
		return	mPartMats[index].mMaterialName;
	}


	public void SetPartVisible(int index, bool bVisible)
	{
		if(index < 0 || index >= mParts.Count)
		{
			return;
		}
		mPartMats[index].mbVisible	=bVisible;
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

		UpdateBones(mAnimLib.GetSkeleton(), mSkin);
	}


	public void Animate(string anim, float time)
	{
		mAnimLib.Animate(anim, time);

		UpdateBones(mAnimLib.GetSkeleton(), mSkin);
	}


	public bool RayIntersectBones(Vector3 startPos, Vector3 endPos, float rayRadius,
									out int boneHit, out Vector3 hitPos, out Vector3 hitNorm)
	{
		Skeleton	skel	=mAnimLib.GetSkeleton();
		Ray			ray		=new Ray(startPos, Vector3.Normalize(endPos - startPos));

		Matrix4x4	boneToWorld	=Matrix4x4.Identity;

		float	bestDist	=float.MaxValue;
		int		bestBone	=-1;
		Vector3	bestHit		=Vector3.Zero;
		Vector3	bestNorm	=Vector3.UnitZ;
		for(int i=0;i < mBones.Length;i++)
		{
			int	choice	=mSkin.GetBoundChoice(i);

			if(choice == Skin.Capsule)
			{
				BoundingCapsule	?bc	=mSkin.GetBoneBoundCapsule(i, false);
				if(bc == null)
				{
					continue;
				}

				//really seems like this should work with the shader bones already here
				boneToWorld	=mSkin.GetBoneByIndexNoBind(i, skel);
				boneToWorld	*=mTransform;

				Vector3	jointPos	=Mathery.TransformCoordinate(Vector3.Zero, ref boneToWorld);
				
				Vector3	impact1, impact2;
				Vector3	norm1, norm2;
				if(bc.Value.IntersectRay(jointPos, boneToWorld.Forward(), ray, rayRadius,
					out impact1, out impact2, out norm1, out norm2))
				{
					float	dist	=Vector3.Distance(startPos, impact1);
					if(dist < bestDist)
					{
						bestBone	=i;
						bestDist	=dist;
						bestHit		=impact1;
						bestNorm	=norm1;
					}
					dist	=Vector3.Distance(startPos, impact2);
					if(dist < bestDist)
					{
						bestBone	=i;
						bestDist	=dist;
						bestHit		=impact2;
						bestNorm	=norm2;
					}
				}
			}
			else if(choice == Skin.Sphere)
			{
				BoundingSphere	?bs	=mSkin.GetBoneBoundSphere(i, false);
				if(bs == null)
				{
					continue;
				}

				boneToWorld	=mSkin.GetBoneByIndexNoBind(i, skel);
				boneToWorld	*=mTransform;

				//spheres can move along the bone's Z axis
				Vector3	jointPos	=Mathery.TransformCoordinate(bs.Value.Center, ref boneToWorld);

				BoundingSphere	tbs	=new BoundingSphere(jointPos, bs.Value.Radius);

				float	?dist;
				dist	=tbs.Intersects(ray);
				if(dist == null)
				{
					continue;
				}

				if(dist < bestDist)
				{
					bestBone	=i;
					bestDist	=dist.Value;
					bestHit		=ray.Position + ray.Direction * dist.Value;
					bestNorm	=Vector3.Normalize(bestHit - tbs.Center);
				}
			}
			else if(choice == Skin.Box)
			{
				BoundingBox	?bb	=mSkin.GetBoneBoundBox(i, false);
				if(bb == null)
				{
					continue;
				}

				boneToWorld	=mSkin.GetBoneByIndexNoBind(i, skel);
				boneToWorld	*=mTransform;

				//costly!
				Matrix4x4	btwInvert;
				if(!Matrix4x4.Invert(boneToWorld, out btwInvert))
				{
					continue;
				}

				//try moving the ray into bone space
				Vector3	rayInvDir	=Vector3.TransformNormal(ray.Direction, btwInvert);
				Vector3	rayInvStart	=Mathery.TransformCoordinate(ray.Position, ref btwInvert);

				Ray	boneRay	=new Ray(rayInvStart, rayInvDir);

				float	?dist;
				dist	=bb.Value.Intersects(boneRay);
				if(dist == null)
				{
					continue;
				}

				if(dist < bestDist)
				{
					Vector3	boneSpaceHit	=rayInvStart + rayInvDir * dist.Value;

					bestNorm	=Mathery.BoxNormalAtPoint(bb.Value, boneSpaceHit);

					bestBone	=i;
					bestDist	=dist.Value;
					bestHit		=ray.Position + ray.Direction * dist.Value;
					bestNorm	=Vector3.TransformNormal(bestNorm, boneToWorld);
				}
			}
		}

		boneHit	=bestBone;
		hitPos	=bestHit;
		hitNorm	=bestNorm;

		return	(bestBone != -1);
	}


	public void Draw(MatLib mlib)
	{
		Debug.Assert(mPartMats.Count == mParts.Count);

		UpdateShaderBones(mlib.GetDC(), mlib.GetCBKeeper());

		for(int i=0;i < mParts.Count;i++)
		{
			MeshMaterial	mm	=mPartMats[i];

			if(!mm.mbVisible)
			{
				continue;
			}

			Mesh	m	=mParts[i];

			m.Draw(mlib, mTransform, mm);
		}
	}


	public void Draw(MatLib mlib, string altMaterial)
	{
		Debug.Assert(mPartMats.Count == mParts.Count);

		UpdateShaderBones(mlib.GetDC(), mlib.GetCBKeeper());

		for(int i=0;i < mParts.Count;i++)
		{
			MeshMaterial	mm	=mPartMats[i];

			if(!mm.mbVisible)
			{
				continue;
			}

			Mesh	m	=mParts[i];

			m.Draw(mlib, mTransform, mm, altMaterial);
		}
	}


	public void DrawDMN(MatLib mlib)
	{
		Debug.Assert(mPartMats.Count == mParts.Count);

		UpdateShaderBones(mlib.GetDC(), mlib.GetCBKeeper());

		for(int i=0;i < mParts.Count;i++)
		{
			MeshMaterial	mm	=mPartMats[i];

			if(!mm.mbVisible)
			{
				continue;
			}

			Mesh	m	=mParts[i];

			m.DrawDMN(mlib, mTransform, mm);
		}
	}


	//this if for the DMN renderererererer
	public void AssignMaterialIDs(MaterialLib.IDKeeper idk)
	{
		foreach(MeshMaterial mm in mPartMats)
		{
			mm.mMaterialID	=idk.GetID(mm.mMaterialName);
		}
	}


	public Vector3 GetForwardVector()
	{
		Vector3	ret	=Vector3.UnitZ;

		return	Vector3.TransformNormal(ret, mTransform);
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

		FileStream		file	=new FileStream(fileName, FileMode.Create, FileAccess.Write);
		BinaryWriter	bw		=new BinaryWriter(file);

		//write a magic number identifying character instances
		UInt32	magic	=0xCA1EC7BE;

		bw.Write(magic);

		FileUtil.WriteMatrix(bw, mTransform);

		mBound.Write(bw);

		//save skin
		mSkin.Write(bw);

		bw.Write(mParts.Count);
		for(int i=0;i < mParts.Count;i++)
		{
			bw.Write(mParts[i].Name);
			mPartMats[i].Write(bw);
		}

		bw.Close();
		file.Close();
	}
}