using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class GBSPSide
	{
		public GBSPPoly	mPoly;
		public Int32	mPlaneNum;
		public sbyte	mPlaneSide;
		public Int32	mTexInfo;
		public UInt32	mFlags;

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

		public const UInt32 SIDE_HINT		=(1<<0);		// Side is a hint side
		public const UInt32 SIDE_SHEET		=(1<<1);		// Side is a sheet (only visible face in a sheet contents)
		public const UInt32 SIDE_VISIBLE	=(1<<2);		// 
		public const UInt32 SIDE_TESTED		=(1<<3);		// 
		public const UInt32 SIDE_NODE		=(1<<4);		// 


		internal GBSPSide() { }
		internal GBSPSide(GBSPSide copyMe)
		{
			this.mFlags	=copyMe.mFlags;
			this.mPlaneNum	=copyMe.mPlaneNum;
			this.mPlaneSide	=copyMe.mPlaneSide;
			this.mPoly		=new GBSPPoly(copyMe.mPoly);
			this.mTexInfo	=copyMe.mTexInfo;
		}

		internal UInt32 ReadVMFSideBlock(StreamReader sr, PlanePool pool, TexInfoPool tiPool)
		{
			string	s	="";
			string	tex	="";
			UInt32	ret	=0;
			TexInfo	ti	=new TexInfo();
			float	rot	=0.0f;

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

						mPoly	=new GBSPPoly();

						//1, 3, and 5
						mPoly.mVerts.Add(ParseVec(planePoints[1]));
						mPoly.mVerts.Add(ParseVec(planePoints[3]));
						mPoly.mVerts.Add(ParseVec(planePoints[5]));
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
							mFlags		&=~SURF_LIGHT;
							ret			|=Brush.CONTENTS_SOLID;
							ti.mFlags	|=TexInfo.TEXINFO_NO_LIGHTMAP;
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
							mFlags		|=SURF_NOLIGHTMAP;
							mFlags		|=SURF_NONSOLID;
							ret			|=Brush.CONTENTS_MIST;
							ti.mFlags	|=TexInfo.TEXINFO_TRANS;
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
							mFlags		|=SURF_SKY;
							ti.mFlags	|=TexInfo.TEXINFO_SKY;
						}
						if(tex == "TOOLS/TOOLSSKYBOX2D")
						{
							mFlags		|=SURF_SKY;
							ti.mFlags	|=TexInfo.TEXINFO_SKY;
						}
						else if(tex == "TOOLS/TOOLSSKYFOG")
						{
							mFlags		|=SURF_SKY;
							ti.mFlags	|=TexInfo.TEXINFO_SKY;
						}
						else if(tex == "TOOLS/TOOLSTRIGGER")
						{
							mFlags	|=SURF_NODRAW;
							ret		|=Brush.CONTENTS_TRIGGER;
						}
						ti.mTexture	=tex;
					}
					else if(tokens[1] == "uaxis")
					{
						string	[]texVec	=tokens[3].Split('[', ' ', ']');
						ti.mUVec.X	=Convert.ToSingle(texVec[1]);
						ti.mUVec.Y	=Convert.ToSingle(texVec[2]);
						ti.mUVec.Z	=Convert.ToSingle(texVec[3]);

						ti.mShiftU		=Convert.ToSingle(texVec[4]);
						ti.mDrawScaleU	=Convert.ToSingle(texVec[6]);
					}
					else if(tokens[1] == "vaxis")
					{
						string	[]texVec	=tokens[3].Split('[', ' ', ']');
						ti.mVVec.X	=Convert.ToSingle(texVec[1]);
						ti.mVVec.Y	=Convert.ToSingle(texVec[2]);
						ti.mVVec.Z	=Convert.ToSingle(texVec[3]);

						ti.mShiftV		=Convert.ToSingle(texVec[4]);
						ti.mDrawScaleV	=Convert.ToSingle(texVec[6]);
					}
					else if(tokens[1] == "lightmapscale")
					{
						ti.mLightMapScale	=Convert.ToSingle(tokens[3]);
					}
					else if(tokens[1] == "rotation")
					{
						rot	=Convert.ToSingle(tokens[3]);
					}
				}
				else if(s.StartsWith("}"))
				{
					GBSPPlane	plane	=new GBSPPlane(mPoly);

					plane.mType	=GBSPPlane.PLANE_ANY;
					plane.Snap();

					mPlaneNum	=pool.FindPlane(plane, out mPlaneSide);

					if(rot != 0.0f)
					{
						Vector3	texAxis	=Vector3.Cross(ti.mUVec, ti.mVVec);
						Matrix	texRot	=Matrix.CreateFromAxisAngle(texAxis,
							MathHelper.ToRadians(rot));

						//rotate tex vecs
						ti.mUVec	=Vector3.TransformNormal(ti.mUVec, texRot);
						ti.mVVec	=Vector3.TransformNormal(ti.mVVec, texRot);
					}

					mTexInfo	=tiPool.Add(ti);

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


		//convert from hammer flags to genesis flags
		internal void FixFlags()
		{
			UInt32	hammerFlags	=mFlags;

			mFlags	=SIDE_VISIBLE;

			//eventually need to convert these
			//into genesis texinfo stuff
			if((hammerFlags & SURF_ALPHASHADOW) != 0)
			{
			}
			if((hammerFlags & SURF_DUST) != 0)
			{
			}
			if((hammerFlags & SURF_FLESH) != 0)
			{
			}
			if((hammerFlags & SURF_FLOWING) != 0)
			{
			}
			if((hammerFlags & SURF_HINT) != 0)
			{
				mFlags	|=SIDE_HINT;
			}
			if((hammerFlags & SURF_LADDER) != 0)
			{
			}
			if((hammerFlags & SURF_LIGHT) != 0)
			{
			}
			if((hammerFlags & SURF_LIGHTFILTER) != 0)
			{
			}
			if((hammerFlags & SURF_METALSTEPS) != 0)
			{
			}
			if((hammerFlags & SURF_NODAMAGE) != 0)
			{
			}
			if((hammerFlags & SURF_NODLIGHT) != 0)
			{
			}
			if((hammerFlags & SURF_NODRAW) != 0)
			{
				mFlags	&=~SIDE_VISIBLE;
			}
			if((hammerFlags & SURF_NOIMPACT) != 0)
			{
			}
			if((hammerFlags & SURF_NOLIGHTMAP) != 0)
			{
			}
			if((hammerFlags & SURF_NOMARKS) != 0)
			{
			}
			if((hammerFlags & SURF_NONSOLID) != 0)
			{
			}
			if((hammerFlags & SURF_NOSTEPS) != 0)
			{
			}
			if((hammerFlags & SURF_POINTLIGHT) != 0)
			{
			}
			if((hammerFlags & SURF_SKIP) != 0)
			{
			}
			if((hammerFlags & SURF_SKY) != 0)
			{
			}
			if((hammerFlags & SURF_SLICK) != 0)
			{
			}
			if((hammerFlags & SURF_TRANS33) != 0)
			{
			}
			if((hammerFlags & SURF_TRANS66) != 0)
			{
			}
			if((hammerFlags & SURF_WARP) != 0)
			{
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


		internal void AddToBounds(Bounds mBounds)
		{
			foreach(Vector3 vert in mPoly.mVerts)
			{
				mBounds.AddPointToBounds(vert);
			}
		}
	}
}
