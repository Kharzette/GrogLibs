using System;
using System.Collections.Generic;
using System.Xml;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	public class Polygons
	{
		private string		mMaterial;
		private int			mCount;

		private List<Input>					mInputs	=new List<Input>();
		private Dictionary<int, List<int>>	mIndexs	=new Dictionary<int,List<int>>();


		public void Load(XmlReader r)
		{
			int	attCnt	=r.AttributeCount;
			if(attCnt > 0)
			{
				r.MoveToFirstAttribute();
				while(attCnt > 0)
				{
					if(r.Name == "material")
					{
						mMaterial	=r.Value;
					}
					else if(r.Name == "count")
					{
						int.TryParse(r.Value, out mCount);
					}
					r.MoveToNextAttribute();
					attCnt--;
				}

				int	curPoly	=0;

				while(r.Read())
				{
					if(r.NodeType == XmlNodeType.Whitespace)
					{
						continue;	//skip whitey
					}
					if(r.Name == "input")
					{
						Input	inp	=new Input();
						inp.Load(r);
						mInputs.Add(inp);
					}
					else if(r.Name == "p")
					{
						if(r.NodeType == XmlNodeType.EndElement)
						{
							continue;
						}
						List<int>	ind	=new List<int>();

						//go to values
						r.Read();

						string	[]tokens	=r.Value.Split(' ', '\n');
						foreach(string tok in tokens)
						{
							int	i;

							if(int.TryParse(tok, out i))
							{
								ind.Add(i);
							}
						}
						mIndexs.Add(curPoly, ind);
						curPoly++;
					}
					else if(r.Name == "polygons")
					{
						return;
					}
				}
			}
		}
	}
}