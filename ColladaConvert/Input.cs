using System;
using System.Collections.Generic;
using System.Xml;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ColladaConvert
{
	public class Input
	{
		private string	mSemantic, mSource;
		private int		mOffset, mSet;


		public Type GetTypeForSemantic()
		{
			switch(mSemantic)
			{
				case "VERTEX":
					return	typeof(Vector3);
				case "NORMAL":
					return	typeof(Vector3);
				case "TEXCOORD":
					return	typeof(Vector2);
				case "COLOR":
					return	typeof(Vector4);
				default:
					Debug.WriteLine("Warning! unknown input semantic!");
					break;
			}
			return	typeof(object);	//wild guess?
		}


		public bool IsJoint()
		{
			return	(mSemantic == "JOINT");
		}


		public bool IsInverseBindMatrix()
		{
			return	(mSemantic == "INV_BIND_MATRIX");
		}


		public bool IsWeight()
		{
			return	(mSemantic == "WEIGHT");
		}


		public bool IsVertex()
		{
			return	(mSemantic == "VERTEX");
		}


		public bool IsTexCoord(int set)
		{
			return	(mSet == set && mSemantic == "TEXCOORD");
		}


		public bool IsNormal()
		{
			return	(mSemantic == "NORMAL");
		}


		public bool IsPosition()
		{
			return	(mSemantic == "POSITION");
		}


		public string GetKey()
		{
			return	mSource;
		}


		public void Load(XmlReader r)
		{
			int	attCnt	=r.AttributeCount;

			r.MoveToFirstAttribute();
			while(attCnt > 0)
			{
				if(r.Name == "semantic")
				{
					mSemantic	=r.Value;
				}
				else if(r.Name == "offset")
				{
					int.TryParse(r.Value, out mOffset);
				}
				else if(r.Name == "source")
				{
					mSource	=r.Value;
				}
				else if(r.Name == "set")	//by Set!
				{
					int.TryParse(r.Value, out mSet);
				}
				r.MoveToNextAttribute();
				attCnt--;
			}
		}
	}
}