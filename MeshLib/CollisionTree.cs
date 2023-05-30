using System.IO;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using Vortice.Mathematics;
using UtilityLib;


namespace MeshLib;

public class CollisionNode
{
	public const int	Box		=0;
	public const int	Sphere	=1;
	public const int	Capsule	=2;
	public const int	Invalid	=3;

	BoundingBox		?mBox;
	BoundingSphere	?mSphere;
	BoundingCapsule	?mCapsule;

	int	mShape;

	public List<CollisionNode>	mKids	=new List<CollisionNode>();


	public CollisionNode(BoundingBox box)
	{
		mBox	=box;
		mShape	=Box;
	}

	internal CollisionNode() { }


	public void	ChangeShape(int shape)
	{
		if(mShape == Box)
		{
			if(shape == Box)
			{
				//nothing to do
				return;
			}
			else if(shape == Sphere)
			{
				mSphere	=BoundingSphere.CreateFromBoundingBox(mBox.Value);
				mBox	=null;
			}
			else if(shape == Capsule)
			{
				mCapsule	=BoundingCapsule.CreateFromBoundingBox(mBox.Value);
				mBox		=null;
			}
		}
		else if(mShape == Sphere)
		{
			if(shape == Box)
			{
				mBox	=new BoundingBox(mSphere.Value);
				mSphere	=null;
			}
			else if(shape == Sphere)
			{
				//nothing to do
				return;
			}
			else if(shape == Capsule)
			{
				mCapsule	=new BoundingCapsule(mSphere.Value.Radius, 1f);
				mSphere		=null;
			}
		}
		else if(mShape == Capsule)
		{
			if(shape == Box)
			{
				mBox		=BoundingCapsule.BoxFromCapsule(mCapsule.Value);
				mCapsule	=null;
			}
			else if(shape == Sphere)
			{
				mSphere		=new BoundingSphere(Vector3.Zero, mCapsule.Value.mRadius);
				mCapsule	=null;
			}
			else if(shape == Capsule)
			{
				//nothing to do
				return;
			}
		}
		mShape	=shape;
	}


	void	CreateKid(int shape, BoundingBox ?box,
					BoundingSphere ?sp, BoundingCapsule ?cap)
	{
		CollisionNode	kid	=new CollisionNode();

		mKids.Add(kid);

		kid.mBox		=box;
		kid.mSphere		=sp;
		kid.mCapsule	=cap;
		kid.mShape		=shape;
	}


	public void	CreateKid(BoundingBox box)
	{
		CreateKid(Box, box, null, null);
	}

	public void	CreateKid(BoundingSphere sp)
	{
		CreateKid(Sphere, null, sp, null);
	}

	public void	CreateKid(BoundingCapsule cap)
	{
		CreateKid(Capsule, null, null, cap);
	}

	public int GetShape()
	{
		return	mShape;
	}
}

//This is a hierarchy of collision shapes in object space
//One level up in the tree should encompass those below
public class CollisionTree
{
	CollisionNode	mRoot;


	public CollisionNode	GetRoot()
	{
		return	mRoot;
	}


	public void SetRoot(CollisionNode root)
	{
		mRoot	=root;
	}
}