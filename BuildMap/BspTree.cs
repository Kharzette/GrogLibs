using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace BuildMap
{
	class BspTree
	{
		private	BspNode	mRoot;


		public BspTree(List<Brush> brushList)
		{
			foreach(Brush b in brushList)
			{
				//check for null volumes
			}

			mRoot	=new BspNode();

			mRoot.BuildTree(brushList);
		}
	}
}
