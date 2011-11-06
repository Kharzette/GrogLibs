using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;


namespace BSPCore
{
	internal class GBSPVisPortal
	{
		internal GBSPVisPortal	mNext;
		internal GBSPPoly		mPoly;
		internal GBSPPlane		mPlane;
		internal Vector3		mCenter;
		internal float			mRadius;

		internal byte	[]mVisBits;
		internal byte	[]mFinalVisBits;
		internal Int32	mPortNum;		//index into portal array or portal num for vis
		internal Int32	mLeaf;
		internal Int32	mMightSee;
		internal Int32	mCanSee;
		internal bool	mbDone;


		internal void CalcPortalInfo()
		{
			mCenter	=mPoly.Center();
			mRadius	=mPoly.Radius();
		}


		internal void Read(BinaryReader br, List<int> indexes)
		{
			int	idx	=br.ReadInt32();
			indexes.Add(idx);

			mPoly	=new GBSPPoly(0);
			mPoly.Read(br);
			mPlane.Read(br);
			mCenter.X	=br.ReadSingle();
			mCenter.Y	=br.ReadSingle();
			mCenter.Z	=br.ReadSingle();
			mRadius		=br.ReadSingle();

			int	vblen	=br.ReadInt32();
			if(vblen > 0)
			{
				mVisBits	=br.ReadBytes(vblen);
			}

			int	fvblen	=br.ReadInt32();
			if(fvblen > 0)
			{
				mFinalVisBits	=br.ReadBytes(fvblen);
			}

			mPortNum	=br.ReadInt32();
			mLeaf		=br.ReadInt32();
			mMightSee	=br.ReadInt32();
			mCanSee		=br.ReadInt32();
			mbDone		=br.ReadBoolean();
		}


		internal void Write(BinaryWriter bw)
		{
			if(mNext != null)
			{
				bw.Write(mNext.mPortNum);
			}
			else
			{
				bw.Write(-1);
			}
			mPoly.Write(bw);
			mPlane.Write(bw);
			bw.Write(mCenter.X);
			bw.Write(mCenter.Y);
			bw.Write(mCenter.Z);
			bw.Write(mRadius);

			if(mVisBits == null)
			{
				bw.Write(-1);
			}
			else
			{
				bw.Write(mVisBits.Length);
				if(mVisBits.Length > 0)
				{
					bw.Write(mVisBits, 0, mVisBits.Length);
				}
			}

			if(mFinalVisBits == null)
			{
				bw.Write(-1);
			}
			else
			{
				bw.Write(mFinalVisBits.Length);
				if(mFinalVisBits.Length > 0)
				{
					bw.Write(mFinalVisBits, 0, mFinalVisBits.Length);
				}
			}

			bw.Write(mPortNum);
			bw.Write(mLeaf);
			bw.Write(mMightSee);
			bw.Write(mCanSee);
			bw.Write(mbDone);
		}
	}
}
