using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UtilityLib;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;

//ambiguous stuff
using Color		=SharpDX.Color;
using Device	=SharpDX.Direct3D11.Device;


namespace MeshLib
{
	public class EditorMesh : Mesh
	{
		//for welding weights
		public class WeightSeam
		{
			public EditorMesh					mMeshA, mMeshB;
			public Dictionary<int, List<int>>	mSeam;
			public Dictionary<int, List<int>>	mSnapVerts;
		};

		//extra data for modifying stuff for editors
		Array	mVertArray;
		UInt16	[]mIndArray;


		public EditorMesh(string name) : base(name)
		{
		}


		public override void Write(BinaryWriter bw)
		{
			base.Write(bw);

			VertexTypes.WriteVerts(bw, mVertArray, mTypeIndex);

			bw.Write(mIndArray.Length);

			foreach(UInt16 ind in mIndArray)
			{
				bw.Write(ind);
			}
		}


		public override void Read(BinaryReader br, Device gd, bool bEditor)
		{
			base.Read(br, gd, bEditor);

			VertexTypes.ReadVerts(br, gd, out mVertArray);

			int	indLen	=br.ReadInt32();

			mIndArray	=new UInt16[indLen];

			for(int i=0;i < indLen;i++)
			{
				mIndArray[i]	=br.ReadUInt16();
			}

			mVerts	=VertexTypes.BuildABuffer(gd, mVertArray, mTypeIndex);
			mIndexs	=VertexTypes.BuildAnIndexBuffer(gd, mIndArray);

			mVBBinding	=new VertexBufferBinding(mVerts,
				VertexTypes.GetSizeForTypeIndex(mTypeIndex), 0);
		}


		public void SetData(Array vertArray, UInt16 []indArray)
		{
			mVertArray	=vertArray;
			mIndArray	=indArray;
		}


		//try to find verts that are shared
		public WeightSeam FindSeam(EditorMesh mesh2)
		{
			WeightSeam	ret	=new WeightSeam();

			ret.mMeshA	=this;
			ret.mMeshB	=mesh2;

			//grab positions
			List<Vector3>	myVerts		=VertexTypes.GetPositions(mVertArray, mTypeIndex);
			List<Vector3>	otherVerts	=VertexTypes.GetPositions(mesh2.mVertArray, mesh2.mTypeIndex);

			List<int>	snapped	=new List<int>();

			ret.mSnapVerts	=new Dictionary<int, List<int>>();

			//first find == verts in myVerts
			for(int i=0;i < myVerts.Count;i++)
			{
				//note the j=i below!
				for(int j=i;j < myVerts.Count;j++)
				{
					if(i == j)
					{
						continue;
					}

					if(Mathery.CompareVector(myVerts[i], myVerts[j]))
					{
						if(!ret.mSnapVerts.ContainsKey(i))
						{
							ret.mSnapVerts.Add(i, new List<int>());
						}
						ret.mSnapVerts[i].Add(j);
						snapped.Add(j);
					}
				}
			}

			ret.mSeam	=new Dictionary<int, List<int>>();

			//find == verts
			for(int i=0;i < myVerts.Count;i++)
			{
				if(snapped.Contains(i))
				{
					continue;	//skip stuff snapped to previous verts in myVerts
				}

				for(int j=0;j < otherVerts.Count;j++)
				{
					if(Mathery.CompareVector(myVerts[i], otherVerts[j]))
					{
						if(!ret.mSeam.ContainsKey(i))
						{
							ret.mSeam.Add(i, new List<int>());
						}
						ret.mSeam[i].Add(j);
					}
				}
			}
			return	ret;
		}


		//return true if equal
		bool CompareWeights(Half4 weight0, Color idx0, Half4 weight1, Color idx1)
		{
			//indexes might not be in the same order
			for(int i=0;i < 4;i++)
			{
				byte	colVal;
				Half	weightVal;

				if(i == 0)
				{
					colVal		=idx0.R;
					weightVal	=weight0.X;
				}
				else if(i == 1)
				{
					colVal		=idx0.G;
					weightVal	=weight0.Y;
				}
				else if(i == 2)
				{
					colVal		=idx0.B;
					weightVal	=weight0.Z;
				}
				else
				{
					colVal		=idx0.A;
					weightVal	=weight0.W;
				}

				if(weightVal == 0)
				{
					continue;	//not in use
				}

				if(colVal == idx1.R)
				{
					if(weightVal != weight1.X)
					{
						return	false;
					}
				}
				else if(colVal == idx1.G)
				{
					if(weightVal != weight1.Y)
					{
						return	false;
					}
				}
				else if(colVal == idx1.B)
				{
					if(weightVal != weight1.Z)
					{
						return	false;
					}
				}
				else if(colVal == idx1.A)
				{
					if(weightVal != weight1.W)
					{
						return	false;
					}
				}
				else
				{
					return	false;
				}
			}
			return	true;
		}


		void ContributeWeight(float val, byte index, Dictionary<int, List<float>> weights)
		{
			if(val > 0)
			{
				if(weights.ContainsKey(index))
				{
					weights[index].Add(val);
				}
				else
				{
					weights.Add(index, new List<float>());
					weights[index].Add(val);
				}
			}
		}


		void AverageWeight(List<Half4> weights, List<Color> indexes,
			out Half4 avgWeight, out Color avgIndex)
		{
			Debug.Assert(weights.Count == indexes.Count);

			Dictionary<int, List<float>>	boneVals	=new Dictionary<int,List<float>>();

			for(int i=0;i < weights.Count;i++)
			{
				Half4	w	=weights[i];
				Color	ind	=indexes[i];

				ContributeWeight(w.X, ind.R, boneVals);
				ContributeWeight(w.Y, ind.G, boneVals);
				ContributeWeight(w.Z, ind.B, boneVals);
				ContributeWeight(w.W, ind.A, boneVals);
			}

			//fill in a zero for unweighted values
			foreach(KeyValuePair<int, List<float>> inf in boneVals)
			{
				while(inf.Value.Count < weights.Count)
				{
					inf.Value.Add(0f);
				}
			}

			//pare down to 4
			while(boneVals.Count > 4)
			{
				//find the smallest contributor
				float	best	=float.MaxValue;
				int		bestKey	=0;
				foreach(KeyValuePair<int, List<float>> weight in boneVals)
				{
					if(weight.Value.Sum() < best)
					{
						best	=weight.Value.Sum();
						bestKey	=weight.Key;
					}
				}

				boneVals.Remove(bestKey);
			}

			Vector4	averageWeight	=Vector4.Zero;

			avgIndex	=new Color(0, 0, 0, 0);

			int	count	=0;
			foreach(KeyValuePair<int, List<float>> weight in boneVals)
			{
				if(count == 0)
				{
					avgIndex.R		=(byte)weight.Key;
					averageWeight.X	=weight.Value.Sum() / weight.Value.Count;
				}
				else if(count == 1)
				{
					avgIndex.G		=(byte)weight.Key;
					averageWeight.Y	=weight.Value.Sum() / weight.Value.Count;
				}
				else if(count == 2)
				{
					avgIndex.B		=(byte)weight.Key;
					averageWeight.Z	=weight.Value.Sum() / weight.Value.Count;
				}
				else if(count == 3)
				{
					avgIndex.A		=(byte)weight.Key;
					averageWeight.W	=weight.Value.Sum() / weight.Value.Count;
				}
				count++;
			}

			//weight normalize
			float	totalSize	=averageWeight.X + averageWeight.Y
				+ averageWeight.Z + averageWeight.W;

			if(totalSize != 1f)
			{
				totalSize		=1f / totalSize;
				averageWeight	*=totalSize;
			}

			avgWeight	=averageWeight;
		}


		public void WeldAverage(Device gd, WeightSeam ws)
		{
			//weld
			List<Vector3>	myVerts			=VertexTypes.GetPositions(mVertArray, mTypeIndex);
			List<Vector3>	otherVerts		=VertexTypes.GetPositions(ws.mMeshB.mVertArray, ws.mMeshB.mTypeIndex);
			List<Half4>		myWeights		=VertexTypes.GetWeights(mVertArray, mTypeIndex);
			List<Half4>		otherWeights	=VertexTypes.GetWeights(ws.mMeshB.mVertArray, ws.mMeshB.mTypeIndex);
			List<Color>		myInds			=VertexTypes.GetBoneIndexes(mVertArray, mTypeIndex);
			List<Color>		otherInds		=VertexTypes.GetBoneIndexes(ws.mMeshB.mVertArray, ws.mMeshB.mTypeIndex);

			foreach(KeyValuePair<int, List<int>> weldSpot in ws.mSeam)
			{
				//list of stuff to average
				List<Half4>	weights	=new List<Half4>();
				List<Color>	indexes	=new List<Color>();

				weights.Add(myWeights[weldSpot.Key]);
				indexes.Add(myInds[weldSpot.Key]);

				foreach(int ow in weldSpot.Value)
				{
					weights.Add(otherWeights[ow]);
					indexes.Add(otherInds[ow]);
				}

				Half4	finalWeight;
				Color	finalIndex;

				//average based on meshA's indexing
				AverageWeight(weights, indexes,
					out finalWeight, out finalIndex);

				myWeights[weldSpot.Key]	=finalWeight;
				myInds[weldSpot.Key]	=finalIndex;
				
				foreach(int ow in weldSpot.Value)
				{
					otherWeights[ow]	=finalWeight;
					otherInds[ow]		=finalIndex;
					otherVerts[ow]		=myVerts[weldSpot.Key];
				}
			}

			//copy to snapped
			foreach(KeyValuePair<int, List<int>> snap in ws.mSnapVerts)
			{
				Half4	snappedWeight	=myWeights[snap.Key];
				Color	snappedIndex	=myInds[snap.Key];
				Vector3	snappedPos		=myVerts[snap.Key];

				foreach(int idx in snap.Value)
				{
					myWeights[idx]	=snappedWeight;
					myInds[idx]		=snappedIndex;
					myVerts[idx]	=snappedPos;
				}
			}

			VertexTypes.ReplaceWeights(mVertArray, myWeights.ToArray());
			VertexTypes.ReplaceBoneIndexes(mVertArray, myInds.ToArray());
			VertexTypes.ReplaceWeights(ws.mMeshB.mVertArray, otherWeights.ToArray());
			VertexTypes.ReplaceBoneIndexes(ws.mMeshB.mVertArray, otherInds.ToArray());
			VertexTypes.ReplacePositions(ws.mMeshB.mVertArray, otherVerts.ToArray());
			VertexTypes.ReplacePositions(mVertArray, myVerts.ToArray());

			mVerts.Dispose();
			ws.mMeshB.mVerts.Dispose();

			mVerts				=VertexTypes.BuildABuffer(gd, mVertArray, mTypeIndex);
			ws.mMeshB.mVerts	=VertexTypes.BuildABuffer(gd, ws.mMeshB.mVertArray, ws.mMeshB.mTypeIndex);

			mVBBinding				=new VertexBufferBinding(mVerts, VertexTypes.GetSizeForTypeIndex(mTypeIndex), 0);
			ws.mMeshB.mVBBinding	=new VertexBufferBinding(ws.mMeshB.mVerts,
				VertexTypes.GetSizeForTypeIndex(ws.mMeshB.mTypeIndex), 0);
		}


		//welds the other's seam to this mesh's weights
		public void WeldOtherWeights(Device gd, WeightSeam ws)
		{
			//weld
			List<Vector3>	myVerts			=VertexTypes.GetPositions(mVertArray, mTypeIndex);
			List<Vector3>	otherVerts		=VertexTypes.GetPositions(ws.mMeshB.mVertArray, ws.mMeshB.mTypeIndex);
			List<Half4>		myWeights		=VertexTypes.GetWeights(mVertArray, mTypeIndex);
			List<Half4>		otherWeights	=VertexTypes.GetWeights(ws.mMeshB.mVertArray, ws.mMeshB.mTypeIndex);
			List<Color>		myInds			=VertexTypes.GetBoneIndexes(mVertArray, mTypeIndex);
			List<Color>		otherInds		=VertexTypes.GetBoneIndexes(ws.mMeshB.mVertArray, ws.mMeshB.mTypeIndex);

			foreach(KeyValuePair<int, List<int>> weldSpot in ws.mSeam)
			{
				Half4	goodWeight	=myWeights[weldSpot.Key];
				Color	goodIdx		=myInds[weldSpot.Key];
				Vector3	goodPos		=myVerts[weldSpot.Key];

				foreach(int ow in weldSpot.Value)
				{
					otherWeights[ow]	=goodWeight;
					otherInds[ow]		=goodIdx;
					otherVerts[ow]		=goodPos;
				}
			}

			//snap own snapverts too
			foreach(KeyValuePair<int, List<int>> snap in ws.mSnapVerts)
			{
				Vector3	snapTo		=myVerts[snap.Key];

				foreach(int snapIdx in snap.Value)
				{
					myVerts[snapIdx]	=snapTo;
				}
			}

			VertexTypes.ReplacePositions(mVertArray, myVerts.ToArray());

			VertexTypes.ReplacePositions(ws.mMeshB.mVertArray, otherVerts.ToArray());
			VertexTypes.ReplaceWeights(ws.mMeshB.mVertArray, otherWeights.ToArray());
			VertexTypes.ReplaceBoneIndexes(ws.mMeshB.mVertArray, otherInds.ToArray());

			ws.mMeshB.mVerts.Dispose();

			ws.mMeshB.mVerts	=VertexTypes.BuildABuffer(gd, ws.mMeshB.mVertArray, ws.mMeshB.mTypeIndex);

			ws.mMeshB.mVBBinding	=new VertexBufferBinding(ws.mMeshB.mVerts,
				VertexTypes.GetSizeForTypeIndex(ws.mMeshB.mTypeIndex), 0);
		}


		//welds, taking values from the other mesh
		public void WeldMyWeights(Device gd, WeightSeam ws)
		{
			//flip it around (lazy and slow but less code to maintain)
			WeightSeam	ws2	=ws.mMeshB.FindSeam(this);

			ws.mMeshB.WeldOtherWeights(gd, ws2);
		}


		//borrowed from http://www.terathon.com/code/tangent.html
		public void GenTangents(Device gd, int texCoordSet)
		{
			List<Vector3>	verts	=VertexTypes.GetPositions(mVertArray, mTypeIndex);			
			List<Vector2>	texs	=VertexTypes.GetTexCoord(mVertArray, mTypeIndex, texCoordSet);
			List<Vector3>	norms	=VertexTypes.GetNormals(mVertArray, mTypeIndex);

			if(texs.Count == 0)
			{
				return;
			}

			Vector3	[]stan	=new Vector3[mNumVerts];
			Vector3	[]ttan	=new Vector3[mNumVerts];
			Half4	[]tang	=new Half4[mNumVerts];

			for(int i=0;i < mNumTriangles;i++)
			{
				int	idx0	=mIndArray[0 + (i * 3)];
				int	idx1	=mIndArray[1 + (i * 3)];
				int	idx2	=mIndArray[2 + (i * 3)];

				Vector3	v0	=verts[idx0];
				Vector3	v1	=verts[idx1];
				Vector3	v2	=verts[idx2];

				Vector2	w0	=texs[idx0];
				Vector2	w1	=texs[idx1];
				Vector2	w2	=texs[idx2];

				float	x0	=v1.X - v0.X;
				float	x1	=v2.X - v0.X;
				float	y0	=v1.Y - v0.Y;
				float	y1	=v2.Y - v0.Y;
				float	z0	=v1.Z - v0.Z;
				float	z1	=v2.Z - v0.Z;
				float	s0	=w1.X - w0.X;
				float	s1	=w2.X - w0.X;
				float	t0	=w1.Y - w0.Y;
				float	t1	=w2.Y - w0.Y;
				float	r	=1.0F / (s0 * t1 - s1 * t0);

				Vector3	sdir	=Vector3.Zero;
				Vector3	tdir	=Vector3.Zero;

				sdir.X	=(t1 * x0 - t0 * x1) * r;
				sdir.Y	=(t1 * y0 - t0 * y1) * r;
				sdir.Z	=(t1 * z0 - t0 * z1) * r;

				tdir.X	=(s0 * x1 - s1 * x0) * r;
				tdir.Y	=(s0 * y1 - s1 * y0) * r;
				tdir.Z	=(s0 * z1 - s1 * z0) * r;

				stan[idx0]	=sdir;
				stan[idx1]	=sdir;
				stan[idx2]	=sdir;

				ttan[idx0]	=tdir;
				ttan[idx1]	=tdir;
				ttan[idx2]	=tdir;
			}

			for(int i=0;i < mNumVerts;i++)
			{
				Vector3	norm	=norms[i];
				Vector3	t		=stan[i];

				float	dot	=Vector3.Dot(norm, t);

				Vector3	tan	=t - norm * dot;
				tan.Normalize();

				Vector3	norm2	=Vector3.Cross(norm, t);

				dot	=Vector3.Dot(norm2, ttan[i]);

				float	hand	=(dot < 0.0f)? -1.0f : 1.0f;

				tang[i].X	=tan.X;
				tang[i].Y	=tan.Y;
				tang[i].Z	=tan.Z;
				tang[i].W	=hand;
			}

			mVertArray	=VertexTypes.AddTangents(mVertArray, mTypeIndex, tang, out mTypeIndex);

			mVerts	=VertexTypes.BuildABuffer(gd, mVertArray, mTypeIndex);
		}


		public float[,] LudumDareTerrainDataHack()
		{
			List<Vector3>	verts	=VertexTypes.GetPositions(mVertArray, mTypeIndex);

			//these verts are in random order (oops)

			//sort into y bucketses
			Dictionary<int, List<Vector3>>	yBuckets	=new Dictionary<int,List<Vector3>>();
			for(int y=-25;y < 26;y++)
			{
				yBuckets.Add(y, new List<Vector3>());
			}

			for(int y=-25;y < 26;y++)
			{
				foreach(Vector3 pos in verts)
				{
					if(Mathery.CompareFloat(pos.Y, y))
					{
						if(!yBuckets[y].Contains(pos))
						{
							yBuckets[y].Add(pos);
						}
					}
				}
			}

			for(int y=-25;y < 26;y++)
			{
				yBuckets[y].OrderBy(x => x.X);
			}

			float	[,]ret	=new float[51, 51];

			for(int y=-25;y < 26;y++)
			{
				int	x	=0;
				foreach(Vector3 vec in yBuckets[y])
				{
					ret[y + 25, x++]	=vec.Z;
				}
			}
			return	ret;
		}


		public void NukeVertexElement(List<int> indexes, Device gd)
		{
			mVertArray	=VertexTypes.NukeElements(mVertArray, mTypeIndex, indexes, out mTypeIndex);

			mVerts	=VertexTypes.BuildABuffer(gd, mVertArray, mTypeIndex);
		}


		public override void Bound()
		{
			VertexTypes.GetVertBounds(mVertArray, mTypeIndex, out mBoxBound, out mSphereBound);			
		}
	}
}
