using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	//Matrix nodeElement type
	public class MatrixL : NodeElement
	{
		Matrix	mMat;


		public MatrixL(XmlReader r)
		{
			int attcnt2	=r.AttributeCount;
			
			if(attcnt2 > 0)
			{
				//grab SID
				mSID	=r.GetAttribute(0);

				//skip to the next element, the actual value
				r.Read();

				Collada.GetMatrixFromString(r.Value, out mMat);
			}
		}


		public override Type GetAnimatorType(string addr)
		{
			throw new NotImplementedException();
		}


		public override Matrix GetMatrix()
		{
			return	mMat;
		}
	}
}