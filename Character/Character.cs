using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Character
{
	public class Character
	{
		List<Mesh>	mMeshParts	=new List<Mesh>();
		List<Skin>	mSkins		=new List<Skin>();

		//refs to anim and material libs
		MaterialLib	mMatLib;
		AnimLib		mAnimLib;
	}
}