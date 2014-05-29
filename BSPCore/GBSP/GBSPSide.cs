using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SharpDX;
using UtilityLib;


namespace BSPCore
{
	internal class GBSPSide
	{
		internal GBSPPoly	mPoly;
		internal Int32		mPlaneNum;
		internal bool		mbFlipSide;
		internal Int32		mTexInfo;
		internal UInt32		mFlags;

		//flags from Genesis
		internal const UInt32	SIDE_HINT		=(1<<0);	//Side is a hint side
		internal const UInt32	SIDE_SHEET		=(1<<1);	//Side is a sheet (only visible face in a sheet contents)
		internal const UInt32	SIDE_VISIBLE	=(1<<2);	// 
		internal const UInt32	SIDE_TESTED		=(1<<3);	// 
		internal const UInt32	SIDE_NODE		=(1<<4);	// 


		internal GBSPSide() { }
		internal GBSPSide(GBSPSide copyMe)
		{
			this.mFlags		=copyMe.mFlags;
			this.mPlaneNum	=copyMe.mPlaneNum;
			this.mbFlipSide	=copyMe.mbFlipSide;
			this.mPoly		=new GBSPPoly(copyMe.mPoly);
			this.mTexInfo	=copyMe.mTexInfo;
		}


		#region IO
		internal void Write(BinaryWriter bw)
		{
			mPoly.Write(bw);

			bw.Write(mPlaneNum);
			bw.Write(mbFlipSide);
			bw.Write(mTexInfo);
			bw.Write(mFlags);
		}


		internal void Read(BinaryReader br)
		{
			mPoly	=new GBSPPoly(0);

			mPoly.Read(br);

			mPlaneNum	=br.ReadInt32();
			mbFlipSide	=br.ReadBoolean();
			mTexInfo	=br.ReadInt32();
			mFlags		=br.ReadUInt32();
		}


		internal UInt32 ReadMapLine(string szLine,
			TexInfoPool tiPool,	BSPBuildParams prms)
		{
			UInt32	ret	=0;

			//gank (
			szLine.TrimStart('(');

			szLine.Trim();

			string	[]tokens    =szLine.Split(' ');

			List<float>		numbers =new List<float>();
			List<UInt32>	flags	=new List<UInt32>();

			int cnt	=0;

			TexInfo	ti		=new TexInfo();

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
				if(char.IsLetter(tok, 0))
				{
					texName	=tok;
					continue;
				}

				float	num;
				UInt32	inum;

				if(cnt > 13 && cnt < 16)
				{
					if(UtilityLib.Mathery.TryParse(tok, out inum))
					{
						flags.Add(inum);
						cnt++;
					}
				}
				else
				{
					if(UtilityLib.Mathery.TryParse(tok, out num))
					{
						//rest are numbers
						numbers.Add(num);
						cnt++;
					}
				}
			}

			//deal with the numbers...
			//Back in the XNA days, x used to be negated to convert to left handed.
			//Now for DX, only a swapping of y and z is needed
			mPoly	=new GBSPPoly(new Vector3(numbers[0], numbers[2], numbers[1]),
				new Vector3(numbers[3], numbers[5], numbers[4]),
				new Vector3(numbers[6], numbers[8], numbers[7]));

			//all special brush properties are now driven by these quake 3 style flags
			//There used to be a ton of legacy stuff for determining properties from
			//texture name and other such goblinry, but all of that has been ganked
			//in favour of the QuArK addon flags
			if(flags.Count == 2)
			{
				ret			=flags[0];
				ti.mFlags	=flags[1];

				if(Misc.bFlagSet(ti.mFlags, TexInfo.TRANSPARENT | TexInfo.MIRROR))
				{
					ti.mAlpha	=MathUtil.Clamp(numbers[14], 0.0f, 1.0f);

					if(ti.mAlpha == 1.0f)
					{
						CoreEvents.Print("Warning!  Alpha or Mirror with an alpha of 1.0!\n");
					}

					//probably would never want a value of zero, but that is
					//the default.  If zero, set to half
					if(ti.mAlpha == 0.0f)
					{
						ti.mAlpha	=0.5f;
					}
				}
			}

			//temp plane, not pooling yet
			GBSPPlane	plane	=new GBSPPlane(mPoly);

			plane.mType	=GBSPPlane.PLANE_ANY;
			plane.Snap();

			ti.mShiftU		=numbers[9];
			ti.mShiftV		=numbers[10];
			ti.mDrawScaleU	=numbers[12];
			ti.mDrawScaleV	=numbers[13];
			ti.mTexture		=texName;

			GBSPPlane.TextureAxisFromPlane(plane, out ti.mUVec, out ti.mVVec);

			//flip elements
			ti.mUVec.X	=-ti.mUVec.X;
			ti.mVVec.X	=-ti.mVVec.X;

			if(numbers[11] != 0.0f)
			{
				float	rot	=numbers[11];

				int	axis	=GBSPPlane.GetBestAxisFromPlane(plane);

				//some planes need rotation flipped
				//due to the coordinate system change
				if(axis == 0 || axis == 3 || axis == 5)
				{
					rot	=-rot;
				}

				//wrap into 0 to 360
				UtilityLib.Mathery.WrapAngleDegrees(ref rot);

				Matrix	texRot	=Matrix.RotationAxis(plane.mNormal,
					MathUtil.DegreesToRadians(rot));

				//rotate tex vecs
				ti.mUVec	=Vector3.TransformNormal(ti.mUVec, texRot);
				ti.mVVec	=Vector3.TransformNormal(ti.mVVec, texRot);
			}

			FixFlags(ref ti);

			mTexInfo	=tiPool.Add(ti);
			return	ret;
		}
		#endregion


		//make sure flags have legal values
		internal void FixFlags(ref TexInfo ti)
		{
			//defaults
			mFlags		=SIDE_VISIBLE;

			//if mirror, set no lightmap and flat
			if(Misc.bFlagSet(ti.mFlags, TexInfo.MIRROR))
			{
				ti.mFlags	|=TexInfo.FLAT;
				ti.mFlags	|=TexInfo.NO_LIGHTMAP;
			}

			//if both flat and gouraud set, choose flat
			{
				if(Misc.bFlagSet(ti.mFlags, TexInfo.FLAT)
					&& Misc.bFlagSet(ti.mFlags, TexInfo.GOURAUD))
				{
					Misc.ClearFlag(ref ti.mFlags, TexInfo.GOURAUD);
				}
			}

			//if emit light chosen, resulting face should be fullbright
			{
				if(Misc.bFlagSet(ti.mFlags, TexInfo.EMITLIGHT))
				{
					ti.mFlags	|=TexInfo.FULLBRIGHT;
				}
			}

			//if flat or gouraud or fullbright or sky, set the NO_LIGHTMAP flag
			{
				if(Misc.bFlagSet(ti.mFlags, TexInfo.FLAT))
				{
					ti.mFlags	|=TexInfo.NO_LIGHTMAP;
				}

				if(Misc.bFlagSet(ti.mFlags, TexInfo.GOURAUD))
				{
					ti.mFlags	|=TexInfo.NO_LIGHTMAP;
				}

				if(Misc.bFlagSet(ti.mFlags, TexInfo.FULLBRIGHT))
				{
					ti.mFlags	|=TexInfo.NO_LIGHTMAP;
				}

				if(Misc.bFlagSet(ti.mFlags, TexInfo.SKY))
				{
					ti.mFlags	|=TexInfo.NO_LIGHTMAP;
				}
			}
		}


		Vector3	ParseVec(string tok)
		{
			Vector3	ret	=Vector3.Zero;

			string	[]vecStr	=tok.Split(' ');

			//swap y and z
			UtilityLib.Mathery.TryParse(vecStr[0], out ret.X);
			UtilityLib.Mathery.TryParse(vecStr[1], out ret.Z);
			UtilityLib.Mathery.TryParse(vecStr[2], out ret.Y);

			ret.X	=-ret.X;

			return	ret;
		}


		internal void GetTriangles(PlanePool pp,
			Color matColor,
			List<Vector3> verts,
			List<Vector3> normals,
			List<Color> colors,
			List<UInt16> indexes, bool bCheckFlags)
		{
			if(bCheckFlags)
			{
				if((mFlags & SIDE_VISIBLE) == 0)
				{
					return;
				}
			}
			if(mPoly != null)
			{
				GBSPPlane	p	=pp.mPlanes[mPlaneNum];

				if(mbFlipSide)
				{
					p.Inverse();
				}

				mPoly.GetTriangles(p, matColor, verts, normals, colors, indexes, bCheckFlags);
			}
		}


		internal void GetLines(List<Vector3> verts, List<UInt32> indexes, bool bCheckFlags)
		{
			if(bCheckFlags)
			{
				if((mFlags & SIDE_VISIBLE) == 0)
				{
					return;
				}
			}
			if(mPoly != null)
			{
				mPoly.GetLines(verts, indexes, bCheckFlags);
			}
		}


		internal void MovePoly(Vector3 delta)
		{
			mPoly.Move(delta);
		}


		internal void PoolPlane(PlanePool pool)
		{
			GBSPPlane	p	=new GBSPPlane(mPoly);

			p.mType	=GBSPPlane.PLANE_ANY;
			p.Snap();

			mPlaneNum	=pool.FindPlane(p, out mbFlipSide);
		}


		internal void AddToBounds(Bounds bnd)
		{
			mPoly.AddToBounds(bnd);
		}


		internal Vector3 GetCenter()
		{
			return	mPoly.Center();
		}
	}
}