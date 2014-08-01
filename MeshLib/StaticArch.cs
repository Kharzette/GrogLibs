using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;

//ambiguous stuff
using Color		=SharpDX.Color;
using Device	=SharpDX.Direct3D11.Device;
using MatLib	=MaterialLib.MaterialLib;


namespace MeshLib
{
	//this is a mesh archetype, whereas staticmesh is an instance
	public class StaticArch
	{
		List<Mesh>	mMeshParts	=new List<Mesh>();


		public void FreeAll()
		{
			foreach(Mesh m in mMeshParts)
			{
				m.FreeAll();
			}
		}


		internal bool RenameMesh(int index, string newName)
		{
			if(index < 0 || index >= mMeshParts.Count)
			{
				return	false;
			}

			mMeshParts[index].Name	=newName;

			return	true;
		}


		internal void AddMeshPart(Mesh m)
		{
			if(m != null)
			{
				mMeshParts.Add(m);
			}
		}


		public Mesh GetMeshPart(int index)
		{
			if(index < 0 || index >= mMeshParts.Count)
			{
				return	null;
			}
			return	mMeshParts[index];
		}


		public Mesh GetMeshPart(string name)
		{
			foreach(Mesh m in mMeshParts)
			{
				if(m.Name == name)
				{
					return	m;
				}
			}
			return	null;
		}


		internal void NukeMesh(int index)
		{
			if(index < 0 || index >= mMeshParts.Count)
			{
				return;
			}

			mMeshParts.RemoveAt(index);
		}


		//for gui
		public List<Mesh> GetMeshPartList()
		{
			return	mMeshParts;
		}


		internal void Draw(DeviceContext dc,
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


		internal void DrawDMN(DeviceContext dc,
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

				m.DrawDMN(dc, mm);
			}
		}


		internal float? RayIntersect(Vector3 start, Vector3 end, bool bBox, out Mesh partHit)
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


		public void UpdateBounds()
		{
			foreach(Mesh m in mMeshParts)
			{
				m.Bound();
			}
		}


		public BoundingBox GetBoxBound()
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


		public BoundingSphere GetSphereBound()
		{
			BoundingSphere	merged;
			merged.Center	=Vector3.Zero;
			merged.Radius	=0.0f;
			foreach(Mesh m in mMeshParts)
			{
				BoundingSphere	s			=m.GetSphereBounds();
				Matrix			meshTrans	=m.GetTransform();

				Vector3	pos		=s.Center;

				s.Center	=Vector3.TransformCoordinate(pos, meshTrans);
				
				//this should work but needs testing TODO
				s.Radius	*=meshTrans.ScaleVector.Length();

				merged	=BoundingSphere.Merge(merged, s);
			}
			return	merged;
		}


		//this probably won't work TODO
		public static Dictionary<string, StaticArch> LoadAllMeshes(
			string dir,	Device gd)
		{
			Dictionary<string, StaticArch>	ret	=new Dictionary<string, StaticArch>();

			if(Directory.Exists(dir))
			{
				DirectoryInfo	di	=new DirectoryInfo(dir + "/");

				FileInfo[]		fi	=di.GetFiles("*.Static", SearchOption.TopDirectoryOnly);
				foreach(FileInfo f in fi)
				{
					//strip back
					string	path	=f.DirectoryName;

					StaticArch	smo	=new StaticArch();
					bool	bWorked	=smo.ReadFromFile(path + "\\" + f.Name, gd, false);

					if(bWorked)
					{
						ret.Add(f.Name, smo);
					}
				}
			}

			return	ret;
		}


		public void SaveToFile(string fileName)
		{
			FileStream		file	=new FileStream(fileName, FileMode.Create, FileAccess.Write);
			BinaryWriter	bw		=new BinaryWriter(file);

			//write a magic number identifying a static
			UInt32	magic	=0x57A71C35;

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
		public bool ReadFromFile(string fileName, Device gd, bool bEditor)
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

			if(magic != 0x57A71C35)
			{
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
	}
}
