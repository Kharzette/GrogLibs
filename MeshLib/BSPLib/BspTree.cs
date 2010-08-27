using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BSPLib
{
	public class BspTree
	{
		//tree root
		BspNode		mRoot;
		List<Face>	mPortals;
//		List<Brush>	mTempBrushList;	//debuggery remove


		#region Constructors
		public BspTree(List<Brush> brushList, float bevelDistance, bool bBevel)
		{
			//ensure no overlap
			RemoveOverlap(brushList);

/*			mTempBrushList	=new List<Brush>();

			foreach(Brush b in brushList)
			{
				Brush copy	=new Brush(b);

				mTempBrushList.Add(copy);
			}*/
			
			//build a tree
			mRoot	=new BspNode();
			mRoot.BuildTree(brushList);

			MarkPortals();

			if(bBevel)
			{
				mRoot.BevelObtuse(bevelDistance);
			}
		}


		public BspTree()
		{
			mRoot	=new BspNode();
		}
		#endregion


		#region Queries
		public Bounds GetBounds()
		{
			Bounds	bnd	=new Bounds();

			mRoot.AddToBounds(ref bnd);

			return	bnd;
		}


		public bool ClassifyPoint(Vector3 pnt)
		{
			return	mRoot.ClassifyPoint(pnt);
		}


		internal int GetNumPortals()
		{
			return	mPortals.Count;
		}


		public BspNode GetRoot()
		{
			return	mRoot;
		}


		public void GetTriangles(List<Vector3> verts, List<UInt16> indexes)
		{
/*			foreach(Brush b in mTempBrushList)
			{
				b.GetTriangles(verts, indexes);
			}
			mRoot.GetTriangles(verts, indexes);
			*/
			
			foreach(Face port in mPortals)
			{
				port.GetTriangles(verts, indexes);
			}
			
		}


		void GetPlanes(List<Plane> planes)
		{
			mRoot.GetPlanes(planes);
		}
		#endregion


		#region IO
		public void Write(BinaryWriter bw)
		{
			mRoot.Write(bw);
		}


		public void Read(BinaryReader br)
		{
			mRoot.Read(br);
		}
		#endregion


		void MarkPortals()
		{
			List<Plane>	planes	=new List<Plane>();

			GetPlanes(planes);

			//first phase
			foreach(Plane p in planes)
			{
				Face	f	=new Face(p, null);
				mRoot.MarkPortal(f);
			}

			List<Face>	portals	=new List<Face>();
			mRoot.GetPortals(portals);

			List<Face>	newPortals	=new List<Face>();

			//see which survive
			foreach(Face f in portals)
			{
				mRoot.MergePortal(f, newPortals);
			}
			portals.Clear();

			mRoot.GetPortals(portals);

			mPortals	=portals;
			mPortals.AddRange(newPortals);
		}


		//makes sure that only one volume
		//occupies any space
		public static void RemoveOverlap(List<Brush> brushes)
		{
			for(int i=0;i < brushes.Count;i++)
			{
				for(int j=0;j < brushes.Count;j++)
				{
					if(i == j)
					{
						continue;
					}

					if(!brushes[i].Intersects(brushes[j]))
					{
						continue;
					}

					List<Brush>	cutup	=new List<Brush>();
					List<Brush>	cutup2	=new List<Brush>();

					if(brushes[i].SubtractBrush(brushes[j], out cutup))
					{
						//make sure the brush returned is
						//not the one passed in
						if(cutup.Count == 1)
						{
							Debug.Assert(!brushes[i].Equals(cutup[0]));
						}
					}
					else
					{
						cutup.Clear();
					}

					if(brushes[j].SubtractBrush(brushes[i], out cutup2))
					{
						//make sure the brush returned is
						//not the one passed in
						if(cutup2.Count == 1)
						{
							Debug.Assert(!brushes[j].Equals(cutup2[0]));
						}
					}
					else
					{
						cutup2.Clear();
					}

					if(cutup.Count==0 && cutup2.Count==0)
					{
						continue;
					}

					if(cutup.Count > 4 && cutup2.Count > 4)
					{
						continue;
					}

					if(cutup.Count < cutup2.Count)
					{
						cutup2.Clear();

						foreach(Brush b in cutup)
						{
							if(b.IsValid())
							{
								brushes.Add(b);
							}
						}
						cutup.Clear();
						brushes.RemoveAt(i);
						i--;
						break;
					}
					else
					{
						cutup.Clear();

						foreach(Brush b in cutup2)
						{
							if(b.IsValid())
							{
								brushes.Add(b);
							}
						}
						cutup2.Clear();
						brushes.RemoveAt(j);
						j--;
						continue;
					}
				}
			}

			//make sure no overlap was skipped
			for(int i=0;i < brushes.Count;i++)
			{
				for(int j=0;j < brushes.Count;j++)
				{
					if(i == j)
					{
						continue;
					}

					if(brushes[i].Intersects(brushes[j]))
					{
						Debug.WriteLine("Overlap not entirely eliminated!");
					}
				}
			}

			//nuke thins and invalids
			for(int i=0;i < brushes.Count;i++)
			{
				Brush	b	=brushes[i];

				if(!b.IsValid())
				{
					Debug.WriteLine("Brush totally clipped away");
					brushes.RemoveAt(i);
					i--;
					continue;
				}

				if(b.IsVeryThin())
				{
					b.RemoveVeryThinSides();
					if(!b.IsValid())
					{
						Debug.WriteLine("Brush totally clipped away");
						brushes.RemoveAt(i);
						i--;
					}
				}
			}
		}
	}
}
