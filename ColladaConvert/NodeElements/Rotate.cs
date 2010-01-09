using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	public class Rotate : NodeElement
	{
		public Quaternion	mValue;


		public Rotate(XmlReader r)
		{
			int attcnt2	=r.AttributeCount;
			
			if(attcnt2 > 0)
			{
				//grab SID
				mSID	=r.GetAttribute(0);
			}

			//skip to the next element, the actual value
			r.Read();

			Collada.GetQuaternionFromString(r.Value, out mValue);
		}


		public override Type GetAnimatorType(string addr)
		{
			if(addr == "X")
			{
				return	typeof(RotateXAnim);
			}
			else if(addr == "Y")
			{
				return	typeof(RotateYAnim);
			}
			else if(addr == "Z")
			{
				return	typeof(RotateZAnim);
			}
			else if(addr == "ANGLE")
			{
				return	typeof(RotateWAnim);
			}
			Debug.Assert(addr != "bad addr");
			return	typeof(TransXAnim);
		}


		public override Matrix GetMatrix()
		{
			Vector3	axis;
			axis.X	=mValue.X;
			axis.Y	=mValue.Y;
			axis.Z	=mValue.Z;
			return	Matrix.CreateFromAxisAngle(axis, mValue.W);
//			return	Matrix.CreateFromQuaternion(mQuat);
		}
	}
}