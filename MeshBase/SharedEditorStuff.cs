using System;
using System.Numerics;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using Vortice.Mathematics;
using Vortice.Direct3D11;

namespace MeshLib;

//editor / tool related code
internal static class SharedMeshStuff
{
	//generate tangents for the parts specified in partIndexes
	internal static void GenTangents(ID3D11Device gd,
		List<Mesh> parts,		
		List<int> partIndexes, int texCoordSet)
	{
		List<EditorMesh>	toChange	=new List<EditorMesh>();
		List<int>			numTris		=new List<int>();
		foreach(int ind in partIndexes)
		{
			Debug.Assert(ind >= 0 && ind < parts.Count);

			if(ind < 0 || ind >= parts.Count)
			{
				continue;
			}
			Mesh		m	=parts[ind];
			EditorMesh	em	=m.GetEditorMesh();
			toChange.Add(em);
			numTris.Add(m.GetNumTriangles());
		}

		for(int i=0;i < toChange.Count;i++)
		{
			if(toChange[i] == null)
			{
				continue;
			}
			toChange[i].GenTangents(gd, texCoordSet, numTris[i]);
		}
	}


	internal static void NukeVertexElements(ID3D11Device gd,
		List<Mesh> parts,
		List<int> indexes,
		List<int> vertElementIndexes)
	{
		List<EditorMesh>	toChange	=new List<EditorMesh>();
		foreach(int ind in indexes)
		{
			Debug.Assert(ind >= 0 && ind < parts.Count);

			if(ind < 0 || ind >= parts.Count)
			{
				continue;
			}

			toChange.Add(parts[ind].GetEditorMesh());
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


	//find borders of shared verts between mesh parts
	internal static List<EditorMesh.WeightSeam> Frankenstein(List<Mesh> parts)
	{
		List<EditorMesh.WeightSeam>	seams	=new List<EditorMesh.WeightSeam>();

		//make a "compared against" dictionary to prevent
		//needless work
		Dictionary<EditorMesh, List<EditorMesh>>	comparedAgainst	=new Dictionary<EditorMesh, List<EditorMesh>>();
		foreach(Mesh m in parts)
		{
			EditorMesh	em	=m.GetEditorMesh();
			comparedAgainst.Add(em, new List<EditorMesh>());
		}

		for(int i=0;i < parts.Count;i++)
		{
			Mesh		meshA	=parts[i];
			EditorMesh	eMeshA	=meshA.GetEditorMesh();
			if(eMeshA == null)
			{
				continue;
			}

			for(int j=0;j < parts.Count;j++)
			{
				if(i == j)
				{
					continue;
				}

				Mesh		meshB	=parts[j];
				EditorMesh	eMeshB	=meshB.GetEditorMesh();
				if(eMeshB == null)
				{
					continue;
				}

				if(comparedAgainst[eMeshA].Contains(eMeshB)
					|| comparedAgainst[eMeshB].Contains(eMeshA))
				{
					continue;
				}

				EditorMesh.WeightSeam	seam	=eMeshA.FindSeam(eMeshB);

				comparedAgainst[eMeshA].Add(eMeshB);

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


	//for conversion to BSP, probably won't work very well
	internal static void GetPartPlanes(Mesh part, Matrix4x4 partXForm,
		out List<Vector3> normals, out List<float> distances)
	{
		if(part == null)
		{
			normals		=new List<Vector3>();
			distances	=new List<float>();
			return;
		}

		EditorMesh	em	=part.GetEditorMesh();
		if(em == null)
		{
			normals		=new List<Vector3>();
			distances	=new List<float>();
			return;
		}

		em.ConvertToBrushes(ref partXForm, out normals, out distances);
	}


	internal static int GetPartColladaPolys(Mesh part, out string polys, out string counts)
	{
		polys	=null;
		counts	=null;
		if(part == null)
		{
			return	0;
		}

		EditorMesh	em	=part.GetEditorMesh();
		if(em == null)
		{
			return	0;
		}

		return	em.GetColladaPolys(out polys, out counts);
	}


	internal static void GetPartPositions(Mesh part, Matrix4x4 partXForm,
		out List<Vector3> positions, out List<int> indexes)
	{
		positions	=null;
		indexes		=null;
		if(part == null)
		{
			return;
		}

		EditorMesh	em	=part.GetEditorMesh();
		if(em == null)
		{
			return;
		}

		em.GetPositions(ref partXForm, out positions, out indexes);
	}


	internal static void GetPartColladaPositions(Mesh part, out float []positions)
	{
		positions	=null;
		if(part == null)
		{
			return;
		}

		EditorMesh	em	=part.GetEditorMesh();
		if(em == null)
		{
			return;
		}

		em.GetColladaPositions(out positions);
	}


	internal static void GetPartColladaNormals(Mesh part, out float []normals)
	{
		normals	=null;
		if(part == null)
		{
			return;
		}

		EditorMesh	em	=part.GetEditorMesh();
		if(em == null)
		{
			return;
		}

		em.GetColladaNormals(out normals);
	}
}