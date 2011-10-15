using System;
using System.ComponentModel;


namespace Microsoft.Xna.Framework
{
	[Serializable]
	public struct Quaternion
	{
		public float X;
		public float Y;
		public float Z;
		public float W;
		static Quaternion identity = new Quaternion(0, 0, 0, 1);
		
		public Quaternion(float x, float y, float z, float w)
		{
			this.X = x;
			this.Y = y;
			this.Z = z;
			this.W = w;
		}
		
		public Quaternion(Vector3 vectorPart, float scalarPart)
		{
			this.X = vectorPart.X;
			this.Y = vectorPart.Y;
			this.Z = vectorPart.Z;
			this.W = scalarPart;
		}

		public static Quaternion Identity
		{
			get{ return identity; }
		}
	}
}
