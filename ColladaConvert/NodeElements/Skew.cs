using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	public class Skew : NodeElement
	{
		float	mAngle;
		Vector3	mAxisRotation, mAxisTranslation;


		public Skew(XmlReader r)
		{
			int attcnt2	=r.AttributeCount;
			
			if(attcnt2 > 0)
			{
				//grab SID
				mSID	=r.GetAttribute(0);

				//skip to the next element, the actual value
				r.Read();

				Collada.GetSkewFromString(r.Value, out mAngle, out mAxisRotation, out mAxisTranslation);
			}
		}


		public override Type GetAnimatorType(string addr)
		{
			throw new NotImplementedException();
		}


		public override Matrix GetMatrix()
		{
			return	Matrix.Identity;	//TODO: implement this
		}
	}
}