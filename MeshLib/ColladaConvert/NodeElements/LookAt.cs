using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	public class LookAt : NodeElement
	{
		Vector3	mEyePos, mInterestPos, mUpVector;


		public LookAt(XmlReader r)
		{
			int attcnt2	=r.AttributeCount;
			
			if(attcnt2 > 0)
			{
				//grab SID
				mSID	=r.GetAttribute(0);
			}

			//skip to the next element, the actual value
			r.Read();

			Collada.GetLookAtFromString(r.Value, out mEyePos, out mInterestPos, out mUpVector);
		}


		public override Type GetAnimatorType(string addr)
		{
			throw new NotImplementedException();
		}


		public override Matrix GetMatrix()
		{
			return	Matrix.CreateLookAt(mEyePos, mInterestPos, mUpVector);
		}
	}
}