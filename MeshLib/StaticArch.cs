using System;
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


		public bool RenameMesh(string oldName, string newName)
		{
			foreach(Mesh m in mMeshParts)
			{
				if(m.Name == oldName)
				{
					m.Name	=newName;
					return	true;
				}
			}
			return	false;
		}


		public void AddMeshPart(Mesh m)
		{
			if(m != null)
			{
				mMeshParts.Add(m);
			}
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


		public void NukeMesh(Mesh m)
		{
			if(m != null)
			{
				if(mMeshParts.Contains(m))
				{
					mMeshParts.Remove(m);
				}
			}
		}


		//for gui
		public List<Mesh> GetMeshPartList()
		{
			return	mMeshParts;
		}


		public void AssignMaterialIDs(MaterialLib.IDKeeper keeper)
		{
			foreach(Mesh m in mMeshParts)
			{
				m.AssignMaterialIDs(keeper);
			}
		}


		internal void SetTriLightValues(MatLib mats,
			Vector4 col0, Vector4 col1, Vector4 col2, Vector3 lightDir)
		{
			foreach(Mesh m in mMeshParts)
			{
				mats.SetTriLightValues(m.MaterialName, col0, col1, col2, lightDir);
			}
		}


		internal void Draw(DeviceContext dc,
			MaterialLib.MaterialLib matLib,
			Matrix transform)
		{
			foreach(Mesh m in mMeshParts)
			{
				if(!m.Visible)
				{
					continue;
				}
				m.Draw(dc, matLib, transform);
			}
		}


		internal void DrawDMN(DeviceContext dc,
			MaterialLib.MaterialLib matLib,
			Matrix transform)
		{
			foreach(Mesh m in mMeshParts)
			{
				if(!m.Visible)
				{
					continue;
				}
				m.DrawDMN(dc, matLib, transform);
			}
		}


		internal void Draw(DeviceContext dc,
			MaterialLib.MaterialLib matLib,
			string altMatName,
			Matrix transform)
		{
			foreach(Mesh m in mMeshParts)
			{
				if(!m.Visible)
				{
					continue;
				}

				m.Draw(dc, matLib, transform, altMatName);
			}
		}


		internal float? RayIntersect(Vector3 start, Vector3 end, bool bBox, out Mesh partHit)
		{
			//find which piece was hit
			float		minDist	=float.MaxValue;
			partHit				=null;

			foreach(Mesh m in mMeshParts)
			{
				if(!m.Visible)
				{
					continue;
				}
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
