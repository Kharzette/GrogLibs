using System;
using System.Collections.Generic;
using System.Text;
using SharpDX;


namespace BSPCore
{
	public class FaceFixer
	{
		List<Vector3>	mWelded			=new List<Vector3>();
		List<Int32>		mTempIndexes	=new List<Int32>();
		List<Int32>		mEdgeVerts		=new List<Int32>();

		Vector3	mEdgeStart, mEdgeDir;

		int	mTotalIndexes;
		int	mNumTJunctions;
		int	mNumFixedFaces;
		int	mIterationCount;

		public const int	MAX_TEMP_INDEX_VERTS	=1024;
		public const float	OFF_EPSILON				=0.05f;


		internal int NumTJunctions
		{
			get { return mNumTJunctions; }
		}
		internal int NumFixedFaces
		{
			get { return mNumFixedFaces; }
		}
		internal int TotalIndexes
		{
			get { return mTotalIndexes; }
		}
		internal int IterationCount
		{
			get { return mIterationCount; }
			set { mIterationCount = value; }
		}


		internal Vector3[] GetWeldedVertArray()
		{
			return	mWelded.ToArray();
		}


		internal Int32[] IndexFaceVerts(Vector3 []verts)
		{
			mTempIndexes.Clear();

			foreach(Vector3 vert in verts)
			{
				if(mTempIndexes.Count >= MAX_TEMP_INDEX_VERTS)
				{
					CoreEvents.Print("IndexFaceVerts:  Max temp index verts.\n");
					return	null;
				}

				int	Index	=WeldVert(vert);
				if(Index == -1)
				{
					CoreEvents.Print("IndexFaceVerts:  Could not find vert.\n");
					return	null;
				}

				mTempIndexes.Add(Index);
				mTotalIndexes++;
			}
			return	mTempIndexes.ToArray();
		}


		internal bool FixTJunctions(ref Int32[] indexVerts, TexInfo tex)
		{
			Int32	i, P1, P2;
			Int32	[]Start	=new Int32[MAX_TEMP_INDEX_VERTS];
			Int32	[]Count	=new Int32[MAX_TEMP_INDEX_VERTS];
			Vector3	Edge2;
			float	Len;
			Int32	Base;

			mTempIndexes.Clear();

			for (i=0; i < indexVerts.Length;i++)
			{
				P1	=indexVerts[i];
				P2	=indexVerts[(i + 1) % indexVerts.Length];

				mEdgeStart	=mWelded[P1];
				Edge2		=mWelded[P2];

				SetEdgeVerts();

				mEdgeDir	=Edge2 - mEdgeStart;

				Len	=mEdgeDir.Length();
				mEdgeDir.Normalize();

				Start[i]	=mTempIndexes.Count;

				TestEdge_r(0.0f, Len, P1, P2, 0);

				Count[i]	=mTempIndexes.Count - Start[i];
			}

			if(mTempIndexes.Count < 3)
			{
				indexVerts	=null;

				CoreEvents.Print("FixTJunctions:  Face collapsed.\n");
				return	true;
			}

			for(i=0;i < indexVerts.Length;i++)
			{
				if(Count[i] == 1 &&
					Count[(i + indexVerts.Length - 1) % indexVerts.Length] == 1)
				{
					break;
				}
			}

			if(i == indexVerts.Length)
			{
				Base	=0;
			}
			else
			{	//rotate the vertex order
				Base	=Start[i];
			}

			if(!Finalize(Base, tex, ref indexVerts))
			{
				return	false;
			}

			return	true;
		}


		bool Finalize(Int32 Base, TexInfo tex, ref Int32 []indexVerts)
		{
			Int32	i;

			mTotalIndexes	+=mTempIndexes.Count;

			if(mTempIndexes.Count == indexVerts.Length)
			{
				return	true;
			}

			if((tex.mFlags & TexInfo.MIRROR) != 0)
			{
				return	true;
			}

			indexVerts	=new Int32[mTempIndexes.Count];
				
			for(i=0;i < mTempIndexes.Count;i++)
			{
				indexVerts[i]	=mTempIndexes[(i + Base) % mTempIndexes.Count];
			}

			mNumFixedFaces++;

			return	true;
		}


		void SetEdgeVerts()
		{
			mEdgeVerts.Clear();

			for(int i=0;i < mWelded.Count - 1;i++)
			{
				mEdgeVerts.Add(i + 1);
			}
		}


		Int32 WeldVert(Vector3 vert)
		{
			Int32	i=0;
			for(;i < mWelded.Count;i++)
			{
				if(UtilityLib.Mathery.CompareVector(vert, mWelded[i]))
				{
					return	i;
				}
			}

			mWelded.Add(vert);

			return	i;
		}


		bool TestEdge_r(float Start, float End, Int32 p1, Int32 p2, Int32 StartVert)
		{
			Int32	j, k;
			float	Dist;
			Vector3	Delta;
			Vector3	Exact;
			Vector3	Off;
			float	Error;
			Vector3	p;

			if(p1 == p2)
			{
				//GHook.Printf("TestEdge_r:  Degenerate Edge.\n");
				return	true;		// degenerate edge
			}

			for(k=StartVert;k < mEdgeVerts.Count;k++)
			{
				j	=mEdgeVerts[k];

				if(j==p1 || j == p2)
				{
					continue;
				}

				p	=mWelded[j];

				Delta	=p - mEdgeStart;
				Dist	=Vector3.Dot(Delta, mEdgeDir);
				
				if(Dist <= Start || Dist >= End)
				{
					continue;
				}

				Exact	=mEdgeStart + (mEdgeDir * Dist);
				Off		=p - Exact;
				Error	=Off.Length();

				if(Math.Abs(Error) > OFF_EPSILON)
				{
					continue;
				}

				// break the edge
				mNumTJunctions++;

				TestEdge_r(Start, Dist, p1, j, k + 1);
				TestEdge_r(Dist, End, j, p2, k + 1);

				return	true;
			}

			if(mTempIndexes.Count >= MAX_TEMP_INDEX_VERTS)
			{
				CoreEvents.Print("Max Temp Index Verts.\n");
				return	false;
			}

			mTempIndexes.Add(p1);

			return	true;
		}
	}
}
