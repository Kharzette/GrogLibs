using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace MeshLib
{
	public class StaticMeshObject
	{
		List<StaticMesh>	mMeshParts	=new List<StaticMesh>();

		//refs to anim and material libs
		MaterialLib.MaterialLib	mMatLib;

		//events
		public event EventHandler	eRayCollision;


		public StaticMeshObject(MaterialLib.MaterialLib ml)
		{
			mMatLib		=ml;
		}


		public void AddMeshPart(Mesh m)
		{
			StaticMesh	sm	=m as StaticMesh;

			if(sm != null)
			{
				mMeshParts.Add(sm);
			}
		}


		public void NukeMesh(Mesh m)
		{
			StaticMesh	sm	=m as StaticMesh;

			if(sm != null)
			{
				if(mMeshParts.Contains(sm))
				{
					mMeshParts.Remove(sm);
				}
			}
		}


		//for gui
		public List<StaticMesh> GetMeshPartList()
		{
			return	mMeshParts;
		}


		public void SaveToFile(string fileName)
		{
			FileStream	file	=UtilityLib.FileUtil.OpenTitleFile(fileName,
									FileMode.Open, FileAccess.Write);

			BinaryWriter	bw	=new BinaryWriter(file);

			//write a magic number identifying a static
			UInt32	magic	=0x57A71C35;

			bw.Write(magic);

			//save mesh parts
			bw.Write(mMeshParts.Count);
			foreach(StaticMesh m in mMeshParts)
			{
				m.Write(bw);
			}

			bw.Close();
			file.Close();
		}


		//set bEditor if you want the buffers set to readable
		//so they can be resaved if need be
		public bool ReadFromFile(string fileName, GraphicsDevice gd, bool bEditor)
		{
			FileStream	file	=UtilityLib.FileUtil.OpenTitleFile(fileName,
									FileMode.Open, FileAccess.Read);

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
				StaticMesh	m	=new StaticMesh();

				m.Read(br, gd, bEditor);
				mMeshParts.Add(m);
			}

			br.Close();
			file.Close();
			return	true;
		}


		public void Draw(GraphicsDevice gd)
		{
			foreach(StaticMesh m in mMeshParts)
			{
				if(!m.Visible)
				{
					continue;
				}
				m.Draw(gd, mMatLib);
			}
		}


		public void RayIntersectBounds(Vector3 start, Vector3 end)
		{
			foreach(StaticMesh m in mMeshParts)
			{
				if(!m.Visible)
				{
					continue;
				}
				if(m.RayIntersectBounds(start, end))
				{
					if(eRayCollision != null)
					{
						eRayCollision(m, null);
					}
				}
			}
		}
	}
}