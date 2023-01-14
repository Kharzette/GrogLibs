using System;
using System.Numerics;
using System.Collections.Generic;
using System.Text;
using UtilityLib;


namespace BSPCore;

public struct GBSPPlane
{
	public Vector3	mNormal;
	public float	mDist;
	public UInt32	mType;

	//for texturing
	static Vector3	[]BaseAxisGrog		=new Vector3[6 * 3];
	static Vector3	[]BaseAxisQuake1	=new Vector3[6 * 3];

	public const UInt32	PLANE_X		=0;
	public const UInt32	PLANE_Y		=1;
	public const UInt32	PLANE_Z		=2;
	public const UInt32	PLANE_ANYX	=3;
	public const UInt32	PLANE_ANYY	=4;
	public const UInt32	PLANE_ANYZ	=5;
	public const UInt32	PLANE_ANY	=6;

	public const UInt32	PSIDE_FRONT		=1;
	public const UInt32	PSIDE_BACK		=2;
	public const UInt32	PSIDE_BOTH		=(PSIDE_FRONT | PSIDE_BACK);
	public const UInt32	PSIDE_FACING	=4;

	public const float PLANESIDE_EPSILON	=0.001f;


	static GBSPPlane()
	{
		//grog texturing
		BaseAxisGrog[0]		=Vector3.UnitY;
		BaseAxisGrog[1]		=-Vector3.UnitX;
		BaseAxisGrog[2]		=-Vector3.UnitZ;

		BaseAxisGrog[3]		=-Vector3.UnitY;
		BaseAxisGrog[4]		=-Vector3.UnitX;
		BaseAxisGrog[5]		=-Vector3.UnitZ;

		BaseAxisGrog[6]		=-Vector3.UnitX;
		BaseAxisGrog[7]		=Vector3.UnitZ;
		BaseAxisGrog[8]		=-Vector3.UnitY;

		BaseAxisGrog[9]		=Vector3.UnitX;
		BaseAxisGrog[10]	=Vector3.UnitZ;
		BaseAxisGrog[11]	=-Vector3.UnitY;

		BaseAxisGrog[12]	=Vector3.UnitZ;
		BaseAxisGrog[13]	=-Vector3.UnitX;
		BaseAxisGrog[14]	=-Vector3.UnitY;

		BaseAxisGrog[15]	=-Vector3.UnitZ;
		BaseAxisGrog[16]	=-Vector3.UnitX;
		BaseAxisGrog[17]	=-Vector3.UnitY;

		//quake texturing, in the original Q coordinate system
		BaseAxisQuake1[0]	=Vector3.UnitZ;
		BaseAxisQuake1[1]	=Vector3.UnitX;
		BaseAxisQuake1[2]	=-Vector3.UnitY;

		BaseAxisQuake1[3]	=-Vector3.UnitZ;
		BaseAxisQuake1[4]	=Vector3.UnitX;
		BaseAxisQuake1[5]	=-Vector3.UnitY;

		BaseAxisQuake1[6]	=Vector3.UnitX;
		BaseAxisQuake1[7]	=Vector3.UnitY;
		BaseAxisQuake1[8]	=-Vector3.UnitZ;

		BaseAxisQuake1[9]	=-Vector3.UnitX;
		BaseAxisQuake1[10]	=Vector3.UnitY;
		BaseAxisQuake1[11]	=-Vector3.UnitZ;

		BaseAxisQuake1[12]	=Vector3.UnitY;
		BaseAxisQuake1[13]	=Vector3.UnitX;
		BaseAxisQuake1[14]	=-Vector3.UnitZ;

		BaseAxisQuake1[15]	=-Vector3.UnitY;
		BaseAxisQuake1[16]	=Vector3.UnitX;
		BaseAxisQuake1[17]	=-Vector3.UnitZ;
	}


	internal GBSPPlane(GBSPPlane copyMe)
	{
		mNormal	=copyMe.mNormal;
		mDist	=copyMe.mDist;
		mType	=copyMe.mType;
	}


	internal GBSPPlane(GFXPlane copyMe)
	{
		mNormal	=copyMe.mNormal;
		mDist	=copyMe.mDist;
		mType	=copyMe.mType;
	}


	public GBSPPlane(Vector3 []verts)
	{
		int	i;

		mNormal	=Vector3.Zero;

		//catches colinear points now
		for(i=0;i < verts.Length;i++)
		{
			//gen a plane normal from the cross of edge vectors
			Vector3	v1  =verts[i] - verts[(i + 1) % verts.Length];
			Vector3	v2  =verts[(i + 2) % verts.Length] - verts[(i + 1) % verts.Length];

			mNormal   =Vector3.Cross(v1, v2);

			if(!mNormal.Equals(Vector3.Zero))
			{
				break;
			}
			//try the next three if there are three
		}
		if(i >= verts.Length)
		{
			CoreEvents.Print("Face with no normal!");
			mNormal	=Vector3.UnitX;
			mDist	=0.0f;
			mType	=GBSPPlane.PLANE_ANY;
			return;
		}

		mNormal	=Vector3.Normalize(mNormal);
		mDist	=Vector3.Dot(verts[1], mNormal);
		mType	=GetPlaneType(mNormal);
	}


	public GBSPPlane(GBSPPoly poly)
	{
		this	=poly.GenPlane();
	}


	internal void Snap()
	{
		Mathery.SnapNormal(ref mNormal);

		float	roundedDist	=(float)Math.Round((double)mDist);

		if(Math.Abs(mDist - roundedDist) < UtilityLib.Mathery.DIST_EPSILON)
		{
			mDist	=roundedDist;
		}
	}


	static internal UInt32 GetPlaneType(Vector3 normal)
	{
		float	X, Y, Z;
		
		X	=Math.Abs(normal.X);
		Y	=Math.Abs(normal.Y);
		Z	=Math.Abs(normal.Z);
		
		if(X == 1.0f)
		{
			return	PLANE_X;
		}
		else if(Y == 1.0f)
		{
			return	PLANE_Y;
		}
		else if(Z == 1.0f)
		{
			return	PLANE_Z;
		}

		if(X >= Y && X >= Z)
		{
			return	PLANE_ANYX;
		}
		else if(Y >= X && Y >= Z)
		{
			return	PLANE_ANYY;
		}
		else
		{
			return	PLANE_ANYZ;
		}
	}


	internal void Side(out bool side)
	{
		mType	=GetPlaneType(mNormal);

		side	=false;

		UInt32	type	=mType % PLANE_ANYX;

		if(mNormal.ArrayAccess((int)type) < 0)
		{
			Inverse();
			side	=true;
		}
	}


	internal bool Compare(GBSPPlane other)
	{
		Vector3	norm	=mNormal - other.mNormal;
		float	dist	=mDist - other.mDist;

		if(Math.Abs(norm.X) < UtilityLib.Mathery.NORMAL_EPSILON &&
			Math.Abs(norm.Y) < UtilityLib.Mathery.NORMAL_EPSILON &&
			Math.Abs(norm.Z) < UtilityLib.Mathery.NORMAL_EPSILON &&
			Math.Abs(dist) < UtilityLib.Mathery.DIST_EPSILON)
		{
			return	true;
		}
		return	false;
	}


	public void Inverse()
	{
		mNormal	=-mNormal;
		mDist	=-mDist;
	}


	internal float Distance(Vector3 pos)
	{
		return	Vector3.Dot(pos, mNormal) - mDist;
	}


	public static bool TextureAxisFromPlaneGrog(Vector3 planeNormal, out Vector3 xv, out Vector3 yv)
	{
		Int32	bestAxis;
		float	dot, best;
		
		best		=0.0f;
		bestAxis	=-1;

		xv	=Vector3.Zero;
		yv	=Vector3.Zero;

		for(int i=0;i < 6;i++)
		{
			dot	=Vector3.Dot(planeNormal, BaseAxisGrog[i * 3]);
			if(dot > best)
			{
				best		=dot;
				bestAxis	=i;
			}
		}

		xv	=BaseAxisGrog[bestAxis * 3 + 1];
		yv	=BaseAxisGrog[bestAxis * 3 + 2];

		return	true;
	}


	//Q1 tex vectors
	public static bool TextureAxisFromPlaneQuake1(Vector3 planeNormal, out Vector3 xv, out Vector3 yv)
	{
		Int32	bestAxis;
		float	dot, best;

		//swap y and z
		dot				=planeNormal.Z;
		planeNormal.Z	=planeNormal.Y;
		planeNormal.Y	=dot;
		
		best		=0.0f;
		bestAxis	=-1;

		xv	=Vector3.Zero;
		yv	=Vector3.Zero;

		for(int i=0;i < 6;i++)
		{
			dot	=Vector3.Dot(planeNormal, BaseAxisQuake1[i * 3]);
			if(dot > best)
			{
				best		=dot;
				bestAxis	=i;
			}
		}

		//flip y and z to get back to grogland
		xv.X	=BaseAxisQuake1[bestAxis * 3 + 1].X;
		xv.Y	=BaseAxisQuake1[bestAxis * 3 + 1].Z;
		xv.Z	=BaseAxisQuake1[bestAxis * 3 + 1].Y;

		yv.X	=BaseAxisQuake1[bestAxis * 3 + 2].X;
		yv.Y	=BaseAxisQuake1[bestAxis * 3 + 2].Z;
		yv.Z	=BaseAxisQuake1[bestAxis * 3 + 2].Y;

		return	true;
	}


	internal static int GetBestAxisFromPlane(GBSPPlane pln)
	{
		Int32	bestAxis;
		float	dot, best;
		
		best		=0.0f;
		bestAxis	=-1;

		for(int i=0;i < 6;i++)
		{
			dot	=Vector3.Dot(pln.mNormal, BaseAxisGrog[i * 3]);
			if(dot > best)
			{
				best		=dot;
				bestAxis	=i;
			}
		}
		return	bestAxis;
	}


	public void Write(System.IO.BinaryWriter bw)
	{
		bw.Write(mNormal.X);
		bw.Write(mNormal.Y);
		bw.Write(mNormal.Z);
		bw.Write(mDist);
		bw.Write(mType);
	}


	public void Read(System.IO.BinaryReader br)
	{
		mNormal.X	=br.ReadSingle();
		mNormal.Y	=br.ReadSingle();
		mNormal.Z	=br.ReadSingle();
		mDist		=br.ReadSingle();
		mType		=br.ReadUInt32();
	}
}