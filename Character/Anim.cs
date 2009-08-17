using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Character
{
	public class Anim
	{
		List<SubAnim>	[]mControllerAnims;

		string	mName;		//animation name for the library
		bool	mbLooping;
		bool	mbPingPong;


		public string Name
		{
			get { return mName; }
			set { mName = value; }
		}
		public bool Looping
		{
			get { return mbLooping; }
			set { mbLooping = value; }
		}
		public bool PingPong
		{
			get { return mbPingPong; }
			set { mbPingPong = value; }
		}


		public Anim(int numControllers)
		{
			mControllerAnims	=new List<SubAnim>[numControllers];
		}


		public void AddControllerSubAnims(int cidx, List<SubAnim> anims)
		{
			mControllerAnims[cidx]	=anims;
		}


		public void Animate(int cidx, float time, Skeleton gs)
		{
			List<SubAnim>	subs	=mControllerAnims[cidx];

			foreach(SubAnim an in subs)
			{
				an.Animate(time, gs);
			}
		}
	}
}