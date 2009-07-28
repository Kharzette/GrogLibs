using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	public class Vertices
	{
		private List<Input>	mInputs	=new List<Input>();


		public string GetPositionKey()
		{
			foreach(Input inp in mInputs)
			{
				if(inp.IsPosition())
				{
					return	inp.GetKey();
				}
			}
			return	"";
		}


		public void Load(XmlReader r)
		{
			while(r.Read())
			{
				if(r.NodeType == XmlNodeType.Whitespace)
				{
					continue;
				}

				if(r.Name == "input")
				{
					Input	inp	=new Input();
					inp.Load(r);
					mInputs.Add(inp);
				}
				else if(r.Name == "vertices")
				{
					return;
				}
			}
		}
	}
}