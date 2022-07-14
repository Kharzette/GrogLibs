using System;
using System.Numerics;
using System.Collections.Generic;
using Vortice.Direct3D11;
using Vortice.Mathematics;

using MatLib	=MaterialLib.MaterialLib;

namespace MeshLib;

public interface IArch
{
	void FreeAll();

	void SetSkin(Skin s, Skeleton sk);
	Skin GetSkin();

	int GetPartCount();
	void AddPart(Mesh m);
	bool RenamePart(int index, string newName);
	void NukePart(int index);
	void NukeParts(List<int> indexes);
	void NukeVertexElements(ID3D11Device gd, List<int> indexes, List<int> elements);
	void GenTangents(ID3D11Device gd, List<int> parts, int texCoordSet);
	string GetPartName(int index);
	void ReIndexVertWeights(ID3D11Device gd, Dictionary<int, int> idxMap);
	void GetPartBoneNamesInUseByDraw(int index, List<string> names, Skeleton skel);
	Type GetPartVertexType(int index);
	List<EditorMesh.WeightSeam> Frankenstein();
	void GetPartPlanes(int meshIndex, out List<Vector3> normals, out List<float> distances);

	//for collada saving
	int GetPartColladaPolys(int meshIndex, out string polys, out string counts);
	void GetPartColladaPositions(int meshIndex, out float []positions);
	void GetPartColladaNormals(int meshIndex, out float []normals);

	//for bsp interchange
	void GetPartPositions(int meshIndex, out List<Vector3> positions, out List<int> indexes);
	Matrix4x4 GetPartTransform(int meshIndex);

	//the transform here will come from the gameside instance
	//like a particular character or static in a scene with location / orientation inside
	void Draw(MatLib mlib, Matrix4x4 transform, List<MeshMaterial> meshMats);
	void Draw(MatLib mlib, Matrix4x4 transform, List<MeshMaterial> meshMats, string altMaterial);
	void DrawX(MatLib mlib, Matrix4x4 transform, List<MeshMaterial> meshMats, int numInst, string altMaterial);
	void DrawDMN(MatLib mlib, Matrix4x4 transform, List<MeshMaterial> meshMats);

	void UpdateBounds();
	BoundingBox GetBoxBound();
	BoundingSphere GetSphereBound();
	float? RayIntersect(Vector3 start, Vector3 end, bool bBox, out Mesh partHit);

	void SaveToFile(string fileName);
	bool ReadFromFile(string fileName, ID3D11Device gd, bool bEditor);
}

public class ArchEventArgs : EventArgs
{
	public IArch		mArch;
	public List<int>	mIndexes;
}