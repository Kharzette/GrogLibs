using System.IO;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using Vortice.Mathematics;


namespace MeshLib;

public class CollisionNode
{
	public const int	Box		=0;
	public const int	Sphere	=1;
	public const int	Capsule	=2;
	public const int	Invalid	=3;

	public BoundingBox		?mBox;
	public BoundingSphere	?mSphere;
	public BoundingCapsule	?mCapsule;

	//capsule has no position since it was made for bones
	public Vector3			mPosition;

	int		mShape;

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


	int	CreateKid(int shape, BoundingBox ?box,
					BoundingSphere ?sp, BoundingCapsule ?cap)
	{
		CollisionNode	kid	=new CollisionNode();

		mKids.Add(kid);

		kid.mBox		=box;
		kid.mSphere		=sp;
		kid.mCapsule	=cap;
		kid.mShape		=shape;

		return	kid.GetHashCode();
	}


	public int	CreateKid(BoundingBox box)
	{
		return	CreateKid(Box, box, null, null);
	}

	public int	CreateKid(BoundingSphere sp)
	{
		return	CreateKid(Sphere, null, sp, null);
	}

	public int	CreateKid(BoundingCapsule cap)
	{
		return	CreateKid(Capsule, null, null, cap);
	}

	public int GetShape()
	{
		return	mShape;
	}


	public void NukeKid(CollisionNode kid)
	{
		if(!mKids.Contains(kid))
		{
			//not our kid
			return;
		}

		kid.NukeAll();

		mKids.Remove(kid);
	}


	internal void NukeAll()
	{
		foreach(CollisionNode kid in mKids)
		{
			kid.NukeAll();
		}

		mKids.Clear();
	}


	public void AdjustLength(float lenDelta)
	{
		if(mShape == Box && mBox != null)
		{
			Vector3	min	=mBox.Value.Min;
			Vector3	max	=mBox.Value.Max;

			max.Z	+=lenDelta;

			mBox	=new BoundingBox(min, max);
		}
		else if(mShape == Sphere && mSphere != null)
		{
			//len key for spheres does radius
			mSphere	=new BoundingSphere(mSphere.Value.Center,
						mSphere.Value.Radius + lenDelta);
		}
		else if(mShape == Capsule && mCapsule != null)	//capsule
		{
			mCapsule.Value.IncLength(lenDelta);
		}
	}

	public void AdjustDepth(float depthDelta)
	{
		//only really makes sense for boxes
		if(mShape != Box || mBox == null)
		{
			return;
		}
		Vector3	min	=mBox.Value.Min;
		Vector3	max	=mBox.Value.Max;

		min.Y	-=depthDelta * 0.5f;
		max.Y	+=depthDelta * 0.5f;

		mBox	=new BoundingBox(min, max);
	}

	public void AdjustRadius(float radiusDelta)
	{
		if(mShape == Box && mBox != null)
		{
			Vector3	min	=mBox.Value.Min;
			Vector3	max	=mBox.Value.Max;

			min.X	-=radiusDelta * 0.5f;
			max.X	+=radiusDelta * 0.5f;

			mBox	=new BoundingBox(min, max);
		}
		else if(mShape == Sphere && mSphere != null)
		{
			//len key for spheres does radius
			mSphere	=new BoundingSphere(mSphere.Value.Center,
						mSphere.Value.Radius + radiusDelta);
		}
		else if(mShape == Capsule && mCapsule != null)	//capsule
		{
			mCapsule.Value.IncRadius(radiusDelta);
		}
	}

	public void Move(Vector3 delta)
	{
		if(mShape == Box && mBox != null)
		{
			Vector3	min	=mBox.Value.Min;
			Vector3	max	=mBox.Value.Max;

			min	+=delta;
			max	+=delta;

			mBox	=new BoundingBox(min, max);
		}
		else if(mShape == Sphere && mSphere != null)
		{
			mSphere	=new BoundingSphere(mSphere.Value.Center + delta,
						mSphere.Value.Radius);
		}
		else if(mShape == Capsule && mCapsule != null)	//capsule
		{
			mPosition	+=delta;
		}
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


	public void NukeAll()
	{
		mRoot.NukeAll();
	}


	public void SetRoot(CollisionNode root)
	{
		mRoot	=root;
	}
}