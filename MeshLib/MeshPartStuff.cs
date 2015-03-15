using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;
using SharpDX.Direct3D11;

using MatLib	=MaterialLib.MaterialLib;


namespace MeshLib
{
	//handles collections of meshes
	//used by characters and static meshes
	internal class MeshPartStuff
	{
		IArch	mArch;

		//materials per part
		List<MeshMaterial>	mPartMats	=new List<MeshMaterial>();


		internal MeshPartStuff(IArch arch)
		{
			mArch	=arch;
		}


		internal void FreeAll()
		{
			mPartMats.Clear();

			//arch is likely being used elsewhere
			//don't free it here
			mArch	=null;
		}


		internal bool IsEmpty()
		{
			return	(mPartMats.Count <= 0);
		}


		internal Skin GetSkin()
		{
			return	mArch.GetSkin();
		}


		internal void SetPartVisible(int index, bool bVisible)
		{
			Debug.Assert(index >= 0 && index < mPartMats.Count);

			if(index < 0 || index >= mPartMats.Count)
			{
				return;
			}

			mPartMats[index].mbVisible	=bVisible;
		}


		internal void SetPartMaterialName(int index, string matName)
		{
			Debug.Assert(index >= 0 && index < mPartMats.Count);

			if(index < 0 || index >= mPartMats.Count)
			{
				return;
			}

			mPartMats[index].mMaterialName	=matName;
		}


		internal string GetPartMaterialName(int index)
		{
			Debug.Assert(index >= 0 && index < mPartMats.Count);

			if(index < 0 || index >= mPartMats.Count)
			{
				return	"Nothing";
			}

			return	mPartMats[index].mMaterialName;
		}


		internal void SetMatLibs(MatLib mats)
		{
			foreach(MeshMaterial mm in mPartMats)
			{
				mm.mMatLib	=mats;
			}
		}


		//these need to be kept in sync with the arch's mesh parts
		internal void AddPart(MatLib mats, Matrix objectTrans)
		{
			MeshMaterial	mm	=new MeshMaterial();

			mm.mMatLib			=mats;
			mm.mMaterialName	="NoMaterial";
			mm.mbVisible		=true;
			mm.mObjectTransform	=objectTrans;

			mPartMats.Add(mm);
		}


		internal void NukePart(int index)
		{
			Debug.Assert(index >= 0 && index < mPartMats.Count);

			if(index < 0 || index >= mPartMats.Count)
			{
				return;
			}

			mPartMats.RemoveAt(index);
		}


		internal void NukeParts(List<int> indexes)
		{
			List<MeshMaterial>	toNuke	=new List<MeshMaterial>();
			foreach(int ind in indexes)
			{
				Debug.Assert(ind >= 0 && ind < mPartMats.Count);

				if(ind < 0 || ind >= mPartMats.Count)
				{
					continue;
				}

				toNuke.Add(mPartMats[ind]);
			}

			mPartMats.RemoveAll(mp => toNuke.Contains(mp));

			toNuke.Clear();
		}


		internal void SetMatObjTransforms(Matrix trans)
		{
			foreach(MeshMaterial mm in mPartMats)
			{
				mm.mObjectTransform	=trans;
			}
		}


		internal BoundingBox GetBoxBound()
		{
			return	mArch.GetBoxBound();
		}


		internal BoundingSphere GetSphereBound()
		{
			return	mArch.GetSphereBound();
		}


		internal void SetTriLightValues(
			Vector4 col0, Vector4 col1, Vector4 col2, Vector3 lightDir)
		{
			foreach(MeshMaterial mm in mPartMats)
			{
				mm.mMatLib.SetTriLightValues(
					mm.mMaterialName, col0, col1, col2, lightDir);
			}
		}


		internal void Draw(DeviceContext dc)
		{
			mArch.Draw(dc, mPartMats);
		}


		internal void Draw(DeviceContext dc, string altMaterial)
		{
			mArch.Draw(dc, mPartMats, altMaterial);
		}


		internal void DrawDMN(DeviceContext dc)
		{
			mArch.DrawDMN(dc, mPartMats);
		}


		internal float? RayIntersect(Vector3 start, Vector3 end, bool bBox, out Mesh partHit)
		{
			return	mArch.RayIntersect(start, end, bBox, out partHit);
		}


		internal void Write(BinaryWriter bw)
		{
			bw.Write(mPartMats.Count);

			foreach(MeshMaterial mm in mPartMats)
			{
				mm.Write(bw);
			}
		}


		internal void Read(BinaryReader br)
		{
			mPartMats.Clear();

			int	cnt	=br.ReadInt32();

			for(int i=0;i < cnt;i++)
			{
				MeshMaterial	mm	=new MeshMaterial();

				mm.Read(br);

				mPartMats.Add(mm);
			}
		}


		internal void AssignMaterialIDs(MaterialLib.IDKeeper idk)
		{
			foreach(MeshMaterial mm in mPartMats)
			{
				mm.mMaterialID	=idk.GetID(mm.mMaterialName);
			}
		}


		internal void ComputeBoneBounds(List<string> skipMaterials, Skeleton skeleton)
		{
			CharacterArch	ca	=mArch as CharacterArch;
			if(ca == null)
			{
				return;
			}

			List<int>	skipParts	=new List<int>();
			for(int i=0;i < mPartMats.Count;i++)
			{
				if(!mPartMats[i].mbVisible)
				{
					skipParts.Add(i);
					continue;
				}

				if(skipMaterials.Contains(mPartMats[i].mMaterialName))
				{
					skipParts.Add(i);
					continue;
				}
			}

			ca.ComputeBoneBounds(skeleton, skipParts);
		}
	}
}
