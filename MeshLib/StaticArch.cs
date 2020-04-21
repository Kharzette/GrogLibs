using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using UtilityLib;

//ambiguous stuff
using Color		=SharpDX.Color;
using Device	=SharpDX.Direct3D11.Device;
using MatLib	=MaterialLib.MaterialLib;


namespace MeshLib
{
	//this is a mesh archetype, whereas staticmesh is an instance
	public class StaticArch : IArch
	{
		List<Mesh>	mMeshParts	=new List<Mesh>();


		void IArch.FreeAll()
		{
			foreach(Mesh m in mMeshParts)
			{
				m.FreeAll();
			}
		}


		void IArch.SetSkin(Skin s)
		{
			//statics don't do skin
		}


		Skin IArch.GetSkin()
		{
			return	null;
		}


		bool IArch.RenamePart(int index, string newName)
		{
			if(index < 0 || index >= mMeshParts.Count)
			{
				return	false;
			}

			mMeshParts[index].Name	=newName;

			return	true;
		}


		void IArch.AddPart(Mesh m)
		{
			if(m != null)
			{
				mMeshParts.Add(m);
			}
		}


		int IArch.GetPartCount()
		{
			return	mMeshParts.Count;
		}


		void IArch.GenTangents(Device gd,
			List<int> parts, int texCoordSet)
		{
			List<Mesh>	toChange	=new List<Mesh>();
			foreach(int ind in parts)
			{
				Debug.Assert(ind >= 0 && ind < mMeshParts.Count);

				if(ind < 0 || ind >= mMeshParts.Count)
				{
					continue;
				}

				toChange.Add(mMeshParts[ind]);
			}

			foreach(EditorMesh em in toChange)
			{
				if(em == null)
				{
					continue;
				}
				em.GenTangents(gd, texCoordSet);
			}
		}


		void IArch.NukeVertexElements(Device gd,
			List<int> indexes,
			List<int> vertElementIndexes)
		{
			List<Mesh>	toChange	=new List<Mesh>();
			foreach(int ind in indexes)
			{
				Debug.Assert(ind >= 0 && ind < mMeshParts.Count);

				if(ind < 0 || ind >= mMeshParts.Count)
				{
					continue;
				}

				toChange.Add(mMeshParts[ind]);
			}

			foreach(EditorMesh em in toChange)
			{
				if(em == null)
				{
					continue;
				}
				em.NukeVertexElement(vertElementIndexes, gd);
			}
		}


		void IArch.NukePart(int index)
		{
			if(index < 0 || index >= mMeshParts.Count)
			{
				return;
			}

			mMeshParts.RemoveAt(index);
		}


		void IArch.NukeParts(List<int> indexes)
		{
			List<Mesh>	toNuke	=new List<Mesh>();
			foreach(int ind in indexes)
			{
				Debug.Assert(ind >= 0 && ind < mMeshParts.Count);

				if(ind < 0 || ind >= mMeshParts.Count)
				{
					continue;
				}

				toNuke.Add(mMeshParts[ind]);
			}

			mMeshParts.RemoveAll(mp => toNuke.Contains(mp));

			toNuke.Clear();
		}


		Type IArch.GetPartVertexType(int index)
		{
			if(index < 0 || index >= mMeshParts.Count)
			{
				return	null;
			}
			return	mMeshParts[index].VertexType;
		}


		string IArch.GetPartName(int index)
		{
			if(index < 0 || index >= mMeshParts.Count)
			{
				return	"";
			}
			return	mMeshParts[index].Name;
		}


		//statics don't do bones
		void IArch.GetPartBoneNamesInUseByDraw(int index, List<string> names, Skeleton skel)
		{
			return;
		}

		//statics don't do bones
		void IArch.ReIndexVertWeights(Device gd, Dictionary<int, int> idxMap)
		{
			return;
		}



		void IArch.Draw(DeviceContext dc,
			List<MeshMaterial> meshMats)
		{
			Debug.Assert(meshMats.Count == mMeshParts.Count);

			for(int i=0;i < mMeshParts.Count;i++)
			{
				MeshMaterial	mm	=meshMats[i];

				if(!mm.mbVisible)
				{
					continue;
				}

				Mesh	m	=mMeshParts[i];

				m.Draw(dc, mm);
			}
		}


		void IArch.Draw(DeviceContext dc,
			List<MeshMaterial> meshMats, string altMaterial)
		{
			Debug.Assert(meshMats.Count == mMeshParts.Count);

			for(int i=0;i < mMeshParts.Count;i++)
			{
				MeshMaterial	mm	=meshMats[i];

				if(!mm.mbVisible)
				{
					continue;
				}

				Mesh	m	=mMeshParts[i];

				m.Draw(dc, mm, altMaterial);
			}
		}


		void IArch.DrawDMN(DeviceContext dc, List<MeshMaterial> meshMats)
		{
			Debug.Assert(meshMats.Count == mMeshParts.Count);

			for(int i=0;i < mMeshParts.Count;i++)
			{
				MeshMaterial	mm	=meshMats[i];

				if(!mm.mbVisible)
				{
					continue;
				}

				Mesh	m	=mMeshParts[i];

				m.DrawDMN(dc, mm);
			}
		}


		float? IArch.RayIntersect(Vector3 start, Vector3 end, bool bBox, out Mesh partHit)
		{
			//find which piece was hit
			float		minDist	=float.MaxValue;
			partHit				=null;

			foreach(Mesh m in mMeshParts)
			{
				Nullable<float>	dist	=m.RayIntersect(start, end, bBox);
				if(dist != null)
				{
					if(dist.Value < minDist)
					{
						partHit	=m;
						minDist	=dist.Value;
					}
				}
			}

			if(partHit == null)
			{
				return	null;
			}
			return	minDist;
		}


		void IArch.UpdateBounds()
		{
			foreach(Mesh m in mMeshParts)
			{
				m.Bound();
			}
		}


		BoundingBox IArch.GetBoxBound()
		{
			List<Vector3>	pnts	=new List<Vector3>();
			foreach(Mesh m in mMeshParts)
			{
				BoundingBox	b	=m.GetBoxBounds();

				//internal part transforms
				Vector3	transMin	=Vector3.TransformCoordinate(b.Minimum, m.GetTransform());
				Vector3	transMax	=Vector3.TransformCoordinate(b.Maximum, m.GetTransform());

				pnts.Add(transMin);
				pnts.Add(transMax);
			}

			return	BoundingBox.FromPoints(pnts.ToArray());
		}


		BoundingSphere IArch.GetSphereBound()
		{
			BoundingSphere	merged;
			merged.Center	=Vector3.Zero;
			merged.Radius	=0.0f;
			foreach(Mesh m in mMeshParts)
			{
				BoundingSphere	s			=m.GetSphereBounds();
				Matrix			meshTrans	=m.GetTransform();

				s	=Mathery.TransformSphere(meshTrans, s);

				merged	=BoundingSphere.Merge(merged, s);
			}
			return	merged;
		}


		//find borders of shared verts between mesh parts
		List<EditorMesh.WeightSeam> IArch.Frankenstein()
		{
			List<EditorMesh.WeightSeam>	seams	=new List<EditorMesh.WeightSeam>();

			//make a "compared against" dictionary to prevent
			//needless work
			Dictionary<Mesh, List<Mesh>>	comparedAgainst	=new Dictionary<Mesh, List<Mesh>>();
			foreach(Mesh m in mMeshParts)
			{
				comparedAgainst.Add(m, new List<Mesh>());
			}

			for(int i=0;i < mMeshParts.Count;i++)
			{
				EditorMesh	meshA	=mMeshParts[i] as EditorMesh;
				if(meshA == null)
				{
					continue;
				}

				for(int j=0;j < mMeshParts.Count;j++)
				{
					if(i == j)
					{
						continue;
					}

					EditorMesh	meshB	=mMeshParts[j] as EditorMesh;
					if(meshB == null)
					{
						continue;
					}

					if(comparedAgainst[meshA].Contains(meshB)
						|| comparedAgainst[meshB].Contains(meshA))
					{
						continue;
					}

					EditorMesh.WeightSeam	seam	=meshA.FindSeam(meshB);

					comparedAgainst[meshA].Add(meshB);

					if(seam.mSeam.Count == 0)
					{
						continue;
					}

					Debug.WriteLine("Seam between " + meshA.Name + ", and "
						+ meshB.Name + " :Verts: " + seam.mSeam.Count);

					seams.Add(seam);
				}
			}
			return	seams;
		}


		void IArch.GetPartPlanes(int meshIndex, out List<Vector3> normals, out List<float> distances)
		{
			if(meshIndex < 0 || meshIndex >= mMeshParts.Count)
			{
				normals		=new List<Vector3>();
				distances	=new List<float>();
				return;
			}

			Mesh	m	=mMeshParts[meshIndex];
			if(!(m is EditorMesh))
			{
				normals		=new List<Vector3>();
				distances	=new List<float>();
				return;
			}

			EditorMesh	em	=m as EditorMesh;

			em.ConvertToBrushes(out normals, out distances);
		}


		int IArch.GetPartColladaPolys(int meshIndex, out string polys, out string counts)
		{
			polys	=null;
			counts	=null;
			if(meshIndex < 0 || meshIndex >= mMeshParts.Count)
			{
				return	0;
			}

			Mesh	m	=mMeshParts[meshIndex];
			if(!(m is EditorMesh))
			{
				return	0;
			}

			EditorMesh	em	=m as EditorMesh;

			return	em.GetColladaPolys(out polys, out counts);
		}


		void IArch.GetPartPositions(int meshIndex, out List<Vector3> positions, out List<int> indexes)
		{
			positions	=null;
			indexes		=null;
			if(meshIndex < 0 || meshIndex >= mMeshParts.Count)
			{
				return;
			}

			Mesh	m	=mMeshParts[meshIndex];
			if(!(m is EditorMesh))
			{
				return;
			}

			EditorMesh	em	=m as EditorMesh;

			em.GetPositions(out positions, out indexes);
		}


		void IArch.GetPartColladaPositions(int meshIndex, out float []positions)
		{
			positions	=null;
			if(meshIndex < 0 || meshIndex >= mMeshParts.Count)
			{
				return;
			}

			Mesh	m	=mMeshParts[meshIndex];
			if(!(m is EditorMesh))
			{
				return;
			}

			EditorMesh	em	=m as EditorMesh;

			em.GetColladaPositions(out positions);
		}


		void IArch.GetPartColladaNormals(int meshIndex, out float []normals)
		{
			normals	=null;
			if(meshIndex < 0 || meshIndex >= mMeshParts.Count)
			{
				return;
			}

			Mesh	m	=mMeshParts[meshIndex];
			if(!(m is EditorMesh))
			{
				return;
			}

			EditorMesh	em	=m as EditorMesh;

			em.GetColladaNormals(out normals);
		}


		Matrix IArch.GetPartTransform(int meshIndex)
		{
			if(meshIndex < 0 || meshIndex >= mMeshParts.Count)
			{
				return	Matrix.Identity;
			}
			return	mMeshParts[meshIndex].GetTransform();
		}


		void IArch.SaveToFile(string fileName)
		{
			FileStream		file	=new FileStream(fileName, FileMode.Create, FileAccess.Write);
			BinaryWriter	bw		=new BinaryWriter(file);

			//write a magic number identifying statics
			UInt32	magic	=0x57A71CA8;

			bw.Write(magic);

			//save mesh parts
			bw.Write(mMeshParts.Count);
			foreach(Mesh m in mMeshParts)
			{
				m.Write(bw);
			}

			bw.Close();
			file.Close();
		}


		//set bEditor if you want the buffers set to readable
		//so they can be resaved if need be
		bool IArch.ReadFromFile(string fileName, Device gd, bool bEditor)
		{
			Stream	file	=new FileStream(fileName, FileMode.Open, FileAccess.Read);
			if(file == null)
			{
				return	false;
			}
			BinaryReader	br	=new BinaryReader(file);

			//clear existing data
			mMeshParts.Clear();

			//read magic number
			UInt32	magic	=br.ReadUInt32();

			if(magic != 0x57A71CA8)
			{
				br.Close();
				file.Close();
				return	false;
			}

			int	numMesh	=br.ReadInt32();

			for(int i=0;i < numMesh;i++)
			{
				Mesh	m;

				if(bEditor)
				{
					m	=new EditorMesh("temp");
				}
				else
				{
					m	=new Mesh();
				}

				m.Read(br, gd, bEditor);
				mMeshParts.Add(m);
			}

			br.Close();
			file.Close();

			return	true;
		}


		//for collisionry
		internal Dictionary<Mesh, BoundingBox> GetBoundData()
		{
			Dictionary<Mesh, BoundingBox>	ret	=new Dictionary<Mesh, BoundingBox>();

			foreach(Mesh m in mMeshParts)
			{
				ret.Add(m, m.GetBoxBounds());
			}

			return	ret;
		}


		internal void UpdateBounds()
		{
			foreach(Mesh m in mMeshParts)
			{
				m.Bound();
			}
		}
	}
}