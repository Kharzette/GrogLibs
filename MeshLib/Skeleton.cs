using System;
using System.IO;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using Vortice.DXGI;
using Vortice.Direct3D;
using Vortice.Direct3D11;


namespace MeshLib;

public class GSNode
{
	internal string	mName;
	internal int	mIndex;

	List<GSNode>	mChildren	=new List<GSNode>();

	//current pos / rot / scale
	KeyFrame	mKeyValue	=new KeyFrame();


	public GSNode()
	{
	}


	internal int	Count(int cnt)
	{
		foreach(GSNode n in mChildren)
		{
			cnt	=n.Count(cnt);
		}

		cnt++;

		return	cnt;
	}


	public void AddChild(GSNode kid)
	{
		mChildren.Add(kid);
	}


	public void SetName(string name)
	{
		mName	=UtilityLib.Misc.AssignValue(name);
	}


	public Matrix4x4 GetMatrix()
	{
		Matrix4x4	mat	=Matrix4x4.CreateScale(mKeyValue.mScale) *
			Matrix4x4.CreateFromQuaternion(mKeyValue.mRotation) *
			Matrix4x4.CreateTranslation(mKeyValue.mPosition);

		return	mat;
	}


	internal bool GetMatrixForBone(int index, out Matrix4x4 ret)
	{
		if(index == mIndex)
		{
			ret	=GetMatrix();
			return	true;
		}

		foreach(GSNode n in mChildren)
		{
			if(n.GetMatrixForBone(index, out ret))
			{
				ret	*=GetMatrix();
				return	true;
			}
		}
		ret	=Matrix4x4.Identity;
		return	false;
	}


	internal bool GetMatrixForBone(string boneName, out Matrix4x4 ret)
	{
		if(boneName == mName)
		{
			ret	=GetMatrix();
			return	true;
		}

		foreach(GSNode n in mChildren)
		{
			if(n.GetMatrixForBone(boneName, out ret))
			{
				ret	*=GetMatrix();
				return	true;
			}
		}
		ret	=Matrix4x4.Identity;
		return	false;
	}


	internal void NukeBone(string boneName)
	{
		foreach(GSNode n in mChildren)
		{
			if(n.mName == boneName)
			{
				mChildren.Remove(n);
				return;
			}
			n.NukeBone(boneName);
		}
	}


	public void Read(BinaryReader br)
	{
		mName	=br.ReadString();

		mKeyValue.Read(br);

		int	numChildren	=br.ReadInt32();
		for(int i=0;i < numChildren;i++)
		{
			GSNode	n	=new GSNode();
			n.Read(br);

			mChildren.Add(n);
		}
	}


	public void Write(BinaryWriter bw)
	{
		bw.Write(mName);

		mKeyValue.Write(bw);

		bw.Write(mChildren.Count);
		foreach(GSNode n in mChildren)
		{
			n.Write(bw);
		}
	}


	internal void GetBoneNames(List<string> names)
	{
		foreach(GSNode n in mChildren)
		{
			n.GetBoneNames(names);
		}
		names.Add(mName);
	}


	internal bool GetBoneParentName(string boneName, out string parent)
	{
		if(boneName == mName)
		{
			parent	="";
			return	true;
		}

		foreach(GSNode n in mChildren)
		{
			if(n.GetBoneParentName(boneName, out parent))
			{
				if(parent == "")
				{
					parent	=mName;
				}
				return	true;
			}
		}
		parent	=null;
		return	false;
	}


	internal bool GetBoneKey(string bone, out KeyFrame ret)
	{
		if(mName == bone)
		{
			ret	=mKeyValue;
			return	true;
		}

		foreach(GSNode n in mChildren)
		{
			if(n.GetBoneKey(bone, out ret))
			{
				return	true;
			}
		}
		ret	=null;
		return	false;
	}


	public void SetKey(KeyFrame keyFrame)
	{
		mKeyValue.mPosition	=keyFrame.mPosition;
		mKeyValue.mRotation	=keyFrame.mRotation;
		mKeyValue.mScale	=keyFrame.mScale;
	}


	internal void SetIndexes(Dictionary<string, int> nameToIndex)
	{
		if(nameToIndex.ContainsKey(mName))
		{
			mIndex	=nameToIndex[mName];
		}

		foreach(GSNode n in mChildren)
		{
			n.SetIndexes(nameToIndex);
		}
	}


	internal void IterateStructure(Skeleton.IterateStruct ist)
	{
		foreach(GSNode gsn in mChildren)
		{
			ist(gsn.mName, mName);
		}

		foreach(GSNode gsn in mChildren)
		{
			gsn.IterateStructure(ist);
		}
	}
}


public class Skeleton
{
	List<GSNode>	mRoots	=new List<GSNode>();

	Dictionary<string, int>	mNameToIndex	=new Dictionary<string, int>();

	public delegate void IterateStruct(string name, string parent);


	public Skeleton()
	{
	}


	public int	GetBoneCount()
	{
		int	count	=0;
		foreach(GSNode n in mRoots)
		{
			count	=n.Count(count);
		}
		return	count;
	}


	public void AddRoot(GSNode gsn)
	{
		mRoots.Add(gsn);
	}


	public string GetBoneNameMirror(string name)
	{
		//some stuff to watch for...
		//ending with .L or .R (blender gamerig)
		//contains Left or Right (mixamo)
		//contains _L_ or _R_ (biped)
		string	mirror	="";

		if(!mNameToIndex.ContainsKey(name))
		{
			return	null;
		}

		if(name.Contains("Left"))
		{
			mirror	=name.Replace("Left", "Right");
		}
		else if(name.Contains("Right"))
		{
			mirror	=name.Replace("Right", "Left");
		}
		else if(name.EndsWith('L'))
		{
			mirror	=name.Substring(0, name.Length - 1);
			mirror	+='R';
		}
		else if(name.EndsWith('R'))
		{
			mirror	=name.Substring(0, name.Length - 1);
			mirror	+='L';
		}
		else if(name.Contains("_L_"))
		{
			mirror	=name.Replace("_L_", "_R_");
		}
		else if(name.Contains("_R_"))
		{
			mirror	=name.Replace("_R_", "_L_");
		}

		if(mNameToIndex.ContainsKey(mirror))
		{
			return	mirror;
		}
		return	null;
	}


	public void GetBoneNames(List<string> names)
	{
		foreach(GSNode n in mRoots)
		{
			n.GetBoneNames(names);
		}
	}


	public bool GetMatrixForBone(int index, out Matrix4x4 ret)
	{
		foreach(GSNode n in mRoots)
		{
			if(n.GetMatrixForBone(index, out ret))
			{
				return	true;
			}
		}
		ret	=Matrix4x4.Identity;
		return	false;
	}


	public bool GetMatrixForBone(string boneName, out Matrix4x4 ret)
	{
		foreach(GSNode n in mRoots)
		{
			if(n.GetMatrixForBone(boneName, out ret))
			{
				return	true;
			}
		}
		ret	=Matrix4x4.Identity;
		return	false;
	}


	public void NukeBone(string name)
	{
		foreach(GSNode n in mRoots)
		{
			if(n.mName == name)
			{
				mRoots.Remove(n);
				return;
			}
			n.NukeBone(name);
		}
	}


	public void Read(BinaryReader br)
	{
		int	numRoots	=br.ReadInt32();

		for(int i=0;i < numRoots;i++)
		{
			GSNode	n	=new GSNode();

			n.Read(br);

			mRoots.Add(n);
		}
		ComputeNameIndex();
	}


	public void Write(BinaryWriter bw)
	{
		bw.Write(mRoots.Count);

		foreach(GSNode n in mRoots)
		{
			n.Write(bw);
		}
	}


	internal bool GetBoneParentName(string boneName, out string parent)
	{
		foreach(GSNode n in mRoots)
		{
			if(n.GetBoneParentName(boneName, out parent))
			{
				return	true;
			}
		}
		parent	=null;
		return	false;
	}


	public KeyFrame GetBoneKey(string bone)
	{
		KeyFrame	ret	=null;
		foreach(GSNode n in mRoots)
		{
			if(n.GetBoneKey(bone, out ret))
			{
				return	ret;
			}
		}
		return	ret;
	}


	public void IterateStructure(IterateStruct ist)
	{
		//do the roots
		foreach(GSNode gsn in mRoots)
		{
			ist(gsn.mName, null);
		}

		//recurse
		foreach(GSNode gsn in mRoots)
		{
			gsn.IterateStructure(ist);
		}
	}


	public int GetBoneIndex(string name)
	{
		if(!mNameToIndex.ContainsKey(name))
		{
			return	-1;
		}
		return	mNameToIndex[name];
	}


	public string GetBoneName(int index)
	{
		foreach(KeyValuePair<string, int> bn in mNameToIndex)
		{
			if(bn.Value == index)
			{
				return	bn.Key;
			}
		}
		return	"None";
	}


	public int GetNumIndexedBones()
	{
		return	mNameToIndex.Count;
	}


	public bool CheckSkeletonIndexes(Skeleton otherSkel)
	{
		if(otherSkel.mNameToIndex.Count != mNameToIndex.Count)
		{
			return	false;
		}

		foreach(KeyValuePair<string, int> idx in mNameToIndex)
		{
			if(!otherSkel.mNameToIndex.ContainsKey(idx.Key))
			{
				return	false;
			}
			if(otherSkel.mNameToIndex[idx.Key] != idx.Value)
			{
				return	false;
			}
		}
		return	true;
	}


	//return the root bone names
	public void GetRootNames(List<string> ret)
	{
		if(ret == null)
		{
			ret	=new List<string>();
		}
		else
		{
			ret.Clear();
		}

		foreach(GSNode gsn in mRoots)
		{
			ret.Add(gsn.mName);
		}
	}


	//squash the indexes so they match with a linear array for shaders
	public void Compact(Dictionary<int, int> mapToOld)
	{
		if(mapToOld == null)
		{
			mapToOld	=new Dictionary<int, int>();
		}
		else
		{
			mapToOld.Clear();
		}

		List<string>	names	=new List<string>();

		GetBoneNames(names);

		for(int i=0;i < names.Count;i++)
		{
			mapToOld.Add(GetBoneIndex(names[i]), i);
		}

		//reindex
		ComputeNameIndex();
	}


	public void ComputeNameIndex()
	{
		mNameToIndex.Clear();

		List<string>	names	=new List<string>();

		GetBoneNames(names);

		int	idx	=0;
		foreach(string name in names)
		{
			mNameToIndex.Add(name, idx++);
		}

		//set indexes in the bones
		foreach(GSNode gsn in mRoots)
		{
			gsn.SetIndexes(mNameToIndex);
		}
	}
}