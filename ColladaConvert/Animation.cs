using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	public class Animation
	{
		private string	mName;

		private List<SubAnimation>	mSubAnims	=new List<SubAnimation>();


		public void Load(XmlReader r)
		{
			r.MoveToNextAttribute();
			mName	=r.Value;
			while(r.Read())
			{
				if(r.NodeType == XmlNodeType.Whitespace)
				{
					continue;
				}

				if(r.Name == "animation")
				{
					if(r.NodeType == XmlNodeType.EndElement)
					{
						return;
					}

					SubAnimation	sub	=new SubAnimation();
					sub.Load(r);
					mSubAnims.Add(sub);
				}
			}
		}
	}
}