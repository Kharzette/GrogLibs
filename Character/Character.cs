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


		public Character(MaterialLib ml, AnimLib al)
		{
			mMatLib		=ml;
			mAnimLib	=al;
		}


		public void AddMeshPart(Mesh m)
		{
			mMeshParts.Add(m);
		}


		public void AddSkin(Skin s)
		{
			mSkins.Add(s);
		}

		public void Animate(string anim, float time)
		{
			mAnimLib.Animate(anim, time);

			foreach(Mesh m in mMeshParts)
			{
				m.UpdateBones(mAnimLib.GetSkeleton());
			}
		}


		public void Draw(GraphicsDevice gd)
		{
			foreach(Mesh m in mMeshParts)
			{
				m.Draw(gd, mMatLib);
			}
		}
	}
}