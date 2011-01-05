using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.ComponentModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using MeshLib;


namespace ColladaConvert
{
	public class AnimGridModel : BindingList<MeshLib.Anim>
	{
		float	mScrollSpeed;	//scroll speed for the layer


		public float ScrollSpeed
		{
			get { return mScrollSpeed; }
			set { mScrollSpeed = value; }
		}


		public AnimGridModel(List<MeshLib.Anim> anms)
		{
			mScrollSpeed = 1.0f;	//default

			foreach(MeshLib.Anim an in anms)
			{
				Add(an);
			}
		}
	}
}
