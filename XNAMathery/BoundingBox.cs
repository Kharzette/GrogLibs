using System;

namespace Microsoft.Xna.Framework
{
	public struct BoundingBox
	{
		public const int CornerCount = 8;
		public Vector3 Min;
		public Vector3 Max;


		#region Constructors

		public BoundingBox(Vector3 min, Vector3 max)
		{
			this.Min = min;
			this.Max = max;
		}

		public static BoundingBox CreateMerged(BoundingBox original, BoundingBox additional)
		{
			return new BoundingBox(Vector3.Min(original.Min, additional.Min), Vector3.Max(original.Max, additional.Max));
		}

		public static void CreateMerged(ref BoundingBox original, ref BoundingBox additional, out BoundingBox result)
		{
			result.Min = Vector3.Min(original.Min, additional.Min);
			result.Max = Vector3.Max(original.Max, additional.Max);
		}

		#endregion


		#region Get Corners

		public Vector3[] GetCorners()
		{
			Vector3[] retval = new Vector3[CornerCount];
			GetCorners(retval);
			return retval;
		}

		public void GetCorners(Vector3[] corners)
		{
			if(corners == null)
				throw new ArgumentNullException("corners");
			if(corners.Length < 8)
				throw new ArgumentOutOfRangeException("corners");

			corners[0] = new Vector3(Min.X, Max.Y, Max.Z);
			corners[1] = new Vector3(Max.X, Max.Y, Max.Z);
			corners[2] = new Vector3(Max.X, Min.Y, Max.Z);
			corners[3] = new Vector3(Min.X, Min.Y, Max.Z);
			corners[4] = new Vector3(Min.X, Max.Y, Min.Z);
			corners[5] = new Vector3(Max.X, Max.Y, Min.Z);
			corners[6] = new Vector3(Max.X, Min.Y, Min.Z);
			corners[7] = new Vector3(Min.X, Min.Y, Min.Z);
		}

		#endregion


		#region Intersection Tests

		#region With BoundingBox

		public bool Intersects(BoundingBox box)
		{
			if(Max.X < box.Min.X || Min.X > box.Max.X
			|| Max.Y < box.Min.Y || Min.Y > box.Max.Y
			|| Max.Z < box.Min.Z || Min.Z > box.Max.Z)
			{
				return false;
			}
			else
				return true;
		}

		public void Intersects(ref BoundingBox box, out bool result)
		{
			result = Intersects(box);
		}

		public ContainmentType Contains(BoundingBox box)
		{
			if(!Intersects(box))
				return ContainmentType.Disjoint;
			if(Min.X <= box.Min.X && box.Max.X <= Max.X
			&& Min.Y <= box.Min.Y && box.Max.Y <= Max.Y
			&& Min.Z <= box.Min.Z && box.Max.Z <= Max.Z)
			{
				return ContainmentType.Contains;
			}
			else
				return ContainmentType.Intersects;
		}

		public void Contains(ref BoundingBox box, out ContainmentType result)
		{
			result = Contains(box);
		}

		#endregion

		#region With Vector3

		public ContainmentType Contains(Vector3 point)
		{
			if(Max.X < point.X || Min.X > point.X
			|| Max.Y < point.Y || Min.Y > point.Y
			|| Max.Z < point.Z || Min.Z > point.Z)
			{
				return ContainmentType.Disjoint;
			}
			else
				return ContainmentType.Contains;
		}

		public void Contains(ref Vector3 point, out ContainmentType result)
		{
			result = Contains(point);
		}

		#endregion

		#endregion


		#region Object and Equality

		public override int GetHashCode()
		{
			return Min.GetHashCode() + Max.GetHashCode();
		}

		public override string ToString()
		{
			return "{Min:" + Min.ToString() + " Max:" + Max.ToString() + "}";
		}

		public override bool Equals(object obj)
		{
			if(obj is BoundingBox)
				return Equals((BoundingBox)obj);
			else
				return false;
		}

		public bool Equals(BoundingBox other)
		{
			return Min == other.Min && Max == other.Max;
		}

		public static bool operator==(BoundingBox a, BoundingBox b)
		{
			return a.Min == b.Min && a.Max == b.Max;
		}

		public static bool operator!=(BoundingBox a, BoundingBox b)
		{
			return a.Min != b.Min || a.Max != b.Max;
		}

		#endregion

	}
}
