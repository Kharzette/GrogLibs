using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	public class Animator
	{
		//I think this class will need to store all the
		//animation channels per anim, and somehow we need
		//to figure out a way to distinguish different animations.
		//Also need to store the skeleton I think
		//
		//The basic idea behind this right now is to give me the
		//skeleton at time t
		private	Dictionary<string, List<Anim>>	mAnims	=new Dictionary<string, List<Anim>>();


		public Animator(Dictionary<string, Animation> anims, Dictionary<string, SceneNode> roots)
		{
			foreach(KeyValuePair<string, Animation> an in anims)
			{
				List<Anim>	alist	=an.Value.GetAnims(roots);

				mAnims.Add(an.Key, alist);
			}
		}


		public void AnimateAll(float time)
		{
			foreach(KeyValuePair<string, List<Anim>> anlist in mAnims)
			{
				foreach(Anim an in anlist.Value)
				{
					an.Animate(time);
				}
			}
		}


		public void Animate(string name, float time)
		{
			if(mAnims.ContainsKey(name))
			{
				foreach(Anim an in mAnims[name])
				{
					an.Animate(time);
				}
			}
		}
	}
}