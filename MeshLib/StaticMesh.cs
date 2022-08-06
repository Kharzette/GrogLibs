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
using MaterialLib;

using MatLib	=MaterialLib.MaterialLib;

namespace MeshLib;

public class StaticMesh
{
	MeshPartStuff	mParts;

	//transform
	Matrix4x4	mTransform;


	public StaticMesh(IArch statA)
	{
		mParts	=new MeshPartStuff(statA);

		SetTransform(Matrix4x4.Identity);
	}


	public void FreeAll()
	{
		mParts.FreeAll();

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


	//Checks first against a box encompassing all parts
	//TODO: cache that matrix invert somehow?
	//TODO: what about reporting ALL collisions?
	public bool RayIntersect(Vector3 startPos, Vector3 endPos, float rayRadius,
							out Vector3 hitPos, out Vector3 hitNorm)
	{
		hitPos	=hitNorm	=Vector3.Zero;

		IArch	arch	=mParts.GetArch();

		//need this for boxes only
		Matrix4x4	tInv;

		//check rough bounds
		BoundChoice	bc	=arch.GetRoughBoundChoice();

		Vector3	rayDir	=Vector3.Normalize(endPos - startPos);

		if(bc == BoundChoice.Invalid)
		{
			return	false;
		}
		else if(bc == BoundChoice.Box)
		{
			BoundingBox	box	=arch.GetRoughBoxBound();

			if(!Matrix4x4.Invert(mTransform, out tInv))
			{
				return	false;
			}

			Vector3	rayInvDir	=Vector3.TransformNormal(rayDir, tInv);
			Vector3	rayInvStart	=Mathery.TransformCoordinate(startPos, ref tInv);

			Ray	ray	=new Ray(rayInvStart, rayInvDir);

			float	?dist	=box.Intersects(ray);
			if(dist == null)
			{
				return	false;
			}
		}
		else
		{
			//sphere
			BoundingSphere	bs	=arch.GetRoughSphereBound();

			Ray	ray	=new Ray(startPos, rayDir);

			float	?dist	=bs.Intersects(ray);
			if(dist == null)
			{
				return	false;
			}
		}

		//grab closest intersection
		float	bestDist	=float.MaxValue;
		int		bestPart	=-1;
		Vector3	bestHit		=Vector3.Zero;
		Vector3	bestNorm	=Vector3.Zero;

		//check submesh bounds or bone bounds
		int	partCount	=mParts.GetNumParts();

		for(int i=0;i < partCount;i++)
		{
			BoundChoice?	nbc	=arch.GetPartBoundChoice(i);

			if(nbc == null)
			{
				continue;
			}

			if(nbc.Value == BoundChoice.Invalid)
			{
				continue;
			}

			//TODO: hmm there seems to be no mechanism for instances
			//to have their own submesh transforms...
			//this might be a problem for like machines with moving parts
			Matrix4x4	partXForm	=arch.GetPartTransform(i);

			if(nbc.Value == BoundChoice.Box)
			{
				BoundingBox?	pbox	=arch.GetPartBoxBound(i);
				if(pbox == null)
				{
					continue;
				}

				partXForm	*=mTransform;
				if(!Matrix4x4.Invert(partXForm, out tInv))
				{
					return	false;
				}
				Vector3	rayInvDir	=Vector3.TransformNormal(rayDir, tInv);
				Vector3	rayInvStart	=Mathery.TransformCoordinate(startPos, ref tInv);

				Ray	ray	=new Ray(rayInvStart, rayInvDir);

				float	?dist	=pbox.Value.Intersects(ray);
				if(dist == null)
				{
					continue;
				}

				if(dist < bestDist)
				{
					bestPart	=i;
					bestDist	=dist.Value;
					bestHit		=rayInvStart + rayInvDir * dist.Value;	//boxspace
					bestNorm	=Mathery.BoxNormalAtPoint(pbox.Value, bestHit);
					bestHit		=startPos + rayDir * dist.Value;	//worldspace
					bestNorm	=Vector3.TransformNormal(bestNorm, mTransform);
				}
			}
			else
			{
				//sphere
				BoundingSphere?	ps	=arch.GetPartSphereBound(i);
				if(ps == null)
				{
					continue;
				}

				Ray	ray	=new Ray(startPos, rayDir);

				float	?dist	=ps.Value.Intersects(ray);
				if(dist == null)
				{
					continue;
				}

				if(dist < bestDist)
				{
					bestPart	=i;
					bestDist	=dist.Value;
					bestHit		=startPos + rayDir * dist.Value;
					bestNorm	=Vector3.Normalize(bestHit - ps.Value.Center);	//might be backwards?
				}
			}
		}

		if(bestPart == -1)
		{
			//no submesh collisions, though it passed the rough check
			return	false;
		}

		hitPos	=bestHit;
		hitNorm	=bestNorm;

		return	true;
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


	public int GetNumParts()
	{
		return	mParts.GetNumParts();
	}


	public void SetPartVisible(int index, bool bVisible)
	{
		mParts.SetPartVisible(index, bVisible);
	}


	public void Draw(MatLib mlib)
	{
		mParts.Draw(mlib, mTransform);
	}


	public void Draw(MatLib mlib, string altMaterial)
	{
		mParts.Draw(mlib, mTransform, altMaterial);
	}


	public void DrawX(MatLib mlib, int numInst, string altMaterial)
	{
		mParts.DrawX(mlib, mTransform, altMaterial, numInst);
	}


	public void DrawDMN(MatLib mlib)
	{
		mParts.DrawDMN(mlib, mTransform);
	}


	public Vector3 GetForwardVector()
	{
		return	Vector3.TransformNormal(Vector3.UnitZ, mTransform);
	}


	public void SaveToFile(string fileName)
	{
		FileStream		file	=new FileStream(fileName, FileMode.Create, FileAccess.Write);
		BinaryWriter	bw		=new BinaryWriter(file);

		//write a magic number identifying mesh instances
		UInt32	magic	=0x57A71C15;

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
		if(magic != 0x57A71C15)
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


	//this if for the DMN renderererererer
	public void AssignMaterialIDs(MaterialLib.IDKeeper idk)
	{
		mParts.AssignMaterialIDs(idk);
	}
}