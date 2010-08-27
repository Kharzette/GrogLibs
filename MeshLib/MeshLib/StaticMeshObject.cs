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


		public void AddMeshPart(StaticMesh m)
		{
			mMeshParts.Add(m);
		}


		public void NukeMesh(StaticMesh m)
		{
			if(mMeshParts.Contains(m))
			{
				mMeshParts.Remove(m);
			}
		}


		public Vector3 GetBoundsCenter()
		{
			Vector3	accum	=Vector3.Zero;
			foreach(StaticMesh sm in mMeshParts)
			{
				accum	+=sm.GetBoundsCenter();
			}

			accum	/=mMeshParts.Count;

			return	accum;
		}


		public void SetAppearance(List<string> meshParts, List<string> materials)
		{
			foreach(StaticMesh m in mMeshParts)
			{
				if(meshParts.Contains(m.Name))
				{
					m.Visible	=true;

					int	idx	=meshParts.IndexOf(m.Name);

					m.MaterialName	=materials[idx];
				}
				else
				{
					m.Visible	=false;
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

			//write a magic number identifying characters
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


		//for drawing with custom fx for passes
		public void Draw(GraphicsDevice gd, Effect fx)
		{
			foreach(StaticMesh m in mMeshParts)
			{
				if(!m.Visible)
				{
					continue;
				}
				m.Draw(gd, fx);
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