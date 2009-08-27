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

		
		public void ChangeCoordinateSystemMAX()
		{
			//grab the list of bones from the skin
			List<string>	jointNames	=mSkin.GetJointNameArray();

			//grab the inverse bind pose matrix list
			List<Matrix>	ibps	=mSkin.GetInverseBindPoses();

			for(int i=0;i < ibps.Count;i++)
			{
				ibps[i]	=Collada.ConvertMatrixCoordinateSystemMAX(ibps[i]);
			}

			mSkin.ConvertBindShapeMatrixCoordinateSystemMAX();
		}
	}
}