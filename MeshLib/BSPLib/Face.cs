using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.Diagnostics;

namespace BSPLib
{
	public class Face
	{
		private Plane	mFacePlane;
		List<Vector3>	mPoints		=new List<Vector3>();
		public	UInt32	mFlags;

		private	const float	EDGE_LENGTH		=0.1f;

		//Q3 style face flags
		public const UInt32	SURF_LIGHT		=0x1;		// value will hold the light strength
		public const UInt32	SURF_SLICK		=0x2;		// effects game physics
		public const UInt32	SURF_SKY		=0x4;		// don't draw, but add to skybox
		public const UInt32	SURF_WARP		=0x8;		// turbulent water warp
		public const UInt32	SURF_TRANS33	=0x10;
		public const UInt32	SURF_TRANS66	=0x20;
		public const UInt32	SURF_FLOWING	=0x40;	// scroll towards angle
		public const UInt32	SURF_NODAMAGE			=0x1;		// never give falling damage
		public const UInt32	SURF_LADDER				=0x8;
		public const UInt32	SURF_NOIMPACT			=0x10;	// don't make missile explosions
		public const UInt32	SURF_NOMARKS			=0x20;	// don't leave missile marks
		public const UInt32	SURF_FLESH				=0x40;	// make flesh sounds and effects
		public const UInt32	SURF_NODRAW				=0x80;	// don't generate a drawsurface at all
		public const UInt32	SURF_HINT				=0x100;	// make a primary bsp splitter
		public const UInt32	SURF_SKIP				=0x200;	// completely ignore, allowing non-closed brushes
		public const UInt32	SURF_NOLIGHTMAP			=0x400;	// surface doesn't need a lightmap
		public const UInt32	SURF_POINTLIGHT			=0x800;	// generate lighting info at vertexes
		public const UInt32	SURF_METALSTEPS			=0x1000;	// clanking footsteps
		public const UInt32	SURF_NOSTEPS			=0x2000;	// no footstep sounds
		public const UInt32	SURF_NONSOLID			=0x4000;	// don't collide against curves with this set
		public const UInt32	SURF_LIGHTFILTER		=0x8000;	// act as a light filter during q3map -light
		public const UInt32	SURF_ALPHASHADOW		=0x10000;	// do per-pixel light shadow casting in q3map
		public const UInt32	SURF_NODLIGHT			=0x20000;	// don't dlight even if solid (solid lava, skies)
		public const UInt32	SURF_DUST				=0x40000; // leave a dust trail when walking on this surface


		#region Constructors
		public Face()	{}


		public Face(Plane p, Face f)
		{
			mPoints	=new List<Vector3>();
			SetFaceFromPlane(p, Brush.MIN_MAX_BOUNDS);
			mFacePlane	=p;

			if(f != null)
			{
				mFlags		=f.mFlags;
			}
		}


		public Face(Face f)
		{
			mPoints = new List<Vector3>();

			foreach(Vector3 pnt in f.mPoints)
			{
				mPoints.Add(pnt);
			}

			mFacePlane	=f.mFacePlane;
			mFlags		=f.mFlags;
		}


		public Face(Face f, bool bInvert)
		{
			mPoints	=new List<Vector3>();

			foreach(Vector3 pnt in f.mPoints)
			{
				mPoints.Add(pnt);
			}

			mFacePlane	=f.mFacePlane;
			mFlags		=f.mFlags;

			if(bInvert)
			{
				mPoints.Reverse();
				mFacePlane.mNormal		*=-1.0f;
				mFacePlane.mDistance	*=-1.0f;
			}
		}
		#endregion


		#region IO
		//parse map file stuff
		public Face(string szLine)
		{
			mPoints	=new List<Vector3>();

			//gank (
			szLine.TrimStart('(');

			szLine.Trim();

			string	[]tokens    =szLine.Split(' ');

			List<float>		numbers =new List<float>();
			List<UInt32>	flags	=new List<UInt32>();

			int cnt	=0;

			//grab all the numbers out
			string	texName	="";
			foreach(string tok in tokens)
			{
				//skip ()
				if(tok[0] == '(' || tok[0] == ')')
				{
					continue;
				}

				//grab tex name if avail
				if(tok == "clip" || tok == "CLIP")
				{
					mFlags	|=SURF_NODRAW;
					texName	=tok;
				}
				if(tok[0] == '*')
				{
					mFlags	|=SURF_WARP;
					texName	=tok.Substring(1);
					continue;
				}
				else if(tok[0] == '#')
				{
					mFlags	|=SURF_WARP;
					texName	=tok;
					continue;
				}
				else if(tok[0] == '+')
				{
					//animating I think
					texName	=tok;
					mFlags	|=SURF_WARP;
				}
				else if(char.IsLetter(tok, 0))
				{
					texName	=tok;
					continue;
				}

				float	num;
				UInt32	inum;

				if(cnt > 13)
				{
					if(UInt32.TryParse(tok, out inum))
					{
						flags.Add(inum);
						cnt++;
					}
				}
				else
				{
					if(Single.TryParse(tok, out num))
					{
						//rest are numbers
						numbers.Add(num);
						cnt++;
					}
				}
			}

			mPoints.Add(new Vector3(numbers[0], numbers[1], numbers[2]));
			mPoints.Add(new Vector3(numbers[3], numbers[4], numbers[5]));
			mPoints.Add(new Vector3(numbers[6], numbers[7], numbers[8]));

			//deal with the numbers
			//invert x and swap y and z
			//to convert to left handed
//			mPoints.Add(new Vector3(-numbers[0], numbers[2], numbers[1]));
//			mPoints.Add(new Vector3(-numbers[3], numbers[5], numbers[4]));
//			mPoints.Add(new Vector3(-numbers[6], numbers[8], numbers[7]));

			//see if there are any quake 3 style flags
/*			if(flags.Count > 0)
			{
				if((flags[0] & Brush.) != 0)
				{
					mFlags	|=DETAIL;
				}
			}*/

			SetPlaneFromFace();
		}


		internal void Read(BinaryReader br)
		{
			mFacePlane.Read(br);

			mFlags	=br.ReadUInt32();

			int	numPoints	=br.ReadInt32();
			for(int i=0;i < numPoints;i++)
			{
				Vector3	vec	=Vector3.Zero;

				vec.X	=br.ReadSingle();
				vec.Y	=br.ReadSingle();
				vec.Z	=br.ReadSingle();

				mPoints.Add(vec);
			}
		}


		internal void Write(BinaryWriter bw)
		{
			mFacePlane.Write(bw);

			bw.Write(mFlags);
			bw.Write(mPoints.Count);
			for(int i=0;i < mPoints.Count;i++)
			{
				bw.Write(mPoints[i].X);
				bw.Write(mPoints[i].Y);
				bw.Write(mPoints[i].Z);
			}
		}


		Vector3	ParseVec(string tok)
		{
			Vector3	ret	=Vector3.Zero;

			string	[]vecStr	=tok.Split(' ');

			//swap y and z
			Single.TryParse(vecStr[0], out ret.X);
			Single.TryParse(vecStr[1], out ret.Z);
			Single.TryParse(vecStr[2], out ret.Y);

			ret.X	=-ret.X;

			return	ret;
		}


		void SkipVMFDispInfoBlock(StreamReader sr)
		{
			string	s				="";
			int		bracketCount	=0;
			while((s = sr.ReadLine()) != null)
			{
				s	=s.Trim();
				if(s.StartsWith("}"))
				{
					bracketCount--;
					if(bracketCount == 0)
					{
						return;	//skip done
					}
				}
				else if(s.StartsWith("{"))
				{
					bracketCount++;
				}
			}
		}


		internal UInt32 ReadVMFSideBlock(StreamReader sr)
		{
			string	s	="";
			string	tex	="";
			UInt32	ret	=0;
			while((s = sr.ReadLine()) != null)
			{
				s	=s.Trim();
				if(s.StartsWith("\""))
				{
					string	[]tokens;
					tokens	=s.Split('\"');

					if(tokens[1] == "plane")
					{
						string	[]planePoints	=tokens[3].Split('(', ')');

						//1, 3, and 5
						mPoints.Add(ParseVec(planePoints[1]));
						mPoints.Add(ParseVec(planePoints[3]));
						mPoints.Add(ParseVec(planePoints[5]));
					}
					else if(tokens[1] == "material")
					{
						tex	=tokens[3];
						if(tex == "TOOLS/TOOLSAREAPORTAL")
						{
							ret	|=Brush.CONTENTS_AREAPORTAL;
						}
						if(tex == "TOOLS/TOOLSBLACK")
						{
							mFlags	&=~SURF_LIGHT;
							ret		|=Brush.CONTENTS_SOLID;
						}
						if(tex == "TOOLS/TOOLSBLOCK_LOS")
						{
							mFlags	|=SURF_NODRAW;	//not correct
						}
						if(tex == "TOOLS/TOOLSBLOCKBULLETS")
						{
							mFlags	|=SURF_NODRAW;	//not correct
						}
						if(tex == "TOOLS/TOOLSBLOCKLIGHT")
						{
							mFlags	|=SURF_NODRAW;	//not correct
						}
						if(tex == "TOOLS/TOOLSCLIP")
						{
							mFlags	|=SURF_NODRAW;
							ret		|=Brush.CONTENTS_PLAYERCLIP;
							ret		|=Brush.CONTENTS_MONSTERCLIP;
						}
						if(tex == "TOOLS/TOOLSCONTROLCLIP")	//not correct
						{
							mFlags	|=SURF_NODRAW;
							ret		|=Brush.CONTENTS_PLAYERCLIP;
							ret		|=Brush.CONTENTS_MONSTERCLIP;
						}
						if(tex == "TOOLS/TOOLSDOTTED")
						{
							mFlags	|=SURF_NODRAW;	//not correct
						}
						if(tex == "TOOLS/TOOLSFOG")
						{
							mFlags	|=SURF_NODRAW;
							ret		|=Brush.CONTENTS_MIST;
						}
						if(tex == "TOOLS/TOOLSHINT")
						{
							mFlags	|=SURF_NODRAW;
							mFlags	|=SURF_HINT;
							ret		|=Brush.CONTENTS_TRANSLUCENT;
						}
						if(tex == "TOOLS/TOOLSINVISIBLE")
						{
							mFlags	|=SURF_NODRAW;
						}
						if(tex == "TOOLS/TOOLSINVISIBLELADDER")
						{
							mFlags	|=SURF_LADDER;
							mFlags	|=SURF_NODRAW;
							ret		|=Brush.CONTENTS_LADDER;
						}
						if(tex == "TOOLS/TOOLSNODRAW")
						{
							mFlags	|=SURF_NODRAW;
						}
						if(tex == "TOOLS/TOOLSNPCCLIP")
						{
							mFlags	|=SURF_NODRAW;
							ret		|=Brush.CONTENTS_MONSTERCLIP;
						}
						if(tex == "TOOLS/TOOLSOCCLUDER")
						{
							mFlags	|=SURF_NODRAW;
						}
						if(tex == "TOOLS/TOOLSORIGIN")
						{
							ret	|=Brush.CONTENTS_ORIGIN;
						}
						if(tex == "TOOLS/TOOLSPLAYERCLIP")
						{
							mFlags	|=SURF_NODRAW;
							ret		|=Brush.CONTENTS_PLAYERCLIP;
						}
						if(tex == "TOOLS/TOOLSSKIP")
						{
							mFlags	|=SURF_SKIP;
						}
						if(tex == "TOOLS/TOOLSSKYBOX")
						{
							mFlags	|=SURF_SKY;
						}
						if(tex == "TOOLS/TOOLSSKYBOX2D")
						{
							mFlags	|=SURF_SKY;
						}
						else if(tex == "TOOLS/TOOLSSKYFOG")
						{
							mFlags	|=SURF_SKY;
						}
						else if(tex == "TOOLS/TOOLSTRIGGER")
						{
							mFlags	|=SURF_NODRAW;
							ret		|=Brush.CONTENTS_TRIGGER;
						}
					}
					else if(tokens[1] == "uaxis")
					{
						string	[]texVec	=tokens[3].Split('[', ' ', ']');
					}
					else if(tokens[1] == "vaxis")
					{
						string	[]texVec	=tokens[3].Split('[', ' ', ']');
					}
					else if(tokens[1] == "rotation")
					{
					}
				}
				else if(s.StartsWith("}"))
				{
					SetPlaneFromFace();
					return	ret;	//side done
				}
				else if(s == "dispinfo")
				{
					SkipVMFDispInfoBlock(sr);
					ret	|=Brush.CONTENTS_AUX;
				}
			}
			return	ret;
		}
		#endregion


		#region Queries
		internal Plane GetPlane()
		{
			return	mFacePlane;
		}


		internal bool IsBehind(Plane ax)
		{
			foreach(Vector3 pnt in mPoints)
			{
				if(ax.DistanceFrom(pnt) > Face.EDGE_LENGTH)
				{
					return	false;
				}
			}
			return	true;
		}


		internal void GetEdges(List<Edge> edges)
		{
			for(int i=0;i < mPoints.Count;i++)
			{
				int	j	=(i + 1) % mPoints.Count;

				Edge	e	=new Edge();
				e.mP0	=mPoints[i];
				e.mP1	=mPoints[j];

				edges.Add(e);
			}
		}


		internal bool ContainsPoint(Vector3 pnt)
		{
			foreach(Vector3 p in mPoints)
			{
				if(UtilityLib.Mathery.CompareVectorEpsilon(p, pnt, 0.001f))
				{
					return	true;
				}
			}
			return	false;
		}


		internal List<Vector3> GetPoints()
		{
			return	mPoints;
		}


		internal Vector3 GetFirstSharedVert(Face nb)
		{
			foreach(Vector3 pnt in mPoints)
			{
				foreach(Vector3 nbPnt in nb.mPoints)
				{
					if(UtilityLib.Mathery.CompareVectorEpsilon(pnt, nbPnt, 0.001f))
					{
						return	pnt;
					}
				}
			}
			Map.Print("GetFirstSharedVert returning Vector3.Zero!!!");
			return	Vector3.Zero;
		}


		internal float AngleBetween(Face f)
		{
			return	Vector3.Dot(mFacePlane.mNormal, f.mFacePlane.mNormal);
		}


		internal bool IsTiny()
		{
			int i, j;
			int edges   =0;
			for(i=0;i < mPoints.Count;i++)
			{
				j   =(i + 1) % mPoints.Count;

				Vector3	delta	=mPoints[j] - mPoints[i];
				float	len		=delta.Length();

				if(len > EDGE_LENGTH)
				{
					edges++;
					if(edges == 3)
					{
						return	false;
					}
				}
			}
			return	true;
		}


		internal bool IsHowThin(float dist)
		{
			int i, j;
			int edges   =0;
			for(i=0;i < mPoints.Count;i++)
			{
				j   =(i + 1) % mPoints.Count;

				Vector3	delta	=mPoints[j] - mPoints[i];
				float	len		=delta.Length();

				if(len > dist)
				{
					edges++;
					if(edges == 3)
					{
						return	false;
					}
				}
			}
			return	true;
		}


		internal bool IsHuge()
		{
			foreach(Vector3 pnt in mPoints)
			{
				if(pnt.X >= Brush.MIN_MAX_BOUNDS)
				{
					return	true;
				}
				else if(pnt.Y >= Brush.MIN_MAX_BOUNDS)
				{
					return	true;
				}
				else if(pnt.Z >= Brush.MIN_MAX_BOUNDS)
				{
					return	true;
				}
			}
			return	false;
		}


		internal void AddToBounds(Bounds bnd)
		{
			foreach(Vector3 pnt in mPoints)
			{
				bnd.AddPointToBounds(pnt);
			}
		}


		internal bool IsPointBehind(Vector3 pnt)
		{
			float	dist	=Vector3.Dot(mFacePlane.mNormal, pnt) - mFacePlane.mDistance;

			return	(dist < Plane.EPSILON);
		}


		internal bool IsPointBehind(Vector3 pnt, float radius)
		{
			float	dist	=Vector3.Dot(mFacePlane.mNormal, pnt) - mFacePlane.mDistance;

			return	(dist < radius);
		}


		internal void GetFaceMinMaxDistancesFromPlane(Plane p, ref float front, ref float back)
		{
			float	d;

			foreach(Vector3 pnt in mPoints)
			{
				d	=Vector3.Dot(pnt, p.mNormal) - p.mDistance;

				if(d > front)
				{
					front	=d;
				}
				else if(d < back)
				{
					back	=d;
				}
			}
		}


		internal void GetSplitInfo(Face f,
			out int pointsOnFront,
			out int pointsOnBack,
			out int pointsOnPlane)
		{
			pointsOnPlane	=pointsOnFront	=pointsOnBack	=0;
			foreach(Vector3 pnt in mPoints)
			{
				float	dot	=Vector3.Dot(pnt, mFacePlane.mNormal) - mFacePlane.mDistance;

				if(dot > Plane.EPSILON)
				{
					pointsOnFront++;
				}
				else if(dot < -Plane.EPSILON)
				{
					pointsOnBack++;
				}
				else
				{
					pointsOnPlane++;
				}
			}
		}


		internal void GetTriangles(List<Vector3> verts, List<UInt32> indexes)
		{
			int	ofs		=verts.Count;

			UInt32	offset	=(UInt32)ofs;

			//triangulate the brush face points
			foreach(Vector3 pos in mPoints)
			{
				verts.Add(pos);
			}

			int i	=0;
			for(i=1;i < mPoints.Count-1;i++)
			{
				//initial vertex
				indexes.Add(offset);
				indexes.Add((UInt32)(offset + i));
				indexes.Add((UInt32)(offset + ((i + 1) % mPoints.Count)));
			}
		}
		#endregion


		#region Modifications
		internal void Expand()
		{
			SetFaceFromPlane(mFacePlane, Brush.MIN_MAX_BOUNDS);
		}


		internal void Move(float dist)
		{
			mFacePlane.mDistance	+=dist;

			Expand();
		}


		bool SetPlaneFromFace()
		{
			int i;

			//catches colinear points now
			for(i=0;i < mPoints.Count;i++)
			{
				//gen a plane normal from the cross of edge vectors
				Vector3	v1  =mPoints[i] - mPoints[(i + 1) % mPoints.Count];
				Vector3	v2  =mPoints[(i + 2) % mPoints.Count] - mPoints[(i + 1) % mPoints.Count];

				mFacePlane.mNormal   =Vector3.Cross(v1, v2);

				if(!mFacePlane.mNormal.Equals(Vector3.Zero))
				{
					break;
				}
				//try the next three if there are three
			}
			if(i >= mPoints.Count)
			{
				//need a talky flag
				//in some cases this isn't worthy of a warning
				//Debug.WriteLine("Face with no normal!");
				return false;
			}

			mFacePlane.mNormal.Normalize();
			mFacePlane.mDistance	=Vector3.Dot(mPoints[1], mFacePlane.mNormal);

			return	true;
		}


		void SetFaceFromPlane(Plane p, float dist)
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

			org	=p.mNormal * p.mDistance;

			vright	=Vector3.Cross(vup, p.mNormal);

			vup		*=dist;
			vright	*=dist;

			mPoints.Clear();

			mPoints.Add(org - vright + vup);
			mPoints.Add(org + vright + vup);
			mPoints.Add(org + vright - vup);
			mPoints.Add(org - vright - vup);
		}


		//clip this face in front or behind face f
		//only keeps plane on verts if some part is
		//in front or behind unless bKeepOn is set
		internal bool ClipByFace(Face f, bool bFront, bool bKeepOn)
		{
			int				eitherCount	=0;
			List<Vector3>	onBoth		=new List<Vector3>();

			if(mPoints.Count == 3)
			{
				int	j=0;
				j++;
			}

			for(int i = 0;i < mPoints.Count;i++)
			{
				int	j	=(i + 1) % mPoints.Count;
				Vector3	p1, p2;

				p1	=mPoints[i];
				p2	=mPoints[j];

				float	d1	=Vector3.Dot(p1, f.mFacePlane.mNormal)
								- f.mFacePlane.mDistance;
				float	d2	=Vector3.Dot(p2, f.mFacePlane.mNormal)
								- f.mFacePlane.mDistance;

				if(d1 > Plane.EPSILON)
				{
					if(bFront)
					{
						eitherCount++;
						onBoth.Add(p1);
					}
				}
				else if(d1 < -Plane.EPSILON)
				{
					if(!bFront)
					{
						eitherCount++;
						onBoth.Add(p1);
					}
				}
				else
				{
					onBoth.Add(p1);
					continue;
				}

				//skip ahead if next point is onplane
				if(d2 < Plane.EPSILON && d2 > -Plane.EPSILON)
				{
					continue;
				}

				//skip ahead if next point is on same side
				if(d2 > Plane.EPSILON && d1 > Plane.EPSILON)
				{
					continue;
				}

				//skip ahead if next point is on same side
				if(d2 < -Plane.EPSILON && d1 < -Plane.EPSILON)
				{
					continue;
				}

				float	splitRatio	=d1 / (d1 - d2);
				Vector3	mid			=p1 + (splitRatio * (p2 - p1));

				onBoth.Add(mid);
			}

			//dump our point list
			mPoints.Clear();

			if(eitherCount > 0 || bKeepOn)
			{
				if(onBoth.Count == 3)
				{
					int	j=0;
					j++;
				}
				mPoints	=onBoth;
			}

			if(!SetPlaneFromFace())
			{
				//whole face was clipped away, no big deal
				mPoints.Clear();
				return	false;
			}
			return	true;
		}


		//clip to the front, only keeping
		//plane on if the normals match
		internal bool PortalClipByFace(Face f)
		{
			if(!mFacePlane.CompareEpsilon(f.mFacePlane, 0.001f))
			{
				return	false;
			}

			List<Vector3>	onSide		=new List<Vector3>();

			for(int i = 0;i < mPoints.Count;i++)
			{
				Vector3	p1;

				p1	=mPoints[i];

				float	d1	=Vector3.Dot(p1, f.mFacePlane.mNormal)
								- f.mFacePlane.mDistance;

				if(d1 > -Plane.EPSILON && d1 < Plane.EPSILON)
				{
					onSide.Add(p1);
					continue;
				}
				else
				{
					return	false;
				}
			}

			//dump our point list
			mPoints.Clear();

			mPoints	=onSide;

			if(!SetPlaneFromFace())
			{
				//whole face was clipped away, no big deal
				mPoints.Clear();
				return	false;
			}
			return	true;
		}


		//clip line in front or behind this face
		//returns true if line is chopped
		//Bias is needed towards the front, as
		//the results of this will need to be in
		//empty space for movement
		public bool ClipLine(ref Line ln, ref bool chopped)
		{
			Plane	p	=mFacePlane;
			float	d1	=Vector3.Dot(p.mNormal, ln.mP1) - p.mDistance;
			float	d2	=Vector3.Dot(p.mNormal, ln.mP2) - p.mDistance;

			if(d1 > Plane.EPSILON && d2 > Plane.EPSILON)
			{
				//fully in front
				return	false;
			}
			else if(d1 < -Plane.EPSILON && d2 < -Plane.EPSILON)
			{
				//fully behind
				return	true;
			}
			else if(d1 < Plane.EPSILON && d1 >= -Plane.EPSILON && d2 < Plane.EPSILON)
			{
				//mostly back
				chopped	=true;	//touching this plane
				return	true;
			}
			else if(d2 < Plane.EPSILON && d2 >= -Plane.EPSILON && d1 < Plane.EPSILON)
			{
				//mostly back
				chopped	=true;
				return	true;
			}
			else
			{
				d1	=-(Plane.EPSILON - d1);
				d2	=-(Plane.EPSILON - d2);
				float	splitRatio	=d1 / (d1 - d2);
				Vector3	mid			=ln.mP1 + (splitRatio * (ln.mP2 - ln.mP1));

				if(d1 >= 0.0)
				{
					chopped	=true;
					ln.mP1	=mid;
					return	true;
				}
				else
				{
					chopped	=true;
					ln.mP2	=mid;
					return	true;
				}
			}
		}


		internal Line ClipLine(Line ln, float radius)
		{
			Plane	p	=mFacePlane;
			float	d1	=Vector3.Dot(p.mNormal, ln.mP1) - p.mDistance;
			float	d2	=Vector3.Dot(p.mNormal, ln.mP2) - p.mDistance;

			d1	-=radius;
			d2	-=radius;

			float	splitRatio	=d1 / (d1 - d2);
			Vector3	mid			=ln.mP1 + (splitRatio * (ln.mP2 - ln.mP1));

			if(d1 < 0.0)
			{
				Line	ret;
				ret.mP1	=mid;
				ret.mP2	=ln.mP2;
				return	ret;
			}
			else
			{
				Line	ret;
				ret.mP1	=ln.mP1;
				ret.mP2	=mid;
				return	ret;
			}
		}


		//returns -1 for back, 1 for front, 0 for crosses
		internal int RayCheck(Line ln, float radius)
		{
			Plane	p	=mFacePlane;
			float	d1	=Vector3.Dot(p.mNormal, ln.mP1) - p.mDistance;
			float	d2	=Vector3.Dot(p.mNormal, ln.mP2) - p.mDistance;

			if(d1 < radius && d2 >= radius)
			{
				return	0;
			}
			else if(d2 < radius && d1 >= radius)
			{
				return	0;
			}
			else if(d1 >= radius && d2 >= radius)
			{
				return	1;
			}
			return	-1;
		}


		//clip line in front or behind this face
		//returns true if line is chopped
		//Bias is needed towards the front, as
		//the results of this will need to be in
		//empty space for movement
		public bool ClipLine(ref Line ln, ref bool chopped, float radius)
		{
			Plane	p	=mFacePlane;
			float	d1	=Vector3.Dot(p.mNormal, ln.mP1) - p.mDistance;
			float	d2	=Vector3.Dot(p.mNormal, ln.mP2) - p.mDistance;

			if(d1 > radius && d2 > radius)
			{
				//fully in front
				return	false;
			}
			else if(d1 < -radius && d2 < -radius)
			{
				//fully behind
				return	true;
			}
			else
			{
				float	splitRatio	=d1 / (d1 - d2);
				Vector3	mid			=ln.mP1 + (splitRatio * (ln.mP2 - ln.mP1));

				if(d1 > d2)
				{
					chopped	=true;
					ln.mP1	=mid;
					return	true;
				}
				else
				{
					chopped	=true;
					ln.mP2	=mid;
					return	true;
				}
			}
		}


		//clip line in front or behind this face
		//returns true if line is chopped
		//Bias is needed towards the front, as
		//the results of this will need to be in
		//empty space for movement
		public bool ClipLine3(ref Vector3 start, ref Vector3 end, ref bool chopped)
		{
			Plane	p	=mFacePlane;
			float	d1	=Vector3.Dot(p.mNormal, start) - p.mDistance;
			float	d2	=Vector3.Dot(p.mNormal, end) - p.mDistance;

			if(d1 > Plane.EPSILON && d2 > Plane.EPSILON)
			{
				//fully in front
				return	false;
			}
			else if(d1 < -Plane.EPSILON && d2 < -Plane.EPSILON)
			{
				//fully behind
				return	true;
			}
			else if(d1 < Plane.EPSILON && d1 >= -Plane.EPSILON && d2 < Plane.EPSILON)
			{
				//mostly back
				chopped	=true;	//touching this plane
				return	true;
			}
			else if(d2 < Plane.EPSILON && d2 >= -Plane.EPSILON && d1 < Plane.EPSILON)
			{
				//mostly back
				return	true;
			}
			else
			{
				d1	=-(Plane.EPSILON - d1);
				d2	=-(Plane.EPSILON - d2);
				float	splitRatio	=d1 / (d1 - d2);
				Vector3	mid			=start + (splitRatio * (end - start));

				if(d1 >= 0.0)
				{
					chopped	=true;
					start	=mid;
					return	true;
				}
				else
				{
					end	=mid;
					return	true;
				}
			}
		}


		//clip to the back, returning true if
		//portal is gobbled
		internal bool WouldPortalClipBehind(Face f)
		{
			Plane	inv	=f.mFacePlane;
			inv.Invert();
			if(!mFacePlane.CompareEpsilon(inv, 0.001f))
			{
				return	false;
			}

			List<Vector3>	onSide		=new List<Vector3>();

			for(int i = 0;i < mPoints.Count;i++)
			{
				Vector3	p1;

				p1	=mPoints[i];

				float	d1	=Vector3.Dot(p1, f.mFacePlane.mNormal)
								- f.mFacePlane.mDistance;

				if(d1 > -Plane.EPSILON && d1 < Plane.EPSILON)
				{
					onSide.Add(p1);
					continue;
				}
				else
				{
					return	false;
				}
			}

			//dump our point list
			mPoints.Clear();

			mPoints	=onSide;

			if(!SetPlaneFromFace())
			{
				mPoints.Clear();
				Debug.Assert(false);
				return	true;
			}
			return	true;
		}
		#endregion
	}
}
