using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;

using Device	=SharpDX.Direct3D11.Device;


namespace MeshLib
{
	public interface IArch
	{
		void FreeAll();

		void SetSkin(Skin s);
		Skin GetSkin();

		int GetPartCount();
		void AddPart(Mesh m);
		bool RenamePart(int index, string newName);
		void NukePart(int index);
		void NukeParts(List<int> indexes);
		void NukeVertexElements(Device gd, List<int> indexes, List<int> elements);
		void GenTangents(Device gd, List<int> parts, int texCoordSet);
		string GetPartName(int index);
		Type GetPartVertexType(int index);
		List<EditorMesh.WeightSeam> Frankenstein();

		void Draw(DeviceContext dc, List<MeshMaterial> meshMats);
		void Draw(DeviceContext dc, List<MeshMaterial> meshMats, string altMaterial);
		void DrawDMN(DeviceContext dc, List<MeshMaterial> meshMats);

		void UpdateBounds();
		BoundingBox GetBoxBound();
		BoundingSphere GetSphereBound();
		float? RayIntersect(Vector3 start, Vector3 end, bool bBox, out Mesh partHit);

		void SaveToFile(string fileName);
		bool ReadFromFile(string fileName, Device gd, bool bEditor);
	}

	public class ArchEventArgs : EventArgs
	{
		public IArch		mArch;
		public List<int>	mIndexes;
	}
}
