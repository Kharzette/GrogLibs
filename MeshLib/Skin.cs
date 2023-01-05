using System.IO;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using Vortice.Mathematics;
using Vortice.Direct3D11;
using UtilityLib;


namespace MeshLib;

//this is now a master skin, one per character
//all mesh parts will index in the same way
//The tool will need to make sure the inverse bind poses
//are all the same for each bone
public class Skin
{
	Dictionary<int, Matrix4x4>	mInverseBindPoses	=new Dictionary<int, Matrix4x4>();

	//for doing character collision stuff
	Dictionary<int, BoundingBox>		mBoneBoxes		=new Dictionary<int, BoundingBox>();
	Dictionary<int, BoundingSphere>		mBoneSpheres	=new Dictionary<int, BoundingSphere>();
	Dictionary<int, BoundingCapsule>	mBoneCapsules	=new Dictionary<int, BoundingCapsule>();
	Dictionary<int, int>				mBoneColShapes	=new Dictionary<int, int>();

	//This is only needed by collision shapes, as the collision data
	//is already around the origin, ready to be transformed into bone space.
	//However all bone space is in meters, and the bind poses scale to
	//whatever units the user wants (grog, quake, etc)
	float		mScaleFactor, mInvScaleFactor;
	Matrix4x4	mScaleMat, mInvScaleMat;

	//Changing things so that anim/bone space is all in meters.
	//Render bones will need to scale by scalefactor
	//
	//Also the rotational difference from art package to here is
	//stored here in a matrix.  This will be applied to the root
	//in the case of hierarchy bones.
	Matrix4x4	mRootTransform;


	public const int	Box		=0;
	public const int	Sphere	=1;
	public const int	Capsule	=2;
	public const int	Invalid	=3;


	public Skin(float scaleFactor)
	{
		mScaleFactor	=scaleFactor;
		mInvScaleFactor	=1f / scaleFactor;
		mScaleMat		=Matrix4x4.CreateScale(mScaleFactor);
		mInvScaleMat	=Matrix4x4.CreateScale(mInvScaleFactor);
	}


	public void SetRootTransform(Matrix4x4 mat)
	{
		mRootTransform	=mat;
	}


	//adds to existing
	public void SetBonePoses(Dictionary<int, Matrix4x4> invBindPoses)
	{
		foreach(KeyValuePair<int, Matrix4x4> bp in invBindPoses)
		{
			if(mInverseBindPoses.ContainsKey(bp.Key))
			{
				//if bone name already added, make sure the
				//inverse bind pose is the same for this skin
				Debug.Assert(bp.Value == mInverseBindPoses[bp.Key]);
				mInverseBindPoses[bp.Key]	=bp.Value;
			}
			else
			{
				mInverseBindPoses.Add(bp.Key, bp.Value);
			}
		}
	}


	//this is used to adjust points to a bone origin before bounding
	internal void MulByIBP(int index, List<Vector3> toMul)
	{
		if(!mInverseBindPoses.ContainsKey(index))
		{
			return;
		}

		Matrix4x4	ibp	=mInverseBindPoses[index];

		for(int i=0;i < toMul.Count;i++)
		{
			toMul[i]	=Mathery.TransformCoordinate(toMul[i], ref ibp);
		}
	}


	internal void BuildDebugBoundDrawData(int index, CommonPrims cprims)
	{
		if(!mBoneColShapes.ContainsKey(index))
		{
			return;
		}

		int	choice	=mBoneColShapes[index];
		if(choice == Box)
		{
			cprims.AddBox(index, mBoneBoxes[index]);
		}
		else if(choice == Sphere)
		{
			cprims.AddSphere(index, mBoneSpheres[index]);
		}
		else if(choice == Capsule)
		{
			cprims.AddCapsule(index, mBoneCapsules[index]);
		}
	}


	internal void BuildDebugBoundDrawData(CommonPrims cprims)
	{
		foreach(KeyValuePair<int, int> choice in mBoneColShapes)
		{
			BuildDebugBoundDrawData(choice.Key, cprims);
		}
	}


	internal BoundingBox? GetBoneBoundBox(int index, bool bScale = false)
	{
		if(mBoneBoxes.ContainsKey(index))
		{
			return	mBoneBoxes[index];
		}
		return	null;
	}


	internal BoundingSphere? GetBoneBoundSphere(int index, bool bScale = false)
	{
		if(mBoneBoxes.ContainsKey(index))
		{
			return	mBoneSpheres[index];
		}
		return	null;
	}


	internal BoundingCapsule? GetBoneBoundCapsule(int index, bool bScale = false)
	{
		if(mBoneCapsules.ContainsKey(index))
		{
			if(!bScale)
			{
				return	mBoneCapsules[index];
			}
			else
			{
				BoundingCapsule	ret	=mBoneCapsules[index];

//				ret.Scale(mInvScaleFactor);

				return	ret;
			}
		}
		return	null;
	}


	//this is used to get an initial guess based on box bounds
	//worldbox is in worldspace, bonebox in the space of the indexed bone
	internal void SetBoneBounds(int index, BoundingBox boneBox)
	{
		BoundingSphere	bs	=BoundingSphere.CreateFromBoundingBox(boneBox);
		BoundingCapsule	bc	=BoundingCapsule.CreateFromBoundingBox(boneBox);

		if(mBoneBoxes.ContainsKey(index))
		{
			mBoneBoxes[index]		=boneBox;
			mBoneSpheres[index]		=bs;
			mBoneCapsules[index]	=bc;
		}
		else
		{
			mBoneBoxes.Add(index, boneBox);
			mBoneSpheres.Add(index, bs);
			mBoneCapsules.Add(index, bc);
		}

		//default the shape to capsule
		if(mBoneColShapes.ContainsKey(index))
		{
			mBoneColShapes[index]	=Capsule;
		}
		else
		{
			mBoneColShapes.Add(index, Capsule);
		}
	}


	public void CopyBound(int src, int dst)
	{
		if(!mBoneColShapes.ContainsKey(src))
		{
			return;
		}
		if(!mBoneColShapes.ContainsKey(dst))
		{
			return;
		}

		int	shape	=mBoneColShapes[src];

		mBoneColShapes[dst]	=shape;

		if(shape == Box)
		{
			mBoneBoxes[dst]		=mBoneBoxes[src];	//structs jsut copy
		}
		else if(shape == Sphere)
		{
			mBoneSpheres[dst]	=mBoneSpheres[src];
		}
		else	//capsule
		{
			mBoneCapsules[dst]	=mBoneCapsules[src];
		}
	}


	public float GetScaleFactor()
	{
		return	mScaleFactor;
	}


	public int	GetBoundChoice(int index)
	{
		if(mBoneColShapes.ContainsKey(index))
		{
			return	mBoneColShapes[index];
		}
		return	Invalid;
	}


	//set the type of collision shape wanted for this particular bone
	public void SetBoundChoice(int index, int choice)
	{
		if(mBoneColShapes.ContainsKey(index))
		{
			mBoneColShapes[index]	=choice;
		}
		else
		{
			mBoneColShapes.Add(index, choice);
		}
	}


	public void AdjustBoneBoundRadius(int index, float radiusDelta)
	{
		if(!mBoneColShapes.ContainsKey(index))
		{
			return;
		}

		int	shape	=mBoneColShapes[index];

		if(shape == Box)
		{
			BoundingBox	bb	=mBoneBoxes[index];

			Vector3	min	=bb.Min;
			Vector3	max	=bb.Max;

			min.X	-=radiusDelta * 0.5f;
			max.X	+=radiusDelta * 0.5f;

			mBoneBoxes[index]	=new BoundingBox(min, max);
		}
		else if(shape == Sphere)
		{
			BoundingSphere	bs	=mBoneSpheres[index];
			mBoneSpheres[index]	=new BoundingSphere(bs.Center, bs.Radius + radiusDelta);
		}
		else	//capsule
		{
			BoundingCapsule	bc		=mBoneCapsules[index];
			bc.mRadius				+=radiusDelta;
			mBoneCapsules[index]	=bc;	//struct so copy
		}
	}


	public void AdjustBoneBoundDepth(int index, float depthDelta)
	{
		if(!mBoneColShapes.ContainsKey(index))
		{
			return;
		}

		int	shape	=mBoneColShapes[index];

		if(shape == Box)
		{
			BoundingBox	bb	=mBoneBoxes[index];

			Vector3	min	=bb.Min;
			Vector3	max	=bb.Max;

			min.Y	-=depthDelta * 0.5f;
			max.Y	+=depthDelta * 0.5f;

			mBoneBoxes[index]	=new BoundingBox(min, max);
		}
		else if(shape == Sphere)
		{
			//no sphere depth
		}
		else	//capsule
		{
			//no capsule depth
		}
	}


	public void AdjustBoneBoundLength(int index, float lenDelta)
	{
		if(!mBoneColShapes.ContainsKey(index))
		{
			return;
		}

		int	shape	=mBoneColShapes[index];

		if(shape == Box)
		{
			BoundingBox	bb	=mBoneBoxes[index];

			Vector3	min	=bb.Min;
			Vector3	max	=bb.Max;

			max.Z	+=lenDelta;

			mBoneBoxes[index]	=new BoundingBox(min, max);
		}
		else if(shape == Sphere)
		{
			//len key for spheres moves the centerpoint along
			//the bone's Z axis
			BoundingSphere	bs	=mBoneSpheres[index];
			mBoneSpheres[index]	=new BoundingSphere(bs.Center + Vector3.UnitZ * lenDelta, bs.Radius);
		}
		else	//capsule
		{
			BoundingCapsule	bc		=mBoneCapsules[index];
			bc.mLength				+=lenDelta;

			//don't allow negative
			bc.mLength =MathHelper.Max(bc.mLength, 0f);
			mBoneCapsules[index]	=bc;	//struct so copy
		}
	}


	public void SnapBoneBoundToJoint(int index)
	{
		if(!mBoneColShapes.ContainsKey(index))
		{
			return;
		}

		int	shape	=mBoneColShapes[index];

		if(shape == Box)
		{
			//snap box base to joint pos (0, 0, 0) in bone space
			BoundingBox	bb	=mBoneBoxes[index];

			float	xExtent	=bb.Max.X - bb.Min.X;
			float	yExtent	=bb.Max.Y - bb.Min.Y;
			float	zExtent	=bb.Max.Z - bb.Min.Z;

			Vector3	min	=Vector3.Zero;
			Vector3	max	=Vector3.Zero;

			min.X	=-xExtent * 0.5f;
			min.Y	=-yExtent * 0.5f;

			max.X	=xExtent * 0.5f;
			max.Y	=yExtent * 0.5f;
			max.Z	=zExtent;

			mBoneBoxes[index]	=new BoundingBox(min, max);
		}
		else if(shape == Sphere)
		{
			BoundingSphere	bs	=mBoneSpheres[index];
			mBoneSpheres[index]	=new BoundingSphere(Vector3.Zero, bs.Radius);
		}
		//capsule only works from the joint pos
	}


	//I think this is used for gamecode manipulation of bones
	public Matrix4x4 GetBoneByName(string name, Skeleton sk)
	{
		Matrix4x4	ret	=Matrix4x4.Identity;

		sk.GetMatrixForBone(name, out ret);

		int	idx	=sk.GetBoneIndex(name);

		//multiply by inverse bind pose
		ret	=mInverseBindPoses[idx] * ret;

		return	ret;
	}


	public Matrix4x4 GetBoneByNameNoBind(string name, Skeleton sk)
	{
		Matrix4x4	ret	=Matrix4x4.Identity;

		sk.GetMatrixForBone(name, out ret);

		return	ret * mScaleMat * mRootTransform;
	}


	public Matrix4x4 GetBoneByIndex(int idx, Skeleton sk)
	{
		Matrix4x4	ret	=Matrix4x4.Identity;

		sk.GetMatrixForBone(idx, out ret);

		Matrix4x4	ibp	=Matrix4x4.Identity;

		if(mInverseBindPoses.ContainsKey(idx))
		{
			ibp	=mInverseBindPoses[idx];
		}

		//multiply by inverse bind pose
		//and scale factor
		//and rotate into grog coordinates
//		ret	=mScaleMat * ibp * ret * mRootTransform;
		ret	=ibp * ret * mRootTransform * mScaleMat;

		return	ret;
	}


	public Matrix4x4 GetBoneByIndexNoBind(int idx, Skeleton sk)
	{
		Matrix4x4	ret	=Matrix4x4.Identity;

		sk.GetMatrixForBone(idx, out ret);

		return	ret * mScaleMat * mRootTransform;
	}


	public void Read(BinaryReader br)
	{
		mInverseBindPoses.Clear();

		int	numIBP	=br.ReadInt32();
		for(int i=0;i < numIBP;i++)
		{
			int	idx	=br.ReadInt32();

			Matrix4x4	mat	=FileUtil.ReadMatrix(br);

			mInverseBindPoses.Add(idx, mat);
		}

		int	numBoxes	=br.ReadInt32();

		mBoneBoxes.Clear();

		for(int i=0;i < numBoxes;i++)
		{
			Vector3	min	=FileUtil.ReadVector3(br);
			Vector3	max	=FileUtil.ReadVector3(br);

			BoundingBox	box	=new BoundingBox(min, max);

			mBoneBoxes.Add(i, box);
		}

		mBoneSpheres.Clear();

		for(int i=0;i < numBoxes;i++)
		{
			Vector3	center	=FileUtil.ReadVector3(br);
			float	radius	=br.ReadSingle();

			BoundingSphere	sp	=new BoundingSphere(center, radius);

			mBoneSpheres.Add(i, sp);
		}

		mBoneCapsules.Clear();

		for(int i=0;i < numBoxes;i++)
		{
			BoundingCapsule	bc	=new BoundingCapsule(br);

			mBoneCapsules.Add(i, bc);
		}

		mBoneColShapes.Clear();

		for(int i=0;i < numBoxes;i++)
		{
			mBoneColShapes.Add(i, br.ReadInt32());
		}

		mScaleFactor	=br.ReadSingle();
		mScaleMat		=Matrix4x4.CreateScale(mScaleFactor);
		mInvScaleMat	=Matrix4x4.CreateScale(1f / mScaleFactor);

		//read the root transform
		mRootTransform	=FileUtil.ReadMatrix(br);
	}


	public void Write(BinaryWriter bw)
	{
		bw.Write(mInverseBindPoses.Count);
		foreach(KeyValuePair<int, Matrix4x4> ibp in mInverseBindPoses)
		{
			bw.Write(ibp.Key);
			FileUtil.WriteMatrix(bw, ibp.Value);
		}

		bw.Write(mBoneBoxes.Count);
		for(int i=0;i < mBoneBoxes.Count;i++)
		{
			FileUtil.WriteVector3(bw, mBoneBoxes[i].Min);
			FileUtil.WriteVector3(bw, mBoneBoxes[i].Max);
		}
		for(int i=0;i < mBoneBoxes.Count;i++)
		{
			FileUtil.WriteVector3(bw, mBoneSpheres[i].Center);
			bw.Write(mBoneSpheres[i].Radius);
		}
		for(int i=0;i < mBoneBoxes.Count;i++)
		{
			mBoneCapsules[i].Write(bw);
		}
		for(int i=0;i < mBoneBoxes.Count;i++)
		{
			bw.Write(mBoneColShapes[i]);
		}

		bw.Write(mScaleFactor);

		FileUtil.WriteMatrix(bw, mRootTransform);
	}
}