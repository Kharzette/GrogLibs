using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;


namespace MeshLib
{
	public struct Plane3
	{
		public Vector3	Normal;
		public float	Dist;
	}


	public class Bounds
	{
		public const float	MIN_MAX_BOUNDS	=15192.0f;
		public const float	MinimumVolume	=0.1f;

		public Vector3	mMins, mMaxs;

		List<Plane3>	mVolumePlanes	=new List<Plane3>();


		public Bounds()
		{
			ClearBounds();
		}


		public void ClearBounds()
		{
			mMins.X	=mMins.Y =	mMins.Z	=MIN_MAX_BOUNDS;
			mMaxs	=-mMins;
			mVolumePlanes.Clear();
		}


		public void AddPointToBounds(Vector3 pnt)
		{
			if(pnt.X < mMins.X)
			{
				mMins.X	=pnt.X;
			}
			if(pnt.X > mMaxs.X)
			{
				mMaxs.X	=pnt.X;
			}
			if(pnt.Y < mMins.Y)
			{
				mMins.Y	=pnt.Y;
			}
			if(pnt.Y > mMaxs.Y)
			{
				mMaxs.Y	=pnt.Y;
			}
			if(pnt.Z < mMins.Z)
			{
				mMins.Z	=pnt.Z;
			}
			if(pnt.Z > mMaxs.Z)
			{
				mMaxs.Z	=pnt.Z;
			}
			CalcVolumePlanes();
		}


		public void MergeBounds(Bounds b1, Bounds b2)
		{
			if(b1 != null)
			{
				AddPointToBounds(b1.mMins);
				AddPointToBounds(b1.mMaxs);
			}
			if(b2 != null)
			{
				AddPointToBounds(b2.mMins);
				AddPointToBounds(b2.mMaxs);
			}
			CalcVolumePlanes();
		}


		public bool RayIntersect(Vector3 start, Vector3 end)
		{
			if(mVolumePlanes.Count <= 0)
			{
				return	false;
			}

			Vector3	st	=start;
			Vector3	ed	=end;

			foreach(Plane3 p in mVolumePlanes)
			{
				float	ds	=Vector3.Dot(st, p.Normal) - p.Dist;
				float	de	=Vector3.Dot(ed, p.Normal) - p.Dist;

				if(ds > 0.0f && de > 0.0f)
				{
					return	false;	//all on front
				}
				else if(ds < 0.0f && de < 0.0f)
				{
					continue;	//all on back
				}
				else
				{
					//split, keep only back
					float	splitRatio	=ds / (ds - de);

					if(ds > 0.0f)
					{
						st	=st + (splitRatio * (ed - st));
					}
					else
					{
						ed	=st + (splitRatio * (ed - st));
					}
				}
			}
			return	true;
		}


		void CalcVolumePlanes()
		{
			Vector3	vol	=mMaxs - mMins;

			if(vol.Length() <= MinimumVolume)
			{
				return;	//bail
			}

			mVolumePlanes.Clear();

			//x plane
			Plane3	p	=new Plane3();
			p.Normal	=Vector3.UnitX;
			p.Dist		=Vector3.Dot(mMaxs, p.Normal);
			mVolumePlanes.Add(p);

			//y plane
			p.Normal	=Vector3.UnitY;
			p.Dist		=Vector3.Dot(mMaxs, p.Normal);
			mVolumePlanes.Add(p);

			//z plane
			p.Normal	=Vector3.UnitZ;
			p.Dist		=Vector3.Dot(mMaxs, p.Normal);
			mVolumePlanes.Add(p);

			//-x plane
			p.Normal	=-Vector3.UnitX;
			p.Dist		=Vector3.Dot(mMins, p.Normal);
			mVolumePlanes.Add(p);

			//-y plane
			p.Normal	=-Vector3.UnitY;
			p.Dist		=Vector3.Dot(mMins, p.Normal);
			mVolumePlanes.Add(p);

			//-z plane
			p.Normal	=-Vector3.UnitZ;
			p.Dist		=Vector3.Dot(mMins, p.Normal);
			mVolumePlanes.Add(p);
		}


		internal void Write(BinaryWriter bw)
		{
			bw.Write(mMins.X);
			bw.Write(mMins.Y);
			bw.Write(mMins.Z);
			bw.Write(mMaxs.X);
			bw.Write(mMaxs.Y);
			bw.Write(mMaxs.Z);
		}


		internal void Read(BinaryReader br)
		{
			mMins.X	=br.ReadSingle();
			mMins.Y	=br.ReadSingle();
			mMins.Z	=br.ReadSingle();
			mMaxs.X	=br.ReadSingle();
			mMaxs.Y	=br.ReadSingle();
			mMaxs.Z	=br.ReadSingle();

			CalcVolumePlanes();
		}
	}
}
