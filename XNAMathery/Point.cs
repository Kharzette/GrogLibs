using System;
using System.ComponentModel;

namespace Microsoft.Xna.Framework
{
	[Serializable]
	public struct Point : IEquatable<Point>
	{
		#region Private Fields

		private static Point zeroPoint = new Point();

		#endregion Private Fields


		#region Public Fields

		public int X;
		public int Y;

		#endregion Public Fields


		#region Properties

		public static Point Zero
		{
			get { return zeroPoint; }
		}

		#endregion Properties


		#region Constructors

		public Point(int x, int y)
		{
			this.X = x;
			this.Y = y;
		}

		#endregion Constructors


		#region Public methods

		public static bool operator ==(Point a, Point b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(Point a, Point b)
		{
			return !a.Equals(b);
		}

		public bool Equals(Point other)
		{
			return ((X == other.X) && (Y == other.Y));
		}
		
		public override bool Equals(object obj)
		{
			return (obj is Point) ? Equals((Point)obj) : false;
		}

		public override int GetHashCode()
		{
			return X + Y;
		}

		public override string ToString()
		{
			return string.Format("{{X:{0} Y:{1}}}", X, Y);
		}

		#endregion
	}
}


