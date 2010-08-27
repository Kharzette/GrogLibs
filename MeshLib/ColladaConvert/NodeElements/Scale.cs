using System;
using System.Collections.Generic;
using System.Xml;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	public class Scale : NodeElement
	{
		public Vector3	mValue;


		public Scale(XmlReader r)
		{
			int attcnt2	=r.AttributeCount;
			
			if(attcnt2 > 0)
			{
				//grab SID
				mSID	=r.GetAttribute(0);
			}

			//skip to the next element, the actual value
			r.Read();

			Collada.GetVectorFromString(r.Value, out mValue);
		}


		public override Type GetAnimatorType(string addr)
		{
			if(addr == "X")
			{
				return	typeof(ScaleXAnim);
			}
			else if(addr == "Y")
			{
				return	typeof(ScaleYAnim);
			}
			else if(addr == "Z")
			{
				return	typeof(ScaleZAnim);
			}
			Debug.Assert(addr != "bad addr");
			return	typeof(ScaleXAnim);
		}


		public override Matrix GetMatrix()
		{
			return	Matrix.CreateScale(mValue);
		}
	}
}