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

	//This is only needed by collision shapes, as the collision data
	//is already around the origin, ready to be transformed into bone space.
	//However all bone space is in meters, and the bind poses scale to
	//whatever units the user wants (grog, quake, etc)
	float		mScalefactor;
	Matrix4x4	mScaleMat;


	public Skin(float scaleFactor)
	{
		mScalefactor	=scaleFactor;
		mScaleMat		=Matrix4x4.CreateScale(1f / scaleFactor);
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


	internal void BuildDebugBoundDrawData(CommonPrims cprims)
	{
//		foreach(KeyValuePair<int, BoundingBox> boxen in mBoneBoxes)
//		{
//			cprims.AddBox(boxen.Key, boxen.Value);
//		}

//		foreach(KeyValuePair<int, BoundingSphere> sp in mBoneSpheres)
//		{
//			cprims.AddSphere(sp.Key, sp.Value);
//		}

		foreach(KeyValuePair<int, BoundingCapsule> caps in mBoneCapsules)
		{
			cprims.AddCapsule(caps.Key, caps.Value);
		}
	}


	internal BoundingBox GetBoneBoundBox(int index)
	{
		if(mBoneBoxes.ContainsKey(index))
		{
			return	mBoneBoxes[index];
		}
		return	new BoundingBox();
	}


	internal void SetBoneBounds(int index, BoundingBox box)
	{
		BoundingSphere	bs	=BoundingSphere.CreateFromBoundingBox(box);
		BoundingCapsule	bc	=BoundingCapsule.CreateFromBoundingBox(box);

		if(mBoneBoxes.ContainsKey(index))
		{
			mBoneBoxes[index]		=box;
			mBoneSpheres[index]		=bs;
			mBoneCapsules[index]	=bc;
		}
		else
		{
			mBoneBoxes.Add(index, box);
			mBoneSpheres.Add(index, bs);
			mBoneCapsules.Add(index, bc);
		}
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

		return	mScaleMat * ret;
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
		ret	=ibp * ret;

		return	ret;
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

		mBoneBoxes	=new Dictionary<int, BoundingBox>();

		for(int i=0;i < numBoxes;i++)
		{
			BoundingBox	box	=new BoundingBox();

			box.Min	=FileUtil.ReadVector3(br);
			box.Max	=FileUtil.ReadVector3(br);

			mBoneBoxes.Add(i, box);
		}

		mBoneSpheres	=new Dictionary<int, BoundingSphere>();

		for(int i=0;i < numBoxes;i++)
		{
			BoundingSphere	sp	=new BoundingSphere();

			sp.Center	=FileUtil.ReadVector3(br);
			sp.Radius	=br.ReadSingle();

			mBoneSpheres.Add(i, sp);
		}

		mBoneCapsules	=new Dictionary<int, BoundingCapsule>();

		for(int i=0;i < numBoxes;i++)
		{
			BoundingCapsule	bc	=new BoundingCapsule(br);

			mBoneCapsules.Add(i, bc);
		}

		mScalefactor	=br.ReadSingle();
		mScaleMat		=Matrix4x4.CreateScale(1f / mScalefactor);
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

		bw.Write(mScalefactor);
	}
}