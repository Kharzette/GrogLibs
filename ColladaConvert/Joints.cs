using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	public class Joints
	{
		private List<Input>	mInputs	=new List<Input>();


		public string GetJointKey()
		{
			foreach(Input inp in mInputs)
			{
				if(inp.IsJoint())
				{
					return	inp.GetKey();
				}
			}
			return	null;
		}


		public string GetInverseBindPosesKey()
		{
			foreach(Input inp in mInputs)
			{
				if(inp.IsInverseBindMatrix())
				{
					return	inp.GetKey();
				}
			}
			return	null;
		}


		public void Load(XmlReader r)
		{
			while(r.Read())
			{
				if(r.NodeType == XmlNodeType.Whitespace)
				{
					continue;	//skip whitey
				}
				if(r.Name == "input")
				{
					if(r.NodeType == XmlNodeType.Element)
					{
						Input	inp	=new Input();
						inp.Load(r);
						mInputs.Add(inp);
					}
				}
				else if(r.Name == "joints")
				{
					return;
				}
			}
		}
	}
}