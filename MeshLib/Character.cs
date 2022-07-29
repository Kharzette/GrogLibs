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


	public bool RayIntersectBones(Vector3 startPos, Vector3 endPos, float rayRadius,
									out int boneHit, out Vector3 hitPos, out Vector3 hitNorm)
	{
		Skin		sk		=mParts.GetSkin();
		Skeleton	skel	=mAnimLib.GetSkeleton();
		Ray			ray		=new Ray(startPos, Vector3.Normalize(endPos - startPos));

		Matrix4x4	boneToWorld	=Matrix4x4.Identity;

		float	bestDist	=float.MaxValue;
		int		bestBone	=-1;
		Vector3	bestHit		=Vector3.Zero;
		Vector3	bestNorm	=Vector3.UnitZ;
		for(int i=0;i < mBones.Length;i++)
		{
			int	choice	=sk.GetBoundChoice(i);

			if(choice == Skin.Capsule)
			{
				BoundingCapsule	?bc	=sk.GetBoneBoundCapsule(i, false);
				if(bc == null)
				{
					continue;
				}

				//really seems like this should work with the shader bones already here
				boneToWorld	=sk.GetBoneByIndexNoBind(i, skel);
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
				BoundingSphere	?bs	=sk.GetBoneBoundSphere(i, false);
				if(bs == null)
				{
					continue;
				}

				boneToWorld	=sk.GetBoneByIndexNoBind(i, skel);
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
				BoundingBox	?bb	=sk.GetBoneBoundBox(i, false);
				if(bb == null)
				{
					continue;
				}

				boneToWorld	=sk.GetBoneByIndexNoBind(i, skel);
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
			BoundingBox	?box	=sk.GetBoneBoundBox(i);
			if(box == null)
			{
				continue;
			}
			
			Vector3	size	=box.Value.Max - box.Value.Min;
			float	vol		=size.X + size.Y + size.Z;

			//skip bones without much influence?
			if(vol < 1f)	//TODO: this 1 will go wrong at meter scale
			{
				continue;
			}

			box?.GetCorners(corners);

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