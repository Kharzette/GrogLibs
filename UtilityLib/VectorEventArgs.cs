using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;


namespace UtilityLib
{
	public class Vector2EventArgs : EventArgs
	{
		public Vector2	mVector;


		public Vector2EventArgs(Vector2 vec)
		{
			mVector	=vec;
		}
	}


	public class Vector3EventArgs : EventArgs
	{
		public Vector3	mVector;


		public Vector3EventArgs(Vector3 vec)
		{
			mVector	=vec;
		}
	}


	public class Vector4EventArgs : EventArgs
	{
		public Vector4	mVector;


		public Vector4EventArgs(Vector4 vec)
		{
			mVector	=vec;
		}
	}
}
