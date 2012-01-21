using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;


namespace BSPCore
{
	internal class GBSPSide
	{
		internal GBSPPoly	mPoly;
		internal Int32		mPlaneNum;
		internal bool		mbFlipSide;
		internal Int32		mTexInfo;
		internal UInt32		mFlags;

		//Q3 style face flags
		//found in the Q3 Radiant open source
		//Q3 support is kind of non existant though
		internal const UInt32	SURF_LIGHT				=0x1;		//value will hold the light strength
		internal const UInt32	SURF_SLICK				=0x2;		//(using this as gouraud for now, used to be effects game physics)
		internal const UInt32	SURF_SKY				=0x4;		//don't draw, but add to skybox
		internal const UInt32	SURF_WARP				=0x8;		//(Using this as mirror for now, used to be turbulent water warp)
		internal const UInt32	SURF_TRANS33			=0x10;
		internal const UInt32	SURF_TRANS66			=0x20;
		internal const UInt32	SURF_FLOWING			=0x40;		//scroll towards angle
//		internal const UInt32	SURF_NODAMAGE			=0x1;		//never give falling damage
//		internal const UInt32	SURF_LADDER				=0x8;
//		internal const UInt32	SURF_NOIMPACT			=0x10;		//don't make missile explosions
//		internal const UInt32	SURF_NOMARKS			=0x20;		//don't leave missile marks
//		internal const UInt32	SURF_FLESH				=0x40;		//make flesh sounds and effects
		internal const UInt32	SURF_NODRAW				=0x80;		//don't generate a drawsurface at all
		internal const UInt32	SURF_HINT				=0x100;		//make a primary bsp splitter
		internal const UInt32	SURF_SKIP				=0x200;		//completely ignore, allowing non-closed brushes
		internal const UInt32	SURF_NOLIGHTMAP			=0x400;		//surface doesn't need a lightmap
		internal const UInt32	SURF_POINTLIGHT			=0x800;		//generate lighting info at vertexes
		internal const UInt32	SURF_METALSTEPS			=0x1000;	//clanking footsteps
		internal const UInt32	SURF_NOSTEPS			=0x2000;	//no footstep sounds
		internal const UInt32	SURF_NONSOLID			=0x4000;	//don't collide against curves with this set
		internal const UInt32	SURF_LIGHTFILTER		=0x8000;	//act as a light filter during q3map -light
		internal const UInt32	SURF_ALPHASHADOW		=0x10000;	//do per-pixel light shadow casting in q3map
		internal const UInt32	SURF_NODLIGHT			=0x20000;	//don't dlight even if solid (solid lava, skies)
		internal const UInt32	SURF_DUST				=0x40000;	//leave a dust trail when walking on this surface

		//hammer smoothing flags found as a number in the vmfs
		//smoothing stuff is quite hackily used
		//as a way to pass flags
		//just made these up
		internal const UInt32 SMOOTHING_GOURAUD		=0x1;		//for smoothing group stuff from hammer
		internal const UInt32 SMOOTHING_FLAT		=0x1000000;	//for smoothing group stuff from hammer
		internal const UInt32 SMOOTHING_SURFLIGHT	=0x10000;	//turns on radiosity surface lighting

		//flags from Genesis
		internal const UInt32	SIDE_HINT		=(1<<0);	//Side is a hint side
		internal const UInt32	SIDE_SHEET		=(1<<1);	//Side is a sheet (only visible face in a sheet contents)
		internal const UInt32	SIDE_VISIBLE	=(1<<2);	// 
		internal const UInt32	SIDE_TESTED		=(1<<3);	// 
		internal const UInt32	SIDE_NODE		=(1<<4);	// 
		internal const UInt32	SIDE_SLIPPERY	=(1<<5);	//added by me for slippery surfaces


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

						//1, 3, and 5
						mPoly	=new GBSPPoly(ParseVec(planePoints[1]),
							ParseVec(planePoints[3]),
							ParseVec(planePoints[5]));
					}
					else if(tokens[1] == "material")
					{
						tex	=tokens[3];
						if(tex.StartsWith("DEV/DEV_GLASS")
							|| tex.StartsWith("GLASS/"))
						{
							mFlags		|=SURF_TRANS66;
							ti.mFlags	|=TexInfo.TRANS;
						}
						if(tex.StartsWith("DEV/REFLECTIVITY"))
						{
							//mirror surface
							//TODO: reflectivity amount
							ti.mFlags	|=TexInfo.MIRROR;
							ti.mFlags	|=TexInfo.NO_LIGHTMAP;
							ti.mFlags	|=TexInfo.FLAT;
						}
						if(tex == "TOOLS/TOOLSAREAPORTAL")
						{
							ret	|=Contents.CONTENTS_AREAPORTAL;
						}
						if(tex == "TOOLS/TOOLSBLACK")
						{
							mFlags		&=~SURF_LIGHT;
							ret			|=Contents.CONTENTS_SOLID;
							ti.mFlags	|=TexInfo.NO_LIGHTMAP;
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
							ret		|=Contents.CONTENTS_PLAYERCLIP;
							ret		|=Contents.CONTENTS_MONSTERCLIP;
						}
						if(tex == "TOOLS/TOOLSCONTROLCLIP")	//not correct
						{
							mFlags	|=SURF_NODRAW;
							ret		|=Contents.CONTENTS_PLAYERCLIP;
							ret		|=Contents.CONTENTS_MONSTERCLIP;
						}
						if(tex == "TOOLS/TOOLSDOTTED")
						{
							mFlags	|=SURF_NODRAW;	//not correct
						}
						if(tex == "TOOLS/TOOLSFOG")
						{
							mFlags		|=SURF_NOLIGHTMAP;
							mFlags		|=SURF_NONSOLID;
							ret			|=Contents.CONTENTS_MIST;
							ti.mFlags	|=TexInfo.TRANS;
						}
						if(tex == "TOOLS/TOOLSHINT")
						{
							mFlags	|=SURF_NODRAW;
							mFlags	|=SURF_HINT;
							ret		|=Contents.CONTENTS_TRANSLUCENT;
						}
						if(tex == "TOOLS/TOOLSINVISIBLE")
						{
							mFlags	|=SURF_NODRAW;
						}
						if(tex == "TOOLS/TOOLSINVISIBLELADDER")
						{
							mFlags	|=SURF_NODRAW;
							ret		|=Contents.CONTENTS_LADDER;
						}
						if(tex == "TOOLS/TOOLSNODRAW")
						{
							mFlags	|=SURF_NODRAW;
						}
						if(tex == "TOOLS/TOOLSNPCCLIP")
						{
							mFlags	|=SURF_NODRAW;
							ret		|=Contents.CONTENTS_MONSTERCLIP;
						}
						if(tex == "TOOLS/TOOLSOCCLUDER")
						{
							mFlags	|=SURF_NODRAW;
						}
						if(tex == "TOOLS/TOOLSORIGIN")
						{
							ret	|=Contents.CONTENTS_ORIGIN;
						}
						if(tex == "TOOLS/TOOLSPLAYERCLIP")
						{
							mFlags	|=SURF_NODRAW;
							ret		|=Contents.CONTENTS_PLAYERCLIP;
						}
						if(tex == "TOOLS/TOOLSSKIP")
						{
							mFlags	|=SURF_SKIP;
						}
						if(tex == "TOOLS/TOOLSSKYBOX")
						{
							mFlags		|=SURF_SKY;
							ti.mFlags	|=TexInfo.SKY;
							ti.mFlags	|=TexInfo.NO_LIGHTMAP;
						}
						if(tex == "TOOLS/TOOLSSKYBOX2D")
						{
							mFlags		|=SURF_SKY;
							ti.mFlags	|=TexInfo.SKY;
							ti.mFlags	|=TexInfo.NO_LIGHTMAP;
						}
						else if(tex == "TOOLS/TOOLSSKYFOG")
						{
							mFlags		|=SURF_SKY;
							ti.mFlags	|=TexInfo.SKY;
						}
						if(tex == "TOOLS/TOOLSTRIGGER")
						{
							ret			|=Contents.BSP_CONTENTS_USER12;
							ti.mFlags	|=TexInfo.FULLBRIGHT;
							mFlags		|=SURF_NODRAW;
							ti.mFlags	|=TexInfo.NO_LIGHTMAP;
						}
						if(tex == "TEST/COLOR_RED")
						{
							//using this as lava
							ret				|=Contents.BSP_CONTENTS_EMPTY2;
							ret				|=Contents.BSP_CONTENTS_USER1;
							ret				|=Contents.BSP_CONTENTS_WAVY2;
							ti.mFlags		|=TexInfo.TRANS;
							ti.mFlags		|=TexInfo.FLAT;
							ti.mFlags		|=TexInfo.LIGHT;				//lava emits light by default?
							ti.mFlags		|=TexInfo.FULLBRIGHT;
							ti.mFlags		|=TexInfo.NO_LIGHTMAP;
						}
						if(tex.StartsWith("DEV/DEV_WATER") || tex.StartsWith("WATER"))
						{
							ret			|=Contents.BSP_CONTENTS_TRANSLUCENT2;
							ret			|=Contents.BSP_CONTENTS_EMPTY2;
							ret			|=Contents.BSP_CONTENTS_WAVY2;
							ret			|=Contents.BSP_CONTENTS_USER3;
							ti.mFlags	|=TexInfo.TRANS;
						}
						if(tex == "DEV/DEV_SLIME")
						{
							ret			|=Contents.BSP_CONTENTS_TRANSLUCENT2;
							ret			|=Contents.BSP_CONTENTS_EMPTY2;
							ret			|=Contents.BSP_CONTENTS_WAVY2;
							ret			|=Contents.BSP_CONTENTS_USER2;
							ti.mFlags	|=TexInfo.TRANS;
						}
						ti.mTexture	=tex;
					}
					else if(tokens[1] == "uaxis")
					{
						string	[]texVec	=tokens[3].Split('[', ' ', ']');

						UtilityLib.Mathery.TryParse(texVec[1], out ti.mUVec.X);
						UtilityLib.Mathery.TryParse(texVec[2], out ti.mUVec.Y);
						UtilityLib.Mathery.TryParse(texVec[3], out ti.mUVec.Z);

						//negate x and swap y and z
						ti.mUVec.X	=-ti.mUVec.X;
						float	y	=ti.mUVec.Y;
						ti.mUVec.Y	=ti.mUVec.Z;
						ti.mUVec.Z	=y;

						UtilityLib.Mathery.TryParse(texVec[4], out ti.mShiftU);
						UtilityLib.Mathery.TryParse(texVec[6], out ti.mDrawScaleU);
					}
					else if(tokens[1] == "vaxis")
					{
						string	[]texVec	=tokens[3].Split('[', ' ', ']');
						UtilityLib.Mathery.TryParse(texVec[1], out ti.mVVec.X);
						UtilityLib.Mathery.TryParse(texVec[2], out ti.mVVec.Y);
						UtilityLib.Mathery.TryParse(texVec[3], out ti.mVVec.Z);

						//negate x and swap y and z
						ti.mVVec.X	=-ti.mVVec.X;
						float	y	=ti.mVVec.Y;
						ti.mVVec.Y	=ti.mVVec.Z;
						ti.mVVec.Z	=y;

						UtilityLib.Mathery.TryParse(texVec[4], out ti.mShiftV);
						UtilityLib.Mathery.TryParse(texVec[6], out ti.mDrawScaleV);
					}
					else if(tokens[1] == "lightmapscale")
					{
						UtilityLib.Mathery.TryParse(tokens[3], out ti.mLightMapScale);
					}
					else if(tokens[1] == "rotation")
					{
						UtilityLib.Mathery.TryParse(tokens[3], out rot);
					}
					else if(tokens[1] == "smoothing_groups")
					{
						UInt32	smoove;
						UtilityLib.Mathery.TryParse(tokens[3], out smoove);
						if((smoove & SMOOTHING_SURFLIGHT) != 0)
						{
							ti.mFlags		|=TexInfo.LIGHT;
							ti.mFlags		|=TexInfo.FULLBRIGHT;			//emit light so ...?
							ti.mFlags		|=TexInfo.NO_LIGHTMAP;
						}
						if(smoove >= SMOOTHING_GOURAUD && smoove < SMOOTHING_FLAT)
						{
							if((ti.mFlags & TexInfo.FULLBRIGHT) == 0)
							{
								ti.mFlags	|=TexInfo.GOURAUD;
								ti.mFlags	|=TexInfo.NO_LIGHTMAP;
							}
						}
						else if(smoove >= SMOOTHING_FLAT)
						{
							if((ti.mFlags & TexInfo.FULLBRIGHT) == 0)
							{
								ti.mFlags	|=TexInfo.FLAT;
								ti.mFlags	|=TexInfo.NO_LIGHTMAP;
							}
						}
					}
				}
				else if(s.StartsWith("}"))
				{
					GBSPPlane	plane	=new GBSPPlane(mPoly);

					plane.mType	=GBSPPlane.PLANE_ANY;
					plane.Snap();

					mPlaneNum	=pool.FindPlane(plane, out mbFlipSide);

					//hammer prerotates texvecs!
					if(false && rot != 0.0f)
					{
						Vector3	texAxis	=Vector3.Cross(ti.mUVec, ti.mVVec);
						Matrix	texRot	=Matrix.CreateFromAxisAngle(texAxis,
							MathHelper.ToRadians(rot));

						//rotate tex vecs
						ti.mUVec	=Vector3.TransformNormal(ti.mUVec, texRot);
						ti.mVVec	=Vector3.TransformNormal(ti.mVVec, texRot);
					}

					ti.mUVec	/=ti.mDrawScaleU;
					ti.mVVec	/=ti.mDrawScaleV;

					FixFlags(ref ti, false, false);

					mTexInfo	=tiPool.Add(ti);

					return	ret;	//side done
				}
				else if(s == "dispinfo")
				{
					SkipVMFDispInfoBlock(sr);
					ret	|=Contents.CONTENTS_AUX;
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


		internal UInt32 ReadMapLine(string szLine, PlanePool pool, TexInfoPool tiPool,
			bool bSlickAsGouraud, bool bWarpAsMirror)
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
				if(tok == "clip" || tok == "CLIP")
				{
					mFlags	|=SURF_NODRAW;
					texName	=tok;
					ret	|=Contents.CONTENTS_PLAYERCLIP;
					ret	|=Contents.CONTENTS_MONSTERCLIP;
				}
				if(tok[0] == '*' || tok[0] == '#')
				{
					texName	=tok.Substring(1);
					if(texName.StartsWith("lava") || texName.StartsWith("LAVA"))
					{
						ret		|=Contents.CONTENTS_LAVA;
						mFlags	|=SURF_TRANS66;
						mFlags	|=SURF_LIGHT;
					}
					else if(texName.StartsWith("water") || texName.StartsWith("WATER") || texName.Contains("WAT"))
					{
						ret			|=Contents.CONTENTS_WATER;
						ti.mFlags	|=TexInfo.TRANS;
						mFlags		|=SURF_TRANS66;
					}
					else if(texName.StartsWith("slime") || texName.StartsWith("SLIME"))
					{
						ret			|=Contents.CONTENTS_SLIME;
						ti.mFlags	|=TexInfo.TRANS;
						mFlags		|=SURF_TRANS66;
					}
					else if(texName.StartsWith("glass") || texName.StartsWith("GLASS"))
					{
						ret			|=Contents.CONTENTS_WINDOW;
						mFlags		|=SURF_TRANS66;
						ti.mFlags	|=TexInfo.TRANS;
					}
					else
					{
						//generic transparent I guess
						ret			|=Contents.CONTENTS_TRANSLUCENT;
						ti.mFlags	|=TexInfo.TRANS;
					}
					continue;
				}
				else if(tok[0] == '+')
				{
					//animating I think
					texName		=tok;

					//these and perhaps some others could use
					//a sort of emissive material flag, where
					//the bright parts of the texture emit light and glow
				}
				else if(tok.StartsWith("sky") || tok.StartsWith("SKY"))
				{
					texName		=tok;
					mFlags		|=SURF_SKY;
					ti.mFlags	|=TexInfo.NO_LIGHTMAP;
					ti.mFlags	|=TexInfo.SKY;
					mFlags		|=SURF_LIGHT;
				}
				else if(tok.StartsWith("lava") || tok.StartsWith("LAVA"))
				{
					ret		|=Contents.CONTENTS_LAVA;
					mFlags	|=SURF_TRANS66;
					mFlags	|=SURF_LIGHT;
				}
				else if(tok.StartsWith("water") || tok.StartsWith("WATER"))
				{
					ret			|=Contents.CONTENTS_WATER;
					ti.mFlags	|=TexInfo.TRANS;
					mFlags		|=SURF_TRANS66;
				}
				else if(tok.StartsWith("slime") || tok.StartsWith("SLIME"))
				{
					ret			|=Contents.CONTENTS_SLIME;
					ti.mFlags	|=TexInfo.TRANS;
					mFlags		|=SURF_TRANS66;
				}
				else if(tok.StartsWith("trigger") || tok.StartsWith("TRIGGER"))
				{
					ret			|=Contents.CONTENTS_TRIGGER;
					ti.mFlags	|=TexInfo.FULLBRIGHT;
					mFlags		|=SURF_NODRAW;
					ti.mFlags	|=TexInfo.NO_LIGHTMAP;
				}
				else if(tok.StartsWith("window") || tok.StartsWith("WINDOW"))
				{
					ret			|=Contents.CONTENTS_WINDOW;
					mFlags		|=SURF_TRANS66;
					ti.mFlags	|=TexInfo.TRANS;
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

			//see if there are any quake 3 style flags
			//if there are, ignore all the texture name stuff
			if(flags.Count == 3)
			{
				ret		=flags[0];
				mFlags	=flags[1];

				CoreEvents.Print("Quake 3 style flags found " + flags[0] + ", " + flags[1] + "\n");
			}

			GBSPPlane	plane	=new GBSPPlane(mPoly);

			plane.mType	=GBSPPlane.PLANE_ANY;
			plane.Snap();

			mPlaneNum	=pool.FindPlane(plane, out mbFlipSide);

			ti.mShiftU		=numbers[9];
			ti.mShiftV		=numbers[10];
			ti.mDrawScaleU	=numbers[12];
			ti.mDrawScaleV	=numbers[13];
			ti.mTexture		=texName;

			GBSPPlane.TextureAxisFromPlane(plane, out ti.mUVec, out ti.mVVec);

			if(numbers[11] != 0.0f)
			{
				Matrix	texRot	=Matrix.CreateFromAxisAngle(plane.mNormal,
					MathHelper.ToRadians(numbers[11]));

				//rotate tex vecs
				ti.mUVec	=Vector3.TransformNormal(ti.mUVec, texRot);
				ti.mVVec	=Vector3.TransformNormal(ti.mVVec, texRot);
			}

			FixFlags(ref ti, bSlickAsGouraud, bWarpAsMirror);

			mTexInfo	=tiPool.Add(ti);
			return	ret;
		}
		#endregion


		//convert from hammer flags to genesis flags
		internal void FixFlags(ref TexInfo ti, bool bSlickAsGouraud, bool bWarpAsMirror)
		{
			UInt32	hammerFlags	=mFlags;

			//defaults
			mFlags		=SIDE_VISIBLE;
			ti.mAlpha	=1.0f;

			//eventually need to convert these
			//into genesis texinfo stuff
			if((hammerFlags & SURF_ALPHASHADOW) != 0)
			{
			}
			if((hammerFlags & SURF_DUST) != 0)
			{
			}
			if((hammerFlags & SURF_FLOWING) != 0)
			{
			}
			if((hammerFlags & SURF_HINT) != 0)
			{
				mFlags	|=SIDE_HINT;
			}
			if((hammerFlags & SURF_LIGHT) != 0)
			{
				ti.mFlags		|=TexInfo.LIGHT;
				ti.mFlags		|=TexInfo.FULLBRIGHT;
				ti.mFlags		|=TexInfo.NO_LIGHTMAP;
			}
			if((hammerFlags & SURF_LIGHTFILTER) != 0)
			{
			}
			if((hammerFlags & SURF_METALSTEPS) != 0)
			{
			}
			if((hammerFlags & SURF_NODLIGHT) != 0)
			{
			}
			if((hammerFlags & SURF_NODRAW) != 0)
			{
				mFlags	&=~SIDE_VISIBLE;
			}
			if((hammerFlags & SURF_NOLIGHTMAP) != 0)
			{
				ti.mFlags	|=TexInfo.NO_LIGHTMAP;
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
				ti.mFlags	|=TexInfo.NO_LIGHTMAP;
				ti.mFlags	|=TexInfo.SKY;
			}
			if((hammerFlags & SURF_SLICK) != 0)
			{
				if(bSlickAsGouraud)
				{
					ti.mFlags	|=TexInfo.GOURAUD;
					ti.mFlags	|=TexInfo.NO_LIGHTMAP;
				}
				else
				{
					mFlags	|=SIDE_SLIPPERY;
				}
			}
			if((hammerFlags & SURF_TRANS33) != 0)
			{
				ti.mFlags	|=TexInfo.TRANS;
				ti.mAlpha	=0.333f;
			}
			if((hammerFlags & SURF_TRANS66) != 0)
			{
				ti.mFlags	|=TexInfo.TRANS;
				ti.mAlpha	=0.666f;
			}
			if((hammerFlags & SURF_WARP) != 0)
			{
				if(bWarpAsMirror)
				{
					ti.mFlags	|=TexInfo.NO_LIGHTMAP;
					ti.mFlags	|=TexInfo.FLAT;
					ti.mFlags	|=TexInfo.MIRROR;
				}
				else
				{
					//don't really have a warping effect yet
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


		internal void AddToBounds(Bounds bnd)
		{
			mPoly.AddToBounds(bnd);
		}
	}
}