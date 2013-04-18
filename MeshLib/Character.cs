using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MaterialLib;


namespace MeshLib
{
	public class Character
	{
		//parts
		List<SkinnedMesh>	mMeshParts	=new List<SkinnedMesh>();
		List<Skin>			mSkins		=new List<Skin>();

		//refs to anim and material libs
		MaterialLib.MaterialLib	mMatLib;
		AnimLib					mAnimLib;

		//bounds
		BoundingBox		mBoxBound;
		BoundingSphere	mSphereBound;

		//transform
		Matrix	mTransform;


		public Character(MaterialLib.MaterialLib ml, AnimLib al)
		{
			mMatLib		=ml;
			mAnimLib	=al;
			mTransform	=Matrix.Identity;
		}


		public void SetTransform(Matrix mat)
		{
			mTransform	=mat;
		}


		public void AddMeshPart(Mesh m)
		{
			SkinnedMesh	sm	=m as SkinnedMesh;

			if(sm != null)
			{
				mMeshParts.Add(sm);
			}
		}


		public void NukeMesh(Mesh m)
		{
			SkinnedMesh	sm	=m as SkinnedMesh;

			if(sm != null)
			{
				if(mMeshParts.Contains(sm))
				{
					mMeshParts.Remove(sm);
				}
			}
		}


		public void AddSkin(Skin s)
		{
			mSkins.Add(s);
		}


		public void SetAppearance(List<string> meshParts, List<string> materials)
		{
			foreach(SkinnedMesh m in mMeshParts)
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
		public List<SkinnedMesh> GetMeshPartList()
		{
			return	mMeshParts;
		}


		public void SaveToFile(string fileName)
		{
			FileStream	file	=new FileStream(fileName, FileMode.Create, FileAccess.Write);

			BinaryWriter	bw	=new BinaryWriter(file);

			//write a magic number identifying characters
			UInt32	magic	=0xCA1EC7BE;

			bw.Write(magic);

			//save mesh parts
			bw.Write(mMeshParts.Count);
			foreach(SkinnedMesh m in mMeshParts)
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
			Stream			file	=null;
			if(bEditor)
			{
				file	=new FileStream(fileName, FileMode.Open, FileAccess.Read);
			}
			else
			{
				file	=UtilityLib.FileUtil.OpenTitleFile(fileName);
			}

			if(file == null)
			{
				return	false;
			}
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
				SkinnedMesh	m	=new SkinnedMesh();

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

			mTransform	=Matrix.Identity;

			return	true;
		}


		public void Blend(string anim1, float anim1Time,
			string anim2, float anim2Time, float percentage)
		{
			mAnimLib.Blend(anim1, anim1Time, anim2, anim2Time, percentage);

			foreach(SkinnedMesh m in mMeshParts)
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


		public void Animate(string anim, float time)
		{
			mAnimLib.Animate(anim, time);

			foreach(SkinnedMesh m in mMeshParts)
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


		public float? RayIntersect(Vector3 start, Vector3 end, bool bBox)
		{
			if(bBox)
			{
				return	UtilityLib.Mathery.RayIntersectBox(start, end, mBoxBound);
			}
			else
			{
				return	UtilityLib.Mathery.RayIntersectSphere(start, end, mSphereBound);
			}
		}


		public void UpdateBounds()
		{
			if(mSkins == null || mSkins.Count == 0)
			{
				return;
			}

			Skeleton		skel		=mAnimLib.GetSkeleton();
			List<string>	boneNames	=mSkins[0].GetBoneNames();
			List<Vector3>	points		=new List<Vector3>();

			if(skel == null)
			{
				return;
			}

			foreach(string bone in boneNames)
			{
				Matrix	mat	=Matrix.Identity;
				skel.GetMatrixForBone(bone, out mat);

				mat	*=Matrix.CreateRotationX((float)Math.PI / -2.0f);
				mat	*=Matrix.CreateRotationY((float)Math.PI);

				Vector3	pnt	=Vector3.Zero;

				pnt	=Vector3.Transform(pnt, mat);

				points.Add(pnt);
			}
			mBoxBound		=BoundingBox.CreateFromPoints(points);
			mSphereBound	=UtilityLib.Mathery.SphereFromPoints(points);
		}


		public BoundingBox GetBoxBound()
		{
			return	mBoxBound;
		}


		public BoundingSphere GetSphereBound()
		{
			return	mSphereBound;
		}


		public void Draw(GraphicsDevice gd)
		{
			foreach(SkinnedMesh m in mMeshParts)
			{
				if(!m.Visible)
				{
					continue;
				}
				m.Draw(gd, mMatLib, mTransform, "");
			}
		}


		public void Draw(GraphicsDevice gd, string altMaterial)
		{
			foreach(SkinnedMesh m in mMeshParts)
			{
				if(!m.Visible)
				{
					continue;
				}
				m.Draw(gd, mMatLib, mTransform, altMaterial);
			}
		}
	}
}