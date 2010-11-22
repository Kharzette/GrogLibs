using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;


namespace BSPLib
{
	public class Portal
	{
		public BspFlatNode	mOnNode, mFront, mBack;
		public Face			mFace;


		//returns true if actually split
		internal bool Split(Plane splitter, out Portal front, out Portal back)
		{
			int	pointsFront, pointsBack, pointsOn;
			mFace.GetSplitInfo(splitter, out pointsFront, out pointsBack, out pointsOn);

			if(pointsFront == 0 || pointsBack == 0)
			{
				front	=back	=null;
				return	false;
			}

			front			=new Portal();
			front.mFace		=new Face(mFace);
			front.mBack		=mBack;
			front.mFront	=mFront;
			front.mOnNode	=mOnNode;

			front.mFace.ClipByPlane(splitter, true, true);
			if(front.mFace.IsTiny())
			{
				front	=null;
			}

			back			=new Portal();
			back.mFace		=new Face(mFace);
			back.mBack		=mBack;
			back.mFront		=mFront;
			back.mOnNode	=mOnNode;
			back.mFace.ClipByPlane(splitter, false, true);
			if(back.mFace.IsTiny())
			{
				back	=null;
			}

			return	true;
		}
	}
}
