using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
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

				if(cnt > 13)
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

			//deal with the numbers
			//invert x and swap y and z
			//to convert to left handed
			mPoly	=new GBSPPoly(new Vector3(-numbers[0], numbers[2], numbers[1]),
				new Vector3(-numbers[3], numbers[5], numbers[4]),
				new Vector3(-numbers[6], numbers[8], numbers[7]));

			//all special brush properties are now driven by these quake 3 style flags
			//There used to be a ton of legacy stuff for determining properties from
			//texture name and other such goblinry, but all of that has been ganked
			//in favour of the QuArK addon flags
			if(flags.Count == 3)
			{
				ret			=flags[0];
				ti.mFlags	=flags[1];
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

			if(numbers[11] != 0.0f)
			{
				float	rot	=numbers[11];

				//planes pointing in -x, -z, and +y need rotation flipped
				//TODO: fix the .8, should be .7 something
				if(Vector3.Dot(plane.mNormal, Vector3.UnitX) > 0.8f)
				{
					rot	=-rot;
				}
				else if(Vector3.Dot(plane.mNormal, Vector3.UnitZ) > 0.8f)
				{
					rot	=-rot;
				}
				else if(Vector3.Dot(plane.mNormal, -Vector3.UnitY) > 0.8f)
				{
					rot	=-rot;
				}

				//wrap into 0 to 360
				UtilityLib.Mathery.WrapAngleDegrees(ref rot);

				Matrix	texRot	=Matrix.CreateFromAxisAngle(plane.mNormal,
					MathHelper.ToRadians(rot));

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
			ti.mAlpha	=1.0f;

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

			//if transparent, set to half alpha
			if(Misc.bFlagSet(ti.mFlags, TexInfo.TRANSPARENT))
			{
				ti.mAlpha	=0.5f;
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


		internal void GetTriangles(List<Vector3> verts, List<UInt32> indexes, bool bCheckFlags)
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
				mPoly.GetTriangles(verts, indexes, bCheckFlags);
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
	}
}