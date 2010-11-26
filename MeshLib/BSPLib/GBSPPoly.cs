using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class GBSPPoly
	{
		public List<Vector3>	mVerts	=new List<Vector3>();

		public const float	EDGE_LENGTH	=0.1f;


		public GBSPPoly() { }
		internal GBSPPoly(GBSPPoly copyMe)
		{
			mVerts.Clear();
			foreach(Vector3 vert in copyMe.mVerts)
			{
				mVerts.Add(vert);
			}
		}


		public GBSPPoly(GBSPPlane p)
		{
			float	v;
			Vector3	vup, vright, org;

			//find the major axis of the plane normal
			vup.X	=vup.Y	=0.0f;
			vup.Z	=1.0f;
			if((System.Math.Abs(p.mNormal.Z) > System.Math.Abs(p.mNormal.X))
                && (System.Math.Abs(p.mNormal.Z) > System.Math.Abs(p.mNormal.Y)))
			{
				vup.X	=1.0f;
				vup.Y	=vup.Z	=0.0f;
			}

			v	=Vector3.Dot(vup, p.mNormal);

			vup	=vup + p.mNormal * -v;
			vup.Normalize();

			org	=p.mNormal * p.mDist;

			vright	=Vector3.Cross(vup, p.mNormal);

			vup		*=Brush.MIN_MAX_BOUNDS;
			vright	*=Brush.MIN_MAX_BOUNDS;

			mVerts.Clear();

			mVerts.Add(org - vright + vup);
			mVerts.Add(org + vright + vup);
			mVerts.Add(org + vright - vup);
			mVerts.Add(org - vright - vup);
			/*
			Vector3	rightVec, upVec, org, vert;

			if(!TextureAxisFromPlane(p, out rightVec, out upVec))
			{
				mVerts.Clear();
			}

			Vector3.Cross(ref p.mNormal, ref upVec, out rightVec);
			Vector3.Cross(ref p.mNormal, ref rightVec, out upVec);

			upVec.Normalize();
			rightVec.Normalize();

			org			=p.mDist * p.mNormal;
			upVec		*=Brush.MIN_MAX_BOUNDS;
			rightVec	*=Brush.MIN_MAX_BOUNDS;

			vert	=org - rightVec;
			vert	+=upVec;

			mVerts.Add(vert);

			vert	=org + rightVec;
			vert	+=upVec;

			mVerts.Add(vert);

			vert	=org + rightVec;
			vert	-=upVec;

			mVerts.Add(vert);

			vert	=org - rightVec;
			vert	-=upVec;

			mVerts.Add(vert);*/
		}


		bool TextureAxisFromPlane(GBSPPlane pln, out Vector3 Xv, out Vector3 Yv)
		{
			Int32	BestAxis;
			float	Dot,Best;
			
			Best		=0.0f;
			BestAxis	=-1;

			Xv	=Vector3.Zero;
			Yv	=Vector3.Zero;
			
			for(int i=0;i < 3;i++)
			{
				Dot	=Math.Abs(UtilityLib.Mathery.VecIdx(pln.mNormal, i));
				if(Dot > Best)
				{
					Best		=Dot;
					BestAxis	=i;
				}
			}

			switch(BestAxis)
			{
				case 0:						// X
					Xv.X	=0;
					Xv.Y	=0;
					Xv.Z	=1;

					Yv.X	=0;
					Yv.Y	=-1;
					Yv.Z	=0;
					break;
				case 1:						// Y
					Xv.X	=1;
					Xv.Y	=0;
					Xv.Z	=0;

					Yv.X	=0;
					Yv.Y	=0;
					Yv.Z	=1;
					break;
				case 2:						// Z
					Xv.X	=1;
					Xv.Y	=0;
					Xv.Z	=0;

					Yv.X	=0;
					Yv.Y	=-1;
					Yv.Z	=0;
					break;
				default:
					Map.Print("TextureAxisFromPlane: No Axis found.\n");
					return	false;
			}

			return	true;
		}


		internal bool ClipPolyEpsilon(float epsilon, GBSPPlane plane, bool flipTest)
		{
			if(mVerts.Count > 100)
			{
				Map.Print("ClipPoly:  Too many verts.\n");
				return	false;
			}

			Vector3	normal	=plane.mNormal;
			float	dist	=plane.mDist;

			if(flipTest)
			{
				normal	=-normal;
				dist	=-dist;
			}

			float	[]VDist			=new float[mVerts.Count];
			Int32	[]VSides		=new int[mVerts.Count];
			Int32	[]countSides	=new int[3];

			List<Vector3>	frontVerts	=new List<Vector3>();

			for(int i=0;i < mVerts.Count;i++)
			{
				VDist[i]	=Vector3.Dot(mVerts[i], normal) - dist;
				if(VDist[i] > epsilon)
				{
					VSides[i]	=0;
				}
				else if(VDist[i] < -epsilon)
				{
					VSides[i]	=1;
				}
				else
				{
					VSides[i]	=2;
				}

				countSides[VSides[i]]++;
			}

			if(countSides[0] == 0)
			{
				mVerts.Clear();
				return	true;
			}
			if(countSides[1] == 0)
			{
				return	true;
			}

			for(int i=0;i < mVerts.Count;i++)
			{
				Vector3	vert1	=mVerts[i];

				if(VSides[i] == 2)
				{
					frontVerts.Add(vert1);
					continue;
				}

				if(VSides[i] == 0)
				{
					frontVerts.Add(vert1);
				}

				int	nextVert	=(i + 1) % mVerts.Count;

				if(VSides[nextVert] == 2 || VSides[nextVert] == VSides[i])
				{
					continue;
				}

				Vector3	vert2	=mVerts[nextVert];
				float	scale	=VDist[i] / (VDist[i] - VDist[nextVert]);

				frontVerts.Add(vert1 + (vert2 - vert1) * scale);
			}

			mVerts.Clear();
			mVerts	=frontVerts;

			if(mVerts.Count < 3)
			{
				mVerts.Clear();
				return	false;
			}

			return	true;
		}


		internal bool IsTiny()
		{
			if(mVerts.Count < 3)
			{
				return	true;
			}

			int	edges	=0;

			for(int i=0;i < mVerts.Count;i++)
			{
				int	j	=(i == mVerts.Count - 1) ? 0 : i + 1;

				Vector3	delta	=mVerts[j] - mVerts[i];

				float	len	=delta.Length();

				if(len > EDGE_LENGTH)
				{
					if(++edges == 3)
					{
						return	false;
					}
				}
			}

			return	true;
		}


		internal bool SplitEpsilon(float epsilon, GBSPPlane plane, out GBSPPoly polyFront, out GBSPPoly polyBack, bool flipTest)
		{
			polyFront	=null;
			polyBack	=null;

			if(mVerts.Count > 100)
			{
				Map.Print("ClipPoly:  Too many verts.\n");
				return	false;
			}

			Vector3	normal	=plane.mNormal;
			float	dist	=plane.mDist;

			if(flipTest)
			{
				normal	=-normal;
				dist	=-dist;
			}

			float	[]VDist			=new float[mVerts.Count];
			Int32	[]VSides		=new int[mVerts.Count];
			Int32	[]countSides	=new int[3];

			List<Vector3>	frontVerts	=new List<Vector3>();
			List<Vector3>	backVerts	=new List<Vector3>();

			for(int i=0;i < mVerts.Count;i++)
			{
				VDist[i]	=Vector3.Dot(mVerts[i], normal) - dist;
				if(VDist[i] > epsilon)
				{
					VSides[i]	=0;
				}
				else if(VDist[i] < -epsilon)
				{
					VSides[i]	=1;
				}
				else
				{
					VSides[i]	=2;
				}

				countSides[VSides[i]]++;
			}

			if(countSides[0] == 0)
			{
				polyBack	=new GBSPPoly(this);
				return	true;
			}
			if(countSides[1] == 0)
			{
				polyFront	=new GBSPPoly(this);
				return	true;
			}

			for(int i=0;i < mVerts.Count;i++)
			{
				Vector3	vert1	=mVerts[i];

				if(VSides[i] == 2)
				{
					frontVerts.Add(vert1);
					backVerts.Add(vert1);
					continue;
				}

				if(VSides[i] == 0)
				{
					frontVerts.Add(vert1);
				}
				else if(VSides[i] == 1)
				{
					backVerts.Add(vert1);
				}

				int	nextVert	=(i + 1) % mVerts.Count;

				if(VSides[nextVert] == 2 || VSides[nextVert] == VSides[i])
				{
					continue;
				}

				Vector3	vert2	=mVerts[nextVert];
				float	scale	=VDist[i] / (VDist[i] - VDist[nextVert]);

				Vector3	splitVert	=vert1 + (vert2 - vert1) * scale;

				frontVerts.Add(splitVert);
				backVerts.Add(splitVert);
			}

			if(frontVerts.Count < 3)
			{
				return	false;
			}
			else
			{
				polyFront	=new GBSPPoly();
				polyFront.mVerts	=frontVerts;
			}

			if(backVerts.Count < 3)
			{
				return	false;
			}
			else
			{
				polyBack	=new GBSPPoly();
				polyBack.mVerts	=backVerts;
			}

			return	true;
		}


		internal void Reverse()
		{
			mVerts.Reverse();
		}


		internal float Area()
		{
			float	total	=0.0f;
			for(int i=2;i < mVerts.Count;i++)
			{
				Vector3	vect1	=mVerts[i - 1] - mVerts[0];
				Vector3	vect2	=mVerts[i] - mVerts[0];

				Vector3	cross	=Vector3.Cross(vect1, vect2);

				total	+=0.5f * cross.Length();
			}
			return	total;
		}
	}
}
