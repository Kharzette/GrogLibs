using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	public class Controller
	{
		private Skin			mSkin;
		private List<Matrix>	mBones	=new List<Matrix>();

		private Dictionary<string, Int32>	mBIdxMap	=new Dictionary<string,int>();


		public void Load(XmlReader r)
		{
			//read controller stuff
			while(r.Read())
			{
				if(r.NodeType == XmlNodeType.Whitespace)
				{
					continue;	//skip whitey
				}

				if(r.Name == "controller")
				{
					return;
				}
				else if(r.Name == "skin")
				{
					mSkin	=new Skin();
					mSkin.Load(r);
				}
			}
		}


		public Skin	GetSkin()
		{
			return	mSkin;
		}		
	}
}