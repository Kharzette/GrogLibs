using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework;


namespace BSPCore
{
	internal class DirectLight
	{
		internal Int32		mLType;
		internal Vector3	mOrigin;
		internal Vector3	mNormal;
		internal float		mCone;	//used if a spotlight
		internal Vector3	mColor;
		internal float		mIntensity;
		internal UInt32		mType;

		internal const Int32		DLight_Blank	=0;
		internal const Int32		DLight_Point	=1;
		internal const Int32		DLight_Spot		=2;
		internal const Int32		DLight_Surface	=4;
		internal const Int32		DLight_Sun		=8;


		//this does radiosity style surface lights by putting lots of little
		//teeny lights along the surface in the surfaces color
		static internal List<DirectLight> MakeSurfaceLights(List<Vector3> verts,
			GFXTexInfo tex, Vector3 surfColor, Vector3 surfNormal, int surfFreq, int surfStrength)
		{
			List<DirectLight>	ret	=new List<DirectLight>();

			//make some edges
			List<Vector3>	edges	=new List<Vector3>();
			for(int i=0;i < verts.Count;i++)
			{
				Vector3	edge	=verts[(i + 1) % verts.Count] - verts[i];
				edge.Normalize();

				edges.Add(edge);
			}

			//make edge planes
			List<GBSPPlane>	edgePlanes	=new List<GBSPPlane>();
			for(int i=0;i < verts.Count;i++)
			{
				GBSPPlane	edgePlane	=new GBSPPlane();
				edgePlane.mNormal		=Vector3.Cross(surfNormal, edges[i]);

				edgePlane.mNormal.Normalize();

				edgePlane.mDist			=Vector3.Dot(verts[i], edgePlane.mNormal);

				edgePlanes.Add(edgePlane);
			}

			//get bounds
			Bounds	bnd	=new Bounds();
			foreach(Vector3 vert in verts)
			{
				bnd.AddPointToBounds(vert);
			}

			Vector3	uvec	=tex.mVecU;
			Vector3	vvec	=tex.mVecV;

			uvec.Normalize();
			vvec.Normalize();

			float	gridSizeU	=Vector3.Dot(uvec, bnd.mMaxs);
			float	gridSizeV	=Vector3.Dot(vvec, bnd.mMaxs);

			gridSizeU	-=Vector3.Dot(uvec, bnd.mMins);
			gridSizeV	-=Vector3.Dot(vvec, bnd.mMins);

			gridSizeU	/=surfFreq;
			gridSizeV	/=surfFreq;

			uvec	*=surfFreq;
			vvec	*=surfFreq;

			if(gridSizeU < 0.0f)
			{
				gridSizeU	=-gridSizeU;
				uvec		=-uvec;
			}
			if(gridSizeV < 0.0f)
			{
				gridSizeV	=-gridSizeV;
				vvec		=-vvec;
			}

			Debug.Assert(gridSizeU > 0.0f && gridSizeV > 0.0f);

			//make a grid of lights through the bounds
			for(int v=0;v < gridSizeV;v++)
			{
				for(int u=0;u < gridSizeU;u++)
				{
					Vector3	org	=bnd.mMins + (uvec * u) + (vvec * v);

					//bump out a tad
					org	+=surfNormal;

					bool	bOutside	=false;
					foreach(GBSPPlane p in edgePlanes)
					{
						if(p.Distance(org) > 0.0f)
						{
							bOutside	=true;
							break;	//outside the poly
						}
					}
					if(bOutside)
					{
						continue;
					}

					DirectLight	dl	=new DirectLight();
					dl.mOrigin		=org;
					dl.mIntensity	=surfStrength;
					dl.mColor		=surfColor * 255.0f;
					dl.mType		=DLight_Surface;
					dl.mNormal		=surfNormal;

					ret.Add(dl);
				}
			}
			return	ret;
		}
	}
}
