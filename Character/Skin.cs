using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Character
{
	public class Skin
	{
		List<string>	mBoneNames			=new List<string>();
		List<Matrix>	mInverseBindPoses	=new List<Matrix>();
		Matrix			mBindShapeMatrix;
	}
}