using System;
using System.Numerics;
using System.Collections.Generic;


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


	public class Vector2PairEventArgs : EventArgs
	{
		public Vector2	mVecA, mVecB;

		public Vector2PairEventArgs(Vector2 vecA, Vector2 vecB)
		{
			mVecA	=vecA;
			mVecB	=vecB;
		}
	}


	public class Vector3PairEventArgs : EventArgs
	{
		public Vector3	mVecA, mVecB;

		public Vector3PairEventArgs(Vector3 vecA, Vector3 vecB)
		{
			mVecA	=vecA;
			mVecB	=vecB;
		}
	}


	public class Vector4PairEventArgs : EventArgs
	{
		public Vector4	mVecA, mVecB;

		public Vector4PairEventArgs(Vector4 vecA, Vector4 vecB)
		{
			mVecA	=vecA;
			mVecB	=vecB;
		}
	}


	//not really a vector, but whatever
	public class ListEventArgs<T> : EventArgs
	{
		public List<T>	mList;

		public ListEventArgs(object list)
		{
			mList	=(List<T>)list;
		}
	}
}
