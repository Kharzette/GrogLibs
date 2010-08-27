using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MaterialLib;


namespace MeshLib
{
	public class Character
	{
		List<Mesh>	mMeshParts	=new List<Mesh>();
		List<Skin>	mSkins		=new List<Skin>();

		//refs to anim and material libs
		MaterialLib.MaterialLib	mMatLib;
		AnimLib					mAnimLib;

		//events
		public event EventHandler	eRayCollision;


		public Character(MaterialLib.MaterialLib ml, AnimLib al)
		{
			mMatLib		=ml;
			mAnimLib	=al;
		}


		public void AddMeshPart(Mesh m)
		{
			mMeshParts.Add(m);
		}


		public void NukeMesh(Mesh m)
		{
			if(mMeshParts.Contains(m))
			{
				mMeshParts.Remove(m);
			}
		}


		public void AddSkin(Skin s)
		{
			mSkins.Add(s);
		}


		public void SetAppearance(List<string> meshParts, List<string> materials)
		{
			foreach(Mesh m in mMeshParts)
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
		public List<Mesh> GetMeshPartList()
		{
			return	mMeshParts;
		}


		public void SaveToFile(string fileName)
		{
			FileStream	file	=UtilityLib.FileUtil.OpenTitleFile(fileName,
									FileMode.Open, FileAccess.Write);

			BinaryWriter	bw	=new BinaryWriter(file);

			//write a magic number identifying characters
			UInt32	magic	=0xCA1EC7BE;

			bw.Write(magic);

			//save mesh parts
			bw.Write(mMeshParts.Count);
			foreach(Mesh m in mMeshParts)
			{
				m.Write(bw);
			}

			//save skins
			bw.Write(mSkins.Count);
			foreach(Skin sk in mSkins)
			{
				sk.Write(bw);
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
			mSkins.Clear();

			//read magic number
			UInt32	magic	=br.ReadUInt32();

			if(magic != 0xCA1EC7BE)
			{
				br.Close();
				file.Close();
				return	false;
			}

			int	numMesh	=br.ReadInt32();
			for(int i=0;i < numMesh;i++)
			{
				Mesh	m	=new Mesh();

				m.Read(br, gd, bEditor);
				mMeshParts.Add(m);
			}

			int	numSkin	=br.ReadInt32();
			for(int i=0;i < numSkin;i++)
			{
				Skin	sk	=new Skin();

				sk.Read(br);
				mSkins.Add(sk);
			}

			//fix skin refs in meshes
			for(int i=0;i < numMesh;i++)
			{
				int	skidx	=mMeshParts[i].GetSkinIndex();

				mMeshParts[i].SetSkin(mSkins[skidx]);
			}

			br.Close();
			file.Close();
			return	true;
		}


		public void Animate(string anim, float time)
		{
			mAnimLib.Animate(anim, time);

			foreach(Mesh m in mMeshParts)
			{
				if(!m.Visible)
				{
					continue;
				}

				if(m.MaterialName == null || m.MaterialName == "Blank" || m.MaterialName == "")
				{
					continue;	//don't bother unless it can be seen
				}
				m.UpdateBones(mAnimLib.GetSkeleton());
			}
		}


		public void RayIntersectBounds(Vector3 start, Vector3 end)
		{
			foreach(Mesh m in mMeshParts)
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


		public void Draw(GraphicsDevice gd)
		{
			foreach(Mesh m in mMeshParts)
			{
				if(!m.Visible)
				{
					continue;
				}
				m.Draw(gd, mMatLib);
			}
		}
	}
}