using System;
using System.IO;
using System.Numerics;
using System.Collections.Generic;
using Vortice.Mathematics;
using UtilityLib;


namespace BSPCore;

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


	//groglibs with the quark addon
	internal UInt32 ReadMapLineGrog(string szLine,
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

		Vector3	planePos0	=new Vector3(numbers[0], numbers[1], numbers[2]);
		Vector3	planePos1	=new Vector3(numbers[3], numbers[4], numbers[5]);
		Vector3	planePos2	=new Vector3(numbers[6], numbers[7], numbers[8]);

		Mathery.TransformCoordinate(planePos0, ref Map.mGrogTransform, out planePos0);
		Mathery.TransformCoordinate(planePos1, ref Map.mGrogTransform, out planePos1);
		Mathery.TransformCoordinate(planePos2, ref Map.mGrogTransform, out planePos2);

		mPoly	=new GBSPPoly(planePos0, planePos1, planePos2);

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
				ti.mAlpha	=Math.Clamp(numbers[14], 0f, 1f);

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

		GBSPPlane.TextureAxisFromPlaneGrog(plane.mNormal, out ti.mUVec, out ti.mVVec);

		//flip elements
		//TODO: why is this needed?
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

			Matrix4x4	texRot	=Matrix4x4.CreateFromAxisAngle(
				plane.mNormal, MathHelper.ToRadians(rot));

			//rotate tex vecs
			ti.mUVec	=Vector3.TransformNormal(ti.mUVec, texRot);
			ti.mVVec	=Vector3.TransformNormal(ti.mVVec, texRot);
		}

		FixFlags(ref ti);

		mTexInfo	=tiPool.Add(ti);
		return	ret;
	}


	//for working with old flags
	internal UInt32 ReadMapLineQuake1(string szLine,
		TexInfoPool tiPool,	BSPBuildParams prms)
	{
		UInt32	ret	=0;

		//gank (
		szLine.TrimStart('(');

		szLine.Trim();

		string	[]tokens    =szLine.Split(' ');

		List<float>		numbers =new List<float>();
		List<UInt32>	flags	=new List<UInt32>();
		TexInfo			ti		=new TexInfo();

		//grab all the numbers out
		int 	cnt		=0;
		string	texName	="";
		foreach(string tok in tokens)
		{
			//skip ()
			if(tok[0] == '(' || tok[0] == ')')
			{
				continue;
			}
			//skip comments
			if(tok.StartsWith("//"))
			{
				continue;
			}

			//grab tex name if avail
			if(tok == "clip" || tok == "CLIP")
			{
				texName	=tok;
				ret	|=GrogContents.BSP_CONTENTS_CLIP2;
			}
			if(tok[0] == '*' || tok[0] == '#')
			{
				texName	=tok.Substring(1);
				if(texName.StartsWith("lava") || texName.StartsWith("LAVA"))
				{
					ret			|=GrogContents.BSP_CONTENTS_USER5;
					ret			|=GrogContents.BSP_CONTENTS_EMPTY2;
					ret			|=GrogContents.BSP_CONTENTS_WAVY2;
					ti.mFlags	|=TexInfo.FULLBRIGHT;
					ti.mFlags	|=TexInfo.TRANSPARENT;
					ti.mFlags	|=TexInfo.EMITLIGHT;
					ti.mAlpha	=0.95f;
				}
				else if(texName.Contains("water") || texName.Contains("WATER")
					|| texName.Contains("MWAT") || texName.Contains("mwat"))
				{
					ret			|=GrogContents.BSP_CONTENTS_USER3;
					ret			|=GrogContents.BSP_CONTENTS_EMPTY2;
					ret			|=GrogContents.BSP_CONTENTS_WAVY2;
					ti.mFlags	|=TexInfo.TRANSPARENT;
					ti.mAlpha	=0.75f;
				}
				else if(texName.StartsWith("slime") || texName.StartsWith("SLIME"))
				{
					ret			|=GrogContents.BSP_CONTENTS_USER4;
					ret			|=GrogContents.BSP_CONTENTS_EMPTY2;
					ret			|=GrogContents.BSP_CONTENTS_WAVY2;
					ti.mFlags	|=TexInfo.TRANSPARENT;
					ti.mAlpha	=0.85f;
				}
				else if(texName.StartsWith("glass") || texName.StartsWith("GLASS"))
				{
					ret			|=GrogContents.BSP_CONTENTS_WINDOW2;
					ti.mFlags	|=TexInfo.TRANSPARENT;
					ti.mAlpha	=0.5f;
				}
				else if(texName.StartsWith("teleport") || texName.StartsWith("TELEPORT"))
				{
					ret			|=GrogContents.BSP_CONTENTS_USER7;
					ret			|=GrogContents.BSP_CONTENTS_EMPTY2;
					ret			|=GrogContents.BSP_CONTENTS_WAVY2;
					ti.mFlags	|=TexInfo.FULLBRIGHT;
					ti.mFlags	|=TexInfo.TRANSPARENT;
					ti.mFlags	|=TexInfo.EMITLIGHT;
					ti.mAlpha	=0.75f;
				}
				else
				{
					//breakpoint to check stuff here
					int	gack	=69;
					gack++;
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
			//can probably do without these cases (TODO: test)
			else if(tok.StartsWith("sky") || tok.StartsWith("SKY") || tok.StartsWith("e1u1/sky"))
			{
				texName		=tok;
				ti.mFlags	|=TexInfo.NO_LIGHTMAP;
				ti.mFlags	|=TexInfo.SKY;
			}
			else if(tok.StartsWith("lava") || tok.StartsWith("LAVA"))
			{
				ret			|=GrogContents.BSP_CONTENTS_USER5;
				ret			|=GrogContents.BSP_CONTENTS_EMPTY2;
				ret			|=GrogContents.BSP_CONTENTS_WAVY2;
				ti.mFlags	|=TexInfo.FULLBRIGHT;
				ti.mFlags	|=TexInfo.TRANSPARENT;
				ti.mFlags	|=TexInfo.EMITLIGHT;
				ti.mAlpha	=0.98f;
			}
			else if(tok.Contains("water") || tok.Contains("WATER"))
			{
				ret			|=GrogContents.BSP_CONTENTS_USER3;
				ret			|=GrogContents.BSP_CONTENTS_EMPTY2;
				ret			|=GrogContents.BSP_CONTENTS_WAVY2;
				ti.mFlags	|=TexInfo.TRANSPARENT;
				ti.mAlpha	=0.85f;
			}
			else if(tok.StartsWith("slime") || tok.StartsWith("SLIME"))
			{
				ret			|=GrogContents.BSP_CONTENTS_USER4;
				ret			|=GrogContents.BSP_CONTENTS_EMPTY2;
				ret			|=GrogContents.BSP_CONTENTS_WAVY2;
				ti.mFlags	|=TexInfo.TRANSPARENT;
				ti.mAlpha	=0.95f;
			}
			else if(tok.StartsWith("trigger") || tok.StartsWith("TRIGGER"))
			{
				ret			|=GrogContents.BSP_CONTENTS_TRIGGER;
				ti.mFlags	|=TexInfo.FULLBRIGHT;
				ti.mFlags	|=TexInfo.NO_LIGHTMAP;
			}
			else if(tok.StartsWith("window") || tok.StartsWith("WINDOW"))
			{
				ret			|=GrogContents.BSP_CONTENTS_WINDOW2;
				ti.mFlags	|=TexInfo.TRANSPARENT;
				ti.mAlpha	=0.35f;
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

		//deal with the numbers...
		//Back in the XNA days, x used to be negated to convert to left handed.
		//Now for DX, only a swapping of y and z is needed
		mPoly	=new GBSPPoly(new Vector3(numbers[0], numbers[2], numbers[1]),
			new Vector3(numbers[3], numbers[5], numbers[4]),
			new Vector3(numbers[6], numbers[8], numbers[7]));

		//see if there are any quake 3 style flags
		//if there are, ignore all the texture name stuff
		if(flags.Count == 3)
		{
			CoreEvents.Print("Quake 3 style flags found " + flags[0] + ", " + flags[1] + "\n");
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

		GBSPPlane.TextureAxisFromPlaneQuake1(plane.mNormal, out ti.mUVec, out ti.mVVec);

		if(numbers[11] != 0.0f)
		{
			float	rot	=numbers[11];

			int	axis	=GBSPPlane.GetBestAxisFromPlane(plane);

			//some planes need rotation flipped
			if(axis == 0 || axis == 3 || axis == 5)
			{
				rot	=-rot;
			}

			Matrix4x4	texRot	=Matrix4x4.CreateFromAxisAngle(plane.mNormal,
				MathHelper.ToRadians(rot));

			//rotate tex vecs
			ti.mUVec	=Vector3.TransformNormal(ti.mUVec, texRot);
			ti.mVVec	=Vector3.TransformNormal(ti.mVVec, texRot);
		}

		FixFlags(ref ti);

		mTexInfo	=tiPool.Add(ti);
		return	ret;
	}


	//for working with old flags
	/*
	internal UInt32 ReadMapLineQuake2(string szLine, PlanePool pool,
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
					if(prms.mbLavaEmitLight)
					{
						mFlags	|=SURF_LIGHT;
					}
				}
				else if(texName.Contains("water") || texName.Contains("WATER")
					|| texName.Contains("MWAT") || texName.Contains("mwat"))
				{
					ret			|=Contents.CONTENTS_WATER;
					ti.mFlags	|=TexInfo.TRANSPARENT;
					mFlags		|=SURF_TRANS66;
				}
				else if(texName.StartsWith("slime") || texName.StartsWith("SLIME"))
				{
					ret			|=Contents.CONTENTS_SLIME;
					ti.mFlags	|=TexInfo.TRANSPARENT;
					mFlags		|=SURF_TRANS66;
				}
				else if(texName.StartsWith("glass") || texName.StartsWith("GLASS"))
				{
					if(prms.mbWindowTransparent)
					{
						ret			|=Contents.CONTENTS_WINDOW;
						mFlags		|=SURF_TRANS66;
						ti.mFlags	|=TexInfo.TRANSPARENT;
					}
					if(prms.mbWindowEmitLight)
					{
						mFlags	|=SURF_LIGHT;
					}
				}
				else if(texName.StartsWith("teleport") || texName.StartsWith("TELEPORT"))
				{
					ret			|=Contents.CONTENTS_AUX;	//using aux for teleport
					ti.mFlags	|=TexInfo.FULLBRIGHT;
					ti.mFlags	|=TexInfo.NO_LIGHTMAP;
				}
				else
				{
					//guessing this is a mist content?
					ret			|=Contents.CONTENTS_MIST;
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
			//can probably do without these cases (TODO: test)
			else if(tok.StartsWith("sky") || tok.StartsWith("SKY") || tok.StartsWith("e1u1/sky"))
			{
				texName		=tok;
				mFlags		|=SURF_SKY;
				ti.mFlags	|=TexInfo.NO_LIGHTMAP;
				ti.mFlags	|=TexInfo.SKY;
				if(prms.mbSkyEmitLight)
				{
					mFlags		|=SURF_LIGHT;
				}
			}
			else if(tok.StartsWith("lava") || tok.StartsWith("LAVA"))
			{
				ret		|=Contents.CONTENTS_LAVA;
				mFlags	|=SURF_TRANS66;
				if(prms.mbLavaEmitLight)
				{
					mFlags	|=SURF_LIGHT;
				}
			}
			else if(tok.Contains("water") || tok.Contains("WATER"))
			{
				ret			|=Contents.CONTENTS_WATER;
				ti.mFlags	|=TexInfo.TRANSPARENT;
				mFlags		|=SURF_TRANS66;
			}
			else if(tok.StartsWith("slime") || tok.StartsWith("SLIME"))
			{
				ret			|=Contents.CONTENTS_SLIME;
				ti.mFlags	|=TexInfo.TRANSPARENT;
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
				if(prms.mbWindowTransparent)
				{
					ret			|=Contents.CONTENTS_WINDOW;
					mFlags		|=SURF_TRANS66;
					ti.mFlags	|=TexInfo.TRANSPARENT;
				}
				if(prms.mbWindowEmitLight)
				{
					mFlags	|=SURF_LIGHT;
				}
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

		FixFlags(ref ti, prms.mbSlickAsGouraud, prms.mbWarpAsMirror);

		mTexInfo	=tiPool.Add(ti);
		return	ret;
	}


	//for working with old flags
	internal UInt32 ReadMapLineQuake3(string szLine, PlanePool pool,
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
					if(prms.mbLavaEmitLight)
					{
						mFlags	|=SURF_LIGHT;
					}
				}
				else if(texName.Contains("water") || texName.Contains("WATER")
					|| texName.Contains("MWAT") || texName.Contains("mwat"))
				{
					ret			|=Contents.CONTENTS_WATER;
					ti.mFlags	|=TexInfo.TRANSPARENT;
					mFlags		|=SURF_TRANS66;
				}
				else if(texName.StartsWith("slime") || texName.StartsWith("SLIME"))
				{
					ret			|=Contents.CONTENTS_SLIME;
					ti.mFlags	|=TexInfo.TRANSPARENT;
					mFlags		|=SURF_TRANS66;
				}
				else if(texName.StartsWith("glass") || texName.StartsWith("GLASS"))
				{
					if(prms.mbWindowTransparent)
					{
						ret			|=Contents.CONTENTS_WINDOW;
						mFlags		|=SURF_TRANS66;
						ti.mFlags	|=TexInfo.TRANSPARENT;
					}
					if(prms.mbWindowEmitLight)
					{
						mFlags	|=SURF_LIGHT;
					}
				}
				else if(texName.StartsWith("teleport") || texName.StartsWith("TELEPORT"))
				{
					ret			|=Contents.CONTENTS_AUX;	//using aux for teleport
					ti.mFlags	|=TexInfo.FULLBRIGHT;
					ti.mFlags	|=TexInfo.NO_LIGHTMAP;
				}
				else
				{
					//guessing this is a mist content?
					ret			|=Contents.CONTENTS_MIST;
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
			//can probably do without these cases (TODO: test)
			else if(tok.StartsWith("sky") || tok.StartsWith("SKY") || tok.StartsWith("e1u1/sky"))
			{
				texName		=tok;
				mFlags		|=SURF_SKY;
				ti.mFlags	|=TexInfo.NO_LIGHTMAP;
				ti.mFlags	|=TexInfo.SKY;
				if(prms.mbSkyEmitLight)
				{
					mFlags		|=SURF_LIGHT;
				}
			}
			else if(tok.StartsWith("lava") || tok.StartsWith("LAVA"))
			{
				ret		|=Contents.CONTENTS_LAVA;
				mFlags	|=SURF_TRANS66;
				if(prms.mbLavaEmitLight)
				{
					mFlags	|=SURF_LIGHT;
				}
			}
			else if(tok.Contains("water") || tok.Contains("WATER"))
			{
				ret			|=Contents.CONTENTS_WATER;
				ti.mFlags	|=TexInfo.TRANSPARENT;
				mFlags		|=SURF_TRANS66;
			}
			else if(tok.StartsWith("slime") || tok.StartsWith("SLIME"))
			{
				ret			|=Contents.CONTENTS_SLIME;
				ti.mFlags	|=TexInfo.TRANSPARENT;
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
				if(prms.mbWindowTransparent)
				{
					ret			|=Contents.CONTENTS_WINDOW;
					mFlags		|=SURF_TRANS66;
					ti.mFlags	|=TexInfo.TRANSPARENT;
				}
				if(prms.mbWindowEmitLight)
				{
					mFlags	|=SURF_LIGHT;
				}
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

		FixFlags(ref ti, prms.mbSlickAsGouraud, prms.mbWarpAsMirror);

		mTexInfo	=tiPool.Add(ti);
		return	ret;
	}*/
	#endregion


	internal void Free()
	{
		mPoly.Free();
	}

	
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