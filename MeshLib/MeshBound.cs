using System;
using System.IO;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using Vortice.Mathematics;
using UtilityLib;

namespace MeshLib;

//handles all bound related stuff for a mesh instance
//(static or character) except for bone bounds
internal class MeshBound
{
	BoundingSphere	mSphere;
	BoundingBox		mBox;
	bool			mbChoice;	//true for box

	//bound for each part
	List<BoundingBox>		mPartBoxes		=new List<BoundingBox>();
	List<BoundingSphere>	mPartSpheres	=new List<BoundingSphere>();
	List<bool>				mChoices		=new List<bool>();				//true for box


	internal void FreeAll()
	{
		mPartBoxes.Clear();
		mPartSpheres.Clear();
		mChoices.Clear();
	}


	internal void Read(BinaryReader br)
	{
		mSphere.Center	=FileUtil.ReadVector3(br);
		mSphere.Radius	=br.ReadSingle();

		mBox.Min	=FileUtil.ReadVector3(br);
		mBox.Max	=FileUtil.ReadVector3(br);

		mbChoice	=br.ReadBoolean();

		mPartBoxes.Clear();
		mPartSpheres.Clear();
		mChoices.Clear();

		int	numParts	=br.ReadInt32();

		for(int i=0;i < numParts;i++)
		{
			BoundingBox		bb	=BoundingBox.Empty;
			BoundingSphere	sp	=BoundingSphere.Empty;

			bb.Min	=FileUtil.ReadVector3(br);
			bb.Max	=FileUtil.ReadVector3(br);

			mSphere.Center	=FileUtil.ReadVector3(br);
			mSphere.Radius	=br.ReadSingle();

			bool	bChoice	=br.ReadBoolean();

			mPartBoxes.Add(bb);
			mPartSpheres.Add(sp);
			mChoices.Add(bChoice);
		}
	}


	internal void Write(BinaryWriter bw)
	{
		Debug.Assert(mChoices.Count == mPartBoxes.Count);
		Debug.Assert(mChoices.Count == mPartSpheres.Count);

		FileUtil.WriteVector3(bw, mSphere.Center);
		bw.Write(mSphere.Radius);

		FileUtil.WriteVector3(bw, mBox.Min);
		FileUtil.WriteVector3(bw, mBox.Max);

		bw.Write(mbChoice);

		bw.Write(mChoices.Count);

		for(int i=0;i < mChoices.Count;i++)
		{
			FileUtil.WriteVector3(bw, mPartBoxes[i].Min);
			FileUtil.WriteVector3(bw, mPartBoxes[i].Max);

			FileUtil.WriteVector3(bw, mPartSpheres[i].Center);
			bw.Write(mPartSpheres[i].Radius);

			bw.Write(mChoices[i]);
		}
	}


	internal bool?	GetPartChoice(int index)
	{
		if(index < 0 || index >= mChoices.Count)
		{
			return	null;
		}
		return	mChoices[index];
	}


	internal BoundingBox? GetPartBox(int index)
	{
		if(index < 0 || index >= mChoices.Count)
		{
			return	null;
		}
		return	mPartBoxes[index];
	}


	internal BoundingSphere? GetPartSphere(int index)
	{
		if(index < 0 || index >= mChoices.Count)
		{
			return	null;
		}
		return	mPartSpheres[index];
	}


	//this is used by characters
	internal void ComputeRoughFromBox(Vector3 center, Vector3 min, Vector3 max)
	{
		mBox.Min	=min;
		mBox.Max	=max;

		float	distMin	=Vector3.Distance(min, center);
		float	distMax	=Vector3.Distance(max, center);

		mSphere.Center	=center;
		mSphere.Radius	=(distMin > distMax)? distMin : distMax;
	}


	public void SetRoughChoice(bool bChoice)
	{
		mbChoice	=bChoice;
	}


	public bool GetRoughBoundChoice()
	{
		return	mbChoice;
	}

	public BoundingBox	GetRoughBox()
	{
		return	mBox;
	}

	public BoundingSphere	GetRoughSphere()
	{
		return	mSphere;
	}


	//probably only makes sense for statics
	public bool ComputeParts(List<Mesh> parts)
	{
		mPartBoxes.Clear();
		mPartSpheres.Clear();
		mChoices.Clear();

		for(int i=0;i < parts.Count;i++)
		{
			EditorMesh	em	=parts[i].GetEditorMesh();
			if(em == null)
			{
				return	false;
			}

			BoundingBox		b;
			BoundingSphere	sp;

			em.ComputeRoughBound(out b, out sp);

			mPartBoxes.Add(b);
			mPartSpheres.Add(sp);
			mChoices.Add(true);		//box default?
		}
		return	true;
	}


	//this will be a starting point, user can edit the shape
	//in ColladaConvert
	public void ComputeOverall(List<Mesh> parts, List<Matrix4x4> transforms)
	{
		List<Vector3>	pnts	=new List<Vector3>();

		//characters usually won't have part transforms, everything origined
		if(transforms != null)
		{
			Debug.Assert(parts.Count == transforms.Count);
		}

		for(int i=0;i < parts.Count;i++)
		{
			EditorMesh	em	=parts[i].GetEditorMesh();
			if(em == null)
			{
				continue;
			}

			Matrix4x4	partTrans	=Matrix4x4.Identity;
			if(transforms != null)
			{
				partTrans	=transforms[i];
			}

			BoundingBox		b;
			BoundingSphere	sp;

			em.ComputeRoughBound(out b, out sp);

			//internal part transforms
			Vector3	transMin;
			Vector3	transMax;

			Mathery.TransformCoordinate(b.Min, partTrans, out transMin);
			Mathery.TransformCoordinate(b.Max, partTrans, out transMax);

			pnts.Add(transMin);
			pnts.Add(transMax);
		}

		mBox	=BoundingBox.CreateFromPoints(pnts.ToArray());
		mSphere	=Mathery.SphereFromPoints(pnts);
	}


	//A first check against a box encompassing all parts
	//TODO: cache that matrix invert somehow?
	//TODO: what about reporting ALL collisions?
	public bool RayIntersectRough(ref Matrix4x4 transform,
									Vector3 startPos, Vector3 endPos, float rayRadius)
	{
		//need this for boxes only
		Matrix4x4	tInv;

		//check rough bounds
		Vector3	rayDir	=Vector3.Normalize(endPos - startPos);

		if(mbChoice)	//box?
		{
			if(!Matrix4x4.Invert(transform, out tInv))
			{
				return	false;
			}

			Vector3	rayInvDir	=Vector3.TransformNormal(rayDir, tInv);
			Vector3	rayInvStart	=Mathery.TransformCoordinate(startPos, ref tInv);

			Ray	ray	=new Ray(rayInvStart, rayInvDir);

			float	?dist	=mBox.Intersects(ray);
			if(dist == null)
			{
				return	false;
			}
		}
		else
		{
			//sphere
			Ray	ray	=new Ray(startPos, rayDir);

			float	?dist	=mSphere.Intersects(ray);
			if(dist == null)
			{
				return	false;
			}
		}
		return	true;
	}


	public bool RayIntersectParts(ref Matrix4x4 transform, List<Matrix4x4> partXForms,
								Vector3 startPos, Vector3 endPos, float rayRadius,
								out Vector3 hitPos, out Vector3 hitNorm)
	{
		Debug.Assert(partXForms.Count == mChoices.Count);
		Debug.Assert(partXForms.Count == mPartBoxes.Count);
		Debug.Assert(partXForms.Count == mPartSpheres.Count);

		hitPos	=hitNorm	=Vector3.Zero;

		//need this for boxes only
		Matrix4x4	tInv;

		Vector3	rayDir	=Vector3.Normalize(endPos - startPos);

		//grab closest intersection
		float	bestDist	=float.MaxValue;
		int		bestPart	=-1;
		Vector3	bestHit		=Vector3.Zero;
		Vector3	bestNorm	=Vector3.Zero;

		//check submesh bounds or bone bounds
		int	partCount	=mChoices.Count;

		for(int i=0;i < partCount;i++)
		{
			bool	nbc	=mChoices[i];

			Matrix4x4	partXForm	=partXForms[i];

			if(nbc)
			{
				BoundingBox	pbox	=mPartBoxes[i];

				partXForm	*=transform;
				if(!Matrix4x4.Invert(partXForm, out tInv))
				{
					return	false;
				}
				Vector3	rayInvDir	=Vector3.TransformNormal(rayDir, tInv);
				Vector3	rayInvStart	=Mathery.TransformCoordinate(startPos, ref tInv);

				Ray	ray	=new Ray(rayInvStart, rayInvDir);

				float	?dist	=pbox.Intersects(ray);
				if(dist == null)
				{
					continue;
				}

				if(dist < bestDist)
				{
					bestPart	=i;
					bestDist	=dist.Value;
					bestHit		=rayInvStart + rayInvDir * dist.Value;	//boxspace
					bestNorm	=Mathery.BoxNormalAtPoint(pbox, bestHit);
					bestHit		=startPos + rayDir * dist.Value;	//worldspace
					bestNorm	=Vector3.TransformNormal(bestNorm, transform);
				}
			}
			else
			{
				//sphere
				BoundingSphere	ps	=mPartSpheres[i];

				Ray	ray	=new Ray(startPos, rayDir);

				float	?dist	=ps.Intersects(ray);
				if(dist == null)
				{
					continue;
				}

				if(dist < bestDist)
				{
					bestPart	=i;
					bestDist	=dist.Value;
					bestHit		=startPos + rayDir * dist.Value;
					bestNorm	=Vector3.Normalize(bestHit - ps.Center);	//might be backwards?
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
}