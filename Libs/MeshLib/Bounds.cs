using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;


namespace MeshLib
{
	public interface IRayCastable
	{
		float?	RayIntersect(Vector3 start, Vector3 end);
		void	AddPointListToBounds(List<Vector3> points);
		void	Write(BinaryWriter bw);
		void	Read(BinaryReader br);
		void	GetMinMax(out Vector3 min, out Vector3 max);
	}


	public class CollisionEventArgs : EventArgs
	{
		public float	mDistance;

		public CollisionEventArgs(float dist)
		{
			mDistance	=dist;
		}
	}


	public class SphereBounds : IRayCastable
	{
		public BoundingSphere	mSphere	=new BoundingSphere();


		public void AddPointListToBounds(List<Vector3> points)
		{
			//find the center
			Vector3	center	=Vector3.Zero;
			foreach(Vector3 pnt in points)
			{
				center	+=pnt;
			}
			center	/=points.Count;

			//find radius
			float	radius	=0.0f;
			foreach(Vector3 pnt in points)
			{
				float	dist	=Vector3.Distance(pnt, center);
				if(dist > radius)
				{
					radius	=dist;
				}
			}

			mSphere.Center	=center;
			mSphere.Radius	=radius;
		}


		public float? RayIntersect(Vector3 start, Vector3 end)
		{
			Ray	r	=new Ray();

			Vector3	dir	=end - start;

			dir.Normalize();

			r.Direction	=dir;
			r.Position	=start;

			return	r.Intersects(mSphere);
		}


		public void Write(BinaryWriter bw)
		{
			bw.Write(mSphere.Center.X);
			bw.Write(mSphere.Center.Y);
			bw.Write(mSphere.Center.Z);
			bw.Write(mSphere.Radius);
		}


		public void Read(BinaryReader br)
		{
			mSphere.Center.X	=br.ReadSingle();
			mSphere.Center.Y	=br.ReadSingle();
			mSphere.Center.Z	=br.ReadSingle();
			mSphere.Radius		=br.ReadSingle();
		}


		public void GetMinMax(out Vector3 min, out Vector3 max)
		{
			min	=Vector3.One;
			max	=Vector3.One;

			min	*=-mSphere.Radius;
			max	*=mSphere.Radius;

			min	+=mSphere.Center;
			max	+=mSphere.Center;
		}
	}


	public class AxialBounds : IRayCastable
	{
		public BoundingBox	mBox	=new BoundingBox();


		public void ClearBounds()
		{
			mBox.Max	=Vector3.Zero;
			mBox.Min	=Vector3.Zero;
		}


		public void AddPointListToBounds(List<Vector3> points)
		{
			foreach(Vector3 pnt in points)
			{
				if(pnt.X < mBox.Min.X)
				{
					mBox.Min.X	=pnt.X;
				}
				if(pnt.X > mBox.Max.X)
				{
					mBox.Max.X	=pnt.X;
				}
				if(pnt.Y < mBox.Min.Y)
				{
					mBox.Min.Y	=pnt.Y;
				}
				if(pnt.Y > mBox.Max.Y)
				{
					mBox.Max.Y	=pnt.Y;
				}
				if(pnt.Z < mBox.Min.Z)
				{
					mBox.Min.Z	=pnt.Z;
				}
				if(pnt.Z > mBox.Max.Z)
				{
					mBox.Max.Z	=pnt.Z;
				}
			}
		}


		public float? RayIntersect(Vector3 start, Vector3 end)
		{
			Ray	r	=new Ray();

			Vector3	dir	=end - start;

			dir.Normalize();

			r.Direction	=dir;
			r.Position	=start;

			return	r.Intersects(mBox);
		}


		public void Write(BinaryWriter bw)
		{
			bw.Write(mBox.Min.X);
			bw.Write(mBox.Min.Y);
			bw.Write(mBox.Min.Z);
			bw.Write(mBox.Max.X);
			bw.Write(mBox.Max.Y);
			bw.Write(mBox.Max.Z);
		}


		public void Read(BinaryReader br)
		{
			mBox.Min.X	=br.ReadSingle();
			mBox.Min.Y	=br.ReadSingle();
			mBox.Min.Z	=br.ReadSingle();
			mBox.Max.X	=br.ReadSingle();
			mBox.Max.Y	=br.ReadSingle();
			mBox.Max.Z	=br.ReadSingle();
		}


		public void GetMinMax(out Vector3 min, out Vector3 max)
		{
			min	=mBox.Min;
			max	=mBox.Max;
		}
	}
}
