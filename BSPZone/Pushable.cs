using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using UtilityLib;


namespace BSPZone
{
	internal class Pushable
	{
		internal object			mContext;
		internal BoundingBox	mBox;
		internal Vector3		mWorldCenter;
		internal int			mModelOn;


		internal Pushable(object context, BoundingBox box, Vector3 worldCenter, int modelOn)
		{
			mContext		=context;
			mBox			=box;
			mWorldCenter	=worldCenter;
			mModelOn		=modelOn;
		}
	}
}
