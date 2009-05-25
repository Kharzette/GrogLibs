using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	public class Param
	{
		public string	mType, mName;


		public void Load(XmlReader r)
		{
			int	attCnt	=r.AttributeCount;
			if(attCnt > 0)
			{
				r.MoveToFirstAttribute();
				while(attCnt > 0)
				{
					if(r.Name == "name")
					{
						mName	=r.Value;
					}
					else if(r.Name == "type")
					{
						mType	=r.Value;
					}
					r.MoveToNextAttribute();
					attCnt--;
				}
			}
		}
	}
}