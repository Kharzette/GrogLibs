using System;
using System.Collections.Generic;
using System.Xml;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	public class Translate : NodeElement
	{
		public Vector3	mValue;


		public Translate(XmlReader r)
		{
			int attcnt2	=r.AttributeCount;
			
			if(attcnt2 > 0)
			{
				//grab SID
				mSID	=r.GetAttribute(0);

				//skip to the next element, the actual value
				r.Read();

				Collada.GetVectorFromString(r.Value, out mValue);
			}
		}


		public override Type GetAnimatorType(string addr)
		{
			if(addr == "X")
			{
				return	typeof(TransXAnim);
			}
			else if(addr == "Y")
			{
				return	typeof(TransYAnim);
			}
			else if(addr == "Z")
			{
				return	typeof(TransZAnim);
			}
			Debug.Assert(addr != "bad addr");
			return	typeof(TransXAnim);
		}


		public override Matrix GetMatrix()
		{
			return	Matrix.CreateTranslation(mValue);
		}
	}
}