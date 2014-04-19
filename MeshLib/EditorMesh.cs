using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UtilityLib;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;

//ambiguous stuff
using Buffer = SharpDX.Direct3D11.Buffer;
using Color = SharpDX.Color;
using Device = SharpDX.Direct3D11.Device;

namespace MeshLib
{
	public class EditorMesh : Mesh
	{
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

			BufferDescription	indDesc	=new BufferDescription(mIndArray.Length * 2,
				ResourceUsage.Default, BindFlags.IndexBuffer,
				CpuAccessFlags.None, ResourceOptionFlags.None, 0);

			mIndexs	=Buffer.Create<UInt16>(gd, mIndArray, indDesc);

			mVBinding	=new VertexBufferBinding(mVerts,
				VertexTypes.GetSizeForTypeIndex(mTypeIndex), 0);
		}


		public void SetData(Array vertArray, UInt16 []indArray)
		{
			mVertArray	=vertArray;
			mIndArray	=indArray;
		}


		//weld mesh 2 to this mesh's weights for verts in the same spot
		public void WeldWeights(Device gd, EditorMesh mesh2)
		{
			List<Vector3>	myVerts		=VertexTypes.GetPositions(mVertArray, mTypeIndex);
			List<Vector3>	otherVerts	=VertexTypes.GetPositions(mesh2.mVertArray, mesh2.mTypeIndex);

			Dictionary<int, List<int>>	toWeld	=new Dictionary<int, List<int>>();

			//find == verts
			for(int i=0;i < myVerts.Count;i++)
			{
				for(int j=0;j < otherVerts.Count;j++)
				{
					if(Mathery.CompareVector(myVerts[i], otherVerts[j]))
					{
						if(!toWeld.ContainsKey(i))
						{
							toWeld.Add(i, new List<int>());
						}
						toWeld[i].Add(j);
					}
				}
			}

			//weld
			List<Half4>	myWeights		=VertexTypes.GetWeights(mVertArray, mTypeIndex);
			List<Half4>	otherWeights	=VertexTypes.GetWeights(mesh2.mVertArray, mesh2.mTypeIndex);
			List<Color>	myInds			=VertexTypes.GetBoneIndexes(mVertArray, mTypeIndex);
			List<Color>	otherInds		=VertexTypes.GetBoneIndexes(mesh2.mVertArray, mesh2.mTypeIndex);

			foreach(KeyValuePair<int, List<int>> weldSpot in toWeld)
			{
				Half4	goodWeight	=myWeights[weldSpot.Key];
				Color	goodIdx		=myInds[weldSpot.Key];

				foreach(int ow in weldSpot.Value)
				{
					otherWeights[ow]	=goodWeight;
					otherInds[ow]		=goodIdx;
				}
			}

			VertexTypes.ReplaceWeights(mesh2.mVertArray, otherWeights.ToArray());
			VertexTypes.ReplaceBoneIndexes(mesh2.mVertArray, otherInds.ToArray());

			mesh2.mVerts	=VertexTypes.BuildABuffer(gd, mesh2.mVertArray, mesh2.mTypeIndex);
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
			Vector4	[]tang	=new Vector4[mNumVerts];

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
